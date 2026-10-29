import { existsSync, readFileSync } from 'node:fs';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { delimiter, dirname, extname, isAbsolute, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { parseArgs } from 'node:util';
import GithubSlugger from 'github-slugger';
import katex from 'katex';
import MarkdownIt from 'markdown-it';
import texmath from 'markdown-it-texmath';
import puppeteer from 'puppeteer-core';

const root = fileURLToPath(new URL('../', import.meta.url));
const githubRoot = 'https://github.com/mkunori/geometry-study-group/blob/main/';
const require = createRequire(import.meta.url);

function findBrowser(explicit) {
  if (explicit) {
    if (!existsSync(explicit)) throw new Error(`Browser not found: ${explicit}`);
    return explicit;
  }
  const candidates = [
    ...[process.env.PROGRAMFILES, process.env['PROGRAMFILES(X86)'], process.env.LOCALAPPDATA]
      .filter(Boolean).flatMap((base) => [
        join(base, 'Microsoft/Edge/Application/msedge.exe'),
        join(base, 'Google/Chrome/Application/chrome.exe'),
      ]),
    '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    '/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge',
    ...(process.env.PATH ?? '').split(delimiter).flatMap((base) =>
      ['google-chrome', 'chromium', 'chromium-browser', 'microsoft-edge'].map((name) => join(base, name))),
  ];
  const browser = candidates.find((candidate) => existsSync(candidate));
  if (!browser) throw new Error('Install Chrome/Edge, or pass --browser-path <executable>.');
  return browser;
}

function githubLink(href, source) {
  const url = new URL(href, source);
  if (href.startsWith('#') || url.protocol !== 'file:') return href;
  const path = relative(root, fileURLToPath(url));
  if (path === '..' || path.startsWith(`..${sep}`) || isAbsolute(path)) {
    throw new Error(`Link is outside the repository: ${href}`);
  }
  const target = new URL(path.split(sep).map(encodeURIComponent).join('/'), githubRoot);
  target.search = url.search;
  target.hash = url.hash;
  return target.href;
}

function embedFile(file) {
  const types = { '.svg': 'image/svg+xml', '.png': 'image/png', '.jpg': 'image/jpeg',
    '.jpeg': 'image/jpeg', '.webp': 'image/webp', '.gif': 'image/gif',
    '.woff2': 'font/woff2', '.woff': 'font/woff', '.ttf': 'font/ttf' };
  const type = types[extname(file).toLowerCase()];
  if (!type) throw new Error(`Unsupported image/font: ${file}`);
  return `data:${type};base64,${readFileSync(file).toString('base64')}`;
}

async function build() {
  const { positionals, values } = parseArgs({
    allowPositionals: true,
    options: { 'browser-path': { type: 'string' }, 'html-preview': { type: 'string' } },
  });
  const [input, output] = positionals;
  if (positionals.length !== 2 || !input?.endsWith('.md') || !output?.endsWith('.pdf')) {
    throw new Error('Usage: npm run pdf -- <input.md> <output.pdf> [--browser-path <executable>] [--html-preview <file.html>]');
  }
  const executablePath = findBrowser(values['browser-path']);
  const source = pathToFileURL(resolve(root, input));
  const destination = resolve(root, output);
  const markdown = await readFile(source, 'utf8');
  const mathErrors = [];
  const engine = { renderToString(tex, options) {
    try { return katex.renderToString(tex, { ...options, throwOnError: true }); }
    catch (error) { mathErrors.push(error.message); return ''; }
  } };
  const md = new MarkdownIt({ html: true }).use(texmath, {
    engine, delimiters: 'dollars', katexOptions: { output: 'htmlAndMathml', trust: false },
  });
  const slugger = new GithubSlugger();
  md.renderer.rules.heading_open = (tokens, index, options, env, renderer) => {
    tokens[index].attrSet('id', slugger.slug(tokens[index + 1].content));
    return renderer.renderToken(tokens, index, options);
  };
  const linkOpen = md.renderer.rules.link_open;
  md.renderer.rules.link_open = (tokens, index, options, env, renderer) => {
    tokens[index].attrSet('href', githubLink(tokens[index].attrGet('href'), source));
    return linkOpen ? linkOpen(tokens, index, options, env, renderer) : renderer.renderToken(tokens, index, options);
  };
  const image = md.renderer.rules.image;
  md.renderer.rules.image = (tokens, index, options, env, renderer) => {
    const url = new URL(tokens[index].attrGet('src'), source);
    if (url.protocol === 'file:') tokens[index].attrSet('src', embedFile(fileURLToPath(url)));
    return image(tokens, index, options, env, renderer);
  };
  const body = md.render(markdown);
  if (mathErrors.length) throw new Error(mathErrors.join('\n'));
  const katexPath = join(dirname(require.resolve('katex')), 'katex.min.css');
  const mathCss = readFileSync(katexPath, 'utf8').replace(/url\(([^)]+)\)/g, (_, path) =>
    `url(${embedFile(resolve(dirname(katexPath), path.replace(/["']/g, '')))})`);
  const printCss = await readFile(new URL('./print.css', import.meta.url), 'utf8');
  const title = markdown.match(/^# (.+)$/m)?.[1] ?? input;
  const html = `<!doctype html><html lang="ja"><meta charset="utf-8"><title>${md.utils.escapeHtml(title)}</title>
<style>${mathCss}\n${printCss}</style><body><article>${body}</article></body></html>`;
  const browser = await puppeteer.launch({ executablePath, headless: true });
  try {
    const page = await browser.newPage();
    await page.setJavaScriptEnabled(false);
    await page.setViewport({ width: 794, height: 1123, deviceScaleFactor: 1 });
    await page.setContent(html, { waitUntil: 'load' });
    await page.emulateMediaType('print');
    const stats = await page.evaluate(async () => {
      await document.fonts.ready;
      await Promise.all([...document.images].map((image) => image.decode()));
      // Keep a short explanatory continuation with its equation, without binding whole chapters.
      for (const equation of document.querySelectorAll('section:has(> eqn)')) {
        const next = equation.nextElementSibling;
        if (next?.tagName === 'P' && next.textContent.length < 100 &&
            /^(なので|と(同じ|置けば|\s)|ここで|基底を変えず|両方)/.test(next.textContent.trim())) {
          next.classList.add('after-equation');
        }
      }
      const overflow = [...document.querySelectorAll('article > *, .katex-display')]
        .filter((element) => element.scrollWidth > element.clientWidth + 2)
        .map((element) => element.textContent.slice(0, 80));
      return { math: document.querySelectorAll('.katex').length, images: document.images.length, overflow };
    });
    if (stats.overflow.length) throw new Error(`Content exceeds the page width: ${stats.overflow.join('; ')}`);
    if (values['html-preview']) await writeFile(resolve(root, values['html-preview']), await page.content());
    await mkdir(dirname(destination), { recursive: true });
    await page.pdf({
      path: destination, format: 'A4', preferCSSPageSize: true, printBackground: true,
      displayHeaderFooter: true, headerTemplate: '<span></span>',
      footerTemplate: '<div style="width:100%;text-align:center;font:9px sans-serif;color:#666"><span class="pageNumber"></span> / <span class="totalPages"></span></div>',
      tagged: true, outline: true,
    });
    console.log(`PDF ready: ${output} (A4 portrait, ${stats.math} math expressions, ${stats.images} images)`);
  } finally {
    await browser.close();
  }
}

build().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});

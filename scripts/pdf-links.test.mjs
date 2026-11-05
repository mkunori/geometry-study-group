import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, relative, sep } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import test from 'node:test';
import { decodePDFRawStream, PDFArray, PDFDocument, PDFName } from 'pdf-lib';
import puppeteer from 'puppeteer-core';
import { comparablePdf } from './published-pdfs.mjs';
import { isReturnNavigation, pdfLinkTarget, pdfReferenceText, preparePdfLinks,
  sourceNotice, sourceRepository } from './pdf-links.mjs';

const root = new URL('../', import.meta.url);
const source = new URL('sessions/01-matrix/explanation.md', root);

test('PDF targets preserve local jumps and external citations, unlinking other repository files', () => {
  for (const href of ['../02-vector/explanation.md', 'examples/README.md',
    'output/explanation.pdf?raw=true', '../../.github/workflows/check-csharp-examples.yml']) {
    assert.equal(pdfLinkTarget(href, source, root), null, href);
  }
  assert.equal(pdfLinkTarget('#学習の流れと表記', source, root), '#学習の流れと表記');
  assert.equal(pdfLinkTarget('explanation.md#target', source, root), '#target');
  for (const href of ['https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector3',
    'https://help.autodesk.com/view/OARX/2026/ENU/',
    'https://www.geometrictools.com/Documentation/MethodOfSeparatingAxes.pdf',
    'https://erich.realtimerendering.com/ptinpoly/']) {
    assert.equal(pdfLinkTarget(href, source, root), href);
  }
  assert.throws(() => pdfLinkTarget('../../../outside.md', source, root), /outside the repository/);
});

test('explicit GitHub URLs are unlinked, including raw, protocol-relative, query and fragment forms', () => {
  for (const href of [`${sourceRepository}/blob/main/README.md`, `${sourceRepository}/issues/98`,
    `${sourceRepository}/pull/92#discussion`, `${sourceRepository}?tab=readme-ov-file`,
    '//github.com/mkunori/geometry-study-group',
    'https://raw.githubusercontent.com/mkunori/geometry-study-group/main/README.md',
    'https://gist.github.com/example/id']) {
    assert.equal(pdfLinkTarget(href, source, root), null, href);
  }
  const external = 'https://notgithub.com/reference';
  assert.equal(pdfLinkTarget(external, source, root), external);
});

test('literal GitHub URLs become readable references while citation URLs and punctuation survive', () => {
  assert.equal(pdfReferenceText(`参照（${sourceRepository}/blob/main/sessions/12-new/homework/solutions.md）。`),
    '参照（第12回の宿題の解答・解説）。');
  assert.equal(pdfReferenceText(`${sourceRepository}/issues/98, ${sourceRepository}/pull/92.`), 'Issue #98, PR #92.');
  assert.equal(pdfReferenceText('github.com/mkunori/geometry-study-group'), '教材リポジトリ');
  assert.equal(pdfReferenceText('https://github.com.'), 'GitHub.');
  assert.equal(pdfReferenceText('https://github.com:443/mkunori/geometry-study-group'), '教材リポジトリ');
  assert.equal(pdfReferenceText('https://github.com/research/paper/blob/main/proof.md'),
    'GitHubの参照資料（research/paper/blob/main/proof.md）');
  for (const text of ['https://learn.microsoft.com/reference', 'https://www.geometrictools.com/paper.pdf',
    'https://notgithub.com/', 'https://github.com.example.org/']) assert.equal(pdfReferenceText(text), text);
});

test('only exact return-navigation labels can be removed', () => {
  for (const label of ['この回のトップへ戻る', '宿題トップへ戻る', 'セッションのトップページへ戻る',
    '勉強会トップへ戻る', '第12回トップへ戻る']) assert.equal(isReturnNavigation(label), true);
  for (const label of ['第2回', '問題ページへ', '前提を確認して、この回のトップへ戻る']) {
    assert.equal(isReturnNavigation(label), false);
  }
});

// Decode Chromium's embedded ToUnicode maps, so the regression checks inspect the
// PDF text itself (including Japanese), rather than merely its preview HTML.
function pdfText(pdf, page) {
  const fonts = page.node.Resources().lookup(PDFName.of('Font'));
  const maps = new Map();
  const unicode = (hex) => new TextDecoder('utf-16be').decode(Buffer.from(hex, 'hex'));
  for (const [name] of fonts.entries()) {
    const font = fonts.lookup(name), stream = font.lookup(PDFName.of('ToUnicode'));
    if (!stream) continue;
    const cmap = Buffer.from(decodePDFRawStream(stream).decode()).toString();
    const glyphs = new Map();
    for (const [, block] of cmap.matchAll(/beginbfchar([\s\S]*?)endbfchar/g)) {
      for (const [, key, value] of block.matchAll(/<([\da-f]+)>\s*<([\da-f]+)>/gi)) glyphs.set(parseInt(key, 16), unicode(value));
    }
    for (const [, block] of cmap.matchAll(/beginbfrange([\s\S]*?)endbfrange/g)) {
      for (const [, first, last, value, array] of block.matchAll(/<([\da-f]+)>\s*<([\da-f]+)>\s*(?:<([\da-f]+)>|\[([^\]]+)\])/gi)) {
        const values = array && [...array.matchAll(/<([\da-f]+)>/gi)].map((m) => m[1]);
        for (let key = parseInt(first, 16); key <= parseInt(last, 16); key++) {
          const offset = key - parseInt(first, 16);
          glyphs.set(key, unicode(values ? values[offset] : (BigInt(`0x${value}`) + BigInt(offset)).toString(16).padStart(value.length, '0')));
        }
      }
    }
    maps.set(name.toString(), glyphs);
  }
  const contents = page.node.Contents();
  const streams = contents instanceof PDFArray ? contents.asArray() : [contents];
  let font, text = '';
  for (const ref of streams) {
    const commands = Buffer.from(decodePDFRawStream(pdf.context.lookup(ref)).decode()).toString();
    for (const match of commands.matchAll(/(\/[^\s]+)\s+[\d.]+\s+Tf|<([\da-f]+)>\s*Tj|\[([^\]]*)\]\s*TJ/gi)) {
      if (match[1]) font = maps.get(match[1]);
      else for (const hex of match[2] ? [match[2]] : [...match[3].matchAll(/<([\da-f]+)>/gi)].map((m) => m[1])) {
        for (const code of hex.match(/.{1,4}/g) ?? []) text += font?.get(parseInt(code, 16)) ?? '';
      }
    }
  }
  return text;
}

test('real PDF retains prose, math, SVG, bookmarks and local jumps with one final repository URL',
  { skip: !process.env.PDF_TEST_BROWSER }, async (t) => {
    const folder = await mkdtemp(join(tmpdir(), 'geometry-pdf-links-'));
    t.after(() => rm(folder, { recursive: true, force: true }));
    const file = join(folder, 'fixture.md'), output = join(folder, 'fixture.pdf'), preview = join(folder, 'fixture.html');
    const local = relative(folder, fileURLToPath(new URL('sessions/02-vector/explanation.md', root))).split(sep).join('/');
    const markdown = String.raw`# PDF link fixture

[Jump](#target) [Named jump](#named-target) [Lesson two](${local}) [Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector3)
[Math citation](https://www.geometrictools.com/Documentation/MethodOfSeparatingAxes.pdf)

本文を残す。[この回のトップへ戻る](${local}) ／ [第2回](${sourceRepository}/blob/main/sessions/02-vector/explanation.md) も参照する。

<a href="${sourceRepository}/issues/98">Issue reference</a>

Raw: ${sourceRepository}/blob/main/sessions/02-vector/explanation.md

    ${sourceRepository}/issues/98

<${sourceRepository}/pull/92>

## target

<a id="named-target">Named target</a>

途中計算と前提を残す。

$$
\begin{bmatrix}
1 & 0 \\
0 & 1
\end{bmatrix}
$$

[![Embedded SVG](fixture.svg)](${local})

${sourceNotice}

${sourceNotice}
`;
    await writeFile(file, markdown);
    await writeFile(join(folder, 'fixture.svg'), '<svg xmlns="http://www.w3.org/2000/svg" width="200" height="40"><rect width="190" height="30" fill="orange"/></svg>');
    const result = spawnSync(process.execPath, [fileURLToPath(new URL('scripts/build-pdf.mjs', root)), file, output,
      '--browser-path', process.env.PDF_TEST_BROWSER, '--html-preview', preview], { encoding: 'utf8' });
    assert.equal(result.status, 0, result.stdout + result.stderr);
    assert.equal(await readFile(file, 'utf8'), markdown);
    const pdf = await PDFDocument.load(await readFile(output));
    const texts = pdf.getPages().map((page) => pdfText(pdf, page));
    const compact = (value) => value.normalize('NFKC').replace(/\s+/g, '');
    const text = compact(texts.join(''));
    assert.equal(text.split(sourceRepository).length - 1, 1);
    assert.equal(text.split(compact(sourceNotice)).length - 1, 1);
    for (const content of ['本文を残す。', 'も参照する。', '途中計算と前提を残す。', 'Lesson two', 'Issue reference', 'Issue #98', '第2回の詳細解説', 'PR #92']) {
      assert.ok(text.includes(compact(content)), `${content}: ${text}`);
    }
    assert.doesNotMatch(text.replace(sourceRepository, ''), /github\.com|githubusercontent\.com|トップへ戻る/);
    assert.ok(pdf.catalog.get(PDFName.of('Outlines')), 'bookmarks');
    const actions = pdf.getPages().flatMap((page) => (page.node.Annots()?.asArray() ?? []).map((ref) => pdf.context.lookup(ref)));
    const uris = actions.map((a) => a.lookup(PDFName.of('A'))?.lookup(PDFName.of('URI'))?.decodeText()).filter(Boolean);
    assert.equal(uris.filter((uri) => uri.includes('github.com')).length, 1);
    assert.ok(uris.includes(sourceRepository));
    assert.ok(uris.includes('https://www.geometrictools.com/Documentation/MethodOfSeparatingAxes.pdf'));
    assert.ok(actions.some((a) => a.has(PDFName.of('Dest')) || a.lookup(PDFName.of('A'))?.get(PDFName.of('S')) === PDFName.of('GoTo')), 'local jump');
    assert.ok(texts.at(-1).replace(/\s+/g, '').includes(sourceRepository), 'notice on final nonblank page');
    const browser = await puppeteer.launch({ executablePath: process.env.PDF_TEST_BROWSER, headless: true });
    t.after(() => browser.close());
    const page = await browser.newPage();
    await page.setContent(await readFile(preview, 'utf8'));
    await preparePdfLinks(page, pathToFileURL(file), root);
    await preparePdfLinks(page, pathToFileURL(file), root);
    assert.equal(await page.$eval('#named-target', (a) => a.textContent), 'Named target');
    assert.deepEqual(await page.evaluate(() => ({ notices: document.querySelectorAll('[data-pdf-source]').length,
      math: document.querySelectorAll('.katex').length, images: document.images.length,
      last: document.querySelector('article').lastElementChild.textContent })),
    { notices: 1, math: 1, images: 1, last: sourceNotice });
    const original = await comparablePdf(await readFile(output));
    // Check real browser rendering changes, not just synthetic PDF fixtures.
    for (const kind of ['text', 'math', 'link', 'svg']) {
      const changed = kind === 'text' ? markdown.replace('途中計算と前提を残す。', '途中計算の説明を変更する。') :
        kind === 'math' ? markdown.replace('1 & 0', '2 & 0') :
        kind === 'link' ? markdown.replace('system.numerics.vector3)', 'system.numerics.vector2)') : markdown;
      await writeFile(file, changed);
      await writeFile(join(folder, 'fixture.svg'), `<svg xmlns="http://www.w3.org/2000/svg" width="200" height="40"><rect width="190" height="30" fill="${kind === 'svg' ? 'blue' : 'orange'}"/></svg>`);
      const rebuilt = spawnSync(process.execPath, [fileURLToPath(new URL('scripts/build-pdf.mjs', root)), file, output,
        '--browser-path', process.env.PDF_TEST_BROWSER], { encoding: 'utf8' });
      assert.equal(rebuilt.status, 0, rebuilt.stdout + rebuilt.stderr);
      assert.notDeepEqual(await comparablePdf(await readFile(output)), original, `${kind} change must be detected`);
    }
  });

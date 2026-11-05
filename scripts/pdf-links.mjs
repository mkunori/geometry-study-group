import { isAbsolute, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

export const sourceRepository = 'https://github.com/mkunori/geometry-study-group';
export const sourceNotice = `資料の元データ（GitHubリポジトリ）： ${sourceRepository}`;

function isGithubHost(host) {
  return /(^|\.)github\.com$|(^|\.)githubusercontent\.com$/i.test(host);
}

// null means keeping the label/content without a clickable destination.
export function pdfLinkTarget(href, source, root) {
  if (href.startsWith('#')) return href;
  const url = new URL(href, source);
  if (isGithubHost(url.hostname)) return null;
  if (url.protocol !== 'file:') return href;
  const path = relative(fileURLToPath(root), fileURLToPath(url));
  if (path === '..' || path.startsWith(`..${sep}`) || isAbsolute(path)) {
    throw new Error(`Link is outside the repository: ${href}`);
  }
  // A file-qualified link to the same document is also a local jump.
  if (url.hash && fileURLToPath(url) === fileURLToPath(source)) return url.hash;
  return null;
}

function githubReference(href) {
  const url = new URL(href.startsWith('//') ? `https:${href}` :
    /^https?:/i.test(href) ? href : `https://${href}`);
  const parts = url.pathname.split('/').filter(Boolean).map((part) => {
    try { return decodeURIComponent(part); } catch { return part; }
  });
  const ownRepository = parts[0] === 'mkunori' && parts[1] === 'geometry-study-group';
  if (ownRepository) {
    const section = parts.indexOf('sessions');
    const session = parts[section + 1]?.match(/^(\d+)-/);
    if (section >= 0 && session) {
      const resource = parts.slice(section + 2).join('/');
      const labels = { 'explanation.md': '詳細解説', 'homework/problems.md': '宿題の問題',
        'homework/solutions.md': '宿題の解答・解説', 'examples/README.md': 'C#実装例の説明',
        'images/README.md': '図解の目次', 'README.md': '資料' };
      return `第${Number(session[1])}回の${labels[resource] ?? parts.at(-1)}`;
    }
    if (/^(issues|pull)$/.test(parts[2]) && /^\d+$/.test(parts[3])) {
      return `${parts[2] === 'issues' ? 'Issue' : 'PR'} #${parts[3]}`;
    }
    if (parts[2] === 'actions') return 'GitHub Actionsの実行結果';
    return parts.length === 2 ? '教材リポジトリ' : `教材リポジトリの${parts.at(-1)}`;
  }
  return parts.length ? `GitHubの参照資料（${parts.join('/')}）` : 'GitHub';
}

// Also catches bare URLs, autolink labels and URLs in code/HTML text nodes.
const githubUrl = /(?<![\w.-])(?:(?:https?:)?\/\/)?(?:[\w-]+\.)*(?:github\.com|githubusercontent\.com)(?![\w-]|\.[\w-])(?::\d+)?(?:[/?#][^\s<>"'`\[\]{}()（）「」『』、。，；]*)?/giu;

export function pdfReferenceText(text) {
  return text.replace(githubUrl, (href) => {
    const trailing = href.match(/[.,;:!?]+$/)?.[0] ?? '';
    return githubReference(href.slice(0, href.length - trailing.length)) + trailing;
  });
}

export function isReturnNavigation(label) {
  return /^(?:この回|セッション|宿題|勉強会|第\d+回|参考コード)(?:の)?トップ(?:ページ)?へ戻る$/.test(label.trim());
}

// Work on the rendered document, so Markdown links and raw HTML share one policy.
// Classification and URL-to-text conversion remain pure and independently testable.
export async function preparePdfLinks(page, source, root) {
  const snapshot = await page.evaluate((notice) => {
    const article = document.querySelector('article');
    article.querySelectorAll('[data-pdf-source]').forEach((element) => element.remove());
    for (const paragraph of article.querySelectorAll('p')) {
      if (paragraph.textContent.replace(/\s+/g, ' ').trim() === notice) paragraph.remove();
    }
    const walker = document.createTreeWalker(article, NodeFilter.SHOW_TEXT);
    const texts = [];
    while (walker.nextNode()) texts.push(walker.currentNode.data);
    return { links: [...article.querySelectorAll('a')].map((a) => ({
      href: a.getAttribute('href') ?? a.getAttribute('xlink:href'), label: a.textContent,
    })), texts };
  }, sourceNotice);
  const links = snapshot.links.map(({ href, label }) => {
    const target = href === null ? null : pdfLinkTarget(href, source, root);
    return { target, remove: href !== null && target === null && isReturnNavigation(label) };
  });
  const texts = snapshot.texts.map(pdfReferenceText);
  await page.evaluate(({ links, texts, repository, notice }) => {
    const article = document.querySelector('article');
    const anchors = [...article.querySelectorAll('a')];
    const walker = document.createTreeWalker(article, NodeFilter.SHOW_TEXT);
    const nodes = [];
    while (walker.nextNode()) nodes.push(walker.currentNode);
    texts.forEach((text, i) => { nodes[i].data = text; });
    const navigationParagraphs = new Set();
    links.forEach(({ target, remove }, i) => {
      const anchor = anchors[i];
      if (remove) {
        const paragraph = anchor.closest('p');
        if (paragraph) navigationParagraphs.add(paragraph);
        // Remove only the separator adjoining a removed navigation link.
        const previous = anchor.previousSibling, next = anchor.nextSibling;
        if (previous?.nodeType === Node.TEXT_NODE && /[／/|]\s*$/.test(previous.data)) {
          previous.data = previous.data.replace(/[／/|]\s*$/, '');
        } else if (next?.nodeType === Node.TEXT_NODE) {
          next.data = next.data.replace(/^\s*[／/|]/, '');
        }
        anchor.remove();
      } else if (target === null) {
        // Keep IDs/named targets and child formatting for local PDF jumps.
        anchor.removeAttribute('href');
        anchor.removeAttribute('xlink:href');
      } else {
        anchor.removeAttribute('xlink:href');
        anchor.setAttribute('href', target);
      }
    });
    for (const paragraph of navigationParagraphs) {
      if (!paragraph.textContent.trim() && !paragraph.querySelector('img,svg,math')) paragraph.remove();
    }
    const footer = document.createElement('p');
    footer.className = 'pdf-source';
    footer.dataset.pdfSource = '';
    footer.append(document.createTextNode(notice.slice(0, -repository.length)));
    const link = document.createElement('a');
    link.href = repository;
    link.textContent = repository;
    footer.append(link);
    article.append(footer);
  }, { links, texts, repository: sourceRepository, notice: sourceNotice });
}

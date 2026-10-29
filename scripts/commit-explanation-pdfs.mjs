import { spawnSync } from 'node:child_process';
import { access } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { publishedPdfs } from './published-pdfs.mjs';

export async function commitPdfs(root) {
  function git(args, allowed = [0]) {
    const result = spawnSync('git', args, { cwd: fileURLToPath(root), encoding: 'utf8' });
    if (result.error) throw result.error;
    if (!allowed.includes(result.status)) throw new Error(`git ${args[0]} failed:\n${result.stderr || result.stdout}`);
    return result;
  }
  if (git(['branch', '--show-current']).stdout.trim() !== 'main') {
    throw new Error('PDF commits are only permitted from the main branch.');
  }
  if (git(['diff', '--cached', '--quiet'], [0, 1]).status !== 0) {
    throw new Error('Refusing to commit an existing staged change.');
  }
  git(['fetch', 'origin', 'main']);
  if (git(['rev-parse', 'HEAD']).stdout !== git(['rev-parse', 'FETCH_HEAD']).stdout) {
    throw new Error('main advanced during generation. No commit was made. Run the workflow again on the latest main.');
  }
  const outputs = (await publishedPdfs(root)).map((target) => target.output);
  for (const output of outputs) await access(new URL(output, root));
  git(['add', '--', ...outputs]);
  const staged = git(['diff', '--cached', '--name-only', '-z']).stdout.split('\0').filter(Boolean);
  if (staged.some((path) => !outputs.includes(path))) throw new Error('Refusing to commit a non-PDF path.');
  if (!staged.length) {
    console.log('No PDF changes. No commit needed.');
    return;
  }
  console.log(`Commit PDFs only:\n${staged.join('\n')}`);
  git(['-c', 'user.name=github-actions[bot]', '-c',
    'user.email=41898282+github-actions[bot]@users.noreply.github.com',
    'commit', '-m', '詳細解説PDFを自動更新']);
  try {
    git(['push', 'origin', 'HEAD:refs/heads/main']);
  } catch (error) {
    throw new Error(`${error.message}\nPush rejected. Check GITHUB_TOKEN contents: write, main rulesets/branch protection, or concurrent main updates. Do not bypass protection; adjust approved settings or switch to PDF update PRs. Rerun on the latest main after resolving the cause.`);
  }
  console.log('PDF update pushed to main.');
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  try { await commitPdfs(new URL('../', import.meta.url)); }
  catch (error) { console.error(error.message); process.exitCode = 1; }
}

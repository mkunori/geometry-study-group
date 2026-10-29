import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';
import test from 'node:test';
import { PDFDocument, PDFName, PDFString } from 'pdf-lib';
import { comparablePdf, publishedPdfs } from './published-pdfs.mjs';
import { buildPublishedPdfs } from './build-published-pdfs.mjs';
import { commitPdfs } from './commit-explanation-pdfs.mjs';

async function temporary(t) {
  const folder = await mkdtemp(join(tmpdir(), 'geometry-pdf-test-'));
  t.after(() => rm(folder, { recursive: true, force: true }));
  return { folder, root: pathToFileURL(`${folder}/`) };
}

async function pdfFixture({ date = '2020-01-01', text = 'original', uri = 'https://example.com/', id = 'node0001', header = id } = {}) {
  const pdf = await PDFDocument.create();
  pdf.setCreationDate(new Date(date));
  pdf.setModificationDate(new Date(date));
  const page = pdf.addPage();
  page.drawText(text);
  page.node.set(PDFName.of('Annots'), pdf.context.obj([{
    Type: 'Annot', Subtype: 'Link', Rect: [0, 0, 10, 10], A: { S: 'URI', URI: PDFString.of(uri) },
  }]));
  const element = pdf.context.register(pdf.context.obj({ Type: 'StructElem', ID: PDFString.of(id),
    A: { O: 'Table', Headers: [PDFString.of(header)] } }));
  const second = pdf.context.register(pdf.context.obj({ Type: 'StructElem', ID: PDFString.of('node5000') }));
  const entries = [[id, element], ['node5000', second]].sort(([a], [b]) => a.localeCompare(b, 'en'));
  const names = pdf.context.register(pdf.context.obj({ Names: entries.flatMap(([key, ref]) => [PDFString.of(key), ref]),
    Limits: [PDFString.of(entries[0][0]), PDFString.of(entries.at(-1)[0])] }));
  pdf.catalog.set(PDFName.of('StructTreeRoot'), pdf.context.obj({ Type: 'StructTreeRoot', K: [element, second], IDTree: { Kids: [names] } }));
  pdf.context.trailerInfo.ID = pdf.context.obj([PDFString.of(id), PDFString.of(id)]);
  return Buffer.from(await pdf.save());
}

test('comparison ignores dates and generated identifiers, retaining text, links and structural relationships', async () => {
  const base = await comparablePdf(await pdfFixture());
  const renamed = await comparablePdf(await pdfFixture({ date: '2026-10-29', id: 'node9999' }));
  assert.deepEqual(base, renamed);
  for (const change of [{ text: 'changed' }, { uri: 'https://example.com/new' }, { header: 'different-node' }]) {
    assert.notDeepEqual(base, await comparablePdf(await pdfFixture(change)));
  }
  await assert.rejects(comparablePdf(Buffer.from('not a PDF')));
});

test('publication list is explicit, unique and cannot escape the session directory', async (t) => {
  const { folder, root } = await temporary(t);
  await mkdir(join(folder, 'scripts'));
  for (const value of [[], ['01-matrix', '01-matrix'], ['../outside'], ['01-matrix/output'], '01-matrix', [null]]) {
    await writeFile(new URL('scripts/pdf-sessions.json', root), JSON.stringify(value));
    await assert.rejects(publishedPdfs(root));
  }
  await writeFile(new URL('scripts/pdf-sessions.json', root), '["01-matrix"]');
  assert.deepEqual(await publishedPdfs(root), [{ input: 'sessions/01-matrix/explanation.md', output: 'sessions/01-matrix/output/explanation.pdf' }]);
});

async function buildFixture(t) {
  const result = await temporary(t);
  const { folder, root } = result;
  await mkdir(join(folder, 'scripts'));
  await writeFile(new URL('scripts/pdf-sessions.json', root), '["01-matrix"]');
  await writeFile(new URL('scripts/build-pdf.mjs', root), `
    import { readFileSync, writeFileSync } from 'node:fs';
    import { dirname, join } from 'node:path';
    readFileSync(process.argv[2]);
    if (process.argv[2].includes('fail')) process.exit(1);
    writeFileSync(process.argv[3], readFileSync(join(dirname(process.argv[2]), 'fixture.pdf')));
  `);
  const base = await pdfFixture();
  const source = new URL('sessions/01-matrix/', root);
  await mkdir(new URL('output/', source), { recursive: true });
  await writeFile(new URL('explanation.md', source), '# Ready');
  await writeFile(new URL('fixture.pdf', source), base);
  await writeFile(new URL('output/explanation.pdf', source), base);
  // This TODO session must remain unpublished unless explicitly selected.
  await mkdir(new URL('sessions/02-vector/', root));
  await writeFile(new URL('sessions/02-vector/explanation.md', root), '# TODO');
  return { ...result, source, base };
}

test('build preserves original bytes on metadata-only changes and publishes a genuine change', async (t) => {
  const { root, source, base } = await buildFixture(t);
  await writeFile(new URL('fixture.pdf', source), await pdfFixture({ date: '2026-10-29', id: 'node9999' }));
  await buildPublishedPdfs(root);
  assert.deepEqual(await readFile(new URL('output/explanation.pdf', source)), base);
  await assert.rejects(readFile(new URL('sessions/02-vector/output/explanation.pdf', root)), { code: 'ENOENT' });
  const changed = await pdfFixture({ text: 'changed' });
  await writeFile(new URL('fixture.pdf', source), changed);
  await buildPublishedPdfs(root);
  assert.deepEqual(await readFile(new URL('output/explanation.pdf', source)), changed);
  await buildPublishedPdfs(root);
  assert.deepEqual(await readFile(new URL('output/explanation.pdf', source)), changed);
});

test('a later generation failure leaves all published PDFs unchanged', async (t) => {
  const { root, source, base } = await buildFixture(t);
  await writeFile(new URL('scripts/pdf-sessions.json', root), '["01-matrix", "02-fail"]');
  await mkdir(new URL('sessions/02-fail/', root));
  await writeFile(new URL('sessions/02-fail/explanation.md', root), '# Ready');
  await writeFile(new URL('fixture.pdf', source), await pdfFixture({ text: 'changed' }));
  await assert.rejects(buildPublishedPdfs(root), /generation failed/);
  assert.deepEqual(await readFile(new URL('output/explanation.pdf', source)), base);
});

function git(folder, ...args) {
  const result = spawnSync('git', args, { cwd: folder, encoding: 'utf8' });
  assert.equal(result.status, 0, `${args.join(' ')}: ${result.stderr}`);
  return result.stdout.trim();
}

async function gitFixture(t) {
  const result = await buildFixture(t);
  const { folder } = result;
  const remote = join(folder, 'remote.git');
  git(folder, 'init', '--bare', remote);
  git(folder, 'init', '--initial-branch=main');
  git(folder, 'config', 'user.name', 'Test');
  git(folder, 'config', 'user.email', 'test@example.com');
  await writeFile(join(folder, '.gitignore'), '/remote.git/\n');
  await writeFile(join(folder, 'README.md'), 'source');
  git(folder, 'add', '.');
  git(folder, 'commit', '-m', '初期状態');
  git(folder, 'remote', 'add', 'origin', remote);
  git(folder, 'push', '-u', 'origin', 'main');
  return { ...result, remote };
}

test('bot commits only selected PDFs; unchanged execution creates no commit', async (t) => {
  const { folder, root, source, remote } = await gitFixture(t);
  const initial = git(folder, 'rev-parse', 'HEAD');
  await commitPdfs(root);
  assert.equal(git(folder, 'rev-parse', 'HEAD'), initial);
  await writeFile(new URL('output/explanation.pdf', source), await pdfFixture({ text: 'changed' }));
  await writeFile(join(folder, 'README.md'), 'unrelated local source change');
  await commitPdfs(root);
  assert.equal(git(folder, 'show', '--format=', '--name-only', 'HEAD'), 'sessions/01-matrix/output/explanation.pdf');
  assert.equal(git(folder, 'log', '-1', '--format=%an <%ae>'), 'github-actions[bot] <41898282+github-actions[bot]@users.noreply.github.com>');
  assert.equal(git(folder, 'log', '-1', '--format=%s'), '詳細解説PDFを自動更新');
  assert.equal(git(remote, 'rev-parse', 'main'), git(folder, 'rev-parse', 'HEAD'));
  const updated = git(folder, 'rev-parse', 'HEAD');
  await commitPdfs(root);
  assert.equal(git(folder, 'rev-parse', 'HEAD'), updated);
  assert.equal(git(folder, 'diff', '--name-only'), 'README.md');
});

test('bot rejects feature branches, pre-staged changes and stale main', async (t) => {
  const { folder, root } = await gitFixture(t);
  git(folder, 'switch', '-c', 'feature');
  await assert.rejects(commitPdfs(root), /only permitted from the main/);
  git(folder, 'switch', 'main');
  await writeFile(join(folder, 'README.md'), 'changed');
  git(folder, 'add', 'README.md');
  await assert.rejects(commitPdfs(root), /existing staged change/);
  git(folder, 'commit', '-m', '未送信の変更');
  await assert.rejects(commitPdfs(root), /main advanced/);
});

test('push rejection explains token permissions and protection without bypassing them', async (t) => {
  const { folder, root, source, remote } = await gitFixture(t);
  await writeFile(join(remote, 'hooks/pre-receive'), '#!/bin/sh\necho "protected branch" >&2\nexit 1\n', { mode: 0o755 });
  const initial = git(remote, 'rev-parse', 'main');
  await writeFile(new URL('output/explanation.pdf', source), await pdfFixture({ text: 'changed' }));
  await assert.rejects(commitPdfs(root), /Push rejected.*Check GITHUB_TOKEN[\s\S]*Do not bypass protection/);
  assert.equal(git(remote, 'rev-parse', 'main'), initial);
});

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdtemp, mkdir, readFile, rm, symlink, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';
import test from 'node:test';
import { PDFDocument, PDFName, PDFString } from 'pdf-lib';
import { comparablePdf, publishedPdfs, publicationMarker } from './published-pdfs.mjs';
import { sessionPdfs } from './pdf-materials.mjs';
import { buildPublishedPdfs } from './build-published-pdfs.mjs';
import { commitPdfs } from './commit-explanation-pdfs.mjs';

async function temporary(t) {
  const folder = await mkdtemp(join(tmpdir(), 'geometry-pdf-test-'));
  t.after(() => rm(folder, { recursive: true, force: true }));
  return { folder, root: pathToFileURL(`${folder}/`) };
}

async function pdfFixture({ date = '2020-01-01', text = 'original', uri = 'https://example.com/', id = 'node0001', header = id,
  math = 'x^2', svg = 'M 0 0 L 10 10' } = {}) {
  const pdf = await PDFDocument.create();
  pdf.setCreationDate(new Date(date));
  pdf.setModificationDate(new Date(date));
  const page = pdf.addPage();
  page.drawText(text);
  page.drawText(math, { y: 50 });
  page.drawSvgPath(svg);
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
  for (const change of [{ text: 'changed' }, { math: 'x^3' }, { svg: 'M 0 0 L 20 30' },
    { uri: 'https://example.com/new' }, { header: 'different-node' }]) {
    assert.notDeepEqual(base, await comparablePdf(await pdfFixture(change)));
  }
  await assert.rejects(comparablePdf(Buffer.from('not a PDF')));
});

async function addSession(root, name, { enabled = true, outputs = true } = {}) {
  const targets = sessionPdfs(`sessions/${name}`);
  const base = await pdfFixture();
  for (const target of targets) {
    const input = new URL(target.input, root);
    await mkdir(new URL('./', input), { recursive: true });
    await writeFile(input, '# Ready\n\nComplete learning material.\n');
    await writeFile(new URL(`${target.input}.pdf`, root), base);
    if (outputs) {
      const output = new URL(target.output, root);
      await mkdir(new URL('./', output), { recursive: true });
      await writeFile(output, base);
    }
  }
  if (enabled !== null) await writeFile(new URL(`sessions/${name}/${publicationMarker}`, root), JSON.stringify({ enabled }));
  return { targets, base, source: new URL(`sessions/${name}/`, root) };
}

test('discovery handles zero, one, five and six sessions in stable order with four documents each', async (t) => {
  const { root } = await temporary(t);
  await mkdir(new URL('sessions/', root));
  assert.deepEqual(await publishedPdfs(root), []);
  const names = ['topic-z', '100-new-topic', '06-curves', 'topic-c', 'topic-b', 'topic-a'];
  for (const name of names) {
    await addSession(root, name);
    const selected = names.slice(0, names.indexOf(name) + 1).sort();
    assert.deepEqual(await publishedPdfs(root), selected.flatMap((n) => sessionPdfs(`sessions/${n}`)));
  }
  await rm(new URL('sessions/topic-z/', root), { recursive: true });
  assert.equal((await publishedPdfs(root)).length, 20);
});

test('unmarked or disabled sessions stay private; enabled missing and unfinished sources are rejected', async (t) => {
  const { root } = await temporary(t);
  const { targets } = await addSession(root, 'ready');
  await addSession(root, 'unmarked', { enabled: null });
  await addSession(root, 'disabled', { enabled: false });
  await writeFile(new URL('sessions/unmarked/explanation.md', root), '# TODO');
  await rm(new URL('sessions/disabled/homework/', root), { recursive: true });
  assert.deepEqual(await publishedPdfs(root), targets);
  for (const target of targets) {
    const file = new URL(target.input, root), original = await readFile(file);
    await rm(file);
    await assert.rejects(publishedPdfs(root), /Required PDF path.*missing/);
    for (const text of ['', '# Title only', '#\n\nBody without a valid title', '# Ready\n\nTODO: write content',
      '# Ready\n\n準備中', '# Ready\n\n## 準備中\n\nPlaceholder content.']) {
      await writeFile(file, text);
      await assert.rejects(publishedPdfs(root), /empty or unfinished/);
    }
    await writeFile(file, original);
  }
});

test('invalid names and marker settings cannot inject duplicate or escaping target paths', async (t) => {
  const { root } = await temporary(t);
  await addSession(root, 'ready');
  const marker = new URL(`sessions/ready/${publicationMarker}`, root);
  for (const value of ['invalid json', 'null', '[]', '{}', '{"enabled":"true"}', '{"enabled":true,"path":"../outside"}', '{"enabled":true,"outputs":["a","a"]}']) {
    await writeFile(marker, value);
    await assert.rejects(publishedPdfs(root), /publication marker/);
  }
  await writeFile(marker, '{"enabled":true}');
  for (const name of ['Bad-Name', 'bad_name', 'bad%2fpath', 'bad..name']) {
    const directory = new URL(`sessions/${encodeURIComponent(name)}/`, root);
    await mkdir(directory);
    await assert.rejects(publishedPdfs(root), /session directory/);
    await rm(directory, { recursive: true });
  }
});

test('session aliases, source directory junctions and output junctions are rejected', async (t) => {
  const { folder, root } = await temporary(t);
  const { source } = await addSession(root, 'ready');
  const type = process.platform === 'win32' ? 'junction' : 'dir';
  const outside = join(folder, 'outside');
  await mkdir(outside);
  const alias = new URL('sessions/alias/', root);
  await symlink(new URL('.', source), alias, type);
  await assert.rejects(publishedPdfs(root), /Session links/);
  await rm(alias);
  for (const part of ['homework', 'output']) {
    const directory = new URL(part, source);
    await rm(directory, { recursive: true });
    await symlink(outside, directory, type);
    await assert.rejects(publishedPdfs(root), /without links/);
    await rm(directory);
    if (part === 'homework') await addSession(root, 'ready');
  }
});

async function buildFixture(t) {
  const result = await temporary(t);
  await mkdir(new URL('scripts/', result.root));
  // Controlled renderer responses isolate orchestration from browser layout.
  await writeFile(new URL('scripts/build-pdf.mjs', result.root), `
    import { readFileSync, writeFileSync } from 'node:fs';
    const input = process.argv[2], text = readFileSync(input, 'utf8');
    if (text.includes('BUILD_FAIL')) process.exit(1);
    writeFileSync(process.argv[3], readFileSync(input + (text.includes('changed') ? '.changed.pdf' : '.pdf')));
  `);
  const session = await addSession(result.root, 'first-topic');
  await addSession(result.root, 'unpublished', { enabled: null, outputs: false });
  await writeFile(new URL('sessions/unpublished/explanation.md', result.root), '# TODO');
  return { ...result, ...session };
}

test('build preserves original bytes on metadata-only changes and publishes a genuine change', async (t) => {
  const { root, targets, base } = await buildFixture(t);
  for (const target of targets) await writeFile(new URL(`${target.input}.pdf`, root), await pdfFixture({ date: '2026-10-29', id: 'node9999' }));
  await buildPublishedPdfs(root);
  for (const target of targets) assert.deepEqual(await readFile(new URL(target.output, root)), base);
  await assert.rejects(readFile(new URL('sessions/unpublished/output/explanation.pdf', root)), { code: 'ENOENT' });
  const changed = await pdfFixture({ text: 'changed' });
  await writeFile(new URL(targets[0].input, root), '# Ready\n\nchanged explanation');
  await writeFile(new URL(`${targets[0].input}.changed.pdf`, root), changed);
  await buildPublishedPdfs(root);
  assert.deepEqual(await readFile(new URL(targets[0].output, root)), changed);
  await buildPublishedPdfs(root);
  assert.deepEqual(await readFile(new URL(targets[0].output, root)), changed);
});

for (const input of ['homework/problems.md', 'homework/solutions.md', 'examples/README.md']) {
  test(`changing only ${input} updates only its PDF across multiple sessions`, async (t) => {
    const { root, targets, base } = await buildFixture(t);
    const later = await addSession(root, 'later-topic');
    const target = later.targets.find((item) => item.input.endsWith(input));
    const changed = await pdfFixture({ text: `changed ${input}` });
    await writeFile(new URL(target.input, root), `# Ready\n\nchanged ${input}`);
    await writeFile(new URL(`${target.input}.changed.pdf`, root), changed);
    await buildPublishedPdfs(root);
    for (const item of [...targets, ...later.targets]) assert.deepEqual(await readFile(new URL(item.output, root)), item === target ? changed : base);
  });
}

test('a later session solution failure prevents every output replacement and bot commit', async (t) => {
  const { root, folder, targets, base, remote } = await gitFixture(t);
  const later = await addSession(root, 'later-topic');
  git(folder, 'add', '.'); git(folder, 'commit', '-m', '次の回を追加'); git(folder, 'push', 'origin', 'main');
  const initial = git(remote, 'rev-parse', 'main');
  await writeFile(new URL(targets[0].input, root), '# Ready\n\nchanged explanation');
  await writeFile(new URL(`${targets[0].input}.changed.pdf`, root), await pdfFixture({ text: 'changed' }));
  await writeFile(new URL('homework/solutions.md', later.source), '# Ready\n\nBUILD_FAIL');
  await assert.rejects((async () => { await buildPublishedPdfs(root); await commitPdfs(root); })(), /generation failed.*solutions.md/);
  for (const target of [...targets, ...later.targets]) assert.deepEqual(await readFile(new URL(target.output, root)), base);
  assert.equal(git(folder, 'diff', '--cached', '--name-only'), '');
  assert.equal(git(remote, 'rev-parse', 'main'), initial);
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
  assert.equal(git(folder, 'show', '--format=', '--name-only', 'HEAD'), 'sessions/first-topic/output/explanation.pdf');
  assert.equal(git(folder, 'log', '-1', '--format=%an <%ae>'), 'github-actions[bot] <41898282+github-actions[bot]@users.noreply.github.com>');
  assert.equal(git(folder, 'log', '-1', '--format=%s'), '配布用PDFを自動更新');
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

test('new session publishes all four PDFs and never stages source, test data or unpublished output', async (t) => {
  const { folder, root, remote } = await gitFixture(t);
  const added = await addSession(root, 'sixth-topic', { outputs: false });
  git(folder, 'add', '.'); git(folder, 'commit', '-m', '新しい回の原本を追加'); git(folder, 'push', 'origin', 'main');
  await buildPublishedPdfs(root);
  await mkdir(new URL('sessions/unpublished/output/', root));
  await writeFile(new URL('sessions/unpublished/output/explanation.pdf', root), added.base);
  await writeFile(new URL('sessions/sixth-topic/explanation.md', root), '# Ready\n\nUncommitted source edit.');
  await writeFile(join(folder, 'test-data.pdf'), added.base);
  await writeFile(join(folder, 'diagram.svg'), '<svg/>');
  await commitPdfs(root);
  assert.deepEqual(git(folder, 'show', '--format=', '--name-only', 'HEAD').split('\n'), added.targets.map((t) => t.output).sort());
  for (const target of added.targets) assert.deepEqual(await readFile(new URL(target.output, root)), added.base);
  assert.equal(git(remote, 'rev-parse', 'main'), git(folder, 'rev-parse', 'HEAD'));
  assert.equal(git(folder, 'diff', '--name-only'), 'sessions/sixth-topic/explanation.md');
  assert.match(git(folder, 'ls-files', '--others', '--exclude-standard'), /test-data.pdf/);
  assert.match(git(folder, 'ls-files', '--others', '--exclude-standard'), /sessions\/unpublished\/output\/explanation.pdf/);
  const head = git(folder, 'rev-parse', 'HEAD');
  await commitPdfs(root);
  assert.equal(git(folder, 'rev-parse', 'HEAD'), head);
});

test('missing one of four PDFs prevents staging, and zero enabled sessions is a no-op', async (t) => {
  const { folder, root, targets, source, base } = await gitFixture(t);
  const head = git(folder, 'rev-parse', 'HEAD');
  await writeFile(new URL(targets[0].output, root), await pdfFixture({ text: 'changed' }));
  const missing = new URL(targets.at(-1).output, root);
  await rm(missing);
  await assert.rejects(commitPdfs(root), { code: 'ENOENT' });
  assert.equal(git(folder, 'diff', '--cached', '--name-only'), '');
  assert.equal(git(folder, 'rev-parse', 'HEAD'), head);
  await writeFile(missing, base);
  await writeFile(new URL(publicationMarker, source), '{"enabled":false}');
  await commitPdfs(root);
  assert.equal(git(folder, 'diff', '--cached', '--name-only'), '');
  assert.equal(git(folder, 'rev-parse', 'HEAD'), head);
});

test('remote main advancing during generation is rejected before staging any PDF', async (t) => {
  const { folder, root, source, remote } = await gitFixture(t);
  const other = join(folder, 'other-checkout');
  git(folder, 'clone', '--branch', 'main', remote, other);
  git(other, 'config', 'user.name', 'Other'); git(other, 'config', 'user.email', 'other@example.com');
  await writeFile(join(other, 'README.md'), 'new main source');
  git(other, 'add', 'README.md'); git(other, 'commit', '-m', 'mainを更新'); git(other, 'push', 'origin', 'main');
  const head = git(folder, 'rev-parse', 'HEAD'), remoteHead = git(remote, 'rev-parse', 'main');
  await writeFile(new URL('output/problems.pdf', source), await pdfFixture({ text: 'generated from old main' }));
  await assert.rejects(commitPdfs(root), /main advanced during generation/);
  assert.equal(git(folder, 'diff', '--cached', '--name-only'), '');
  assert.equal(git(folder, 'rev-parse', 'HEAD'), head);
  assert.equal(git(remote, 'rev-parse', 'main'), remoteHead);
});

test('workflow watches four sources and publication markers while preserving main-only sandboxed writes', async () => {
  const workflow = await readFile(new URL('../.github/workflows/build-explanation-pdfs.yml', import.meta.url), 'utf8');
  for (const path of ['explanation.md', 'homework/problems.md', 'homework/solutions.md', 'examples/README.md', 'images/**', publicationMarker]) {
    assert.ok(workflow.includes(`'sessions/**/${path}'`), `missing trigger: ${path}`);
  }
  assert.doesNotMatch(workflow, /pull_request|--no-sandbox|cancel-in-progress:\s*true/);
  assert.match(workflow, /branches: \[main\]/);
  assert.match(workflow, /if: github.ref == 'refs\/heads\/main'/);
  assert.match(workflow, /permissions:\s*contents: read/);
  assert.match(workflow, /permissions:\s*contents: write/);
  assert.match(workflow, /Verify Chrome sandbox/);
  assert.ok(workflow.indexOf('Generate published material PDFs') < workflow.indexOf('Commit changed PDFs only'));
  assert.doesNotMatch(workflow, /sessions\/[^*'\s]+\/output/);
});

test('push rejection explains token permissions and protection without bypassing them', async (t) => {
  const { folder, root, source, remote } = await gitFixture(t);
  await writeFile(join(remote, 'hooks/pre-receive'), '#!/bin/sh\necho "protected branch" >&2\nexit 1\n', { mode: 0o755 });
  const initial = git(remote, 'rev-parse', 'main');
  await writeFile(new URL('output/explanation.pdf', source), await pdfFixture({ text: 'changed' }));
  await assert.rejects(commitPdfs(root), /Push rejected.*Check GITHUB_TOKEN[\s\S]*Do not bypass protection/);
  assert.equal(git(remote, 'rev-parse', 'main'), initial);
});

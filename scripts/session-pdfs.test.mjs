import assert from 'node:assert/strict';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { pathToFileURL } from 'node:url';
import test from 'node:test';
import { PDFDocument } from 'pdf-lib';
import { buildPdfTargets } from './build-published-pdfs.mjs';
import { sessionPdfs } from './session-pdfs.mjs';

test('four documents map to distinct sources and outputs without a fixed session count', () => {
  for (const directory of ['sessions/06-curves', 'sessions/100-future-topic', 'sessions/new-topic']) {
    assert.deepEqual(sessionPdfs(directory), [
      { input: `${directory}/explanation.md`, output: `${directory}/output/explanation.pdf` },
      { input: `${directory}/homework/problems.md`, output: `${directory}/output/problems.pdf` },
      { input: `${directory}/homework/solutions.md`, output: `${directory}/output/solutions.pdf` },
      { input: `${directory}/examples/README.md`, output: `${directory}/output/examples.pdf` },
    ]);
  }
  assert.deepEqual(sessionPdfs('sessions\\06-curves\\'), sessionPdfs('sessions/06-curves'));
  for (const value of ['../outside', 'sessions/../outside', '/sessions/topic', 'sessions/topic/output', 'sessions/topic.md']) {
    assert.throws(() => sessionPdfs(value), /session directory/);
  }
});

test('batch retains four distinct documents, ignores metadata changes and publishes nothing on failure', async (t) => {
  const directory = await mkdtemp(join(tmpdir(), 'geometry-session-test-'));
  t.after(() => rm(directory, { recursive: true, force: true }));
  const root = pathToFileURL(`${directory}/`), targets = sessionPdfs('sessions/future-topic');
  await mkdir(new URL('scripts/', root));
  // A fake renderer isolates batch behavior; real rendering is checked by pdf-links.test.mjs.
  await writeFile(new URL('scripts/build-pdf.mjs', root), `
    import { readFileSync, writeFileSync } from 'node:fs';
    writeFileSync(process.argv[3], readFileSync(process.argv[2] + '.pdf'));
  `);
  const originals = [];
  for (const [index, target] of targets.entries()) {
    await mkdir(new URL(target.input.slice(0, target.input.lastIndexOf('/') + 1), root), { recursive: true });
    const pdf = await PDFDocument.create();
    pdf.addPage().drawText(target.input);
    originals.push(Buffer.from(await pdf.save()));
    await writeFile(new URL(target.input, root), `# Document ${index}`);
    await writeFile(new URL(`${target.input}.pdf`, root), originals[index]);
  }
  await buildPdfTargets(root, targets);
  for (const [index, target] of targets.entries()) {
    assert.deepEqual(await readFile(new URL(target.output, root)), originals[index]);
    const pdf = await PDFDocument.load(originals[index]);
    pdf.setModificationDate(new Date('2030-01-01'));
    await writeFile(new URL(`${target.input}.pdf`, root), await pdf.save());
  }
  await buildPdfTargets(root, targets);
  for (const [index, target] of targets.entries()) {
    assert.deepEqual(await readFile(new URL(target.output, root)), originals[index]);
  }
  const changed = await PDFDocument.create();
  changed.addPage().drawText('changed');
  await writeFile(new URL(`${targets[0].input}.pdf`, root), await changed.save());
  await rm(new URL(`${targets.at(-1).input}.pdf`, root));
  await assert.rejects(buildPdfTargets(root, targets), /generation failed/);
  for (const [index, target] of targets.entries()) {
    assert.deepEqual(await readFile(new URL(target.output, root)), originals[index]);
  }
});

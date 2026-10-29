import { spawnSync } from 'node:child_process';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';
import { comparablePdf, publishedPdfs } from './published-pdfs.mjs';

export async function buildPublishedPdfs(root, browserPath) {
  const targets = await publishedPdfs(root);
  const temporary = await mkdtemp(join(tmpdir(), 'geometry-pdfs-'));
  const updates = [];
  try {
    for (const [index, target] of targets.entries()) {
      console.log(`Build: ${target.input} -> ${target.output}`);
      const output = join(temporary, `${index}.pdf`);
      const args = [fileURLToPath(new URL('scripts/build-pdf.mjs', root)), target.input, output];
      if (browserPath) args.push('--browser-path', browserPath);
      const result = spawnSync(process.execPath, args, { cwd: fileURLToPath(root), stdio: 'inherit' });
      if (result.error) throw result.error;
      if (result.status !== 0) throw new Error(`PDF generation failed: ${target.input}`);
      const bytes = await readFile(output);
      const normalized = await comparablePdf(bytes);
      let old;
      try { old = await readFile(new URL(target.output, root)); }
      catch (error) { if (error.code !== 'ENOENT') throw error; }
      if (old && normalized.equals(await comparablePdf(old))) {
        console.log(`Unchanged (including metadata-only differences): ${target.output}`);
      } else {
        updates.push({ ...target, bytes });
      }
    }
    // Do not replace any published PDF until every target has built successfully.
    for (const { output, bytes } of updates) {
      const destination = fileURLToPath(new URL(output, root));
      await mkdir(dirname(destination), { recursive: true });
      await writeFile(destination, bytes);
      console.log(`Updated: ${output}`);
    }
    console.log(`PDFs: ${targets.length} built, ${updates.length} updated.`);
  } finally {
    await rm(temporary, { recursive: true, force: true });
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  try {
    const { values, positionals } = parseArgs({ options: { 'browser-path': { type: 'string' } } });
    if (positionals.length) throw new Error('Unexpected positional arguments.');
    await buildPublishedPdfs(new URL('../', import.meta.url), values['browser-path']);
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}

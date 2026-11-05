import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';
import { buildPdfTargets } from './build-published-pdfs.mjs';
import { sessionPdfs } from './pdf-materials.mjs';
export { pdfMaterials, sessionPdfs } from './pdf-materials.mjs';

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  try {
    const { values, positionals } = parseArgs({
      allowPositionals: true, options: { 'browser-path': { type: 'string' } },
    });
    if (positionals.length !== 1) {
      throw new Error('Usage: npm run pdf:session -- sessions/<session> [--browser-path <executable>]');
    }
    await buildPdfTargets(new URL('../', import.meta.url), sessionPdfs(positionals[0]), values['browser-path']);
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}

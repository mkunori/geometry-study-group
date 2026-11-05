import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';
import { buildPdfTargets } from './build-published-pdfs.mjs';

// Source files remain separate; each PDF contains exactly one Markdown document.
export const pdfMaterials = [
  { input: 'explanation.md', output: 'explanation.pdf' },
  { input: 'homework/problems.md', output: 'problems.pdf' },
  { input: 'homework/solutions.md', output: 'solutions.pdf' },
  { input: 'examples/README.md', output: 'examples.pdf' },
];

export function sessionPdfs(session) {
  const directory = session.replaceAll('\\', '/').replace(/\/$/, '');
  if (!/^sessions\/[a-z0-9]+(?:-[a-z0-9]+)*$/.test(directory)) {
    throw new Error('Specify a session directory inside sessions/ (for example sessions/<session>).');
  }
  return pdfMaterials.map(({ input, output }) => ({
    input: `${directory}/${input}`, output: `${directory}/output/${output}`,
  }));
}

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

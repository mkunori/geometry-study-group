import { readFile, writeFile } from 'node:fs/promises';
import { isAbsolute, relative, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { marpCli } from '@marp-team/marp-cli';
import { PDFDict, PDFDocument, PDFName, PDFString } from 'pdf-lib';

const root = fileURLToPath(new URL('../', import.meta.url));
const githubRoot = 'https://github.com/mkunori/geometry-study-group/blob/main/';
const [input, output, ...options] = process.argv.slice(2);

async function build() {
  if (!input?.endsWith('.md') || !output?.endsWith('.pdf')) {
    throw new Error('Usage: npm run pdf -- <slides.md> <output.pdf> [Marp CLI options]');
  }

  const destination = resolve(root, output);
  const status = await marpCli([
    resolve(root, input), '--pdf', '--allow-local-files', '--output', destination,
    ...options,
  ]);
  if (status !== 0) {
    process.exitCode = status;
    return;
  }

  // Keep local images in the source, but make PDF handout links work on any PC.
  const pdf = await PDFDocument.load(await readFile(destination));
  let converted = 0;
  for (const page of pdf.getPages()) {
    for (const ref of page.node.Annots()?.asArray() ?? []) {
      const annotation = pdf.context.lookup(ref, PDFDict);
      const action = annotation.lookupMaybe(PDFName.of('A'), PDFDict);
      if (action?.get(PDFName.of('S')) !== PDFName.of('URI')) continue;
      const uri = action.lookup(PDFName.of('URI'), PDFString).decodeText();
      if (!uri.startsWith('file:')) continue;

      const url = new URL(uri);
      const path = relative(root, fileURLToPath(url));
      if (path === '..' || path.startsWith(`..${sep}`) || isAbsolute(path)) {
        throw new Error(`PDF link is outside the repository: ${uri}`);
      }
      const target = new URL(path.split(sep).map(encodeURIComponent).join('/'), githubRoot);
      target.hash = url.hash;
      action.set(PDFName.of('URI'), PDFString.of(target.href));
      converted += 1;
    }
  }
  await writeFile(destination, await pdf.save());
  console.log(`PDF ready: ${output} (${pdf.getPageCount()} pages, ${converted} GitHub links)`);
}

build().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});

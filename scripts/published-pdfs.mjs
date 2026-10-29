import { readFile } from 'node:fs/promises';
import { PDFArray, PDFDict, PDFDocument, PDFName, PDFString } from 'pdf-lib';

export async function publishedPdfs(root) {
  const sessions = JSON.parse(await readFile(new URL('scripts/pdf-sessions.json', root), 'utf8'));
  if (!Array.isArray(sessions) || !sessions.length || new Set(sessions).size !== sessions.length ||
      sessions.some((name) => typeof name !== 'string' || !/^\d{2}-[a-z0-9]+(?:-[a-z0-9]+)*$/.test(name))) {
    throw new Error('pdf-sessions.json must contain a nonempty, unique list of session directory names.');
  }
  return sessions.map((name) => ({
    input: `sessions/${name}/explanation.md`,
    output: `sessions/${name}/output/explanation.pdf`,
  }));
}

// Compare all PDF objects, including text, links, outlines and accessibility tags.
// Volatile dates, trailer identifiers and Chromium accessibility node names
// are canonicalized. The structure and all references between nodes remain.
export async function comparablePdf(bytes) {
  const pdf = await PDFDocument.load(bytes, { updateMetadata: false });
  const info = pdf.context.lookup(pdf.context.trailerInfo.Info);
  info?.delete(PDFName.of('CreationDate'));
  info?.delete(PDFName.of('ModDate'));
  delete pdf.context.trailerInfo.ID;
  const ids = new Map();
  const structures = [];
  for (const [, object] of pdf.context.enumerateIndirectObjects()) {
    if (object instanceof PDFDict && object.get(PDFName.of('Type')) === PDFName.of('StructElem')) {
      structures.push(object);
      const id = object.lookup(PDFName.of('ID'));
      if (id) ids.set(id.decodeText(), `structure-${ids.size}`);
    }
  }
  function replaceNames(array) {
    for (let index = 0; index < array.size(); index++) {
      const value = array.lookup(index);
      const canonical = ids.get(value.decodeText());
      if (canonical) array.set(index, PDFString.of(canonical));
    }
  }
  for (const element of structures) {
    const id = element.lookup(PDFName.of('ID'));
    if (id) element.set(PDFName.of('ID'), PDFString.of(ids.get(id.decodeText())));
    const attributes = element.lookup(PDFName.of('A'));
    const entries = attributes instanceof PDFArray ? attributes.asArray().map((ref) => pdf.context.lookup(ref)) : [attributes];
    for (const attribute of entries) {
      if (attribute instanceof PDFDict && attribute.get(PDFName.of('O')) === PDFName.of('Table')) {
        const headers = attribute.lookup(PDFName.of('Headers'));
        if (headers) replaceNames(headers);
      }
    }
  }
  const tree = pdf.catalog.lookup(PDFName.of('StructTreeRoot'));
  const idTree = tree?.lookup(PDFName.of('IDTree'));
  if (idTree) {
    function collect(node) {
      const pairs = node.lookup(PDFName.of('Names'));
      const entries = [];
      if (pairs) for (let i = 0; i < pairs.size(); i += 2) {
        const key = pairs.lookup(i).decodeText();
        entries.push([ids.get(key) ?? key, pairs.get(i + 1)]);
      }
      entries.sort(([a], [b]) => a.localeCompare(b, 'en'));
      if (pairs) node.set(PDFName.of('Names'), pdf.context.obj(entries.flatMap(([key, ref]) => [PDFString.of(key), ref])));
      const subtree = [...entries];
      const children = node.lookup(PDFName.of('Kids'));
      if (children) for (let i = 0; i < children.size(); i++) subtree.push(...collect(children.lookup(i)));
      subtree.sort(([a], [b]) => a.localeCompare(b, 'en'));
      if (node.has(PDFName.of('Limits')) && subtree.length) {
        node.set(PDFName.of('Limits'), pdf.context.obj([PDFString.of(subtree[0][0]), PDFString.of(subtree.at(-1)[0])]));
      }
      return subtree;
    }
    const names = collect(idTree);
    // Reuse the root object; keeping it avoids leaving stale names in the PDF context.
    idTree.delete(PDFName.of('Kids'));
    idTree.delete(PDFName.of('Limits'));
    idTree.set(PDFName.of('Names'), pdf.context.obj(names.flatMap(([key, ref]) => [PDFString.of(key), ref])));
  }
  return Buffer.from(await pdf.save({ useObjectStreams: false }));
}

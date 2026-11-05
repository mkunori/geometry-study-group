import { lstat, readdir, readFile } from 'node:fs/promises';
import { PDFArray, PDFDict, PDFDocument, PDFName, PDFString } from 'pdf-lib';
import { sessionPdfs } from './pdf-materials.mjs';

export const publicationMarker = 'pdf-publish.json';

// Reject links/junctions in every path component, including output paths that
// already exist. Missing outputs are allowed; missing enabled sources are not.
async function plainPath(root, path, { optional = false, directory = false } = {}) {
  const parts = path.split('/');
  for (let index = 0; index < parts.length; index++) {
    const current = parts.slice(0, index + 1).join('/');
    let stat;
    try { stat = await lstat(new URL(current, root)); }
    catch (error) {
      if (optional && error.code === 'ENOENT') return false;
      throw new Error(`Required PDF path is missing or unreadable: ${current}`, { cause: error });
    }
    const expectDirectory = index < parts.length - 1 || directory;
    if (stat.isSymbolicLink() || (expectDirectory ? !stat.isDirectory() : !stat.isFile())) {
      throw new Error(`PDF path must be a regular ${expectDirectory ? 'directory' : 'file'} without links: ${current}`);
    }
  }
  return true;
}

export async function publishedPdfs(root) {
  await plainPath(root, 'sessions', { directory: true });
  const entries = await readdir(new URL('sessions/', root), { withFileTypes: true });
  const targets = [], outputs = new Set();
  for (const entry of entries.sort((a, b) => a.name < b.name ? -1 : a.name > b.name ? 1 : 0)) {
    if (entry.isSymbolicLink()) throw new Error(`Session links are not allowed: sessions/${entry.name}`);
    if (!entry.isDirectory()) continue;
    const directory = `sessions/${entry.name}`;
    const materials = sessionPdfs(directory); // Also validates the directory name.
    const marker = `${directory}/${publicationMarker}`;
    if (!await plainPath(root, marker, { optional: true })) continue;
    let setting;
    try { setting = JSON.parse(await readFile(new URL(marker, root), 'utf8')); }
    catch (error) { throw new Error(`Invalid PDF publication marker: ${marker}`, { cause: error }); }
    if (!setting || Array.isArray(setting) || Object.keys(setting).length !== 1 || typeof setting.enabled !== 'boolean') {
      throw new Error(`PDF publication marker must contain only { "enabled": true | false }: ${marker}`);
    }
    if (!setting.enabled) continue;
    for (const target of materials) {
      await plainPath(root, target.input);
      const source = (await readFile(new URL(target.input, root), 'utf8')).trim();
      if (!/^#[\t ]+\S/m.test(source) || !source.replace(/^#+[^\n]*$/gm, '').trim() ||
          /\b(?:TODO|TBD)\b|^[\t ]*(?:(?:#{1,6}|[-*])[\t ]+)?(?:未作成|作成予定|準備中|下書き)(?:[。．:：\s]|$)/imu.test(source)) {
        throw new Error(`Enabled PDF source is empty or unfinished: ${target.input}`);
      }
      await plainPath(root, target.output, { optional: true });
      const key = target.output.toLowerCase();
      if (outputs.has(key)) throw new Error(`Duplicate PDF output: ${target.output}`);
      outputs.add(key);
      targets.push(target);
    }
  }
  return targets;
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

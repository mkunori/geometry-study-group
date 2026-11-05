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

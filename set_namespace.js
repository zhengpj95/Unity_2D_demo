const fs = require('fs');
const path = require('path');

// 你可以在这里修改要处理的文件夹和命名空间
const targetFolder = 'Assets/Scripts/FrogAdventure';
const targetNamespace = 'FrogAdventure';

const rootDir = __dirname;
const folderPath = path.resolve(rootDir, targetFolder);

if (!fs.existsSync(folderPath)) {
  console.error(`目标文件夹不存在: ${folderPath}`);
  process.exit(1);
}

function walk(dir) {
  const results = [];
  for (const fileName of fs.readdirSync(dir)) {
    const fullPath = path.join(dir, fileName);
    const stat = fs.statSync(fullPath);
    if (stat.isDirectory()) {
      results.push(...walk(fullPath));
    } else if (stat.isFile() && fullPath.endsWith('.cs')) {
      results.push(fullPath);
    }
  }
  return results;
}

function replaceNamespaceLine(line, targetNamespace) {
  return line.replace(/^\s*namespace\s+[A-Za-z_][\w\.]*\s*\{/, `namespace ${targetNamespace} {`);
}

function processFile(filePath) {
  const text = fs.readFileSync(filePath, 'utf8');
  const eol = text.includes('\r\n') ? '\r\n' : '\n';
  const lines = text.split(eol);
  const namespaceRegex = /^\s*namespace\s+([A-Za-z_][\w\.]*)\s*\{/;

  const existingNamespaceIndex = lines.findIndex((line) => namespaceRegex.test(line));
  if (existingNamespaceIndex >= 0) {
    const currentNamespace = lines[existingNamespaceIndex].match(namespaceRegex)[1];
    if (currentNamespace === targetNamespace) {
      return { changed: false, reason: 'already correct' };
    }
    lines[existingNamespaceIndex] = replaceNamespaceLine(lines[existingNamespaceIndex], targetNamespace);
    fs.writeFileSync(filePath, lines.join(eol), 'utf8');
    return { changed: true, reason: `namespace ${currentNamespace} -> ${targetNamespace}` };
  }

  let insertIndex = 0;
  while (insertIndex < lines.length && /^\s*using\s+/.test(lines[insertIndex])) {
    insertIndex += 1;
  }

  lines.splice(insertIndex, 0, `\nnamespace ${targetNamespace} {`);
  for (let i = insertIndex + 1; i < lines.length; i += 1) {
    if (lines[i].trim() !== '') {
      lines[i] = `  ${lines[i]}`;
    }
  }
  lines.push('}');

  fs.writeFileSync(filePath, lines.join(eol), 'utf8');
  return { changed: true, reason: 'namespace added' };
}

const files = walk(folderPath);
if (files.length === 0) {
  console.log('未找到任何 .cs 文件。');
  process.exit(0);
}

console.log(`开始处理 ${files.length} 个文件，目标命名空间：${targetNamespace}`);
let modified = 0;

for (const filePath of files) {
  const result = processFile(filePath);
  if (result.changed) {
    modified += 1;
    console.log(`修改: ${filePath} -> ${result.reason}`);
  }
}

console.log(`完成。共 ${modified} 个文件修改。`);

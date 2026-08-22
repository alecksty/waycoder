const fs = require('fs');
const src = fs.readFileSync('UI/WEB/www/app.js', 'utf8');
const m = src.match(/function escapeHtml[\s\S]*?(?=\n\/\/ diff 行着色)/);
eval(m[0]);
const code = [
  'public static void Main(string[] args) {',
  '    // 注释: https://example.com',
  '    var x = 1.5f;',
  '    string s = "hello world";',
  '    if (x > 0 && x < 10) { Console.WriteLine(s); }',
  '}'
].join('\n');
console.log(highlightCode(code, 'csharp'));
console.log('---python---');
const py = [
  'def fib(n):',
  '    # 注释',
  '    if n <= 1: return n',
  "    return fib(n-1) + fib(n-2)  # 尾注释",
  "s = 'it\\'s ok'"
].join('\n');
console.log(highlightCode(py, 'python'));

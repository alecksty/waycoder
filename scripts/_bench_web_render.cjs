// Web 前端渲染热点实测（临时诊断脚本，不属于产品代码）
//
// 目的：Web 端「页面无响应」是主线程被同步重活占住，先用真实数据量出**哪一步最贵**，
// 再据此定阈值 —— 而不是凭感觉设一个数字。
//
// 手法：app.js 是浏览器文件（顶层就摸 document），不能整体 require；
// 这里按大括号配平把**纯函数**抠出来在 node 里 eval（这些函数只依赖字符串与正则）。
//
// 用法：node scripts/_bench_web_render.cjs
const fs = require('fs');
const path = require('path');

const appJs = path.join(__dirname, '..', 'WayCoder', 'UI', 'WEB', 'www', 'app.js');
const src = fs.readFileSync(appJs, 'utf8');

/// 按大括号配平抠出顶层 function / const 声明
function slice(name) {
  const re = new RegExp('^(?:function |const )' + name + '[^A-Za-z0-9_$]', 'm');
  const m = re.exec(src);
  if (!m) throw new Error('not found: ' + name);
  let i = src.indexOf('{', m.index);
  if (i < 0) throw new Error('no brace: ' + name);
  let depth = 0;
  for (let j = i; j < src.length; j++) {
    if (src[j] === '{') depth++;
    else if (src[j] === '}') { depth--; if (depth === 0) return src.slice(m.index, j + 1); }
  }
  throw new Error('unbalanced: ' + name);
}

const names = ['escapeHtml', 'MARKUP_STYLES', 'markupToHtml', 'LANG_KEYWORDS', 'HIGHLIGHT_COMMON',
  'highlightCode', 'highlightDiff', 'splitRow', 'mdToHtml', 'ansiToHtml', 'stripMarkupTags',
  'tailCodePoints', 'MAX_MSG_CHARS', 'ANSI_FG', 'ANSI_BG'];
const code = names.map(n => { try { return slice(n); } catch (e) { console.error('跳过 ' + n + ': ' + e.message); return ''; } }).join('\n');
const mod = new Function(code + '\nreturn {mdToHtml, highlightCode, ansiToHtml, markupToHtml, stripMarkupTags, MAX_MSG_CHARS};')();

function ms(f) {
  const t = process.hrtime.bigint();
  const r = f();
  return [Number(process.hrtime.bigint() - t) / 1e6, r];
}

console.log('=== 1) 大代码块（mdToHtml → highlightCode 逐字符扫描）===');
for (const lines of [200, 1000, 4000, 12000]) {
  const body = Array.from({ length: lines }, (_, i) => `  const value${i} = compute(${i}, "str${i}"); // 注释 ${i}`).join('\n');
  const md = '```js\n' + body + '\n```';
  const [t] = ms(() => mod.mdToHtml(md));
  console.log(`  ${String(lines).padStart(6)} 行 / ${String(md.length).padStart(7)} 字符: ${t.toFixed(1)} ms`);
}

console.log('=== 2) 大纯文本消息（mdToHtml 逐行正则）===');
for (const size of [10000, 50000]) {
  const txt = '这是一段中文说明文字，含 **粗体**、`inline` 与 - 列表项\n'.repeat(Math.ceil(size / 30));
  const [t] = ms(() => mod.mdToHtml(txt));
  console.log(`  ${String(txt.length).padStart(7)} 字符: ${t.toFixed(1)} ms`);
}

console.log('=== 3) 工具输出 ANSI 解码（每 120ms 全量重解一次）===');
for (const chars of [10000, 50000]) {
  const ansi = ('\x1b[32m✓ 通过\x1b[0m 用例名 name_' + 'x'.repeat(20) + '\n').repeat(Math.ceil(chars / 45));
  const [t] = ms(() => mod.ansiToHtml(ansi));
  console.log(`  ${String(ansi.length).padStart(7)} 字符: ${t.toFixed(1)} ms（节流 120ms 一次即 ≥${(t / 120 * 100).toFixed(0)}% 主线程占用）`);
}

console.log('=== 4) 逐 token 追加的字符串拼接（O(n²) 模拟，DOM 之外的部分）===');
for (const tokens of [2000, 20000, 60000]) {
  const tok = '这是一个流式 token 片段，大约二十来个字符。';
  const [t] = ms(() => { let s = ''; for (let i = 0; i < tokens; i++) s += tok; return s.length; });
  console.log(`  ${String(tokens).padStart(6)} 次追加 (最终 ${String(tokens * tok.length).padStart(7)} 字符): ${t.toFixed(1)} ms`);
}
console.log('  ※ 浏览器里每次追加还要额外付一次「文本节点重建 + 布局 + scrollHeight 读取」（强制重排），');
console.log('    实测代价远高于纯拼接 —— 这就是「逐 token 直写 DOM」会成为无响应主因的原因。');

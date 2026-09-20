// Web 端 Markdown / «» 标记渲染验证（诊断脚本，不属于产品代码）
//
// 为什么要有它：Web 的 `mdToHtml` / `markupToHtml` 是**手搓的 JS 实现**，跨语言无法与
// C# 的 `UI/Shared/MarkdownRenderer.cs` 共享代码 ⇒ 只能各写一遍、也就只能各自钉判据。
// 浏览器里肉眼能看结果，看不出「哪个标签字面泄漏了」「实体是不是被二次转义了」。
// 这里把 app.js 里**真实的**那批函数抠出来跑，逐项断言 HTML 输出。
//
// 用法：node scripts/_check_web_markdown.cjs
const fs = require('fs');
const path = require('path');

const src = fs.readFileSync(path.join(__dirname, '..', 'WayCoder', 'UI', 'WEB', 'www', 'app.js'), 'utf8');

function slice(decl, name) {
  const re = new RegExp('^' + decl + ' ' + name + '[^A-Za-z0-9_$]', 'm');
  const m = re.exec(src);
  if (!m) throw new Error('not found: ' + decl + ' ' + name);
  const i = src.indexOf('{', m.index);
  const semi = src.indexOf(';', m.index);
  if (i < 0 || (semi >= 0 && semi < i)) return src.slice(m.index, semi + 1);
  let depth = 0;
  for (let j = i; j < src.length; j++) {
    if (src[j] === '{') depth++;
    else if (src[j] === '}') { depth--; if (depth === 0) return src.slice(m.index, j + 1); }
  }
  throw new Error('unbalanced: ' + name);
}

// 只抠渲染链路上真正需要的那些；代码高亮用桩顶掉（判据不碰代码块）
const parts = [
  'const MARKUP_STYLES = ' + slice('const', 'MARKUP_STYLES').replace(/^const MARKUP_STYLES\s*=\s*/, ''),
  'const MD_ENTITIES = ' + slice('const', 'MD_ENTITIES').replace(/^const MD_ENTITIES\s*=\s*/, ''),
  slice('function', 'escapeHtml'),
  slice('function', 'splitRow'),
  slice('function', 'markupTokenStyle'),
  slice('function', 'markupToHtml'),
  slice('function', 'mdDecodeEntities'),
  slice('function', 'mdToHtml'),
].join('\n');

// 代码高亮用**回显**桩（而不是常量）：块级判据要断言「代码内容进没进 <pre>」，
// 桩成常量的话内容永远断言不到（判据会假红 —— 这轮就踩过一次）。
const sandbox = { highlightCode: (code) => escapeHtmlForStub(code), console };
function escapeHtmlForStub(s) { return String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }
const fn = new Function(...Object.keys(sandbox), parts + '\nreturn { mdToHtml, markupToHtml };');
const { mdToHtml, markupToHtml } = fn(...Object.values(sandbox));

let pass = 0, fail = 0;
function check(name, cond, extra) {
  if (cond) { pass++; console.log('  ✅ ' + name); }
  else { fail++; console.log('  ❌ ' + name + (extra ? '  → ' + extra : '')); }
}

console.log('[«» 标记别名：缺一个就字面泄漏]');
for (const [tag, needle] of [
  ['gray', 'color:#6e7681;'], ['gray' /* 别名 */, null], ['black', 'color:#484f58;'],
  ['purple', 'color:#bc8cff;'], ['strike', 'line-through'], ['s', 'line-through'],
  ['faint', 'opacity'], ['i', 'font-style:italic'], ['u', 'text-decoration:underline'],
  ['blink', null], ['reverse', null],
]) {
  if (needle === null) continue;
  const html = markupToHtml('«' + tag + '»x«/»');
  check('«' + tag + '» 不字面泄漏且有样式', !html.includes('«' + tag + '»') && html.includes(needle), html);
}

console.log('[行内：与共享解析器对齐的语法]');
check('``a`b`` 反引号数量可变', mdToHtml('``a`b``').includes('<code class="md-inline">a`b</code>'), mdToHtml('``a`b``'));
check('~~x~~ → <del>', mdToHtml('~~x~~').includes('<del>x</del>'), mdToHtml('~~x~~'));
check('_x_ → <em>', mdToHtml('_x_').includes('<em>x</em>'), mdToHtml('_x_'));
check('__x__ → <strong>', mdToHtml('__x__').includes('<strong>x</strong>'), mdToHtml('__x__'));
check('***x*** → strong+em', mdToHtml('***x***').includes('<strong><em>x</em></strong>'), mdToHtml('***x***'));
check('相对链接 [文](help:x) 不再是字面', mdToHtml('[文](help:x)').includes('<a href="help:x"'), mdToHtml('[文](help:x)'));
check('链接 title 被剥掉',
  mdToHtml('[文](https://x.com "标题")').includes('href="https://x.com"')
  && !mdToHtml('[文](https://x.com "标题")').includes('标题'), mdToHtml('[文](https://x.com "标题")'));

console.log('[HTML 实体：解码一次、转义一次，不能二次转义]');
check('&amp; 不再显示成 &amp;amp;', (() => { const h = mdToHtml('A &amp; B'); return h.includes('&amp;') && !h.includes('&amp;amp;'); })(), mdToHtml('A &amp; B'));
// ⚠ 判据是「**没有二次转义**」而不是「输出里必须有裸撇号」：`escapeHtml` 会把 `'` 转成 `&#39;`，
//    那在浏览器里照样显示成撇号 —— 转义是对的，坏的只是「转义了两遍」（`&amp;#39;`）。
check('&#39; 不被二次转义', (() => { const h = mdToHtml('it&#39;s'); return !h.includes('&amp;#39;'); })(), mdToHtml('it&#39;s'));
check('&lt;script&gt; 仍被转义（不能因解码而放开 XSS）',
  (() => { const h = mdToHtml('&lt;script&gt;alert(1)&lt;/script&gt;'); return h.includes('&lt;script&gt;') && !h.includes('<script>'); })(),
  mdToHtml('&lt;script&gt;alert(1)&lt;/script&gt;'));

console.log('[表格边界]');
check('表格后的含竖线正文不被吞进 tbody', (() => {
  const h = mdToHtml('| a | b |\n| --- | --- |\n| 1 | 2 |\n\na | b 是或运算');
  return h.indexOf('</table>') >= 0 && h.indexOf('</table>') < h.indexOf('或运算');
})(), mdToHtml('| a | b |\n| --- | --- |\n| 1 | 2 |\n\na | b 是或运算'));

console.log('[块级：向共享解析器对齐]');
check('Setext `===` → h1',
  mdToHtml('一级\n===').includes('<h1>一级</h1>'), mdToHtml('一级\n==='));
check('Setext `---` → h2（不再拆成段落+分割线）',
  (() => { const h = mdToHtml('二级\n---'); return h.includes('<h2>二级</h2>') && !h.includes('<hr>'); })(),
  mdToHtml('二级\n---'));
check('空行后的 `---` 仍是分割线（没被 Setext 抢走）',
  mdToHtml('正文\n\n---').includes('<hr>'), mdToHtml('正文\n\n---'));
check('`~~~` 围栏', mdToHtml('~~~c\nint x;\n~~~').includes('<pre class="md-code">')
  && mdToHtml('~~~c\nint x;\n~~~').includes('int x;'), mdToHtml('~~~c\nint x;\n~~~'));
check('``` 开的块不能被 ~~~ 闭',
  (() => { const h = mdToHtml('```\na\n~~~\nb\n```'); return h.includes('~~~'); })(),
  mdToHtml('```\na\n~~~\nb\n```'));
check('嵌套引用 `>>` 生成两层 blockquote',
  (() => { const h = mdToHtml('> 外\n>> 内'); return h.includes('<blockquote>') && (h.match(/<blockquote>/g) || []).length === 2; })(),
  mdToHtml('> 外\n>> 内'));
check('引用结束后 blockquote 正确闭合',
  (() => { const h = mdToHtml('> 引用\n\n普通段落'); return (h.match(/<blockquote>/g) || []).length === (h.match(/<\/blockquote>/g) || []).length; })(),
  mdToHtml('> 引用\n\n普通段落'));
check('任务列表 `- [x]` → 勾选',
  mdToHtml('- [x] 完成').includes('checked'), mdToHtml('- [x] 完成'));
check('任务列表 `- [ ]` → 未勾选且不是字面',
  (() => { const h = mdToHtml('- [ ] 待办'); return h.includes('type="checkbox"') && !h.includes('[ ] 待办'); })(),
  mdToHtml('- [ ] 待办'));

console.log('\n通过: ' + pass + '  失败: ' + fail + '  总计: ' + (pass + fail));
process.exit(fail === 0 ? 0 : 1);

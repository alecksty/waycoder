// Web 折叠流程验证（临时诊断脚本，不属于产品代码）
//
// 为什么要有它：Web 端的折叠（💭 思考一行 / 🔧 工具调用一行 / 点开详情）是一套**状态机**
// （正文分段 × 思考块 × 工具组 × 分组边界），浏览器里肉眼只能看结果，看不出边界是否走对。
// 这里把 app.js 里**真实的**那批函数抠出来，跑在一个最小 DOM 桩上，模拟一整轮 SSE 事件，
// 逐项断言最终 DOM 结构 —— 因为不能开浏览器，这是最接近真机的验证。
//
// 用法：node scripts/_check_web_collapse.cjs
const fs = require('fs');
const path = require('path');

const src = fs.readFileSync(path.join(__dirname, '..', 'WayCoder', 'UI', 'WEB', 'www', 'app.js'), 'utf8');

function slice(decl, name) {
  const re = new RegExp('^' + decl + ' ' + name + '[^A-Za-z0-9_$]', 'm');
  const m = re.exec(src);
  if (!m) throw new Error('not found: ' + decl + ' ' + name);
  let i = src.indexOf('{', m.index);
  let semi = src.indexOf(';', m.index);
  if (i < 0 || (semi >= 0 && semi < i)) return src.slice(m.index, semi + 1); // 无花括号的声明
  let depth = 0;
  for (let j = i; j < src.length; j++) {
    if (src[j] === '{') depth++;
    else if (src[j] === '}') { depth--; if (depth === 0) return src.slice(m.index, j + 1); }
  }
  throw new Error('unbalanced: ' + name);
}
const fn = (n) => slice('function', n);
const v = (n) => slice('(?:let|const)', n);

// ── 最小 DOM 桩 ──
class TextNode {
  constructor(d) { this.data = d || ''; this.parentNode = null; }
  appendData(s) { this.data += s; }
}
class El {
  constructor(cls) {
    this.className = cls || '';
    this.children = [];
    this.__textNode = null;
    this.hidden = false;
    this.value = '';
    this.textContentSet = 0;
    const self = this;
    this.classList = {
      add: (c) => { if (!self.className.includes(c)) self.className += ' ' + c; },
      remove: (c) => { self.className = self.className.split(' ').filter(x => x !== c).join(' '); },
      contains: (c) => self.className.includes(c),
    };
  }
  get textContent() { return this.children.map(c => (c.data !== undefined ? c.data : c.textContent)).join(''); }
  set textContent(v) { this.textContentSet++; this.children = []; if (v) this.appendChild(new TextNode(v)); }
  set innerHTML(h) { this._html = h; this.children = []; }
  get innerHTML() { return this._html || ''; }
  appendChild(c) { c.parentNode = this; this.children.push(c); return c; }
  insertBefore(c, ref) { c.parentNode = this; this.children.push(c); return c; }
  addEventListener() { }
  get scrollHeight() { return this.children.length; }
  set scrollTop(x) { this._top = x; } get scrollTop() { return this._top || 0; }
  get clientHeight() { return 10; }
  get isConnected() { return true; }
}
const messages = new El('messages');
const byId = {
  'detail-modal': new El('modal'), 'detail-title': new El('h2'),
  'detail-search': new El('input'), 'detail-body': new El('div'),
};
globalThis.messages = messages;
globalThis.document = {
  createTextNode: (d) => new TextNode(d),
  createElement: (t) => new El(t),
  createDocumentFragment: () => new El('frag'),
  getElementById: (id) => byId[id] || new El('div'),
};
globalThis.followBottom = true;
globalThis._scrollRaf = 0;
globalThis.requestAnimationFrame = () => 1; // 合帧回调不执行：本脚本只断言 DOM 结构，不测滚动时序

const parts = [
  v('MAX_MSG_CHARS'), v('DETAIL_TOTAL_BUDGET'), v('DETAIL_ITEM_MIN'), v('DETAIL_ITEM_MAX'),
  v('segEl'), v('think'), v('toolGroup'), v('interruptSinceTool'),
  v('LANG_KEYWORDS'), v('HIGHLIGHT_COMMON'), v('MARKUP_STYLES'), v('ANSI_FG'), v('ANSI_BG'),
  fn('escapeHtml'), fn('markupToHtml'), fn('highlightCode'), fn('highlightDiff'), fn('splitRow'),
  fn('mdToHtml'), fn('ansiToHtml'), fn('stripMarkupTags'), fn('renderToolOutput'),
  v('MAX_DOM_MSGS'), fn('pruneMessagesDom'),
  fn('tailCodePoints'), fn('scroll'), fn('scheduleScroll'), fn('buildMsgEl'), fn('addMsg'),
  fn('streamTextNode'), fn('appendCapped'), fn('pill'), fn('segAppend'), fn('endSeg'),
  fn('ensureThink'), fn('thinkAppend'), fn('thinkAppendCapped'), fn('endThink'),
  fn('newToolGroup'), fn('onToolStart'), fn('onToolOutput'), fn('finishRound'), fn('handleToken'),
  fn('detailItemHtml'), fn('openDetailModal'), fn('renderDetailBody'), fn('closeDetailModal'),
];
const api = new Function(parts.join('\n') + `
  return { handleToken, onToolStart, onToolOutput, finishRound, detailItemHtml,
           showToolDetail: (g) => { openDetailModal({ title: () => '工具详情', search: true,
             render: (q) => g.items.filter(it => !q || (it.name + it.args + it.out).toLowerCase().includes(q))
               .map(it => detailItemHtml(it, g.items.indexOf(it), 30000)).join('') }); },
           showThinkDetail: (b) => { openDetailModal({ title: () => b.secs ? '💭 已思考 ' + b.secs + ' 秒' : '💭 思考中…', search: true,
             render: (q) => '<div class="think-body">' + escapeHtml(q ? b.buf.split('\\n').filter(l => l.toLowerCase().includes(q)).join('\\n') : b.buf) + '</div>' }); },
           get segEl() { return segEl; }, get think() { return think; }, get toolGroup() { return toolGroup; },
           get interruptSinceTool() { return interruptSinceTool; } };
`)();

let pass = 0, fail = 0;
function check(name, cond) {
  if (cond) { pass++; console.log('  ✅ ' + name); }
  else { fail++; console.log('  ❌ ' + name); }
}

// ── 模拟一轮：思考 → 正文 → 工具1 → 输出 → 工具2（连续，应并入同组）→ 正文（另起一段）→ 工具3（应新开组）→ 结束 ──
console.log('[Web 折叠：模拟一整轮 SSE]');
api.handleToken('«dim»让我看看这个文件');
api.handleToken('，先读它的结构…');
const thinkEl = messages.children.find(c => c.className.includes('pill') && c.textContent.startsWith('💭'));
check('思考折叠成一行（💭 胶囊，正文不入聊天流）', !!thinkEl);
check('思考正文只进内存（DOM 里没有推理原文）',
  !messages.children.some(c => String(c.textContent).includes('让我看看这个文件')));
check('思考进行中标题为「思考中 Ns」', !!thinkEl && thinkEl.textContent.startsWith('💭 思考中'));

api.handleToken('«/»');   // 思考块结束 → 标题定稿
check('思考结束标题定稿为「已思考 N 秒」', !!thinkEl && /^💭 已思考 \d+ 秒$/.test(thinkEl.textContent));

api.handleToken('我先读文件。');
api.onToolStart('read_file', 'main.c', false, 'read');
api.onToolOutput('line1\nline2\n');
api.onToolStart('edit_file', 'main.c', false, 'edit');
api.onToolOutput('已修改 3 行\n');
check('连续工具并入同一组（1 组 2 次）', api.toolGroup.items.length === 2
  && messages.children.filter(c => c.className.includes('tools')).length === 1);
const grpEl = messages.children.find(c => c.className.includes('tools'));
check('组行文案为「🔧 工具调用:2 次」', grpEl.textContent === '🔧 工具调用:2 次');

api.handleToken('现在收尾。');   // 正文段 → 内容间断
api.onToolStart('bash', 'ls', true, 'bash');
check('正文段之后的工具**新开一组**', messages.children.filter(c => c.className.includes('tools')).length === 2
  && api.toolGroup.items.length === 1);
api.onToolOutput('\x1b[32mok\x1b[0m\n');
check('工具输出只进内存（DOM 里没有输出元素）',
  !messages.children.some(c => String(c.innerHTML || '').includes('ok')));
check('输出累积到当前项', api.toolGroup.items[0].out.includes('ok'));

// 正文分段：读/编辑前后的正文应是**两个**气泡，不是堆在一个里
const segs = messages.children.filter(c => c.className.includes('msg assistant'));
check('正文按工具边界分段（2 段）', segs.length === 2);

api.finishRound();
check('收口后解绑工具组', api.toolGroup === null && api.interruptSinceTool === false);

// ── 详情浮层：`edit(main.c)` 与搜索 ──
console.log('[Web 详情浮层]');
const items = [
  { name: 'edit_file', short: 'edit', args: 'main.c', raw: false, out: '第一行\n第二行 关键字' },
  { name: 'bash', short: 'bash', args: 'ls -la', raw: true, out: '\x1b[32m✓ 通过\x1b[0m' },
];
const html = items.map((it, i) => api.detailItemHtml(it, i, 30000)).join('');
check('详情项显示缩写名 + 短参数（edit / main.c）', html.includes('edit</h4>') && html.includes('main.c'));
check('详情项不出现真实名 edit_file', !html.includes('edit_file'));
check('bash 输出按命令行格式解码（ANSI 变 span，不显示 ESC）',
  html.includes('<span') && !html.includes('\x1b'));

console.log(`\n通过 ${pass} / 失败 ${fail}`);
process.exit(fail === 0 ? 0 : 1);

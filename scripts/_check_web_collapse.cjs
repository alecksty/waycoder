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
  get textContent() {
    // innerHTML 渲染过的元素：真实 DOM 的 textContent 是「渲染后的文字」——桩里按剥标签模拟，
    // 否则定稿过的气泡会读成空串，把「空泡检测」误判成到处都是空泡
    if (this._html !== undefined) return this._html.replace(/<[^>]*>/g, '');
    return this.children.map(c => (c.data !== undefined ? c.data : c.textContent)).join('');
  }
  set textContent(v) { this.textContentSet++; delete this._html; this.children = []; if (v) this.appendChild(new TextNode(v)); }
  set innerHTML(h) { this._html = String(h); this.children = []; }
  get innerHTML() { return this._html || ''; }
  appendChild(c) { c.parentNode = this; this.children.push(c); return c; }
  insertBefore(c, ref) { c.parentNode = this; this.children.push(c); return c; }
  removeChild(c) { const i = this.children.indexOf(c); if (i >= 0) this.children.splice(i, 1); c.parentNode = null; return c; }
  get firstChild() { return this.children[0] || null; }
  addEventListener(type, fn) { (this._on ||= {})[type] = fn; }
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
globalThis.setInterval = () => 1;            // 详情浮层「思考中每秒刷新」用；桩里不需要真的跑
globalThis.clearInterval = () => { };

const parts = [
  v('MAX_MSG_CHARS'), v('DETAIL_TOTAL_BUDGET'), v('DETAIL_ITEM_MIN'), v('DETAIL_ITEM_MAX'),
  v('segEl'), v('think'), v('thinkDepth'), v('toolGroup'), v('interruptSinceTool'),
  v('detailCfg'), v('detailTimer'),
  v('LANG_KEYWORDS'), v('HIGHLIGHT_COMMON'), v('MARKUP_STYLES'), v('ANSI_FG'), v('ANSI_BG'),
  fn('escapeHtml'), fn('markupTokenStyle'), fn('markupToHtml'), fn('highlightCode'), fn('highlightDiff'), fn('splitRow'),
  fn('mdToHtml'), fn('ansiToHtml'), fn('stripMarkupTags'), fn('renderToolOutput'),
  v('MAX_DOM_MSGS'), fn('pruneMessagesDom'),
  fn('tailCodePoints'), fn('scroll'), fn('scheduleScroll'), fn('buildMsgEl'), fn('addMsg'),
  fn('streamTextNode'), fn('appendCapped'), fn('pill'), fn('isBlankText'), fn('hasVisibleText'),
  fn('segAppend'), fn('endSeg'),
  fn('ensureThink'), fn('thinkAppend'), fn('thinkAppendCapped'), fn('endThink'),
  fn('newToolGroup'), fn('onToolStart'), fn('onToolOutput'), fn('finishRound'),
  fn('markupOpeners'), fn('emitTokenPiece'), fn('handleToken'),
  fn('detailItemHtml'), fn('showThinkDetail'), fn('showToolDetail'),
  fn('openDetailModal'), fn('renderDetailBody'), fn('closeDetailModal'),
];
const api = new Function(parts.join('\n') + `
  return { handleToken, onToolStart, onToolOutput, finishRound, detailItemHtml,
           get segEl() { return segEl; }, get think() { return think; }, get toolGroup() { return toolGroup; },
           get thinkDepth() { return thinkDepth; }, isBlankText,
           get interruptSinceTool() { return interruptSinceTool; }, get detailCfg() { return detailCfg; } };
`)();

/// 模拟点击（拿真实 DOM 桩上注册的 click 处理器跑一遍）
function click(el) {
  const h = el && el._on && el._on.click;
  if (!h) throw new Error('该元素没有 click 处理器');
  h({});
}

let pass = 0, fail = 0;
function check(name, cond) {
  if (cond) { pass++; console.log('  ✅ ' + name); }
  else { fail++; console.log('  ❌ ' + name); }
}

// ── 模拟一轮：思考 → 正文 → 工具1 → 输出 → 工具2（连续，应并入同组）→ 正文（另起一段）→ 工具3（应新开组）→ 结束 ──
console.log('[Web 折叠：模拟一整轮 SSE]');
// 真实流里推理前带一口换行（LLM 发 `"\n«dim»"`）—— 它不该另起一个空正文泡排在胶囊前面
api.handleToken('\n«dim»让我看看这个文件');
api.handleToken('，先读它的结构…');
const thinkEl = messages.children.find(c => c.className.includes('pill') && c.textContent.startsWith('💭'));
check('思考折叠成一行（💭 胶囊，正文不入聊天流）', !!thinkEl);
check('推理前的换行没有另起正文泡（首个元素就是胶囊）', messages.children[0] === thinkEl);
check('思考正文只进内存（DOM 里没有推理原文）',
  !messages.children.some(c => String(c.textContent).includes('让我看看这个文件')));
check('思考进行中标题为「思考中 Ns」', !!thinkEl && thinkEl.textContent.startsWith('💭 思考中'));

api.handleToken('«/»');   // 思考块结束 → 标题定稿
check('思考结束标题定稿为「已思考 N 秒」', !!thinkEl && /^💭 已思考 \d+ 秒$/.test(thinkEl.textContent));

// ── 回归：**思考结束后**点开胶囊必须仍能打开详情 ──
// 曾经的 bug：点击回调闭包捕获可变的 think，思考一结束 think 被置空 ⇒ 回调读到 null ⇒ 抛错、
// 浮层点不开（用户实测「思考气泡有时点不开」）。工具组没这问题，因为它闭包捕获的是局部对象。
{
  let err = null;
  try { click(thinkEl); } catch (e) { err = e; }
  check('思考结束后点胶囊不抛错', err === null);
  check('思考结束后点胶囊能打开详情浮层',
    api.detailCfg !== null && byId['detail-modal'].className.includes('open'));
  check('详情浮层里是完整思考正文', byId['detail-body'].innerHTML.includes('让我看看这个文件'));
}

// ── 回归：**各段思考自成一泡**（用户实测「老的思考气泡都很难点开」，对齐 MAUI 一泡泡一块）──
// 每块必须各自持有自己的正文，点哪个开哪个；第二块出现/结束后，第一块仍要能开、且开的是自己的内容。
{
  api.handleToken('\n«dim»第二轮推理：先看引号里的标记');
  const pill2 = messages.children.filter(c => c.className.includes('pill') && c.textContent.startsWith('💭')).pop();
  check('第二轮思考**新起一块**（不并进上一块）', pill2 !== thinkEl && pill2.textContent.startsWith('💭 思考中'));
  // 推理正文里嵌了别的 «» 标记（LLM 超长时注入的「思考内容过长」就是这种）——
  // 它自带的 `«/»` 只该关掉那个标记，**不该把思考块关掉**
  api.handleToken('\n«orange3»… 思考内容过长，显示窗口受限«/»');
  check('思考块内嵌标记的 «/» 不关掉思考块', !!api.think && api.thinkDepth === 1);
  api.handleToken('后续推理仍在思考里');
  check('嵌套标记之后的推理仍进思考（没漏进正文段）',
    api.think !== null && api.think.buf.includes('后续推理仍在思考里'));
  check('期间没有裸 «/» 漏进聊天流',
    !messages.children.some(c => String(c.textContent).includes('«') && c.className.includes('msg')));
  api.handleToken('«/»');   // 真正的收尾
  check('收尾的 «/» 关掉思考块', api.think === null && api.thinkDepth === 0);

  let e1 = null;
  try { click(pill2); } catch (e) { e1 = e; }
  check('第二轮胶囊点得开', e1 === null && byId['detail-modal'].className.includes('open'));
  const body2 = byId['detail-body'].innerHTML;
  check('第二轮详情是它自己的推理（含嵌套标记那段）',
    body2.includes('第二轮推理') && body2.includes('后续推理仍在思考里'));
  check('详情里剥掉了 «» 标记（不显示裸标签）', !body2.includes('«') && !body2.includes('»'));

  let e0 = null;
  try { click(thinkEl); } catch (e) { e0 = e; }
  check('第二轮结束后，第一轮胶囊仍点得开', e0 === null && byId['detail-modal'].className.includes('open'));
  const body1 = byId['detail-body'].innerHTML;
  check('第一轮详情是**它自己**的推理（不是最新的那块）',
    body1.includes('让我看看这个文件') && !body1.includes('第二轮推理'));
}

// ── 回归：普通正文**绝不能**被当成思考（用户实测「所有内容都是已思考 n 秒」）──
// `«/»` 是所有 «» 标记的统一结束符，不是思考块专用。带颜色的正文必须进正文段。
{
  const before = messages.children.filter(c => c.className.includes('msg assistant')).length;
  const thinkBefore = messages.children.filter(c => c.className.includes('pill') && c.textContent.startsWith('💭')).length;
  api.handleToken('«cyan»这是一句普通正文«/»');
  const after = messages.children.filter(c => c.className.includes('msg assistant')).length;
  const thinkAfter = messages.children.filter(c => c.className.includes('pill') && c.textContent.startsWith('💭')).length;
  check('带颜色标记的正文进正文段（不新建思考气泡）', after === before + 1 && thinkAfter === thinkBefore);
  const lastSeg = messages.children.filter(c => c.className.includes('msg assistant')).pop();
  check('正文内容完整（含标记，交给 mdToHtml 配对颜色）',
    !!lastSeg && lastSeg.textContent.includes('这是一句普通正文'));
  check('正文里的 «/» 未被吞掉（否则颜色配对失配）', !!lastSeg && lastSeg.textContent.includes('«/»'));
}

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

// ── 详情浮层：点开工具组行 + `edit(main.c)` 显示 ──
console.log('[Web 详情浮层]');
{
  const groupEls = messages.children.filter(c => c.className.includes('tools'));
  const group = api.toolGroup || null;
  // 点第一个工具组行（它已收口，组对象仍被闭包持有）
  let err = null;
  try { click(groupEls[0]); } catch (e) { err = e; }
  check('点工具组行不抛错且打开浮层' + (err ? '（异常：' + err.message + '）' : ''),
    err === null && byId['detail-modal'].className.includes('open'));
  const body = byId['detail-body'].innerHTML;
  check('工具详情含缩写名 + 短参数（read / main.c）', body.includes('read</h4>') && body.includes('main.c'));
  check('工具详情不出现真实名 read_file', !body.includes('read_file'));
  check('工具详情含该次调用的输出', body.includes('line1'));
}

// ── 回归：**不发空泡**（用户实测「很多空泡泡，没有任何内容」）──
// 来源：LLM 每段正文开头送一口换行（思考结束就是 `"«/»\n"`）。光凭换行建泡，
// 「想完直接调工具」（没有正文）就会在工具行前留一个空泡；只攒了空白的泡收尾时也要撤掉。
console.log('[Web 空泡]');
{
  const before = messages.children.length;
  api.handleToken('\n«dim»想完了，直接动手');
  api.handleToken('«/»\n');                       // 尾换行：过去就是它建的空泡
  api.onToolStart('bash', 'ls', true, 'bash');
  api.finishRound();
  const empties = messages.children.filter(c => c.className.includes('msg') && !String(c.textContent).trim());
  check('思考完直接调工具：不留空正文泡（新增元素只有思考胶囊 + 工具行）',
    empties.length === 0 && messages.children.length === before + 2);

  // 正文**前后**的空白仍要正常进正文（不能因为防空泡把真实正文也吞了）
  const n0 = messages.children.length;
  api.handleToken('«/»\n');                       // 空思考块（只有标记）→ 不该留泡
  api.handleToken('这是真正的正文');
  const seg = messages.children.filter(c => c.className.includes('msg assistant')).pop();
  check('真实正文照常显示（防空泡没吞正文）', !!seg && seg.textContent.includes('这是真正的正文'));
  api.finishRound();
  check('纯标记段不留泡', messages.children.filter(c => c.className.includes('msg') && !String(c.textContent).trim()).length === 0
    && messages.children.length >= n0);

  // 「只有空格或者不可见字符的泡泡，不发」——零宽空格/连接词、BOM、软连字符、控制字符都算没内容
  const n1 = messages.children.length;
  const m1 = messages.children.filter(c => c.className.includes('msg')).length;
  api.handleToken('\u200B\u200B\n\u00AD\r\n\uFEFF\t  ');
  api.onToolStart('bash', 'ls', true, 'bash');
  api.finishRound();
  check('只有不可见字符的碎片不建泡（新增元素只有工具行）',
    messages.children.length === n1 + 1
    && messages.children.filter(c => c.className.includes('msg')).length === m1);
  check('不可见字符判据本身正确（空白/零宽/BOM 为真，普通文字为假）',
    api.isBlankText('\u200B\u00AD\uFEFF \r\n\u3164') === true
    && api.isBlankText(' 有内容 ') === false && api.isBlankText('') === true);
}

console.log(`\n通过 ${pass} / 失败 ${fail}`);
process.exit(fail === 0 ? 0 : 1);

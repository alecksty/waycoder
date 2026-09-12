// Web 流式追加的 DOM 操作计数（临时诊断脚本，不属于产品代码）
//
// 背景：Web 端「页面卡住一会、久了浏览器弹无响应」＝ 主线程被逐 token 的 DOM 操作排满。
// 纯 JS 那部分实测不贵（见 _bench_web_render.cjs：ANSI 49k=3.7ms、markdown 12万=30ms），
// 真正的代价在 DOM 层：每次 `el.textContent += s` 都要重建整块文本节点，而紧随其后的
// `scroll()` 读 `scrollHeight` 会**强制同步重排**整页。两者都随内容长度线性增长，
// 逐 token 做 ⇒ O(n²) 次字符搬运 + O(n) 次整页重排。
//
// 本脚本把 app.js 里**真实**的 appendCapped/streamTextNode/scheduleScroll 抠出来，
// 跑在一个最小 DOM 桩上，数「整块重建次数」与「强制重排次数」—— 这是修复的直接指标。
//
// 用法：node scripts/_bench_web_stream.cjs
const fs = require('fs');
const path = require('path');

const appJs = path.join(__dirname, '..', 'WayCoder', 'UI', 'WEB', 'www', 'app.js');
const src = fs.readFileSync(appJs, 'utf8');

function slice(name) {
  const re = new RegExp('^(?:function |const )' + name + '[^A-Za-z0-9_$]', 'm');
  const m = re.exec(src);
  if (!m) throw new Error('not found: ' + name);
  let i = src.indexOf('{', m.index);
  let depth = 0;
  for (let j = i; j < src.length; j++) {
    if (src[j] === '{') depth++;
    else if (src[j] === '}') { depth--; if (depth === 0) return src.slice(m.index, j + 1); }
  }
  throw new Error('unbalanced: ' + name);
}

const stats = { fullRewrite: 0, reflow: 0, appendData: 0, frames: 0 };

// ── 最小 DOM 桩：只实现被用到的部分，并对「整块重建 / 强制重排」计数 ──
class TextNode {
  constructor(d) { this.data = d || ''; this.parentNode = null; }
  appendData(s) { stats.appendData++; this.data += s; }
}
class El {
  constructor(cls) { this.className = cls || ''; this.children = []; this.__textNode = null; }
  get textContent() { return this.children.map(c => (c.data !== undefined ? c.data : c.textContent)).join(''); }
  set textContent(v) { stats.fullRewrite++; this.children = []; if (v) this.appendChild(new TextNode(v)); }
  appendChild(c) { c.parentNode = this; this.children.push(c); return c; }
  get scrollHeight() { stats.reflow++; return this.children.length; }
  set scrollTop(v) { this._top = v; }
  get scrollTop() { return this._top || 0; }
  get clientHeight() { return 10; }
}
const messages = new El('messages');
globalThis.messages = messages;
globalThis.requestAnimationFrame = (fn) => { pendingFrames.push(fn); return pendingFrames.length; };
let pendingFrames = [];
globalThis.document = {
  createTextNode: (d) => new TextNode(d),
  createElement: (t) => new El(t),
  createDocumentFragment: () => new El('frag'),
};
globalThis.MAX_MSG_CHARS = 50000;
globalThis.tailCodePoints = (s, n) => s.slice(-n);
globalThis.followBottom = true;
globalThis.scroll = function () { if (followBottom) messages.scrollTop = messages.scrollHeight; };
globalThis.scheduleScroll = function () {
  if (globalThis._scrollRaf) return;
  globalThis._scrollRaf = requestAnimationFrame(() => { globalThis._scrollRaf = 0; globalThis.frames = (globalThis.frames || 0) + 1; scroll(); });
};

const code = slice('streamTextNode') + '\n' + slice('appendCapped') + '\n' + slice('onToolOutput');
const mod = new Function('return (function(){' + code + '\nreturn {streamTextNode, appendCapped, onToolOutput};})()')();
const { appendCapped, onToolOutput } = mod;

const TOKENS = 2000;
const TOK = '这是一个流式 token 片段，大约二十来个字符。';

// ── 旧路径：每个 token 一次 textContent += 紧跟一次 scroll() ──
function oldPath() {
  const el = new El('msg');
  for (let i = 0; i < TOKENS; i++) { el.textContent = el.textContent + TOK; scroll(); }
}
// ── 新路径：appendCapped（文本节点 appendData + 合帧滚动）──
function newPath() {
  const el = new El('msg');
  for (let i = 0; i < TOKENS; i++) appendCapped(el, TOK);
  while (pendingFrames.length) { const f = pendingFrames.shift(); f(); } // 冲掉待执行帧
}

for (const [name, fn] of [['旧（textContent += 逐 token）', oldPath], ['新（appendData + 合帧滚动）', newPath]]) {
  stats.fullRewrite = stats.reflow = stats.appendData = 0;
  pendingFrames = [];
  const t = process.hrtime.bigint();
  fn();
  const ms = Number(process.hrtime.bigint() - t) / 1e6;
  console.log(`${name}`);
  console.log(`  整块文本重建 ${String(stats.fullRewrite).padStart(6)} 次 · 强制重排 ${String(stats.reflow).padStart(6)} 次 · 追加 ${String(stats.appendData).padStart(6)} 次 · 帧 ${pendingFrames.length === 0 ? '已冲' : '待'} (${ms.toFixed(1)} ms 桩内)`);
}
console.log(`\n内容规模：${TOKENS} 个 token ≈ ${TOKENS * TOK.length} 字符`);
console.log('※ 「强制重排」= 读 scrollHeight，浏览器里它会同步重排整页；旧路径每个 token 一次，');
console.log('   新路径每帧一次（120 帧/秒上限，实际按浏览器的 rAF 节拍）。');

// ── 工具输出：折叠前（每个 chunk 一条气泡）vs 折叠后（只进内存）──
// 折叠前 Web/GUI 都是「每个输出 chunk 一次 appendCapped + scroll」，大输出就是成百上千次
// 整块重建 + 整页重排；折叠后 onToolOutput 只往内存字符串追加，DOM 一次都不碰，
// 点开详情浮层时才渲染一次（且按项预算均分）。
const CHUNKS = 400;
const chunkText = '✓ 用例通过：某个测试名\n';
const chunks = new Array(CHUNKS).fill(chunkText);

function oldToolOutput() {
  // 折叠前的实现（照抄改前的代码语义）：每个 chunk 一次整块重建 + 一次强制重排
  const el = new El('tool-output');
  for (let i = 0; i < chunks.length; i++) {
    el.textContent = el.textContent + chunks[i];
    scroll();
  }
}
function newToolOutput() {
  globalThis.toolGroup = { items: [{ name: 'bash', out: '' }], el: null };
  for (let i = 0; i < chunks.length; i++) onToolOutput(chunks[i]);
  return globalThis.toolGroup.items[0].out.length;
}

let outLen = 0;
for (const [name, fn] of [['旧（每个 chunk 一条气泡 + 滚动）', oldToolOutput],
                          ['新（折叠：只进内存，DOM 零写）', () => { outLen = newToolOutput(); }]]) {
  stats.fullRewrite = stats.reflow = stats.appendData = 0;
  pendingFrames = [];
  fn();
  console.log(`${name}`);
  console.log(`  整块文本重建 ${String(stats.fullRewrite).padStart(6)} 次 · 强制重排 ${String(stats.reflow).padStart(6)} 次 · 追加 ${String(stats.appendData).padStart(6)} 次`);
}
console.log(`  工具输出规模：${CHUNKS} 个 chunk ≈ ${chunks.join('').length} 字符（内存里保留 ${outLen} 字符，点开详情才渲染）`);

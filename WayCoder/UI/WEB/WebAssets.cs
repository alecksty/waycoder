using System.Collections.Concurrent;
using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;

namespace WayCoder.UI.Web;

internal static class WebAssets
{
    // ═══════════════════════════════════════════════════════════
    //  内嵌前端（单 HTML，无构建，无外部 CDN）
    //  对标 DeepSeek Harness：黑白主题 + 圆角 + 模型下拉 + F1-F10 槽位 + 设置抽屉 + key 弹窗
    // ═══════════════════════════════════════════════════════════

    internal const string Html = """
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>WayCoder（道码）</title>
<style>
:root {
  --bg:#0f1117; --panel:#171a23; --panel2:#1d2230; --border:#262b3a; --text:#e6e8ee; --dim:#8b93a7;
  --accent:#4f8cff; --user:#1f3a5f; --tool:#2a2416; --danger:#3a2a2a; --shadow:0 4px 20px rgba(0,0,0,.4);
  --diff-del:#ff7b72; --diff-del-bg:rgba(248,81,73,.14); --diff-add:#7ee787; --diff-add-bg:rgba(46,160,67,.14);
  --tok-kw:#ff7b72; --tok-str:#a5d6ff; --tok-num:#79c0ff; --tok-fn:#d2a8ff; --tok-com:#7d8590;
  --scroll:rgba(255,255,255,.16); --scroll-hover:rgba(255,255,255,.32);
}
[data-theme="light"] {
  --bg:#f5f6f8; --panel:#ffffff; --panel2:#f0f2f6; --border:#e2e5ec; --text:#1a1d24; --dim:#6b7280;
  --accent:#2f6bff; --user:#e3ecff; --tool:#fff4dc; --danger:#ffe2e2; --shadow:0 4px 20px rgba(0,0,0,.12);
  --diff-del:#d73a49; --diff-del-bg:rgba(255,129,130,.15); --diff-add:#1a7f37; --diff-add-bg:rgba(63,185,80,.15);
  --tok-kw:#cf222e; --tok-str:#0a3069; --tok-num:#0550ae; --tok-fn:#6639ba; --tok-com:#57606a;
  --scroll:rgba(0,0,0,.18); --scroll-hover:rgba(0,0,0,.32);
}
* { box-sizing:border-box; margin:0; padding:0; }
body { background:var(--bg); color:var(--text); font:14px/1.6 -apple-system,"PingFang SC","Microsoft YaHei",sans-serif; height:100vh; display:flex; flex-direction:column; transition:background .2s,color .2s; overflow:hidden; }
/* ── 半透明细滚动条（深色主题下自动融入，不刺眼）── */
::-webkit-scrollbar { width:8px; height:8px; }
::-webkit-scrollbar-track { background:transparent; }
::-webkit-scrollbar-thumb { background:var(--scroll); border-radius:4px; }
::-webkit-scrollbar-thumb:hover { background:var(--scroll-hover); }
* { scrollbar-width:thin; scrollbar-color:var(--scroll) transparent; }
header { position:relative; padding:9px 14px; border-bottom:1px solid var(--border); background:var(--panel); display:flex; align-items:center; gap:10px; flex-wrap:nowrap; }
.logo { font-weight:700; color:var(--text); font-size:15px; white-space:nowrap; }
.logo span { color:var(--accent); }
.agent-label { position:absolute; left:50%; transform:translateX(-50%); font-size:13px; font-weight:600; color:var(--dim); white-space:nowrap; }
.version { font-size:12px; color:var(--dim); white-space:nowrap; padding:0 2px; }
.spacer { flex:1; }
select, .btn { height:32px; border-radius:9px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; padding:0 11px; cursor:pointer; outline:none; }
select:focus { border-color:var(--accent); }
select optgroup { background:var(--panel); color:var(--text); }
.btn { display:inline-flex; align-items:center; gap:6px; font-weight:600; }
.btn:hover { border-color:var(--accent); }
.btn.ghost { background:transparent; border:none; font-size:17px; padding:0 8px; }
.btn.primary { background:var(--accent); color:#fff; border:none; }
.btn.danger { background:var(--danger); color:#ff9a9a; border:none; }

/* ── 三栏布局 ── */
.layout { flex:1; display:grid; grid-template-columns:236px minmax(0,1fr) 300px; min-height:0; }
#sidebar-left { background:var(--panel); border-right:1px solid var(--border); overflow-y:auto; display:flex; flex-direction:column; }
#sidebar-right { background:var(--panel); border-left:1px solid var(--border); overflow-y:auto; }
#chat-col { display:flex; flex-direction:column; min-width:0; min-height:0; }

.panel-head { padding:11px 14px 7px; font-size:12px; font-weight:700; color:var(--dim); text-transform:uppercase; letter-spacing:.5px; }
#slot-list { display:grid; grid-template-columns:repeat(5,1fr); gap:5px; padding:2px 12px 8px; }
.slot { height:30px; border-radius:8px; border:1px solid var(--border); background:var(--panel2); color:var(--dim); font-size:11px; cursor:pointer; display:flex; align-items:center; justify-content:center; transition:all .15s; }
.slot:hover { border-color:var(--accent); color:var(--text); }
.slot.active { background:var(--accent); border-color:var(--accent); color:#fff; font-weight:700; }
.slot.has { color:var(--text); border-color:var(--dim); }
#new-session { margin:8px 12px; }

#session-list { flex:1; overflow-y:auto; padding:0 8px; }
.session-item { padding:8px 8px; border-radius:9px; cursor:pointer; position:relative; transition:background .12s; }
.session-item:hover { background:var(--panel2); }
.session-item .preview { font-size:13px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis; }
.session-item .meta { font-size:11px; color:var(--dim); margin-top:1px; }
.session-item .ops { position:absolute; top:6px; right:6px; display:none; gap:4px; }
.session-item:hover .ops { display:flex; }
.session-item .ops button { width:22px; height:22px; border-radius:6px; border:1px solid var(--border); background:var(--panel); color:var(--dim); cursor:pointer; font-size:11px; line-height:1; }
.session-item .ops button:hover { color:var(--text); border-color:var(--accent); }
.empty { color:var(--dim); font-size:12px; padding:8px 12px; text-align:center; }

/* ── 聊天 ── */
#messages { flex:1; overflow-y:auto; padding:16px; display:flex; flex-direction:column; gap:12px; }
.msg { max-width:82%; padding:10px 15px; border-radius:14px; white-space:pre-wrap; word-break:break-word; }
.msg.user { align-self:flex-end; background:var(--user); border-bottom-right-radius:4px; }
.msg.assistant { align-self:flex-start; background:var(--panel); border:1px solid var(--border); border-bottom-left-radius:4px; }
.msg.system { align-self:center; color:var(--dim); font-size:13px; background:transparent; }
.msg.cmd { align-self:stretch; max-width:100%; background:var(--panel); border:1px solid var(--border); border-left:3px solid var(--accent); border-radius:10px; white-space:normal; }
.msg.reasoning { align-self:flex-start; color:var(--dim); font-size:12.5px; white-space:pre-wrap; word-break:break-word; padding:0 4px; max-width:88%; }
.tool { align-self:flex-start; background:var(--tool); border:1px solid var(--border); border-radius:12px; padding:7px 13px; font-size:13px; color:var(--dim); }
.tool b { color:#e8b34b; }
.tool-output { align-self:stretch; background:var(--panel2); border:1px solid var(--border); border-radius:12px; padding:9px 13px; font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace; font-size:12px; white-space:pre-wrap; word-break:break-word; color:var(--text); max-height:320px; overflow-y:auto; }

/* ── Markdown 渲染 ── */
.msg.assistant { white-space:normal; }
.msg.assistant.streaming { white-space:pre-wrap; }
.msg h1,.msg h2,.msg h3,.msg h4,.msg h5,.msg h6 { margin:10px 0 4px; line-height:1.3; font-weight:700; }
.msg h1 { font-size:1.35em; } .msg h2 { font-size:1.25em; } .msg h3 { font-size:1.15em; } .msg h4,.msg h5,.msg h6 { font-size:1.05em; }
.msg p { margin:4px 0; }
.msg ul,.msg ol { margin:4px 0 4px 22px; }
.msg li { margin:2px 0; }
.msg blockquote { border-left:3px solid var(--accent); padding:2px 0 2px 11px; margin:6px 0; color:var(--dim); }
.msg blockquote p { margin:2px 0; }
.msg a { color:var(--accent); text-decoration:none; }
.msg a:hover { text-decoration:underline; }
.msg hr { border:none; border-top:1px solid var(--border); margin:12px 0; }
.msg .md-code { background:var(--bg); border:1px solid var(--border); border-radius:9px; padding:10px 13px; margin:8px 0; overflow-x:auto; font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace; font-size:12.5px; white-space:pre; }
.msg .md-code code { font-family:inherit; background:none; border:none; padding:0; }
.msg .md-inline { background:var(--panel2); border:1px solid var(--border); border-radius:5px; padding:0 5px; font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace; font-size:.9em; }
/* ── 语法高亮 token ── */
.tok-kw { color:var(--tok-kw); font-weight:600; }
.tok-str { color:var(--tok-str); }
.tok-num { color:var(--tok-num); }
.tok-fn { color:var(--tok-fn); }
.tok-com { color:var(--tok-com); font-style:italic; }
.msg .md-table { border-collapse:collapse; margin:8px 0; font-size:12.5px; max-width:100%; display:block; overflow-x:auto; }
.msg .md-table th,.msg .md-table td { border:1px solid var(--border); padding:5px 10px; text-align:left; white-space:normal; }
.msg .md-table th { background:var(--panel2); font-weight:700; }
#input-bar { display:flex; gap:8px; padding:11px 14px 6px; border-top:1px solid var(--border); background:var(--panel); align-items:flex-end; }
#input { flex:1; resize:none; background:var(--bg); color:var(--text); border:1px solid var(--border); border-radius:14px; padding:11px 14px; font:inherit; min-height:64px; max-height:240px; outline:none; line-height:1.5; }
#input:focus { border-color:var(--accent); }
#input::placeholder { color:var(--dim); opacity:1; }
#send { width:46px; height:46px; border-radius:13px; font-size:19px; padding:0; display:flex; align-items:center; justify-content:center; background:var(--accent); color:#fff; border:none; cursor:pointer; flex-shrink:0; transition:background .15s; }
#send:hover { filter:brightness(1.12); }
#send.stop { background:var(--danger); color:#ff9a9a; }
#model-bar { display:flex; gap:8px; align-items:center; padding:4px 14px 11px; background:var(--panel); flex-wrap:wrap; }
#model-bar select { height:28px; border-radius:8px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; font-size:12px; padding:0 7px; cursor:pointer; outline:none; max-width:160px; }
#model-add-btn { height:28px; padding:0 9px; font-size:14px; line-height:1; }
#model-bar .model-label { display:inline-flex; align-items:center; gap:5px; font-size:12px; color:var(--dim); white-space:nowrap; }
#model-bar .model-label select { max-width:195px; }
.model-status { margin-left:auto; font-size:12px; color:var(--dim); white-space:nowrap; overflow:hidden; text-overflow:ellipsis; max-width:38%; }

/* ── 右栏卡片 ── */
.card { border-bottom:1px solid var(--border); padding:11px 14px; }
.card-head { font-size:12px; font-weight:700; color:var(--accent); margin-bottom:7px; }
.card .row { font-size:12.5px; padding:2px 0; display:flex; gap:6px; align-items:flex-start; }
.card .row .k { color:var(--dim); flex-shrink:0; }
.card .row .v { word-break:break-all; }
.card .item { font-size:12.5px; padding:3px 0; border-bottom:1px dashed var(--border); }
.card .item:last-child { border-bottom:none; }
.stat-add { color:var(--diff-add); }
.stat-del { color:var(--diff-del); margin-left:5px; }
.dot { display:inline-block; width:8px; height:8px; border-radius:50%; margin-right:5px; vertical-align:middle; }
.dot.pending { background:#8b93a7; }
.dot.in_progress { background:#4f8cff; }
.dot.completed { background:#3fb950; }
.dot.cancelled { background:#e5534b; }
.dot.blocked { background:#e8b34b; }
.dot.on { background:#3fb950; }
.dot.off { background:#e5534b; }
.dot.connecting { background:#e8b34b; }
.token-grid { display:grid; grid-template-columns:1fr 1fr; gap:3px 10px; font-size:12px; }
.token-grid .v { text-align:right; font-variant-numeric:tabular-nums; }

/* ── 设置窗口（两列，居中弹出）── */
#drawer { position:fixed; top:50%; left:50%; width:780px; max-width:94vw; height:82vh; max-height:92vh; background:var(--panel); border:1px solid var(--border); border-radius:16px; box-shadow:var(--shadow); transform:translate(-50%,-50%) scale(.96); opacity:0; pointer-events:none; transition:transform .2s,opacity .2s; z-index:50; display:flex; flex-direction:column; }
#drawer.open { transform:translate(-50%,-50%) scale(1); opacity:1; pointer-events:auto; }
#drawer-head { padding:13px 18px; border-bottom:1px solid var(--border); display:flex; align-items:center; }
#drawer-head b { flex:1; }
#drawer-body { flex:1; overflow:hidden; display:flex; }
#settings-nav { width:180px; border-right:1px solid var(--border); overflow-y:auto; padding:8px; }
#settings-nav .nav-item { display:block; width:100%; text-align:left; padding:9px 12px; border-radius:9px; background:transparent; border:none; color:var(--text); font:inherit; cursor:pointer; margin-bottom:3px; }
#settings-nav .nav-item:hover { background:var(--panel2); }
#settings-nav .nav-item.active { background:var(--panel2); color:var(--accent); font-weight:600; }
#settings-detail { flex:1; overflow-y:auto; padding:14px 18px 24px; }
.set-row { margin-bottom:13px; }
.set-row label { display:block; font-size:13px; color:var(--text); margin-bottom:4px; font-weight:500; }
.set-row .desc { font-size:11px; color:var(--dim); margin-bottom:5px; }
.set-row input, .set-row select { width:100%; height:33px; border-radius:9px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; padding:0 10px; outline:none; }
.set-row input:focus, .set-row select:focus { border-color:var(--accent); }
.set-row input[type="checkbox"] { width:auto; height:auto; }
#drawer-foot { padding:12px 18px; border-top:1px solid var(--border); display:flex; justify-content:flex-end; gap:8px; }

/* ── 模态框（key / ask）── */
.modal { position:fixed; inset:0; background:rgba(0,0,0,.5); display:none; align-items:center; justify-content:center; z-index:60; }
.modal.open { display:flex; }
.modal-card { background:var(--panel); border:1px solid var(--border); border-radius:16px; padding:20px 22px; width:440px; max-width:92vw; max-height:86vh; overflow-y:auto; box-shadow:var(--shadow); }
.modal-card.diff { width:680px; }
.modal-card h2 { font-size:16px; margin-bottom:6px; }
.modal-card p { font-size:13px; color:var(--dim); margin-bottom:12px; }
.modal-card input[type="text"], .modal-card input[type="password"] { width:100%; height:37px; border-radius:11px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; padding:0 12px; outline:none; margin-bottom:12px; }
.modal-card input:focus { border-color:var(--accent); }
.modal-card .row { display:flex; gap:8px; justify-content:flex-end; flex-wrap:wrap; }
.ask-option { display:block; width:100%; text-align:left; padding:10px 13px; margin-bottom:7px; border-radius:10px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; cursor:pointer; }
.ask-option:hover { border-color:var(--accent); }
.ask-message { background:var(--bg); border:1px solid var(--border); border-radius:10px; padding:10px 12px; font-family:ui-monospace,Menlo,Consolas,monospace; font-size:12px; white-space:pre-wrap; word-break:break-all; margin-bottom:12px; max-height:220px; overflow-y:auto; }
.ask-multi { display:block; padding:6px 2px; font-size:13.5px; }
.ask-multi input { margin-right:8px; }
/* ── Diff 预览 ── */
.diff-hunk { border:1px solid var(--border); border-radius:10px; margin-bottom:9px; overflow:hidden; }
.diff-hunk-head { display:flex; align-items:center; gap:8px; padding:7px 11px; background:var(--panel2); font-family:ui-monospace,Menlo,Consolas,monospace; font-size:12px; color:var(--dim); cursor:pointer; }
.diff-hunk-head input { margin:0; }
.diff-hunk-lines { margin:0; padding:8px 11px; font-family:ui-monospace,Menlo,Consolas,monospace; font-size:12px; line-height:1.55; white-space:pre-wrap; word-break:break-all; background:var(--bg); }
.diff-line { display:block; }
.diff-line.del { color:var(--diff-del); background:var(--diff-del-bg); }
.diff-line.add { color:var(--diff-add); background:var(--diff-add-bg); }
.diff-line.ctx { color:var(--dim); }
/* ── 模型选择窗口 ── */
.model-card { width:560px; display:flex; flex-direction:column; overflow:hidden; }
.model-card h2, .model-card .row { flex-shrink:0; }
#model-list { flex:1; min-height:0; overflow-y:auto; }
#model-search { margin-bottom:0; }
.model-group .gname { font-size:12px; color:var(--dim); font-weight:700; margin:10px 0 5px; text-transform:uppercase; letter-spacing:.4px; }
.model-item { display:flex; align-items:center; gap:8px; padding:9px 12px; border-radius:10px; border:1px solid var(--border); background:var(--panel2); cursor:pointer; margin-bottom:6px; }
.model-item:hover { border-color:var(--accent); }
.model-item.selected { border-color:var(--accent); background:var(--panel); }
.model-item .name { font-weight:600; }
.model-item .meta { font-size:11px; color:var(--dim); white-space:nowrap; margin-left:auto; }
.tag { font-size:10px; padding:1px 7px; border-radius:8px; white-space:nowrap; }
.tag.cat { background:var(--panel); border:1px solid var(--border); color:var(--dim); }
.tag.nokey { background:var(--danger); color:#ff9a9a; }
</style>
</head>
<body>
<header>
  <div class="logo">🤖 道码 Way<span>Coder</span></div>
  <div class="agent-label" id="agent-label">智能体: 智能体1</div>
  <div class="spacer"></div>
  <button class="btn" id="model-btn" title="选择模型">🧠 <span id="model-btn-label">模型</span></button>
  <select id="perm-select" title="权限模式（YOLO=直接执行 / Ask=每次确认）">
    <option value="ask">🛡 Ask</option>
    <option value="auto">✅ Auto</option>
    <option value="smartauto">🧭 SmartAuto</option>
    <option value="yolo">⚡ YOLO</option>
  </select>
  <button class="btn ghost" id="theme-btn" title="切换主题">🌙</button>
  <button class="btn" id="settings-btn" title="设置">⚙ 设置</button>
  <span class="version" id="version-label">__VERSION__</span>
</header>

<div class="layout">
  <aside id="sidebar-left">
    <div class="panel-head">🗂 槽位</div>
    <div id="slot-list"></div>
    <div class="panel-head">📜 历史会话</div>
    <button class="btn" id="new-session">＋ 新建会话</button>
    <div id="session-list"><div class="empty">加载中…</div></div>
  </aside>

  <main id="chat-col">
    <div id="messages"></div>
    <div id="input-bar">
      <button class="btn ghost" id="attach-btn" title="上传图片 / 音频">📎</button>
      <input type="file" id="file-input" accept="image/*,audio/*" style="display:none">
      <textarea id="input" placeholder="输入消息，Enter 发送，Ctrl+Enter 换行" rows="3"></textarea>
      <button class="btn" id="send" title="发送">✈️</button>
    </div>
    <div id="model-bar">
      <button class="btn ghost" id="model-add-btn" title="添加 / 选择模型">➕</button>
      <label class="model-label">大模型:<select id="big-model-select" title="大模型（复杂任务）"></select></label>
      <label class="model-label">小模型:<select id="small-model-select" title="小模型（简单 / 压缩）"></select></label>
      <select id="economy-select" title="省 Token 模式"></select>
      <span id="model-status" class="model-status"></span>
    </div>
  </main>

  <aside id="sidebar-right">
    <div class="card"><div class="card-head">📋 任务</div><div id="panel-todos"><div class="empty">无任务</div></div></div>
    <div class="card"><div class="card-head">💰 Token / 费用</div><div id="panel-tokens"></div></div>
    <div class="card"><div class="card-head">🔧 修改文件</div><div id="panel-files"><div class="empty">无</div></div></div>
    <div class="card"><div class="card-head">🔌 MCP 服务器</div><div id="panel-mcp"><div class="empty">未配置</div></div></div>
    <div class="card"><div class="card-head">🧠 LSP 会话</div><div id="panel-lsp"><div class="empty">无活动会话</div></div></div>
  </aside>
</div>

<div id="drawer">
  <div id="drawer-head"><b>⚙ 设置</b><button class="btn ghost" id="drawer-close" style="font-size:20px;">×</button></div>
  <div id="drawer-body">
    <div id="settings-nav"></div>
    <div id="settings-detail"></div>
  </div>
  <div id="drawer-foot">
    <button class="btn" id="settings-save">💾 保存</button>
  </div>
</div>

<div class="modal" id="model-modal">
  <div class="modal-card model-card">
    <h2>🧠 选择模型</h2>
    <div class="row" style="margin-bottom:10px;">
      <input id="model-search" type="text" placeholder="搜索模型名称 / 供应商…">
      <button class="btn ghost" id="model-close" style="font-size:20px;">×</button>
    </div>
    <div id="model-list"></div>
    <div class="row" style="margin-top:14px;">
      <button class="btn ghost" id="model-cancel">取消</button>
      <button class="btn" id="model-confirm">切换模型</button>
    </div>
  </div>
</div>

<div class="modal" id="key-modal">
  <div class="modal-card">
    <h2>🔑 输入 API Key</h2>
    <p id="key-hint">当前模型需要 API Key（将按供应商保存到本地）。</p>
    <input id="key-input" type="password" placeholder="sk-...">
    <div class="row">
      <button class="btn" id="key-cancel">取消</button>
      <button class="btn primary" id="key-save">保存</button>
    </div>
  </div>
</div>

<div class="modal" id="ask-modal">
  <div class="modal-card">
    <h2 id="ask-title"></h2>
    <div id="ask-body"></div>
    <div class="row" id="ask-actions"></div>
  </div>
</div>

<script>
const messages = document.getElementById('messages');
const input = document.getElementById('input');
const slotsEl = document.getElementById('slot-list');
const sessionListEl = document.getElementById('session-list');
const drawer = document.getElementById('drawer');
const drawerBody = document.getElementById('drawer-body');
const settingsNav = document.getElementById('settings-nav');
const settingsDetail = document.getElementById('settings-detail');
const keyModal = document.getElementById('key-modal');

// ── 流指针（滚动 bug 修复：assistant 文本流 与 工具输出流 分离）──
let assistantStreamEl = null;
let toolOutputEl = null;
let reasoningEl = null;
let currentProvider = '';
let hasKey = false;
let isBusy = false;
function setBusy(b) {
  isBusy = b;
  const btn = document.getElementById('send');
  if (b) { btn.innerHTML = '⏹'; btn.classList.add('stop'); btn.title = '停止'; }
  else { btn.innerHTML = '✈️'; btn.classList.remove('stop'); btn.title = '发送'; }
}

function scroll() { messages.scrollTop = messages.scrollHeight; }
function addMsg(role, text) {
  const el = document.createElement('div');
  el.className = 'msg ' + role;
  if ((role === 'assistant' || role === 'cmd') && text) {
    el.innerHTML = mdToHtml(text);
  } else {
    el.textContent = text;
  }
  messages.appendChild(el);
  scroll();
  return el;
}
function addTool(name, args) {
  const el = document.createElement('div');
  el.className = 'tool';
  el.innerHTML = '🔧 <b>' + name + '</b> ' + (args || '');
  messages.appendChild(el);
  scroll();
}
function ensureAssistantStream() {
  if (!assistantStreamEl) {
    assistantStreamEl = addMsg('assistant', '');
    assistantStreamEl.classList.add('streaming');
  }
  return assistantStreamEl;
}
function endAssistantStream() { assistantStreamEl = null; }
function finalizeAssistant() {
  if (assistantStreamEl) {
    assistantStreamEl.classList.remove('streaming');
    if (assistantStreamEl.textContent) {
      assistantStreamEl.innerHTML = mdToHtml(assistantStreamEl.textContent);
    }
  }
}
function ensureToolOutput() {
  if (!toolOutputEl) {
    toolOutputEl = document.createElement('div');
    toolOutputEl.className = 'tool-output';
    messages.appendChild(toolOutputEl);
    scroll();
  }
  return toolOutputEl;
}
function endToolOutput() {
  if (toolOutputEl) toolOutputEl.innerHTML = renderToolOutput(toolOutputEl.textContent);
  toolOutputEl = null;
}
function addReasoning() {
  const el = document.createElement('div');
  el.className = 'msg reasoning';
  messages.appendChild(el);
  scroll();
  return el;
}
function endReasoning() { reasoningEl = null; }
function clearMessages() { messages.innerHTML = ''; assistantStreamEl = null; toolOutputEl = null; reasoningEl = null; }

// ── 主题 ──
function applyTheme(t) {
  document.documentElement.dataset.theme = t;
  localStorage.setItem('waycoder-theme', t);
  document.getElementById('theme-btn').textContent = t === 'light' ? '☀️' : '🌙';
}
document.getElementById('theme-btn').onclick = () =>
  applyTheme(document.documentElement.dataset.theme === 'light' ? 'dark' : 'light');

// ── 权限模式（顶栏下拉）──
const permSelect = document.getElementById('perm-select');
function applyPermMode(mode) {
  if (mode && permSelect.value !== mode) permSelect.value = mode;
}
permSelect.onchange = () =>
  fetch('/perm', { method: 'POST', body: JSON.stringify({ mode: permSelect.value }) }).catch(() => {});

// ── 槽位（左栏）──
function renderSlots(state) {
  document.getElementById('agent-label').textContent = '智能体: 智能体' + (state.activeSlot + 1);
  slotsEl.innerHTML = '';
  for (let i = 0; i < state.slots.length; i++) {
    const s = state.slots[i];
    const b = document.createElement('div');
    b.className = 'slot' + (i === state.activeSlot ? ' active' : '') + (s.hasHistory ? ' has' : '');
    b.textContent = 'F' + (i + 1);
    b.title = s.model ? ('F' + (i + 1) + ' · ' + s.model) : ('F' + (i + 1) + ' · 空');
    b.onclick = () => switchSlot(i);
    slotsEl.appendChild(b);
  }
}
function switchSlot(i) {
  fetch('/slot', { method: 'POST', body: JSON.stringify({ slot: i }) })
    .then(r => r.json())
    .then(list => { clearMessages(); list.forEach(m => addMsg(m.role === 'user' ? 'user' : 'assistant', m.content)); })
    .then(fetchPanel);
}

// ── 历史会话（左栏）──
function fetchSessions() {
  fetch('/sessions').then(r => r.json()).then(renderSessions).catch(() => {});
}
function renderSessions(list) {
  sessionListEl.innerHTML = '';
  if (!list || !list.length) { sessionListEl.innerHTML = '<div class="empty">暂无历史会话</div>'; return; }
  list.forEach(s => {
    const item = document.createElement('div');
    item.className = 'session-item';
    item.title = s.id;
    const p = document.createElement('div');
    p.className = 'preview';
    p.textContent = s.preview || s.id;
    const m = document.createElement('div');
    m.className = 'meta';
    m.textContent = (s.model || '?') + ' · ' + (s.savedAt || '') + (s.msgCount ? (' · ' + s.msgCount + ' 条') : '');
    const ops = document.createElement('div');
    ops.className = 'ops';
    const rb = document.createElement('button'); rb.textContent = '✎'; rb.title = '重命名';
    rb.onclick = e => { e.stopPropagation(); renameSession(s.id); };
    const db = document.createElement('button'); db.textContent = '✕'; db.title = '删除';
    db.onclick = e => { e.stopPropagation(); deleteSession(s.id); };
    ops.appendChild(rb); ops.appendChild(db);
    item.appendChild(p); item.appendChild(m); item.appendChild(ops);
    item.onclick = () => loadSession(s.id);
    sessionListEl.appendChild(item);
  });
}
function loadSession(id) {
  fetch('/sessions/load', { method: 'POST', body: JSON.stringify({ id: id }) })
    .then(r => r.json())
    .then(res => { if (res && res.ok === false) { alert(res.error || '加载失败'); return; } })
    .catch(() => {});
}
function deleteSession(id) {
  if (!confirm('删除会话 ' + id + ' ?')) return;
  fetch('/sessions/delete', { method: 'POST', body: JSON.stringify({ id: id }) }).then(fetchSessions).catch(() => {});
}
function renameSession(id) {
  const newId = prompt('重命名会话（ID）:', id);
  if (!newId || newId === id) return;
  fetch('/sessions/rename', { method: 'POST', body: JSON.stringify({ id: id, newId: newId }) })
    .then(r => r.json())
    .then(res => { if (res && res.ok === false) alert(res.error || '重命名失败'); })
    .then(fetchSessions)
    .catch(() => {});
}
document.getElementById('new-session').onclick = () => {
  fetch('/sessions/new', { method: 'POST' })
    .then(r => r.json())
    .then(res => { if (res && res.ok) alert('已保存新会话：' + res.id); })
    .then(fetchSessions)
    .catch(() => {});
};

// ── 右栏面板 ──
function fetchPanel() {
  if (document.hidden) return;
  fetch('/panel').then(r => r.json()).then(renderPanel).catch(() => {});
}
function renderPanel(p) {
  renderTodos(p.todos);
  renderTokens(p.tokens, p.cost);
  renderFiles(p.files);
  renderMcp(p.mcp);
  renderLsp(p.lsp);
}
function statusDot(status) {
  const map = { pending:'pending', in_progress:'in_progress', completed:'completed', cancelled:'cancelled', blocked:'blocked' };
  return '<span class="dot ' + (map[status] || 'pending') + '"></span>';
}
function renderTodos(todos) {
  const el = document.getElementById('panel-todos');
  if (!todos || !todos.length) { el.innerHTML = '<div class="empty">无任务</div>'; return; }
  el.innerHTML = todos.map(t => '<div class="item">' + statusDot(t.status) + escapeHtml(t.title || t.id) + '</div>').join('');
}
function renderTokens(tokens, cost) {
  const el = document.getElementById('panel-tokens');
  if (!tokens) { el.innerHTML = ''; return; }
  const tp = tokens.totalPrompt || 0, tc = tokens.totalCompletion || 0;
  const tP = tokens.taskPrompt || 0, tC = tokens.taskCompletion || 0;
  const fmt = n => (n == null ? '—' : Number(n).toLocaleString());
  const usd = n => (n == null ? '—' : '$' + Number(n).toFixed(4));
  el.innerHTML =
    '<div class="row"><span class="k">本轮</span><span class="v">' + fmt(tP) + ' / ' + fmt(tC) + '</span></div>' +
    '<div class="row"><span class="k">累计</span><span class="v">' + fmt(tp) + ' / ' + fmt(tc) + '</span></div>' +
    '<div class="row"><span class="k">本轮费用</span><span class="v">' + usd(cost && cost.task) + '</span></div>' +
    '<div class="row"><span class="k">累计估计</span><span class="v">' + usd(cost && cost.estimated) + '</span></div>' +
    (tokens.tokensPerSec ? '<div class="row"><span class="k">速率</span><span class="v">' + Number(tokens.tokensPerSec).toFixed(1) + ' tok/s</span></div>' : '');
}
function renderFiles(files) {
  const el = document.getElementById('panel-files');
  if (!files || !files.length) { el.innerHTML = '<div class="empty">无</div>'; return; }
  el.innerHTML = files.map(f => {
    const path = (f && f.path) || f || '';
    const name = String(path).split(/[\\/]/).pop();
    const a = f && f.added, d = f && f.deleted;
    const stat = (a || d) ? (' <span class="stat-add">+' + (a || 0) + '</span><span class="stat-del">-' + (d || 0) + '</span>') : '';
    return '<div class="item">' + escapeHtml(name) + stat + '</div>';
  }).join('');
}
function renderMcp(mcp) {
  const el = document.getElementById('panel-mcp');
  if (!mcp || !mcp.length) { el.innerHTML = '<div class="empty">未配置</div>'; return; }
  el.innerHTML = mcp.map(s => {
    const dot = s.status === 'connected' ? 'on' : (s.status === 'connecting' ? 'connecting' : 'off');
    const extra = s.toolCount ? (' · ' + s.toolCount + ' 工具') : '';
    return '<div class="item"><span class="dot ' + dot + '"></span>' + escapeHtml(s.name) + extra +
      (s.error ? '<div style="color:var(--dim);font-size:11px;">' + escapeHtml(s.error) + '</div>' : '') + '</div>';
  }).join('');
}
function renderLsp(lsp) {
  const el = document.getElementById('panel-lsp');
  if (!lsp || !lsp.length) { el.innerHTML = '<div class="empty">无活动会话</div>'; return; }
  el.innerHTML = lsp.map(s => {
    const dot = s.hasExited ? 'off' : (s.initialized ? 'on' : 'connecting');
    return '<div class="item"><span class="dot ' + dot + '"></span>' + escapeHtml(s.command) +
      (s.root ? '<div style="color:var(--dim);font-size:11px;">' + escapeHtml(s.root) + '</div>' : '') + '</div>';
  }).join('');
}

// ── 模型选择窗口 ──
let modelMap = {};
let allModels = [];
let currentModelId = '';
let currentSmallModel = '';
let pendingModelId = '';
let pendingSmallModelId = '';
let selectedModelId = '';
function renderModels(models, state) {
  modelMap = {};
  allModels = models;
  models.forEach(m => { modelMap[m.id] = m; });
  currentModelId = state.model;
  updateModelBtn();
  renderModelBar(state);
}
function updateModelBtn() {
  const m = modelMap[currentModelId];
  document.getElementById('model-btn-label').textContent = m ? m.name : (currentModelId || '模型');
}
// ── 模型状态栏（输入框下方：大模型 / 小模型 / 省钱模式 / 状态）──
const ECONOMY_OPTIONS = [['off', '省钱：关'], ['auto', '省钱：自动'], ['on', '省钱：开']];
const SMALL_MODEL_IDS = ['deepseek-chat', 'deepseek-v4-flash', 'gpt-5.4-mini', 'gpt-4o-mini', 'deepseek-v4-pro'];
function renderModelBar(state) {
  const big = document.getElementById('big-model-select');
  const small = document.getElementById('small-model-select');
  const eco = document.getElementById('economy-select');
  if (allModels.length && big.options.length === 0) {
    allModels.forEach(m => { const op = document.createElement('option'); op.value = m.id; op.textContent = m.name; big.appendChild(op); });
    SMALL_MODEL_IDS.forEach(id => { const m = modelMap[id]; if (m) { const op = document.createElement('option'); op.value = id; op.textContent = m.name; small.appendChild(op); } });
    ECONOMY_OPTIONS.forEach(([v, l]) => { const op = document.createElement('option'); op.value = v; op.textContent = l; eco.appendChild(op); });
  }
  if (currentModelId) big.value = currentModelId;
  currentSmallModel = state.smallModel || '';
  if (currentSmallModel) small.value = currentSmallModel;
  if (state.economy) eco.value = state.economy;
  const m = modelMap[currentModelId];
  const providerName = state.providerName || state.provider || '';
  document.getElementById('model-status').textContent =
    (m ? m.name : (currentModelId || '')) + (providerName ? ' · ' + providerName : '') + (state.hasKey ? '' : ' · ⚠ 无 key');
}
document.getElementById('big-model-select').onchange = e => {
  const m = modelMap[e.target.value];
  if (!m) { e.target.value = currentModelId; return; }
  if (!m.hasKey && m.providerId !== 'local' && m.providerId !== 'custom') e.target.value = currentModelId;
  chooseModel(m);
};
document.getElementById('small-model-select').onchange = e => {
  const m = modelMap[e.target.value];
  if (!m) { e.target.value = currentSmallModel; return; }
  if (!m.hasKey && m.providerId !== 'local' && m.providerId !== 'custom') {
    e.target.value = currentSmallModel;
    pendingSmallModelId = m.id;
    currentProvider = m.providerId;
    document.getElementById('key-hint').textContent = '为 ' + m.providerId + ' 输入 API Key（保存后切换到小模型 ' + m.name + '）。';
    document.getElementById('key-input').value = '';
    keyModal.classList.add('open');
    return;
  }
  fetch('/settings', { method: 'POST', body: JSON.stringify({ key: 'SmallModel', value: m.id }) }).catch(() => {});
};
document.getElementById('economy-select').onchange = e =>
  fetch('/settings', { method: 'POST', body: JSON.stringify({ key: 'EconomyMode', value: e.target.value }) }).catch(() => {});
document.getElementById('model-add-btn').onclick = openModelModal;
function formatContext(ctx) {
  if (!ctx) return '';
  return ctx >= 1024 ? (Math.round(ctx / 1024)) + 'k' : ctx;
}
function openModelModal() {
  selectedModelId = currentModelId;
  document.getElementById('model-search').value = '';
  renderModelList('');
  document.getElementById('model-modal').classList.add('open');
}
function renderModelList(filter) {
  const el = document.getElementById('model-list');
  const f = (filter || '').trim().toLowerCase();
  const byProvider = {};
  allModels.forEach(m => {
    if (f && !(m.name.toLowerCase().includes(f) || m.provider.toLowerCase().includes(f) || m.providerId.toLowerCase().includes(f))) return;
    (byProvider[m.providerId] = byProvider[m.providerId] || []).push(m);
  });
  el.innerHTML = '';
  const pids = Object.keys(byProvider);
  if (pids.length === 0) { el.innerHTML = '<div class="empty">无匹配模型</div>'; return; }
  pids.forEach(pid => {
    const g = document.createElement('div');
    g.className = 'model-group';
    const gn = document.createElement('div');
    gn.className = 'gname';
    gn.textContent = pid;
    g.appendChild(gn);
    byProvider[pid].forEach(m => {
      const item = document.createElement('div');
      item.className = 'model-item' + (m.id === selectedModelId ? ' selected' : '');
      const name = document.createElement('span');
      name.className = 'name';
      name.textContent = m.name;
      const cat = document.createElement('span');
      cat.className = 'tag cat';
      cat.textContent = m.category || pid;
      item.appendChild(name);
      item.appendChild(cat);
      if (!m.hasKey) {
        const nk = document.createElement('span');
        nk.className = 'tag nokey';
        nk.textContent = '需 key';
        item.appendChild(nk);
      }
      const meta = document.createElement('span');
      meta.className = 'meta';
      meta.textContent = formatContext(m.context) + (m.inputPrice > 0 ? (' · $' + m.inputPrice) : '');
      item.appendChild(meta);
      item.onclick = () => selectModel(m);
      g.appendChild(item);
    });
    el.appendChild(g);
  });
}
function selectModel(m) {
  selectedModelId = m.id;
  renderModelList(document.getElementById('model-search').value);
}
function chooseModel(m) {
  if (!m.hasKey && m.providerId !== 'local' && m.providerId !== 'custom') {
    pendingModelId = m.id;
    currentProvider = m.providerId;
    document.getElementById('key-hint').textContent = '为 ' + m.providerId + ' 输入 API Key（保存后切换到 ' + m.name + '）。';
    document.getElementById('key-input').value = '';
    document.getElementById('model-modal').classList.remove('open');
    keyModal.classList.add('open');
    return;
  }
  fetch('/model', { method: 'POST', body: JSON.stringify({ modelId: m.id }) })
    .then(() => { currentModelId = m.id; selectedModelId = ''; updateModelBtn(); document.getElementById('model-modal').classList.remove('open'); })
    .catch(() => {});
}
function confirmModel() {
  const m = modelMap[selectedModelId];
  if (m) chooseModel(m);
}
document.getElementById('model-btn').onclick = openModelModal;
document.getElementById('model-search').oninput = e => renderModelList(e.target.value);
document.getElementById('model-close').onclick = () => { selectedModelId = ''; document.getElementById('model-modal').classList.remove('open'); };
document.getElementById('model-cancel').onclick = () => { selectedModelId = ''; document.getElementById('model-modal').classList.remove('open'); };
document.getElementById('model-confirm').onclick = confirmModel;
// 点击遮罩（卡片外）关闭模型弹窗
document.getElementById('model-modal').addEventListener('click', e => {
  if (e.target === e.currentTarget) { selectedModelId = ''; e.currentTarget.classList.remove('open'); }
});

// ── key 弹窗 ──
function saveKey() {
  const k = document.getElementById('key-input').value.trim();
  if (!k) return;
  fetch('/key', { method: 'POST', body: JSON.stringify({ providerId: currentProvider, apiKey: k }) })
    .then(() => {
      hasKey = true;
      keyModal.classList.remove('open');
      if (pendingModelId) {
        const id = pendingModelId; pendingModelId = '';
        fetch('/model', { method: 'POST', body: JSON.stringify({ modelId: id }) })
          .then(() => { currentModelId = id; updateModelBtn(); })
          .catch(() => {});
      }
      if (pendingSmallModelId) {
        const id = pendingSmallModelId; pendingSmallModelId = '';
        fetch('/settings', { method: 'POST', body: JSON.stringify({ key: 'SmallModel', value: id }) })
          .then(() => { currentSmallModel = id; })
          .catch(() => {});
      }
    });
}
document.getElementById('key-save').onclick = saveKey;
document.getElementById('key-cancel').onclick = () => keyModal.classList.remove('open');
document.getElementById('key-input').onkeydown = e => { if (e.key === 'Enter') saveKey(); };

// ── 设置（两列：左类别导航 + 右详细设置）──
let settingsGroups = [];
let settingsActiveGroup = 0;
function renderSettingsNav() {
  settingsNav.innerHTML = '';
  if (settingsActiveGroup >= settingsGroups.length) settingsActiveGroup = 0;
  settingsGroups.forEach((g, i) => {
    const b = document.createElement('button');
    b.className = 'nav-item' + (i === settingsActiveGroup ? ' active' : '');
    b.textContent = g.category;
    b.onclick = () => {
      settingsActiveGroup = i;
      settingsNav.querySelectorAll('.nav-item').forEach(x => x.classList.remove('active'));
      b.classList.add('active');
      renderSettingsDetail(g);
    };
    settingsNav.appendChild(b);
  });
  if (settingsGroups.length > 0) renderSettingsDetail(settingsGroups[settingsActiveGroup]);
}
function renderSettingsDetail(g) {
  settingsDetail.innerHTML = '';
  g.items.forEach(it => {
    const row = document.createElement('div');
    row.className = 'set-row';
    const label = document.createElement('label');
    label.textContent = it.label;
    const desc = document.createElement('div');
    desc.className = 'desc';
    desc.textContent = it.desc;
    row.appendChild(label);
    row.appendChild(desc);
    let ctrl;
    if (it.type === 'select' && it.options && it.options.length) {
      ctrl = document.createElement('select');
      it.options.forEach(o => { const op = document.createElement('option'); op.value = o; op.textContent = o || '(默认)'; if (o === it.value) op.selected = true; ctrl.appendChild(op); });
    } else if (it.type === 'toggle') {
      ctrl = document.createElement('input'); ctrl.type = 'checkbox';
      ctrl.checked = it.value === 'true' || it.value === '1';
    } else if (it.type === 'secret') {
      ctrl = document.createElement('input'); ctrl.type = 'password';
      ctrl.placeholder = it.value ? '已设置（留空则不修改）' : '未设置';
      ctrl.dataset.secret = '1';
    } else {
      ctrl = document.createElement('input');
      ctrl.type = it.type === 'number' ? 'number' : 'text';
      ctrl.value = it.value;
    }
    ctrl.dataset.key = it.key;
    ctrl.dataset.type = it.type;
    row.appendChild(ctrl);
    settingsDetail.appendChild(row);
  });
}
function saveSetting(ctrl) {
  let value;
  if (ctrl.dataset.type === 'toggle') value = ctrl.checked ? 'true' : 'false';
  else if (ctrl.dataset.secret === '1' && ctrl.value === '') return null; // 留空不修改
  else value = ctrl.value;
  return { key: ctrl.dataset.key, value: value };
}
document.getElementById('settings-save').onclick = () => {
  const ctrls = settingsDetail.querySelectorAll('input, select');
  const btn = document.getElementById('settings-save');
  const pending = [];
  ctrls.forEach(c => { const p = saveSetting(c); if (p) pending.push(p); });
  if (pending.length === 0) {
    btn.textContent = '无改动';
    setTimeout(() => { btn.textContent = '💾 保存'; }, 1500);
    return;
  }
  btn.textContent = '⏳ 保存中…';
  Promise.all(pending.map(p =>
    fetch('/settings', { method: 'POST', body: JSON.stringify(p) })
      .then(r => r.json())
      .then(res => (res && res.ok === false) ? Promise.reject(new Error(res.error || '设置失败')) : res)
  )).then(() => {
    btn.textContent = '✅ 已保存 ' + pending.length + ' 项';
    setTimeout(() => { btn.textContent = '💾 保存'; }, 1500);
  }).catch(err => {
    btn.textContent = '💾 保存';
    alert('保存失败：' + (err && err.message ? err.message : err));
  });
};
document.getElementById('settings-btn').onclick = () => {
  fetch('/settings').then(r => r.json()).then(g => { settingsGroups = g; renderSettingsNav(); drawer.classList.add('open'); });
};
document.getElementById('drawer-close').onclick = () => drawer.classList.remove('open');

// ── Web 交互对话框（ask）──
let pendingAsk = null;
function showAsk(d) {
  pendingAsk = d;
  const title = document.getElementById('ask-title');
  const body = document.getElementById('ask-body');
  const actions = document.getElementById('ask-actions');
  title.textContent = d.title || '';
  body.innerHTML = '';
  actions.innerHTML = '';
  document.querySelector('#ask-modal .modal-card').classList.remove('diff');
  if (d.kind === 'select') {
    d.choices.forEach(c => {
      const b = document.createElement('button');
      b.className = 'ask-option';
      b.textContent = c;
      b.onclick = () => answerAsk(c);
      body.appendChild(b);
    });
  } else if (d.kind === 'multi') {
    const selected = new Set();
    d.choices.forEach(c => {
      const lbl = document.createElement('label');
      lbl.className = 'ask-multi';
      const cb = document.createElement('input'); cb.type = 'checkbox'; cb.value = c;
      cb.onchange = () => { cb.checked ? selected.add(c) : selected.delete(c); };
      lbl.appendChild(cb); lbl.appendChild(document.createTextNode(c));
      body.appendChild(lbl);
    });
    const ok = document.createElement('button'); ok.className = 'btn primary'; ok.textContent = '确定';
    ok.onclick = () => answerAsk([...selected].join('\n'));
    actions.appendChild(ok);
  } else if (d.kind === 'text') {
    const inp = document.createElement('input'); inp.type = 'text'; inp.id = 'ask-input';
    if (d.default) inp.value = d.default;
    body.appendChild(inp);
    const ok = document.createElement('button'); ok.className = 'btn primary'; ok.textContent = '确定';
    ok.onclick = () => answerAsk(inp.value);
    actions.appendChild(ok);
    inp.onkeydown = e => { if (e.key === 'Enter') answerAsk(inp.value); };
    setTimeout(() => inp.focus(), 50);
  } else if (d.kind === 'confirm') {
    title.textContent = d.title || '确认操作';
    const msg = document.createElement('div'); msg.className = 'ask-message'; msg.textContent = d.message || '';
    body.appendChild(msg);
    const yes = document.createElement('button'); yes.className = 'btn primary'; yes.textContent = '是';
    yes.onclick = () => answerAsk('0');
    const no = document.createElement('button'); no.className = 'btn danger'; no.textContent = '否';
    no.onclick = () => answerAsk('2');
    actions.appendChild(yes);
    if (d.allowAll) {
      const all = document.createElement('button'); all.className = 'btn'; all.textContent = '总是允许';
      all.onclick = () => answerAsk('1');
      actions.appendChild(all);
    }
    actions.appendChild(no);
  } else if (d.kind === 'diff') {
    title.textContent = d.title || 'Diff 预览';
    document.querySelector('#ask-modal .modal-card').classList.add('diff');
    (d.hunks || []).forEach((h, hi) => {
      const block = document.createElement('div');
      block.className = 'diff-hunk';
      const head = document.createElement('label');
      head.className = 'diff-hunk-head';
      const cb = document.createElement('input');
      cb.type = 'checkbox';
      cb.className = 'diff-hunk-check';
      cb.checked = true;
      cb.dataset.idx = hi;
      const hdr = document.createElement('span');
      hdr.textContent = h.header || ('Hunk ' + (hi + 1));
      head.appendChild(cb);
      head.appendChild(hdr);
      const pre = document.createElement('pre');
      pre.className = 'diff-hunk-lines';
      (h.lines || []).forEach(l => {
        const ln = document.createElement('span');
        ln.className = 'diff-line ' + (l.kind === '-' ? 'del' : l.kind === '+' ? 'add' : 'ctx');
        ln.textContent = (l.kind === ' ' ? ' ' : l.kind) + (l.text || '');
        pre.appendChild(ln);
      });
      block.appendChild(head);
      block.appendChild(pre);
      body.appendChild(block);
    });
    const acceptAll = document.createElement('button'); acceptAll.className = 'btn primary'; acceptAll.textContent = '全部接受';
    acceptAll.onclick = () => answerDiff('accept', null);
    const applySel = document.createElement('button'); applySel.className = 'btn'; applySel.textContent = '应用选中';
    applySel.onclick = () => {
      const acc = [];
      body.querySelectorAll('.diff-hunk-check').forEach(c => { if (c.checked) acc.push(Number(c.dataset.idx)); });
      answerDiff('partial', acc);
    };
    const rejectAll = document.createElement('button'); rejectAll.className = 'btn danger'; rejectAll.textContent = '全部拒绝';
    rejectAll.onclick = () => answerDiff('reject', null);
    actions.appendChild(acceptAll);
    actions.appendChild(applySel);
    actions.appendChild(rejectAll);
  }
  document.getElementById('ask-modal').classList.add('open');
}
function answerDiff(decision, accepted) {
  if (!pendingAsk) return;
  const id = pendingAsk.requestId;
  const value = JSON.stringify({ decision: decision, accepted: accepted || [] });
  fetch('/answer', { method: 'POST', body: JSON.stringify({ requestId: id, value: value }) })
    .then(() => { document.getElementById('ask-modal').classList.remove('open'); pendingAsk = null; })
    .catch(() => {});
}
function answerAsk(value) {
  if (!pendingAsk) return;
  const id = pendingAsk.requestId;
  fetch('/answer', { method: 'POST', body: JSON.stringify({ requestId: id, value: value }) })
    .then(() => { document.getElementById('ask-modal').classList.remove('open'); pendingAsk = null; })
    .catch(() => {});
}

// ── 发送 / 停止 ──
function handleUiCommand(text) {
  const lower = text.toLowerCase();
  if (lower === '/theme') {
    applyTheme(document.documentElement.dataset.theme === 'light' ? 'dark' : 'light');
    return true;
  }
  if (lower === '/settings') {
    fetch('/settings').then(r => r.json()).then(g => { settingsGroups = g; renderSettingsNav(); drawer.classList.add('open'); });
    return true;
  }
  if (lower === '/model' || lower === '/m') {
    openModelModal();
    return true;
  }
  return false;
}
function send() {
  if (isBusy) { fetch('/interrupt', { method: 'POST' }).catch(() => {}); return; }
  const text = input.value.trim();
  if (!text) return;
  input.value = '';
  input.style.height = 'auto';

  // 纯 UI 斜杠命令（操作 DOM，不进入聊天流）
  if (handleUiCommand(text)) return;

  addMsg('user', text);
  setBusy(true);

  // 斜杠命令 → 后端路由（未识别回退为普通 Agent 消息）
  if (text.startsWith('/') && text.length > 1) {
    fetch('/command', { method: 'POST', body: JSON.stringify({ input: text }) })
      .then(r => r.json())
      .then(res => {
        if (res && res.ok && res.handled) {
          addMsg('cmd', res.output || '');
          setBusy(false);
        } else {
          fetch('/chat', { method: 'POST', body: text }).catch(() => {});
        }
      })
      .catch(() => { fetch('/chat', { method: 'POST', body: text }).catch(() => {}); });
    return;
  }

  fetch('/chat', { method: 'POST', body: text }).catch(() => {});
}
document.getElementById('send').onclick = send;
input.addEventListener('keydown', e => {
  if (e.key === 'Enter' && !e.ctrlKey && !e.shiftKey) { e.preventDefault(); send(); }
});
input.addEventListener('input', () => { input.style.height = 'auto'; input.style.height = Math.min(input.scrollHeight, 240) + 'px'; });

// ── 多模态上传（图片 → vision 队列 / 音频 → 转录为文字）──
const attachBtn = document.getElementById('attach-btn');
const fileInput = document.getElementById('file-input');
attachBtn.onclick = () => fileInput.click();
fileInput.onchange = () => {
  if (fileInput.files && fileInput.files.length) { uploadFile(fileInput.files[0]); fileInput.value = ''; }
};
function uploadFile(file) {
  const kind = (file.type || '').startsWith('audio/') ? 'audio' : 'image';
  addMsg('system', '⏳ 上传中 ' + file.name + '…');
  fetch('/upload?kind=' + kind, {
    method: 'POST',
    headers: { 'X-File-Name': encodeURIComponent(file.name) },
    body: file
  })
    .then(r => r.json())
    .then(res => {
      if (res && res.ok) {
        if (res.kind === 'image') {
          addMsg('system', '🖼 图片 ' + (res.name || file.name) + ' 已附加，下一条消息将发送给多模态模型');
        } else {
          addMsg('system', '🎙 音频 ' + (res.name || file.name) + ' 转录完成');
          if (res.text) { addMsg('user', res.text); fetch('/chat', { method: 'POST', body: res.text }).catch(() => {}); }
        }
      } else {
        addMsg('system', '✘ 上传失败：' + (res && res.error ? res.error : '未知错误'));
      }
    })
    .catch(() => addMsg('system', '✘ 上传失败（网络错误）'));
}

// ── 工具函数 ──
function escapeHtml(s) {
  return String(s == null ? '' : s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}
// 推理内容按 «dim»…«/» 标记以淡色块显示（颜色变淡，不进正文 Markdown）
function handleToken(s) {
  s = String(s == null ? '' : s);
  if (s.indexOf('«dim»') >= 0 || s.indexOf('«/»') >= 0) {
    if (s.indexOf('«dim»') >= 0 && !reasoningEl) reasoningEl = addReasoning();
    const rest = s.split('«dim»').join('').split('«/»').join('');
    if (s.indexOf('«/»') >= 0) endReasoning();
    if (rest.trim()) (reasoningEl || ensureAssistantStream()).textContent += rest;
    scroll();
    return;
  }
  endToolOutput();
  if (reasoningEl) reasoningEl.textContent += s;
  else ensureAssistantStream().textContent += s;
  scroll();
}

// ── 语法高亮（手搓 tokenizer，无 CDN，XSS 安全：先扫 token 再统一转义着色）──
const LANG_KEYWORDS = {
  'js': 'const let var function return if else for while do switch case break continue new class extends super this typeof instanceof in of import export default try catch finally throw async await null undefined true false delete void yield',
  'javascript': 'const let var function return if else for while do switch case break continue new class extends super this typeof instanceof in of import export default try catch finally throw async await null undefined true false',
  'ts': 'const let var function return if else for while do switch case break continue new class extends super this typeof instanceof in of import export default try catch finally throw async await null undefined true false type interface enum namespace readonly abstract implements declare keyof infer never unknown any void',
  'typescript': 'const let var function return if else for while do switch case break continue new class extends super this typeof instanceof in of import export default try catch finally throw async await null undefined true false type interface enum namespace readonly abstract implements declare keyof infer never unknown any void',
  'csharp': 'public private protected internal static void class struct interface enum namespace using return if else for foreach while do switch case break continue new virtual override abstract sealed readonly const async await try catch finally throw null true false var string int bool double float decimal long byte char object is as lock get set value',
  'cs': 'public private protected internal static void class struct interface enum namespace using return if else for foreach while do switch case break continue new virtual override abstract sealed readonly const async await try catch finally throw null true false var string int bool double float decimal long byte char object is as lock',
  'python': 'def return if elif else for while in not and or import from as class try except finally with lambda pass break continue raise None True False global nonlocal assert del is yield',
  'py': 'def return if elif else for while in not and or import from as class try except finally with lambda pass break continue raise None True False global nonlocal assert del is yield',
  'java': 'public private protected static final void class interface enum extends implements import package return if else for while do switch case break continue new try catch finally throw throws null true false abstract synchronized volatile transient native instanceof this super int long double float boolean char byte short',
  'c': 'return if else for while do switch case break continue struct typedef union enum static const extern void char int long float double unsigned signed sizeof null true false',
  'cpp': 'return if else for while do switch case break continue struct class namespace template typename using public private protected virtual override constexpr static const auto new delete nullptr true false void char int long float double unsigned signed sizeof this try catch throw',
  'c++': 'return if else for while do switch case break continue struct class namespace template typename using public private protected virtual override constexpr static const auto new delete nullptr true false void char int long float double unsigned signed sizeof this try catch throw',
  'go': 'func return if else for range switch case break continue type struct interface map chan go defer import package var const nil true false select',
  'rust': 'fn let mut return if else for while loop match impl trait struct enum use pub crate mod self super async await ref move where type const static unsafe extern',
  'bash': 'if then else elif fi for while do done case esac function return exit echo cd export source local readonly shift',
  'sh': 'if then else elif fi for while do done case esac function return exit echo cd export source local',
  'shell': 'if then else elif fi for while do done case esac function return exit echo cd export source local',
  'html': 'html head body div span p a img script style link meta title h1 h2 h3 h4 h5 h6 ul ol li table tr td th form input button select option label header footer nav main section article',
  'css': 'color background border margin padding width height display position flex grid font text align center left right top bottom absolute relative none block inline',
  'sql': 'select from where insert into values update set delete create table drop alter index view join inner left right outer on group by order having limit union and or not null as distinct',
  'yaml': 'true false null', 'yml': 'true false null',
  'json': '', 'xml': '', 'diff': '', 'text': '', 'plaintext': '', 'md': '', 'markdown': ''
};
const HIGHLIGHT_COMMON = 'return if else for while do switch case break continue function class struct enum interface type var let const import export new try catch finally throw null true false this static public private protected void string int bool';
function highlightCode(code, lang) {
  if (!code) return '';
  const kwStr = Object.prototype.hasOwnProperty.call(LANG_KEYWORDS, lang || '') ? LANG_KEYWORDS[lang || ''] : HIGHLIGHT_COMMON;
  const kw = new Set(kwStr.split(' ').filter(Boolean));
  const hashComment = lang === 'python' || lang === 'py' || lang === 'bash' || lang === 'sh' || lang === 'shell' || lang === 'yaml' || lang === 'yml' || lang === 'sql';
  const dashComment = lang === 'sql';
  const htmlMode = lang === 'html' || lang === 'xml';
  let out = '';
  let i = 0;
  const n = code.length;
  while (i < n) {
    const c = code[i];
    if (c === ' ' || c === '\t' || c === '\n' || c === '\r') { out += c; i++; continue; }
    // 行注释
    if ((c === '/' && code[i + 1] === '/') || (c === '#' && hashComment)) {
      let j = i; while (j < n && code[j] !== '\n') j++;
      out += '<span class="tok-com">' + escapeHtml(code.slice(i, j)) + '</span>'; i = j; continue;
    }
    // SQL -- 注释
    if (dashComment && c === '-' && code[i + 1] === '-') {
      let j = i; while (j < n && code[j] !== '\n') j++;
      out += '<span class="tok-com">' + escapeHtml(code.slice(i, j)) + '</span>'; i = j; continue;
    }
    // HTML 注释
    if (htmlMode && c === '<' && code.slice(i, i + 4) === '<!--') {
      let j = code.indexOf('-->', i); j = j < 0 ? n : j + 3;
      out += '<span class="tok-com">' + escapeHtml(code.slice(i, j)) + '</span>'; i = j; continue;
    }
    // 块注释
    if (c === '/' && code[i + 1] === '*') {
      let j = code.indexOf('*/', i + 2); j = j < 0 ? n : j + 2;
      out += '<span class="tok-com">' + escapeHtml(code.slice(i, j)) + '</span>'; i = j; continue;
    }
    // 字符串
    if (c === '"' || c === "'" || c === '`') {
      const q = c; let j = i + 1;
      while (j < n) { if (code[j] === '\\') { j += 2; continue; } if (code[j] === q) { j++; break; } j++; }
      out += '<span class="tok-str">' + escapeHtml(code.slice(i, j)) + '</span>'; i = j; continue;
    }
    // 数字
    if (/[0-9]/.test(c) || (c === '.' && /[0-9]/.test(code[i + 1] || ''))) {
      let j = i; while (j < n && /[0-9a-fA-FxX._oObB]/.test(code[j])) j++;
      out += '<span class="tok-num">' + escapeHtml(code.slice(i, j)) + '</span>'; i = j; continue;
    }
    // 标识符 / 关键字 / 函数
    if (/[A-Za-z_$]/.test(c)) {
      let j = i; while (j < n && /[A-Za-z0-9_$]/.test(code[j])) j++;
      const word = code.slice(i, j);
      if (kw.has(word)) out += '<span class="tok-kw">' + escapeHtml(word) + '</span>';
      else if (code[j] === '(') out += '<span class="tok-fn">' + escapeHtml(word) + '</span>';
      else out += escapeHtml(word);
      i = j; continue;
    }
    out += escapeHtml(c); i++;
  }
  return out;
}
// diff 行着色（工具输出 edit_file 的 unified diff）
function highlightDiff(text) {
  const out = [];
  for (const ln of text.split('\n')) {
    if (ln.startsWith('+') && !ln.startsWith('+++')) out.push('<span class="diff-line add">' + escapeHtml(ln) + '</span>');
    else if (ln.startsWith('-') && !ln.startsWith('---')) out.push('<span class="diff-line del">' + escapeHtml(ln) + '</span>');
    else if (ln.startsWith('@@')) out.push('<span class="diff-line" style="color:var(--accent)">' + escapeHtml(ln) + '</span>');
    else out.push('<span class="diff-line ctx">' + escapeHtml(ln) + '</span>');
  }
  return out.join('');
}
// 工具输出渲染：diff → 着色；含代码块 → Markdown（代码块语法高亮）；否则纯文本转义
function renderToolOutput(text) {
  if (!text) return '';
  if (/^(---|\+\+\+|diff --git)/.test(text) || /\n(---|\+\+\+) /.test(text)) return highlightDiff(text);
  if (text.indexOf('```') >= 0) return mdToHtml(text);
  return escapeHtml(text);
}

// ── Markdown 渲染（手搓、XSS 安全：先转义再结构化）──
function mdToHtml(src) {
  if (!src) return '';
  const lines = src.split('\n');
  const out = [];
  let paragraph = [];
  let listType = null;   // 'ul' | 'ol' | null
  let quote = false;

  function inline(s) {
    s = escapeHtml(s);
    s = s.replace(/`([^`]+)`/g, '<code class="md-inline">$1</code>');
    s = s.replace(/\[([^\]]+)\]\((https?:\/\/[^)\s]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>');
    s = s.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    s = s.replace(/(^|[^*\w])\*([^*\n]+)\*(?!\*)/g, '$1<em>$2</em>');
    return s;
  }
  function flushParagraph() {
    if (paragraph.length) { out.push('<p>' + paragraph.map(inline).join('<br>') + '</p>'); paragraph = []; }
  }
  function flushList() {
    if (listType) { out.push('</' + listType + '>'); listType = null; }
  }
  function flushQuote() {
    if (quote) { out.push('</blockquote>'); quote = false; }
  }

  let i = 0;
  while (i < lines.length) {
    const line = lines[i];

    // 围栏代码块
    if (/^```/.test(line)) {
      flushParagraph(); flushList(); flushQuote();
      const lang = line.slice(3).trim();
      const code = [];
      i++;
      while (i < lines.length && !/^```/.test(lines[i])) { code.push(lines[i]); i++; }
      if (i < lines.length) i++; // 跳过结束 ```
      out.push('<pre class="md-code"><code' + (lang ? ' class="lang-' + escapeHtml(lang) + '"' : '') + '>' + highlightCode(code.join('\n'), lang) + '</code></pre>');
      continue;
    }

    // 表格（分隔行含 -，且上一行与分隔行都含 |）
    if (line.includes('|') && i + 1 < lines.length && /^\s*\|?[\s:|-]+\|[\s:|-]*$/.test(lines[i + 1]) && lines[i + 1].includes('-')) {
      flushParagraph(); flushList(); flushQuote();
      const headers = splitRow(line);
      i += 2; // 跳过表头与分隔行
      const rows = [];
      while (i < lines.length && lines[i].includes('|')) { rows.push(splitRow(lines[i])); i++; }
      let t = '<table class="md-table"><thead><tr>' + headers.map(h => '<th>' + inline(h) + '</th>').join('') + '</tr></thead><tbody>';
      t += rows.map(r => '<tr>' + r.map(c => '<td>' + inline(c) + '</td>').join('') + '</tr>').join('');
      t += '</tbody></table>';
      out.push(t);
      continue;
    }

    // 水平线
    if (/^\s*(-{3,}|\*{3,}|_{3,})\s*$/.test(line)) {
      flushParagraph(); flushList(); flushQuote();
      out.push('<hr>');
      i++;
      continue;
    }

    // 标题
    const h = /^(#{1,6})\s+(.*)$/.exec(line);
    if (h) {
      flushParagraph(); flushList(); flushQuote();
      const lv = h[1].length;
      out.push('<h' + lv + '>' + inline(h[2]) + '</h' + lv + '>');
      i++;
      continue;
    }

    // 引用
    const q = /^>\s?(.*)$/.exec(line);
    if (q) {
      flushParagraph(); flushList();
      if (!quote) { out.push('<blockquote>'); quote = true; }
      out.push('<p>' + inline(q[1]) + '</p>');
      i++;
      continue;
    }

    // 无序列表
    const ul = /^\s*[-*+]\s+(.*)$/.exec(line);
    if (ul) {
      flushParagraph(); flushQuote();
      if (listType !== 'ul') { flushList(); out.push('<ul>'); listType = 'ul'; }
      out.push('<li>' + inline(ul[1]) + '</li>');
      i++;
      continue;
    }

    // 有序列表
    const ol = /^\s*\d+[.)]\s+(.*)$/.exec(line);
    if (ol) {
      flushParagraph(); flushQuote();
      if (listType !== 'ol') { flushList(); out.push('<ol>'); listType = 'ol'; }
      out.push('<li>' + inline(ol[1]) + '</li>');
      i++;
      continue;
    }

    // 空行 → 段落/列表/引用收尾
    if (/^\s*$/.test(line)) {
      flushParagraph(); flushList(); flushQuote();
      i++;
      continue;
    }

    // 普通文本行 → 段落
    flushList(); flushQuote();
    paragraph.push(line);
    i++;
  }
  flushParagraph(); flushList(); flushQuote();
  return out.join('\n');
}

function splitRow(line) {
  let s = line.trim();
  if (s.startsWith('|')) s = s.slice(1);
  if (s.endsWith('|')) s = s.slice(0, -1);
  return s.split('|').map(c => c.trim());
}

// ── SSE ──
const es = new EventSource('/events');
es.addEventListener('token', e => { setBusy(true); handleToken(JSON.parse(e.data)); });
es.addEventListener('tool', e => { setBusy(true); endReasoning(); finalizeAssistant(); endAssistantStream(); const d = JSON.parse(e.data); addTool(d.name, d.args); });
es.addEventListener('tool_output', e => { ensureToolOutput().textContent += JSON.parse(e.data); scroll(); });
es.addEventListener('done', () => { setBusy(false); endReasoning(); finalizeAssistant(); endAssistantStream(); endToolOutput(); fetchPanel(); });
es.addEventListener('interrupted', () => { setBusy(false); endReasoning(); finalizeAssistant(); endAssistantStream(); endToolOutput(); addMsg('system', '⚠ 已中断'); fetchPanel(); });
es.addEventListener('failed', e => { setBusy(false); endReasoning(); finalizeAssistant(); endAssistantStream(); endToolOutput(); addMsg('system', '✘ ' + JSON.parse(e.data)); fetchPanel(); });
es.addEventListener('history', e => {
  const list = JSON.parse(e.data);
  if (messages.children.length === 0)
    list.forEach(m => addMsg(m.role === 'user' ? 'user' : 'assistant', m.content));
});
es.addEventListener('state', e => {
  const state = JSON.parse(e.data);
  currentProvider = state.provider;
  hasKey = state.hasKey;
  applyPermMode(state.permMode);
  renderSlots(state);
  if (state.model && state.model !== currentModelId) { currentModelId = state.model; updateModelBtn(); }
  renderModelBar(state);
  fetchPanel();
});
es.addEventListener('sessions', () => fetchSessions());
es.addEventListener('ask', e => showAsk(JSON.parse(e.data)));

// ── 初始化 ──
applyTheme(localStorage.getItem('waycoder-theme') || 'dark');
fetch('/state').then(r => r.json()).then(state => {
  currentProvider = state.provider;
  hasKey = state.hasKey;
  applyPermMode(state.permMode);
  renderSlots(state);
});
fetch('/models').then(r => r.json()).then(models =>
  fetch('/state').then(r => r.json()).then(state => renderModels(models, state)));
fetchSessions();
fetchPanel();
setInterval(fetchPanel, 2000);
</script>
</body>
</html>
""";
}

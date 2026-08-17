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
#chat-col { display:flex; flex-direction:column; min-width:0; min-height:0; position:relative; }

.panel-head { padding:11px 14px 7px; font-size:12px; font-weight:700; color:var(--dim); text-transform:uppercase; letter-spacing:.5px; }
#slot-list { display:grid; grid-template-columns:repeat(5,1fr); gap:5px; padding:2px 12px 8px; }
.slot { height:30px; border-radius:8px; border:1px solid var(--border); background:var(--panel2); color:var(--dim); font-size:11px; cursor:pointer; display:flex; align-items:center; justify-content:center; transition:all .15s; }
.slot:hover { border-color:var(--accent); color:var(--text); }
.slot.active { background:var(--accent); border-color:var(--accent); color:#fff; font-weight:700; }
.slot.has { color:var(--text); border-color:var(--dim); }
.session-actions { display:flex; gap:6px; margin:8px 12px; }
.session-actions .btn { flex:1; }

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
.shell-output { align-self:stretch; background:var(--panel2); border:1px solid var(--border); border-radius:12px; padding:10px 14px; font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace; font-size:13px; line-height:1.5; white-space:pre-wrap; word-break:break-word; color:var(--text); }

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
.msg .md-table { border-collapse:collapse; margin:8px 0; font-size:12.5px; max-width:100%; }
.msg .md-table th,.msg .md-table td { border:1px solid var(--border); padding:5px 10px; text-align:left; white-space:normal; }
.msg .md-table th { background:var(--panel2); font-weight:700; }
#input-bar { display:flex; gap:8px; padding:11px 14px 6px; border-top:1px solid var(--border); background:var(--panel); align-items:flex-end; }
#input { flex:1; resize:none; background:var(--bg); color:var(--text); border:1px solid var(--border); border-radius:14px; padding:11px 14px; font:inherit; min-height:64px; max-height:240px; outline:none; line-height:1.5; }
#input:focus { border-color:var(--accent); }
#input::placeholder { color:var(--dim); opacity:1; }
#send { width:46px; height:46px; border-radius:50%; font-size:19px; padding:0; display:flex; align-items:center; justify-content:center; background:var(--accent); color:#fff; border:none; cursor:pointer; flex-shrink:0; transition:background .15s; }
#send svg { width:21px; height:21px; fill:currentColor; display:block; }
#send:hover { filter:brightness(1.12); }
#send.stop { background:var(--danger); color:#ff9a9a; }
#input-bar { position:relative; }
#suggest-box { position:absolute; bottom:100%; left:14px; right:14px; margin-bottom:6px; background:var(--panel); border:1px solid var(--border); border-radius:12px; box-shadow:var(--shadow); max-height:280px; overflow-y:auto; z-index:50; display:none; }
#suggest-box.open { display:block; }
.suggest-item { padding:8px 12px; font-size:13px; cursor:pointer; display:flex; align-items:center; gap:8px; }
.suggest-item:hover, .suggest-item.active { background:var(--panel2); }
.suggest-item .si-icon { flex-shrink:0; width:20px; text-align:center; }
.suggest-item .si-label { flex:1; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
.suggest-item .si-desc { font-size:11px; color:var(--dim); flex-shrink:0; }
#model-bar { display:flex; gap:8px; align-items:center; padding:4px 14px 11px; background:var(--panel); flex-wrap:wrap; }
#model-bar select { height:28px; border-radius:8px; border:1px solid var(--border); background:var(--panel2); color:var(--text); font:inherit; font-size:12px; padding:0 7px; cursor:pointer; outline:none; max-width:160px; }
#model-bar .model-pick-btn { height:28px; padding:0 10px; font-size:12px; border-radius:8px; }
#model-bar .model-pick-btn b { color:var(--text); }
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
.model-card { width:860px; max-width:94vw; display:flex; flex-direction:column; overflow:hidden; }
.model-title { display:flex; align-items:center; justify-content:space-between; margin-bottom:6px; }
.model-title h2 { flex-shrink:0; margin-bottom:0; }
#model-close { font-size:22px; padding:0 10px; line-height:1; }
#model-list { flex:1; min-height:0; overflow-y:auto; padding:0 6px; }
#model-search { margin-bottom:0; }
.model-head { display:grid; grid-template-columns:28px 1fr 96px 58px 68px 36px 36px; gap:6px; padding:7px 12px 5px; font-size:11px; color:var(--dim); font-weight:700; border-bottom:1px solid var(--border); }
.model-head span.r { text-align:right; }
.model-group .gname { font-size:12px; color:var(--dim); font-weight:700; margin:10px 6px 5px; text-transform:uppercase; letter-spacing:.4px; }
.model-item { display:grid; grid-template-columns:28px 1fr 96px 58px 68px 36px 36px; gap:6px; align-items:center; padding:7px 6px; border-radius:9px; border:1px solid transparent; cursor:pointer; }
.model-item:hover { background:var(--panel2); border-color:var(--border); }
.model-item.selected { border-color:var(--accent); background:var(--panel); }
.model-item .key { text-align:center; }
.model-item .name { font-weight:600; overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
.model-item .prov { font-size:12px; color:var(--dim); overflow:hidden; text-overflow:ellipsis; white-space:nowrap; }
.model-item .ctx { font-size:12px; color:var(--dim); text-align:right; }
.model-item .price { font-size:12px; color:var(--dim); text-align:right; }
.model-item .chk { text-align:center; color:var(--accent); font-weight:700; }
.model-foot { display:flex; align-items:center; gap:8px; padding-top:12px; }
.model-foot .btn { flex-shrink:0; white-space:nowrap; }
.model-foot .spacer { flex:1; }

/* ── 上下文压缩指示条（动画 spinner + 进度条，压缩完成淡出消失） ── */
#compress-indicator {
  position:absolute; top:10px; left:50%; transform:translateX(-50%) translateY(-6px);
  display:flex; align-items:center; gap:9px; z-index:60; pointer-events:none;
  padding:8px 16px; border-radius:22px; font-size:12.5px; color:var(--text);
  background:var(--panel2); border:1px solid var(--accent); box-shadow:var(--shadow);
  opacity:0; transition:opacity .28s ease, transform .28s ease;
}
#compress-indicator.show { opacity:1; transform:translateX(-50%) translateY(0); }
#compress-indicator .cspin {
  width:14px; height:14px; flex-shrink:0; border-radius:50%;
  border:2px solid var(--border); border-top-color:var(--accent);
  animation:cspin .7s linear infinite;
}
#compress-indicator .cbar { width:110px; height:5px; border-radius:3px; background:var(--border); overflow:hidden; flex-shrink:0; }
#compress-indicator .cbar i { display:block; height:100%; background:var(--accent); border-radius:3px; transition:width .35s ease; }
#compress-indicator .cpct { font-size:11px; color:var(--dim); min-width:34px; text-align:right; font-variant-numeric:tabular-nums; }
@keyframes cspin { to { transform:rotate(360deg); } }
</style>
</head>
<body>
<header>
  <div class="logo">🤖 道码 Way<span>Coder</span></div>
  <div class="agent-label" id="agent-label">智能体1</div>
  <div class="spacer"></div>
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
    <div class="session-actions">
      <button class="btn" id="new-session">＋ 新建会话</button>
      <button class="btn danger" id="clear-sessions" title="清空所有会话记录">🗑 清空</button>
    </div>
    <div id="session-list"><div class="empty">加载中…</div></div>
  </aside>

  <main id="chat-col">
    <div id="compress-indicator">
      <span class="cspin"></span>
      <span id="compress-label">压缩中…</span>
      <span class="cbar"><i id="compress-fill"></i></span>
      <span class="cpct" id="compress-pct"></span>
    </div>
    <div id="messages"></div>
    <div id="input-bar">
      <div id="suggest-box"></div>
      <button class="btn ghost" id="attach-btn" title="上传图片 / 音频">📎</button>
      <input type="file" id="file-input" accept="image/*,audio/*" style="display:none">
      <textarea id="input" placeholder="输入消息，Enter 发送，Ctrl+Enter 换行" rows="3"></textarea>
      <button class="btn" id="send" title="发送"><svg viewBox="0 0 24 24"><path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/></svg></button>
    </div>
    <div id="model-bar">
      <button class="btn model-pick-btn" id="big-model-btn" title="选择大模型（复杂任务）">🤖 大模型: <b id="big-model-label">…</b></button>
      <button class="btn model-pick-btn" id="small-model-btn" title="选择小模型（简单 / 压缩）">🔧 小模型: <b id="small-model-label">…</b></button>
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
    <div class="model-title">
      <h2 id="model-title">🧠 选择模型</h2>
      <button class="btn ghost" id="model-close" title="关闭">×</button>
    </div>
    <input id="model-search" type="text" placeholder="搜索模型名称 / 供应商…">
    <div class="model-head">
      <span>🔑</span><span>模型</span><span>厂商</span><span class="r">窗口</span><span class="r">价格</span><span class="r">大</span><span class="r">小</span>
    </div>
    <div id="model-list"></div>
    <div class="model-foot">
      <button class="btn" id="model-scan-btn" title="测试各服务商端点连通性">📡 扫描</button>
      <button class="btn" id="model-import-btn" title="导入其他软件的模型与 API Key">📥 自动导入</button>
      <button class="btn" id="model-set-key-btn" title="为当前选中的模型设置 API Key">🔑 设置 key</button>
      <button class="btn" id="model-clear-key-btn" title="清除当前选中模型的 API Key">🗑 清除 key</button>
      <span id="model-scan-status" style="font-size:12px;color:var(--dim);"></span>
      <span class="spacer"></span>
      <button class="btn ghost" id="model-cancel">取消</button>
      <button class="btn" id="model-save-btn" title="保存选中的模型为默认配置（不中断当前任务）">💾 保存</button>
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

// 本页面唯一客户端标识：开始/停止只作用于当前页面绑定的槽位
const clientId = 'c' + Date.now().toString(36) + Math.random().toString(36).slice(2, 10);
const cq = (p) => p + (p.indexOf('?') >= 0 ? '&' : '?') + 'client=' + clientId;

// ── 流指针（滚动 bug 修复：assistant 文本流 与 工具输出流 分离）──
let assistantStreamEl = null;
let toolOutputEl = null;
let reasoningEl = null;
let currentProvider = '';
let hasKey = false;
let isBusy = false;
const PAPER_PLANE = '<svg viewBox="0 0 24 24"><path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/></svg>';
function setBusy(b) {
  isBusy = b;
  const btn = document.getElementById('send');
  if (b) { btn.innerHTML = '⏹'; btn.classList.add('stop'); btn.title = '停止'; }
  else { btn.innerHTML = PAPER_PLANE; btn.classList.remove('stop'); btn.title = '发送'; }
}

function showCompress(d) {
  const el = document.getElementById('compress-indicator');
  if (!el) return;
  if (d.done) { el.classList.remove('show'); return; } // 压缩完成 → 淡出消失
  const label = document.getElementById('compress-label');
  const fill = document.getElementById('compress-fill');
  const pctEl = document.getElementById('compress-pct');
  if (label) label.textContent = '🔄 ' + (d.label || '压缩中…');
  const pct = Math.max(0, Math.min(100, d.percent || 0));
  if (fill) fill.style.width = pct + '%';
  if (pctEl) pctEl.textContent = Math.round(pct) + '%';
  el.classList.add('show');
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
let currentSlot = 0;          // 本页面当前绑定的槽位
const slotDrafts = {};        // 槽位索引 → 未发送的输入草稿（切换槽位时各自保留）

function renderSlots(state) {
  currentSlot = state.activeSlot;
  document.getElementById('agent-label').textContent = '智能体' + (state.activeSlot + 1);
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
  if (i === currentSlot) return; // 已在该槽位，无需重复切换（避免清空输入框）
  setBusy(false); // 切换槽位：本页面不再处于旧槽位的流式接收态，重置发送/停止按钮
  slotDrafts[currentSlot] = input.value; // 保存当前槽位未发送的草稿
  currentSlot = i;
  input.value = slotDrafts[i] || '';      // 恢复目标槽位的草稿
  autoResizeInput();
  fetch(cq('/slot'), { method: 'POST', body: JSON.stringify({ slot: i }) })
    .then(r => r.json())
    .then(list => { clearMessages(); list.forEach(m => addMsg(m.role === 'user' ? 'user' : 'assistant', m.content)); })
    .then(fetchPanel);
}

// ── 历史会话（左栏）──
function fetchSessions() {
  fetch(cq('/sessions')).then(r => r.json()).then(renderSessions).catch(() => {});
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
  fetch(cq('/sessions/load'), { method: 'POST', body: JSON.stringify({ id: id }) })
    .then(r => r.json())
    .then(res => { if (res && res.ok === false) { alert(res.error || '加载失败'); return; } })
    .catch(() => {});
}
function deleteSession(id) {
  if (!confirm('删除会话 ' + id + ' ?')) return;
  fetch(cq('/sessions/delete'), { method: 'POST', body: JSON.stringify({ id: id }) }).then(fetchSessions).catch(() => {});
}
function renameSession(id) {
  const newId = prompt('重命名会话（ID）:', id);
  if (!newId || newId === id) return;
  fetch(cq('/sessions/rename'), { method: 'POST', body: JSON.stringify({ id: id, newId: newId }) })
    .then(r => r.json())
    .then(res => { if (res && res.ok === false) alert(res.error || '重命名失败'); })
    .then(fetchSessions)
    .catch(() => {});
}
document.getElementById('new-session').onclick = () => {
  clearMessages();
  fetch(cq('/sessions/new'), { method: 'POST' }).catch(() => {});
};
document.getElementById('clear-sessions').onclick = () => {
  if (!confirm('确定清空所有会话记录？此操作不可恢复。')) return;
  fetch(cq('/sessions/clear'), { method: 'POST' })
    .then(r => r.json())
    .then(res => { if (res && res.ok) alert('已清空 ' + (res.deleted || 0) + ' 条会话记录'); })
    .then(fetchSessions)
    .catch(() => {});
};

// ── 右栏面板 ──
function fetchPanel() {
  if (document.hidden) return;
  fetch(cq('/panel')).then(r => r.json()).then(renderPanel).catch(() => {});
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
let pendingMode = '';   // 'large' | 'small' — 当前弹窗为谁选模型
let scanMap = {};       // providerId -> 'ok' | 'fail'
function renderModels(models, state) {
  modelMap = {};
  allModels = models;
  models.forEach(m => { modelMap[m.id] = m; });
  currentModelId = state.model || currentModelId;
  currentSmallModel = state.smallModel || currentSmallModel;
  renderModelBar(state);
}
function fetchModels() {
  fetch('/models').then(r => r.json()).then(models =>
    fetch(cq('/state')).then(r => r.json()).then(state => {
      renderModels(models, state);
      if (document.getElementById('model-modal').classList.contains('open'))
        renderModelList(document.getElementById('model-search').value);
    }));
}
// ── 模型状态栏（输入框下方：大模型 / 小模型 / 省钱模式 / 状态）──
const ECONOMY_OPTIONS = [['off', '省钱：关'], ['auto', '省钱：自动'], ['on', '省钱：开']];
function renderModelBar(state) {
  const eco = document.getElementById('economy-select');
  if (allModels.length && eco.options.length === 0) {
    ECONOMY_OPTIONS.forEach(([v, l]) => { const op = document.createElement('option'); op.value = v; op.textContent = l; eco.appendChild(op); });
  }
  const big = modelMap[state.model || currentModelId];
  const small = modelMap[state.smallModel || currentSmallModel];
  document.getElementById('big-model-label').textContent = big ? big.name : (state.model || currentModelId || '选择');
  document.getElementById('small-model-label').textContent = small ? small.name : (state.smallModel || currentSmallModel || '选择');
  if (state.economy) eco.value = state.economy;
  const m = modelMap[currentModelId];
  const providerName = state.providerName || state.provider || '';
  const hk = (state.hasKey !== undefined) ? state.hasKey : hasKey;
  document.getElementById('model-status').textContent =
    (m ? m.name : (currentModelId || '')) + (providerName ? ' · ' + providerName : '') + (hk ? '' : ' · ⚠ 无 key');
}
document.getElementById('big-model-btn').onclick = () => openModelModal('large');
document.getElementById('small-model-btn').onclick = () => openModelModal('small');
document.getElementById('economy-select').onchange = e =>
  fetch('/settings', { method: 'POST', body: JSON.stringify({ key: 'EconomyMode', value: e.target.value }) }).catch(() => {});
function formatContext(ctx) {
  if (!ctx || ctx <= 0) return '-';
  if (ctx >= 1000000) return (Math.round(ctx / 100000) / 10) + 'M';
  return Math.round(ctx / 1000) + 'K';
}
function formatPrice(m) {
  const p = m.inputPrice;
  if (!p || p <= 0) return 'Free';
  if (p < 0.01) return '<$0.01';
  return '$' + p.toFixed(2);
}
function openModelModal(mode) {
  pendingMode = mode;
  const isSmall = mode === 'small';
  selectedModelId = isSmall ? currentSmallModel : currentModelId;
  document.getElementById('model-title').textContent = isSmall ? '🔧 选择小模型' : '🤖 选择大模型';
  document.getElementById('model-search').value = '';
  document.getElementById('model-scan-status').textContent = '';
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
    const st = scanMap[pid];
    gn.textContent = pid + (st === 'ok' ? ' ✅' : (st === 'fail' ? ' ❌' : ''));
    g.appendChild(gn);
    byProvider[pid].forEach(m => {
      const item = document.createElement('div');
      item.className = 'model-item' + (m.id === selectedModelId ? ' selected' : '');
      const key = document.createElement('span');
      key.className = 'key';
      key.textContent = m.hasKey ? '🔑' : '';
      const name = document.createElement('span');
      name.className = 'name';
      name.textContent = m.name;
      name.title = m.id;
      const prov = document.createElement('span');
      prov.className = 'prov';
      prov.textContent = m.provider || pid;
      const ctx = document.createElement('span');
      ctx.className = 'ctx';
      ctx.textContent = formatContext(m.context);
      const price = document.createElement('span');
      price.className = 'price';
      price.textContent = formatPrice(m);
      const large = document.createElement('span');
      large.className = 'chk';
      large.textContent = m.id === currentModelId ? '✓' : '';
      const small = document.createElement('span');
      small.className = 'chk';
      small.textContent = m.id === currentSmallModel ? '✓' : '';
      [key, name, prov, ctx, price, large, small].forEach(c => item.appendChild(c));
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
  const isSmall = pendingMode === 'small';
  if (!m.hasKey && m.providerId !== 'local' && m.providerId !== 'custom') {
    pendingModelId = isSmall ? '' : m.id;
    pendingSmallModelId = isSmall ? m.id : '';
    currentProvider = m.providerId;
    document.getElementById('key-hint').textContent = '为 ' + m.providerId + ' 输入 API Key（保存后切换到' + (isSmall ? '小模型' : '大模型') + ' ' + m.name + '）。';
    document.getElementById('key-input').value = '';
    document.getElementById('model-modal').classList.remove('open');
    keyModal.classList.add('open');
    return;
  }
  const body = isSmall ? { key: 'SmallModel', value: m.id } : { modelId: m.id };
  const url = isSmall ? '/settings' : cq('/model');
  fetch(url, { method: 'POST', body: JSON.stringify(body) })
    .then(() => {
      if (isSmall) currentSmallModel = m.id; else currentModelId = m.id;
      selectedModelId = ''; pendingMode = '';
      renderModelBar({});
      document.getElementById('model-modal').classList.remove('open');
    })
    .catch(() => {});
}
function confirmModel() {
  const m = modelMap[selectedModelId];
  if (m) chooseModel(m);
}
function closeModelModal() { selectedModelId = ''; pendingMode = ''; document.getElementById('model-modal').classList.remove('open'); }
document.getElementById('model-search').oninput = e => renderModelList(e.target.value);
document.getElementById('model-close').onclick = closeModelModal;
document.getElementById('model-cancel').onclick = closeModelModal;
document.getElementById('model-confirm').onclick = confirmModel;
// 点击遮罩（卡片外）关闭模型弹窗
document.getElementById('model-modal').addEventListener('click', e => {
  if (e.target === e.currentTarget) closeModelModal();
});

// ── 扫描（测试各服务商端点连通性）──
document.getElementById('model-scan-btn').onclick = () => {
  const btn = document.getElementById('model-scan-btn');
  const status = document.getElementById('model-scan-status');
  btn.disabled = true;
  status.textContent = '扫描中…';
  fetch('/models/scan', { method: 'POST' }).then(r => r.json()).then(res => {
    btn.disabled = false;
    scanMap = {};
    let ok = 0, fail = 0;
    (res.results || []).forEach(p => {
      scanMap[p.providerId] = p.ok ? 'ok' : 'fail';
      if (p.ok) ok++; else fail++;
    });
    status.textContent = ok + ' 连通 / ' + fail + ' 不通';
    renderModelList(document.getElementById('model-search').value);
  }).catch(() => { btn.disabled = false; status.textContent = '扫描失败'; });
};

// ── 自动导入（其他软件的模型列表与 API Key）──
document.getElementById('model-import-btn').onclick = () => {
  const status = document.getElementById('model-scan-status');
  status.textContent = '导入中…';
  fetch('/models/import', { method: 'POST' }).then(r => r.json()).then(res => {
    status.textContent = '';
    const keys = res.keys || [];
    const keySummary = keys.length ? ('导入 Key: ' + keys.map(k => k.providerId + '(' + k.source + ')').join(', ')) : '未发现新 Key';
    alert('模型导入：' + (res.modelReport || '完成') + '\n' + keySummary);
    fetchModels();
  }).catch(() => { status.textContent = '导入失败'; });
};

// ── 设置 / 清除 key（对当前选中的模型所属供应商）──
document.getElementById('model-set-key-btn').onclick = () => {
  const m = modelMap[selectedModelId];
  const status = document.getElementById('model-scan-status');
  if (!m) { status.textContent = '请先选择一个模型'; return; }
  if (m.providerId === 'local' || m.providerId === 'custom') { status.textContent = '本地模型无需 API Key'; return; }
  pendingModelId = ''; pendingSmallModelId = '';
  currentProvider = m.providerId;
  document.getElementById('key-hint').textContent = '为 ' + m.providerId + ' 设置 API Key（保存后该供应商所有模型可用）。';
  document.getElementById('key-input').value = '';
  keyModal.classList.add('open');
};
document.getElementById('model-clear-key-btn').onclick = () => {
  const m = modelMap[selectedModelId];
  const status = document.getElementById('model-scan-status');
  if (!m) { status.textContent = '请先选择一个模型'; return; }
  if (m.providerId === 'local' || m.providerId === 'custom') { status.textContent = '本地模型无需 API Key'; return; }
  fetch('/key', { method: 'POST', body: JSON.stringify({ providerId: m.providerId, apiKey: '' }) })
    .then(() => { status.textContent = '已清除 ' + m.providerId + ' 的 Key'; fetchModels(); })
    .catch(() => { status.textContent = '清除失败'; });
};

// ── 保存（持久化选中模型，不中断当前会话）──
document.getElementById('model-save-btn').onclick = () => {
  const m = modelMap[selectedModelId];
  const status = document.getElementById('model-scan-status');
  if (!m) { status.textContent = '请先选择一个模型'; return; }
  const isSmall = pendingMode === 'small';
  const body = isSmall ? { key: 'SmallModel', value: m.id } : { modelId: m.id };
  const url = isSmall ? '/settings' : '/model/save';
  fetch(url, { method: 'POST', body: JSON.stringify(body) })
    .then(() => {
      if (isSmall) currentSmallModel = m.id; else currentModelId = m.id;
      status.textContent = '已保存 ' + (isSmall ? '小模型' : '大模型') + ' ' + m.name + '（不中断当前任务，新会话/重启生效）';
      renderModelBar({});
      renderModelList(document.getElementById('model-search').value);
    })
    .catch(() => { status.textContent = '保存失败'; });
};

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
        fetch(cq('/model'), { method: 'POST', body: JSON.stringify({ modelId: id }) })
          .then(() => { currentModelId = id; renderModelBar({}); })
          .catch(() => {});
      }
      if (pendingSmallModelId) {
        const id = pendingSmallModelId; pendingSmallModelId = '';
        fetch('/settings', { method: 'POST', body: JSON.stringify({ key: 'SmallModel', value: id }) })
          .then(() => { currentSmallModel = id; renderModelBar({}); })
          .catch(() => {});
      }
      fetchModels();
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
// Shell 裸 ANSI 配色样例（/test ansi 用）：模拟 Shell 命令产生的原始 tty 转义序列，
// 验证 Web 端 ansiToHtml 解码器（CLI/TUI 直接透传终端，Web 需转 HTML）。
const ANSI_SAMPLE =
  '\x1b[1;32m✅ 成功（粗体绿）\x1b[0m  ' +
  '\x1b[31m✘ 失败（红）\x1b[0m  ' +
  '\x1b[33m⚠ 警告（黄）\x1b[0m  ' +
  '\x1b[34m信息（蓝）\x1b[0m  ' +
  '\x1b[35m紫色\x1b[0m  ' +
  '\x1b[36m青色\x1b[0m  ' +
  '\x1b[90m暗灰\x1b[0m\n' +
  '\x1b[41m 红底 \x1b[0m ' +
  '\x1b[42m 绿底 \x1b[0m ' +
  '\x1b[43m 黄底 \x1b[0m ' +
  '\x1b[44m 蓝底 \x1b[0m ' +
  '\x1b[45m 紫底 \x1b[0m\n' +
  '\x1b[1m粗体\x1b[0m ' +
  '\x1b[4m下划线\x1b[0m ' +
  '\x1b[2m暗淡\x1b[0m ' +
  '\x1b[1;36m粗体青色\x1b[0m';
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
    openModelModal('large');
    return true;
  }
  if (lower === '/test ansi' || lower === '/test tty') {
    addShellOutput(ANSI_SAMPLE);
    return true;
  }
  return false;
}
// ── 特殊符号前缀（/命令、!Shell、#文件引用）+ 全角/半角兼容 + 提示框 ──
const suggestBox = document.getElementById('suggest-box');
const SLASH_COMMANDS = [
  ['/help', '显示帮助'],
  ['/perm', '切换权限模式'],
  ['/model', '打开模型选择'],
  ['/theme', '切换明暗主题'],
  ['/settings', '打开设置'],
  ['/reset', '清空当前会话'],
  ['/session', '会话管理'],
  ['/tokens', 'Token 统计'],
  ['/stats', '会话统计'],
  ['/recent', '本次修改的文件'],
  ['/mcp', 'MCP 服务器状态'],
  ['/todo', '任务列表'],
  ['/interrupt', '中断当前任务'],
  ['/test', '渲染测试（markdown/table/markup/ansi）'],
];
let suggestItems = [];   // [{label, desc, icon, fill}]
let suggestActive = -1;

// 全角 → 半角：仅处理开头的前缀符号，避免破坏正文中的全角标点
function normalizeFullWidth(s) {
  if (s.startsWith('／')) return '/' + s.slice(1);
  if (s.startsWith('！')) return '!' + s.slice(1);
  if (s.startsWith('＃')) return '#' + s.slice(1);
  return s;
}

function hideSuggest() {
  suggestBox.classList.remove('open');
  suggestBox.innerHTML = '';
  suggestItems = [];
  suggestActive = -1;
}

function renderSuggest() {
  if (!suggestItems.length) { hideSuggest(); return; }
  suggestBox.innerHTML = '';
  suggestItems.forEach((it, i) => {
    const el = document.createElement('div');
    el.className = 'suggest-item' + (i === suggestActive ? ' active' : '');
    el.innerHTML = '<span class="si-icon">' + escapeHtml(it.icon) + '</span>' +
      '<span class="si-label">' + escapeHtml(it.label) + '</span>' +
      '<span class="si-desc">' + escapeHtml(it.desc) + '</span>';
    el.onmousedown = e => { e.preventDefault(); acceptSuggest(i); };
    el.onmouseenter = () => { suggestActive = i; renderSuggest(); };
    suggestBox.appendChild(el);
  });
  suggestBox.classList.add('open');
}

function acceptSuggest(i) {
  const it = suggestItems[i];
  if (!it) return;
  input.value = it.fill;
  hideSuggest();
  input.focus();
  autoResizeInput();
}

function autoResizeInput() {
  input.style.height = 'auto';
  input.style.height = Math.min(input.scrollHeight, 240) + 'px';
}

function updateSuggest() {
  const raw = normalizeFullWidth(input.value);
  const trimmed = raw.trimStart();
  if (trimmed.startsWith('/')) {
    const q = trimmed.slice(1).toLowerCase();
    suggestItems = SLASH_COMMANDS
      .filter(c => !q || c[0].slice(1).startsWith(q) || c[0].slice(1).indexOf(q) >= 0)
      .map(c => ({ label: c[0], desc: c[1], icon: '⚡', fill: c[0] + ' ' }));
    suggestActive = suggestItems.length ? 0 : -1;
    renderSuggest();
  } else if (trimmed.startsWith('!')) {
    const rest = trimmed.slice(1).trim();
    suggestItems = [{ label: rest ? ('执行：' + rest) : '执行 Shell 命令', desc: 'Shell', icon: '💻', fill: '!' + rest }];
    suggestActive = 0;
    renderSuggest();
  } else if (trimmed.startsWith('#')) {
    const q = trimmed.slice(1);
    hideSuggest();
    fetch('/filelist', { method: 'POST', body: JSON.stringify({ prefix: q }) })
      .then(r => r.json())
      .then(res => {
        if (trimmed !== normalizeFullWidth(input.value.trimStart())) return; // 已过期，丢弃
        const files = (res && res.files) || [];
        suggestItems = files.map(f => ({
          label: f.name,
          desc: f.isDir ? '目录' : '文件',
          icon: f.isDir ? '📁' : '📄',
          fill: '#' + f.path,
        }));
        suggestActive = suggestItems.length ? 0 : -1;
        renderSuggest();
      })
      .catch(() => hideSuggest());
  } else {
    hideSuggest();
  }
}

function addShellOutput(text) {
  const el = document.createElement('div');
  el.className = 'shell-output';
  el.innerHTML = ansiToHtml(text) || '(无输出)';
  messages.appendChild(el);
  scroll();
  return el;
}

function send() {
  if (isBusy) { fetch(cq('/interrupt'), { method: 'POST' }).catch(() => {}); return; }
  hideSuggest();
  const text = normalizeFullWidth(input.value.trim());
  if (!text) return;
  input.value = '';
  autoResizeInput();

  // 纯 UI 斜杠命令（操作 DOM，不进入聊天流）
  if (handleUiCommand(text)) return;

  addMsg('user', text);

  // !Shell 指令 → 直接执行并显示输出（对标 Claude Code `!`）
  if (text.startsWith('!') && text.length > 1) {
    const command = text.slice(1).trim();
    if (!command) return;
    setBusy(true);
    fetch(cq('/shell'), { method: 'POST', body: JSON.stringify({ command }) })
      .then(r => r.json())
      .then(res => {
        setBusy(false);
        addShellOutput(res && res.ok ? (res.output || '(无输出)') : ('❌ ' + (res && res.output || '执行失败')));
      })
      .catch(() => { addShellOutput('❌ Shell 执行失败'); setBusy(false); });
    return;
  }

  // #文件引用 → 读取文件注入上下文（对标 Claude Code `#`）
  if (text.startsWith('#') && text.length > 1) {
    const path = text.slice(1).trim();
    if (!path) return;
    setBusy(true);
    fetch(cq('/fileref'), { method: 'POST', body: JSON.stringify({ path }) })
      .then(r => r.json())
      .then(res => {
        setBusy(false);
        if (res && res.ok) addMsg('cmd', '📄 已引用 `' + path + '`（' + ((res.content || '').split('\n').length) + ' 行）');
        else addMsg('cmd', '❌ ' + ((res && res.content) || '读取失败'));
      })
      .catch(() => { addMsg('cmd', '❌ 文件读取失败'); setBusy(false); });
    return;
  }

  setBusy(true);

  // 斜杠命令 → 后端路由（未识别回退为普通 Agent 消息）
  if (text.startsWith('/') && text.length > 1) {
    fetch(cq('/command'), { method: 'POST', body: JSON.stringify({ input: text }) })
      .then(r => r.json())
      .then(res => {
        if (res && res.ok && res.handled) {
          addMsg('cmd', res.output || '');
          setBusy(false);
        } else {
          fetch(cq('/chat'), { method: 'POST', body: text }).catch(() => {});
        }
      })
      .catch(() => { fetch(cq('/chat'), { method: 'POST', body: text }).catch(() => {}); });
    return;
  }

  fetch(cq('/chat'), { method: 'POST', body: text }).catch(() => {});
}
document.getElementById('send').onclick = send;
input.addEventListener('keydown', e => {
  if (suggestBox.classList.contains('open')) {
    if (e.key === 'ArrowDown') { e.preventDefault(); suggestActive = (suggestActive + 1) % suggestItems.length; renderSuggest(); return; }
    if (e.key === 'ArrowUp') { e.preventDefault(); suggestActive = (suggestActive - 1 + suggestItems.length) % suggestItems.length; renderSuggest(); return; }
    if (e.key === 'Tab' || e.key === 'Enter') {
      e.preventDefault();
      if (suggestActive >= 0) acceptSuggest(suggestActive);
      return;
    }
    if (e.key === 'Escape') { e.preventDefault(); hideSuggest(); return; }
  }
  if (e.key === 'Enter' && !e.ctrlKey && !e.shiftKey) { e.preventDefault(); send(); }
});
input.addEventListener('input', () => { autoResizeInput(); updateSuggest(); });

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
  fetch(cq('/upload?kind=' + kind), {
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
          if (res.text) { addMsg('user', res.text); fetch(cq('/chat'), { method: 'POST', body: res.text }).catch(() => {}); }
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
// ANSI SGR 转义码 → 行内样式（模拟 tty 配色），文本先转义保证 XSS 安全
const ANSI_FG = { 30:'#8b949e',31:'#ff7b72',32:'#3fb950',33:'#d29922',34:'#58a6ff',35:'#bc8cff',36:'#39c5cf',37:'#c9d1d9',
                  90:'#6e7681',91:'#ffa198',92:'#56d364',93:'#e3b341',94:'#79c0ff',95:'#d2a8ff',96:'#56d4dd',97:'#f0f6fc' };
const ANSI_BG = { 40:'#161b22',41:'#6e2222',42:'#1a4d1a',43:'#5c4a1e',44:'#1f3d6e',45:'#4b2a6e',46:'#1c4d4d',47:'#4a4a4a',
                  100:'#30363d',101:'#8b3a3a',102:'#2a5d2a',103:'#6e5c2e',104:'#2e4d7e',105:'#5c3a8b',106:'#2e5c5c',107:'#5a5a5a' };
function ansiToHtml(text) {
  if (!text) return '';
  let fg = null, bg = null, bold = false, dim = false, underline = false;
  let i = 0, n = text.length, buf = '', out = '';
  function styleStr() {
    let s = '';
    if (bold) s += 'font-weight:700;';
    if (dim) s += 'opacity:.6;';
    if (underline) s += 'text-decoration:underline;';
    if (fg) s += 'color:' + fg + ';';
    if (bg) s += 'background:' + bg + ';';
    return s;
  }
  function flush() {
    if (!buf) return;
    const st = styleStr();
    out += st ? '<span style="' + st + '">' + escapeHtml(buf) + '</span>' : escapeHtml(buf);
    buf = '';
  }
  while (i < n) {
    if (text.charCodeAt(i) === 0x1b && text[i + 1] === '[') {
      // CSI 序列：找最终字节（m = SGR，其余如光标控制直接跳过）
      let j = i + 2;
      while (j < n && text.charCodeAt(j) < 0x40) j++;
      const final = j < n ? text[j] : null;
      if (final === 'm') {
        const codes = text.slice(i + 2, j).split(';').map(x => parseInt(x, 10) || 0);
        flush();
        for (const code of codes) {
          if (code === 0) { fg = null; bg = null; bold = false; dim = false; underline = false; }
          else if (code === 1) bold = true;
          else if (code === 2) dim = true;
          else if (code === 4) underline = true;
          else if (code === 22) { bold = false; dim = false; }
          else if (code === 24) underline = false;
          else if (code >= 30 && code <= 37) fg = ANSI_FG[code];
          else if (code >= 90 && code <= 97) fg = ANSI_FG[code];
          else if (code >= 40 && code <= 47) bg = ANSI_BG[code];
          else if (code >= 100 && code <= 107) bg = ANSI_BG[code - 60];
          else if (code === 39) fg = null;
          else if (code === 49) bg = null;
        }
        i = j + 1;
        continue;
      }
      i = j + 1; // 非 SGR：跳过整个 CSI
      continue;
    }
    buf += text[i];
    i++;
  }
  flush();
  return out;
}
// ── «» 中间格式 → HTML（对标后端 SpectreToAnsi：CLI/TUI 转 ANSI，Web 转 HTML span）──
// WayCoder 所有格式消息（text/markdown/code/…）统一用 «tag»…«/» 表达颜色/样式，
// 由各平台渲染器决定呈现：CLI/TUI → ANSI、Web → HTML、GUI → 富文本。这里只负责 Web。
// 颜色值与 ANSI_FG 对齐（同源 TuiColors），保证三端观感一致。
const MARKUP_STYLES = {
  'red': 'color:#ff7b72;', 'green': 'color:#3fb950;', 'yellow': 'color:#d29922;',
  'cyan': 'color:#39c5cf;', 'blue': 'color:#58a6ff;', 'magenta': 'color:#bc8cff;',
  'white': 'color:#c9d1d9;', 'orange3': 'color:#d29922;', 'grey': 'color:#6e7681;',
  'dim': 'opacity:.6;', 'bold': 'font-weight:700;',
  'underline': 'text-decoration:underline;', 'italic': 'font-style:italic;',
};
function markupToHtml(text) {
  if (!text) return '';
  let out = '';
  let i = 0;
  const n = text.length;
  const stack = [];   // 活跃样式串（合并成单个 span，避免标签顺序问题）
  let buf = '';
  function flush() {
    if (!buf) return;
    const st = stack.join('');
    out += st ? '<span style="' + st + '">' + escapeHtml(buf) + '</span>' : escapeHtml(buf);
    buf = '';
  }
  while (i < n) {
    const open = text.indexOf('«', i);
    if (open < 0) { buf += text.slice(i); break; }
    buf += text.slice(i, open);
    const close = text.indexOf('»', open);
    if (close < 0) { buf += text.slice(open); break; }
    const tag = text.slice(open + 1, close).trim();
    flush();
    if (tag === '/') {
      if (stack.length) stack.pop();
      else buf += '«/»';   // 无匹配开标签，原样保留结束符
    } else {
      // 复合标签如 «bold yellow» 按空格拆分，多个样式合并到单个 span
      const styles = [];
      for (const p of tag.split(/\s+/)) if (MARKUP_STYLES[p]) styles.push(MARKUP_STYLES[p]);
      if (styles.length) stack.push(styles.join(''));
      else buf += '«' + tag + '»';   // 未知标签原样保留
    }
    i = close + 1;
  }
  flush();
  return out;
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
  return markupToHtml(text);
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
    s = markupToHtml(s);
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
      // 分隔行对齐：:--- 左对齐 · ---: 右对齐 · :---: 居中 · --- 默认左
      const aligns = splitRow(lines[i + 1]).map(seg => {
        const l = seg.startsWith(':'), r = seg.endsWith(':');
        return (l && r) ? 'center' : (r ? 'right' : 'left');
      });
      i += 2; // 跳过表头与分隔行
      const rows = [];
      while (i < lines.length && lines[i].includes('|')) { rows.push(splitRow(lines[i])); i++; }
      const alignAttr = idx => (aligns[idx] && aligns[idx] !== 'left') ? ' style="text-align:' + aligns[idx] + '"' : '';
      let t = '<table class="md-table"><thead><tr>' + headers.map((h, idx) => '<th' + alignAttr(idx) + '>' + inline(h) + '</th>').join('') + '</tr></thead><tbody>';
      t += rows.map(r => '<tr>' + r.map((c, idx) => '<td' + alignAttr(idx) + '>' + inline(c) + '</td>').join('') + '</tr>').join('');
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
  // 转义竖线 \| 不参与分列，还原为字面 |（避免「/perm [a\|b\|c]」被误拆成多列）
  return s.split(/(?<!\\)\|/).map(c => c.trim().replace(/\\\|/g, '|'));
}

// ── SSE ──
const es = new EventSource('/events?client=' + clientId);
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
  if (state.model) currentModelId = state.model;
  if (state.smallModel) currentSmallModel = state.smallModel;
  renderModelBar(state);
  fetchPanel();
});
es.addEventListener('sessions', () => fetchSessions());
es.addEventListener('ask', e => showAsk(JSON.parse(e.data)));
es.addEventListener('compress', e => showCompress(JSON.parse(e.data)));

// ── 初始化 ──
applyTheme(localStorage.getItem('waycoder-theme') || 'dark');
fetch(cq('/state')).then(r => r.json()).then(state => {
  currentProvider = state.provider;
  hasKey = state.hasKey;
  applyPermMode(state.permMode);
  renderSlots(state);
});
fetch('/models').then(r => r.json()).then(models =>
  fetch(cq('/state')).then(r => r.json()).then(state => renderModels(models, state)));
fetchSessions();
fetchPanel();
setInterval(fetchPanel, 2000);
</script>
</body>
</html>
""";
}

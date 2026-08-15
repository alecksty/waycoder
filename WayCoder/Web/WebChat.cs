using System.Collections.Concurrent;
using System.Text;

namespace WayCoder.Web;

/// <summary>
/// 浏览器聊天桥接层：把 <see cref="Agent.ChatAsync"/> 的流式回调（onToken/onTool/onToolOutput）
/// 转为 SSE 事件广播给浏览器，接收浏览器 POST 的输入入队，支持中断。
/// </summary>
public sealed class WebChatServer
{
    private readonly Agent _agent;
    private readonly HttpServer _server;
    private readonly ConcurrentQueue<string> _input = new();
    private readonly object _lock = new();
    private readonly List<SseClient> _clients = new();
    private readonly CancellationTokenSource _serverCts = new();
    private CancellationTokenSource? _roundCts;
    private Task? _loopTask;

    /// <summary>SSE 客户端（写失败 = 断开）。</summary>
    private sealed class SseClient
    {
        public StreamWriter Writer = null!;
        public readonly TaskCompletionSource Closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public WebChatServer(Agent agent, int port)
    {
        _agent = agent;
        _server = new HttpServer(port);
    }

    /// <summary>实际绑定端口（传入 0 时由系统分配）。</summary>
    public int Port => _server.ActualPort;

    public void Start()
    {
        _server.OnRequest = HandleRequest;
        _server.OnSse = HandleSseAsync;
        _server.Start();
        _loopTask = Task.Run(() => MainLoopAsync(_serverCts.Token));
    }

    public void Stop()
    {
        try { _serverCts.Cancel(); } catch { }
        try { _roundCts?.Cancel(); } catch { }
        _server.Stop();
    }

    // ═══════════════════════════════════════════════════════════
    //  路由
    // ═══════════════════════════════════════════════════════════

    private HttpResponse? HandleRequest(HttpRequest req)
    {
        if (req.Method == "GET" && req.Path == "/")
            return HttpResponse.Html(Html);

        if (req.Method == "POST" && req.Path == "/chat")
        {
            if (!string.IsNullOrWhiteSpace(req.Body)) _input.Enqueue(req.Body);
            return HttpResponse.Text("ok");
        }

        if (req.Method == "POST" && req.Path == "/interrupt")
        {
            Interrupt();
            return HttpResponse.Text("ok");
        }

        if (req.Method == "GET" && req.Path == "/history")
            return HttpResponse.JsonBody(SerializeHistory());

        return null;
    }

    // ═══════════════════════════════════════════════════════════
    //  Agent 桥接
    // ═══════════════════════════════════════════════════════════

    private void Interrupt()
    {
        try { _roundCts?.Cancel(); } catch { }
    }

    private async Task MainLoopAsync(CancellationToken serverToken)
    {
        while (!serverToken.IsCancellationRequested)
        {
            if (_input.TryDequeue(out var userInput))
            {
                _roundCts = new CancellationTokenSource();
                var token = _roundCts.Token;
                try
                {
                    var final = await _agent.ChatAsync(
                        userInput,
                        onToken: t => Broadcast("token", JsonStr(t)),
                        onTool: (name, brief) => Broadcast("tool", JsonTool(name, brief)),
                        onToolOutput: o => Broadcast("tool_output", JsonStr(o)),
                        cancellationToken: token);
                    Broadcast("done", JsonStr(final));
                }
                catch (OperationCanceledException)
                {
                    Broadcast("interrupted", "null");
                }
                catch (Exception ex)
                {
                    Broadcast("failed", JsonStr(ex.Message));
                }
                finally
                {
                    _roundCts.Dispose();
                    _roundCts = null;
                }
            }
            else
            {
                try { await Task.Delay(50, serverToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task HandleSseAsync(StreamWriter writer)
    {
        var client = new SseClient { Writer = writer };
        lock (_lock) _clients.Add(client);
        try
        {
            // 连接即回放历史，前端初始化渲染
            writer.Write(HttpServer.SseEvent("history", SerializeHistory()));
            await client.Closed.Task;
        }
        catch { /* 连接断开 */ }
        finally
        {
            lock (_lock) _clients.Remove(client);
            try { writer.Dispose(); } catch { }
        }
    }

    private void Broadcast(string type, string dataJson)
    {
        var sse = HttpServer.SseEvent(type, dataJson);
        List<SseClient> snapshot;
        lock (_lock) snapshot = _clients.ToList();
        foreach (var c in snapshot)
        {
            try { c.Writer.Write(sse); }
            catch { c.Closed.TrySetResult(); }
        }
    }

    private string SerializeHistory()
    {
        var arr = JNode.Array();
        foreach (var m in _agent.Messages)
        {
            var role = m["role"]?.AsString();
            if (role != "user" && role != "assistant") continue;
            var content = m["content"]?.AsString();
            if (string.IsNullOrEmpty(content)) continue;
            arr.Add(JNode.Object().Set("role", role).Set("content", content));
        }
        return arr.ToJson();
    }

    // ═══════════════════════════════════════════════════════════
    //  JSON 辅助
    // ═══════════════════════════════════════════════════════════

    private static string JsonStr(string s) => JNode.Str(s).ToJson();

    private static string JsonTool(string name, string brief)
        => JNode.Object().Set("name", name).Set("args", brief).ToJson();

    // ═══════════════════════════════════════════════════════════
    //  内嵌前端（单 HTML，无构建，无外部 CDN）
    // ═══════════════════════════════════════════════════════════

    internal const string Html = """
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>WayCoder 聊天</title>
<style>
:root { --bg:#0f1117; --panel:#171a23; --border:#262b3a; --text:#e6e8ee; --dim:#8b93a7; --accent:#4f8cff; --user:#1f3a5f; --tool:#2a2416; }
* { box-sizing:border-box; margin:0; padding:0; }
body { background:var(--bg); color:var(--text); font:15px/1.6 -apple-system,"PingFang SC","Microsoft YaHei",sans-serif; height:100vh; display:flex; flex-direction:column; }
header { padding:12px 16px; border-bottom:1px solid var(--border); font-weight:600; color:#fff; background:var(--panel); }
#messages { flex:1; overflow-y:auto; padding:16px; display:flex; flex-direction:column; gap:12px; }
.msg { max-width:82%; padding:10px 14px; border-radius:10px; white-space:pre-wrap; word-break:break-word; }
.msg.user { align-self:flex-end; background:var(--user); }
.msg.assistant { align-self:flex-start; background:var(--panel); border:1px solid var(--border); }
.msg.system { align-self:center; color:var(--dim); font-size:13px; background:transparent; }
.tool { align-self:flex-start; background:var(--tool); border:1px solid var(--border); border-radius:10px; padding:8px 14px; font-size:13px; color:var(--dim); }
.tool b { color:#e8b34b; }
#input-bar { display:flex; gap:8px; padding:12px 16px; border-top:1px solid var(--border); background:var(--panel); }
#input { flex:1; resize:none; background:var(--bg); color:var(--text); border:1px solid var(--border); border-radius:8px; padding:10px 12px; font:inherit; min-height:44px; max-height:200px; outline:none; }
#input:focus { border-color:var(--accent); }
button { padding:0 18px; border:none; border-radius:8px; font:inherit; font-weight:600; cursor:pointer; }
#send { background:var(--accent); color:#fff; }
#send:hover { opacity:.9; }
#stop { background:#3a2a2a; color:#ff9a9a; }
#stop:hover { background:#4a3232; }
</style>
</head>
<body>
<header>🤖 WayCoder 聊天</header>
<div id="messages"></div>
<div id="input-bar">
  <textarea id="input" placeholder="输入消息，Enter 发送，Shift+Enter 换行" rows="1"></textarea>
  <button id="send">发送</button>
  <button id="stop">停止</button>
</div>
<script>
const messages = document.getElementById('messages');
const input = document.getElementById('input');
let streamEl = null;

function scroll() { messages.scrollTop = messages.scrollHeight; }

function addMsg(role, text) {
  const el = document.createElement('div');
  el.className = 'msg ' + role;
  el.textContent = text;
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

function ensureStream() {
  if (!streamEl) streamEl = addMsg('assistant', '');
  return streamEl;
}

function endStream() { streamEl = null; }

const es = new EventSource('/events');
es.addEventListener('token', e => { ensureStream().textContent += JSON.parse(e.data); scroll(); });
es.addEventListener('tool', e => { const d = JSON.parse(e.data); addTool(d.name, d.args); });
es.addEventListener('tool_output', e => { ensureStream().textContent += JSON.parse(e.data); scroll(); });
es.addEventListener('done', () => endStream());
es.addEventListener('interrupted', () => { endStream(); addMsg('system', '⚠ 已中断'); });
es.addEventListener('failed', e => { endStream(); addMsg('system', '✘ ' + JSON.parse(e.data)); });
es.addEventListener('history', e => {
  const list = JSON.parse(e.data);
  if (messages.children.length === 0)
    list.forEach(m => addMsg(m.role === 'user' ? 'user' : 'assistant', m.content));
});

function send() {
  const text = input.value.trim();
  if (!text) return;
  addMsg('user', text);
  input.value = '';
  input.style.height = 'auto';
  fetch('/chat', { method: 'POST', body: text }).catch(() => {});
}
document.getElementById('send').onclick = send;
document.getElementById('stop').onclick = () => fetch('/interrupt', { method: 'POST' }).catch(() => {});
input.addEventListener('keydown', e => {
  if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); }
});
input.addEventListener('input', () => { input.style.height = 'auto'; input.style.height = Math.min(input.scrollHeight, 200) + 'px'; });
</script>
</body>
</html>
""";
}

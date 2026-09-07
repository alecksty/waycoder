using System.Text;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Web;

/// <summary>
/// Web 命令上下文：路由层 / HandleCommand 在每个命令执行前设置当前参与执行的 Agent 与槽位，
/// 供 Web 命令读取（命令签名 ExecuteAsync 不含 agent/slot，经此传达）。
/// 用 <see cref="AsyncLocal{T}"/> 隔离——多浏览器绑定不同槽位并发 POST /command 时互不串扰。
/// </summary>
public static class WebCommandContext
{
    private static readonly AsyncLocal<Agent?> _agent = new();
    private static readonly AsyncLocal<int> _slot = new();

    public static Agent? Agent { get => _agent.Value; set => _agent.Value = value; }
    public static int Slot { get => _slot.Value; set => _slot.Value = value; }
}

/// <summary>
/// Web 命令输出桥：把命令的 AddSystemMsg/AddMessage 收集到 <see cref="Output"/>，供 /command 路由
/// 取回返回前端。继承主工程 ChatScreen 但 override 消息方法为「收集」，不触碰任何 TUI 控件
/// （基类构造仅设 Name="chat"，无渲染依赖，可安全在 Web 进程实例化）。
/// </summary>
public sealed class WebChatScreen : ChatScreen
{
    public StringBuilder Output { get; } = new();

    public override void AddSystemMsg(string content) => Output.Append(content).Append('\n');

    public override void AddMessage(string content, string role = "assistant", bool? centered = null, int indent = 0, bool shellBlock = false)
        => Output.Append(content).Append('\n');

    public override void AddUserMsg(string content) => Output.Append(content).Append('\n');

    public override void ClearChat() => Output.Clear();
}

/// <summary>
/// Web 端斜杠命令（文本版）。经 <see cref="SlashCommandRegistry.ApplyEndCommands"/> 注入、覆盖主工程同名命令，
/// 复用 <see cref="WebChatServer"/> 的 WebXxxText 纯逻辑。命令集静态，构建一次缓存（避免每 /command 请求重建实例）。
/// 别名一律带前导 '/'（对齐 CLI 约定，否则输入 /clear 等与裸别名永不匹配）。
/// </summary>
public static class WebCommands
{
    // 命令集静态缓存：All() 直接返回，避免 MatchWebCommand 每次 /command 都重建命令实例（生成器 yield）。
    private static readonly IReadOnlyList<ISlashCommand> _commands = Build();

    public static IEnumerable<ISlashCommand> All() => _commands;

    private static IReadOnlyList<ISlashCommand> Build()
    {
        var l = new List<ISlashCommand>();
        l.Add(new WebTextCommand("/help", _ => WebChatServer.WebHelpText()));
        l.Add(new WebTextCommand("/perm", a => WebChatServer.WebSandboxText(a), "/permissions"));
        l.Add(new WebTextCommand("/permit", a => WebChatServer.WebPermText(a)));
        l.Add(new WebTextCommand("/reset", _ =>
        {
            var ag = WebCommandContext.Agent;
            if (ag != null) ag.ClearMessages();
            return "🗑 已清空当前会话";
        }, "/clear"));
        l.Add(new WebTextCommand("/session", a => WebChatServer.WebSessionText(a, WebCommandContext.Agent, WebCommandContext.Slot)));
        l.Add(new WebTextCommand("/tokens", _ => WebChatServer.WebTokensText(WebCommandContext.Agent)));
        l.Add(new WebTextCommand("/mcp", _ => WebChatServer.WebMcpText()));
        l.Add(new WebTextCommand("/todo", _ => WebChatServer.WebTodoText()));
        l.Add(new WebTextCommand("/stats", _ => WebChatServer.WebStatsText(WebCommandContext.Agent)));
        l.Add(new WebTextCommand("/recent", _ => WebChatServer.WebRecentText(), "/diff"));
        l.Add(new WebTextCommand("/free", _ => WebChatServer.WebFreeText()));
        l.Add(new WebTextCommand("/free-restore", _ => ModelCli.RestorePrevious(), "/恢复模型"));
        l.Add(new WebTextCommand("/test", a => WebChatServer.WebTestText(a)));
        // /interrupt /stop：真实中断副作用在 WebChat 路由层（需实例 Cts）；此处补 ack，HandleCommand 直调也能回应。
        l.Add(new WebTextCommand("/interrupt", _ => "⏹ 已请求中断", "/stop"));
        return l;
    }

    /// <summary>Web 命令基类：Render(args) 返回文本，ExecuteAsync 经 WebChatScreen 收集。</summary>
    private sealed class WebTextCommand : SlashCommand
    {
        private readonly Func<string, string> _render;
        private readonly string[] _aliases;
        public override string Name { get; }
        public override string[] Aliases => _aliases;
        public override string Description => $"Web 命令（{Name}）";

        public WebTextCommand(string name, Func<string, string> render, params string[] aliases)
        {
            Name = name; _render = render; _aliases = aliases;
        }

        public override Task ExecuteAsync(string args, ChatScreen screen)
        {
            screen.AddSystemMsg(_render(args));
            return Task.CompletedTask;
        }
    }
}

using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Gui;

/// <summary>GUI 命令上下文：MainWindow 构造时赋当前主窗口，供 GuiCommands 调用（与 Web 的 WebCommandContext 同理）。</summary>
public static class GuiContext
{
    public static MainWindow? MainWindow;
}

/// <summary>
/// GUI 端斜杠命令（以弹窗/界面操作为主）。经 RegisterAll→ApplyEndCommands 注入 GUI 自建 SlashCommandRegistry，
/// 命名与 CLI/Web 一致（同名命令统一语义）；命令忽略 screen 参数、经 GuiContext.MainWindow 调用主窗口
/// internal 方法实现 GUI 特有行为。覆盖 CLI/Web 的通用命令子集，读全局状态（ModelCli/McpManager/SessionManager/
/// EditFileTool/TodoTool）并回写系统消息。
/// </summary>
public static class GuiCommands
{
    public static IEnumerable<ISlashCommand> All()
    {
        yield return new HelpCmd();
        yield return new ModelCmd();
        yield return new ProviderCmd();
        yield return new ReviewCmd();
        yield return new SettingsCmd();
        yield return new ThemeCmd();
        yield return new ResetCmd();
        yield return new TodoCmd();
        yield return new TokensCmd();
        yield return new PermCmd();
        yield return new PermitCmd();
        yield return new SlotsCmd();
        yield return new SessionCmd();
        yield return new StatsCmd();
        yield return new McpCmd();
        yield return new FreeCmd();
        yield return new FreeRestoreCmd();
        yield return new RecentCmd();
        yield return new DiffCmd();

        // —— 补齐主工程 TUI 命令（能映射端界面/读真实状态的做实，其余给端化说明）——
        yield return new InfoCmd("/about", "关于 WayCoder", _ => "WayCoder（道码）· C# (.NET) · 中文版易用编程智能体");
        yield return new InfoCmd("/menu", "功能菜单（模型/设置/会话/Diff 等界面直达 + 常用命令）", _ => "GUI 顶栏/☰ 为命令入口；斜杠命令直接输入（/help 看全量）");
        yield return new InfoCmd("/edit", "打开编辑器", _ => { GuiContext.MainWindow?.OpenEditor(); return "🧬 已打开编辑器"; });
        yield return new InfoCmd("/config", "打开设置", _ => { GuiContext.MainWindow?.OpenSettings(); return "⚙️ 已打开设置"; });
        yield return new InfoCmd("/mode", "显示工作模式", _ => $"当前工作模式: {WorkModeManager.CurrentMode}");
        yield return new InfoCmd("/cd", "当前目录", _ => $"当前目录: {System.IO.Directory.GetCurrentDirectory()}");
        yield return new InfoCmd("/git", "Git 状态", _ => "GUI 下 Git 操作见 GitSync/顶部 Git 菜单");
        yield return new InfoCmd("/pr", "Pull Request", _ => "GUI 下 PR 请在 Git 仓库终端发起");
        yield return new InfoCmd("/connection", "当前连接", _ => "连接/模型见顶栏 Provider；切换用 /model", "/connect");
        yield return new InfoCmd("/checkpoint", "检查点", _ => "检查点请用 Checkpoint 面板或 /timeline");
        yield return new InfoCmd("/checkpoints", "列出检查点", _ => "检查点列表见 /timeline");
        yield return new InfoCmd("/timeline", "时间线", _ => "时间线见顶部会话/检查点面板");
        yield return new InfoCmd("/undo", "撤销", _ => "GUI 撤销在编辑器内（Ctrl+Z）");
        yield return new InfoCmd("/versions", "文件版本", _ => "版本/快照见编辑器或 Git");
        yield return new InfoCmd("/compact", "上下文压缩", _ => "上下文压缩自动触发（见动态进度条）");
        yield return new InfoCmd("/diag", "诊断", _ => "运行 `waycoder doctor` 查看系统自检");
        yield return new InfoCmd("/debug-on", "开启调试日志", _ => "调试日志请在配置/启动参数设 --debug");
        yield return new InfoCmd("/debug-off", "关闭调试日志", _ => "调试日志关闭（重启生效）");
        yield return new InfoCmd("/update", "检查更新", _ => "更新请下载新版本（GitHub Releases）或在终端 `--update`");
        yield return new InfoCmd("/resume", "恢复会话", _ => "用法: /session load <会话ID>");
        yield return new InfoCmd("/import", "导入模型/配置", _ => "导入用顶部菜单或 `--model import`");
        yield return new InfoCmd("/init", "初始化项目", _ => "项目初始化已生成 AGENTS.md/CLAUDE.md（/init claude）");
        yield return new InfoCmd("/reproduce", "复现报告", _ => "复现报告见 /diag", "/repro");
        yield return new InfoCmd("/architect", "架构审查", _ => "架构审查在 /review 里（git diff 维度）");
        yield return new InfoCmd("/search", "搜索", _ => "GUI 搜索用顶部搜索框 / 编辑器（Ctrl+F）");
        yield return new InfoCmd("/lint", "静态检查", _ => "GUI 静态检查在编辑器诊断/Lint");
        yield return new InfoCmd("/export", "导出会话", _ => "导出会话见顶部 ☰ 菜单 / 会话保存");
        yield return new InfoCmd("/join", "加入会话", _ => "多端会话/协作见 /session 或 Web 页");
        yield return new InfoCmd("/send", "跨槽位发送", _ => "槽位间消息用 F1-F10 切换槽位");
        yield return new InfoCmd("/broadcast", "广播", _ => "广播见多 Agent 面板");
        yield return new InfoCmd("/repomap", "仓库地图", _ => "仓库地图见 README/项目概览");
        yield return new InfoCmd("/sync-qr", "扫码同步", _ => "扫码同步为移动端功能（TUI/MAUI）");
        yield return new InfoCmd("/teach", "教学", _ => "教学模块见 /help 或文档");
        yield return new InfoCmd("/kb", "知识库", _ => "知识库见 /help 或文档");
        yield return new InfoCmd("/doctor", "系统自检", _ => "运行 `waycoder doctor` 查看系统自检");
        yield return new InfoCmd("/auto", "自动提交", _ => "自动提交见 Git 面板/--git-auto");
        yield return new InfoCmd("/autocommit", "自动提交", _ => "自动提交见 Git 面板");
        yield return new InfoCmd("/exit", "退出", _ => "请关闭窗口（或 Ctrl+Q）退出 GUI");
        yield return new InfoCmd("/test", "测试命令", _ => "/test 为开发版调试命令");
        yield return new InfoCmd("/history", "会话历史", _ =>
        {
            var ss = SessionManager.ListSessions(20, 0, -1);
            return ss.Count == 0 ? "📂 没有已保存的会话"
                : $"📂 **会话历史**（{ss.Count} 条）\n" + string.Join("\n", ss.Select(s => $"- `{s.Id}` · {s.Model}"));
        }, "/hist");
    }

    private abstract class GuiCmd : SlashCommand
    {
        public sealed override Task ExecuteAsync(string args, ChatScreen screen)
        {
            _ = screen; // GUI 命令经 GuiContext.MainWindow 操作主窗口，不使用传入屏幕
            Run(GuiContext.MainWindow, args);
            return Task.CompletedTask;
        }

        protected abstract void Run(MainWindow? win, string args);
    }

    /// <summary>说明/数据命令基类：Render(args) 返回文本（可读全局状态），经 NotifySystem 回写。</summary>
    private sealed class InfoCmd : GuiCmd
    {
        private readonly string _name;
        private readonly string[] _aliases;
        private readonly string _desc;
        private readonly Func<string, string> _render;
        public override string Name => _name;
        public override string[] Aliases => _aliases;
        public override string Description => _desc;

        public InfoCmd(string name, string desc, Func<string, string> render, params string[] aliases)
        {
            _name = name; _desc = desc; _render = render; _aliases = aliases;
        }

        protected override void Run(MainWindow? win, string args) => win?.NotifySystem(_render(args));
    }

    private sealed class HelpCmd : GuiCmd
    {
        public override string Name => "/help";
        public override string[] Aliases => ["/h"];
        public override string Description => "显示命令帮助";
        protected override void Run(MainWindow? win, string args) => win?.NotifySystem(
            "GUI 斜杠命令：\n/help 帮助\n/model 选择模型\n/provider 服务商管理\n/review 代码审查\n/settings 设置\n/theme 切主题\n/reset 清空会话\n/todo 任务列表\n/tokens 本轮 token/费用\n/perm <off|project|network-off|hard> 沙箱边界\n/permit <ask|auto|smart|yolo> 权限模式\n/session [list|save|load <id>] 会话\n/stats 会话统计\n/mcp MCP 状态\n/free 免费模型\n/free-restore 恢复模型\n/recent 本次修改文件\n/diff 改动文件差异\n/slots 槽位说明");
    }

    private sealed class ModelCmd : GuiCmd
    {
        public override string Name => "/model";
        public override string Description => "选择模型";
        protected override void Run(MainWindow? win, string args) => win?.OpenModelPicker();
    }

    private sealed class ProviderCmd : GuiCmd
    {
        public override string Name => "/provider";
        public override string Description => "服务商管理（Key/改名/改地址/删除/测试）";
        protected override void Run(MainWindow? win, string args) => win?.OpenProviders();
    }

    private sealed class ReviewCmd : GuiCmd
    {
        public override string Name => "/review";
        public override string[] Aliases => ["/审查"];
        public override string Description => "代码审查（git diff + 多维度分析）";
        protected override void Run(MainWindow? win, string args) => win?.RunReview();
    }

    private sealed class SettingsCmd : GuiCmd
    {
        public override string Name => "/settings";
        public override string Description => "打开设置";
        protected override void Run(MainWindow? win, string args) => win?.OpenSettings();
    }

    private sealed class ThemeCmd : GuiCmd
    {
        public override string Name => "/theme";
        public override string Description => "切换深/浅主题";
        protected override void Run(MainWindow? win, string args) => win?.ToggleThemeUi();
    }

    private sealed class ResetCmd : GuiCmd
    {
        public override string Name => "/reset";
        public override string Description => "清空当前会话";
        protected override void Run(MainWindow? win, string args) => win?.ResetSession();
    }

    private sealed class TodoCmd : GuiCmd
    {
        public override string Name => "/todo";
        public override string[] Aliases => ["/todos"];
        public override string Description => "显示任务列表";
        protected override void Run(MainWindow? win, string args)
        {
            var items = TodoTool.Items;
            if (items == null || items.Count == 0) { win?.NotifySystem("[无任务]"); return; }
            var sb = new StringBuilder();
            foreach (var t in items) sb.AppendLine($"• [{t.Status}] {t.Title}");
            win?.NotifySystem(sb.ToString());
        }
    }

    private sealed class TokensCmd : GuiCmd
    {
        public override string Name => "/tokens";
        public override string Description => "显示本轮 token/费用";
        protected override void Run(MainWindow? win, string args) => win?.NotifySystem(win?.TokensSummary() ?? "[无活动数据]");
    }

    /// <summary>/perm —— 沙箱边界（对齐 CLI/Web：边界独立于权限，/permit 才管确认）。</summary>
    private sealed class PermCmd : GuiCmd
    {
        public override string Name => "/perm";
        public override string Description => "<off|project|network-off|hard> 沙箱边界（独立于权限）";
        protected override void Run(MainWindow? win, string args)
        {
            var arg = args.Trim();
            if (arg.Length == 0) { win?.NotifySystem($"当前沙箱边界: {SandboxManager.Level}（off/project/network-off/hard；/permit 管权限）"); return; }
            try { SandboxManager.SetLevel(arg); win?.NotifySystem($"沙箱边界已切换: {SandboxManager.Level}"); }
            catch (Exception ex) { win?.NotifySystem($"[切换失败] {ex.Message}"); }
        }
    }

    /// <summary>/permit —— 权限模式（确认轴，对齐 CLI/Web）。</summary>
    private sealed class PermitCmd : GuiCmd
    {
        public override string Name => "/permit";
        public override string Description => "<ask|auto|smart|yolo> 切换权限模式";
        protected override void Run(MainWindow? win, string args)
        {
            var arg = args.Trim();
            if (arg.Length == 0) { win?.NotifySystem($"当前权限模式: {PermLabel()}（ask/auto/smart/yolo）"); return; }
            try
            {
                if (PermissionManager.IsChatModeAlias(arg)) { WorkModeManager.SetMode(WorkMode.Chat); win?.NotifySystem("[工作模式已切换: 💬 聊天（纯聊天 · 0 工具 0 提示词）]"); return; }
                PermissionManager.SetMode(arg);
                win?.NotifySystem($"[权限模式已切换: {arg}]（{PermLabel()}）");
            }
            catch (Exception ex) { win?.NotifySystem($"[切换失败] {ex.Message}"); }
        }

        /// <summary>文案唯一真源见 <see cref="UiText.PermFull"/>。</summary>
        private static string PermLabel() => UiText.PermFull(PermissionManager.CurrentMode);
    }

    private sealed class SlotsCmd : GuiCmd
    {
        public override string Name => "/slots";
        public override string Description => "槽位说明";
        protected override void Run(MainWindow? win, string args) => win?.NotifySystem("F1-F10 切换 10 个独立槽位（各自会话/模型/草稿）；顶栏标签显示当前槽位");
    }

    private sealed class SessionCmd : GuiCmd
    {
        public override string Name => "/session";
        public override string Description => "[list|save|load <id>] 会话管理";
        protected override void Run(MainWindow? win, string args)
        {
            var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var sub = parts.Length > 0 ? parts[0].ToLowerInvariant() : "list";
            var rest = parts.Length > 1 ? parts[1].Trim() : "";
            var agent = win?.ActiveAgent;
            var slot = win?.ActiveSlotIndex ?? -1;

            switch (sub)
            {
                case "save":
                    if (agent == null) { win?.NotifySystem("⚠ 无活跃槽位"); return; }
                    win?.NotifySystem($"💾 会话已保存: **{agent.SaveSession(null, slot)}**");
                    return;
                case "load":
                    if (string.IsNullOrWhiteSpace(rest)) { win?.NotifySystem("用法: /session load <会话ID>"); return; }
                    var loaded = SessionManager.LoadSession(rest, slot);
                    if (loaded == null) { win?.NotifySystem($"❌ 会话不存在: {rest}"); return; }
                    if (agent == null) { win?.NotifySystem("⚠ 无活跃槽位"); return; }
                    agent.ReplaceMessages(loaded.Value.Messages);
                    win?.NotifySystem($"📂 已加载会话: **{rest}**（{loaded.Value.Messages.Count} 条消息）");
                    return;
                default:
                    var sessions = SessionManager.ListSessions(20, 0, slot);
                    if (sessions.Count == 0) { win?.NotifySystem("📂 没有已保存的会话"); return; }
                    var sb = new StringBuilder();
                    sb.AppendLine($"📂 **已保存的会话**（{sessions.Count} 条）");
                    foreach (var s in sessions) sb.AppendLine($"- `{s.Id}` · {s.Model} · {s.SavedAt}");
                    win?.NotifySystem(sb.ToString());
                    return;
            }
        }
    }

    private sealed class StatsCmd : GuiCmd
    {
        public override string Name => "/stats";
        public override string Description => "会话统计";
        protected override void Run(MainWindow? win, string args)
        {
            var llm = win?.ActiveAgent?.LlmClient;
            if (llm == null) { win?.NotifySystem("[无活动数据]"); return; }
            win?.NotifySystem($"模型：`{llm.Model}`\n请求数：{llm.TotalRequests}\n累计 token：prompt {llm.TotalPromptTokens} / completion {llm.TotalCompletionTokens}\n本轮 token：prompt {llm.TaskPromptTokens} / completion {llm.TaskCompletionTokens}" +
                (llm.TaskCost.HasValue ? $"\n本轮费用：${llm.TaskCost.Value:F4}" : ""));
        }
    }

    private sealed class McpCmd : GuiCmd
    {
        public override string Name => "/mcp";
        public override string Description => "MCP 服务器状态";
        protected override void Run(MainWindow? win, string args)
        {
            var servers = McpManager.Servers;
            if (servers.Count == 0) { win?.NotifySystem("🔌 未配置 MCP 服务器"); return; }
            var sb = new StringBuilder();
            foreach (var s in servers)
            {
                var icon = s.Status == McpServerStatus.Connected ? "🟢" : s.Status == McpServerStatus.Connecting ? "🟡" : "🔴";
                sb.AppendLine($"- {icon} `{s.Name}`（{s.Transport}）· {s.ToolCount} 工具");
            }
            win?.NotifySystem(sb.ToString());
        }
    }

    private sealed class FreeCmd : GuiCmd
    {
        public override string Name => "/free";
        public override string Description => "扫描可用免费模型（省钱，记住切换前模型）";
        protected override void Run(MainWindow? win, string args)
        {
            ModelCli.RememberCurrentModel();
            var available = ModelCli.LoadFreeJson();
            if (available.Count == 0) { win?.NotifySystem("⚠️ 暂无免费可用列表。先跑 `--model free` 扫描一次生成 free.json"); return; }
            var sb = new StringBuilder();
            sb.AppendLine($"💰 **可用免费模型**（{available.Count} 个 · 缓存）");
            foreach (var c in available) sb.AppendLine($"- `{ModelCatalog.ShortDisplayName(c.ModelId)}` | {ModelCatalog.ProviderDisplayName(c.ProviderId)}");
            win?.NotifySystem(sb.ToString());
        }
    }

    private sealed class FreeRestoreCmd : GuiCmd
    {
        public override string Name => "/free-restore";
        public override string[] Aliases => ["/恢复模型"];
        public override string Description => "恢复 /free 切换前的模型";
        protected override void Run(MainWindow? win, string args) => win?.NotifySystem(ModelCli.RestorePrevious());
    }

    private sealed class RecentCmd : GuiCmd
    {
        public override string Name => "/recent";
        public override string Description => "本次修改的文件";
        protected override void Run(MainWindow? win, string args) => win?.NotifySystem(ChangedFilesText("📝 **本次修改的文件**"));
    }

    private sealed class DiffCmd : GuiCmd
    {
        public override string Name => "/diff";
        public override string Description => "本次改动文件的差异摘要（GUI 无终端 DiffPreview，展示文件+增删行）";
        protected override void Run(MainWindow? win, string args) => win?.NotifySystem(ChangedFilesText("🧾 **本次改动文件**（+add -del）"));
    }

    private static string ChangedFilesText(string title)
    {
        var files = EditFileTool.ChangedFiles.ToList();
        if (files.Count == 0) return "📝 本次会话尚未修改文件";
        var sb = new StringBuilder();
        sb.AppendLine($"{title}（{files.Count} 个）");
        foreach (var f in files)
        {
            EditFileTool.ChangedFileStats.TryGetValue(f, out var st);
            sb.AppendLine($"- `{System.IO.Path.GetFileName(f)}` +{st.Added} -{st.Deleted}");
        }
        return sb.ToString();
    }
}

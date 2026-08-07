using System.Text;
using CoreCoderSharp.Tools;
using CoreCoderSharp.UI;
using Spectre.Console;

namespace CoreCoderSharp;

/// <summary>
/// 入口 + CLI + REPL —— 面向用户的终端界面。
/// </summary>
public class Program
{
    private static Config _config = new();
    private static LLM? _llm;
    private static Agent? _agent;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // 手动解析 CLI 参数
        string? model = null, baseUrl = null, apiKey = null, prompt = null, resumeId = null;
        double? maxBudget = null;
        bool showVersion = false, yoloMode = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-m" or "--model" when i + 1 < args.Length: model = args[++i]; break;
                case "--base-url" when i + 1 < args.Length: baseUrl = args[++i]; break;
                case "--api-key" when i + 1 < args.Length: apiKey = args[++i]; break;
                case "-p" or "--prompt" when i + 1 < args.Length: prompt = args[++i]; break;
                case "-r" or "--resume" when i + 1 < args.Length: resumeId = args[++i]; break;
                case "-v" or "--version": showVersion = true; break;
                case "-t" or "--test": SelfTest.Run(); return 0;
                case "--debug": DebugLog.Enable(); break;
                case "--yolo": yoloMode = true; break;
                case "--max-budget-usd" when i + 1 < args.Length:
                    if (double.TryParse(args[++i], out var b)) maxBudget = b; break;
                case "-h" or "--help": ShowUsage(); return 0;
            }
        }

        if (showVersion) { Console.WriteLine("CoreCoderSharp v0.11.0"); return 0; }

        _config = Config.FromEnv();
        if (model != null) _config.Model = model;
        if (baseUrl != null) _config.BaseUrl = baseUrl;
        if (apiKey != null) _config.ApiKey = apiKey;
        if (maxBudget != null) _config.MaxBudgetUsd = maxBudget;

        // DeepSeek 模型自动设置 base URL
        if (_config.BaseUrl == null && _config.Model.StartsWith("deepseek"))
            _config.BaseUrl = "https://api.deepseek.com";

        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            MarkupLine("[bold red]╔══════════════════════════════╗[/]");
            MarkupLine("[bold red]║  API 密钥未设置！           ║[/]");
            MarkupLine("[bold red]╚══════════════════════════════╝[/]");
            Console.WriteLine();
            Console.WriteLine("请设置以下环境变量之一:");
            Console.WriteLine("  CORECODER_API_KEY");
            Console.WriteLine("  OPENAI_API_KEY");
            Console.WriteLine("  DEEPSEEK_API_KEY");
            Console.WriteLine();
            Console.WriteLine("或者在项目根目录创建 .env 文件:");
            Console.WriteLine("  CORECODER_API_KEY=sk-你的密钥");
            return 1;
        }

        _llm = new LLM(_config.Model, _config.ApiKey, _config.BaseUrl,
            _config.MaxTokens, _config.Temperature);
        _agent = new Agent(_llm, maxContextTokens: _config.MaxContextTokens,
            maxBudgetUsd: _config.MaxBudgetUsd);

        // --yolo: 一次性模式下跳过所有权限确认
        if (yoloMode)
            PermissionManager.SetMode("yolo");

        // 加载自定义斜杠命令、hooks 和 MCP 服务器
        CustomCommands.Load();
        HooksManager.Init();
        McpManager.Init();

        // 恢复会话
        if (resumeId != null)
        {
            var loaded = SessionManager.LoadSession(resumeId);
            if (loaded != null)
            {
                _agent.Messages = loaded.Value.Messages;
                if (model == null) { _llm.Model = loaded.Value.Model; _config.Model = loaded.Value.Model; }
                MarkupLine($"[green]✔ 已恢复会话:[/] [cyan]{E(resumeId)}[/] [dim](模型: {E(_llm.Model)})[/]");
            }
            else
            {
                MarkupLine($"[red]✘ 会话 '{E(resumeId)}' 未找到[/]");
                return 1;
            }
        }

        if (prompt != null)
            await RunOnceAsync(prompt);
        else
            await RunReplAsync();

        return 0;
    }

    // ========================================================================
    // 一次性模式
    // ========================================================================

    private static async Task RunOnceAsync(string prompt)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            MarkupLine($"[dim]🤖 {E(prompt)}[/]");
            await ChatWithStatusAsync(prompt, cts.Token);
            Console.WriteLine();
        }
        catch (OperationCanceledException)
        {
            if (cts.IsCancellationRequested)
            {
                MarkupLine("\n[orange3]⚠ 已中断[/]");
                Environment.Exit(130);
            }
            else
            {
                TuiBox.Error("请求超时", "服务器 60s 未响应，请检查网络或 API 配置");
                Environment.Exit(1);
            }
        }
        catch (Exception ex)
        {
            TuiBox.Error("错误", ex.Message);
            Environment.Exit(1);
        }
    }

    // ========================================================================
    // 交互式 REPL
    // ========================================================================

    private static async Task RunReplAsync()
    {
        var sm = ScreenManager.Instance;
        sm.Enter();
        sm.ChatMessages.Clear();
        sm.InputLines.Clear();
        sm.InputLines.Add(new StringBuilder());
        sm.AddSystemMsg($"CoreCoder v0.11.0 · 模型: {_config.Model}  ·  /help 帮助");
        sm.StatusLeft = _config.Model;

        var running = true;
        while (running)
        {
            sm.Render();

            var key = Console.ReadKey(intercept: true);
            bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);
            bool shift = key.Modifiers.HasFlag(ConsoleModifiers.Shift);

            // 终端 resize 检测
            var (tw, th) = (Console.WindowWidth, Console.WindowHeight);
            if (tw != sm.TW || th != sm.TH) { sm.Render(); continue; }

            // ---- 建议模式 ----
            if (sm.SuggestActive)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Escape: sm.SuggestActive = false; continue;
                    case ConsoleKey.UpArrow: if (sm.SuggestIdx > 0) sm.SuggestIdx--; continue;
                    case ConsoleKey.DownArrow: if (sm.SuggestIdx < sm.Suggestions.Count - 1) sm.SuggestIdx++; continue;
                    case ConsoleKey.Enter: case ConsoleKey.Tab:
                        sm.AcceptSuggestion(); sm.UpdateSuggestions(); continue;
                    case ConsoleKey.Backspace: sm.InputBackspace(); sm.UpdateSuggestions(); break;
                    case ConsoleKey.LeftArrow: case ConsoleKey.RightArrow:
                        sm.SuggestActive = false; break;
                    default: break;
                }
                if (key.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow or ConsoleKey.Enter or ConsoleKey.Tab or ConsoleKey.Escape)
                    continue;
            }

            sm.SyncTodos(); // 每帧同步 Todo 数据

            switch (key.Key)
            {
                case ConsoleKey.Enter when !ctrl && !shift:
                    var input = sm.GetInputText();
                    if (string.IsNullOrWhiteSpace(input)) continue;
                    sm.AddUserMsg(input);
                    sm.SetInput("");
                    sm.Render();
                    await ProcessUserInput(input, sm);
                    break;

                case ConsoleKey.F2:
                    sm.ActivePanel = sm.ActivePanel switch
                    {
                        ScreenManager.PanelTab.Off => ScreenManager.PanelTab.Todo,
                        ScreenManager.PanelTab.Todo => ScreenManager.PanelTab.Files,
                        ScreenManager.PanelTab.Files => ScreenManager.PanelTab.LSP,
                        ScreenManager.PanelTab.LSP => ScreenManager.PanelTab.MCP,
                        _ => ScreenManager.PanelTab.Off,
                    };
                    if (sm.ActivePanel == ScreenManager.PanelTab.Files)
                        sm.ModifiedFiles = EditFileTool.ChangedFiles.ToList();
                    break;

                // ---- 聊天区滚动 ----
                case ConsoleKey.PageUp: sm.ChatScrollUp(Math.Max(1, (Console.WindowHeight - 10) / 2)); break;
                case ConsoleKey.PageDown: sm.ChatScrollDown(Math.Max(1, (Console.WindowHeight - 10) / 2)); break;
                case ConsoleKey.Home when ctrl: sm.ChatScrollTop(); break;
                case ConsoleKey.End when ctrl: sm.ChatScrollBottom(); break;

                case ConsoleKey.Enter when ctrl || shift:
                    sm.InputNewLine();
                    sm.UpdateSuggestions();
                    break;
                case ConsoleKey.Escape when string.IsNullOrEmpty(sm.GetInputText()):
                    running = false;
                    break;

                case ConsoleKey.Backspace: sm.InputBackspace(); sm.UpdateSuggestions(); break;
                case ConsoleKey.Delete: sm.InputDelete(); sm.UpdateSuggestions(); break;
                case ConsoleKey.LeftArrow: sm.InputMoveLeft(); break;
                case ConsoleKey.RightArrow: sm.InputMoveRight(); break;
                case ConsoleKey.UpArrow: sm.InputMoveUp(); break;
                case ConsoleKey.DownArrow: sm.InputMoveDown(); break;
                case ConsoleKey.Home: sm.InputCx = 0; break;
                case ConsoleKey.End: sm.InputCx = sm.InputLines[sm.InputCy].Length; break;
                case ConsoleKey.Tab:
                    for (int t = 0; t < 4; t++) sm.InputInsert(' ');
                    sm.UpdateSuggestions();
                    break;

                default:
                    if (key.KeyChar >= ' ')
                    {
                        sm.InputInsert(key.KeyChar);
                        sm.UpdateSuggestions();
                    }
                    break;
            }
        }

        sm.Exit();
    }

    /// <summary>处理用户输入：内置命令或 Agent 调用</summary>
    private static async Task ProcessUserInput(string userInput, ScreenManager sm)
    {
        // 全角规范化
        userInput = userInput
            .Replace('／', '/').Replace('！', '!').Replace('＃', '#');
        var lower = userInput.ToLowerInvariant();

        // 退出
        if (lower is "quit" or "exit" or "/quit" or "/exit")
        {
            sm.Exit();
            Environment.Exit(0);
        }

        // 触发提示 (已通过建议面板处理，但保留备用)
        if (userInput == "/") { sm.SetInput(ShowCommandPalette()); sm.Render(); return; }
        if (userInput == "!") { await RunShellOnceAsync(); return; }

        // 内置命令
        if (userInput == "/help") { ShowHelpInChat(sm); return; }
        if (userInput == "/reset") { _agent!.Reset(); sm.AddSystemMsg("♻ 对话已重置"); return; }
        if (userInput == "/tokens") { ShowTokensInChat(sm); return; }
        if (userInput == "/model") { sm.AddSystemMsg($"当前模型: {_config.Model}"); return; }
        if (userInput.StartsWith("/model ")) { SwitchModelInline(userInput, sm); return; }
        if (userInput == "/compact") { await CompactAsync(); sm.AddSystemMsg("✔ 上下文已压缩"); return; }
        if (userInput == "/save") { SaveSessionInChat(sm); return; }
        if (userInput == "/permissions" || userInput == "/perm") { ShowPermStatusInChat(sm); return; }
        if (userInput.StartsWith("/perm ")) { PermissionManager.SetMode(userInput[6..].Trim()); sm.AddSystemMsg($"权限模式已切换"); return; }
        if (userInput == "/sessions") { ShowSessionsInChat(sm); return; }
        if (userInput == "/diff") { ShowDiffInChat(sm); return; }
        if (userInput == "/plan") { sm.AddSystemMsg("📋 计划模式"); await PlanModeAsync(); return; }
        if (userInput.StartsWith("/search ")) { await RunSearchAsync(userInput[8..].Trim()); return; }
        if (userInput == "/edit") { await Editor.PickAndRunAsync(); sm.Render(); return; }
        if (userInput.StartsWith("/edit ")) { await Editor.RunAsync(userInput[6..].Trim()); sm.Render(); return; }
        if (userInput is "/settings" or "/config") { SettingsPage.Show(); return; }

        // 调用 Agent
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            sm.StartAgentMsg();
            sm.Render();

            await _agent!.ChatAsync(userInput,
                onToken: tok =>
                {
                    sm.AppendToken(tok);
                    sm.Render();
                },
                onTool: (name, brief) =>
                {
                    sm.FinishAgentMsg();
                    sm.AddToolMsg(name, brief.Length > 60 ? brief[..57] + "..." : brief);
                    sm.StartAgentMsg();
                    sm.Render();
                },
                cancellationToken: cts.Token);

            sm.FinishAgentMsg();
        }
        catch (OperationCanceledException)
        {
            sm.FinishAgentMsg();
            sm.AddSystemMsg(cts.IsCancellationRequested
                ? "⚠ 已中断" : "⏰ 服务器 60s 未响应");
        }
        catch (Exception ex)
        {
            sm.FinishAgentMsg();
            sm.AddSystemMsg($"✘ 错误: {ex.Message}");
        }

        // 更新右下角 token 显示
        sm.UpdateTokenDisplay(
            _llm!.TotalPromptTokens, _llm.TotalCompletionTokens,
            _llm.EstimatedCost,
            ContextManager.EstimateTokens(_agent!.Messages), _config.MaxContextTokens);
        sm.Render();
    }

    // ---- 内置命令的聊天内联版本 ----
    private static void ShowHelpInChat(ScreenManager sm)
    {
        sm.AddSystemMsg("帮助: /help /reset /model /tokens /compact /diff /save /sessions /permissions /perm /plan /todo /git-status /git-log /review /lint /search /edit /repomap /pr quit");
    }
    private static void ShowTokensInChat(ScreenManager sm)
    {
        var p = _llm!.TotalPromptTokens; var c = _llm!.TotalCompletionTokens;
        sm.AddSystemMsg($"Token: {p:N0} 输入 + {c:N0} 输出 = {(p + c):N0} 总计");
    }
    private static void SaveSessionInChat(ScreenManager sm)
    {
        var sid = SessionManager.SaveSession(_agent!.Messages, _config.Model);
        sm.AddSystemMsg($"✔ 会话已保存: {sid}");
    }
    private static void SwitchModelInline(string input, ScreenManager sm)
    {
        var m = input[7..].Trim();
        if (!string.IsNullOrEmpty(m)) { _llm!.Model = m; _config.Model = m; sm.StatusLeft = m; sm.AddSystemMsg($"已切换到: {m}"); }
    }
    private static void ShowPermStatusInChat(ScreenManager sm)
    {
        var mode = PermissionManager.CurrentMode.ToString();
        sm.AddSystemMsg($"权限模式: {mode} (危险工具需确认: bash/write/edit/agent/kill/rm)");
    }
    private static void ShowSessionsInChat(ScreenManager sm)
    {
        var sessions = SessionManager.ListSessions();
        if (sessions.Count == 0) sm.AddSystemMsg("没有已保存的会话");
        else foreach (var s in sessions) sm.AddSystemMsg($"{s.Id}  {s.Model}  {s.SavedAt}");
    }
    private static void ShowDiffInChat(ScreenManager sm)
    {
        var files = EditFileTool.ChangedFiles;
        if (files.Count == 0) sm.AddSystemMsg("未修改任何文件");
        else foreach (var f in files) sm.AddSystemMsg($"  {f}");
    }

    // ========================================================================
    // 命令实现
    // ========================================================================

    private static void ShowUsage()
    {
        MarkupLine("[bold yellow]CoreCoderSharp[/] — 极简 AI 编程智能体");
        Console.WriteLine();
        MarkupLine("[bold]使用方法:[/] [cyan]corecoder [[选项]][/]");
        Console.WriteLine();
        MarkupLine("  [bold]选项:[/]");
        MarkupLine("  [cyan]-m, --model[/] <名称>   模型名称 (默认: deepseek-v4-flash)");
        MarkupLine("  [cyan]--base-url[/] <URL>     API 基础 URL");
        MarkupLine("  [cyan]--api-key[/] <密钥>     API 密钥");
        MarkupLine("  [cyan]-p, --prompt[/] <文本>  一次性提示词 (非交互模式)");
        MarkupLine("  [cyan]-r, --resume[/] <ID>    恢复已保存的会话");
        MarkupLine("  [cyan]-v, --version[/]        显示版本信息");
        MarkupLine("  [cyan]-t, --test[/]           运行自测");
        MarkupLine("  [cyan]--debug[/]              开启调试日志 (记录到 logs/ 目录)");
        MarkupLine("  [cyan]--yolo[/]              跳过所有权限确认 (非交互模式必备)");
        MarkupLine("  [cyan]--max-budget-usd[/] <金额> 费用上限（美元），超支自动停止");
        MarkupLine("  [cyan]-h, --help[/]           显示此帮助");
        Console.WriteLine();
        MarkupLine("  [bold]示例:[/]");
        MarkupLine("  [dim]$[/] corecoder                                     [dim]# 交互式 REPL[/]");
        MarkupLine("  [dim]$[/] corecoder [cyan]-p[/] [green]\"列出当前目录\"[/]               [dim]# 一次性模式[/]");
        MarkupLine("  [dim]$[/] corecoder [cyan]-m[/] deepseek-v4-pro             [dim]# 指定模型[/]");
        MarkupLine("  [dim]$[/] corecoder [cyan]-t[/]                              [dim]# 运行自测[/]");
    }

    private static void ShowHelp()
    {
        var table = new TuiTable("命令");
        table.AddColumn("命令");
        table.AddColumn("说明");

        // 内置命令
        table.AddRow("/help", "显示此帮助");
        table.AddRow("/reset", "清空对话历史");
        table.AddRow("/model", "显示当前模型");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/model[/] [dim]&lt;名称&gt;[/]", "切换模型");
        table.AddRow("/tokens", "显示 Token 用量");
        table.AddRow("/compact", "压缩上下文");
        table.AddRow("/diff", "修改文件列表");
        table.AddRow("/save", "保存会话");
        table.AddRow("/sessions", "已保存的会话");
        table.AddRow("/debug-on / -off", "开启/关闭调试日志");
        table.AddRow("/permissions", "权限管理");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/perm[/] [dim]&lt;ask|auto|yolo&gt;[/]", "设置权限模式");
        table.AddRow("/plan", "计划模式");
        table.AddRow("/todo", "查看任务列表");
        table.AddRow("/git-status", "Git 状态");
        table.AddRow("/git-log", "Git 日志");
        table.AddRow("/git-diff", "Git 差异");
        table.AddRow("/review", "代码审查");
        table.AddRow("/lint", "运行 lint 检查");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/search[/] [dim]&lt;关键词&gt;[/]", "网页搜索");
        table.AddRow("/checkpoint", "创建检查点");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/undo[/] [dim][[编号]][/]", "回退检查点");
        table.AddRow("/checkpoints", "列出检查点");
        table.AddRow("/repomap", "刷新仓库地图");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/pr[/] [dim][[标题]][/]", "创建 Pull Request");
        table.AddMarkupRow($"[{TuiColors.AccentMarkup}]/edit[/] [dim][[文件]][/]", "终端源码编辑器");
        table.AddRow("/settings", "设置界面");
        table.AddRow("quit", "退出");

        // 自定义命令
        if (CustomCommands.Commands.Count > 0)
        {
            foreach (var (name, cmd) in CustomCommands.Commands)
            {
                var desc = cmd.Description.Length > 20
                    ? cmd.Description[..17] + "..."
                    : cmd.Description;
                table.AddRow($"/{name}", desc);
            }
        }

        table.Render();
    }

    private static void ShowTokens()
    {
        var p = _llm!.TotalPromptTokens;
        var c = _llm!.TotalCompletionTokens;
        var total = p + c;
        var cost = _llm.EstimatedCost;

        var content = new StringBuilder();
        content.AppendLine($"[{TuiColors.AccentMarkup}]{p:N0}[/] 输入 " +
            $"+ [{TuiColors.AccentMarkup}]{c:N0}[/] 输出 " +
            $"= [bold {TuiColors.SuccessMarkup}]{total:N0}[/] 总计");
        if (cost != null)
            content.Append($"约 [dim]${cost:F4}[/]");

        TuiBox.Info("Token 用量", content.ToString().TrimEnd());
    }

    private static void SwitchModel(string input)
    {
        var newModel = input[7..].Trim();
        if (!string.IsNullOrEmpty(newModel))
        {
            _llm!.Model = newModel;
            _config.Model = newModel;
            MarkupLine($"[green]✔ 已切换到:[/] [bold cyan]{E(newModel)}[/]");
        }
    }

    private static async Task CompactAsync()
    {
        var before = ContextManager.EstimateTokens(_agent!.Messages);
        var compressed = await _agent.Context.MaybeCompressAsync(_agent.Messages, _llm);
        var after = ContextManager.EstimateTokens(_agent.Messages);
        if (compressed)
            MarkupLine($"[green]✔ 已压缩:[/] {before:N0} → [cyan]{after:N0}[/] tokens ([dim]{_agent.Messages.Count} 条消息[/])");
        else
            MarkupLine($"[dim]无需压缩 ({before:N0} tokens, {_agent.Messages.Count} 条消息)[/]");
    }

    private static void SaveSession()
    {
        var sid = SessionManager.SaveSession(_agent!.Messages, _config.Model);
        MarkupLine($"[green]✔ 会话已保存:[/] [cyan]{E(sid)}[/]");
        MarkupLine($"[dim]恢复命令: corecoder -r {E(sid)}[/]");
    }

    private static void ShowDiff()
    {
        var files = EditFileTool.ChangedFiles;
        if (files.Count == 0)
        {
            MarkupLine("[dim]未修改任何文件[/]");
            return;
        }

        var table = new TuiTable($"修改的文件 ({files.Count} 个)");
        table.AddColumn("文件路径");
        foreach (var f in files.OrderBy(f => f))
            table.AddRow(f);
        table.Render();
    }

    // ========================================================================
    // 输入触发式智能提示
    // ========================================================================

    /// <summary>/ 触发：弹出命令面板，用方向键选择，回车执行</summary>
    private static string ShowCommandPalette()
    {
        var commands = new List<string>
        {
            "/help", "/reset", "/model", "/model <名称>", "/tokens",
            "/compact", "/diff", "/save", "/sessions",
            "/debug-on", "/debug-off", "/permissions", "/perm <ask|auto|yolo>",
            "/plan", "/todo", "/git-status", "/git-log", "/git-diff",
            "/review", "/lint", "/search <关键词>",
            "/checkpoint", "/undo [编号]", "/checkpoints",
            "/repomap", "/pr [标题]", "/edit [文件]", "/settings", "quit",
        };

        // 追加自定义命令
        foreach (var (name, _) in CustomCommands.Commands)
            commands.Add($"/{name}");

        var choice = TuiList.Select("命令面板 ↑↓ 选择 Enter 执行 Esc 取消", commands);
        if (choice == null) return "";

        // 对于带参数的命令，截取命令名
        var spaceIdx = choice.IndexOf(' ');
        return spaceIdx > 0 ? choice[..spaceIdx] : choice;
    }

    /// <summary>! 触发：输入 Shell 命令并立即执行</summary>
    private static async Task<string> RunShellOnceAsync()
    {
        var cmd = TuiPrompt.Ask("! 命令");
        if (string.IsNullOrWhiteSpace(cmd)) return "";

        try
        {
            var result = await new Tools.BashTool().ExecuteAsync(
                new Dictionary<string, object?> { ["command"] = cmd });
            Console.WriteLine(result);
        }
        catch (Exception ex)
        {
            TuiBox.Error("Shell 错误", ex.Message);
        }
        return ""; // 不回传给 Agent
    }

    /// <summary># 触发：显示工程文件列表，辅助输入</summary>
    private static async Task<string> ShowFileHintAsync()
    {
        // 快速显示目录结构
        try
        {
            var treeResult = await new Tools.TreeTool().ExecuteAsync(
                new Dictionary<string, object?> { ["depth"] = 2, ["max"] = 30 });
            // 只显示前 20 行
            var lines = treeResult.Split('\n').Take(20);
            AnsiConsole.MarkupLine($"[{TuiColors.DimMarkup}]── 工程文件 (前 20 行) ──[/]");
            foreach (var line in lines)
                Console.WriteLine($"  [{TuiColors.DimMarkup}]{TuiHelper.Esc(line)}[/]");
            Console.WriteLine();
        }
        catch { /* 静默失败 */ }

        var file = TuiPrompt.Ask("# 文件");
        return string.IsNullOrWhiteSpace(file) ? "" : $"#{file}";
    }

    private static void ShowSessions()
    {
        var sessions = SessionManager.ListSessions();
        if (sessions.Count == 0)
        {
            MarkupLine("[dim]没有已保存的会话[/]");
            return;
        }

        var table = new TuiTable($"已保存的会话 ({sessions.Count} 个)");
        table.AddColumn("ID", 12);
        table.AddColumn("模型", 20);
        table.AddColumn("保存时间", 20);
        table.AddColumn("预览");

        foreach (var s in sessions)
            table.AddRow(s.Id, s.Model, s.SavedAt, s.Preview);

        table.Render();
    }

    // ========================================================================
    // 计划模式 / Todo / Git 命令
    // ========================================================================

    private static async Task PlanModeAsync()
    {
        MarkupLine("[bold cyan]📋 计划模式[/] — Agent 将先规划再执行");
        MarkupLine("[dim]输入你的需求，Agent 会先分析并列出执行计划[/]");
        Console.WriteLine();

        var userInput = TuiInput.ReadInput();
        if (string.IsNullOrWhiteSpace(userInput)) return;

        var planPrompt = $"请分析以下需求，先列出详细的执行计划（分步骤、涉及的文件、可能的风险），再逐步执行。\n\n需求：{userInput}\n\n请先输出计划，然后逐步执行。";

        using var cts = new CancellationTokenSource();
        try
        {
            await ChatWithStatusAsync(planPrompt, cts.Token);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            TuiBox.Error("错误", ex.Message);
        }
    }

    private static void ShowTodo()
    {
        var items = TodoTool.Items;
        if (items.Count == 0)
        {
            MarkupLine("[dim]（暂无任务）[/]");
            return;
        }

        var completed = items.Count(i => i.Status == "completed");
        var table = new TuiTable($"任务列表 ({completed}/{items.Count} 完成)");
        table.AddColumn("#", 5);
        table.AddColumn("状态", 14);
        table.AddColumn("标题");

        var statusMarkup = new Dictionary<string, string>
        {
            ["completed"] = $"[{TuiColors.SuccessMarkup}]✅ 已完成[/]",
            ["in_progress"] = $"[{TuiColors.AccentMarkup}]🔄 进行中[/]",
            ["pending"] = $"[{TuiColors.WarnMarkup}]⏳ 待处理[/]",
            ["cancelled"] = $"[{TuiColors.DimMarkup}]❌ 已取消[/]",
        };

        foreach (var item in items.OrderBy(i => i.Id))
        {
            var status = statusMarkup.GetValueOrDefault(item.Status, $"❓ {item.Status}");
            table.AddMarkupRow($"#{item.Id}", status, TuiHelper.Esc(item.Title));
        }

        table.Render();
    }

    private static async Task RunReviewAsync()
    {
        var reviewPrompt = ReviewMode.BuildReviewPrompt();
        if (reviewPrompt.StartsWith("（没有修改过"))
        {
            MarkupLine($"[dim]{E(reviewPrompt)}[/]");
            return;
        }

        MarkupLine("[bold cyan]🔍 代码审查中...[/]");
        using var cts = new CancellationTokenSource();
        try
        {
            await ChatWithStatusAsync(reviewPrompt, cts.Token);
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            TuiBox.Error("审查出错", ex.Message);
        }
    }

    private static async Task RunGitAsync(string command)
    {
        MarkupLine($"[dim]git {E(command)}[/]");
        var result = await new Tools.GitTool().ExecuteAsync(new() { ["command"] = command });
        Console.WriteLine(result);
    }

    // ========================================================================
    // Lint / Search / Checkpoint 命令 (v0.6.1+)
    // ========================================================================

    private static async Task RunLintAsync()
    {
        MarkupLine("[bold cyan]🔍 Lint 检查中...[/]");
        var lintTool = new Tools.LintTool();
        var result = await lintTool.ExecuteAsync(new Dictionary<string, object?>());
        Console.WriteLine(result);
    }

    private static async Task RunSearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            MarkupLine("[orange3]用法: /search <关键词>[/]");
            return;
        }
        MarkupLine($"[bold cyan]🔍 搜索: {E(query)}[/]");
        var searchTool = new Tools.WebSearchTool();
        var result = await searchTool.ExecuteAsync(new Dictionary<string, object?> { ["query"] = query });
        Console.WriteLine(result);
    }

    private static async Task CreateCheckpointAsync()
    {
        MarkupLine("[bold cyan]📦 创建检查点...[/]");
        var cp = await CheckpointManager.CreateAsync("手动创建");
        if (cp != null)
            MarkupLine($"[green]✔ 检查点 #{cp.Id} 已创建[/] [dim]({cp.Type})[/]");
        else
            MarkupLine("[red]✘ 检查点创建失败[/]");
    }

    private static async Task UndoCheckpointAsync(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int? id = null;
        if (parts.Length > 1 && int.TryParse(parts[1], out var parsed))
            id = parsed;

        MarkupLine("[bold orange3]⏪ 回退中...[/]");
        var result = await CheckpointManager.UndoAsync(id);
        Console.WriteLine(result);
    }

    private static void ShowCheckpoints()
    {
        MarkupLine("[bold]检查点列表:[/]");
        Console.WriteLine(CheckpointManager.ListCheckpoints());
    }

    private static void ShowRepoMap()
    {
        MarkupLine("[bold]仓库地图 (刷新中...)[/]");
        RepoMapGenerator.Invalidate();
        var map = RepoMapGenerator.Generate();
        Console.WriteLine(map);
    }

    private static async Task RunPRAsync(string input)
    {
        var parts = input.Split(' ', 2);
        var title = parts.Length > 1 ? parts[1].Trim() : "";
        if (string.IsNullOrEmpty(title))
        {
            // 仅显示 PR 链接
            var prTool = new GitPRTool();
            var result = await prTool.ExecuteAsync(new Dictionary<string, object?> { ["action"] = "url" });
            Console.WriteLine(result);
            return;
        }
        else
        {
            var prTool = new GitPRTool();
            var result = await prTool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["action"] = "create",
                ["title"] = title,
                ["description"] = $"🤖 Generated with [CoreCoder](https://github.com/alecksty/corecoder)"
            });
            Console.WriteLine(result);
        }
    }

    // ========================================================================
    // 辅助方法: 安全的 Spectre.Console 输出 + 状态动画
    // ========================================================================

    /// <summary>转义用户内容中的 Spectre 标记字符</summary>
    private static string E(string? text) => Markup.Escape(text ?? "");

    /// <summary>输出带标记的行（纯静态标记，无动态内容）</summary>
    private static void MarkupLine(string markup) => AnsiConsole.MarkupLine(markup);

    /// <summary>输出带标记的文本（不换行）</summary>
    private static void M(string markup) => AnsiConsole.Markup(markup);

    /// <summary>
    /// 带旋转动画 + 超时提示的 ChatAsync 包装器。
    /// 等待 LLM 时显示 "⠋ 思考中..." 旋转动画，网络卡顿无响应时有进度提示。
    /// </summary>
    private static async Task<string> ChatWithStatusAsync(
        string userInput,
        CancellationToken ct,
        Action<bool>? setStreamed = null)
    {
        // ANSI 控制序列 (显式 ESC 字节, 兼容所有终端)
        const string DimOn = "[2m";
        const string DimOff = "[0m";
        const string ClearLine = "[2K";
        var spinnerFrames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var spinnerActive = false;
        var startTime = DateTime.UtcNow;
        CancellationTokenSource? spinnerCts = null;

        void StartSpinner()
        {
            if (spinnerActive) return;
            spinnerActive = true;
            startTime = DateTime.UtcNow;
            spinnerCts = new CancellationTokenSource();
            var token = spinnerCts.Token;
            _ = Task.Run(async () =>
            {
                var i = 0;
                while (!token.IsCancellationRequested)
                {
                    var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                    var frame = spinnerFrames[i % spinnerFrames.Length];
                    string status;
                    if (elapsed > 60)
                        status = $"{frame} 响应缓慢, 请耐心等待... ({elapsed:F0}s)";
                    else if (elapsed > 30)
                        status = $"{frame} 等待响应中... ({elapsed:F0}s)";
                    else if (elapsed > 15)
                        status = $"{frame} 思考中... ({elapsed:F0}s)";
                    else
                        status = $"{frame} 思考中...";

                    // 清行 + 回行首 + 动画帧 (直接写 stdout，绕过 Spectre 管线)
                    System.Console.Write($"\r{ClearLine}  {DimOn}{status}{DimOff}");
                    System.Console.Out.Flush();
                    i++;
                    try { await Task.Delay(120, token); }
                    catch (OperationCanceledException) { break; }
                }
            }, token);
        }

        void StopSpinner()
        {
            if (!spinnerActive) return;
            spinnerActive = false;
            spinnerCts?.Cancel();
            Console.Write("\r" + new string(' ', 60) + "\r");
            Console.Out.Flush();
        }

        // 初始动画
        StartSpinner();

        var response = await _agent!.ChatAsync(userInput,
            onToken: tok =>
            {
                StopSpinner();
                Console.Write(tok);
                if (setStreamed != null) setStreamed(true);
            },
            onTool: (name, brief) =>
            {
                StopSpinner();
                Console.WriteLine(); // 结束上一行流式输出
                var shortBrief = brief.Length > 60 ? brief[..57] + "..." : brief;
                AnsiConsole.MarkupLine($"  [dim]🔧 {E(name)}({E(shortBrief)})[/]");
                StartSpinner(); // 下一轮 LLM 等待
            },
            cancellationToken: ct);

        // 清除最后一轮动画
        StopSpinner();

        return response;
    }
}

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

        if (showVersion) { Console.WriteLine("CoreCoderSharp v0.10.0"); return 0; }

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
            MarkupLine("\n[orange3]⚠ 已中断[/]");
            Environment.Exit(130);
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
        // 欢迎横幅
        TuiBanner.Show("CoreCoder", "0.10.0", _config.Model,
            _config.BaseUrl, DebugLog.Enabled);

        while (true)
        {
            string? userInput;
            try
            {
                userInput = AnsiConsole.Prompt(
                    new TextPrompt<string>("[bold cyan]You[/] > ")
                        .AllowEmpty()
                        .PromptStyle(new Style(foreground: Color.White)));
            }
            catch (OperationCanceledException)
            {
                MarkupLine("\n[dim]👋 再见![/]");
                break;
            }

            userInput = userInput?.Trim() ?? "";
            if (string.IsNullOrEmpty(userInput)) continue;

            var lower = userInput.ToLowerInvariant();
            if (lower is "quit" or "exit" or "/quit" or "/exit") break;

            // 内置命令
            if (userInput == "/help") { ShowHelp(); continue; }
            if (userInput == "/reset") { _agent!.Reset(); MarkupLine("[orange3]♻ 对话已重置[/]"); continue; }
            if (userInput == "/tokens") { ShowTokens(); continue; }
            if (userInput == "/model") { MarkupLine($"[dim]当前模型:[/] [green]{E(_config.Model)}[/]"); continue; }
            if (userInput.StartsWith("/model ")) { SwitchModel(userInput); continue; }
            if (userInput == "/compact") { await CompactAsync(); continue; }
            if (userInput == "/save") { SaveSession(); continue; }
            if (userInput == "/diff") { ShowDiff(); continue; }
            if (userInput == "/sessions") { ShowSessions(); continue; }
            if (userInput == "/debug-on") { DebugLog.Enable(); MarkupLine("[green]✔ 调试日志已开启[/] [dim](logs/ 目录)[/]"); continue; }
            if (userInput == "/debug-off") { DebugLog.Disable(); MarkupLine("[orange3]✔ 调试日志已关闭[/]"); continue; }
            if (userInput == "/permissions" || userInput == "/perm") { PermissionManager.ShowStatus(); continue; }
            if (userInput.StartsWith("/perm ")) { PermissionManager.SetMode(userInput[6..].Trim()); continue; }
            if (userInput == "/plan") { await PlanModeAsync(); continue; }
            if (userInput == "/todo") { ShowTodo(); continue; }
            if (userInput == "/git-status") { await RunGitAsync("status"); continue; }
            if (userInput == "/git-log") { await RunGitAsync("log --oneline -20"); continue; }
            if (userInput == "/git-diff") { await RunGitAsync("diff"); continue; }
            if (userInput == "/jobs") { Console.WriteLine(BackgroundTaskManager.ListTasks()); continue; }
            if (userInput.StartsWith("/job-output ")) { Console.WriteLine(BackgroundTaskManager.GetOutput(int.TryParse(userInput[12..].Trim(), out var jid) ? jid : -1)); continue; }
            if (userInput == "/memory") { Console.WriteLine(MemoryStore.Read()); continue; }
            if (userInput == "/review") { await RunReviewAsync(); continue; }
            if (userInput == "/lint") { await RunLintAsync(); continue; }
            if (userInput.StartsWith("/search ")) { await RunSearchAsync(userInput[8..].Trim()); continue; }
            if (userInput == "/checkpoint") { await CreateCheckpointAsync(); continue; }
            if (userInput.StartsWith("/undo")) { await UndoCheckpointAsync(userInput); continue; }
            if (userInput == "/checkpoints") { ShowCheckpoints(); continue; }
            if (userInput == "/repomap" || userInput == "/map") { ShowRepoMap(); continue; }
            if (userInput.StartsWith("/pr")) { await RunPRAsync(userInput); continue; }

            if (userInput.StartsWith('/'))
            {
                // 检查自定义命令
                var cmdParts = userInput.Split(' ', 2);
                var cmdName = cmdParts[0][1..]; // 去掉前导 /
                if (CustomCommands.Commands.ContainsKey(cmdName))
                {
                    var args = cmdParts.Length > 1 ? cmdParts[1] : "";
                    var (content, replace) = CustomCommands.Execute(cmdName, args, _agent!);
                    if (replace)
                    {
                        // 命令输出替换用户输入
                        userInput = content;
                    }
                    else
                    {
                        MarkupLine($"[dim]📋 /{E(cmdName)}[/]");
                        Console.WriteLine(content);
                        // 将命令输出注入到 Agent 上下文
                        var cmdMsg = $"[命令 /{cmdName} 输出]\n{content}";
                        try
                        {
                            using var cts2 = new CancellationTokenSource();
                            await ChatWithStatusAsync(cmdMsg, cts2.Token);
                            Console.WriteLine();
                        }
                        catch (Exception ex)
                        {
                            MarkupLine($"[red]错误: {E(ex.Message)}[/]");
                        }
                    }
                    continue;
                }

                MarkupLine($"[orange3]未知命令: {E(userInput.Split()[0])}[/] [dim](输入 /help 查看帮助)[/]");
                continue;
            }

            // 调用智能体
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                var streamed = false;
                var response = await ChatWithStatusAsync(userInput, cts.Token,
                    setStreamed: s => streamed = s);

                if (streamed) Console.WriteLine();
                else if (!string.IsNullOrEmpty(response)) Console.WriteLine(response);
            }
            catch (OperationCanceledException)
            {
                MarkupLine("\n[orange3]⚠ 已中断[/]");
            }
            catch (Exception ex)
            {
                TuiBox.Error("错误", ex.Message);
            }
        }
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

        var userInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold cyan]需求[/] > ").AllowEmpty());
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
    /// 带状态动画的 ChatAsync 包装器。
    /// 等待 LLM 时显示 "⏳ 思考中..."，首 token 到达后清除，
    /// 工具调用时显示 "🔧 工具名"，调用完成后恢复 "⏳ 思考中..."。
    /// </summary>
    private static async Task<string> ChatWithStatusAsync(
        string userInput,
        CancellationToken ct,
        Action<bool>? setStreamed = null)
    {
        var thinking = true;
        var statusText = $"  [dim]⏳ 思考中...[/]";

        void ClearStatus()
        {
            if (thinking)
            {
                Console.Write("\r" + new string(' ', 40) + "\r");
                thinking = false;
            }
        }

        void ShowStatus()
        {
            if (!thinking)
            {
                AnsiConsole.Markup(statusText);
                thinking = true;
            }
        }

        // 初始状态
        AnsiConsole.Markup(statusText);

        var response = await _agent!.ChatAsync(userInput,
            onToken: tok =>
            {
                ClearStatus();
                Console.Write(tok);
                if (setStreamed != null) setStreamed(true);
            },
            onTool: (name, brief) =>
            {
                // 不换行清除状态行
                if (thinking)
                    Console.Write("\r" + new string(' ', 40) + "\r");
                else
                    Console.WriteLine(); // 结束上一行流式输出
                thinking = false;

                var shortBrief = brief.Length > 60 ? brief[..57] + "..." : brief;
                AnsiConsole.MarkupLine($"  [dim]🔧 {E(name)}({E(shortBrief)})[/]");

                // 为下一轮 LLM 调用显示状态
                ShowStatus();
                thinking = true;
            },
            cancellationToken: ct);

        // 清除最后一轮的状态
        if (thinking)
            Console.Write("\r" + new string(' ', 40) + "\r");

        return response;
    }
}

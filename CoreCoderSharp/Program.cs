using System.Text;
using CoreCoderSharp.Tools;
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
        bool showVersion = false;

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
                case "-h" or "--help": ShowUsage(); return 0;
            }
        }

        if (showVersion) { Console.WriteLine("CoreCoderSharp v0.2.0"); return 0; }

        _config = Config.FromEnv();
        if (model != null) _config.Model = model;
        if (baseUrl != null) _config.BaseUrl = baseUrl;
        if (apiKey != null) _config.ApiKey = apiKey;

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
        _agent = new Agent(_llm, maxContextTokens: _config.MaxContextTokens);

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
            await _agent!.ChatAsync(prompt,
                onToken: tok => Console.Write(tok),
                onTool: (name, brief) => MarkupLine($"  [dim grey]⚙ {E(name)}({E(brief)})[/]"),
                cancellationToken: cts.Token);
            Console.WriteLine();
        }
        catch (OperationCanceledException)
        {
            MarkupLine("\n[orange3]⚠ 已中断[/]");
            Environment.Exit(130);
        }
        catch (Exception ex)
        {
            MarkupLine($"\n[red]✘ 错误: {E(ex.Message)}[/]");
            Environment.Exit(1);
        }
    }

    // ========================================================================
    // 交互式 REPL
    // ========================================================================

    private static async Task RunReplAsync()
    {
        // 彩色欢迎横幅 + ASCII Art
        MarkupLine("");
        MarkupLine("  [bold cyan]╔═══════════════════════════════════════════╗[/]");
        MarkupLine("  [bold cyan]║[/]  [bold yellow]   ██████╗ ██████╗ ██████╗ ███████╗   [/]  [bold cyan]║[/]");
        MarkupLine("  [bold cyan]║[/]  [bold yellow]  ██╔════╝██╔═══██╗██╔══██╗██╔════╝   [/]  [bold cyan]║[/]");
        MarkupLine("  [bold cyan]║[/]  [bold yellow]  ██║     ██║   ██║██████╔╝█████╗     [/]  [bold cyan]║[/]");
        MarkupLine("  [bold cyan]║[/]  [bold yellow]  ██║     ██║   ██║██╔══██╗██╔══╝     [/]  [bold cyan]║[/]");
        MarkupLine("  [bold cyan]║[/]  [bold yellow]  ╚██████╗╚██████╔╝██║  ██║███████╗   [/]  [bold cyan]║[/]");
        MarkupLine("  [bold cyan]║[/]  [bold yellow]   ╚═════╝ ╚═════╝ ╚═╝  ╚═╝╚══════╝   [/]  [bold cyan]║[/]");
        MarkupLine("  [bold cyan]║[/]                                         [bold cyan]║[/]");
        MarkupLine("  [bold cyan]║[/]  [bold]CoreCoder[/] [dim]v0.2.0[/]  ·  AI 编程智能体       [bold cyan]║[/]");
        MarkupLine($"  [bold cyan]║[/]  模型: [green]{E(_config.Model)}[/]                        [bold cyan]║[/]");
        if (_config.BaseUrl != null)
            MarkupLine($"  [bold cyan]║[/]  API: [dim]{E(_config.BaseUrl)}[/] [bold cyan]║[/]");
        MarkupLine("  [bold cyan]║[/]  [dim]/help 帮助  quit 退出  Ctrl+C 取消[/]  [bold cyan]║[/]");
        if (DebugLog.Enabled)
            MarkupLine("  [bold cyan]║[/]  [bold orange3]🐛 DEBUG 模式已开启 → logs/ 目录[/]    [bold cyan]║[/]");
        MarkupLine("  [bold cyan]╚═══════════════════════════════════════════╝[/]");
        Console.WriteLine();

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

            if (userInput.StartsWith('/'))
            {
                MarkupLine($"[orange3]未知命令: {E(userInput.Split()[0])}[/] [dim](输入 /help 查看帮助)[/]");
                continue;
            }

            // 调用智能体
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                var streamed = false;
                var response = await _agent!.ChatAsync(userInput,
                    onToken: tok => { Console.Write(tok); streamed = true; },
                    onTool: (name, brief) => MarkupLine($"  [dim grey]⚙ {E(name)}({E(brief)})[/]"),
                    cancellationToken: cts.Token);

                if (streamed) Console.WriteLine();
                else if (!string.IsNullOrEmpty(response)) Console.WriteLine(response);
            }
            catch (OperationCanceledException)
            {
                MarkupLine("\n[orange3]⚠ 已中断[/]");
            }
            catch (Exception ex)
            {
                MarkupLine($"\n[red]✘ 错误: {E(ex.Message)}[/]");
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
        MarkupLine("[bold yellow]╭─ 命令 ─────────────────────────╮[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/help[/]          显示此帮助   [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/reset[/]         清空对话历史  [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/model[/]         显示当前模型  [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/model[/] [dim]<名称>[/]  切换模型     [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/tokens[/]        显示 Token 用量[b][bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/compact[/]       压缩上下文    [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/diff[/]          修改文件列表  [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/save[/]          保存会话      [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/sessions[/]      已保存的会话  [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/debug-on[/]      开启调试日志  [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/debug-off[/]     关闭调试日志  [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/permissions[/]   权限管理      [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/perm[/] [dim]<ask|auto|yolo>[/]  设置权限模式 [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/plan[/]          计划模式      [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/todo[/]          查看任务列表  [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/git-status[/]    Git 状态      [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/git-log[/]       Git 日志      [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]/git-diff[/]      Git 差异      [bold yellow]│[/]");
        MarkupLine("[bold yellow]│[/] [cyan]quit[/]           退出          [bold yellow]│[/]");
        MarkupLine("[bold yellow]╰────────────────────────────────╯[/]");
    }

    private static void ShowTokens()
    {
        var p = _llm!.TotalPromptTokens;
        var c = _llm!.TotalCompletionTokens;
        var total = p + c;
        M($"[bold]Token 用量:[/] ");
        M($"[cyan]{p:N0}[/] 输入 + [cyan]{c:N0}[/] 输出 = [bold green]{total:N0}[/] 总计");

        var cost = _llm.EstimatedCost;
        if (cost != null)
            M($"  [dim]约 ${cost:F4}[/]");
        Console.WriteLine();
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
        }
        else
        {
            MarkupLine($"[bold]修改的文件 ([green]{files.Count}[/] 个):[/]");
            foreach (var f in files.OrderBy(f => f))
                MarkupLine($"  [cyan]{E(f)}[/]");
        }
    }

    private static void ShowSessions()
    {
        var sessions = SessionManager.ListSessions();
        if (sessions.Count == 0)
        {
            MarkupLine("[dim]没有已保存的会话[/]");
        }
        else
        {
            MarkupLine($"[bold]已保存的会话 ([green]{sessions.Count}[/] 个):[/]");
            foreach (var s in sessions)
                MarkupLine($"  [cyan]{E(s.Id)}[/] [dim]{E(s.Model)}  {E(s.SavedAt)}[/]  {E(s.Preview)}");
        }
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
            var response = await _agent!.ChatAsync(planPrompt,
                onToken: tok => Console.Write(tok),
                onTool: (name, brief) => MarkupLine($"  [dim grey]⚙ {E(name)}({E(brief)})[/]"),
                cancellationToken: cts.Token);
            Console.WriteLine();
            if (!string.IsNullOrEmpty(response))
                Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            MarkupLine($"[red]错误: {E(ex.Message)}[/]");
        }
    }

    private static void ShowTodo()
    {
        var items = TodoTool.Items;
        if (items.Count == 0)
        {
            MarkupLine("[dim]（暂无任务）[/]");
        }
        else
        {
            MarkupLine($"[bold]任务列表 ([green]{items.Count(i => i.Status == "completed")}[/]/{items.Count} 完成):[/]");
            var icons = new Dictionary<string, string>
            {
                ["pending"] = "⏳", ["in_progress"] = "🔄", ["completed"] = "✅", ["cancelled"] = "❌",
            };
            foreach (var item in items.OrderBy(i => i.Id))
            {
                var icon = icons.GetValueOrDefault(item.Status, "❓");
                var color = item.Status switch
                {
                    "completed" => "[green]", "in_progress" => "[cyan]", "cancelled" => "[dim]",
                    _ => "[yellow]",
                };
                MarkupLine($"  #{item.Id} {icon} {color}[{item.Status}][/] {E(item.Title)}");
            }
        }
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
            var response = await _agent!.ChatAsync(reviewPrompt,
                onToken: tok => Console.Write(tok),
                onTool: (name, brief) => MarkupLine($"  [dim grey]⚙ {E(name)}({E(brief)})[/]"),
                cancellationToken: cts.Token);
            Console.WriteLine();
            if (!string.IsNullOrEmpty(response)) Console.WriteLine(response);
        }
        catch (Exception ex)
        {
            MarkupLine($"[red]审查出错: {E(ex.Message)}[/]");
        }
    }

    private static async Task RunGitAsync(string command)
    {
        MarkupLine($"[dim]git {E(command)}[/]");
        var result = await new Tools.GitTool().ExecuteAsync(new() { ["command"] = command });
        Console.WriteLine(result);
    }

    // ========================================================================
    // 辅助方法: 安全的 Spectre.Console 输出
    // ========================================================================

    /// <summary>转义用户内容中的 Spectre 标记字符</summary>
    private static string E(string? text) => Markup.Escape(text ?? "");

    /// <summary>输出带标记的行（纯静态标记，无动态内容）</summary>
    private static void MarkupLine(string markup) => AnsiConsole.MarkupLine(markup);

    /// <summary>输出带标记的文本（不换行）</summary>
    private static void M(string markup) => AnsiConsole.Markup(markup);
}

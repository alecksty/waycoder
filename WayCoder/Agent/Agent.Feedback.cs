using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

/// <summary>
/// 核心智能体循环。这是 WayCoder 的心脏。
///
/// 模式很简单：
///   用户消息 -> LLM（带工具）-> 有工具调用？-> 执行 -> 循环
///                             -> 文本回复？-> 返回给用户
///
/// 它会持续循环，直到 LLM 回复纯文本（没有工具调用），
/// 这意味着它已完成工作并准备报告结果。
/// </summary>
public partial class Agent
{

    /// <summary>
    /// 写文件后自动运行 lint，错误注入工具结果，形成自动修复闭环。
    /// 仅对 write_file / edit_file 触发，lint 无错误则不追加。
    /// </summary>
    private async Task<string> AppendLintFeedbackAsync(ToolCall tc, string toolResult)
    {
        if (tc.Name is not "write_file" and not "edit_file")
            return toolResult;

        // 文件修改后使仓库地图缓存失效
        RepoMapGenerator.Invalidate();

        var filePath = tc.Arguments.GetValueOrDefault("file_path")?.ToString();
        if (string.IsNullOrWhiteSpace(filePath))
            return toolResult;

        try
        {
            var lang = LintTool.DetectLanguage(filePath);
            if (lang == null) return toolResult;

            var lintTool = new LintTool();
            var lintArgs = new Dictionary<string, object?> { ["path"] = filePath };
            var lintResult = await lintTool.ExecuteAsync(lintArgs);

            // 仅当 lint 发现问题时才追加反馈
            if (!lintResult.Contains("✅") && !lintResult.Contains("⚠"))
                return toolResult;

            // 截断过长输出
            if (lintResult.Length > 1500)
                lintResult = ContextManager.TruncateByRunes(lintResult, 1500) + "\n... (已截断)";

            return toolResult + $"\n\n--- Lint 自动检查 ({lang}) ---\n{lintResult}";
        }
        catch
        {
            return toolResult; // lint 失败不影响主流程
        }
    }

    /// <summary>上次自动跑测试的时间（防抖：同一项目 60s 内不重复跑）</summary>
    private static DateTime _lastTestRun;
    private static string? _lastTestProject;

    /// <summary>
    /// 写源码文件后自动运行项目测试，失败结果注入工具结果，形成自动修复闭环。
    /// </summary>
    private async Task<string> AppendTestFeedbackAsync(ToolCall tc, string toolResult)
    {
        if (tc.Name is not "write_file" and not "edit_file")
            return toolResult;

        var filePath = tc.Arguments.GetValueOrDefault("file_path")?.ToString();
        if (string.IsNullOrWhiteSpace(filePath)) return toolResult;

        // 仅源码文件触发测试
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var srcExts = new[] { ".cs", ".py", ".ts", ".js", ".go", ".rs", ".java", ".kt", ".swift", ".c", ".cpp", ".rb" };
        if (!srcExts.Contains(ext)) return toolResult;

        // 防抖：同一项目 N 秒内不重复跑
        var cwd = Directory.GetCurrentDirectory();
        if (_lastTestProject == cwd && (DateTime.UtcNow - _lastTestRun).TotalSeconds < Config.Instance.AutoTestDebounceSec)
            return toolResult;

        // DetectTestCommand 的 Directory.GetFiles(AllDirectories) 遇到不可读目录会抛
        // UnauthorizedAccessException——必须在 try/catch 内，否则崩溃整个 Agent 循环
        string? testCmd;
        try { testCmd = DetectTestCommand(); }
        catch (Exception ex)
        {
            ErrorLog.Warning("Agent", $"自动测试命令探测失败: {ex.Message}");
            testCmd = null;
        }
        if (testCmd == null) return toolResult;

        // WayCoder 自己的自测可能很慢，跳过（编辑 WayCoder 自身时）
        if (testCmd.Contains("--test") && File.Exists(Path.Combine(cwd, "SelfTest.cs")))
            return toolResult;

        try
        {
            _lastTestProject = cwd;
            _lastTestRun = DateTime.UtcNow;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = testCmd.Split(' ')[0],
                Arguments = string.Join(' ', testCmd.Split(' ').Skip(1)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return toolResult;

            // 最多等 N 秒。stdout/stderr 必须并发读：先读完 stdout 再读 stderr，
            // 子进程向 stderr 写满管道缓冲（约 4KB+）时会阻塞在写 stderr，永远不退出
            // → stdout 读任务永不完成 → 被误判超时并强杀本应通过的测试。
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            var timeoutTask = Task.Delay(Config.Instance.AutoTestTimeoutSec * 1000);
            var completed = await Task.WhenAny(Task.WhenAll(stdoutTask, stderrTask), timeoutTask);
            if (completed == timeoutTask)
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                ErrorLog.Warning("Agent", $"自动测试超时（{Config.Instance.AutoTestTimeoutSec}s），已终止进程");
                return toolResult;
            }

            var output = await stdoutTask;
            var errorOutput = await stderrTask;
            await proc.WaitForExitAsync();

            var fullOutput = output + errorOutput;
            if (proc.ExitCode == 0)
                return toolResult; // 测试通过，不追加

            // 截断
            if (fullOutput.Length > 2000)
                fullOutput = ContextManager.TruncateByRunes(fullOutput, 2000) + $"\n... (共 {fullOutput.Length} 字符)";

            return toolResult + $"\n\n--- 🔴 自动测试失败 (exit={proc.ExitCode}) ---\n{fullOutput}\n[请修复代码使测试通过]";
        }
        catch
        {
            return toolResult; // 测试失败不影响主流程
        }
    }

    /// <summary>检测当前项目的测试命令</summary>
    private static string? DetectTestCommand()
    {
        var cwd = Directory.GetCurrentDirectory();

        // WayCoder 自测 (内置 SelfTest)
        if (File.Exists(Path.Combine(cwd, "SelfTest.cs")))
            return "dotnet run -c Release -- --test";

        // .NET 测试项目
        var testProjects = Directory.GetFiles(cwd, "*.Tests.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(cwd, "*.Test.csproj", SearchOption.AllDirectories)).ToArray();
        if (testProjects.Length > 0)
            return "dotnet test --nologo -v q";

        // Node.js
        if (File.Exists(Path.Combine(cwd, "package.json")))
        {
            try
            {
                var pkg = Json.Parse(
                    File.ReadAllText(Path.Combine(cwd, "package.json")));
                if (pkg?["scripts"]?["test"] != null)
                    return "npm test --silent";
            }
            catch { }
        }

        // Go
        if (File.Exists(Path.Combine(cwd, "go.mod")))
            return "go test ./...";

        // Rust
        if (File.Exists(Path.Combine(cwd, "Cargo.toml")))
            return "cargo test -q";

        // Python
        if (Directory.GetFiles(cwd, "test_*.py", SearchOption.AllDirectories).Any() ||
            Directory.GetFiles(cwd, "*_test.py", SearchOption.AllDirectories).Any())
            return "python -m pytest -q";

        return null;
    }
}

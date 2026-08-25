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

    /// <summary>上次自动跑测试的时间（防抖：同一项目 N 秒内不重复跑）。实例级：
    /// 静态会让多槽位/多子智能体并行写文件时互相抑制测试反馈（一个跑过抑制另一个）。</summary>
    private DateTime _lastTestRun;
    private string? _lastTestProject;

    /// <summary>本轮对话是否有测试失败（硬绿判定信号：完成后若仍红则继续修复，不结束）。</summary>
    private bool _turnTestFailed;
    /// <summary>本轮硬绿判定是否已执行（每轮最多一次，防止测试无法修复时无限循环）。</summary>
    private bool _hardGreenGateDone;

    /// <summary>本轮是否已通过验证（测试 exit 0）。修完必验证门据此决定收尾前是否再验一次。</summary>
    private bool _turnVerified;
    /// <summary>本轮是否改过源码文件（write/edit 命中源码扩展名）。</summary>
    private bool _turnModifiedSource;
    /// <summary>本轮「修完必验证」门是否已执行（每轮最多一次，防验证命令反复失败死循环）。</summary>
    private bool _verifyGateDone;

    /// <summary>源码扩展名集合（触发自动测试/修完必验证的文件类型）。</summary>
    private static readonly string[] SourceExtensions =
        [".cs", ".py", ".ts", ".js", ".tsx", ".jsx", ".go", ".rs", ".java", ".kt", ".swift", ".c", ".cpp", ".rb"];

    /// <summary>判断文件路径是否为源码文件（按扩展名）。</summary>
    internal static bool IsSourceFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return SourceExtensions.Contains(ext);
    }

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
        if (!IsSourceFile(filePath)) return toolResult;

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

            var (exitCode, fullOutput) = await RunTestCommandAsync(testCmd, Config.Instance.AutoTestTimeoutSec);
            if (exitCode == null)
                return toolResult; // 超时/启动失败，不影响主流程

            if (exitCode == 0)
            {
                _turnTestFailed = false;
                _turnVerified = true; // 本轮已通过测试验证，修完必验证门不再重复验
                return toolResult; // 测试通过，不追加
            }

            _turnTestFailed = true;
            // 截断
            if (fullOutput.Length > 2000)
                fullOutput = ContextManager.TruncateByRunes(fullOutput, 2000) + $"\n... (共 {fullOutput.Length} 字符)";

            var failure = $"\n\n--- 🔴 自动测试失败 (exit={exitCode}) ---\n{fullOutput}\n[请修复代码使测试通过]";
            // 学习型智能体：召回知识库 + git 修复史中同类错误的已知解法
            try { failure += await KbIndex.DiagnoseError(fullOutput, 2); } catch { }
            return toolResult + failure;
        }
        catch
        {
            return toolResult; // 测试失败不影响主流程
        }
    }

    /// <summary>
    /// 运行测试命令，返回 (exitCode, 合并输出)。exitCode 为 null 表示超时/无法启动。
    /// stdout/stderr 必须并发读 + 超时兜底：子进程写满任一管道缓冲会阻塞 → 死锁误判超时。
    /// </summary>
    private static async Task<(int? exitCode, string output)> RunTestCommandAsync(string testCmd, int timeoutSec)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = testCmd.Split(' ')[0],
            Arguments = string.Join(' ', testCmd.Split(' ').Skip(1)),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true, // 不共享主控台 stdin（防与 TUI 主循环抢控制台输入致 ReadKey 永久阻塞）
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        if (proc == null) return (null, "");
        try { proc.StandardInput.Close(); } catch { } // stdin 置 EOF

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        var timeoutTask = Task.Delay(timeoutSec * 1000);
        var completed = await Task.WhenAny(Task.WhenAll(stdoutTask, stderrTask), timeoutTask);
        if (completed == timeoutTask)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            ErrorLog.Warning("Agent", $"测试命令超时（{timeoutSec}s），已终止进程");
            return (null, "");
        }

        // 守护子进程继承管道会让读取永不 EOF：加超时兜底
        var output = await WayCoder.Infra.ProcUtil.AwaitReadWithTimeoutAsync(stdoutTask, TimeSpan.FromSeconds(5)) ?? "";
        var errorOutput = await WayCoder.Infra.ProcUtil.AwaitReadWithTimeoutAsync(stderrTask, TimeSpan.FromSeconds(5)) ?? "";
        try { await proc.WaitForExitAsync(); } catch { }
        return (proc.ExitCode, output + errorOutput);
    }

    /// <summary>检测当前项目的测试命令。优先用户指定的 TestCommand，否则按项目类型自动探测。</summary>
    private static string? DetectTestCommand()
    {
        var cwd = Directory.GetCurrentDirectory();

        // 用户显式指定测试命令（测试驱动修复）：最高优先级
        if (!string.IsNullOrWhiteSpace(Config.Instance.TestCommand))
            return Config.Instance.TestCommand;

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

    /// <summary>
    /// 检测当前项目的构建命令（修完必验证：无测试命令时回退到构建验证）。
    /// 与 <see cref="DetectTestCommand"/> 互补——测试优先，无测试才退构建。
    /// </summary>
    internal static string? DetectBuildCommand()
    {
        var cwd = Directory.GetCurrentDirectory();

        // .NET：csproj / sln 直接构建
        try
        {
            if (Directory.GetFiles(cwd, "*.csproj", SearchOption.TopDirectoryOnly).Any() ||
                Directory.GetFiles(cwd, "*.sln", SearchOption.TopDirectoryOnly).Any())
                return "dotnet build --nologo -v q";
        }
        catch { }

        // Node.js：仅当 package.json 声明了 build 脚本
        if (File.Exists(Path.Combine(cwd, "package.json")))
        {
            try
            {
                var pkg = Json.Parse(File.ReadAllText(Path.Combine(cwd, "package.json")));
                if (pkg?["scripts"]?["build"] != null)
                    return "npm run build --silent";
            }
            catch { }
        }

        // Go
        if (File.Exists(Path.Combine(cwd, "go.mod")))
            return "go build ./...";

        // Rust
        if (File.Exists(Path.Combine(cwd, "Cargo.toml")))
            return "cargo build -q";

        return null;
    }
}

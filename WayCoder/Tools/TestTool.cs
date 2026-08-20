using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace WayCoder.Tools;

/// <summary>
/// 测试运行工具 —— 封装「跑测试 → 统计通过/失败 → 定位失败用例」闭环。
/// 支持 dotnet test / pytest / npm test / cargo test / go test 等常见框架，
/// 替代 bash 手工解析测试输出。
/// </summary>
public class TestTool : ITool
{
    public string Name => "test";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "运行测试命令并解析结果：统计通过/失败、定位失败用例。支持 dotnet test/pytest/npm test/cargo test/go test 等。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("command", JNode.Object()
                .Set("type", "string")
                .Set("description", "测试命令，如 'dotnet test --no-build'、'pytest -x'、'npm test'、'cargo test'"))
            .Set("cwd", JNode.Object()
                .Set("type", "string")
                .Set("description", "工作目录，默认当前目录"))
            .Set("timeout", JNode.Object()
                .Set("type", "integer")
                .Set("description", "超时秒数，默认 300，最大 3600")))
        .Set("required", JNode.Array().Add("command"));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var command = arguments.GetValueOrDefault("command")?.ToString() ?? "";
        var cwd = arguments.GetValueOrDefault("cwd")?.ToString();
        var timeout = Math.Clamp(ToolArgs.GetInt(arguments, "timeout", 300), 1, 3600);

        if (string.IsNullOrWhiteSpace(command))
            return "错误：请提供测试命令 (command)";

        // 安全防护：test 命令经 shell 执行，必须先过 BashGuard 黑名单（防 curl/sudo/apt 等绕过）。
        // 权限确认由 Agent 层 DangerTools（含 "test"）统一处理。
        var (blocked, reason) = BashGuard.CheckBanned(command);
        if (blocked)
            return reason ?? "⚠ 已阻止：命令违反安全策略";

        return await RunAsync(command, cwd, timeout);
    }

    private static async Task<string> RunAsync(string command, string? cwd, int timeout)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "/bin/bash",
            Arguments = isWindows ? $"/c \"{command}\"" : $"-c \"{command.Replace("\"", "\\\"")}\"",
            WorkingDirectory = string.IsNullOrWhiteSpace(cwd) ? Directory.GetCurrentDirectory() : cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var proc = Process.Start(psi)!;
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            if (!proc.WaitForExit(timeout * 1000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                return $"错误：测试命令在 {timeout} 秒后超时";
            }

            var stdout = await WayCoder.Infra.ProcUtil.AwaitReadWithTimeoutAsync(stdoutTask, TimeSpan.FromSeconds(5)) ?? "";
            var stderr = await WayCoder.Infra.ProcUtil.AwaitReadWithTimeoutAsync(stderrTask, TimeSpan.FromSeconds(5)) ?? "";
            var combined = stdout + (string.IsNullOrWhiteSpace(stderr) ? "" : "\n" + stderr);

            return BuildSummary(proc.ExitCode, combined);
        }
        catch (Exception ex)
        {
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
    }

    /// <summary>根据退出码 + 输出生成结构化摘要（纯逻辑，便于自测）。</summary>
    internal static string BuildSummary(int exitCode, string output)
    {
        var sb = new StringBuilder();
        var (passed, failed) = ExtractCounts(output);

        if (exitCode == 0)
            sb.AppendLine("✅ 测试通过（exit 0）");
        else
            sb.AppendLine($"❌ 测试失败（exit {exitCode}）");

        if (passed >= 0 || failed >= 0)
            sb.AppendLine($"统计: 通过 {Math.Max(0, passed)}，失败 {Math.Max(0, failed)}");

        // 失败用例定位
        if (exitCode != 0)
        {
            var failures = ExtractFailures(output);
            if (failures.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"失败用例（{failures.Count}）:");
                foreach (var f in failures.Take(20))
                    sb.AppendLine($"  {f}");
                if (failures.Count > 20)
                    sb.AppendLine($"  ...（共 {failures.Count} 处失败）");
            }
        }

        // 输出末尾（通常是统计摘要行）
        var lines = output.Split('\n');
        var tail = string.Join("\n", lines.Skip(Math.Max(0, lines.Length - 40)));
        if (!string.IsNullOrWhiteSpace(tail))
            sb.AppendLine($"\n--- 输出末尾 ---\n{tail.TrimEnd()}");

        return sb.ToString();
    }

    /// <summary>从测试输出提取 (通过数, 失败数)，未匹配返回 -1。</summary>
    internal static (int Passed, int Failed) ExtractCounts(string output)
    {
        int passed = -1, failed = -1;

        // pytest/jest/cargo 格式: "N passed, M failed"
        var mPassed = Regex.Match(output, @"(\d+)\s+passed", RegexOptions.IgnoreCase);
        var mFailed = Regex.Match(output, @"(\d+)\s+failed", RegexOptions.IgnoreCase);
        if (mPassed.Success) passed = int.Parse(mPassed.Groups[1].Value);
        if (mFailed.Success) failed = int.Parse(mFailed.Groups[1].Value);

        // dotnet 格式: "Failed: 0, Passed: 100"
        if (failed < 0)
        {
            var m = Regex.Match(output, @"Failed:\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success) failed = int.Parse(m.Groups[1].Value);
        }
        if (passed < 0)
        {
            var m = Regex.Match(output, @"Passed:\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success) passed = int.Parse(m.Groups[1].Value);
        }

        return (passed, failed);
    }

    /// <summary>提取失败用例行（含 FAILED/FAIL:/Error: 标记，排除统计摘要行）。</summary>
    internal static List<string> ExtractFailures(string output)
    {
        var failures = new List<string>();
        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').Trim();
            if (line.Length == 0) continue;

            var isFailureMark =
                line.Contains("FAILED", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("FAIL:", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Error:", StringComparison.OrdinalIgnoreCase);

            // 排除统计摘要行（如 "Failed: 0, Passed: 100" / "10 passed, 3 failed"）
            var isSummaryLine =
                line.Contains("Failed:", StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(line, @"\d+\s+failed", RegexOptions.IgnoreCase);

            if (isFailureMark && !isSummaryLine)
                failures.Add(line.Length > 200 ? ContextManager.TruncateByRunes(line, 200) + "..." : line);
        }
        return failures;
    }
}

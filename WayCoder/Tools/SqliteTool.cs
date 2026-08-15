using System.Diagnostics;
using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// SQLite 数据库查询工具 —— 通过命令行 sqlite3 执行 SQL。
/// 需要系统已安装 sqlite3（macOS/Linux 通常预装，Windows 需手动安装）。
/// 零依赖、跨平台，AOT 安全（不引入 native 驱动）。
/// </summary>
public class SqliteTool : ITool
{
    public string Name => "sqlite";
    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;
    public string Description => "查询 SQLite 数据库：执行 SQL（SELECT/INSERT/UPDATE/DELETE 等）返回结果。需系统安装 sqlite3 命令行工具（macOS/Linux 通常预装）。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("database", JNode.Object()
                .Set("type", "string")
                .Set("description", "SQLite 数据库文件路径（.db/.sqlite）。省略则作用于内存库"))
            .Set("query", JNode.Object()
                .Set("type", "string")
                .Set("description", "要执行的 SQL 语句")))
        .Set("required", JNode.Array().Add("query"));

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var database = arguments.GetValueOrDefault("database")?.ToString() ?? "";
        var query = arguments.GetValueOrDefault("query")?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(query))
            return "错误：请提供 SQL 查询 (query)";

        return await RunAsync(database, query);
    }

    private static async Task<string> RunAsync(string database, string query)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sqlite3",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            // database 为空时用 :memory: 占位，否则 query 会被当作数据库文件名
            psi.ArgumentList.Add(string.IsNullOrWhiteSpace(database) ? ":memory:" : database);
            psi.ArgumentList.Add("-header");
            psi.ArgumentList.Add("-column");
            psi.ArgumentList.Add(query);

            using var proc = new Process { StartInfo = psi };
            proc.Start();

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            if (!proc.WaitForExit(30_000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { }
                return "错误：SQL 执行超时（30 秒）";
            }

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (proc.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
                return $"错误：SQL 执行失败 — {stderr.Trim()}";

            return string.IsNullOrWhiteSpace(stdout) ? "（查询无结果）" : stdout.TrimEnd();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return "错误：未找到 sqlite3 命令行工具。\n" +
                   "  macOS: brew install sqlite3\n" +
                   "  Linux: apt-get install sqlite3（或 yum install sqlite）\n" +
                   "  Windows: 从 https://sqlite.org/download.html 下载 sqlite-tools 并加入 PATH";
        }
        catch (Exception ex)
        {
            return $"错误：{ex.GetType().Name}: {ex.Message}";
        }
    }
}

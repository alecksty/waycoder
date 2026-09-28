using WayCoder.Tools;
using WayCoder.UI.Shared.Terminal;

namespace WayCoder.UI.TUI.Renderers;

/// <summary>
/// Bash 工具渲染器 —— 命令头 + 退出码标记 + 输出截断提示。
///
/// <para>
/// ⚠ 本文件有四处「拿输出文案当判据」，全部要中英双认，理由同 <see cref="WriteToolRenderer"/>。
/// 其中**退出码那处最险**：前缀长度是切片偏移，中英长度不同（`[退出码：` 5 字符 /
/// `[exit code: ` 12 字符），偏移必须取自命中的那一支 —— 写死 `+5` 的话英文下会把
/// `de: 0]` 当码值、判成非 0，于是**成功的命令被标成红底**。
/// </para>
/// </summary>
public class BashToolRenderer : IToolRenderer
{
    public string ToolName => "bash";

    /// <summary>退出码标记的两个形态（生产者：<c>Infra/PersistentShell.cs</c> / <c>Tools/GitTool.cs</c>）。
    /// ⚠ 两处生产者的英文大小写**不一致**（`[exit code: ` vs `[Exit code: `），故匹配按 OrdinalIgnoreCase。</summary>
    private const string ExitZh = "[退出码：";
    private const string ExitEn = "[exit code: ";

    public string FormatHeader(string brief)
    {
        return $"💻 bash {brief}";
    }

    public string FormatOutput(string rawOutput)
    {
        if (string.IsNullOrEmpty(rawOutput)) return rawOutput;

        // 检测退出码并添加着色标记
        var result = rawOutput;

        // 给 [退出码：N] / [exit code: N] 着色：0=绿色，非0=红色
        var exitPrefix = ExitZh;
        var exitIdx = result.LastIndexOf(ExitZh, StringComparison.Ordinal);
        if (exitIdx < 0)
        {
            exitIdx = result.LastIndexOf(ExitEn, StringComparison.OrdinalIgnoreCase);
            if (exitIdx >= 0) exitPrefix = result.Substring(exitIdx, ExitEn.Length); // 取实际大小写
        }
        if (exitIdx >= 0)
        {
            var endIdx = result.IndexOf(']', exitIdx);
            if (endIdx >= 0)
            {
                var exitCodeStr = result[(exitIdx + exitPrefix.Length)..endIdx];
                var isSuccess = exitCodeStr == "0";
                var color = isSuccess ? AnsiTty.Fg(32) : AnsiTty.FgBg(37, 41); // 绿或红底
                var before = result[..exitIdx];
                var after = result[(endIdx + 1)..];
                result = before + color + result[exitIdx..(endIdx + 1)] + AnsiTty.SgrReset + after;
            }
        }

        // [stderr] 标记红色（语言中性，两语同形）
        result = result.Replace("[stderr]", $"{AnsiTty.FgBg(37, 41)}[stderr]{AnsiTty.SgrReset}");

        // 错误前缀着色：ToolErrors 前缀（同源常量）+ 安全阻止族
        // （阻止文案生产者：Infra/BashGuard.cs「⚠ 已阻止/⚠ Blocked」）
        if (result.StartsWith(ToolErrors.ZhPrefix, StringComparison.Ordinal)
            || result.StartsWith(ToolErrors.EnPrefix, StringComparison.Ordinal)
            || result.StartsWith("⚠ 已阻止", StringComparison.Ordinal)
            || result.StartsWith("⚠ Blocked", StringComparison.Ordinal))
        {
            result = AnsiTty.ErrorBlock(result);
        }

        // 无输出提示（生产者：Infra/PersistentShell.cs / Tools/GitTool.cs）
        if (result.Contains("（无输出）", StringComparison.Ordinal))
            result = result.Replace("（无输出）", $"{AnsiTty.SgrDim}（无输出）{AnsiTty.SgrReset}");
        else if (result.Contains("(no output)", StringComparison.Ordinal))
            result = result.Replace("(no output)", $"{AnsiTty.SgrDim}(no output){AnsiTty.SgrReset}");

        return result;
    }
}

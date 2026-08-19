using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.Web;
using Arguments = WayCoder.UI.Cli.Arguments;

namespace WayCoder;

/// <summary>
/// 入口 + CLI + REPL —— 面向用户的终端界面。
/// </summary>
public partial class Program
{
    // ========================================================================
    /// <summary>构建模型回退链：当前模型 + 备选模型</summary>
    private static string[] BuildFallbackChain()
    {
        var primary = _config.Model;
        var fallbacks = new List<string> { primary };
        foreach (var fb in new[] { "deepseek-v4-flash", "gpt-5.4-mini", "deepseek-v4-pro", "gpt-5.4" })
            if (fb != primary && !fallbacks.Contains(fb))
                fallbacks.Add(fb);
        return fallbacks.ToArray();
    }

    // 辅助方法: 安全的控制台输出 + 状态动画
    // ========================================================================

    /// <summary>转义用户内容中的 [ ] 标记字符</summary>
    private static string E(string? text) => AnsiHelper.Esc(text);

    /// <summary>输出带标记的行（转换 Spectre 标记为 ANSI）</summary>
    private static void MarkupLine(string markup) => Console.WriteLine(SpectreToAnsi(markup));

    /// <summary>将类 Spectre 风格标记（使用 «» 符号）转换为 ANSI 转义码（通过 AnsiText 封装层）</summary>
    /// <remarks>
    /// 与 MarkdownRenderer.MapMarkupTag 保持语义一致：复合标签（bold/bright + 颜色）
    /// 先于单标签替换，避免「bold yellow」被单标签误替换；「bold X」必须同时带粗体（SgrBold）。
    /// </remarks>
    internal static string SpectreToAnsi(string markup)
    {
        return markup
            // ── 复合标签（先替换，避免被单标签截断）──
            .Replace("«bold yellow»", AnsiTty.SgrBold + AnsiTty.FgCode(AnsiColors.Yellow))
            .Replace("«bold cyan»", AnsiTty.SgrBold + AnsiTty.FgCode(AnsiColors.Cyan))
            .Replace("«bold red»", AnsiTty.SgrBold + AnsiTty.FgCode(AnsiColors.Red))
            .Replace("«bold green»", AnsiTty.SgrBold + AnsiTty.FgCode(AnsiColors.Green))
            .Replace("«bold blue»", AnsiTty.SgrBold + AnsiTty.FgCode(AnsiColors.Blue))
            .Replace("«bold magenta»", AnsiTty.SgrBold + AnsiTty.FgCode(AnsiColors.Magenta))
            .Replace("«bold orange3»", AnsiTty.SgrBold + AnsiTty.FgCode(AnsiColors.Orange3))
            .Replace("«bright red»", AnsiTty.FgCode(AnsiColors.BrightRed))
            .Replace("«bright green»", AnsiTty.FgCode(AnsiColors.BrightGreen))
            .Replace("«bright yellow»", AnsiTty.FgCode(AnsiColors.BrightYellow))
            .Replace("«bright blue»", AnsiTty.FgCode(AnsiColors.BrightBlue))
            .Replace("«bright magenta»", AnsiTty.FgCode(AnsiColors.BrightMagenta))
            .Replace("«bright cyan»", AnsiTty.FgCode(AnsiColors.BrightCyan))
            // ── 样式标签 ──
            .Replace("«/»", AnsiTty.SgrReset)
            .Replace("«bold»", AnsiTty.SgrBold)
            .Replace("«bright»", AnsiTty.SgrBold)
            .Replace("«dim»", AnsiTty.SgrDim)
            .Replace("«italic»", AnsiTty.SgrItalic)
            .Replace("«underline»", AnsiTty.SgrUnderline)
            .Replace("«strike»", AnsiTty.Sgr(9))
            .Replace("«strikethrough»", AnsiTty.Sgr(9))
            // ── 颜色标签 ──
            .Replace("«cyan»", AnsiTty.FgCode(AnsiColors.Cyan))
            .Replace("«green»", AnsiTty.FgCode(AnsiColors.Green))
            .Replace("«yellow»", AnsiTty.FgCode(AnsiColors.Yellow))
            .Replace("«red»", AnsiTty.FgCode(AnsiColors.Red))
            .Replace("«blue»", AnsiTty.FgCode(AnsiColors.Blue))
            .Replace("«magenta»", AnsiTty.FgCode(AnsiColors.Magenta))
            .Replace("«white»", AnsiTty.FgCode(AnsiColors.White))
            .Replace("«black»", AnsiTty.FgCode(AnsiColors.Black))
            .Replace("«orange3»", AnsiTty.FgCode(AnsiColors.Orange3))
            .Replace("«orange»", AnsiTty.FgCode(AnsiColors.Orange))
            .Replace("«grey»", AnsiTty.FgCode(AnsiColors.Grey))
            .Replace("«gray»", AnsiTty.FgCode(AnsiColors.Grey));
    }

    /// <summary>
    /// 带旋转动画 + 超时提示的 ChatAsync 包装器。
    /// 等待 LLM 时显示 "⠋ 思考中..." 旋转动画，网络卡顿无响应时有进度提示。
    /// </summary>
    private static async Task<string> ChatWithStatusAsync(
        string userInput,
        CancellationToken ct,
        Action<bool>? setStreamed = null)
    {
        // ANSI 控制序列（通过 AnsiText 封装层）
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

                    // 清行 + 回行首 + 动画帧（直接写 stdout）
                    Console.Write($"\r{AnsiTty.ClearToEnd}  {AnsiTty.SgrDim}{status}");
                    await Console.Out.FlushAsync(token);
                    i++;
                    try
                    {
                        await Task.Delay(120, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        void StopSpinner()
        {
            if (!spinnerActive) return;
            spinnerActive = false;
            spinnerCts?.Cancel();
            spinnerCts?.Dispose(); // 释放 CTS，避免一次性模式反复调用时泄漏
            spinnerCts = null;
            Console.Write("\r" + new string(' ', 60) + "\r");
            Console.Out.Flush();
        }

        // 初始动画
        StartSpinner();

        var response = await _agent!.ChatAsync(userInput,
            onToken: tok =>
            {
                StopSpinner();
                // 同 CLI 模式：token 里的 «dim»/«/» 等中间格式要转成 ANSI 效果，不能裸打印
                Console.Write(SpectreToAnsi(tok));
                if (setStreamed != null) setStreamed(true);
            },
            onTool: (name, brief) =>
            {
                StopSpinner();
                Console.WriteLine(); // 结束上一行流式输出
                var shortBrief = brief.Length > 60 ? ContextManager.TruncateByRunes(brief, 57) + "..." : brief;
                MarkupLine($"  «dim»⚙ {E(name)}({E(shortBrief)})«/»");
            },
            onToolOutput: line =>
            {
                // 管道模式：逐行输出 bash 结果到控制台
                MarkupLine($"  «dim»│ {E(line)}«/»");
            },
            cancellationToken: ct);

        // 清除最后一轮动画
        StopSpinner();

        return response;
    }
}

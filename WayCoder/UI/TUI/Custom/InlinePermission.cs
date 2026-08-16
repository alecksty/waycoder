using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;

namespace WayCoder.UI.Tui;

/// <summary>
/// 行内权限确认控件 —— 对标 Crush inline permission block。
/// 在聊天流中渲染 3 行黄色背景交互块，Y/N/A 快捷键无需弹窗。
/// </summary>
public class InlinePermission : TuiControl
{
    /// <summary>工具名（e.g. "bash", "write_file"）</summary>
    public string ToolName { get; set; } = "";

    /// <summary>参数摘要（格式化的键值文本）</summary>
    public string ArgsSummary { get; set; } = "";

    /// <summary>完整参数详情（按 D 展开）</summary>
    public string ArgsDetail { get; set; } = "";

    /// <summary>是否已展开详情</summary>
    public bool Expanded { get; set; }

    /// <summary>用户选择：0=允许 1=全部允许 2=拒绝</summary>
    public int Result { get; private set; } = -1;

    /// <summary>是否已完成（选择后不再响应输入）</summary>
    public bool IsResolved => Result >= 0;

    /// <summary>结果回调</summary>
    public Action<int>? OnResolved { get; set; }

    /// <summary>是否为危险操作（决定是否显示"全部允许"）</summary>
    public bool IsDangerous { get; set; } = true;

    // 颜色常量
    private const int WarnBg = 43;    // 黄色背景
    private const int BlackFg = 30;   // 黑色前景
    private const int BoldOn = 1;     // 粗体
    private const int DimFg = 90;     // 暗灰

    public InlinePermission()
    {
        Height = 3; // 默认3行：标题 + 参数 + 操作提示
    }

    public override bool CanFocus => !IsResolved;

    /// <summary>计算实际渲染高度</summary>
    public int RenderHeight => 3 + (Expanded ? ArgsDetail.Split('\n').Length : 0);

    protected override void OnRender(StringBuilder sb, int absX, int absY)
    {
        if (IsResolved)
        {
            // 已解决：显示一行灰色决议
            var resolution = Result switch
            {
                0 => "✅ 已允许",
                1 => "✅ 已全部允许（本会话自动放行）",
                _ => "❌ 已拒绝"
            };
            WriteAt(sb, absY, absX, resolution, DimFg, 0);
            return;
        }

        int w = Width;
        int row = absY;

        // ── 第1行：⚠ 工具名 + 参数摘要 ──
        string line1 = $" ⚠ {ToolName}";
        if (!string.IsNullOrEmpty(ArgsSummary))
        {
            var summary = Truncate(ArgsSummary, w - 4 - ToolName.Length - 3);
            line1 += $" · {summary}";
        }
        line1 = Truncate(line1, w - 1);
        WriteLine(sb, row, absX, line1, BlackFg, WarnBg, BoldOn, ' ');
        row++;

        // ── 第2行：参数摘要（着色） ──
        RenderArgsLine(sb, row, absX, w);
        row++;

        // ── 展开详情（如果按了 D） ──
        if (Expanded && !string.IsNullOrEmpty(ArgsDetail))
        {
            foreach (var detailLine in ArgsDetail.Split('\n'))
            {
                WriteLine(sb, row, absX, " " + detailLine, DimFg, WarnBg, 0, ' ');
                row++;
            }
        }

        // ── 第3行：操作提示 ──
        var actions = IsDangerous
            ? "[Y] 允许  [N] 拒绝  [D] 详情"
            : "[Y] 允许  [A] 全部允许  [N] 拒绝  [D] 详情";
        actions = Truncate(actions, w - 2);
        WriteLine(sb, row, absX, " " + actions, BlackFg, WarnBg, BoldOn, ' ');
    }

    /// <summary>渲染参数行——bash 命令绿色、write_file 路径青色</summary>
    private void RenderArgsLine(StringBuilder sb, int row, int absX, int w)
    {
        sb.Append(AnsiTty.CursorPos(row, absX))
          .Append(AnsiTty.FgBg(BlackFg, WarnBg));

        if (string.IsNullOrEmpty(ArgsDetail)) return;

        // 简单着色：bash 参数中的命令用绿色
        var detail = Truncate(ArgsDetail.Replace('\n', ' '), w - 3);
        if (ToolName == "bash")
        {
            // 查找 "command:" 后的文本用绿色
            var cmdIdx = detail.IndexOf("command:", StringComparison.OrdinalIgnoreCase);
            if (cmdIdx >= 0)
            {
                var before = detail[..(cmdIdx + 8)];
                var cmd = detail[(cmdIdx + 8)..];
                sb.Append("  ").Append(before)
                  .Append(AnsiTty.Sgr(TuiColors.Green, WarnBg, 1))
                  .Append(Truncate(cmd, w - 3 - TuiHelper.DisplayWidth(before)))
                  .Append(AnsiTty.FgBg(BlackFg, WarnBg));
                return;
            }
        }
        else if (ToolName is "write_file" or "edit_file")
        {
            // 路径部分用青色
            var pathIdx = detail.IndexOf("file_path:", StringComparison.OrdinalIgnoreCase);
            if (pathIdx >= 0)
            {
                var before = detail[..(pathIdx + 10)];
                var path = detail[(pathIdx + 10)..];
                sb.Append("  ").Append(before)
                  .Append(AnsiTty.Sgr(TuiColors.Cyan, WarnBg, 1))
                  .Append(Truncate(path, w - 3 - TuiHelper.DisplayWidth(before)))
                  .Append(AnsiTty.FgBg(BlackFg, WarnBg));
                return;
            }
        }

        sb.Append("  ").Append(detail);
    }

    // ── 输入处理 ──

    public override bool OnKey(ConsoleKeyInfo key)
    {
        if (IsResolved) return false;

        char ch = char.ToUpperInvariant(key.KeyChar);

        switch (ch)
        {
            case 'Y':
                Result = 0;
                OnResolved?.Invoke(0);
                MarkDirty();
                return true;
            case 'A':
                if (!IsDangerous)
                {
                    Result = 1;
                    OnResolved?.Invoke(1);
                    MarkDirty();
                    return true;
                }
                return false;
            case 'N':
                Result = 2;
                OnResolved?.Invoke(2);
                MarkDirty();
                return true;
            case 'D':
                Expanded = !Expanded;
                Height = RenderHeight;
                MarkDirty();
                return true;
        }

        return false;
    }

    public override bool HandleMouse(InputEvent ev)
    {
        if (IsResolved) return false;
        // 鼠标点击→允许（简化交互）
        if (ev.Type == InputType.Mouse && ev.MouseLeft)
        {
            Result = 0;
            OnResolved?.Invoke(0);
            MarkDirty();
            return true;
        }
        return false;
    }

    // ── 工具 ──

    private static string Truncate(string text, int maxVw)
    {
        if (string.IsNullOrEmpty(text)) return "";
        int vw = 0, chars = 0;
        foreach (var r in text.EnumerateRunes())
        {
            int w = TuiHelper.RuneWidth(r);
            if (vw + w > maxVw) break;
            vw += w; chars += r.Utf16SequenceLength;
        }
        return chars >= text.Length ? text : text[..chars] + "…";
    }

    private void WriteLine(StringBuilder sb, int row, int col,
        string text, int fg, int bg, int attr, char pad)
    {
        sb.Append(AnsiTty.CursorPos(row, col));
        if (attr > 0) sb.Append(AnsiTty.Sgr(fg, bg, attr));
        else sb.Append(AnsiTty.FgBg(fg, bg));

        sb.Append(text);

        // 填充到 Width
        int vw = TuiHelper.DisplayWidth(text);
        int remaining = Width - vw;
        if (remaining > 0)
            sb.Append(new string(pad, remaining));
    }
}

using System.Text;

namespace WayCoder.Preview.Render;

/// <summary>
/// 颜色对照表 —— 生成一个展示所有 ANSI 颜色（16 色 / 256 色 / TrueColor）的 .tui 标记字符串。
/// 用 `--colors` 打开，走与预览完全相同的颜色映射（AnsiToColor），看到的即实际渲染色，便于比对哪色不准。
/// </summary>
public static class ColorPalette
{
    private static readonly string[] ColorNames =
        ["黑", "红", "绿", "黄", "蓝", "紫", "青", "白", "亮黑", "亮红", "亮绿", "亮黄", "亮蓝", "亮紫", "亮青", "亮白"];

    public static string GenerateMarkup()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Window title=\"🎨 颜色对照\" width=\"78\" height=\"40\" border=\"double\" borderColor=\"8\">");
        sb.AppendLine("  <VBox>");
        sb.AppendLine("    <Label text=\"── 16 色前景 (fg) ── 数字=色码，文字即该色\" fg=\"8\" />");
        sb.AppendLine("    <HBox>" + FgRow(0, 8, 30) + "</HBox>");
        sb.AppendLine("    <HBox>" + FgRow(8, 16, 90) + "</HBox>");
        sb.AppendLine("    <Label text=\" \" height=\"1\" />");
        sb.AppendLine("    <Label text=\"── 16 色背景 (bg) ── 数字=色码，底即该色\" fg=\"8\" />");
        sb.AppendLine("    <HBox>" + BgRow(0, 8, 40) + "</HBox>");
        sb.AppendLine("    <HBox>" + BgRow(8, 16, 100) + "</HBox>");
        sb.AppendLine("    <Label text=\" \" height=\"1\" />");
        sb.AppendLine("    <Label text=\"── 256 色 (16-231 立方体 + 232-255 灰阶) ── 底色即该色\" fg=\"8\" />");
        for (int row = 16; row < 232; row += 12)
            sb.AppendLine("    <HBox>" + CubeRow(row, Math.Min(row + 12, 232)) + "</HBox>");
        sb.AppendLine("    <HBox>" + CubeRow(232, 256) + "</HBox>");
        sb.AppendLine("    <Label text=\" \" height=\"1\" />");
        sb.AppendLine("    <Label text=\"── TrueColor ── 前景即该色\" fg=\"8\" />");
        sb.AppendLine("    <HBox><Label text=\"红\" fg=\"#ff0000\" /><Label text=\"绿\" fg=\"#00ff00\" /><Label text=\"蓝\" fg=\"#0000ff\" />"
            + "<Label text=\"橙\" fg=\"#ff8700\" /><Label text=\"紫\" fg=\"#b450ff\" /><Label text=\"粉\" fg=\"#ff64c8\" />"
            + "<Label text=\"青\" fg=\"#00e6ff\" /><Label text=\"黄\" fg=\"#ffff50\" /></HBox>");
        sb.AppendLine("  </VBox>");
        sb.AppendLine("</Window>");
        return sb.ToString();
    }

    private static string FgRow(int start, int end, int baseCode)
    {
        var sb = new StringBuilder();
        for (int i = start; i < end; i++)
        {
            int code = baseCode + (i - start);
            sb.Append($"<Label text=\" {code} {ColorNames[i]} \" fg=\"{code}\" bg=\"0\" />");
        }
        return sb.ToString();
    }

    private static string BgRow(int start, int end, int baseCode)
    {
        var sb = new StringBuilder();
        for (int i = start; i < end; i++)
        {
            int code = baseCode + (i - start);
            sb.Append($"<Label text=\" {code} {ColorNames[i]} \" fg=\"0\" bg=\"{code}\" />");
        }
        return sb.ToString();
    }

    private static string CubeRow(int start, int end)
    {
        var sb = new StringBuilder();
        for (int n = start; n < end; n++)
            sb.Append($"<Label text=\"{n}\" fg=\"0\" bg=\"{n}\" width=\"6\" />");
        return sb.ToString();
    }
}

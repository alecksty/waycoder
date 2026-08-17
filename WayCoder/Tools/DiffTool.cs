using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// 文件差异对比工具 —— 纯 C# 实现的最简 Diff。
/// 逐行比较两个文本文件，输出统一格式差异。
/// </summary>
public class DiffTool : ITool
{
    public string Name => "diff";
    public string Description => "比较两个文本文件的逐行差异。输出添加(+)、删除(-)、上下文行。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("file1", JNode.Object()
                .Set("type", "string")
                .Set("description", "第一个文件路径"))
            .Set("file2", JNode.Object()
                .Set("type", "string")
                .Set("description", "第二个文件路径"))
            .Set("context", JNode.Object()
                .Set("type", "integer")
                .Set("description", "差异周围显示的上下文行数（默认 3）")))
        .Set("required", JNode.Array().Add("file1").Add("file2"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var f1 = arguments.GetValueOrDefault("file1")?.ToString() ?? "";
        var f2 = arguments.GetValueOrDefault("file2")?.ToString() ?? "";
        var context = ToolArgs.GetInt(arguments, "context", 3);
        context = Math.Clamp(context, 0, 10_000); // 防极端值 i±context 整数溢出

        return Task.FromResult(Execute(f1, f2, context));
    }

    private static string Execute(string f1, string f2, int contextLines)
    {
        try
        {
            if (!File.Exists(f1)) return $"错误：文件不存在 — {f1}";
            if (!File.Exists(f2)) return $"错误：文件不存在 — {f2}";

            var lines1 = File.ReadAllLines(f1);
            var lines2 = File.ReadAllLines(f2);

            if (lines1.Length == 0 && lines2.Length == 0)
                return "（两个文件均为空）";

            if (lines1.SequenceEqual(lines2))
                return "（文件内容相同）";

            // 简单逐行比较（非精确 LCS，但速度快、可理解）
            var sb = new StringBuilder();
            sb.AppendLine($"--- {Path.GetFileName(f1)}");
            sb.AppendLine($"+++ {Path.GetFileName(f2)}");

            var maxLen = Math.Max(lines1.Length, lines2.Length);
            var diffCount = 0;
            var maxDiffs = 200; // 防止输出爆炸

            for (int i = 0; i < maxLen && diffCount < maxDiffs; i++)
            {
                var l1 = i < lines1.Length ? lines1[i] : null;
                var l2 = i < lines2.Length ? lines2[i] : null;

                if (l1 == l2) continue; // 相同行跳过

                diffCount++;

                // 显示上下文（之前的行）
                for (int ctx = Math.Max(0, i - contextLines); ctx < i; ctx++)
                {
                    if (ctx < lines1.Length)
                        sb.AppendLine($"  {ctx + 1}: {lines1[ctx]}");
                }

                // 差异行
                if (l1 != null)
                    sb.AppendLine($"- {i + 1}: {l1}");
                if (l2 != null)
                    sb.AppendLine($"+ {i + 1}: {l2}");

                // 显示上下文（之后的行）
                for (int ctx = i + 1; ctx <= Math.Min(maxLen - 1, i + contextLines); ctx++)
                {
                    if (ctx < lines1.Length)
                        sb.AppendLine($"  {ctx + 1}: {lines1[ctx]}");
                }

                sb.AppendLine();
            }

            if (diffCount >= maxDiffs)
                sb.AppendLine($"... (已达差异上限 {maxDiffs}，可能还有更多)");

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"diff 错误：{ex.GetType().Name}: {ex.Message}";
        }
    }
}

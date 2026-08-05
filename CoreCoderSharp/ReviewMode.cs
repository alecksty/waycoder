namespace CoreCoderSharp;

/// <summary>
/// 代码审查模式 —— 审查修改过的文件。
/// /review 命令触发，将 diff 发送给 Agent 进行多维度审查。
/// </summary>
public static class ReviewMode
{
    /// <summary>
    /// 生成审查 prompt 并返回，由 Agent 执行审查。
    /// </summary>
    public static string BuildReviewPrompt()
    {
        var changed = Tools.EditFileTool.ChangedFiles;
        if (changed.Count == 0)
            return "（没有修改过的文件，无需审查）";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("请审查以下已修改的文件，从多个维度分析：");
        sb.AppendLine();
        sb.AppendLine("## 审查维度");
        sb.AppendLine("1. **正确性** — 逻辑错误、边界情况、空引用");
        sb.AppendLine("2. **安全性** — 注入风险、敏感信息泄露、权限问题");
        sb.AppendLine("3. **性能** — 不必要的分配、算法复杂度、IO 效率");
        sb.AppendLine("4. **可维护性** — 命名、结构、注释、重复代码");
        sb.AppendLine("5. **测试覆盖** — 缺少的测试场景");
        sb.AppendLine();
        sb.AppendLine("## 修改的文件");

        foreach (var file in changed)
        {
            sb.AppendLine($"\n### {file}");
            try
            {
                var content = File.ReadAllText(file);
                sb.AppendLine($"```{Path.GetExtension(file).TrimStart('.')}");
                if (content.Length > 3000)
                    sb.AppendLine(content[..3000] + $"\n... (共 {content.Length} 字符)");
                else
                    sb.AppendLine(content);
                sb.AppendLine("```");
            }
            catch (Exception ex)
            {
                sb.AppendLine($"（无法读取: {ex.Message}）");
            }
        }

        sb.AppendLine();
        sb.AppendLine("请逐一审查每个文件，对每个问题标注严重程度（🔴严重 🟡中等 🟢建议）和所在行号。");
        sb.AppendLine("最后给出总体评价和改进建议。");

        return sb.ToString();
    }
}

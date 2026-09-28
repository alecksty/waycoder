using System.Diagnostics;

namespace WayCoder;

/// <summary>
/// 代码审查模式 —— 审查修改过的文件。
/// /review 命令触发，使用 git diff 获取改动内容。
/// </summary>
public static class ReviewMode
{
    /// <summary>
    /// 生成审查 prompt 并返回，由 Agent 执行审查。
    /// 优先使用 git diff（聚焦实际改动），失败时回退到文件内容。
    /// </summary>
    public static string BuildReviewPrompt()
    {
        var changed = Tools.EditFileTool.ChangedFiles;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(L.Pick("请审查以下修改，从多个维度分析：", "Please review the following changes across multiple dimensions:"));
        sb.AppendLine();
        sb.AppendLine(L.Pick("## 审查维度", "## Review dimensions"));
        sb.AppendLine(L.Pick("1. **正确性** — 逻辑错误、边界情况、空引用", "1. **Correctness** — logic errors, edge cases, null references"));
        sb.AppendLine(L.Pick("2. **安全性** — 注入风险、敏感信息泄露、权限问题", "2. **Security** — injection risks, leaked secrets, permission issues"));
        sb.AppendLine(L.Pick("3. **性能** — 不必要的分配、算法复杂度、IO 效率", "3. **Performance** — needless allocations, algorithmic complexity, I/O efficiency"));
        sb.AppendLine(L.Pick("4. **可维护性** — 命名、结构、注释、重复代码", "4. **Maintainability** — naming, structure, comments, duplicated code"));
        sb.AppendLine(L.Pick("5. **测试覆盖** — 缺少的测试场景", "5. **Test coverage** — missing test scenarios"));
        sb.AppendLine();

        // 没有修改过的文件，无需审查
        if (changed.Count == 0)
        {
            sb.AppendLine(L.Pick("（没有修改过的文件，无需审查）", "(No files were modified, nothing to review.)"));
        }
        else
        {
            // 始终列出修改的文件名
            sb.AppendLine(L.Pick("## 修改的文件", "## Modified files"));
            foreach (var file in changed)
                sb.AppendLine($"- `{Path.GetFileName(file)}` ({file})");
            sb.AppendLine();

            // 尝试 git diff（聚焦实际改动）
            var diff = GetGitDiff();
            if (!string.IsNullOrWhiteSpace(diff))
            {
                const int maxDiff = 8000;
                sb.AppendLine("## Git Diff");
                sb.AppendLine();
                sb.AppendLine("```diff");
                if (diff.Length > maxDiff)
                    sb.AppendLine(ContextManager.TruncateByRunes(diff, maxDiff) +
                        L.Pick($"\n... (diff 已截断，共 {diff.Length} 字符)", $"\n... (diff truncated, {diff.Length} chars total)"));
                else
                    sb.AppendLine(diff);
                sb.AppendLine("```");
            }
        }

        sb.AppendLine();
        sb.AppendLine(L.Pick("请逐一审查每个变更，对每个问题标注严重程度（🔴严重 🟡中等 🟢建议）和所在行号。",
            "Review each change one by one, and label every finding with its severity (🔴critical 🟡moderate 🟢suggestion) and line number."));
        sb.AppendLine(L.Pick("最后给出总体评价和改进建议。", "Finish with an overall assessment and improvement suggestions."));

        return sb.ToString();
    }

    /// <summary>获取工作区 git diff（含未跟踪文件的内容预览）。</summary>
    private static string? GetGitDiff()
    {
        try
        {
            var sb = new System.Text.StringBuilder();

            // 已跟踪文件的 diff
            var tracked = RunGit("diff HEAD -- .");
            if (!string.IsNullOrWhiteSpace(tracked))
                sb.AppendLine(tracked);

            // 未跟踪文件：用 git diff 无法捕获，显示内容预览
            var untracked = RunGit("ls-files --others --exclude-standard");
            if (!string.IsNullOrWhiteSpace(untracked))
            {
                foreach (var file in untracked.Trim().Split('\n',
                    StringSplitOptions.RemoveEmptyEntries))
                {
                    var f = file.Trim();
                    if (string.IsNullOrWhiteSpace(f)) continue;
                    sb.AppendLine(L.Pick($"\n--- 新文件: {f} ---", $"\n--- New file: {f} ---"));
                    try
                    {
                        var content = File.ReadAllText(f);
                        if (content.Length > 1500)
                        {
                            var originalLen = content.Length;
                            content = ContextManager.TruncateByRunes(content, 1500) +
                                L.Pick($"\n... (共 {originalLen} 字符)", $"\n... ({originalLen} chars total)");
                        }
                        sb.AppendLine(content);
                    }
                    catch { sb.AppendLine(L.Pick("(无法读取)", "(unreadable)")); }
                }
            }

            var result = sb.ToString().Trim();
            return result.Length > 0 ? result : null;
        }
        catch
        {
            return null;
        }
    }

    private static string RunGit(string args)
    {
        return GitRunner.Output(args);
    }
}

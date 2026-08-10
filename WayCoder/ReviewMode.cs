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
        sb.AppendLine("请审查以下修改，从多个维度分析：");
        sb.AppendLine();
        sb.AppendLine("## 审查维度");
        sb.AppendLine("1. **正确性** — 逻辑错误、边界情况、空引用");
        sb.AppendLine("2. **安全性** — 注入风险、敏感信息泄露、权限问题");
        sb.AppendLine("3. **性能** — 不必要的分配、算法复杂度、IO 效率");
        sb.AppendLine("4. **可维护性** — 命名、结构、注释、重复代码");
        sb.AppendLine("5. **测试覆盖** — 缺少的测试场景");
        sb.AppendLine();

        // 没有修改过的文件，无需审查
        if (changed.Count == 0)
        {
            sb.AppendLine("（没有修改过的文件，无需审查）");
        }
        else
        {
            // 始终列出修改的文件名
            sb.AppendLine("## 修改的文件");
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
                    sb.AppendLine(diff[..maxDiff] + $"\n... (diff 已截断，共 {diff.Length} 字符)");
                else
                    sb.AppendLine(diff);
                sb.AppendLine("```");
            }
        }

        sb.AppendLine();
        sb.AppendLine("请逐一审查每个变更，对每个问题标注严重程度（🔴严重 🟡中等 🟢建议）和所在行号。");
        sb.AppendLine("最后给出总体评价和改进建议。");

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
                    sb.AppendLine($"\n--- 新文件: {f} ---");
                    try
                    {
                        var content = File.ReadAllText(f);
                        if (content.Length > 1500)
                            content = content[..1500] + $"\n... (共 {content.Length} 字符)";
                        sb.AppendLine(content);
                    }
                    catch { sb.AppendLine($"(无法读取)"); }
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
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            proc.Start();
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output;
        }
        catch { return ""; }
    }
}

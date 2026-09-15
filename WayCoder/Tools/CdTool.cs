namespace WayCoder.Tools;

/// <summary>
/// 切换工作目录工具 —— 纯 C# 实现。
/// 更新 BashTool 的 AsyncLocal cwd 追踪。
/// </summary>
public class CdTool : ITool
{
    public string Name => "cd";
    public string Description => "切换当前工作目录。支持相对路径和绝对路径。返回切换后的完整路径。纯 C# 实现。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("path", JNode.Param("string", "目标目录路径（相对或绝对）")))
        .Set("required", JNode.Array("path"));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var path = arguments.GetValueOrDefault("path")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult("错误：path 参数不能为空");

        try
        {
            var current = CwdContext.Root;

            // 处理 ~ 展开：仅前缀 ~ 或 ~/ 展开为 home，`~user`/路径中段 ~ 保持原样
            if (path.StartsWith('~'))
                path = ExpandHome(path);

            var fullPath = Path.GetFullPath(Path.Combine(current, path));

            if (PathGuard.RequireDir(fullPath) is { } e) return Task.FromResult(e);

            // 沙箱下不许切到项目外：写工具只在项目根内可写，切出去之后每一个写都会被拒，
            // 表现为「cd 成功 → 编辑全失败」的半死状态（实测把 Agent 卡死在项目外的克隆目录里）。
            // 拒绝要发生在**切换之前**，并给出可操作的提示。
            if (SandboxManager.OutsideAllowed(fullPath) is { } outside)
                return Task.FromResult(
                    $"⛔ 沙箱（仅项目内）：不能切换到项目目录外 — {outside}\n"
                    + "请在项目目录内操作（写/编辑/克隆都只允许在项目根以内）。");

            CwdContext.Current = fullPath;
            return Task.FromResult($"✔ 工作目录: {fullPath}");
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolErrors.Error("cd ", ex));
        }

        static string ExpandHome(string p)
        {
            var home = WayCoder.Global.Home;
            if (p == "~") return home;
            if (p.StartsWith("~/") || p.StartsWith("~\\")) return Path.Combine(home, p[2..]);
            return p; // ~user 等形式保持原样
        }
    }
}

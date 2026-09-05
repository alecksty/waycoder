namespace WayCoder.Tools;

/// <summary>
/// 路径存在性守卫 —— 不存在即返回「错误：…」文案（null=存在）。
/// 收敛各工具重复的 File/Dir 存在检查与手拼错误消息（统一文案格式）。
/// CpTool/MvTool 的「源可为文件或目录」检查（RequireSource）独立保留。
/// </summary>
public static class PathGuard
{
    /// <summary>文件不存在返回「错误：{label}不存在 — {path}」，存在返回 null。</summary>
    public static string? RequireFile(string path, string label = "文件")
        => File.Exists(path) ? null : $"错误：{label}不存在 — {path}";

    /// <summary>目录不存在返回「错误：目录不存在 — {path}」，存在返回 null。</summary>
    public static string? RequireDir(string path)
        => Directory.Exists(path) ? null : $"错误：目录不存在 — {path}";
}

namespace WayCoder.Infra;

/// <summary>通用格式化工具（合并多处重复的 FormatSize 实现）。</summary>
internal static class FormatUtil
{
    /// <summary>字节数 → 可读大小（B/KB/MB/GB 四档，带空格，GB 保留 2 位小数）。</summary>
    public static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
    };

    /// <summary>紧凑格式（无空格短后缀 K/M/G，用于窄列 UI 显示，如文件选择器）。</summary>
    public static string FormatSizeCompact(long bytes) => bytes switch
    {
        < 1024 => $"{bytes}B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1}K",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1}M",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1}G",
    };
}

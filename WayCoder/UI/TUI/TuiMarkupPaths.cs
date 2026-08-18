namespace WayCoder.UI.TUI;

/// <summary>
/// 声明式标记资源（.tui 文件）定位。
/// 优先从发布输出目录（csproj 已把 tuidemo/**/*.tui 复制到输出）读取，
/// 开发态回退向上查找仓库根下的 tuidemo/ 目录。
/// </summary>
public static class TuiMarkupPaths
{
    /// <summary>定位一个 .tui 标记文件的完整路径；找不到抛异常。</summary>
    public static string ResolveDemoFile(string name)
    {
        // 1. 发布输出目录（tuidemo/chat.tui → {BaseDir}/tuidemo/chat.tui）
        var published = Path.Combine(AppContext.BaseDirectory, "tuidemo", name);
        if (File.Exists(published)) return published;

        // 2. 开发态：从当前目录向上找 tuidemo/
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "tuidemo", name);
            if (File.Exists(candidate)) return candidate;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }

        throw new FileNotFoundException($"未找到标记资源 tuidemo/{name}（发布输出或仓库根下）");
    }
}

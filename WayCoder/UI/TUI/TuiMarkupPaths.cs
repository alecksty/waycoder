namespace WayCoder.UI.TUI;

/// <summary>
/// 声明式标记资源（.tui 文件）定位与读取。
///
/// 资源位于本项目的 UI/TUI/Raw/（主界面 chat.tui、对话框/选择器 dialogs/*.tui）。
/// 读取策略：
///   1. 文件系统优先 —— 开发态从当前目录向上找 Raw/ 或 UI/TUI/Raw/（可编辑热刷新）；
///      发布输出若含 Raw/ 复制也命中。
///   2. 嵌入资源兜底 —— csproj 已把 Raw/**/*.tui 以逻辑名 WayCoder.UI.TUI.Raw.<path>
///      嵌入程序集，AOT 单文件 exe 内无需外部 .tui 文件即可读取。
/// </summary>
public static class TuiMarkupPaths
{
    /// <summary>嵌入资源的逻辑名前缀（与 csproj EmbeddedResource LogicalName 对应）。</summary>
    public const string ResourcePrefix = "WayCoder.UI.TUI.Raw";

    /// <summary>读取一个 .tui 资源的文本内容（文件系统优先，嵌入资源兜底）。</summary>
    /// <param name="name">相对 Raw/ 的资源名，如 "chat.tui"、"dialogs/confirm.tui"。</param>
    public static string LoadText(string name)
    {
        // 1. 文件系统（开发/预览热刷新 + 发布输出的 Raw/ 复制）
        var file = TryResolveFile(name);
        if (file != null) return File.ReadAllText(file);

        // 2. 嵌入资源（AOT 单文件 exe）
        var embedded = TryReadResource(name);
        if (embedded != null) return embedded;

        throw new FileNotFoundException(
            $"未找到标记资源 {name}（文件系统 Raw/ 或嵌入资源 {ResourcePrefix} 下均无）");
    }

    /// <summary>定位一个 .tui 标记文件的完整路径（仅文件系统）；找不到返回 null。</summary>
    public static string? TryResolveFile(string name)
    {
        // 发布/预览输出：Raw/<name> 与 UI/TUI/Raw/<name> 两种布局都查
        foreach (var rel in new[] { Path.Combine("Raw", name), Path.Combine("UI", "TUI", "Raw", name) })
        {
            var p = Path.Combine(AppContext.BaseDirectory, rel);
            if (File.Exists(p)) return p;
        }

        // 开发态：从当前目录向上找 Raw/、UI/TUI/Raw/ 或项目子目录 WayCoder/UI/TUI/Raw/
        // （覆盖从仓库根、项目子目录或 bin 输出目录启动的多种工作目录）
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            foreach (var rel in new[]
                     {
                         Path.Combine("Raw", name),
                         Path.Combine("UI", "TUI", "Raw", name),
                         Path.Combine("WayCoder", "UI", "TUI", "Raw", name),
                     })
            {
                var c = Path.Combine(dir, rel);
                if (File.Exists(c)) return c;
            }
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }

        return null;
    }

    /// <summary>兼容旧调用：返回标记文件完整路径；找不到抛异常（预览 --colors 等仍走文件系统）。</summary>
    public static string ResolveDemoFile(string name)
    {
        var file = TryResolveFile(name);
        if (file != null) return file;
        throw new FileNotFoundException($"未找到标记资源 {name}（Raw/ 或 UI/TUI/Raw/ 下）");
    }

    /// <summary>从程序集嵌入资源读取 .tui 内容；未嵌入返回 null。</summary>
    private static string? TryReadResource(string name)
    {
        var asm = typeof(TuiMarkupPaths).Assembly;

        // 资源名分两种形态（Windows RecursiveDir 用反斜杠）：点分隔 与 反斜杠 都试
        var candidates = new[]
        {
            ResourcePrefix + "." + name.Replace('\\', '.').Replace('/', '.'),
            ResourcePrefix + "." + name.Replace('/', '\\'),
        };
        foreach (var resName in candidates)
        {
            try
            {
                using var s = asm.GetManifestResourceStream(resName);
                if (s != null)
                {
                    using var r = new StreamReader(s);
                    return r.ReadToEnd();
                }
            }
            catch { }
        }

        // 兜底：按后缀匹配资源名（对实际资源名不确定时最稳，资源集很小）
        try
        {
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (!n.StartsWith(ResourcePrefix, StringComparison.OrdinalIgnoreCase)) continue;
                bool hit = n.EndsWith(name, StringComparison.OrdinalIgnoreCase)
                    || n.Replace('\\', '.').EndsWith(name.Replace('/', '.'), StringComparison.OrdinalIgnoreCase);
                if (!hit) continue;
                using var s = asm.GetManifestResourceStream(n);
                if (s == null) continue;
                using var r = new StreamReader(s);
                return r.ReadToEnd();
            }
        }
        catch { }
        return null;
    }
}

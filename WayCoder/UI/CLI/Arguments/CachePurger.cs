using WayCoder.Tools;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.UI.Cli.Arguments;

/// <summary>清理缓存文件（保守：只清明确是缓存的内容，不动会话/记忆/检查点）</summary>
public static class CachePurger
{
    public static int Run()
    {
        var purged = new List<string>();
        var cwd = Directory.GetCurrentDirectory();
        TryPurgeFile(Path.Combine(cwd, ".waycoder", "file-tracker.json"), purged);
        TryPurgeFile(Path.Combine(cwd, ".waycoder", "todos.json"), purged);
        TryPurgeDir(Path.Combine(cwd, ".waycoder", "trajectory"), purged);
        Console.WriteLine(purged.Count == 0 ? "没有可清理的缓存文件" : $"已清理 {purged.Count} 项缓存:");
        foreach (var p in purged) Console.WriteLine($"  - {p}");
        return 0;
    }

    private static void TryPurgeFile(string path, List<string> purged)
    {
        try { if (File.Exists(path)) { File.Delete(path); purged.Add(path); } } catch { }
    }

    private static void TryPurgeDir(string dir, List<string> purged)
    {
        try { if (Directory.Exists(dir)) { Directory.Delete(dir, true); purged.Add(dir); } } catch { }
    }
}

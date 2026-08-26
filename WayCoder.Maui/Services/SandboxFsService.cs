using WayCoder;

namespace WayCoder.Maui.Services;

/// <summary>
/// 沙箱文件服务 —— 浏览/读写 <see cref="MauiBootstrap.WorkspaceDir"/> 内的文件树。
///
/// 双保险：主工程写工具（write_file/edit_file）内部已走 <see cref="SandboxManager.CheckWritable"/>
/// 拦截越界；本服务作为 UI 层（FilesPage/EditorPage）的入口再做一次路径钳制（<see cref="ResolveInSandbox"/>），
/// 即使 UI 层误传绝对路径，也保证所有读写被锁死在沙箱根内 —— 防御纵深。
/// </summary>
public static class SandboxFsService
{
    /// <summary>沙箱根（workspace）。</summary>
    public static string Root => MauiBootstrap.WorkspaceDir;

    /// <summary>文件树节点（供 FilesPage 展示，绑定友好）。</summary>
    public sealed class FsEntry
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime Modified { get; set; }

        public string Icon => IsDirectory ? "📁" : "📄";
        public string DisplaySize => IsDirectory ? "" : FormatSize(Size);
        public string DisplayModified => Modified.ToString("MM-dd HH:mm");

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / 1024.0 / 1024.0:F1} MB",
            _ => $"{bytes / 1024.0 / 1024.0 / 1024.0:F1} GB",
        };
    }

    /// <summary>
    /// 将用户提供的相对/绝对路径安全解析到沙箱内，越界返回 null。
    /// 绝对路径必须在根内；相对路径拼接根后同样钳制（防 `../` 逃逸）。
    /// </summary>
    public static string? ResolveInSandbox(string path)
    {
        if (string.IsNullOrWhiteSpace(Root)) return null;

        var root = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(root, path));

        // 根本身或根内路径合法；根外（如 ../ 逃逸、绝对路径指向别处）拒绝
        if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase)) return full;
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return null;
        return full;
    }

    /// <summary>列出沙箱内某目录（相对根；null/空 = 根目录）。目录在前、文件在后，按名排序。</summary>
    public static List<FsEntry> ListDir(string? relDir)
    {
        var dir = ResolveInSandbox(relDir ?? "") ?? Root;
        var result = new List<FsEntry>();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return result;

        foreach (var sub in Directory.EnumerateDirectories(dir))
        {
            var di = new DirectoryInfo(sub);
            result.Add(new FsEntry { Name = di.Name, FullPath = sub, IsDirectory = true, Modified = di.LastWriteTime });
        }
        foreach (var file in Directory.EnumerateFiles(dir))
        {
            var fi = new FileInfo(file);
            result.Add(new FsEntry { Name = fi.Name, FullPath = file, IsDirectory = false, Size = fi.Length, Modified = fi.LastWriteTime });
        }

        return result
            .OrderBy(e => e.IsDirectory ? 0 : 1)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>读取沙箱内文本文件；路径越界或不存在返回 null。</summary>
    public static string? ReadText(string relPath)
    {
        var full = ResolveInSandbox(relPath);
        if (full == null || !File.Exists(full)) return null;
        return File.ReadAllText(full);
    }

    /// <summary>写文本到沙箱内（无 BOM，复用 Global 编码策略）；路径越界抛异常。</summary>
    public static void WriteText(string relPath, string content)
    {
        var full = ResolveInSandbox(relPath) ?? throw new InvalidOperationException($"路径越界：{relPath}");
        var parent = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        Global.WriteAllTextPreserveBom(full, content);
    }

    /// <summary>文档导入：从系统文件选择器拷入沙箱根，返回落地相对路径。</summary>
    public static async Task<string> ImportAsync(FileResult file)
    {
        var target = Path.Combine(Root, file.FileName);
        await using var src = await file.OpenReadAsync();
        await using var dst = File.Create(target);
        await src.CopyToAsync(dst);
        return file.FileName;
    }

    /// <summary>计算某路径相对沙箱根的子路径（用于导航/面包屑）；越界返回 null。</summary>
    public static string? ToRelative(string fullPath)
    {
        var root = Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(fullPath);
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return null;
        return full[(root.Length + 1)..];
    }

    /// <summary>重命名沙箱内文件/目录（newName 只取文件名，防路径逃逸）；越界/不存在/目标已存在返回 false。</summary>
    public static bool Rename(string relPath, string newName)
    {
        var full = ResolveInSandbox(relPath);
        if (full == null || (!File.Exists(full) && !Directory.Exists(full))) return false;

        var safeName = Path.GetFileName(newName?.Trim() ?? "");
        if (safeName.Length == 0 || string.Equals(safeName, Path.GetFileName(full), StringComparison.Ordinal)) return false;

        var parent = Path.GetDirectoryName(full);
        var target = ResolveInSandbox(Path.Combine(parent ?? "", safeName));
        if (target == null || string.Equals(target, full, StringComparison.OrdinalIgnoreCase)) return false;
        if (File.Exists(target) || Directory.Exists(target)) return false;

        if (File.Exists(full)) File.Move(full, target);
        else Directory.Move(full, target);
        return true;
    }

    /// <summary>删除沙箱内文件/目录（目录递归）；越界或不存在返回 false。</summary>
    public static bool Delete(string relPath)
    {
        var full = ResolveInSandbox(relPath);
        if (full == null || (!File.Exists(full) && !Directory.Exists(full))) return false;

        if (Directory.Exists(full)) Directory.Delete(full, recursive: true);
        else File.Delete(full);
        return true;
    }
}

using System.IO.Compression;
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
    /// <summary>文件类型分类：源码/文本→编辑器，图片→预览页，音频/视频→系统播放器，未知→仅外部打开。</summary>
    public enum FileCategory { Source, Image, Audio, Video, Unknown }

    private static readonly HashSet<string> SourceExts = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",".js",".ts",".jsx",".tsx",".py",".go",".rs",".java",".c",".h",".cpp",".hpp",".cc",
        ".json",".xml",".html",".htm",".md",".mdx",".sh",".bash",".zsh",".yml",".yaml",".sql",
        ".css",".scss",".rb",".php",".swift",".kt",".kts",".vue",".txt",".log",".csv",".ini",
        ".toml",".conf",".csproj",".sln",".tui",".env",".gitignore",
    };
    private static readonly HashSet<string> ImageExts = new(StringComparer.OrdinalIgnoreCase)
        { ".png",".jpg",".jpeg",".gif",".webp",".bmp",".svg",".ico" };
    private static readonly HashSet<string> AudioExts = new(StringComparer.OrdinalIgnoreCase)
        { ".mp3",".wav",".ogg",".m4a",".aac",".flac" };
    private static readonly HashSet<string> VideoExts = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4",".webm",".mkv",".mov",".avi" };

    /// <summary>按扩展名分类文件类型。</summary>
    public static FileCategory DetectCategory(string path)
    {
        var ext = System.IO.Path.GetExtension(path);
        if (SourceExts.Contains(ext)) return FileCategory.Source;
        if (ImageExts.Contains(ext)) return FileCategory.Image;
        if (AudioExts.Contains(ext)) return FileCategory.Audio;
        if (VideoExts.Contains(ext)) return FileCategory.Video;
        return FileCategory.Unknown;
    }

    /// <summary>文件类型图标。</summary>
    public static string CategoryIcon(FileCategory c) => c switch
    {
        FileCategory.Source => "📝",
        FileCategory.Image => "🖼",
        FileCategory.Audio => "🎵",
        FileCategory.Video => "🎬",
        _ => "📄",
    };

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

        /// <summary>文件类型分类（源码/文本/图片/音频/视频/未知）。</summary>
        public FileCategory Category { get; set; } = FileCategory.Unknown;

        public string Icon => IsDirectory ? "📁" : CategoryIcon(Category);
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
            result.Add(new FsEntry
            {
                Name = fi.Name,
                FullPath = file,
                IsDirectory = false,
                Size = fi.Length,
                Modified = fi.LastWriteTime,
                Category = DetectCategory(file),
            });
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

    /// <summary>新建空文件；路径越界、已存在同名文件/目录返回 false。</summary>
    public static bool CreateFile(string relPath)
    {
        var full = ResolveInSandbox(relPath);
        if (full == null || File.Exists(full) || Directory.Exists(full)) return false;
        var parent = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        File.WriteAllText(full, "");
        return true;
    }

    /// <summary>新建目录；路径越界、已存在同名文件/目录返回 false。</summary>
    public static bool CreateDir(string relPath)
    {
        var full = ResolveInSandbox(relPath);
        if (full == null || File.Exists(full) || Directory.Exists(full)) return false;
        Directory.CreateDirectory(full);
        return true;
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

    /// <summary>
    /// 把沙箱内文件/目录打成 zip，落同一父目录下（同名 zip 已存在则不覆盖返回 null）。
    /// 返回 zip 的相对路径；失败返回 null。
    /// </summary>
    public static string? CreateZip(string relPath)
    {
        var full = ResolveInSandbox(relPath);
        if (full == null || (!File.Exists(full) && !Directory.Exists(full))) return null;

        var baseName = Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar, '/')) ?? "archive";
        var parentRel = Path.GetDirectoryName(relPath)?.Replace('\\', '/') ?? "";
        var zipRel = string.IsNullOrEmpty(parentRel) ? baseName + ".zip" : parentRel + "/" + baseName + ".zip";
        var zipFull = ResolveInSandbox(zipRel);
        if (zipFull == null || File.Exists(zipFull)) return null;   // 已存在不覆盖

        try
        {
            using var fs = File.Create(zipFull);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
            if (Directory.Exists(full))
            {
                // 目录：递归加入，条目路径相对目录本身
                foreach (var f in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
                {
                    var rel = Path.GetRelativePath(full, f).Replace('\\', '/');
                    var entry = zip.CreateEntry(rel);
                    using var es = entry.Open();
                    using var src = File.OpenRead(f);
                    src.CopyTo(es);
                }
            }
            else
            {
                var entry = zip.CreateEntry(Path.GetFileName(full));
                using var es = entry.Open();
                using var src = File.OpenRead(full);
                src.CopyTo(es);
            }
        }
        catch { try { File.Delete(zipFull); } catch { } return null; }
        return zipRel;
    }
}

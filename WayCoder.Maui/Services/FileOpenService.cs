using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using WayCoder.Infra;

namespace WayCoder.Maui.Services;

/// <summary>
/// 用系统外部应用打开沙箱文件（HTML→浏览器、PDF→阅读器、图片→相册等）。
///
/// 跨平台走 <see cref="Launcher"/>：Android 端 MAUI 内部用 FileProvider
/// 生成 content:// URI（需 Manifest 注册 authority="{applicationId}.fileprovider"，
/// 见 file_paths.xml 只暴露 workspace），iOS 走 UIDocumentInteractionController。
/// 避免 Android 7+ 直接共享 file:// 路径抛 FileUriExposedException。
/// </summary>
public static class FileOpenService
{
    /// <summary>按扩展名推断 MIME 类型（HTML 等关键类型精确映射，其余回退 octet-stream）。</summary>
    public static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "html" or "htm" => "text/html",
            "md" or "markdown" => "text/markdown",
            "txt" or "log" or "csv" or "json" or "xml" or "yaml" or "yml" or "ini" or "toml" or "conf" => "text/plain",
            "js" or "ts" or "css" or "c" or "h" or "cs" or "java" or "py" or "go" or "rs" or "sh" or "cpp" or "vue" or "tsx" => "text/plain",
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "gif" => "image/gif",
            "webp" => "image/webp",
            "svg" => "image/svg+xml",
            "bmp" => "image/bmp",
            "pdf" => "application/pdf",
            "mp3" or "wav" or "ogg" or "m4a" or "aac" or "flac" => "audio/*",
            "mp4" or "webm" or "mkv" or "mov" or "avi" => "video/*",
            "zip" => "application/zip",
            "gz" or "tar" or "7z" or "rar" or "deb" => "application/octet-stream",
            _ => "application/octet-stream",
        };
    }

    /// <summary>
    /// 用系统默认应用打开文件；无可处理应用或异常返回 false。
    /// </summary>
    /// <param name="fullPath">沙箱根内的绝对路径（FsEntry.FullPath 即可）。</param>
    /// <param name="fileName">展示名（ActionSheet/系统标题用）。</param>
    public static async Task<bool> OpenWithExternalAsync(string fullPath, string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath)) return false;
            var contentType = GetContentType(fullPath);
            var request = new OpenFileRequest(fileName, new ReadOnlyFile(fullPath, contentType));
            return await Launcher.Default.OpenAsync(request);
        }
        catch (Exception ex)
        {
            ErrorLog.Error("FileOpen", $"外部打开失败 {fileName}", ex);
            return false;
        }
    }
}

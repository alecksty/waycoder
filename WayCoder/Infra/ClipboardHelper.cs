using System.Diagnostics;

namespace WayCoder;

/// <summary>跨平台剪贴板读写</summary>
public static class ClipboardHelper
{
    public static async Task<string> GetTextAsync()
    {
        try
        {
            if (OperatingSystem.IsMacOS())
                return await RunAsync("pbpaste") ?? "";
            if (OperatingSystem.IsLinux())
                return await RunAsync("xclip -o -selection clipboard 2>/dev/null")
                    ?? await RunAsync("xsel --clipboard --output 2>/dev/null")
                    ?? "";
            if (OperatingSystem.IsWindows())
                return await RunAsync("powershell -command \"Get-Clipboard\"") ?? "";
        }
        catch { }
        return "";
    }

    /// <summary>同步读取剪贴板（键盘处理等同步上下文中使用）</summary>
    public static string GetText()
    {
        try { return GetTextAsync().GetAwaiter().GetResult(); }
        catch { return ""; }
    }

    /// <summary>异步写入剪贴板</summary>
    public static async Task SetTextAsync(string text)
    {
        try
        {
            var escaped = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
            if (OperatingSystem.IsMacOS())
                await RunAsync($"echo \"{escaped}\" | pbcopy");
            else if (OperatingSystem.IsLinux())
                await RunAsync($"echo \"{escaped}\" | xclip -selection clipboard 2>/dev/null");
            else if (OperatingSystem.IsWindows())
                await RunAsync($"powershell -command \"Set-Clipboard -Value '{escaped}'\"");
        }
        catch { /* 剪贴板不可用时静默忽略 */ }
    }

    /// <summary>同步写入剪贴板</summary>
    public static void SetText(string text)
    {
        try { SetTextAsync(text).GetAwaiter().GetResult(); }
        catch { /* 静默忽略 */ }
    }

    private static async Task<string?> RunAsync(string cmd)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c \"{cmd}\"" : $"-c \"{cmd}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true, // 不共享主控台 stdin（防 TUI ReadKey 竞态）
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            try { proc.StandardInput.Close(); } catch { } // stdin 置 EOF
            var result = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return result?.TrimEnd('\n', '\r');
        }
        catch { return null; }
    }
}

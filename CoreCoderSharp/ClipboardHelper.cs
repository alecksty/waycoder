using System.Diagnostics;

namespace CoreCoderSharp;

/// <summary>跨平台剪贴板读取</summary>
public static class ClipboardHelper
{
    public static async Task<string> GetTextAsync()
    {
        try
        {
            if (OperatingSystem.IsMacOS())
                return await RunAsync("pbpaste");
            if (OperatingSystem.IsLinux())
                return await RunAsync("xclip -o -selection clipboard 2>/dev/null")
                    ?? await RunAsync("xsel --clipboard --output 2>/dev/null")
                    ?? "";
            if (OperatingSystem.IsWindows())
                return await RunAsync("powershell -command \"Get-Clipboard\"");
        }
        catch { }
        return "";
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
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            var result = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return result?.TrimEnd('\n', '\r');
        }
        catch { return null; }
    }
}

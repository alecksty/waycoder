namespace WayCoder.Infra;

/// <summary>
/// 跨平台命令运行器选择：统一 Windows 与 Unix 的 shell / 解释器差异。
/// 避免各处硬编码 "bash"/"python3"/"python" 导致在另一平台不可用。
/// </summary>
public static class CrossPlatform
{
    /// <summary>当前是否运行于 Windows。</summary>
    public static bool IsWindows => OperatingSystem.IsWindows();

    /// <summary>shell 可执行文件：Windows 用 cmd.exe，Unix 用 /bin/bash。</summary>
    public static string ShellExecutable => IsWindows ? "cmd.exe" : "/bin/bash";

    /// <summary>用 shell 执行命令的参数（cmd.exe 用 /c，bash 用 -c 并对内层引号转义）。</summary>
    public static string ShellArgs(string command) => IsWindows
        ? $"/c \"{command}\""
        : $"-c \"{command.Replace("\"", "\\\"")}\"";

    /// <summary>Python 解释器可执行文件：Windows 用 python，Unix 用 python3。</summary>
    public static string PythonExecutable => IsWindows ? "python" : "python3";
}

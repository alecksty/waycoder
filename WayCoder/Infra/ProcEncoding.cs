using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 进程输出解码编码设置 —— Windows 按系统 OEM 代码页正确解码 cmd.exe 输出。
///
/// 背景：Windows 中文系统 cmd.exe/其子进程输出到管道的字节流是系统 OEM 代码页编码（936/GBK），
/// 直接按 UTF-8 解码会乱码（如「涓�瓒�」）。**实测 `chcp 65001` 只改控制台代码页、不改重定向管道的
/// 输出字节**（cmd 内建 echo/dir 等对管道始终写 OEM 代码页）——所以「强制 UTF-8」不可行，
/// 正确做法是 <see cref="Apply"/> 按系统 OEM 代码页解码，得到与 UTF-8 语义一致的正确 Unicode。
/// Unix（/bin/bash）输出本就是 UTF-8，无需改动。
/// </summary>
public static partial class ProcEncoding
{
    /// <summary>kernel32 获取系统 OEM 代码页（GetOEMCP）。中文系统返回 936(GBK)，英文系统返回 437/850 等。</summary>
    [LibraryImport("kernel32.dll", EntryPoint = "GetOEMCP")]
    private static partial uint GetOemCp();

    private static readonly Lazy<Encoding?> _oem = new(GetOemEncoding);

    /// <summary>Windows OEM 代码页编码（中文系统=936/GBK；英文=437/850）。非 Windows 返回 null。</summary>
    public static Encoding? OemEncoding => _oem.Value;

    private static Encoding? GetOemEncoding()
    {
        if (!OperatingSystem.IsWindows()) return null;
        // GBK/GB18030 等代码页编码不在 .NET Core 内置，需先注册 CodePagesEncodingProvider
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
        // GetOEMCP 可能返回 0（极少见）：回退到 GBK，仍失败则宽松 UTF-8（至少保证可读不崩）
        try
        {
            var cp = GetOemCp();
            if (cp == 0) cp = 936;
            return Encoding.GetEncoding((int)cp);
        }
        catch
        {
            try { return Encoding.GetEncoding(936); }
            catch { return Encoding.UTF8; }
        }
    }

    /// <summary>去除输出开头的 UTF-8 BOM（若某些命令/program 输出带 BOM）。</summary>
    public static string StripBom(string output)
        => output.Length > 0 && output[0] == '﻿' ? output[1..] : output;

    /// <summary>
    /// 按代码页取编码（GBK/GB18030 等不在 .NET Core 内置，需先注册 CodePagesEncodingProvider）。
    /// 非 Windows 或取不到返回 null —— 调用方据此决定「没有回退可用」。
    ///
    /// 用途：**控制台输入字节**的回退解码。conhost 在 VT 输入模式下按 `GetConsoleCP()` 编码非 ASCII
    /// 按键字节（见 <c>WinConsoleMode.EnsureInputCodePage</c>），中文系统是 936(GBK)。
    /// </summary>
    public static Encoding? ForCodePage(uint codePage)
    {
        if (!OperatingSystem.IsWindows() || codePage == 0) return null;
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
        try { return Encoding.GetEncoding((int)codePage); }
        catch { return null; }
    }

    /// <summary>
    /// 为跨平台进程设置输出解码编码：Windows 用系统 OEM 代码页（正确解码 cmd.exe 的 GBK/GB18030 字节），
    /// Unix 保持默认 UTF-8。在构造 ProcessStartInfo 之后、Process.Start 之前调用。
    /// </summary>
    public static void Apply(ProcessStartInfo psi)
    {
        if (!OperatingSystem.IsWindows()) return;
        if (!psi.RedirectStandardOutput && !psi.RedirectStandardError) return;
        var enc = OemEncoding;
        if (enc != null)
        {
            if (psi.RedirectStandardOutput) psi.StandardOutputEncoding = enc;
            if (psi.RedirectStandardError) psi.StandardErrorEncoding = enc;
        }
    }

    /// <summary>
    /// 该可执行文件在 Windows 上是否为「控制台包装器」—— 只有这类程序的**重定向输出**才是
    /// OEM 代码页字节（cmd.exe / .bat / .cmd / npm 系列的 shim）。
    ///
    /// 判据是「**启动的是什么**」，不是「所有进程都套 OEM」：原生程序（git / node / dotnet /
    /// gcc / 语言服务器 / sqlite3）输出 UTF-8，套上 OEM 反而把中文解成乱码。此前这条判断
    /// 散在十几个进程启动点各判各的：8 处该 Apply 的没 Apply（含自更新启动 upgrade.bat、
    /// LintTool 跑 npx、McpTransport 起 npx 型 server），而 KillTool / PsTool 又把同一份
    /// 实现各写一遍。规则收敛到这里，新启动点调 <see cref="ApplyIfConsoleWrapper"/> 即可。
    /// </summary>
    /// <summary>纯名字判断（平台无关，主自测直接覆盖）：该可执行名是否属于控制台包装器。</summary>
    public static bool IsConsoleWrapperName(string? fileName)
    {
        // 手动按两种分隔符取末段：Path.GetFileName 只认**当前平台**的分隔符，
        // 传 Windows 路径（C:\Windows\System32\cmd.exe）在 Unix 上会整串返回、判不出来。
        var raw = (fileName ?? "").Trim();
        var cut = raw.LastIndexOfAny(['/', '\\']);
        var n = (cut >= 0 ? raw[(cut + 1)..] : raw).ToLowerInvariant();
        if (n.Length == 0) return false;
        return n is "cmd" or "cmd.exe" or "command.com"
            or "powershell" or "powershell.exe" or "pwsh" or "pwsh.exe"
            or "npm" or "npm.cmd" or "npx" or "npx.cmd"
            or "yarn" or "yarn.cmd" or "pnpm" or "pnpm.cmd"
            || n.EndsWith(".bat") || n.EndsWith(".cmd");
    }

    /// <summary>Windows 上且属于控制台包装器 → 需要 OEM 解码（非 Windows 恒 false）。</summary>
    public static bool IsWindowsConsoleWrapper(string? fileName)
        => OperatingSystem.IsWindows() && IsConsoleWrapperName(fileName);

    /// <summary>按「启动的是什么」决定是否套 OEM 解码（仅控制台包装器需要；原生程序保持 UTF-8）。</summary>
    public static void ApplyIfConsoleWrapper(ProcessStartInfo psi, string? fileName)
    {
        if (IsWindowsConsoleWrapper(fileName)) Apply(psi);
    }
}

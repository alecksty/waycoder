using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 进程输出解码编码设置 —— 修复 Windows 中文系统下 cmd.exe 输出 GBK/OEM 字节、
/// 而 StreamReader 默认按 UTF-8 解码导致的乱码。
///
/// 根因：Windows 控制台程序（cmd.exe/其子进程）输出到管道的字节流是系统 OEM 代码页编码
/// （中文系统为 936/GBK），而非 UTF-8。.NET 的 Process.StandardOutput/StandardError 默认
/// StreamReader 编码跟随 Console.OutputEncoding（本程序在 Main 里被强制设为 UTF-8），
/// 于是 GBK 字节被误当 UTF-8 解码 → 中文/特殊字符显示成乱码（如「涓�瓒�」）。
///
/// 解决：为 <see cref="ProcessStartInfo"/> 统一设置 StandardOutput/ErrorEncoding 为 OEM 代码页编码，
/// 让 StreamReader 按正确编码解码字节流。Unix（/bin/bash）输出本就是 UTF-8，无需改动。
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
        // GetOEMCP 可能返回 0（极少见）：回退到 GBK，仍失败则宽松 UTF-8（至少保证可读不崩）
        try
        {
            var cp = GetOemCp();
            if (cp == 0) cp = 936;
            return Encoding.GetEncoding((int)cp);
        }
        catch
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(936);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }
    }

    /// <summary>
    /// 为跨平台进程设置输出解码编码：Windows 用 OEM 代码页（正确解码 cmd.exe 输出），Unix 用 UTF-8（保持默认）。
    /// 仅在重定向 stdout/stderr 时有效；在构造 ProcessStartInfo 之后、Process.Start 之前调用。
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
}

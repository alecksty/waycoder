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
}

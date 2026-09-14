namespace WayCoder.Infra;

/// <summary>
/// **「用哪个 shell、命令怎么包」的唯一真源。**
///
/// 起因：这句 <c>IsWindows ? "cmd.exe" : "/bin/bash"</c>（连同参数拼接）原先在**四处各写一遍** ——
/// <c>BashTool</c> / <c>PersistentShell</c> / <c>BackgroundTask</c> / <c>SandboxManager</c>。
/// 桌面三平台看不出问题，因为 `cmd.exe` 与 `/bin/bash` 覆盖了它们；
/// 而**手机端（Android）两个都没有** —— 只有 <c>/system/bin/sh</c>（mksh）。
/// 四份平行实现里漏改任何一处，症状都是「只在手机上炸、桌面全绿」，
/// 正是本仓库排第一的那类重复坑（见 CLAUDE.md「共享助手已存在但调用点绕过」）。
///
/// ⚠ iOS 不走这里也不该走：`fork/exec` 被沙箱物理拒绝，移动端在 iOS 上用的是
/// <c>CoreStubs.cs</c> 里那个会返回「不支持」的 <c>BashTool</c> 桩
/// （见 WayCoder.Maui.csproj 的平台条件 include）。
///
/// **每条规则都拆成「纯函数 + 无参包装」两半**：平台判断走 <c>OperatingSystem.Is*()</c>，
/// 自测里没法假装自己是 Android，所以真正的分支判断收在 <c>*For(isWindows, isAndroid)</c> 里 ——
/// 于是「三条分支各自返回什么」在桌面上就能全部钉住，不必真有那台机器。
/// </summary>
public static class ShellPath
{
    /// <summary>
    /// 当前平台的 shell 可执行文件。
    ///
    /// Android 用系统自带的 <c>/system/bin/sh</c>（mksh）—— 它**一定存在**，且不受
    /// Android 10+ 的 W^X 限制（那条限制管的是「从 app 可写目录里 exec 文件」，系统目录不受影响）。
    /// </summary>
    public static string Resolve() => ResolveFor(OperatingSystem.IsWindows(), OperatingSystem.IsAndroid());

    /// <summary>纯函数版：Windows <c>cmd.exe</c> / Android <c>/system/bin/sh</c> / 其余 <c>/bin/bash</c>。</summary>
    public static string ResolveFor(bool isWindows, bool isAndroid) =>
        isWindows ? "cmd.exe"
        : isAndroid ? "/system/bin/sh"
        : "/bin/bash";

    /// <summary>
    /// 把一条命令包成「让 shell 执行它」的参数串：Windows <c>/c "…"</c>，其余 <c>-c "…"</c>。
    /// 转义规则与收敛前**逐字一致**（改的是位置，不是行为）。
    /// </summary>
    public static string BuildArgs(string command) => BuildArgsFor(command, OperatingSystem.IsWindows());

    /// <summary>纯函数版。</summary>
    public static string BuildArgsFor(string command, bool isWindows) =>
        isWindows
            ? $"/c \"{command}\""
            : $"-c \"{command.Replace("\"", "\\\"")}\"";

    /// <summary>
    /// **长驻 shell** 的启动参数（走 stdin 反复喂命令那种，见 <c>PersistentShell</c>）。
    ///
    /// Windows 用 <c>/Q</c> 关回显；Unix 关掉 profile/rc（干净、无交互 prompt）。
    /// </summary>
    public static string PersistentArgs() =>
        PersistentArgsFor(OperatingSystem.IsWindows(), OperatingSystem.IsAndroid());

    /// <summary>
    /// 纯函数版。Android 传空 —— mksh 非交互时不读 rc，而 <c>--noprofile/--norc</c> 是
    /// **bash 专有**的，喂给 mksh 会直接以「未知选项」启动失败。
    ///
    /// ⚠ **Android 这一支没在真机上验过**：本轮只保证不炸在参数解析上，
    /// 长驻 shell 的状态保持体验不在本轮范围（AI 不带 session_id 时根本不走这条）。
    /// </summary>
    public static string PersistentArgsFor(bool isWindows, bool isAndroid) =>
        isWindows ? "/Q"
        : isAndroid ? ""
        : "--noprofile --norc";
}

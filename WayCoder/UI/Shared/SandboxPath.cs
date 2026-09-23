namespace WayCoder.UI.Shared;

/// <summary>
/// **沙箱内相对路径的目录导航**（归一化 / 根判据 / 上一级 / 显示形态）—— 只此一份。
///
/// <para>
/// 手机端铁律：一切路径以工作区为根、**只按相对路径工作**（CLAUDE.md「路径语义总则」）。
/// 于是「这是不是根」「上一层是谁」「顶栏那行怎么显示」在**文件页的返回键**、
/// 文件页的「↩ 上级」按钮、路径标签三处都要用到 —— 各写一份的话，边界
/// （根、多余的分隔符、Windows 形态的 <c>\</c>）必然漂移，而症状是
/// 「某一处退不到根 / 退过头」，界面上完全看不出是路径计算的问题。
/// </para>
///
/// <para>
/// ⚠ 这里**只管相对路径的文本形状**（<c>a/b</c> ↔ 空串）：不碰文件系统、也不知道沙箱根在哪。
/// 「越界 / 真实存在性」是 <c>SandboxFsService.ResolveInSandbox</c> 的职责，别在这里重判。
/// 与 <see cref="PathText"/> 的分工：那个认**两种分隔符**（路径可能来自别的机器），
/// 这个在它之上多管**归一化与上下行**。
/// </para>
/// </summary>
public static class SandboxPath
{
    /// <summary>
    /// 归一化相对路径：<c>\</c> → <c>/</c>、丢掉首尾与重复的分隔符。**沙箱根 = 空串**。
    ///
    /// 空串与 <c>"/"</c> 都归到根 —— 后者是界面上「已经在根」的显示形态，
    /// 万一它被当成输入传回来（复制粘贴、配置里存过），不该算成"一个名字是空的子目录"。
    /// </summary>
    public static string Normalize(string? rel)
    {
        if (string.IsNullOrEmpty(rel)) return "";
        // 分段复用唯一那一份（两种分隔符都认、空段丢弃），不在本类里再切一次字符串
        var segments = PathText.Segments(rel!);
        return segments.Length == 0 ? "" : string.Join('/', segments);
    }

    /// <summary>是不是沙箱根（<c>null</c> / 空串 / 只有分隔符都算）。</summary>
    public static bool IsRoot(string? rel) => Normalize(rel).Length == 0;

    /// <summary>
    /// 上一级目录；**已经在根时返回 <c>null</c>**。
    ///
    /// 用 <c>null</c> 而不是「返回空串」来表达「没有上一级」：空串就是根本身，
    /// 两者混在一起的话调用方分不出「退到了根」与「本来就在根」——
    /// 而文件页恰恰要按这个区别决定「是刷新列表、还是问要不要退出应用」。
    /// </summary>
    public static string? ParentOf(string? rel)
    {
        var norm = Normalize(rel);
        if (norm.Length == 0) return null;   // 已在根 ⇒ 没有上一级
        var i = norm.LastIndexOf('/');
        return i < 0 ? "" : norm[..i];       // `a` ⇒ 根（空串）
    }

    /// <summary>
    /// 相对路径 → 界面上那一行的显示形态（根 = <c>/</c>，<c>a/b</c> → <c>/a/b</c>）。
    ///
    /// 归一化之后再拼：直接 <c>"/" + rel</c> 的话 <c>a/b/</c> 会显示成 <c>/a/b/</c>、
    /// <c>a//b</c> 会多出一个空段，而它们与 <c>a/b</c> 是同一个目录。
    /// </summary>
    public static string Display(string? rel)
    {
        var norm = Normalize(rel);
        return norm.Length == 0 ? "/" : "/" + norm;
    }
}

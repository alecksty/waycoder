namespace WayCoder.UI.Shared;

/// <summary>
/// **路径的跨平台文本处理** —— 只认字符、不碰文件系统的那几个操作，只此一份。
///
/// <para>
/// ## 为什么不能直接用 <c>System.IO.Path</c>
///
/// `Path.GetFileName` / `Path.GetDirectoryName` **按当前平台的分隔符切**：Windows 上
/// `\` 与 `/` 都算，Unix 上**只有 `/` 算**。而本仓要处理的一大批路径**不是我们自己拼的**，
/// 形态由**产出它的那一方**决定，不由我们运行在哪台机器决定：
/// </para>
///
/// <list type="bullet">
/// <item><b>编译器输出</b>——诊断里带的路径（`D:\proj\main.c:12: error: …`）；</item>
/// <item><b>跨端同步的数据</b>——会话记录、配置、repo map 里存下来的路径；</item>
/// <item><b>用户敲进来的</b>——`@` 补全、`/cd`、粘贴过来的一整条路径。</item>
/// </list>
///
/// <para>
/// 拿 `Path.GetFileName` 处理这些的后果很安静：Unix 上 `D:\proj\main.cpp` 会被**原样返回**，
/// 与 `main.cpp` 一比就"不是同一个文件" —— 诊断被当成"别的文件的"，**行锚丢掉**（气泡还在，
/// 只是不再画箭头），从界面上看完全联想不到分隔符。
/// **实测**：`SelfTest` 里"真机上的路径形态（Windows 盘符 + 反斜杠）"那一组
/// 在 Windows 上全绿、在 macOS/Linux 上**必红一条**。
/// </para>
///
/// <para>
/// ⚠ 反过来，**纯粹是本进程自己 <c>Directory.GetFiles</c> 出来的路径**用
/// `System.IO.Path` 是对的（分隔符一定与平台一致，而且 `Path` 还处理了别的边角）——
/// 不必为了统一而改那些地方。**判据是"这个路径从哪来"**，不是"哪个 API 更保险"。
/// </para>
/// </summary>
public static class PathText
{
    /// <summary>归一化：`\` → `/`。**要先切分/比较就一律先过这一步**。</summary>
    public static string Normalize(string path) => path.Replace('\\', '/');

    /// <summary>
    /// 取文件名（去掉目录部分）。**`/` 与 `\` 都当分隔符**，与运行平台无关。
    ///
    /// <para>
    /// 拿不到名字时（空串、或以分隔符结尾）**原样返回** —— 调用方多半是拿它做显示，
    /// 总得有东西可显示，返回空串会让界面上那一格变空白。
    /// </para>
    /// </summary>
    public static string FileNameOf(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        int i = path.LastIndexOfAny(['/', '\\']);
        if (i < 0) return path;                  // 没有分隔符 ⇒ 它自己就是文件名
        var name = path[(i + 1)..];
        return name.Length == 0 ? path : name;   // 以分隔符结尾 ⇒ 拿不到名字，原样返回
    }

    /// <summary>
    /// 路径分段（两种分隔符都认，空段丢弃）。
    ///
    /// ⚠ 与 <see cref="string.Split(char[])"/> 的差别是 **`RemoveEmptyEntries`**：
    /// 路径里连续的分隔符（`a//b`、`C:\\x`）不产生空段，否则按段取 `[^1]` 会拿到空串。
    /// </summary>
    public static string[] Segments(string path)
        => Normalize(path).Split('/', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// 是不是绝对路径（**按形态判，不查文件系统**）：Unix `/…`、盘符 `C:\` / `C:/`、UNC `\\srv\share`。
    /// </summary>
    public static bool IsAbsoluteShaped(string path)
        => path.Length > 0 && (path[0] is '/' or '\\' || (path.Length > 1 && path[1] == ':'));
}

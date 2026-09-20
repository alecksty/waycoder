namespace VMLAssembler;

/// <summary>
/// 「预处理拼接后的行号 → (原文件, 原行)」的**唯一实现**。
///
/// <para>
/// **为什么放在 VMLAssembler 而不是 CompilerBase**：链接器（<see cref="LibraryLinker"/>）
/// 也要用它 —— 「未定义的函数」这一类错误是**链接期**才发现的，而报错要指到用户写的那一行。
/// 依赖方向是 `CompilerBase → VMLAssembler` 单向（前者引用后者），所以实现必须在**下层**，
/// 上层转调。反过来放会让链接器够不着，只能在两边各抄一份（本仓头号坑）。
/// </para>
///
/// <para>
/// 映射表**跟着 <see cref="VmlProgram"/> 走**（<see cref="VmlProgram.SourceLineMap"/>）：
/// 链接器手里只有程序对象，拿不到编译器那边的"生效中的表"。
/// </para>
/// </summary>
public static class SourceLineMapUtil
{
    /// <summary>`Preprocessor` 在主文件名未知时用的占位符（见 <see cref="Map"/>）。</summary>
    public const string UnknownFilePlaceholder = "<unknown>";

    /// <summary>
    /// 把「预处理拼接后的行号」换成「(原文件, 原行)」。
    ///
    /// <para>
    /// 越界/没有映射 ⇒ 返回 <c>(null, 传入的行号)</c>：**宁可退回拼接行号，也不能报出别的
    /// 文件的行** —— 报错指错地方比报不出位置更糟（用户会去改一行根本没问题的代码）。
    /// </para>
    /// </summary>
    public static (string? File, int Line) Map(List<(string, int)>? lineMap, int processedLine)
    {
        if (lineMap != null && processedLine > 0 && processedLine <= lineMap.Count)
        {
            var e = lineMap[processedLine - 1];
            // ⚠ **`<unknown>` 是占位符，不是文件名**。
            //   `Preprocessor` 的 `currentFile` 初值就是它，只有 `Process(filePath)` 传了
            //   真实路径才会被换掉；而 21 门语言的 `Compile(string)` 走的都是无参 `Process()`
            //   ⇒ 主文件每一行都会被映射成 `("<unknown>", N)`。
            //   照原样往上冒，用户会看到 `<unknown>:4:20: error: …` —— 比原来那个
            //   `<input>` 更难懂，而且把"文件名未知"谎报成了"文件名叫 <unknown>"。
            //   所以这里**只把文件退成未知、行号照旧映射**（行号是真的，不该丢）。
            //   `CppCompiler` 用的是 `<input>`（那是宿主认得的"正在编的这个文件"占位符）——
            //   **那个不能退**，退了 C++ 的 `ASTNode.OriginalFile` 会跟着变 null，
            //   正好把它已经修好的头文件定位弄坏。
            string? file = e.Item1;
            if (file == UnknownFilePlaceholder) file = null;
            return (file, e.Item2);
        }
        return (null, processedLine);
    }
}

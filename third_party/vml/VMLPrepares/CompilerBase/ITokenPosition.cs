namespace CompilerBase
{
    /// <summary>
    /// 「这个词法单元在源码的哪一行哪一列」—— **跨语言的最小契约**。
    ///
    /// <para>
    /// 22 门前端各有自己的 `Token` 类（互不继承、字段名也不完全一致），
    /// 但**每一门都同时有 `Line` 与 `Column` 这两个属性**（实测 21/21；Scheme 的
    /// `Token` 是主构造函数写法，属性名为 `Line`/`Column` 同样成立）。
    /// `ParserBase` 拿不到它们 —— 它是 `ParserBase&lt;TToken, TTokenType&gt;`，
    /// `TToken` 无约束，基类里写 `token.Line` 编不过。
    /// </para>
    ///
    /// <para>
    /// 于是从前只有两条路：① 二十门各覆写一遍 `GetTokenLine`/`GetTokenColumn`
    /// （**同一规则多处实现**）；② 干脆不覆写、让位置恒为 0（**实际发生的那条**，
    /// 结果是这 20 门的**语法错误一个位置都报不出来**）。
    /// 让 `Token` 挂一个只有两个成员的小接口，位置就**在基类一处**取得到 ——
    /// 每门只要在 `Token` 的声明上加一个接口名，不必写任何实现代码。
    /// </para>
    ///
    /// <para>
    /// ⚠ 行号语义是**预处理之后**的行号。有 `#include` 展开时会整体推后 ——
    /// 需要原文件行号的场合，调用方还要过一遍 `ParserBase.MapOriginal`
    /// （`ResolveDiagnosticPosition` 已经统一这么做了）。
    /// </para>
    /// </summary>
    public interface ITokenPosition
    {
        /// <summary>源码行号（1 基）。</summary>
        int Line { get; }

        /// <summary>源码列号（1 基）。</summary>
        int Column { get; }
    }
}

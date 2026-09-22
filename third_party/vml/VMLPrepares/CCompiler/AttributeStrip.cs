using System;
using System.Text;

namespace CCompiler
{
    /// <summary>
    /// 剥掉 GCC / MSVC 的**属性注解** —— `__attribute__((...))`、`__declspec(...)`、
    /// 以及裸的 `__extension__`。
    ///
    /// ## 为什么在词法之前剥，而不是在解析器里认（v0.96.358）
    ///
    /// 我们**不用属性做任何事**（不做对齐、不做 `noreturn` 分析、不做 `format` 检查），
    /// 它们对这条链纯粹是噪音。但噪音能出现在**十来种语法位置**：
    ///
    /// ```c
    /// void f(int x __attribute__((unused)));          /* 形参 */
    /// __attribute__((unused)) static int g(void);     /* 声明前 */
    /// __attribute__((noreturn)) void die(void) { }    /* 函数定义前 */
    /// struct S { int a __attribute__((packed)); };    /* 成员后 */
    /// typedef int T __attribute__((aligned(16)));     /* typedef 里 */
    /// ```
    ///
    /// 逐个在解析器里认，等于把**同一条规则实现十来遍** —— 而"同一规则两处实现必然漂移"
    /// 是本仓头号坑。在词法之前剥一次，解析器一行都不用改。
    ///
    /// **真实受害程序**：`kilo`（antirez/kilo，约 1270 行）——
    /// `void handleSigWinCh(int unused __attribute__((unused)))` 报
    /// `语法错误：期望 RPAREN，但得到 IDENTIFIER`，整个程序编不过。
    /// 而 GCC 属性在真实老程序里极常见（`unused` / `noreturn` / `packed` / `format` …），
    /// 不是边角写法。
    ///
    /// ## 两条容易被忽略的约束
    ///
    /// ⚠ **必须认字面量与注释** —— 与 `CompilerBase.Preprocessor.StripComments` **同源的坑**
    ///   （那个缺陷在同一天刚修过一次，见 `FRONTEND_DEFECTS.md`）：不认字面量的话，
    ///   字符串里的 `"__attribute__"` 会被当注解**误删**。
    ///   本文件与 `StripComments` 是两条独立的文本扫描、规则相同 —— **改动其一请对照另一个**。
    ///
    /// ⚠ **换行必须原样补回** —— 属性可以跨行写：
    /// ```c
    /// int f(int x __attribute__((unused,
    ///                            deprecated))) { return x; }
    /// ```
    ///   整段吃掉而不补换行，**后面的报错行号就会整体上移**。
    ///   本仓对行号很在意（`lineMap`、逐条诊断定位），所以跳过时把其中的 `\n` 数出来补回去。
    /// </summary>
    public static class AttributeStrip
    {
        private const string AttrGcc   = "__attribute__";
        private const string AttrMsvc  = "__declspec";

        /// <summary>
        /// **按「透明的空白」处理的裸关键字** —— 不带括号，见到整词直接丢掉。
        ///
        /// 判据两条，缺一不可：① 我们**不用它做任何事**；② **丢掉不改变语义**。
        /// 位置难兼容的关键字一律按这条走，**不要在解析器里逐个位置去认它**
        /// （`__attribute__` 能出现在十来个语法位置，逐个认就是把同一条规则实现十来遍）。
        ///
        /// ## 逐条为什么可以丢
        ///
        /// **GCC 系**（`__x__` 双下划线是 GCC 的惯用命名法）：
        /// - `__extension__` —— "我接下来用的是扩展语法"前缀，纯声明性；
        /// - `__inline__` / `__inline` —— 内联**提示**，不改变可观察行为；
        /// - `__restrict` / `__restrict__` / `restrict`（后者是 **C99 标准**关键字）——
        ///   别名限定符，只允许编译器假设指针之间不重叠；我们不做别名优化，丢掉照旧正确。
        /// - `__declspec`（**MSVC**）与 `__attribute__`（GCC）在上面的括号分支里处理。
        ///
        /// **Borland / Turbo C 系**（DOS 时代的老程序大头，见下条边界）：
        /// - `far` / `near` / `huge` —— **内存模型限定符**。在平坦地址空间下本就无意义，
        ///   丢掉是**正确**的（不是"将就"）：它们当年只是告诉 16 位编译器该用哪种远/近指针。
        ///
        /// ## ⚠ 故意**不**加进来的 —— 这些是**有意不支持**，不是"待实现"
        ///
        /// **用户 2026-09-22 定案**：
        /// > 直接操作汇编、直接操作内存的老程序**就不支持**，或者**修改掉才能支持**。
        ///
        /// 所以下面这一族**不要**当透明关键字加进来 —— 它们不是"还没做"，
        /// 而是**已经决定不做**（加了反而会让这类程序**看起来能跑、实际静默编错**，
        /// 比直接编不过更糟）：
        ///
        /// - `_AX` / `_BX` / `_CX` / `_DX` … —— **寄存器伪变量**，它们就是读写寄存器本身；
        /// - `__asm__` / `asm` 块、`__emit__` —— 直接操作汇编 / 直接发射字节；
        /// - 显存直写（`0xB8000` 文本 / `0xA0000` 图形）、`bios.h`、`int86` —— 直接操作内存；
        /// - `interrupt` / `_interrupt` —— ISR 语义（`Lexer.cs` 已认 `interrupt` 这个**词**，
        ///   但中断处理程序本身依赖裸硬件）；
        /// - `__typeof__` / `__builtin_*`（GCC）—— 带操作数、有语义。
        ///
        /// ⚠ 唯一的边界灰色地带是**调用约定**：`_pascal` / `_fastcall` / `pascal` / `fastcall`
        /// 是"不同的调用约定"（参数顺序与清理方都不同），当透明丢掉会**静默编出错误的调用序列**；
        /// 而 `_cdecl` / `cdecl` 与我们的默认一致、丢掉是无操作。**这一族暂不加** ——
        /// 要加也得先确认默认口径，别"看起来是一族"就一起丢。
        ///
        /// 同类边界见 [`docs/老程序兼容性.md`] 的 C 档（"低，且不该做"）与
        /// `gfx_*`/显存/BGI 那一条定案。
        /// </summary>
        private static readonly string[] TransparentKeywords =
        {
            // GCC / C99
            "__extension__",
            "__inline__", "__inline",
            "__restrict", "__restrict__", "restrict",
            // Borland / Turbo C：内存模型限定符（平坦地址空间下无意义）
            "far", "near", "huge",
        };

        /// <summary>把源码里的属性注解与透明关键字剥掉（不改动其余任何字符）。</summary>
        public static string Apply(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;

            // 绝大多数文件一个都没有 —— 先做一次廉价判断，避免无谓地重建整份源码
            if (source.IndexOf(AttrGcc, StringComparison.Ordinal) < 0
                && source.IndexOf(AttrMsvc, StringComparison.Ordinal) < 0
                && !ContainsAnyTransparentKeyword(source))
                return source;

            var sb = new StringBuilder(source.Length);
            int i = 0;
            while (i < source.Length)
            {
                char c = source[i];

                // ── 字符串 / 字符字面量：整段照抄（里面的 __attribute__ 不是注解）──
                if (c == '"' || c == '\'')
                {
                    int end = SkipLiteral(source, i);
                    sb.Append(source, i, end - i);
                    i = end;
                    continue;
                }

                // ── 行注释 / 块注释：整段照抄 ──
                if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
                {
                    int end = source.IndexOf('\n', i);
                    if (end < 0) end = source.Length;
                    sb.Append(source, i, end - i);
                    i = end;
                    continue;
                }
                if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
                {
                    int end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    end = end < 0 ? source.Length : end + 2;
                    sb.Append(source, i, end - i);
                    i = end;
                    continue;
                }

                // ── __attribute__ / __declspec：连同后面配平的括号整段丢掉 ──
                if (IsWordAt(source, i, AttrGcc) || IsWordAt(source, i, AttrMsvc))
                {
                    int len = IsWordAt(source, i, AttrGcc) ? AttrGcc.Length : AttrMsvc.Length;
                    int end = SkipBalancedParens(source, i + len);
                    AppendNewlines(sb, source, i, end);   // 行号不能因此偏移
                    i = end;
                    continue;
                }

                // ── 透明关键字（裸词，不带括号）：整词丢掉 ──
                {
                    string? hit = MatchTransparentKeyword(source, i);
                    if (hit != null)
                    {
                        i += hit.Length;
                        continue;
                    }
                }

                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        private static bool ContainsAnyTransparentKeyword(string source)
        {
            foreach (var kw in TransparentKeywords)
                if (source.IndexOf(kw, StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        /// <summary><paramref name="start"/> 处若正好是某个透明关键字（整词），返回它；否则 null。</summary>
        private static string? MatchTransparentKeyword(string source, int start)
        {
            foreach (var kw in TransparentKeywords)
                if (IsWordAt(source, start, kw)) return kw;
            return null;
        }

        /// <summary>`source[start]` 处是否正好是**整词** <paramref name="word"/>。</summary>
        private static bool IsWordAt(string source, int start, string word)
        {
            if (start + word.Length > source.Length) return false;
            if (string.CompareOrdinal(source, start, word, 0, word.Length) != 0) return false;
            // 词边界：前后都不能紧邻标识符字符（否则 `my__attribute__x` 会被误判）
            if (start > 0 && IsIdentChar(source[start - 1])) return false;
            int after = start + word.Length;
            if (after < source.Length && IsIdentChar(source[after])) return false;
            return true;
        }

        private static bool IsIdentChar(char c)
            => char.IsLetterOrDigit(c) || c == '_' || c == '$';

        /// <summary>跳过起于 <paramref name="start"/> 的字面量，返回**收尾引号之后**的下标。</summary>
        private static int SkipLiteral(string source, int start)
        {
            char quote = source[start];
            int i = start + 1;
            while (i < source.Length)
            {
                if (source[i] == '\\' && i + 1 < source.Length) { i += 2; continue; } // 转义
                bool closed = source[i] == quote;
                i++;
                if (closed) break;
            }
            return i;
        }

        /// <summary>
        /// 从 <paramref name="start"/> 起（先跳过空白）吃掉一个配平的括号组，返回其后下标。
        /// 括号内若出现字面量，其括号不计入深度。
        /// </summary>
        private static int SkipBalancedParens(string source, int start)
        {
            int i = start;
            while (i < source.Length && char.IsWhiteSpace(source[i])) i++;
            if (i >= source.Length || source[i] != '(') return start; // 没跟括号：不动（如 `__declspec` 被当标识符用）

            int depth = 0;
            while (i < source.Length)
            {
                char c = source[i];
                if (c == '"' || c == '\'') { i = SkipLiteral(source, i); continue; }
                if (c == '(') depth++;
                else if (c == ')')
                {
                    depth--;
                    i++;
                    if (depth == 0) return i;
                    continue;
                }
                i++;
            }
            return i; // 括号不配平（源码本身有错）：吃到末尾，交给后面的诊断去报
        }

        /// <summary>把 <c>[from, to)</c> 区间里的换行补进 <paramref name="sb"/>，使行号不偏移。</summary>
        private static void AppendNewlines(StringBuilder sb, string source, int from, int to)
        {
            for (int k = from; k < to && k < source.Length; k++)
                if (source[k] == '\n') sb.Append('\n');
        }
    }
}

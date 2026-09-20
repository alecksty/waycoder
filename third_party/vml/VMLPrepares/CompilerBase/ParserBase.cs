using System;
using System.Collections.Generic;

namespace CompilerBase
{
    /// <summary>
    /// 所有语言编译器 Parser 的抽象基类。
    /// 消除 22 个编译器中重复的 Peek/Advance/Match/Check/Expect 辅助方法。
    ///
    /// 迁移指南（从手动实现迁移到 ParserBase）:
    ///   Peek() → Cur          (当前 token)
    ///   Peek(0) → Cur         (当前 token)
    ///   Peek(1) → Peek(1)     (不变)
    ///   PeekNext() → Peek(1)
    ///   Advance() → Advance() (不变)
    ///   Match(type) → Match(type) (不变)
    ///   MatchAny(a,b) → Match(a,b) (统一命名)
    ///   Check(type) → Check(type) (不变)
    ///   Expect(type) → Expect(type) (新增无消息重载)
    ///   Expect(type, msg) → Expect(type, msg) (不变)
    ///   Consume(type, msg) → Expect(type, msg) (统一命名)
    ///   Current()/CurrentToken → Cur
    ///   AtEnd()/IsEnd → IsAtEnd
    /// </summary>
    /// <typeparam name="TToken">语言特定的 Token 类型</typeparam>
    /// <typeparam name="TTokenType">语言特定的 TokenType 枚举</typeparam>
    public abstract class ParserBase<TToken, TTokenType> where TTokenType : struct, Enum
    {
        protected readonly List<TToken> _tokens;
        protected int _pos;

        protected ParserBase(List<TToken> tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            _pos = 0;
        }

        /// <summary>当前 Token（越界安全：_pos 超出范围时返回 EOF 哨兵而非崩溃）</summary>
        protected TToken Cur => _pos < _tokens.Count ? _tokens[_pos] : _tokens[^1];

        /// <summary>
        /// **诊断取位置**用的当前 Token —— 默认就是 <see cref="Cur"/>。
        ///
        /// ⚠ 为什么不能直接用 `Cur`：**有的前端自己维护游标**。
        /// `CSharpCompiler.Parser` 把 `Peek`/`Advance`/`Check`/`IsAtEnd`/`Previous`
        /// 全部 `new` 掉了（见它自己 `ParseStatement` 那段注释），基类的 `_pos`
        /// **从不移动** ⇒ 读 `Cur` 永远拿到 `tokens[0]`（第一行那个 `class`），
        /// 于是它报的**每条语法错误都是 `1:1`**。
        ///
        /// ⚠ 为什么不改成"让 C# 同步维护 `_pos`"：那就是**两个游标**，
        /// 必然漂移（本仓反复踩的「同一件事两处实现」）。这里的接缝是**一个问题**：
        /// 「你这门语言的当前位置在哪」—— 自维护游标的前端覆写本属性返回它自己的
        /// 当前 Token 即可，位置仍然只有 `ResolveDiagnosticPosition` 一处取法。
        /// </summary>
        protected virtual TToken CurrentToken => Cur;

        /// <summary>Token 总数</summary>
        protected int TokenCount => _tokens.Count;

        /// <summary>源文件名（用于 GCC 风格诊断）</summary>
        public string? FileName { get; set; }
        /// <summary>GCC 风格诊断收集器（设置后 Error/GccError 将使用新格式）</summary>
        public DiagnosticBag? Diagnostics { get; set; }

        /// <summary>
        /// 预处理行号映射（与 <see cref="CompilerBase.LexerBase.SourceLineMap"/> 同一份数据）。
        ///
        /// <para>
        /// 解析期诊断的位置必须**映射回原文件**，否则 `#include` 一展开，行号就整体后移 ——
        /// 用户看到的是「我文件里那一行没问题，编译器却指着它报错」。
        /// </para>
        /// </summary>
        public List<(string, int)>? SourceLineMap
        {
            // 显式设进来的优先（C/C++ 走这条，语义零变化）；没设就**认领生效中那一份** ——
            // 解析器手里只有 token 列表，拿不到源码字符串，无法自己去比对，
            // 所以由词法器在构造时认领（`LexerBase.RefreshLineMap`），这里取用。
            // 21 门语言因此**一个字段都不用加**就能把头文件里的错报到头文件上去。
            get => _sourceLineMap ?? CompilerHelper.ActiveLineMap;
            set => _sourceLineMap = value;
        }

        private List<(string, int)>? _sourceLineMap;

        /// <summary>
        /// 「预处理拼接行号」→「(原文件, 原行)」——规则本体在
        /// <see cref="CompilerHelper.MapOriginalLine"/> **一处**（词法器那边也是转调它），
        /// 别在解析器里再写一遍 `lineMap[line - 1]`。
        /// </summary>
        protected (string? File, int Line) MapOriginal(int processedLine)
            => CompilerHelper.MapOriginalLine(SourceLineMap, processedLine);

        /// <summary>是否到达输入末尾（默认停在最后一个 Token 前，因为大多数编译器在末尾放置 EOF 哨兵）</summary>
        protected virtual bool IsAtEnd => _pos >= _tokens.Count - 1;

        /// <summary>向前查看 n 个 Token（默认 1=下一个），越界返回最后一个 Token。n=0 等价于 Cur</summary>
        protected TToken Peek(int n = 1) =>
            _pos + n < _tokens.Count ? _tokens[_pos + n] : _tokens[^1];

        /// <summary>消费当前 Token 并返回，_pos 后移（越界安全：_pos 超出范围时返回 EOF 哨兵而非崩溃）</summary>
        protected TToken Advance() => _pos < _tokens.Count ? _tokens[_pos++] : _tokens[^1];

        /// <summary>返回上一个被消费的 Token</summary>
        protected TToken Previous() => _pos > 0 ? _tokens[_pos - 1] : _tokens[0];

        // ---- 子类必须实现或可选覆写 ----

        protected abstract TTokenType GetTokenType(TToken token);

        /// <summary>
        /// 取 Token 的行号（供 GCC 错误格式使用）。
        ///
        /// **默认实现走 <see cref="ITokenPosition"/>**：22 门语言的 `Token` 各自定义了
        /// `Line`/`Column` 属性，但基类拿不到（`TToken` 是无约束泛型）—— 从前这里返回 0、
        /// 而那 20 门又没覆写 ⇒ `GccError` 拼出来的位置恒是 `文件:0:0`，于是**语法错误**那条
        /// 通路只能退化成不带位置的裸消息（见 <see cref="Error"/>）。
        /// 让 Token 实现一个只有一个契约的小接口，位置就**在基类一处**取得到，
        /// 不必二十门各写一遍（本仓头号坑：同一规则多处实现）。
        ///
        /// C/Cpp 仍覆写它 —— 它们的 Token 有 `OriginalLine`（`#include` 展开前的行号），
        /// 覆写版的语义更精确，**优先于**这条通用实现。
        /// </summary>
        protected virtual int GetTokenLine(TToken token) => token is ITokenPosition p ? p.Line : 0;

        /// <summary>取 Token 的列号（语义见 <see cref="GetTokenLine"/>）。</summary>
        protected virtual int GetTokenColumn(TToken token) => token is ITokenPosition p ? p.Column : 0;

        // ---- 类型检查 ----

        /// <summary>检查当前 Token 是否为指定类型（不消费）</summary>
        protected bool Check(TTokenType type) =>
            !IsAtEnd && EqualityComparer<TTokenType>.Default.Equals(GetTokenType(Cur), type);

        /// <summary>检查当前 Token 是否为任意一个指定类型（不消费）。用于需要多类型判断的场景（如 C#/Java 的修饰符检查）</summary>
        protected bool Check(params TTokenType[] types)
        {
            if (IsAtEnd) return false;
            var curType = GetTokenType(Cur);
            foreach (var t in types)
                if (EqualityComparer<TTokenType>.Default.Equals(curType, t))
                    return true;
            return false;
        }

        /// <summary>如果当前 Token 匹配指定类型则消费，返回是否成功</summary>
        protected bool Match(TTokenType type)
        {
            if (Check(type)) { Advance(); return true; }
            return false;
        }

        /// <summary>如果当前 Token 匹配任意一个类型则消费</summary>
        protected bool Match(params TTokenType[] types)
        {
            foreach (var t in types)
                if (Check(t)) { Advance(); return true; }
            return false;
        }

        // ---- 期望/错误 ----

        /// <summary>期望当前 Token 为指定类型，否则抛出 ParseException</summary>
        protected virtual TToken Expect(TTokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(message);
        }

        /// <summary>期望当前 Token 为指定类型（自动生成错误消息），否则抛出 ParseException。
        /// 用于 Go/Forth/Python 等不需要自定义消息的编译器。</summary>
        protected TToken Expect(TTokenType type)
        {
            if (Check(type)) return Advance();
            var got = IsAtEnd ? "EOF" : GetTokenType(Cur).ToString();
            throw Error($"期望 {type}，实际得到 {got}");
        }

        /// <summary>
        /// 当前 Token 该报到哪个位置 —— **全仓唯一的解析期位置判据**。
        ///
        /// `GccError`（走 Diagnostics 收集）与 <see cref="Error"/>（直接抛）两条出口
        /// 从前**各算一遍**：前者拼 `文件:行:列`，后者干脆只给裸消息。
        /// 结果是同一件事两套答案 —— 20 门语言的**语法错误**（全部走 `Error`）
        /// 一个位置都拿不到，哪怕 `ParseException` 里明明带着 Token。
        /// 现在只有这一处算位置，两条出口都从它取。
        /// </summary>
        protected (string File, int Line, int Column) ResolveDiagnosticPosition()
        {
            var cur = CurrentToken;
            var line = GetTokenLine(cur);
            var col = GetTokenColumn(cur);
            // 位置映射回**原文件**（`#include` 展开会把行号整体推后；没有映射时原样退回）
            var (originFile, originLine) = MapOriginal(line);
            return (originFile ?? FileName ?? "<input>", originLine, col);
        }

        /// <summary>GCC 风格错误报告（如果 Diagnostics 设置则收集，否则抛出）</summary>
        protected void GccError(string message, ErrorCode code = ErrorCode.Unknown)
        {
            var (file, line, col) = ResolveDiagnosticPosition();
            if (Diagnostics != null)
            {
                Diagnostics.AddError(file, line, col, code, message);
            }
            else
            {
                throw new ParseException(code, FormatDiagnostic(file, line, col, message));
            }
        }

        /// <summary>
        /// 组装一条 GCC 风格报文 `文件:行:列: error: 消息`。
        ///
        /// ⚠ **级别词固定 `error`**：宿主侧 `VmlDiagnostics` 的 4 条正则按它判级别、
        /// 给气泡配色；`vml-diag-probe` 也有按这个子串判"编不过"的用例。改了它，
        /// 警告会被当成错误、或者反过来 —— 这条**不许动**。
        /// </summary>
        private static string FormatDiagnostic(string file, int line, int col, string message)
            => $"{file}:{line}:{col}: error: {message}";

        /// <summary>
        /// 创建带当前位置信息的 ParseException。
        ///
        /// ⚠ **位置必须拼进 `Message`**：宿主（CLI/MAUI/LSP）拿到的就是 `ex.Message`，
        /// 它**不读** `ParseException.Token`。从前这里返回的是裸消息
        ///（`new ParseException(message, Cur)`），于是 20 门语言的语法错误在编辑器里
        /// **锚不到任何一行** —— 用户看到一句「意外的 token」却不知道在哪。
        /// 位置信息一直都在（`Cur` 就在手上），只是没往外送。
        ///
        /// 列/行取法与 `GccError` 完全同源（<see cref="ResolveDiagnosticPosition"/>）。
        /// </summary>
        protected virtual ParseException Error(string message)
        {
            var (file, line, col) = ResolveDiagnosticPosition();
            // **位置用「字段」给，不用「拼好的字符串」给** —— `ParseException` 自己会把
            // 三段拼进 `Message`，同时把位置与正文各留一份。外层
            // `CompilerHelper.CompileWithDiagnostics` 要用分开的两份去调 `AddError`，
            // 直接塞拼好的字符串就会拼出两层前缀（见 `ParseException.BareMessage`）。
            return new ParseException(ErrorCode.Unknown, message, CurrentToken!, file, line, col);
        }

        /// <summary>
        /// 与 <see cref="Error"/> **同源**，但位置取**指定的那个 token**。
        ///
        /// <para>
        /// 用在「错在**上一个** token 之后」这一类：解析器往往是看到**下一个** token 才发现
        /// 前面缺了东西的，把位置钉在 `Cur` 上就会报到**下一行**去。
        /// 典型是缺操作数：`let c = a +` 要等换行后遇到 `}` 才发作 ——
        /// 报在 `}` 上（下一行）用户找不到，报在 `+` 上（同一行）才是"出错的那一行"。
        /// </para>
        ///
        /// <para>
        /// 判据：**位置取"缺口在哪"，文案取"看到了什么"** —— GCC 也是这么做的
        /// （`expected expression before '}' token` 指在 `+` 上）。
        /// 两个都要给对，不能只给一个。
        /// </para>
        /// </summary>
        protected ParseException ErrorAt(string message, TToken anchor)
        {
            var (file, line, col) = ResolveAnchor(anchor);
            return new ParseException(ErrorCode.Unknown, message, anchor, file, line, col);
        }

        /// <summary>该 token 是**表达式收尾符**（`)` / `]` / `}` / `,` / `:` / `;` / 换行 / EOF）。
        /// 逐语言给 —— 只有各门自己知道自己的 TokenType 长什么样。</summary>
        protected virtual bool IsExpressionCloser(TToken token) => false;

        /// <summary>该 token 是**语句分隔**（换行 / `;`）。同样逐语言给。</summary>
        protected virtual bool IsStatementSeparator(TToken token) => false;

        /// <summary>
        /// 报「表达式缺失」时该把位置**锚在哪个 token** 上 —— 缺口比撞上的 token 更早时返回上一个，
        /// 否则返回当前 token。三个判据，缺一不可：
        ///
        /// <list type="number">
        /// <item><b>撞上的必须是收尾符</b>。收尾符**永远不会是表达式的开头**，所以撞上它就说明
        /// 缺口在**它之前**（`x = 1 +` 换行后才遇到 `NEWLINE`，报 `NEWLINE` 就跑到第 5 行去了，
        /// 而错在第 4 行）。反过来，撞上的若是普通 token（比如行首一个不该出现的符号），
        /// **它自己就是问题**，锚到上一个 token 只会报到上一行去。</item>
        /// <item><b>上一个不能是语句分隔</b>。`x = 1` 换行后跟一个 `)`：缺口在本行**行首**，
        /// 锚到上一行的 `NEWLINE` 是错的（那一格正好被第 1 条放行进来）。</item>
        /// <item><b>两者要跨行</b>。同行就直接报撞上的那个（`f(a,)` 报 `)` 是对的）——
        /// 锚定只用来解决「缺口在上一行」这一件事。</item>
        /// </list>
        ///
        /// 本仓的判据是**位置取"缺口在哪"、文案取"看到了什么"**（见 <see cref="ErrorAt"/>）。
        /// 用法：<c>ErrorAt($"…{Cur.Type}…", GapAnchor())</c>。
        /// </summary>
        protected TToken GapAnchor()
        {
            var prev = Previous();
            if (!IsExpressionCloser(CurrentToken)) return CurrentToken;
            if (IsStatementSeparator(prev)) return CurrentToken;
            if (GetTokenLine(CurrentToken) == GetTokenLine(prev)) return CurrentToken;
            return prev;
        }

        /// <summary>
        /// <see cref="GccError"/> 的锚定版：**收集**（不抛）到**指定 token** 的位置上。
        ///
        /// 与 <see cref="ErrorAt"/> / <see cref="GccError"/> 的分工是同一套：
        /// 「锚定与否」×「收集还是抛」四种组合，位置计算只有 <see cref="ResolveAnchor"/> 一处。
        /// 用在**行/块结尾才发现缺口**的语言上 —— BASIC 与 Swift 的语句以换行收尾，
        /// `PRINT 1 +` 的下一个 token 是**下一行**的 `EOF`，按当前位置收集就报到了下一行。
        /// </summary>
        protected void GccErrorAt(string message, TToken anchor, ErrorCode code = ErrorCode.Unknown)
        {
            var (file, line, col) = ResolveAnchor(anchor);
            if (Diagnostics != null)
            {
                Diagnostics.AddError(file, line, col, code, message);
            }
            else
            {
                throw new ParseException(code, message, anchor, file, line, col);
            }
        }

        /// <summary>
        /// 把某个 token 解析成 `(文件, 行, 列)` —— **锚定类诊断的唯一位置计算**。
        ///
        /// 与 <see cref="ResolveDiagnosticPosition"/> 的差别只有"锚在哪一个 token"：
        /// 那边锚 `CurrentToken`（当前位置），这边锚调用方给的那个。
        /// 行号映射（`#include` 展开要把行号换回原文件）走的是**同一套**，
        /// 两处各写一遍必然漂移。
        /// </summary>
        private (string File, int Line, int Column) ResolveAnchor(TToken anchor)
        {
            var line = GetTokenLine(anchor);
            var col = GetTokenColumn(anchor);
            var (originFile, originLine) = MapOriginal(line);
            return (originFile ?? FileName ?? "<input>", originLine, col);
        }

        /// <summary>
        /// 把一条**已经算好位置**的语法错误收进诊断（**不抛**），供容错恢复用。
        /// 返回是否收下了 —— 没收集器时返回 false，调用方应让它继续往外抛。
        ///
        /// <para>
        /// **为什么需要它**：错误分两类，容错策略也必须分两类。
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// **能继续的错误**（少个操作数、少个分号、某个 token 不该出现…）：
        /// 解析器手上还握得住局面，**应当报出来然后接着编** —— 这样一份文件里的
        /// 后面几处错也能一起报给用户，而不是改一个、编一次、再看下一个。
        /// </item>
        /// <item>
        /// **无法继续的错误**（词法器坏了、内部状态不可信）：
        /// 继续编只会级联出一堆假错，**应当当场停**。
        /// </item>
        /// </list>
        ///
        /// <para>
        /// ⚠ **"恢复"与"吞掉"是两件事，本仓在这上面栽过**：
        /// 前端顶层那种 `catch (Exception) { …跳过、继续… }` 把**语法错误一起吞了**，
        /// 一行日志都不留 ⇒ 用户零错误提示、程序照常编出来（"能跑但少一段"），
        /// 到代码生成才崩成一句没有位置的「内部错误」。
        /// 正确做法是**先收进诊断、再恢复**：错报了、编译整体照样失败，
        /// 而用户一次能看到尽可能多的错。本方法就是那个"先收进诊断"。
        /// </para>
        ///
        /// <para>
        /// 典型用法是**异常过滤器**，没有收集器时自动退回抛出：
        /// <c>catch (ParseException ex) when (Collect(ex)) { /* 恢复 */ }</c>
        /// </para>
        /// </summary>
        protected bool Collect(ParseException ex)
        {
            if (Diagnostics == null) return false;
            // 位置与正文都用**分开的字段**（`AddError` 自己会拼前缀；
            // 塞已经拼好的 `Message` 会得到两层前缀，见 `ParseException.BareMessage`）。
            Diagnostics.AddError(ex.File, ex.Line, ex.Column, ex.Code, ex.BareMessage);
            return true;
        }
    }
}

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
            var cur = Cur;
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
            return new ParseException(ErrorCode.Unknown, message, Cur!, file, line, col);
        }
    }
}

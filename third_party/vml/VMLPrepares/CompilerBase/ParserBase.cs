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

        /// <summary>获取 Token 的行号（供 GCC 错误格式使用）。子类可覆写以返回真实行号。</summary>
        protected virtual int GetTokenLine(TToken token) => 0;
        /// <summary>获取 Token 的列号（供 GCC 错误格式使用）。子类可覆写以返回真实列号。</summary>
        protected virtual int GetTokenColumn(TToken token) => 0;

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
            throw Error($"Expected {type}, but got {got}");
        }

        /// <summary>GCC 风格错误报告（如果 Diagnostics 设置则收集，否则抛出）</summary>
        protected void GccError(string message, ErrorCode code = ErrorCode.Unknown)
        {
            var cur = Cur;
            var line = GetTokenLine(cur);
            var col = GetTokenColumn(cur);
            if (Diagnostics != null)
            {
                Diagnostics.AddError(FileName ?? "<input>", line, col, code, message);
            }
            else
            {
                throw new ParseException(code, $"{FileName ?? "<input>"}:{line}:{col}: error: {message}");
            }
        }

        /// <summary>创建带当前位置信息的 ParseException。
        /// 子类可覆写以提供更丰富的行/列信息。</summary>
        protected virtual ParseException Error(string message)
        {
            return new ParseException(message, Cur!);
        }
    }
}

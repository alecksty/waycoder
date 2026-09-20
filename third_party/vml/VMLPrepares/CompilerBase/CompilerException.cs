using System;

namespace CompilerBase
{
    /// <summary>词法/语法分析异常, 携带错误码和可选 token</summary>
    public class ParseException : Exception
    {
        public ErrorCode Code { get; }
        public object? Token { get; }

        /// <summary>报错位置（1 基）；**0 = 未知**（= 消息里没有位置前缀）。</summary>
        public int Line { get; }
        /// <summary>报错列号（1 基）；0 = 未知。</summary>
        public int Column { get; }
        /// <summary>报错所在文件；null = 未知。</summary>
        public string? File { get; }

        /// <summary>
        /// **不含位置前缀**的消息正文。
        ///
        /// 与 <see cref="Exception.Message"/> 的关系：`Message` = 有位置时
        /// `文件:行:列: error: 正文`、没有时就是正文。两者**不是两份数据** ——
        /// `Message` 在构造函数里由 `<see cref="File"/>`/`<see cref="Line"/>`/
        /// `<see cref="Column"/>`/`BareMessage` **拼出来一次**，此后只读。
        /// 之所以要把正文也留一份：外层 `CompilerHelper.CompileWithDiagnostics` 要用
        /// 「位置 + 正文」两个**分开的**字段去调 `DiagnosticBag.AddError`，
        /// 而 `AddError` 自己会拼前缀 —— 直接把已带前缀的 `Message` 塞进去就会变成
        /// `&lt;input&gt;: error: &lt;input&gt;:4:18: error: …`（实测踩过），
        /// 用户看到的第一个 `error:` 前面没有任何位置。
        /// </summary>
        public string BareMessage { get; }

        /// <summary>
        /// 带位置的构造：`Message` 会被拼成 **GCC 形态 `文件:行:列: error: 消息`**。
        ///
        /// ⚠ 位置必须拼进 `Message`：**不经过 `CompileWithDiagnostics` 的调用方
        /// 只拿得到 `ex.Message`**（探针、部分前端的 `Compile(string)` 入口就是如此），
        /// 不拼进去他们就看不到位置。
        /// ⚠ `line &lt;= 0` 时**不加任何前缀** —— 位置未知就如实说"不知道"，
        /// 别编一个 `文件:0:0:` 出来（那不是 GCC 语法，宿主侧正则也认不出）。
        /// </summary>
        public ParseException(ErrorCode code, string message, object? token = null,
                              string? file = null, int line = 0, int column = 0)
            : base(line > 0 ? $"{file ?? "<input>"}:{line}:{column}: error: {message}" : message)
        {
            Code = code;
            Token = token;
            File = file;
            Line = line;
            Column = column;
            BareMessage = message;
        }

        /// <summary>不带位置的构造（位置未知，`Message` 就是原文）。</summary>
        public ParseException(string message, object? token = null) : this(ErrorCode.Unknown, message, token) { }
    }

    /// <summary>编译异常(可包装词法/语法/代码生成异常), 携带错误码</summary>
    public class CompilationException : Exception
    {
        public ErrorCode Code { get; }

        public CompilationException(ErrorCode code, string message) : base(message) { Code = code; }
        public CompilationException(ErrorCode code, string message, Exception innerException) : base(message, innerException) { Code = code; }
        public CompilationException(string message) : this(ErrorCode.Unknown, message) { }
        public CompilationException(string message, Exception innerException) : this(ErrorCode.Unknown, message, innerException) { }
    }

    /// <summary>代码生成异常, 携带错误码</summary>
    public class CodeGenerationException : Exception
    {
        public ErrorCode Code { get; }

        public CodeGenerationException(ErrorCode code, string message) : base(message) { Code = code; }
        public CodeGenerationException(string message) : this(ErrorCode.Unknown, message) { }
    }
}

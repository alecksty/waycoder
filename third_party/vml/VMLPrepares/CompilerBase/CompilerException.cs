using System;

namespace CompilerBase
{
    /// <summary>词法/语法分析异常, 携带错误码和可选 token</summary>
    public class ParseException : Exception
    {
        public ErrorCode Code { get; }
        public object? Token { get; }

        public ParseException(ErrorCode code, string message, object? token = null) : base(message)
        {
            Code = code;
            Token = token;
        }
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

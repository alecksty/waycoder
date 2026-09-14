namespace VMLPlugins
{
    public enum WarningLevel
    {
        None = 0,
        Normal = 1,
        Extra = 2,
        All = 3,
    }

    public class CompilerWarning
    {
        public string CompilerName { get; }
        public string Message { get; }
        public string? File { get; }
        public int Line { get; }
        public bool IsError { get; }

        public CompilerWarning(string compiler, string message, string? file = null, int line = 0, bool isError = false)
        {
            CompilerName = compiler;
            Message = message;
            File = file;
            Line = line;
            IsError = isError;
        }

        public override string ToString()
        {
            var tag = IsError ? "error" : "warning";
            var loc = File != null ? $"{File}:{Line}: " : "";
            return $"{loc}{CompilerName}: {tag}: {Message}";
        }
    }

    /// <summary>
    /// 编译警告收集器 — [ThreadStatic] 隔离，支持多线程并行编译。
    /// 后续可改为实例模式，由 CompilerOptions 持有。
    /// </summary>
    public static class WarningEmitter
    {
        [ThreadStatic]
        private static List<CompilerWarning>? _warnings;

        private static List<CompilerWarning> WarningsList => _warnings ??= new();

        public static IReadOnlyList<CompilerWarning> Warnings => WarningsList.AsReadOnly();
        public static bool HasErrors => WarningsList.Any(w => w.IsError);
        public static int Count => WarningsList.Count;

        public static void Clear() => WarningsList.Clear();

        public static void Emit(string compiler, string message, string? file = null, int line = 0)
        {
            var opts = CompilerOptionsContext.Current;
            var level = opts.WarningLevel;
            if (level == 0) return;

            var warn = new CompilerWarning(compiler, message, file, line, isError: opts.WarningsAsErrors);
            WarningsList.Add(warn);
            Console.WriteLine(warn.ToString());
        }

        public static void Error(string compiler, string message, string? file = null, int line = 0)
        {
            var warn = new CompilerWarning(compiler, message, file, line, isError: true);
            WarningsList.Add(warn);
            Console.Error.WriteLine(warn.ToString());
        }

        /// <summary>
        /// 检查是否有警告/错误，如有则抛异常（供编译流程使用）
        /// </summary>
        public static void ThrowOnErrors()
        {
            if (HasErrors)
                throw new System.InvalidOperationException($"编译失败: {WarningsList.Count} 个错误");
        }
    }
}

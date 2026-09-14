namespace VMLPlugins
{
    /// <summary>
    /// 编译器选项上下文 — 线程静态单例，VMLTool 在调用编译前设置，
    /// 各编译器在编译时读取。无需修改 IFrontendCompiler 接口。
    /// </summary>
    public static class CompilerOptionsContext
    {
        [ThreadStatic]
        private static CompilerOptions? _current;

        public static CompilerOptions Current
        {
            get => _current ??= new CompilerOptions();
            set => _current = value;
        }

        /// <summary>
        /// 使用给定选项执行编译动作
        /// </summary>
        public static T RunWith<T>(CompilerOptions options, Func<T> action)
        {
            var prev = _current;
            _current = options;
            try { return action(); }
            finally { _current = prev; }
        }
    }
}

using System.Collections.Generic;

namespace CompilerBase
{
    /// <summary>
    /// 通用静态编译器 CLI 入口（消除 Program.cs 样板重复）。
    /// 适用于调用 XxxCompiler.CompileFileWithIncludes() 静态方法的编译器。
    /// </summary>
    public sealed class SimpleCompilerProgram : CompilerProgramBase
    {
        private readonly string _languageName;
        private readonly string _envVarName;
        private readonly System.Func<string, List<string>, List<string>, string> _compileFunc;

        public SimpleCompilerProgram(string languageName, string envVarName,
            System.Func<string, List<string>, List<string>, string> compileFunc)
        {
            _languageName = languageName;
            _envVarName = envVarName;
            _compileFunc = compileFunc;
        }

        protected override string LanguageName => _languageName;
        protected override string EnvVarName => _envVarName;
        protected override string Compile(string sourceFile, List<string> includePaths, List<string> libraryPaths)
            => _compileFunc(sourceFile, includePaths, libraryPaths);
    }

    /// <summary>
    /// 通用实例编译器 CLI 入口（消除 Program.cs 样板重复）。
    /// 适用于创建编译器实例、调用 compiler.Compile(source, debugMode) 的编译器。
    /// </summary>
    public sealed class SimpleInstanceCompilerProgram : CompilerProgramInstanceBase
    {
        private readonly string _languageName;
        private readonly System.Func<string, bool, VMLAssembler.VmlProgram> _compileFunc;

        public SimpleInstanceCompilerProgram(string languageName,
            System.Func<string, bool, VMLAssembler.VmlProgram> compileFunc)
        {
            _languageName = languageName;
            _compileFunc = compileFunc;
        }

        protected override string LanguageName => _languageName;
        protected override VMLAssembler.VmlProgram CompileSource(string sourceCode, bool debugMode)
            => _compileFunc(sourceCode, debugMode);
    }
}

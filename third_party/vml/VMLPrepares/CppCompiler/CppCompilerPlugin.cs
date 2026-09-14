using CompilerBase;
using VMLAssembler;

namespace CppCompiler
{
    public class CppCompilerPlugin : CompilerPluginExBase
    {
        public override string Name => "cpp";
        public override string Description => "C++语言编译器 (C++17标准子集)";
        public override string SupportedExtensions => ".cpp,.cc,.cxx,.hpp,.hh";

        protected override System.Func<string, VmlProgram> CompileFunc
            => source => CppCompiler.Compile(source);

        protected override System.Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc
            => CppCompiler.CompileFile;

        protected override System.Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc
            => CppCompiler.CompileFileWithIncludes;
    }
}

using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace PythonCompiler
{
    public class PythonCompilerPlugin : CompilerPluginExBase
    {
        public override string Name               => "python";
        public override string Description        => "Python语言编译器 (支持Python语法子集)";
        public override string SupportedExtensions => ".py,.python";

        protected override System.Func<string, VmlProgram> CompileFunc
            => PythonCompiler.Compile;

        protected override System.Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc
            => PythonCompiler.CompileFile;

        protected override System.Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc
            => PythonCompiler.CompileFileWithIncludes;
    }
}

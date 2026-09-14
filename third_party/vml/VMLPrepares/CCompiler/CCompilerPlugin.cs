using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace CCompiler
{
    /// <summary>
    /// C编译器插件适配器
    /// </summary>
    public class CCompilerPlugin : CompilerPluginExBase
    {
        public override string Name               => "C";
        public override string Description        => "C语言编译器 (C99标准)";
        public override string SupportedExtensions => ".c,.h";

        protected override System.Func<string, VmlProgram> CompileFunc
            => source => CCompiler.Compile(source);

        protected override System.Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc
            => CCompiler.CompileFile;

        protected override System.Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc
            => CCompiler.CompileFileWithIncludes;
    }
}

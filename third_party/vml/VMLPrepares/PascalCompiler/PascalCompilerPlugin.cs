using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace PascalCompiler
{
    /// <summary>
    /// Pascal编译器插件适配器
    /// </summary>
    public class PascalCompilerPlugin : CompilerPluginExBase
    {
        public override string Name               => "pascal";
        public override string Description        => "Pascal语言编译器 (支持Pascal语法子集)";
        public override string SupportedExtensions => ".pas,.pascal";

        protected override System.Func<string, VmlProgram> CompileFunc
            => source => PascalCompiler.Compile(source);

        protected override System.Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc
            => PascalCompiler.CompileFile;

        protected override System.Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc
            => PascalCompiler.CompileFileWithIncludes;
    }
}

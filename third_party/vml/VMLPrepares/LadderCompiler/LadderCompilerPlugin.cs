using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace LadderCompiler
{
    public class LadderCompilerPlugin : CompilerPluginExBase
    {
        public override string Name               => "ladder";
        public override string Description        => "梯形图语言编译器 (工业控制语言)";
        public override string SupportedExtensions => ".ld,.ladder";

        protected override System.Func<string, VmlProgram> CompileFunc
            => LadderCompiler.Compile;

        protected override System.Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc
            => LadderCompiler.CompileFile;

        protected override System.Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc
            => LadderCompiler.CompileFileWithIncludes;
    }
}

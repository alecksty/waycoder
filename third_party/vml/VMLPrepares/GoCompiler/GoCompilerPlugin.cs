using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace GoCompiler
{
    /// <summary>
    /// Go编译器插件 — 继承 CompilerPluginExBase，委托给 GoCompiler 静态类
    /// </summary>
    public class GoCompilerPlugin : CompilerPluginExBase
    {
        public override string Name => "go";
        public override string Description => "Go语言编译器 (并发语言)";
        public override string SupportedExtensions => ".go";
        protected override Func<string, VmlProgram> CompileFunc => GoCompiler.Compile;
        protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => GoCompiler.CompileFile;
        protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => GoCompiler.CompileFileWithIncludes;
    }
}

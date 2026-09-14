using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace ForthCompiler
{
    /// <summary>
    /// Forth编译器插件 — 继承 CompilerPluginExBase，委托给 ForthCompiler 静态类
    /// </summary>
    public class ForthCompilerPlugin : CompilerPluginExBase
    {
        public override string Name => "forth";
        public override string Description => "Forth语言编译器 (栈式语言)";
        public override string SupportedExtensions => ".fth,.forth";
        protected override Func<string, VmlProgram> CompileFunc => ForthCompiler.Compile;
        protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => ForthCompiler.CompileFile;
        protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => ForthCompiler.CompileFileWithIncludes;
    }
}

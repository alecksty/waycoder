using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace RustCompiler
{
    /// <summary>
    /// Rust编译器插件 — 继承 CompilerPluginExBase，委托给 RustCompiler 静态类
    /// </summary>
    public class RustCompilerPlugin : CompilerPluginExBase
    {
        public override string Name => "rust";
        public override string Description => "Rust语言编译器 (系统语言)";
        public override string SupportedExtensions => ".rs,.rust";
        protected override Func<string, VmlProgram> CompileFunc => RustCompiler.Compile;
        protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => RustCompiler.CompileFile;
        protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => RustCompiler.CompileFileWithIncludes;
    }
}

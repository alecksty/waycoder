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
        /// <summary>
        /// `.fs` 是 **gforth 的默认扩展名**（`gforth foo.fs`），也是最常见的一种 ——
        /// 此前只登记了 `.fth`/`.forth`，于是 `Examples/forth/parserexp_demo.fs` 被
        /// CLI 判成「认不出这个扩展名」而拒绝编译（**不是语言的问题，是派发表少一条**）。
        /// </summary>
        public override string SupportedExtensions => ".fth,.forth,.fs";
        protected override Func<string, VmlProgram> CompileFunc => ForthCompiler.Compile;
        protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => ForthCompiler.CompileFile;
        protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => ForthCompiler.CompileFileWithIncludes;
    }
}

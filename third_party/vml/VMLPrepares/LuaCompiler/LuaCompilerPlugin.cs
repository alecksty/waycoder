using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace LuaCompiler
{
    /// <summary>
    /// Lua编译器插件 — 继承 CompilerPluginExBase，委托给 LuaCompiler 静态类
    /// </summary>
    public class LuaCompilerPlugin : CompilerPluginExBase
    {
        public override string Name => "lua";
        public override string Description => "Lua语言编译器 (脚本语言)";
        public override string SupportedExtensions => ".lua";
        protected override Func<string, VmlProgram> CompileFunc => LuaCompiler.Compile;
        protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => LuaCompiler.CompileFile;
        protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => LuaCompiler.CompileFileWithIncludes;
    }
}

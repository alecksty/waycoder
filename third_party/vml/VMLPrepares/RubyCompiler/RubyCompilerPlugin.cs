using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace RubyCompiler;

public class RubyCompilerPlugin : CompilerPluginExBase
{
    public override string Name => "ruby";
    public override string Description => "Ruby语言编译器 (脚本语言)";
    public override string SupportedExtensions => ".rb";
    protected override Func<string, VmlProgram> CompileFunc => RubyCompiler.Compile;
    protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => RubyCompiler.CompileFile;
    protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => RubyCompiler.CompileFileWithIncludes;
}

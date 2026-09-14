using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace RCompiler;

public class RCompilerPlugin : CompilerPluginExBase
{
    public override string Name => "r";
    public override string Description => "R语言编译器 (统计语言)";
    public override string SupportedExtensions => ".r";
    protected override Func<string, VmlProgram> CompileFunc => RCompiler.Compile;
    protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => RCompiler.CompileFile;
    protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => RCompiler.CompileFileWithIncludes;
}

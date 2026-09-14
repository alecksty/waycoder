using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace DCompiler;

public class DCompilerPlugin : CompilerPluginExBase
{
    public override string Name => "d";
    public override string Description => "D语言编译器 (系统编程语言)";
    public override string SupportedExtensions => ".d";
    protected override Func<string, VmlProgram> CompileFunc => DCompiler.Compile;
    protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => DCompiler.CompileFile;
    protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => DCompiler.CompileFileWithIncludes;
}

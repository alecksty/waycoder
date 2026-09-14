using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace DartCompiler;

public class DartCompilerPlugin : CompilerPluginExBase
{
    public override string Name => "dart";
    public override string Description => "Dart语言编译器 (C-like, classes)";
    public override string SupportedExtensions => ".dart";
    protected override Func<string, VmlProgram> CompileFunc => DartCompiler.Compile;
    protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => DartCompiler.CompileFile;
    protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => DartCompiler.CompileFileWithIncludes;
}

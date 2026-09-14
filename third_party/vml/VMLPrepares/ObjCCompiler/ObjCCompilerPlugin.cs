using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace ObjCCompiler;

public class ObjCCompilerPlugin : CompilerPluginExBase
{
    public override string Name => "objc";
    public override string Description => "Objective-C语言编译器 (兼容C)";
    public override string SupportedExtensions => ".m,.mm";
    protected override Func<string, VmlProgram> CompileFunc => ObjCCompiler.Compile;
    protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => ObjCCompiler.CompileFile;
    protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => ObjCCompiler.CompileFileWithIncludes;
}

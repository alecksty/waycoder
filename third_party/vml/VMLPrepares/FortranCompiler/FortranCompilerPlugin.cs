using System;
using System.Collections.Generic;
using CompilerBase;
using VMLAssembler;

namespace FortranCompiler;

public class FortranCompilerPlugin : CompilerPluginExBase
{
    public override string Name => "fortran";
    public override string Description => "Fortran语言编译器 (科学计算语言)";
    public override string SupportedExtensions => ".f90,.f,.f95,.f03,.f08,.for,.ftn";
    protected override Func<string, VmlProgram> CompileFunc => FortranCompiler.Compile;
    protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc => FortranCompiler.CompileFile;
    protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc => FortranCompiler.CompileFileWithIncludes;
}

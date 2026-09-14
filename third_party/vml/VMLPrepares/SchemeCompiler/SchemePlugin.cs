using VMLAssembler;
using CompilerBase;

namespace SchemeCompiler;
public class SchemePlugin : CompilerPluginExBase {
    public override string Name => "Scheme";
    public override string Description => "Scheme/Lisp 编译器 — 闭包/尾调用优化/宏/向量/列表操作, MCU生产可用";
    public override string SupportedExtensions => ".scm,.ss,.sls";
    protected override Func<string, VmlProgram> CompileFunc => SchemeCompiler.Compile;
    protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc
        => SchemeCompiler.CompileFile;
    protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc
        => SchemeCompiler.CompileFileWithIncludes;
}

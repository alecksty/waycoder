using VMLAssembler;
using CompilerBase;

namespace KotlinCompiler;
public class KotlinCompilerPlugin : CompilerPluginExBase {
    public override string Name => "Kotlin";
    public override string Description => "Kotlin 编译器 — 数据类/when表达式/lambda/泛型/安全调用, MCU生产可用";
    public override string SupportedExtensions => ".kt,.kts";
    protected override Func<string, VmlProgram> CompileFunc => KotlinCompiler.Compile;
    protected override Func<string, List<string>, List<string>, bool, VmlProgram> CompileFileFunc
        => KotlinCompiler.CompileFile;
    protected override Func<string, List<string>, List<string>, bool, bool, string> CompileFileWithIncludesFunc
        => KotlinCompiler.CompileFileWithIncludes;
}

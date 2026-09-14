using VMLAssembler;
using VMLPlugins;
using CompilerBase;

namespace DCompiler;

public static class CompilerProgram
{
    static async Task Main(string[] args)
        => await new SimpleCompilerProgram("d", "VML_D_LIB",
            (s, i, l) => DCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
}

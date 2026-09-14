using VMLAssembler;
using VMLPlugins;
using CompilerBase;

namespace DartCompiler;

public static class CompilerProgram
{
    static async Task Main(string[] args)
        => await new SimpleCompilerProgram("dart", "VML_DART_LIB",
            (s, i, l) => DartCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
}

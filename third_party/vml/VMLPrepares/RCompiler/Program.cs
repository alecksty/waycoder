using System.Threading.Tasks;
using VMLAssembler;
using VMLPlugins;
using CompilerBase;

namespace RCompiler;

public static class CompilerProgram
{
    static async Task Main(string[] args)
        => await new SimpleCompilerProgram("r", "VML_R_LIB",
            (s, i, l) => RCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
}

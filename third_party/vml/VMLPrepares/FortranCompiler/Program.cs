using System.Threading.Tasks;
using VMLAssembler;
using VMLPlugins;
using CompilerBase;

namespace FortranCompiler;

public static class CompilerProgram
{
    static async Task Main(string[] args)
        => await new SimpleCompilerProgram("fortran", "VML_FORTRAN_LIB",
            (s, i, l) => FortranCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
}

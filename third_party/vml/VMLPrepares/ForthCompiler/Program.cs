using CompilerBase;

namespace ForthCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("forth", "VML_FORTH_LIB",
                (s, i, l) => ForthCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

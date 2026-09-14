using CompilerBase;

namespace PascalCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("pascal", "VML_PASCAL_LIB",
                (s, i, l) => PascalCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

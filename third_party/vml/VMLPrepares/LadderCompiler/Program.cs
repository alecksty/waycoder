using CompilerBase;

namespace LadderCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("ladder", "VML_LADDER_LIB",
                (s, i, l) => LadderCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

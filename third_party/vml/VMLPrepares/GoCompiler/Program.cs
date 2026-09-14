using CompilerBase;

namespace GoCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("go", "VML_GO_LIB",
                (s, i, l) => GoCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

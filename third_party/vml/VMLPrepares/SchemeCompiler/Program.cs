using CompilerBase;

namespace SchemeCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("scheme", "VML_SCHEME_LIB",
                (s, i, l) => SchemeCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

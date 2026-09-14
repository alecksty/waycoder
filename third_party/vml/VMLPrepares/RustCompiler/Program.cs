using CompilerBase;

namespace RustCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("rust", "VML_RUST_LIB",
                (s, i, l) => RustCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

using CompilerBase;

namespace CppCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("cpp", "VML_CPP_LIB",
                (s, i, l) => CppCompiler.CompileFileWithIncludes(s, i, l, false, true)).RunAsync(args);
    }
}

using CompilerBase;

namespace KotlinCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("kotlin", "VML_KOTLIN_LIB",
                (s, i, l) => KotlinCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

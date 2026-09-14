using CompilerBase;

namespace BasicCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("basic", "VML_BASIC_LIB",
                (s, i, l) => BasicCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

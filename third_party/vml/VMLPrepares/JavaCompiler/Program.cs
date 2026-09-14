using CompilerBase;

namespace JavaCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleInstanceCompilerProgram("java",
                (s, dbg) => new JavaCompiler().Compile(s, dbg)).RunAsync(args);
    }
}

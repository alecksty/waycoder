using CompilerBase;

namespace SwiftCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleInstanceCompilerProgram("swift",
                (s, dbg) => new SwiftCompiler().Compile(s, dbg)).RunAsync(args);
    }
}

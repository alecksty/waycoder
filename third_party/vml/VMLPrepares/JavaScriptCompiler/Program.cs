using CompilerBase;

namespace JavaScriptCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleInstanceCompilerProgram("javascript",
                (s, dbg) => new JavaScriptCompiler().Compile(s, dbg)).RunAsync(args);
    }
}

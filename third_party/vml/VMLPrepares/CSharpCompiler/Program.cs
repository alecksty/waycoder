using CompilerBase;

namespace CSharpCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleInstanceCompilerProgram("csharp",
                (s, dbg) => new CSharpCompiler().Compile(s, dbg)).RunAsync(args);
    }
}

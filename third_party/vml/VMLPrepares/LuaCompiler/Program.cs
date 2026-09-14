using CompilerBase;

namespace LuaCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("lua", "VML_LUA_LIB",
                (s, i, l) => LuaCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

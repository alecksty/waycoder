using VMLAssembler;
using VMLPlugins;
using CompilerBase;

namespace ObjCCompiler;

public static class CompilerProgram
{
    static async Task Main(string[] args)
        => await new SimpleCompilerProgram("objc", "VML_OBJC_LIB",
            (s, i, l) => ObjCCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
}

using VMLAssembler;
using VMLPlugins;
using CompilerBase;

namespace RubyCompiler;

public static class CompilerProgram
{
    static async Task Main(string[] args)
        => await new SimpleCompilerProgram("ruby", "VML_RUBY_LIB",
            (s, i, l) => RubyCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
}

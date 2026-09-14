using CompilerBase;

namespace PythonCompiler
{
    public static class CompilerProgram
    {
        static async Task Main(string[] args)
            => await new SimpleCompilerProgram("python", "VML_PYTHON_LIB",
                (s, i, l) => PythonCompiler.CompileFileWithIncludes(s, i, l, true)).RunAsync(args);
    }
}

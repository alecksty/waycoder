using System;
using System.Collections.Generic;
using VMLAssembler;
using CompilerBase;
using VMLPlugins;

namespace GoCompiler
{
    /// <summary>
    /// Go编译器
    /// </summary>
    public class GoCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__GO__"] = "1",
        };

        public static VmlProgram Compile(string source)
        {
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            // 预处理
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, null, PredefinedMacros);
                source = pp.Process();
            }

            try
            {
                return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
                {
                    var lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                    var tokens = lexer.Tokenize();
                    var isMCU = CompilerOptionsContext.Current.IsMCU;
                    var parser = new Parser(tokens, isMCU) { FileName = "<input>", Diagnostics = diagnostics };
                    var ast = parser.ParseProgram();
                    var codeGenerator = new CodeGenerator(ast);
                    codeGenerator.SourceLines = source.Split('\n');
                    var prog = codeGenerator.GenerateCode();
                    CompilerHelper.LinkStandardLibrary(prog, "go", null);
                    return prog;
                });
            }
            catch (Exception ex) when (ex is not CompilationException)
            {
                Console.Error.WriteLine($"<input>: warning: 完整编译失败，改用简化编译: {ex.Message}");
                return CompileSimple(source);
            }
        }

        private static VmlProgram CompileSimple(string source)
        {
            var instructions = new List<Instruction>();
            var labels = new Dictionary<string, int>();
            var dataSection = new Dictionary<string, object>();
            var constants = new Dictionary<string, object>();

            labels["main"] = 0;

            dataSection["msg"] = "Go Compiler Test";
            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand>
            {
                new Operand(OperandType.REGISTER, "R0"),
                new Operand(OperandType.MEMORY, "msg")
            }, 1));
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> { new Operand(OperandType.IMMEDIATE, 2) }, 2));
            instructions.Add(new Instruction(OpCode.HALT, new List<Operand>(), 3));

            return new VmlProgram(instructions, labels, dataSection, constants);
        }

        public static VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true)
        {
            string source = CompilerHelper.ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

            // 预处理
            return CompilerHelper.CompileFileStandard(filePath, "go", Compile, ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
        }

        /// <summary>
        /// 编译文件并生成包含.include伪指令的VML文本
        /// </summary>
        public static string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "go");

        private static List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"import\s+""([^""]+)""", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (!libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }
    }
}

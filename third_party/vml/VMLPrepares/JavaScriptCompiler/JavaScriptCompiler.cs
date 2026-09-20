using System.Collections.Generic;
using System.Text;
using CompilerBase;
using VMLAssembler;
using VMLPlugins;

namespace JavaScriptCompiler
{
    /// <summary>
    /// JavaScript语言编译器
    /// </summary>
    public class JavaScriptCompiler
    {
        private List<Instruction> instructions;
        private Dictionary<string, int> labels;
        private Dictionary<string, object> dataSection;
        private Dictionary<string, object> constants;

        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__JAVASCRIPT__"] = "1",
        };

        public JavaScriptCompiler(bool debugMode = false)
        {
            instructions = new List<Instruction>();
            labels = new Dictionary<string, int>();
            dataSection = new Dictionary<string, object>();
            constants = new Dictionary<string, object>();
        }

        public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
        {
            return CompilerHelper.CompileFileStandard(filePath, "javascript", s => new JavaScriptCompiler().Compile(s), ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
        }

        private static List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"import\s+(?:\{[^}]*\}\s+from\s+)?[""']([^""']+)[""']", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (!libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }

        public string CompileFileWithIncludes(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "javascript");

        public VmlProgram Compile(string source, bool debugMode = false)
        {
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, null, PredefinedMacros);
                source = pp.Process();
            }

            return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
            {
                var tokens = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics }.Tokenize();
                var parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
                var astProgram = parser.Parse();
                var codeGenerator = new CodeGenerator();
                codeGenerator.SourceLines = source.Split('\n');
                var vmlProgram = codeGenerator.Generate(astProgram);

                instructions = vmlProgram.Instructions;
                labels = vmlProgram.Labels;
                dataSection = vmlProgram.DataSection;
                constants = vmlProgram.Constants;

                if (!labels.ContainsKey("main"))
                    AddDefaultMain();

                // ⚠ 这里**又造了一个** `VmlProgram`（字段是上面那份缓存），而 `Generate()`
                //   返回的那一份才是 `BuildProgram` 盖过行号映射的 —— 两张对象不是同一个，
                //   表得**搬过来**。
                //   本门是唯一"在编译委托内部就调 `LinkStandardLibrary`"的 ⇒ 一旦表没搬，
                //   链接期读到的就是 null：错在 `#include` 进来的头文件里时会报成
                //   `<input>:4`（实测 DiagProbe【头文件里的错】js 一栏就是这条）。
                var program = new VmlProgram(instructions, labels, dataSection, constants)
                {
                    SourceLineMap = vmlProgram.SourceLineMap
                };
                CompilerHelper.LinkStandardLibrary(program, "javascript", null);
                return program;
            });
        }

        private void AddDefaultMain()
        {
            labels["main"] = instructions.Count;

            instructions.Add(new Instruction(OpCode.MOVE, new List<Operand> {
                new Operand(OperandType.REGISTER, 12),
                new Operand(OperandType.REGISTER, 13)
            }));
            instructions.Add(new Instruction(OpCode.SYSCALL, new List<Operand> {
                new Operand(OperandType.IMMEDIATE, 3)
            }));
        }
    }
}

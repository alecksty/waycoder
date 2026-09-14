using System.Collections.Generic;
using System.IO;
using CompilerBase;
using VMLAssembler;
using VMLPlugins;

namespace JavaCompiler
{
    /// <summary>
    /// Java语言编译器
    /// </summary>
    public class JavaCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__JAVA__"] = "1",
        };

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
                var lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                lexer.Tokenize();
                var parser = new Parser(lexer.Tokens) { FileName = "<input>", Diagnostics = diagnostics };
                var ast = parser.Parse();
                var codeGenerator = new CodeGenerator();
                codeGenerator.SourceLines = source.Split('\n');
                var program = codeGenerator.Generate(ast);
                // LinkStandardLibrary 由调用方 (CompileCore/CLI) 统一处理，避免重复链接
                return program;
            });
        }

        public static VmlProgram CompileFile(string filePath, List<string>? includePaths = null, List<string>? libraryPaths = null, bool autoLinkStdLib = true)
        {
            return CompilerHelper.CompileFileStandard(filePath, "java", s => new JavaCompiler().Compile(s), ExtractImports, PredefinedMacros, includePaths, libraryPaths, autoLinkStdLib);
        }

        private static List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"import\s+((?:[\w]+\.)*[\w]+)\.\*", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;  // 完整包名如 "a.b.c.d"
                if (!libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }
        
        /// <summary>
        /// 编译文件并生成包含.include伪指令的VML文本
        /// </summary>
        public string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
        {
            // 编译主程序
            var mainProgram = CompileFile(filePath, includePaths, null, false); // 不自动链接库
            
            // 构建包含文件列表
            var includeFiles = new List<string>();
            
            // 添加标准库包含
            if (autoLinkStdLib)
            {
                string javaLibPath = Path.Combine(Directory.GetCurrentDirectory(), "Lib", "java");
                if (Directory.Exists(javaLibPath))
                {
                    string stdlibPath = Path.Combine(javaLibPath, "stdlib.vml");
                    if (File.Exists(stdlibPath))
                    {
                        includeFiles.Add(stdlibPath);
                    }
                }
            }
            
            // 添加用户指定的库路径
            if (libraryPaths != null && libraryPaths.Count > 0)
            {
                foreach (var libPath in libraryPaths)
                {
                    if (Directory.Exists(libPath))
                    {
                        var vmlFiles = Directory.GetFiles(libPath, "*.vml");
                        includeFiles.AddRange(vmlFiles);
                    }
                    else if (File.Exists(libPath))
                    {
                        includeFiles.Add(libPath);
                    }
                }
            }
            
            // 生成包含.include伪指令的VML文本
            // 简化实现：直接返回程序文本
            return mainProgram.ToString();
        }
    }
}
using CompilerBase;
using VMLAssembler;

namespace JavaCompiler
{
    /// <summary>
    /// Java编译器插件 — 继承 CompilerPluginBase，实现 IFrontendCompilerEx
    /// </summary>
    public class JavaCompilerPlugin : CompilerPluginBase
    {
        public override string Name => "java";
        public override string Description => "Java语言编译器 - 支持Java语法子集，包括类、方法、变量、控制流等基本特性";
        public override string SupportedExtensions => ".java";
        protected override string LibDirectory => "java";
        protected override VmlProgram CompileSource(string source) => new JavaCompiler().Compile(source, false);

        protected override List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"import\s+((?:[\w]+\.)*[\w]+)\.\*", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;  // 完整包名如 "a.b.c.d"
                if (name != "java" && !libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }
    }
}

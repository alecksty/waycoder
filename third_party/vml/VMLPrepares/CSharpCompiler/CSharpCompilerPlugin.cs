using CompilerBase;
using VMLAssembler;

namespace CSharpCompiler
{
    /// <summary>
    /// C#编译器插件 — 继承 CompilerPluginBase，实现 IFrontendCompilerEx
    /// </summary>
    public class CSharpCompilerPlugin : CompilerPluginBase
    {
        public override string Name => "csharp";
        public override string Description => "C#语言编译器 - 支持C#语法子集，包括类、方法、变量、控制流等基本特性";
        public override string SupportedExtensions => ".cs";
        protected override string LibDirectory => "csharp";
        protected override VmlProgram CompileSource(string source) => new CSharpCompiler().Compile(source, false);

        protected override List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"using\s+(\w+)\s*;", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (name != "System" && !libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }
    }
}

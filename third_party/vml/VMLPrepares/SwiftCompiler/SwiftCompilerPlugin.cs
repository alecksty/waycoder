using CompilerBase;
using VMLAssembler;

namespace SwiftCompiler
{
    /// <summary>
    /// Swift编译器插件 — 继承 CompilerPluginBase，实现 IFrontendCompilerEx
    /// </summary>
    public class SwiftCompilerPlugin : CompilerPluginBase
    {
        public override string Name => "swift";
        public override string Description => "Swift语言编译器 - 支持Swift语法子集，包括变量、函数、控制流、可选类型等基本特性";
        public override string SupportedExtensions => ".swift";
        protected override string LibDirectory => "swift";
        protected override VmlProgram CompileSource(string source) => new SwiftCompiler().Compile(source, false);

        protected override List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"^import\s+(\w+)", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (name != "Foundation" && name != "UIKit" && !libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }
    }
}

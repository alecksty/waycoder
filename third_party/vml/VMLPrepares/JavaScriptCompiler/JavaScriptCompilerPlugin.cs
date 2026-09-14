using CompilerBase;
using VMLAssembler;

namespace JavaScriptCompiler
{
    /// <summary>
    /// JavaScript编译器插件 — 继承 CompilerPluginBase，实现 IFrontendCompilerEx
    /// </summary>
    public class JavaScriptCompilerPlugin : CompilerPluginBase
    {
        public override string Name => "javascript";
        public override string Description => "JavaScript语言编译器 - 支持JavaScript语法子集，包括变量、函数、控制流等基本特性";
        public override string SupportedExtensions => ".js";
        protected override string LibDirectory => "javascript";
        protected override VmlProgram CompileSource(string source) => new JavaScriptCompiler().Compile(source, false);

        protected override List<string> ExtractImports(string source)
        {
            var libs = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(source, @"import\s+(?:\{[^}]*\}\s+from\s+)?[""']([^""']+)[""']", System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                string name = m.Groups[1].Value;
                if (!libs.Contains(name)) libs.Add(name);
            }
            return libs;
        }

        /// <summary>
        /// 移除 import 语句 — JS 解析器不支持 import 语法，需要在编译前剥离
        /// </summary>
        protected override string StripImports(string source)
        {
            return System.Text.RegularExpressions.Regex.Replace(source,
                @"^import\s+(?:\{[^}]*\}\s+from\s+)?[""'][^""']+[""']\s*;?\s*$",
                "", System.Text.RegularExpressions.RegexOptions.Multiline);
        }
    }
}

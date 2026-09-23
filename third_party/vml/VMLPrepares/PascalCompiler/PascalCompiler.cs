using VMLAssembler;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using CompilerBase;
using VMLPlugins;

namespace PascalCompiler
{
    /// <summary>
    /// Pascal 语言编译器
    /// </summary>
    public class PascalCompiler
    {
        private static readonly Dictionary<string, string> PredefinedMacros = new(CompilerHelper.BasePredefinedMacros())
        {
            ["__PASCAL__"] = "1",
        };

        public static VmlProgram Compile(string source, List<(string, int)> lineMap = null)
        {
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, null, PredefinedMacros);
                source = pp.Process();
            }
            return CompilerHelper.CompileWithDiagnostics(null, diagnostics =>
            {
                Lexer lexer = new Lexer(source) { FileName = "<input>", Diagnostics = diagnostics };
                var tokens = lexer.Tokenize();
                Parser parser = new Parser(tokens) { FileName = "<input>", Diagnostics = diagnostics };
                var ast = parser.Parse();
                if (ast is ProgramNode programNode)
                {
                    CodeGenerator codeGen = new CodeGenerator(programNode);
                    codeGen.SourceLines = source.Split('\n');
                    codeGen.UseCrtOutput = parser.UsesNames.Contains("crt");
                    return codeGen.GenerateCode();
                }
                if (ast is UnitNode unitNode)
                {
                    CodeGenerator codeGen = new CodeGenerator(unitNode);
                    codeGen.SourceLines = source.Split('\n');
                    codeGen.UseCrtOutput = parser.UsesNames.Contains("crt");
                    return codeGen.GenerateCode();
                }
                throw new CodeGenerationException(ErrorCode.CodeGen_UnsupportedExpression, "不支持的AST节点类型");
            });
        }

        public static VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true)
        {
            string source = CompilerHelper.ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

            // 预处理
            Preprocessor pp = null;
            if (source.Contains('#'))
            {
                pp = new Preprocessor(source, includePaths, PredefinedMacros);
                source = pp.Process(filePath);
            }

            // ⚠ **必须把诊断收集器接给词法器与解析器** —— 这一条是"一次多报多个错误"的前提。
            //
            // 本方法是 Pascal 自己手写的一条流水线（不像其余 17 门走
            // `CompilerHelper.CompileFileStandard` → `Compile` → `CompileWithDiagnostics`），
            // 所以此前 `new Parser(tokens)` **既没 `Diagnostics`、也没 `FileName`**。
            // 后果不是"少了个字段"，而是**错误处理策略整个退化**：
            // `GccError` 见 `Diagnostics == null` 只能**抛**（它没有地方可收），
            // 于是文件里后面的错全部看不到 —— 实测一份有两个错的文件只报出第一条。
            // 接上收集器之后，能继续的错误才会走"收集 + 恢复"，一路报到底。
            var diagnostics = new DiagnosticBag();
            Lexer lexer = new Lexer(source) { FileName = filePath, Diagnostics = diagnostics };
            var tokens = lexer.Tokenize();

            Parser parser = new Parser(tokens) { FileName = filePath, Diagnostics = diagnostics };
            var ast = parser.Parse();

            // 解析阶段收下的错在这里一次性报出去（与 `CompileWithDiagnostics` 同一口径：
            // 用 `FormatAll()` 把所有错一起带给用户，而不是只报第一条）。
            if (diagnostics.HasErrors)
                throw new CompilationException(diagnostics.FirstErrorCode, diagnostics.FormatAll());

            /* ⚠ **静态的「uses 单元符号表」必须在这里清空** —— 这三个表是 `CodeGenerator` 上的
               static（沿用它已有的 `ExternalFuncTypes` 那套形状），而一个进程里会编译**很多份**
               源文件（手机端点一次运行是一份，批量/自测是几百份）。
               不清的话会出现"上一份文件 `uses graph`，这一份没写 uses 却认得出 `GetMaxX`"
               这种**跨编译泄漏** —— 症状是同一份源文件单独编不过、跟着别的文件一起编就过了，
               而这类问题从来没人能一眼看出来。
               为什么清在**入口**而不是出口：下面那条递归（单元文件也走 `CompileFile`）
               会在**主程序 codegen 之后**才发生，那时主程序要用的符号已经吃进指令里了，
               子调用清表不再影响它；而"清在出口"要包 try/finally 才算得住（本方法多处 throw）。 */
            CodeGenerator.ExternalFuncTypes.Clear();
            CodeGenerator.UnitConstInts.Clear();
            CodeGenerator.UnitSubprograms.Clear();

            // 解析 uses 子句中引用的单元文件 (v1.66.32+)
            List<string> searchPaths = new List<string> { sourceDir };
            if (includePaths != null) searchPaths.AddRange(includePaths);
            searchPaths.AddRange(UnitSearchDirs(sourceDir));

            /* 把 `uses` 单元的 **interface 符号**（整数常量 / 子程序头 / 返回类型）先收进
               `CodeGenerator` 的静态表，**再**生成主程序代码。

               ⚠ 这一句的位置是这次改动的**全部要点**：原先它在方法末尾（生成代码之后），
                 于是这些符号对**主程序**来说从来没生效过 —— `uses graph` 之后写 `Detect`、
                 `GetMaxX`、`grOk` 一律报"未声明的变量"，而那正是老 Pascal 图形程序的
                 标准开场（语料里 `graphresult` 21 处、`grapherrormsg` 16 处、`getmaxx` 72 处）。
                 对单元自己（`ast is UnitNode`）也一样：单元之间互相 `uses` 时同样要看得见。 */
            foreach (string unitName in parser.UsesNames)
            {
                string unitPath = ResolveUnitFile(unitName, searchPaths);
                if (unitPath != null) RegisterUnitFunctions(unitPath);
            }

            VmlProgram prog;
            if (ast is ProgramNode programNode)
            {
                CodeGenerator codeGen = new CodeGenerator(programNode);
                codeGen.SourceLines = source.Split('\n');
                codeGen.UseCrtOutput = parser.UsesNames.Contains("crt");
                prog = codeGen.GenerateCode();
            }
            else if (ast is UnitNode unitNode)
            {
                CodeGenerator codeGen = new CodeGenerator(unitNode);
                codeGen.SourceLines = source.Split('\n');
                prog = codeGen.GenerateCode();
                prog.IsLibrary = true; // 单元编译为库模式
            }
            else
                throw new CompilationException(ErrorCode.Compilation_InternalError, "不支持的AST节点类型");

            List<string> allLibraryPaths = new List<string>();
            if (libraryPaths != null)
                allLibraryPaths.AddRange(libraryPaths);

            // 合并预处理器和 Lexer 中收集的 #param lib 库
            if (pp != null)
            {
                foreach (var lib in pp.ParamLibraries)
                {
                    string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "pascal");
                    if (resolved != null && !allLibraryPaths.Contains(resolved))
                        allLibraryPaths.Add(resolved);
                }
            }
            foreach (var lib in lexer.ParamLibraries)
            {
                string resolved = CompilerHelper.ResolveImportLibrary(lib, sourceDir, "pascal");
                if (resolved != null && !allLibraryPaths.Contains(resolved))
                    allLibraryPaths.Add(resolved);
            }

            var unitPrograms = new List<VmlProgram>();
            foreach (string unitName in parser.UsesNames)
            {
                string unitPath = ResolveUnitFile(unitName, searchPaths);
                if (unitPath != null)
                {
                    // 递归编译单元文件 (处理嵌套 uses)
                    var unitProg = CompileFile(unitPath, includePaths, libraryPaths, false);
                    unitPrograms.Add(unitProg);
                }
            }

            // 链接所有单元到主程序 (主程序+单元 → 合并)
            if (unitPrograms.Count > 0)
            {
                // 先应用每个程序的 exports，创建公开别名
                prog.ApplyExports();
                foreach (var up in unitPrograms) up.ApplyExports();

                var allPrograms = new List<VmlProgram> { prog };
                allPrograms.AddRange(unitPrograms);
                prog = VmlProgram.Link(allPrograms);
            }

            // 自动检测 uses xxx → 链接对应库 (v1.66.33)
            AutoLinkUnit(parser, prog, "dos", "dos.vml", "DOS 系统调用库", sourceDir);
            /* ⚠ `uses graph` 链的是 **`bgi.vml`**，不是 `graph.vml` —— 这条是**有意换的**：
               · `Lib/shared/src/graph.c`（→ `graph.vml`）走的是 `gfx_*` + 模拟 VGA 显存那条路，
                 而 `Lib/` 里**没有任何模块提供 `gfx_*`**（58 处 `call gfx_*` 全是未解析标签）；
                 `docs/老程序兼容性.md` 已定案「gfx_ 与显存那一整套保持现状、不修」——
                 接上它就是对着一条注定不通的路修。
               · `bgi.vml` 由 `Lib/shared/src/bgi.c` 生成，那份 C 只是**转发**
                 `Lib/c/graphics.h`（纯头文件的 BGI 实现，45 个函数，走 `ui_*` 宿主图元、
                 `Examples/c/test_bgi.c` 已逐格体检过）。⇒ 一条实现、C 与 Pascal 共享。
               · 老的 `graph.c`/`graph.vml` **留在盘上不动**（别的语言/历史引用还在）。 */
            AutoLinkUnit(parser, prog, "graph", "bgi.vml", "BGI 图形库 (graphics.h 转发层)", sourceDir);
            AutoLinkUnit(parser, prog, "conv", "conv.vml", "类型转换库", sourceDir);

            if (autoLinkStdLib)
                CompilerHelper.LinkStandardLibrary(prog, "pascal", allLibraryPaths);
            else if (allLibraryPaths.Count > 0)
                VMLAssembler.LibraryLinker.LinkLibraries(prog, allLibraryPaths);

            return prog;
        }

        /// <summary>
        /// `uses` 单元的搜索目录（按优先级）。
        ///
        /// <para>
        /// ⚠ **主判据是 `VML_HOME`，不是数 `..` 的个数**。从前这里只有两条按相对层级
        /// 拼出来的路径（`sourceDir/../../../Lib/pascal` 与 `sourceDir/../Lib/pascal`），
        /// 它们只对"恰好落在 `<VML_HOME>` 下面某一层/两层"的源文件目录成立：
        /// </para>
        /// <list type="bullet">
        ///   <item><c>third_party/vml/.bgitest/x.pas</c> ⇒ `../Lib/pascal` 命中 ✓；</item>
        ///   <item><c>third_party/vml/Examples/pascal/x.pas</c> ⇒ **两条都不命中** ✗ ——
        ///         而**语料就在那个目录里**（`Examples/pascal/` 111 份老程序）。
        ///         于是 `uses Graph` 一条声明都拿不到，症状是一整片
        ///         "未声明的变量 'Detect' / 'GetMaxX' / 'grOk'"。</item>
        /// </list>
        /// <para>
        /// `VML_HOME` 是这条流水线里**本来就有**的量（CLI 与手机端都设它，见
        /// `scripts/vmlcli/Program.cs` 与 `MauiVml`），用它拼 `<VML_HOME>/Lib/pascal`
        /// 与源文件放哪儿无关。旧的两条**保留在后面**只为兼容"没有 VML_HOME"的调用方。
        /// </para>
        /// </summary>
        private static IEnumerable<string> UnitSearchDirs(string sourceDir)
        {
            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
            if (!string.IsNullOrEmpty(vmlHome))
                yield return Path.Combine(vmlHome, "Lib", "pascal");
            yield return Path.Combine(Directory.GetCurrentDirectory(), "Lib", "pascal");
            // 从程序集目录向上找（手机端/嵌入式宿主里没有 cwd 语义）
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6 && dir != null; i++)
            {
                yield return Path.Combine(dir, "Lib", "pascal");
                dir = Path.GetDirectoryName(dir);
            }
            // 旧的两条（按源文件目录的相对层级）——留在最后，只当上面都没命中时的兜底
            yield return Path.GetFullPath(Path.Combine(sourceDir, "..", "..", "..", "Lib", "pascal"));
            yield return Path.GetFullPath(Path.Combine(sourceDir, "..", "Lib", "pascal"));
        }

        /// <summary>
        /// 搜索单元文件：**只找 `.pas` 源码**。
        ///
        /// <para>
        /// ⚠ 从前这里还会返回同名的 `.vml`（"预编译库"），而那是**错的**：
        /// 返回的那个路径会被 `CompileFile` 当成 **Pascal 源码**去词法分析，而
        /// `Lib/pascal/*.vml` 是 **GenLib 生成的模块包装**（第一行 `; Auto-generated by GenLib`）——
        /// 于是 `uses Crt` 的报错是 `<…>/Lib/pascal/Crt.vml:1:29: 未知字符: —`，
        /// **整份程序编不过**，而错在库里一个跟用户源码毫无关系的文件上。
        /// </para>
        ///
        /// <para>
        /// 这不是理论问题：`Examples/pascal/**` 里凡在 `Lib/pascal` 可达位置的程序
        /// （也就是**例程目录自己**）只要写 `uses Crt` 就必然撞上 —— 而 `uses Crt` 是
        /// 老 Pascal 程序的常态（语料 111 份里 47 份用它）。库的**链接**另有两条正路：
        /// `vmltool.config.xml` 的语言 `Libs=`（crt/builtins/vmlui）与 `AutoLinkUnit`
        /// （`uses xxx` → 同名库），**都不需要**把库文件当源码编一遍。
        /// </para>
        /// </summary>
        private static string ResolveUnitFile(string unitName, List<string> searchPaths)
        {
            foreach (var dir in searchPaths)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                string pasPath = Path.Combine(dir, unitName + ".pas");
                if (File.Exists(pasPath)) return pasPath;
                // 不区分大小写
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, "*.pas", SearchOption.TopDirectoryOnly))
                    {
                        if (string.Equals(Path.GetFileNameWithoutExtension(f), unitName, StringComparison.OrdinalIgnoreCase))
                            return f;
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// 编译文件并生成包含.include伪指令的VML文本
        /// </summary>
        public static string CompileFileWithIncludes(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true, bool useSharedLibrary = true)
            => CompilerHelper.BuildCompileFileWithIncludes(
                CompileFile(filePath, includePaths, null, false),
                filePath, libraryPaths, autoLinkStdLib, useSharedLibrary, "pascal");

    /// <summary>
    /// 把 <c>uses</c> 单元的 **interface 符号**登记进 <see cref="CodeGenerator"/> 的静态表
    /// （整数常量 / 子程序头 / 返回类型）。
    ///
    /// <para>
    /// Pascal 的语义是"单元的 interface 就是它对使用者的样子"，而 VML 的库是**按标签调用**的
    /// ⇒ 单元文件（`Lib/pascal/graph.pas` 这种）是**声明的唯一真源**，库里那份
    /// （`Lib/shared/bgi.vml`）是实现。这样做的意义在于**不为某个库在 C# 里另写一张名字表**：
    /// `graph.pas` 写 `function GetMaxX: integer;`，前端就认得出 `GetMaxX` 是零参函数、
    /// 该发 `CALL GetMaxX`；换个库只要加一个 `.pas`，前端一行不用改。
    /// </para>
    ///
    /// <para>
    /// 登记三样（缺一样就有一种老写法编不过）：
    /// <list type="bullet">
    ///   <item><c>UnitSubprograms</c>：子程序头 ⇒ ①**零参函数裸写**（`GetMaxX div 2`、
    ///         `if KeyPressed then`）能当成调用而不是"未声明的变量"；②`var` 形参的地址传递
    ///         （`DrawPoly(4, Poly)` 要传数组地址，不是元素值）；③**大小写归一**
    ///         （Pascal 不区分大小写，而 VML 的标签表区分 ⇒ `Getmaxx` 也得发 `CALL GetMaxX`）。</item>
    ///   <item><c>UnitConstInts</c>：interface 整数常量（`Detect`/`grOk`/`VGA`/颜色…）。</item>
    ///   <item><c>ExternalFuncTypes</c>：返回类型（`GraphErrorMsg` 是 STRING、`GetMaxX` 是 INTEGER）
    ///         —— 这个表本来就有，只是原先**登记在生成代码之后**（见调用点的 ⚠），等于没生效。</item>
    /// </list>
    /// </para>
    /// </summary>
    private static void RegisterUnitFunctions(string unitPath)
        {
            try {
                string s = File.ReadAllText(unitPath);
                var lx = new Lexer(s); var up = new Parser(lx.Tokenize()); var ast = up.Parse();
                if (ast is UnitNode un)
                {
                    foreach (var sub in un.InterfaceSubprograms)
                    {
                        if (string.IsNullOrEmpty(sub.Name)) continue;
                        CodeGenerator.UnitSubprograms[sub.Name.ToLower()] = sub;
                        if (sub is FunctionDeclarationNode fd)
                            CodeGenerator.ExternalFuncTypes[fd.Name.ToLower()] =
                                fd.ReturnType is SimpleTypeNode st ? st.TypeName.ToUpper() : "INTEGER";
                    }
                    /* 常量只收**整数**的：带类型标注的数组常量、字符串常量、REAL 常量各有各的落法，
                       而本表只有"把值当立即数发出去"这一条路 ⇒ 收不了的就不收（宁可它报
                       "未声明的变量"——那是响亮的失败，好过悄悄按 0 算）。
                       `-1` 这种**带一元负号**的也必须收：BGI 的错误码常量
                       （`grNoInitGraph = -1` 一路到 `grInvalidVersion = -18`）
                       **全是负的**，而解析器给的是 `UnaryOpNode("-", LiteralNode)`，
                       不是 `LiteralNode` —— 只认后者的话这些常量一个都登记不上。 */
                    foreach (var decl in un.InterfaceDeclarations)
                    {
                        if (decl is ConstDeclarationNode cd && cd.ArrayValues == null)
                        {
                            int? v = cd.Value switch
                            {
                                LiteralNode { Type: TokenType.INTEGER_LITERAL } lit => Convert.ToInt32(lit.Value),
                                UnaryOpNode { Operator: TokenType.MINUS, Operand: LiteralNode { Type: TokenType.INTEGER_LITERAL } neg }
                                    => -Convert.ToInt32(neg.Value),
                                _ => null
                            };
                            if (v.HasValue) CodeGenerator.UnitConstInts[cd.Name.ToLower()] = v.Value;
                        }
                    }
                }
            } catch { }
        }

        private static void AutoLinkUnit(Parser parser, VmlProgram prog, string unitName, string libFile, string desc, string sourceDir)
    {
        if (!parser.UsesNames.Any(u => string.Equals(u, unitName, StringComparison.OrdinalIgnoreCase)))
            return;

        // ⚠ **`VML_HOME` 优先**，理由与 `UnitSearchDirs` 完全相同（按相对层级数 `..`
        //   在 `Examples/pascal/` 那种深度上够不着 `Lib/`）。
        var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
            ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
        var searchPaths = new List<string>
        {
            Path.Combine(sourceDir, libFile),
            !string.IsNullOrEmpty(vmlHome) ? Path.Combine(vmlHome, "Lib", "shared", libFile) : null!,
            Path.Combine(Directory.GetCurrentDirectory(), "Lib", "shared", libFile),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Lib", "shared", libFile),
            Path.Combine(sourceDir, "..", "..", "..", "Lib", "shared", libFile),
        };
        string? foundPath = searchPaths.FirstOrDefault(File.Exists);
        if (foundPath == null) return;

        /* ⚠ **登记到 `LinkedFiles`（= 输出一条 `.linked`），不是 `libraryPaths`。**
         *
         * 从前这里是 `allLibraryPaths.Add(foundPath)`，而那个列表会在本方法返回后立刻触发
         * `CompileFile` 末尾那次 `LinkLibraries(prog, allLibraryPaths)` —— **只有这一个库**
         * 就被链接了。于是"用户代码里引用了一个库函数"当场被判成未解析并**抛异常**
         * （`UnresolvedSymbolException` 对用户档是硬错误，见 LibraryLinker.ReportUnresolved）：
         * 实测 `uses Crt, Graph` 的程序死在这条上 —— 报 `未定义的函数 'CRT_READKEY'`，
         * 而 `crt.vml` **本来就在这门语言的标准库里**、只是还没来得及链。
         * 换句话说，凡是 `uses dos/graph/conv` 的程序都编不过，而错看起来像"用户写错了函数名"。
         *
         * `.linked` 才是这套工具链里"我要链这个库"的正路（`#param lib` 走的就是它，
         * 见 `VmlProgram.LinkedFiles` 的注释），而且**链接时机对了**：汇编器解析 `.linked`
         * 之后由 `LinkLibraries` 一次性把语言标准库 + 这些库 + 它们的传递依赖一起链，
         * 谁也不会因为"同伴还没到"而被判成未定义。
         *
         * ⚠ 也不要折中成"两边都加" —— 那样那次过早的链接照样发生，等于没改。 */
        if (!prog.LinkedFiles.Contains(foundPath))
        {
            Console.WriteLine($"    自动链接: {libFile} ({desc})");
            prog.LinkedFiles.Add(foundPath);
        }
    }
}
}


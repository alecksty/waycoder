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

        /// <summary>
        /// Pascal **条件编译符号**的初值 —— **默认为空**（`{$IFDEF FPC}` / `{$IFDEF WINDOWS}`
        /// 都判假，于是 `{$ELSE}` 那支 Turbo Pascal 写法胜出，见 `PascalDirectives` 的类注释）。
        ///
        /// <para>
        /// 唯一的来源是 `-d SYM`（`CompilerOptions.Defines`）—— 于是
        /// `vmlcli foo.pas -d FPC` 就能编 FPC 那一支；`-U SYM` 从符号集里去掉。
        /// 这正是「把符号集暴露成参数」那条：**不做任何隐式定义**。
        /// </para>
        ///
        /// <para>
        /// ⚠ `CompilerOptions.PascalDialect`（默认 `Turbo`）看上去是更自然的挂点，但**故意没接**：
        /// 一接上，任何把方言设成 `FreePascal` 的调用方就会**自动**编进 FPC 分支，
        /// 而那批代码引用的是 `PtcGraph`/`SysUtils` 这些本平台没有的东西 ——
        /// 得到的是一串指向别处的错误，而不是一句"已切到 FPC 方言"。
        /// 要 FPC 分支就显式 `-d FPC`。
        /// </para>
        /// </summary>
        private static List<string> DirectiveSymbols()
        {
            var list = new List<string>();
            var opts = CompilerOptionsContext.Current;
            if (opts == null) return list;
            foreach (var d in opts.Defines)
            {
                if (string.IsNullOrWhiteSpace(d)) continue;
                var t = d.Trim();
                int eq = t.IndexOf('=');
                if (eq >= 0) t = t.Substring(0, eq).Trim();
                if (t.Length > 0) list.Add(t);
            }
            foreach (var u in opts.Undefines)
            {
                if (string.IsNullOrWhiteSpace(u)) continue;
                list.RemoveAll(s => string.Equals(s, u.Trim(), StringComparison.OrdinalIgnoreCase));
            }
            return list;
        }

        /// <summary>
        /// 把「指令预处理的行号映射」与「`#` 预处理的行号映射」**组合**成一张，投递给
        /// 词法器/解析器/代码生成器（`CompilerHelper.TrackPreprocessedOutput`，判据是
        /// **引用相等** —— 所以传进来的 <paramref name="finalSource"/> 正是要交给
        /// `new Lexer(...)` 的那个字符串实例）。
        ///
        /// <para>
        /// 组合规则：`ppMap` 的第 N 项 = 「`#` 预处理输出第 N 行」来自
        /// `(文件, 指令预处理输出第 M 行)`。`文件` 是主文件时，那一行**经过了我们的改动**，
        /// 于是再查一次 <paramref name="directiveMap"/> 拿回真正的原文件行号；
        /// 不是主文件（= `#include` 进来的头文件）时，它的行号**本来就是原文件的**，
        /// 原样保留即可（那张头文件根本没经过指令预处理）。
        /// </para>
        /// </summary>
        private static void RegisterLineMap(string finalSource, string ppMainName,
            List<(string, int)> directiveMap, List<(string, int)> ppMap)
        {
            if (directiveMap == null || directiveMap.Count == 0) return;   // 没映射就别去盖掉别人的
            if (ppMap == null || ppMap.Count == 0)
            {
                CompilerHelper.TrackPreprocessedOutput(finalSource, directiveMap);
                return;
            }

            var composed = new List<(string, int)>(ppMap.Count);
            foreach (var (file, line) in ppMap)
            {
                if (line >= 1 && line <= directiveMap.Count &&
                    string.Equals(file, ppMainName, StringComparison.Ordinal))
                    composed.Add(directiveMap[line - 1]);
                else
                    composed.Add((file, line));
            }
            CompilerHelper.TrackPreprocessedOutput(finalSource, composed);
        }

        public static VmlProgram Compile(string source, List<(string, int)> lineMap = null)
        {
            source = CompilerHelper.InjectDefines(source, "c", CompilerOptionsContext.Current);

            // 内存编译这条路（`CompileFile` 之外的那条）同样要过一遍编译器指令 ——
            // 手机端 `MauiVml` 的「打开 .pas 直接跑」走的就是它。没有文件路径，
            // 所以 `{$I …}` 解析不了（会记一条告警），条件编译照常工作。
            var directives = new PascalDirectives(DirectiveSymbols());
            source = directives.Process(source, null);

            List<(string, int)> ppLineMap = null;
            if (source.Contains('#'))
            {
                var pp = new Preprocessor(source, null, PredefinedMacros) { HashNeedsIdentifier = true };
                source = pp.Process();
                ppLineMap = pp.LineMap;
            }
            if (directives.Changed)
                RegisterLineMap(source, CompilerHelper.UnknownFilePlaceholder, directives.LineMap, ppLineMap);
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
                    codeGen.UseCrtOutput = UsesUnit(parser, "crt");
                    return codeGen.GenerateCode();
                }
                if (ast is UnitNode unitNode)
                {
                    CodeGenerator codeGen = new CodeGenerator(unitNode);
                    codeGen.SourceLines = source.Split('\n');
                    codeGen.UseCrtOutput = UsesUnit(parser, "crt");
                    return codeGen.GenerateCode();
                }
                throw new CodeGenerationException(ErrorCode.CodeGen_UnsupportedExpression, "不支持的AST节点类型");
            });
        }

        public static VmlProgram CompileFile(string filePath, List<string> includePaths = null, List<string> libraryPaths = null, bool autoLinkStdLib = true)
        {
            string source = CompilerHelper.ReadSourceFile(filePath);
            string sourceDir = Path.GetDirectoryName(Path.GetFullPath(filePath));

            var diagnostics = new DiagnosticBag();

            // ── ① Pascal **编译器指令**（`{$IFDEF …}` / `{$ELSE}` / `{$I …}`）─────────
            //
            // ⚠ **必须排在 `#` 预处理之前**：不活跃的 `{$IFDEF}` 分支里可能有
            // `#include`、也可能有一整段为别的编译器写的代码，先让 `#` 预处理过一遍
            // 会为一段**本来就不该被编译**的文本报错（"找不到 xxx.h"）。
            //
            // 这一层从前**根本没有**：`Lexer.SkipComment` 只特判了 `{$param …}`，
            // 其余 `{$…}` 全当注释丢掉，而**两个分支的代码都留在流里** ——
            // `{$IFDEF FPC} PtcGraph {$ELSE} Graph {$ENDIF}` 于是把两边一起编进去。
            // 语料 112 份 Pascal 老程序里 64 份带指令（`{$IFDEF FPC}` 99 处），
            // 这是它们编不过的最大单一原因。
            var directives = new PascalDirectives(DirectiveSymbols());
            source = directives.Process(source, filePath);
            foreach (var (wFile, wLine, wMsg) in directives.Warnings)
                diagnostics.AddWarning(wFile, wLine, 1, ErrorCode.Unknown, wMsg);

            // ── ② C 风格 `#` 预处理（本来就有的那一步，判据一字未改）──────────────
            Preprocessor pp = null;
            List<(string, int)> ppLineMap = null;
            if (source.Contains('#'))
            {
                pp = new Preprocessor(source, includePaths, PredefinedMacros) { HashNeedsIdentifier = true };
                source = pp.Process(filePath);
                ppLineMap = pp.LineMap;
            }

            // ── ③ 把两层预处理的行号映射**组合**后投递（见 `RegisterLineMap`）──────
            if (directives.Changed)
                RegisterLineMap(source, filePath, directives.LineMap, ppLineMap);

            // ⚠ **必须把诊断收集器接给词法器与解析器** —— 这一条是"一次多报多个错误"的前提。
            //
            // 本方法是 Pascal 自己手写的一条流水线（不像其余 17 门走
            // `CompilerHelper.CompileFileStandard` → `Compile` → `CompileWithDiagnostics`），
            // 所以此前 `new Parser(tokens)` **既没 `Diagnostics`、也没 `FileName`**。
            // 后果不是"少了个字段"，而是**错误处理策略整个退化**：
            // `GccError` 见 `Diagnostics == null` 只能**抛**（它没有地方可收），
            // 于是文件里后面的错全部看不到 —— 实测一份有两个错的文件只报出第一条。
            // 接上收集器之后，能继续的错误才会走"收集 + 恢复"，一路报到底。
            // （`diagnostics` 在方法开头就建好了 —— 上面那层指令预处理也要往里报告警。）
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
                if (unitPath != null)
                {
                    RegisterUnitFunctions(unitPath, diagnostics);
                    continue;
                }

                /* ── `uses` 了一个**找不到的单元**：从前是彻底静默 ────────────────────
                 *
                 * `ResolveUnitFile` 返回 null 就 continue，一个字的提示都没有 ⇒ 那个单元
                 * 在程序里**等于不存在**：它声明的常量、子程序头、类型全都没有，
                 * 于是下游表现成一片"未声明的变量 'X'" —— 而**真凶在 `uses` 那一行**，
                 * 离报错位置几十上百行。（实测：语料里 `uses Vector` 9 份、`uses Strings` 5 份、
                 * `uses UMouse`/`Menu` 各 2 份，都是程序自带、本仓库没有的单元，
                 * 它们全都表现成"程序里到处是未声明的变量"。）
                 *
                 * ⚠ **是警告不是错误**，这是刻意的：老程序普遍 `uses` 一堆在本平台
                 * 无关紧要的单元（`ShellApi`、`Printer`、`TpEms`…），硬失败会把它们
                 * 全部挡在门外；而"找不到"本身不影响能编过 —— 它只影响**诊断的质量**。
                 * 把话说清楚，让用户能一眼看到"是我少带了一个文件"，而不是去猜。
                 *
                 * ⚠ 唯一例外的名字是 `System`：它是**隐式**单元（Turbo Pascal 里不用写
                 * `uses` 就已经在作用域内），本平台的 System 语义在**前端内建**，
                 * 没有、也不该有对应的单元文件 ⇒ 显式写 `uses System` 不该被警告。 */
                if (!string.Equals(unitName, "system", StringComparison.OrdinalIgnoreCase)
                    && !LibModuleExists(unitName, sourceDir))
                    diagnostics.AddWarning(filePath, 1, 1, ErrorCode.Unknown,
                        $"找不到单元 '{unitName}'：搜索路径里既没有它的 `.pas` 声明、" +
                        "也没有同名的库模块，它声明的常量/子程序**一个都不会生效**" +
                        "（下游会表现成「未声明的变量」）。若是程序自带的单元，" +
                        "把那份源码一起放进项目目录即可。");
            }

            /* ── 这个 bag 里的**警告**要有出口 ──────────────────────────────────────
             *
             * ⚠ 它此前**收集了没人看**：本方法是 Pascal 自己手写的一条流水线，`diagnostics`
             * 是这个方法的局部量，而唯一读它的地方是上面那句 `if (diagnostics.HasErrors) throw`
             * —— 于是 `{$IFDEF}` 指令层报的告警、以及 `RegisterUnitFunctions` 报的告警
             * **一条都到不了用户眼前**（错误走异常、警告走这里，两条路）。
             * 与 `CodeGeneratorBase.BuildProgram` 里那段"警告要有出口"同一形状、
             * 同一格式（GCC 风 `file:line:col: warning: …`，宿主侧 `VmlDiagnostics` 认这个形状）。
             *
             * ⚠ **位置必须在代码生成之前**：codegen 在 `BuildProgram` 里见 `Diags.HasErrors`
             *   就抛，一旦这句摆在后面，"编译失败"这一路上攒下的告警就全被异常带走了 ——
             *   而"恰恰编不过"正是最需要看见这些告警的时候（实测第一版就摆错了位置）。
             *   本 bag 在 codegen 期间不再新增内容（codegen 自持另一个 bag，见
             *   `CodeGeneratorBase.Diags`），所以放在这里与放在末尾等价、且更可靠。
             *
             * 走 **stderr** 不走 stdout：`vml-out-probe` 那套是拿 stdout 逐字节比对程序输出的，
             * 警告混进 stdout 会把"程序输出对不对"的判据污染掉。 */
            if (diagnostics.WarningCount > 0)
                foreach (var warn in diagnostics.Warnings)
                    Console.Error.WriteLine(warn.ToString().TrimEnd());

            VmlProgram prog;
            if (ast is ProgramNode programNode)
            {
                CodeGenerator codeGen = new CodeGenerator(programNode);
                codeGen.SourceLines = source.Split('\n');
                codeGen.UseCrtOutput = UsesUnit(parser, "crt");
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
    /// <param name="diags">
    /// ⚠ **不是可有可无的**：本方法从前是 `catch { }` 一包到底的，于是"单元文件解析失败"
    /// 与"解析出来根本不是个单元"这两种情况**都表现为"这个单元的常量一个都没登记上"** ——
    /// 用户看到的是「未声明的变量 'White'」，而真凶在一个跟他源码无关的文件里。
    /// </param>
    private static void RegisterUnitFunctions(string unitPath, DiagnosticBag diags)
        {
            /* ⚠ **这里会把"生效中的行号映射"抹掉** —— `LexerBase` 的构造函数每次都会
               `SetActiveLineMap(这份源码对应的表)`，而单元文件是一份**另外的源码**，
               认不到 ⇒ 写成 null。于是**它后面**的代码生成（`DiagPosition` 读的就是那张表）
               失去映射，「未定义的函数」这类错会退回预处理后的行号。
               从前无害（Pascal 大多没有映射，写 null 与原来一样），加上指令预处理之后就
               真的会掉位置了 ⇒ 就地存/恢复。
               ⚠ 这个"谁最后构造谁赢"的形状**不是**本次引入的，`LexerBase` 那条
               `RefreshLineMap` 本来就是无栈的全局认领；这里只是保住本方法不越权改它。 */
            var savedMap = CompilerHelper.ActiveLineMap;
            try {
                string s = File.ReadAllText(unitPath);
                // 单元文件里的 `{$IFDEF}` 同样要处理 —— 否则两个分支的 `interface` 声明
                // 会一起进 `UnitSubprograms`，调用点拿到的是哪个取决于遍历顺序。
                s = new PascalDirectives(DirectiveSymbols()).Process(s, unitPath);
                // ⚠ `FileName` 必须给 —— 不给的话单元文件里的语法错会报成 `<input>:行:列`，
                //    而外面那条告警说的是"这个单元没登记上"，两者对不起来，
                //    排查时还得自己猜是哪个文件第几行（实测踩过）。
                var lx = new Lexer(s) { FileName = unitPath };
                var up = new Parser(lx.Tokenize()) { FileName = unitPath };
                var ast = up.Parse();
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
                else
                {
                    /* ⚠ **走到这里说明"找到了文件、但它不是单元"** —— 最常见的一种是
                       **同名遮蔽**：`sourceDir` 是搜索路径的**第一站**（Pascal 的常规语义，
                       本地文件优先），所以工作区里一个叫 `crt.pas` 的**主程序**会把
                       `Lib/pascal/crt.pas` 整个顶掉，于是 `uses Crt` 之后
                       `TextColor(White)` 报「未声明的变量 'White'」——而 `crt.pas`
                       在用户的目录里躺着、看上去毫无关系。（这个坑实测吃掉过两轮排查。）

                       这里是**警告不是错误**：老程序里"自己写个 `dos.pas` 覆盖库单元"
                       是合法写法，只是我们没能从它那儿拿到声明 ⇒ 把话说清楚，别拦。 */
                    diags.AddWarning(unitPath, 1, 1, ErrorCode.Unknown,
                        $"`uses` 引到一个单元，但它不是单元文件（`{Path.GetFileName(unitPath)}` 里不是 `unit …;`）" +
                        "—— 它声明的常量/子程序**一个都不会生效**。若是工作区里的同名文件遮蔽了库单元，改名即可。");
                }
            }
            catch (System.Exception ex)
            {
                /* 解析失败**也要出声**：静默的后果与上面那条一模一样（符号悄悄全没了），
                   而"少一个常量"在下游只会表现成"未声明的变量"，指不回这里。 */
                diags.AddWarning(unitPath, 1, 1, ErrorCode.Unknown,
                    $"`uses` 引到的单元文件解析失败，其中的声明不会生效：{ex.Message}");
            }
            finally { CompilerHelper.SetActiveLineMap(savedMap); }
        }

        /// <summary>
        /// `uses` 里有没有这个单元 —— **按 Pascal 的规矩不区分大小写**。
        ///
        /// <para>
        /// ⚠ 从前这里是三处各写一遍的 <c>parser.UsesNames.Contains("crt")</c>，而
        /// `UsesNames` 存的是**词法层原样的大小写**（见 `Lexer.ReadIdentifier`：它用小写副本查关键字表，
        /// 但 token 的 Value 仍是 `value.ToString()`）⇒ **只有把小写 `crt` 一个字母不差地写出来**
        /// 才判得中。老程序里的标准写法是 `uses Crt`（语料 112 份里 47 份这么写），
        /// 于是它们**从来没进过 CRT 通道**（`OutputSyscall` 一直退回普通 stdio 号）——
        /// 不报错、只是 `TextColor` 设的颜色在 `WriteLn` 上不生效。
        /// </para>
        ///
        /// <para>
        /// 判据与 `AutoLinkUnit` 对齐（那边一直是 `OrdinalIgnoreCase`，所以 `uses Crt`
        /// **链接** CRT 库是成功的 —— 一个用大写、一个用小写，正是"同一规则两处实现"的典型）。
        /// </para>
        /// </summary>
        /// <summary>
        /// `uses` 的这个单元有没有**可链接的库模块**（`<单元名>.vml`）。
        ///
        /// <para>
        /// 有些单元**本来就不需要 `.pas`**：它们的实现模块直接导出 **Pascal 大小写的裸标签**
        /// （`Lib/shared/dos.vml` 里就是 `GetDate:` / `FindFirst:` 这样），
        /// `uses Dos` 之后 `CALL GetDate` 在链接期按名字就能解析。
        /// 最典型的例子是 `Dos` —— 语料里 26 份程序用它，而 `Lib/pascal/` 下
        /// **没有、也不需要** `dos.pas`。
        /// </para>
        ///
        /// <para>
        /// ⚠ 少了这条判据，"找不到单元"那条告警会对 `uses Dos` **全线误报**
        /// （26 份语料一起报），比不做还糟 —— 所以两个判据必须成对出现。
        /// </para>
        ///
        /// <para>
        /// 候选路径**刻意与 `AutoLinkUnit` 保持一致**（那里是真正去链库的地方）：
        /// 两边不一致的话，会出现"这里说找得到、那里却没链上"这种最难查的分叉。
        /// </para>
        /// </summary>
        private static bool LibModuleExists(string unitName, string sourceDir)
        {
            var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
                ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
            string file = unitName + ".vml";
            var candidates = new List<string?>
            {
                Path.Combine(sourceDir, file),
                !string.IsNullOrEmpty(vmlHome) ? Path.Combine(vmlHome, "Lib", "shared", file) : null,
                !string.IsNullOrEmpty(vmlHome) ? Path.Combine(vmlHome, "Lib", "pascal", file) : null,
                Path.Combine(Directory.GetCurrentDirectory(), "Lib", "shared", file),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Lib", "shared", file),
                Path.Combine(sourceDir, "..", "..", "..", "Lib", "shared", file),
            };
            return candidates.Any(c => c != null && File.Exists(c));
        }

        private static bool UsesUnit(Parser parser, string unitName)
            => parser.UsesNames.Any(u => string.Equals(u, unitName, StringComparison.OrdinalIgnoreCase));

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


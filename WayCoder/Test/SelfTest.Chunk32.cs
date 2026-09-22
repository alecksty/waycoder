using WayCoder.UI.Shared;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// VML 工程文件（`.vmk`）与 Makefile 导入器 —— **枢纽格式**的两半。
    ///
    /// <para>
    /// ## 为什么这一批必须钉住
    ///
    /// `.vmk` 是"各种老项目的构建描述**一次性转进来**、此后以它为准"的那个格式。
    /// 它一旦出错，症状是**静默**的：少读一个宏 ⇒ 老程序编不过或行为不对；
    /// 少读一个头文件路径 ⇒ 报"未声明的变量"，而错误信息指向源码里某一行，
    /// **完全不提构建描述**（`cmatrix` 的 `VERSION` 就是这么卡了一整轮）。
    /// </para>
    ///
    /// <para>
    /// 所以这批判据的重点不是"解析器能跑"，而是**三条不肯让步的规矩**：
    /// </para>
    /// <list type="number">
    /// <item><b>认不出就报错</b>（拼错的 `<Defnes>`、没名字的 `<Define>`、
    /// 不认识的构建文件）—— 静默忽略是这类格式最坏的失败方式；</item>
    /// <item><b>多源文件必须说出来</b> —— VML 没有跨 TU 链接，静默取第一个的后果是
    /// "编过了、少了半个程序"；</item>
    /// <item><b>路径要跨平台</b> —— Windows 上生成的 `.vmk` 得能在 macOS/安卓打开。</item>
    /// </list>
    ///
    /// <para>
    /// ⚠ 这一批能用，全靠 `.vmk` 的三个文件住在 <c>UI/Shared/</c>（桌面自测、`vmlcli`、
    /// 手机端**编同一份源码**）。它们原本放在 `third_party/vml/VMLTool/` ——
    /// 那样只有 `vmlcli` 用得上，桌面自测与手机端各得再抄一份，
    /// 正是本仓头号坑「同一规则两处实现」。
    /// </para>
    /// </summary>
    private static void TestChunk32(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        // ══ 一、`.vmk` 解析 ══
        Section("VML 工程文件 .vmk：解析");

        const string good = """
            <?xml version="1.0" encoding="utf-8"?>
            <VMLProject Version="1">
              <Name>tetris</Name>
              <Entry>src/tetris.c</Entry>
              <Output>build/tetris.vml</Output>
              <Includes>
                <Dir>src</Dir>
                <Dir>../common</Dir>
              </Includes>
              <Defines>
                <Define Name="VERSION" Value="1.2"/>
                <Define Name="DEBUG"/>
              </Defines>
              <Libs><Lib>stdio</Lib></Libs>
            </VMLProject>
            """;

        var p = VmlProject.Parse(good, "/proj");
        Check("完整工程：名字/入口/产物", p.Name == "tetris" && p.Entry == "src/tetris.c" && p.Output == "build/tetris.vml");
        Check("Includes 两条", p.Includes.Count == 2 && p.Includes[1] == "../common");
        Check("Defines 两条", p.Defines.Count == 2);

        // 没有 Value ⇒ "1"，与 `-DFOO` 同义
        Check("`<Define Name=\"DEBUG\"/>` 无值 ⇒ 取 1",
            p.Defines[1].Name == "DEBUG" && p.Defines[1].Value == "1");

        Check("IncludePaths 相对基准是 .vmk 所在目录",
            p.IncludePaths.Count == 2 && p.IncludePaths[0] == Path.GetFullPath("/proj/src"));

        // 没写 <Output> ⇒ 与入口同目录同名的 .vml（与手机端 NextArtifact 同规则）
        var noOut = VmlProject.Parse("<VMLProject><Entry>a/b/main.c</Entry></VMLProject>", "/proj");
        Check("没写 <Output> ⇒ 入口换 .vml",
            noOut.OutputPath == Path.GetFullPath("/proj/a/b/main.vml"));
        Check("没写 <Name> ⇒ 取入口文件名", noOut.Name == "main");

        // ══ 二、认不出必须报错（铁律①）══
        Section("VML 工程文件 .vmk：认不出就报错");

        CheckThrows(Check, Fail, "根元素不对要报错",
            () => VmlProject.Parse("<Project><Entry>a.c</Entry></Project>", "/p"));

        // 拼错的元素名 —— 静默忽略的话，用户会得到"宏没生效、一个字都没提示"
        CheckThrows(Check, Fail, "元素名拼错（<Defnes>）要报错",
            () => VmlProject.Parse("<VMLProject><Entry>a.c</Entry><Defnes/></VMLProject>", "/p"));

        CheckThrows(Check, Fail, "缺 <Entry> 要报错",
            () => VmlProject.Parse("<VMLProject><Name>x</Name></VMLProject>", "/p"));

        CheckThrows(Check, Fail, "`<Define>` 没写名字要报错",
            () => VmlProject.Parse("<VMLProject><Entry>a.c</Entry><Defines><Define Value=\"1\"/></Defines></VMLProject>", "/p"));

        CheckThrows(Check, Fail, "不是合法 XML 要报错",
            () => VmlProject.Parse("<VMLProject><Entry>a.c</Entry>", "/p"));

        // `<Define>FOO=1</Define>` 这种图省事的写法也认（人写 XML 容易这么写）
        var textForm = VmlProject.Parse(
            "<VMLProject><Entry>a.c</Entry><Defines><Define>FOO=bar</Define></Defines></VMLProject>", "/p");
        Check("`<Define>FOO=bar</Define>` 文本形态也认",
            textForm.Defines.Count == 1 && textForm.Defines[0].Name == "FOO" && textForm.Defines[0].Value == "bar");

        // ══ 三、跨平台路径（铁律③）══
        Section("VML 工程文件 .vmk：路径跨平台");

        // Windows 上生成的 .vmk 拿到 macOS/安卓上打开 —— 反斜杠必须当分隔符
        var win = VmlProject.Parse(
            @"<VMLProject><Entry>src\main.c</Entry><Includes><Dir>inc\x</Dir></Includes></VMLProject>", "/proj");
        Check(@"入口src\main.c 在 Unix 上也解析成 src/main.c（否则 Path.Combine 出来是个名字带反斜杠的文件）",
            win.EntryPath == Path.GetFullPath("/proj/src/main.c"));
        Check(@"-I 目录 inc\x 同样跨平台",
            win.IncludePaths[0] == Path.GetFullPath("/proj/inc/x"));

        // 往返：写出去的 .vmk 分隔符一律正斜杠
        var rt = VmlProject.Parse(
            @"<VMLProject><Name>n</Name><Entry>src\main.c</Entry></VMLProject>", "/proj");
        Check("写出去的 <Entry> 是正斜杠（工程文件要能跨平台传）",
            rt.ToXml().Contains("<Entry>src/main.c</Entry>"));

        // 往返一致
        var back = VmlProject.Parse(p.ToXml(), "/proj");
        Check("ToXml → Parse 往返一致（名/入口/产物/宏/包含/库）",
            back.Name == p.Name && back.Entry == p.Entry && back.Output == p.Output
            && back.Includes.Count == p.Includes.Count
            && back.Defines.Count == p.Defines.Count
            && back.Defines[0].Value == p.Defines[0].Value
            && back.Libs.Count == p.Libs.Count);

        // ══ 三之二、产物类型与格式 ══
        Section("VML 工程文件 .vmk：产物类型与格式");

        var dflt = VmlProject.Parse("<VMLProject><Entry>a.c</Entry></VMLProject>", "/p");
        Check("默认 类型=exe / 格式=vml（老行为，既有 .vmk 一个字节都不用改）",
            dflt.OutputKind == "exe" && dflt.OutputFormat == "vml");
        Check(@"默认后缀仍是 .vml", dflt.OutputPath.EndsWith("a.vml"));

        var vmb = VmlProject.Parse("<VMLProject><Entry>a.c</Entry><Output Format=\"vmb\"/></VMLProject>", "/p");
        Check(@"Format=""vmb"" ⇒ 默认后缀自动变 .vmb（不用手写 Output 路径）",
            vmb.OutputFormat == "vmb" && vmb.OutputPath.EndsWith("a.vmb"));

        var lib = VmlProject.Parse("<VMLProject><Entry>a.c</Entry><Output Kind=\"lib\"/></VMLProject>", "/p");
        Check(@"Kind=""lib"" 认得（库）", lib.OutputKind == "lib");

        // 预留的格式名要**认得**（好让 make 报"还没做"而不是"拼错了"）
        var hex = VmlProject.Parse("<VMLProject><Entry>a.c</Entry><Output Format=\"hex\"/></VMLProject>", "/p");
        // ⚠ 中文引号 `「」` 不是手滑：这段是 verbatim 字符串 `@"…"`，里面再写 ASCII 双引号
        //   要么 `""` 转义、要么干脆别用 —— 直接写会**提前结束字符串**，报一堆语法错。
        Check(@"预留格式 hex 认得（留给 make 报「还没做」而不是「拼错了」）",
            hex.OutputFormat == "hex");

        // 压根不认识的名字要报错（多半是拼错）
        CheckThrows(Check, Fail, @"Format=""vml2""（拼错）要报错",
            () => VmlProject.Parse("<VMLProject><Entry>a.c</Entry><Output Format=\"vml2\"/></VMLProject>", "/p"));
        CheckThrows(Check, Fail, @"Kind=""dll""（不是 exe/lib）要报错",
            () => VmlProject.Parse("<VMLProject><Entry>a.c</Entry><Output Kind=\"dll\"/></VMLProject>", "/p"));

        // 属性只在**非默认**时写出来（默认的 exe/vml 是老行为，写出来只是噪音）
        Check("默认不写 Kind/Format 属性", !dflt.ToXml().Contains("Kind=") && !dflt.ToXml().Contains("Format="));
        Check(@"非默认要写出来（往返不丢）",
            vmb.ToXml().Contains("Format=\"vmb\"")
            && VmlProject.Parse(vmb.ToXml(), "/p").OutputFormat == "vmb");

        // ══ 四、Makefile 导入 ══
        Section("Makefile → .vmk 导入");

        var dir = Path.Combine(Path.GetTempPath(), "vmk-selftest-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(Path.Combine(dir, "src"));
            Directory.CreateDirectory(Path.Combine(dir, "include"));
            File.WriteAllText(Path.Combine(dir, "src", "main.c"), "int main(void){return 0;}\n");
            File.WriteAllText(Path.Combine(dir, "src", "util.c"), "int helper(void){return 7;}\n");

            // ① 单源 + CFLAGS（典型手写 Makefile）
            File.WriteAllText(Path.Combine(dir, "Makefile"), """
                CC      = gcc
                CFLAGS  = -O2 -Wall -DVERSION=\"1.2\" -Iinclude
                LIBS    = -lm
                SRCS    = src/main.c
                OBJS    = $(SRCS:.c=.o)

                prog: $(OBJS)
                	$(CC) $(CFLAGS) -o prog $(OBJS) $(LIBS)
                """);

            var r = new MakefileImporter().Import(Path.Combine(dir, "Makefile"));
            Check("入口 = 含 main 的那个", r.Project.Entry.Replace('\\', '/') == "src/main.c");
            Check("-I 抽出来了", r.Project.Includes.Count == 1 && r.Project.Includes[0] == "include");
            Check("-D 抽出来了", r.Project.Defines.Count == 1 && r.Project.Defines[0].Name == "VERSION");

            // ⚠ 转义引号是**值的一部分**：gcc 收到 `-DVERSION="1.2"`，VERSION 展开成字符串字面量
            Check("`-DVERSION=\\\"1.2\\\"` ⇒ 值是带引号的 `\"1.2\"`（老程序靠它当字符串用）",
                r.Project.Defines[0].Value == "\"1.2\"");
            Check("单源不该有多源提示", r.Notes.Count == 0);

            // ② 多源 —— **必须报出来**（VML 没有跨 TU 链接）
            File.WriteAllText(Path.Combine(dir, "Makefile.multi"), """
                SRCS = src/main.c src/util.c
                OBJS = $(SRCS:.c=.o)
                prog: $(OBJS)
                	gcc -o prog $(OBJS)
                """);
            var r2 = new MakefileImporter().Import(Path.Combine(dir, "Makefile.multi"));
            Check("多源文件：入口取含 main 的那个", r2.Project.Entry.Replace('\\', '/') == "src/main.c");
            Check("多源文件：**必须有提示**（静默取第一个 = 编过了、少了半个程序）",
                r2.Notes.Count == 1 && r2.Notes[0].Contains("util.c"));

            // ③ `$(MAKE)` 转发式 —— 拒绝并说明原因
            File.WriteAllText(Path.Combine(dir, "Makefile.fwd"), "all:\n\t$(MAKE) -C sub\n");
            CheckThrows(Check, Fail, "转发式 Makefile 要拒绝",
                () => new MakefileImporter().Import(Path.Combine(dir, "Makefile.fwd")));

            // ④ `-include`（IDE 生成的 .mk）—— 拒绝
            File.WriteAllText(Path.Combine(dir, "sources.mk"), "-include subdir.mk\nSRCS = a.c\n");
            CheckThrows(Check, Fail, "`-include` 递归依赖（IDE 生成的 .mk）要拒绝",
                () => new MakefileImporter().Import(Path.Combine(dir, "sources.mk")));

            // ⑤ 裸引号会被 shell 吃掉 ⇒ 剥掉（与转义引号相反，见上面 ①）
            File.WriteAllText(Path.Combine(dir, "Makefile.bare"), """
                CFLAGS = -DFOO="bar"
                SRCS   = src/main.c
                prog: $(SRCS)
                	gcc $(CFLAGS) -o prog $(SRCS)
                """);
            var r3 = new MakefileImporter().Import(Path.Combine(dir, "Makefile.bare"));
            Check("`-DFOO=\"bar\"`（裸引号）⇒ 值是不带引号的 `bar`",
                r3.Project.Defines.Count == 1 && r3.Project.Defines[0].Value == "bar");

            // ⑥ 一个源文件都找不到 ⇒ 报错，不是返回空工程
            File.WriteAllText(Path.Combine(dir, "Makefile.empty"), "all:\n\techo nothing\n");
            CheckThrows(Check, Fail, "找不出源文件要报错（不是给个空工程）",
                () => new MakefileImporter().Import(Path.Combine(dir, "Makefile.empty")));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }

        // ══ 五、导入器注册表（"以后加格式"的落点）══
        Section("导入器注册表");

        Check("Makefile 认得", ProjectImporters.Resolve("Makefile")?.Name == "make");
        Check("makefile 大小写不敏感", ProjectImporters.Resolve("makefile")?.Name == "make");
        Check("*.mk 认得", ProjectImporters.Resolve("sources.mk")?.Name == "make");
        Check("CMakeLists.txt 现在还不认（将来加 CMakeImporter 时这条要改）",
            ProjectImporters.Resolve("CMakeLists.txt") is null);
    }

    /// <summary>
    /// 断言"这段代码必须抛 <see cref="VmlProjectException"/>"。
    ///
    /// ⚠ **必须区分"抛了"与"没抛"**，不能只写 `try { …; Check(false) } catch {}` ——
    /// 那样连 `NullReferenceException` 也算通过，"报错"就变成了"崩了也算"。
    /// </summary>
    private static void CheckThrows(Action<string, bool> Check, Action<string> Fail, string what, Action act)
    {
        try
        {
            act();
            Check($"{what}（**没抛**）", false);
        }
        catch (VmlProjectException)
        {
            Check(what, true);
        }
        catch (Exception ex)
        {
            Check($"{what}（抛的是 {ex.GetType().Name}，不是 VmlProjectException）", false);
        }
    }
}

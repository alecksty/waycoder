using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Edit;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.TUI.Renderers;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>把当前屏幕帧解释成文本行 —— 复用按键测试那套 FrameBuffer 模拟器，勿再手写 ANSI 解析。</summary>
    private static List<string> FrameLines(TuiManager mgr)
    {
        var fb = new FrameBuffer(Tty.Rows, Tty.Cols);
        fb.Apply(mgr.LastCleanFrame);
        return fb.Dump();
    }

    private static void TestChunk12(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[TuiMarkup 新标签]");

        // ── ListView / DynamicBar / PromptBar / SidePanel 标签解析 ──
        var lvRes = TuiMarkup.Load("<VBox><ListView id=\"lv\" itemSpacing=\"3\" autoScroll=\"false\"/></VBox>");
        var lv = lvRes.Find<TuiListView>("lv");
        Check("TuiMarkup ListView 解析", lv != null);
        Check("TuiMarkup ListView itemSpacing", lv != null && lv.ItemSpacing == 3);
        Check("TuiMarkup ListView autoScroll=false", lv != null && !lv.IsAutoScrollToEnd);

        var dbRes = TuiMarkup.Load("<VBox><DynamicBar id=\"db\"/></VBox>");
        Check("TuiMarkup DynamicBar 解析", dbRes.Find<TuiDynamicBar>("db") != null);

        var pbRes = TuiMarkup.Load("<VBox><PromptBar id=\"pb\" maxVisible=\"6\" itemHeight=\"2\" separatorColor=\"yellow\"/></VBox>");
        var pb = pbRes.Find<TuiPromptBar>("pb");
        Check("TuiMarkup PromptBar 解析", pb != null);
        Check("TuiMarkup PromptBar maxVisible", pb != null && pb.MaxVisible == 6);
        Check("TuiMarkup PromptBar itemHeight", pb != null && pb.ItemHeight == 2);

        var spRes = TuiMarkup.Load("<VBox><SidePanel id=\"sp\" borderWidth=\"2\" borderColor=\"yellow\" panelVisible=\"false\"/></VBox>");
        var sPanel = spRes.Find<TuiSidePanel>("sp");
        Check("TuiMarkup SidePanel 解析", sPanel != null);
        Check("TuiMarkup SidePanel borderWidth", sPanel != null && sPanel.BorderWidth == 2);
        Check("TuiMarkup SidePanel panelVisible=false", sPanel != null && !sPanel.PanelVisible);

        // SidePanel 嵌套 <Section><Line> 声明分区（布局写标记）
        var secRes = TuiMarkup.Load("<VBox><SidePanel id=\"sp2\"><Section title=\"🏷 道码\"><Line text=\"WayCoder v1\"/><Line text=\"AOT\"/></Section><Section title=\"Todo\"><Line text=\"空\"/></Section></SidePanel></VBox>");
        var sp2 = secRes.Find<TuiSidePanel>("sp2");
        Check("SidePanel Section 解析", sp2 != null && sp2.Sections.Count == 2);
        Check("SidePanel Section 标题", sp2 != null && sp2.Sections[0].Title == "🏷 道码" && sp2.Sections[1].Title == "Todo");
        Check("SidePanel Section 行", sp2 != null && sp2.Sections[0].Lines.Count == 2
            && sp2.Sections[0].Lines[0] == "WayCoder v1" && sp2.Sections[0].Lines[1] == "AOT");
        // 空 Section 忽略
        var secEmpty = TuiMarkup.Load("<VBox><SidePanel id=\"sp3\"><Section title=\"\"></Section></SidePanel></VBox>");
        Check("SidePanel 空 Section 忽略", secEmpty.Find<TuiSidePanel>("sp3") is { Sections.Count: 0 });

        // ── 既有标签属性补齐 ──
        var tbRes = TuiMarkup.Load("<VBox><TitleBar id=\"tb\" title=\"T\" center=\"C\" version=\"v1\" gitBranch=\"master\"/></VBox>");
        Check("TuiMarkup TitleBar gitBranch", tbRes.Find<TuiTitleBar>("tb") is { GitBranch: "master" });

        var taRes = TuiMarkup.Load("<VBox><TextArea id=\"ta\" placeholder=\"ph\" showLineNumbers=\"false\"/></VBox>");
        var ta = taRes.Find<TuiTextArea>("ta");
        Check("TuiMarkup TextArea placeholder", ta != null && ta.Placeholder == "ph");
        Check("TuiMarkup TextArea showLineNumbers=false", ta != null && !ta.ShowLineNumbers);

        var mdRes = TuiMarkup.Load("<VBox><Markdown id=\"md\" role=\"user\" plainText=\"true\" isError=\"true\"/></VBox>");
        var md = mdRes.Find<WayCoder.UI.Tui.Controls.TuiMarkdown>("md");
        Check("TuiMarkup Markdown role", md != null && md.Role == "user");
        Check("TuiMarkup Markdown plainText", md != null && md.IsPlainText);
        Check("TuiMarkup Markdown isError", md != null && md.IsError);

        var sepRes = TuiMarkup.Load("<VBox><Separator id=\"sep\" lineChar=\"━\" lineColor=\"yellow\"/></VBox>");
        var sep = sepRes.Find<TuiSeparator>("sep");
        Check("TuiMarkup Separator lineChar", sep != null && sep.LineChar == "━");
        Check("TuiMarkup Separator lineColor", sep != null && sep.LineColor > 0);

        var vbRes = TuiMarkup.Load("<VBox id=\"v\" floating=\"true\"/>");
        var vb = vbRes.Find<TuiVBox>("v");
        Check("TuiMarkup VBox floating", vb != null && vb.Floating);

        Console.WriteLine();

        // ── UI/TUI/Raw/chat.tui 完整聊天布局加载（文件系统优先、嵌入资源兜底）──
        Section("[TuiMarkup chat.tui]");
        try
        {
            var main = TuiMarkup.LoadResource("chat.tui");
            Check("chat.tui Screen 根", main.Screen != null);
            Check("chat.tui RootView 非空", main.Screen?.RootView != null);
            int childCount = main.Screen?.RootView?.Children.Count ?? 0;
            Check("chat.tui RootView 子节点=13", childCount == 13); // 模式栏下 shortcutRow（快捷键行）+ inlineChoice（行内选择栏）

            Check("chat.tui titleBar", main.Find<TuiTitleBar>("titleBar") != null);
            Check("chat.tui chatList", main.Find<TuiListView>("chatList") != null);
            Check("chat.tui sidePanel", main.Find<TuiSidePanel>("sidePanel") != null);
            Check("chat.tui suggestPanel", main.Find<TuiVBox>("suggestPanel") != null);
            Check("chat.tui promptBar", main.Find<TuiPromptBar>("promptBar") != null);
            Check("chat.tui inlineChoice(行内选择栏)", main.Find<TuiPromptBar>("inlineChoice") != null);
            Check("chat.tui dynamicBar", main.Find<TuiDynamicBar>("dynamicBar") != null);
            Check("chat.tui inputArea", main.Find<TuiTextArea>("inputArea") != null);
            Check("chat.tui statusBar", main.Find<TuiStatusBar>("statusBar") != null);
            Check("chat.tui modelInfoRow(SmartLabel)", main.Find<TuiSmartLabel>("modelInfoRow") != null);
            Check("chat.tui shortcutRow(SmartLabel)", main.Find<TuiSmartLabel>("shortcutRow") != null);
            Check("chat.tui modelInfoRow 居中", main.Find<TuiSmartLabel>("modelInfoRow")?.TextAlign == EHAlign.Center);
            Check("chat.tui shortcutRow 居中", main.Find<TuiSmartLabel>("shortcutRow")?.TextAlign == EHAlign.Center);
        }
        catch (Exception ex)
        {
            Check($"chat.tui 加载失败: {ex.Message}", false);
        }
        Console.WriteLine();

        // ── MarkupChatScreen 实例化 + 渲染冒烟（无头，复用 ChatScreen 渲染链路）──
        Section("[MarkupChatScreen]");
        var mgr = TuiManager.Instance;
        var mScreen = new MarkupChatScreen();
        string frame = "";
        int chatListW = 0;
        bool entered = false;
        try
        {
            var prevOut = Console.Out;
            Console.SetOut(TextWriter.Null); // 抑制 Enter/Render 的屏幕输出，仅渲染不打印
            try
            {
                if (!mgr.IsActive) { mgr.Enter(); entered = true; }
                mgr.PushScreen(mScreen);
                chatListW = mScreen.ChatList.Width; // Activate 应已应用动态尺寸（横幅居中前提）
                mScreen.SyncTheme();
                mScreen.RefreshTheme();
                mgr.Render();
                frame = mgr.LastCleanFrame;
                mgr.PopScreen();
            }
            finally { Console.SetOut(prevOut); }
        }
        catch (Exception ex)
        {
            Check($"MarkupChatScreen 初始化失败: {ex.Message}", false);
        }
        finally
        {
            if (entered) { try { mgr.Exit(); } catch { } }
        }
        // Activate 末尾 ApplyDynamicSizes 后 ChatList.Width 应=屏幕宽（默认无侧栏），否则欢迎横幅按错误宽度居中
        Check("Activate 后 ChatList.Width 就绪=TW", chatListW > 0 && chatListW == mScreen.TW);

        // ── 回归：提示栏开合不吞聊天消息（MarkDirty→MarkTreeDirty 修复）──
        // 提示栏出现/消失会改 chatH → ChatList 渲染时先整视口擦成空白；若只标脏容器（MarkDirty）不标脏子项，
        // 消息子项因 parentDirty=false 不重画 → 收起提示栏后聊天内容永久空白（MarkTreeDirty 修复）。累积增量帧到网格断言消息仍在。
        Section("[提示栏开合保留消息]");
        {
            var savedSz = Tty.SizeOverride;
            Tty.SizeOverride = (100, 30);
            var mgrR = TuiManager.Instance;
            bool enteredR = false;
            bool msgPresent = false;
            var msgText = "提示栏开合回归标记XYZ";
            try
            {
                var prevOut = Console.Out;
                Console.SetOut(TextWriter.Null); // 抑制 Enter/Render 屏幕输出（LastCleanFrame 仍填充）
                try
                {
                    if (!mgrR.IsActive) { mgrR.Enter(); enteredR = true; }
                    var chatR = new MarkupChatScreen();
                    mgrR.PushScreen(chatR);
                    chatR.AddMessage(msgText, "user");
                    mgrR.Render();
                    var fb = new FrameBuffer(Tty.Rows, Tty.Cols);
                    fb.Apply(mgrR.LastCleanFrame);
                    // 弹出提示栏（模拟输入 / 前缀）→ 收起，消息不应被擦成空白
                    chatR.ShowPromptBar(new List<PromptItem> { new() { Label = "测试项", Value = "x" } });
                    mgrR.Render();
                    fb.Apply(mgrR.LastCleanFrame);
                    chatR.HidePromptBar();
                    mgrR.Render();
                    fb.Apply(mgrR.LastCleanFrame);
                    msgPresent = fb.Dump().Any(l => l.Contains(msgText));
                }
                finally { Console.SetOut(prevOut); }
            }
            catch (Exception ex)
            {
                Check($"提示栏开合渲染异常: {ex.Message}", false);
            }
            finally
            {
                if (enteredR) { try { mgrR.Exit(); } catch { } }
                Tty.SizeOverride = savedSz;
            }
            Check("提示栏开合后聊天消息保留", msgPresent);
        }
        Console.WriteLine();

        // ── 聊天区代码块语法高亮（走完整渲染路径：AddMessage → ChatList → TuiMarkdown → ANSI）──
        // 只测 TuiMarkdown.RenderMessage 的返回段是不够的：颜色可能在后续节点包装/裁剪里丢掉。
        Section("[聊天区代码块高亮]");
        {
            var savedSzC = Tty.SizeOverride;
            Tty.SizeOverride = (100, 30);
            var mgrC = TuiManager.Instance;
            bool enteredC = false;
            bool keyColor = false, strColor = false;
            try
            {
                var prevOut = Console.Out;
                Console.SetOut(TextWriter.Null);
                try
                {
                    if (!mgrC.IsActive) { mgrC.Enter(); enteredC = true; }
                    var chatC = new MarkupChatScreen();
                    mgrC.PushScreen(chatC);
                    chatC.AddMessage("```csharp\npublic class Demo { string s = \"hi\"; return 42; }\n```", "assistant");
                    mgrC.Render();
                    var raw = mgrC.LastCleanFrame;
                    keyColor = raw.Contains(AnsiTty.FgCode(Syntax.Keyword));
                    strColor = raw.Contains(AnsiTty.FgCode(Syntax.Str));
                    mgrC.PopScreen();
                }
                finally { Console.SetOut(prevOut); }
            }
            catch (Exception ex)
            {
                Check($"聊天区代码块渲染异常: {ex.Message}", false);
            }
            finally
            {
                if (enteredC) { try { mgrC.Exit(); } catch { } }
                Tty.SizeOverride = savedSzC;
            }
            Check("聊天区代码块：关键字色出现在渲染输出", keyColor);
            Check("聊天区代码块：字符串色出现在渲染输出", strColor);
            // ```c# 是模型最常写的标签之一，此前不在 ByLanguage 表里 → 整块落到 Plain（看不出高亮）
            Check("代码块语言 ```c# 认得（别名表）", Syntax.ByLanguage("c#").Name == "C#");
            Check("代码块语言 ```C# 大小写不敏感", Syntax.ByLanguage("C#").Name == "C#");
            Check("不认识的标签走通用表（而非整块白）", Syntax.ByLanguage("brainfuck").Name == Syntax.GenericName);
            Check("```text 显式纯文本仍不上色", Syntax.ByLanguage("text").Name == "纯文本");

            // 运算符与括号标点也上色（此前只有关键字/字符串/注释/数字）
            var symSyn = Syntax.ForFile("a.cs");
            Check("语法：运算符着色",
                symSyn.Tokenize("a = b + c;").Any(t => t.Text == "=" && t.Color == Syntax.Operator));
            Check("语法：多字符运算符合并成一段",
                symSyn.Tokenize("x != y").Any(t => t.Text == "!=" && t.Color == Syntax.Operator));
            Check("语法：括号着色",
                symSyn.Tokenize("f(x)").Any(t => t.Text == "(" && t.Color == Syntax.Bracket));
            Check("语法：逗号分号着色",
                symSyn.Tokenize("a, b;").Any(t => t.Text == "," && t.Color == Syntax.Bracket));
            // 纯文本不色符号：散文里的括号、破折号、冒号不该变成代码色
            Check("语法：纯文本不着色符号",
                !Syntax.ByLanguage("text").Tokenize("a - b (c)").Any(t => t.Color == Syntax.Operator));
            // 字符字面量独立于字符串；标识符按「函数 / 类型名 / 普通」三档分类
            var cs3 = Syntax.ForFile("a.cs");
            Check("语法：字符字面量独立着色（与字符串分开）",
                cs3.Tokenize("a = 'x';").Any(t => t.Text == "'x'" && t.Color == Syntax.Char));
            Check("语法：函数名着色（后跟括号）",
                cs3.Tokenize("f(x)").Any(t => t.Text == "f" && t.Color == Syntax.Function));
            Check("语法：类型名着色（首字母大写）",
                cs3.Tokenize("var x = Foo;").Any(t => t.Text == "Foo" && t.Color == Syntax.TypeName));
            // 普通标识符给**明确的亮灰**而非 Default(0)：0 = 用终端默认前景，而暗色终端的
            // 默认前景本身就偏暗，会和注释（238 暗灰）糊成一片 —— 用户实测「标识符和注释一样」
            Check("语法：标识符与注释颜色分得开",
                Syntax.Identifier != Syntax.Comment && Syntax.Identifier > Syntax.Comment);
            Check("语法：普通标识符用亮灰（不是 Default）",
                cs3.Tokenize("var abc = 1;").Any(t => t.Text == "abc" && t.Color == Syntax.Identifier));
            // 纯文本仍不上色（连标识符色也不给）
            Check("语法：纯文本的标识符不上色",
                Syntax.ByLanguage("text").Tokenize("hello world").All(t => t.Color == Syntax.Default));

            // Windows 批处理：独立语言（注释是 REM/::，变量是 %VAR%，与 Shell 的 #/$VAR 两套）
            Check("语法：.bat 识别为批处理", Syntax.ForFile("run.bat").Name == Syntax.BatchName);
            Check("语法：.cmd 识别为批处理", Syntax.ForFile("run.cmd").Name == Syntax.BatchName);
            var bat = Syntax.ByLanguage("bat");
            Check("语法：批处理关键字着色",
                bat.Tokenize("if exist x goto end").Any(t => t.Text == "if" && t.Color == Syntax.Keyword));
            Check("语法：批处理 REM 注释", bat.Tokenize("REM 这是注释").Any(t => t.Color == Syntax.Comment));
            Check("语法：批处理 :: 注释", bat.Tokenize(":: 也是注释").Any(t => t.Color == Syntax.Comment));
            Check("语法：批处理 %VAR% 变量",
                bat.Tokenize("echo %PATH%").Any(t => t.Text == "%PATH%" && t.Color == Syntax.Variable));

            // Linux shell：关键字扩充 + $VAR / ${VAR}
            var sh = Syntax.ByLanguage("bash");
            Check("语法：shell 关键字扩充（until/select/coproc 等）",
                sh.Tokenize("until select coproc").Count(t => t.Color == Syntax.Keyword) == 3);
            Check("语法：shell $VAR 变量",
                sh.Tokenize("echo $HOME").Any(t => t.Text == "$HOME" && t.Color == Syntax.Variable));
            Check("语法：shell ${VAR} 变量",
                sh.Tokenize("echo ${HOME}").Any(t => t.Text == "${HOME}" && t.Color == Syntax.Variable));
            Check("语法：shell 不把 % 当变量（两套语法不串）",
                !sh.Tokenize("echo 100%").Any(t => t.Color == Syntax.Variable));

            // 通用兜底（无语言标签的代码块）仍要色符号
            Check("语法：通用表也色符号",
                Syntax.Generic().Tokenize("a = (b)").Any(t => t.Color == Syntax.Operator));
            var noTag = UI.Tui.TuiMarkdown.RenderMessage("```\npublic class A { return null; }\n```", "assistant", 80);
            Check("无语言标签代码块：关键字仍上色（通用表兜底）",
                noTag.SelectMany(l => l).Any(s => s.Fg == Syntax.Keyword));
            // 通用表刻意剔掉英文常用词，散文不该被误上色
            var prose = UI.Tui.TuiMarkdown.RenderMessage("```\nThis is a note and it has no code in it.\n```", "assistant", 80);
            Check("通用表不误色散文（in/is/and 不在表内）",
                !prose.SelectMany(l => l).Any(s => s.Fg == Syntax.Keyword));

            // ── 工具输出（write / edit 贴出的代码）：靠**文件后缀**定语言 ──
            // 这类代码没有语言标注，但文件路径一定有 —— 比内容启发式准得多。
            var addedCs = WayCoder.UI.Tui.ContentDiffFormatter.FormatAddedContent(
                "public class A { return 1; }", "/tmp/A.cs");
            Check("写文件展示：按 .cs 后缀上语法色", addedCs.Contains("fg:#"));
            Check("写文件展示：行号与 + 标记仍在", addedCs.Contains(" +«/»"));
            var addedPy = WayCoder.UI.Tui.ContentDiffFormatter.FormatAddedContent(
                "def f():\n    return 1", "/tmp/a.py");
            Check("写文件展示：按 .py 后缀上语法色", addedPy.Contains("fg:#"));
            var addedTxt = WayCoder.UI.Tui.ContentDiffFormatter.FormatAddedContent(
                "hello world", "/tmp/a.txt");
            Check("写文件展示：.txt 不上色（关键字表为空）", !addedTxt.Contains("fg:#"));

            var editCs = WayCoder.UI.Tui.ContentDiffFormatter.FormatEditContent(
                "public class A { }\n", "public class A { int x = 1; }\n", "/tmp/A.cs");
            Check("编辑展示：+ 行代码上语法色", editCs.Contains("fg:#"));

            var markupInCode = WayCoder.UI.Tui.ContentDiffFormatter.FormatAddedContent(
                "var s = \"«red»\";", "/tmp/A.cs");
            Check("代码含 «» 字面量时不上色（标记无转义机制）", !markupInCode.Contains("fg:#"));

            // 端到端：256 色语法值 → «fg:#rrggbb» → 渲染成真彩 ANSI（三端都认真彩标记）
            var cdfSegs = UI.Tui.TuiMarkdown.RenderMessage(addedCs, "tool", 80);
            Check("工具输出的语法色渲染成真彩 ANSI", cdfSegs.SelectMany(l => l).Any(s => s.Fg >= 0x1000000));

            // diff：+/- 行铺底色（对标竞品），上下文行无底色但**代码同样上语法色**
            var editBg = WayCoder.UI.Tui.ContentDiffFormatter.FormatEditContent(
                "public class A { }\npublic class B { }\n",
                "public class A { int x = 1; }\npublic class B { }\n", "/tmp/A.cs");
            Check("diff：+ 行有暗绿背景", editBg.Contains("bg:#0e2a17"));

            // 底色铺满行尾（右对齐到渲染宽度）——竞品都是这样，只裹住文字会在右侧留断口
            var diffRows = UI.Tui.TuiMarkdown.RenderMessage(
                "@@ -1 +1 @@\n+public class A { }\n", "tool", 40, plainText: true);
            var plusRow = diffRows.FirstOrDefault(r => string.Concat(r.Select(x => x.Text)).Contains("public class A"));
            int plusRowW = plusRow?.Sum(x => AnsiHelper.DisplayWidth(x.Text)) ?? 0;
            Check("diff：+ 行底色铺满到渲染宽度", plusRowW == 40);
            var plainRows = UI.Tui.TuiMarkdown.RenderMessage("普通一行文字", "tool", 40, plainText: true);
            Check("普通文本行不补底色（无背景）",
                plainRows.All(r => r.All(x => x.Bg == 0)));
            // 上下文行的行号段后紧跟语法色段（行号灰 «grey»   2  «/» + 代码 «fg:#..»public«/»）
            Check("diff：上下文行代码也上语法色",
                editBg.Split('\n').Any(l => l.Contains("   2  «/»") && l.Contains("fg:#")));

            // ── 容错围栏：模型把 ``` 写成 ` 或 `` 的情况 ──
            // 判据本身是纯逻辑，直接测比透过渲染间接验灵敏得多
            Check("CodeFence: 单反引号+语言名 → 开栏",
                CodeFence.TryOpen("`csharp", out var fl1, out var ft1) && fl1 == "csharp" && ft1 == 1);
            Check("CodeFence: 双反引号+语言名 → 开栏",
                CodeFence.TryOpen("``py", out var fl2, out var ft2) && fl2 == "py" && ft2 == 2);
            Check("CodeFence: 标准三反引号",
                CodeFence.TryOpen("```js", out _, out var ft3) && ft3 == 3);
            Check("CodeFence: 四反引号围栏仍认", CodeFence.TryOpen("````xml", out _, out var ft4) && ft4 == 4);
            Check("CodeFence: 普通正文行不是开栏", !CodeFence.TryOpen("这是正文段落", out _, out _));
            Check("CodeFence: 行内代码 `foo bar`（含空格）不算开栏",
                !CodeFence.TryOpen("`foo bar`", out _, out _));
            Check("CodeFence: 闭栏要整行纯反引号",
                CodeFence.IsClose("`", 1) && !CodeFence.IsClose("`x", 1) && CodeFence.IsClose("````", 3));
            // 实测里出现过的三种围栏：1 个、3 个、4 个反引号；开闭数量还可能不一致
            Check("CodeFence: 4 开 4 闭", CodeFence.IsClose("````", 4));
            Check("CodeFence: 4 开 3 闭也认（模型常有，否则会吞掉后面所有正文）",
                CodeFence.IsClose("```", 4));
            Check("CodeFence: 闭栏带杂字不认", !CodeFence.IsClose("``` x", 3));

            // 渲染级：单/双反引号的多行块真的被当代码块（关键字上色）
            // 判据用**结构性事实**（代码块渲染会带行号、语言标签不再含反引号），
            // 不用「有没有关键字色」——行内代码也可能被上某种色，那种断言会假阳性
            static string Flat(List<List<(string Text, int Fg, int Bg)>> rows)
                => string.Concat(rows.SelectMany(r => r).Select(s => s.Text));
            var shortFence = UI.Tui.TuiMarkdown.RenderMessage(
                "`csharp\npublic class A { return 1; }\n`", "assistant", 80);
            var shortTxt = Flat(shortFence);
            Check("单反引号围栏：识别为代码块（带行号、标签无残余反引号）",
                shortTxt.Contains("1 public class A") && !shortTxt.Contains("`csharp"));
            // 前置说明行的情况（keypad 实测里就是这种）：围栏不在首行
            var fenced2 = UI.Tui.TuiMarkdown.RenderMessage(
                "说明文字：\n`csharp\npublic class A { return 1; }\n`", "assistant", 80);
            Check("前置说明行后的单反引号围栏也识别", Flat(fenced2).Contains("1 public class A"));
            var doubleFence = UI.Tui.TuiMarkdown.RenderMessage(
                "``py\ndef f(): return 1\n``", "assistant", 80);
            var doubleTxt = Flat(doubleFence);
            Check("双反引号围栏：识别为代码块（带行号、标签无残余反引号）",
                doubleTxt.Contains("1 def f()") && !doubleTxt.Contains("``py"));
            var fourFence = UI.Tui.TuiMarkdown.RenderMessage(
                "````csharp\npublic class A { return 1; }\n````", "assistant", 80);
            Check("4 反引号围栏：识别为代码块", Flat(fourFence).Contains("1 public class A"));
            var mixFence = UI.Tui.TuiMarkdown.RenderMessage(
                "说明：\n````csharp\npublic class A { return 1; }\n```", "assistant", 80);
            var mixTxt = Flat(mixFence);
            Check("4 开 3 闭 + 前置说明：识别为代码块且不吞后续正文",
                mixTxt.Contains("1 public class A") && !mixTxt.Contains("````csharp"));
        }
        Console.WriteLine();

        // ── 动态栏：子智能体数（右段 🤖N）──
        Section("[动态栏 · 子智能体数]");
        {
            var initial = AgentTool.ActiveSubAgents; // 没有子智能体在跑时应为 0
            var db = new TuiDynamicBar { Width = 100 };
            db.SubAgentCount = 3;
            bool shown = db.BuildRightItems(0, 60).Any(i => i.Text.Contains("🤖3"));
            db.SubAgentCount = 0;
            bool hiddenWhenZero = !db.BuildRightItems(0, 60).Any(i => i.Text.Contains("🤖"));
            Check("子智能体计数：空闲时为 0", initial == 0);
            Check("动态栏右段：子智能体数 >0 时产出 🤖N", shown);
            Check("动态栏右段：子智能体数 =0 时不占位", hiddenWhenZero);

            // 右对齐：末项右端贴住控件右边缘（absX + Width - 1）
            var db2 = new TuiDynamicBar { Width = 100 };
            db2.CpuPercent = 12;
            db2.TokenDisplay = "🔤大12K 小3K";
            db2.CostDisplay = "¥0.42";
            var items2 = db2.BuildRightItems(0, 50);
            int lastEnd = items2[^1].Col + AnsiHelper.DisplayWidth(items2[^1].Text) - 1;
            Check("动态栏右段：末项贴右边缘（右对齐）", lastEnd == 99);

            // 几何：左段压到 1/5、中段吃掉剩余（工具命令优先放得下）
            var geo2 = db2.ComputeGeometry(0, 100);
            Check("动态栏几何：中段明显宽于左段", geo2.MidWidth > 100 / 5);
            Check("动态栏几何：右段贴右边缘", items2.Count > 0 && geo2.RightItems.Count > 0);
        }
        Console.WriteLine();

        // ── 工具行：统一标题 + 内容另起一条消息 ──
        Section("[工具行]");
        {
            // 图标统一 🔧、名称 PascalCase、加粗染黄、参数灰色括起来
            // 括号与每段参数各自包标记（bash 的参数还要按 shell 语法上色，所以不能再整段一个 «grey»）
            var hdr = ToolRendererFactory.FormatHeader("edit_file", "a.cs");
            Check("工具标题：统一图标与格式",
                hdr.StartsWith("💡 «bold»«orange»Edit«/»«/»") && hdr.Contains("(«/»") && hdr.EndsWith("«grey»)«/»"));
            Check("工具标题：read_file → Read（去 _file 后缀）",
                ToolRendererFactory.DisplayName("read_file") == "Read");
            Check("工具标题：write_file → Write",
                ToolRendererFactory.DisplayName("write_file") == "Write");
            Check("工具标题：multi_edit → MultiEdit（snake→Pascal）",
                ToolRendererFactory.DisplayName("multi_edit") == "MultiEdit");
            // «orange» 必须真的解出橙色：色名写错（或某端色表没登记）会整段**无色**而不是报错，
            // Web 端此前就只登记了 orange3 没有 orange —— 这类「静默失色」只能靠解出来的色值发现
            var ttSegs = UI.Tui.TuiMarkdown.RenderMessage(
                ToolRendererFactory.FormatHeader("edit_file", "a.cs"), "tool", 40, plainText: true);
            var ttFlat = ttSegs.SelectMany(x => x).ToList();
            Check("工具标题：名称渲染成橙色（剥掉粗体位后）",
                ttFlat.Any(x => (x.Fg & ~AnsiTty.BoldFlag) == AnsiColors.Orange));
            Check("工具标题：参数渲染成灰色", ttFlat.Any(x => x.Fg == AnsiColors.BrightBlack));

            // 参数超宽 → 折行而非截断；宽字符不从中间切断；每行不超宽
            var longBrief = "src/很长的中文目录名/AnotherLongPath/文件.cs --option value";
            var wrapped = ToolRendererFactory.FormatHeader("read_file", longBrief, 40);
            Check("工具标题：超宽参数折行（有换行、无省略号）",
                wrapped.Contains('\n') && !wrapped.Contains('…'));
            var wSegs = UI.Tui.TuiMarkdown.RenderMessage(wrapped, "tool", 40, plainText: true);
            var wTxt = string.Concat(wSegs.SelectMany(x => x).Select(x => x.Text));
            // 折行会插入换行与缩进空格，比较前都去掉空白
            static string Squash(string v) => new(v.Where(c => !char.IsWhiteSpace(c)).ToArray());
            Check("工具标题：折行后参数完整（一个字符都不丢）", Squash(wTxt).Contains(Squash(longBrief)));
            Check("工具标题：折行后每行不超宽",
                wSegs.All(r => r.Sum(x => AnsiHelper.DisplayWidth(x.Text)) <= 40));
            Check("工具标题：宽字符不从中间切断", !wTxt.Contains('\uFFFD'));

            // bash 的参数就是一条 shell 命令行 → 按 Shell 语法上色；其他工具的 brief 是路径/描述，保持统一灰
            Check("工具标题：bash 参数按 shell 语法上色",
                ToolRendererFactory.FormatHeader("bash", "dotnet build -c Release", 200).Contains("fg:#"));
            Check("工具标题：非 bash 参数仍是灰色",
                !ToolRendererFactory.FormatHeader("read_file", "src/Foo.cs", 200).Contains("fg:#"));

            // 括号里只留参数值，不带 `key=`（参数名对用户没信息量，还挤占本就不宽的一行）
            var td3 = new Dictionary<string, object?> { ["pattern"] = "TODO", ["glob"] = "*.cs" };
            var brief3 = ToolDisplay.Brief(td3);
            Check("工具摘要：多参数不带参数名",
                !brief3.Contains("pattern=") && !brief3.Contains("glob=") && brief3.Contains("TODO"));
            var tdPath = new Dictionary<string, object?> { ["file_path"] = "/a/b/c/main.cs" };
            Check("工具摘要：路径类参数只给缩写路径",
                ToolDisplay.Brief(tdPath) == "main.cs");

            // 括号内容默认不截断：工具行按控件宽折行、允许多行（截断会把 bash 命令切得没法看）
            var longVal = ToolDisplay.Brief(new Dictionary<string, object?> { ["pattern"] = new string('x', 300) });
            Check("工具摘要：默认不截断（长度不受限）", longVal.Length == 300);

            // 粗体不丢：段模型 (Text,Fg,Bg) 没有样式通道，«bold» 被编进颜色高位
            // （此前直接置 color=1，会被内层的 «orange» 覆盖 ⇒ 用户看到「加粗没起作用」）
            Check("工具标题：名称段带粗体位",
                ttFlat.Any(x => (x.Fg & AnsiTty.BoldFlag) != 0));
            Check("工具标题：粗体渲染成 SGR 1",
                AnsiTty.FgCode(208 | AnsiTty.BoldFlag).StartsWith("[1m"));

            // SGR 1（粗体）是**粘性**的：粗体段结束必须显式 SGR 22，否则会染到后面的参数与下一行
            // （用户实测「工具参数字体好像也被加粗了」）。代码块等非粗体段同样受益。
            var mdCtl = new TuiMarkdown("«bold»粗体«/»正常文字", "assistant") { Width = 60, Height = 3 };
            var mdSb = new StringBuilder();
            mdCtl.Render(mdSb, 0, 0);
            Check("渲染：粗体段结束后发 SGR 22（不染后段）", mdSb.ToString().Contains("\x1b[22m"));

            Check("工具标题：无参数时不带空括号",
                ToolRendererFactory.FormatHeader("bash", "") == "💡 «bold»«orange»Bash«/»«/»");

            // 输出另起一条消息（不与标题同行）—— 接在后面时首行会紧贴标题
            Section("[工具行 · 输出另起]");
            var savedSzT = Tty.SizeOverride;
            Tty.SizeOverride = (100, 30);
            var mgrT = TuiManager.Instance;
            bool enteredT = false;
            int before = 0, after = 0;
            bool bodyOwn = false, titleNotShell = false;
            try
            {
                var prevOut = Console.Out;
                Console.SetOut(TextWriter.Null);
                try
                {
                    if (!mgrT.IsActive) { mgrT.Enter(); enteredT = true; }
                    var chatT = new MarkupChatScreen();
                    mgrT.PushScreen(chatT);
                    chatT.AddToolProgress("edit_file", "a.cs");
                    // 判据用 ChatList 项数：追加到现有项时不变，另起一条才会 +1 —— 后者正是「分开」
                    before = chatT.ChatList.ItemCount;
                    chatT.AppendToLast("+public class A { }");
                    after = chatT.ChatList.ItemCount;
                    var body = chatT.ChatList.GetItem(after - 1) as TuiListItem;
                    var title = chatT.ChatList.GetItem(after - 2) as TuiListItem;
                    bodyOwn = after == before + 1
                           && body != null && body.MarkdownContent.Contains("public class A")
                           && title != null && !title.MarkdownContent.Contains("public class A");
                    // bash 的**标题行**不能走 shellBlock —— 那条渲染路径故意不解码 «»
                    // （bash 输出是 OS 原文，那里的 «» 只是普通字符），而标题是我们生成的
                    // «bold»«yellow»Bash«/» 标记，跟着不解码就会把标记原样打在屏幕上
                    // （实测 `│ 🔧 «bold»«yellow»Bash«/»…`，正是 --keypad 抓到的）。
                    var chatB = new MarkupChatScreen();
                    mgrT.PushScreen(chatB);
                    chatB.AddToolProgress("bash", "ls");
                    titleNotShell = chatB.ChatMessages.Count > 0 && !chatB.ChatMessages[^1].ShellBlock;
                    mgrT.PopScreen();
                }
                finally { Console.SetOut(prevOut); }
            }
            catch (Exception ex) { Check($"工具行测试异常: {ex.Message}", false); }
            finally
            {
                if (enteredT) { try { mgrT.Exit(); } catch { } }
                Tty.SizeOverride = savedSzT;
            }
            Check("工具输出另起一条消息（不与标题同行）", bodyOwn);
            Check("工具标题行不走 shellBlock（bash 的 «» 标记要能解码）", titleNotShell);
        }
        Console.WriteLine();

        // ── 底部状态栏：槽位用方括号框住当前 ──
        Section("[状态栏 · 槽位]");
        {
            var bar = new TuiStatusBar { Width = 80, ActiveSlotIndex = 2 };
            var sbb = new StringBuilder();
            bar.Render(sbb, 0, 0);
            var fbBar = new FrameBuffer(1, 80);
            fbBar.Apply(sbb.ToString());
            var barLine = fbBar.Dump().Count > 0 ? fbBar.Dump()[0] : "";
            // 当前槽位（索引 2 → 第 3 个）用 [3] 框住；白底/颜色在浅色主题与色盲下都不够明确
            Check("状态栏槽位：当前用 [3] 框住", barLine.Contains("[3]"));
            Check("状态栏槽位：其余仍是裸数字（无多余括号）",
                !barLine.Contains("[2]") && !barLine.Contains("[4]") && barLine.Contains("2"));
            // 第 10 槽显示为 0（个位等宽，`…8 9 0`）
            Check("状态栏槽位：第 10 槽显示为 0（等宽）",
                barLine.Contains("9 0") && !barLine.Contains("10"));
            var bar10 = new TuiStatusBar { Width = 80, ActiveSlotIndex = 9 };
            var sb10 = new StringBuilder();
            bar10.Render(sb10, 0, 0);
            var fb10 = new FrameBuffer(1, 80);
            fb10.Apply(sb10.ToString());
            Check("状态栏槽位：第 10 槽激活时框住 [0]",
                fb10.Dump().Count > 0 && fb10.Dump()[0].Contains("[0]"));
        }
        Console.WriteLine();

        // ── 行内选择栏（权限确认/计划审批就地选择，不弹窗）──
        // 用「走真实屏幕路径」的方式测（mgr.PushScreen + screen.OnKey），不直接调 KeyHook ——
        // 直接调体会绕开 OnKey 的优先级编排（行内栏必须优先于建议面板/提示栏），测不出抢键失序。
        Section("[行内选择栏]");
        {
            var savedSzI = Tty.SizeOverride;
            Tty.SizeOverride = (100, 30);
            var mgrI = TuiManager.Instance;
            bool enteredI = false;
            int chatHFull = 0, chatHShrunk = 0;
            int downCode = -1, escCode = -1, yCode = -1;
            bool msgKept = false, arrowShown = false, wired = false;
            var msgText = "行内选择回归标记ABC";
            try
            {
                var prevOut = Console.Out;
                Console.SetOut(TextWriter.Null); // 抑制 Enter/Render 屏幕输出（LastCleanFrame 仍填充）
                try
                {
                    if (!mgrI.IsActive) { mgrI.Enter(); enteredI = true; }
                    var chatI = new MarkupChatScreen();
                    mgrI.PushScreen(chatI);
                    chatI.AddMessage(msgText, "user");
                    mgrI.Render();
                    chatHFull = chatI.ChatList.Height;

                    List<PromptItem> Choices(params (string Label, int Code)[] spec)
                        => spec.Select(s => new PromptItem
                        {
                            Kind = EPromptKind.Choice /* 无图标，序号在 Label 里 */,
                            Label = s.Label,
                            ResultCode = s.Code,
                        }).ToList();

                    // ① 3 项（非危险）→ 占行 = 3 + 上下边框(2) = 5，聊天区相应减 5
                    int? result = null;
                    chatI.ShowInlineChoice(
                        Choices(("1. 允许", 0), ("2. 全部允许", 1), ("3. 拒绝", 2)), c => result = c);
                    mgrI.Render();
                    chatHShrunk = chatI.ChatList.Height;

                    // ② ↓ 移动一次 + Enter → 第 2 项的结果码 1
                    chatI.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
                    // 抓一帧**显示态**（Enter 之前）：选中那行有 ❯ 箭头、未选中那行没有 ——
                    // 端到端验渲染，而不只是验属性（属性对了但渲染漏写箭头的事发生过）。
                    mgrI.Render();
                    var fbSel = new FrameBuffer(Tty.Rows, Tty.Cols);
                    fbSel.Apply(mgrI.LastCleanFrame);
                    var linesSel = fbSel.Dump();
                    arrowShown = linesSel.Any(l => l.Contains('❯') && l.Contains("2. 全部允许"))
                              && linesSel.Any(l => l.Contains("1. 允许") && !l.Contains('❯'));
                    // 标记版接线：❯ 箭头 + 黄底黑字（chat.tui 只声明结构，这三个呈现属性在 code-behind 设）
                    wired = chatI.InlineChoice.ShowArrow
                         && chatI.InlineChoice.HighlightBg == AnsiColors.BgYellow
                         && chatI.InlineChoice.HighlightFg == AnsiColors.Black;

                    chatI.OnKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
                    downCode = result ?? -1;

                    // ③ Esc = 拒绝（2）
                    result = null;
                    chatI.ShowInlineChoice(Choices(("1. 允许", 0), ("2. 拒绝", 2)), c => result = c);
                    mgrI.Render();
                    chatI.OnKey(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));
                    escCode = result ?? -1;

                    // ④ Y 单键 = 允许（0）
                    result = null;
                    chatI.ShowInlineChoice(Choices(("1. 允许", 0), ("2. 拒绝", 2)), c => result = c);
                    mgrI.Render();
                    chatI.OnKey(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false));
                    yCode = result ?? -1;

                    mgrI.Render();
                    var fbI = new FrameBuffer(Tty.Rows, Tty.Cols);
                    fbI.Apply(mgrI.LastCleanFrame);
                    msgKept = fbI.Dump().Any(l => l.Contains(msgText));
                    mgrI.PopScreen();
                }
                finally { Console.SetOut(prevOut); }
            }
            catch (Exception ex)
            {
                Check($"行内选择栏渲染异常: {ex.Message}", false);
            }
            finally
            {
                if (enteredI) { try { mgrI.Exit(); } catch { } }
                Tty.SizeOverride = savedSzI;
            }
            // Check 一律放在 SetOut 捕获区之外：捕获区内失败行会被吞进 TextWriter.Null
            Check("行内选择栏：显示后聊天区高度减 5（3 项+上下边框）", chatHFull - chatHShrunk == 5);
            Check("行内选择栏：↓+Enter 返回第 2 项结果码 1", downCode == 1);
            Check("行内选择栏：Esc 返回拒绝码 2", escCode == 2);
            Check("行内选择栏：Y 单键返回允许码 0", yCode == 0);
            Check("行内选择栏：收起后聊天消息仍在（MarkTreeDirty 账）", msgKept);
            Check("行内选择栏：选中行 ❯ 箭头、未选中行无箭头", arrowShown);
            Check("行内选择栏：标记版接线（❯ + 黄底黑字）", wired);
        }
        Console.WriteLine();

        // ── 行内问卷：多选一 / 多选多 / 横向标签页 / 分步骤 ──
        Section("[行内问卷]");
        {
            var savedSzQ = Tty.SizeOverride;
            Tty.SizeOverride = (100, 30);
            var mgrQ = TuiManager.Instance;
            bool enteredQ = false;
            int hFull = 0, hPage1 = 0, hMulti = 0;
            bool tabsShown = false, stepShown = false, page2Shown = false, page1BackShown = false;
            bool checksShown = false, checkedShown = false;
            bool headBordered = false, borderIntact = false;
            SurveyResult? multi = null, cancelled = null, paged = null, otherResult = null, skipResult = null;
            bool inOther = false;
            try
            {
                var prevOut = Console.Out;
                Console.SetOut(TextWriter.Null);
                try
                {
                    if (!mgrQ.IsActive) { mgrQ.Enter(); enteredQ = true; }
                    var chatQ = new MarkupChatScreen();
                    mgrQ.PushScreen(chatQ);
                    mgrQ.Render();
                    hFull = chatQ.ChatList.Height;

                    var twoQ = new List<SurveyQuestion>
                    {
                        // 高度账断言用的问卷：显式关掉「其他/跳过」两个附加项，行数只跟选项数走
                        new("权限", "允许哪些操作？",
                            [new SurveyOption("读取", "只读文件"), new SurveyOption("写入", "修改文件")], false,
                            AllowOther: false, AllowSkip: false),
                        new("范围", "作用范围？",
                            [new SurveyOption("本次"), new SurveyOption("本会话"), new SurveyOption("总是")], true,
                            AllowOther: false, AllowSkip: false),
                    };
                    // 单题问卷（默认带附加项）→ 选项 = 读取 / 写入 / 其他 / 跳过
                    var otherQ = new List<SurveyQuestion>
                    {
                        new("权限", "允许哪些操作？",
                            [new SurveyOption("读取", "只读文件"), new SurveyOption("写入", "修改文件")], false),
                    };

                    // ① 横向标签页：页头 1 行 + 第 1 页 2 选项 + 上下边框 2 = 5 行
                    multi = null;
                    chatQ.ShowInlineSurvey(twoQ, r => multi = r, showTabs: true);
                    mgrQ.Render();
                    hPage1 = chatQ.ChatList.Height;
                    tabsShown = FrameLines(mgrQ).Any(l => l.Contains("▶ 权限") && l.Contains("范围"));

                    // 第 1 页（单选）↓ 选「写入」→ Enter 进第 2 页（多选，3 项 → 高度 1+3+2=6）
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
                    mgrQ.Render();
                    hMulti = chatQ.ChatList.Height;
                    var linesMulti = FrameLines(mgrQ);
                    checksShown = linesMulti.Any(l => l.Contains("[ ]")) && !linesMulti.Any(l => l.Contains("[x]"));

                    // 第 2 页（多选）勾「本次」(idx 0) 与「总是」(idx 2)：Space / ↓↓ Space
                    chatQ.OnKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false));
                    mgrQ.Render();
                    checkedShown = FrameLines(mgrQ).Count(l => l.Contains("[x]")) == 2;
                    chatQ.OnKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)); // 最后一页 → 提交

                    // ② 分步骤（无标签行）：页头显示「步骤 1/2」
                    cancelled = null;
                    chatQ.ShowInlineSurvey(twoQ, r => cancelled = r, showTabs: false);
                    mgrQ.Render();
                    stepShown = FrameLines(mgrQ).Any(l => l.Contains("步骤 1/2"));
                    // 页头行必须是完整的「框内行」：左右边框都在（曾漏渲染边框 + 整行填充，
                    // 表现为动态栏残留粘在页头行上 —— --keypad 帧抓到）
                    var headLine = FrameLines(mgrQ).FirstOrDefault(l => l.Contains("步骤 1/2"));
                    headBordered = headLine != null
                        && headLine.TrimStart().StartsWith('│') && headLine.TrimEnd().EndsWith('│');
                    // 上边框中间必须清一色 ─：换页改变高度 → 下方兄弟位移，若没请求全屏重绘，
                    // 上一帧内容会把边框啃出豁口（`╭─── ─ ───── ─ ───╮`）
                    var topBorder = FrameLines(mgrQ).FirstOrDefault(l => l.Contains('╭'));
                    borderIntact = topBorder != null
                        && topBorder.Trim().StartsWith('╭') && topBorder.Trim().EndsWith('╮')
                        && topBorder.Count(c => c == '─') == topBorder.Trim().Length - 2;
                    chatQ.OnKey(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false)); // 取消

                    // ③ ←→ 翻页
                    paged = null;
                    chatQ.ShowInlineSurvey(twoQ, r => paged = r, showTabs: true);
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
                    mgrQ.Render();
                    page2Shown = FrameLines(mgrQ).Any(l => l.Contains("▶ 范围"));
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
                    mgrQ.Render();
                    page1BackShown = FrameLines(mgrQ).Any(l => l.Contains("▶ 权限"));
                    chatQ.OnKey(new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));

                    // ④「其他」自定义输入：↓↓ 到「其他」→ Enter 进输入态 → 打字 → Enter 提交（单题即完成）
                    otherResult = null;
                    chatQ.ShowInlineSurvey(otherQ, r => otherResult = r, showTabs: false);
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
                    inOther = chatQ.InlineSurveyVisible && !chatQ.InlineChoiceVisible; // 选项栏让位给输入框
                    foreach (var ch in "自定义XYZ")
                        chatQ.OnKey(new ConsoleKeyInfo(ch, ConsoleKey.A, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false)); // 提交自定义答案 → 完成

                    // ⑤ 跳过此题：↓↓↓ 到末尾的「跳过」项 → Enter（该题结果为空列表）
                    skipResult = null;
                    chatQ.ShowInlineSurvey(otherQ, r => skipResult = r, showTabs: false);
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
                    chatQ.OnKey(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
                    mgrQ.PopScreen();
                }
                finally { Console.SetOut(prevOut); }
            }
            catch (Exception ex)
            {
                Check($"行内问卷渲染异常: {ex.Message}", false);
            }
            finally
            {
                if (enteredQ) { try { mgrQ.Exit(); } catch { } }
                Tty.SizeOverride = savedSzQ;
            }
            Check("问卷：单选页占行 = 页头1+选项2+边框2", hFull - hPage1 == 5);
            Check("问卷：多选页占行 = 页头1+选项3+边框2", hFull - hMulti == 6);
            Check("问卷：横向标签行显示 ▶ 当前页 + 其余标题", tabsShown);
            Check("问卷：多选页渲染 [ ] 勾选框", checksShown);
            Check("问卷：Space 勾选两项后渲染两个 [x]", checkedShown);
            Check("问卷：单选页选第 2 项 → 结果 [1]", multi?.Picks is [[1], [0, 2]]);
            Check("问卷：分步骤页头显示「步骤 1/2」", stepShown);
            Check("问卷：页头行含左右边框（非豁口）", headBordered);
            Check("问卷：换页后上边框完整（无上一帧残留豁口）", borderIntact);
            Check("问卷：Esc 取消 → 结果 null", cancelled == null);
            Check("问卷：→ 翻到第 2 页、← 回到第 1 页", page2Shown && page1BackShown);
            Check("问卷：翻页后取消 → 结果 null", paged == null);
            Check("问卷：「其他」项转入输入态（选项栏让位给输入框）", inOther);
            Check("问卷：自定义答案进结果（索引 -1 + 文本）",
                otherResult?.Picks.Count > 0 && otherResult.Picks[0] is [-1]
                && otherResult.Others[0] == "自定义XYZ");
            Check("问卷：「跳过此题」→ 该题结果为空列表", skipResult?.Picks.Count > 0 && skipResult.Picks[0] is []);
        }
        Console.WriteLine();

        // ── 模型信息行（输入区下方，Render 每帧同步；动态栏不放模型）──
        // 静音窗口内只取数据（渲染帧不污染输出），断言挪到恢复后统一做，避免 Check 输出被抑制
        {
            var savedSz = Tty.SizeOverride;
            var mscr2 = new MarkupChatScreen();
            bool entered2 = false;
            bool rowVisible = false;
            string? rowText = null;
            string? planRowText = null;
            try
            {
                Tty.SizeOverride = (200, 40);
                var prevOut = Console.Out;
                Console.SetOut(TextWriter.Null);
                try
                {
                    if (!mgr.IsActive) { mgr.Enter(); entered2 = true; }
                    mgr.PushScreen(mscr2);
                    mscr2.SyncTheme();
                    mscr2.RefreshTheme();
                    mgr.Render();

                    var row = mscr2.ModelInfoRow;
                    rowVisible = row is { Visible: true };
                    rowText = row?.Text;

                    // 模式切换 → 下一帧刷新内容
                    var savedMode = WorkModeManager.CurrentMode;
                    try
                    {
                        WorkModeManager.SetMode(WorkMode.Plan);
                        mgr.Render();
                        planRowText = mscr2.ModelInfoRow?.Text;
                    }
                    finally { WorkModeManager.SetMode(savedMode); }

                    mgr.PopScreen();
                }
                finally { Console.SetOut(prevOut); }

                Check("模型信息行: 可见", rowVisible);
                Check("模型信息行: 含工作模式", rowText is { } r && r.Contains("模式:"));
                Check("模型信息行: 含经济模式", rowText is { } r1 && r1.Contains("经济:"));
                // 模型栏只显示当前生效模型：前缀为通道（大模型/自由模型/回滚模型/小模型）+ `:`，不再并列大小模型
                Check("模型信息行: 含通道前缀模型", rowText is { } r2 && (r2.Contains("大模型:") || r2.Contains("自由模型:") || r2.Contains("回滚模型:") || r2.Contains("小模型:")));
                Check("模型信息行: 不再并列小模型", rowText is { } r3 && !r3.Contains("小:"));
                Check("模型信息行: 无尖括号", rowText is { } r4 && !r4.Contains('<') && !r4.Contains('>'));
                Check("模型信息行: · 分隔", rowText is { } r5 && r5.Contains(" · "));
                Check("模型信息行: 模式切换刷新", planRowText is { } r6 && r6.Contains("计划"));
            }
            catch (Exception ex)
            {
                Check($"模型信息行测试异常: {ex.Message}", false);
            }
            finally
            {
                Tty.SizeOverride = savedSz;
                if (entered2) { try { mgr.Exit(); } catch { } }
            }
        }
        // ── 窗口型界面 .tui 资源加载（选择器/帮助/设置/Diff 壳）──
        Section("[TuiMarkup 窗口界面]");
        {
            // (文件, 关键 id 数组) —— 覆盖 dialogs/ 全部 19 个，id 从对应代码的 res.Find 调用提取
            var files = new (string File, string[] Ids)[]
            {
                // 选择器/帮助/设置/Diff 壳（Custom/*.cs）
                ("modelpicker.tui", ["search", "table", "slotBar", "help"]),
                ("filepicker.tui", ["path", "search", "table", "help"]),
                ("sessionpicker.tui", ["stats", "search", "list", "openBtn", "renameBtn", "delBtn", "closeBtn", "help"]),
                ("commandpalette.tui", ["search", "list", "help"]),
                ("reasoningpicker.tui", ["search", "list"]),
                ("keybindhelp.tui", ["list", "hint"]),
                ("settings.tui", ["header", "catList", "detailPanel", "hintBar"]),
                ("diffpreview.tui", ["body", "btnAccept", "btnSkip", "btnAll", "btnCancel"]),
                // 对话框工厂（TuiDialog.cs 的 Find ?? throw 引用）
                ("ask.tui", ["msgBox", "list", "ok", "cancel"]),
                ("confirm.tui", ["msgBox", "yes", "no"]),
                ("confirm3.tui", ["msgBox", "yes", "no", "cancel"]),
                ("findreplace.tui", ["find", "repl", "case", "regex", "word", "findNext", "replace", "replaceAll", "close"]),
                ("info.tui", ["msgBox", "ok"]),
                ("input.tui", ["msgBox", "input", "ok", "cancel"]),
                ("inputline.tui", ["msgBox", "input", "ok", "cancel"]),
                ("multiselect.tui", ["list", "ok", "cancel"]),
                ("permission.tui", ["msgBox", "allow", "deny", "always"]),
                ("secret.tui", ["msgBox", "input", "ok", "cancel"]),
                ("select.tui", ["list", "cancel"]),
            };
            foreach (var (file, ids) in files)
            {
                try
                {
                    var res = TuiMarkup.LoadResource($"dialogs/{file}");
                    bool allFound = true;
                    foreach (var id in ids)
                        allFound &= res.Find(id) != null;
                    Check($"{file} Window/Screen 根", res.Window != null || res.Screen != null);
                    Check($"{file} 关键 id={string.Join("/", ids)}", allFound);
                }
                catch (Exception ex)
                {
                    Check($"{file} 加载失败: {ex.Message}", false);
                }
            }
        }
        Console.WriteLine();

        // ── 环境量：InDesign / SimulatedScreen 传播到加载的元素 ──
        Section("[TuiMarkup 环境量]");
        bool prevD = WayCoder.UI.TUI.TuiMarkup.InDesign;
        bool prevS = WayCoder.UI.TUI.TuiMarkup.SimulatedScreen;
        try
        {
            WayCoder.UI.TUI.TuiMarkup.InDesign = true;
            WayCoder.UI.TUI.TuiMarkup.SimulatedScreen = true;
            var dsRes = TuiMarkup.Load("<Dialog title=\"d\"><VBox><Label id=\"l\" text=\"x\"/></VBox></Dialog>");
            var dLabel = dsRes.Find<TuiLabel>("l");
            Check("InDesign=true 注入控件", dLabel != null && dLabel.InDesign && dLabel.SimulatedScreen);
            Check("InDesign=true 注入窗口", dsRes.Window != null && dsRes.Window.InDesign && dsRes.Window.SimulatedScreen);

            // {InDesign '样本'} 设计态数据标记：设计态取引号内内容
            const string DsMarkup = "<Dialog title=\"d\"><VBox>"
                + "<List id=\"dl\" items=\"{InDesign '甲,乙,丙'}\"/>"
                + "<Label id=\"dt\" text=\"前{InDesign '中'}后\"/></VBox></Dialog>";
            var dsData = TuiMarkup.Load(DsMarkup);
            Check("设计态数据标记: 列表出样本", dsData.Find<TuiList>("dl")?.Items is { Count: 3 } dsIt && dsIt[0] == "甲");
            Check("设计态数据标记: 文本内联展开", dsData.Find<TuiLabel>("dt")?.Text == "前中后");
            Check("设计态数据标记: 无引号形式", TuiMarkup.ResolveDesign("{InDesign 样本}") == "样本");
            Check("设计态数据标记: 同前缀占位符不误伤", TuiMarkup.ResolveDesign("{InDesignMode}") == "{InDesignMode}");
            Check("设计态数据标记: 引号未闭合原样保留", TuiMarkup.ResolveDesign("{InDesign '甲}") == "{InDesign '甲}");

            WayCoder.UI.TUI.TuiMarkup.InDesign = false;
            WayCoder.UI.TUI.TuiMarkup.SimulatedScreen = false;
            var rtRes = TuiMarkup.Load("<Dialog title=\"d\"><VBox><Label id=\"l2\" text=\"x\"/></VBox></Dialog>");
            Check("InDesign=false 不注入", rtRes.Find<TuiLabel>("l2") is { InDesign: false, SimulatedScreen: false });

            // 运行态：样本解析为空串 —— 列表不加项、文本只剩固定部分（杜绝样本泄漏进真实 UI）
            var rtData = TuiMarkup.Load(DsMarkup);
            Check("运行态数据标记: 列表无样本", (rtData.Find<TuiList>("dl")?.Items.Count ?? 0) == 0);
            Check("运行态数据标记: 文本剔除样本", rtData.Find<TuiLabel>("dt")?.Text == "前后");
        }
        finally
        {
            WayCoder.UI.TUI.TuiMarkup.InDesign = prevD;
            WayCoder.UI.TUI.TuiMarkup.SimulatedScreen = prevS;
        }
        Console.WriteLine();

        // 检查在恢复输出后执行
        Check("MarkupChatScreen 渲染非空", !string.IsNullOrEmpty(frame));
        Check("MarkupChatScreen 子视图就位", mScreen.TitleBar != null && mScreen.ChatList != null
            && mScreen.InputArea != null && mScreen.StatusBar != null && mScreen.SidePanel != null
            && mScreen.DynamicBar != null && mScreen.PromptBar != null);
        Check("MarkupChatScreen 输入区接线", mScreen.InputArea != null && mScreen.InputArea.OnSubmit != null);
        Console.WriteLine();
    }
}

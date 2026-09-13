using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

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

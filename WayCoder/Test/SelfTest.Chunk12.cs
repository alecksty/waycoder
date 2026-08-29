using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder;

public static partial class SelfTest
{
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
            Check("chat.tui RootView 子节点=12", childCount == 12); // 模式栏下新增 shortcutRow（快捷键行）

            Check("chat.tui titleBar", main.Find<TuiTitleBar>("titleBar") != null);
            Check("chat.tui chatList", main.Find<TuiListView>("chatList") != null);
            Check("chat.tui sidePanel", main.Find<TuiSidePanel>("sidePanel") != null);
            Check("chat.tui suggestPanel", main.Find<TuiVBox>("suggestPanel") != null);
            Check("chat.tui promptBar", main.Find<TuiPromptBar>("promptBar") != null);
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
        bool entered = false;
        try
        {
            var prevOut = Console.Out;
            Console.SetOut(TextWriter.Null); // 抑制 Enter/Render 的屏幕输出，仅渲染不打印
            try
            {
                if (!mgr.IsActive) { mgr.Enter(); entered = true; }
                mgr.PushScreen(mScreen);
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
                Check("模型信息行: 含大模型", rowText is { } r2 && r2.Contains("大:"));
                Check("模型信息行: 含小模型", rowText is { } r3 && r3.Contains("小:"));
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

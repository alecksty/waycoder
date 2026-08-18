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

        // ── tuidemo/chat.tui 完整聊天布局加载 ──
        Section("[TuiMarkup chat.tui]");
        try
        {
            var main = TuiMarkup.LoadFile(TuiMarkupPaths.ResolveDemoFile("chat.tui"));
            Check("chat.tui Screen 根", main.Screen != null);
            Check("chat.tui RootView 非空", main.Screen?.RootView != null);
            int childCount = main.Screen?.RootView?.Children.Count ?? 0;
            Check("chat.tui RootView 子节点=9", childCount == 9);

            Check("chat.tui titleBar", main.Find<TuiTitleBar>("titleBar") != null);
            Check("chat.tui chatList", main.Find<TuiListView>("chatList") != null);
            Check("chat.tui sidePanel", main.Find<TuiSidePanel>("sidePanel") != null);
            Check("chat.tui suggestPanel", main.Find<TuiVBox>("suggestPanel") != null);
            Check("chat.tui promptBar", main.Find<TuiPromptBar>("promptBar") != null);
            Check("chat.tui dynamicBar", main.Find<TuiDynamicBar>("dynamicBar") != null);
            Check("chat.tui inputArea", main.Find<TuiTextArea>("inputArea") != null);
            Check("chat.tui statusBar", main.Find<TuiStatusBar>("statusBar") != null);
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
        // ── 窗口型界面 .tui 资源加载（选择器/帮助/设置/Diff 壳）──
        Section("[TuiMarkup 窗口界面]");
        {
            // (文件, 关键 id 数组)
            var files = new (string File, string[] Ids)[]
            {
                ("modelpicker.tui", ["search", "table", "slotBar", "help"]),
                ("filepicker.tui", ["path", "search", "table", "help"]),
                ("sessionpicker.tui", ["stats", "search", "list", "openBtn", "renameBtn", "delBtn", "closeBtn", "help"]),
                ("commandpalette.tui", ["search", "list", "help"]),
                ("reasoningpicker.tui", ["search", "list"]),
                ("keybindhelp.tui", ["list", "hint"]),
                ("settings.tui", ["header", "catList", "detailPanel", "hintBar"]),
                ("diffpreview.tui", ["body", "status"]),
            };
            foreach (var (file, ids) in files)
            {
                try
                {
                    var res = TuiMarkup.LoadFile(TuiMarkupPaths.ResolveDemoFile(Path.Combine("dialogs", file)));
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

        // 检查在恢复输出后执行
        Check("MarkupChatScreen 渲染非空", !string.IsNullOrEmpty(frame));
        Check("MarkupChatScreen 子视图就位", mScreen.TitleBar != null && mScreen.ChatList != null
            && mScreen.InputArea != null && mScreen.StatusBar != null && mScreen.SidePanel != null
            && mScreen.DynamicBar != null && mScreen.PromptBar != null);
        Check("MarkupChatScreen 输入区接线", mScreen.InputArea != null && mScreen.InputArea.OnSubmit != null);
        Console.WriteLine();
    }
}

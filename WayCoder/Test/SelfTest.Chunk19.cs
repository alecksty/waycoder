using System.Collections.Concurrent;
using WayCoder.Infra;
using WayCoder.Tools;
using WayCoder.UI.Cli.Arguments;
using WayCoder.UI.Cli.Commands;
using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.TUI.Custom;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// WindowsCharSource 状态化 UTF-8 解码测试（code-review finding #3 回归护栏）：
    /// ①emoji（4 字节 UTF-8 → 代理对）应完整返回高位+低位；
    /// ②前导字节与续字节跨两次读到达（模拟 64B 读边界拆包）不得丢字节或出 U+FFFD；
    /// ③ASCII 单字节直通。
    /// 用可注入 Stream 构造 + 阻塞喂入流，避免依赖真实控制台。
    /// </summary>
    private static void TestChunk19(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[斜杠命令匹配(core SlashMatcher)]");
        var helpCmd = new WayCoder.UI.Cli.Commands.HelpCommand(); // 主名 /help、别名 /h
        Check("/help 精确 → 空参", SlashMatcher.ExtractArgs(helpCmd, "/help") == "");
        Check("主名大小写不敏感", SlashMatcher.ExtractArgs(helpCmd, "/HELP") == "");
        Check("主名带参 → 参数 Trim", SlashMatcher.ExtractArgs(helpCmd, "/help  me ") == "me");
        Check("别名 /h 精确匹配", SlashMatcher.ExtractArgs(helpCmd, "/h") == "");
        Check("别名带参匹配", SlashMatcher.ExtractArgs(helpCmd, "/H  x y") == "x y");
        Check("不匹配 → null", SlashMatcher.ExtractArgs(helpCmd, "/he") == null
            && SlashMatcher.ExtractArgs(helpCmd, "/helpme") == null
            && SlashMatcher.ExtractArgs(helpCmd, "x /help") == null);

        Section("[token 估算(core ContextManager.EstimateText)]");
        Check("空串/空引用 → 0", ContextManager.EstimateText("") == 0 && ContextManager.EstimateText(null!) == 0);
        Check("纯 ASCII 4 字符 → 1 (0.25/字)", ContextManager.EstimateText("abcd") == 1);
        Check("纯 ASCII 1000 字符 → 250", ContextManager.EstimateText(new string('a', 1000)) == 250);
        Check("CJK 2 字 → 3 (1.5/字)", ContextManager.EstimateText("中文") == 3);
        Check("CJK 4 字 → 6", ContextManager.EstimateText("中文中文") == 6);
        Check("混合 中a → 1 (截断)", ContextManager.EstimateText("中a") == 1);
        Check("emoji 按宽字符计(代理对安全) → 1", ContextManager.EstimateText("😀") == 1);
        Check("单调不减", ContextManager.EstimateText("这是一个较长的句子用于估算") > ContextManager.EstimateText("短"));

        Section("[/topage 路由映射(core PageRouteMap)]");
        Check("home/chat/files/settings → //xxx", PageRouteMap.Resolve("home") == "//home"
            && PageRouteMap.Resolve("CHAT") == "//chat" && PageRouteMap.Resolve(" files ") == "//files"
            && PageRouteMap.Resolve("settings") == "//settings" && PageRouteMap.Resolve("config") == "//settings");
        Check("独立页别名 → 路由", PageRouteMap.Resolve("history") == "sessions"
            && PageRouteMap.Resolve("menu") == "panel" && PageRouteMap.Resolve("model") == "modelpicker"
            && PageRouteMap.Resolve("供应商") == "models" && PageRouteMap.Resolve("sync") == "gitsync");
        Check("/home 带斜杠也识别", PageRouteMap.Resolve("/about") == "about");
        Check("未知页面 → null(回显用法)", PageRouteMap.Resolve("nope") == null && PageRouteMap.Resolve("") == null);

        Section("[跨端 UI 文本(core UiText)]");
        Check("PermName Yolo/SmartAuto/Auto/Ask", UiText.PermName(PermissionManager.Mode.Yolo) == "Yolo"
            && UiText.PermName(PermissionManager.Mode.SmartAuto) == "SmartAuto"
            && UiText.PermName(PermissionManager.Mode.Auto) == "Auto"
            && UiText.PermName(PermissionManager.Mode.Ask) == "Ask");
        Check("EconomyName 关/自动/开/极致", UiText.EconomyName(EconomyMode.Off) == "关"
            && UiText.EconomyName(EconomyMode.Auto) == "自动"
            && UiText.EconomyName(EconomyMode.On) == "开"
            && UiText.EconomyName(EconomyMode.Extreme) == "极致");
        Check("FormatK 999/1000/1500", UiText.FormatK(999) == "999" && UiText.FormatK(1000) == "1.0k" && UiText.FormatK(1500) == "1.5k");
        Check("IsSessionBodyRole user/assistant 真、tool/system 假",
            UiText.IsSessionBodyRole("user") && UiText.IsSessionBodyRole("assistant")
            && !UiText.IsSessionBodyRole("tool") && !UiText.IsSessionBodyRole("system") && !UiText.IsSessionBodyRole(""));

        // 相对时间阶梯：三处（TUI 侧边栏 / 移动端会话列表 / 检查点列表）此前各写一遍，
        // 移动端那份漏了「N 周前」⇒ 10 天前在手机上显示「10 天前」、桌面显示「1 周前」。
        var t0 = new DateTime(2026, 1, 1, 12, 0, 0);
        Check("RelativeTime: <60s → 刚刚", UiText.RelativeTime(t0.AddSeconds(30), t0) == "刚刚");
        Check("RelativeTime: 5 分钟前", UiText.RelativeTime(t0.AddMinutes(5), t0) == "5 分钟前");
        Check("RelativeTime: 3 小时前", UiText.RelativeTime(t0.AddHours(3), t0) == "3 小时前");
        Check("RelativeTime: 2 天前", UiText.RelativeTime(t0.AddDays(2), t0) == "2 天前");
        Check("RelativeTime: 10 天 → 1 周前（移动端此前显示「10 天前」）",
            UiText.RelativeTime(t0.AddDays(10), t0) == "1 周前");
        Check("RelativeTime: >30 天 → 日期兜底",
            UiText.RelativeTime(t0.AddDays(40), t0) == t0.ToString("MM-dd HH:mm"));
        Check("RelativeTime: withSeconds 保留秒档（检查点列表）",
            UiText.RelativeTime(t0.AddSeconds(30), t0, withSeconds: true) == "30 秒前");

        Section("[输入 CSI 功能键映射]");
        // code-review finding #1 回归护栏：Windows VT 输入下裸 CSI 光标键/编辑键必须映射为对应
        // ConsoleKey，绝不可退化成裸 ESC（否则取消在跑 agent / 丢聊天草稿）。
        ConsoleKey ParseKey(string param, char term) => InputManager.ParseCsiFuncKey(param, term)?.KeyInfo.Key ?? ConsoleKey.NoName;
        Check("ESC[A → Up(非 ESC)", ParseKey("A", 'A') == ConsoleKey.UpArrow);
        Check("ESC[B → Down", ParseKey("B", 'B') == ConsoleKey.DownArrow);
        Check("ESC[D → Left", ParseKey("D", 'D') == ConsoleKey.LeftArrow);
        Check("ESC[H → Home", ParseKey("H", 'H') == ConsoleKey.Home);
        Check("ESC[F → End", ParseKey("F", 'F') == ConsoleKey.End);
        Check("1~ → Home", ParseKey("1~", '~') == ConsoleKey.Home);
        Check("3~ → Delete", ParseKey("3~", '~') == ConsoleKey.Delete);
        Check("5~ → PgUp", ParseKey("5~", '~') == ConsoleKey.PageUp);
        Check("6~ → PgDn", ParseKey("6~", '~') == ConsoleKey.PageDown);
        Check("15~ → F5", ParseKey("15~", '~') == ConsoleKey.F5);
        var ctrlEv = InputManager.ParseCsiFuncKey("1;5D", 'D');
        Check("1;5D → Ctrl+Left", ctrlEv?.KeyInfo is { } kk && kk.Key == ConsoleKey.LeftArrow && kk.Modifiers.HasFlag(ConsoleModifiers.Control));
        // SS3 形态（ESC O x，序列里没有 '['）：xterm/Windows Terminal 的 F1-F4 走这条。
        // 漏处理会让 F1-F4 落进「Alt+字符」分支 → 槽位切换键整排失效。
        Check("SS3 P/Q/R/S → F1-F4（槽位切换键）",
            InputManager.MapSs3Key('P') == ConsoleKey.F1 && InputManager.MapSs3Key('Q') == ConsoleKey.F2
            && InputManager.MapSs3Key('R') == ConsoleKey.F3 && InputManager.MapSs3Key('S') == ConsoleKey.F4);
        Check("SS3 A/B/C/D/H/F → 方向键与 Home/End（应用光标键模式）",
            InputManager.MapSs3Key('A') == ConsoleKey.UpArrow && InputManager.MapSs3Key('B') == ConsoleKey.DownArrow
            && InputManager.MapSs3Key('C') == ConsoleKey.RightArrow && InputManager.MapSs3Key('D') == ConsoleKey.LeftArrow
            && InputManager.MapSs3Key('H') == ConsoleKey.Home && InputManager.MapSs3Key('F') == ConsoleKey.End);
        Check("SS3 未识别字节返回 null（不吞按键）", InputManager.MapSs3Key('Z') == null);

        Section("[char→ConsoleKey 统一映射]");
        Check("MapToConsoleKey a→A", WindowsCharSource.MapToConsoleKey('a') == ConsoleKey.A);
        Check("MapToConsoleKey 9→D9", WindowsCharSource.MapToConsoleKey('9') == ConsoleKey.D9);
        Check("MapToConsoleKey 空格→Spacebar", WindowsCharSource.MapToConsoleKey(' ') == ConsoleKey.Spacebar);
        Check("MapToConsoleKey ESC→Escape", WindowsCharSource.MapToConsoleKey('\x1b') == ConsoleKey.Escape);
        Check("MapToConsoleKey CJK→NoName", WindowsCharSource.MapToConsoleKey('中') == ConsoleKey.NoName);
        Check("MapToConsoleKey DEL(0x7F)→Backspace",
            WindowsCharSource.MapToConsoleKey('\x7f') == ConsoleKey.Backspace);

        // ── 字节流按键还原：v0.96.74 把 Windows 读键改成 VT 字节流后，字节层丢掉了修饰键信息，
        //    且 0x7F/控制符两条映射一起漏掉 → 退格擦不掉、Ctrl 组合键全静默失效。
        //    下面直接喂字节走真实 WindowsCharSource → TryReadKey，等于端到端复现真机路径。──
        Section("[字节流按键还原（Backspace / Ctrl）]");
        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            feed.Feed([0x7F]); // 真机 Backspace 在 VT 输入下发的就是 DEL
            var bs = ReadKey(src, feed);
            Check("字节 0x7F → ConsoleKey.Backspace", bs?.Key == ConsoleKey.Backspace);
            Check("字节 0x7F → KeyChar 归一为 \\b", bs?.KeyChar == '\b');
        }
        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            feed.Feed([0x08]); // BS 变体：少数终端/粘贴路径发这个，须同样识别
            var bs = ReadKey(src, feed);
            Check("字节 0x08 → ConsoleKey.Backspace", bs?.Key == ConsoleKey.Backspace);
        }
        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            feed.Feed([0x01, 0x10, 0x15, 0x1A]); // Ctrl+A / Ctrl+P / Ctrl+U / Ctrl+Z
            var ca = ReadKey(src, feed);
            var cp = ReadKey(src, feed);
            var cu = ReadKey(src, feed);
            var cz = ReadKey(src, feed);
            Check("字节 0x01 → Ctrl+A",
                ca?.Key == ConsoleKey.A && ca.Value.Modifiers.HasFlag(ConsoleModifiers.Control));
            Check("字节 0x10 → Ctrl+P（权限循环键）",
                cp?.Key == ConsoleKey.P && cp.Value.Modifiers.HasFlag(ConsoleModifiers.Control));
            // 命令面板键改纯 Ctrl+U 后必须走真机字节路径验证：Ctrl+Shift+P 那种组合在
            // Windows 上根本到不了程序（见 ChatScreen.HandleGlobalShortcut 的说明）
            Check("字节 0x15 → Ctrl+U（命令面板键）",
                cu?.Key == ConsoleKey.U && cu.Value.Modifiers.HasFlag(ConsoleModifiers.Control));
            Check("字节 0x1A → Ctrl+Z（优雅暂停键）",
                cz?.Key == ConsoleKey.Z && cz.Value.Modifiers.HasFlag(ConsoleModifiers.Control));
        }
        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            // 歧义码位不得被 Ctrl 还原抢走既有语义
            feed.Feed([0x09, 0x0D, 0x1B]);
            var tab = ReadKey(src, feed);
            var enter = ReadKey(src, feed);
            var esc = ReadKey(src, feed);
            Check("Tab/Enter/ESC 语义不被 Ctrl 还原破坏",
                tab?.Key == ConsoleKey.Tab && enter?.Key == ConsoleKey.Enter && esc?.Key == ConsoleKey.Escape);
        }
        // Alt+字符的合成发生在 InputManager 的**转义解析**（泵线程），
        // 不在字节源那一层（字节源只做「字节 → ConsoleKeyInfo」）。所以必须驱动带转义解析的
        // 真实读键路径（SetSourceForTest + ReadInput），否则测的是下面那一层，永远绿。
        // 语义：xterm 系终端对 Alt+字母/数字/符号发 ESC + 字符（altSendsEscape）。
        // 注意 ReadInput 首帧会先返回 Resize（尺寸未初始化），必须跳过非按键事件再断言。
        static WayCoder.UI.TUI.Base.InputEvent? NextKey(WayCoder.UI.TUI.Base.InputManager m)
        {
            for (int i = 0; i < 30; i++)
            {
                var e = m.ReadInput(200);
                if (e.Type == WayCoder.UI.TUI.Base.InputType.Key) return e;
            }
            return null;
        }
        {
            using var feed = new TestFeedStream();
            using var im = new WayCoder.UI.TUI.Base.InputManager();
            im.SetSourceForTest(new WindowsCharSource(feed), hasKeySource: true);
            feed.Feed([0x1B, (byte)'t', 0x1B, (byte)'1', 0x1B, (byte)'/']);
            var altT = NextKey(im);
            var altOne = NextKey(im);
            var altSlash = NextKey(im);
            Check("ESC+t → Alt+T（Alt 组合可用；此前拆成 ESC + 裸 t = 误触中断 Agent）",
                altT != null && altT.KeyInfo.Key == ConsoleKey.T
                && altT.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt));
            Check("ESC+t → 不是 Escape（否则按 Alt+T 会中断 Agent）",
                altT != null && altT.KeyInfo.Key != ConsoleKey.Escape);
            Check("ESC+1 → Alt+1（数字可用）",
                altOne != null && altOne.KeyInfo.Key == ConsoleKey.D1
                && altOne.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt));
            Check("ESC+/ → Alt+/（符号可用：KeyChar 保留）",
                altSlash != null && altSlash.KeyInfo.KeyChar == '/'
                && altSlash.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt));
        }
        {
            // 反向：孤立 ESC（20ms 窗口内没有字符）必须仍是 ESC —— 它是「中断 Agent」的键，
            // 一律当 Alt 会让这个最常用的取消键失灵。
            using var feed = new TestFeedStream();
            using var im = new WayCoder.UI.TUI.Base.InputManager();
            im.SetSourceForTest(new WindowsCharSource(feed), hasKeySource: true);
            feed.Feed([0x1B]);
            var lone = NextKey(im);
            Check("孤立 ESC → Escape（无后续字符，未被 Alt 化）",
                lone != null && lone.KeyInfo.Key == ConsoleKey.Escape
                && !lone.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Alt));
        }
        // InputManager 的字符转换须与字节流共用同一实现（修一处全端生效）
        Check("ToConsoleKeyInfo 与 MapToConsoleKey 同源（0x7F）",
            WindowsCharSource.ToConsoleKeyInfo('\x7f').Key == ConsoleKey.Backspace
            && WindowsCharSource.ToConsoleKeyInfo('\x10').Modifiers.HasFlag(ConsoleModifiers.Control));

        Section("[字符源 UTF-8 状态化]");
        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            // ① 一次喂入完整 emoji（😀 = F0 9F 98 80）+ ASCII 'a'
            feed.Feed([0xF0, 0x9F, 0x98, 0x80, (byte)'a']);
            var c1 = ReadChar(src, feed);
            Check("emoji 高位返回(非 U+FFFD/非丢失)", c1.HasValue && char.IsHighSurrogate(c1.Value));
            var c2 = ReadChar(src, feed);
            Check("emoji 低位接着返回", c2.HasValue && char.IsLowSurrogate(c2.Value));
            var c3 = ReadChar(src, feed);
            Check("紧随 ASCII 'a' 不丢", c3 == 'a');
        }

        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            // ② 跨读边界：先只喂前导+首续字节，解码应暂缓（返回 false / 无字符，且不丢首字节不出 U+FFFD）
            feed.Feed([0xF0, 0x9F]);
            Thread.Sleep(10);
            var early = ReadChar(src, feed, allowEmpty: true);
            Check("半字符暂缓(不产出 U+FFFD)", early == null || early != '�');
            // 再喂余下续字节
            feed.Feed([0x98, 0x80]);
            var h = ReadChar(src, feed);
            var l = ReadChar(src, feed);
            Check("跨边界拆包后仍合成完整代理对", h.HasValue && char.IsHighSurrogate(h.Value)
                && l.HasValue && char.IsLowSurrogate(l.Value));
        }

        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            // ③ ASCII 直通 + RS(0x1E) 分隔符被跳过
            feed.Feed([0x1E, (byte)'x']);
            var cx = ReadChar(src, feed);
            Check("RS 分隔符跳过、ASCII 直通", cx == 'x');
        }

        Section("[TuiDynamicBar 按值标脏（与聊天区解耦护栏）]");
        // 契约：动态栏内容属性只在「值真变化」时标脏。spinner 动画由 RenderDirect 直写转动、
        // 不依赖脏标记，所以绝不能靠「定时无条件标脏」来刷 —— 那会按动画节拍把整个屏幕
        // （含聊天区）反复拖进渲染路径，表现为内容没变却一直闪。
        {
            var db = new WayCoder.UI.Tui.Controls.TuiDynamicBar();
            db.ClearDirty();
            db.Status = db.Status;
            db.LeftText = db.LeftText;
            db.ToolText = db.ToolText;
            db.TokenDisplay = db.TokenDisplay;
            db.CostDisplay = db.CostDisplay;
            db.ContextPercent = db.ContextPercent;
            db.CpuPercent = db.CpuPercent;
            db.ProgressPercent = db.ProgressPercent;
            Check("同值赋值不标脏（内容没变就不重绘）", !db.IsDirty);

            // 未渲染过（无 spinner 位置）→ 直写不可用 → 变值须退回整行标脏兜底，
            // 否则被遮挡/未上屏时内容会丢。直写可用时则交给段级直写（只重写变化的那一段）。
            db.LeftText = "思考中…";
            Check("直写不可用时：左段变值标脏（兜底不丢内容）", db.IsDirty);

            db.ClearDirty();
            db.Status = WayCoder.UI.Shared.AgentStatus.Thinking;
            Check("直写不可用时：Status 变值标脏", db.IsDirty);

            db.ClearDirty();
            db.TokenDisplay = "🔤大1K 小0";
            Check("直写不可用时：TokenDisplay 变值标脏", db.IsDirty);
        }

        // 段级刷新契约：动态栏上屏后（可直写），只有变化的段被重写，其余段不动。
        {
            var segBar = new WayCoder.UI.Tui.Controls.TuiDynamicBar { Width = 60 };
            // owner 必须对齐当前活跃屏幕，否则 RenderDirect 的门控（owner != ActiveScreen）会把直写整个跳过，
            // 断言就会「空字符串通过」——这是套件里跑才暴露的环境依赖。
            segBar.RegisterDirectWrite(WayCoder.UI.TUI.Base.TuiManager.Instance?.ActiveScreen);
            segBar.LeftText = "思考中";
            segBar.ToolText = "bash工具";
            segBar.TokenDisplay = "🔤大1K";
            var keepOut = Console.Out;
            string idle, changed;
            // 捕获必须与断言分开：Check 也写 Console，写在重定向作用域内会被一起吞掉（只剩计数、看不到结果）
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);
                try
                {
                    // OnRender：整行写入 + 记录段缓存（顺便让 _spinnerX 有值 → 可直写）
                    segBar.Render(new System.Text.StringBuilder(), 0, 0, 0, 0, 60, 5);
                    sw.GetStringBuilder().Clear();

                    WayCoder.UI.Tui.Controls.TuiDynamicBar.RenderAllDirect(); // 内容未变
                    idle = sw.ToString();

                    sw.GetStringBuilder().Clear();
                    segBar.TokenDisplay = "🔤大2K"; // 只动右段
                    WayCoder.UI.Tui.Controls.TuiDynamicBar.RenderAllDirect();
                    changed = sw.ToString();
                }
                finally { Console.SetOut(keepOut); segBar.OnDestroy(); }
            }

            // idle 必须非空（否则直写被门控跳过，下面两条会「空通过」变成假绿）
            Check("段级直写：内容未变时不重写左/中/右段（且 spinner 仍在写）",
                idle.Length > 0 && !idle.Contains("思考中") && !idle.Contains("bash工具") && !idle.Contains("🔤大1K"));
            Check("段级直写：只改右段时只重写右段（左/中段不动）",
                changed.Contains("🔤大2K") && !changed.Contains("思考中") && !changed.Contains("bash工具"));
        }

        Section("[动态栏分区刷新（未变内容不重绘护栏）]");
        // 契约：动态栏按**区段**刷新，各段时机不同 —— 左段(状态)只在状态变时写、
        // 中段(工具)只在工具变时写、右段(📊⚡🔤¥)在思考与流式期间持续跳变、spinner 每帧转。
        // 谁变写谁，谁都不带着别人整行重画；内容一字未变时**一个字节都不写**。
        // 此前 OnRender 无条件整行重写：别处重绘把本栏当父容器脏顺带带进来
        //（增量渲染 child.IsDirty || parentDirty）就整行写一遍 —— 实测表现为「空闲时整行一直闪」。
        {
            var owner = WayCoder.UI.TUI.Base.TuiManager.Instance?.ActiveScreen;
            Check("分区刷新：测试前置（存在活跃屏幕，否则直写被门控、会空通过）", owner != null);
            if (owner != null)
            {
                var bar = new WayCoder.UI.Tui.Controls.TuiDynamicBar { Width = 60 };
                var keepOut = Console.Out;
                string first, repeat, marked, rightOnly;
                try
                {
                    bar.RegisterDirectWrite(owner);
                    bar.LeftText = "空闲";
                    bar.ToolText = "bash工具";
                    owner.IsIncrementalUpdate = true; // 增量帧（全屏清屏帧必须整行重画，由该标志区分）
                    var sink = new System.Text.StringBuilder();

                    Console.SetOut(TextWriter.Null); // Render 不写控制台，但防意外输出污染套件
                    bar.MarkDirty();                        // 首次：整行写入（模拟上屏的那一帧）
                    bar.Render(sink, 0, 0, 0, 0, 60, 5);
                    first = sink.ToString();

                    sink.Clear();
                    bar.Render(sink, 0, 0, 0, 0, 60, 5);     // 内容未变 + 增量 + 可直写
                    repeat = sink.ToString();

                    sink.Clear();
                    bar.MarkDirty();                        // 显式标脏（遮挡解除/浮层让位）
                    bar.Render(sink, 0, 0, 0, 0, 60, 5);
                    marked = sink.ToString();

                    sink.Clear();
                    bar.TokenDisplay = "🔤大2K";            // 只有右段变了
                    bar.Render(sink, 0, 0, 0, 0, 60, 5);
                    rightOnly = sink.ToString();
                }
                finally
                {
                    Console.SetOut(keepOut);
                    owner.IsIncrementalUpdate = false;
                    bar.OnDestroy();
                }

                Check("分区刷新：内容未变 + 增量帧 + 可直写 → 整行零写入",
                    first.Length > 0 && repeat.Length == 0);
                Check("分区刷新：显式标脏 → 整行重写一次（遮挡/浮层让位不漏补）",
                    marked.Contains("空闲") && marked.Contains("bash工具"));
                Check("分区刷新：只改右段 → 只补右段，不重写左/中段",
                    rightOnly.Contains("🔤大2K")
                    && !rightOnly.Contains("空闲") && !rightOnly.Contains("bash工具"));

                // 位移：输入区从 1 行长到 3 行会把动态栏整体挪一行。此时段内容可能一字未变，
                // 但段缓存里的绝对行/列已失效 —— 不整行重画就会「老行留旧像素、新行只有 spinner」。
                var moved = new System.Text.StringBuilder();
                bar.Render(moved, 0, 4, 0, 0, 60, 9); // 同一内容、换一行渲染
                Check("分区刷新：本栏位移（行号变化）→ 整行重写",
                    moved.ToString().Contains("空闲") && moved.ToString().Contains("bash工具"));

                // OnRender 补过的段必须刷新段缓存，否则紧随其后的 RenderDirect（同一帧内
                // TuiManager.Render 写完帧就调 RenderAllDirect）会把同一段再写一遍。
                //
                // 这一块容易写成「空通过」假绿，两个坑都堵上：
                //  ① 上面 finally 里的 OnDestroy 已把本栏摘出直写名单 —— 不重新登记，RenderDirect
                //     根本不会跑，`direct` 恒为空字符串，断言「不含 🔤大3K」自然成立却什么都没测到。
                //  ② 必须先正向确认「这一帧真的走了段级补写」，否则整行渲染碰巧不写该段也能绿。
                var direct = new System.Text.StringBuilder();
                var keepOut2 = Console.Out;
                using (var sw = new StringWriter())
                {
                    Console.SetOut(sw);
                    try
                    {
                        bar.RegisterDirectWrite(owner); // ① 重新入册（OnDestroy 摘掉了）
                        owner.IsIncrementalUpdate = true; // 增量帧 → 走「只补变化段」那条路
                        bar.TokenDisplay = "🔤大3K";
                        var patch = new System.Text.StringBuilder();
                        bar.Render(patch, 0, 4, 0, 0, 60, 9); // OnRender 补右段并刷新段缓存
                        Check("分区刷新：改右段后 OnRender 确实走段级补写（非整行、不碰左/中段）",
                            patch.ToString().Contains("🔤大3K")
                            && !patch.ToString().Contains("空闲") && !patch.ToString().Contains("bash工具"));

                        sw.GetStringBuilder().Clear();
                        WayCoder.UI.Tui.Controls.TuiDynamicBar.RenderAllDirect();
                        direct.Append(sw.ToString());
                    }
                    finally
                    {
                        Console.SetOut(keepOut2);
                        owner.IsIncrementalUpdate = false;
                        bar.OnDestroy(); // 别把本栏留在直写名单里跨用例污染 RenderAllDirect
                    }
                }
                var directText = direct.ToString();
                Check("分区刷新：OnRender 补过的段刷新缓存 → 同帧 RenderDirect 不重写该段",
                    directText.Length > 0 && !directText.Contains("🔤大3K")); // ② 非空 = 直写真的跑了
            }

            // 「内容变了 → 只补变化段」与「显式标脏 → 整行重写」是两条相反的契约，此前没有任何
            // 测试区分它们（两边都只断言 IsDirty，两种调用都为真）——于是把 SetContent 的兜底
            // 改回覆写版 MarkDirty()（= 整行重写）能悄悄通过，而那会在模态遮挡期间每个 token
            // 重刷整条底色、把遮罩抹掉。这里用「owner 不是活跃屏幕」制造直写不可用（无需真弹窗）。
            {
                var occl = new WayCoder.UI.Tui.Controls.TuiDynamicBar { Width = 60 };
                var elsewhere = new WayCoder.UI.Tui.Screens.ChatScreen(); // 非活跃屏 ⇒ CanDirectWrite() = false
                elsewhere.IsIncrementalUpdate = true;
                var sinkBase = new System.Text.StringBuilder();
                var sinkContent = new System.Text.StringBuilder();
                var keepOut3 = Console.Out;
                try
                {
                    Console.SetOut(TextWriter.Null);
                    occl.RegisterDirectWrite(elsewhere);
                    occl.LeftText = "空闲";
                    occl.ToolText = "bash工具";
                    occl.MarkRowInvalidated();                                  // 首帧：整行建立基线
                    occl.Render(sinkBase, 0, 0, 0, 0, 60, 5);

                    occl.TokenDisplay = "🔤大9K";                               // 内容变 → 兜底路径
                    bool dirtied = occl.IsDirty;
                    occl.Render(sinkContent, 0, 0, 0, 0, 60, 5);
                    Console.SetOut(keepOut3);

                    Check("分区刷新：遮挡/非活跃屏下内容变化仍标脏（内容不丢）", dirtied);
                    Check("分区刷新：遮挡/非活跃屏下内容变化只做段级补写，不整行重写（不重刷底色抹掉遮罩）",
                        sinkContent.ToString().Contains("🔤大9K")
                        && !sinkContent.ToString().Contains("空闲")
                        && !sinkContent.ToString().Contains("bash工具"));
                }
                finally { Console.SetOut(keepOut3); occl.OnDestroy(); }
            }
        }

        Section("[动态栏直写登记（框架统一登记护栏）]");
        // 直写 spinner 与段级增量全靠 owner 门控（owner 必须是当前活跃屏幕）。**漏登记 = 直写整体失效
        // = 每帧整行重写**（表现正是「内容没变动态栏却一直闪」）。此前登记只写在手写版 ChatScreen 的
        // BuildLayout 里，而默认界面是标记版 MarkupChatScreen（覆写 BuildLayout 且不调 base）——
        // 于是默认界面根本没登记，修闪烁的段级机制在用户实际跑的界面上完全没生效。
        // 现在登记收到框架侧：TuiManager.PushScreen/PopScreen 在 Activate 之后调 RegisterDirectWriters。
        {
            var screen = new WayCoder.UI.Tui.Screens.ChatScreen();
            var bar = new WayCoder.UI.Tui.Controls.TuiDynamicBar { Width = 40 };
            screen.RootView.Add(bar);
            Check("直写登记：未登记时不在直写名单", !bar.IsDirectWriteRegistered);

            screen.RegisterDirectWriters();
            Check("直写登记：RegisterDirectWriters 遍历控件树并认领动态栏", bar.IsDirectWriteRegistered);

            screen.RootView.Remove(bar);
            bar.OnDestroy();
            Check("直写登记：销毁后自动从直写名单摘除（不写已销毁控件）", !bar.IsDirectWriteRegistered);
        }

        // ⚠ 上面用的是**手写版** ChatScreen（自己 new 一个栏塞进 RootView），而用户实际跑的是
        // **标记版** MarkupChatScreen —— 它覆写 BuildLayout 且不调 base，动态栏来自 chat.tui。
        // 曾经那次致命回归恰恰是「登记只写进手写版」⇒ 默认界面直写整体失效、每帧整行重写（空闲一直闪），
        // 而只测手写版的自测全绿。所以这里必须经**框架入口**推一个真·标记版屏幕来验。
        {
            var mgr = WayCoder.UI.TUI.Base.TuiManager.Instance;
            Check("直写登记：测试前置（存在 TuiManager 实例）", mgr != null);
            if (mgr != null)
            {
                int before = WayCoder.UI.Tui.Controls.TuiDynamicBar.RegisteredCount;
                try
                {
                    var mk = new WayCoder.UI.Tui.Screens.MarkupChatScreen();
                    mgr.PushScreen(mk);
                    Check("直写登记：标记版界面经 PushScreen 后 dynamicBar 已认领（登记在框架侧，非手写 BuildLayout）",
                        mk.DynamicBar != null && mk.DynamicBar.IsDirectWriteRegistered);
                    mgr.PopScreen();
                    Check("直写登记：PopScreen 后名单归还到推入前的数量（登记/退册成对，不泄漏）",
                        WayCoder.UI.Tui.Controls.TuiDynamicBar.RegisteredCount == before);
                }
                catch (Exception ex)
                {
                    Check($"直写登记：标记版界面推入后认领动态栏（异常：{ex.Message}）", false);
                }
            }
        }

        Section("[无参数启动界面：重定向 stdin 也能进 TUI]");
        // 「能不能开全屏界面」的判据不是「stdin 是否被重定向」，而是**有没有画布 + 拿不拿得到键盘**：
        // 被别的程序拉起 / 脚本调用 / `waycoder < 文件` 都可能让 stdin 是管道，而进程仍挂着可用控制台。
        // 此前只看 IsInputRedirected → 这种情况下静默退出（零输出、退出码 0），用户看不出发生了什么。
        {
            // 直接调纯逻辑重载（AOT 禁反射，测试也不得走 GetMethod）
            bool Can(bool sin, bool sout, bool dev, bool win)
                => WayCoder.UI.TUI.Base.ConsoleDevice.CanUseFullScreen(sin, sout, dev, win);

            Check("启动判据：正常终端 → 开 TUI", Can(false, false, false, true));
            Check("启动判据：stdin 被重定向但控制台设备可用（Windows）→ 仍开 TUI",
                Can(true, false, true, true));
            Check("启动判据：stdin 被重定向且拿不到控制台设备 → 不开（报错退出，不再静默）",
                !Can(true, false, false, true));
            Check("启动判据：输出被重定向（没有画布）→ 不开", !Can(false, true, true, true));
            Check("启动判据：Unix 上 stdin 被重定向 → 不开（ReadKey 的 raw mode 绑在 stdin）",
                !Can(true, false, true, false));

            // ⚠ 能开界面 ≠ 会读键：读键泵线程走的是**另一条判据** HasKeyboard。上面那几条全绿，
            // 泵仍可能永不启动 —— 那时 UI 起来了、画面在，但按键全无反应，且挂在泵线程上的心跳
            // （spinner / 冻结看门狗 / CPU 采样）一起停摆，只有 Ctrl+C 能逃出去。这条正是本次漏掉的。
            bool KB(bool sin, bool dev) => WayCoder.UI.TUI.Base.ConsoleDevice.HasKeyboard(sin, dev);

            Check("读键闸门：stdin 接键盘 → 启动泵线程", KB(false, false));
            Check("读键闸门：stdin 被重定向但从控制台设备拿到键盘（Windows CONIN$）→ 仍启动泵线程",
                KB(true, true));
            Check("读键闸门：stdin 被重定向且无控制台设备（手上只有空管道）→ 不启动（读它毫无意义）",
                !KB(true, false));

            // 上面只是纯谓词 —— 而自测进程本身就是「重定向 stdin」，谓词全绿也证明不了**闸门真的看了它**。
            // 这里驱动真正的泵启动路径：把「已建好的源 + 有没有键盘通道」直接喂给 InputManager，
            // 再看泵线程有没有起来。若有人把 EnsurePumpStarted 改回 `if (Console.IsInputRedirected) return;`
            //（或反过来无条件启动），下面两条立刻红。
            {
                var withKeys = new WayCoder.UI.TUI.Base.InputManager();
                withKeys.SetSourceForTest(new SilentCharSource(), hasKeySource: true);
                withKeys.ReadInput(1); // 触发 EnsurePumpStarted
                Check("读键闸门：有键盘通道 → 泵线程真的启动（不只是纯谓词成立）",
                    withKeys.IsPumpRunningForTest);
                withKeys.Dispose();

                var noKeys = new WayCoder.UI.TUI.Base.InputManager();
                noKeys.SetSourceForTest(new SilentCharSource(), hasKeySource: false);
                noKeys.ReadInput(1);
                Check("读键闸门：无键盘通道 → 泵线程不启动（不会去读空管道）",
                    !noKeys.IsPumpRunningForTest);
                noKeys.Dispose();
            }
        }

        Section("[启动界面分发：无参数 = 全屏 TUI]");
        // 产品默认行为：`waycoder` 不带任何参数 → 全屏 TUI。此前这条只存在于 Main 里的一串 if 中，
        // 没有任何测试钉住；判据写错（或给某开关加默认值）会让默认界面静默换掉而自测全绿。
        {
            StartupRoute Route(bool web, bool cli, bool tui, bool json, bool prompt)
                => StartupRouter.Decide(web, cli, tui, json, prompt);

            Check("分发：无参数 → Repl（全屏 TUI）", Route(false, false, false, false, false) == StartupRoute.Repl);
            Check("分发：--tui 无参数 → 仍 Repl", Route(false, false, true, false, false) == StartupRoute.Repl);
            Check("分发：--web → Web", Route(true, false, false, false, false) == StartupRoute.Web);
            Check("分发：--cli → Cli", Route(false, true, false, false, false) == StartupRoute.Cli);
            Check("分发：--cli --tui → Repl（--tui 压过 --cli）",
                Route(false, true, true, false, false) == StartupRoute.Repl);

            // -p 走 CLI 纯文本（与 --cli 同源输出，无 spinner）——不再是一次性 spinner 模式
            Check("分发：-p → CliOneShot（CLI 纯文本，非全屏）",
                Route(false, false, false, false, true) == StartupRoute.CliOneShot);
            Check("分发：--json -p → OneShotJson（无界面）",
                Route(false, false, false, true, true) == StartupRoute.OneShotJson);

            // 显式指定界面优先
            Check("分发：--cli -p → CliOneShot（指定 CLI，提示词照常执行，不被丢弃）",
                Route(false, true, false, false, true) == StartupRoute.CliOneShot);
            Check("分发：--web -p → Web（指定 web 优先；提示词由 Main 提示不在终端执行）",
                Route(true, false, false, false, true) == StartupRoute.Web);

            // --tui 带 -p 的搬运：Main 在调 Decide 前把 prompt 挪进槽位队列并置 null，
            // 于是判据落到 Repl —— 这正是「--tui -p」与「-p」行为不同的原因。
            Check("分发：--tui -p 经槽位搬运后（prompt=null）→ Repl",
                Route(false, false, true, false, false) == StartupRoute.Repl);

            // -p1~-p0 槽位任务：prompt 恒为 null（走 _pendingSlotQueues），判据落 Repl
            Check("分发：-p1 槽位任务（prompt=null）→ Repl（进 TUI）",
                Route(false, false, false, false, false) == StartupRoute.Repl);
            Check("分发：-p1 槽位任务 + --json → Repl（JSON 由槽位无界面路径处理）",
                Route(false, false, false, true, false) == StartupRoute.Repl);
        }

        Section("[连接解析：命名连接名优先]");
        // `select default` / `/connect default` 里的 default 是**命名连接名**，不是模型名。
        // 修复前两条解析都落空 → 走「裸模型名」分支，静默建出 providerId/modelId="default"
        // 的假 connect，并把主模型换成一个根本不存在的模型（实测踩过一次）。
        {
            var savedPersist = Global.PersistDisabled;
            var savedM = Config.Instance.Model; var savedP = Config.Instance.Provider;
            var savedSm = Config.Instance.SmallModel; var savedSp = Config.Instance.SmallProvider;
            var savedB = Config.Instance.BaseUrl;
            Global.PersistDisabled = true; // 只改内存，绝不写真实 connections.json / config.json / .env
            try
            {
                // 自包含：不依赖上游 chunk 留下的 fixture 状态（它们中途换过 FilePathOverride）
                ConnectionConfig.ClearCache();
                var big = ConnectionConfig.FindOrCreateConnect("deepseek", "deepseek-v4-pro");
                var small = ConnectionConfig.FindOrCreateConnect("deepseek", "deepseek-v4-flash");
                ConnectionConfig.AddConnection("__test_named_conn__", big!.Name, small!.Name, out _);

                int FakeCount() => ConnectionConfig.ListConnects().Count(c =>
                    string.Equals(c.ModelId, "__test_named_conn__", StringComparison.OrdinalIgnoreCase));
                var before = FakeCount();

                ConnectionConfig.ApplySpec("__test_named_conn__", true, out _);

                Check("连接解析：命名连接名优先于裸模型名（ApplySpec）",
                    ConnectionConfig.ActiveName == "__test_named_conn__"
                    && Config.Instance.Model == "deepseek-v4-pro");
                Check("连接解析：命名连接切换带上小模型（大+小一起切）",
                    Config.Instance.SmallModel == "deepseek-v4-flash");
                Check("连接解析：未把它当模型名建假 connect", FakeCount() == before);
            }
            finally
            {
                Config.Instance.Model = savedM; Config.Instance.Provider = savedP;
                Config.Instance.SmallModel = savedSm; Config.Instance.SmallProvider = savedSp;
                Config.Instance.BaseUrl = savedB;
                Global.PersistDisabled = savedPersist;
                ConnectionConfig.ClearCache();
            }
        }

        Section("[ANSI 网格：审计与按键测试同一套模拟器]");
        // TuiAudit.AnsiToGrid 此前是第二份手写解析（只处理 CUP/\r\n\t/字符），
        // 现已复用 Keypad.FrameBuffer（按键测试用的那套）——否则两个工具会对同一份终端
        // 输出给出互相矛盾的结论。这里钉住它的语义：CUP 定位、宽字符延续格、裁末尾空行。
        {
            Check("ANSI 网格: CUP 定位到第 2 行第 3 列写字，末尾空行裁掉",
                TuiAudit.AnsiToGrid("\x1b[2;3H中", 3, 10) is ["", "  中"]);
            Check("ANSI 网格: 宽字符占两格（延续格不重复输出字符）",
                TuiAudit.AnsiToGrid("中文", 1, 10) is ["中文"]);
            Check("ANSI 网格: SGR 颜色序列不占格子",
                TuiAudit.AnsiToGrid("\x1b[31m红\x1b[0m", 1, 10) is ["红"]);

            // ── 以下五条钉的是「新旧实现结果**不同**」的语义 ──
            // 上面三条对两版解析器同样成立（code-review #5），钉不住本次重构；这五条才是。
            Check("ANSI 网格: 写到宽字符延续格 → 打断该宽字符（真终端把首格清成空格）",
                TuiAudit.AnsiToGrid("\x1b[1;1H中\x1b[1;2HX", 1, 4) is [" X"]);
            Check("ANSI 网格: EL0 从光标擦到行尾（K 序列；旧手写解析一律忽略）",
                TuiAudit.AnsiToGrid("abcdef\x1b[3G\x1b[0K", 1, 10) is ["ab"]);
            Check("ANSI 网格: 满行后 EL1 不越界且擦净（_curC==_cols 时 to 必须钳到末列）",
                TuiAudit.AnsiToGrid(new string('x', 10) + "\x1b[1K", 1, 10).Count == 0);
            Check("ANSI 网格: 光标保存/恢复（s/u；旧手写解析一律忽略）",
                TuiAudit.AnsiToGrid("ab\x1b[s\x1b[1;1HX\x1b[uY", 1, 10) is ["XbY"]);
            Check("ANSI 网格: 零尺寸入参不抛（构造函数钳到 1×1，防 Math.Clamp 的 min>max）",
                TuiAudit.AnsiToGrid("x", 0, 0) is ["x"]);
        }

        Section("[ANSI 解析：生产与测试两套实现的对照]");
        // 生产侧 FrameSnapshot（WayCoder.Preview 渲染用，UI/TUI/Base）与测试侧 FrameBuffer
        // （按键/审计用）是同一件事的两套实现，此前**没有任何用例比较两者**（code-review #3）。
        // 这里在两者的**共同能力范围**内对照（CUP 定位 + SGR + 普通/宽字符），
        // 差异一旦出现（如宽字符延续格、零宽字符处理），这条会红。
        {
            // 注意 CSI 必须有终止符：`\x1b[2;3H`（H=CUP），写成 `\x1b[2;3中` 会让「中」被当成参数
            // 的一部分继续扫描，两个解析器会一致地把它当普通文本（探测过一次，正是这条对照测试的价值）
            var ansi = "A\x1b[2;3H中\x1b[3;1HB";   // 第 1 行 A；第 2 行第 3 列「中」；第 3 行 B
            var fb = new FrameBuffer(4, 10);
            fb.Apply(ansi);
            var grid = fb.Dump();
            var snap = FrameSnapshot.Capture(ansi, 0, 0, 10, 4);

            Check("ANSI 对照: FrameBuffer 与 FrameSnapshot 均已解析",
                grid.Count == 3 && snap != null);
            Check("ANSI 对照: 第 1 行首格一致",
                grid.Count == 3 && snap!.CharAt(0, 0) == "A");
            Check("ANSI 对照: CUP 定位的宽字符一致",
                grid.Count == 3 && grid[1] == "  中" && snap!.CharAt(1, 2) == "中");
            Check("ANSI 对照: 第 3 行位置一致",
                grid.Count == 3 && grid[2] == "B" && snap!.CharAt(2, 0) == "B");
        }

        Section("[列表导航键表：选择器共用]");
        {
            ConsoleKeyInfo K(ConsoleKey k, char c = '\0') => new(c, k, false, false, false);
            Check("导航键: 上下/Home/End/PageUp/PageDown 为导航键",
                TuiListNav.IsNavKey(K(ConsoleKey.UpArrow)) && TuiListNav.IsNavKey(K(ConsoleKey.DownArrow))
                && TuiListNav.IsNavKey(K(ConsoleKey.Home)) && TuiListNav.IsNavKey(K(ConsoleKey.End))
                && TuiListNav.IsNavKey(K(ConsoleKey.PageUp)) && TuiListNav.IsNavKey(K(ConsoleKey.PageDown)));
            Check("导航键: Enter / 字母 / 左右方向键不算（要交给提交或搜索框）",
                !TuiListNav.IsNavKey(K(ConsoleKey.Enter, '\r'))
                && !TuiListNav.IsNavKey(K(ConsoleKey.A, 'a'))
                && !TuiListNav.IsNavKey(K(ConsoleKey.LeftArrow)));
        }

        Section("[区间合并与转录构建：共用实现]");
        {
            // 区间合并：GitCore 的手写 git-diff hunk 与 UnifiedDiff 的 hunk 构建共用同一份
            Check("区间合并: 重叠区间并成一段",
                UnifiedDiff.MergeRanges([(0, 5), (3, 8)]).SequenceEqual([(0, 8)]));
            Check("区间合并: 相接区间合并（s <= 上一段 E）",
                UnifiedDiff.MergeRanges([(0, 3), (3, 6)]).SequenceEqual([(0, 6)]));
            Check("区间合并: 不相邻保持独立",
                UnifiedDiff.MergeRanges([(0, 2), (5, 7)]).SequenceEqual([(0, 2), (5, 7)]));
            Check("区间合并: 空输入 → 空", UnifiedDiff.MergeRanges([]).Count == 0);
            Check("区间合并: 单区间原样", UnifiedDiff.MergeRanges([(2, 4)]).SequenceEqual([(2, 4)]));

            // 转录构建：/kb retro 与 /teach 此前各一份逐字相同实现（只有阈值不同）
            var trMsgs = new List<JNode>
            {
                JNode.Object().Set("role", "user").Set("content", "问题"),
                JNode.Object().Set("role", "assistant").Set("content", ""),      // 空正文跳过
                JNode.Object().Set("role", "assistant").Set("content", "回答"),
            };
            var tr = CommandTextHelpers.BuildTranscript(trMsgs, 2000);
            Check("转录构建: role 作标题、跳过空正文（只出 2 段）",
                tr.Contains("## user") && tr.Contains("问题")
                && tr.Contains("## assistant") && tr.Contains("回答")
                && tr.Split("## ").Length == 3);
            var trLong = CommandTextHelpers.BuildTranscript(
                [JNode.Object().Set("role", "user").Set("content", new string('中', 100))], 10);
            Check("转录构建: 超长按阈值截断且不拆半 CJK",
                trLong.Contains(new string('中', 10)) && !trLong.Contains(new string('中', 11)));
        }

        Section("[Claude 会话解析：唯一实现]");
        // ContextBridge（导外部会话进上下文，要工具调用/摘要）与 ImportHelper（导入成消息）
        // 此前各写一份同格式解析，连文本提取都有三份 → 收敛为 ClaudeSessionParser。
        {
            var jsonl = string.Join("\n",
                """{"type":"user","message":{"content":"你好"}}""",
                """{"type":"user","isSidechain":true,"message":{"content":"侧链应跳过"}}""",
                """{"type":"assistant","message":{"content":[{"type":"text","text":"回复一"},{"type":"tool_use","name":"todo_write","input":{"todos":[{"id":"1"}]}},{"type":"text","text":"回复二"}]}}""",
                """{"type":"summary","summary":"会话摘要"}""",
                """{"type":"unknown_thing"}""",
                """不是 JSON 的行""");
            var tmpJsonl = Path.Combine(Path.GetTempPath(), "waycoder_cc_" + Guid.NewGuid().ToString("N")[..6] + ".jsonl");
            try
            {
                File.WriteAllText(tmpJsonl, jsonl);
                var entries = ClaudeSessionParser.Parse(tmpJsonl);
                Check("Claude 解析: 跳过侧链与无法解析的行（user/assistant×2/tool/summary 共 5 条）",
                    entries.Count == 5);
                Check("Claude 解析: user 正文", entries[0].Kind == "user" && entries[0].Text == "你好");
                // 顺序严格按块出现次序：text(回复一) → tool_use → text(回复二)
                Check("Claude 解析: assistant 多个 text 块各成一条（按块序）",
                    entries[1].Kind == "assistant" && entries[1].Text == "回复一"
                    && entries[3].Kind == "assistant" && entries[3].Text == "回复二");
                Check("Claude 解析: tool_use 带出工具名与 input（夹在文本块之间也按序）",
                    entries[2].Kind == "tool" && entries[2].ToolName == "todo_write"
                    && entries[2].ToolInput?["todos"] != null);
                Check("Claude 解析: summary 单独成条", entries[4].Kind == "summary" && entries[4].Text == "会话摘要");
            }
            finally { try { File.Delete(tmpJsonl); } catch { } }

            Check("Claude 解析: ExtractText 字符串直通", ClaudeSessionParser.ExtractText(JNode.Str("abc")) == "abc");
            Check("Claude 解析: ExtractText 数组取 text 块并以换行连接",
                ClaudeSessionParser.ExtractText(Json.Parse("""[{"type":"text","text":"A"},{"type":"text","text":"B"}]""")) == "A\nB");
            Check("Claude 解析: ExtractText 空数组 → null（旧版返回空串会让标题变空）",
                ClaudeSessionParser.ExtractText(JNode.Array()) == null);
        }

        Section("[任务列表：todo 与 struct_todo 共用前置]");
        // 两个工具的 List 此前各写一份逐字相同的「Load + filter 解析 + OrderBy/ThenBy」，
        // 只有之后的渲染格式不同（那是刻意的）。前置收敛为 TodoStore.LoadFiltered。
        {
            var savedCwdVal = CwdContext.Current.Value;
            var todoTmp = Path.Combine(Path.GetTempPath(), "waycoder_todo_" + Guid.NewGuid().ToString("N")[..6]);
            Directory.CreateDirectory(Path.Combine(todoTmp, ".waycoder"));
            try
            {
                // StorePath 基于 CwdContext.Root → 把工作目录指到临时目录即可隔离真实 todos.json
                CwdContext.Current.Value = todoTmp;
                File.WriteAllText(Path.Combine(todoTmp, ".waycoder", "todos.json"), """
                [
                  { "id": "a", "title": "已完成", "status": "completed", "created_at": "2026-01-01T00:00:00Z" },
                  { "id": "b", "title": "进行中", "status": "in_progress", "created_at": "2026-01-02T00:00:00Z" },
                  { "id": "c", "title": "待办",   "status": "pending",     "created_at": "2026-01-03T00:00:00Z" }
                ]
                """);

                var all = TodoStore.LoadFiltered(null);
                Check("任务列表: 无 filter → 全部，按状态排序（进行中最前、已完成最后）",
                    all.Count == 3 && all[0].Id == "b" && all[2].Id == "a");
                var pending = TodoStore.LoadFiltered("pending");
                Check("任务列表: filter 单状态", pending.Count == 1 && pending[0].Id == "c");
                var multi = TodoStore.LoadFiltered("pending,completed");
                Check("任务列表: filter 多状态（逗号分隔）",
                    multi.Count == 2 && multi.All(t => t.Status is "pending" or "completed"));
                Check("任务列表: filter 空白串等同全部", TodoStore.LoadFiltered("   ").Count == 3);
            }
            finally
            {
                CwdContext.Current.Value = savedCwdVal;
                try { Directory.Delete(todoTmp, true); } catch { }
            }
        }

        Section("[MCP 状态图标：唯一真源]");
        // 此前 5 处各写一份 switch（TUI 侧栏 / /mcp 命令 / 命令行 / 连接状态汇总 / Web），
        // 且汇总那处用 ASCII 的 ✓✗?、其余用 ✅⏳❌ ⇒ 同一状态在不同入口图标不同。
        {
            Check("MCP 图标: 文本端四态一致（Connected/Connecting/Failed/未知）",
                McpStatusIcon.Text(McpServerStatus.Connected) == "✅"
                && McpStatusIcon.Text(McpServerStatus.Connecting) == "⏳"
                && McpStatusIcon.Text(McpServerStatus.Failed) == "❌"
                && McpStatusIcon.Text((McpServerStatus)99) == "❓");
            Check("MCP 图标: Web 端圆点四态一致",
                McpStatusIcon.Dot(McpServerStatus.Connected) == "🟢"
                && McpStatusIcon.Dot(McpServerStatus.Connecting) == "🟡"
                && McpStatusIcon.Dot(McpServerStatus.Failed) == "🔴"
                && McpStatusIcon.Dot((McpServerStatus)99) == "⚪");
        }

        Section("[视觉列换算：编辑器唯一实现]");
        // TuiEditBase.VisualToCharCol 与 TuiRichEditor.VisualToCol 此前各写一份逐字相同的实现，
        // 漂移即「同一个点击位置在两个编辑器里落到不同字符上」。现在共用 AnsiHelper 那一份。
        {
            Check("视觉列换算: ASCII 单宽", AnsiHelper.VisualColToCharIndex("abcdef", 3) == 3);
            Check("视觉列换算: CJK 双宽（第 2 个视觉列落在首个汉字之后的索引 1）",
                AnsiHelper.VisualColToCharIndex("中文abc", 2) == 1);
            Check("视觉列换算: CJK 行内定位（视觉列 4 → 索引 2，即第二个汉字）",
                AnsiHelper.VisualColToCharIndex("中文abc", 4) == 2);
            Check("视觉列换算: Tab 按 4 展开（视觉列 3 仍属第一个 Tab → 索引 0）",
                AnsiHelper.VisualColToCharIndex("\tabc", 3) == 0
                && AnsiHelper.VisualColToCharIndex("\tabc", 4) == 1);
            Check("视觉列换算: 0 或负列 → 0", AnsiHelper.VisualColToCharIndex("abc", 0) == 0
                && AnsiHelper.VisualColToCharIndex("abc", -5) == 0);
            Check("视觉列换算: 超出行宽 → 行末长度", AnsiHelper.VisualColToCharIndex("abc", 99) == 3);
            Check("视觉列换算: 空行 → 0", AnsiHelper.VisualColToCharIndex("", 5) == 0);
            Check("视觉列换算: emoji（代理对）后再定位不拆半",
                AnsiHelper.VisualColToCharIndex("😀x", 2) == 2);
        }

        Section("[工具清单跨端同步（MAUI vs 桌面）]");
        // 两份 ToolRegistry.cs 是**刻意分开**的（MAUI 版裁掉进程类工具，桌面版在 MAUI 的
        // Exclude 清单里），但共享工具清单也各写一份 ⇒ 新增共享工具只加桌面侧时，
        // 移动端**静默少一个工具**（有独立的 ToolRegistry，编译不会报错）。
        // 这条护栏把「桌面 − MAUI == 已知进程类」钉死，漂移即红。
        {
            static string? FindRepoFile(string rel)
            {
                for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d != null; d = d.Parent)
                {
                    var p = Path.Combine(d.FullName, rel);
                    if (File.Exists(p)) return p;
                }
                return null;
            }
            static HashSet<string> ToolNames(string path) =>
                System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path), @"new\s+(\w+Tool)\s*\(\s*\)")
                    .Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

            var deskPath = FindRepoFile(Path.Combine("WayCoder", "Tools", "ToolRegistry.cs"));
            var mauiPath = FindRepoFile(Path.Combine("WayCoder.Maui", "ToolRegistry.cs"));
            if (deskPath == null || mauiPath == null)
            {
                // 打包/发布产物里没有源码 → 该护栏只在开发期生效，跳过不算失败
                Check("工具清单: 无源码目录（打包环境），跳过跨端比对", true);
            }
            else
            {
                var desk = ToolNames(deskPath);
                var maui = ToolNames(mauiPath);
                // 移动端裁剪进程类工具是刻意的；判据是「差集恰好等于这批」
                var desktopOnly = new HashSet<string>(StringComparer.Ordinal)
                {
                    "BashTool", "GitPRTool", "JobKillTool", "JobOutputTool", "KillTool",
                    "LintTool", "LspTool", "PsTool", "ScreenshotTool", "TestTool",
                };
                Check("工具清单: MAUI 无独有工具（必须是桌面的真子集）",
                    maui.All(desk.Contains));
                Check("工具清单: 桌面 − MAUI 恰好是那批进程类工具（新增共享工具漏加一侧即红）",
                    desk.Except(maui).OrderBy(x => x).SequenceEqual(desktopOnly.OrderBy(x => x)));
            }
        }

        Section("[导入源解析：/model 与 /provider 共用一份]");
        // `/model import <源>` 与 `/provider import <源>` 此前各写一份逐字相同的解析：
        // 新增一个在线源、或改一次源名规则，就要改两处（漏一处 = 一个命令能导入、另一个不能）。
        {
            Check("导入源: online 判定（含前缀形式）",
                ModelCli.IsOnlineSource("online") && ModelCli.IsOnlineSource("online opencode,claude")
                && ModelCli.IsOnlineSource("  ONLINE  ") && ModelCli.IsOnlineSource("allonline"));
            Check("导入源: 本地源不被误判为在线",
                !ModelCli.IsOnlineSource("all") && !ModelCli.IsOnlineSource("") && !ModelCli.IsOnlineSource(null)
                && !ModelCli.IsOnlineSource("opencode,codex"));
            Check("导入源: 源名按空格/逗号切分且去掉前缀词",
                ModelCli.ParseOnlineNames("online opencode,claude codex") is ["opencode", "claude", "codex"]);
            Check("导入源: `online` 无源名 → 空数组（= 全部在线源）",
                ModelCli.ParseOnlineNames("allonline").Length == 0);
        }

        Section("[文件锁：冲突提示文案唯一]");
        // 此前 7 个写文件工具各自把处置提示写死在调用点，分裂成两种，其中 4 处干脆没传
        // ⇒ 同样撞锁，有的提示「请等待锁释放或使用其他文件名」，有的只有「文件被锁定」。
        {
            var lockTmp = Path.Combine(Path.GetTempPath(), "waycoder_lock_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
            try
            {
                FileLockManager.ReleaseAll("__lockA__");
                FileLockManager.ReleaseAll("__lockB__");
                FileLockManager.TryAcquire(lockTmp, "__lockA__");
                var lockErr = FileLockManager.TryAcquireOrError(lockTmp, "__lockB__");
                Check("文件锁: 冲突提示含统一处置文案（调用点不再各写一份）",
                    lockErr != null && lockErr.Contains(FileLockManager.BusyHint));
                Check("文件锁: 同 agent 重入不算冲突",
                    FileLockManager.TryAcquireOrError(lockTmp, "__lockA__") == null);
            }
            finally
            {
                FileLockManager.ReleaseAll("__lockA__");
                FileLockManager.ReleaseAll("__lockB__");
            }
        }

        Section("[进程编码判据：哪些程序需要 OEM 解码]");
        // 「所有 cmd 启动点都要 ProcEncoding.Apply」是对的方向，但判据是「启动的是什么」：
        // 只有 cmd.exe / .bat / .cmd / npm 系 shim 的重定向输出才是 OEM 字节；原生程序是 UTF-8，
        // 套 OEM 反而乱码。此前这条判断散在十几个启动点各判各的，8 处该 Apply 的漏了。
        {
            Check("ProcEncoding: cmd.exe 判为控制台包装器（需 OEM 解码）",
                ProcEncoding.IsConsoleWrapperName("cmd.exe"));
            Check("ProcEncoding: npx / npm 判为包装器（npm 装的 .cmd shim）",
                ProcEncoding.IsConsoleWrapperName("npx") && ProcEncoding.IsConsoleWrapperName("npm.cmd"));
            Check("ProcEncoding: *.bat / *.cmd 判为包装器（大小写不敏感）",
                ProcEncoding.IsConsoleWrapperName("run.bat") && ProcEncoding.IsConsoleWrapperName("Build.CMD"));
            Check("ProcEncoding: 原生程序不套 OEM（git / dotnet / node / 绝对路径）",
                !ProcEncoding.IsConsoleWrapperName("git")
                && !ProcEncoding.IsConsoleWrapperName("/usr/bin/dotnet")
                && !ProcEncoding.IsConsoleWrapperName("node"));
            Check("ProcEncoding: 空名 / null 不误判",
                !ProcEncoding.IsConsoleWrapperName("") && !ProcEncoding.IsConsoleWrapperName(null));
            Check("ProcEncoding: 全路径也按文件名判断",
                ProcEncoding.IsConsoleWrapperName(@"C:\Windows\System32\cmd.exe"));
        }

        Section("[MCP 配置存储：mcp_servers.json 唯一读写实现]");
        // 此前三处各写一套「读 → 去重 → 写」，已漂移：去重一处忽略大小写、一处区分大小写；
        // 且三处都是 File.WriteAllText(..., Encoding.UTF8) —— 非原子 + 凭空带 BOM。
        {
            var mcpTmp = Path.Combine(Path.GetTempPath(), "waycoder_mcp_" + Guid.NewGuid().ToString("N")[..6] + ".json");
            try
            {
                Check("McpConfig: 文件不存在 → 空列表", !McpConfigStore.Load(mcpTmp).Items.Any());

                // 模板里那条示例（_comment 含「示例」）不能被当成真服务器
                File.WriteAllText(mcpTmp, """
                [
                  { "_comment": "MCP 服务器配置示例。", "name": "filesystem", "command": "npx" },
                  { "name": "real-one", "command": "node" }
                ]
                """);
                Check("McpConfig: 读时剔掉模板示例条目", McpConfigStore.Load(mcpTmp).Items.Count() == 1);

                // 去重口径统一为「忽略大小写」——此前 McpClient 忽略、ImportHelper 区分，判定相反
                var dup = McpConfigStore.TryAdd(mcpTmp,
                    JNode.Object().Set("name", "REAL-ONE").Set("command", "node"), out var dupErr);
                Check("McpConfig: 同名（忽略大小写）判为已存在，不重复写", !dup && dupErr != null);

                Check("McpConfig: 新服务器写入成功",
                    McpConfigStore.TryAdd(mcpTmp, JNode.Object().Set("name", "second").Set("command", "node"), out _));

                var mcpBytes = File.ReadAllBytes(mcpTmp);
                Check("McpConfig: 写出的文件无 BOM（jq / python json.load 可直接解析）",
                    !(mcpBytes.Length >= 3 && mcpBytes[0] == 0xEF && mcpBytes[1] == 0xBB && mcpBytes[2] == 0xBF));

                var batch = new List<JNode>
                {
                    JNode.Object().Set("name", "third").Set("command", "node"),
                    JNode.Object().Set("name", "real-one").Set("command", "node"), // 已存在（大小写不同）
                };
                Check("McpConfig: 批量导入按同一口径去重（新增 1 条）", McpConfigStore.TryAddRange(mcpTmp, batch) == 1);
                Check("McpConfig: 批量导入后总数正确（real-one, second, third）",
                    McpConfigStore.Load(mcpTmp).Items.Count() == 3);
            }
            finally
            {
                try { File.Delete(mcpTmp); File.Delete(mcpTmp + ".tmp"); } catch { }
            }
        }

        Section("[项目根解析边界（性能回归护栏）]");
        // 非项目目录下 FindProjectRoot 绝不可把用户主目录当项目根：home 下通常有 package.json，
        // 一旦被选中，DetectLanguages 会递归遍历整个 home（几十万文件）——实测系统提示词构建
        // 从 ~0.1s 恶化到 12~36s，且每次会话构建提示词都吃这个开销。
        var savedRootCwd = Directory.GetCurrentDirectory();
        var rootTmp = Path.Combine(Path.GetTempPath(), "waycoder_root_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(rootTmp);
        try
        {
            Directory.SetCurrentDirectory(rootTmp);
            var rootSw = System.Diagnostics.Stopwatch.StartNew();
            var rootInfo = ProjectContext.DetectProject();
            rootSw.Stop();

            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Check("非项目目录下项目根不落在用户主目录",
                !PathsEqual(rootInfo.ProjectRoot, profile) && !PathsEqual(rootInfo.ProjectRoot, Global.Home));
            Check("非项目目录下项目检测 < 3s", rootSw.Elapsed.TotalSeconds < 3);
        }
        finally
        {
            Directory.SetCurrentDirectory(savedRootCwd);
            try { Directory.Delete(rootTmp, true); } catch { }
        }
    }

    /// <summary>路径等价比较（忽略末尾分隔符与大小写，Windows 语义）。</summary>
    private static bool PathsEqual(string a, string b) =>
        string.Equals(a.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                      b.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                      StringComparison.OrdinalIgnoreCase);

    /// <summary>读一个完整按键（带 ConsoleKey/修饰键），语义同 <see cref="ReadChar"/> 的等待策略。</summary>
    private static ConsoleKeyInfo? ReadKey(WindowsCharSource src, TestFeedStream feed)
    {
        for (int i = 0; i < 200; i++)
        {
            if (src.TryReadKey(out var k)) return k;
            Thread.Sleep(2);
        }
        return null;
    }

    private static char? ReadChar(WindowsCharSource src, TestFeedStream feed, bool allowEmpty = false)
    {
        for (int i = 0; i < 200; i++)
        {
            if (src.TryReadChar(out var c)) return c;
            if (src.HasInput || feed.Length > 0) { Thread.Sleep(2); continue; }
            if (allowEmpty) return null;
            Thread.Sleep(2);
        }
        return null;
    }

    /// <summary>阻塞喂入流：Read 在无数据时等待（reader 线程用），Feed 追加并唤醒。</summary>
    private sealed class TestFeedStream : Stream
    {
        private readonly object _lock = new();
        private readonly Queue<byte> _q = new();
        private bool _closed;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length { get { lock (_lock) return _q.Count; } }
        public override long Position { get => 0; set { } }

        public void Feed(byte[] data)
        {
            lock (_lock)
            {
                foreach (var b in data) _q.Enqueue(b);
                Monitor.PulseAll(_lock);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                while (_q.Count == 0 && !_closed) Monitor.Wait(_lock);
                if (_q.Count == 0) return 0;
                int n = Math.Min(count, _q.Count);
                for (int i = 0; i < n; i++) buffer[offset + i] = _q.Dequeue();
                return n;
            }
        }

        public override void Close()
        {
            lock (_lock) { _closed = true; Monitor.PulseAll(_lock); }
            base.Close();
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    /// <summary>永不产出按键的字符源 —— 供「泵线程闸门」用例驱动真实的
    /// <see cref="WayCoder.UI.TUI.Base.InputManager"/> 启动路径，而不触碰真实控制台。</summary>
    private sealed class SilentCharSource : WayCoder.UI.TUI.Base.ICharSource
    {
        public bool HasInput => false;
        public bool TryReadChar(out char c) { c = '\0'; return false; }
        public bool TryReadKey(out ConsoleKeyInfo key) { key = default; return false; }
    }
}

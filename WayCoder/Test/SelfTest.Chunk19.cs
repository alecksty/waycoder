using System.Collections.Concurrent;
using WayCoder.UI.Cli.Arguments;
using WayCoder.UI.TUI.Base;

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
            feed.Feed([0x01, 0x10, 0x1A]); // Ctrl+A / Ctrl+P / Ctrl+Z
            var ca = ReadKey(src, feed);
            var cp = ReadKey(src, feed);
            var cz = ReadKey(src, feed);
            Check("字节 0x01 → Ctrl+A",
                ca?.Key == ConsoleKey.A && ca.Value.Modifiers.HasFlag(ConsoleModifiers.Control));
            Check("字节 0x10 → Ctrl+P（权限循环键）",
                cp?.Key == ConsoleKey.P && cp.Value.Modifiers.HasFlag(ConsoleModifiers.Control));
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

using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Shared;
using WayCoder.Infra;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.TUI.Custom;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

/// <summary>
/// 按键脚本回放 —— 用脚本驱动 TUI（按键 / 打字 / 延时 / 打开对话框），并在任意节点抓取渲染帧，
/// 便于肉眼核对界面排版与键盘交互是否符合预期。
///
/// 运行：waycoder --keypad 脚本.txt
///
/// 脚本格式（每行一条命令，`#` 或 `//` 开头为注释，空行忽略）：
///   KEY:F1              按键（支持修饰键：CTRL+P / SHIFT+TAB / CTRL+SHIFT+F1）
///   KEY:Up / KEY:Down   方向键（Up/Down/Left/Right/Home/End/PgUp/PgDn/Tab/Space/Enter/Escape/Backspace/Delete）
///   RAWKEY:7F           按【终端原始字节】喂键（hex，空格分隔；如 RAWKEY:1B 5B 41）——
///                       走 字节→WindowsCharSource→ConsoleKeyInfo 的真机路径，能复现
///                       Backspace(7F)/Ctrl+字母(01..1A) 这类只有字节层才会暴露的映射问题（KEY 覆盖不到）
///   TEXT:hello world    逐字符键入（用于输入框；换行用单独 KEY:Enter）
///   DELAY:1000          延时毫秒（等待动画/异步更新）
///   SNAP:标签           抓取当前帧并输出纯文本（省略标签则为 SNAP）
///   DIALOG:confirm      打开一个演示对话框（permission/input/list/confirm/multi/buttons/...）
///   STATUS:<名>         强制动态栏状态（thinking/tool/compressing/idle…）并渲染一帧
///   DIRTY               只把屏幕标脏、不渲染（配合 FRAMES 观察「脏帧」的真实重绘范围）
///   FRAMES:<n>          连续渲染 n 帧，逐帧报告写出的字节数 + 光标定位到的行 + 文本——
///                       排查「闪烁 / 区域联动」的核心工具：内容未变的帧应只碰动画行与光标行
///   STREAM:<文本>        模拟一次流式追加（等价 LLM 吐一个 token），走 EnsureAgentStreaming→
///                       AppendToLast→QueueStreamLayout 真实路径
///   STREAMFRAME:<文本>   追加并**当场渲染一帧**后报告其字节数/光标行——测流式重绘范围必须用它，
///                       否则 keypad 循环自己的渲染会先吃掉脏帧，测到的是善后帧
///   FOCUS              转储当前焦点状态（焦点窗口 / RootView 类型 / 焦点控件 / 可聚焦控件列表）
///   MSG:角色:内容       注入一条聊天消息（user/assistant/agent/system/tool/error/banner；`\n` 换行）
///   FILL:数量           批量注入编号消息（交替 user/assistant/system，用于撑满列表测滚动）
///
/// 说明：
///   - 全程重定向 Console.Out 隔离 TUI 的 ANSI 转义。
///   - 帧内容用一个持久化 FrameBuffer 累积增量渲染（与真实终端逐帧覆盖一致），
///     因此 Tab 切焦点 / 增量重绘等只输出 delta 的渲染也能得到完整画面。
///   - 键注入走 TuiManager.OnKey → ChatScreen.OnKey → 模态窗口 → 控件 的完整分发链，
///     能真实验证 Tab 切焦点 / Enter 点击 / Space 激活 / Esc 关闭 等键盘契约。
///   - 全屏 ANSI 对话框（ModelPicker / SessionPicker 等）自读 Console.ReadKey，无法被脚本驱动，
///     仅 TuiWindow 系对话框（TuiDialog.Confirm/Input/Select/MultiSelect 等）可脚本化。
/// </summary>
public static class Keypad
{
    /// <summary>
    /// 行内选择/问卷演示（`INLINE:perm` / `permdanger` / `survey` / `step`）。
    /// 刻意直接调 <see cref="ChatScreen.ShowInlineChoice"/> / <see cref="ChatScreen.ShowInlineSurvey"/>
    /// 而**不**走 UxHelper 的 RunInline* —— 后者会 RenderWait 阻塞到用户作答，脚本没法在展示态抓帧。
    /// </summary>
    private static void ShowInlineDemo(TextWriter orig, ChatScreen screen, string kind)
    {
        switch (kind)
        {
            case "perm":
                screen.ShowInlineChoice(
                [
                    new PromptItem { Kind = EPromptKind.Choice, Label = "1. 允许", Detail = "仅本次执行", ResultCode = 0 },
                    new PromptItem { Kind = EPromptKind.Choice, Label = "2. 全部允许", Detail = "本会话内不再询问同类操作", ResultCode = 1 },
                    new PromptItem { Kind = EPromptKind.Choice, Label = "3. 拒绝", Detail = "取消本次操作", ResultCode = 2 },
                ], code => Emit(orig, $"# 行内权限选择结果 = {code}"));
                break;

            case "permdanger":
                screen.ShowInlineChoice(
                [
                    new PromptItem { Kind = EPromptKind.Choice, Label = "1. 允许", Detail = "仅本次执行", ResultCode = 0, IsDangerous = true },
                    new PromptItem { Kind = EPromptKind.Choice, Label = "2. 拒绝", Detail = "取消本次操作", ResultCode = 2, IsDangerous = true },
                ], code => Emit(orig, $"# 行内危险操作选择结果 = {code}（A 键应被忽略）"));
                break;

            case "step":
                screen.ShowInlineSurvey(DemoSurvey(),
                    r => Emit(orig, "# 问卷(分步骤)结果 = " + FmtSurvey(r)), showTabs: false);
                break;

            default: // survey
                screen.ShowInlineSurvey(DemoSurvey(),
                    r => Emit(orig, "# 问卷(横向标签页)结果 = " + FmtSurvey(r)), showTabs: true);
                break;
        }
    }

    /// <summary>演示问卷：一题单选 + 一题多选（覆盖两种形态）</summary>
    private static List<SurveyQuestion> DemoSurvey() =>
    [
        new("权限", "允许哪些操作？",
            [new SurveyOption("读取", "只读文件"), new SurveyOption("写入", "修改文件")], false),
        new("范围", "作用范围？",
            [new SurveyOption("本次"), new SurveyOption("本会话"), new SurveyOption("总是")], true),
    ];

    private static string FmtSurvey(SurveyResult? r)
        => r == null ? "null（已取消）"
         : string.Join(" | ", r.Picks.Select((p, i) =>
             "[" + string.Join(",", p) + "]" + (r.Others[i] != null ? "(其他:" + r.Others[i] + ")" : "")));

    public static int Run(string scriptPath)
    {
        if (!File.Exists(scriptPath))
        {
            Console.Error.WriteLine($"✘ 按键脚本不存在: {scriptPath}");
            return 1;
        }

        string[] lines;
        try { lines = File.ReadAllLines(scriptPath); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✘ 读取脚本失败: {ex.Message}");
            return 1;
        }

        // 隔离 TUI 的 ANSI 输出（备用屏转义 + 每帧渲染），帧内容改从持久化 FrameBuffer 读取
        var orig = Console.Out;
        Console.SetOut(TextWriter.Null);
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

        var mgr = TuiManager.Instance;
        try
        {
            mgr.Enter();
            mgr.RefreshTheme();
            SlashCommandRegistry.RegisterAll(); // COMMAND 命令测试需命令注册表

            var screen = new ChatScreen { ChatDisplayStyle = Config.Instance.ChatDisplayStyle };
            mgr.PushScreen(screen);
            screen.ChatMessages.Add(new ChatMsg { Role = "system", Content = $"⌨ Keypad 回放: {Path.GetFileName(scriptPath)}" });

            // 绑定快捷键回调（真实 REPL 在 Program.Repl 注入）——Keypad 只记录触发，不执行阻塞对话框
            screen.OnOpenSessions = () => Emit(orig, "# [键] Ctrl+S → 会话列表回调");
            screen.OnOpenDiff = () => Emit(orig, "# [键] Ctrl+D → diff 预览回调");
            screen.OnCycleModel = () => Emit(orig, "# [键] Ctrl+M → 模型选择回调");
            screen.OnReasoningEffort = () => Emit(orig, "# [键] Ctrl+G → 推理深度回调");
            screen.OnShowHelp = () => Emit(orig, "# [键] Ctrl+H → 帮助回调");
            screen.OnSearchHistory = q => Emit(orig, $"# [键] Ctrl+F → 搜索回调: {q}");
            screen.OnOpenCommandPalette = () => Emit(orig, "# [键] Ctrl+U → 命令面板回调");

            // 支持 WAYCODER_KEYPAD_SIZE=WxH 覆盖帧尺寸（排查不同终端宽度下的布局）
            var envSize = Environment.GetEnvironmentVariable("WAYCODER_KEYPAD_SIZE");
            if (!string.IsNullOrEmpty(envSize))
            {
                var parts = envSize.Split('x');
                if (parts.Length == 2
                    && int.TryParse(parts[0], out var ew) && int.TryParse(parts[1], out var eh))
                    Tty.SizeOverride = (Math.Clamp(ew, 40, 240), Math.Clamp(eh, 12, 120));
            }

            int rows = Math.Clamp(Tty.Rows, 12, 120);
            int cols = Math.Clamp(Tty.Cols, 40, 240);
            var frame = new FrameBuffer(rows, cols);

            mgr.Render();
            frame.Apply(mgr.LastCleanFrame);

            int step = 0;
            foreach (var raw in lines)
            {
                var (op, value) = Parse(raw);
                step++;

                switch (op)
                {
                    case "":   // 空行
                    case "#":  // 注释
                        break;

                    case "KEY":
                        if (TryParseKey(value, out var k))
                            mgr.OnKey(k);
                        else
                            Emit(orig, $"# (第 {step} 行) 无法识别的按键: {value}");
                        break;

                    case "INJECT":
                        // 注入按键到 InputManager 队列：供后台阻塞式选择器（ModelPicker 等）的
                        // RenderWait(forceReadKeys) 消费 —— 那些选择器主线程阻塞，只能走队列喂键。
                        if (TryParseKey(value, out var ik))
                            mgr.Input.InjectKey(ik);
                        else
                            Emit(orig, $"# (第 {step} 行) 无法识别的按键: {value}");
                        break;

                    case "INLINE":
                        // 行内选择栏 / 问卷演示 —— 只挂栏**不等待**（UxHelper 那条路会阻塞在 RenderWait，
                        // 脚本就再也走不到后面的 SNAP 了）。结果经回调打印成 `# …` 行。
                        ShowInlineDemo(orig, screen, value.Trim());
                        break;

                    case "DIRTY":
                        // 诊断：只标脏不渲染（配合 FRAMES 观察「脏帧」的真实重绘范围）
                        mgr.IsDirty = true;
                        screen.MarkDirty();
                        break;

                    case "STREAMFRAME":
                        // 诊断：模拟一次流式追加并**当场渲染一帧**，报告该帧字节数与光标行。
                        // 必须与追加同帧，否则 keypad 循环自己的渲染会先吃掉脏帧，测到的是善后帧。
                        {
                            screen.EnsureAgentStreaming();
                            screen.AppendToken(value.Replace("\\n", "\n"));
                            var cap2 = new StringWriter();
                            var keep2 = Console.Out;
                            Console.SetOut(cap2);
                            try { mgr.Render(); } finally { Console.SetOut(keep2); }
                            var s2 = cap2.ToString();
                            var rows2 = System.Text.RegularExpressions.Regex
                                .Matches(s2, @"\x1b\[(\d+);(\d+)H")
                                .Select(m => int.Parse(m.Groups[1].Value)).Distinct().OrderBy(r => r).ToArray();
                            Emit(orig, $"# STREAMFRAME: bytes={s2.Length} rows=[{string.Join(",", rows2)}]");
                        }
                        break;

                    case "STREAM":
                        // 诊断：模拟一次流式追加（等价于 LLM 吐一个 token），走
                        // EnsureAgentStreaming → AppendToLast → QueueStreamLayout 的真实路径。
                        screen.EnsureAgentStreaming();
                        screen.AppendToken(value.Replace("\\n", "\n"));
                        break;

                    case "FRAMES":
                        // 诊断：连续渲染 n 帧，逐帧报告「写出的字节数 / 光标定位到的行」。
                        // 用途：验证「动态栏动画是否带动其它区域重绘」——内容未变时不该出现别的行。
                        {
                            int frameCount = int.TryParse(value.Trim(), out var nn) && nn > 0 ? Math.Min(nn, 40) : 6;
                            for (int fi = 0; fi < frameCount; fi++)
                            {
                                var cap = new StringWriter();
                                var keep = Console.Out;
                                Console.SetOut(cap);
                                try { mgr.Render(); } finally { Console.SetOut(keep); }
                                var s = cap.ToString();
                                var hitRows = System.Text.RegularExpressions.Regex
                                    .Matches(s, @"\x1b\[(\d+);(\d+)H")
                                    .Select(m => int.Parse(m.Groups[1].Value))
                                    .Distinct().OrderBy(r => r).ToArray();
                                var preview = System.Text.RegularExpressions.Regex
                                    .Replace(s, @"\x1b\[[0-9;?]*[A-Za-z]", "")
                                    .Replace("\r", "").Replace("\n", "");
                                Emit(orig, $"# FRAME {fi}: bytes={s.Length} rows=[{string.Join(",", hitRows)}] text={preview}");
                                Thread.Sleep(120);
                            }
                        }
                        break;

                    case "STATUS":
                        // 诊断：强制动态栏状态（thinking/tool/compressing/idle…），复现「思考中」等状态的渲染行为
                        if (Enum.TryParse<AgentStatus>(value.Trim(), ignoreCase: true, out var st))
                        {
                            screen.DynamicBar.Status = st;
                            screen.DynamicBar.MarkDirty();
                            screen.MarkDirty();
                            mgr.Render();
                        }
                        else Emit(orig, $"# 未知状态: {value}");
                        break;

                    case "RAWKEY":
                        // 走【终端原始字节】真机路径：hex 字节 → WindowsCharSource → ConsoleKeyInfo → OnKey。
                        // KEY:/INJECT 直接注入 ConsoleKeyInfo，**绕过了字节映射层**——Windows 读键自 v0.96.74
                        // 起走 VT 字节流，Backspace(0x7F)、Ctrl+字母(0x01..0x1A) 这类映射漏洞只有这条路径能复现。
                        // 仅覆盖单字节键；ESC 开头的方向/功能键序列仍用 KEY:Up 等。
                        foreach (var kb in RawBytesToKeys(value))
                            mgr.OnKey(kb);
                        break;

                    case "MODEL":
                        // 后台线程打开 ModelPicker（forceReadKeys=true 主动读队列），主线程可继续
                        // DELAY/SNAP/INJECT 驱动它；模拟真实 /model 的「弹框选模型」完整交互。
                        {
                            bool forceRead = value.Trim().Equals("force", StringComparison.OrdinalIgnoreCase);
                            var t = Task.Run(() => ModelPicker.Show(-1, true));
                            Emit(orig, $"# MODEL: 已启动 ModelPicker 后台线程（forceReadKeys=true）");
                        }
                        break;

                    case "TEXT":
                        foreach (var c in value)
                            mgr.OnKey(CharToKey(c));
                        break;

                    case "DELAY":
                        if (int.TryParse(value, out var ms) && ms > 0)
                            Thread.Sleep(Math.Min(ms, 60000));
                        break;

                    case "DIALOG":
                        OpenDialog(screen, value);
                        break;

                    case "MSG":
                        AddMsg(screen, value);
                        break;

                    case "FILL":
                        if (int.TryParse(value, out var n) && n > 0)
                            FillChat(screen, Math.Min(n, 500));
                        else
                            Emit(orig, $"# (第 {step} 行) FILL 需要正整数: {value}");
                        break;

                    case "FOCUS":
                        DumpFocus(screen, orig);
                        break;

                    case "TREE":
                        DumpTree(screen, orig);
                        break;

                    case "SETTINGSALL":
                        DiagnoseSettingsAll(orig);
                        break;

                    case "DIAGDIALOG":
                        DiagnoseDialogLengths(orig);
                        break;

                    case "DIAGSELECT":
                        DiagnoseSelectN(orig);
                        break;

                    case "COMMANDMAIN":
                        // 主线程同步执行斜杠命令（模拟真实 REPL：Program.Repl 在 UI 线程 await 命令）。
                        // 弹窗类命令（/free 等）必须在主线程 ShowWindow 才能渲染 —— COMMAND 后台线程弹窗不画。
                        {
                            var (cmd2, cargs2) = SlashCommandRegistry.Match(value.Trim());
                            if (cmd2 == null) { Emit(orig, $"# 未知命令: {value}"); break; }
                            try
                            {
                                cmd2.ExecuteAsync(cargs2, screen).GetAwaiter().GetResult();
                                Emit(orig, $"# ✓ {value}: 完成");
                            }
                            catch (Exception ex)
                            {
                                Emit(orig, $"# 命令 {value} 异常: {ex.GetType().Name}: {ex.Message}");
                            }
                        }
                        break;

                    case "COMMAND":
                        // 执行斜杠命令（后台线程 + 超时检测），排查哪些命令卡住界面
                        {
                            var (cmd, cargs) = SlashCommandRegistry.Match(value.Trim());
                            if (cmd == null) { Emit(orig, $"# 未知命令: {value}"); break; }
                            var ctask = Task.Run(() =>
                            {
                                try { cmd.ExecuteAsync(cargs, screen).GetAwaiter().GetResult(); return true; }
                                catch (Exception ex) { Emit(orig, $"# 命令 {value} 异常: {ex.GetType().Name}: {ex.Message}"); return true; }
                            });
                            if (ctask.Wait(8000)) Emit(orig, $"# ✓ {value}: 完成");
                            else Emit(orig, $"# ⚠ {value}: 卡住（8s 未完成，仍在后台跑）");
                        }
                        break;

                    case "COMPLETE":
                        // 模拟任务完成阶段（RunSlotAgentAsync 完成块）：模型流式宣告「任务已完成」→
                        // FinishAgentMsg → ✅完成系统消息 → UpdateTokenDisplayFull → 渲染。
                        // 用于确定性复现「任务已完成出现后 TUI 死机」；COMPLETE:n 可流式 n 条。
                        {
                            int streamN = 1;
                            if (int.TryParse(value.Trim(), out var cnt) && cnt > 0) streamN = Math.Min(cnt, 200);
                            screen.Running = false;
                            for (int i = 0; i < streamN; i++)
                            {
                                screen.StartAgentMsg();
                                foreach (var ch in $"任务已完成（第 {i + 1} 批）\n\n已按计划完成全部步骤。")
                                {
                                    screen.AppendToken(ch.ToString());
                                    mgr.Render();
                                }
                                screen.FinishAgentMsg();
                                mgr.Render();
                            }
                            screen.AddSystemMsg("✅ 完成 · 耗时 3.2s · 📊 1200+800=2000 词元 · 💰 ¥0.03");
                            screen.UpdateTokenDisplayFull(12345, 6789, 0.123, 80000, 128000, 0, 0);
                            mgr.Render();
                            Emit(orig, $"# COMPLETE: 任务完成阶段已执行（流式 {streamN} 批）");
                        }
                        break;

                    case "EDITOR":
                        // 直接打开带文件的编辑器（绕过无文件时的文件选择框），便于测试编辑/保存
                        // EDITOR:/path --readonly → 只读模式（禁止修改）
                        {
                            bool ro = value.Contains("--readonly", StringComparison.OrdinalIgnoreCase);
                            string path = value.Replace("--readonly", "", StringComparison.OrdinalIgnoreCase).Trim();
                            try { mgr.PushScreen(new EditorScreen(path, ro)); }
                            catch (Exception ex) { Emit(orig, $"# (第 {step} 行) 打开编辑器失败: {ex.Message}"); }
                        }
                        break;

                    case "SNAP":
                        EmitFrame(orig, value, frame.Dump());
                        break;

                    case "RAW":
                        // 调试：输出上次 Render 的原始 ANSI（转义 ESC 便于阅读），排查增量重绘坐标问题
                        {
                            var rawSb = mgr.LastCleanFrame
                                .Replace("\x1b", "«E»")
                                .Replace("\r", "«R»")
                                .Replace("\n", "«N»\n");
                            Emit(orig, $"===== [RAW {value}] =====");
                            Emit(orig, rawSb);
                            Emit(orig, $"===== [/RAW {value}] =====");
                        }
                        break;

                    case "SNAPCOLOR":
                        EmitFrame(orig, value, frame.DumpAnsi());
                        break;

                    case "MOUSE":
                        // MOUSE:x,y —— 模拟鼠标左键点击（0-based 坐标），路由到当前屏幕
                        {
                            var p = value.Split(',');
                            if (p.Length == 2
                                && int.TryParse(p[0].Trim(), out var mx) && int.TryParse(p[1].Trim(), out var my))
                            {
                                mgr.HandleMouse(new InputEvent { Type = InputType.Mouse, MouseX = mx, MouseY = my, MouseLeft = true });
                            }
                            else Emit(orig, $"# (第 {step} 行) MOUSE 需 x,y（0-based 屏幕坐标）: {value}");
                        }
                        break;

                    case "SCROLL":
                        // SCROLL:up|down —— 模拟鼠标滚轮（真实滚轮带指针坐标；TuiListView 等按坐标命中才滚动）
                        {
                            int mx = Math.Clamp(Tty.Cols / 2, 0, Math.Max(0, Tty.Cols - 1));
                            int my = Math.Clamp(2, 0, Math.Max(0, Tty.Rows - 1)); // 聊天列表/内容区域
                            if (value.Trim().ToLowerInvariant() == "up")
                                mgr.HandleMouse(new InputEvent { Type = InputType.Mouse, MouseX = mx, MouseY = my, MouseScrollUp = true });
                            else if (value.Trim().ToLowerInvariant() == "down")
                                mgr.HandleMouse(new InputEvent { Type = InputType.Mouse, MouseX = mx, MouseY = my, MouseScrollDown = true });
                            else Emit(orig, $"# (第 {step} 行) SCROLL 需 up|down: {value}");
                        }
                        break;

                    case "SHOT":
                        // 截屏：把当前帧渲染为 PNG（TrueTypeFont 矢量渲染，CJK 可读）
                        try
                        {
                            string p = string.IsNullOrWhiteSpace(value) ? $"keypad_{step}.png" : value;
                            frame.RenderPng(p);
                            Emit(orig, $"# (第 {step} 行) 截屏已保存: {p}");
                        }
                        catch (Exception ex)
                        {
                            Emit(orig, $"# (第 {step} 行) 截屏失败: {ex.Message}");
                        }
                        break;

                    default:
                        Emit(orig, $"# (第 {step} 行) 未知命令: {op}");
                        break;
                }

                // 每条命令后统一渲染 + 累积到持久化帧缓冲（与真实终端行为一致）
                mgr.Render();
                frame.Apply(mgr.LastCleanFrame);
            }
        }
        finally
        {
            try { mgr.Exit(); } catch { }
            Console.SetOut(orig);
        }

        return 0;
    }

    // ── 脚本解析 ──

    /// <summary>把一行脚本拆成 (命令, 值)，命令转大写、值保留原文。注释行返回 Op="# "。</summary>
    static (string Op, string Value) Parse(string line)
    {
        line = line.Trim();
        if (line.Length == 0) return ("", "");
        if (line.StartsWith('#') || line.StartsWith("//")) return ("#", "");

        int idx = line.IndexOf(':');
        if (idx < 0) return (line.ToUpperInvariant(), "");
        return (line[..idx].Trim().ToUpperInvariant(), line[(idx + 1)..].Trim());
    }

    // ── 按键解析 ──

    /// <summary>解析按键描述（如 F1 / Up / CTRL+P / SHIFT+TAB）为 ConsoleKeyInfo。</summary>
    static bool TryParseKey(string spec, out ConsoleKeyInfo key)
    {
        key = default;
        spec = spec.Trim();
        if (spec.Length == 0) return false;

        bool ctrl = false, shift = false, alt = false;
        var parts = spec.Split('+');
        var keyName = parts[^1].Trim();

        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].Trim().ToUpperInvariant())
            {
                case "CTRL": case "CONTROL": ctrl = true; break;
                case "SHIFT": shift = true; break;
                case "ALT": alt = true; break;
                default: return false; // 未知修饰键
            }
        }

        var consoleKey = ParseKeyName(keyName);
        if (consoleKey == null) return false;

        key = new ConsoleKeyInfo(DetermineKeyChar(consoleKey.Value, shift, ctrl),
            consoleKey.Value, shift, alt, ctrl);
        return true;
    }

    static ConsoleKey? ParseKeyName(string name)
    {
        name = name.Trim();
        if (name.Length == 1)
        {
            char c = name[0];
            if (c >= 'a' && c <= 'z') return (ConsoleKey)(ConsoleKey.A + (c - 'a'));
            if (c >= 'A' && c <= 'Z') return (ConsoleKey)(ConsoleKey.A + (c - 'A'));
            if (c >= '0' && c <= '9') return (ConsoleKey)(ConsoleKey.D0 + (c - '0'));
        }

        switch (name.ToUpperInvariant())
        {
            case "ENTER": case "RETURN": return ConsoleKey.Enter;
            case "ESC": case "ESCAPE": return ConsoleKey.Escape;
            case "UP": case "UPARROW": return ConsoleKey.UpArrow;
            case "DOWN": case "DOWNARROW": return ConsoleKey.DownArrow;
            case "LEFT": case "LEFTARROW": return ConsoleKey.LeftArrow;
            case "RIGHT": case "RIGHTARROW": return ConsoleKey.RightArrow;
            case "TAB": return ConsoleKey.Tab;
            case "SPACE": case "SPACEBAR": return ConsoleKey.Spacebar;
            case "PGUP": case "PAGEUP": return ConsoleKey.PageUp;
            case "PGDN": case "PAGEDOWN": return ConsoleKey.PageDown;
            case "HOME": return ConsoleKey.Home;
            case "END": return ConsoleKey.End;
            case "BACKSPACE": case "BACK": return ConsoleKey.Backspace;
            case "DEL": case "DELETE": return ConsoleKey.Delete;
            case "INS": case "INSERT": return ConsoleKey.Insert;
        }

        if (name.StartsWith('F')
            && int.TryParse(name[1..], out var fn) && fn >= 1 && fn <= 24)
            return (ConsoleKey)(ConsoleKey.F1 + fn - 1);

        return null;
    }

    static char DetermineKeyChar(ConsoleKey key, bool shift, bool ctrl)
    {
        if (ctrl)
        {
            // Ctrl+字母 → 控制字符（与终端一致）
            if (key >= ConsoleKey.A && key <= ConsoleKey.Z)
                return (char)(key - ConsoleKey.A + 1);
            return '\0';
        }
        if (key >= ConsoleKey.A && key <= ConsoleKey.Z)
            return shift ? (char)(key - ConsoleKey.A + 'A') : (char)(key - ConsoleKey.A + 'a');
        if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9)
        {
            if (!shift) return (char)(key - ConsoleKey.D0 + '0');
            return ")!@#$%^&*("[key - ConsoleKey.D0];
        }
        if (key == ConsoleKey.Spacebar) return ' ';
        return '\0';
    }

    /// <summary>
    /// hex 字节串（"7F" 或 "1B 5B 41"，空格/逗号分隔）→ 真实 <see cref="WindowsCharSource"/> 解出的按键列表。
    /// 用于在脚本里复现「终端交给程序的原始字节」，覆盖 KEY:/INJECT 绕过的字节映射层。
    /// </summary>
    static List<ConsoleKeyInfo> RawBytesToKeys(string spec)
    {
        var bytes = new List<byte>();
        foreach (var tok in spec.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            var t = tok.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? tok[2..] : tok;
            if (byte.TryParse(t, System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out var b))
                bytes.Add(b);
        }

        var keys = new List<ConsoleKeyInfo>();
        if (bytes.Count == 0) return keys;

        // 用可注入流构造真实字符源：读取线程把字节入队后，TryReadKey 走的就是生产同一条解码路径。
        using var src = new WindowsCharSource(new System.IO.MemoryStream(bytes.ToArray()));

        // 读取线程是异步的：刚构造完队列还是空的，此时 HasInput=false 只代表「还没读进来」，
        // 不代表「读完了」。因此先有界等首字节入队，再按「连续 quiet 次读空」判定排空结束。
        for (int i = 0; i < 300 && !src.HasInput; i++) Thread.Sleep(1);

        for (int quiet = 0; quiet < 25 && keys.Count < 64; )
        {
            if (src.TryReadKey(out var k)) { keys.Add(k); quiet = 0; continue; }
            Thread.Sleep(2);
            quiet++;
        }
        return keys;
    }

    /// <summary>单个可打印字符 → ConsoleKeyInfo。文本插入走 TuiEditBase 的 keyChar 分支。</summary>
    static ConsoleKeyInfo CharToKey(char c)
    {
        return c switch
        {
            '\n' => new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false),
            '\t' => new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false),
            _ => new ConsoleKeyInfo(c, (ConsoleKey)0, false, false, false),
        };
    }

    // ── 演示对话框 ──

    static void OpenDialog(ChatScreen screen, string name)
    {
        // 支持 DIALOG:name 或 DIALOG:name:长度（内容=「测」×长度，测长内容显示/折行）
        int textLen = 0;
        int colon = name.LastIndexOf(':');
        if (colon > 0 && int.TryParse(name[(colon + 1)..], out var tl) && tl > 0)
        {
            textLen = tl;
            name = name[..colon];
        }
        string Txt(string fallback) => textLen > 0 ? new string('测', textLen) : fallback;

        switch (name.ToLowerInvariant())
        {
            case "permission": TuiDemo.ShowPermissionDemo(screen); return;
            case "input": TuiDemo.ShowInputDemo(screen); return;
            case "list": case "select": TuiDemo.ShowListDemo(screen); return;
            case "confirm": TuiDemo.ShowConfirmDemo(screen); return;
            case "findreplace": case "find": TuiDemo.ShowFindReplaceDemo(screen); return;
            case "shortmenu": case "short": TuiDemo.ShowShortMenuDemo(screen); return;
            case "longmenu": case "long": TuiDemo.ShowLongMenuDemo(screen); return;
            case "contextmenu": case "context": TuiDemo.ShowContextMenuDemo(screen); return;
            case "multi": case "multiselect": TuiDemo.ShowMultiSelectDemo(screen); return;
            case "buttons": case "buttongroup": TuiDemo.ShowButtonGroupDemo(screen); return;
            case "controls": TuiDemo.ShowControlsDemo(screen); return;
            case "panel": TuiDemo.ShowPanelDemo(screen); return;
            case "tree": TuiDemo.ShowTreeDemo(screen); return;
            // ── TuiDialog 系（非阻塞 ShowWindow，可脚本按键测试；confirm/input/select/multiselect 已在上面 TuiDemo 分支覆盖）──
            case "info": screen.ShowWindow(TuiDialog.Info("信息", Txt("这是一条信息"))); return;
            case "success": screen.ShowWindow(TuiDialog.Success("成功", Txt("操作成功"))); return;
            case "warn": screen.ShowWindow(TuiDialog.Warn("警告", Txt("请注意"))); return;
            case "error": screen.ShowWindow(TuiDialog.Error("错误", Txt("出错了"))); return;
            case "confirm3": screen.ShowWindow(TuiDialog.Confirm3("确认", Txt("确定执行？"), _ => { })); return;
            case "inputline": screen.ShowWindow(TuiDialog.InputLine("输入", Txt("输入内容"), "", _ => { })); return;
            case "secret": screen.ShowWindow(TuiDialog.Secret("密钥", Txt("输入密钥"), "", _ => { })); return;
            case "ask": screen.ShowWindow(TuiDialog.Ask("提问", "请选择一个选项",
                ["选项 1", "选项 2", "选项 3"], false, _ => { }, _ => { })); return;
            case "perm": screen.ShowWindow(TuiDialog.Permission("权限确认", "允许执行此命令？", _ => { })); return;
        }
    }

    // ── 聊天消息注入 ──

    /// <summary>注入一条聊天消息：MSG:角色:内容。`\n` 转义为换行，角色决定渲染方式。</summary>
    static void AddMsg(ChatScreen screen, string spec)
    {
        int idx = spec.IndexOf(':');
        string role = idx < 0 ? "system" : spec[..idx].Trim().ToLowerInvariant();
        string content = (idx < 0 ? spec : spec[(idx + 1)..]).Trim().Replace("\\n", "\n");

        switch (role)
        {
            case "user": screen.AddMessage(content, "user"); return;
            case "assistant": case "agent": screen.AddMessage(content, "assistant"); return;
            case "system": screen.AddMessage(content, "system"); return;
            case "tool": screen.AddMessage(content, "tool", indent: 1); return;
            case "error": screen.AddMessage("❌ " + content, "system"); return;
            case "banner": screen.AddMessage(content, "banner", centered: true); return;
            default: screen.AddMessage(content, "system"); return;
        }
    }

    /// <summary>批量注入编号消息（交替 user/assistant/system），用于撑满聊天列表测滚动/翻页。</summary>
    static void FillChat(ChatScreen screen, int n)
    {
        for (int i = 1; i <= n; i++)
        {
            int m = i % 3;
            if (m == 0)
                screen.AddMessage($"系统 #{i}: 这是一条较长的系统提示消息，用于填充聊天列表以验证滚动与翻页的显示效果。", "system");
            else if (m == 2)
                screen.AddMessage($"助手回复 #{i}\n- 要点 A{i}\n- 要点 B{i}\n\n`代码片段 {i}` 已就绪。", "assistant");
            else
                screen.AddMessage($"用户提问 #{i}: 请帮我实现功能 {i}，并说明设计思路。", "user");
        }
    }

    // ── 设置界面全项巡检 ──

    /// <summary>自动巡检设置界面所有设置项：逐项 Enter 弹编辑对话框，检查窗口弹出，Esc 关闭。
    /// Model/SmallModel 走阻塞 ModelPicker（RenderWait），跳过不测。结果输出到 w。</summary>
    static void DiagnoseSettingsAll(TextWriter w)
    {
        var mgr = TuiManager.Instance;
        var schema = Config.SettingSchema();
        var groups = schema.GroupBy(s => s.Category)
            .Select(g => (Cat: g.Key, Items: g.OrderBy(s => s.Order).ToList()))
            .ToList();

        var sc = new SettingsScreen();
        mgr.PushScreen(sc);

        var down = new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false);
        var tab = new ConsoleKeyInfo('\0', ConsoleKey.Tab, false, false, false);
        var enter = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        var esc = new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false);

        int curCat = 0, curItem = 0;
        bool onDetail = false;
        int pass = 0, fail = 0, skip = 0;

        w.WriteLine($"[SETTINGSALL] 设置界面全项巡检（{groups.Count} 个分类，{schema.Count} 项）");
        for (int gi = 0; gi < groups.Count; gi++)
        {
            var g = groups[gi];
            while (curCat < gi) { mgr.OnKey(down); curCat++; }
            if (!onDetail) { mgr.OnKey(tab); onDetail = true; }

            for (int ji = 0; ji < g.Items.Count; ji++)
            {
                var item = g.Items[ji];
                while (curItem < ji) { mgr.OnKey(down); curItem++; }

                if (item.Key is "Model" or "SmallModel")
                {
                    w.WriteLine($"  [跳过] {g.Cat}/{item.Label}（ModelPicker 阻塞，另测）");
                    skip++;
                    mgr.OnKey(down); // 实际按 Down 移到下一项，保持逻辑 curItem 与 UI 选中同步
                    curItem++;
                    continue;
                }

                w.WriteLine($"    [编辑] {g.Cat}/{item.Label} (ji={ji} curItem={curItem})");
                mgr.OnKey(enter);
                mgr.Render();
                var win = sc.FocusedWindow;
                bool ok = win != null && win.Title == item.Label;
                if (ok) pass++;
                else { fail++; w.WriteLine($"  [✗] {g.Cat}/{item.Label} ({item.Type}): 窗口={(win == null ? "<无>" : "「" + win.Title + "」")}"); }
                if (ok)
                    w.WriteLine($"  [✓] {g.Cat}/{item.Label} ({item.Type}): 窗口「{win!.Title}」");
                if (win != null)
                {
                    mgr.CloseWindow(win);
                    mgr.Render();
                }
                mgr.OnKey(down); // 每一项（含跳过项）后都按 Down 移到下一项，curItem 与 UI 选中同步
                curItem++;
            }
            curItem = 0;
            mgr.OnKey(tab); onDetail = false; // 回左侧，切下一分类
        }

        w.WriteLine($"[SETTINGSALL] 巡检结果: 通过 {pass}  失败 {fail}  跳过 {skip}  共 {schema.Count}");
        mgr.OnKey(esc); // 退出设置界面
        mgr.Render();
    }

    // ── 对话框内容长度测试 ──

    /// <summary>遍历常用对话框 × 内容长度（1/10/50/100/500/1000 字），打开→检查窗口弹出→关闭，
    /// 验证长内容不崩溃、窗口正常显示（花屏需配合 SNAP 抽查）。</summary>
    static void DiagnoseDialogLengths(TextWriter w)
    {
        var mgr = TuiManager.Instance;
        var screen = mgr.ActiveScreen as ChatScreen;
        if (screen == null) { w.WriteLine("[DIAGDIALOG] 无 ChatScreen"); return; }

        int[] lengths = [1, 10, 50, 100, 500, 1000];
        var builders = new (string Name, Func<string, string, TuiWindow> Build)[]
        {
            ("info",      (t, m) => TuiDialog.Info(t, m)),
            ("confirm",   (t, m) => TuiDialog.Confirm(t, m, _ => { })),
            ("confirm3",  (t, m) => TuiDialog.Confirm3(t, m, _ => { })),
            ("input",     (t, m) => TuiDialog.Input(t, m, "", _ => { })),
            ("inputline", (t, m) => TuiDialog.InputLine(t, m, "", _ => { })),
            ("secret",    (t, m) => TuiDialog.Secret(t, m, "", _ => { })),
        };

        int pass = 0, fail = 0;
        foreach (var (name, build) in builders)
        {
            foreach (var len in lengths)
            {
                string content = new string('测', len);
                try
                {
                    var win = build("长度测试", content);
                    screen.ShowWindow(win);
                    mgr.Render();
                    bool ok = screen.HasModal && screen.FocusedWindow == win;
                    if (!ok) { fail++; w.WriteLine($"  [✗] {name} 长度{len}: 窗口未弹出"); }
                    else pass++;
                    screen.CloseWindow(win);
                    mgr.Render();
                }
                catch (Exception ex)
                {
                    fail++;
                    w.WriteLine($"  [✗] {name} 长度{len}: 异常 {ex.GetType().Name}: {ex.Message}");
                }
            }
            w.WriteLine($"  {name}: 6 个长度完成");
        }
        w.WriteLine($"[DIAGDIALOG] 对话框长度测试: 通过 {pass}  失败 {fail}");
    }

    // ── 单选/多选列表容量测试 ──

    /// <summary>实测 Select（单选）/ MultiSelect（多选）在选项数 12/50/100/500/1000 下：
    /// 窗口正常弹出、可见项 ≤12、滚动到末项、无崩溃。输出可见项数与滚动偏移。</summary>
    static void DiagnoseSelectN(TextWriter w)
    {
        var mgr = TuiManager.Instance;
        var screen = mgr.ActiveScreen as ChatScreen;
        if (screen == null) { w.WriteLine("[DIAGSELECT] 无 ChatScreen"); return; }

        int[] counts = [12, 50, 100, 500, 1000];
        int pass = 0, fail = 0;
        foreach (var n in counts)
        {
            var items = Enumerable.Range(1, n).Select(i => $"选项 {i}").ToList();
            try
            {
                // 单选
                var selWin = TuiDialog.Select($"单选 {n} 项", items, _ => { });
                screen.ShowWindow(selWin);
                mgr.Render();
                var selList = selWin.RootView.FindFocused() as TuiList;
                int vis = Math.Min(n, 12);
                if (screen.HasModal && selList != null)
                {
                    // 滚动到底，验证末项可达
                    for (int i = 0; i < n; i++) mgr.OnKey(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));
                    mgr.Render();
                    bool endReached = selList.SelectedIndex == n - 1 || selList.SelectedIndex >= n - 1;
                    pass++;
                    w.WriteLine($"  单选 {n} 项: 可见={vis} 末项选中={selList.SelectedIndex} {(endReached ? "✓" : "✗")}");
                }
                else { fail++; w.WriteLine($"  单选 {n} 项: 窗口/列表异常"); }
                screen.CloseWindow(selWin);

                // 多选
                var multiWin = TuiDialog.MultiSelect($"多选 {n} 项", items, _ => { });
                screen.ShowWindow(multiWin);
                mgr.Render();
                var multiList = multiWin.RootView.FindFocused() as TuiList;
                if (screen.HasModal && multiList != null)
                {
                    pass++;
                    w.WriteLine($"  多选 {n} 项: 可见={Math.Min(n, 12)} 项数={multiList.Items.Count} ✓");
                }
                else { fail++; w.WriteLine($"  多选 {n} 项: 窗口/列表异常"); }
                screen.CloseWindow(multiWin);
                mgr.Render();
            }
            catch (Exception ex)
            {
                fail++;
                w.WriteLine($"  [✗] 选项数 {n}: 异常 {ex.GetType().Name}: {ex.Message}");
            }
        }
        w.WriteLine($"[DIAGSELECT] 单选/多选容量测试: 通过 {pass}  失败 {fail}");
    }

    // ── 焦点转储 ──

    /// <summary>输出当前焦点状态：焦点窗口、RootView 类型、焦点控件、可聚焦控件列表。</summary>
    static void DumpFocus(ChatScreen screen, TextWriter w)
    {
        // 用当前活跃屏幕（设置界面/编辑器等 PushScreen 后，弹框窗口在其 Windows 栈上，
        // 而不是 Keypad 创建的主 ChatScreen 上 —— 否则测设置界面弹框时 FOCUS 恒为空）
        var active = TuiManager.Instance?.ActiveScreen ?? screen;
        var win = active.FocusedWindow;
        if (win == null)
        {
            w.WriteLine("[FOCUS] 无焦点窗口");
            return;
        }
        w.WriteLine($"[FOCUS] 窗口: \"{win.Title}\" (modal={win.Modal})");
        var rv = win.RootView;
        w.WriteLine($"[FOCUS] RootView: {(rv == null ? "<null>" : rv.GetType().Name)} (Focused={rv?.Focused})");
        var fc = win.FocusedControl;
        w.WriteLine($"[FOCUS] 焦点控件: {(fc == null ? "<无>" : fc.GetType().Name)}");
        var sel = DescribeSelection(fc);
        if (sel.Length > 0)
            w.WriteLine($"[FOCUS]   选中项: {sel}");
        // 菜单（MenuView 本身就是 RootView 且直接处理按键，FocusedControl 可能为空）
        var rvSel = DescribeSelection(rv);
        if (rvSel.Length > 0 && rv != fc)
            w.WriteLine($"[FOCUS]   RootView 选中项: {rvSel}");
        if (rv != null)
        {
            var list = rv.GetAllFocusable();
            w.WriteLine($"[FOCUS] 可聚焦控件 ({list.Count}):");
            for (int i = 0; i < list.Count; i++)
                w.WriteLine($"  [{i}] {list[i].GetType().Name}{(list[i].Focused ? "  ◀ 已聚焦" : "")}");
        }
    }

    // ── 控件树转储 ──

    /// <summary>输出当前屏幕控件树：每个控件的 X/Y/Width/Height/绝对坐标，用于排查布局错位（如内容渲染到错误位置）。</summary>
    static void DumpTree(ChatScreen screen, TextWriter w)
    {
        var active = TuiManager.Instance?.ActiveScreen;
        var root = active?.RootView;
        if (root == null) { w.WriteLine("[TREE] RootView 为空"); return; }
        w.WriteLine($"[TREE] 屏幕 {active!.GetType().Name} RootView: {root.GetType().Name} W={root.Width} H={root.Height}");

        void Walk(TuiControl c, int depth)
        {
            // GetAbsoluteX 是 protected，用 Parent 链累加（RootView.Parent=null，X 即绝对）
            int ax = c.X, ay = c.Y;
            for (var p = c.Parent; p != null; p = p.Parent) { ax += p.X; ay += p.Y; }
            string text = DescribeContent(c);
            w.WriteLine($"{new string(' ', depth * 2)}{c.GetType().Name} x={c.X} y={c.Y} w={c.Width} h={c.Height} vis={c.Visible} foc={c.Focused} abs=({ax},{ay}){text}");
            if (c is TuiView v)
                foreach (var ch in v.Children)
                    Walk(ch, depth + 1);
        }
        Walk(root, 0);
    }

    static string DescribeContent(TuiControl c)
    {
        switch (c)
        {
            case TuiLabel l:
                var t = l.Text;
                if (t.Length > 24) t = t[..24] + "…";
                return $" text=\"{t}\"";
            case TuiList l:
                return $" items={l.Items.Count} sel={l.SelectedIndex}";
            case TuiListView lv:
                return $" items={lv.Children.Count} sel={lv.SelectedIndex} scroll={lv.ScrollOffset} auto={lv.IsAutoScrollToEnd}";
            default:
                return "";
        }
    }

    /// <summary>读取控件的「选中索引/选中文本」，用于验证上下键是否真的移动了选中项（AOT 无反射，用类型匹配）。</summary>
    static string DescribeSelection(object? c)
    {
        switch (c)
        {
            case TuiList l:
                return l.MultiSelect
                    ? $"idx={l.SelectedIndex}/{l.Items.Count} 勾选=[{string.Join(",", l.CheckedIndices.Order())}]"
                    : $"idx={l.SelectedIndex}/{l.Items.Count}";
            case TuiListView lv:
                return $"idx={lv.SelectedIndex}/{lv.Children.Count}";
            case TuiRadioGroup rg:
                return $"idx={rg.SelectedIndex}/{rg.Options.Count}";
            case TuiComboBox cb:
                return $"idx={cb.SelectedIndex}/{cb.Options.Count} 展开={cb.IsExpanded}";
            case TuiTreeView tv:
                return tv.SelectedNode == null ? "idx=<无>" : $"「{tv.SelectedNode.Text}」";
            case TuiMenu.MenuView mv:
                return $"idx={mv.SelectedIndex}/{mv.ItemCount}";
            default:
                return "";
        }
    }

    // ── 输出 ──

    static void Emit(TextWriter w, string text) => w.WriteLine(text);

    static void EmitFrame(TextWriter w, string label, List<string> lines)
    {
        var tag = string.IsNullOrWhiteSpace(label) ? "SNAP" : "SNAP " + label;
        w.WriteLine();
        w.WriteLine($"===== [{tag}] =====");
        foreach (var l in lines) w.WriteLine(l);
        w.WriteLine($"===== [/{tag}] =====");
    }

    // ── 持久化帧缓冲：累积增量渲染，等价于真实终端的屏幕缓冲区 ──
}
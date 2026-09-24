using WayCoder.Infra;
using WayCoder.UI.Shared;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// **VML 宿主层**（`UI/Shared/VmlHostRuntime.cs` + `IVmlHost`）的自测。
    ///
    /// ## 为什么这一层值得单独测
    ///
    /// 它是**手机端与桌面端编同一份文件**的那块（桌面 `scripts/vmlcli`、手机
    /// `WayCoder.Maui/Services/VmlUiCalls.cs` 都只是它的薄壳）。抽出来的目的就是
    /// "同一规则只有一处实现"，而抽出来之后**两端都不可能再各自发现分叉** ——
    /// 所以护栏必须落在这里，而且必须用一个**假的**宿主驱动（不依赖 MAUI、不依赖 vmlcli）。
    ///
    /// ## 覆盖的三件事
    ///
    /// ① **消息队列的账**（`VmlMessageQueue`）—— 这里曾经有一个真 bug，
    ///    见 <see cref="TestVmlMessageQueue"/> 的注释（"定时器明明投了消息，`ui_wait` 却当场返回超时"）；
    /// ② **syscall 分派**：号段外必须放行、`#57` 只截那一个号、返回值写回 R0、参数钳位；
    /// ③ **内存读写**：越界一律返回空/不写，绝不让程序给的野指针把宿主带崩。
    /// </summary>
    private static void TestChunk28(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        TestVmlMessageQueue(Section, Check, Fail);
        TestVmlHostDispatch(Section, Check, Fail);
        TestVmlPixelReadback(Section, Check, Fail);
        TestVmlMemoryAccess(Section, Check, Fail);
        TestVmlScreenshot(Section, Check, Fail);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ① 消息队列
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// **信号量的账必须与队列长度一一对应。**
    ///
    /// 这里修过一个真 bug（`VmlUiProtocol.VmlMessageQueue`）：`Post` 原来写的是
    /// `if (_signal.CurrentCount == 0) _signal.Release();`（把信号量当"唤醒开关"），
    /// 而 `TryRead` **不消费**许可 ⇒ 队列可以被取空而计数还留着，
    /// 于是 `Read` 里那次 `Wait` 会**空唤醒**、当场返回 null。
    ///
    /// 实测症状（`scripts/vmlcli-verify/run.sh` 抓住的）：程序连读两条消息、再装一个定时器，
    /// 60ms 后定时器投的消息明明进了队列，`ui_wait(msg, 2000)` 却**立刻**返回 0 ——
    /// 调用方完全看不出是宿主的账没对上，只会以为"这一拍没有事件"。
    /// 手机端跑的是同一份代码，同一套账。
    ///
    /// ⚠ 判据要**两条一起**，缺一条都测不出来：
    ///   · 队列空了之后再读，必须**真的等**（不能秒回）；
    ///   · 等的时候有人投消息，必须**被叫醒**（不能睡死到超时）。
    ///   只测第一条的话，一个"永远睡死"的实现也会通过。
    /// </summary>
    private static void TestVmlMessageQueue(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 消息队列：信号量的账");

        var q = new VmlMessageQueue();
        q.Post(new VmlMessage(VmlMsgType.KeyDown, 37, 0, 0));
        q.Post(new VmlMessage(VmlMsgType.KeyUp, 37, 0, 0));

        var m1 = q.TryTake();
        var m2 = q.TryTake();
        Check("连投两条能逐条取走", m1?.A == 37 && m2?.A == 37
            && m1.Value.Type == VmlMsgType.KeyDown && m2.Value.Type == VmlMsgType.KeyUp);

        // 取空之后再投：**这一条才是那个 bug 的判据** ——
        // 修复前，上面两次 TryTake 没消费许可 ⇒ 计数还留着 ⇒ 下面这次 Wait 空唤醒秒回 null。
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var waiter = Task.Run(() => q.Read(3000, keep: false));
        Thread.Sleep(80);
        Check("队列空时阻塞读取不会秒回（账没对上就会秒回）", !waiter.IsCompleted);

        q.Post(new VmlMessage(VmlMsgType.Timer, 7, 99, 0));
        var woken = waiter.Wait(2000);
        sw.Stop();
        Check("等待中的读取被新消息叫醒", woken && waiter.Result?.Type == VmlMsgType.Timer);
        Check("被叫醒的是一个真结果，不是超时", waiter.Result is { A: 7, B: 99 });

        // 超时必须如实返回 null（而不是拿队列里的旧消息凑数）
        var t0 = Environment.TickCount64;
        var none = q.Read(120, keep: false);
        var elapsed = Environment.TickCount64 - t0;
        Check("空队列读到超时返回 null", none is null);
        Check($"超时是真的等满了（实等 {elapsed}ms）", elapsed >= 100);

        // keep（只看队头）不取走：连看两次拿同一条，且**不消费许可**（再看一次阻塞读仍能拿到它）
        q.Post(new VmlMessage(VmlMsgType.TouchDown, 11, 22, 0));
        var peek1 = q.TryRead(keep: true);
        var peek2 = q.TryRead(keep: true);
        Check("keep 模式连读两次是同一条", peek1?.A == 11 && peek2?.A == 11);
        Check("keep 之后仍能消费到同一条", q.Read(500, keep: false)?.A == 11);

        q.Clear();
        Check("Clear 之后计数归零（再读会如实超时）", q.Count == 0 && q.Read(80, keep: false) is null);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ② syscall 分派（用一个假宿主驱动）
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 一个**记录型**的假宿主：所有平台答案都可预期、所有调用都留痕。
    /// 两端（手机 / 桌面）真实现的行为差别都在这一层，所以共享层的自测只需要它。
    /// </summary>
    private sealed class FakeVmlHost : IVmlHost
    {
        public (int Width, int Height) ScreenArea() => (480, 640);
        public int Orientation() => VmlUi.Portrait;

        public VmlScene? Opened;
        public int CloseCount;
        public int SceneChangedCount;
        public bool OpenResult = true;

        public bool OpenWindow(VmlScene scene) { Opened = scene; return OpenResult; }

        /// <summary>光栅化时落过的临时图（自测结束要删）。</summary>
        public readonly List<string> TempImages = new();

        /// <summary>
        /// **真光栅化**（不是记一笔就算）—— 走的是与两端宿主完全相同的那条路
        /// （`DrawRunner.Parse` + `Rasterize`）。
        ///
        /// <para>
        /// 假宿主只在**平台答案**上假（屏幕尺寸、对话框、音效…），而"把场景画成像素"
        /// 这件事**没有平台差异**，用真的才能把 floodfill / getimage / putimage
        /// 这三个号测透 —— 记一笔的假实现在这里等于什么都没测。
        /// </para>
        /// </summary>
        public bool Rasterize(int x, int y, int w, int h, byte[] dest)
        {
            if (Opened is null || w <= 0 || h <= 0 || dest.Length < w * h * 4) return false;
            var canvas = DrawRunner.Rasterize(DrawRunner.Parse(Opened.BuildDsl()));

            for (int row = 0; row < h; row++)
            {
                int sy = y + row;
                if (sy < 0 || sy >= canvas.Height) continue;
                for (int col = 0; col < w; col++)
                {
                    int sx = x + col;
                    if (sx < 0 || sx >= canvas.Width) continue;
                    int si = (sy * canvas.Width + sx) * 4;
                    int di = (row * w + col) * 4;
                    dest[di] = canvas.Pixels[si];
                    dest[di + 1] = canvas.Pixels[si + 1];
                    dest[di + 2] = canvas.Pixels[si + 2];
                    dest[di + 3] = canvas.Pixels[si + 3];
                }
            }
            return true;
        }

        /// <summary>真编码成 PNG 落临时目录（用完由用例删）。</summary>
        public string? SaveTempImage(int w, int h, byte[] rgba)
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), $"vmlhost-{Guid.NewGuid():N}.png");
                File.WriteAllBytes(path, PngEncoder.Encode(w, h, rgba));
                TempImages.Add(path);
                return path;
            }
            catch { return null; }
        }
        public void CloseWindow() => CloseCount++;
        public void SceneChanged(VmlScene scene) => SceneChangedCount++;

        public readonly List<string> Dialogs = new();
        /// <summary>消息框/单选/多选的固定答案（-1 = 取消）。</summary>
        public int DialogAnswer = -1;
        /// <summary>输入框的固定答案（null = 取消）。</summary>
        public string? InputAnswer = "(用户输入)";

        public int DlgMsg(string title, string body, int style)
        { Dialogs.Add($"msg|{title}|{body}|{style}"); return DialogAnswer < 0 ? 0 : DialogAnswer; }
        public int DlgSelect(string title, string prompt, IReadOnlyList<string> options)
        { Dialogs.Add($"select|{title}|{string.Join(",", options)}"); return DialogAnswer; }
        public int DlgMulti(string title, string prompt, IReadOnlyList<string> options)
        { Dialogs.Add($"multi|{title}|{string.Join(",", options)}"); return DialogAnswer; }
        public string? DlgInput(string title, string prompt)
        { Dialogs.Add($"input|{title}|{prompt}"); return InputAnswer; }

        public readonly List<(int Hz, int Ms, int Wave, int Vol)> Tones = new();
        public void Tone(int hz, int ms, int wave, int volume) => Tones.Add((hz, ms, wave, volume));

        public readonly List<string> Audio = new();
        public bool PlayAudio(string fullPath, bool loop) { Audio.Add($"play|{fullPath}|{loop}"); return true; }
        public void StopAudio() => Audio.Add("stop");
        public void SetAudioVolume(int volume) => Audio.Add($"vol|{volume}");

        public readonly List<string> Vibes = new();
        public bool Vibrate(int ms, int amplitude) { Vibes.Add($"{ms}/{amplitude}"); return true; }
        public bool VibratePattern(long[] pattern) { Vibes.Add(string.Join("+", pattern)); return true; }

        public readonly Dictionary<string, string> Store = new(StringComparer.Ordinal);
        public string? StoreGet(string key) => Store.TryGetValue(key, out var v) ? v : null;
        public void StoreSet(string key, string value) => Store[key] = value;
        public void StoreDel(string key) => Store.Remove(key);

        public bool? LastKeepScreenOn;
        public void KeepScreenOn(bool on) => LastKeepScreenOn = on;

        /// <summary>
        /// `ResolvePath` 的前缀。默认 `/fake/` —— 记录型，不指向任何真实目录。
        ///
        /// ⚠ 截屏用例会**真的写文件**，那时必须把它指到临时目录：写到 `/fake/` 会因
        /// 目录不存在而失败，测出来的就不是"截屏逻辑"而是"目录不存在"。
        /// </summary>
        public string ResolvePrefix = "/fake/";

        public string ResolvePath(string relative) => ResolvePrefix + relative;

        /// <summary>截屏的固定答案（null = 这一端截不了，用来测失败路径）。</summary>
        public RasterImage? CaptureAnswer;

        /// <summary>截屏被调用了几次 —— 用来断言「路径非法时压根没去截」。</summary>
        public int CaptureCalls;

        public RasterImage? CaptureAppWindow() { CaptureCalls++; return CaptureAnswer; }

        public readonly List<string> Logs = new();
        public void Log(string message) => Logs.Add(message);

        public void RegisterJsonHandlers()
            => VmlJsonApi.Register("fake.version", _ => JNode.Object().Set("v", "fake"));
    }

    private static void TestVmlHostDispatch(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 宿主：syscall 分派");

        var host = new FakeVmlHost();
        var rt = new VmlHostRuntime(host);
        var regs = new int[32];
        var mem = new byte[4096];

        // ── 号段之外必须**放行**（`Handles` 只认 500–599，别把内置 syscall 吞掉）──────
        Check("号段外的 syscall 交回运行时（4）", !rt.HandleSyscall(4, regs, mem));
        Check("号段外的 syscall 交回运行时（400）", !rt.HandleSyscall(400, regs, mem));
        Check("号段外的 syscall 交回运行时（600）", !rt.HandleSyscall(600, regs, mem));

        // ── `#57` 喇叭蜂鸣：**只截这一个内置号**，且要接到宿主音频上 ────────────────
        regs[0] = 440; regs[1] = 250;
        Check("#57 被宿主认领", rt.HandleSyscall(VmlUi.VmSpeakerBeep, regs, mem));
        Check("#57 接到宿主的 Tone（波形=方波 1）", host.Tones.Count == 1 && host.Tones[0] == (440, 250, 1, 100));
        Check("#57 返回 0", regs[0] == 0);
        // 别的内置号不能被顺带截走
        Check("#58 仍然交回运行时", !rt.HandleSyscall(58, regs, mem));

        // ── 参数钳位：`ClampTone` 的两端都要生效 ──────────────────────────────────
        regs[0] = 1; regs[1] = 999_999;
        rt.HandleSyscall(VmlUi.VmSpeakerBeep, regs, mem);
        Check("超范围频率/时长被钳（不是原样透传）",
            host.Tones[1].Hz == VmlUi.ToneMinHz && host.Tones[1].Ms == VmlUi.ToneMaxMs);

        // ── 开窗：默认值 + 场景声明 ──────────────────────────────────────────────
        regs[0] = WriteCStr(mem, 0, "标题");
        regs[1] = 0; regs[2] = -5;                    // 非法宽高 → 回落到 320×240
        regs[3] = 0; regs[4] = 0;
        Check("WIN_OPEN 返回 1（句柄）", rt.HandleSyscall(VmlUi.WinOpen, regs, mem) && regs[0] == 1);
        Check("非法宽高回落到 320×240", host.Opened is { Width: 320, Height: 240 });
        Check("老号 = Legacy 转屏声明（不动坐标系）", host.Opened?.Rotation == WindowRotation.Legacy);
        Check("标题从内存里读出来了", host.Opened?.Title == "标题");

        // ── 老号**不能**去读 R3/R4：只传 3 个参数的程序，那两只寄存器里是它自己的垃圾 ──
        regs[3] = VmlUi.PortraitOnly; regs[4] = VmlUi.NoGamepad;
        rt.HandleSyscall(VmlUi.WinOpen, regs, mem);
        Check("老号不读 R3（声明照旧是 Legacy）", host.Opened?.Rotation == WindowRotation.Legacy);
        Check("老号不读 R4（手柄照旧是要）", host.Opened?.NeedGamepad == true);

        // ── 新号（WIN_OPEN_EX）才读那两个声明 ────────────────────────────────────
        rt.HandleSyscall(VmlUi.WinOpenEx, regs, mem);
        Check("WIN_OPEN_EX 读到 R3=只竖屏", host.Opened?.Rotation == WindowRotation.PortraitOnly);
        Check("WIN_OPEN_EX 读到 R4=不要手柄", host.Opened?.NeedGamepad == false);

        // ── 场景尺寸取自程序，`SCR_W/H` 取自宿主（两者互不干扰）─────────────────────
        regs[0] = 0; regs[1] = 200; regs[2] = 100; regs[3] = 1; regs[4] = 1;
        rt.HandleSyscall(VmlUi.WinOpenEx, regs, mem);
        regs[0] = regs[1] = regs[2] = regs[3] = 0;
        rt.HandleSyscall(VmlUi.ScrW, regs, mem);
        var sw = regs[0];
        rt.HandleSyscall(VmlUi.ScrH, regs, mem);
        Check("SCR_W/SCR_H 报的是宿主那块区域（480×640）", sw == 480 && regs[0] == 640);
        Check("场景尺寸是程序自己要的（200×100）", host.Opened is { Width: 200, Height: 100 });

        // ── 绘制：图元真的进了场景，且宿主收到了"变了"的通知 ─────────────────────
        var before = host.SceneChangedCount;
        regs[0] = 10; regs[1] = 20; regs[2] = 30; regs[3] = 40; regs[4] = unchecked((int)0xFF00FF00);
        regs[5] = 1; regs[6] = 0; regs[7] = 0;
        rt.HandleSyscall(VmlUi.DrawRect, regs, mem);
        Check("rect 进了场景（图元数 +1）", host.Opened?.FigureCount == 1);
        Check("宿主收到了场景变化通知", host.SceneChangedCount > before);

        rt.HandleSyscall(VmlUi.DrawPresent, regs, mem);
        Check("ui_present 拍下了快照（PresentVersion=1）", host.Opened?.PresentVersion == 1);
        Check("快照里有这一帧的 DSL", host.Opened?.PresentedDsl?.Contains("rect 10 20 30 40") == true);

        rt.HandleSyscall(VmlUi.DrawClear, regs, mem);
        Check("ui_clear 清空了图元", host.Opened?.FigureCount == 0);

        // ── 文字属性钳位（0 / 负字号会让排版算出零行高）────────────────────────────
        regs[0] = 0; regs[1] = 0; regs[2] = 0; regs[3] = 0;
        rt.HandleSyscall(VmlUi.SetFont, regs, mem);
        Check("字号 0 被钳到下限 6", host.Opened?.FontSize == 6);
        regs[0] = 99999;
        rt.HandleSyscall(VmlUi.SetFont, regs, mem);
        Check("超大字号被钳到 200", host.Opened?.FontSize == 200);

        // ── 对话框：内存里的字符串原样交给宿主，返回值写回 R0 ──────────────────────
        host.DialogAnswer = 1;
        var tOff = WriteCStr(mem, 200, "标题");
        var bOff = WriteCStr(mem, 300, "正文");
        regs[0] = tOff; regs[1] = bOff; regs[2] = 2;      // style=2 → 标题加 ⛔ 前缀
        rt.HandleSyscall(VmlUi.DlgMsg, regs, mem);
        Check("DlgMsg 把标题/正文读出来交给宿主", host.Dialogs.Count == 1
            && host.Dialogs[0] == "msg|⛔ 标题|正文|2");
        Check("DlgMsg 把宿主的答案写回 R0", regs[0] == 1);

        // 选项块：count 个 \0 分隔的串
        var oOff = 400;
        WriteCStrSeq(mem, oOff, "甲", "乙", "丙");
        host.DialogAnswer = 2;
        regs[0] = tOff; regs[1] = bOff; regs[2] = oOff; regs[3] = 3;
        rt.HandleSyscall(VmlUi.DlgSelect, regs, mem);
        Check("DlgSelect 读出 3 个选项", host.Dialogs[^1] == "select|标题|甲,乙,丙");
        Check("DlgSelect 回传宿主给的答案", regs[0] == 2);

        // 选项数为 0：不去打扰宿主，直接回 -1（取消）
        var dlgCount = host.Dialogs.Count;
        regs[0] = tOff; regs[1] = bOff; regs[2] = oOff; regs[3] = 0;
        rt.HandleSyscall(VmlUi.DlgSelect, regs, mem);
        Check("零选项不弹框、直接回 -1", regs[0] == -1 && host.Dialogs.Count == dlgCount);

        // 输入框：宿主给的字串按 UTF-8 写回程序内存，返回**字节数**（不是字符数）
        var buf = 1000;
        Array.Clear(mem, buf, 64);
        host.InputAnswer = "你好";                 // 6 字节
        regs[0] = tOff; regs[1] = bOff; regs[2] = buf; regs[3] = 64;
        rt.HandleSyscall(VmlUi.DlgInput, regs, mem);
        Check("DlgInput 返回字节数（=6，不是 2）", regs[0] == 6);
        Check("DlgInput 把内容写进了程序内存", VmlHostRuntime.Str(mem, buf) == "你好");
        Check("DlgInput 补了结尾 NUL", mem[buf + 6] == 0);

        // 取消 → -1，且**不写内存**
        host.InputAnswer = null;
        Array.Clear(mem, buf, 64);
        regs[2] = buf; regs[3] = 64;
        rt.HandleSyscall(VmlUi.DlgInput, regs, mem);
        Check("DlgInput 取消返回 -1 且不写缓冲", regs[0] == -1 && mem[buf] == 0);

        // ── 存档：键加 `vml.` 前缀并清洗（不能污染宿主自己的设置）────────────────────
        var kOff = WriteCStr(mem, 1200, "hi score!");
        var vOff = WriteCStr(mem, 1300, "42");
        regs[0] = kOff; regs[1] = vOff;
        rt.HandleSyscall(VmlUi.StoreSet, regs, mem);
        // 清洗规则（VmlUi.StoreKey）：字母数字与 `._-` 保留、空格换 `_`、**其余字符直接丢**
        Check("存档键加了 vml. 前缀且清洗掉了空格/感叹号",
            host.Store.ContainsKey("vml.hi_score") && host.Store["vml.hi_score"] == "42");

        Array.Clear(mem, buf, 64);
        regs[0] = kOff; regs[1] = buf; regs[2] = 64;
        rt.HandleSyscall(VmlUi.StoreGet, regs, mem);
        Check("StoreGet 返回长度并写回缓冲", regs[0] == 2 && VmlHostRuntime.Str(mem, buf) == "42");

        regs[0] = kOff;
        rt.HandleSyscall(VmlUi.StoreDel, regs, mem);
        Check("StoreDel 删掉了", !host.Store.ContainsKey("vml.hi_score_"));

        // ── 音效 / 震动 ────────────────────────────────────────────────────────
        regs[0] = unchecked((int)0xFFFFFFFF);     // -1ms → 钳到 1
        regs[1] = 999;                            // 强度 → 钳到 255
        rt.HandleSyscall(VmlUi.Vibrate, regs, mem);
        Check("震动时长/强度都被钳过", host.Vibes[^1] == $"{1}/{255}");

        // ── 通用宿主调用口（577–580）：没接上寄存器组时要**回失败码**而不是抛 ────────
        regs[0] = VmlCallIds.HostInfo;
        Check("577 被认领", rt.HandleSyscall(VmlUi.CallWithInt8, regs, mem));
        Check("没接上寄存器组 → 回内部错误码（不抛）", regs[0] == VmlCallRegistry.ErrorInternal);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ③ 内存读写
    // ══════════════════════════════════════════════════════════════════════

    private static void TestVmlMemoryAccess(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 宿主：程序内存读写");

        var mem = new byte[256];

        // 地址是**程序给的**，可以是任何 int（负数、越界、超大）——
        // 一律返回空串，绝不能让宿主抛索引越界（那会把 VM 打挂）。
        Check("负地址读成空串", VmlHostRuntime.Str(mem, -1) == "");
        Check("越界地址读成空串", VmlHostRuntime.Str(mem, 256) == "");
        Check("超大地址读成空串", VmlHostRuntime.Str(mem, int.MaxValue) == "");
        Check("刚好在界内的空串合法", VmlHostRuntime.Str(mem, 255) == "");

        var at = WriteCStr(mem, 10, "abc");
        Check("写进去能原样读出来", VmlHostRuntime.Str(mem, at) == "abc");

        // 没有终止符时不许越界扫描
        var tail = new byte[8];
        for (var i = 0; i < 8; i++) tail[i] = (byte)'x';
        Check("全无 NUL 时读到缓冲末尾就停", VmlHostRuntime.Str(tail, 0) == "xxxxxxxx");

        // 选项块：越界的 count / 地址都不能读坏内存
        var blk = new byte[64];
        WriteCStrSeq(blk, 0, "甲", "乙");
        var opts = VmlHostRuntime.StrBlock(blk, 0, 2);
        Check("选项块按 \\0 切分", opts.Count == 2 && opts[0] == "甲" && opts[1] == "乙");
        Check("负数 count 返回空表", VmlHostRuntime.StrBlock(blk, 0, -1).Count == 0);
        Check("越界地址返回空表", VmlHostRuntime.StrBlock(blk, 999, 3).Count == 0);

        // 写串：留一个 NUL、放不下返回 -1
        var dst = new byte[16];
        Check("正常写入返回字节数", VmlHostRuntime.WriteString(dst, 0, 16, "hi") == 2 && dst[2] == 0);
        Check("放不下返回 -1", VmlHostRuntime.WriteString(dst, 0, 2, "hello") == -1);
        Check("容量 1 也放不下（要留 NUL）", VmlHostRuntime.WriteString(dst, 0, 1, "x") == -1);
        Check("越界目标地址返回 -1", VmlHostRuntime.WriteString(dst, 15, 8, "x") == -1);
        Check("负目标地址返回 -1", VmlHostRuntime.WriteString(dst, -1, 8, "x") == -1);
    }

    /// <summary>把 C 串写进内存，返回**它自己的偏移**（当作指针用）。</summary>
    /// <summary>
    /// **像素读回**（583–585）：`floodfill` / `getimage` / `putimage`。
    ///
    /// <para>
    /// 这一批是**老 graphics.h 程序的命脉** —— 填充与精灵都绕不开读像素
    /// （实测语料里 `floodfill` 出现 10 次、`getimage`/`putimage` 8 次）。
    /// 而场景是**保留模式**的（只有图元、没有像素缓冲），所以三个号在宿主侧都要
    /// **先光栅化一次**才能回答"这个像素是什么颜色"。
    /// </para>
    ///
    /// <para>
    /// ⚠ 判据必须落到**像素**上，不能只看"调用返回了 1"：`floodfill` 最容易出的错是
    /// **填错地方**（种子点判错、边界色比错、游程坐标算反），而那几种错法**返回值都正常**。
    /// 所以这里用假宿主的**真光栅化**（`DrawRunner`，与两端宿主同一条路）把画面读回来逐点比。
    /// </para>
    /// </summary>
    private static void TestVmlPixelReadback(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 宿主：像素读回（floodfill / getimage / putimage）");

        var host = new FakeVmlHost();
        var rt = new VmlHostRuntime(host);
        var regs = new int[32];
        var mem = new byte[4096];

        // 开一个 16×16 的窗（走真实 syscall 路径，场景由运行时建）
        regs[0] = WriteCStr(mem, 0, "readback");
        regs[1] = 16; regs[2] = 16; regs[3] = 0; regs[4] = 0;
        rt.HandleSyscall(VmlUi.WinOpen, regs, mem);
        var scene = rt.Scene();
        if (scene is null) { Check("开窗拿到场景", false); return; }
        scene.Width = 16; scene.Height = 16;

        const uint BORDER = 0xFFFF0000;   // 红边
        const uint FILL = 0xFF00FF00;     // 绿填充

        // 沿四边铺一圈红色 —— 中间 14×14 是"里面"
        scene.AddFilledRun(0, 0, 16, 1, BORDER);
        scene.AddFilledRun(0, 15, 16, 1, BORDER);
        scene.AddFilledRun(0, 1, 1, 14, BORDER);
        scene.AddFilledRun(15, 1, 1, 14, BORDER);

        // ── floodfill ────────────────────────────────────────────────────────
        regs[0] = 8; regs[1] = 8;                       // 种子点（框内）
        regs[2] = unchecked((int)FILL);                 // 填充色
        regs[3] = unchecked((int)BORDER);               // 边界色
        Check("FLOOD_FILL 被宿主认领", rt.HandleSyscall(VmlUi.FloodFill, regs, mem));
        Check($"框内 14×14 落成 14 条游程（实得 {regs[0]}）", regs[0] == 14);

        // **像素判据**：重新光栅化，逐点看框内是不是填成了绿色、框上还是红的
        var buf = new byte[16 * 16 * 4];
        if (!host.Rasterize(0, 0, 16, 16, buf))
        {
            Check("假宿主能光栅化（自测装置本身）", false);
        }
        else
        {
            bool InsideGreen(int x, int y)
            {
                int i = (y * 16 + x) * 4;
                return buf[i] == 0x00 && buf[i + 1] == 0xFF && buf[i + 2] == 0x00;
            }
            bool EdgeRed(int x, int y)
            {
                int i = (y * 16 + x) * 4;
                return buf[i] == 0xFF && buf[i + 1] == 0x00 && buf[i + 2] == 0x00;
            }

            Check("框内被填成填充色（逐像素）",
                InsideGreen(8, 8) && InsideGreen(1, 1) && InsideGreen(14, 14));
            Check("边界**没有被越过**（框上仍是边框色）",
                EdgeRed(0, 0) && EdgeRed(8, 0) && EdgeRed(0, 8) && EdgeRed(15, 15));
        }

        // 种子点在边界色上 ⇒ BGI 语义是"什么都不做"（不是错误）
        regs[0] = 0; regs[1] = 0; regs[2] = unchecked((int)FILL); regs[3] = unchecked((int)BORDER);
        rt.HandleSyscall(VmlUi.FloodFill, regs, mem);
        Check("种子点在边界上 ⇒ 不填（返回 0）", regs[0] == 0);

        // ── getimage / putimage ─────────────────────────────────────────────
        regs[0] = 0; regs[1] = 0; regs[2] = 4; regs[3] = 4;
        rt.HandleSyscall(VmlUi.GetImage, regs, mem);
        int handle = regs[0];
        Check($"GET_IMAGE 返回句柄（实得 {handle}）", handle >= 1);

        int figuresBefore = scene.FigureCount;
        regs[0] = 4; regs[1] = 4; regs[2] = handle; regs[3] = 0;    // COPY
        rt.HandleSyscall(VmlUi.PutImage, regs, mem);
        Check("PUT_IMAGE(COPY) 成功并在场景里落了一笔", regs[0] == 1 && scene.FigureCount > figuresBefore);

        // 句柄不认识 ⇒ 必须失败（**不能拿别的块碰运气贴上去**）
        regs[0] = 4; regs[1] = 4; regs[2] = 9999; regs[3] = 0;
        rt.HandleSyscall(VmlUi.PutImage, regs, mem);
        Check("不认识的句柄 ⇒ 返回 0 且不落笔", regs[0] == 0);

        // 尺寸非法 ⇒ GET_IMAGE 返回 0（不是崩）
        regs[0] = 0; regs[1] = 0; regs[2] = 0; regs[3] = -1;
        rt.HandleSyscall(VmlUi.GetImage, regs, mem);
        Check("GET_IMAGE 尺寸非法 ⇒ 返回 0、不抛", regs[0] == 0);

        // 清理假宿主落的临时图（不清理会在临时目录里越堆越多）
        foreach (var f in host.TempImages) { try { File.Delete(f); } catch { } }
    }

    private static int WriteCStr(byte[] mem, int offset, string s)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        Array.Copy(bytes, 0, mem, offset, Math.Min(bytes.Length, mem.Length - offset - 1));
        mem[offset + bytes.Length] = 0;
        return offset;
    }

    /// <summary>
    /// 连写多个 C 串，返回**第一个的偏移**（当作"选项块"的指针用）。
    ///
    /// ⚠ 与 <see cref="WriteCStr"/> 分成两个方法，是因为返回值含义不同 ——
    /// 一开始只有前者，我拿它当"写到哪儿了"用（`p = WriteCStr(mem, p, s)`），
    /// 于是**每个串都写在同一个偏移上**、后面的把前面的覆盖掉，
    /// 而断言报出来的是"选项块内容不对"（看着像 `StrBlock` 有问题，
    /// 其实是自测自己把数据写坏了）。**返回值有两种含义就别共用一个函数名。**
    /// </summary>
    private static int WriteCStrSeq(byte[] mem, int offset, params string[] items)
    {
        var at = offset;
        foreach (var s in items)
        {
            WriteCStr(mem, at, s);
            at += System.Text.Encoding.UTF8.GetByteCount(s) + 1;   // 串本身 + 结尾 NUL
        }
        return offset;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 主动截屏（#588）
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// **路径清洗是防逃逸的唯一屏障**（两端的 `ResolvePath` 都不做钳制），
    /// 所以这一节的每一条都当安全断言看，不是"格式整理"。
    ///
    /// 另一半是**端到端**：真截（假宿主给一张合成图）→ 真编码 → 真落盘 →
    /// 核对 PNG 魔数、长度、以及"路径非法时**一个字节都不落盘**"。
    /// </summary>
    private static void TestVmlScreenshot(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 宿主：主动截屏（#588）");

        // ── 一、路径清洗（纯逻辑）────────────────────────────────────────────
        // 拒绝：回退段 / 绝对路径 / 盘符或 scheme。三种都必须整体拒绝，
        // **不能"就地消解"**（悄悄改掉用户给的路径比直接拒绝更难排查）。
        Check("拒绝 `..`", VmlUi.SanitizeShotPath("../x.png") is null);
        Check("拒绝路径中间的 `..`", VmlUi.SanitizeShotPath("a/../../b.png") is null);
        // 绝对路径是**剥掉首部分隔符**、不是拒绝（与 `SandboxPath` 同一口径：沙箱内只有相对路径）。
        // 结果仍是相对路径 ⇒ 逃逸不了，所以这条要盯的是"剥干净了"，不是"拒了"。
        Check("绝对路径剥成相对（`/etc/passwd` → `etc/passwd.png`）",
            VmlUi.SanitizeShotPath("/etc/passwd") == "etc/passwd.png");
        Check("拒绝 Windows 盘符", VmlUi.SanitizeShotPath("C:\\Windows\\x.png") is null);
        Check("拒绝 scheme（含冒号）", VmlUi.SanitizeShotPath("http://x/y.png") is null);
        Check("拒绝单个 `.` 段", VmlUi.SanitizeShotPath("./x.png") is null);
        Check("反斜杠当分隔符时 `..` 同样被拒", VmlUi.SanitizeShotPath("..\\x.png") is null);

        // 接受与规范化
        Check("空串 → 默认名", VmlUi.SanitizeShotPath("") == VmlUi.DefaultShotName);
        Check("纯空白 → 默认名", VmlUi.SanitizeShotPath("   ") == VmlUi.DefaultShotName);
        Check("无扩展名 → 补 .png", VmlUi.SanitizeShotPath("shot") == "shot.png");
        Check("有扩展名 → 原样", VmlUi.SanitizeShotPath("a.png") == "a.png");
        Check("子目录保留（分隔符归一成 /）",
            VmlUi.SanitizeShotPath("shots\\1") == "shots/1.png");
        Check("重复分隔符不产生空段", VmlUi.SanitizeShotPath("a//b.png") == "a/b.png");

        // ── 二、端到端（真落盘）──────────────────────────────────────────────
        var dir = Path.Combine(Path.GetTempPath(), $"wcvml-shot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var host = new FakeVmlHost { ResolvePrefix = dir + Path.DirectorySeparatorChar };
            var rt = new VmlHostRuntime(host);
            var regs = new int[32];
            var mem = new byte[4096];

            // 一张 2×2 的合成图：像素 (1,0) 是纯红 —— 用来核对 PNG 里的字节顺序
            var pixels = new byte[2 * 2 * 4];
            pixels[4] = 0xFF; pixels[5] = 0x00; pixels[6] = 0x00; pixels[7] = 0xFF;  // (1,0) = 红
            host.CaptureAnswer = new RasterImage(2, 2, pixels);

            regs[0] = WriteCStr(mem, 0, "shot");
            Check("SCREENSHOT 被宿主认领", rt.HandleSyscall(VmlUi.Screenshot, regs, mem));

            var file = Path.Combine(dir, "shot.png");
            Check("文件真的落盘了（无扩展名自动补 .png）", File.Exists(file));
            if (File.Exists(file))
            {
                var bytes = File.ReadAllBytes(file);
                Check($"返回长度 == 文件长度（返 {regs[0]} / 实 {bytes.Length}）", regs[0] == bytes.Length);
                Check("内容是 PNG（魔数）",
                    bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50
                    && bytes[2] == 0x4E && bytes[3] == 0x47);
                // 解码回来逐点核对：证明"通道序没搞反"，而不只是"写出了一坨字节"
                var back = PngDecoder.Decode(bytes);
                Check($"解码回 2×2（实得 {back.Width}×{back.Height}）",
                    back.Width == 2 && back.Height == 2);
                Check("红色像素落在 (1,0) 且是 RGBA 序（通道没反）",
                    back.Rgba[4] == 0xFF && back.Rgba[5] == 0x00
                    && back.Rgba[6] == 0x00 && back.Rgba[7] == 0xFF);
            }

            // 子目录：不先建目录就会抛，这条钉住"建目录"那一步
            regs[0] = WriteCStr(mem, 64, "shots/1");
            rt.HandleSyscall(VmlUi.Screenshot, regs, mem);
            Check("子目录里的图也能落盘", File.Exists(Path.Combine(dir, "shots", "1.png")));

            // ── 三、失败路径 ─────────────────────────────────────────────────
            // ① 路径非法 ⇒ **压根不去截**，也不落盘
            host.CaptureCalls = 0;
            regs[0] = WriteCStr(mem, 128, "../escape.png");
            rt.HandleSyscall(VmlUi.Screenshot, regs, mem);
            Check("非法路径 ⇒ -1", regs[0] == -1);
            Check("非法路径 ⇒ 连截都没截（CaptureCalls == 0）", host.CaptureCalls == 0);
            Check("非法路径 ⇒ 没有产生任何文件", !File.Exists(Path.Combine(dir, "..", "escape.png")));

            // ② 这一端截不了 ⇒ -1（能力缺失，不是错误）
            host.CaptureAnswer = null;
            regs[0] = WriteCStr(mem, 192, "nocap.png");
            rt.HandleSyscall(VmlUi.Screenshot, regs, mem);
            Check("本端无截屏能力 ⇒ -1", regs[0] == -1);
            Check("无能力时不留下空文件", !File.Exists(Path.Combine(dir, "nocap.png")));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }

        // 号段表里有它（漏加 = 那道查重护栏形同虚设）
        Check("#588 已登记进 AllNumbers", Array.IndexOf(VmlUi.AllNumbers, VmlUi.Screenshot) >= 0);
    }
}

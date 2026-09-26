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
        TestVmlWaitRenew(Section, Check, Fail);
        TestVmlStatusLines(Section, Check, Fail);
        TestVmlMaskBool(Section, Check, Fail);
        TestVmlMobileOps(Section, Check, Fail);
    }

    /// <summary>
    /// **超时续期只挂在"真的等过"的路径上。**
    ///
    /// <para>
    /// 这一对断言就是"游戏能一直玩下去"的护栏：`ui_wait_msg` 等完必须续期 ——
    /// 否则程序每 40ms 等一次、而超时却在后台按**墙钟**走，120 秒必被杀；
    /// 被杀之后窗口留在最后一帧（宿主那时已经不关窗了，见 `MauiVml` 的收尾），
    /// 用户看到的就是"游戏卡死、触摸没反应"（真机实测 `gorilla.bas` 报的正是这个）。
    /// </para>
    /// <para>
    /// ⚠ **两个断言必须成对**：只测"Wait 会续期"的话，将来谁顺手把 <c>OnWaitEnded</c>
    /// 也挂到 `Poll` 上（看起来更"完整"），看门狗就永远不触发了，而测试照样全绿。
    /// </para>
    /// </summary>
    private static void TestVmlWaitRenew(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 宿主：超时续期只在阻塞路径上");

        var rt = new VmlHostRuntime(new FakeVmlHost());
        var regs = new int[32];
        var mem = new byte[4096];

        int renewed = 0;
        rt.OnWaitEnded = () => renewed++;

        // `ui_wait_msg(目标, 超时ms, 保留)` —— 队列空，1ms 后超时返回。**这条路径算等过。**
        regs[0] = VmlUi.MsgOp.Wait; regs[1] = 0; regs[2] = 1; regs[3] = 0;
        rt.HandleSyscall(VmlUi.Msg, regs, mem);
        Check("ui_wait_msg 返回后触发续期", renewed == 1);

        // `ui_poll` 是非阻塞的：**一次都不许续期**（程序每帧都调它）
        renewed = 0;
        regs[0] = VmlUi.MsgOp.Poll; regs[1] = 0;
        rt.HandleSyscall(VmlUi.Msg, regs, mem);
        Check("ui_poll 不触发续期（否则超时永不触发）", renewed == 0);

        // 与窗态判据配对：`ui_win_close` 之后宿主必须认为"窗口没了"，
        // 否则程序结束时的收尾关窗会去关一个已经关掉的窗口。
        Check("开窗前 WindowOpen 为假", !rt.WindowOpen);
        regs[0] = WriteCStr(mem, 0, "窗"); regs[1] = 100; regs[2] = 100; regs[3] = 0; regs[4] = 0;
        rt.HandleSyscall(VmlUi.WinOpen, regs, mem);
        Check("开窗后 WindowOpen 为真", rt.WindowOpen);
        rt.HandleSyscall(VmlUi.WinClose, regs, mem);
        Check("程序自己关窗后 WindowOpen 为假", !rt.WindowOpen);
    }

    /// <summary>
    /// 状态面板的**格式化**（<see cref="VmlStatusSnapshot.FormatLines"/>）。
    ///
    /// 测的全是"看着对、其实错一格"的东西：十六进制补零、标志位的 `+/-`、尺寸单位换算、
    /// 行宽补齐（`R0` 与 `R12` 名字长度不同，补错了整行串位），以及**空快照不许抛** ——
    /// 面板在"没跑任何程序"时也会被打开，那是最容易漏的一条路径。
    /// </summary>
    private static void TestVmlStatusLines(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VM 状态面板：格式化");

        var empty = new VmlStatusSnapshot().FormatLines();
        Check("空闲快照不抛且有提示", empty.Length == 1 && empty[0].Contains("空闲"));

        var s = new VmlStatusSnapshot
        {
            HasVm = true, Running = true, PrivilegeLevel = 1,
            Pc = 0x120A3C,
            Registers = [0, 0x0C, 0xFF100, 1, 0, 0, 0, 3, 0, 0, 0, 0, 0xFEFF8, 0xFF000, 0x1208A0, 0],
            FloatRegisters = [0f, 1.5f],
            DoubleRegisters = [0d],
            LongRegisters = [7L],
            Zf = true, Cf = false, Sf = true,
            InstructionsExecuted = 1234567, SyscallsExecuted = 42,
            MemoryBytes = 16 * 1024 * 1024, StackBytes = 1024 * 1024,
            // 占用取整档（4M/16M = 25%、256K/1M = 25%）—— 用整齐的数才验得出"百分比是算出来的"，
            // 拿 3.2M 那类值写断言只能证明"我按计算器算过一遍"
            MemoryUsedBytes = 4 * 1024 * 1024, StackUsedBytes = 256 * 1024,
            TimeoutSeconds = 120, TimeoutRemainingSeconds = 87,
            HasScene = true, WindowTitle = "大猩猩", SceneWidth = 376, SceneHeight = 610,
            FigureCount = 253, FrameNumber = 1234, Fps = 17.6,
        };
        var lines = s.FormatLines();
        var text = string.Join("\n", lines);

        Check("PC 补足八位十六进制", text.Contains("PC 00120A3C"));
        Check("SP 取 R13、FP 取 R12（别按 R14/R15 认）",
            text.Contains("SP 000FF000") && text.Contains("FP 000FEFF8"));
        // 名字补到 3 再加一个分隔空格 ⇒ 前缀恒 5 字符，所以 R0 后面是 3 个空格、R12 后面是 2 个：
        // **两个 hex 的起始列相同**才是判据（按 2 补的话 R0 行与 R12 行会差一格、整行串位）
        Check("R0 与 R12 **同一列**对齐", text.Contains("R0   00000000") && text.Contains("R12  000FEFF8"));
        Check("标志位写成 Z+/C-/S+", text.Contains("Z+ C- S+"));
        Check("超时剩余显示出来（排查卡死的第一读数）", text.Contains("超时 120s · 剩 87s"));
        Check("内存/栈显示**占用百分比**（用户要的那个数）",
            text.Contains("内存 25%（4M/16M）") && text.Contains("栈 25%（256K/1M）"));
        Check("总量未知时给「-」而不是 100%（100% 会被读成「用满了」）",
            new VmlStatusSnapshot { HasVm = true, MemoryUsedBytes = 100, MemoryBytes = 0 }
                .FormatLines().Any(l => l.Contains("内存 -（")));
        Check("图元数与帧率在场景那一行", text.Contains("图元 253") && text.Contains("17.6fps"));
        Check("不限时的程序写「不限」而不是「剩 0s」",
            new VmlStatusSnapshot { HasVm = true, TimeoutSeconds = 0 }.FormatLines()
                .Any(l => l.Contains("超时 不限")));
        Check("行数不失控（面板不该把画面全遮住）", lines.Length <= 16);

        // ── 小窗（`FormatLines(false)`）────────────────────────────────────
        //
        // 判据不是"少了几行"而是**"该在的还在、该走的真走了"**：
        // 少一行容易（删了就少），难的是删完之后 PC/SP 还在、寄存器那四组是真没了。
        // ⚠ 断言 `!text.Contains("R0")` 是**故意挑 R0 而不是 R12** —— `R0` 是 16 个通用寄存器里
        //   唯一不会被别的词偶然命中的名字（`R12` 会撞上……不，其实都不会；但 `R0` 还额外
        //   保证了"逐个寄存器都没了"，因为大窗那四行里它排头一个）。
        var small = s.FormatLines(detailed: false);
        var smallText = string.Join("\n", small);
        Check("小窗留 PC/SP/LR（排查「卡在哪」不能省）",
            smallText.Contains("PC 00120A3C") && smallText.Contains("SP 000FF000") && smallText.Contains("LR 001208A0"));
        Check("小窗砍掉全部寄存器组（R0–R15 / F / D / L）",
            !smallText.Contains("R0 ") && !smallText.Contains("F  0") && !smallText.Contains("D  0") && !smallText.Contains("L  "));
        Check("小窗仍报超时剩余（含这行才算「基本状态」）", smallText.Contains("超时剩 87s"));
        Check("小窗仍报内存/栈占用", smallText.Contains("内存 25%") && smallText.Contains("栈 25%"));
        Check("小窗仍报帧率（还出不出帧）", smallText.Contains("17.6fps"));
        Check("小窗明显比大窗矮（不然「小窗」二字没意义）", small.Length <= 5 && small.Length < lines.Length / 2);
        Check("小窗带 `detailed: false` 与不带参的**默认**是两种形态",
            !string.Equals(smallText, text));

        var smallIdle = new VmlStatusSnapshot().FormatLines(detailed: false);
        Check("小窗空闲时一行且不抛", smallIdle.Length == 1 && smallIdle[0].Contains("空闲"));
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
        /// <summary>光栅化被调了几次 —— 截屏用例断言「路径非法时压根没去光栅化」。</summary>
        public int RasterCalls;

        /// <summary>
        /// 非 0 时：在**光栅化这一刻**把场景改成这个边长 —— 模拟"UI 线程在截屏中途
        /// 改了 `scene.Width/Height`"。真机上那是 40ms 定时器的 `ResizeScene`，
        /// 症状是 `ArgumentException: 像素缓冲长度不足`（实现分几次读尺寸就会被它咬到）。
        /// </summary>
        public int ResizeSceneOnRaster;

        public bool Rasterize(int x, int y, int w, int h, byte[] dest)
        {
            RasterCalls++;
            if (ResizeSceneOnRaster > 0 && Opened is not null)
            {
                Opened.Width = ResizeSceneOnRaster;
                Opened.Height = ResizeSceneOnRaster;
            }
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

        // ── 复音（`Audio` 的 op 4/5/6，v0.96.485）────────────────────────────
        //
        // ⚠ 这里挂的是一个**真的** `VmlToneSynth`（不是记录型替身）。
        //   理由：那样"经 HandleSyscall 走一遍完整的 op → 宿主 → 合成器"这条链
        //   就能被自测断言到底 —— 包括**复音真的叠加了**（RMS 比值），
        //   而不只是"宿主收到了调用"。记录型替身验不到叠加。
        public readonly VmlToneSynth Synth = new();

        public readonly List<string> NoteLog = new();

        public bool NoteOn(int channel, int note, int velocity, int wave)
        {
            NoteLog.Add($"on|{channel}|{note}|{velocity}");
            return Synth.NoteOn(channel, VmlToneSynth.NoteToHz(VmlUi.ClampNote(note)),
                                VmlUi.ClampNote(note), VmlUi.ClampVelocity(velocity), wave);
        }

        public bool NoteOff(int channel, int note)
        {
            NoteLog.Add($"off|{channel}|{note}");
            return Synth.NoteOff(channel, note);
        }

        public int ToneControl(int ctl, int a, int b)
        {
            NoteLog.Add($"ctl|{ctl}|{a}|{b}");
            switch (ctl)
            {
                case VmlUi.AudioCtl.AllNotesOff: Synth.AllNotesOff(); return 0;
                case VmlUi.AudioCtl.Wave: return Synth.SetChannelWave(a, b) ? 0 : -1;
                case VmlUi.AudioCtl.MaxVoices: Synth.MaxVoicesLimit = a; return 0;
                case VmlUi.AudioCtl.Voices: return Synth.ActiveVoices;
                case VmlUi.AudioCtl.Panic: Synth.Panic(); return 0;
                default: return -1;
            }
        }

        public readonly Dictionary<string, string> Store = new(StringComparer.Ordinal);
        public string? StoreGet(string key) => Store.TryGetValue(key, out var v) ? v : null;
        public void StoreSet(string key, string value) => Store[key] = value;
        public void StoreDel(string key) => Store.Remove(key);

        public bool? LastKeepScreenOn;
        public void KeepScreenOn(bool on) => LastKeepScreenOn = on;

        // ── 手机特有的操作方式（§3 P1）—— 记录型：自测只关心"宿主有没有把话传下来"，
        //    真正的平台效果在各自那一端（桌面根本没有屏幕方向/沉浸式这些概念）。
        public bool AudioPlayingFlag;
        public int LastOrientationLock = -1;
        public bool LastImmersive;

        public bool AudioPlaying() => AudioPlayingFlag;
        public void LockOrientation(int mode) => LastOrientationLock = mode;
        public void SetImmersive(bool on) => LastImmersive = on;

        /// <summary>
        /// `ResolvePath` 的前缀。默认 `/fake/` —— 记录型，不指向任何真实目录。
        ///
        /// ⚠ 截屏用例会**真的写文件**，那时必须把它指到临时目录：写到 `/fake/` 会因
        /// 目录不存在而失败，测出来的就不是"截屏逻辑"而是"目录不存在"。
        /// </summary>
        public string ResolvePrefix = "/fake/";

        public string ResolvePath(string relative) => ResolvePrefix + relative;

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
        regs[0] = VmlUi.StoreOp.Set; regs[1] = kOff; regs[2] = vOff;
        rt.HandleSyscall(VmlUi.Store, regs, mem);
        // 清洗规则（VmlUi.StoreKey）：字母数字与 `._-` 保留、空格换 `_`、**其余字符直接丢**
        Check("存档键加了 vml. 前缀且清洗掉了空格/感叹号",
            host.Store.ContainsKey("vml.hi_score") && host.Store["vml.hi_score"] == "42");

        Array.Clear(mem, buf, 64);
        regs[0] = VmlUi.StoreOp.Get; regs[1] = kOff; regs[2] = buf; regs[3] = 64;
        rt.HandleSyscall(VmlUi.Store, regs, mem);
        Check("StoreGet 返回长度并写回缓冲", regs[0] == 2 && VmlHostRuntime.Str(mem, buf) == "42");

        regs[0] = VmlUi.StoreOp.Delete; regs[1] = kOff;
        rt.HandleSyscall(VmlUi.Store, regs, mem);
        Check("StoreDel 删掉了", !host.Store.ContainsKey("vml.hi_score_"));

        // ── 音效 / 震动 ────────────────────────────────────────────────────────
        // ⚠ 参数整体后移一格（`R0` 是操作码）—— 这条断言同时钉住了"移位真的生效"。
        regs[0] = VmlUi.VibrateOp.Simple;
        regs[1] = unchecked((int)0xFFFFFFFF);     // -1ms → 钳到 1
        regs[2] = 999;                            // 强度 → 钳到 255
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
        // ⚠ 空串走的是**空串**（= "没指定，调用方去生成默认名"），不是 null ——
        //   把两者混起来的话，"没给路径"会被当成非法路径直接失败。
        Check("空串 → 空串（没指定，不是失败）", VmlUi.SanitizeShotPath("") == "");
        Check("纯空白 → 空串", VmlUi.SanitizeShotPath("   ") == "");
        Check("只给分隔符 → 空串", VmlUi.SanitizeShotPath("///") == "");
        Check("无扩展名 → 补 .png", VmlUi.SanitizeShotPath("shot") == "shot.png");
        Check("有扩展名 → 原样", VmlUi.SanitizeShotPath("a.png") == "a.png");
        Check("子目录保留（分隔符归一成 /）",
            VmlUi.SanitizeShotPath("shots\\1") == "shots/1.png");
        Check("重复分隔符不产生空段", VmlUi.SanitizeShotPath("a//b.png") == "a/b.png");

        // ── 默认路径（没给路径时）：`shot/<标题>_<日期>_<时间>.png` ──────────
        var t = new DateTime(2026, 9, 24, 10, 25, 30);
        Check("默认路径 = shot/<标题>_<日期>_<时间>.png",
            VmlUi.DefaultShotPath("tetris", t) == "shot/tetris_20260924_102530.png");
        Check("中文标题保留（`char.IsLetterOrDigit('中')` 为真，这里正要它真）",
            VmlUi.DefaultShotPath("五子棋", t) == "shot/五子棋_20260924_102530.png");
        Check("空标题退回 fallback",
            VmlUi.DefaultShotPath("", t) == "shot/shot_20260924_102530.png");
        Check("纯符号标题退回 fallback",
            VmlUi.DefaultShotPath("../", t) == "shot/shot_20260924_102530.png");
        // ⚠ 这条是**安全**断言：标题是程序自己起的，含分隔符时若原样拼进路径
        //   就是一条逃逸通道。清洗后必须是单个安全段（不产生新层级）。
        Check("标题里的路径分隔符被清洗掉（不产生新层级）",
            VmlUi.DefaultShotPath("a/b\\c", t) == "shot/a_b_c_20260924_102530.png");
        Check("标题里的 `..` 出不来",
            !VmlUi.DefaultShotPath("..", t).Contains(".."));
        Check("默认路径整体仍是安全的（过一遍清洗不报错）",
            VmlUi.SanitizeShotPath(VmlUi.DefaultShotPath("t", t)) == "shot/t_20260924_102530.png");

        // 主干清洗的边界。⚠ 两条判据分别是「截断按码点」与「非保留字符换成一个 _」，
        //   别把它们混起来：emoji 不是字母数字 ⇒ **会被换掉**（那是设计，不是 bug），
        //   要钉的是"换成**一个** `_` 而不是被切半成两个替换字符"。
        var stem = VmlUi.SafeFileStem(new string('汉', 80));
        Check($"过长标题截到 {VmlUi.MaxShotStemRunes} 个码点（实得 {stem.EnumerateRunes().Count()}）",
            stem.EnumerateRunes().Count() == VmlUi.MaxShotStemRunes);

        // 扩展 B 汉字（**代理对**，且 `Rune.IsLetterOrDigit` 为真 ⇒ 会被保留）——
        // 截断必须按**码点**：按 `char` 截会把它切半成 U+FFFD（本仓的字符串硬规矩）
        var extB = VmlUi.SafeFileStem(string.Concat(Enumerable.Repeat(char.ConvertFromUtf32(0x20000), 80)));
        Check($"扩展 B 汉字截断后仍是 {VmlUi.MaxShotStemRunes} 个码点（实得 {extB.EnumerateRunes().Count()}）",
            extB.EnumerateRunes().Count() == VmlUi.MaxShotStemRunes);
        Check("截断结果里没有 U+FFFD（没把代理对切半）", !extB.Contains('�'));

        // emoji 不是字母数字 ⇒ 按规则**换成 `_`**（这是设计，不是 bug）。
        // 要钉的是"换成**一个** `_`"，而不是被切半成两个替换字符。
        Check("emoji 换成**一个** `_`（不是保留、也不是切半）",
            VmlUi.SafeFileStem("游戏😀测试") == "游戏_测试");
        Check("连续符号压成一个 `_`，且不留尾随 `_`",
            VmlUi.SafeFileStem("a!!b??") == "a_b");

        // ── 二、端到端（真落盘）──────────────────────────────────────────────
        var dir = Path.Combine(Path.GetTempPath(), $"wcvml-shot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var host = new FakeVmlHost { ResolvePrefix = dir + Path.DirectorySeparatorChar };
            var rt = new VmlHostRuntime(host);
            var regs = new int[32];
            var mem = new byte[4096];

            // 开个 4×4 的窗口，整屏铺**纯红** —— 截图之后按像素核对颜色。
            // 走的是真 syscall（开窗 / 清屏），不是往场景里直接塞图元。
            regs[0] = WriteCStr(mem, 0, "shot");
            regs[1] = 4; regs[2] = 4; regs[3] = 0; regs[4] = 0;
            rt.HandleSyscall(VmlUi.WinOpen, regs, mem);
            var scene = rt.Scene();
            if (scene is null) { Check("开窗拿到场景（装置本身）", false); return; }
            scene.Width = 4; scene.Height = 4;

            regs[0] = unchecked((int)0xFFFF0000u);   // 纯红
            rt.HandleSyscall(VmlUi.DrawClear, regs, mem);

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

                // 解码回来逐点核对：既证明尺寸对，也证明**通道序没搞反**
                //（红线写出的字节必须是 R=FF,G=00,B=00,A=FF，反了就是蓝色）
                var back = PngDecoder.Decode(bytes);
                Check($"解码回 4×4（实得 {back.Width}×{back.Height}）",
                    back.Width == 4 && back.Height == 4);
                Check("像素是 RGBA 序的纯红（通道没反）",
                    back.Rgba[0] == 0xFF && back.Rgba[1] == 0x00
                    && back.Rgba[2] == 0x00 && back.Rgba[3] == 0xFF);
            }

            // 子目录：不先建目录就会抛，这条钉住"建目录"那一步
            regs[0] = WriteCStr(mem, 64, "shots/1");
            rt.HandleSyscall(VmlUi.Screenshot, regs, mem);
            Check("子目录里的图也能落盘", File.Exists(Path.Combine(dir, "shots", "1.png")));

            // ── 特征要保留：粗体 / 斜体 在出图里必须**真的画出来** ─────────────
            // 用户定的口径是"允许与屏幕不同，但**特征不许丢**"，所以这条钉的是出图本身。
            // 用两行 `IIII`（竖笔画串）：斜体是 0.25 切变 ⇒ "斜没斜"**可量化**，不靠肉眼。
            // ⚠ 量法用**墨迹质心**漂移，不用"每行最左一列"——后者是单像素统计，
            //   抗锯齿抖一两像素就能报出 ±0.15 的假斜度（网格体检那一版就误报过）。
            var sPtr = WriteCStr(mem, 512, "IIII");
            void DrawStyled(int x, int y, int style)
            {
                var rr = new int[32];
                rr[0] = x; rr[1] = y; rr[2] = sPtr;
                rr[3] = unchecked((int)0xFFFFFFFFu); rr[4] = 20; rr[5] = 0;
                rr[6] = style; rr[7] = 0;
                rt.HandleSyscall(VmlUi.DrawText, rr, mem);
            }
            // ⚠ 场景尺寸要先放大 —— 上面那个用例把场景改成了 4×4，不放大什么都画不进去。
            scene.Width = 48; scene.Height = 48;

            // **一图一行**：每次清屏后只画一行，再对**整张图**量斜度。
            // ⚠ 别在**同一张图**里按 y 划带来分别量 —— `#528` 的 `y` 是**基线**
            //   （不是顶线），按顶线猜带边界必然错位（实测就错了两轮）。
            //   少一个假设，就少一处会错的地方。
            double SlantOfOneLine(int style, string file)
            {
                regs[0] = unchecked((int)0xFF000000u);   // 清成黑底：白字才好判"是不是墨迹"
                rt.HandleSyscall(VmlUi.DrawClear, regs, mem);
                DrawStyled(4, 40, style);                // y=基线 ⇒ 字形落在 y≈20..40

                regs[0] = WriteCStr(mem, 640, file);
                rt.HandleSyscall(VmlUi.Screenshot, regs, mem);

                var path = Path.Combine(dir, file);
                if (!File.Exists(path)) return double.NaN;
                var img = PngDecoder.Decode(File.ReadAllBytes(path));

                var rows = new List<(int Y, double Cx, int N)>();
                for (int y = 0; y < img.Height; y++)
                {
                    double sx2 = 0; int n = 0;
                    for (int x = 0; x < img.Width; x++)
                    {
                        int o = (y * img.Width + x) * 4;
                        // 墨迹 = **白字**（三个通道都亮）。⚠ 别按单通道判：
                        // 底色若不是黑，`R>110` 会把底色一并算进来 ⇒ 质心恒居中 ⇒ 斜度恒 0（踩过）
                        if (img.Rgba[o] > 110 && img.Rgba[o + 1] > 110 && img.Rgba[o + 2] > 110)
                        { sx2 += x; n++; }
                    }
                    if (n > 0) rows.Add((y, sx2 / n, n));
                }
                if (rows.Count < 4) return double.NaN;
                var solid = rows.Where(t => t.N >= 2).ToList();
                if (solid.Count < 4) solid = rows;
                int k = Math.Max(1, solid.Count / 3);
                double dy = solid[^1].Y - solid[0].Y;
                return dy <= 0 ? double.NaN
                    : (solid.Skip(solid.Count - k).Average(t => t.Cx)
                       - solid.Take(k).Average(t => t.Cx)) / dy;
            }

            double upright = SlantOfOneLine(0, "feat_upright.png");
            double italic = SlantOfOneLine(2, "feat_italic.png");
            Check($"常规文字出图**不斜**（斜度 {upright:+0.00;-0.00;0.00}）",
                !double.IsNaN(upright) && Math.Abs(upright) < 0.10);
            Check($"斜体文字出图**真的斜**（斜度 {italic:+0.00;-0.00;0.00}，应 ≈0.25）",
                !double.IsNaN(italic) && italic > 0.15);

            // 没给路径（空串）⇒ 自动命名 `shot/<标题>_<日期>_<时间>.png`
            regs[0] = WriteCStr(mem, 256, "");
            rt.HandleSyscall(VmlUi.Screenshot, regs, mem);
            var autoDir = Path.Combine(dir, VmlUi.DefaultShotDir);
            var auto = Directory.Exists(autoDir) ? Directory.GetFiles(autoDir, "shot_*.png") : [];
            Check($"空路径 ⇒ 自动命名到 {VmlUi.DefaultShotDir}/（实得 {auto.Length} 个）", auto.Length == 1);

            // ── 跨线程改尺寸（真机踩到的那个 ArgumentException）──────────────
            // `DrawWindowPage` 的 40ms 定时器会在**别的线程**上按实测视口改
            // `scene.Width/Height`。实现若分几次读它（分配缓冲读一次、编码校验再读一次），
            // 中间被改一次就是 `像素缓冲长度不足`。
            // 这里让假宿主在**光栅化那一刻**把场景撑大 —— 一个确定性的复现。
            regs[0] = WriteCStr(mem, 320, "race.png");
            host.ResizeSceneOnRaster = 40;   // 光栅化时把场景改成 40×40
            rt.HandleSyscall(VmlUi.Screenshot, regs, mem);
            host.ResizeSceneOnRaster = 0;
            Check("光栅化期间场景被改尺寸 ⇒ 不抛、按**快照**的尺寸出图（不炸在 PngEncoder）",
                File.Exists(Path.Combine(dir, "race.png")));

            // ── 三、失败路径 ─────────────────────────────────────────────────
            // ① 路径非法 ⇒ **压根不去光栅化**，也不落盘
            host.RasterCalls = 0;
            regs[0] = WriteCStr(mem, 128, "../escape.png");
            rt.HandleSyscall(VmlUi.Screenshot, regs, mem);
            Check("非法路径 ⇒ -1", regs[0] == -1);
            Check("非法路径 ⇒ 连光栅化都没做（RasterCalls == 0）", host.RasterCalls == 0);
            Check("非法路径 ⇒ 没有产生任何文件", !File.Exists(Path.Combine(dir, "..", "escape.png")));

            // ② 还没开窗（没有场景）⇒ -1，且不留空文件
            var bare = new VmlHostRuntime(new FakeVmlHost { ResolvePrefix = dir + Path.DirectorySeparatorChar });
            var regs2 = new int[32];
            regs2[0] = WriteCStr(mem, 192, "nowin.png");
            bare.HandleSyscall(VmlUi.Screenshot, regs2, mem);
            Check("没开窗 ⇒ -1", regs2[0] == -1);
            Check("没开窗时不留下空文件", !File.Exists(Path.Combine(dir, "nowin.png")));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }

        // 号段表里有它（漏加 = 那道查重护栏形同虚设）
        Check("#588 已登记进 AllNumbers", Array.IndexOf(VmlUi.AllNumbers, VmlUi.Screenshot) >= 0);
    }

    /// <summary>
    /// **手机特有的操作方式**（#556–559 / #547）。
    ///
    /// <para>
    /// ⚠ 判据是"**宿主把话传对了没有**"，不是"屏幕上真的锁了方向" —— 后者只有真机能验，
    /// 而这一层的价值在于：**共享层这一份两端编的是同一个**（桌面 vmlcli 与手机
    /// `MauiVmlHost` 都调它），所以这里对了，两端的行为就一致。
    /// </para>
    ///
    /// <para>
    /// ⚠ 触摸那条最有价值的断言是"**两份状态同源**"：平台只有 `PostTouch` 一个入口，
    /// 它必须**同时**更新状态（给轮询）**和**投一条消息（给事件驱动）。
    /// 少任何一半都有一整类程序用不了 —— 而只测其中一半的话，另一半漏了照样全绿。
    /// </para>
    /// </summary>
    private static void TestVmlMobileOps(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 宿主：手机特有的操作方式（触摸 / 按键 / 方向 / 沉浸 / BGM）");

        var host = new FakeVmlHost();
        var rt = new VmlHostRuntime(host);
        var regs = new int[32];
        var mem = new byte[4096];

        // 内存按**小端**手工解码 —— 与 `WriteInt32` 对称，不依赖宿主机的端序
        static int Rd(byte[] m, int at)
            => m[at] | (m[at + 1] << 8) | (m[at + 2] << 16) | (m[at + 3] << 24);

        bool Touch(int slot, out int x, out int y, out int down)
        {
            Array.Clear(regs);
            regs[0] = slot; regs[1] = 100;      // out 缓冲区放在 mem[100]
            var claimed = rt.HandleSyscall(VmlUi.TouchQuery, regs, mem);
            x = Rd(mem, 100); y = Rd(mem, 104); down = Rd(mem, 108);
            return claimed && regs[0] == 1;
        }

        // ── 多点触控 ──────────────────────────────────────────────────────
        rt.PostTouch(0, 111, 222, true);
        rt.PostTouch(1, 333, 444, true);
        rt.PostTouch(1, 333, 444, false);

        Check("TOUCH_QUERY：槽位 0 的坐标与按下状态",
            Touch(0, out var x0, out var y0, out var d0) && x0 == 111 && y0 == 222 && d0 == 1);
        Check("TOUCH_QUERY：槽位 1 已抬起（坐标还在）",
            Touch(1, out var x1, out var y1, out var d1) && x1 == 333 && y1 == 444 && d1 == 0);
        Check("TOUCH_QUERY：没碰过的槽位全零（不是脏数据）",
            Touch(2, out var x2, out _, out var d2) && x2 == 0 && d2 == 0);
        Check("TOUCH_QUERY：槽位越界返回 0", !Touch(99, out _, out _, out _));

        // 移动：坐标更新、仍然按着
        rt.PostTouchMove(0, 150, 260);
        Check("TOUCH_QUERY：移动后坐标更新，仍是按着",
            Touch(0, out var xm, out var ym, out var dm) && xm == 150 && ym == 260 && dm == 1);
        // 没按着的手指报移动 ⇒ 忽略（平台偶尔补发，不该把"抬起"变回"按着"）
        rt.PostTouchMove(1, 999, 999);
        Check("TOUCH_QUERY：抬起的手指报移动会被忽略",
            Touch(1, out var xs, out _, out var ds) && xs == 333 && ds == 0);

        // ⚠ **两份状态同源**：`PostTouch` 必须同时投一条触摸消息
        int Poll()
        {
            Array.Clear(regs);
            regs[0] = VmlUi.MsgOp.Poll;
            rt.HandleSyscall(VmlUi.Msg, regs, mem);
            return regs[0];
        }
        while (Poll() != 0) { }                       // 清掉上面那些
        rt.PostTouch(0, 7, 8, true);
        Check("槽位 0 同时投了一条 TOUCHDOWN 消息（老程序照旧收得到）",
            Poll() == (int)VmlMsgType.TouchDown);
        // ⚠ 别的槽位**只更新状态、不投消息**：消息通道是单指语义，
        //   把第 2 根手指也投进去会让老程序把一次双指操作算成两次点击。
        while (Poll() != 0) { }
        rt.PostTouch(1, 9, 9, true);
        Check("槽位 1 不投消息（多指只走查询，不干扰老程序）", Poll() == 0);
        Check("但槽位 1 的状态照样能查到",
            Touch(1, out var x1b, out var y1b, out var d1b) && x1b == 9 && y1b == 9 && d1b == 1);

        // ── 按键查询 ──────────────────────────────────────────────────────
        int KeyQ(int k)
        {
            Array.Clear(regs);
            regs[0] = k;
            rt.HandleSyscall(VmlUi.KeyQuery, regs, mem);
            return regs[0];
        }

        Check("KEY_QUERY：没按时为 0", KeyQ(37) == 0);
        rt.PostInput(VmlMsgType.KeyDown, 37);
        Check("KEY_QUERY：按下之后为 1", KeyQ(37) == 1);
        Check("KEY_QUERY：别的键不受影响", KeyQ(38) == 0);
        rt.PostInput(VmlMsgType.KeyUp, 37);
        Check("KEY_QUERY：抬起之后回到 0", KeyQ(37) == 0);

        // ── 方向锁 / 沉浸式 / BGM：转发给平台 ─────────────────────────────
        Array.Clear(regs); regs[0] = 1;
        Check("ORIENTATION_LOCK 被认领", rt.HandleSyscall(VmlUi.OrientationLock, regs, mem));
        Check("ORIENTATION_LOCK 原样转发给平台（横屏=1）", host.LastOrientationLock == 1);

        Array.Clear(regs); regs[0] = 1;
        rt.HandleSyscall(VmlUi.Immersive, regs, mem);
        Check("IMMERSIVE 转发给平台（开）", host.LastImmersive);
        Array.Clear(regs); regs[0] = 0;
        rt.HandleSyscall(VmlUi.Immersive, regs, mem);
        Check("IMMERSIVE 转发给平台（关）", !host.LastImmersive);

        host.AudioPlayingFlag = true;
        Array.Clear(regs); regs[0] = VmlUi.AudioOp.IsPlaying;
        rt.HandleSyscall(VmlUi.Audio, regs, mem);
        Check("AUDIO_IS_PLAYING：平台说在放 ⇒ 1", regs[0] == 1);
        host.AudioPlayingFlag = false;
        Array.Clear(regs); regs[0] = VmlUi.AudioOp.IsPlaying;
        rt.HandleSyscall(VmlUi.Audio, regs, mem);
        Check("AUDIO_IS_PLAYING：平台说没放 ⇒ 0", regs[0] == 0);

        // 号段表里有它们（漏登记 = 那道查重护栏形同虚设）
        Check("新号已登记进 AllNumbers",
            Array.IndexOf(VmlUi.AllNumbers, VmlUi.TouchQuery) >= 0
            && Array.IndexOf(VmlUi.AllNumbers, VmlUi.KeyQuery) >= 0
            && Array.IndexOf(VmlUi.AllNumbers, VmlUi.OrientationLock) >= 0
            && Array.IndexOf(VmlUi.AllNumbers, VmlUi.Immersive) >= 0
            && Array.IndexOf(VmlUi.AllNumbers, VmlUi.Audio) >= 0);
    }

    /// <summary>
    /// **蒙版与布尔运算**（`ui_gfx` op 3/4/5 与 op 11）。
    ///
    /// <para>
    /// 判据分两层，两层都要：**纯逻辑**（直接构造 `MaskExpr` 求值 + 编解码往返）与
    /// **端到端**（走真实 syscall 把形状写进场景、再用假宿主的光栅化读像素）。
    /// 纯逻辑绿而端到端红 ⇒ "DSL 那一跳"漏了；反过来 ⇒ 判定本身写错。两者的修法完全不同。
    /// </para>
    ///
    /// <para>
    /// ⚠ 布尔运算的错法**全都是不报错的错法**：`XOR` 写成并集、`SUBTRACT` 写成交集，
    /// 画面上只是"多一块/少一块"，而返回值、图元数、DSL 文本**一切正常**。
    /// 所以每一条都落到"某个具体坐标该不该是那个色"上，绝不看"跑没跑通"。
    /// </para>
    ///
    /// <para>
    /// ⚠ 采样点一律取在**离边界 5px 以上**的地方：判据不该被抗锯齿的边缘像素左右
    /// （`BlendPixel` 是按覆盖率混的，边上一像素本来就不是纯色）。
    /// </para>
    /// </summary>
    private static void TestVmlMaskBool(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 宿主：蒙版与布尔运算");

        const uint FILL = 0xFF3C6EB4;

        // ── 一、纯逻辑：段链的求值 ─────────────────────────────────────────
        // 两个交叠的方框，取三个点：**只在 A** / **两者都在** / **只在 B**。
        // 三个点足以把五种运算两两区分开 —— 少一个点就会有"两种运算结果一样"的盲区。
        var boxA = MaskShape.Rect(0, 0, 10, 10);
        var boxB = MaskShape.Rect(5, 0, 10, 10);

        MaskExpr Build(bool inside, params (int Op, MaskShape[] Shapes)[] segs)
        {
            var e = new MaskExpr { Inside = inside };
            foreach (var (op, shapes) in segs)
            {
                var s = new MaskExpr.Segment { Op = op };
                s.Shapes.AddRange(shapes);
                e.Segments.Add(s);
            }
            return e;
        }

        var replace = Build(true, (MaskOp.Replace, new[] { boxA }));
        Check("REPLACE：A 内命中、B 独有处不命中",
            replace.Hit(2, 5) && replace.Hit(7, 5) && !replace.Hit(12, 5));

        var union = Build(true, (MaskOp.Replace, new[] { boxA }), (MaskOp.Union, new[] { boxB }));
        Check("UNION：三处全命中", union.Hit(2, 5) && union.Hit(7, 5) && union.Hit(12, 5));

        var inter = Build(true, (MaskOp.Replace, new[] { boxA }), (MaskOp.Intersect, new[] { boxB }));
        Check("INTERSECT：只有重叠处命中", !inter.Hit(2, 5) && inter.Hit(7, 5) && !inter.Hit(12, 5));

        var sub = Build(true, (MaskOp.Replace, new[] { boxA }), (MaskOp.Subtract, new[] { boxB }));
        Check("SUBTRACT：A 减去 B", sub.Hit(2, 5) && !sub.Hit(7, 5) && !sub.Hit(12, 5));

        var xor = Build(true, (MaskOp.Replace, new[] { boxA }), (MaskOp.Xor, new[] { boxB }));
        Check("XOR：重叠处互相抵消", xor.Hit(2, 5) && !xor.Hit(7, 5) && xor.Hit(12, 5));

        // `inside=0` 与 SUBTRACT **正交**：先算完整条布尔链，最后整体取反。
        // 这条是文档 §10.2 要求"定清楚"的那一条 —— 两种写法（老式 inside=0 挖洞 /
        // 新式 SUBTRACT 挖洞）给出**同样的画面**，那是等价表达，不是两种语义。
        var outside = Build(false, (MaskOp.Replace, new[] { boxA }));
        Check("inside=0：整体取反（形状外可见）", !outside.Hit(2, 5) && outside.Hit(12, 5));

        var outsideSub = Build(false, (MaskOp.Replace, new[] { boxA }), (MaskOp.Subtract, new[] { boxB }));
        Check("inside=0 + SUBTRACT = 非(A 减 B)",
            !outsideSub.Hit(2, 5) && outsideSub.Hit(7, 5) && outsideSub.Hit(12, 5));

        // 首段忽略运算符（它是链的起点）：`UNION` 打头不等于"和空集取并"以外的任何东西
        var headUnion = Build(true, (MaskOp.Union, new[] { boxA }));
        Check("首段的运算符被忽略（链从它开始）",
            headUnion.Hit(2, 5) && headUnion.Hit(7, 5) && !headUnion.Hit(12, 5));

        // ── 二、纯逻辑：空蒙版的语义 ───────────────────────────────────────
        Check("没有段 ⇒ 空蒙版", new MaskExpr().IsEmpty);
        // ⚠ `ui_mask_clear` 的实现是"开一个空的再收" ⇒ 产出**一个空段**。
        //   若按"段数为 0"判空，取消蒙版会变成"一个空蒙版"= 整屏什么都画不出来。
        Check("只有一个空段 ⇒ 空蒙版（ui_mask_clear 的产物）",
            Build(true, (MaskOp.Replace, Array.Empty<MaskShape>())).IsEmpty);
        Check("有形状 + 空段 ⇒ 不是空蒙版（空段对链无影响）",
            !Build(true, (MaskOp.Replace, new[] { boxA }), (MaskOp.Subtract, Array.Empty<MaskShape>())).IsEmpty);

        // ── 三、纯逻辑：编解码往返 ─────────────────────────────────────────
        // 三个段，其中一段是**多边形**（可变长编码那条路）—— 定长元组式的编码在这里会露馅。
        var chain = Build(true, (MaskOp.Replace, new[] { boxA }), (MaskOp.Xor, new[] { boxB }));
        var polySeg = new MaskExpr.Segment { Op = MaskOp.Union };
        polySeg.Shapes.Add(MaskShape.Polygon(new List<double> { 0, 0, 10, 0, 5, 10 }));
        chain.Segments.Add(polySeg);

        string Sample(MaskExpr e)
        {
            int[] xs = { 2, 7, 12, 3, 3 };
            int[] ys = { 5, 5, 5, 8, 2 };
            var s = "";
            for (int i = 0; i < xs.Length; i++) s += e.Hit(xs[i], ys[i]) ? '1' : '0';
            return s;
        }

        var fig = new DrawFigure { Kind = "mask" };
        MaskExpr.Encode(fig, true, chain.Segments);
        var back = MaskExpr.Decode(fig.Args);
        Check("编解码往返：段数 / 多边形种类都在",
            back != null && back.Inside && back.Segments.Count == 3
            && back.Segments[2].Shapes.Count == 1
            && back.Segments[2].Shapes[0].Kind == MaskShape.KindPolygon);
        Check($"编解码往返：求值一致（{Sample(chain)}）", back != null && Sample(back) == Sample(chain));

        Check("解码空 Args ⇒ null（当作没有蒙版）", MaskExpr.Decode(new List<double>()) == null);

        // 程序给的数可能是坏的 —— 解码**绝不能抛异常**（宿主 syscall 处理器抛出去会把 VM 打挂，
        // 而程序那边只看到"窗口没了"）。截断的 Args 交回已解出的部分即可。
        bool threw = false;
        try { MaskExpr.Decode(new List<double> { 1, 3, 0, 9, 1, 4, 1, 2 }); }
        catch { threw = true; }
        Check("解码截断/超额的 Args 不抛异常", !threw);

        // ── 四、端到端：syscall → 场景 → 光栅 ─────────────────────────────
        var host = new FakeVmlHost();
        var rt = new VmlHostRuntime(host);
        var regs = new int[32];
        var mem = new byte[8192];

        regs[0] = WriteCStr(mem, 0, "maskbool");
        regs[1] = 64; regs[2] = 64; regs[3] = 0; regs[4] = 0;
        rt.HandleSyscall(VmlUi.WinOpen, regs, mem);
        var scene = rt.Scene();
        if (scene is null) { Check("开窗拿到场景（自测装置本身）", false); return; }
        scene.Width = 64; scene.Height = 64;

        bool Gfx(int op, int a = 0)
        {
            Array.Clear(regs);
            regs[0] = op; regs[1] = a;
            return rt.HandleSyscall(VmlUi.GfxState, regs, mem);
        }

        /// <summary>发一条 `ui_gfx` 并把**应答**（`regs[0]`）取回来 —— 查询类操作用。</summary>
        int GfxQ(int op, int a = 0, int b = 0, int c = 0, int d = 0)
        {
            Array.Clear(regs);
            regs[0] = op; regs[1] = a; regs[2] = b; regs[3] = c; regs[4] = d;
            rt.HandleSyscall(VmlUi.GfxState, regs, mem);
            return regs[0];
        }

        void Circle(int cx, int cy, int r)
        {
            Array.Clear(regs);
            regs[0] = cx; regs[1] = cy; regs[2] = r;
            regs[3] = unchecked((int)0xFFFFFFFF); regs[4] = 1; regs[5] = 0;
            rt.HandleSyscall(VmlUi.DrawCircle, regs, mem);
        }

        void FillAll()
        {
            Array.Clear(regs);
            regs[0] = 0; regs[1] = 0; regs[2] = 64; regs[3] = 64;
            regs[4] = unchecked((int)FILL); regs[5] = 1; regs[6] = 0; regs[7] = 0;
            rt.HandleSyscall(VmlUi.DrawRect, regs, mem);
        }

        // 甜甜圈：大圆（r=24）SUBTRACT 小圆（r=12），圆心都在 (32,32)
        Gfx(VmlUi.GfxOp.MaskBegin);
        Circle(32, 32, 24);
        Gfx(VmlUi.GfxOp.MaskEnd, 1);
        Gfx(VmlUi.GfxOp.MaskBegin);
        Circle(32, 32, 12);
        Check("ui_mask_end2 被宿主认领", Gfx(VmlUi.GfxOp.MaskEnd2, MaskOp.Subtract));
        FillAll();
        Gfx(VmlUi.GfxOp.MaskClear);

        // 未知运算符必须被**拒掉**：`regs[0] == 0`（"认领了这个号，但没执行" ——
        // 与 `GfxState` 处理未实现 op 的既有约定同构），且**不能当成 REPLACE 蒙混过去**。
        // ⚠ 判据不是 `HandleSyscall` 的返回值：那个只表示"这个 syscall 号归我管"。
        Check("未知运算符被拒（返回 0，不当 REPLACE）",
            Gfx(VmlUi.GfxOp.MaskEnd2, 99) && regs[0] == 0);

        var buf = new byte[64 * 64 * 4];
        if (!host.Rasterize(0, 0, 64, 64, buf))
        {
            Check("假宿主能光栅化（自测装置本身）", false);
            return;
        }

        bool IsFill(int x, int y)
        {
            int i = (y * 64 + x) * 4;
            return buf[i] == 0x3C && buf[i + 1] == 0x6E && buf[i + 2] == 0xB4;
        }

        Check("SUBTRACT 端到端：环上有填充（r=18 处）", IsFill(50, 32));
        Check("SUBTRACT 端到端：洞里是空的（挖掉了）", !IsFill(32, 32) && !IsFill(36, 32));
        Check("SUBTRACT 端到端：圆外是空的", !IsFill(2, 2) && !IsFill(32, 4));

        // ── 五、端到端：路径当蒙版（曲线展平那条路）────────────────────────
        // 三角形 (10,50) (54,50) (32,10)。它走的是 `DrawPath.Flatten` ——
        // 与 `ui_path` 当图元画时**同一个展平器**，所以这条也顺带钉住了"两条路同源"。
        scene.Clear(0xFF000000);
        // ⚠ `Gfx()` 内部会 `Array.Clear(regs)`（它就是"把操作码放进 R0"）⇒
        //   **必须先把状态指令发完、再填这一条的参数**，顺序反了参数会被清成 0，
        //   而 `DrawPath` 拿到空字符串是**直接 return**（不报错）—— 表现为"蒙版是空的"。
        Gfx(VmlUi.GfxOp.MaskBegin);
        Array.Clear(regs);
        regs[0] = WriteCStr(mem, 256, "M10 50 L54 50 L32 10 Z");
        regs[1] = unchecked((int)0xFFFFFFFF); regs[2] = 1;
        regs[3] = unchecked((int)0xFFFFFFFF); regs[4] = WriteCStr(mem, 512, "");
        regs[5] = 1; regs[6] = 0;
        rt.HandleSyscall(VmlUi.DrawPath, regs, mem);
        Gfx(VmlUi.GfxOp.MaskEnd, 1);
        FillAll();
        Gfx(VmlUi.GfxOp.MaskClear);

        var buf2 = new byte[64 * 64 * 4];
        host.Rasterize(0, 0, 64, 64, buf2);
        bool IsFill2(int x, int y)
        {
            int i = (y * 64 + x) * 4;
            return buf2[i] == 0x3C && buf2[i + 1] == 0x6E && buf2[i + 2] == 0xB4;
        }

        // y=45 处三角形的 x 范围是 [13.5, 50.5] 附近 ⇒ (32,45) 在内、(12,45) 在外
        Check("路径蒙版：三角形内可见", IsFill2(32, 45) && IsFill2(32, 20));
        Check("路径蒙版：三角形外不可见", !IsFill2(12, 45) && !IsFill2(52, 45) && !IsFill2(32, 5));

        // ── 七、蒙版当碰撞体（`ui_mask_test`）────────────────────────────
        // 蒙版已经是"可见区域的几何定义"，顺手就是一份碰撞体 —— 程序不必再自己
        // 维护一份洞的坐标表。⚠ 判据要与**画面**一致：`ui_mask_test` 说不可见的点，
        // 光栅出来就该是空的（下面拿甜甜圈的环做交叉验证）。
        int MaskTest(int x, int y)
        {
            Array.Clear(regs);
            regs[0] = VmlUi.GfxOp.MaskTest; regs[1] = x; regs[2] = y;
            rt.HandleSyscall(VmlUi.GfxState, regs, mem);
            return regs[0];
        }

        // 先把甜甜圈重新设上（上面区域 5 之后蒙版已被清掉）
        Gfx(VmlUi.GfxOp.MaskBegin);
        Circle(32, 32, 24);
        Gfx(VmlUi.GfxOp.MaskEnd, 1);
        Gfx(VmlUi.GfxOp.MaskBegin);
        Circle(32, 32, 12);
        Gfx(VmlUi.GfxOp.MaskEnd2, MaskOp.Subtract);

        Check("MaskTest：环上（r=18）⇒ 1", MaskTest(50, 32) == 1);
        Check("MaskTest：洞里（被挖掉）⇒ 0", MaskTest(32, 32) == 0 && MaskTest(36, 32) == 0);
        Check("MaskTest：圆外 ⇒ 0", MaskTest(2, 2) == 0);
        // 与画面**交叉验证**：同一个点，蒙版说 1 ⇒ 上面光栅出来就该是填充色
        Check("MaskTest 与光栅同源（环上那点画出来了）", MaskTest(50, 32) == 1 && IsFill(50, 32));

        // `inside=0` 取反之后，查询结果也要跟着取反
        Gfx(VmlUi.GfxOp.MaskBegin);
        Circle(32, 32, 12);
        Gfx(VmlUi.GfxOp.MaskEnd, 0);
        Check("MaskTest：inside=0 时内外互换",
            MaskTest(32, 32) == 0 && MaskTest(2, 2) == 1);

        // 没有蒙版 ⇒ 处处可见（与绘制那边"没有蒙版就全画"同一条口径）
        Gfx(VmlUi.GfxOp.MaskClear);
        Check("MaskTest：没有蒙版时恒为 1", MaskTest(2, 2) == 1 && MaskTest(63, 63) == 1);

        // 路径蒙版也能查（多边形形状走的是同一条判定）
        Gfx(VmlUi.GfxOp.MaskBegin);
        Array.Clear(regs);
        regs[0] = WriteCStr(mem, 256, "M10 50 L54 50 L32 10 Z");
        regs[1] = unchecked((int)0xFFFFFFFF); regs[2] = 1;
        regs[3] = unchecked((int)0xFFFFFFFF); regs[4] = WriteCStr(mem, 512, "");
        regs[5] = 1; regs[6] = 0;
        rt.HandleSyscall(VmlUi.DrawPath, regs, mem);
        Gfx(VmlUi.GfxOp.MaskEnd, 1);
        Check("MaskTest：路径蒙版内 / 外",
            MaskTest(32, 45) == 1 && MaskTest(12, 45) == 0 && MaskTest(32, 5) == 0);
        Gfx(VmlUi.GfxOp.MaskClear);

        // ── 九、蒙版 → 路径（描洞口的边）──────────────────────────────────
        // **判据是"往返"**：导出的 SVG 路径喂回 `DrawPath.Flatten`，展平出来的点
        // 必须仍落在原形状上。只断言"返回了一个非空串"证明不了什么 ——
        // 格式写错（少个空格、`A` 的参数顺序不对）照样返回一串东西。
        Gfx(VmlUi.GfxOp.MaskBegin);
        Circle(20, 20, 12);
        Gfx(VmlUi.GfxOp.MaskEnd, 1);
        Gfx(VmlUi.GfxOp.MaskBegin);
        Circle(20, 20, 5);
        Gfx(VmlUi.GfxOp.MaskEnd2, MaskOp.Subtract);

        Check("Mask 导出：段数 2 / 第 1 段是 SUBTRACT / 该段 1 个形状",
            GfxQ(VmlUi.GfxOp.MaskSegCount) == 2
            && GfxQ(VmlUi.GfxOp.MaskSegOp, 1) == MaskOp.Subtract
            && GfxQ(VmlUi.GfxOp.MaskShapeCount, 1) == 1);

        int len = GfxQ(VmlUi.GfxOp.MaskPath, 1, 0, 300, 256);
        var d = len > 0 ? System.Text.Encoding.UTF8.GetString(mem, 300, len) : "";
        Check($"Mask 导出：圆的路径有内容（{len} 字节）", len > 0 && d.StartsWith("M"));

        var subs = DrawPath.Flatten(d);
        Check("Mask 导出：路径能喂回解析器", subs.Count == 1 && subs[0].Points.Count >= 8);
        bool onCircle = subs.Count == 1;
        if (onCircle)
        {
            foreach (var (px, py) in subs[0].Points)
            {
                double rr = Math.Sqrt((px - 20) * (px - 20) + (py - 20) * (py - 20));
                if (Math.Abs(rr - 5) > 0.2) { onCircle = false; break; }
            }
        }
        Check("Mask 导出：展平后的点都落在原圆上（半径 5）", onCircle);

        // 矩形导出走另一条分支（`L` 命令），同样做往返
        Gfx(VmlUi.GfxOp.MaskClear);
        Gfx(VmlUi.GfxOp.MaskBegin);
        Array.Clear(regs);
        regs[0] = 0; regs[1] = 0; regs[2] = 40; regs[3] = 30; regs[4] = 0;
        regs[5] = 1; regs[6] = 0; regs[7] = 0;
        rt.HandleSyscall(VmlUi.DrawRect, regs, mem);
        Gfx(VmlUi.GfxOp.MaskEnd, 1);
        int rlen = GfxQ(VmlUi.GfxOp.MaskPath, 0, 0, 300, 256);
        var rd = rlen > 0 ? System.Text.Encoding.UTF8.GetString(mem, 300, rlen) : "";
        var rsubs = DrawPath.Flatten(rd);
        Check("Mask 导出：矩形的路径往返（4 个角点）",
            rsubs.Count == 1 && rsubs[0].Points.Count >= 4
            && rsubs[0].Points.Exists(p => Math.Abs(p.X) < 0.01 && Math.Abs(p.Y) < 0.01)
            && rsubs[0].Points.Exists(p => Math.Abs(p.X - 40) < 0.01 && Math.Abs(p.Y - 30) < 0.01));

        // 越界与容量不足：都返回 -1（"没拿到"），程序据此跳过
        Check("Mask 导出：段越界 ⇒ -1", GfxQ(VmlUi.GfxOp.MaskPath, 9, 0, 300, 256) == -1);
        Check("Mask 导出：形状越界 ⇒ -1", GfxQ(VmlUi.GfxOp.MaskPath, 0, 9, 300, 256) == -1);
        Check("Mask 导出：缓冲区放不下 ⇒ -1", GfxQ(VmlUi.GfxOp.MaskPath, 0, 0, 300, 4) == -1);

        Gfx(VmlUi.GfxOp.MaskClear);
        Check("Mask 导出：没有蒙版时段数为 0", GfxQ(VmlUi.GfxOp.MaskSegCount) == 0);

        // ── 十、图层（`ui_gfx` op 6/7）：整层离屏合成 ──────────────────────
        // **判别性判据是"组内重叠只透一次"** —— 那正是图层与 `ui_alpha` 的区别：
        //   `ui_alpha` 是每个图元各自半透明（重叠处**互相透出来**，交界更深），
        //   图层是"先画好一整张、再整张压上去"（组内怎么重叠都只透一次）。
        //   只测"半透明生效"的话，用 `ui_alpha` 的错误实现照样能过。
        Canvas LayerRaster(string dsl) => DrawRunner.Rasterize(DrawRunner.Parse(dsl));

        const string OverlapPair =
            "rect 10 10 30 30 #ff0000 1 0 0\n"
            + "rect 25 25 30 30 #ff0000 1 0 0\n";     // 两块重叠的红（重叠区 x/y ∈ 25..40）

        static uint Solid2(Canvas cv, int x, int y)
        {
            int i = (y * cv.Width + x) * 4;
            return (uint)((cv.Pixels[i + 3] << 24) | (cv.Pixels[i] << 16)
                        | (cv.Pixels[i + 1] << 8) | cv.Pixels[i + 2]);
        }

        // 图层：整组 50% 透明
        var lc = LayerRaster("canvas 64 64 #ffffff\nlayer_begin\n" + OverlapPair + "layer_end 128\n");
        uint solid = Solid2(lc, 15, 15);      // 只有一块红的地方
        uint overlap = Solid2(lc, 32, 32);    // 两块重叠的地方
        Check($"图层：整层半透明生效（实得 0x{solid:X8}）",
            ColorUtil.R(solid) > 200 && ColorUtil.G(solid) > 90 && ColorUtil.G(solid) < 170);
        Check("图层：**组内重叠只透一次**（重叠处与非重叠处同色）", solid == overlap);

        // 对照：同样两块**各自半透明**的红（`#80ff0000`）—— 重叠处会更深。
        // ⚠ 不能用 DSL 的 `alpha 128`：那条指令是**给 VML 宿主**用的
        //   （`VmlScene.Style` 在把颜色写进 DSL 时就把 alpha 乘进去了），
        //   而 `DrawRunner` 直接解析 DSL 时它**什么都不做** —— 写它等于没写。
        var ac = LayerRaster("canvas 64 64 #ffffff\n"
            + "rect 10 10 30 30 #80ff0000 1 0 0\nrect 25 25 30 30 #80ff0000 1 0 0\n");
        Check($"对照：`ui_alpha` 的重叠处确实更深（单 0x{Solid2(ac, 15, 15):X8} / 叠 0x{Solid2(ac, 32, 32):X8}）",
            Solid2(ac, 15, 15) != Solid2(ac, 32, 32));

        // alpha=0 ⇒ 整层不画（也省掉那块画布）
        var zero = LayerRaster("canvas 64 64 #ffffff\nlayer_begin\n" + OverlapPair + "layer_end 0\n");
        Check("图层：alpha=0 ⇒ 整层不画", Solid2(zero, 15, 15) == 0xFFFFFFFF);

        // alpha=255 ⇒ 与不加图层一样（不透明整层贴回来）
        var full = LayerRaster("canvas 64 64 #ffffff\nlayer_begin\n" + OverlapPair + "layer_end 255\n");
        Check($"图层：alpha=255 ⇒ 纯红（实得 0x{Solid2(full, 15, 15):X8}）", Solid2(full, 15, 15) == 0xFFFF0000);

        // 子图元确实被**收进**了 layer 图元（不是留在顶层）
        var ldoc = DrawRunner.Parse("canvas 64 64\nlayer_begin\nrect 1 1 5 5 0xFF000000 1 0 0\n"
            + "circle 10 10 3 0xFF000000 1 0\nlayer_end 255\nrect 40 40 5 5 0xFF000000 1 0 0\n");
        var layerFig = ldoc.Figures.Find(f2 => f2.Kind == "layer");
        Check("图层：层内两个图元被收进 Children，层外那个留在顶层",
            layerFig?.Children is { Count: 2 } && ldoc.Figures.Count == 2);

        // 合成走 `SetPixel` ⇒ **整层受外层裁剪约束**（这是有意设计的）
        var clipped = LayerRaster("canvas 64 64 #ffffff\nclip 0 0 20 20\nlayer_begin\n"
            + "rect 0 0 60 60 #ff0000 1 0 0\nlayer_end 255\n");
        Check("图层：贴在裁剪里的整层被裁掉（合成走 SetPixel，受外层约束）",
            Solid2(clipped, 5, 5) == 0xFFFF0000 && Solid2(clipped, 40, 40) == 0xFFFFFFFF);

        // 矢量后端：整层离屏合成没有通用做法 ⇒ 如实标记不支持（回退光栅）
        var lrec = new RecordingVectorTarget();
        foreach (var f3 in ldoc.Figures)
            DrawCommandRegistry.Get(f3.Kind)?.Vector(lrec, f3);
        Check("图层：矢量后端标记 Unsupported（回退光栅）",
            lrec.Unsupported.Contains("layer"));

        // 端到端：走 **syscall** 收一层（上面那些只证明了 `DrawRunner` 认识这两个词，
        // 而程序走的是 `ui_gfx` op 6/7 那条路 —— 两跳都要验）
        scene.Clear(0xFFFFFFFF);
        Gfx(VmlUi.GfxOp.LayerBegin);
        Array.Clear(regs);
        regs[0] = 0; regs[1] = 0; regs[2] = 64; regs[3] = 64;
        regs[4] = unchecked((int)0xFF0000FF); regs[5] = 1; regs[6] = 0; regs[7] = 0;
        rt.HandleSyscall(VmlUi.DrawRect, regs, mem);
        Check("LAYER_END 被宿主认领", Gfx(VmlUi.GfxOp.LayerEnd, 128));

        var lbuf = new byte[64 * 64 * 4];
        host.Rasterize(0, 0, 64, 64, lbuf);
        int li = (32 * 64 + 32) * 4;
        Check($"图层端到端：整层半透明（实得 R{lbuf[li]} G{lbuf[li + 1]} B{lbuf[li + 2]}）",
            lbuf[li] > 100 && lbuf[li + 1] > 100 && lbuf[li + 2] > 200);

        // **图层里套蒙版**（两个收集器同时开着）：层内的蒙版必须**照常生效**。
        // ⚠ 这条钉的是解析期两个缓冲的**先后**：反了的话形状被图层截走 ⇒ 蒙版收到空集
        //   ⇒ 静默失效，而且那些形状还会被当普通图元画出来（"蒙版没用、形状照画"）。
        var lm = LayerRaster("canvas 64 64 #ffffff\nlayer_begin\n"
            + "mask_begin\nrect 0 0 32 64 #ffffff 1 0 0\nmask_end 1\n"
            + "rect 0 0 64 64 #ff0000 1 0 0\nlayer_end 255\n");
        Check("图层里套蒙版：蒙版照常生效（右半没画、左半红）",
            Solid2(lm, 16, 32) == 0xFFFF0000 && Solid2(lm, 48, 32) == 0xFFFFFFFF);

        // ── 六、SVG 导出：`<g>` 必须配对（蒙版开的那层原来没人关）──────────
        var doc = DrawRunner.Parse(
            "canvas 32 32\n"
            + "mask_begin\ncircle 16 16 10 0 1 0\nmask_end 1\n"
            + "rect 0 0 32 32 0xFF00FF00 1 0 0\n"
            + "mask_begin\nrect 8 8 8 8 0 1 0\nmask_end2 3\n"
            + "line 0 0 32 32 0xFF000000 1\n");
        var svg = DrawRunner.ToSvg(doc);
        int opens = CountOccurrences(svg, "<g"), closes = CountOccurrences(svg, "</g>");
        Check($"SVG 的 <g> 配对（{opens} 开 / {closes} 闭）", opens == closes && opens >= 2);
        Check("SVG 里含 <mask>（布尔链走的是亮度蒙版）", svg.Contains("<mask id="));

        // ── 八、矢量后端：布尔链**折叠成一条路径 + 一个填充规则**────────────
        // 矢量后端不懂布尔运算，只认"一条能直接裁剪的路径"。折叠不了就回退光栅 ——
        // 所以这里既测"能折叠的折叠对了"，也测"折叠不了的**确实拒绝了**"（后者更要紧：
        // 硬塞一条路径进去画出来是错的，而错的画面比慢的画面糟得多）。
        var oneSeg = Build(true, (MaskOp.Replace, new[] { MaskShape.Circle(0, 0, 10) }));
        var fold1 = oneSeg.ToClipPath();
        Check("矢量折叠：单段圆 ⇒ 一条子路径 + NonZero",
            fold1 != null && fold1.Value.Subpaths.Count == 1
            && !fold1.Value.EvenOdd && fold1.Value.Subpaths[0].Count >= 6);

        var ring2 = Build(true, (MaskOp.Replace, new[] { MaskShape.Rect(0, 0, 100, 100) }),
                                (MaskOp.Subtract, new[] { MaskShape.Circle(50, 50, 20) }));
        var fold2 = ring2.ToClipPath();
        Check("矢量折叠：矩形减圆 ⇒ EvenOdd + 两条子路径",
            fold2 != null && fold2.Value.EvenOdd && fold2.Value.Subpaths.Count == 2);

        // 洞跑到矩形**外面**：even-odd 会把洞外面那块填**实**（穿一次 = 奇数 = 内部）⇒ 必须拒绝
        var holeOut = Build(true, (MaskOp.Replace, new[] { MaskShape.Rect(0, 0, 100, 100) }),
                                  (MaskOp.Subtract, new[] { MaskShape.Circle(200, 200, 20) }));
        Check("矢量折叠：洞在底外 ⇒ 拒绝", holeOut.ToClipPath() == null);

        // 两个洞重叠：重叠区穿过两次 ⇒ 反而被填实 ⇒ 必须拒绝
        var holeOverlap = Build(true, (MaskOp.Replace, new[] { MaskShape.Rect(0, 0, 100, 100) }),
                                      (MaskOp.Subtract, new[] { MaskShape.Circle(40, 50, 20),
                                                                MaskShape.Circle(50, 50, 20) }));
        Check("矢量折叠：洞与洞重叠 ⇒ 拒绝", holeOverlap.ToClipPath() == null);

        // 单段（并集）里形状重叠是**合法**的：NonZero 规则下重叠区仍是内部，正是并集。
        // ⚠ 这与"多段 EvenOdd 下底形状不能重叠"是两回事 —— 规则换了，同一个几何结论就反了。
        var baseOverlap = Build(true, (MaskOp.Replace, new[] { MaskShape.Rect(0, 0, 60, 60),
                                                              MaskShape.Rect(30, 30, 60, 60) }));
        var fold3 = baseOverlap.ToClipPath();
        Check("矢量折叠：单段形状重叠 ⇒ 允许（NonZero 天然是并集）",
            fold3 != null && !fold3.Value.EvenOdd && fold3.Value.Subpaths.Count == 2);

        // ⚠ **两个挨着但不相碰的圆洞必须能折叠** —— 这正是 gorilla 的日常情形
        //   （香蕉炸出来的洞常常挨得很近）。用**包围盒**判"洞与洞重叠"会把它们误拒，
        //   于是**几乎每一帧都退回光栅** —— 玩家直接看出来"画面怎么变成光栅了"。
        //   两个圆 r=30、圆心距 61 ⇒ 不相碰，但包围盒必然相交。
        var nearMiss = Build(true, (MaskOp.Replace, new[] { MaskShape.Rect(0, 0, 200, 200) }),
                                   (MaskOp.Subtract, new[] { MaskShape.Circle(60, 100, 30),
                                                             MaskShape.Circle(121, 100, 30) }));
        Check("矢量折叠：两个挨着但不相碰的洞 ⇒ 能折叠（gorilla 的日常情形）",
            nearMiss.ToClipPath() != null);

        // 真的重叠（圆心距 50 < 60）⇒ 仍然拒（even-odd 下重叠区会被填实）
        var reallyOverlap = Build(true, (MaskOp.Replace, new[] { MaskShape.Rect(0, 0, 200, 200) }),
                                       (MaskOp.Subtract, new[] { MaskShape.Circle(60, 100, 30),
                                                                 MaskShape.Circle(110, 100, 30) }));
        Check("矢量折叠：真的重叠的洞 ⇒ 仍然拒", reallyOverlap.ToClipPath() == null);

        // 相切（圆心距恰好 60）= 不算重叠 —— "差一点点就拒"会让刚好挨着的洞永远走光栅
        var tangent = Build(true, (MaskOp.Replace, new[] { MaskShape.Rect(0, 0, 200, 200) }),
                                 (MaskOp.Subtract, new[] { MaskShape.Circle(60, 100, 30),
                                                           MaskShape.Circle(120, 100, 30) }));
        Check("矢量折叠：相切的洞 ⇒ 算能折叠（切点面积为零）", tangent.ToClipPath() != null);

        var inter2 = Build(true, (MaskOp.Replace, new[] { boxA }), (MaskOp.Intersect, new[] { boxB }));
        var xor2 = Build(true, (MaskOp.Replace, new[] { boxA }), (MaskOp.Xor, new[] { boxB }));
        Check("矢量折叠：INTERSECT / XOR 拒绝（要真正的路径布尔）",
            inter2.ToClipPath() == null && xor2.ToClipPath() == null);

        // 端到端：DSL → 矢量后端（用记录型落笔面，桌面就能验）
        var rec = new RecordingVectorTarget();
        var vdoc = DrawRunner.Parse(
            "canvas 64 64\n"
            + "mask_begin\nrect 0 0 40 40 0 1 0 0\nmask_end 1\n"
            + "mask_begin\ncircle 20 20 8 0 1 0\nmask_end2 3\n"
            + "rect 0 0 64 64 0xFF00FF00 1 0 0\n"
            + "mask_begin\nmask_end 1\n"
            + "rect 0 0 8 8 0xFF000000 1 0 0\n");
        foreach (var vf in vdoc.Figures)
            DrawCommandRegistry.Get(vf.Kind)?.Vector(rec, vf);
        // ⚠ `mask_end` 与 `mask_end2` **各生成一条蒙版图元**（前一条是"底"，后一条才是完整的
        //   布尔链）—— 与光栅那边逐条 `SetMask` 完全一致，所以这里要断言**第二条**。
        Check($"矢量端到端：先是一条底（NonZero），再是甜甜圈（EvenOdd）",
            rec.Masks.Count == 2
            && !rec.Masks[0].EvenOdd && rec.Masks[0].Subpaths.Count == 1
            && rec.Masks[1].EvenOdd && rec.Masks[1].Subpaths.Count == 2);
        Check("矢量端到端：空蒙版（clear）只弹不推", rec.MaskPops >= 2);
        Check("矢量端到端：没有 MarkUnsupported（这一帧能走矢量快路）", rec.Unsupported.Count == 0);

        static int CountOccurrences(string s, string needle)
        {
            int n = 0, i = 0;
            while ((i = s.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
            return n;
        }
    }
}

using WayCoder.UI.Shared;
#if IOS || MACCATALYST
using AVFoundation;
using Foundation;
#endif

namespace WayCoder.Maui.Services;

/// <summary>
/// VML 程序的**音效与 BGM** —— `AUDIO_TONE` / `AUDIO_PLAY` / `AUDIO_STOP` / `AUDIO_VOLUME`
/// （号段 540–543，见 <see cref="VmlUi"/> 与 docs/VML宿主接口.md）。
///
/// ## 为什么音效是"现场合成"而不是播放素材
/// `AUDIO_TONE` 按频率+时长+波形**当场算出一段 PCM** 交给 `AudioTrack`（静态模式）播放。
/// 三个好处，缺一个都不成立：
///   · **游戏不用带任何音频素材** —— 消行、跳跃、吃豆这些音效本来就是几十毫秒的方波/正弦，
///     为它们各打一个 mp3 进去，包体和版权都是白来的麻烦；
///   · 参数是程序算的（频率随分数升高、时长随连击变长），素材做不到；
///   · 不用管解码器/格式兼容/文件找不到 —— 少掉一整类失败模式。
///
/// `AUDIO_PLAY` 才是播文件（BGM），走 `MediaPlayer`，路径由调用方解析成**沙箱内的绝对路径**。
///
/// ## 平台
/// MAUI 没有音频 API，所以这里必须分平台。**Android 是真实现**（手机端只有它有意义），
/// 其余平台一律空实现 —— 不是"没做"，是那些平台本来就不是这个 App 的目标，
/// 而且桌面端跑 VML 用的是 TUI 那条路（没有 `DrawWindowPage`，也就没人调这里）。
///
/// ## 线程
/// 这些方法**在 VM 线程上被 syscall 同步调用**。静态模式的 `Write` 是一次内存拷贝
/// （最长 5 秒 × 44.1kHz × 2 字节 ≈ 441 KB，亚毫秒级），可以接受；
/// ⚠ **绝对不要在这里做 `Prepare` 之类的阻塞调用** —— 那会把整个 VML 程序卡住。
/// `MediaPlayer.Prepare` 因此走**异步**（`PrepareAsync` + 监听），播不出来最多是没声音，不会卡程序。
/// </summary>
internal static class VmlAudio
{
    /// <summary>合成音的音量（0–100），由 `AUDIO_VOLUME` 改；对之后播的音生效。</summary>
    private static int _volume = 80;

    /// <summary>
    /// **复音合成器** —— 与桌面 vmlcli、与主工程自测用的是**同一个类**
    /// （`WayCoder/UI/Shared/VmlToneSynth.cs`）。所以"桌面上验出来的复音对不对"
    /// 这个结论对手机同样成立 —— 这正是把合成抽成纯逻辑的收益。
    /// </summary>
    private static readonly VmlToneSynth Synth = new();

    /// <summary>设置整体音量（已由协议层钳过 0–100）。**BGM 与合成器一起改** ——
    /// 只改一个的话，调音量会出现"背景音乐小了、音效还震耳"。</summary>
    public static void SetVolume(int volume)
    {
        _volume = VmlUi.ClampVolume(volume);
        Synth.Volume = _volume;
        SetBgmVolume(_volume);
    }

    /// <summary>
    /// 合成音（用当前音量）—— **VM 内置 `#57` 蜂鸣走这条**。
    /// 参数先过协议层钳位：频率传 0 会让合成器算出除零/零周期，症状是"没声音"甚至卡住，
    /// 而程序那边完全看不出是参数问题。
    ///
    /// <para>
    /// ⚠ 现在它走**合成器的老式蜂鸣声道**（<see cref="VmlToneSynth.LegacyLane"/>），
    /// 而不是自己造一段 PCM 丢给静态轨：
    /// 同一通道上后音掐前音 ⇒「**连发一串只听见最后一个**」这个老语义**原样保留**，
    /// 而它**不再掐掉别的通道** ⇒ 游戏音效可以与和弦共存。这正是复音改造的目的。
    /// 唯一的（有意的）差异：老实现是硬切，现在是 5ms 淡出 —— 去掉了"咔"声，可观察语义不变。
    /// </para>
    /// </summary>
    public static bool Tone(int hz, int ms, int wave)
    {
        var (f, d, w, _) = VmlUi.ClampTone(hz, ms, wave, _volume);
        Synth.NoteOn(VmlToneSynth.LegacyLane, f, -1, 100, w, holdMs: d);
        EnsureMixer();                       // 惰性起混音线程（没起过才起）
        return true;
    }

    /// <summary>复音：在通道上起一个音（音符号 0–127，力度 0 等同关音）。</summary>
    public static bool NoteOn(int channel, int note, int velocity, int wave)
    {
        var n = VmlUi.ClampNote(note);
        var ok = Synth.NoteOn(VmlUi.ClampChannel(channel), VmlToneSynth.NoteToHz(n), n,
                              VmlUi.ClampVelocity(velocity), wave);
        if (ok) EnsureMixer();
        return ok;
    }

    /// <summary>复音：关一个音（<paramref name="note"/> = -1 表示该通道全部）。</summary>
    public static bool NoteOff(int channel, int note) => Synth.NoteOff(VmlUi.ClampChannel(channel), note);

    /// <summary>复音：杂项控制（控制码见 <see cref="VmlUi.AudioCtl"/>）。</summary>
    public static int ToneControl(int ctl, int a, int b)
    {
        switch (ctl)
        {
            case VmlUi.AudioCtl.AllNotesOff: Synth.AllNotesOff(); return 0;
            case VmlUi.AudioCtl.Wave: return Synth.SetChannelWave(a, b) ? 0 : -1;
            case VmlUi.AudioCtl.MaxVoices: Synth.MaxVoicesLimit = a; return 0;
            case VmlUi.AudioCtl.Voices: return Synth.ActiveVoices;
            case VmlUi.AudioCtl.Panic: Synth.Panic(); return 0;
            default: return -1;   // ⚠ 与宿主侧"认不出的 ctl 返回 -1"同一口径
        }
    }

    /// <summary>
    /// <b>音频自检</b>（命令行页敲 <c>audio</c> 触发）—— 把"这台设备上音频到底通没通"
    /// 逐环节摆到屏幕上，并挨个放几种声音让人对着听。
    ///
    /// <para>
    /// 为什么值得做成常驻功能：iOS 那条「**一声不响、什么都不报**」的路已经坑过两次
    /// （会话类别跟随静音开关、UIKit 跨线程），而**从 Mac 读不到设备上的日志**
    /// （`devicectl` 这个版本不支持读 App 容器）⇒ 唯一的观测手段就是让它**打在屏幕上**。
    /// 判据是「看得见的状态 + 听得见的声音」两样一起，缺一样都定不了性。
    /// </para>
    ///
    /// <para>
    /// ⚠ 每一步都要**真的过一遍发声链**（而不是只读字段）：`NoteOn` 会顺带 `EnsureMixer()`，
    /// 引擎与播放节点正是那一步建起来的 —— 先放一声再看状态，才看得到"建没建起来"。
    /// </para>
    /// </summary>
    public static async Task<string> SelfCheckAsync()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("🔊 音频自检（每一步都会真的响，听着对一下）");
        sb.AppendLine($"· 合成器：主音量={_volume} 声部上限={Synth.MaxVoicesLimit}");

        // ⚠⚠ **每一步各自 try/catch，而且异常必须打在屏幕上**（2026-09-27 真机实测）：
        //   用户在 iPad 上跑到第 ③ 步**闪退**（SIGABRT、托管异常未捕获），而异常消息只在
        //   App 私有的日志文件里 —— **从 Mac 读不到设备容器**（devicectl 不支持），
        //   于是"崩在哪一行、什么异常"完全看不见，只能靠猜 ✗。
        //   自检工具**崩掉是最糟的失败形态**：它本来就是用来把不可见的故障变成可见的。
        //   现在每一步独立兜住，异常类型 + 消息直接进返回值（打在屏幕上）——
        //   崩到哪一步、是什么异常，一眼就能读出来。
        async Task Step(string title, Func<Task> body)
        {
            sb.AppendLine(title);
            try { await body(); }
            catch (Exception ex)
            {
                sb.AppendLine($"   ❌ **这一步抛异常**：{ex.GetType().Name}：{ex.Message}");
                var st = ex.StackTrace ?? "";
                if (st.Length > 400) st = st[..400] + "…";
                sb.AppendLine("   " + st.Replace("\n", "\n   "));
            }
        }

        // 设备状态整段也兜住（会话/节点属性在真机上更可能抛）
        try { sb.AppendLine($"· 发声前：{DescribePlatform()}"); }
        catch (Exception ex) { sb.AppendLine($"· 发声前状态读取失败：{ex.GetType().Name}：{ex.Message}"); }

        await Step("① 单音 do（约 0.6 秒）", async () =>
        {
            NoteOn(0, 60, 100, 0);
            await Task.Delay(650);
            sb.AppendLine($"   → 此刻声部={Synth.ActiveVoices}（应为 1 —— 是 0 说明调用没进合成器）");
            NoteOff(0, 60);
        });

        await Step("② 和弦 do-mi-sol（约 0.9 秒，三个音同时）", async () =>
        {
            NoteOn(0, 60, 100, 0);
            NoteOn(1, 64, 100, 0);
            NoteOn(2, 67, 100, 0);
            await Task.Delay(950);
            sb.AppendLine($"   → 此刻声部={Synth.ActiveVoices}（应为 3）");
            ToneControl(VmlUi.AudioCtl.AllNotesOff, 0, 0);
        });

        await Step("③ 老式蜂鸣 880Hz（约 0.4 秒，**与游戏音效同一条路**）", async () =>
        {
            Tone(880, 400, 1);
            await Task.Delay(600);
        });

        try
        {
            sb.AppendLine("· 发声后：" + DescribePlatform());
            sb.AppendLine(Synth.ActiveVoices == 0
                ? "· 声部已清空 ✓"
                : $"· ⚠️ 还有 {Synth.ActiveVoices} 个声部没关");
        }
        catch (Exception ex) { sb.AppendLine($"· 发声后状态读取失败：{ex.GetType().Name}：{ex.Message}"); }

        sb.AppendLine("· ①②有声音、③ 没声音 ⇒ 复音那条路的问题；全都没声音 ⇒ 引擎/会话那条路的问题。");
        sb.AppendLine("· 哪一步写了 ❌ ⇒ 那一步抛了异常（类型与消息就在上面），把那几行发我。");
        return sb.ToString();
    }

    // `DescribePlatform()` 由**下面每个平台分支各定义一份**（引擎/混音轨/播放节点的字段本来
    // 就分属各分支）—— 与 `EnsureMixer` / `StopAll` 同一个做法。少写一个分支会直接编不过，
    // 这正是想要的失败方式（本仓宁可编不过，也不要"某个平台悄悄没实现"）。

#if ANDROID
    // ── 复音混音器（v0.96.485）────────────────────────────────────────────
    //
    // 老实现是"每发一个音当场算一段 PCM、丢给一个 **Static** 模式的 AudioTrack" ——
    // 一条轨只能装一段、播完即弃，所以下一个音必须先把它 Stop 掉 ⇒ **结构性单通道**，
    // 和弦/旋律都做不出来。现在换成**一条 Stream 轨 + 一个后台混音线程**：
    //   · 声部表在 `Synth` 里（`UI/Shared/VmlToneSynth.cs`，与桌面/自测**同一份**）
    //   · 线程每 1024 帧调一次 `Mix()` 填 PCM，`Write()` 进轨
    //   · **`Write` 是阻塞的** —— 环形缓冲一满它就等，循环自然按实时速率走。
    //     这是选 Stream 模式最大的隐性收益：不用自己算节拍、不用轮询播放头。
    //   · 渲染彻底搬离 VM 线程（老实现是在 VM 线程上同步合成，那是知情的将就）

    /// <summary>流式输出轨（惰性建）。</summary>
    private static Android.Media.AudioTrack? _mixer;

    /// <summary>混音线程（惰性起、随 <see cref="StopAll"/> 收）。</summary>
    private static Thread? _mixThread;

    /// <summary>线程退出的唯一开关。**置位顺序见 <see cref="StopAll"/> —— 反了会卡住。**</summary>
    private static volatile bool _running;

    /// <summary>渲染块（帧）。1024 帧 ≈ 23ms —— 也是"按下到出声"延迟的上界。</summary>
    private const int BlockFrames = 1024;


    /// <summary>BGM 播放器（惰性建）。</summary>
    private static Android.Media.MediaPlayer? _bgm;

    /// <summary>
    /// 起混音器（**惰性** —— 第一次真要发声时才起）。
    /// 一个全程不发声的程序不该开音频轨：省电，也少一类失败模式。
    /// </summary>
    private static void EnsureMixer()
    {
        if (_mixer != null) return;
        try
        {
            var minBytes = Android.Media.AudioTrack.GetMinBufferSize(
                VmlToneSynth.SampleRate, Android.Media.ChannelOut.Mono, Android.Media.Encoding.Pcm16bit);
            // 至少留 4 个块的余量 —— 太小会欠载（听感是断续/爆音）。
            var bufBytes = Math.Max(minBytes, BlockFrames * 2 * 4);
            var track = new Android.Media.AudioTrack(
                Android.Media.Stream.Music, VmlToneSynth.SampleRate, Android.Media.ChannelOut.Mono,
                Android.Media.Encoding.Pcm16bit, bufBytes, Android.Media.AudioTrackMode.Stream);
            track.Play();
            _mixer = track;
            _running = true;
            _mixThread = new Thread(MixLoop) { IsBackground = true, Name = "vml-audio" };
            _mixThread.Start();
        }
        catch (Exception ex)
        {
            // 起不来就**退化**成"没声音"，绝不抛回 VM 线程（那会把 VML 程序打挂）。
            _mixer = null;
            ErrorLog.Error("VmlAudio", "混音器起不来（退化：程序照跑，只是没声音）", ex);
        }
    }

    /// <summary>混音线程主体：填一块、写一块，直到 <see cref="_running"/> 被清。</summary>
    private static void MixLoop()
    {
        var samples = new short[BlockFrames];
        var bytes = new byte[BlockFrames * 2];
        try
        {
            while (_running)
            {
                Synth.Mix(samples, BlockFrames);
                Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
                var track = _mixer;
                if (track == null) break;
                // ⚠ 这一句就是**背压**：缓冲满了它自己阻塞，循环自然跟着实时速率走。
                track.Write(bytes, 0, bytes.Length);
            }
        }
        catch (Exception ex)
        {
            // 正常收尾（Stop 之后再 Write 会抛）不该刷错误日志。
            if (_running) ErrorLog.Error("VmlAudio", "混音线程异常退出", ex);
        }
    }

    /// <summary>停掉正在响的合成音 —— 现在语义是"清掉老式蜂鸣那条通道"。</summary>
    public static void StopTone() => Synth.NoteOff(VmlToneSynth.LegacyLane);

    /// <summary>自检用：这台 Android 设备上混音那一路的实际状态。</summary>
    private static string DescribePlatform()
        => $"Android 混音轨={(_mixer == null ? "**未起（没声音就是这里）**" : "已起")}"
         + $" / 喂块线程={(_mixThread?.IsAlive == true ? "在跑" : (_running ? "已起但不在跑" : "没起"))}";

    /// <summary>同步 BGM 音量（`AUDIO_VOLUME` 要同时作用于 BGM 与合成器）。</summary>
    private static void SetBgmVolume(int volume)
    {
        var mp = _bgm;
        if (mp == null) return;
        try { var v = Math.Clamp(volume, 0, 100) / 100f; mp.SetVolume(v, v); }
        catch { /* 已释放：忽略 */ }
    }

    /// <summary>播一个音频文件（BGM）。返回失败原因，成功返回 null。</summary>
    public static string? Play(string path, bool loop)
    {
        try
        {
            StopBgm();

            var mp = new Android.Media.MediaPlayer();
            mp.SetDataSource(path);
            mp.Looping = loop;
            var v = Math.Clamp(_volume, 0, 100) / 100f;
            mp.SetVolume(v, v);

            // ⚠ 用 PrepareAsync 而不是 Prepare：Prepare 是**同步阻塞**的，
            // 在 VM 线程上会把这个 VML 程序整个卡住几百毫秒（而 syscall 是同步返回的）。
            mp.Prepared += (_, _) => { try { mp.Start(); } catch { } };
            mp.PrepareAsync();
            _bgm = mp;
            return null;
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlAudio", $"BGM 播放失败：{path}", ex);
            return ex.Message;
        }
    }

    /// <summary>停掉 BGM。</summary>
    public static void StopBgm()
    {
        var mp = _bgm;
        _bgm = null;
        if (mp == null) return;
        try { if (mp.IsPlaying) mp.Stop(); } catch { }
        try { mp.Release(); } catch { }
    }

    /// <summary>
    /// BGM 还在放吗（`AUDIO_IS_PLAYING` #547）。
    /// ⚠ 没放过的、已经放完的、被停掉的都返回 false —— 对程序来说都是"现在没声音"，
    ///   不必区分（要区分的话那是另一个问题，别把语义搅在一起）。
    /// </summary>
    public static bool IsPlaying()
    {
        var mp = _bgm;
        if (mp == null) return false;
        try { return mp.IsPlaying; } catch { return false; }
    }

    /// <summary>
    /// 震动一下。<paramref name="amplitude"/> = 0 表示用系统默认强度。
    ///
    /// ⚠ 需要 `android.permission.VIBRATE`（**normal 级**，装上就生效，不用运行时申请）——
    /// 清单里漏了声明的表现是"调了没反应"，而且**不抛异常**（`Vibrate` 对无权限的调用是静默失败）。
    /// </summary>
    public static bool Vibrate(int ms, int amplitude)
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26) && amplitude > 0)
            {
                var vib = VibratorService();
                if (vib == null) return false;
                vib.Vibrate(Android.OS.VibrationEffect.CreateOneShot(ms, Math.Clamp(amplitude, 1, 255)));
                return true;
            }

            // 没有强度参数（或系统太老）走 MAUI 的通用路：只有"振多久"这一个参数。
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms));
            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlAudio", "震动失败", ex);
            return false;
        }
    }

    /// <summary>
    /// 按节奏震动（毫秒序列，奇数下标=静、偶数下标=动，同 Android `Vibrator.vibrate(long[], -1)`）。
    ///
    /// MAUI 的 `Vibration` 只支持"振一下"，模式必须落到 Android 的 `Vibrator`；
    /// 自己用后台线程 sleep 逐段触发也能做，但精度差（几十毫秒级）且程序结束后还在跑，
    /// 交给系统一条波形是又准又省。
    /// </summary>
    public static bool VibratePattern(long[] pattern)
    {
        try
        {
            var vib = VibratorService();
            if (vib == null) return false;

            if (OperatingSystem.IsAndroidVersionAtLeast(26))
                vib.Vibrate(Android.OS.VibrationEffect.CreateWaveform(pattern, -1));
            else
#pragma warning disable CA1422 // 老 API，仅在新 API 不可用时走这里
                vib.Vibrate(pattern, -1);
#pragma warning restore CA1422
            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlAudio", "模式震动失败", ex);
            return false;
        }
    }

    private static Android.OS.Vibrator? VibratorService()
        => Android.App.Application.Context.GetSystemService(Android.Content.Context.VibratorService)
           as Android.OS.Vibrator;

    /// <summary>
    /// 页面消失/程序结束时全停 —— 否则退出游戏后 BGM 还在响（比不响更糟）。
    ///
    /// <para>
    /// ⚠ **收尾的次序是坑，不能改**：
    /// ① 先 <c>Panic</c> 立刻静音（否则淡出还没走完就被切，尾音是"噗"一下）；
    /// ② 置 <c>_running = false</c> —— **必须先置位**；
    /// ③ 再 <c>Stop()</c> 从另一线程把**阻塞中的 Write 解开**；
    /// ④ <c>Join</c> 带超时 —— **绝不因为音频线程卡住而冻住退出流程**；
    /// ⑤ 最后 Flush/Release。
    /// </para>
    ///
    /// <para>
    /// 反例：先 Release 再 Join ⇒ <c>Write</c> 立刻抛/返回，循环变空转烧 CPU；
    /// 先 Join 再 Stop ⇒ 死等（<c>Write</c> 永远阻塞在满缓冲上）。两者都是
    /// "退出时卡一下 / 退不掉"的经典成因。
    /// </para>
    ///
    /// <para>**幂等** —— 页面消失可能被调多次，也可能从没起过混音器。</para>
    /// </summary>
    public static void StopAll()
    {
        Synth.Panic();                       // ① 立刻静音
        _running = false;                    // ② 先置位
        var track = _mixer;
        try { track?.Stop(); } catch { }     // ③ 解开阻塞中的 Write
        try { _mixThread?.Join(200); } catch { }   // ④ 有超时，绝不冻住退出
        try { track?.Flush(); track?.Release(); } catch { }   // ⑤
        _mixer = null;
        _mixThread = null;
        Synth.Panic();                       // 音频线程可能刚写完最后一块，再清一次
        StopBgm();
    }

#elif IOS || MACCATALYST
    // ── iOS / MacCatalyst：同一套合成思路，落到 AVFoundation ──
    //
    // 与 Android 那份**语义完全一致** —— 而且现在连"合成"那部分都是**同一个类**
    // （`UI/Shared/VmlToneSynth.cs`）：两端各写各的只剩**播放**这一层
    // （`AudioTrack` vs `AVAudioEngine`）。
    //
    // ⚠ 这一段原先是"不抽音频抽象接口"的理由（"两条实现都只有几十行"）。
    //   **复音推翻了这个前提**：有了声部表、相位累加、包络、混音、限幅之后，
    //   那就不再是几十行采样循环，而是一段**两端必须逐字一致**的算法 ——
    //   否则同一个程序在两台机器上音色不同，那是最难查的一类分叉。
    //   所以边界重划：**抽「合成/混音」，不抽「播放」**。
    //
    private static AVAudioEngine? _engine;
    private static AVAudioPlayerNode? _node;
    private static AVAudioPlayer? _bgmPlayer;

    private const int SampleRate = 44100;

    /// <summary>
    /// 合成音与播放节点共用的 PCM 格式（16 位单声道 44.1kHz 非交错）。
    /// **必须是同一个实例**：连接节点用的格式与喂给 buffer 的格式一旦不同，
    /// 引擎会按连接格式重采样，音量/音高都可能变。
    /// （旧代码每次 <c>ToneCore</c> 都 new 一个、且从不 Dispose —— 缓存后顺带修掉那处泄漏。）
    /// </summary>
    private static AVAudioFormat? _format;

    /// <summary>惰性建引擎与播放节点（只建一次；建失败返回 null，调用方当"没声音"处理）。</summary>
    private static AVAudioPlayerNode? EnsureNode()
    {
        if (_node != null) return _node;
        try
        {
            // ⚠⚠ **必须配音频会话，否则"一点声音都没有"**（2026-09-27 真机报「iOS 设备上没有声音」）：
            //   iOS 的默认类别是 `SoloAmbient` —— 它**跟随静音开关**（侧边开关/响铃静音）。开关一
            //   拨到静音，App 里所有合成音**一声不响**，而且**不报错**：引擎照常 `Start` 成功、
            //   `ToneCore` 照常返回 true、日志里干干净净，就是听不见 —— 实测设备日志里一条音频
            //   错误都没有，正对上这一条。
            //   `Playback` = 忽略静音开关（游戏音效就该这样）；再加 `MixWithOthers`，
            //   免得把用户正在听的音乐掐掉（`Playback` 默认会独占）。
            //
            // ⚠ **只对 iOS 做，Mac Catalyst 上跳过**（2026-09-27 macOS 实测）：
            //   `AVAudioSession` 是 **UIKit 一致性**要求的 API —— 从非主线程调它就报
            //   `UIKitThreadAccessException: UIKit Consistency error: you are calling a UIKit method
            //   that can only be invoked from the UI thread`（本文件这两句正好**成对**触发，
            //   日志里就是同一毫秒两条）。而本方法是从 **VM / 混音线程**进来的
            //   （`ui_beep` → 宿主 → Tone），不在主线程。
            //   macOS 这边**不需要**这段：没有静音开关，也没有"类别"这回事，`Playback`/`MixWithOthers`
            //   的语义在 macOS 上是空操作 —— 拿一条会报错的调用去换一个空操作不划算。
            //   （真要在 Catalyst 上配，就得 `MainThread.InvokeOnMainThreadAsync` 包起来；
            //     那会引入"VM 线程等主线程"的阻塞，只在确有需要时才做。）
#if IOS
            var session = AVAudioSession.SharedInstance();
            session.SetCategory(AVAudioSessionCategory.Playback, AVAudioSessionCategoryOptions.MixWithOthers);
            session.SetActive(true);
#endif

            var engine = new AVAudioEngine();
            var node = new AVAudioPlayerNode();
            engine.AttachNode(node);

            // ⚠ **必须接上混音器**：`AttachNode` 只是把节点挂进图里，**不会创建输出节点**；
            // 直接 `StartAndReturnError` 会抛 ObjCException
            // （`required condition is false: inputNode != nullptr || outputNode != nullptr`），
            // 被下面的 catch 吞掉 ⇒ `EnsureNode` 恒返回 null、`ToneCore` 恒返回 false ——
            // **编译通过、一声不响、什么都没坏，就是永远没声音**。
            // 访问 `MainMixerNode` 会顺带建出混音器与输出节点，这是 AVAudioEngine 的标准接法。
            engine.Connect(node, engine.MainMixerNode, _format ??= new AVAudioFormat(AVAudioCommonFormat.PCMInt16, SampleRate, 1, false));

            if (!engine.StartAndReturnError(out var err) || err != null)
            {
                engine.Dispose();
                // ⚠ **这一路必须留痕**（2026-09-27）：原先它直接 `return null`，而调用方一律当
                //   "没声音"处理 ⇒ 症状就是那条最贵的"引擎没起来、一声不响、日志干干净净"。
                //   iOS 那次花了几小时才从"设备日志一条音频错误都没有"倒推回来；
                //   Mac Catalyst 上 `AVAudioSession` 能力有限（会话可能直接抛），更不能静默。
                ErrorLog.Error("VmlAudio", $"音频引擎启动失败：{err?.LocalizedDescription ?? "(无错误对象)"}");
                return null;
            }
            _engine = engine;
            // 起得来也记一笔（只此一次）：排查"到底出没出声"时，**有**这条才算引擎真的跑起来了 ——
            // 静默成功同样是不可判定的（见上）。
            ErrorLog.Info("VmlAudio", $"音频引擎已启动：采样率={SampleRate} 声道=1"
#if IOS
                + "（音频会话 = Playback + MixWithOthers）"
#else
                + "（Mac Catalyst 不配音频会话 —— 见 EnsureNode 里的说明）"
#endif
            );
            return _node = node;
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlAudio", "iOS 音频引擎启动失败", ex);
            return null;
        }
    }

    // ── 复音混音器（v0.96.485）────────────────────────────────────────────
    //
    // 与 Android 那份同一套结构：**一个后台线程持续把 `Synth.Mix()` 出来的块喂给播放节点**，
    // 而不是"每发一个音算一段、Stop 掉上一个"（那是结构性单通道）。
    //
    // ⚠ 差别在"背压从哪来"：Android 的 `AudioTrack.Write` 是阻塞的，缓冲一满自然等；
    //   `ScheduleBuffer` **不阻塞、只管排队** ⇒ 得自己控速，否则会一直往队列里堆。
    //   这里的做法是**按块时长 sleep**（1024 帧 ≈ 23ms）—— 简单、可控，
    //   代价是时钟会有微小漂移（长时间连续播放才看得出来）。
    //   更精确的做法是用 `ScheduleBuffer` 的完成回调驱动下一块，留作后续。

    /// <summary>喂块线程（惰性起、随 <see cref="StopAll"/> 收）。</summary>
    private static Thread? _mixThread;

    /// <summary>线程退出的唯一开关。置位顺序见 <see cref="StopAll"/> —— 反了会卡住。</summary>
    private static volatile bool _running;

    /// <summary>渲染块（帧）—— 与 Android、与桌面同值。</summary>
    private const int BlockFrames = 1024;

    private static readonly short[] MixScratch = new short[BlockFrames];

    /// <summary>起混音线程（**惰性** —— 第一次真要发声时才起；幂等）。</summary>
    private static void EnsureMixer()
    {
        if (_running) return;
        if (EnsureNode() == null) return;   // 引擎起不来 ⇒ 退化（程序照跑，只是没声音）
        _running = true;
        _mixThread = new Thread(MixLoop) { IsBackground = true, Name = "vml-audio" };
        _mixThread.Start();
    }

    private static void MixLoop()
    {
        var format = _format ??= new AVAudioFormat(AVAudioCommonFormat.PCMInt16, SampleRate, 1, false);
        var blockMs = BlockFrames * 1000 / SampleRate;
        try
        {
            // ⚠⚠ **必须留余量，不能"刚好实时"**（2026-09-27 真机：iPad 上「有的声音是破音的」）：
            //   原来每轮排完一块就 `Thread.Sleep(块长)` —— 长跑的平均速率虽然对得上，
            //   但**节点队列里始终只有一块**：线程被调度晚一次（GC、别的线程抢 CPU、
            //   系统忙）就是一个断点，听感就是"咔 / 破音"，而且**只在真机上偶发**
            //   （模拟器/桌面不经过这条路径）。
            //   现在按**队列剩余量**控速：不足 3 块就补，够了就短睡 ⇒ 常驻约 70ms 余量，
            //   足以吸收抖动；进度用节点自己的 `LastRenderTime` 算，**不是靠猜**，
            //   所以也不会无限堆积（另有 8 块的硬上限兜底，防进度读不到时胀出去）。
            const int TargetLeadBlocks = 3;
            const int MaxLeadBlocks = 8;
            long scheduled = 0;      // 已经排进队列的帧数
            long playedBase = -1;    // 播放进度的基准（首次读到的值）
            long played = 0;         // 相对基准已播出的帧数

            while (_running)
            {
                var node = _node;
                if (node == null) break;

                // 引擎的播放进度（拿不到就退化成"照旧一块一块来"）
                var rt = node.LastRenderTime;
                if (rt != null)
                {
                    if (playedBase < 0) playedBase = rt.SampleTime;
                    played = rt.SampleTime - playedBase;
                }
                if (playedBase < 0) played = scheduled;

                var lead = scheduled - played;
                if (lead > (long)BlockFrames * MaxLeadBlocks) scheduled = played;   // 兜底：进度卡住了就别再堆

                if (lead < (long)BlockFrames * TargetLeadBlocks)
                {
                    Synth.Mix(MixScratch, BlockFrames);

                    // ⚠ `AVAudioPcmBuffer` 每块新建（约 2KB）—— 换来的是不必和
                    //   "上一块还在播、这块要覆盖它"的竞态打交道。23ms 一块，GC 扛得住。
                    using var buf = new AVAudioPcmBuffer(format, BlockFrames);
                    buf.FrameLength = BlockFrames;
                    unsafe
                    {
                        // `int16ChannelData` 的 ObjC 类型是 `int16_t * const *`（通道指针数组），
                        // .NET 绑定把它收成了 `nint` ⇒ 还原成 `short**` 再取通道 0。
                        var ch = ((short**)buf.Int16ChannelData)[0];
                        for (var i = 0; i < BlockFrames; i++) ch[i] = MixScratch[i];
                    }
                    node.ScheduleBuffer(buf, (Action?)null);
                    scheduled += BlockFrames;
                    if (!node.Playing) node.Play();
                    Thread.Sleep(blockMs);
                }
                else
                {
                    Thread.Sleep(2);   // 队列够了：短睡等它播掉一点（不空转烧 CPU）
                }
            }
        }
        catch (Exception ex)
        {
            if (_running) ErrorLog.Error("VmlAudio", "iOS 混音线程异常退出", ex);
        }
    }

    /// <summary>停掉正在响的合成音 —— 现在语义是"清掉老式蜂鸣那条通道"。</summary>
    public static void StopTone() => Synth.NoteOff(VmlToneSynth.LegacyLane);

    /// <summary>
    /// 自检用：Apple 这一支的实际状态。**这是"没声音"时唯一看得见的判据** ——
    /// 引擎 `StartAndReturnError` 成功、`ToneCore` 返回 true、日志干干净净，而声音就是不出来，
    /// 只有把「引擎建没建、节点在不在播、会话是什么类别」摆出来才定得了性。
    /// </summary>
    private static string DescribePlatform()
    {
        var s = $"Apple 引擎={(_engine == null ? "**未启动（声音出不去）**" : "已启动")}"
              + $" / 播放节点={(_node == null ? "无" : _node.Playing ? "正在播" : "**建了但没在播（声音出不去）**")}";
#if IOS
        // 会话类别：`Playback` 才是"忽略静音开关"；读**实际值**比"我们设过什么"可信。
        // ⚠ `OutputVolume` 是**这条链上唯一能揭穿"系统音量被拧到 0 / 静音"的读数** ——
        //   那种情况下引擎、节点、声部全部正常，就是一个字都听不见（真机排查的头号嫌疑）。
        try
        {
            var session = AVFoundation.AVAudioSession.SharedInstance();
            s += $" / 会话类别={session.Category}"
               + $" / 输出音量={session.OutputVolume:0.00}（0 = 静音，系统音量被拧到底）";
        }
        catch (Exception ex) { s += $" / 会话读取失败：{ex.Message}"; }
#else
        s += " / 会话=不适用（Mac Catalyst 不配，见 EnsureNode 的说明）";
#endif
        return s;
    }

    /// <summary>同步 BGM 音量（`AUDIO_VOLUME` 要同时作用于 BGM 与合成器）。</summary>
    private static void SetBgmVolume(int volume)
    {
        try { if (_bgmPlayer != null) _bgmPlayer.Volume = Math.Clamp(volume, 0, 100) / 100f; }
        catch { /* 已释放：忽略 */ }
    }

    public static string? Play(string path, bool loop)
    {
        try
        {
            StopBgm();
            var player = AVAudioPlayer.FromUrl(NSUrl.FromFilename(path));
            if (player == null) return "无法打开音频文件";
            player.NumberOfLoops = loop ? -1 : 0;
            player.Volume = Math.Clamp(_volume, 0, 100) / 100f;
            player.PrepareToPlay();
            player.Play();
            _bgmPlayer = player;
            return null;
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlAudio", $"iOS BGM 播放失败：{path}", ex);
            return ex.Message;
        }
    }

    public static void StopBgm()
    {
        try { _bgmPlayer?.Stop(); } catch { }
        _bgmPlayer?.Dispose();
        _bgmPlayer = null;
    }

    /// <summary>BGM 还在放吗（`AUDIO_IS_PLAYING` #547）—— 与 Android 那侧同一条口径。</summary>
    public static bool IsPlaying()
    {
        var p = _bgmPlayer;
        if (p == null) return false;
        try { return p.Playing; } catch { return false; }
    }

    public static bool Vibrate(int ms, int amplitude)
    {
        // iOS 的震动就是"系统震动一次"，**没有时长参数**（`AudioServicesPlaySystemSound` 的性质）
        // —— 与 Android 那边给多少振多少不同。这是平台差异，不是实现偷懒：
        // iOS 上能表达的就是"振一下"。
        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms)); return true; }
        catch (Exception ex) { ErrorLog.Error("VmlAudio", "iOS 震动失败", ex); return false; }
    }

    public static bool VibratePattern(long[] pattern)
    {
        // iOS 没有"按波形震动"的公开 API，退化成"按总时长振一下"（至少节奏的时长信息还在）。
        var total = pattern.Where((_, i) => i % 2 == 0).Sum();
        return Vibrate((int)Math.Clamp(total, 1, VmlUi.VibrateMaxSegmentMs), 0);
    }

    /// <summary>
    /// 页面消失/程序结束时全停（**幂等**）。
    /// ⚠ 次序与 Android 那份同一道理：**先置开关、再停引擎、最后带超时 Join** ——
    /// 反了会死等（线程阻塞在 ScheduleBuffer 的队列上）。
    /// </summary>
    public static void StopAll()
    {
        Synth.Panic();          // ① 立刻静音（别让尾音拖到引擎停掉那一刻）
        _running = false;       // ② 先置位
        try { _node?.Stop(); } catch { }        // ③ 让线程下一轮醒来时看到 _running=false
        try { _mixThread?.Join(200); } catch { } // ④ 有超时，绝不冻住退出
        _mixThread = null;
        try { _engine?.Stop(); } catch { }
        StopBgm();
    }

#else
    // 其余平台（Windows 等）：空实现。桌面端跑 VML 走的是 TUI 那条路，
    // 根本没有 DrawWindowPage，也就没人调到这里 —— 不是"没做"，是那条路不存在。
    public static bool ToneCore(int hz, int ms, int wave, int volume) => false;
    public static void StopTone() { }
    /// <summary>自检用：这一支是空实现（桌面端走 TUI，没有绘窗那条路）。</summary>
    private static string DescribePlatform() => "该平台没有音频输出实现（桌面端走 TUI 那条路）";
    public static string? Play(string path, bool loop) => "当前平台不支持音频播放";
    public static void StopBgm() { }

    /// <summary>震动是 MAUI Essentials 的跨平台能力，非 Android 也有，照常实现。</summary>
    public static bool Vibrate(int ms, int amplitude)
    {
        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(ms)); return true; }
        catch (Exception ex) { ErrorLog.Error("VmlAudio", "震动失败", ex); return false; }
    }

    /// <summary>模式震动只有 Android 有系统级支持，其余平台退化成"振一下总时长"。</summary>
    public static bool VibratePattern(long[] pattern)
    {
        try
        {
            var total = pattern.Where((_, i) => i % 2 == 0).Sum();
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(Math.Clamp(total, 1, VmlUi.VibrateMaxSegmentMs)));
            return true;
        }
        catch (Exception ex) { ErrorLog.Error("VmlAudio", "模式震动失败", ex); return false; }
    }

    public static void StopAll() { }
#endif
}

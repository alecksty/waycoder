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

    /// <summary>设置整体音量（已由协议层钳过 0–100）。</summary>
    public static void SetVolume(int volume) => _volume = VmlUi.ClampVolume(volume);

    /// <summary>
    /// 合成音（用当前音量）—— **VM 内置 `#57` 蜂鸣走这条**。
    /// 参数先过协议层钳位：频率传 0 会让合成器算出除零/零周期，症状是"没声音"甚至卡住，
    /// 而程序那边完全看不出是参数问题。
    /// </summary>
    public static bool Tone(int hz, int ms, int wave)
    {
        var (f, d, w, _) = VmlUi.ClampTone(hz, ms, wave, _volume);
        return ToneCore(f, d, w, _volume);
    }

#if ANDROID
    /// <summary>当前正在响的合成音 —— 新的来了先把它停掉（单通道，与协议注释一致）。</summary>
    private static Android.Media.AudioTrack? _tone;

    /// <summary>BGM 播放器（惰性建）。</summary>
    private static Android.Media.MediaPlayer? _bgm;

    /// <summary>合成一段音并播放（参数已由 <see cref="VmlUi.ClampTone"/> 钳过）。</summary>
    private static bool ToneCore(int hz, int ms, int wave, int volume)
    {
        const int rate = 44100;
        try
        {
            StopTone();

            var count = rate * ms / 1000;
            var pcm = new byte[count * 2];   // 16 位单声道
            var amp = 32767.0 * Math.Clamp(volume, 0, 100) / 100.0;
            var period = (double)rate / hz;

            for (var i = 0; i < count; i++)
            {
                var phase = i / period;
                var frac = phase - Math.Floor(phase);          // 0..1 的一个周期内位置
                var v = wave switch
                {
                    1 => frac < 0.5 ? 1.0 : -1.0,               // 方波（8 位机味，音效最常用）
                    2 => 2.0 * frac - 1.0,                      // 锯齿
                    3 => 4.0 * Math.Abs(frac - 0.5) - 1.0,      // 三角
                    _ => Math.Sin(phase * 2 * Math.PI),         // 正弦
                };

                // **包络**：直接切方波头尾会有"咔"的爆音（电平瞬间从 0 跳满），
                // 加 3ms 淡入淡出就干净了 —— 这是合成音效最容易漏、又最刺耳的一处。
                var fade = Math.Min(1.0, Math.Min(i, count - 1 - i) / (rate * 0.003));
                var sample = (short)(v * amp * Math.Max(0, fade));
                pcm[i * 2] = (byte)(sample & 0xFF);
                pcm[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            var track = new Android.Media.AudioTrack(
                Android.Media.Stream.Music, rate, Android.Media.ChannelOut.Mono,
                Android.Media.Encoding.Pcm16bit, pcm.Length, Android.Media.AudioTrackMode.Static);
            track.Write(pcm, 0, pcm.Length);
            track.Play();
            _tone = track;
            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlAudio", "合成音播放失败", ex);
            return false;
        }
    }

    /// <summary>停掉正在响的合成音（没有就什么都不做）。</summary>
    public static void StopTone()
    {
        var t = _tone;
        _tone = null;
        if (t == null) return;
        try { t.Stop(); } catch { /* 已经播完自己停了：Stop 会抛，忽略 */ }
        try { t.Release(); } catch { }
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

    /// <summary>页面消失/程序结束时全停 —— 否则退出游戏后 BGM 还在响（比不响更糟）。</summary>
    public static void StopAll()
    {
        StopTone();
        StopBgm();
    }

#elif IOS || MACCATALYST
    // ── iOS / MacCatalyst：同一套合成思路，落到 AVFoundation ──
    //
    // 与 Android 那份**语义完全一致**（同样的钳位、同样的包络、同样的单通道），
    // 换的只是 API：AVAudioEngine + AVAudioPlayerNode + 一块 PCM 缓冲。
    // 之所以不抽一层"音频抽象接口"再各写一份实现：那样要多两个文件、一层虚调用，
    // 而两条实现都只有几十行、且**必须各写一遍**（平台 API 完全不同），
    // 抽了也只是把 `#if` 挪个地方。

    private static AVAudioEngine? _engine;
    private static AVAudioPlayerNode? _node;
    private static AVAudioPlayer? _bgmPlayer;

    private const int SampleRate = 44100;

    /// <summary>惰性建引擎与播放节点（只建一次；建失败返回 null，调用方当"没声音"处理）。</summary>
    private static AVAudioPlayerNode? EnsureNode()
    {
        if (_node != null) return _node;
        try
        {
            var engine = new AVAudioEngine();
            var node = new AVAudioPlayerNode();
            engine.AttachNode(node);
            if (!engine.StartAndReturnError(out var err) || err != null)
            {
                engine.Dispose();
                return null;
            }
            _engine = engine;
            return _node = node;
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlAudio", "iOS 音频引擎启动失败", ex);
            return null;
        }
    }

    private static bool ToneCore(int hz, int ms, int wave, int volume)
    {
        try
        {
            var node = EnsureNode();
            if (node == null) return false;

            var format = new AVAudioFormat(AVAudioCommonFormat.PCMInt16, SampleRate, 1, false);
            var frames = (uint)(SampleRate * ms / 1000);
            if (frames == 0) return false;

            using var buf = new AVAudioPCMBuffer(format, frames);
            buf.FrameLength = frames;

            var amp = 32767.0 * Math.Clamp(volume, 0, 100) / 100.0;
            var period = (double)SampleRate / hz;
            unsafe
            {
                var ch = buf.Int16ChannelData[0];
                for (var i = 0; i < frames; i++)
                {
                    var phase = i / period;
                    var frac = phase - Math.Floor(phase);
                    var v = wave switch
                    {
                        1 => frac < 0.5 ? 1.0 : -1.0,
                        2 => 2.0 * frac - 1.0,
                        3 => 4.0 * Math.Abs(frac - 0.5) - 1.0,
                        _ => Math.Sin(phase * 2 * Math.PI),
                    };
                    // 包络：不加的话方波头尾会有"咔"的爆音（与 Android 那份同一处理）
                    var fade = Math.Min(1.0, Math.Min(i, frames - 1 - i) / (SampleRate * 0.003));
                    ch[i] = (short)(v * amp * Math.Max(0, fade));
                }
            }

            node.Stop();
            node.ScheduleBuffer(buf);
            node.Play();
            return true;
        }
        catch (Exception ex)
        {
            ErrorLog.Error("VmlAudio", "iOS 合成音播放失败", ex);
            return false;
        }
    }

    public static void StopTone()
    {
        try { _node?.Stop(); } catch { }
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

    public static void StopAll()
    {
        StopTone();
        StopBgm();
    }

#else
    // 其余平台（Windows 等）：空实现。桌面端跑 VML 走的是 TUI 那条路，
    // 根本没有 DrawWindowPage，也就没人调到这里 —— 不是"没做"，是那条路不存在。
    public static bool ToneCore(int hz, int ms, int wave, int volume) => false;
    public static void StopTone() { }
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

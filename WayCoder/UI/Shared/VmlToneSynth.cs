using System;

namespace WayCoder.UI.Shared;

/// <summary>
/// 一个**声部**（voice）—— 混音的最小单位。一个声部 = 一个正在响的音。
///
/// <para>
/// ⚠ <c>Phase</c> **必须存在声部里、跨块累积**。老实现（<c>VmlAudio.ToneCore</c>）是
/// "每次算一段 PCM、块内用 <c>i / period</c> 求相位"——那等于**每块都从 0 重新起振**，
/// 块边界必然跳变（听感上就是"咔"），所以它不得不再加 3ms 淡入淡出去掩盖。
/// 累积相位的写法从根上没这个问题：<c>Render(64)+Render(64)</c> 与 <c>Render(128)</c>
/// **逐样本相等**（自测钉着这条）。
/// </para>
/// </summary>
internal struct VmlToneVoice
{
    /// <summary>这个槽位上有声部吗。</summary>
    public bool Active;

    /// <summary>占用的通道（0–15 是 MIDI 通道，<see cref="VmlToneSynth.LegacyLane"/> = 老式蜂鸣专用）。</summary>
    public int Channel;

    /// <summary>音符号（0–127）。老式蜂鸣声道用 -1 —— 它拿到的是频率、不是音符号。</summary>
    public int Note;

    /// <summary>波形：0 正弦 / 1 方波 / 2 锯齿 / 3 三角（与 <c>VmlUi.ClampTone</c> 的口径一致）。</summary>
    public int Wave;

    /// <summary>一周期内的位置，0..1。<b>跨块累积</b>，这是"不爆音"的根本。</summary>
    public double Phase;

    /// <summary>每样本的相位增量（<c>hz / SampleRate</c>）。</summary>
    public double PhaseInc;

    /// <summary>力度 × 音色音量，0..1。</summary>
    public double Gain;

    /// <summary>当前包络值 0..1。</summary>
    public double Env;

    /// <summary>包络阶段，见 <see cref="VmlToneSynth"/> 的常量。</summary>
    public int Stage;

    /// <summary>Attack 每样本的增量。</summary>
    public double AttackInc;

    /// <summary>Release 每样本的乘数（&lt; 1，逐样本乘 ⇒ 指数下降且只需一次乘法）。</summary>
    public double ReleaseCoef;

    /// <summary>起音序号 —— 抢占时用它判"谁最老"。</summary>
    public long Serial;

    /// <summary>
    /// 自动释放的采样数（0 = 不限，等 <see cref="VmlToneSynth.NoteOff"/>）。
    ///
    /// <para>
    /// 给**老式蜂鸣**（`ui_beep(freq, ms)`）用的 —— 它自带时长，而 `note_on` 没有。
    /// 有了这一条，`ui_beep` 的"响 25ms 就停"能在复音合成器里原样表达，
    /// 不必为它再留一条平行的发声通路。
    /// </para>
    /// </summary>
    public int HoldSamples;

    /// <summary>这个声部已经响了几个采样（配合 <see cref="HoldSamples"/> 判到点没有）。</summary>
    public int Age;
}

/// <summary>
/// **复音合成器**（纯逻辑，无平台 API）—— 一份实现，四处共用：
/// Android / iOS / 桌面 vmlcli / 主工程自测。
///
/// <para>
/// ## 为什么要抽这一层
///
/// `VmlAudio.cs` 头上原先写着"刻意不抽音频抽象接口"，理由是"两条实现都只有几十行、
/// 且必须各写一遍（平台 API 完全不同），抽了只是把 <c>#if</c> 挪个地方"。
/// **复音推翻了那个前提里的"只有几十行"**：一旦有声部表、相位累加、包络、多声部混音、
/// 限幅，那就是一段**两端必须逐字一致**的算法 —— 否则同一个程序在两台机器上音色不同，
/// 那是最难查的一类分叉。
/// </para>
///
/// <para>
/// 所以边界重新划了一次：**抽「合成/混音」（本文件），不抽「播放」**
/// （<c>AudioTrack</c> vs <c>AVAudioEngine</c> 确实各写各的更好，
/// 原判断对"播放"那一半依然成立）。
/// </para>
///
/// <para>
/// ## 线程模型
///
/// 声部表由一把锁保护：VM 线程 <see cref="NoteOn"/>/<see cref="NoteOff"/> 改表（微秒级），
/// 音频线程 <see cref="Mix"/> 读表（毫秒级）。选锁而不是无锁命令环，是因为
/// **本层要能被自测直接驱动**（自测里没有"音频线程"这个概念），复杂结构换来的那点
/// 延迟在最坏情况也就一个块的时间。将来真成为瓶颈再换命令环不迟。
/// </para>
///
/// <para>
/// ## 防爆音的四条纪律（每条都有自测钉着）
///
/// 1. **任何声部都从 <c>Env = 0</c> 起音**，attack 至少 <see cref="MinAttackMs"/> 毫秒 ——
///    从非零值起音会瞬变，那就是"咔"。
/// 2. **包络没归零就绝不回收槽位**：<see cref="NoteOff"/> 只把声部推进 Release，
///    等 <c>Env</c> 掉到 <see cref="EnvFloor"/> 以下才真正空出来。
///    （老实现是直接 <c>Stop()</c> + <c>Release()</c> 硬切 —— 那正是"咔"的来源。）
/// 3. **Release 走逐样本乘法**（预先算好一个系数），单调下降、不碰 <c>exp</c>。
/// 4. **混音后走软限幅**，不硬钳 —— 十指和弦全按下时硬钳会削出刺耳谐波。
/// </para>
/// </summary>
public sealed class VmlToneSynth
{
    // ══════════════════════════════════════════════════════════════════════
    // 常量
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>采样率（与 <c>VmlAudio.SampleRate</c> 同值 —— 两端必须一样）。</summary>
    public const int SampleRate = 44100;

    /// <summary>声部槽位数。32 够"十指和弦 + 音效 + 老式蜂鸣"还有富余。</summary>
    public const int MaxVoices = 32;

    /// <summary>
    /// **老式蜂鸣专用"通道"**（`ui_beep` / VM 内置 <c>#57</c> 走这条）。
    ///
    /// <para>
    /// 它不在 0–15 的 MIDI 通道里，是为了让 <c>ui_beep</c> 的**老语义原样保留**：
    /// 同一通道上后一个音掐掉前一个（所以"连发一串只听见最后一个"不变），
    /// 但**不再掐掉别的通道** ⇒ 游戏音效可以与和弦共存。这正是这次改造的核心诉求。
    /// </para>
    /// </summary>
    public const int LegacyLane = 16;

    /// <summary>通道号上限（不含 <see cref="LegacyLane"/>）。</summary>
    public const int MidiChannels = 16;

    /// <summary>最短 attack（毫秒）—— 再短就有可闻的起音瞬变。</summary>
    public const double MinAttackMs = 3.0;

    /// <summary>默认 attack（毫秒）。钢琴那类"敲击"音色用短的。</summary>
    public const double DefaultAttackMs = 4.0;

    /// <summary>默认 release（毫秒）。</summary>
    public const double DefaultReleaseMs = 90.0;

    /// <summary>老式蜂鸣换音时的快速释放（毫秒）—— 要短，否则"连发"会听成叠音。</summary>
    public const double LegacyReleaseMs = 5.0;

    /// <summary>包络低于它就回收槽位（-60dB 量级，听不出来）。</summary>
    public const double EnvFloor = 1e-3;

    /// <summary>软限幅的门槛（样本值）。超过它的部分按 <see cref="LimitSoftDiv"/> 压缩。</summary>
    public const int LimitThreshold = 32000;

    /// <summary>软限幅的分母 —— 越大越"软"、越不容易听出压缩。</summary>
    public const int LimitSoftDiv = 8;

    /// <summary>主音量衰减：满音量时也不让 32 个声部的理论峰值顶到 short 上限。</summary>
    public const double MasterGain = 0.5;

    private const int StageAttack = 0;
    private const int StageSustain = 1;
    private const int StageRelease = 2;

    // ══════════════════════════════════════════════════════════════════════
    // 纯函数（自测直接钉）
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// MIDI 音符号 → 频率（Hz）。十二平均律，**A4 = 69 = 440Hz**：
    /// <c>note 60（中央 C）≈ 261.63</c>、<c>0 ≈ 8.176</c>、<c>127 ≈ 12543.85</c>。
    ///
    /// <para>
    /// ⚠ 转换结果**不套 <c>ClampTone</c> 的 20–20000Hz**：那条下限存在的理由是
    /// "频率 0 会算出零周期"，而本函数恒 &gt; 0，套上去反而会把低音钳掉。
    /// </para>
    /// </summary>
    public static double NoteToHz(int note) => 440.0 * Math.Pow(2.0, (note - 69) / 12.0);

    /// <summary>
    /// 波形求值：给一个周期内的位置 <paramref name="phase"/>（0..1），返回 -1..1。
    ///
    /// <para>
    /// ⚠ **这是全仓唯一的波形实现**。原先 Android（<c>VmlAudio.cs:77-83</c>）与
    /// iOS（<c>:319-325</c>）各有一份逐行相同的 <c>switch</c> —— 那正是本仓记过多次的
    /// "同一规则两处实现"，改一处忘一处不会有任何编译期提示。
    /// </para>
    ///
    /// <para>口径与 <c>VmlUi.ClampTone</c> 一致：<c>0 与其它值都落正弦</c>、1 方波、2 锯齿、3 三角。</para>
    /// </summary>
    public static double WaveSample(int wave, double phase)
    {
        switch (wave)
        {
            case 1:  // 方波
                return phase < 0.5 ? 1.0 : -1.0;
            case 2:  // 锯齿
                return 2.0 * phase - 1.0;
            case 3:  // 三角
                return 4.0 * Math.Abs(phase - 0.5) - 1.0;
            default: // 0 与其它 = 正弦（与 ClampTone 的 0..3 口径对齐）
                return Math.Sin(2.0 * Math.PI * phase);
        }
    }

    /// <summary>
    /// 把一个 int 样本**软限幅**到 16 位范围。
    ///
    /// <para>
    /// 超过 <see cref="LimitThreshold"/> 的部分不是砍掉而是**除以 <see cref="LimitSoftDiv"/>**，
    /// 于是"响得过分"听起来是压缩、而不是削顶。硬钳在十指和弦全按下时会削出刺耳谐波
    /// （那还会污染频率判据，让"音准"验不准）。
    /// </para>
    /// </summary>
    public static short SoftLimit(int sample)
    {
        if (sample > LimitThreshold)
            sample = LimitThreshold + (sample - LimitThreshold) / LimitSoftDiv;
        else if (sample < -LimitThreshold)
            sample = -LimitThreshold + (sample + LimitThreshold) / LimitSoftDiv;

        if (sample > short.MaxValue) sample = short.MaxValue;
        else if (sample < short.MinValue) sample = short.MinValue;
        return (short)sample;
    }

    // ══════════════════════════════════════════════════════════════════════
    // 状态
    // ══════════════════════════════════════════════════════════════════════

    private readonly object _gate = new();
    private readonly VmlToneVoice[] _voices = new VmlToneVoice[MaxVoices];
    private readonly int[] _channelWave = new int[MidiChannels];   // 每通道默认波形
    private readonly long[] _channelSerial = new long[MidiChannels];
    private long _serial;
    private int _volume = 80;
    private int _maxVoices = MaxVoices;

    /// <summary>主音量 0–100（与 <c>VmlAudio.SetVolume</c> 同一条口径）。</summary>
    public int Volume
    {
        get { lock (_gate) return _volume; }
        set { lock (_gate) _volume = Math.Clamp(value, 0, 100); }
    }

    /// <summary>同时允许的声部上限（1..<see cref="MaxVoices"/>）。超过就抢占最老/最弱的。</summary>
    public int MaxVoicesLimit
    {
        get { lock (_gate) return _maxVoices; }
        set { lock (_gate) _maxVoices = Math.Clamp(value, 1, MaxVoices); }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 发声
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 在 <paramref name="ch"/> 通道上起一个音。
    ///
    /// <para>
    /// ⚠ **同一 (通道, 音符号) 只会有一个声部** —— 重复 note_on 是"重触发"（先把旧的
    /// 推进快速释放、再起新的），而不是叠加两个完全同频的声部（那只会变响、还会拍频）。
    /// </para>
    ///
    /// <paramref name="note"/> 传 -1 表示"这个音不是 MIDI 音符号"（老式蜂鸣用，
    /// 它拿到的是频率），此时按频率区分重触发。
    /// </summary>
    /// <returns>是否成功起音（通道非法返回 false）。</returns>
    /// <param name="holdMs">
    /// 自动释放的毫秒数（0 = 一直响，等 <see cref="NoteOff"/>）。
    /// **老式蜂鸣**（<c>ui_beep(freq, ms)</c>）就是靠它表达"响这么久"的。
    /// </param>
    public bool NoteOn(int ch, double hz, int note, int velocity, int wave = -1, int holdMs = 0)
    {
        if (hz <= 0 || double.IsNaN(hz) || double.IsInfinity(hz)) return false;
        if (ch < 0 || ch > LegacyLane) return false;
        if (velocity <= 0) { NoteOff(ch, note); return true; }   // 真 MIDI 语义：力度 0 = 关音

        lock (_gate)
        {
            if (wave < 0) wave = ch < MidiChannels ? _channelWave[ch] : 1;
            wave = Math.Clamp(wave, 0, 3);
            velocity = Math.Clamp(velocity, 1, 127);
            var gain = velocity / 127.0;
            var releaseMs = ch == LegacyLane ? LegacyReleaseMs : DefaultReleaseMs;

            // 同 (通道, 音符号) 的重触发：旧的走快速释放，新的另起一个槽位 ——
            // 不能就地改频率（那会让相位跳变、听出"咔"）。
            RetireSameLocked(ch, note, hz, releaseMs);

            var slot = FindFreeSlotLocked();
            _serial++;
            _voices[slot] = new VmlToneVoice
            {
                Active = true,
                Channel = ch,
                Note = note,
                Wave = wave,
                Phase = 0,
                PhaseInc = hz / SampleRate,
                Gain = gain,
                Env = 0,                                   // ⚠ 纪律 1：从 0 起音
                Stage = StageAttack,
                AttackInc = 1.0 / (SampleRate * MinAttackMs / 1000.0),
                ReleaseCoef = MakeReleaseCoef(releaseMs),
                Serial = _serial,
                HoldSamples = holdMs > 0 ? Math.Max(1, SampleRate * holdMs / 1000) : 0,
                Age = 0,
            };
            return true;
        }
    }

    /// <summary>关掉 <paramref name="ch"/> 通道上的某个音（<paramref name="note"/> = -1 表示该通道全部）。</summary>
    /// <returns>是否有声部被推进释放（"这个音本来就没在响"返回 false）。</returns>
    public bool NoteOff(int ch, int note = -1)
    {
        lock (_gate)
        {
            var any = false;
            for (var i = 0; i < _voices.Length; i++)
            {
                ref var v = ref _voices[i];
                if (!v.Active || v.Channel != ch) continue;
                if (note >= 0 && v.Note != note) continue;
                // ⚠ 已经在释放中的**不算**"这次关掉了它" —— 否则同一句 `note_off` 调两次
                //   都会返回成功，调用方没法区分"我关掉了一个音"与"那个音早就在消退了"。
                if (v.Stage == StageRelease) continue;
                ReleaseLocked(ref v);
                any = true;
            }
            return any;
        }
    }

    /// <summary>所有声部走释放（不是硬切 —— 硬切会"咔"）。</summary>
    public void AllNotesOff()
    {
        lock (_gate)
        {
            for (var i = 0; i < _voices.Length; i++)
            {
                ref var v = ref _voices[i];
                if (v.Active) ReleaseLocked(ref v);
            }
        }
    }

    /// <summary>所有声部**立即**消失（panic：用户强制停止、页面被销毁这类场合）。</summary>
    public void Panic()
    {
        lock (_gate)
        {
            for (var i = 0; i < _voices.Length; i++) _voices[i] = default;
        }
    }

    /// <summary>设置某个通道的默认波形（<c>ui_tone_wave</c>）。</summary>
    public bool SetChannelWave(int ch, int wave)
    {
        if (ch < 0 || ch >= MidiChannels) return false;
        lock (_gate) _channelWave[ch] = Math.Clamp(wave, 0, 3);
        return true;
    }

    /// <summary>
    /// **此刻在响的**声部数。
    ///
    /// <para>
    /// ⚠ **不含正在释放衰减的那些**。槽位可能还被占着（要等包络掉到阈值才回收），
    /// 但那个音已经"开始消失"了 —— 对数的人（程序、日志判据）来说它不在响。
    /// 把两者混起来，"关掉一个音之后快照还是 3 个"会让人以为 note_off 没生效。
    /// </para>
    /// </summary>
    public int ActiveVoices
    {
        get
        {
            lock (_gate) return CountSoundingLocked();
        }
    }

    private int CountSoundingLocked()
    {
        var n = 0;
        for (var i = 0; i < _voices.Length; i++)
            if (_voices[i].Active && _voices[i].Stage != StageRelease) n++;
        return n;
    }

    /// <summary>
    /// 当前在响的音的**快照**，形如 <c>"3 [60,64,67]"</c>。
    ///
    /// <para>
    /// 给桌面日志与自测共用：桌面宿主不发声，"复音真的叠加了"这件事在那边
    /// **只能从这个字符串判定** —— 三个音先后 note_on、期间没有 note_off，
    /// 快照里就该同时出现三个。
    /// </para>
    /// </summary>
    public string VoiceSnapshot()
    {
        lock (_gate)
        {
            var notes = new System.Collections.Generic.List<int>();
            for (var i = 0; i < _voices.Length; i++)
                if (_voices[i].Active && _voices[i].Stage != StageRelease) notes.Add(_voices[i].Note);
            notes.Sort();
            return $"{notes.Count} [{string.Join(",", notes)}]";
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 混音
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 把接下来 <paramref name="frames"/> 个采样填进 <paramref name="buf"/>（单声道 16 位）。
    ///
    /// <para>
    /// 音频线程每块调一次。<b>不分配、不阻塞</b>（除了那把锁），
    /// 且 <c>Mix(a)+Mix(b)</c> 与 <c>Mix(a+b)</c> 逐样本相等（相位在声部里累积）。
    /// </para>
    /// </summary>
    /// <returns>本块的峰值声部数（诊断用）。</returns>
    public int Mix(short[] buf, int frames)
    {
        if (buf == null || frames <= 0) return 0;
        if (frames > buf.Length) frames = buf.Length;

        lock (_gate)
        {
            var masterGain = MasterGain * (_volume / 100.0);
            var peakVoices = 0;

            for (var i = 0; i < frames; i++)
            {
                var acc = 0.0;
                var live = 0;

                for (var v = 0; v < _voices.Length; v++)
                {
                    ref var voice = ref _voices[v];
                    if (!voice.Active) continue;

                    // ── 到点自动释放（老式蜂鸣的"响 ms 毫秒"靠这条表达）──
                    if (voice.HoldSamples > 0 && voice.Stage != StageRelease)
                    {
                        voice.Age++;
                        if (voice.Age >= voice.HoldSamples) voice.Stage = StageRelease;
                    }

                    // ── 包络推进 ──
                    switch (voice.Stage)
                    {
                        case StageAttack:
                            voice.Env += voice.AttackInc;
                            if (voice.Env >= 1.0) { voice.Env = 1.0; voice.Stage = StageSustain; }
                            break;
                        case StageRelease:
                            voice.Env *= voice.ReleaseCoef;
                            if (voice.Env < EnvFloor) { voice.Active = false; continue; }  // 纪律 2
                            break;
                    }

                    // ── 取样 + 相位推进（⚠ 累积，不从 0 重来）──
                    acc += voice.Env * voice.Gain * WaveSample(voice.Wave, voice.Phase);
                    voice.Phase += voice.PhaseInc;
                    if (voice.Phase >= 1.0) voice.Phase -= Math.Floor(voice.Phase);
                    live++;
                }

                if (live > peakVoices) peakVoices = live;
                buf[i] = SoftLimit((int)(acc * masterGain * short.MaxValue));
            }

            return peakVoices;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 内部
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>把某一 (通道, 音符号) 上还在 Attack/Sustain 的声部推进释放。</summary>
    private void RetireSameLocked(int ch, int note, double hz, double releaseMs)
    {
        for (var i = 0; i < _voices.Length; i++)
        {
            ref var v = ref _voices[i];
            if (!v.Active || v.Channel != ch || v.Stage == StageRelease) continue;
            // note = -1（老式蜂鸣：拿到的是频率）时按相位增量比 —— 那才是"同一个音"。
            var same = note >= 0 ? v.Note == note : Math.Abs(v.PhaseInc - hz / SampleRate) < 1e-12;
            if (!same) continue;
            v.Stage = StageRelease;
            v.ReleaseCoef = MakeReleaseCoef(releaseMs);
        }
    }

    private void ReleaseLocked(ref VmlToneVoice v)
    {
        if (v.Stage == StageRelease) return;   // 已经在放了，别把包络重置回去
        v.Stage = StageRelease;
    }

    /// <summary>找一个空槽；没有就抢占（优先抢"已经在释放"的，否则抢最老的）。</summary>
    private int FindFreeSlotLocked()
    {
        var active = 0;
        var releasingLowest = -1;
        var releasingEnv = double.MaxValue;
        var oldest = 0;

        for (var i = 0; i < _voices.Length; i++)
        {
            ref var v = ref _voices[i];
            if (!v.Active) return i;
            if (v.Stage == StageRelease && v.Env < releasingEnv) { releasingEnv = v.Env; releasingLowest = i; }
            if (v.Serial < _voices[oldest].Serial) oldest = i;
            active++;
        }

        // 到达上限才抢；没到上限却一个空槽都没有是不可能的（上面已经 return 了）。
        if (active >= _maxVoices && releasingLowest >= 0) return releasingLowest;
        return oldest;
    }

    /// <summary>
    /// 每样本的 release 乘数：让包络在 <paramref name="releaseMs"/> 毫秒内降到
    /// <see cref="EnvFloor"/>。<c>pow(EnvFloor, 1/N)</c> —— 预先算一次，之后每样本一次乘法。
    /// </summary>
    private static double MakeReleaseCoef(double releaseMs)
    {
        var n = SampleRate * Math.Max(releaseMs, 0.5) / 1000.0;
        return n <= 1 ? 0.0 : Math.Pow(EnvFloor, 1.0 / n);
    }
}

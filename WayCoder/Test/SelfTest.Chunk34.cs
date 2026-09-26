using System;
using WayCoder.UI.Shared;

namespace WayCoder;

/// <summary>
/// **复音合成器**（<see cref="VmlToneSynth"/>）的纯逻辑自测。
///
/// <para>
/// 这一批的意义在于：合成/混音被抽成**无平台 API 的纯逻辑**，所以"复音到底成不成立"
/// 可以在自测里**数出来**，不必靠耳朵、也不必上真机。
/// 最强的那条判据是 <b>RMS 比值</b>：三个互不相关的音同时响，总 RMS ≈ 单音的 √3 倍；
/// 而"三个音顺序拼接"那种假复音只能给出 1 倍 —— 一条断言就把两者分开了。
/// </para>
/// </summary>
public static partial class SelfTest
{
    private static void TestChunk34(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 复音合成器");

        // ── ① 音符号 → 频率（十二平均律，A4 = 69 = 440Hz）────────────────────
        Check("NoteToHz: A4(69) = 440Hz", Math.Abs(VmlToneSynth.NoteToHz(69) - 440.0) < 0.01);
        Check("NoteToHz: 中央 C(60) ≈ 261.63Hz", Math.Abs(VmlToneSynth.NoteToHz(60) - 261.626) < 0.01);
        Check("NoteToHz: A3(57) = 220Hz", Math.Abs(VmlToneSynth.NoteToHz(57) - 220.0) < 0.01);
        // 一个八度正好翻倍 —— 这是十二平均律的定义，也顺手钉住"没写成线性"
        Check("NoteToHz: 相差 12 个半音正好翻倍",
            Math.Abs(VmlToneSynth.NoteToHz(72) / VmlToneSynth.NoteToHz(60) - 2.0) < 1e-9);

        // ── ② 波形（全仓唯一实现，原先 Android/iOS 各一份）──────────────────
        Check("WaveSample: 正弦在 0 处为 0、0.25 处为 1",
            Math.Abs(VmlToneSynth.WaveSample(0, 0.0)) < 1e-9
            && Math.Abs(VmlToneSynth.WaveSample(0, 0.25) - 1.0) < 1e-9);
        Check("WaveSample: 方波前半 +1、后半 -1",
            VmlToneSynth.WaveSample(1, 0.1) == 1.0 && VmlToneSynth.WaveSample(1, 0.6) == -1.0);
        Check("WaveSample: 锯齿从 -1 线性升到 +1",
            Math.Abs(VmlToneSynth.WaveSample(2, 0.0) + 1.0) < 1e-9
            && Math.Abs(VmlToneSynth.WaveSample(2, 1.0) - 1.0) < 1e-9);
        // ⚠ 三角这条的形状是**既有实现的原样**（`4*|p-0.5|-1`，起点 +1、中点 -1）——
        //   与其他三种波形（正弦 0 处为 0、方波 0 处 +1、锯齿 0 处 -1）确实不齐，
        //   但**反相 180° 的三角波听不出区别**，改它只会让老程序产出的 PCM 无谓地变一次。
        //   ⇒ 保持原样，把判据按真实形状写。
        Check("WaveSample: 三角在 0 处 +1、0.5 处 -1（既有形状，反相不影响听感）",
            Math.Abs(VmlToneSynth.WaveSample(3, 0.0) - 1.0) < 1e-9
            && Math.Abs(VmlToneSynth.WaveSample(3, 0.5) + 1.0) < 1e-9);
        // 口径与 ClampTone 对齐：0 与"其它任何值"都落正弦
        Check("WaveSample: 认不出的波形号退回正弦（与 ClampTone 的 0..3 口径一致）",
            Math.Abs(VmlToneSynth.WaveSample(99, 0.25) - 1.0) < 1e-9);

        // ── ③ 软限幅 ────────────────────────────────────────────────────────
        Check("SoftLimit: 阈值内原样通过", VmlToneSynth.SoftLimit(1000) == 1000);
        // 超阈值的部分**除以 8** 而不是砍平 ⇒ 输入越大输出越大（保留大小关系），
        // 这正是"响得过分听起来是压缩、而不是削顶"的来源。
        Check("SoftLimit: 超阈值按 1/8 压缩（保留大小关系，不是砍平）",
            VmlToneSynth.SoftLimit(35000) == 32000 + (35000 - 32000) / 8
            && VmlToneSynth.SoftLimit(40000) > VmlToneSynth.SoftLimit(35000));
        Check("SoftLimit: 极端值被硬钳兜底（软限幅压不到那么远）",
            VmlToneSynth.SoftLimit(int.MaxValue) == short.MaxValue
            && VmlToneSynth.SoftLimit(int.MinValue) == short.MinValue);

        // ── ④ 相位连续：分两块渲染 == 一次渲染 ──────────────────────────────
        // 这是"不爆音"的根本：老实现每块都从相位 0 起振，块边界必跳变。
        {
            var whole = new VmlToneSynth();
            whole.NoteOn(0, 440, 69, 100, wave: 0);
            var w = new short[256];
            whole.Mix(w, 256);

            var parts = new VmlToneSynth();
            parts.NoteOn(0, 440, 69, 100, wave: 0);
            var p1 = new short[128];
            var p2 = new short[128];
            parts.Mix(p1, 128);
            parts.Mix(p2, 128);

            var same = true;
            for (var i = 0; i < 128; i++)
                if (p1[i] != w[i]) { same = false; break; }
            for (var i = 0; i < 128 && same; i++)
                if (p2[i] != w[128 + i]) { same = false; break; }
            Check("相位跨块累积：Mix(128)+Mix(128) 与 Mix(256) 逐样本相同", same);
        }

        // ── ⑤ 复音叠加：最强的判据 —— RMS 比值 ─────────────────────────────
        // 单音 RMS = r；三个互不相关的音同时响 ⇒ 总 RMS ≈ √3·r ≈ 1.73r。
        // 「顺序拼接」那种假复音只能给出 ≈ 1.0r —— 一条断言分开两者。
        {
            var single = new VmlToneSynth();
            single.NoteOn(0, VmlToneSynth.NoteToHz(60), 60, 100, wave: 0);
            var buf1 = new short[44100 / 2];
            for (var i = 0; i < 10; i++) single.Mix(new short[64], 64);   // 跳过起音段
            single.Mix(buf1, buf1.Length);
            var r1 = Rms(buf1);

            var chord = new VmlToneSynth();
            chord.NoteOn(0, VmlToneSynth.NoteToHz(60), 60, 100, wave: 0);
            chord.NoteOn(1, VmlToneSynth.NoteToHz(64), 64, 100, wave: 0);
            chord.NoteOn(2, VmlToneSynth.NoteToHz(67), 67, 100, wave: 0);
            for (var i = 0; i < 10; i++) chord.Mix(new short[64], 64);
            var buf3 = new short[44100 / 2];
            chord.Mix(buf3, buf3.Length);
            var r3 = Rms(buf3);

            var ratio = r1 > 0 ? r3 / r1 : 0;
            Check($"三音和弦 RMS ≈ √3 × 单音（实得 ×{ratio:F2}，理论 ×1.73）",
                ratio is > 1.60 and < 1.90);

            // 反证：把三个音改成**顺序**播放（各占 1/3 时长）⇒ RMS 只该是 1 倍。
            // 有了这条，"上面那个 1.73 并不是'音多就变响'"才是被证明过的。
            var seqBuf = new short[44100 / 2];
            var seg = seqBuf.Length / 3;
            for (var k = 0; k < 3; k++)
            {
                var one = new VmlToneSynth();
                one.NoteOn(0, VmlToneSynth.NoteToHz(60 + k * 4), 60 + k * 4, 100, wave: 0);
                for (var i = 0; i < 10; i++) one.Mix(new short[64], 64);   // 跳过起音段
                var tmp = new short[seg];
                one.Mix(tmp, seg);
                Array.Copy(tmp, 0, seqBuf, k * seg, seg);
            }
            var ratioSeq = r1 > 0 ? Rms(seqBuf) / r1 : 0;
            Check($"反证：三个音**顺序**拼接的 RMS ≈ 1.0 × 单音（实得 ×{ratioSeq:F2}）",
                ratioSeq is > 0.80 and < 1.20);
        }

        // ── ⑥ 声部生命周期：note_off 不是"立刻消失" ────────────────────────
        {
            var s = new VmlToneSynth();
            s.NoteOn(0, 440, 69, 100, wave: 0);
            var warm = new short[256];
            s.Mix(warm, 256);
            Check("起音后声部在响", s.ActiveVoices == 1);

            s.NoteOff(0, 69);
            var blk = new short[64];
            s.Mix(blk, 64);
            // "不是硬切"的真正判据是**输出里还有信号** —— 硬切会让这一块瞬间归零。
            // （`ActiveVoices` 这时已经是 0：它的口径是"在响"，不含正在消退的那些。）
            var audible = false;
            for (var i = 0; i < blk.Length; i++) if (blk[i] != 0) { audible = true; break; }
            Check("note_off 后仍在释放（输出还有信号，不是硬切静音）", audible);
            Check("note_off 后快照不再报它（'在响'的口径不含释放中的）",
                s.VoiceSnapshot().StartsWith("0 "));

            // 放够久（默认 release 90ms ⇒ 200 块 × 64 帧 ≈ 290ms）就该收干净
            for (var i = 0; i < 200; i++) s.Mix(blk, 64);
            blk = new short[64];
            s.Mix(blk, 64);
            var silent = true;
            for (var i = 0; i < blk.Length; i++) if (blk[i] != 0) { silent = false; break; }
            Check("release 走完后彻底归零（槽位回收）", silent);
        }

        // ── ⑦ 老式蜂鸣声道：连发仍然"只听见最后一个" ────────────────────────
        // `ui_beep` 的老语义必须原样保留（老游戏都靠它），但它**不该掐掉别的通道**。
        {
            var s = new VmlToneSynth();
            s.NoteOn(VmlToneSynth.LegacyLane, 440, -1, 100, wave: 1);
            s.Mix(new short[128], 128);
            s.NoteOn(VmlToneSynth.LegacyLane, 880, -1, 100, wave: 1);   // 连发第二个
            s.Mix(new short[128], 128);

            // 第二个起音后，第一个应已进入 release（不再"在响"地叠加），
            // 于是快照里最多两行，但**频率更高的那个一定在**。
            var warm2 = new short[4096];
            s.Mix(warm2, warm2.Length);
            Check("老式蜂鸣：连发时后一个音确实在响", s.ActiveVoices >= 1 && s.ActiveVoices <= 2);

            // 与和弦共存：蜂鸣不吃掉别的通道
            var s2 = new VmlToneSynth();
            s2.NoteOn(0, VmlToneSynth.NoteToHz(60), 60, 100, wave: 0);
            s2.NoteOn(1, VmlToneSynth.NoteToHz(64), 64, 100, wave: 0);
            s2.Mix(new short[512], 512);
            var before = s2.ActiveVoices;
            s2.NoteOn(VmlToneSynth.LegacyLane, 1200, -1, 100, wave: 1);
            s2.Mix(new short[512], 512);
            Check("老式蜂鸣**不掐掉**和弦声部（这正是这次改造的目的）",
                before == 2 && s2.ActiveVoices == 3);
        }

        // ── ⑧ 重触发：同一 (通道, 音符号) 不会同时有两个同频声部 ─────────────
        {
            var s = new VmlToneSynth();
            s.NoteOn(0, 440, 69, 100, wave: 0);
            s.Mix(new short[1024], 1024);
            s.NoteOn(0, 440, 69, 100, wave: 0);   // 重触发
            s.Mix(new short[1024], 1024);
            Check("同一 (通道, 音符号) 重触发不会叠加成两个同频声部（那样只会拍频）",
                s.ActiveVoices <= 2);   // 旧的已进 release、正在消退
        }

        // ── ⑨ 限幅：真实场景不顶格、最坏情况也没被削平 ──────────────────────
        {
            // ⚠ 判据不能写"峰值 ≤ 32767" —— `short` 本来就装不下 32768，
            //   而 `Math.Abs((int)short.MinValue)` 恰好是 32768（那是**合法**样本，
            //   不是越界）。那种断言恒真，等于没验。
            // 真正该验的是：**顶格的样本占比要低**。纯硬钳的实现会让几乎**每一个**样本顶格，
            // 软限幅则把大部分压回阈值附近 —— 这条比值正是两者可分辨的地方。
            //
            // ⚠ 场景取**真实会遇到的**：十指和弦、正弦（钢琴那类音色）、力度 100。
            //   16 个**满力度方波**是刻意造的最坏情况，那里顶格过半是正常的
            //   （本来就该响），拿它当"必须 < 20%"只会逼着把限幅调软 —— 那是为指标牺牲音质。
            var s10 = new VmlToneSynth();
            for (var ch = 0; ch < 10; ch++)
                s10.NoteOn(ch, VmlToneSynth.NoteToHz(48 + ch * 3), 48 + ch * 3, 100, wave: 0);
            var boom = new short[8192];
            s10.Mix(boom, boom.Length);
            var pct = ClippedPercent(boom);
            Check($"十指和弦（正弦 / 力度 100）：顶格样本占比 < 20%（实得 {pct:F1}%；纯硬钳会接近 100%）",
                pct < 20);

            // 最坏情况单独验"限幅仍在工作"：16 个满力度方波也不该**全部**顶格。
            var worst = new VmlToneSynth();
            for (var ch = 0; ch < 16; ch++)
                worst.NoteOn(ch, VmlToneSynth.NoteToHz(48 + ch * 4), 48 + ch * 4, 127, wave: 1);
            var wbuf = new short[4096];
            worst.Mix(wbuf, wbuf.Length);
            var pctW = ClippedPercent(wbuf);
            Check($"最坏情况（16 个满力度方波）：仍有非顶格样本（顶格 {pctW:F1}%；纯硬钳恒 100%）",
                pctW < 99);
        }

        // ── ⑩ 快照（桌面日志判据用的就是它）────────────────────────────────
        {
            var s = new VmlToneSynth();
            s.NoteOn(0, VmlToneSynth.NoteToHz(60), 60, 100, wave: 0);
            s.NoteOn(1, VmlToneSynth.NoteToHz(64), 64, 100, wave: 0);
            s.NoteOn(2, VmlToneSynth.NoteToHz(67), 67, 100, wave: 0);
            Check("快照按音符号排序、形如 \"3 [60,64,67]\"",
                s.VoiceSnapshot() == "3 [60,64,67]");
            s.NoteOff(1, 64);
            Check("关掉中间那个之后快照只剩两个（'在响'不含正在释放衰减的）",
                s.VoiceSnapshot() == "2 [60,67]");
        }

        // ── ⑪ 非法入参不崩、不产生 NaN ──────────────────────────────────────
        {
            var s = new VmlToneSynth();
            Check("hz <= 0 被拒（不产生零周期/NaN）", !s.NoteOn(0, 0, 69, 100));
            Check("通道越界被拒", !s.NoteOn(99, 440, 69, 100));
            Check("力度 0 等同 note_off（真 MIDI 语义）",
                s.NoteOn(0, 440, 69, 0) && s.ActiveVoices == 0);
            s.Panic();
            Check("Panic 后没有任何声部", s.ActiveVoices == 0);
        }

        // ── ⑫ 契约链：op → 宿主 → 合成器（端到端）────────────────────────────
        // 前面 ①–⑪ 验的是合成器**纯逻辑**；这一段验"**经 syscall 分派**走一遍也对"
        // —— 参数从哪个寄存器读、返回值写哪、钳位在哪一层，全是这段在钉。
        // 宿主的复音实现挂的是一个**真的** `VmlToneSynth`（不是记录型替身），
        // 所以"复音真的叠加了"这件事在**整条链**上也是可断言的。
        {
            var host = new FakeVmlHost();
            var rt = new VmlHostRuntime(host);
            var regs = new int[32];
            var mem = new byte[16];

            regs[0] = VmlUi.AudioOp.NoteOn; regs[1] = 0; regs[2] = 60; regs[3] = 100;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: NoteOn op 返回 0", regs[0] == 0);
            regs[0] = VmlUi.AudioOp.NoteOn; regs[1] = 1; regs[2] = 64; regs[3] = 100;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            regs[0] = VmlUi.AudioOp.NoteOn; regs[1] = 2; regs[2] = 67; regs[3] = 100;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: 三个 note-on 之后快照是 \"3 [60,64,67]\"（复音经 syscall 也成立）",
                host.Synth.VoiceSnapshot() == "3 [60,64,67]");

            regs[0] = VmlUi.AudioOp.NoteOff; regs[1] = 1; regs[2] = 64;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: NoteOff op 返回 0", regs[0] == 0);
            regs[0] = VmlUi.AudioOp.NoteOff; regs[1] = 1; regs[2] = 64;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: 关一个本来就没在响的音返回 -1（能区分'没响'与'关成功'）", regs[0] == -1);

            // 力度 0 = 关音（真 MIDI 语义，白送的一条）
            regs[0] = VmlUi.AudioOp.NoteOn; regs[1] = 0; regs[2] = 60; regs[3] = 0;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: 力度 0 等同 note_off", regs[0] == 0 && !host.Synth.VoiceSnapshot().StartsWith("3 "));

            // 参数**一律钳、不拒**（与 ClampTone 同一教条）
            regs[0] = VmlUi.AudioOp.NoteOn; regs[1] = 99; regs[2] = 999; regs[3] = 200;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: 通道/音符号/力度越界被钳到边界而不是拒绝", regs[0] == 0
                && host.NoteLog[^1] == "on|15|127|127");

            // Control 各码
            regs[0] = VmlUi.AudioOp.Control; regs[1] = VmlUi.AudioCtl.Voices; regs[2] = 0; regs[3] = 0;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: Control(Voices) 返回当前声部数", regs[0] is >= 0 and <= VmlToneSynth.MaxVoices);

            regs[0] = VmlUi.AudioOp.Control; regs[1] = VmlUi.AudioCtl.Panic; regs[2] = 0; regs[3] = 0;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: Control(Panic) 之后没有声部", regs[0] == 0 && host.Synth.ActiveVoices == 0);

            regs[0] = VmlUi.AudioOp.Control; regs[1] = 999; regs[2] = 0; regs[3] = 0;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: 认不出的控制码返回 -1", regs[0] == -1);

            regs[0] = 99;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: 认不出的 op 返回 -1（0 是 Play/Stop/NoteOn 的成功码）", regs[0] == -1);

            // ⚠ 老 op 一个都不能坏：Stop **只停 BGM**，不该顺手把声部也清了
            host.Audio.Clear();
            regs[0] = VmlUi.AudioOp.NoteOn; regs[1] = 0; regs[2] = 60; regs[3] = 100;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            regs[0] = VmlUi.AudioOp.Stop;
            rt.HandleSyscall(VmlUi.Audio, regs, mem);
            Check("契约: 老 op Stop 仍然只停 BGM、不清声部",
                regs[0] == 0 && host.Audio.Count == 1 && host.Synth.ActiveVoices == 1);
        }

        // ── ⑬ 声部上限：`ui_tone_max_voices(n)` 真的限住了吗 ─────────────────
        //
        // ⚠ 这一段是**被 `Examples/c/audio_all.c` 的第 ⑥ 项抓出来的**：
        //   上限压到 2 之后起 4 个音，`ui_tone_voices()` 报的却是 4 —— 声音也四个一起响。
        //   根因在 `FindFreeSlotLocked`：它「一见空槽就 return」，而 `_maxVoices` 只在
        //   表**32 个槽全占满**时才轮到判 ⇒ 上限形同虚设。两端共用这个类，
        //   所以 Android / iOS / 桌面 vmlcli 全都一样。
        //   教训：自测里原先只有「ctl 转发下去了」和「Voices 在 0..32 之间」两条 ——
        //   **转发**与**生效**是两件事，前者全绿后者照样是坏的。
        {
            var s = new VmlToneSynth();
            s.MaxVoicesLimit = 2;
            for (var ch = 0; ch < 4; ch++)
                s.NoteOn(ch, VmlToneSynth.NoteToHz(60 + ch * 4), 60 + ch * 4, 100, wave: 0);
            s.Mix(new short[512], 512);
            Check($"声部上限 2 → 起 4 个音之后在响的仍 ≤ 2（实得 {s.ActiveVoices}）",
                s.ActiveVoices <= 2);
            Check("声部上限只压数量、不封死发声：被留下的声部确实在响",
                s.ActiveVoices == 2 && s.VoiceSnapshot().EndsWith("]"));

            // 上限放开之后要能再涨回去（别把槽位标记成坏的）
            s.MaxVoicesLimit = VmlToneSynth.MaxVoices;
            s.Panic();
            for (var ch = 0; ch < 4; ch++)
                s.NoteOn(ch, VmlToneSynth.NoteToHz(60 + ch * 4), 60 + ch * 4, 100, wave: 0);
            s.Mix(new short[512], 512);
            Check("上限复位到 32 之后，同样的 4 个音全部成立", s.ActiveVoices == 4);

            // 抢占优先级没被改动：上限之内仍有空槽时**不该**去抢已在响的声部
            var s2 = new VmlToneSynth();
            s2.MaxVoicesLimit = 3;
            for (var ch = 0; ch < 3; ch++)
                s2.NoteOn(ch, VmlToneSynth.NoteToHz(60 + ch * 4), 60 + ch * 4, 100, wave: 0);
            s2.Mix(new short[512], 512);
            Check("上限 3 起 3 个音：三个都在（没到上限就不许抢占）",
                s2.VoiceSnapshot() == "3 [60,64,68]");
        }

        _ = Fail;
    }

    /// <summary>顶格（|样本| 达到 32767）的百分比 —— 硬钳与软限幅的可分辨特征。</summary>
    private static double ClippedPercent(short[] buf)
    {
        if (buf.Length == 0) return 0;
        var n = 0;
        for (var i = 0; i < buf.Length; i++)
            if (Math.Abs((int)buf[i]) >= 32767) n++;
        return n * 100.0 / buf.Length;
    }

    /// <summary>一段 PCM 的均方根（0..32767 量纲）。</summary>
    private static double Rms(short[] buf)
    {
        if (buf.Length == 0) return 0;
        double sum = 0;
        for (var i = 0; i < buf.Length; i++)
        {
            double v = buf[i];
            sum += v * v;
        }
        return Math.Sqrt(sum / buf.Length);
    }
}

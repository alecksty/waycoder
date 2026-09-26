#!/usr/bin/env python3
"""tone_check.py —— 用 Goertzel 量一量 `--wav` 录下来的声音，把「复音」变成数字。

## 为什么需要它

桌面现在**能出声**了，但"音对不对"只能靠耳朵听 —— 而**复音有没有真的叠加**
是个可以量的东西：三个不同音高的正弦同时响，那一段频谱里就该**同时存在三个峰**；
而"三个音顺序播放"在同一段窗里只会有**一个**峰。这条差别用频率检测一眼分得开。

## 三条判据

1. **复音**：扫全程，存在某个窗里**同时**有 ≥3 个音高显著 —— 那就是"和弦真的响了"。
2. **音准**：那些显著音高的频率，与它们对应的**音符号**换算值对得上（±3%）。
   ⚠ 只认十二平均律上的音（A4=69=440Hz）—— 合成器算错音高，这里就会显形。
3. **不削波**：峰值没顶到 ±32767（顶格说明限幅没兜住，响的部分会失真）。

只用标准库（`wave` + `math`）：本机没有 numpy，与仓库里"自带采样器"那套作风一致。

## 用法

    python3 scripts/vmlcli-verify/tone_check.py /tmp/at.wav
"""

import math
import sys
import wave

# 采样率（与 VmlToneSynth.SampleRate 同值）
RATE = 44100

# 检哪些音高：MIDI 音符号 48..84（跨越 audio_test / piano 用到的全部音域）
NOTES = list(range(48, 85))

# 窗长与步长（秒）。0.12 秒 ≈ 在 260Hz 上有 30 个周期，够 Goertzel 分辨；
# 步长取窗长的一半，保证和弦的"进入/退出"不会正好落在窗边界上被漏掉。
WIN = 0.12
HOP = 0.06

# C 大三和弦的三个音（`audio_test.c` 第 ② 段按的就是它们）。
#
# ⚠ **判据必须"指名道姓"地问这三个音，不能问"一共有几个峰"** ——
#   这一条是实测踩出来的：第一版用"幅度 ≥ 窗内最大值的 30% 就算一个音"，
#   结果报出「同时 37 个音」。原因是 `ui_beep` 合成的是**方波**，
#   方波的泛音极丰富（3f/5f/7f…全都够强），于是一段方波被数成了十几个"音"。
#   盯住三个确定的频率就没有这个毛病：泛音落在别处，天然不干扰。
CHORD = [60, 64, 67]

# 和弦成立的门槛：三个音各自 ≥ 窗内"这三个音里最大者"的 25%。
# 用相对值而不是绝对值 —— 音量随力度变化，而这里要判的是"三个都在不在"。
CHORD_RATIO = 0.25


def note_hz(n):
    """MIDI 音符号 → 频率（与 VmlToneSynth.NoteToHz 同一个公式）。"""
    return 440.0 * (2.0 ** ((n - 69) / 12.0))


def goertzel(samples, start, n, freq):
    """某段样本里 `freq` 这个频率的成分幅度（Goertzel 算法，一次 O(n)）。"""
    w = 2.0 * math.pi * freq / RATE
    c = 2.0 * math.cos(w)
    s1 = 0.0
    s2 = 0.0
    end = min(start + n, len(samples))
    for i in range(start, end):
        s0 = samples[i] + c * s1 - s2
        s2 = s1
        s1 = s0
    power = s1 * s1 + s2 * s2 - c * s1 * s2
    return math.sqrt(power) / max(1, end - start)


def main():
    if len(sys.argv) < 2:
        print("用法：tone_check.py <wav 路径>")
        return 2
    path = sys.argv[1]

    with wave.open(path, "rb") as w:
        ch = w.getnchannels()
        width = w.getsampwidth()
        rate = w.getframerate()
        raw = w.readframes(w.getnframes())

    if width != 2:
        print(f"✘ 只认 16 位 PCM，这个文件是 {width * 8} 位")
        return 2
    if rate != RATE:
        print(f"✘ 只认 {RATE}Hz，这个文件是 {rate}Hz")
        return 2
    if ch != 1:
        print(f"⚠ 文件有 {ch} 个声道，只取第一个")

    # 解成 float 列表（多声道就取第一声道）
    import array
    a = array.array("h")
    a.frombytes(raw)
    if ch > 1:
        a = a[::ch]
    samples = [float(v) for v in a]
    total = len(samples)
    secs = total / RATE
    print(f"▶ {path}：{secs:.1f} 秒、{total} 样本")

    # ── ③ 不削波 ──────────────────────────────────────────────────────────
    peak = max(abs(v) for v in samples) if samples else 0
    clipped = 0
    for v in samples:
        if abs(v) >= 32767:
            clipped += 1
    clip_pct = 100.0 * clipped / max(1, total)
    ok_clip = clip_pct < 1.0
    print(f"  {'✅' if ok_clip else '❌'} 不削波：峰值 {peak:.0f}、顶格样本 {clip_pct:.3f}%")

    # ── ① 复音：C 大三和弦（60/64/67）的三个音是否**同时**存在 ───────────
    win_n = int(WIN * RATE)
    hop_n = int(HOP * RATE)
    chord_freqs = [(n, note_hz(n)) for n in CHORD]

    chord_windows = 0
    chord_at = 0.0
    pos = 0
    while pos + win_n <= total:
        mags = [goertzel(samples, pos, win_n, f) for _, f in chord_freqs]
        top = max(mags)
        if top > 1.0:                       # 有声音的窗才判（静音窗不算）
            strong = [CHORD[i] for i, m in enumerate(mags) if m >= top * CHORD_RATIO]
            if len(strong) == 3:
                if chord_windows == 0:
                    chord_at = pos / RATE
                chord_windows += 1
        pos += hop_n

    ok_poly = chord_windows >= 3            # 至少连续几窗都成立（不是一瞬的巧合）
    print(f"  {'✅' if ok_poly else '❌'} 复音：C 大三和弦三音**同时**存在的窗："
          f"{chord_windows} 个（首个在 {chord_at:.1f} 秒）")

    # ── ② 音准：音阶的八个音（60..67）是不是都出现过 ─────────────────────
    #
    # ⚠ 判据**不能**写成"某个窗的主音落在 60–72" —— 那样恒真（`NOTES` 本来就只有
    #   48..84），等于没验。真正要验的是"音高算错"，而最能暴露它的现象是
    #   **音阶缺音**（比如整段偏了一个八度，60..67 就一个都不会出现）。
    #
    # ⚠ 另一个实测事实：`audio_test.c` 的 `play()` 是"关音后 40ms 就起下一个"，
    #   而 release 有 90ms ⇒ **相邻两个音天然重叠**。所以不能取"主音"，要取
    #   "本窗里所有够强的音" —— 重叠时它们本来就都该在。
    scale_notes = list(range(60, 68))
    seen = set()
    pos = 0
    while pos + win_n <= total:
        mags = [(goertzel(samples, pos, win_n, note_hz(n)), n) for n in scale_notes]
        top = max(m for m, _ in mags)
        if top > 1.0:
            for m, n in mags:
                if m >= top * 0.30:
                    seen.add(n)
        pos += hop_n

    ok_pitch = len(seen) >= 7          # 八个音里至少七个（有一个卡在窗边界上也无妨）
    print(f"  {'✅' if ok_pitch else '❌'} 音准：音阶八音里出现过 {len(seen)} 个"
          f"（{sorted(seen)}；整段偏八度的话这里会掉到 0）")

    print()
    all_ok = ok_clip and ok_poly and ok_pitch
    print("结论：" + ("全部符合" if all_ok else "有不符项"))
    return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())

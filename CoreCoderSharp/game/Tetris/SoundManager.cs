using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tetris;

/// <summary>
/// 音效管理器：程序启动时生成一组短 WAV 音效到临时目录，
/// 播放时调用平台自带播放器（macOS: afplay / Linux: paplay / Windows: powershell），
/// 无外部依赖、播放失败静默忽略。
/// </summary>
public static class SoundManager
{
    private const int SampleRate = 44100;

    /// <summary>音效开关。</summary>
    public static bool Enabled { get; set; } = true;

    private static readonly Dictionary<string, string> Files = new();
    private static bool _initialized;

    /// <summary>在临时目录生成音效文件（幂等）。</summary>
    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "tetris-sounds");
            Directory.CreateDirectory(dir);
            Files["move"] = WriteWav(Path.Combine(dir, "move.wav"), MakeTone(60, 420));
            Files["rotate"] = WriteWav(Path.Combine(dir, "rotate.wav"), MakeTone(90, 620));
            Files["drop"] = WriteWav(Path.Combine(dir, "drop.wav"), MakeTone(120, 260));
            Files["clear"] = WriteWav(Path.Combine(dir, "clear.wav"), MakeClearSweep());
            Files["hold"] = WriteWav(Path.Combine(dir, "hold.wav"), MakeTone(70, 520));
            Files["over"] = WriteWav(Path.Combine(dir, "over.wav"), MakeOverSound());
        }
        catch
        {
            Files.Clear(); // 生成失败则不播放
        }
    }

    /// <summary>播放指定音效（异步，不阻塞）。</summary>
    public static void Play(string name)
    {
        if (!Enabled || !_initialized) return;
        if (!Files.TryGetValue(name, out var path)) return;

        try
        {
            var psi = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };
            if (OperatingSystem.IsMacOS())
            {
                psi.FileName = "afplay";
                psi.ArgumentList.Add(path);
            }
            else if (OperatingSystem.IsLinux())
            {
                psi.FileName = "paplay";
                psi.ArgumentList.Add(path);
            }
            else if (OperatingSystem.IsWindows())
            {
                psi.FileName = "powershell";
                psi.ArgumentList.Add("-c");
                psi.ArgumentList.Add($"(New-Object Media.SoundPlayer '{path.Replace("'", "''")}').Play()");
            }
            else return;

            Process.Start(psi);
        }
        catch
        {
            // 播放失败静默忽略
        }
    }

    // ── WAV 生成 ──────────────────────────────────────────────

    private static string WriteWav(string path, short[] samples)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs, Encoding.UTF8);
        int dataSize = samples.Length * 2;
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataSize);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);                       // fmt 块大小
        bw.Write((short)1);                 // PCM
        bw.Write((short)1);                 // 单声道
        bw.Write(SampleRate);
        bw.Write(SampleRate * 2);           // 字节率
        bw.Write((short)2);                 // 块对齐
        bw.Write((short)16);                // 位深
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);
        foreach (var s in samples) bw.Write(s);
        return path;
    }

    /// <summary>生成一段带淡入淡出的正弦音。</summary>
    private static short[] MakeTone(int ms, double freq)
    {
        int n = SampleRate * ms / 1000;
        var buf = new short[n];
        double fade = Math.Min(n / 4, 800);
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / SampleRate;
            double env = Math.Min(1.0, Math.Min(i / fade, (n - i) / fade));
            buf[i] = (short)(Math.Sin(2 * Math.PI * freq * t) * env * 12000);
        }
        return buf;
    }

    /// <summary>消行音：频率上扫 + 谐波，更明亮。</summary>
    private static short[] MakeClearSweep()
    {
        int ms = 260;
        int n = SampleRate * ms / 1000;
        var buf = new short[n];
        double fade = Math.Min(n / 4, 1000);
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / SampleRate;
            double f = 500 + 900 * ((double)i / n); // 500→1400Hz 上扫
            double env = Math.Min(1.0, Math.Min(i / fade, (n - i) / fade));
            double s = Math.Sin(2 * Math.PI * f * t) * 0.8
                     + Math.Sin(2 * Math.PI * f * 2 * t) * 0.2;
            buf[i] = (short)(s * env * 12000);
        }
        return buf;
    }

    /// <summary>游戏结束音：下行双音。</summary>
    private static short[] MakeOverSound()
    {
        int ms = 500;
        int n = SampleRate * ms / 1000;
        var buf = new short[n];
        for (int i = 0; i < n; i++)
        {
            double t = (double)i / SampleRate;
            double f = i < n / 2 ? 300 : 200; // 300Hz → 200Hz
            double env = 1.0 - (double)i / n;
            buf[i] = (short)(Math.Sin(2 * Math.PI * f * t) * env * 11000);
        }
        return buf;
    }
}

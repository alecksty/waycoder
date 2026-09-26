using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace VmlCli;

/// <summary>
/// **桌面音频输出** —— 让 `vmlcli` 真的发出声音（在此之前它只往 stderr 打一行日志）。
///
/// <para>
/// ## 为什么走平台原生 API
///
/// 与仓库既有做法一致（`WinConsoleMode` 直接 P/Invoke `kernel32`）：
/// **零第三方依赖、AOT 安全、真·实时复音**。
/// 引入 NAudio / PortAudio 那类绑定能少写几百行，但会带进外部依赖，
/// 对"单文件 + 跨平台"这个前提是净负担。
/// </para>
///
/// <para>
/// ## 它和 <see cref="VmlToneSynth"/> 的分工
///
/// 合成器（`UI/Shared/VmlToneSynth.cs`）是**纯逻辑**，与手机端同一份；
/// 本文件只管"把混好的 PCM 塞给声卡"。所以桌面听到的音色与手机**逐样本一致**。
/// </para>
///
/// <para>
/// ## 无音频设备时要优雅退化
///
/// 起不来就**只打日志、不抛**：CI / 容器 / 远程会话里跑 `vmlcli` 是常态，
/// 那里没有声卡，而程序不该因此挂掉（判据仍然靠日志）。
/// </para>
/// </summary>
internal static class DesktopAudio
{
    /// <summary>渲染块（帧）—— 与 Android 那份同值，保证两端行为一致。</summary>
    private const int BlockFrames = 1024;

    /// <summary>排队的缓冲个数。太少会欠载（断续），太多会加延迟。</summary>
    private const int BufferCount = 3;

    private static readonly object Gate = new();
    private static bool _started;
    private static bool _available;
    private static string _backend = "（未启动）";

    /// <summary>要数据时回调它：填 <c>frames</c> 帧进 <c>buf</c>。返回真正填了几帧。</summary>
    private static Func<short[], int, int>? _mixer;

    /// <summary>本机是否真的能出声（起过且没失败）。给日志用。</summary>
    public static bool Available { get { lock (Gate) return _available; } }

    /// <summary>当前后端名（日志里显示"是哪个 API 在响"）。</summary>
    public static string Backend { get { lock (Gate) return _backend; } }

    /// <summary>
    /// 启动输出。**幂等** —— 已经在跑就什么都不做。
    /// <paramref name="mixer"/> 会在音频线程上被调用，必须**快、不分配、不抛**。
    /// </summary>
    public static void Start(int sampleRate, Func<short[], int, int> mixer)
    {
        lock (Gate)
        {
            if (_started) return;
            _started = true;
            _mixer = mixer;

            try
            {
                if (OperatingSystem.IsMacOS())
                {
                    _available = MacStart(sampleRate);
                    _backend = _available ? "CoreAudio (AudioQueue)" : $"CoreAudio 启动失败：{LastError}";
                }
                else if (OperatingSystem.IsWindows())
                {
                    _available = WinStart(sampleRate);
                    _backend = _available ? "winmm (waveOut)" : "winmm 启动失败";
                }
                else
                {
                    // Linux：ALSA 的 P/Invoke 需要 libasound 且设备名因发行版而异，
                    // 这一版**先不接**（宁可不出声，也不要因为猜错设备名而抛异常）。
                    _available = false;
                    _backend = "Linux 暂未接音频输出";
                }
            }
            catch (Exception ex)
            {
                _available = false;
                _backend = "启动异常：" + ex.Message;
            }
            // ⚠ 失败**不抛**：CI / 容器里没有声卡是常态，程序不该因此挂掉。
        }
    }

    /// <summary>停止输出（幂等）。</summary>
    public static void Stop()
    {
        lock (Gate)
        {
            if (!_started) return;
            _started = false;
            try
            {
                if (_available && OperatingSystem.IsMacOS()) MacStop();
                else if (_available && OperatingSystem.IsWindows()) WinStop();
            }
            catch { /* 收尾失败无所谓 */ }
            _available = false;
            _mixer = null;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // macOS：AudioToolbox 的 AudioQueue
    //
    // 选 AudioQueue 而不是 AudioUnit：它的使用面最小（New/Allocate/Enqueue/Start
    // 五个调用），且回调天然跑在**内部线程**上（`inCallbackRunLoop` 传 NULL），
    // 不需要自己建 runloop、也不需要在 VM 线程上做任何阻塞的事。
    // ══════════════════════════════════════════════════════════════════════

    private const string AudioToolbox = "/System/Library/Frameworks/AudioToolbox.framework/AudioToolbox";

    /// <summary>`AudioStreamBasicDescription`（CoreAudio 的格式描述，9 个字段）。</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Asbd
    {
        public double SampleRate;
        public uint FormatID;
        public uint FormatFlags;
        public uint BytesPerPacket;
        public uint FramesPerPacket;
        public uint BytesPerFrame;
        public uint ChannelsPerFrame;
        public uint BitsPerChannel;
        public uint Reserved;
    }

    /// <summary>
    /// `AudioQueueBuffer` 的**前缀**（后面还有内部字段，我们不读它们，
    /// 但布局必须对 —— 只要前三个字段的偏移与 C 一致就能拿到 `mAudioData`）。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct AqBuffer
    {
        public uint AudioDataBytesCapacity;
        public IntPtr AudioData;          // ⚠ 8 字节指针，前面有 4 字节 padding（下面手工补）
        public uint AudioDataByteSize;
        public IntPtr UserData;
    }

    // 'lpcm' 四字符码
    private const uint FormatLinearPcm = 0x6C70636D;
    // kAudioFormatFlagIsSignedInteger | kAudioFormatFlagIsPacked
    private const uint FlagSignedIntPacked = 0x4 | 0x8;

    private static IntPtr _aq;
    private static IntPtr[] _aqBuffers = Array.Empty<IntPtr>();
    private static AudioQueueOutputCallback? _cbKeepAlive;   // ⚠ 必须留着，否则回调被 GC 回收

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AudioQueueOutputCallback(IntPtr userData, IntPtr aq, IntPtr buffer);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueNewOutput(ref Asbd format, AudioQueueOutputCallback callback,
        IntPtr userData, IntPtr callbackRunLoop, IntPtr callbackRunLoopMode, uint flags, out IntPtr outAq);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueAllocateBuffer(IntPtr aq, uint byteSize, out IntPtr outBuffer);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueEnqueueBuffer(IntPtr aq, IntPtr buffer, uint numPacketDescs, IntPtr packetDescs);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueStart(IntPtr aq, IntPtr startTime);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueStop(IntPtr aq, byte immediate);

    [DllImport(AudioToolbox)]
    private static extern int AudioQueueDispose(IntPtr aq, byte immediate);

    /// <summary>启动失败时的人话说明（哪一步、OSStatus 是多少）—— 排 P/Invoke 全靠它。</summary>
    public static string LastError { get; private set; } = "";

    private static bool MacStart(int sampleRate)
    {
        var fmt = new Asbd
        {
            SampleRate = sampleRate,
            FormatID = FormatLinearPcm,
            FormatFlags = FlagSignedIntPacked,
            BytesPerPacket = 2,          // 16 位单声道
            FramesPerPacket = 1,
            BytesPerFrame = 2,
            ChannelsPerFrame = 1,
            BitsPerChannel = 16,
            Reserved = 0,
        };

        _cbKeepAlive = MacCallback;
        // inCallbackRunLoop = NULL ⇒ 回调跑在 AudioQueue 自己的线程上（正是我们要的）
        var st = AudioQueueNewOutput(ref fmt, _cbKeepAlive, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, out _aq);
        if (st != 0 || _aq == IntPtr.Zero)
        {
            LastError = $"AudioQueueNewOutput 返回 {st}（aq={(long)_aq}）";
            return false;
        }

        var bytes = (uint)(BlockFrames * 2);
        _aqBuffers = new IntPtr[BufferCount];
        for (var i = 0; i < BufferCount; i++)
        {
            st = AudioQueueAllocateBuffer(_aq, bytes, out _aqBuffers[i]);
            if (st != 0)
            {
                LastError = $"AudioQueueAllocateBuffer[{i}] 返回 {st}";
                return false;
            }
            FillAqBuffer(_aqBuffers[i]);
            st = AudioQueueEnqueueBuffer(_aq, _aqBuffers[i], 0, IntPtr.Zero);
            if (st != 0)
            {
                LastError = $"AudioQueueEnqueueBuffer[{i}] 返回 {st}";
                return false;
            }
        }

        st = AudioQueueStart(_aq, IntPtr.Zero);
        if (st != 0)
        {
            LastError = $"AudioQueueStart 返回 {st}";
            return false;
        }
        return true;
    }

    private static void MacStop()
    {
        if (_aq == IntPtr.Zero) return;
        try { AudioQueueStop(_aq, 1); } catch { }
        try { AudioQueueDispose(_aq, 1); } catch { }
        _aq = IntPtr.Zero;
        _aqBuffers = Array.Empty<IntPtr>();
        _cbKeepAlive = null;
    }

    /// <summary>把一块缓冲填满并重新入队（这就是"持续混音流"的驱动点）。</summary>
    private static void MacCallback(IntPtr userData, IntPtr aq, IntPtr buffer)
    {
        if (!_started) return;
        try
        {
            FillAqBuffer(buffer);
            AudioQueueEnqueueBuffer(aq, buffer, 0, IntPtr.Zero);
        }
        catch
        {
            // ⚠ 回调里**绝不能抛** —— 它跑在原生线程上，异常会直接掀掉进程。
        }
    }

    private static readonly short[] Scratch = new short[BlockFrames];

    private static void FillAqBuffer(IntPtr buffer)
    {
        // `mAudioData`: 结构体第 0 个字段是 uint(4) → 指针在偏移 8（对齐后）
        var data = Marshal.ReadIntPtr(buffer, 8);

        var mixer = _mixer;
        var n = 0;
        if (mixer != null) n = Math.Clamp(mixer(Scratch, BlockFrames), 0, BlockFrames);

        // ⚠ **永远是整块**，没填的部分补静音。
        //   这里踩过一次：最初按"mixer 返回几帧就报几字节"写，而**第一次入队时合成器里
        //   还没有任何声部** ⇒ `Mix` 返回 0 ⇒ 长度报成 0 ⇒
        //   `AudioQueueEnqueueBuffer` 直接返回 -66686（`kAudioQueueErr_BufferEmpty`），
        //   整个后端起不来。"没有声部"的正确输出是**静音**，不是"零长度"。
        if (n < BlockFrames) Array.Clear(Scratch, n, BlockFrames - n);
        Marshal.Copy(Scratch, 0, data, BlockFrames);
        // 设 `mAudioDataByteSize`：第 2 个字段在偏移 16
        Marshal.WriteInt32(buffer, 16, BlockFrames * 2);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Windows：winmm 的 waveOut
    //
    // ⚠ **本机（macOS）无法验证这一段** —— 留作骨架，逻辑与 macOS 那份同构：
    //   同样的块大小、同样的"回调里填满再送回"、同样的失败即退化。
    //   在 Windows 机器上跑 `vmlcli` 时若这条能出声，就说明它对；
    //   若不能，第一件事是核对 `WAVEHDR` 的字段布局（它是最容易写错的地方）。
    // ══════════════════════════════════════════════════════════════════════

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public ushort FormatTag;      // 1 = PCM
        public ushort Channels;       // 1
        public uint SamplesPerSec;
        public uint AvgBytesPerSec;
        public ushort BlockAlign;
        public ushort BitsPerSample;  // 16
        public ushort ExtraSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHdr
    {
        public IntPtr Data;
        public uint BufferLength;
        public uint BytesRecorded;
        public IntPtr User;
        public uint Flags;
        public uint Loops;
        public IntPtr Next;
        public IntPtr Reserved;
    }

    [DllImport("winmm.dll")]
    private static extern int waveOutOpen(out IntPtr hwo, uint deviceId, ref WaveFormatEx fmt,
        IntPtr callback, IntPtr instance, uint flags);

    [DllImport("winmm.dll")]
    private static extern int waveOutPrepareHeader(IntPtr hwo, IntPtr hdr, uint size);

    [DllImport("winmm.dll")]
    private static extern int waveOutWrite(IntPtr hwo, IntPtr hdr, uint size);

    [DllImport("winmm.dll")]
    private static extern int waveOutReset(IntPtr hwo);

    [DllImport("winmm.dll")]
    private static extern int waveOutClose(IntPtr hwo);

    private static IntPtr _wo;
    private static IntPtr[] _woHdrs = Array.Empty<IntPtr>();
    private static IntPtr[] _woData = Array.Empty<IntPtr>();
    private static Thread? _woThread;
    private static volatile bool _woRun;

    private static bool WinStart(int sampleRate)
    {
        var fmt = new WaveFormatEx
        {
            FormatTag = 1,
            Channels = 1,
            SamplesPerSec = (uint)sampleRate,
            AvgBytesPerSec = (uint)(sampleRate * 2),
            BlockAlign = 2,
            BitsPerSample = 16,
            ExtraSize = 0,
        };

        // 回调用 CALLBACK_NULL(0)：**不用系统回调**，改成自己起一个线程轮询着写。
        // 理由：`waveOutOpen` 的 CALLBACK_FUNCTION 在托管里要回调进 C#，
        // 而它跑在驱动线程上、异常会掀进程 —— 自己起线程简单可控得多。
        if (waveOutOpen(out _wo, 0 /*WAVE_MAPPER*/, ref fmt, IntPtr.Zero, IntPtr.Zero, 0) != 0) return false;

        var hdrSize = (uint)Marshal.SizeOf<WaveHdr>();
        var bytes = BlockFrames * 2;
        var n = BufferCount;
        _woHdrs = new IntPtr[n];
        _woData = new IntPtr[n];
        for (var i = 0; i < n; i++)
        {
            _woData[i] = Marshal.AllocHGlobal(bytes);
            _woHdrs[i] = Marshal.AllocHGlobal((int)hdrSize);
            var hdr = new WaveHdr { Data = _woData[i], BufferLength = (uint)bytes };
            Marshal.StructureToPtr(hdr, _woHdrs[i], false);
            waveOutPrepareHeader(_wo, _woHdrs[i], hdrSize);
        }

        _woRun = true;
        _woThread = new Thread(WinLoop) { IsBackground = true, Name = "vml-audio-win" };
        _woThread.Start();
        return true;
    }

    private static void WinLoop()
    {
        var hdrSize = (uint)Marshal.SizeOf<WaveHdr>();
        try
        {
            while (_woRun)
            {
                // 简单轮询：写一帧、等一个块的时间。够用且不依赖驱动回调。
                // ⚠ 更好的做法是查 `dwFlags` 的 WHDR_DONE 位回收缓冲，这里先取最简形状。
                for (var i = 0; i < _woHdrs.Length && _woRun; i++)
                {
                    var mixer = _mixer;
                    if (mixer == null) break;
                    var frames = mixer(Scratch, BlockFrames);
                    var bytes = Math.Clamp(frames, 0, BlockFrames) * 2;
                    Marshal.Copy(Scratch, 0, _woData[i], Math.Clamp(frames, 0, BlockFrames));
                    var hdr = Marshal.PtrToStructure<WaveHdr>(_woHdrs[i]);
                    hdr.BufferLength = (uint)bytes;
                    hdr.Flags &= ~0x10u;   // 清 WHDR_DONE，允许重写
                    Marshal.StructureToPtr(hdr, _woHdrs[i], false);
                    waveOutWrite(_wo, _woHdrs[i], hdrSize);
                }
                Thread.Sleep(BlockFrames * 1000 / 44100);
            }
        }
        catch { /* 同上：音频线程异常不允许掀进程 */ }
    }

    private static void WinStop()
    {
        _woRun = false;
        try { _woThread?.Join(200); } catch { }
        if (_wo != IntPtr.Zero)
        {
            try { waveOutReset(_wo); } catch { }
            try { waveOutClose(_wo); } catch { }
            _wo = IntPtr.Zero;
        }
        foreach (var h in _woHdrs) if (h != IntPtr.Zero) Marshal.FreeHGlobal(h);
        foreach (var d in _woData) if (d != IntPtr.Zero) Marshal.FreeHGlobal(d);
        _woHdrs = Array.Empty<IntPtr>();
        _woData = Array.Empty<IntPtr>();
    }
}

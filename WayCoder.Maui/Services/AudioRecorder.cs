using WayCoder;

namespace WayCoder.Maui.Services;

/// <summary>
/// 实时录音服务 —— 手搓平台原生（Android MediaRecorder / iOS AVAudioRecorder），
/// 用 <c>#if</c> 条件编译单文件实现，不引入第三方录音库（符合「优先开源，否则手搓」原则）。
///
/// 录音落沙箱 workspace（<see cref="MauiBootstrap.WorkspaceDir"/>），输出 m4a（AAC），
/// 转录由 <see cref="WayCoder.Tools.TranscribeAudioTool"/> 复用主工程逻辑完成。
/// </summary>
public static class AudioRecorder
{
    /// <summary>是否正在录音。</summary>
    public static bool IsRecording { get; private set; }

    private static string? _outputPath;

    /// <summary>开始录音（沙箱 workspace 内新建 m4a）。</summary>
    public static Task StartAsync()
    {
#if ANDROID
        _outputPath = Path.Combine(MauiBootstrap.WorkspaceDir, $"rec-{Environment.TickCount64}.m4a");
        _recorder = new Android.Media.MediaRecorder();
        _recorder.SetAudioSource(Android.Media.AudioSource.Mic);
        _recorder.SetOutputFormat(Android.Media.OutputFormat.Mpeg4);
        _recorder.SetAudioEncoder(Android.Media.AudioEncoder.Aac);
        _recorder.SetAudioSamplingRate(44100);
        _recorder.SetAudioEncodingBitRate(96000);
        _recorder.SetOutputFile(_outputPath);
        _recorder.Prepare();
        _recorder.Start();
#elif IOS
        _outputPath = Path.Combine(MauiBootstrap.WorkspaceDir, $"rec-{Environment.TickCount64}.m4a");
        var url = Foundation.NSUrl.FromFilename(_outputPath);
        // kAudioFormatMPEG4AAC：Create 第 2 参要求 AVFoundation.AudioSettings 强类型（不能用 NSDictionary）
        var settings = new AVFoundation.AudioSettings
        {
            Format = AudioToolbox.AudioFormatType.MPEG4AAC,
            SampleRate = 44100f,
            NumberChannels = 1,
        };
        _recorder = AVFoundation.AVAudioRecorder.Create(url, settings, out _);
        _recorder?.PrepareToRecord();
        _recorder?.Record();
#endif
        IsRecording = true;
        return Task.CompletedTask;
    }

    /// <summary>停止录音，返回录音文件完整路径（未在录音返回 null）。</summary>
    public static Task<string?> StopAsync()
    {
        IsRecording = false;
        string? path = null;
#if ANDROID
        if (_recorder == null) return Task.FromResult<string?>(null);
        try { _recorder.Stop(); } catch { /* 录制时长过短可能抛异常，忽略 */ }
        try { _recorder.Release(); } catch { }
        _recorder = null;
        path = _outputPath;
#elif IOS
        _recorder?.Stop();
        _recorder = null;
        path = _outputPath;
#endif
        CleanupOldRecordings(); // 防 workspace 磁盘无限涨
        return Task.FromResult(path);
    }

    /// <summary>清理最旧录音文件：保留最近 <see cref="Global.MaxAudioRecordings"/> 个 rec-*.m4a（文件名按 TickCount64 单调递增，ordinal 排序≈创建顺序）。</summary>
    private static void CleanupOldRecordings()
    {
        try
        {
            var dir = MauiBootstrap.WorkspaceDir;
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            var recs = Directory.GetFiles(dir, "rec-*.m4a").OrderBy(f => f, StringComparer.Ordinal).ToList();
            while (recs.Count > Global.MaxAudioRecordings)
            {
                File.Delete(recs[0]);
                recs.RemoveAt(0);
            }
        }
        catch { /* 清理失败静默 */ }
    }

#if ANDROID
    private static Android.Media.MediaRecorder? _recorder;
#elif IOS
    private static AVFoundation.AVAudioRecorder? _recorder;
#endif
}

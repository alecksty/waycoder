using System;
using System.Collections.Generic;
using System.IO;

namespace VMLRuntime.Device
{
/// <summary>
/// PC Speaker device — software-controlled via SYSCALL #4 BEL (0x07).
/// Records audio samples to a WAV file when recording is enabled.
/// </summary>
public class VmSpeakerDevice : IDevice
{
    private EDeviceStatus _status;
        private static string _recordPath = null;
        private static List<short> _samples = new List<short>();
        private static int _sampleRate = 44100;
        private static int _globalTimeMs = 0, _lastBeepTimeMs = 0;
        private static bool _recording = false;
        private static readonly object _lock = new();

        public static void BeginRecording(string path, int sampleRate = 44100)
        {
            lock (_lock)
            {
                _recordPath = path;
                _sampleRate = sampleRate;
                _samples.Clear();
                _globalTimeMs = 0;
                _lastBeepTimeMs = 0;
                _recording = true;
            }
        }

        public static void EndRecording()
        {
            lock (_lock)
            {
                if (!_recording || string.IsNullOrEmpty(_recordPath)) return;
                _recording = false;
                SaveWav(_recordPath, _samples, _sampleRate);
            }
        }

        private static void RecordTone(int frequency, int durationMs)
        {
            if (!_recording) return;
            int totalSamples = _sampleRate * durationMs / 1000;
            int gapSamples = _sampleRate * (_globalTimeMs - _lastBeepTimeMs) / 1000;
            if (gapSamples > 0)
                for (int i = 0; i < gapSamples; i++)
                    _samples.Add(0);
            for (int i = 0; i < totalSamples; i++)
            {
                double t = (double)i / _sampleRate;
                double angle = 2.0 * Math.PI * frequency * t;
                short sample = (short)(Math.Sin(angle) * 16000); // ~50% volume
                _samples.Add(sample);
            }
            _lastBeepTimeMs = _globalTimeMs + durationMs;
            _globalTimeMs += durationMs;
        }

        private static void SaveWav(string path, List<short> samples, int sampleRate)
        {
            int dataSize = samples.Count * 2;
            using var fs = new FileStream(path, FileMode.Create);
            using var bw = new BinaryWriter(fs);
            bw.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            bw.Write(36 + dataSize);
            bw.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
            bw.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            bw.Write(16); // chunk size
            bw.Write((short)1); // PCM
            bw.Write((short)1); // mono
            bw.Write(sampleRate);
            bw.Write(sampleRate * 2); // byte rate
            bw.Write((short)2); // block align
            bw.Write((short)16); // bits per sample
            bw.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            bw.Write(dataSize);
            foreach (var s in samples) bw.Write(s);
        }

        public string Name => "speaker";
        public EDeviceType Type => EDeviceType.Other;
        public EDeviceStatus Status => _status;

        public bool Open() { _status = EDeviceStatus.Open; return true; }
        public void Close() { _status = EDeviceStatus.Closed; }
        public int Read(byte[] buffer, int offset, int count) => 0;
        public int Write(byte[] buffer, int offset, int count) => 0;
        public int Control(int command, byte[] data) => -1;

        public static void Beep(int frequency = 800, int durationMs = 100)
        {
            if (frequency < 37) frequency = 37;
            if (frequency > 32767) frequency = 32767;
            if (durationMs < 1) durationMs = 1;
            if (durationMs > 60000) durationMs = 60000;
            lock (_lock) { RecordTone(frequency, durationMs); }
#pragma warning disable CA1416
            try { Console.Beep(frequency, durationMs); }
            catch { Console.Write('\a'); }
#pragma warning restore CA1416
        }
    }
}

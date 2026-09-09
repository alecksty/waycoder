using System.Collections.Concurrent;
using WayCoder.UI.TUI.Base;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// WindowsCharSource 状态化 UTF-8 解码测试（code-review finding #3 回归护栏）：
    /// ①emoji（4 字节 UTF-8 → 代理对）应完整返回高位+低位；
    /// ②前导字节与续字节跨两次读到达（模拟 64B 读边界拆包）不得丢字节或出 U+FFFD；
    /// ③ASCII 单字节直通。
    /// 用可注入 Stream 构造 + 阻塞喂入流，避免依赖真实控制台。
    /// </summary>
    private static void TestChunk19(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("[字符源 UTF-8 状态化]");
        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            // ① 一次喂入完整 emoji（😀 = F0 9F 98 80）+ ASCII 'a'
            feed.Feed([0xF0, 0x9F, 0x98, 0x80, (byte)'a']);
            var c1 = ReadChar(src, feed);
            Check("emoji 高位返回(非 U+FFFD/非丢失)", c1.HasValue && char.IsHighSurrogate(c1.Value));
            var c2 = ReadChar(src, feed);
            Check("emoji 低位接着返回", c2.HasValue && char.IsLowSurrogate(c2.Value));
            var c3 = ReadChar(src, feed);
            Check("紧随 ASCII 'a' 不丢", c3 == 'a');
        }

        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            // ② 跨读边界：先只喂前导+首续字节，解码应暂缓（返回 false / 无字符，且不丢首字节不出 U+FFFD）
            feed.Feed([0xF0, 0x9F]);
            Thread.Sleep(10);
            var early = ReadChar(src, feed, allowEmpty: true);
            Check("半字符暂缓(不产出 U+FFFD)", early == null || early != '�');
            // 再喂余下续字节
            feed.Feed([0x98, 0x80]);
            var h = ReadChar(src, feed);
            var l = ReadChar(src, feed);
            Check("跨边界拆包后仍合成完整代理对", h.HasValue && char.IsHighSurrogate(h.Value)
                && l.HasValue && char.IsLowSurrogate(l.Value));
        }

        using (var feed = new TestFeedStream())
        using (var src = new WindowsCharSource(feed))
        {
            // ③ ASCII 直通 + RS(0x1E) 分隔符被跳过
            feed.Feed([0x1E, (byte)'x']);
            var cx = ReadChar(src, feed);
            Check("RS 分隔符跳过、ASCII 直通", cx == 'x');
        }
    }

    private static char? ReadChar(WindowsCharSource src, TestFeedStream feed, bool allowEmpty = false)
    {
        for (int i = 0; i < 200; i++)
        {
            if (src.TryReadChar(out var c)) return c;
            if (src.HasInput || feed.Length > 0) { Thread.Sleep(2); continue; }
            if (allowEmpty) return null;
            Thread.Sleep(2);
        }
        return null;
    }

    /// <summary>阻塞喂入流：Read 在无数据时等待（reader 线程用），Feed 追加并唤醒。</summary>
    private sealed class TestFeedStream : Stream
    {
        private readonly object _lock = new();
        private readonly Queue<byte> _q = new();
        private bool _closed;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length { get { lock (_lock) return _q.Count; } }
        public override long Position { get => 0; set { } }

        public void Feed(byte[] data)
        {
            lock (_lock)
            {
                foreach (var b in data) _q.Enqueue(b);
                Monitor.PulseAll(_lock);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_lock)
            {
                while (_q.Count == 0 && !_closed) Monitor.Wait(_lock);
                if (_q.Count == 0) return 0;
                int n = Math.Min(count, _q.Count);
                for (int i = 0; i < n; i++) buffer[offset + i] = _q.Dequeue();
                return n;
            }
        }

        public override void Close()
        {
            lock (_lock) { _closed = true; Monitor.PulseAll(_lock); }
            base.Close();
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}

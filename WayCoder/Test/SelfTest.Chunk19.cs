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
        Section("[跨端 UI 文本(core UiText)]");
        Check("PermName Yolo/SmartAuto/Auto/Ask", UiText.PermName(PermissionManager.Mode.Yolo) == "Yolo"
            && UiText.PermName(PermissionManager.Mode.SmartAuto) == "SmartAuto"
            && UiText.PermName(PermissionManager.Mode.Auto) == "Auto"
            && UiText.PermName(PermissionManager.Mode.Ask) == "Ask");
        Check("EconomyName 关/自动/开/极致", UiText.EconomyName(EconomyMode.Off) == "关"
            && UiText.EconomyName(EconomyMode.Auto) == "自动"
            && UiText.EconomyName(EconomyMode.On) == "开"
            && UiText.EconomyName(EconomyMode.Extreme) == "极致");
        Check("FormatK 999/1000/1500", UiText.FormatK(999) == "999" && UiText.FormatK(1000) == "1.0k" && UiText.FormatK(1500) == "1.5k");
        Check("IsSessionBodyRole user/assistant 真、tool/system 假",
            UiText.IsSessionBodyRole("user") && UiText.IsSessionBodyRole("assistant")
            && !UiText.IsSessionBodyRole("tool") && !UiText.IsSessionBodyRole("system") && !UiText.IsSessionBodyRole(""));

        Section("[输入 CSI 功能键映射]");
        // code-review finding #1 回归护栏：Windows VT 输入下裸 CSI 光标键/编辑键必须映射为对应
        // ConsoleKey，绝不可退化成裸 ESC（否则取消在跑 agent / 丢聊天草稿）。
        ConsoleKey ParseKey(string param, char term) => InputManager.ParseCsiFuncKey(param, term)?.KeyInfo.Key ?? ConsoleKey.NoName;
        Check("ESC[A → Up(非 ESC)", ParseKey("A", 'A') == ConsoleKey.UpArrow);
        Check("ESC[B → Down", ParseKey("B", 'B') == ConsoleKey.DownArrow);
        Check("ESC[D → Left", ParseKey("D", 'D') == ConsoleKey.LeftArrow);
        Check("ESC[H → Home", ParseKey("H", 'H') == ConsoleKey.Home);
        Check("ESC[F → End", ParseKey("F", 'F') == ConsoleKey.End);
        Check("1~ → Home", ParseKey("1~", '~') == ConsoleKey.Home);
        Check("3~ → Delete", ParseKey("3~", '~') == ConsoleKey.Delete);
        Check("5~ → PgUp", ParseKey("5~", '~') == ConsoleKey.PageUp);
        Check("6~ → PgDn", ParseKey("6~", '~') == ConsoleKey.PageDown);
        Check("15~ → F5", ParseKey("15~", '~') == ConsoleKey.F5);
        var ctrlEv = InputManager.ParseCsiFuncKey("1;5D", 'D');
        Check("1;5D → Ctrl+Left", ctrlEv?.KeyInfo is { } kk && kk.Key == ConsoleKey.LeftArrow && kk.Modifiers.HasFlag(ConsoleModifiers.Control));

        Section("[char→ConsoleKey 统一映射]");
        Check("MapToConsoleKey a→A", WindowsCharSource.MapToConsoleKey('a') == ConsoleKey.A);
        Check("MapToConsoleKey 9→D9", WindowsCharSource.MapToConsoleKey('9') == ConsoleKey.D9);
        Check("MapToConsoleKey 空格→Spacebar", WindowsCharSource.MapToConsoleKey(' ') == ConsoleKey.Spacebar);
        Check("MapToConsoleKey ESC→Escape", WindowsCharSource.MapToConsoleKey('\x1b') == ConsoleKey.Escape);
        Check("MapToConsoleKey CJK→NoName", WindowsCharSource.MapToConsoleKey('中') == ConsoleKey.NoName);

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

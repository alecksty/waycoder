using WayCoder.UI.Shared.Terminal;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// **命令行窗实时化**（`ShellStream`）的判据。
    ///
    /// ## 为什么这块必须单独立判据
    ///
    /// 原先命令行页是**整段缓冲**的：程序跑完才拿整段输出判一次、刷一次。
    /// 于是 `top` / `vim` / `mc` / `cmatrix` 这一整类（PC/Linux/老 Mac 程序里的**绝大多数**）
    /// 一个都跑不起来 —— 它们全是"不退出就一直画"，而屏幕在运行期间是空的。
    ///
    /// 改成流式之后多出来一整类**只在流式下才存在**的故障：
    /// 判据在**半截输入**上做出、并且**不可逆**（判成线性就再也不能变成网格）。
    /// 所以这里的用例重心不在"正常路径"，而在**块的边界**上。
    ///
    /// ## 覆盖
    ///
    /// ① `ScreenOutput.Scan` 新加的 `truncated` 契约（纯增，行为一字未变）；
    /// ② `ShellStream` 的状态机：探测 → 线性 / 探测 → 网格，以及**截断时不许下结论**；
    /// ③ 分块喂入时**一个字符都不能丢**（流式最典型的回归）。
    /// </summary>
    private static void TestChunk29(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        TestShellStreamScan(Section, Check);
        TestShellStreamProbe(Section, Check);
        TestShellStreamChunking(Section, Check);
    }

    // ═══ ① `ScreenOutput.Scan` 的 truncated 契约 ═══
    //
    // `LooksFullScreen` 原本把"确定不是全屏"和"还下不了结论"混成同一个 false。
    // 流式判定照它走，块边界落在 `ESC[` 中间时就会把一个全屏程序**永久**误判成线性。
    private static void TestShellStreamScan(Action<string> Section, Action<string, bool> Check)
    {
        Section("[命令行·流式输出] 截断判据（ScreenOutput.Scan）");

        // 完整的、但不是光标定位的序列 ⇒ **有结论**（确定不是全屏）
        ScreenOutput.Scan("\x1b[31m", out bool t1);
        Check("Scan: 完整 SGR 序列 ⇒ 有结论（truncated=false）", !t1);

        ScreenOutput.Scan("hello\x1b[K", out bool t2);
        Check("Scan: 完整 \\x1b[K ⇒ 有结论", !t2);

        // 半截 ⇒ **没结论**（这正是流式必须区分的那一档）
        ScreenOutput.Scan("\x1b[3", out bool t3);
        Check("Scan: \\x1b[3（没到终结符）⇒ truncated=true", t3);

        ScreenOutput.Scan("abc\x1b", out bool t4);
        Check("Scan: 尾部半个 ESC ⇒ truncated=true", t4);

        // 光标定位序列一旦出现，**无论后面有没有半截**都要认
        ScreenOutput.Scan("\x1b[H\x1b[3", out bool t5);
        Check("Scan: 已有 \\x1b[H 时后面跟着半截也算全屏", t5 == false);

        // 行为没变：老的薄包装照旧
        Check("Scan/LooksFullScreen 同源：\\x1b[H 两边都认",
            ScreenOutput.LooksFullScreen("\x1b[H") && ScreenOutput.Scan("\x1b[H", out _));
    }

    // ═══ ② 状态机：探测 → 线性 / 网格 ═══
    private static void TestShellStreamProbe(Action<string> Section, Action<string, bool> Check)
    {
        Section("[命令行·流式输出] 探测：何时定线性、何时定网格");

        // ── 一个 ESC 都没有 ⇒ **当场就能定线性**（没 ESC 就不可能在做光标定位）──
        var s1 = new ShellStream();
        var r1 = s1.Feed("hello\nworld\n");
        Check("ShellStream: 纯文本当场判线性", r1 is { IsGrid: false });
        Check("ShellStream: 纯文本内容一字不丢", r1?.Text.Contains("hello") == true && r1.Text.Contains("world") == true);
        Check("ShellStream: 判为线性后模式不变", s1.Mode == ShellStreamMode.Linear);

        // ── 开场就是光标定位 ⇒ 判网格 ──
        var s2 = new ShellStream(25, 80);
        var r2 = s2.Feed("\x1b[H\x1b[2J\x1b[3;5HX");
        Check("ShellStream: 开场 \\x1b[H 判网格", r2 is { IsGrid: true });
        Check("ShellStream: 网格态带上尺寸", r2?.Rows == 25 && r2.Cols == 80);
        Check("ShellStream: 模式为 Grid", s2.Mode == ShellStreamMode.Grid);

        // ── 只有颜色（有 ESC 但没光标定位）⇒ **先攒着**，别急着判 ──
        //
        // 这一条是流式独有的：`ls --color` 那种输出**也是**带 ESC 的，
        // 但全屏程序的开场（切屏 + 清屏 + 归位）通常只有几十字节 —— 等一会儿再定。
        var s3 = new ShellStream();
        var r3 = s3.Feed("\x1b[31mred\x1b[0m");
        Check("ShellStream: 有 ESC 但没光标定位 ⇒ 暂不下结论", r3 is null);
        Check("ShellStream: 暂缓时模式还是 Probing", s3.Mode == ShellStreamMode.Probing);

        // 攒够 HasEscLimit 之后判线性，且**攒着的内容一次性吐出来**
        var r3b = s3.Feed(new string('x', 1100));
        Check("ShellStream: 攒够阈值后判线性", r3b is { IsGrid: false });
        Check("ShellStream: 攒着的内容一次性吐出（不丢 red）", r3b?.Text.Contains("red") == true);
    }

    // ═══ ③ 分块：流式最典型的回归是"丢字" ═══
    private static void TestShellStreamChunking(Action<string> Section, Action<string, bool> Check)
    {
        Section("[命令行·流式输出] 分块边界");

        // ── 序列被切开：第一块以 `\x1b[` 结尾 ⇒ **必须推迟**，不能判成线性 ──
        var s = new ShellStream();
        var a = s.Feed("输出\x1b[");
        Check("ShellStream: 块尾半截序列 ⇒ 不返回、不判线性", a is null);
        Check("ShellStream: 此时仍是 Probing", s.Mode == ShellStreamMode.Probing);

        var b = s.Feed("H\x1b[2J画好了");
        Check("ShellStream: 拼齐后判网格", b is { IsGrid: true });

        // ── 线性态：连续多块拼起来 == 原文 ──
        var lin = new ShellStream();
        var sb = new System.Text.StringBuilder();
        foreach (var piece in new[] { "aaa\n", "bbb\n", "ccc" })
        {
            var rr = lin.Feed(piece);
            if (rr != null) sb.Append(rr.Text);
        }
        var fin = lin.Finish();
        if (fin != null) sb.Append(fin.Text);
        Check("ShellStream: 线性态分块喂入不丢字",
            sb.ToString().Contains("aaa") && sb.ToString().Contains("bbb") && sb.ToString().Contains("ccc"));

        /* ── 收尾：末尾是半截序列时，**它前面的正文不能跟着丢** ──
           ⚠ 第一版这条写错了（期望值不是独立推导的）：先 `Feed("正文")` 的话，
             那一段**当场**就按线性吐出去了，`Finish` 之后再断言等于什么都没测。
             要压的是"探测期攒着 + 尾巴半截"同时成立：正文和半截序列在**同一块**里，
             且因为带了 ESC，探测期不会当场下结论。 */
        var tail = new ShellStream();
        var t1 = tail.Feed("\x1b[31m正文\x1b[");
        Check("ShellStream: 带 ESC 且尾部半截 ⇒ 攒着不出", t1 is null);
        var f = tail.Finish();
        Check("ShellStream: Finish 吐出半截序列之前的正文",
            f is { IsGrid: false } && f.Text.Contains("正文"));

        // ── 网格态连续两帧：第二次是**替换**（整屏重出），不是追加 ──
        var g = new ShellStream(5, 20);
        g.Feed("\x1b[HAAAA");
        var frame2 = g.Feed("\x1b[HBBBB");
        Check("ShellStream: 网格第二帧照旧是整屏渲染", frame2 is { IsGrid: true });
        Check("ShellStream: 第二帧内容是新的（AAAA 已被 BBBB 覆盖）",
            frame2?.Text.Contains("BBBB") == true && frame2.Text.Contains("AAAA") == false);

        // ── Finish 对网格态也要能收尾（程序没退出就一直画，退出时补最后一帧）──
        var g2 = new ShellStream(5, 20);
        g2.Feed("\x1b[HZZ");
        Check("ShellStream: 网格态 Finish 不抛异常", g2.Finish() is null or { IsGrid: true });
    }
}

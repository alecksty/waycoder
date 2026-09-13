using System.Text;
using WayCoder.Infra;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 大文件编辑器数据层（<see cref="TextSourceFactory"/> / <see cref="IndexedTextSource"/>）
    /// 与纯计算层（<see cref="TextEditorMath"/> / <see cref="EditHistory"/>）。
    ///
    /// 这一层刻意做成纯逻辑，就是为了能被这里覆盖 —— 渲染必须上真机，但**「100MB 能不能打开」
    /// 的决定因素全在这一层**（永不整份读进内存、行索引、按行解码）。
    /// </summary>
    private static void TestChunk21(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        var dir = Path.Combine(Path.GetTempPath(), "wc_large_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);

        static (ITextSource? Src, string Reason) Open(string path)
            => TextSourceFactory.OpenAsync(path).GetAwaiter().GetResult();

        static string? Line(ITextSource src, long i)
        {
            src.PrefetchAsync(i, i).GetAwaiter().GetResult();
            return src.GetLine(i);
        }

        try
        {
            // ── 基本：行数、行内容、尾随换行 ──
            Section("[大文件索引(LargeTextFile)]");
            {
                var p = Path.Combine(dir, "basic.txt");
                File.WriteAllText(p, "第一行\nsecond\n第三行\n");
                var (src, reason) = Open(p);
                Check("大文件索引: 打开成功", src != null && reason == "");
                Check($"大文件索引: 尾随换行算一行空（实际 {src?.LineCount}）", src?.LineCount == 4);
                Check("大文件索引: 首行内容", Line(src!, 0) == "第一行");
                Check("大文件索引: 中间行内容", Line(src!, 1) == "second");
                Check("大文件索引: 末行是空串", Line(src!, 3) == "");
                Check("大文件索引: 编码判为 UTF-8", src!.EncodingName == "UTF-8");
                Check("大文件索引: 非 CRLF", !src.UsesCrlf);
                src.Dispose();

                // 无尾随换行：行数少一行
                var p2 = Path.Combine(dir, "nofinalnl.txt");
                File.WriteAllText(p2, "a\nb");
                var (s2, _) = Open(p2);
                Check($"大文件索引: 无尾随换行 → 2 行（实际 {s2?.LineCount}）", s2?.LineCount == 2);
                Check("大文件索引: 末行无换行也读得到", Line(s2!, 1) == "b");
                s2!.Dispose();

                // CRLF：行尾 \r 必须剥掉，否则每行末尾多一个不可见字符、宽度全错
                var p3 = Path.Combine(dir, "crlf.txt");
                File.WriteAllText(p3, "a\r\nb\r\n");
                var (s3, _) = Open(p3);
                Check("大文件索引: 识别 CRLF", s3!.UsesCrlf);
                Check("大文件索引: CRLF 行尾 \\r 已剥掉", Line(s3, 0) == "a" && Line(s3, 1) == "b");
                s3.Dispose();

                // 空文件
                var p4 = Path.Combine(dir, "empty.txt");
                File.WriteAllText(p4, "");
                var (s4, _) = Open(p4);
                Check("大文件索引: 空文件 1 行空串", s4?.LineCount == 1 && Line(s4!, 0) == "");
                s4!.Dispose();
            }

            // ── 编码：GB18030 与「按 0x0A 切分字节」的地基性质 ──
            Section("[大文件编码]");
            {
                // 整个数据层建立在「这些编码的续字节不含 0x0A」之上。这条断言成本极低，
                // 但它一旦不成立，按行解码就会切出半个字符、整个索引路径都得推倒重来。
                var gbk = TextEncoding.GB18030.GetBytes("中文测试\n第二行\nabc\n");
                int splits = 0;
                for (int i = 0; i < gbk.Length; i++) if (gbk[i] == 0x0A) splits++;
                var decoded = TextEncoding.GB18030.GetString(gbk);
                Check($"大文件编码: GB18030 字节分片数({splits}) == 解码后行数-1({decoded.Split('\n').Length - 1})",
                    splits == decoded.Split('\n').Length - 1);

                var utf8 = Encoding.UTF8.GetBytes("中文😀测试\n第二行\n");
                int s8 = 0;
                foreach (var b in utf8) if (b == 0x0A) s8++;
                Check("大文件编码: UTF-8（含 emoji 代理对）字节分片数一致",
                    s8 == Encoding.UTF8.GetString(utf8).Split('\n').Length - 1);

                // GB18030 文件端到端
                var p = Path.Combine(dir, "gbk.txt");
                File.WriteAllBytes(p, gbk);
                var (src, _) = Open(p);
                Check("大文件编码: GB18030 识别", src!.EncodingName == "GB18030");
                Check("大文件编码: GB18030 行内容不乱码", Line(src, 0) == "中文测试" && Line(src, 1) == "第二行");
                src.Dispose();

                // 采样探测：把多字节字符切一半不该被误判成 GB18030
                var half = Encoding.UTF8.GetBytes("中");
                Check("大文件编码: 采样尾部切断的多字节序列不误判为 GB18030",
                    TextSourceFactory.IsValidUtf8Prefix(half[..2], 0));
                Check("大文件编码: 真正非法的 UTF-8 被判出",
                    !TextSourceFactory.IsValidUtf8Prefix(new byte[] { 0xC0, 0x20 }, 0));

                // UTF-16 大文件：明确拒绝，而不是解成一堆 NUL
                var p16 = Path.Combine(dir, "big16.txt");
                var bytes16 = new byte[TextSourceFactory.MemoryModeMaxBytes + 1024];
                for (int i = 0; i + 1 < bytes16.Length; i += 2) { bytes16[i] = (byte)'a'; bytes16[i + 1] = 0; }
                bytes16[0] = 0xFF; bytes16[1] = 0xFE;   // UTF-16 LE BOM
                File.WriteAllBytes(p16, bytes16);
                var (s16, r16) = Open(p16);
                Check($"大文件编码: UTF-16 大文件明确拒绝（{r16}）", s16 == null && r16.Contains("UTF-16"));

                // 二进制
                var pbin = Path.Combine(dir, "bin.dat");
                File.WriteAllBytes(pbin, new byte[] { 1, 2, 0, 3, 4 });
                var (sbin, rbin) = Open(pbin);
                Check($"大文件编码: 二进制拒绝（{rbin}）", sbin == null && rbin.Contains("二进制"));
            }

            // ── 超长单行：不整条读进内存 ──
            Section("[大文件超长行]");
            {
                var p = Path.Combine(dir, "longline.txt");
                // 造一条 5MB 的单行（超过 MaxLineReadBytes=4MB）
                int target = IndexedTextSource.MaxLineReadBytes + (1 << 20);
                using (var fs = new FileStream(p, FileMode.Create, FileAccess.Write))
                {
                    var buf = new byte[1 << 16];
                    Array.Fill(buf, (byte)'x');
                    long written = 0;
                    while (written < target) { fs.Write(buf, 0, buf.Length); written += buf.Length; }
                }
                var (src, _) = Open(p);
                Check($"大文件超长行: 打开成功（{src?.LineCount} 行）", src is { LineCount: 1 });
                Check($"大文件超长行: 标注为截断（{src!.LineBytes(0)} 字节）",
                    src.LineBytes(0) > IndexedTextSource.MaxLineReadBytes);
                var line = Line(src, 0);
                Check($"大文件超长行: 只读出上限内长度（{line?.Length}）",
                    line != null && line.Length <= IndexedTextSource.MaxLineReadBytes);
                src.Dispose();
            }

            // ── 编辑器数学 ──
            Section("[编辑器数学]");
            {
                // 字宽注入：ASCII 1 列、CJK 2 列（真源是 AnsiString.CharWidth，这里用等价简化版）
                static int W(Rune r) => r.Value > 0x2E80 ? 2 : 1;

                Check("编辑器数学: tab 展开到 4 的倍数", TextEditorMath.ExpandTabs("a\tb") == "a   b");
                Check("编辑器数学: 行首 tab 展开 4 空格", TextEditorMath.ExpandTabs("\txx") == "    xx");
                Check("编辑器数学: 无 tab 原样返回", TextEditorMath.ExpandTabs("abc") == "abc");
                Check("编辑器数学: tab 按字符列推进（对齐 tab stop）",
                    TextEditorMath.ExpandTabs("ab\tc") == "ab  c");

                Check("编辑器数学: 视觉列 0 → 索引 0", TextEditorMath.VisualColToSourceIndex("中abc", 0, W) == 0);
                Check("编辑器数学: CJK 占 2 列 → 视觉列 2 落到下一字符",
                    TextEditorMath.VisualColToSourceIndex("中abc", 2, W) == 1);
                Check("编辑器数学: 视觉列落在 CJK 中间归到该字符",
                    TextEditorMath.VisualColToSourceIndex("中abc", 1, W) == 0);
                Check("编辑器数学: 反向映射一致",
                    TextEditorMath.SourceIndexToVisualCol("中abc", 1, W) == 2);

                // 超长行的可见窗口
                var (win, idx) = TextEditorMath.WindowByColumns("abcdefghij", 3, 4, W);
                Check($"编辑器数学: 可见窗口 [{win}] 起点 {idx}", win == "defg" && idx == 3);

                // 滚动定位
                Check("编辑器数学: 目标行在下方时滚进来",
                    TextEditorMath.EnsureVisible(0, 25, 20, 100) == 6);
                Check("编辑器数学: 目标行在上方时贴上",
                    TextEditorMath.EnsureVisible(50, 10, 20, 100) == 10);
                Check("编辑器数学: 已在视口内不动",
                    TextEditorMath.EnsureVisible(10, 15, 20, 100) == 10);
                Check("编辑器数学: 靠近文件末尾不越界",
                    TextEditorMath.EnsureVisible(0, 99, 20, 100) == 80);
                Check("编辑器数学: 居中模式",
                    TextEditorMath.EnsureVisible(0, 50, 20, 100, center: true) == 40);

                // 惯性
                Check("编辑器数学: 惯性总位移为正",
                    TextEditorMath.FlingDistance(6000, 0.998) > 0);
                Check("编辑器数学: 惯性随时间衰减（每秒减半时 10 秒后趋零）",
                    TextEditorMath.FlingVelocity(1000, 1, 0.5) < 600
                    && TextEditorMath.FlingVelocity(1000, 10, 0.5) < 1);
            }

            // ── 编辑历史 ──
            Section("[编辑历史]");
            {
                var h = new EditHistory();
                void Push(long line, string oldT, string newT, long ms)
                    => h.Push(new EditOp(line, [oldT], [newT], 0, newT.Length, ms));

                Push(0, "a", "ab", 1000);
                Push(0, "ab", "abc", 1200);   // 时间窗内 + 单字符追加 → 应合并
                Check($"编辑历史: 连续打字合并成一步（{h.UndoDepth}）", h.UndoDepth == 1);

                Push(0, "abc", "abd", 5000);  // 超时间窗 → 新步骤
                Check($"编辑历史: 超时间窗不合并（{h.UndoDepth}）", h.UndoDepth == 2);

                // 跨行（拆行）不合并
                h.Push(new EditOp(1, ["x"], ["x", ""], 0, 0, 5100));
                Check($"编辑历史: 拆行独立成步（{h.UndoDepth}）", h.UndoDepth == 3);

                var u1 = h.Undo();
                Check("编辑历史: 撤销取回最后一步", u1?.NewLines.Length == 2 && u1?.OldLines[0] == "x");
                Check("编辑历史: 撤销后重做栈非空", h.RedoDepth == 1 && h.CanRedo);
                var r1 = h.Redo();
                Check("编辑历史: 重做取回同一步", r1?.NewLines.Length == 2 && h.RedoDepth == 0);
                Check("编辑历史: 撤销到空返回 null",
                    h.Undo() != null && h.Undo() != null && h.Undo() != null && h.Undo() == null);

                // 上限：超深时丢最旧
                var small = new EditHistory { MaxDepth = 3 };
                for (int i = 0; i < 6; i++)
                    small.Push(new EditOp(i, ["x"], ["x" + i], 0, 0, i * 10_000));
                Check($"编辑历史: 超上限丢最旧（{small.UndoDepth}）", small.UndoDepth == 3);

                h.Clear();
                Check("编辑历史: Clear 清两侧", h.CanUndo == false && h.CanRedo == false);
            }

            // ── 可编辑行集合 ──
            Section("[可编辑行集合]");
            {
                var ed = new EditableLines("a\nb\nc", "UTF-8", false, 5);
                Check($"可编辑行集合: 初始 3 行（{ed.LineCount}）", ed.LineCount == 3);

                ed.ReplaceRange(1, 1, ["B"]);                      // 改一行
                Check("可编辑行集合: 替换单行", ed.GetLine(1) == "B" && ed.LineCount == 3);

                ed.ReplaceRange(1, 1, ["B1", "B2"]);               // 拆行
                Check($"可编辑行集合: 拆行后 4 行（{ed.LineCount}）",
                    ed.LineCount == 4 && ed.GetLine(1) == "B1" && ed.GetLine(2) == "B2");

                ed.ReplaceRange(1, 2, ["B"]);                      // 合行
                Check($"可编辑行集合: 合行后 3 行（{ed.LineCount}）", ed.LineCount == 3 && ed.GetLine(1) == "B");

                ed.ReplaceRange(0, 0, ["head"]);                   // 行首插入
                Check("可编辑行集合: 行首插入", ed.GetLine(0) == "head" && ed.LineCount == 4);

                var snap = ed.Snapshot(0, 2);
                Check("可编辑行集合: 快照取段", snap.Length == 2 && snap[0] == "head");

                ed.ReplaceRange(0, 4, ["x", "y"]);
                Check("可编辑行集合: 整体替换 + 导出", ed.LineCount == 2 && ed.ReadAll() == "x\ny");
            }
        }
        catch (Exception ex)
        {
            Fail($"大文件数据层: 异常 {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }

        // ── 真实大文件冒烟（本机有该文件时才跑；CI/别人机器上自动跳过）──
        // 「能否打开 100MB」的决定因素全在数据层：行索引、按行解码、缓存上限。
        // 渲染层对大文件与小文件是同一套代码（都只画可见的几十行），所以这里量的是内存与耗时。
        SmokeTestBigFile(Section, Check);
    }

    /// <summary>用本机真实大文件做一次端到端冒烟（不存在则静默跳过）。</summary>
    private static void SmokeTestBigFile(Action<string> Section, Action<string, bool> Check)
    {
        const string bigPath = "/tmp/editor_bench/demo_large.cs";
        if (!File.Exists(bigPath)) return;

        Section("[大文件冒烟(真实文件)]");
        long before = GC.GetTotalMemory(forceFullCollection: true);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // editableMaxBytes=0 ⇒ 强制走索引型只读来源（正是大文件的路径）
        var (src, reason) = TextSourceFactory.OpenAsync(bigPath, 0).GetAwaiter().GetResult();
        sw.Stop();

        long fileMb = new FileInfo(bigPath).Length / (1024 * 1024);
        Check($"大文件冒烟: 打开 {fileMb}MB 成功（{sw.ElapsedMilliseconds}ms）", src != null);
        if (src == null) { Check($"大文件冒烟: 失败原因 {reason}", false); return; }

        long after = GC.GetTotalMemory(forceFullCollection: true);
        long usedMb = (after - before) / (1024 * 1024);

        Check($"大文件冒烟: 行数 {src.LineCount:N0}（应 ≈ {fileMb * 2 / 100}万+）", src.LineCount > 1_000_000);
        // 索引是 long[]：100 万行 8MB；加上缓存预算 8MB，总增量应远小于文件本身
        Check($"大文件冒烟: 内存增量 {usedMb}MB < 文件大小 {fileMb}MB（没有整份装进内存）",
            usedMb < fileMb);

        // 首行 / 中间行 / 末行都能读出来（验证索引与按行解码）
        src.PrefetchAsync(0, 3).GetAwaiter().GetResult();
        var head = src.GetLine(0);
        Check($"大文件冒烟: 首行非空 [{head?[..Math.Min(24, head.Length)]}]", !string.IsNullOrEmpty(head));

        long mid = src.LineCount / 2;
        src.PrefetchAsync(mid, mid).GetAwaiter().GetResult();
        Check($"大文件冒烟: 中间行（{mid}）可读", src.GetLine(mid) != null);

        long last = src.LineCount - 1;
        src.PrefetchAsync(last - 1, last).GetAwaiter().GetResult();
        Check($"大文件冒烟: 末行可读", src.GetLine(last) != null);

        Check($"大文件冒烟: 编码 {src.EncodingName}", src.EncodingName == "UTF-8");
        src.Dispose();
    }
}

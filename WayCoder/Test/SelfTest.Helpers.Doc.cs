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
    /// <summary>获取 notebook cell 的 source 文本（测试助手）</summary>
    private static string GetNotebookSource(JNode notebook, int cellIndex)
    {
        var cells = notebook["cells"];
        if (cells == null || cellIndex >= cells.Count) return "";
        var source = cells[cellIndex]?["source"];
        if (source is { Kind: JKind.Array })
        {
            var sb = new StringBuilder();
            foreach (var line in source.Items) sb.Append(line.AsString() ?? "");
            return sb.ToString();
        }
        return source?.AsString() ?? "";
    }

    // ═══════════════════════════════════════════════════════════
    //  ContextManager 单元测试
    // ═══════════════════════════════════════════════════════════

    /// <summary>构造带深度嵌套数组（depth 层）的最小 PDF，用于验证解析器防栈溢出护栏。</summary>
    private static byte[] BuildNestedPdf(int depth)
    {
        var bytes = new List<byte>();
        void Add(string s) => bytes.AddRange(Encoding.ASCII.GetBytes(s));

        Add("%PDF-1.4\n");
        var offsets = new long[4];
        offsets[1] = bytes.Count;
        Add("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        offsets[2] = bytes.Count;
        Add("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        offsets[3] = bytes.Count;
        Add("3 0 obj\n<< /Type /Page /Parent 2 0 R >>\nendobj\n");

        var xrefPos = bytes.Count;
        Add("xref\n0 4\n");
        Add("0000000000 65535 f \n");
        for (int i = 1; i <= 3; i++)
            Add($"{offsets[i]:D10} 00000 n \n");

        Add("trailer\n<< /Size 4 /Root 1 0 R /Deep ");
        Add(new string('[', depth));
        Add("1");
        Add(new string(']', depth));
        Add(" >>\nstartxref\n" + xrefPos + "\n%%EOF\n");

        return bytes.ToArray();
    }

    /// <summary>构造内容流（stream）为深层嵌套数组的 PDF，用于测试 ParseStreamValue↔ParseStreamArray 递归护栏。</summary>
    private static byte[] BuildNestedContentPdf(int depth)
    {
        var bytes = new List<byte>();
        void Add(string s) => bytes.AddRange(Encoding.ASCII.GetBytes(s));

        Add("%PDF-1.4\n");
        var offsets = new long[5];
        offsets[1] = bytes.Count;
        Add("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        offsets[2] = bytes.Count;
        Add("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        offsets[3] = bytes.Count;
        Add("3 0 obj\n<< /Type /Page /Parent 2 0 R /Contents 4 0 R >>\nendobj\n");

        var nested = new string('[', depth) + "1" + new string(']', depth);
        offsets[4] = bytes.Count;
        Add($"4 0 obj\n<< /Length {nested.Length} >>\nstream\n{nested}\nendstream\nendobj\n");

        var xrefPos = bytes.Count;
        Add("xref\n0 5\n");
        Add("0000000000 65535 f \n");
        for (int i = 1; i <= 4; i++)
            Add($"{offsets[i]:D10} 00000 n \n");
        Add($"trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF\n");

        return bytes.ToArray();
    }

    /// <summary>手搓 PDF 解析器测试：结构解析 + 文本提取 + 编码 + 错误分支</summary>
    private static void TestPdf(Action<string, bool> Check)
    {
        // ── 1. 最小 PDF 构造 → 解析 ──
        var pdfBytes = BuildMinimalPdf();
        var parser = PdfParser.Open(pdfBytes);
        Check("Pdf: 最小 PDF 解析成功", parser != null);
        if (parser != null)
        {
            Check("Pdf: 页数正确", parser.NumberOfPages == 1);
            Check("Pdf: 标题解析", parser.Title == "Test Document");

            var text = parser.ExtractPageText(1);
            Check("Pdf: 提取 Hello World", text.Contains("Hello World"));
            Check("Pdf: 提取第二行", text.Contains("Second line"));
            Check("Pdf: 换行分隔", text.Contains("\n"));
        }

        // ── 2. 错误分支 ──
        Check("Pdf: 非 PDF 返回 null", PdfParser.Open(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }) == null);
        Check("Pdf: 空数据返回 null", PdfParser.Open(Array.Empty<byte>()) == null);

        // ── 3. PdfExtractor 公共 API 不变 ──
        var tmpPdf = Path.Combine(Path.GetTempPath(), "wc_pdf_" + Guid.NewGuid().ToString("N")[..6] + ".pdf");
        try
        {
            File.WriteAllBytes(tmpPdf, pdfBytes);
            var result = PdfExtractor.Extract(tmpPdf);
            Check("PdfExtractor: 非错误", !result.IsError);
            Check("PdfExtractor: 总页数", result.TotalPages == 1);
            Check("PdfExtractor: 页数已提取", result.PagesExtracted == 1);
            Check("PdfExtractor: 有字符", result.TotalChars > 0);
            Check("PdfExtractor: 标题", result.Title == "Test Document");
            var md = result.ToMarkdown();
            Check("PdfExtractor: ToMarkdown 含标签", md.Contains("<pdf>") && md.Contains("Hello World"));

            var meta = PdfExtractor.GetMeta(tmpPdf);
            Check("PdfExtractor: GetMeta 页数", meta?.Pages == 1);
            Check("PdfExtractor: GetMeta 标题", meta?.Title == "Test Document");

            Check("PdfExtractor: 不存在文件报错", PdfExtractor.Extract("/nonexistent.pdf").IsError);
            Check("PdfExtractor: 不存在文件 GetMeta null", PdfExtractor.GetMeta("/nonexistent.pdf") == null);
        }
        finally { try { File.Delete(tmpPdf); } catch { } }

        // ── 4. 损坏 PDF 优雅失败 ──
        var corrupt = Path.Combine(Path.GetTempPath(), "wc_corrupt_" + Guid.NewGuid().ToString("N")[..6] + ".pdf");
        try
        {
            File.WriteAllText(corrupt, "%PDF-1.4\nthis is not a valid pdf");
            Check("Pdf: 损坏 PDF 报错", PdfExtractor.Extract(corrupt).IsError);
        }
        finally { try { File.Delete(corrupt); } catch { } }
    }

    /// <summary>构造一个最小可解析 PDF（1 页、Type1 Helvetica、两个文本行 + 标题）。</summary>
    private static byte[] BuildMinimalPdf()
    {
        var bytes = new List<byte>();
        void Add(string s) => bytes.AddRange(Encoding.ASCII.GetBytes(s));

        Add("%PDF-1.4\n");

        var offsets = new long[7];
        offsets[1] = bytes.Count;
        Add("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        offsets[2] = bytes.Count;
        Add("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        offsets[3] = bytes.Count;
        Add("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");

        offsets[4] = bytes.Count;
        Add("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        var content = "BT /F1 12 Tf 72 720 Td (Hello World) Tj 0 -14 Td (Second line) Tj ET";
        offsets[5] = bytes.Count;
        Add($"5 0 obj\n<< /Length {content.Length} >>\nstream\n{content}\nendstream\nendobj\n");

        offsets[6] = bytes.Count;
        Add("6 0 obj\n<< /Title (Test Document) >>\nendobj\n");

        var xrefPos = bytes.Count;
        Add("xref\n0 7\n");
        Add("0000000000 65535 f \n");
        for (int i = 1; i <= 6; i++)
            Add($"{offsets[i]:D10} 00000 n \n");
        Add($"trailer\n<< /Size 7 /Root 1 0 R /Info 6 0 R >>\nstartxref\n{xrefPos}\n%%EOF\n");

        return bytes.ToArray();
    }

    /// <summary>WPS/老式二进制 Office（.wps/.et/.dps/.doc/.xls/.ppt）：CFB 解析器 + DOC/XLS/PPT 文本提取 + 容器识别/RTF/HTML 路由</summary>
    private static void TestWps(Action<string, bool> Check)
    {
        // ── 1. CFB 解析器 round-trip（小流走 mini 链、大流走常规扇区链）──
        var smallData = Encoding.ASCII.GetBytes("Hello Mini Stream!");
        var largeData = new byte[5000];
        for (int i = 0; i < largeData.Length; i++) largeData[i] = (byte)('A' + (i % 26));

        var cfb = BuildCfb(("SmallStream", smallData), ("LargeStream", largeData));
        Check("Wps: CFB 签名识别", CfbParser.IsCfb(cfb));
        Check("Wps: 非 CFB 拒绝", !CfbParser.IsCfb(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));

        var doc = CfbParser.Open(cfb);
        Check("Wps: CFB 解析成功", doc != null);
        if (doc != null)
        {
            var small = doc.GetStream("SmallStream");
            var large = doc.GetStream("LargeStream");
            Check("Wps: 小流（mini 链）字节一致",
                small != null && small.SequenceEqual(smallData));
            Check("Wps: 大流（常规扇区）字节一致",
                large != null && large.SequenceEqual(largeData));
            Check("Wps: 未知名返回 null", doc.GetStream("nope") == null);
            Check("Wps: 流名列表", doc.StreamNames.Contains("SmallStream") && doc.StreamNames.Contains("LargeStream"));
        }

        // ── 2. 容器识别 ──
        Check("Wps: 识别 CFB", LegacyOffice.DetectContainer(cfb) == LegacyOffice.Container.Cfb);
        Check("Wps: 识别 ZIP", LegacyOffice.DetectContainer(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0, 0 }) == LegacyOffice.Container.Zip);
        Check("Wps: 识别 RTF", LegacyOffice.DetectContainer(Encoding.ASCII.GetBytes("{\\rtf1\\ansi hi}")) == LegacyOffice.Container.Rtf);
        Check("Wps: 识别 HTML", LegacyOffice.DetectContainer(Encoding.ASCII.GetBytes("<html><body>x</body></html>")) == LegacyOffice.Container.Html);
        Check("Wps: 识别纯文本", LegacyOffice.DetectContainer(Encoding.UTF8.GetBytes("plain text")) == LegacyOffice.Container.Text);

        // ── 3. 二进制 DOC（FIB + piece table）──
        var docText = ExtractDocDirect();
        Check("Wps: DOC 提取 Hello World", docText.Contains("Hello World"));
        Check("Wps: DOC 提取中文", docText.Contains("第二行"));
        Check("Wps: DOC 压缩文本折半定位", ExtractDocCompressed().Contains("Compressed Hello"));

        // ── 4. 二进制 XLS（BIFF8 SST + LABEL）──
        var xlsText = LegacyOffice.ExtractXls(BuildXlsWorkbook(new[] { "共享字符串一", "shared2" }, "内联标签"));
        Check("Wps: XLS 提取 SST", xlsText.Contains("共享字符串一") && xlsText.Contains("shared2"));
        Check("Wps: XLS 提取 LABEL", xlsText.Contains("内联标签"));
        Check("Wps: XLS 空白表格不 dump 元数据", LegacyOffice.ExtractXls(BuildXlsWorkbookModern(Array.Empty<string>(), false)) == "(XLS 无文本内容)");
        Check("Wps: XLS 加密文件", LegacyOffice.ExtractXls(BuildXlsWorkbookModern(Array.Empty<string>(), true)) == "(XLS 已加密)");

        // ── 5. 二进制 PPT（TextCharsAtom）──
        var pptText = LegacyOffice.ExtractPpt(BuildPptStream("幻灯片标题内容"));
        Check("Wps: PPT 提取文本", pptText.Contains("幻灯片标题内容"));
        Check("Wps: PPT 嵌套容器文本", LegacyOffice.ExtractPpt(BuildPptStreamNested("嵌套容器里的标题")).Contains("嵌套容器里的标题"));
        Check("Wps: PPT 加密分支", LegacyOffice.ExtractPpt(BuildPptStream("x"), encrypted: true) == "(PPT 已加密)");

        // ── 6. RTF 剥离 ──
        var rtfText = LegacyOffice.ExtractRtf("{\\rtf1\\ansi Hello {\\b bold} world \\par second}");
        Check("Wps: RTF 含正文", rtfText.Contains("Hello") && rtfText.Contains("bold") && rtfText.Contains("world"));
        Check("Wps: RTF 剥离控制字", !rtfText.Contains("\\rtf") && !rtfText.Contains("\\par") && !rtfText.Contains("{"));

        // ── 7. 端到端：CFB .wps → read_file ──
        try
        {
            var wpsPath = Path.Combine(Path.GetTempPath(), "wc_test_" + Guid.NewGuid().ToString("N")[..6] + ".wps");
            var wpsCfb = BuildCfb(("WordDocument", BuildDocWordStream("WPS 文档内容")), ("0Table", BuildDocTableStream("WPS 文档内容")));
            File.WriteAllBytes(wpsPath, wpsCfb);
            var readResult = new ReadFileTool().ExecuteAsync(new() { ["file_path"] = wpsPath }).Result;
            Check("Wps: read_file 读 .wps", readResult.Contains("<doc>") && readResult.Contains("WPS 文档内容"));
            File.Delete(wpsPath);
        }
        catch { Check("Wps: read_file 读 .wps", false); }

        // ── 8. 端到端：CFB .ppt 加密检测（headerToken 高 16 位）──
        try
        {
            var encPptPath = Path.Combine(Path.GetTempPath(), "wc_test_" + Guid.NewGuid().ToString("N")[..6] + ".ppt");
            var encCfb = BuildCfb(("PowerPoint Document", BuildPptStream("不应被读取")), ("Current User", BuildPptCurrentUser(true)));
            File.WriteAllBytes(encPptPath, encCfb);
            Check("Wps: PPT 加密检测", LegacyOffice.Extract(encPptPath) == "(PPT 已加密)");

            var plainPptPath = Path.Combine(Path.GetTempPath(), "wc_test_" + Guid.NewGuid().ToString("N")[..6] + ".ppt");
            var plainCfb = BuildCfb(("PowerPoint Document", BuildPptStream("明文标题")), ("Current User", BuildPptCurrentUser(false)));
            File.WriteAllBytes(plainPptPath, plainCfb);
            Check("Wps: PPT 未加密正常提取", LegacyOffice.Extract(plainPptPath).Contains("明文标题"));

            File.Delete(encPptPath);
            File.Delete(plainPptPath);
        }
        catch { Check("Wps: PPT 加密检测", false); }
    }

    private static string ExtractDocDirect()
        => LegacyOffice.ExtractDoc(
            BuildDocWordStream("Hello World\r第二行"),
            BuildDocTableStream("Hello World\r第二行"),
            null);

    private static string ExtractDocCompressed()
        => LegacyOffice.ExtractDoc(
            BuildDocWordStreamCompressed("Compressed Hello"),
            BuildDocTableStreamCompressed("Compressed Hello"),
            null);

    /// <summary>构造最小 WordDocument 流：FIB + UTF-16 文本（piece table 在表流，由 BuildDocTableStream 提供）。</summary>
    private static byte[] BuildDocWordStream(string text)
    {
        const int textOffset = 2048;
        int ccp = text.Length;

        int size = Math.Max(4096, textOffset + ccp * 2 + 4);
        var b = new byte[size];

        W16(b, 0, 0xA5EC);          // wIdent
        // flags @10 = 0（未加密、0Table）
        W16(b, 32, 14);             // csw
        W16(b, 62, 22);             // cslw @ 34 + csw*2 = 62
        W32(b, 76, (uint)ccp);      // ccpText @ fibRgLw[3]，fibRgLwOff=36+csw*2=64 → 64+12=76
        W32(b, 418, 0);             // fcClx @ fibRgFcLcb[33]，fibRgFcLcbOff=64+22*4+2=154 → 154+264=418（表流内偏移 0）
        W32(b, 422, 21);            // lcbClx = 1 + 4 + (2*4) + 8 = 21

        var textBytes = Encoding.Unicode.GetBytes(text);
        Array.Copy(textBytes, 0, b, textOffset, textBytes.Length);
        return b;
    }

    /// <summary>构造最小 WordDocument 流（压缩文本：cp1252 单字节，piece fc 折半定位）。</summary>
    private static byte[] BuildDocWordStreamCompressed(string text)
    {
        const int textOffset = 2048;
        int ccp = text.Length;

        int size = Math.Max(4096, textOffset + ccp + 4);
        var b = new byte[size];

        W16(b, 0, 0xA5EC);
        W16(b, 32, 14);             // csw
        W16(b, 62, 22);             // cslw @ 34 + csw*2
        W32(b, 76, (uint)ccp);      // ccpText @ fibRgLw[3]
        W32(b, 418, 0);             // fcClx → 表流偏移 0
        W32(b, 422, 21);            // lcbClx

        var textBytes = Encoding.ASCII.GetBytes(text);
        Array.Copy(textBytes, 0, b, textOffset, textBytes.Length);
        return b;
    }

    /// <summary>构造最小表流：单 piece 的 piece table（Clx，piece 的 fc 指向 WordDocument 流 textOffset）。</summary>
    private static byte[] BuildDocTableStream(string text)
    {
        const int textOffset = 2048;
        int ccp = text.Length;

        var clx = new byte[21];
        clx[0] = 0x02;              // Pcdt
        W32(clx, 1, 16);            // lcb（PlcPcd 大小 = 4*(n+1)+8*n, n=1 → 16）
        W32(clx, 5, 0);             // CP[0]
        W32(clx, 9, (uint)ccp);     // CP[1]
        // PCD @ offset 13: aCP(2)=0, fc(4)=textOffset, prm(2)=0
        W32(clx, 15, textOffset);   // fc @ pcd+2（非压缩：字节偏移 = fc）
        return clx;
    }

    /// <summary>构造最小表流（压缩 piece：bit30=1，字节偏移 = fc/2，故 fc = 2*textOffset）。</summary>
    private static byte[] BuildDocTableStreamCompressed(string text)
    {
        const int textOffset = 2048;
        int ccp = text.Length;

        var clx = new byte[21];
        clx[0] = 0x02;              // Pcdt
        W32(clx, 1, 16);
        W32(clx, 5, 0);
        W32(clx, 9, (uint)ccp);
        W32(clx, 15, (uint)(textOffset * 2) | 0x40000000u); // 压缩 fc
        return clx;
    }

    /// <summary>构造最小 BIFF8 Workbook 流：SST 共享字符串 + LABEL 内联标签 + EOF。</summary>
    private static byte[] BuildXlsWorkbook(string[] sstStrings, string inlineLabel)
    {
        var body = new MemoryStream();

        var sst = new MemoryStream();
        W32S(sst, sstStrings.Length);
        W32S(sst, sstStrings.Length);
        foreach (var s in sstStrings)
        {
            var bs = BiffString(s);
            sst.Write(bs, 0, bs.Length);
        }
        WriteRecord(body, 0x00FC, sst.ToArray());

        var label = new MemoryStream();
        W16S(label, 0); // row
        W16S(label, 0); // col
        W16S(label, 0); // xf
        var lb = BiffString(inlineLabel);
        label.Write(lb, 0, lb.Length);
        WriteRecord(body, 0x0204, label.ToArray());

        WriteRecord(body, 0x000A, Array.Empty<byte>()); // EOF
        return body.ToArray();
    }

    /// <summary>构造带 BOF(BIFF8) 的 Workbook 流：BOF + 可选 FILEPASS + SST + EOF。</summary>
    private static byte[] BuildXlsWorkbookModern(string[] sstStrings, bool filePass)
    {
        var body = new MemoryStream();

        var bof = new MemoryStream();
        W16S(bof, 0x0600); // BIFF8
        W16S(bof, 0x0005); // dt = workbook globals
        W16S(bof, 0x0DBB); // rupBuild
        W16S(bof, 0x07CC); // rupYear
        W32S(bof, 0x00000041); // bfh
        W32S(bof, 0x00000006); // sfo
        WriteRecord(body, 0x0809, bof.ToArray());

        if (filePass)
            WriteRecord(body, 0x002F, new byte[] { 0x00, 0x00 }); // FILEPASS

        var sst = new MemoryStream();
        W32S(sst, sstStrings.Length);
        W32S(sst, sstStrings.Length);
        foreach (var s in sstStrings)
        {
            var bs = BiffString(s);
            sst.Write(bs, 0, bs.Length);
        }
        WriteRecord(body, 0x00FC, sst.ToArray());

        WriteRecord(body, 0x000A, Array.Empty<byte>()); // EOF
        return body.ToArray();
    }

    private static byte[] BiffString(string s)
    {
        var ms = new MemoryStream();
        W16S(ms, s.Length);
        ms.WriteByte(0x01); // fHighByte（UTF-16）
        var enc = Encoding.Unicode.GetBytes(s);
        ms.Write(enc, 0, enc.Length);
        return ms.ToArray();
    }

    private static void WriteRecord(MemoryStream ms, ushort id, byte[] data)
    {
        W16S(ms, id);
        W16S(ms, data.Length);
        ms.Write(data, 0, data.Length);
    }

    /// <summary>构造最小 PPT 流：单个 TextCharsAtom（UTF-16）。</summary>
    private static byte[] BuildPptStream(string text)
    {
        var ms = new MemoryStream();
        W16S(ms, 0x0000);           // recVer=0 + recInstance=0
        W16S(ms, 0x0FA0);           // RT_TextCharsAtom
        var enc = Encoding.Unicode.GetBytes(text);
        W32S(ms, enc.Length);
        ms.Write(enc, 0, enc.Length);
        return ms.ToArray();
    }

    /// <summary>构造 Current User 流（CurrentUserAtom），headerToken 高 16 位标记是否加密。</summary>
    private static byte[] BuildPptCurrentUser(bool encrypted)
    {
        var ms = new MemoryStream();
        W16S(ms, 0x0000);                          // recVer+recInstance
        W16S(ms, 0x0FF6);                          // CurrentUserAtom
        var atom = new MemoryStream();
        W32S(atom, 20);                            // size
        W32S(atom, encrypted ? 0xF3D1C05Fu : 0xE391C05Fu); // headerToken（高 16 位 0xF3D1=加密）
        W32S(atom, 0);                             // offsetToCurrentEdit
        W16S(atom, 0);                             // lenUserName
        W16S(atom, 0x03F4);                        // docFileVersion
        W16S(atom, 0);                             // unused
        var ab = atom.ToArray();
        W32S(ms, ab.Length);                       // recLen
        ms.Write(ab, 0, ab.Length);
        return ms.ToArray();
    }

    /// <summary>构造嵌套 PPT 流：TextCharsAtom 嵌在容器（recVer=0xF）内部。</summary>
    private static byte[] BuildPptStreamNested(string text)
    {
        var child = new MemoryStream();
        W16S(child, 0x0000); // TextCharsAtom 头
        W16S(child, 0x0FA0);
        var enc = Encoding.Unicode.GetBytes(text);
        W32S(child, enc.Length);
        child.Write(enc, 0, enc.Length);

        var ms = new MemoryStream();
        W16S(ms, 0x000F); // recVer=0xF（容器）+ recInstance=0
        W16S(ms, 0x0F9F); // 容器类型（任意，模拟 Text 容器）
        var cb = child.ToArray();
        W32S(ms, cb.Length);
        ms.Write(cb, 0, cb.Length);
        return ms.ToArray();
    }

    /// <summary>
    /// 构造最小合法 CFB 复合文档（512 字节扇区 / 64 字节 mini 扇区）。
    /// 小流（&lt;4096）进 mini 流 + mini FAT；大流（≥4096）进常规扇区 + FAT。
    /// </summary>
    private static byte[] BuildCfb(params (string name, byte[] data)[] streams)
    {
        const int sectorSize = 512;
        const int miniSectorSize = 64;
        const int headerSize = 512;
        const uint miniCutoff = 4096;

        var mini = streams.Where(s => s.data.Length < miniCutoff).ToArray();
        var regular = streams.Where(s => s.data.Length >= miniCutoff).ToArray();

        // mini 流 + mini FAT
        var miniData = new MemoryStream();
        var miniFat = new List<uint>();
        var miniMeta = new List<(string name, byte[] data, int miniStart, int size)>();
        foreach (var s in mini)
        {
            int cnt = (s.data.Length + miniSectorSize - 1) / miniSectorSize;
            int start = miniFat.Count;
            var padded = new byte[cnt * miniSectorSize];
            Array.Copy(s.data, padded, s.data.Length);
            miniData.Write(padded, 0, padded.Length);
            for (int i = 0; i < cnt; i++)
                miniFat.Add((i < cnt - 1) ? (uint)(start + i + 1) : CfbParser.EndOfChain);
            miniMeta.Add((s.name, s.data, start, s.data.Length));
        }
        byte[] miniStreamBytes = miniData.ToArray();

        // 常规扇区布局（扇区号 N 对应偏移 (N+1)*512，header 在偏移 0 不算扇区）
        // 物理顺序：header, FAT, dir, [miniFAT], [mini 流], [常规流]
        int fatSector = 0, dirSector = 1;
        int miniFatSector = miniFat.Count > 0 ? 2 : -1;
        int next = miniFatSector >= 0 ? 3 : 2;

        int miniStreamStart = -1;
        int miniStreamSectors = (miniStreamBytes.Length + sectorSize - 1) / sectorSize;
        if (miniStreamSectors > 0) { miniStreamStart = next; next += miniStreamSectors; }

        var regMeta = new List<(string name, byte[] data, int start, int cnt)>();
        foreach (var s in regular)
        {
            int cnt = (s.data.Length + sectorSize - 1) / sectorSize;
            regMeta.Add((s.name, s.data, next, cnt));
            next += cnt;
        }
        if (next > 128) throw new Exception("CFB 测试构建器超出单 FAT 扇区");

        // FAT
        var fat = new uint[128];
        for (int i = 0; i < 128; i++) fat[i] = CfbParser.FreeSect;
        fat[fatSector] = CfbParser.FatSect;   // FAT 扇区自标记
        fat[dirSector] = CfbParser.EndOfChain;
        if (miniFatSector >= 0) fat[miniFatSector] = CfbParser.EndOfChain;
        for (int i = 0; i < miniStreamSectors; i++)
            fat[miniStreamStart + i] = (i < miniStreamSectors - 1) ? (uint)(miniStreamStart + i + 1) : CfbParser.EndOfChain;
        foreach (var r in regMeta)
            for (int i = 0; i < r.cnt; i++)
                fat[r.start + i] = (i < r.cnt - 1) ? (uint)(r.start + i + 1) : CfbParser.EndOfChain;

        // 组装
        var file = new MemoryStream();

        var header = new byte[headerSize];
        var sig = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
        Array.Copy(sig, 0, header, 0, 8);
        W16(header, 24, 0x003E); // minor version
        W16(header, 26, 0x0003); // major version
        W16(header, 28, 0xFFFE); // byte order
        W16(header, 30, 9);      // sector shift
        W16(header, 32, 6);      // mini sector shift
        W32(header, 40, 1);      // num directory sectors
        W32(header, 44, 1);      // num FAT sectors
        W32(header, 48, dirSector);
        W32(header, 56, miniCutoff);
        W32(header, 60, miniFatSector >= 0 ? (uint)miniFatSector : CfbParser.EndOfChain);
        W32(header, 64, miniFatSector >= 0 ? 1u : 0u);
        W32(header, 68, CfbParser.EndOfChain);
        W32(header, 72, 0);
        W32(header, 76, fatSector);
        for (int i = 1; i < 109; i++) W32(header, 76 + i * 4, CfbParser.FreeSect);
        file.Write(header, 0, header.Length);

        var fatBytes = new byte[sectorSize];
        for (int i = 0; i < 128; i++) W32(fatBytes, i * 4, fat[i]);
        file.Write(fatBytes, 0, fatBytes.Length);

        var dir = new byte[sectorSize];
        WriteDirEntry(dir, 0, "Root Entry", 5,
            (uint)(miniStreamStart >= 0 ? miniStreamStart : unchecked((int)CfbParser.EndOfChain)),
            (ulong)miniStreamBytes.Length);
        int di = 1;
        foreach (var m in miniMeta) WriteDirEntry(dir, di++ * 128, m.name, 2, (uint)m.miniStart, (ulong)m.size);
        foreach (var r in regMeta) WriteDirEntry(dir, di++ * 128, r.name, 2, (uint)r.start, (ulong)r.data.Length);
        file.Write(dir, 0, dir.Length);

        if (miniFatSector >= 0)
        {
            var mf = new byte[sectorSize];
            for (int i = 0; i < miniFat.Count; i++) W32(mf, i * 4, miniFat[i]);
            file.Write(mf, 0, mf.Length);
        }

        if (miniStreamSectors > 0)
        {
            var padded = new byte[miniStreamSectors * sectorSize];
            Array.Copy(miniStreamBytes, padded, miniStreamBytes.Length);
            file.Write(padded, 0, padded.Length);
        }

        foreach (var r in regMeta)
        {
            var padded = new byte[r.cnt * sectorSize];
            Array.Copy(r.data, padded, r.data.Length);
            file.Write(padded, 0, padded.Length);
        }

        return file.ToArray();
    }

    private static void WriteDirEntry(byte[] dir, int off, string name, int type, uint start, ulong size)
    {
        var enc = Encoding.Unicode.GetBytes(name);
        Array.Copy(enc, 0, dir, off, Math.Min(enc.Length, 64));
        W16(dir, off + 64, (name.Length + 1) * 2); // nameLen（含 null 的字节数）
        dir[off + 66] = (byte)type;
        dir[off + 67] = 1; // black
        W32(dir, off + 68, CfbParser.FreeSect); // left
        W32(dir, off + 72, CfbParser.FreeSect); // right
        W32(dir, off + 76, CfbParser.FreeSect); // child
        W32(dir, off + 116, start);
        W64(dir, off + 120, size);
    }

    // 小端写入辅助（测试 fixture 构造）

    private static void W16(byte[] b, int o, int v) { b[o] = (byte)(v & 0xFF); b[o + 1] = (byte)((v >> 8) & 0xFF); }

    private static void W32(byte[] b, int o, long v) { b[o] = (byte)(v & 0xFF); b[o + 1] = (byte)((v >> 8) & 0xFF); b[o + 2] = (byte)((v >> 16) & 0xFF); b[o + 3] = (byte)((v >> 24) & 0xFF); }

    private static void W64(byte[] b, int o, ulong v) { W32(b, o, (long)(v & 0xFFFFFFFF)); W32(b, o + 4, (long)(v >> 32)); }

    private static void W16S(MemoryStream ms, int v) { ms.WriteByte((byte)(v & 0xFF)); ms.WriteByte((byte)((v >> 8) & 0xFF)); }

    private static void W32S(MemoryStream ms, long v) { ms.WriteByte((byte)(v & 0xFF)); ms.WriteByte((byte)((v >> 8) & 0xFF)); ms.WriteByte((byte)((v >> 16) & 0xFF)); ms.WriteByte((byte)((v >> 24) & 0xFF)); }

}
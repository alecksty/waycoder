using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace WayCoder.Infra;

/// <summary>
/// 老式二进制 Office 文档 / WPS 格式文本提取器。
///
/// 现代 WPS（2016+）默认保存为 OOXML（.docx/.xlsx/.pptx），已由 <see cref="OfficeExtractor"/> 覆盖；
/// 本类补齐老式二进制格式：.doc（WordDocument 流 + piece table）、.xls（Workbook/Book 流的 BIFF）、
/// .ppt（PowerPoint Document 流的 TextCharsAtom/TextBytesAtom），以及 WPS 的老后缀 .wps/.et/.dps。
/// 同时支持这些后缀被误存为 RTF / HTML / 纯文本 / ZIP(OFFXML) 时的识别与路由。
///
/// 全程手搓、零外部依赖、AOT 兼容、跨平台。
/// 规范参考：[MS-CFB] [MS-DOC] [MS-XLS] [MS-PPT]。
/// </summary>
public static class LegacyOffice
{
    public enum Container { Unknown, Cfb, Zip, Rtf, Html, Text }

    private const int DefaultMaxChars = 50_000;

    // ════════════════════════════════════════════════════════════
    // 容器识别 + 顶层路由
    // ════════════════════════════════════════════════════════════

    /// <summary>按文件头魔数识别容器类型（扩展名不可靠，必须看内容）。</summary>
    public static Container DetectContainer(byte[] data)
    {
        if (data.Length == 0) return Container.Unknown;
        if (CfbParser.IsCfb(data)) return Container.Cfb;
        if (data.Length >= 4 && data[0] == 0x50 && data[1] == 0x4B && data[2] == 0x03 && data[3] == 0x04)
            return Container.Zip;

        // RTF：前导空白后跟 {\rtf
        int i = 0;
        while (i < data.Length && (data[i] == ' ' || data[i] == '\t' || data[i] == '\r' || data[i] == '\n')) i++;
        if (i + 5 <= data.Length && data[i] == '{' && data[i + 1] == '\\' &&
            (data[i + 2] == 'r' || data[i + 2] == 'R') &&
            (data[i + 3] == 't' || data[i + 3] == 'T') &&
            (data[i + 4] == 'f' || data[i + 4] == 'F'))
            return Container.Rtf;

        // HTML：跳过 BOM/空白后以 '<' 开头
        i = 0;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) i = 3;
        while (i < data.Length && (data[i] == ' ' || data[i] == '\t' || data[i] == '\r' || data[i] == '\n')) i++;
        if (i < data.Length && data[i] == '<') return Container.Html;

        return Container.Text;
    }

    /// <summary>识别并提取老式 Office/WPS 文档文本。返回纯文本（含错误提示文案）。</summary>
    public static string Extract(string filePath, int maxChars = DefaultMaxChars)
    {
        try
        {
            byte[] data;
            try { data = File.ReadAllBytes(filePath); }
            catch (Exception ex) { return $"错误：无法读取 {filePath}: {ex.Message}"; }

            if (data.Length == 0) return "(空文件)";

            switch (DetectContainer(data))
            {
                case Container.Cfb: return ExtractCfb(data, maxChars);
                case Container.Zip: return ExtractZipDisguised(filePath, maxChars);
                case Container.Rtf: return ExtractRtf(DecodeAscii(data), maxChars);
                case Container.Html: return StripHtml(DecodeText(data), maxChars);
                case Container.Text: return DecodeText(data);
                default: return "(无法识别的二进制格式)";
            }
        }
        catch (Exception ex)
        {
            return $"错误：读取失败: {ex.Message}";
        }
    }

    // ════════════════════════════════════════════════════════════
    // CFB 容器 → 按流名分派 DOC / XLS / PPT
    // ════════════════════════════════════════════════════════════

    private static string ExtractCfb(byte[] data, int maxChars)
    {
        var doc = CfbParser.Open(data);
        if (doc == null) return "(无效的复合文档)";

        var wordDoc = doc.GetStream("WordDocument");
        if (wordDoc != null)
            return ExtractDoc(wordDoc, doc.GetStream("0Table"), doc.GetStream("1Table"), maxChars);

        var workbook = doc.GetStream("Workbook") ?? doc.GetStream("Book");
        if (workbook != null) return ExtractXls(workbook, maxChars);

        var pptDoc = doc.GetStream("PowerPoint Document");
        if (pptDoc != null)
            return ExtractPpt(pptDoc, maxChars, IsPptEncrypted(doc.GetStream("Current User")));

        var names = string.Join(", ", doc.StreamNames.Take(10));
        return $"(复合文档，未识别的内部流: {names})";
    }

    private static string ExtractZipDisguised(string filePath, int maxChars)
    {
        try
        {
            using var zip = ZipFile.OpenRead(filePath);
            if (zip.GetEntry("word/document.xml") != null) return OfficeExtractor.ExtractDocx(filePath);
            if (zip.GetEntry("xl/workbook.xml") != null) return OfficeExtractor.ExtractXlsx(filePath);
            if (zip.GetEntry("ppt/presentation.xml") != null) return OfficeExtractor.ExtractPptx(filePath);
            return "(ZIP 归档，非 Office 文档)";
        }
        catch (Exception ex)
        {
            return $"(损坏的 ZIP 归档: {ex.Message})";
        }
    }

    // ════════════════════════════════════════════════════════════
    // 二进制 DOC（WordDocument 流 → FIB → piece table）
    // ════════════════════════════════════════════════════════════

    /// <summary>从 WordDocument 流（及 0Table/1Table 表流）提取纯文本。</summary>
    public static string ExtractDoc(byte[] wordDoc, byte[]? table0, byte[]? table1, int maxChars = DefaultMaxChars)
    {
        try
        {
            if (wordDoc.Length < 32) return "(空 DOC)";
            if (Bin.U16(wordDoc, 0) != 0xA5EC) return "(无效 DOC：FIB 标识不符)";

            int flags = Bin.U16(wordDoc, 10);
            if ((flags & 0x0100) != 0) return "(DOC 已加密，无法提取文本)"; // fEncrypted

            bool fWhichTblStm = (flags & 0x0200) != 0; // bit9 → 1Table 否则 0Table
            byte[] table = fWhichTblStm
                ? (table1 ?? table0 ?? Array.Empty<byte>())
                : (table0 ?? table1 ?? Array.Empty<byte>());

            ushort csw = Bin.U16(wordDoc, 32);
            // FIB 布局：csw@32 → FibRgW(csw*2) → cslw@34+csw*2 → FibRgLw@36+csw*2 → cbRgFcLcb(2) → FibRgFcLcb
            ushort cslw = Bin.U16(wordDoc, 34 + csw * 2);
            int fibRgLwOff = 36 + csw * 2;
            int fibRgFcLcbOff = fibRgLwOff + cslw * 4 + 2; // +2 = cbRgFcLcb 字段
            if (fibRgFcLcbOff + 33 * 8 + 8 > wordDoc.Length) return "(无效 DOC：FIB 截断)";

            uint ccpText = Bin.U32(wordDoc, fibRgLwOff + 3 * 4); // 字符总数
            uint fcClx = Bin.U32(wordDoc, fibRgFcLcbOff + 33 * 8);
            uint lcbClx = Bin.U32(wordDoc, fibRgFcLcbOff + 33 * 8 + 4);

            if (lcbClx < 4 || ccpText == 0)
                return ExtractUtf16Runs(wordDoc, maxChars);

            // CLX（piece table）位于表流：fcClx 是表流内偏移（MS-DOC §2.4.1）
            if (fcClx + lcbClx > (uint)table.Length)
                return ExtractUtf16Runs(wordDoc, maxChars);
            byte[] clx = Slice(table, (int)fcClx, (int)lcbClx);

            // 解析 Pcdt：可选 [0x02][lcb] 头 + PlcPcd
            int plcOff;
            uint plcLen;
            if (clx.Length >= 5 && clx[0] == 0x02)
            {
                plcLen = Bin.U32(clx, 1);
                plcOff = 5;
            }
            else
            {
                plcLen = (uint)clx.Length;
                plcOff = 0;
            }

            if (plcLen < 16 || plcOff + (int)plcLen > clx.Length)
                return ExtractUtf16Runs(wordDoc, maxChars);

            int n = (int)((plcLen - 4) / 12); // piece 数
            if (n <= 0) return ExtractUtf16Runs(wordDoc, maxChars);

            var sb = new StringBuilder();
            for (int i = 0; i < n && sb.Length < maxChars; i++)
            {
                uint cp0 = Bin.U32(clx, plcOff + i * 4);
                uint cp1 = Bin.U32(clx, plcOff + (i + 1) * 4);
                int pcdOff = plcOff + 4 * (n + 1) + i * 8;
                if (pcdOff + 8 > clx.Length) break;

                uint fcRaw = Bin.U32(clx, pcdOff + 2); // FcCompressed
                bool compressed = (fcRaw & 0x40000000) != 0; // bit30
                uint fc = fcRaw & 0x3FFFFFFF;                // bit31 为保留位 r1，忽略

                int charCount = (int)(cp1 - cp0);
                if (charCount <= 0) continue;

                // Pcd.fc 指向 WordDocument 流；压缩文本字节偏移 = fc/2，非压缩 = fc
                int fileOff = compressed ? (int)(fc >> 1) : (int)fc;

                AppendPieceText(sb, wordDoc, fileOff, charCount, compressed, maxChars - sb.Length);
            }

            var result = sb.ToString().Trim();
            return result.Length > 0 ? result : "(DOC 无文本内容)";
        }
        catch
        {
            return "(DOC 解析失败)";
        }
    }

    private static void AppendPieceText(StringBuilder sb, byte[] src, int off, int charCount, bool compressed, int budget)
    {
        if (compressed)
        {
            // 压缩文本为 cp1252 单字节，0x80–0x9F 需特殊映射
            for (int i = 0; i < charCount && sb.Length < budget && off + i < src.Length; i++)
                AppendDocChar(sb, Cp1252(src[off + i]));
        }
        else
        {
            for (int i = 0; i < charCount && sb.Length < budget && off + i * 2 + 1 < src.Length; i++)
                AppendDocChar(sb, Bin.U16(src, off + i * 2));
        }
    }

    /// <summary>DOC 文本字符 → 可打印输出（段落/制表/换行规范化）。</summary>
    private static void AppendDocChar(StringBuilder sb, int c)
    {
        switch (c)
        {
            case 0x0D: // 段落标记
            case 0x0B: // 换行
            case 0x0C: // 分页
            case 0x07: // 表格单元格结束
                sb.Append('\n');
                break;
            case 0x09:
                sb.Append('\t');
                break;
            case 0x13: case 0x14: case 0x15: // 域 begin/sep/end
            case 0x01: case 0x02: case 0x05: case 0x08: // 对象/脚注等控制符
                break;
            default:
                if (c >= 0x20) sb.Append((char)c);
                break;
        }
    }

    // ════════════════════════════════════════════════════════════
    // 二进制 XLS（Workbook 流 → BIFF8 记录）
    // ════════════════════════════════════════════════════════════

    /// <summary>从 Workbook/Book 流（BIFF）提取纯文本。</summary>
    public static string ExtractXls(byte[] workbook, int maxChars = DefaultMaxChars)
    {
        try
        {
            var sst = new List<string>();
            var labels = new List<string>();
            bool modern = false; // 是否 BIFF5/8（有 SST/LABEL 结构，无需退化扫描）
            bool encrypted = false; // 是否带 FILEPASS（密码保护，正文加密无法提取）

            int i = 0;
            while (i + 4 <= workbook.Length)
            {
                ushort id = Bin.U16(workbook, i);
                ushort len = Bin.U16(workbook, i + 2);
                int dataOff = i + 4;
                if (dataOff + len > workbook.Length) break;

                switch (id)
                {
                    case 0x0809: // BOF：版本字段判定 BIFF5/8（0x0500/0x0600）vs 老 BIFF2-4
                        if (len >= 2 && Bin.U16(workbook, dataOff) >= 0x0500) modern = true;
                        break;
                    case 0x002F: // FILEPASS 密码保护记录：正文加密，无法提取明文
                        encrypted = true;
                        break;
                    case 0x00FC: // SST 共享字符串表
                        ParseSst(workbook, dataOff, len, sst);
                        break;
                    case 0x0204: // LABEL 内联标签（row2+col2+xf2 后接字符串）
                        labels.Add(ParseXlString(workbook, dataOff + 6, len - 6, out _));
                        break;
                    case 0x00FD: // LABELSST 引用 SST（row2+col2+xf2+isst4）
                        if (len >= 10)
                        {
                            uint idx = Bin.U32(workbook, dataOff + 6);
                            if (idx < sst.Count) labels.Add(sst[(int)idx]);
                        }
                        break;
                    case 0x0207: // STRING 公式字符串结果
                        labels.Add(ParseXlString(workbook, dataOff, len, out _));
                        break;
                    case 0x00D6: // RSTRING 富文本（row2+col2+xf2 后接字符串）
                        labels.Add(ParseXlString(workbook, dataOff + 6, len - 6, out _));
                        break;
                    case 0x000A: // EOF
                        i = workbook.Length;
                        continue;
                }
                i = dataOff + len;
            }

            // 密码保护文件：正文加密，任何「文本」都是乱码，直接报加密
            if (encrypted) return "(XLS 已加密)";

            var sb = new StringBuilder();
            foreach (var s in sst)
                if (!string.IsNullOrWhiteSpace(s)) { sb.AppendLine(s); if (sb.Length > maxChars) break; }
            foreach (var s in labels)
                if (!string.IsNullOrWhiteSpace(s)) { sb.AppendLine(s); if (sb.Length > maxChars) break; }

            var result = sb.ToString().Trim();
            if (result.Length > 0) return result;

            // BIFF5/8（有 SST/LABEL 结构）但无文本 → 空白表格，返回无内容标记，
            // 不做 UTF-16 退化扫描（否则会把字体名/数字格式/表名当正文 dump 出来）。
            if (modern) return "(XLS 无文本内容)";

            // 退化：BIFF2-4（Book 流，无 SST，文本内联）→ UTF-16 文本串扫描
            return ExtractUtf16Runs(workbook, maxChars);
        }
        catch
        {
            return "(XLS 解析失败)";
        }
    }

    private static void ParseSst(byte[] b, int off, int len, List<string> sst)
    {
        int p = off + 8; // cstTotal(4) + cstUnique(4)
        int end = off + len;
        int guard = 0;
        while (p + 3 <= end && guard++ < 1_000_000)
        {
            string s = ParseXlString(b, p, end - p, out int consumed);
            sst.Add(s);
            if (consumed <= 0) break;
            p += consumed;
        }
    }

    /// <summary>解析 BIFF8 字符串（XLUnicodeRichExtendedString）。</summary>
    private static string ParseXlString(byte[] b, int off, int maxLen, out int consumed)
    {
        consumed = 0;
        if (off + 3 > b.Length) return "";
        ushort cch = Bin.U16(b, off);
        byte flags = b[off + 2];
        bool highByte = (flags & 0x01) != 0;
        bool extSt = (flags & 0x04) != 0;
        bool richSt = (flags & 0x08) != 0;

        int p = off + 3;
        int cRun = 0, cbExt = 0;
        if (richSt) { cRun = Bin.U16(b, p); p += 2; }
        if (extSt) { cbExt = Bin.I32(b, p); p += 4; }

        var sb = new StringBuilder(cch);
        for (int i = 0; i < cch && p < b.Length; i++)
        {
            if (highByte)
            {
                if (p + 1 < b.Length) sb.Append((char)Bin.U16(b, p));
                p += 2;
            }
            else
            {
                sb.Append(Cp1252(b[p]));
                p += 1;
            }
        }

        p += cRun * 4 + cbExt;
        consumed = p - off;
        return sb.ToString();
    }

    // ════════════════════════════════════════════════════════════
    // 二进制 PPT（PowerPoint Document 流 → 文本 atom）
    // ════════════════════════════════════════════════════════════

    /// <summary>从 PowerPoint Document 流提取纯文本。</summary>
    public static string ExtractPpt(byte[] pptDoc, int maxChars = DefaultMaxChars, bool encrypted = false)
    {
        try
        {
            if (encrypted) return "(PPT 已加密)";

            var sb = new StringBuilder();
            // PPT 记录是分层嵌套结构（容器 recVer=0xF），文本 atom 嵌在容器内部，
            // 必须递归下降，否则平铺扫描会跳过容器内的所有子记录。
            ScanPptRecords(pptDoc, 0, pptDoc.Length, sb, maxChars, 0);
            var result = sb.ToString().Trim();
            return result.Length > 0 ? result : "(PPT 无文本内容)";
        }
        catch
        {
            return "(PPT 解析失败)";
        }
    }

    /// <summary>从 Current User 流判定 PPT 是否标准加密（headerToken 高 16 位 0xF3D1=加密）。</summary>
    private static bool IsPptEncrypted(byte[]? currentUser)
    {
        if (currentUser == null || currentUser.Length < 16) return false;
        if (Bin.U16(currentUser, 2) != 0x0FF6) return false; // 非 CurrentUserAtom
        uint headerToken = Bin.U32(currentUser, 12);
        return (headerToken >> 16) == 0xF3D1;
    }

    /// <summary>递归扫描 PPT 记录树：[start, end) 范围内的记录。</summary>
    private static void ScanPptRecords(byte[] b, int start, int end, StringBuilder sb, int maxChars, int depth)
    {
        int i = start;
        int guard = 0;
        while (i + 8 <= end && sb.Length < maxChars && guard++ < 1_000_000)
        {
            ushort verInst = Bin.U16(b, i);
            int recVer = verInst & 0x000F;
            ushort recType = Bin.U16(b, i + 2);
            uint recLen = Bin.U32(b, i + 4);
            int dataOff = i + 8;
            if (dataOff + recLen > end) break;

            if (recType == 0x0FA0) // RT_TextCharsAtom（UTF-16LE）
            {
                AppendUtf16Run(sb, b, dataOff, (int)recLen, maxChars);
            }
            else if (recType == 0x0FA8) // RT_TextBytesAtom（ANSI 单字节）
            {
                for (int k = 0; k < recLen && dataOff + k < b.Length && sb.Length < maxChars; k++)
                    sb.Append(Cp1252(b[dataOff + k]));
                sb.AppendLine();
            }
            else if (recVer == 0x0F && depth < 32) // 容器：递归进子记录
            {
                ScanPptRecords(b, dataOff, dataOff + (int)recLen, sb, maxChars, depth + 1);
            }

            i = dataOff + (int)recLen;
        }
    }

    // ════════════════════════════════════════════════════════════
    // RTF / HTML 提取
    // ════════════════════════════════════════════════════════════

    /// <summary>从 RTF 文本剥离控制字/组，提取可读文本。</summary>
    public static string ExtractRtf(string rtf, int maxChars = DefaultMaxChars)
    {
        if (string.IsNullOrEmpty(rtf)) return "(空 RTF)";
        var sb = new StringBuilder();
        int i = 0, n = rtf.Length;
        int depth = 0;
        int skipDepth = -1; // 遇到 \* 时记录要跳过的组深度

        while (i < n && sb.Length < maxChars)
        {
            char c = rtf[i];
            if (c == '{') { depth++; i++; continue; }
            if (c == '}')
            {
                depth--;
                if (skipDepth >= 0 && depth < skipDepth) skipDepth = -1;
                i++;
                continue;
            }
            if (c == '\\')
            {
                i++;
                if (i >= n) break;
                char nc = rtf[i];
                if (nc == '\\' || nc == '{' || nc == '}') { if (skipDepth < 0) sb.Append(nc); i++; continue; }
                if (nc == '\'')
                {
                    if (i + 2 < n && IsHex(rtf[i + 1]) && IsHex(rtf[i + 2]))
                    {
                        int v = HexVal(rtf[i + 1]) * 16 + HexVal(rtf[i + 2]);
                        if (skipDepth < 0) sb.Append((char)v);
                        i += 3;
                    }
                    else i++;
                    continue;
                }
                if (nc == '*') { skipDepth = depth; i++; continue; }

                if (IsLetter(nc))
                {
                    int start = i;
                    while (i < n && IsLetter(rtf[i])) i++;
                    string word = rtf.Substring(start, i - start);
                    bool neg = false;
                    if (i < n && rtf[i] == '-') { neg = true; i++; }
                    bool hasParam = false;
                    int param = 0;
                    while (i < n && IsDigit(rtf[i])) { hasParam = true; param = param * 10 + (rtf[i] - '0'); i++; }
                    if (neg) param = -param;

                    if (skipDepth < 0)
                    {
                        if (word == "u" && hasParam)
                        {
                            int cp = param < 0 ? param + 65536 : param;
                            sb.Append((char)cp);
                            if (i < n && rtf[i] == '?') i++;
                            if (i < n && rtf[i] == ' ') i++;
                        }
                        else if (word == "par" || word == "line" || word == "row") sb.Append('\n');
                        else if (word == "tab" || word == "cell") sb.Append('\t');
                        else if (word == "emdash") sb.Append('—');
                        else if (word == "endash") sb.Append('–');
                        else if (word == "lquote") sb.Append('‘');
                        else if (word == "rquote") sb.Append('’');
                        else if (word == "ldblquote") sb.Append('“');
                        else if (word == "rdblquote") sb.Append('”');
                        else if (word == "bullet") sb.Append('•');
                    }
                    if (hasParam && i < n && rtf[i] == ' ') i++; // 吞掉参数后的分隔空格
                    continue;
                }

                // 控制符号（非字母）
                if (skipDepth < 0)
                {
                    if (nc == '~') sb.Append(' ');
                    else if (nc == '_') sb.Append('‑');
                }
                i++;
                continue;
            }

            if (skipDepth < 0) sb.Append(c);
            i++;
        }

        var result = sb.ToString().Trim();
        return result.Length > 0 ? result : "(RTF 无文本内容)";
    }

    /// <summary>剥离 HTML 标签，提取纯文本。</summary>
    public static string StripHtml(string html, int maxChars = DefaultMaxChars)
    {
        if (string.IsNullOrEmpty(html)) return "(空 HTML)";
        var t = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"<[^>]+>", " ");
        t = System.Net.WebUtility.HtmlDecode(t);
        t = Regex.Replace(t, @"[ \t]+", " ");
        t = Regex.Replace(t, @"(\r?\n[ \t]*){3,}", "\n\n");
        return Truncate(t.Trim(), maxChars);
    }

    // ════════════════════════════════════════════════════════════
    // 通用辅助
    // ════════════════════════════════════════════════════════════

    /// <summary>UTF-16LE 文本串扫描（损坏/旧格式的兜底提取）。</summary>
    private static string ExtractUtf16Runs(byte[] data, int maxChars)
    {
        var sb = new StringBuilder();
        int i = 0;
        while (i + 1 < data.Length && sb.Length < maxChars)
        {
            ushort u = Bin.U16(data, i);
            if (u >= 0x20 && u != 0xFFFF && u < 0xFFFE)
            {
                var run = new StringBuilder();
                while (i + 1 < data.Length)
                {
                    ushort c = Bin.U16(data, i);
                    if (c < 0x20 || c >= 0xFFFE) break;
                    if (c == 0x0D || c == 0x0B || c == 0x0C) { run.Append('\n'); i += 2; continue; }
                    if (c == 0x09) { run.Append('\t'); i += 2; continue; }
                    run.Append((char)c);
                    i += 2;
                }
                if (run.Length >= 2) { sb.AppendLine(run.ToString()); }
            }
            else i += 2;
        }
        var result = sb.ToString().Trim();
        return result.Length > 0 ? result : "(未找到文本)";
    }

    /// <summary>UTF-16 文本 atom → 追加（规范控制符）。</summary>
    private static void AppendUtf16Run(StringBuilder sb, byte[] b, int off, int len, int maxChars)
    {
        var run = new StringBuilder();
        for (int k = 0; k + 1 < len && off + k + 1 < b.Length; k += 2)
        {
            ushort u = Bin.U16(b, off + k);
            if (u == 0) continue;
            if (u == 0x0B || u == 0x0D) { run.Append('\n'); continue; }
            if (u == 0x0E) continue; // PPT 段内换行/句柄
            run.Append((char)u);
        }
        if (run.Length > 0 && sb.Length < maxChars)
            sb.AppendLine(run.ToString());
    }

    private static byte[] Slice(byte[] b, int off, int len)
    {
        if (off < 0) off = 0;
        if (off >= b.Length) return Array.Empty<byte>();
        len = Math.Min(len, b.Length - off);
        var r = new byte[len];
        Array.Copy(b, off, r, 0, len);
        return r;
    }

    private static string DecodeAscii(byte[] data) => Encoding.ASCII.GetString(data);

    private static string DecodeText(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            return Encoding.UTF8.GetString(data, 3, data.Length - 3);
        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
            return Encoding.Unicode.GetString(data, 2, data.Length - 2);
        if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
        try
        {
            var s = Encoding.UTF8.GetString(data);
            if (!s.Contains('�')) return s;
        }
        catch { }
        return Encoding.Latin1.GetString(data);
    }

    private static string Truncate(string s, int maxChars)
        => s.Length > maxChars ? s[..maxChars] + $"\n...(截断于 {maxChars:N0} 字符)" : s;

    /// <summary>cp1252 单字节 → Unicode（0x80–0x9F 特殊映射，其余 Latin-1）。</summary>
    private static char Cp1252(byte b)
    {
        if (b < 0x80) return (char)b;
        if (b < 0xA0) return Cp1252High[b - 0x80];
        return (char)b;
    }

    private static readonly char[] Cp1252High =
    {
        '€', '?', '‚', 'ƒ', '„', '…', '†', '‡',
        'ˆ', '‰', 'Š', '‹', 'Œ', '?', 'Ž', '?',
        '?', '‘', '’', '“', '”', '•', '–', '—',
        '˜', '™', 'š', '›', 'œ', '?', 'ž', 'Ÿ'
    };

    private static bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    private static bool IsDigit(char c) => c >= '0' && c <= '9';
    private static bool IsHex(char c) => IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    private static int HexVal(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'a' && c <= 'f') return c - 'a' + 10;
        return c - 'A' + 10;
    }
}

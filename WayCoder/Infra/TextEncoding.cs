using System.Text;

namespace WayCoder;

/// <summary>
/// 文本编码自动识别 —— 打开文件时按「BOM → 严格 UTF-8 → GB18030」顺序探测，
/// 正确解码中文旧编码文件（GBK/GB2312/GB18030），供 TUI / Web / GUI / 移动端编辑器
/// 与 read_file 工具统一复用，避免三端各自用 Encoding.UTF8 把 GBK 文件读成乱码。
///
/// GB18030（代码页 54936）依赖 <see cref="CodePagesEncodingProvider"/>：其编码表是
/// 编译期生成的静态表（非反射），NativeAOT 安全；惰性注册 + 失败回退宽松 UTF-8，
/// 保证任何文件都能打开、不崩。
/// </summary>
public static class TextEncoding
{
    /// <summary>检测结果：解码后的文本 + 编码显示名 + 写回时用的 <see cref="Encoding"/>。</summary>
    public readonly record struct Detected(string Text, string EncodingName, Encoding Encoding);

    private static readonly byte[] Utf8Bom = { 0xEF, 0xBB, 0xBF };
    private static readonly byte[] Utf16LeBom = { 0xFF, 0xFE };
    private static readonly byte[] Utf16BeBom = { 0xFE, 0xFF };

    private static Encoding? _gb18030;

    /// <summary>GB18030 编码（GBK/GB2312 超集），惰性注册 CodePages provider。</summary>
    public static Encoding GB18030
    {
        get
        {
            if (_gb18030 != null) return _gb18030;
            try { _gb18030 = Encoding.GetEncoding(54936); }
            catch
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                _gb18030 = Encoding.GetEncoding(54936);
            }
            return _gb18030;
        }
    }

    /// <summary>从字节流识别编码并解码为文本。</summary>
    public static Detected Detect(byte[] bytes)
    {
        // 1) UTF-8 BOM（EF BB BF）
        if (bytes.Length >= 3 && bytes[0] == Utf8Bom[0] && bytes[1] == Utf8Bom[1] && bytes[2] == Utf8Bom[2])
            return new Detected(new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3), "UTF-8 BOM", new UTF8Encoding(true));

        // 2) UTF-16 LE / BE BOM
        if (bytes.Length >= 2 && bytes[0] == Utf16LeBom[0] && bytes[1] == Utf16LeBom[1])
            return new Detected(Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), "UTF-16 LE", Encoding.Unicode);
        if (bytes.Length >= 2 && bytes[0] == Utf16BeBom[0] && bytes[1] == Utf16BeBom[1])
            return new Detected(Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), "UTF-16 BE", Encoding.BigEndianUnicode);

        // 3) 无 BOM：严格 UTF-8（非法字节序列抛异常）→ 命中即 UTF-8
        var strict = new UTF8Encoding(false, throwOnInvalidBytes: true);
        try { return new Detected(strict.GetString(bytes), "UTF-8", new UTF8Encoding(false)); }
        catch (DecoderFallbackException) { }

        // 4) 非法 UTF-8 → GB18030（GBK/GB2312 超集）
        try { return new Detected(GB18030.GetString(bytes), "GB18030", GB18030); }
        catch
        {
            // 极端：GB18030 也不可用 → 宽松 UTF-8（非法字节替换 �），保证能打开不崩
            return new Detected(Encoding.UTF8.GetString(bytes), "UTF-8", new UTF8Encoding(false));
        }
    }

    /// <summary>读文件并自动识别编码（文件不存在抛 FileNotFoundException，由调用方处理）。</summary>
    public static Detected ReadFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return Detect(bytes);
    }

    /// <summary>按指定编码写文件（编码自带 BOM 则写 BOM；UTF-8/GB18030 无 BOM 则无 BOM）。</summary>
    public static void WriteFile(string path, string content, Encoding encoding)
        => File.WriteAllText(path, content, encoding);

    /// <summary>编码显示名 → Encoding（Web 编辑器保存时按前端回传的编码名写回；未知名回退 UTF-8 无 BOM）。
    /// 与 <see cref="ResolveEncoding"/> 同源，保留为兼容别名。</summary>
    public static Encoding GetByName(string? name) => ResolveEncoding(name);

    /// <summary>
    /// 解析编码名/别名/code page 数字 → Encoding。默认 UTF-8（无 BOM）。
    /// 覆盖市面绝大多数编码：Unicode 家族（UTF-8/16/32）+ 简体/繁体中文 + 日文 + 韩文 +
    /// 西里尔 + ISO-8859 系列 + Windows 代码页 + DOS 代码页。未知名称尽力让 .NET 自解析，
    /// 仍失败则回退 UTF-8（保证任何输入都能转换，不抛异常）。
    /// </summary>
    public static Encoding ResolveEncoding(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return new UTF8Encoding(false);
        var n = name.Trim();

        // 纯数字 = 代码页（如 936=GBK、950=Big5、54936=GB18030）
        if (int.TryParse(n, out var codePageNum))
            return GetCodePage(codePageNum);

        switch (n.ToLowerInvariant())
        {
            case "utf-8": case "utf8": return new UTF8Encoding(false);
            case "utf-8 bom": case "utf-8-bom": case "utf8bom": case "utf-8-sig": return new UTF8Encoding(true);
            case "utf-16": case "utf-16 le": case "utf-16le": case "utf16": case "utf16le": case "unicode": return Encoding.Unicode;
            case "utf-16 be": case "utf-16be": case "utf16be": case "big-endian-unicode": case "bigendianunicode": return Encoding.BigEndianUnicode;
            case "utf-32": case "utf32": case "utf-32 le": case "utf-32le": return Encoding.UTF32;
            case "utf-32 be": case "utf-32be": return new UTF32Encoding(true, true);
            case "ascii": case "us-ascii": case "usascii": return Encoding.ASCII;
            default:
                if (CodePageByAlias.TryGetValue(n, out var cp)) return GetCodePage(cp);
                EnsureCodePages();
                try { return Encoding.GetEncoding(n); } catch { return new UTF8Encoding(false); }
        }
    }

    /// <summary>按代码页取编码。GB18030 复用单例；其余走 CodePages（框架内置、编译期静态表，AOT 安全），失败回退 UTF-8。</summary>
    public static Encoding GetCodePage(int codePage)
    {
        if (codePage == 54936) return GB18030;
        EnsureCodePages();
        try { return Encoding.GetEncoding(codePage); } catch { return new UTF8Encoding(false); }
    }

    private static bool _codePagesReady;

    /// <summary>幂等注册 CodePages provider（编码表为编译期静态表，非反射，AOT 安全）。</summary>
    private static void EnsureCodePages()
    {
        if (_codePagesReady) return;
        try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
        _codePagesReady = true;
    }

    /// <summary>
    /// 编码别名表（友好名 → 代码页）。Unicode/ASCII 走内置 Encoding（见 ResolveEncoding 的 switch），
    /// 其余（中文/日文/韩文/西里尔/ISO-8859/Windows/DOS）映射到代码页数字，经 <see cref="GetCodePage"/> 取编码。
    /// </summary>
    private static readonly Dictionary<string, int> CodePageByAlias = new(StringComparer.OrdinalIgnoreCase)
    {
        // 简体中文
        ["gb2312"] = 20936, ["gb-2312"] = 20936, ["gb2312-80"] = 20936, ["euc-cn"] = 20936, ["cn-gb"] = 20936,
        ["gbk"] = 936, ["cp936"] = 936, ["ms936"] = 936,
        ["gb18030"] = 54936, ["gb-18030"] = 54936, ["cp54936"] = 54936,
        // 繁体中文
        ["big5"] = 950, ["big-5"] = 950, ["cp950"] = 950, ["big5-hkscs"] = 950,
        // 日文
        ["shift-jis"] = 932, ["shift_jis"] = 932, ["sjis"] = 932, ["cp932"] = 932, ["ms-kanji"] = 932, ["windows-31j"] = 932,
        ["euc-jp"] = 51932, ["eucjp"] = 51932,
        ["iso-2022-jp"] = 50220, ["jis"] = 50220, ["csiso2022jp"] = 50220,
        // 韩文
        ["euc-kr"] = 51949, ["euckr"] = 51949, ["ks_c_5601-1987"] = 51949, ["ksc5601"] = 51949,
        ["uhc"] = 949, ["cp949"] = 949, ["ms949"] = 949,
        ["iso-2022-kr"] = 50225,
        // 西里尔（俄文等）
        ["koi8-r"] = 20866, ["koi8r"] = 20866,
        // ISO 8859 系列（Latin-1 ~ Latin-10，覆盖西欧/中欧/北欧/南欧/东欧/波罗的海）
        ["iso-8859-1"] = 28591, ["iso8859-1"] = 28591, ["latin1"] = 28591, ["latin-1"] = 28591, ["cp819"] = 28591,
        ["iso-8859-2"] = 28592, ["iso8859-2"] = 28592, ["latin2"] = 28592, ["latin-2"] = 28592,
        ["iso-8859-3"] = 28593, ["iso8859-3"] = 28593, ["latin3"] = 28593,
        ["iso-8859-4"] = 28594, ["iso8859-4"] = 28594, ["latin4"] = 28594,
        ["iso-8859-5"] = 28595, ["iso8859-5"] = 28595,
        ["iso-8859-6"] = 28596, ["iso8859-6"] = 28596,
        ["iso-8859-7"] = 28597, ["iso8859-7"] = 28597,
        ["iso-8859-8"] = 28598, ["iso8859-8"] = 28598,
        ["iso-8859-9"] = 28599, ["iso8859-9"] = 28599, ["latin5"] = 28599,
        ["iso-8859-10"] = 28600, ["iso8859-10"] = 28600, ["latin6"] = 28600,
        ["iso-8859-11"] = 28601, ["iso8859-11"] = 28601,
        ["iso-8859-13"] = 28603, ["iso8859-13"] = 28603,
        ["iso-8859-14"] = 28604, ["iso8859-14"] = 28604, ["latin8"] = 28604,
        ["iso-8859-15"] = 28605, ["iso8859-15"] = 28605, ["latin9"] = 28605, ["latin-9"] = 28605,
        ["iso-8859-16"] = 28606, ["iso8859-16"] = 28606, ["latin10"] = 28606,
        // Windows 代码页
        ["windows-1250"] = 1250, ["cp1250"] = 1250, ["win1250"] = 1250,
        ["windows-1251"] = 1251, ["cp1251"] = 1251, ["win1251"] = 1251,
        ["windows-1252"] = 1252, ["cp1252"] = 1252, ["win1252"] = 1252,
        ["windows-1253"] = 1253, ["cp1253"] = 1253, ["win1253"] = 1253,
        ["windows-1254"] = 1254, ["cp1254"] = 1254, ["win1254"] = 1254,
        ["windows-1255"] = 1255, ["cp1255"] = 1255, ["win1255"] = 1255,
        ["windows-1256"] = 1256, ["cp1256"] = 1256, ["win1256"] = 1256,
        ["windows-1257"] = 1257, ["cp1257"] = 1257, ["win1257"] = 1257,
        ["windows-1258"] = 1258, ["cp1258"] = 1258, ["win1258"] = 1258,
        // DOS 代码页
        ["ibm437"] = 437, ["cp437"] = 437, ["oem-us"] = 437,
        ["ibm850"] = 850, ["cp850"] = 850,
    };

    /// <summary>用指定编码解码字节流，头部若为 UTF-8/16/32 BOM 则跳过（避免把 BOM 解码成 U+FEFF 字符）。
    /// 独立检测 BOM、不依赖编码实例是否声明 BOM（UTF8Encoding(false) 也照常跳过 UTF-8 BOM）。</summary>
    public static string Decode(byte[] bytes, Encoding encoding)
    {
        int start = 0;
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF) start = 4;      // UTF-32 BE
        else if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00) start = 4; // UTF-32 LE
        else if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) start = 3;                     // UTF-8
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) start = 2;                                          // UTF-16 BE
        else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) start = 2;                                          // UTF-16 LE
        return encoding.GetString(bytes, start, bytes.Length - start);
    }
}

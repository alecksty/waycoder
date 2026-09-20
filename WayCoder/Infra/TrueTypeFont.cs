using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 手搓 TrueType 字体解析 + 字形光栅化（含抗锯齿）。
/// 仅支持 glyf 轮廓的 TrueType（非 CFF/OTTO）；复合字形（numberOfContours==-1）返回空轮廓（跳过）。
/// 零反射、零依赖、AOT 安全、跨平台。配合 Canvas.BlendPixel 实现字形边缘抗锯齿。
/// </summary>
public sealed class TrueTypeFont
{
    readonly byte[] _data;
    readonly Dictionary<string, (int Offset, int Length)> _tables;
    readonly int _unitsPerEm;
    readonly int _numGlyphs;
    readonly int _indexToLocFormat;
    readonly int _ascent;
    readonly int _numberOfHMetrics;
    readonly Func<int, int> _cmap;
    readonly int[] _advance;
    readonly int[] _lsb;

    // 族名 -> 字体（含 null 表示「解析失败/未找到」，避免反复扫描磁盘）
    static readonly Dictionary<string, TrueTypeFont?> _cache = new(StringComparer.OrdinalIgnoreCase);
    static List<FontEntry>? _fontList;
    static readonly object _cacheLock = new();

    TrueTypeFont(byte[] data, Dictionary<string, (int, int)> tables, int unitsPerEm, int numGlyphs,
        int indexToLocFormat, int ascent, int numberOfHMetrics, Func<int, int> cmap, int[] advance, int[] lsb)
    {
        _data = data; _tables = tables; _unitsPerEm = unitsPerEm; _numGlyphs = numGlyphs;
        _indexToLocFormat = indexToLocFormat; _ascent = ascent; _numberOfHMetrics = numberOfHMetrics;
        _cmap = cmap; _advance = advance; _lsb = lsb;
    }

    public int UnitsPerEm => _unitsPerEm;
    public int NumGlyphs => _numGlyphs;
    public int Ascent => _ascent;

    /// <summary>
    /// CSS 通用族名 / 空族名 —— 它们**永远匹配不到任何文件名**（<see cref="FontEntry.Family"/> 取的是
    /// 文件名），所以对它们只能是"猜"。猜的时候有额外一条要求：**必须真有中文字形**。
    ///
    /// 为什么单列一类：`DrawFigure.FontFamily` 的默认值就是 <c>"sans-serif"</c>，也就是说
    /// **所有没写族名的文字都走这条路**（`draw` 工具、VML 的 <c>ui_text</c> 全在内）。
    /// </summary>
    static bool IsGenericFamily(string key) => key.Length == 0
        || key.Equals("sans-serif", StringComparison.OrdinalIgnoreCase)
        || key.Equals("sans serif", StringComparison.OrdinalIgnoreCase)
        || key.Equals("sansserif", StringComparison.OrdinalIgnoreCase)
        || key.Equals("serif", StringComparison.OrdinalIgnoreCase)
        || key.Equals("monospace", StringComparison.OrdinalIgnoreCase)
        || key.Equals("system-ui", StringComparison.OrdinalIgnoreCase)
        || key.Equals("default", StringComparison.OrdinalIgnoreCase);

    /// <summary>按族名解析并加载系统字体；空族名用首选默认字体。找不到/失败返回 null。</summary>
    public static TrueTypeFont? Resolve(string? family)
    {
        var raw = string.IsNullOrWhiteSpace(family) ? "" : family.Trim();
        // **"sans-serif" 这类通用名当空处理** —— 否则 Pick 会去文件名里找 "sansserif" 必然落空，
        // 白白跳过首选表，直接掉进"随便挑一个能加载的"。
        var generic = IsGenericFamily(raw);
        var key = generic ? "" : raw;
        // 静态缓存非线程安全：多槽位并行（F1-F10 各跑一个 Agent）同时触发 DrawTool 文本渲染时
        // 并发读改写 _cache/_fontList 会破坏 Dictionary 内部状态。加锁串行化解析。
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(key, out var cached)) return cached;

            TrueTypeFont? result = null;
            // 候选里第一个「能加载但画不出汉字」的 —— 全都不行时拿它兜底（有字总比没有强）。
            TrueTypeFont? fallback = null;
            try
            {
                _fontList ??= FontFinder.Find();
                var entry = Pick(_fontList, key);

                // 首选字体**可能加载不了**（典型：Android 的中日韩字体是 CFF(OTF) 轮廓，
                // 而本解析器只支持 glyf）—— 这时不能就此罢休，要继续往后找能用的。
                // 实测踩过：挑了 `NotoSansCJK-Regular.ttc` → `Load` 返回 null → 整页文字
                // 退化成豆腐块；其实列表里还有能用的 glyf 中文字体（随包的 Sarasa）。
                foreach (var cand in Candidates(_fontList, key, entry))
                {
                    var t = Load(File.ReadAllBytes(cand.Path));
                    if (t == null) continue;

                    // **「能加载」不等于「有中文字形」**：实测某台 Windows 上
                    // `HYZhongHeiTi-197`（一个只覆盖少量字形的试用水印字体）排在所有真正的中文字体
                    // **之前**，能解析、能加载，但 `中` 落回 .notdef ⇒ 界面上每一个汉字都是一个空心方框。
                    // 猜字体时（族名是空的或 `sans-serif` 这类通用名）就该要求它真能画出汉字，
                    // 否则继续往后找；全都画不出才退回第一个能加载的。
                    if (generic && t.GlyphIndex('中') == 0) { fallback ??= t; continue; }

                    result = t;
                    break;
                }

                result ??= fallback;
            }
            catch { result = null; }
            _cache[key] = result;
            return result;
        }
    }

    /// <summary>
    /// 文件名里带这些词的，认为**带中文字形** —— 没指定族名时的兜底要用它。
    ///
    /// 为什么需要这一层：`FontEntry.Family` 取的是**文件名去扩展名**（不是字体内部的家族名），
    /// 所以 Android 的 `NotoSansCJK-Regular.ttc` 归一化后是 `notosanscjkregular`，
    /// 与首选表里的 `notosanscjksc` **对不上**；对不上就退化成"取第一个"，
    /// 而 `/system/fonts` 有两百多个文件、排在前面的多是纯拉丁字体 ⇒ 中文依旧渲染成豆腐块。
    /// </summary>
    static readonly string[] CjkHints =
    {
        "cjk", "notosanssc", "notosanstc", "droidsansfallback",
        "pingfang", "yahei", "simhei", "simsun", "heiti", "songti", "wqy", "wenquanyi", "sarasa",
    };

    static bool LooksCjk(string family)
    {
        var n = FontFinder.Normalize(family);
        foreach (var h in CjkHints) if (n.Contains(h, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>
    /// 候选字体序列（按优先级）：首选 → 其余带中文字形的 → 剩下全部。
    /// 调用方逐个尝试加载，**取第一个真能解析出来的** —— 单看"名字像不像"是不够的，
    /// 解析器只支持 glyf 轮廓，而系统里的中日韩字体往往是 CFF（见 <see cref="Resolve"/> 注释）。
    /// </summary>
    static IEnumerable<FontEntry> Candidates(List<FontEntry> fonts, string family, FontEntry? primary)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (primary != null && seen.Add(primary.Path)) yield return primary;
        foreach (var e in fonts) if (LooksCjk(e.Family) && seen.Add(e.Path)) yield return e;
        foreach (var e in fonts) if (seen.Add(e.Path)) yield return e;
    }

    static FontEntry? Pick(List<FontEntry> fonts, string family)
    {
        if (fonts.Count == 0) return null;
        if (family.Length == 0)
        {
            foreach (var pref in FontFinder.PreferredFamilies)
                foreach (var e in fonts)
                    if (FontFinder.Normalize(e.Family) == FontFinder.Normalize(pref)) return e;

            // 首选表没命中 → **先挑带中文字形的**，再退化到"任意一个"。
            // 不这么做的话，一个中文字符都画不出来（而界面文案几乎全是中文）。
            foreach (var e in fonts) if (LooksCjk(e.Family)) return e;

            return fonts[0];
        }
        var target = FontFinder.Normalize(family);
        foreach (var e in fonts) if (FontFinder.Normalize(e.Family) == target) return e;
        foreach (var e in fonts) if (FontFinder.Normalize(e.Family).Contains(target)) return e;
        return null;
    }

    public static TrueTypeFont? Load(string path)
    {
        try { return Load(File.ReadAllBytes(path)); }
        catch { return null; }
    }

    public static TrueTypeFont? Load(byte[] data)
    {
        try
        {
            if (data.Length < 12) return null;
            uint version = BE32(data, 0);

            // **`.ttc` 字体集合**：Android 的中日韩字体基本都是它（如 NotoSansCJK-Regular.ttc），
            // 跳过它就等于在 Android 上没有可用的中文字形（文本渲染成豆腐块）。
            //
            // ⚠ **不能"切掉集合头再递归"**：TTC 里每张表的 `offset` 是**相对整个文件**的
            // （OpenType 规范如此），把数组切一刀之后这些偏移全部失效、指到切片外面去。
            // 正确做法是**只在原数组上换目录起点**，偏移保持原样。
            //
            // 取集合里的**第一个** —— 对"画布上渲染文字"这个用途足够（第一个通常是 Regular）。
            int dirOff = 0;
            if (version == 0x74746366) // 'ttcf'
            {
                if (data.Length < 16) return null;
                dirOff = (int)BE32(data, 12);
                if (dirOff <= 0 || dirOff + 12 > data.Length) return null;
                version = BE32(data, dirOff);
            }

            if (version == 0x4F54544F) return null; // 'OTTO' CFF 轮廓，不支持
            if (version != 0x00010000) return null; // 仅 TrueType
            int numTables = BE16(data, dirOff + 4);
            var tables = new Dictionary<string, (int Offset, int Length)>();
            for (int i = 0; i < numTables; i++)
            {
                int off = dirOff + 12 + i * 16;
                if (off + 16 > data.Length) break;
                string tag = Encoding.ASCII.GetString(data, off, 4);
                int tOff = (int)BE32(data, off + 8);
                int tLen = (int)BE32(data, off + 12);
                tables[tag] = (tOff, tLen);
            }
            if (!tables.TryGetValue("head", out var head) || !tables.TryGetValue("maxp", out var maxp)) return null;
            int unitsPerEm = BE16(data, head.Offset + 18);
            if (unitsPerEm <= 0) return null;
            int indexToLocFormat = (short)BE16(data, head.Offset + 50);
            int numGlyphs = BE16(data, maxp.Offset + 4);
            if (numGlyphs <= 0) return null;

            int ascent = unitsPerEm * 8 / 10;
            int numberOfHMetrics = numGlyphs;
            if (tables.TryGetValue("hhea", out var hhea) && hhea.Offset + 36 <= data.Length)
            {
                int a = (short)BE16(data, hhea.Offset + 4);
                if (a > 0) ascent = a;
                int n = BE16(data, hhea.Offset + 34);
                if (n > 0 && n <= numGlyphs) numberOfHMetrics = n;
            }

            Func<int, int> cmap = (_) => 0;
            if (tables.TryGetValue("cmap", out var cmapT)) cmap = ParseCmap(data, cmapT) ?? ((_) => 0);

            var advance = new int[numGlyphs];
            var lsb = new int[numGlyphs];
            if (tables.TryGetValue("hmtx", out var hmtx))
            {
                for (int g = 0; g < numGlyphs; g++)
                {
                    int idx = g < numberOfHMetrics ? g : numberOfHMetrics - 1;
                    int baseOff = hmtx.Offset + idx * 4;
                    advance[g] = BE16(data, baseOff);
                    lsb[g] = (short)BE16(data, baseOff + 2);
                }
            }
            else for (int g = 0; g < numGlyphs; g++) advance[g] = unitsPerEm;

            return new TrueTypeFont(data, tables, unitsPerEm, numGlyphs, indexToLocFormat, ascent, numberOfHMetrics, cmap, advance, lsb);
        }
        catch { return null; }
    }

    public int GlyphIndex(int codePoint) => _cmap(codePoint);
    public int AdvanceWidth(int glyph) => glyph >= 0 && glyph < _numGlyphs ? _advance[glyph] : _unitsPerEm;

    /// <summary>测量文本渲染宽度（像素）。</summary>
    public double Measure(string text, double size)
    {
        double scale = size / _unitsPerEm;
        double w = 0;
        foreach (var rune in text.EnumerateRunes()) w += AdvanceWidth(GlyphIndex(rune.Value)) * scale;
        return w;
    }

    /// <summary>
    /// 渲染一行文本到画布。y 为文本顶线（与位图字体一致），x 受 anchor 影响（start/middle/end）。
    /// 字形边缘按 4×4 超采样抗锯齿，bold 双次偏移描粗，italic 简单斜切。
    /// </summary>
    /// <param name="sample">
    /// **逐像素取色**（可选）。给 null 就是原来的纯色填充。
    /// 文字渐变走这里：字形填充是扫描线，落笔点只有一处（`BlendPixel`），
    /// 所以换成"按坐标问颜色"就够了，不必给光栅器再写一套。
    /// 参数是**画布坐标**（与 `c` 同一坐标系，含超采样那层缩放）——
    /// 逆变换回局部坐标由调用方在委托里做（只有它知道几何的变换）。
    /// </param>
    public void Render(Canvas c, string text, double x, double yTop, double size, uint color, string anchor,
        bool bold, bool italic, Func<double, double, uint>? sample = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        double width = Measure(text, size);
        double penX = x;
        if (anchor == "middle") penX = x - width / 2;
        else if (anchor == "end") penX = x - width;
        double baseline = yTop + _ascent * (size / _unitsPerEm);

        if (bold) DrawString(c, text, penX + size * 0.02, baseline, size, color, italic, sample);
        DrawString(c, text, penX, baseline, size, color, italic, sample);
    }

    void DrawString(Canvas c, string text, double penX, double baseline, double size, uint color,
        bool italic, Func<double, double, uint>? sample = null)
    {
        double scale = size / _unitsPerEm;
        double slant = italic ? 0.25 : 0.0;
        double curX = penX;
        foreach (var rune in text.EnumerateRunes())
        {
            int g = GlyphIndex(rune.Value);
            double advance = AdvanceWidth(g) * scale;
            var contours = GetOutline(g);
            if (contours.Count > 0)
                FillGlyphAa(c, contours, curX, baseline, scale, slant, color, sample);
            curX += advance;
        }
    }

    /// <summary>解析 cmap 子表，返回 codepoint→glyph 映射。</summary>
    static Func<int, int>? ParseCmap(byte[] data, (int Offset, int Length) cmapT)
    {
        int baseOff = cmapT.Offset;
        if (baseOff + 4 > data.Length) return null;
        int numSub = BE16(data, baseOff + 2);
        int bestOff = -1, bestPriority = int.MaxValue;
        for (int i = 0; i < numSub; i++)
        {
            int rec = baseOff + 4 + i * 8;
            if (rec + 8 > data.Length) break;
            int platform = BE16(data, rec);
            int encoding = BE16(data, rec + 2);
            int subOff = baseOff + (int)BE32(data, rec + 4);
            int priority = platform == 3 && encoding == 1 ? 0
                : platform == 3 && encoding == 10 ? 1
                : platform == 0 && (encoding == 3 || encoding == 4) ? 2 : -1;
            if (priority < 0 || priority >= bestPriority) continue;
            bestPriority = priority; bestOff = subOff;
        }
        if (bestOff < 0 || bestOff + 2 > data.Length) return null;
        int format = BE16(data, bestOff);
        if (format == 4) return ParseCmap4(data, bestOff);
        if (format == 12) return ParseCmap12(data, bestOff);
        return null;
    }

    static Func<int, int>? ParseCmap4(byte[] data, int off)
    {
        try
        {
            int segCount = BE16(data, off + 6) / 2;
            if (segCount <= 0) return null;
            int endOff = off + 14;
            int startOff = endOff + segCount * 2 + 2;
            int deltaOff = startOff + segCount * 2;
            int rangeOff = deltaOff + segCount * 2;
            int glyphArrayOff = rangeOff + segCount * 2;
            var endCode = new int[segCount];
            var startCode = new int[segCount];
            var idDelta = new int[segCount];
            var idRange = new int[segCount];
            for (int i = 0; i < segCount; i++)
            {
                endCode[i] = BE16(data, endOff + i * 2);
                startCode[i] = BE16(data, startOff + i * 2);
                idDelta[i] = (short)BE16(data, deltaOff + i * 2);
                idRange[i] = BE16(data, rangeOff + i * 2);
            }
            return (cp) =>
            {
                if (cp > 0xFFFF) return 0;
                for (int i = 0; i < segCount; i++)
                {
                    if (cp < startCode[i] || cp > endCode[i]) continue;
                    if (idRange[i] == 0) return (cp + idDelta[i]) & 0xFFFF;
                    int idx = idRange[i] / 2 + (cp - startCode[i]) - (segCount - i);
                    int g = BE16(data, glyphArrayOff + idx * 2);
                    return g == 0 ? 0 : (g + idDelta[i]) & 0xFFFF;
                }
                return 0;
            };
        }
        catch { return null; }
    }

    static Func<int, int>? ParseCmap12(byte[] data, int off)
    {
        try
        {
            int nGroups = (int)BE32(data, off + 12);
            return (cp) =>
            {
                int lo = 0, hi = nGroups - 1;
                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    int g = off + 16 + mid * 12;
                    int start = (int)BE32(data, g);
                    int end = (int)BE32(data, g + 4);
                    int startGlyph = (int)BE32(data, g + 8);
                    if (cp < start) hi = mid - 1;
                    else if (cp > end) lo = mid + 1;
                    else return startGlyph + (cp - start);
                }
                return 0;
            };
        }
        catch { return null; }
    }

    (int, int) GlyphRange(int glyph)
    {
        if (glyph < 0 || glyph >= _numGlyphs) return (0, 0);
        if (!_tables.TryGetValue("loca", out var loca) || !_tables.TryGetValue("glyf", out var glyf)) return (0, 0);
        if (_indexToLocFormat == 0)
        {
            int a = BE16(_data, loca.Offset + glyph * 2) * 2;
            int b = BE16(_data, loca.Offset + glyph * 2 + 2) * 2;
            return (glyf.Offset + a, glyf.Offset + b);
        }
        else
        {
            int a = (int)BE32(_data, loca.Offset + glyph * 4);
            int b = (int)BE32(_data, loca.Offset + glyph * 4 + 4);
            return (glyf.Offset + a, glyf.Offset + b);
        }
    }

    /// <summary>提取字形轮廓（每 contour 为已扁平化的多边形点列表 x,y 交替）。复合字形返回空。</summary>
    public List<double[]> GetOutline(int glyph)
    {
        var result = new List<double[]>();
        var (off, next) = GlyphRange(glyph);
        if (off >= next || off + 10 > _data.Length) return result;
        int numContours = (short)BE16(_data, off);
        if (numContours <= 0) return result; // 空字形或复合字形（不支持）
        int p = off + 10;
        var endPts = new int[numContours];
        for (int i = 0; i < numContours; i++) endPts[i] = BE16(_data, p + i * 2);
        p += numContours * 2;
        int instrLen = BE16(_data, p); p += 2 + instrLen;
        int numPoints = endPts[numContours - 1] + 1;
        var flags = new byte[numPoints];
        for (int i = 0; i < numPoints;)
        {
            if (p >= _data.Length) return result;
            byte f = _data[p++]; flags[i++] = f;
            if ((f & 0x08) != 0)
            {
                if (p >= _data.Length) return result;
                int rep = _data[p++];
                for (int r = 0; r < rep && i < numPoints; r++) flags[i++] = f;
            }
        }
        var xs = new double[numPoints];
        var ys = new double[numPoints];
        double x = 0;
        for (int i = 0; i < numPoints; i++)
        {
            byte f = flags[i]; double dx;
            if ((f & 0x02) != 0) { if (p >= _data.Length) return result; dx = _data[p++]; if ((f & 0x10) == 0) dx = -dx; }
            else if ((f & 0x10) != 0) dx = 0;
            else { dx = (short)BE16(_data, p); p += 2; }
            x += dx; xs[i] = x;
        }
        double y = 0;
        for (int i = 0; i < numPoints; i++)
        {
            byte f = flags[i]; double dy;
            if ((f & 0x04) != 0) { if (p >= _data.Length) return result; dy = _data[p++]; if ((f & 0x20) == 0) dy = -dy; }
            else if ((f & 0x20) != 0) dy = 0;
            else { dy = (short)BE16(_data, p); p += 2; }
            y += dy; ys[i] = y;
        }
        int start = 0;
        for (int c = 0; c < numContours; c++)
        {
            int end = endPts[c];
            result.Add(FlattenContour(xs, ys, flags, start, end));
            start = end + 1;
        }
        return result;
    }

    static double[] FlattenContour(double[] xs, double[] ys, byte[] flags, int start, int end)
    {
        int count = end - start + 1;
        var px = new List<double>(count * 2);
        var py = new List<double>(count * 2);
        var on = new List<bool>(count * 2);
        for (int i = 0; i < count; i++)
        {
            int pi = start + i;
            px.Add(xs[pi]); py.Add(ys[pi]); on.Add((flags[pi] & 1) != 0);
        }
        // 连续两个 off-curve 之间插入隐含 on-curve 中点
        var px2 = new List<double>(); var py2 = new List<double>(); var on2 = new List<bool>();
        for (int i = 0; i < count; i++)
        {
            px2.Add(px[i]); py2.Add(py[i]); on2.Add(on[i]);
            int j = (i + 1) % count;
            if (!on[i] && !on[j])
            {
                px2.Add((px[i] + px[j]) / 2); py2.Add((py[i] + py[j]) / 2); on2.Add(true);
            }
        }
        int n2 = px2.Count;
        int firstOn = -1;
        for (int i = 0; i < n2; i++) if (on2[i]) { firstOn = i; break; }
        var poly = new List<double>();
        if (firstOn < 0)
        {
            for (int i = 0; i < n2; i++) { poly.Add(px2[i]); poly.Add(py2[i]); }
            return poly.ToArray();
        }
        int idx = firstOn, guard = 0;
        poly.Add(px2[firstOn]); poly.Add(py2[firstOn]);
        while (guard++ < n2 + 4)
        {
            int ni = (idx + 1) % n2;
            if (on2[idx] && on2[ni])
            {
                poly.Add(px2[ni]); poly.Add(py2[ni]);
                idx = ni;
            }
            else if (on2[idx] && !on2[ni])
            {
                int nni = (idx + 2) % n2;
                EmitQuadratic(poly, px2[idx], py2[idx], px2[ni], py2[ni], px2[nni], py2[nni]);
                poly.Add(px2[nni]); poly.Add(py2[nni]);
                idx = nni;
            }
            else idx = ni;
            if (idx == firstOn) break;
        }
        return poly.ToArray();
    }

    static void EmitQuadratic(List<double> poly, double p0x, double p0y, double cx, double cy, double p1x, double p1y)
    {
        const int steps = 8;
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            double a = (1 - t) * (1 - t), b = 2 * t * (1 - t), c2 = t * t;
            poly.Add(a * p0x + b * cx + c2 * p1x);
            poly.Add(a * p0y + b * cy + c2 * p1y);
        }
    }

    // —— 抗锯齿字形填充（4×4 超采样 + 非零环绕）——

    void FillGlyphAa(Canvas c, List<double[]> contours, double penX, double baseline, double scale,
        double slant, uint color, Func<double, double, uint>? sample = null)
    {
        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
        var world = new List<double[]>(contours.Count);
        foreach (var raw in contours)
        {
            int n = raw.Length / 2;
            var w = new double[n * 2];
            for (int i = 0; i < n; i++)
            {
                double fx = raw[i * 2], fy = raw[i * 2 + 1];
                double wx = penX + fx * scale + slant * (_ascent - fy) * scale;
                double wy = baseline - fy * scale;
                w[i * 2] = wx; w[i * 2 + 1] = wy;
                if (wx < minX) minX = wx;
                if (wx > maxX) maxX = wx;
                if (wy < minY) minY = wy;
                if (wy > maxY) maxY = wy;
            }
            world.Add(w);
        }
        if (world.Count == 0) return;
        // bbox 钳制到画布范围：损坏/畸形字体的控制点坐标可能极大或极负（如 1e9），
        // 不 clamp 的话双层循环会遍历天文数字像素（即使 BlendPixel 越界跳过，循环本身也 DoS）。
        int x0 = Math.Max(0, (int)Math.Floor(minX)), x1 = Math.Min(c.Width - 1, (int)Math.Ceiling(maxX));
        int y0 = Math.Max(0, (int)Math.Floor(minY)), y1 = Math.Min(c.Height - 1, (int)Math.Ceiling(maxY));
        int wpx = x1 - x0 + 1;
        if (wpx <= 0 || y1 < y0) return;

        // ── 扫描线填充（**与逐点版本逐像素一致**，只是把重复计算提出来）──
        //
        // 原来是"每个像素 × 4×4 采样 × 每个采样点把**所有边**跑一遍"：
        // 一个 51px 高的汉字 ≈ 2600 像素 × 16 × 150 条边 ≈ 620 万次内层运算。实测
        // **12 个汉字串要 869ms（不开超采样）/ 7.2s（3× 超采样）** —— 手机端每个绘图帧
        // 都要走这一遍，游戏直接卡死（`vmlhost --sim` 量出来的）。
        //
        // 关键观察：**同一条子扫描线上，所有采样点的 y 相同 ⇒ 与各边的相交情况相同**。
        // 所以每条子扫描线只需求一次交点，再用**后缀和 + 单调游标**回答该行上所有采样点。
        //
        // 判据与 `Winding`/`Inside` 完全等价（非零环绕）：对一条边，令 lo=min(y1,y2)、hi=max(y1,y2)，
        // 则它只在 `lo <= y < hi` 时有贡献，贡献方向 = 上边(y1<=y2) +1 / 下边 -1，
        // 且**只在采样点位于交点左侧（x < xCross）时**计入 —— 于是
        //   winding(x) = Σ_{xCross > x} dir
        // 排序后就是后缀和。
        var xa = new double[2048];
        var da = new int[2048];
        var suf = new int[2049];
        var hits = new int[wpx];
        const int SS = 4;

        for (int py = y0; py <= y1; py++)
        {
            Array.Clear(hits, 0, wpx);
            for (int sy = 0; sy < SS; sy++)
            {
                double qy = py + (sy + 0.5) / SS;
                int m = 0;
                foreach (var poly in world)
                {
                    int n = poly.Length / 2;
                    for (int i = 0; i < n; i++)
                    {
                        int j = (i + 1) % n;
                        double y1v = poly[i * 2 + 1], y2v = poly[j * 2 + 1];
                        double lo = y1v <= y2v ? y1v : y2v, hi = y1v <= y2v ? y2v : y1v;
                        if (qy < lo || qy >= hi) continue;
                        if (m == xa.Length) break;                 // 畸形字形兜底，不越界
                        double x1v = poly[i * 2], x2v = poly[j * 2];
                        xa[m] = x1v + (qy - y1v) / (y2v - y1v) * (x2v - x1v);
                        da[m] = y1v <= y2v ? 1 : -1;
                        m++;
                    }
                }
                if (m == 0) continue;
                Array.Sort(xa, da, 0, m);
                suf[m] = 0;
                for (int i = m - 1; i >= 0; i--) suf[i] = suf[i + 1] + da[i];

                int ptr = 0;
                for (int px = x0; px <= x1; px++)
                    for (int sx = 0; sx < SS; sx++)
                    {
                        double qx = px + (sx + 0.5) / SS;
                        while (ptr < m && xa[ptr] <= qx) ptr++;
                        if (suf[ptr] != 0) hits[px - x0]++;
                    }
            }
            for (int px = x0; px <= x1; px++)
                if (hits[px - x0] > 0)
                    c.BlendPixel(px, py, sample != null ? sample(px, py) : color,
                        (double)hits[px - x0] / (SS * SS));
        }
    }

    static bool Inside(List<double[]> world, double x, double y)
    {
        int wn = 0;
        foreach (var poly in world) wn += Winding(poly, x, y);
        return wn != 0;
    }

    static int Winding(double[] poly, double x, double y)
    {
        int wn = 0;
        int n = poly.Length / 2;
        for (int i = 0; i < n; i++)
        {
            double x1 = poly[i * 2], y1 = poly[i * 2 + 1];
            double x2 = poly[(i + 1) % n * 2], y2 = poly[(i + 1) % n * 2 + 1];
            if (y1 <= y) { if (y2 > y && IsLeft(x1, y1, x2, y2, x, y) > 0) wn++; }
            else { if (y2 <= y && IsLeft(x1, y1, x2, y2, x, y) < 0) wn--; }
        }
        return wn;
    }

    static double IsLeft(double x1, double y1, double x2, double y2, double x, double y)
        => (x2 - x1) * (y - y1) - (x - x1) * (y2 - y1);

    static int BE16(byte[] d, int off) => off >= 0 && off + 2 <= d.Length ? (d[off] << 8) | d[off + 1] : 0;
    static uint BE32(byte[] d, int off) => off >= 0 && off + 4 <= d.Length
        ? ((uint)d[off] << 24) | ((uint)d[off + 1] << 16) | ((uint)d[off + 2] << 8) | d[off + 3]
        : 0;
}

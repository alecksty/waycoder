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

    /// <summary>按族名解析并加载系统字体；空族名用首选默认字体。找不到/失败返回 null。</summary>
    public static TrueTypeFont? Resolve(string? family)
    {
        var key = string.IsNullOrWhiteSpace(family) ? "" : family.Trim();
        if (_cache.TryGetValue(key, out var cached)) return cached;

        TrueTypeFont? result = null;
        try
        {
            _fontList ??= FontFinder.Find();
            var entry = Pick(_fontList, key);
            if (entry != null) result = Load(File.ReadAllBytes(entry.Path));
        }
        catch { result = null; }
        _cache[key] = result;
        return result;
    }

    static FontEntry? Pick(List<FontEntry> fonts, string family)
    {
        if (fonts.Count == 0) return null;
        if (family.Length == 0)
        {
            foreach (var pref in FontFinder.PreferredFamilies)
                foreach (var e in fonts)
                    if (FontFinder.Normalize(e.Family) == FontFinder.Normalize(pref)) return e;
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
            if (version == 0x4F54544F) return null; // 'OTTO' CFF 轮廓，不支持
            if (version != 0x00010000) return null; // 仅 TrueType
            int numTables = BE16(data, 4);
            var tables = new Dictionary<string, (int Offset, int Length)>();
            for (int i = 0; i < numTables; i++)
            {
                int off = 12 + i * 16;
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
        foreach (char ch in text) w += AdvanceWidth(GlyphIndex((int)ch)) * scale;
        return w;
    }

    /// <summary>
    /// 渲染一行文本到画布。y 为文本顶线（与位图字体一致），x 受 anchor 影响（start/middle/end）。
    /// 字形边缘按 4×4 超采样抗锯齿，bold 双次偏移描粗，italic 简单斜切。
    /// </summary>
    public void Render(Canvas c, string text, double x, double yTop, double size, uint color, string anchor,
        bool bold, bool italic)
    {
        if (string.IsNullOrEmpty(text)) return;
        double width = Measure(text, size);
        double penX = x;
        if (anchor == "middle") penX = x - width / 2;
        else if (anchor == "end") penX = x - width;
        double baseline = yTop + _ascent * (size / _unitsPerEm);

        if (bold) DrawString(c, text, penX + size * 0.02, baseline, size, color, italic);
        DrawString(c, text, penX, baseline, size, color, italic);
    }

    void DrawString(Canvas c, string text, double penX, double baseline, double size, uint color, bool italic)
    {
        double scale = size / _unitsPerEm;
        double slant = italic ? 0.25 : 0.0;
        double curX = penX;
        foreach (char ch in text)
        {
            int g = GlyphIndex((int)ch);
            double advance = AdvanceWidth(g) * scale;
            var contours = GetOutline(g);
            if (contours.Count > 0)
                FillGlyphAa(c, contours, curX, baseline, scale, slant, color);
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

    void FillGlyphAa(Canvas c, List<double[]> contours, double penX, double baseline, double scale, double slant, uint color)
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
        int x0 = (int)Math.Floor(minX), x1 = (int)Math.Ceiling(maxX);
        int y0 = (int)Math.Floor(minY), y1 = (int)Math.Ceiling(maxY);
        const int SS = 4;
        for (int py = y0; py <= y1; py++)
            for (int px = x0; px <= x1; px++)
            {
                int hits = 0;
                for (int sy = 0; sy < SS; sy++)
                    for (int sx = 0; sx < SS; sx++)
                    {
                        double qx = px + (sx + 0.5) / SS;
                        double qy = py + (sy + 0.5) / SS;
                        if (Inside(world, qx, qy)) hits++;
                    }
                if (hits > 0) c.BlendPixel(px, py, color, (double)hits / (SS * SS));
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

    static int BE16(byte[] d, int off) => off + 2 <= d.Length ? (d[off] << 8) | d[off + 1] : 0;
    static uint BE32(byte[] d, int off) => off + 4 <= d.Length
        ? ((uint)d[off] << 24) | ((uint)d[off + 1] << 16) | ((uint)d[off + 2] << 8) | d[off + 3]
        : 0;
}

using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 语义绘图指令 —— flowchart（流程图）。用 Mermaid 风格的单行字符串描述「节点 + 连线」，
/// 自动分层布局并生成图元（矩形/圆角/菱形/圆形节点 + 箭头 + 文字标签），免去手动定位每个坐标。
///
/// 语法：<c>flowchart "A[开始]-->B{判断}-->C((结束))"</c>
///   节点形状：[标签] 矩形 · (标签) 圆角 · {标签} 菱形(判断) · ((标签)) 圆形 · 裸名 矩形
///   连线：--> 箭头 · -.-> 虚线箭头 · ==> 粗箭头 · --- 无箭头连线
///   连线标签：-->|标签|（标签绘制在连线中点上）
/// </summary>
internal static class FlowchartCommand
{
    private sealed class FlowNode
    {
        public string Id = "";
        public string Label = "";
        public string Shape = "rect"; // rect | round | diamond | circle
        public double X, Y, W, H;
    }

    private sealed class FlowEdge
    {
        public string From = "";
        public string To = "";
        public bool Dashed;
        public bool Thick;
        public bool HasHead = true;
        public string? Label;
    }

    private const double FontSize = 14;
    private const double PadX = 20;
    private const double PadY = 14;
    private const double ColGap = 72;
    private const double RowGap = 30;
    private const double Margin = 44;

    private static readonly uint NodeFill = 0xFFFFFFFF;
    private static readonly uint NodeStroke = 0xFF374151;
    private static readonly uint TextColor = 0xFF111827;
    private static readonly uint EdgeColor = 0xFF6B7280;

    /// <summary>解析 flowchart 指令，把生成的节点/连线/文字图元追加到 doc，并设置画布尺寸。</summary>
    public static void Build(DrawDocument doc, IReadOnlyList<DrawToken> args)
    {
        if (args.Count < 1) { doc.Error = "参数错误: flowchart 需流程字符串（如 \"A[开始]-->B[结束]\"）"; return; }

        var sb = new StringBuilder();
        foreach (var a in args) sb.Append(a.Value);
        var s = sb.ToString();
        if (string.IsNullOrWhiteSpace(s)) { doc.Error = "参数错误: flowchart 流程字符串为空"; return; }

        var nodes = new List<FlowNode>();
        var edges = new List<FlowEdge>();
        if (!ParseGraph(s, nodes, edges, out var err)) { doc.Error = err; return; }

        Layout(doc, nodes, edges);
        Emit(doc, nodes, edges);
    }

    // ── 图解析 ──

    static bool ParseGraph(string s, List<FlowNode> nodes, List<FlowEdge> edges, out string? err)
    {
        err = null;
        int i = 0, autoId = 0;
        if (!ParseNode(s, ref i, nodes, ref autoId)) { err = "flowchart 语法错误：缺少起始节点"; return false; }
        while (i < s.Length)
        {
            if (!ParseEdge(s, ref i, nodes, edges, ref autoId))
            {
                err = $"flowchart 语法错误：无法解析位置 {i} 附近的连线（支持 --> / -.-> / ==> / --- ）";
                return false;
            }
        }
        return true;
    }

    static bool ParseNode(string s, ref int i, List<FlowNode> nodes, ref int autoId)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        if (i >= s.Length) return false;

        int start = i;
        while (i < s.Length && IsIdChar(s[i])) i++;
        string id = i > start ? s[start..i] : "";

        string shape = "rect", label;
        if (i < s.Length && s[i] == '[') { i++; label = ReadUntil(s, ref i, ']'); i++; }
        else if (i < s.Length && s[i] == '(')
        {
            if (i + 1 < s.Length && s[i + 1] == '(')
            {
                i += 2; label = ReadUntil(s, ref i, ')'); i++;
                if (i < s.Length && s[i] == ')') i++;
                shape = "circle";
            }
            else { i++; label = ReadUntil(s, ref i, ')'); i++; shape = "round"; }
        }
        else if (i < s.Length && s[i] == '{') { i++; label = ReadUntil(s, ref i, '}'); i++; shape = "diamond"; }
        else label = id;

        if (id.Length == 0) id = "_n" + (autoId++);
        if (string.IsNullOrWhiteSpace(label)) label = id;
        nodes.Add(new FlowNode { Id = id, Label = label.Trim(), Shape = shape });
        return true;
    }

    static bool ParseEdge(string s, ref int i, List<FlowNode> nodes, List<FlowEdge> edges, ref int autoId)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        if (i >= s.Length) return false;

        bool dashed = false, thick = false, hasHead = true;
        if (Match(s, i, "-.->")) { dashed = true; i += 4; }
        else if (Match(s, i, "==>")) { thick = true; i += 3; }
        else if (Match(s, i, "-->")) { i += 3; }
        else if (Match(s, i, "->")) { i += 2; }
        else if (Match(s, i, "---")) { hasHead = false; i += 3; }
        else if (Match(s, i, "-.-")) { dashed = true; hasHead = false; i += 3; }
        else if (Match(s, i, "--")) { hasHead = false; i += 2; }
        else return false;

        string? label = null;
        if (i < s.Length && s[i] == '|') { i++; label = ReadUntil(s, ref i, '|'); i++; }

        if (!ParseNode(s, ref i, nodes, ref autoId)) return false;
        edges.Add(new FlowEdge
        {
            From = nodes[^2].Id,
            To = nodes[^1].Id,
            Dashed = dashed,
            Thick = thick,
            HasHead = hasHead,
            Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim(),
        });
        return true;
    }

    static string ReadUntil(string s, ref int i, char close)
    {
        // 读到闭合符为止，i 停在 close 处（调用方负责跳过闭合符本身）。
        int start = i;
        while (i < s.Length && s[i] != close) i++;
        return s[start..i];
    }

    static bool Match(string s, int i, string pat)
        => i + pat.Length <= s.Length && string.CompareOrdinal(s, i, pat, 0, pat.Length) == 0;

    static bool IsIdChar(char c)
        => !char.IsWhiteSpace(c) && c != '[' && c != ']' && c != '(' && c != ')' && c != '{' && c != '}'
        && c != '-' && c != '=' && c != '|' && c != '>' && c != '<' && c != '.' && c != '"';

    // ── 布局 ──

    static void Layout(DrawDocument doc, List<FlowNode> nodes, List<FlowEdge> edges)
    {
        // 最长路径分层（含环时迭代上限 = 节点数，保证收敛）。
        var level = new Dictionary<string, int>();
        foreach (var n in nodes) level[n.Id] = 0;
        for (int guard = 0; guard <= nodes.Count; guard++)
        {
            bool changed = false;
            foreach (var e in edges)
            {
                int want = level[e.From] + 1;
                if (level.TryGetValue(e.To, out var cur) && cur < want && want <= nodes.Count)
                {
                    level[e.To] = want;
                    changed = true;
                }
            }
            if (!changed) break;
        }

        // 按层分组，计算每列 x 与每行 y。
        var byLevel = new SortedDictionary<int, List<FlowNode>>();
        foreach (var n in nodes)
        {
            if (!byLevel.TryGetValue(level[n.Id], out var list)) { list = new List<FlowNode>(); byLevel[level[n.Id]] = list; }
            list.Add(n);
        }

        double x = Margin;
        foreach (var kv in byLevel)
        {
            double colW = 0;
            foreach (var n in kv.Value)
            {
                (n.W, n.H) = NodeSize(n);
                colW = Math.Max(colW, n.W);
            }
            foreach (var n in kv.Value) n.X = x;
            x += colW + ColGap;
        }

        double maxBottom = 0;
        foreach (var kv in byLevel)
        {
            double y = Margin;
            foreach (var n in kv.Value)
            {
                n.Y = y;
                y += n.H + RowGap;
                maxBottom = Math.Max(maxBottom, y);
            }
        }

        // 画布尺寸 = 内容包围盒 + 边距（防退化：至少一个节点）。
        double totalW = x - ColGap + Margin;
        if (nodes.Count == 1) totalW = nodes[0].W + Margin * 2;
        double totalH = Math.Max(120, maxBottom + Margin - RowGap);
        int w = (int)Math.Ceiling(Math.Min(totalW, 10000));
        int h = (int)Math.Ceiling(Math.Min(totalH, 10000));
        if (w > 0) doc.Width = w;
        if (h > 0) doc.Height = h;
    }

    static (double, double) NodeSize(FlowNode n)
    {
        double textW = EstimateTextWidth(n.Label);
        int lines = 1 + n.Label.Count(c => c == '\n');
        double w = Math.Clamp(textW + PadX * 2, 84, 340);
        double h = FontSize * 1.4 * lines + PadY * 2;
        if (n.Shape == "diamond") { w = Math.Clamp(textW * 1.3 + PadX * 3, 120, 380); h = Math.Max(h * 1.7, 72); }
        else if (n.Shape == "circle") { double d = Math.Max(w, h); w = d; h = d; }
        return (w, h);
    }

    static double EstimateTextWidth(string text)
    {
        double w = 0;
        foreach (var r in text.EnumerateRunes())
            w += IsWide(r.Value) ? FontSize : FontSize * 0.55;
        return w;
    }

    static bool IsWide(int cp)
        => cp > 0x2E7F; // CJK / 全角 / 韩文等东亚宽字符（近似），BMP 宽字符区间起点之上

    // ── 图元生成 ──

    static void Emit(DrawDocument doc, List<FlowNode> nodes, List<FlowEdge> edges)
    {
        var pos = new Dictionary<string, FlowNode>();
        foreach (var n in nodes) pos[n.Id] = n;

        foreach (var n in nodes)
        {
            double cx = n.X + n.W / 2, cy = n.Y + n.H / 2;
            doc.Figures.Add(ShapeFigure(n, cx, cy));
            var t = new DrawFigure { Kind = "text", Text = n.Label, Fill = TextColor, Anchor = "middle", FontSize = FontSize };
            t.Args.Add(cx);
            t.Args.Add(cy + FontSize * 0.35); // 基线补偿，视觉垂直居中
            doc.Figures.Add(t);
        }

        foreach (var e in edges)
        {
            if (!pos.TryGetValue(e.From, out var a) || !pos.TryGetValue(e.To, out var b)) continue;
            double ax = a.X + a.W / 2, ay = a.Y + a.H / 2;
            double bx = b.X + b.W / 2, by = b.Y + b.H / 2;

            double sx = ax, sy = ay, ex = bx, ey = by;
            if (ClipSegment(ax, ay, bx, by, a.X, a.Y, a.X + a.W, a.Y + a.H, out _, out _, out var ox1, out var oy1))
            { sx = ox1; sy = oy1; }
            if (ClipSegment(ax, ay, bx, by, b.X, b.Y, b.X + b.W, b.Y + b.H, out var ix0, out var iy0, out _, out _))
            { ex = ix0; ey = iy0; }

            var ef = new DrawFigure
            {
                Kind = e.HasHead ? "arrow" : "line",
                Stroke = EdgeColor,
                StrokeWidth = e.Thick ? 3 : 1.5,
                LineCap = "round",
                Dashed = e.Dashed,
            };
            ef.Args.Add(sx); ef.Args.Add(sy); ef.Args.Add(ex); ef.Args.Add(ey);
            doc.Figures.Add(ef);

            if (e.Label != null)
            {
                var lt = new DrawFigure { Kind = "text", Text = e.Label, Fill = TextColor, Anchor = "middle", FontSize = 12 };
                lt.Args.Add((sx + ex) / 2);
                lt.Args.Add((sy + ey) / 2 - 6);
                doc.Figures.Add(lt);
            }
        }
    }

    static DrawFigure ShapeFigure(FlowNode n, double cx, double cy)
    {
        var f = new DrawFigure { Fill = NodeFill, Stroke = NodeStroke, StrokeWidth = 1.5 };
        switch (n.Shape)
        {
            case "diamond":
                f.Kind = "polygon";
                f.Args.Add(cx); f.Args.Add(n.Y);
                f.Args.Add(cx + n.W / 2); f.Args.Add(cy);
                f.Args.Add(cx); f.Args.Add(n.Y + n.H);
                f.Args.Add(cx - n.W / 2); f.Args.Add(cy);
                break;
            case "circle":
                f.Kind = "circle";
                f.Args.Add(cx); f.Args.Add(cy); f.Args.Add(Math.Min(n.W, n.H) / 2);
                break;
            case "round":
                f.Kind = "roundrect";
                f.Args.Add(n.X); f.Args.Add(n.Y); f.Args.Add(n.W); f.Args.Add(n.H); f.Args.Add(12);
                break;
            default:
                f.Kind = "rect";
                f.Args.Add(n.X); f.Args.Add(n.Y); f.Args.Add(n.W); f.Args.Add(n.H);
                break;
        }
        return f;
    }

    /// <summary>Liang-Barsky 线段裁剪：返回线段与轴对齐矩形相交的部分（无交返回 false）。</summary>
    static bool ClipSegment(double x0, double y0, double x1, double y1,
        double xmin, double ymin, double xmax, double ymax,
        out double ox0, out double oy0, out double ox1, out double oy1)
    {
        ox0 = x0; oy0 = y0; ox1 = x1; oy1 = y1;
        double dx = x1 - x0, dy = y1 - y0;
        double t0 = 0, t1 = 1;
        double[] p = { -dx, dx, -dy, dy };
        double[] q = { x0 - xmin, xmax - x0, y0 - ymin, ymax - y0 };
        for (int i = 0; i < 4; i++)
        {
            if (Math.Abs(p[i]) < 1e-12)
            {
                if (q[i] < 0) return false;
            }
            else
            {
                double r = q[i] / p[i];
                if (p[i] < 0) { if (r > t1) return false; if (r > t0) t0 = r; }
                else { if (r < t0) return false; if (r < t1) t1 = r; }
            }
        }
        ox0 = x0 + t0 * dx; oy0 = y0 + t0 * dy;
        ox1 = x0 + t1 * dx; oy1 = y0 + t1 * dy;
        return true;
    }
}

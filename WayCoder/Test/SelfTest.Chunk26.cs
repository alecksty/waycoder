using WayCoder.UI.Shared;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 绘图接口的护栏 —— 先是**号段查重**，后面会陆续加"刷子/渐变"那批（见
    /// `docs/VML宿主接口.md` §5 的加接口步骤）。
    ///
    /// ## 为什么第一条断言是查重
    ///
    /// syscall 号是**在 `VmlUi` 里一个个 `public const int Xxx = 5xx;` 加上去的**，
    /// 而 AOT 禁反射 ⇒ 运行时枚举不出来 ⇒ 没有这张清单时，"两个特性抢同一个号"
    /// 只能靠人眼比对。它的症状不是报错，而是**一个功能静默变成另一个功能**：
    /// `HandleSyscall` 的 `switch` 遇到两个相同 `case` 时**先写的那个赢**
    /// （后写的那个是**永远不可达**的代码，编译器只给一条 CS0162 警告），
    /// 于是新加的功能"调了没反应"、或者更糟——跑的是旧那个的语义。
    ///
    /// 这与本仓反复踩的「平行表」是同一族问题，但危害更大：平行表漂了是"两边显示不一样"，
    /// 号撞了是"功能整个不见了"。所以这条断言放在新一批接口动工**之前**先落。
    /// </summary>
    private static void TestChunk26(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("VML 宿主接口：号段查重");

        var all = VmlUi.AllNumbers;
        Check($"号清单非空（实得 {all.Length} 个）", all.Length > 0);

        // ① 同一个号出现两次 —— 后加的那个功能会被先加的抢走，且**编译期只给警告**
        var dupes = all.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Check(dupes.Count == 0
                ? "号段无重复"
                : $"号段无重复（重复：{string.Join(", ", dupes)}）",
            dupes.Count == 0);

        // ② 都在 500–599，且都被 Handles/ReservedRange 认领 —— 号段外的号会被内置 syscall 接走
        var outside = all.Where(n => !VmlUi.Handles(n)).ToList();
        Check(outside.Count == 0
                ? "所有号都落在 500–599（Handles 认领）"
                : $"所有号都落在 500–599（越界：{string.Join(", ", outside)}）",
            outside.Count == 0);

        var reserved = new HashSet<int>(VmlUi.ReservedRange());
        var notReserved = all.Where(n => !reserved.Contains(n)).ToList();
        Check(notReserved.Count == 0
                ? "所有号都在 ReservedRange 里（否则运行时会以「权限不足」拒掉）"
                : $"所有号都在 ReservedRange 里（缺：{string.Join(", ", notReserved)}）",
            notReserved.Count == 0);

        // ③ 反证：这道网**真的会响**。把清单临时改成一个必然撞号的形状，
        //    断言查重逻辑抓得到 —— 「不响的自测比没有更糟」是本仓记过的一条。
        var probe = all.Append(all[0]).ToArray();
        var probeDupes = probe.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Check("反证：清单里塞一个重复号，查重能抓到", probeDupes.Count == 1 && probeDupes[0] == all[0]);

        _ = Fail;

        TestStrokeStyleCompat(Section, Check);
        TestBrushModel(Section, Check);
    }

    /// <summary>
    /// **刷子模型的参数防护** —— 这批号是"一个号 + 操作码"，程序传进来的
    /// 种类/槽位/形状码**全是裸整数**，写错一个就是未定义行为。
    ///
    /// 仓库既有的规矩是「**让异常参数最多画不出来，绝不崩**」（见 `VmlScene` 那一堆
    /// `InCoordRange` / `Dim` / `MaxPolyPoints`）。这一批同样按这条写，但
    /// **写的时候没有自测钉住** —— 于是补在这里。
    ///
    /// 判据刻意只断言两类，因为这两类才是"崩"的来源：
    /// ① **不抛**（越界索引、除零、空引用）；
    /// ② **越界的东西真的被挡住了**（不是"没崩就算过"—— 静默画错也是错）。
    /// </summary>
    private static void TestBrushModel(Action<string> Section, Action<string, bool> Check)
    {
        Section("绘图 DSL：刷子模型的参数防护");

        // ── 形状码 ──
        var s = new VmlScene();
        Check("未知形状码 → false（不抛）", !s.AddShape(999, 1, 2, 3, 4, 5, 6, 7));
        Check("负形状码 → false", !s.AddShape(-1, 1, 2, 3, 4, 5, 6, 7));

        // 坐标越界整条丢弃：屏幕外的东西本来也看不见，丢掉还省内存（与既有策略一致）
        Check("坐标超出 ±100 万 → 丢弃",
            !s.AddShape(VmlShape.Rect, VmlScene.CoordLimit + 1, 0, 10, 10, 0, 0, 0));
        Check("合法坐标 → 受理", s.AddShape(VmlShape.Rect, 0, 0, 10, 10, 0, 0, 0));

        // 角数钳位 —— 与 DSL 侧 StarCommand/RegularCommand 的判据同源
        Check("星形角数 < 2 被抬到 2",
            s.AddStar(10, 10, 20, 8, 0, 0) && s.BuildDsl().Contains("star 10 10 20 8 2 0"));
        Check("星形角数 > 4096 被压到 4096",
            s.AddStar(10, 10, 20, 8, 99999, 0) && s.BuildDsl().Contains("star 10 10 20 8 4096 0"));
        Check("正多边形边数 < 3 被抬到 3",
            s.AddRegular(10, 10, 20, 1, 0) && s.BuildDsl().Contains("regular 10 10 20 3 0"));
        Check("旋转角归一到 [0,360)", s.AddStar(10, 10, 20, 8, 5, -90)
            && s.BuildDsl().Contains("star 10 10 20 8 5 270"));

        // ── 样式槽 ──
        Check("未知样式槽 → false（不抛）", !s.SetStyle(99, unchecked((int)0xFF00FF00), 0, 0, 0, 0));
        Check("SetStyle 正常槽 → true", s.SetStyle(VmlStyleSlot.Fill, unchecked((int)0xFF00FF00), 0, 0, 0, 0));

        // 句柄越界一律当"没有"，绝不能拿它去索引刷子表
        Check("越界句柄 → BrushToken 为 null", s.BrushToken(999) == null);
        Check("句柄 0 → null（0 是「没有」）", s.BrushToken(0) == null);
        Check("未知槽位不产生刷子", s.BrushToken(-5) == null);

        // 渐变刷子给了"画笔"槽（本批还不支持）→ **退化到起始色并记警告**，不崩、不静默
        var s2 = new VmlScene();
        var gb = s2.AddGradientBrush(radial: false, 0xFFFF0000, 0xFF0000FF, 0, 0, 1000, 0);
        Check("渐变刷子句柄有效", gb >= 1 && s2.BrushToken(gb)!.StartsWith('@'));
        Check("渐变刷子给了画笔槽 → 受理（退化成起始色，不崩）",
            s2.SetStyle(VmlStyleSlot.Pen, gb, 3, 0, 0, 0) && s2.PenToken == "#FFFF0000");

        // ── 刷子表上限 ──
        var s3 = new VmlScene();
        var last = 0;
        for (var i = 0; i < VmlScene.MaxBrushes + 10; i++) last = s3.AddSolidBrush((uint)(0xFF000000 + i));
        Check("刷子表满了之后返回 0（不越界、不崩）", last == 0);
        Check($"上限就是 {VmlScene.MaxBrushes}",
            s3.BrushToken(VmlScene.MaxBrushes) != null && s3.BrushToken(VmlScene.MaxBrushes + 1) == null);

        // 同色复用：循环里反复造不该把表撑爆
        var s4 = new VmlScene();
        var h1 = s4.AddSolidBrush(0xFF123456);
        var h2 = s4.AddSolidBrush(0xFF123456);
        Check("同色刷子复用同一个句柄", h1 == h2 && h1 >= 1);
    }

    /// <summary>
    /// **描边类图元解析的兼容性主闸**。
    ///
    /// `line` / `arrow` / `polyline` 以前各写了一份逐字相同的样式循环，现在收口成
    /// `DrawParse.ParseStrokeStyle` 一处。收口本身必须**逐字段零变化** ——
    /// 这几条断言就是那个"零"：把每条指令解析出来的
    /// `Stroke / StrokeWidth / LineCap / Dashed / Fill / GradientRef / FontFamily / Args` 全部钉住。
    ///
    /// 为什么值得单列一批：这三条是 `draw` 工具（AI 直接写 DSL）与桌面导出共用的一面，
    /// 解析漂了不会报错，只会"画出来的东西和写的不一样"。
    /// </summary>
    private static void TestStrokeStyleCompat(Action<string> Section, Action<string, bool> Check)
    {
        Section("绘图 DSL：描边类解析（收口前后必须逐字段不变）");

        // 把一条图元的全部样式字段打成一个字符串 —— 断言一条字符串比断言八个字段好读，
        // 而且**漏掉某个字段**本身就会被看见（那一格永远是默认值）。
        static string Dump(string dsl)
        {
            var doc = DrawRunner.Parse("canvas 200 200 #ffffff\n" + dsl);
            if (doc.Figures.Count != 1) return $"图元数={doc.Figures.Count} err={doc.Error}";
            var f = doc.Figures[0];
            var args = string.Join(",", f.Args.Select(v => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)));
            return $"{f.Kind} args=[{args}] stroke=#{f.Stroke:X8} w={f.StrokeWidth:0.###} cap={f.LineCap} dash={f.Dashed} "
                 + $"fill=#{f.Fill:X8} gref={f.GradientRef ?? "-"} font={f.FontFamily} size={f.FontSize:0.###} anchor={f.Anchor}";
        }

        // ① line：裸线 / 颜色 + 线宽 / 线帽 + 虚线
        Check("line 默认", Dump("line 0 0 10 10") ==
            "line args=[0,0,10,10] stroke=#FF000000 w=1 cap=butt dash=False fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");
        Check("line 颜色+线宽", Dump("line 0 0 10 10 #ff0000 5") ==
            "line args=[0,0,10,10] stroke=#FFFF0000 w=5 cap=butt dash=False fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");
        Check("line 线帽+虚线", Dump("line 0 0 10 10 #f00 2 round dash") ==
            "line args=[0,0,10,10] stroke=#FFFF0000 w=2 cap=round dash=True fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");

        // ② arrow：与 line 同一套样式段
        Check("arrow 颜色", Dump("arrow 0 0 10 10 #00ff00") ==
            "arrow args=[0,0,10,10] stroke=#FF00FF00 w=1 cap=butt dash=False fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");

        // ③ polyline：几何**变长**，样式段从第 N 个 token 起（与 line/arrow 的起点不同）
        Check("polyline 颜色+线宽+线帽", Dump("polyline 0 0 10 0 10 10 #0000ff 3 square") ==
            "polyline args=[0,0,10,0,10,10] stroke=#FF0000FF w=3 cap=square dash=False fill=#FF000000 gref=- font=sans-serif size=14 anchor=start");

        // ④ 填充形状走的是另一套（第一个颜色=填充、第二个=描边）—— 顺手一起钉住，
        //    免得将来"统一到 ParseStyle"时把描边类的单色语义搞混
        Check("rect 双色（填充+描边）", Dump("rect 0 0 10 10 #ff0000 #00ff00 3") ==
            "rect args=[0,0,10,10] stroke=#FF00FF00 w=3 cap=butt dash=False fill=#FFFF0000 gref=- font=sans-serif size=14 anchor=start");
        Check("rect 渐变填充", Dump("rect 0 0 10 10 @g #00ff00 3") ==
            "rect args=[0,0,10,10] stroke=#FF00FF00 w=3 cap=butt dash=False fill=#FF000000 gref=g font=sans-serif size=14 anchor=start");

        // ⑤ text：`@id` 必须进**渐变引用**，绝不能落进"其余裸词=字体族名"的兜底
        var textDump = Dump("text 10 20 \"hi\" 24 #ffffff middle");
        Check("text 常规（颜色/字号/锚点）", textDump ==
            "text args=[10,20] stroke=#00000000 w=1 cap=butt dash=False fill=#FFFFFFFF gref=- font=sans-serif size=24 anchor=middle");
        var textGrad = Dump("text 10 20 \"hi\" 24 @g1");
        Check("text @id → 渐变引用（不是字体族名）", textGrad.Contains("gref=g1"));
        Check("text @id 不再污染字体族", textGrad.Contains("font=sans-serif"));
    }
}

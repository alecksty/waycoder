using WayCoder.Infra;
using WayCoder.UI.Shared;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// 绘图增强（v0.96.176）：**路径展平**与**渐变/路径这一段的端到端出图**。
    ///
    /// 两条判据层次不同，都要有：
    ///   · **纯逻辑**（<see cref="DrawPath"/>）：曲线展平成什么点、圆弧算得对不对 —— 判数值，
    ///     不依赖像素，错了能一眼看出是哪条命令；
    ///   · **出图**（<c>VmlScene → DSL → DrawRunner.ToPng</c>）：真的落到像素上没有 ——
    ///     纯逻辑全对但图元没接上（比如 DSL 少个关键字）时，只有这一层拦得住。
    ///     这也正是本仓一贯的"渲染路径只能靠出图验证"。
    /// </summary>
    private static void TestChunk22(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        Section("绘图增强：路径展平 / 曲线 / 圆弧");

        // ── 直线段：首点不丢（老回归），且子路径被正确切开 ──
        var one = DrawPath.Flatten("M 10 20 L 30 40");
        Check("DrawPath: M/L 得到 1 条子路径 2 个点",
            one.Count == 1 && one[0].Points.Count == 2);
        Check("DrawPath: 首点坐标是 (10,20) 而不是被当成 y 丢掉",
            one.Count == 1 && one[0].Points[0] == (10, 20) && one[0].Points[1] == (30, 40));

        // ── 隐含重复：`L 1 2 3 4` 是两次 lineto（SVG 规定）──
        var rep = DrawPath.Flatten("M0 0 L 1 2 3 4");
        Check("DrawPath: 连写的坐标对按重复命令展开（共 3 个点）",
            rep.Count == 1 && rep[0].Points.Count == 3 && rep[0].Points[2] == (3, 4));

        // ── 相对命令：小写 m/l 相对当前点 ──
        var rel = DrawPath.Flatten("m 10 10 l 5 0 l 0 5");
        Check("DrawPath: 小写相对命令累加到当前点",
            rel.Count == 1 && rel[0].Points[1] == (15, 10) && rel[0].Points[2] == (15, 15));

        // ── 三次贝塞尔：终点必须**精确**落在指定点上（展平不能有累积误差）──
        var cub = DrawPath.Flatten("M0 0 C 0 100 100 100 100 0");
        var cubPts = cub[0].Points;
        var last = cubPts[^1];
        Check("DrawPath: 三次贝塞尔终点精确落在 (100,0)",
            Math.Abs(last.X - 100) < 1e-9 && Math.Abs(last.Y) < 1e-9);
        Check("DrawPath: 三次贝塞尔被展平成多点（>8 段）", cubPts.Count > 8);
        Check("DrawPath: 曲线中点确实离弦很远（说明是曲线不是直线）",
            cubPts.Any(p => p.Y > 50));   // 控制点都在 y=100，曲线必然鼓起来

        // ── 二次贝塞尔 + T 的镜像控制点 ──
        var quad = DrawPath.Flatten("M0 0 Q 50 100 100 0");
        Check("DrawPath: 二次贝塞尔终点精确落在 (100,0)",
            Math.Abs(quad[0].Points[^1].X - 100) < 1e-9 && Math.Abs(quad[0].Points[^1].Y) < 1e-9);

        // ── 圆弧：半圆的直径必须等于 2r（这是圆弧算错最直接的指纹）──
        // 方向那一位是有讲究的：SVG 的 y 轴向下，sweep=1（正角方向）= 屏幕上"顺时针"，
        // 从左点 (0,0) 走到右点 (100,0) 顺时针要**从上方绕过去** ⇒ y 为负。
        // 断言写反的话会把"对的实现"判成错的（这一条就是照着实测改过来的）。
        var arc = DrawPath.Flatten("M 0 0 A 50 50 0 0 1 100 0");
        var arcPts = arc[0].Points;
        var minY = arcPts.Min(p => p.Y);
        Check("DrawPath: sweep=1 的半圆向上鼓起到半径高度（minY≈−50）",
            Math.Abs(minY + 50) < 2.0);
        var arcDown = DrawPath.Flatten("M 0 0 A 50 50 0 0 0 100 0");
        Check("DrawPath: sweep=0 的半圆向下鼓起（maxY≈50）",
            Math.Abs(arcDown[0].Points.Max(p => p.Y) - 50) < 2.0);
        Check("DrawPath: 圆弧终点精确落在 (100,0)",
            Math.Abs(arcPts[^1].X - 100) < 1e-6 && Math.Abs(arcPts[^1].Y) < 1e-6);

        // 半径不够大时要按 SVG 规范放大半径（否则终点画不到）
        var arc2 = DrawPath.Flatten("M 0 0 A 1 1 0 0 1 100 0");
        Check("DrawPath: 半径过小时按规范放大（终点仍落在 (100,0)）",
            Math.Abs(arc2[0].Points[^1].X - 100) < 1e-6);

        // ── 闭合与多子路径（填充挖洞靠它）──
        var two = DrawPath.Flatten("M0 0 L10 0 L10 10 Z M2 2 L4 2 L4 4 Z");
        Check("DrawPath: 两条子路径被分开（奇偶挖洞的前提）", two.Count == 2);
        Check("DrawPath: Z 闭合会把起点补回终点", two[0].Closed && two[0].Points[^1] == (0, 0));

        // ── 坏输入不该炸、也不该让整张图消失 ──
        Check("DrawPath: 空串 → 空结果（不抛）", DrawPath.Flatten("").Count == 0);
        Check("DrawPath: 垃圾输入 → 不抛异常",
            DrawPath.Flatten("M ,, L qq 5").Count >= 0);
        Check("DrawPath: 只有 M 没有后续 → 画不出东西，子路径被丢掉",
            DrawPath.Flatten("M 5 5").Count == 0);

        Section("绘图增强：路径 / 渐变端到端出图");

        // ── 曲线真的落到像素上了吗：一条横向的贝塞尔，扫它"该鼓起"的那一列 ──
        var s1 = new VmlScene { Width = 120, Height = 80 };
        s1.Clear(0xFFFFFFFF);
        s1.AddPath("M 10 40 C 40 0 80 0 110 40", 0xFF000000, 3);
        var px1 = Rasterize(s1);
        Check("出图：贝塞尔在弦的上方确实画到了（曲线中点那一列有黑像素）",
            HasDarkNear(px1, 60, 8, 30));
        Check("出图：直线不该出现在弦的下方（说明画的真是曲线不是直连）",
            !HasDarkNear(px1, 60, 52, 70));

        // ── 填充：闭合路径填充后，内部一定是填充色 ──
        var s2 = new VmlScene { Width = 120, Height = 80 };
        s2.Clear(0xFFFFFFFF);
        s2.AddPath("M 20 20 L 100 20 L 100 60 L 20 60 Z", 0, 0, 0, 0xFF00FF00, fillSet: true);
        var px2 = Rasterize(s2);
        Check("出图：闭合路径 fill 之后内部是填充色（绿）", IsGreen(px2, 30, 40));

        // ── 奇偶挖洞：外框填 + 内框再填一次 ⇒ 中心该是"没填"（挖出洞）──
        var s3 = new VmlScene { Width = 120, Height = 80 };
        s3.Clear(0xFFFFFFFF);
        s3.AddPath("M 20 20 L 100 20 L 100 60 L 20 60 Z M 50 35 L 70 35 L 70 45 L 50 45 Z",
            0, 0, 0, 0xFF00FF00, fillSet: true);
        var px3 = Rasterize(s3);
        Check("出图：多子路径按奇偶规则挖洞（洞心 60,40 是背景色）", !IsGreen(px3, 60, 40));
        Check("出图：挖洞后外环仍在（环上 30,40 是绿的）", IsGreen(px3, 30, 40));

        // ── 渐变刷子：矩形的左右两端颜色必须不同（都同色就说明渐变没接上）──
        var s4 = new VmlScene { Width = 120, Height = 80 };
        s4.Clear(0xFFFFFFFF);
        s4.AddGradient("g", radial: false, 0xFFFF0000, 0xFF0000FF);   // 默认几何即"从左到右"
        s4.AddRect(10, 20, 100, 40, 0, filled: true, width: 0, radius: 0, fillGradient: "g");
        var px4 = Rasterize(s4);
        var left = px4.ColorAt(15, 40);
        var right = px4.ColorAt(105, 40);
        Check("出图：线性渐变左端偏红、右端偏蓝（两端颜色不同）",
            left != right && R(left) > B(left) && B(right) > R(right));

        // ── 超采样倍率的选择（纯函数，性能与画质的那个折中）──
        Section("绘图性能：超采样倍率按面积自适应");
        Check("超采样: 小画布拿满 3×（图标/缩略图这类）", DrawRunner.ChooseSupersample(64, 48) == 3);
        Check("超采样: 手机全屏降到 2×（377×539 —— 3× 会到 183 万像素）",
            DrawRunner.ChooseSupersample(377, 539) == 2);
        Check("超采样: 超大画布退到 1×（宁可不抗锯齿也别卡住）",
            DrawRunner.ChooseSupersample(2000, 2000) == 1);
        Check("超采样: 倍率单调不升（面积越大越省）",
            DrawRunner.ChooseSupersample(100, 100) >= DrawRunner.ChooseSupersample(400, 400));

        // ── id 清洗：带空格/引号的 id 不能把 DSL 那一行拆坏 ──
        Check("VmlUi.SafeId: 空格与引号被替换成下划线",
            VmlUi.SafeId("a b\"c") == "a_b_c");
        Check("VmlUi.SafeId: 正常 id 原样保留", VmlUi.SafeId("btn-bg.1") == "btn-bg.1");
        Check("VmlUi.SafeId: 空/全非法 → 空串（调用方据此跳过）", VmlUi.SafeId("") == "" && VmlUi.SafeId(" ").Length == 1);
    }

    // ── 出图工具：**复用既有那条链**（VmlScene → DSL → ToPng → PngDecoder → RasterImage），
    //    与 SelfTest.Chunk10 的"空心矩形是黄色"同一套 —— 别再自己开一条取像素的路。

    private static RasterImage Rasterize(VmlScene s)
        => PngDecoder.Decode(DrawRunner.ToPng(DrawRunner.Parse(s.BuildDsl())));

    // `RasterImage.ColorAt` 的序是 **0xAARRGGBB**（与 VML 的颜色一致）：
    // 实现是 `(A<<24)|(R<<16)|(G<<8)|B`（见 RasterImage.cs:28）。**别看反了** ——
    // 这一版就先看反了一次，把对的实现判成错的，白绕一圈。
    private static byte R(uint c) => (byte)(c >> 16);
    private static byte G(uint c) => (byte)(c >> 8);
    private static byte B(uint c) => (byte)c;
    private static byte A(uint c) => (byte)(c >> 24);

    /// <summary>在 (x, y0..y1) 这一列里找有没有"暗"像素（描边色是黑）。</summary>
    /// <summary>
    /// 在 (x, y0..y1) 这一列里找有没有"**明显暗于白底**"的像素。
    ///
    /// ⚠ 阈值不能按"接近纯黑"定：抗锯齿是 3× 超采样再盒式降采样，3px 宽的线降采样后
    /// 中心也只有 `#555555` 左右（实测）。按 R&lt;80 判会把"确实画上了"判成没画 ——
    /// 判据是**相对背景**（背景是白），不是"是不是纯黑"。
    /// </summary>
    private static bool HasDarkNear(RasterImage img, int x, int y0, int y1)
    {
        for (var y = y0; y <= y1; y++)
        {
            var p = img.ColorAt(x, y);
            if (R(p) < 200 && G(p) < 200 && B(p) < 200) return true;
        }
        return false;
    }

    private static bool IsGreen(RasterImage img, int x, int y)
    {
        var p = img.ColorAt(x, y);
        return G(p) > 150 && R(p) < 100 && B(p) < 100;
    }
}

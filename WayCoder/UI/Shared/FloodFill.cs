namespace WayCoder.UI.Shared;

/// <summary>
/// **种子填充**（BGI 的 `floodfill`）—— 扫描线算法，纯逻辑、与宿主无关。
///
/// <para>
/// ## 为什么老程序需要它
///
/// DOS 时代的 graphics.h 程序画**填充图形**几乎都这么写：
/// </para>
///
/// <code>
/// setcolor(YELLOW);
/// circle(50, 100, 25);          // 先描边
/// setfillstyle(SOLID_FILL, YELLOW);
/// floodfill(50, 100, YELLOW);   // 再从内部某点灌色
/// </code>
///
/// <para>
/// 实测语料里 `floodfill` 出现 **10 次**（6 个经典 BGI 程序里 2 个要用它）——
/// 不做这个，那一整类程序在手机上就是"画得出轮廓、填不上色"。
/// </para>
///
/// <para>
/// ## ⚠ 为什么返回**水平游程**而不是逐像素
///
/// 这个东西的消费方是**保留模式**的场景（`VmlScene` 只有图元，没有像素缓冲）。
/// 逐像素输出意味着一次填充要往场景里塞几十万个 `ui_pixel` 图元 —— 光是想一遍就知道不行。
/// </para>
///
/// <para>
/// 而扫描线填充**天生**是按行产出连续段的：一段 = 一个 `矩形(x, y, w, 1)`。
/// 实测一个 640×480 里的封闭图形，游程数是**百量级**，与逐像素差三个数量级。
/// 这也正是这里选扫描线而不是简单四连通递归的原因 —— **算法形状要和消费方对齐**。
/// </para>
///
/// <para>
/// ## BGI 的语义（照抄，不改）
///
/// `floodfill(x, y, border)`：从 `(x, y)` 出发，**遇到 `border` 色的像素就停**，
/// 四连通地把连通的非边界区域整个填掉。种子点本身就是边界色时**什么都不做**
/// （不是错误 —— 老程序常见"点在线上"的写法）。
/// </para>
/// </summary>
public static class FloodFill
{
    /// <summary>一段水平游程：从 <see cref="X"/> 起、宽 <see cref="W"/>、在 <see cref="Y"/> 行。</summary>
    public readonly record struct Run(int X, int Y, int W);

    /// <summary>
    /// 在 <paramref name="pixels"/>（行优先、`0xAARRGGBB`）上从 <paramref name="sx"/>,<paramref name="sy"/>
    /// 做四连通种子填充，返回**水平游程**。
    ///
    /// <para>
    /// **不做任何绘制** —— 只算"哪些像素要变"，怎么落到场景里是调用方的事
    /// （这正是它能在桌面被逐条自测的原因）。
    /// </para>
    /// </summary>
    /// <param name="pixels">像素缓冲（行优先，长度应 ≥ width*height）</param>
    /// <param name="width">缓冲宽</param>
    /// <param name="height">缓冲高</param>
    /// <param name="sx">种子点 x</param>
    /// <param name="sy">种子点 y</param>
    /// <param name="border">**边界色**：碰到它就停（BGI 的第三个参数）</param>
    public static List<Run> Runs(ReadOnlySpan<int> pixels, int width, int height,
        int sx, int sy, int border)
    {
        var runs = new List<Run>();
        if (width <= 0 || height <= 0) return runs;
        if (sx < 0 || sy < 0 || sx >= width || sy >= height) return runs;
        if (pixels.Length < width * height) return runs;

        // 种子点本身在边界上 ⇒ BGI 的语义是"什么都不做"（不是错误）
        if (pixels[sy * width + sx] == border) return runs;

        // `filled` 是一张与画面同尺寸的位图 —— 标记"这个像素已经归我了"。
        // 用位图而不是改动 `pixels` 本身：调用方可能还要拿原图做别的事
        //（比如 `getimage` 拿的是**填充前**的内容），改了就说不清了。
        var filled = new bool[width * height];

        // 显式栈存**种子**（不是待填像素）：每段只压一次，栈深与行数同阶而不是与像素数同阶。
        var stack = new Stack<(int X, int Y)>();
        stack.Push((sx, sy));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            int row = y * width;

            if (filled[row + x] || pixels[row + x] == border) continue;

            // ① 向左右扩到段的边界
            int left = x;
            while (left > 0 && !filled[row + left - 1] && pixels[row + left - 1] != border) left--;
            int right = x;
            while (right < width - 1 && !filled[row + right + 1] && pixels[row + right + 1] != border) right++;

            // ② 整段标记
            for (int i = left; i <= right; i++) filled[row + i] = true;
            runs.Add(new Run(left, y, right - left + 1));

            // ③ 上下两行里**新出现的**段各压一个种子。
            //    ⚠ 判据是"这一格能填 **且** 左边那格不能填" —— 也就是**段的起点**。
            //    每格都压的话栈会爆（一段要压 W 个），只压起点则一段一个。
            ScanAdjacent(stack, filled, pixels, width, left, right, y - 1, border);
            ScanAdjacent(stack, filled, pixels, width, left, right, y + 1, border);
        }

        return runs;
    }

    private static void ScanAdjacent(Stack<(int X, int Y)> stack, bool[] filled, ReadOnlySpan<int> pixels,
        int width, int left, int right, int y, int border)
    {
        if (y < 0 || y >= filled.Length / width) return;
        int row = y * width;
        bool prevFillable = false;

        for (int x = left; x <= right; x++)
        {
            bool fillable = !filled[row + x] && pixels[row + x] != border;
            if (fillable && !prevFillable) stack.Push((x, y));   // 段的起点
            prevFillable = fillable;
        }
    }
}

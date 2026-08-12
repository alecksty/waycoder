using System.Text;
using WayCoder.Terminal;

namespace WayCoder.UI;

/// <summary>
/// 模型选择对话框 —— 居中 ANSI 对话框，橙→黄渐变实心外框 + 比例列宽 + 整行高亮。
/// 外框渐变参考权限确认对话框的 GradOrangeYellow。
/// </summary>
public static class ModelPicker
{
    public record ModelEntry(string Id, string DisplayName, string Provider,
        string ProviderId, bool HasApiKey, int ContextWindow, double InputPrice);
    public record Result(string ModelId, bool IsLarge, int TargetSlot,
        bool NeedsApiKey = false, string? ProviderId = null);

    private const int MinW = 62, MinH = 16;
    private const int PadX = 2;   // 边框内侧缩进
    private const int ColGap = 1; // 列间距
    private const int ColIcon = 2;

    // 列权重（总和 22，不含图标固定宽度）
    private const int WtName = 8, WtProv = 5, WtCtx = 2, WtPrice = 3, WtLarge = 2, WtSmall = 2;
    private const int WtTotal = WtName + WtProv + WtCtx + WtPrice + WtLarge + WtSmall;

    private const int FrameH = 9; // 标题2 + 搜索1 + 上分隔1 + 列头1 + 下分隔1 + 帮助1 + 槽位1 + 底框1

    // 渐变外框色 —— 橙→黄（对标权限确认对话框 GradOrangeYellow）
    private static readonly int GradStart = AnsiTty.RgbCode(255, 180, 0);   // 橙色
    private static readonly int GradEnd   = AnsiTty.RgbCode(255, 255, 80);  // 黄色
    private static readonly int DimBgCode = AnsiTty.RgbCode(8, 8, 12);      // 暗蓝黑底

    // 分隔线色 —— 取渐变色 30% 位置
    private static readonly int SepColor = AnsiTty.LerpRgb(GradStart, GradEnd, 0.3f);

    // ═══════════════════════════════════════════════
    // Show
    // ═══════════════════════════════════════════════

    /// <summary>
    /// 显示模型选择对话框。
    /// </summary>
    /// <param name="currentSlot">当前槽位索引：0-9=槽位F1-F10, -1=全局默认</param>
    public static Result? Show(int currentSlot = -1)
    {
        var cfg = Config.Instance;
        string large = cfg.Model, small = cfg.SmallModel;
        // 槽位模式：读取该槽位的模型配置
        if (currentSlot >= 0 && currentSlot < 10)
        {
            var sc = AgentSlotConfig.Get(currentSlot);
            if (!sc.UseGlobal)
            {
                if (!string.IsNullOrEmpty(sc.LargeModel)) large = sc.LargeModel;
                if (!string.IsNullOrEmpty(sc.SmallModel)) small = sc.SmallModel;
            }
        }
        bool isLarge = true;
        string filter = "";
        int sel = 0, scr = 0;
        int targetSlot = currentSlot; // 当前目标槽位：-2=全部, -1=全局, 0-9=具体槽位
        List<ModelEntry> models = GetAvailableModels();
        bool firstFrame = true;

        while (true)
        {
            int tw = Tty.Cols, th = Tty.Rows;
            int dw = Math.Max(MinW, tw * 2 / 3);
            int dh = Math.Max(MinH, th * 2 / 3);
            int bx = Math.Max(1, (tw - dw) / 2);
            int by = Math.Max(1, (th - dh) / 2);
            int innerW = dw - 2;
            int listH = dh - FrameH;

            var filtered = string.IsNullOrEmpty(filter)
                ? models
                : models.Where(m =>
                    m.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    m.Id.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    m.Provider.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

            // 根据 targetSlot 确定 large/small 当前值
            (large, small) = ResolveSlotModels(targetSlot, large, small);

            // 首次打开：将选中项定位到当前模型
            string curModel = isLarge ? large : small;
            if (firstFrame) { for (int i = 0; i < filtered.Count; i++) if (filtered[i].Id == curModel) { sel = i; break; } firstFrame = false; }
            sel = Math.Clamp(sel, 0, Math.Max(0, filtered.Count - 1));
            if (sel < scr) scr = sel;
            if (sel >= scr + listH) scr = sel - listH + 1;
            scr = Math.Clamp(scr, 0, Math.Max(0, filtered.Count - listH));

            // 计算比例列宽
            int avail = innerW - PadX * 2 - 6 * ColGap - ColIcon;
            int[] cw = DistributeWidths(avail);

            // ═══ 绘制 ═══
            var sb = new StringBuilder();
            sb.Append(AnsiTty.CursorHide);

            // 暗化背景
            for (int y = 0; y < dh; y++)
                FillRow(sb, by + y, bx, dw, 0, DimBgCode, ' ');

            // ═══ 渐变上边框（实心，逐字橙→黄渐变）═══
            WriteGradChar(sb, by, bx,             '┌', GradStart, dw, 0);
            for (int i = 1; i < dw - 1; i++)
                WriteGradChar(sb, by, bx + i,     '─', GradStart, dw, i);
            WriteGradChar(sb, by, bx + dw - 1,    '┐', GradEnd,   dw, dw - 1);

            // ═══ 标题行 ═══
            WriteGradSideL(sb, by + 1, bx, dw);
            var slotLabel = targetSlot switch { -2 => " — 全部槽位", >= 0 => $" — F{targetSlot + 1} 槽位", _ => "" };
            var title = isLarge ? $"🤖 选择大模型 (复杂任务){slotLabel}" : $"🔧 选择小模型 (简单任务){slotLabel}";
            sb.Append(AnsiTty.CursorPos(by + 1, bx + PadX + 1))
              .Append(AnsiTty.FgBgCode(TuiColors.White, DimBgCode))
              .Append(AnsiTty.SgrBold).Append(title).Append(AnsiTty.SgrReset);
            var tab = "Tab 切换";
            sb.Append(AnsiTty.CursorPos(by + 1, bx + dw - 1 - VW(tab) - 1))
              .Append(AnsiTty.FgBgCode(TuiColors.BrightBlack, DimBgCode))
              .Append(AnsiTty.SgrDim).Append(tab).Append(AnsiTty.SgrReset);
            WriteGradSideR(sb, by + 1, bx, dw);

            // ═══ 搜索行 ═══
            WriteGradSideL(sb, by + 2, bx, dw);
            DrawSearchBox(sb, by + 2, bx, innerW, filter);
            WriteGradSideR(sb, by + 2, bx, dw);

            // ═══ 上分隔线（渐变横线）═══
            WriteGradChar(sb, by + 3, bx,             '├', SepColor, dw, 0);
            for (int i = 1; i < dw - 1; i++)
                WriteGradChar(sb, by + 3, bx + i,     '─', SepColor, dw, i);
            WriteGradChar(sb, by + 3, bx + dw - 1,    '┤', SepColor, dw, dw - 1);

            // ═══ 列标题 ═══
            WriteGradSideL(sb, by + 4, bx, dw);
            DrawColHeaders(sb, by + 4, bx + PadX, cw);
            WriteGradSideR(sb, by + 4, bx, dw);

            // ═══ 列表行 ═══
            int dataTop = by + 5;
            for (int i = 0; i < listH; i++)
            {
                int mi = scr + i, row = dataTop + i;
                bool selected = mi >= 0 && mi < filtered.Count && mi == sel;
                var model = mi >= 0 && mi < filtered.Count ? filtered[mi] : null;
                bool isL = model != null && model.Id == large;
                bool isS = model != null && model.Id == small;

                WriteGradSideL(sb, row, bx, dw);

                if (model == null)
                {
                    FillRow(sb, row, bx + 1, innerW, TuiColors.White, TuiColors.BgBlack, ' ');
                }
                else
                {
                    int bg = selected ? TuiColors.BgYellow : TuiColors.BgBlack;
                    int fg = selected ? TuiColors.Black : TuiColors.White;
                    FillRow(sb, row, bx + 1, innerW, fg, bg, ' ');
                    DrawModelRow(sb, row, bx + PadX, model, cw, fg, bg, selected, isL, isS);
                }
                WriteGradSideR(sb, row, bx, dw);
            }

            // ═══ 下分隔线 ═══
            int sep2 = dataTop + listH;
            WriteGradChar(sb, sep2, bx,             '├', SepColor, dw, 0);
            for (int i = 1; i < dw - 1; i++)
                WriteGradChar(sb, sep2, bx + i,     '─', SepColor, dw, i);
            WriteGradChar(sb, sep2, bx + dw - 1,    '┤', SepColor, dw, dw - 1);

            // ═══ 帮助行 ═══
            WriteGradSideL(sb, sep2 + 1, bx, dw);
            var help = "↑↓导航  Enter默认  Esc取消  Tab切换  A全槽位  1-0指定槽位  输入搜索";
            if (filtered.Count > listH)
                help += $"  {scr * 100 / Math.Max(1, filtered.Count - listH)}%";
            sb.Append(AnsiTty.CursorPos(sep2 + 1, bx + PadX))
              .Append(AnsiTty.FgBgCode(TuiColors.White, DimBgCode))
              .Append(' ').Append(TruncVW(help, innerW - 2));
            WriteGradSideR(sb, sep2 + 1, bx, dw);

            // ═══ 槽位行 ═══
            WriteGradSideL(sb, sep2 + 2, bx, dw);
            DrawSlotBar(sb, sep2 + 2, bx + PadX, targetSlot, currentSlot);
            WriteGradSideR(sb, sep2 + 2, bx, dw);

            // ═══ 渐变下边框 ═══
            WriteGradChar(sb, sep2 + 3, bx,             '└', GradStart, dw, 0);
            for (int i = 1; i < dw - 1; i++)
                WriteGradChar(sb, sep2 + 3, bx + i,     '─', GradStart, dw, i);
            WriteGradChar(sb, sep2 + 3, bx + dw - 1,    '┘', GradEnd,   dw, dw - 1);

            sb.Append(AnsiTty.SgrReset);
            Console.Write(sb.ToString());

            // ═══ 输入 ═══
            var key = Console.ReadKey(intercept: true);
            bool hasCtrl = (key.Modifiers & ConsoleModifiers.Control) != 0;

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:    if (sel > 0) sel--; break;
                case ConsoleKey.DownArrow:  if (sel < filtered.Count - 1) sel++; break;
                case ConsoleKey.Home:       sel = 0; break;
                case ConsoleKey.End:        sel = Math.Max(0, filtered.Count - 1); break;
                case ConsoleKey.PageUp:     sel = Math.Max(0, sel - listH); break;
                case ConsoleKey.PageDown:   sel = Math.Min(filtered.Count - 1, sel + listH); break;
                case ConsoleKey.Tab:
                    isLarge = !isLarge; filter = ""; sel = 0; scr = 0; firstFrame = true; break;
                case ConsoleKey.Escape:
                    Console.Write(AnsiTty.CursorShow); TuiManager.RequestFullRefresh(); return null;
                case ConsoleKey.Backspace:
                    if (filter.Length > 0) { filter = filter[..^1]; sel = 0; } break;
                case ConsoleKey.Enter:
                    return EnterOrPromptKey(filtered, sel, isLarge, targetSlot);

                // Ctrl+A 或 A（筛选空时）：切换 全部 / 当前槽位
                case ConsoleKey.A:
                    if (hasCtrl || filter.Length == 0) { targetSlot = targetSlot == -2 ? currentSlot : -2; firstFrame = true; }
                    else { filter += 'a'; sel = 0; }
                    break;

                default:
                    // Ctrl+数字 或 数字（筛选空时）：切换目标槽位
                    if (TrySlotKey(key, out int slot))
                    {
                        if (hasCtrl || filter.Length == 0) { targetSlot = slot; firstFrame = true; }
                        else { filter += key.KeyChar; sel = 0; }
                        break;
                    }
                    // 普通字符 → 搜索过滤
                    if (key.KeyChar >= ' ' && key.KeyChar <= '~')
                    { filter += key.KeyChar; sel = 0; }
                    break;
            }
        }
    }

    // ═══════════════════════════════════════════════
    // 渐变外框渲染
    // ═══════════════════════════════════════════════

    /// <summary>写单个渐变字符：根据位置计算渐变色</summary>
    private static void WriteGradChar(StringBuilder sb, int row, int col, char ch,
        int gs, int totalW, int pos)
    {
        float t = totalW > 1 ? (float)pos / (totalW - 1) : 0;
        int c = AnsiTty.LerpRgb(GradStart, GradEnd, t);
        sb.Append(AnsiTty.CursorPos(row, col))
          .Append(AnsiTty.FgBgCode(c, DimBgCode))
          .Append(ch);
    }

    /// <summary>左侧竖线 — 渐变起始色（橙色）</summary>
    private static void WriteGradSideL(StringBuilder sb, int row, int bx, int dw)
    {
        sb.Append(AnsiTty.CursorPos(row, bx))
          .Append(AnsiTty.FgBgCode(GradStart, DimBgCode))
          .Append('│');
    }

    /// <summary>右侧竖线 — 渐变终止色（黄色）</summary>
    private static void WriteGradSideR(StringBuilder sb, int row, int bx, int dw)
    {
        sb.Append(AnsiTty.CursorPos(row, bx + dw - 1))
          .Append(AnsiTty.FgBgCode(GradEnd, DimBgCode))
          .Append('│');
    }

    /// <summary>填充整行（单色背景）</summary>
    private static void FillRow(StringBuilder sb, int row, int col, int w, int fg, int bg, char fill)
    {
        sb.Append(AnsiTty.CursorPos(row, col))
          .Append(AnsiTty.FgBgCode(fg, bg))
          .Append(new string(fill, w));
    }

    // ═══════════════════════════════════════════════
    // 子渲染
    // ═══════════════════════════════════════════════

    private static void DrawSearchBox(StringBuilder sb, int y, int bx, int innerW, string filter)
    {
        int contentW = innerW - PadX * 2;
        sb.Append(AnsiTty.CursorPos(y, bx + PadX))
          .Append(AnsiTty.FgBgCode(TuiColors.White, DimBgCode));
        var label = "🔍 ";
        var rawText = filter.Length > 0 ? filter : "输入关键词过滤...";
        var style = filter.Length > 0 ? "" : AnsiTty.SgrDim;
        int maxW = contentW - VW(label) - 1;
        var disp = TruncVW(rawText, maxW);
        sb.Append(label).Append(style).Append(disp);
        int fill = maxW - VW(disp) + 1;
        if (fill > 0) sb.Append(new string(' ', fill));
        sb.Append(AnsiTty.SgrReset);
    }

    private static void DrawColHeaders(StringBuilder sb, int y, int cx, int[] cw)
    {
        // 与 DrawModelRow 完全对齐：每列用 CursorPos 定位，col 按 cw[n]+ColGap 递进
        sb.Append(AnsiTty.FgBgCode(TuiColors.BrightBlack, DimBgCode))
          .Append(AnsiTty.SgrDim);
        int col = cx;
        sb.Append(AnsiTty.CursorPos(y, col)).Append(Pad(" ", ColIcon));
        col += ColIcon + ColGap;
        sb.Append(AnsiTty.CursorPos(y, col)).Append(Pad("模型名称", cw[0]));
        col += cw[0] + ColGap;
        sb.Append(AnsiTty.CursorPos(y, col)).Append(Pad("厂商", cw[1]));
        col += cw[1] + ColGap;
        sb.Append(AnsiTty.CursorPos(y, col)).Append(Pad("上下文", cw[2]));
        col += cw[2] + ColGap;
        sb.Append(AnsiTty.CursorPos(y, col)).Append(PadR("价格", cw[3]));
        col += cw[3] + ColGap;
        sb.Append(AnsiTty.CursorPos(y, col)).Append(Pad("大模型", cw[4]));
        col += cw[4] + ColGap;
        sb.Append(AnsiTty.CursorPos(y, col)).Append(Pad("小模型", cw[5]));
        sb.Append(AnsiTty.SgrReset);
    }

    private static void DrawModelRow(StringBuilder sb, int y, int cx, ModelEntry m, int[] cw,
        int fgBase, int bg, bool sel, bool isL, bool isS)
    {
        int col = cx;

        // Icon — 仅在有 API key 时显示🔑
        sb.Append(AnsiTty.CursorPos(y, col));
        if (sel)
            sb.Append(AnsiTty.FgBgCode(fgBase, bg)).Append(" ▶");
        else if (m.HasApiKey)
            sb.Append(AnsiTty.FgBgCode(TuiColors.Green, bg)).Append(" 🔑");
        else
            sb.Append(AnsiTty.FgBgCode(fgBase, bg)).Append("  ");
        col += ColIcon + ColGap;

        // Name
        sb.Append(AnsiTty.CursorPos(y, col));
        int nameFg = (!sel && (isL || isS)) ? TuiColors.Green : fgBase;
        sb.Append(AnsiTty.FgBgCode(nameFg, bg)).Append(Pad(m.DisplayName, cw[0]));
        col += cw[0] + ColGap;

        // Provider
        sb.Append(AnsiTty.CursorPos(y, col));
        sb.Append(AnsiTty.FgBgCode(sel ? fgBase : TuiColors.BrightBlack, bg));
        if (!sel) sb.Append(AnsiTty.SgrDim);
        sb.Append(Pad(m.Provider, cw[1]));
        if (!sel) sb.Append(AnsiTty.SgrReset);
        col += cw[1] + ColGap;

        // Context
        sb.Append(AnsiTty.CursorPos(y, col));
        sb.Append(AnsiTty.FgBgCode(fgBase, bg)).Append(Pad(FmtCtx(m.ContextWindow), cw[2]));
        col += cw[2] + ColGap;

        // Price (right-aligned)
        sb.Append(AnsiTty.CursorPos(y, col));
        int pf = (!sel && m.InputPrice <= 0) ? TuiColors.Green : fgBase;
        sb.Append(AnsiTty.FgBgCode(pf, bg)).Append(PadR(FmtPrice(m.InputPrice), cw[3]));
        col += cw[3] + ColGap;

        // Large ✓
        sb.Append(AnsiTty.CursorPos(y, col));
        int lf = !sel && isL ? TuiColors.Green : TuiColors.BrightBlack;
        sb.Append(AnsiTty.FgBgCode(lf, bg)).Append(Pad(isL ? " ✓" : "  ", cw[4]));
        col += cw[4] + ColGap;

        // Small ✓
        sb.Append(AnsiTty.CursorPos(y, col));
        int sf = !sel && isS ? TuiColors.Green : TuiColors.BrightBlack;
        sb.Append(AnsiTty.FgBgCode(sf, bg)).Append(Pad(isS ? " ✓" : "  ", cw[5]));

        sb.Append(AnsiTty.SgrReset);
    }

    private static void DrawSlotBar(StringBuilder sb, int y, int cx, int targetSlot, int currentSlot)
    {
        const int DarkFg = 37;  // 白字 — 暗底上可见
        const int LightFg = 30; // 黑字 — 黄底上可见

        sb.Append(AnsiTty.CursorPos(y, cx));

        // "全部"按钮
        bool allMode = targetSlot == -2;
        int allBg = allMode ? TuiColors.BgYellow : DimBgCode;
        int allFg = allMode ? LightFg : DarkFg;
        sb.Append(AnsiTty.FgBgCode(allFg, allBg));
        if (allMode)
            sb.Append('[').Append(AnsiTty.Fg(TuiColors.Green)).Append('▶').Append(AnsiTty.Fg(allFg)).Append(']');
        else
            sb.Append('[').Append(AnsiTty.SgrDim).Append('A').Append(AnsiTty.SgrReset).Append(AnsiTty.FgBgCode(allFg, allBg)).Append(']');
        sb.Append("全部  ").Append(AnsiTty.SgrReset);

        for (int i = 0; i < 10; i++)
        {
            var sc = AgentSlotConfig.Get(i);
            bool hasCfg = !sc.UseGlobal;
            bool isTarget = i == targetSlot;
            bool isCur = i == currentSlot;
            string label = i == 9 ? "0" : (i + 1).ToString();

            int slotBg = (isTarget || hasCfg) ? TuiColors.BgYellow : DimBgCode;
            int slotFg = (isTarget || hasCfg) ? LightFg : DarkFg;

            sb.Append(AnsiTty.FgBgCode(slotFg, slotBg));

            if (isTarget)
                sb.Append('[').Append(AnsiTty.Fg(TuiColors.Green)).Append('▶').Append(AnsiTty.Fg(slotFg)).Append(']');
            else if (hasCfg)
                sb.Append('[').Append(AnsiTty.Fg(TuiColors.Green)).Append('x').Append(AnsiTty.Fg(slotFg)).Append(']');
            else
                sb.Append('[').Append(AnsiTty.SgrDim).Append(' ').Append(AnsiTty.SgrReset).Append(AnsiTty.FgBgCode(slotFg, slotBg)).Append(']');

            // 当前槽位（非目标）→ 绿色数字
            if (isCur && !isTarget)
                sb.Append(AnsiTty.Fg(TuiColors.Green));
            sb.Append(label);
            if (isCur && !isTarget)
                sb.Append(AnsiTty.Fg(slotFg));
            sb.Append(AnsiTty.SgrReset);

            if (i < 9) sb.Append(AnsiTty.FgBgCode(DarkFg, DimBgCode)).Append(' ');
        }
        sb.Append(AnsiTty.SgrReset);
    }

    // ═══════════════════════════════════════════════
    // 模型操作
    // ═══════════════════════════════════════════════

    /// <summary>Enter：无 key 则返回 NeedsApiKey，有 key 则直接应用</summary>
    private static Result? EnterOrPromptKey(List<ModelEntry> models, int idx, bool isLarge, int slot)
    {
        if (idx < 0 || idx >= models.Count) return null;
        var m = models[idx];
        if (!m.HasApiKey)
        {
            // 返回 NeedsApiKey，由调用方弹出输入框
            Console.Write(AnsiTty.CursorShow);
            TuiManager.RequestFullRefresh();
            return new(m.Id, isLarge, slot, NeedsApiKey: true, ProviderId: m.ProviderId);
        }
        Apply(m.Id, isLarge, slot);
        Console.Write(AnsiTty.CursorShow);
        TuiManager.RequestFullRefresh();
        return new(m.Id, isLarge, slot);
    }

    private static bool TrySlotKey(ConsoleKeyInfo key, out int slot)
    {
        slot = key.Key switch
        {
            ConsoleKey.D1 or ConsoleKey.NumPad1 => 0,
            ConsoleKey.D2 or ConsoleKey.NumPad2 => 1,
            ConsoleKey.D3 or ConsoleKey.NumPad3 => 2,
            ConsoleKey.D4 or ConsoleKey.NumPad4 => 3,
            ConsoleKey.D5 or ConsoleKey.NumPad5 => 4,
            ConsoleKey.D6 or ConsoleKey.NumPad6 => 5,
            ConsoleKey.D7 or ConsoleKey.NumPad7 => 6,
            ConsoleKey.D8 or ConsoleKey.NumPad8 => 7,
            ConsoleKey.D9 or ConsoleKey.NumPad9 => 8,
            ConsoleKey.D0 or ConsoleKey.NumPad0 => 9,
            _ => -1
        };
        return slot >= 0;
    }

    private static void Apply(string modelId, bool isLarge, int slot)
    {
        var cfg = Config.Instance;
        if (slot == -1)
        {
            if (isLarge) cfg.Model = modelId; else cfg.SmallModel = modelId;
            cfg.SaveToEnvFile();
        }
        else if (slot == -2)
        {
            AgentSlotConfig.SetUniform(new AgentSlotConfig.SlotConfig
            { UseGlobal = false, LargeModel = isLarge ? modelId : null, SmallModel = isLarge ? null : modelId });
            if (isLarge) cfg.Model = modelId; else cfg.SmallModel = modelId;
            cfg.SaveToEnvFile();
        }
        else if (slot is >= 0 and < 10)
        {
            var e = AgentSlotConfig.Get(slot);
            AgentSlotConfig.Set(slot, new AgentSlotConfig.SlotConfig
            {
                UseGlobal = false,
                LargeModel = isLarge ? modelId : e.LargeModel,
                SmallModel = isLarge ? e.SmallModel : modelId,
                BaseUrl = e.BaseUrl, ApiKeyProviderId = e.ApiKeyProviderId, ApiKey = e.ApiKey,
            });
        }
    }

    /// <summary>根据 targetSlot 解析该槽位的大/小模型配置</summary>
    private static (string large, string small) ResolveSlotModels(int targetSlot, string fallbackLarge, string fallbackSmall)
    {
        var cfg = Config.Instance;
        if (targetSlot == -2) return (fallbackLarge, fallbackSmall); // 全部：用当前显示的
        if (targetSlot == -1) return (cfg.Model, cfg.SmallModel);     // 全局
        if (targetSlot is >= 0 and < 10)
        {
            var sc = AgentSlotConfig.Get(targetSlot);
            if (!sc.UseGlobal)
                return (
                    string.IsNullOrEmpty(sc.LargeModel) ? cfg.Model : sc.LargeModel,
                    string.IsNullOrEmpty(sc.SmallModel) ? cfg.SmallModel : sc.SmallModel);
        }
        return (cfg.Model, cfg.SmallModel);
    }

    /// <summary>供应商 → 专属环境变量名映射</summary>
    private static readonly Dictionary<string, string> ProviderEnvVar = new(StringComparer.OrdinalIgnoreCase)
    {
        ["openai"] = "OPENAI_API_KEY",
        ["anthropic"] = "ANTHROPIC_API_KEY",
        ["deepseek"] = "DEEPSEEK_API_KEY",
        ["google"] = "GOOGLE_API_KEY",
        ["qwen"] = "DASHSCOPE_API_KEY",
        ["moonshot"] = "MOONSHOT_API_KEY",
        ["zhipu"] = "ZHIPU_API_KEY",
        ["bytedance"] = "ARK_API_KEY",
        ["01ai"] = "YI_API_KEY",
        ["xai"] = "XAI_API_KEY",
        ["mistral"] = "MISTRAL_API_KEY",
        ["siliconflow"] = "SILICONFLOW_API_KEY",
        ["meta"] = "META_API_KEY",
    };

    /// <summary>检查指定模型是否有 API Key</summary>
    private static bool ModelHasKey(string providerId, string modelId)
    {
        // 1. ApiKeyStore 显式存储（按供应商）
        if (!string.IsNullOrEmpty(ApiKeyStore.Get(providerId)))
            return true;
        // 2. 供应商专属环境变量（如 DEEPSEEK_API_KEY）
        if (ProviderEnvVar.TryGetValue(providerId, out var envVar))
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
                return true;
        }
        // 3. 通用模式：{PROVIDER}_API_KEY
        var genericEnv = $"{providerId}_API_KEY".ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(genericEnv)))
            return true;
        // 4. 全局 WAYCODER_API_KEY → 仅当前配置的大小模型
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYCODER_API_KEY")))
        {
            var cfg = Config.Instance;
            if (modelId == cfg.Model || modelId == cfg.SmallModel)
                return true;
        }
        // 5. Local/Custom 不需要 key
        if (providerId is "local" or "custom") return true;

        return false;
    }

    private static List<ModelEntry> GetAvailableModels()
    {
        var list = new List<ModelEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var info in ModelCatalog.BuiltIn)
        {
            if (!seen.Add(info.Id)) continue;
            var hasKey = ModelHasKey(info.ProviderId, info.Id);
            list.Add(new(info.Id, info.DisplayName, info.Provider, info.ProviderId, hasKey, info.ContextWindow, info.InputPrice));
        }
        if (!string.IsNullOrEmpty(Config.Instance.FallbackChain))
            foreach (var m in Config.Instance.FallbackChain.Split(','))
            { var t = m.Trim(); if (!string.IsNullOrEmpty(t) && seen.Add(t)) list.Add(new(t, t, "自定义", "custom", true, 128_000, 0)); }
        return list;
    }

    // ═══════════════════════════════════════════════
    // 比例列宽
    // ═══════════════════════════════════════════════

    private static int[] DistributeWidths(int avail)
    {
        var cw = new int[6];
        cw[0] = Math.Max(6, avail * WtName / WtTotal);
        cw[1] = Math.Max(4, avail * WtProv / WtTotal);
        cw[2] = Math.Max(4, avail * WtCtx / WtTotal);
        cw[3] = Math.Max(5, avail * WtPrice / WtTotal);
        cw[4] = Math.Max(4, avail * WtLarge / WtTotal);
        cw[5] = Math.Max(4, avail * WtSmall / WtTotal);
        int sum = cw.Sum();
        cw[0] += avail - sum;
        if (cw[0] < 4) cw[0] = 4;
        return cw;
    }

    // ═══════════════════════════════════════════════
    // 格式化
    // ═══════════════════════════════════════════════

    private static string FmtCtx(int t) => t switch
    {
        <= 0 => "   -", >= 1_000_000 => $"{t / 1_000_000.0:0.#}M".PadLeft(4),
        _ => $"{t / 1_000}K".PadLeft(4),
    };

    private static string FmtPrice(double p) => p switch
    {
        <= 0 => "Free", < 0.01 => "<$0.01", _ => $"${p:F2}",
    };

    // ═══════════════════════════════════════════════
    // 文本工具
    // ═══════════════════════════════════════════════

    private static string Pad(string text, int w)
    {
        int v = VW(text);
        return v >= w ? TruncVW(text, w) : text + new string(' ', w - v);
    }

    private static string PadR(string text, int w)
    {
        int v = VW(text);
        return v >= w ? TruncVW(text, w) : new string(' ', w - v) + text;
    }

    private static int VW(string text) => TuiHelper.DisplayWidth(text);

    private static string TruncVW(string text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (VW(text) <= max) return text;
        var runes = text.EnumerateRunes().ToList();
        int w = 0, n = 0;
        foreach (var r in runes)
        {
            int rw = TuiHelper.RuneWidth(r);
            if (w + rw + 1 > max) break;
            w += rw; n++;
        }
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++) sb.Append(runes[i].ToString());
        sb.Append('…');
        return sb.ToString();
    }
}

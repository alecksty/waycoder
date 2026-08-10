using System.Text;
using CoreCoderSharp.Terminal;

namespace CoreCoderSharp.UI.TuiControls;

/// <summary>
/// 弹出菜单 —— 带标题栏的可滚动选项列表。
///
/// 特性：
/// - 窗口边框 + 标题栏 + 可滚动列表
/// - 键盘导航：↑↓ Home End Enter Esc
/// - 快捷键：1-9 快速选择前 9 项
/// - 分隔线（空字符串或 "---"）
/// - 内容过多时自动滚动 + 滚动条指示器
///
/// 用法（ChatScreen 内）：
///   var win = TuiMenu.Show("操作", items, x, y, idx => ..., onCancel: () => ...);
///   screen.ShowWindow(win);
/// </summary>
public static class TuiMenu
{
    /// <summary>最大可见行数（超出则滚动）</summary>
    private const int MaxVisible = 14;

    /// <summary>
    /// 创建弹出菜单窗口。
    /// </summary>
    /// <param name="title">标题（""=无标题栏）</param>
    /// <param name="items">菜单项列表（空字符串 = 分隔线）</param>
    /// <param name="x">弹出 X 坐标</param>
    /// <param name="y">弹出 Y 坐标</param>
    /// <param name="onSelect">选中回调（传入索引，-1=取消）</param>
    /// <param name="onCancel">取消回调</param>
    /// <param name="maxVisible">最大可见项数（默认 14）</param>
    public static TuiWindow Show(
        string title,
        List<string> items,
        int x, int y,
        Action<int>? onSelect = null,
        Action? onCancel = null,
        int maxVisible = MaxVisible)
    {
        var visCount = Math.Min(items.Count, Math.Max(5, maxVisible));
        var maxVw = items.Count > 0
            ? items.Where(i => !string.IsNullOrEmpty(i) && i != "---")
                .Max(i => TuiHelper.DisplayWidth(i))
            : 10;
        // 宽度：内容 + 左边距" 1."(3) + 右边距(2) + 快捷键提示(4) + 滚动条(2)
        var contentW = Math.Max(16, Math.Min(maxVw + 11, Tty.Cols - 8));

        var hasTitle = !string.IsNullOrEmpty(title);
        var titleH = hasTitle ? 1 : 0;

        var win = new TuiWindow
        {
            Title = title,
            ShowTitle = hasTitle,
            X = x, Y = y,
            Width = contentW + 2,
            Height = visCount + titleH + 2, // +上下边框
            Modal = true,
            HasMask = false,
            Border = WindowBorder.Rounded,
            BorderColor = TuiTheme.Current.WindowBorderFocused,
            WinBg = TuiTheme.Current.WindowBg,
        };

        // 确保不超出屏幕
        ClampPosition(win);

        // 存储菜单状态到窗口
        var state = new MenuState
        {
            Items = items,
            SelectedIndex = 0,
            ScrollOffset = 0,
            VisibleCount = visCount,
            ContentWidth = contentW,
            HasTitle = hasTitle,
            OnSelect = onSelect,
            OnCancel = onCancel,
            // 关闭窗口回调（由 MenuView 在选中/取消时调用）
            CloseMenu = () => { win.OnClosed?.Invoke(); },
        };
        // 跳过首个分隔线
        while (state.SelectedIndex < items.Count && IsSeparator(items[state.SelectedIndex]))
            state.SelectedIndex++;
        if (state.SelectedIndex >= items.Count)
            state.SelectedIndex = 0;

        win.ContentLines = []; // 不使用 ContentLines
        win.RootView = new MenuView(state);

        // 快捷键：1-9 快速选择
        for (var i = 0; i < Math.Min(9, items.Count); i++)
        {
            var idx = i;
            if (!IsSeparator(items[i]))
            {
                win.RegisterShortcut(ConsoleKey.D1 + i, () =>
                {
                    state.SelectedIndex = idx;
                    state.OnSelect?.Invoke(idx);
                    win.Result = idx;
                    win.OnClosed?.Invoke();
                });
            }
        }

        // Esc 取消
        win.RegisterShortcut(ConsoleKey.Escape, () =>
        {
            win.Result = -1;
            state.OnCancel?.Invoke();
            win.OnClosed?.Invoke();
        });

        return win;
    }

    // ── 内部 ──

    /// <summary>
    /// 判断是否为分隔线（空字符串或 "---"）。
    /// </summary>
    /// <param name="item">菜单项内容。</param>
    /// <returns>是否为分隔线。</returns>
    private static bool IsSeparator(string item)
        => string.IsNullOrEmpty(item) || item == "---";

    /// <summary>
    /// 确保菜单窗口位置不超出屏幕范围。
    /// </summary>
    /// <param name="win">菜单窗口。</param>
    private static void ClampPosition(TuiWindow win)
    {
        if (win.X + win.Width > Tty.Cols)
            win.X = Math.Max(0, Tty.Cols - win.Width);
        if (win.Y + win.Height > Tty.Rows)
            win.Y = Math.Max(0, Tty.Rows - win.Height);
        if (win.X < 0) win.X = 0;
        if (win.Y < 0) win.Y = 0;
    }

    /// <summary>菜单内部状态</summary>
    internal class MenuState
    {
        public List<string> Items = [];
        public int SelectedIndex;
        public int ScrollOffset;
        public int VisibleCount = MaxVisible;
        public int ContentWidth = 30;
        public bool HasTitle;
        public Action<int>? OnSelect;
        public Action? OnCancel;

        /// <summary>关闭菜单窗口（由 MenuView 在选中/取消后调用，确保窗口关闭 + 脏区域重绘）</summary>
        public Action? CloseMenu;
    }

    /// <summary>
    /// 菜单视图 —— 渲染菜单项列表 + 滚动条，处理键盘导航。
    /// 嵌入 TuiWindow.RootView 使用。
    /// </summary>
    internal class MenuView : TuiView
    {
        private readonly MenuState _state;

        /// <summary>
        /// 初始化菜单视图。
        /// </summary>
        /// <param name="state">菜单状态。</param>
        public MenuView(MenuState state)
        {
            _state = state;
            Width = state.ContentWidth;
            Height = state.VisibleCount;
            Focused = true;
        }

        public override void Layout()
        {
            /* 菜单固定尺寸，无需递归布局 */
        }


        /// <summary>
        /// 渲染菜单项列表 + 滚动条。
        /// </summary>
        /// <param name="sb">渲染缓冲区。</param>
        /// <param name="absX">绝对 X 坐标。</param>
        /// <param name="absY">绝对 Y 坐标。</param>
        protected override void OnRender(StringBuilder sb, int absX, int absY)
        {
            var visH = _state.VisibleCount;
            var items = _state.Items;

            // 确保选中项可见
            if (_state.SelectedIndex >= 0)
            {
                if (_state.SelectedIndex < _state.ScrollOffset)
                    _state.ScrollOffset = _state.SelectedIndex;
                if (_state.SelectedIndex >= _state.ScrollOffset + visH)
                    _state.ScrollOffset = _state.SelectedIndex - visH + 1;
                _state.ScrollOffset = Math.Clamp(_state.ScrollOffset, 0,
                    Math.Max(0, items.Count - visH));
            }

            for (int i = 0; i < visH; i++)
            {
                int idx = _state.ScrollOffset + i;
                int row = absY + i;
                if (idx >= items.Count) break;

                var item = items[idx];

                // 分隔线
                if (IsSeparator(item))
                {
                    var sep = new string('─', _state.ContentWidth);
                    var rbSep = new RenderBuffer();
                    rbSep.Write(row, absX, sep, fg: 8); // dim separator
                    sb.Append(rbSep.ToString());
                    continue;
                }

                var sel = idx == _state.SelectedIndex;
                var fg = !IsEnabled ? (DisabledFg > 0 ? DisabledFg : TuiTheme.Current.ControlDisabledFg)
                    : sel ? TuiTheme.Current.ListSelFg : TuiTheme.Current.ListFg;
                // 非选中项用继承背景色（窗口填充色），避免 AnsiTty.SgrReset 重置后变黑
                var inheritedBg = GetInheritedBg();
                var bg = !IsEnabled ? (DisabledBg > 0 ? DisabledBg : inheritedBg)
                    : sel ? TuiTheme.Current.ListSelBg : inheritedBg;

                // 快捷键提示（前 9 项）
                var shortcut = idx < 9 ? $" {idx + 1}" : "  ";
                var display = $"{shortcut}. {item}";
                if (TuiHelper.DisplayWidth(display) > _state.ContentWidth - 1)
                    display = TuiHelper.TruncateByWidth(display, _state.ContentWidth - 1);

                int pad = Math.Max(0, _state.ContentWidth - TuiHelper.DisplayWidth(display));
                var line = display + new string(' ', pad);

                var rb = new RenderBuffer();
                rb.Write(row, absX, line, fg: fg, bg: bg);
                sb.Append(rb.ToString());
            }

            // 滚动指示器
            if (items.Count > visH)
            {
                var barH = Math.Max(1, visH * visH / Math.Max(1, items.Count));
                var maxScroll = Math.Max(0, items.Count - visH);
                var barPos = maxScroll > 0 ? visH * _state.ScrollOffset / maxScroll : 0;
                barPos = Math.Clamp(barPos, 0, visH - barH);

                for (int i = 0; i < visH; i++)
                {
                    var row = absY + i;
                    var ch = (i >= barPos && i < barPos + barH) ? "█" : "│";
                    var rb = new RenderBuffer();
                    rb.Write(row, absX + _state.ContentWidth, ch, fg: 8);
                    sb.Append(rb.ToString());
                }

                // 滚动百分比
                var pct = maxScroll > 0 ? _state.ScrollOffset * 100 / maxScroll : 0;
                var pctText = pct >= 100 ? "▮" : pct >= 50 ? "▬" : "▭";
                {
                    var rbPct = new RenderBuffer();
                    rbPct.Write(visH > 0 ? absY + visH - 1 : absY,
                        absX + _state.ContentWidth, pctText, fg: 8);
                    sb.Append(rbPct.ToString());
                }
            }
        }

        /// <summary>
        /// 处理键盘输入。
        /// </summary>
        /// <param name="key">按下的键。</param>
        /// <returns>是否处理了该键。</returns>
        public override bool OnKey(ConsoleKeyInfo key)
        {
            var items = _state.Items;
            if (items.Count == 0) return false;

            // 禁用状态不响应输入
            if (!IsEnabled) return false;

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    MoveSelection(-1);
                    return true;
                case ConsoleKey.DownArrow:
                    MoveSelection(1);
                    return true;
                case ConsoleKey.Home:
                    SetSelection(0);
                    return true;
                case ConsoleKey.End:
                    SetSelection(items.Count - 1);
                    return true;
                case ConsoleKey.PageUp:
                    _state.ScrollOffset = Math.Max(0, _state.ScrollOffset - _state.VisibleCount);
                    MoveSelection(-_state.VisibleCount);
                    return true;
                case ConsoleKey.PageDown:
                    _state.ScrollOffset = Math.Min(
                        Math.Max(0, items.Count - _state.VisibleCount),
                        _state.ScrollOffset + _state.VisibleCount);
                    MoveSelection(_state.VisibleCount);
                    return true;
                case ConsoleKey.Enter:
                    if (_state.SelectedIndex >= 0 && !IsSeparator(items[_state.SelectedIndex]))
                    {
                        _state.OnSelect?.Invoke(_state.SelectedIndex);
                        _state.CloseMenu?.Invoke();
                    }

                    return true;
            }

            return false;
        }

        private void MoveSelection(int delta)
        {
            var items = _state.Items;
            int max = items.Count;
            for (int attempt = 0; attempt < max; attempt++)
            {
                _state.SelectedIndex = (_state.SelectedIndex + delta + max) % max;
                if (!IsSeparator(items[_state.SelectedIndex]))
                    return;
            }

            _state.SelectedIndex = 0;
        }

        private void SetSelection(int index)
        {
            var items = _state.Items;
            _state.SelectedIndex = Math.Clamp(index, 0, items.Count - 1);
            // 跳到最近的普通项
            if (IsSeparator(items[_state.SelectedIndex]))
                MoveSelection(1);
        }
    }
}
// =============================================================
// TuiApp.cs —— 终端应用宿主
//
// 持有 Screen 双缓冲 + InputReader，驱动渲染循环与事件分发。
// 管理控件集合、焦点、Tab 导航、鼠标点击。IDE 基于此类搭建。
// 也可用于纯逻辑自测（在无 TTY 时仍能创建 Screen）。
// =============================================================
using QBasic.Tui;

namespace QBasic.Controls;

/// <summary>终端应用宿主。</summary>
public class TuiApp
{
    public Screen Screen { get; private set; }
    public List<Control> Controls { get; } = new();
    public MenuBar? MenuBar { get; set; }
    public StatusBar? StatusBar { get; set; }
    public int RootRow { get; set; } = 1;
    public int RootCol { get; set; } = 1;
    public bool Running { get; private set; }
    public bool QuitRequested { get; set; }

    private readonly InputReader _reader;
    private bool _needFull = true;

    public TuiApp(Stream? input = null)
    {
        var (rows, cols) = Terminal.GetSize();
        Screen = new Screen(rows, cols);
        _reader = new InputReader(input ?? Console.OpenStandardInput());
    }

    public void Invalidate() => _needFull = true;

    public void Add(Control c)
    {
        Controls.Add(c);
        c.App = this;
    }

    /// <summary>设置焦点控件。</summary>
    public void SetFocus(Control c)
    {
        foreach (var x in Controls) x.Focused = false;
        c.Focused = true;
    }

    public Control? Focused =>
        Controls.FirstOrDefault(c => c.Focused && c.Visible);

    /// <summary>按 Tab/Shift+Tab 在可聚焦控件间切换焦点。</summary>
    public void MoveFocus(bool forward)
    {
        var focusable = Controls.Where(c => c.CanFocus).ToList();
        if (focusable.Count == 0) return;
        int idx = focusable.FindIndex(c => c.Focused);
        int next = forward
            ? (idx < 0 ? 0 : (idx + 1) % focusable.Count)
            : (idx <= 0 ? focusable.Count - 1 : idx - 1);
        SetFocus(focusable[next]);
    }

    /// <summary>渲染一帧。</summary>
    public void Render()
    {
        if (_needFull)
        {
            if (CustomRender != null)
            {
                Screen.Clear(Color.Black);
                CustomRender(Screen);
                string diff = Screen.Flush();
                if (diff.Length > 0) { Console.Out.Write(diff); Console.Out.Flush(); }
                _needFull = false;
                return;
            }
            Screen.Clear(Color.Black);
            MenuBar?.Draw(Screen, 1);
            foreach (var c in Controls)
                if (c.Visible) c.Draw(Screen);
            StatusBar?.Draw(Screen, Screen.Rows);
            string diff2 = Screen.Flush();
            if (diff2.Length > 0)
            {
                Console.Out.Write(diff2);
                Console.Out.Flush();
            }
            _needFull = false;
        }
    }

    /// <summary>进入主事件循环。</summary>
    public void Run()
    {
        Running = true;
        Terminal.Enter();
        Terminal.EnterAltScreen();
        try
        {
            while (Running && !QuitRequested)
            {
                Render();
                if (!_reader.Read(out var ev)) break;
                HandleInput(ev);
            }
        }
        finally
        {
            Terminal.ExitAltScreen();
            Terminal.Leave();
        }
    }

    private void HandleInput(InputEvent ev)
    {
        // Ctrl+C 停止
        if (ev.IsCtrl('c'))
        {
            OnCtrlC?.Invoke();
            return;
        }
        // 应用级全局输入钩子（可用于运行期输入/输出屏返回）
        if (GlobalInput?.Invoke(ev) == true)
        {
            _needFull = true;
            return;
        }
        // 全局 Tab 导航（未打开菜单）
        if ((ev.IsKey(KeyCode.Tab) || (ev.Key == KeyCode.Tab && ev.Mods.HasFlag(KeyMods.Shift))) && MenuBar is { Open: false })
        {
            MoveFocus(!ev.Mods.HasFlag(KeyMods.Shift));
            _needFull = true;
            return;
        }
        // 菜单栏
        if (MenuBar != null)
        {
            if (MenuBar.OnKey(ev, out var selected))
            {
                _needFull = true;
                if (selected != null) OnMenuSelect?.Invoke(selected);
                return;
            }
        }
        // 鼠标
        if (ev.Key == KeyCode.Mouse)
        {
            HandleMouse(ev);
            return;
        }
        // 焦点控件
        var focused = Focused;
        if (focused != null)
        {
            if (focused.OnKey(ev))
            {
                _needFull = true;
                return;
            }
        }
        // 事件未消费，交给应用级处理器
        if (ev.IsKey(KeyCode.F5)) OnF5?.Invoke();
        _needFull = true;
    }

    private void HandleMouse(InputEvent ev)
    {
        foreach (var c in Controls)
        {
            if (!c.Visible || !c.Enabled) continue;
            if (c.Contains(ev.MouseRow, ev.MouseCol))
            {
                int relRow = ev.MouseRow - (RootRow + c.Row - 1) + 1;
                int relCol = ev.MouseCol - (RootCol + c.Col - 1) + 1;
                if (c.CanFocus) SetFocus(c);
                if (ev.Button == MouseButton.Left) c.OnClick(relRow, relCol);
                _needFull = true;
                return;
            }
        }
    }

    /// <summary>Ctrl+C 处理器。</summary>
    public Action? OnCtrlC { get; set; }
    /// <summary>F5 处理器。</summary>
    public Action? OnF5 { get; set; }
    /// <summary>菜单项选中处理器。</summary>
    public Action<string>? OnMenuSelect { get; set; }
    /// <summary>全局输入钩子：最先收到按键（Ctrl+C 之后）；返回 true 表示已消费。</summary>
    public Func<InputEvent, bool>? GlobalInput { get; set; }
    /// <summary>自定义全屏渲染器（设置后替代默认控件渲染）。</summary>
    public Action<Screen>? CustomRender { get; set; }
}

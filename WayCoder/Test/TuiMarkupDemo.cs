using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.TUI;
using WayCoder.UI.TUI.Base;

namespace WayCoder;

/// <summary>
/// TUI 声明式标记演示 —— 用 tuidemo/*.tui 布局文件 + 少量 code-behind 重构聊天界面与各对话框。
/// 运行：waycoder --tui-markup-demo
/// 演示「布局写标记、交互写代码」的工作模式：加载 .tui → Find(id) 拿控件 → 订阅事件。
/// </summary>
public static class TuiMarkupDemo
{
    public static void Run()
    {
        var mgr = TuiManager.Instance;
        try
        {
            mgr.Enter();
            mgr.RefreshTheme();

            var dir = FindTuiDemoDir();
            var main = TuiMarkup.LoadFile(Path.Combine(dir, "main.tui"));
            var screen = main.Screen ?? throw new Exception("main.tui 根元素应为 Screen");

            var messages = main.Find<TuiLabel>("messages")!;
            var input = main.Find<TuiInput>("input")!;

            void Send()
            {
                var text = input.Text.Trim();
                if (text.Length == 0) return;
                messages.Text += "\n👤 " + text;
                input.Text = "";
            }

            // ── code-behind：主界面事件 ──
            main.Find<TuiButton>("send")!.OnClick = _ => Send();
            input.OnSubmit = _ => Send();

            TuiMarkupResult LoadDialog(string name)
                => TuiMarkup.LoadFile(Path.Combine(dir, "dialogs", name + ".tui"));

            main.Find<TuiButton>("info")!.OnClick = _ =>
            {
                var d = LoadDialog("info");
                d.Find<TuiButton>("ok")!.OnClick = _ => d.Window!.OnClosed?.Invoke();
                screen.ShowWindow(d.Window!);
            };

            main.Find<TuiButton>("confirm")!.OnClick = _ =>
            {
                var d = LoadDialog("confirm");
                d.Find<TuiButton>("yes")!.OnClick = _ => { d.Window!.OnClosed?.Invoke(); messages.Text += "\n[已确认]"; };
                d.Find<TuiButton>("no")!.OnClick = _ => { d.Window!.OnClosed?.Invoke(); messages.Text += "\n[已取消]"; };
                screen.ShowWindow(d.Window!);
            };

            main.Find<TuiButton>("inputdlg")!.OnClick = _ =>
            {
                var d = LoadDialog("input");
                var inp = d.Find<TuiInput>("input")!;
                d.Find<TuiButton>("ok")!.OnClick = _ => { d.Window!.OnClosed?.Invoke(); messages.Text += "\n[输入: " + inp.Text + "]"; };
                d.Find<TuiButton>("cancel")!.OnClick = _ => d.Window!.OnClosed?.Invoke();
                screen.ShowWindow(d.Window!);
            };

            main.Find<TuiButton>("select")!.OnClick = _ =>
            {
                var d = LoadDialog("select");
                var list = d.Find<TuiList>("list")!;
                list.OnSelect = idx => { d.Window!.OnClosed?.Invoke(); messages.Text += "\n[选择: " + list.Items[idx] + "]"; };
                d.Find<TuiButton>("cancel")!.OnClick = _ => d.Window!.OnClosed?.Invoke();
                screen.ShowWindow(d.Window!);
            };

            main.Find<TuiButton>("showcase")!.OnClick = _ =>
                screen.ShowWindow(TuiMarkup.LoadFile(Path.Combine(dir, "showcase.tui")).Window!);

            main.Find<TuiButton>("permission")!.OnClick = _ =>
            {
                var d = LoadDialog("permission");
                d.Find<TuiButton>("allow")!.OnClick = _ => { d.Window!.OnClosed?.Invoke(); messages.Text += "\n[已允许]"; };
                d.Find<TuiButton>("deny")!.OnClick = _ => { d.Window!.OnClosed?.Invoke(); messages.Text += "\n[已拒绝]"; };
                d.Find<TuiButton>("always")!.OnClick = _ => { d.Window!.OnClosed?.Invoke(); messages.Text += "\n[全部允许]"; };
                screen.ShowWindow(d.Window!);
            };

            main.Find<TuiButton>("multiselect")!.OnClick = _ =>
            {
                var d = LoadDialog("multiselect");
                var list = d.Find<TuiList>("list")!;
                list.MultiSelect = true;
                d.Find<TuiButton>("ok")!.OnClick = _ => { d.Window!.OnClosed?.Invoke(); messages.Text += "\n[多选: " + list.CheckedIndices.Count + " 项]"; };
                d.Find<TuiButton>("cancel")!.OnClick = _ => d.Window!.OnClosed?.Invoke();
                screen.ShowWindow(d.Window!);
            };

            mgr.PushScreen(screen);
            mgr.Render();

            // ── 交互循环 ──
            var inputMgr = new InputManager();
            inputMgr.Init();
            bool running = true;
            while (running)
            {
                var ev = inputMgr.ReadInput(50);
                switch (ev.Type)
                {
                    case InputType.Key:
                        if (ev.KeyInfo is { Key: ConsoleKey.D, Modifiers: ConsoleModifiers.Control })
                            running = false;
                        else if (ev.KeyInfo is { Key: ConsoleKey.Escape })
                        {
                            if (screen.HasModal) mgr.OnKey(ev.KeyInfo);
                            else running = false;
                        }
                        else mgr.OnKey(ev.KeyInfo);
                        break;
                    case InputType.Mouse:
                        mgr.HandleMouse(ev);
                        break;
                    case InputType.Resize:
                        mgr.OnResize();
                        break;
                }
                mgr.Render();
            }
        }
        finally
        {
            mgr.Exit();
        }
    }

    /// <summary>从当前目录向上查找 tuidemo/ 目录。</summary>
    private static string FindTuiDemoDir()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "tuidemo");
            if (Directory.Exists(candidate)) return candidate;
            var parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        throw new DirectoryNotFoundException("未找到 tuidemo 目录（请从仓库根目录运行）");
    }
}

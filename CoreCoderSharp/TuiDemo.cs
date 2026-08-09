using CoreCoderSharp.Terminal;
using CoreCoderSharp.UI;
using CoreCoderSharp.UI.Controls;

namespace CoreCoderSharp;

/// <summary>
/// TUI 新架构演示 —— 展示五层体系的效果。
/// 运行：dotnet run -- --tui-demo
/// </summary>
public static class TuiDemo
{
    public static void Run()
    {
        var mgr = TuiManager.Instance;

        try
        {
            mgr.Enter();
            mgr.RefreshTheme();

            var screen = new ChatScreen();
            screen.StatusText = "WayCoder TUI Demo — 五层架构展示 | Ctrl+D 退出 | F1 对话框 | F2 Toast | F3 输入";
            screen.OnSubmit = text =>
            {
                // 用户消息
                screen.AddMessage(text, "user");

                // 模拟 AI 回复
                Task.Delay(300).ContinueWith(_ =>
                {
                    screen.AddMessage(
                        "这是一条 **Markdown** 格式回复。\n\n" +
                        "```csharp\npublic class Demo {\n    public void Run() => Console.WriteLine(\"Hello!\");\n}\n```\n\n" +
                        "| 特性 | 状态 |\n|------|------|\n| 拖拽 | ✅ |\n| 缩放 | ✅ |\n| Markdown | ✅ |",
                        "assistant");
                    mgr.Render();
                });
            };

            mgr.PushScreen(screen);

            // 添加欢迎消息
            screen.AddMessage("## 👋 欢迎使用 WayCoder TUI Demo\n\n" +
                "**五层架构**：TuiManager → TuiScreen → TuiWindow → TuiView → TuiControl\n\n" +
                "- `F1` — 权限确认对话框\n" +
                "- `F2` — Toast 通知\n" +
                "- `F3` — 输入对话框\n" +
                "- `F4` — 列表选择\n" +
                "- `F5` — 确认框\n" +
                "- `F6` — 短菜单（弹出式）\n" +
                "- `F7` — 长滚动菜单\n" +
                "- `F8` — 右键快捷菜单\n" +
                "- `F9` — Markdown 表格\n" +
                "- `F10` — 树形视图\n" +
                "- `F11` — 控件合集\n" +
                "- `F12` — 面板布局\n" +
                "- `Ctrl+D` 或 `Esc` — 退出\n" +
                "- 输入文字后 `Enter` 发送",
                "assistant");

            // 全局热键
            mgr.GlobalKeyHandler = key =>
            {
                // Ctrl+D 退出
                if (key is { Key: ConsoleKey.D, Modifiers: ConsoleModifiers.Control })
                {
                    return true; // 外部循环检测退出
                }

                // Esc 不拦截，交给 Screen（关闭模态窗口）→ 主循环（退出）
                if (key.Key == ConsoleKey.Escape)
                    return false;

                if (screen.HasModal) return false; // 有模态窗口时不处理 F 键

                switch (key.Key)
                {
                    case ConsoleKey.F1:
                        ShowPermissionDemo(screen);
                        return true;
                    case ConsoleKey.F2:
                        screen.ShowToast("✅ 操作已完成！", 2000);
                        return true;
                    case ConsoleKey.F3:
                        ShowInputDemo(screen);
                        return true;
                    case ConsoleKey.F4:
                        ShowListDemo(screen);
                        return true;
                    case ConsoleKey.F5:
                        ShowConfirmDemo(screen);
                        return true;
                    case ConsoleKey.F6:
                        ShowShortMenuDemo(screen);
                        return true;
                    case ConsoleKey.F7:
                        ShowLongMenuDemo(screen);
                        return true;
                    case ConsoleKey.F8:
                        ShowContextMenuDemo(screen);
                        return true;
                    case ConsoleKey.F9:
                        ShowTableDemo(screen);
                        return true;
                    case ConsoleKey.F10:
                        ShowTreeDemo(screen);
                        return true;
                    case ConsoleKey.F11:
                        ShowControlsDemo(screen);
                        return true;
                    case ConsoleKey.F12:
                        ShowPanelDemo(screen);
                        return true;
                }

                return false;
            };

            mgr.Render();

            // ── 主循环 ──
            var input = new InputManager();
            input.Init();

            bool running = true;
            while (running)
            {
                var ev = input.ReadInput(50);

                switch (ev.Type)
                {
                    case InputType.Key:
                        // Ctrl+D 直接退出
                        if (ev.KeyInfo is { Key: ConsoleKey.D, Modifiers: ConsoleModifiers.Control })
                        {
                            running = false;
                        }
                        // Esc: 优先关闭模态窗口，无模态时退出
                        else if (ev.KeyInfo is { Key: ConsoleKey.Escape })
                        {
                            if (screen.HasModal)
                                mgr.HandleKey(ev.KeyInfo);
                            else
                                running = false;
                        }
                        else
                        {
                            mgr.HandleKey(ev.KeyInfo);
                        }
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

    // ── 对话框演示 ──

    private static void ShowPermissionDemo(ChatScreen screen)
    {
        var dialog = TuiDialog.Permission("权限确认",
            "bash 工具请求执行：\n\n  rm -rf /tmp/cache/*\n\n是否允许此操作？",
            result =>
            {
                var msg = result switch
                {
                    TuiDialog.DialogResult.Yes => "✅ 已允许",
                    TuiDialog.DialogResult.No => "❌ 已拒绝",
                    _ => "✅ 全部允许"
                };
                screen.AddMessage(msg, "system");
            });
        screen.ShowWindow(dialog);
    }

    private static void ShowInputDemo(ChatScreen screen)
    {
        var dialog = TuiDialog.Input("输入对话框",
            "请输入项目名称：", "MyProject",
            text =>
            {
                screen.AddMessage($"📝 输入结果：**{text}**", "system");
            });
        screen.ShowWindow(dialog);
    }

    private static void ShowListDemo(ChatScreen screen)
    {
        var items = new List<string>
        {
            "📄 创建新文件",
            "📁 打开文件夹",
            "🔍 搜索符号",
            "🔄 重新加载项目",
            "⚙️ 打开设置",
        };
        var dialog = TuiDialog.Select("选择操作", items,
            idx =>
            {
                screen.AddMessage($"📋 选择了：**{items[idx]}**", "system");
            });
        screen.ShowWindow(dialog);
    }

    private static void ShowConfirmDemo(ChatScreen screen)
    {
        var dialog = TuiDialog.Confirm("确认删除",
            "确定要删除选中的 3 个文件吗？此操作不可撤销。",
            result =>
            {
                screen.AddMessage(result ? "🗑️ 已确认删除" : "🚫 已取消", "system");
            });
        screen.ShowWindow(dialog);
    }

    // ── 菜单演示 ──

    /// <summary>F6 — 短弹出菜单（5-6 项，含分隔线）</summary>
    private static void ShowShortMenuDemo(ChatScreen screen)
    {
        var items = new List<string>
        {
            "📄 新建文件",
            "📁 打开文件夹",
            "---",
            "🔍 查找符号",
            "🔄 重新加载",
            "⚙️ 设置...",
        };
        var win = TuiMenu.Show("快捷操作", items, 8, 4,
            idx =>
            {
                screen.AddMessage($"📋 菜单选择了：**{items[idx]}**", "system");
            },
            onCancel: () =>
            {
                screen.AddMessage("🚫 菜单已取消", "system");
            });
        screen.ShowWindow(win);
    }

    /// <summary>F7 — 长滚动菜单（25+ 项，测试滚动条）</summary>
    private static void ShowLongMenuDemo(ChatScreen screen)
    {
        var items = new List<string>();
        for (int i = 1; i <= 28; i++)
        {
            var icon = (i % 5) switch
            {
                1 => "📄", 2 => "📁", 3 => "🔍", 4 => "⚡", _ => "🔧"
            };
            items.Add($"{icon} 操作项目 第 {i} 项");
        }

        var win = TuiMenu.Show("长列表菜单 (PgUp/PgDn 滚动)", items, 10, 2,
            idx =>
            {
                screen.AddMessage($"📋 选中了第 **{idx + 1}** 项", "system");
            },
            onCancel: () =>
            {
                screen.AddMessage("🚫 长菜单已取消", "system");
            });
        screen.ShowWindow(win);
    }

    /// <summary>F8 — 右键快捷菜单（模拟上下文菜单，含多分隔线分组）</summary>
    private static void ShowContextMenuDemo(ChatScreen screen)
    {
        var items = new List<string>
        {
            "📋 复制",
            "📌 粘贴",
            "✂️ 剪切",
            "---",
            "🔍 查找定义",
            "📖 查看引用",
            "🔄 重命名符号",
            "---",
            "🧪 运行测试",
            "🐛 调试选中",
            "---",
            "🗑️ 删除此行",
            "📝 格式化文档",
        };
        // 模拟右键位置：屏幕中右区域
        var x = Math.Max(0, TTY.Cols - 30);
        var y = 5;
        var win = TuiMenu.Show("右键菜单", items, x, y,
            idx =>
            {
                screen.AddMessage($"🖱️ 右键菜单选择了：**{items[idx]}**", "system");
            },
            onCancel: () =>
            {
                screen.AddMessage("🚫 右键菜单已关闭", "system");
            });
        screen.ShowWindow(win);
    }

    /// <summary>F9 — Markdown 表格演示（含标题/代码块/表格/列表混合）</summary>
    private static void ShowTableDemo(ChatScreen screen)
    {
        screen.AddMessage(
            "## 📊 Markdown 表格渲染\n\n" +
            "WayCoder 的 Markdown 引擎支持 **完整的 GFM 表格语法**，\n" +
            "使用 Unicode 边框字符渲染。\n\n" +
            "### 语言性能对比\n\n" +
            "| 语言 | 启动速度 | 内存占用 | 类型安全 | 综合评分 |\n" +
            "|------|----------|----------|----------|----------|\n" +
            "| **C# AOT** | ⚡ 极快 | 💚 低 | ✅ 强 | **9.5/10** |\n" +
            "| Go | 快 | 低 | ✅ 强 | 9.0/10 |\n" +
            "| Rust | 快 | 极低 | ✅ 极强 | 9.3/10 |\n" +
            "| Python | 慢 | 高 | ❌ 弱 | 6.5/10 |\n" +
            "| JavaScript | 中 | 中 | ❌ 弱 | 7.0/10 |\n\n" +
            "### 模型价格表\n\n" +
            "| 模型 | 输入 $/Mtok | 输出 $/Mtok | 上下文 |\n" +
            "|------|-------------|-------------|--------|\n" +
            "| `deepseek-v4-flash` | $0.28 | $0.56 | 128K |\n" +
            "| `deepseek-v4-pro` | $1.10 | $2.20 | 256K |\n" +
            "| `gpt-5.4-mini` | $0.15 | $0.60 | 256K |\n" +
            "| `gpt-5.4` | $2.50 | $10.00 | 128K |\n\n" +
            "> 💡 提示：终端宽度不足时，表格列会自动等比缩放。",
            "assistant");
    }

    /// <summary>F10 — 树形视图演示（层级目录结构）</summary>
    private static void ShowTreeDemo(ChatScreen screen)
    {
        var tree = new TuiTreeView { Width = 45, Height = 14, X = 1, Y = 0 };

        // 项目结构
        var src = tree.AddRoot("📁 CoreCoderSharp", "📁");
        var ui = new TuiTreeNode("📁 UI", "📁");
        var controls = new TuiTreeNode("📁 TuiControls", "📁");
        var tools = new TuiTreeNode("📁 Tools", "📁");
        src.AddRange(ui, controls, tools);

        ui.Add(new("TuiManager.cs", "📄"));
        ui.Add(new("TuiScreen.cs", "📄"));
        ui.Add(new("TuiWindow.cs", "📄"));
        ui.Add(new("TuiView.cs", "📄"));

        controls.Add(new("TuiButton.cs", "📄"));
        controls.Add(new("TuiInput.cs", "📄"));
        controls.Add(new("TuiCheckbox.cs", "📄"));
        controls.Add(new("TuiComboBox.cs", "📄"));
        controls.Add(new("TuiRadioGroup.cs", "📄"));
        controls.Add(new("TuiSeekBar.cs", "📄"));
        controls.Add(new("TuiTreeView.cs", "📄"));
        controls.Add(new("TuiPanel.cs", "📄"));
        controls.Add(new("TuiSeparator.cs", "📄"));

        tools.Add(new("BashTool.cs", "📄"));
        tools.Add(new("ReadFileTool.cs", "📄"));
        tools.Add(new("WriteFileTool.cs", "📄"));
        tools.Add(new("EditFileTool.cs", "📄"));

        src.AddRange(
            new("Program.cs", "📄"),
            new("Agent.cs", "📄"),
            new("Config.cs", "📄"),
            new("SelfTest.cs", "📄")
        );

        // 展开第一层
        src.IsExpanded = true;

        tree.OnNodeActivated = node =>
        {
            screen.AddMessage($"📌 选中: **{node.Text}** (子节点: {node.Children.Count})", "system");
        };

        var rootView = new TuiVBox();
        rootView.Add(tree);

        var win = new TuiWindow
        {
            Title = "🌲 树形视图 — F10 演示",
            RootView = rootView,
            Width = 49, Height = 17,
            X = 4, Y = 2,
            Modal = true, HasMask = false,
            Border = WindowBorder.Rounded,
            BorderColor = 33, WinBg = 7,
        };
        win.RegisterShortcut(ConsoleKey.Escape, () => win.OnClosed?.Invoke());
        screen.ShowWindow(win);
    }

    /// <summary>F11 — 控件合集演示（RadioGroup + ComboBox + SeekBar + Checkbox）</summary>
    private static void ShowControlsDemo(ChatScreen screen)
    {
        // 使用 VBox 布局
        var vbox = new TuiVBox { Width = 44, Height = 19, X = 1, Y = 0 };

        // 单选按钮组
        var radio = new TuiRadioGroup(["🔵 深海蓝", "🟢 翡翠绿", "🟠 琥珀橙", "🟣 薰衣紫"], 0)
        { Height = 4, Width = 40 };
        vbox.Add(radio);

        vbox.Add(new TuiSeparator { Width = 42 });

        // 组合框
        var combo = new TuiComboBox(["C# (.NET 10)", "Python 3.12", "Rust 1.85", "TypeScript 5.7", "Go 1.24"], 0)
        { Width = 36 };
        vbox.Add(combo);

        vbox.Add(new TuiSeparator { Width = 42 });

        // 滑块
        var seek = new TuiSeekBar(0, 100, 75) { Width = 40 };
        vbox.Add(seek);

        vbox.Add(new TuiSeparator { Width = 42 });

        // 复选框
        var cb1 = new TuiCheckbox("✅ 启用 AOT 编译", true);
        var cb2 = new TuiCheckbox("✅ 启用 Watch 模式", false);
        var cb3 = new TuiCheckbox("✅ 启用 Diff 预览", true);
        vbox.Add(cb1);
        vbox.Add(cb2);
        vbox.Add(cb3);

        vbox.Add(new TuiSeparator { Width = 42 });

        // 按钮
        var btn = new TuiButton("  应用设置  ") { Fg = 30, Bg = 46 };
        btn.OnClick = _ =>
        {
            var labels = new[] { "深海蓝", "翡翠绿", "琥珀橙", "薰衣紫" };
            screen.AddMessage(
                $"### ⚙️ 设置已应用\n\n" +
                $"- 主题：**{labels[radio.SelectedIndex]}**\n" +
                $"- 语言：**{combo.Options[combo.SelectedIndex]}**\n" +
                $"- 音量：**{seek.Value}/100**\n" +
                $"- AOT：**{(cb1.Checked ? "开" : "关")}**  Watch：**{(cb2.Checked ? "开" : "关")}**  Diff：**{(cb3.Checked ? "开" : "关")}**",
                "assistant");
        };
        vbox.Add(btn);

        var win = new TuiWindow
        {
            Title = "🎛️ 控件合集 — F11 演示",
            RootView = vbox,
            Width = 46, Height = 21,
            X = 4, Y = 1,
            Modal = true, HasMask = false,
            Border = WindowBorder.Rounded,
            BorderColor = 35, WinBg = 7,
        };
        win.RegisterShortcut(ConsoleKey.Escape, () => win.OnClosed?.Invoke());
        screen.ShowWindow(win);
        vbox.FocusNext(); // 给第一个控件焦点
    }

    /// <summary>F12 — 面板布局演示（嵌套 Panel）</summary>
    private static void ShowPanelDemo(ChatScreen screen)
    {
        // 外层面板
        var outer = new TuiPanel
        {
            Title = "📦 外层容器",
            Width = 48, Height = 16,
            X = 0, Y = 0,
            BorderStyle = WindowBorder.Double,
            BorderColor = 33
        };

        // 内部水平布局 — 两个并列面板
        var left = new TuiPanel
        {
            Title = "📋 列表",
            Width = 20, Height = 8,
            X = 1, Y = 2,
            BorderColor = 36
        };
        var listView = new TuiListView { Width = 17, Height = 5, X = 1, Y = 0 };
        left.Add(listView);

        var right = new TuiPanel
        {
            Title = "📊 进度",
            Width = 22, Height = 8,
            X = 23, Y = 2,
            BorderColor = 35
        };
        var progressLabel = new TuiLabel("████████░░░ 67%") { X = 1, Y = 1, Fg = 32 };
        right.Add(progressLabel);

        // 底部面板
        var bottom = new TuiPanel
        {
            Title = "📝 日志",
            Width = 44, Height = 4,
            X = 1, Y = 11,
            BorderColor = 90
        };
        var log = new TuiLabel("[2026-08-09 14:32:01] ✅ 编译成功") { X = 1, Y = 0, Fg = 32 };
        bottom.Add(log);

        outer.Add(left);
        outer.Add(right);
        outer.Add(bottom);

        var win = new TuiWindow
        {
            Title = "🗂️ 面板布局 — F12 演示",
            RootView = outer,
            Width = 50, Height = 18,
            X = 4, Y = 1,
            Modal = true, HasMask = false,
            Border = WindowBorder.Rounded,
            BorderColor = 33, WinBg = 7,
        };
        win.RegisterShortcut(ConsoleKey.Escape, () => win.OnClosed?.Invoke());
        screen.ShowWindow(win);
    }
}

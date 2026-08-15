using WayCoder.Terminal;
using WayCoder.UI;
using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

namespace WayCoder;

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
            screen.StatusText = "WayCoder TUI Demo | F1-F12 控件 | /m /s /r /c /f /b 全屏 | /types /multi /diff 内容类型 | Esc 退出";
            screen.OnSubmit = text =>
            {
                // ── Slash 命令：打开全屏对话框 ──
                var trimmed = text.Trim();
                switch (trimmed.ToLowerInvariant())
                {
                    case "/m": case "/model":
                        ShowModelPickerDemo(screen); return;
                    case "/s": case "/session":
                        ShowSessionPickerDemo(screen); return;
                    case "/r": case "/reasoning":
                        ShowReasoningPickerDemo(screen); return;
                    case "/c": case "/command":
                        ShowCommandPaletteDemo(screen); return;
                    case "/f": case "/file":
                        ShowFilePickerDemo(screen); return;
                    case "/b": case "/buttons":
                        ShowButtonGroupDemo(screen); return;
                    case "/types": case "/l":
                        ShowChatContentTypesDemo(screen); return;
                    case "/multi":
                        ShowMultiSelectDemo(screen); return;
                    case "/diff":
                        ShowDiffDemo(screen); return;
                }

                // 注：ChatScreen 已通过 HandleSpecial 添加了用户消息（AddUserMsg），
                // 此处仅模拟 AI 回复，不重复添加用户消息。

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
                "### 基础控件\n" +
                "- `F1` — 权限确认  `F2` — Toast  `F3` — 输入框\n" +
                "- `F4` — 列表选择  `F5` — 确认框\n" +
                "- `F6` — 短菜单  `F7` — 长滚动菜单  `F8` — 右键菜单\n" +
                "- `F9` — Markdown 表格  `F10` — 树形视图\n" +
                "- `F11` — 控件合集  `F12` — 面板布局\n\n" +
                "### 全屏对话框（输入框输入命令，Enter 打开）\n" +
                "- `/m` 或 `/model` — 模型选择器\n" +
                "- `/s` 或 `/session` — 会话管理器\n" +
                "- `/r` 或 `/reasoning` — 推理深度\n" +
                "- `/c` 或 `/command` — 命令面板\n" +
                "- `/f` 或 `/file` — 文件选择器\n" +
                "- `/b` 或 `/buttons` — 按钮组+滚动条\n\n" +
                "### 聊天内容类型（4 角色 × 多类型）\n" +
                "- `/types` 或 `/l` — system/user/agent/tool 内容类型一览\n" +
                "- `/multi` — 会话提问（多选）\n" +
                "- `/diff` — 代码对比（逐 hunk 确认）\n\n" +
                "`Esc` — 退出  |  输入文字后 `Enter` 发送",
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

                if (screen.HasModal) return false; // 有模态窗口时不处理

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
                                mgr.OnKey(ev.KeyInfo);
                            else
                                running = false;
                        }
                        else
                        {
                            mgr.OnKey(ev.KeyInfo);
                        }
                        break;

                    case InputType.Mouse:
                        mgr.HandleMouse(ev);
                        break;

                    case InputType.Paste:
                        if (!string.IsNullOrEmpty(ev.PasteText))
                            screen.HandleBracketedPaste(ev.PasteText);
                        break;

                    case InputType.Resize:
                        mgr.OnResize();
                        break;
                }

                mgr.Render();

                // 处理待提交消息（Enter 后 ChatScreen 会将文本入队到此队列）
                while (screen.PendingSubmissions.TryDequeue(out var submitted))
                {
                    screen.OnSubmit?.Invoke(submitted);
                }
            }
        }
        finally
        {
            mgr.Exit();
        }
    }

    // ── 对话框演示 ──

    internal static void ShowPermissionDemo(ChatScreen screen)
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

    internal static void ShowInputDemo(ChatScreen screen)
    {
        var dialog = TuiDialog.Input("输入对话框",
            "请输入项目名称：", "MyProject",
            text =>
            {
                screen.AddMessage($"📝 输入结果：**{text}**", "system");
            });
        screen.ShowWindow(dialog);
    }

    internal static void ShowListDemo(ChatScreen screen)
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

    internal static void ShowConfirmDemo(ChatScreen screen)
    {
        var dialog = TuiDialog.Confirm("确认删除",
            "确定要删除选中的 3 个文件吗？此操作不可撤销。",
            result =>
            {
                screen.AddMessage(result ? "🗑️ 已确认删除" : "🚫 已取消", "system");
            });
        screen.ShowWindow(dialog);
    }

    internal static void ShowFindReplaceDemo(ChatScreen screen)
    {
        var dialog = TuiDialog.FindReplace("foo", "bar", new FindOptions(),
            (find, opts) => screen.AddMessage($"🔍 查找: {find}", "system"),
            (find, repl, opts) => screen.AddMessage($"✏️ 替换: {find} → {repl}", "system"),
            (find, repl, opts) => screen.AddMessage($"🔄 全部替换: {find} → {repl}", "system"));
        screen.ShowWindow(dialog);
    }

    // ── 菜单演示 ──

    /// <summary>F6 — 短弹出菜单（5-6 项，含分隔线）</summary>
    internal static void ShowShortMenuDemo(ChatScreen screen)
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
    internal static void ShowLongMenuDemo(ChatScreen screen)
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
    internal static void ShowContextMenuDemo(ChatScreen screen)
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
        var x = Math.Max(0, Tty.Cols - 30);
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
    internal static void ShowTableDemo(ChatScreen screen)
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
    internal static void ShowTreeDemo(ChatScreen screen)
    {
        var tree = new TuiTreeView { Width = 45, Height = 14, X = 1, Y = 0 };

        // 项目结构
        var src = tree.AddRoot("📁 WayCoder", "📁");
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
    internal static void ShowControlsDemo(ChatScreen screen)
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
    internal static void ShowPanelDemo(ChatScreen screen)
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

    // ════════════════════════════════════════════════════════════
    // 全屏 ANSI 对话框（对标 Crush）—— Ctrl+字母 触发
    // ════════════════════════════════════════════════════════════

    /// <summary>Ctrl+M — 模型选择对话框（对标 Crush models.go）</summary>
    internal static void ShowModelPickerDemo(ChatScreen screen)
    {
        var result = ModelPicker.Show();
if (result != null)
            screen.AddMessage($"🔄 选择了 **{(result.IsLarge ? "大模型" : "小模型")}** → `{result.ModelId}`", "system");
        else
            screen.AddMessage("🚫 模型选择已取消", "system");
    }

    /// <summary>Ctrl+S — 会话管理对话框（对标 Crush sessions.go）</summary>
    internal static void ShowSessionPickerDemo(ChatScreen screen)
    {
        var result = SessionPicker.Show();
if (result == null)
        {
            screen.AddMessage("🚫 会话管理已取消", "system");
            return;
        }
        var actionText = result.Action switch
        {
            "switch" => "切换",
            "rename" => $"重命名 → {result.NewName}",
            "delete" => "删除",
            _ => result.Action
        };
        screen.AddMessage($"📂 会话操作：**{actionText}** — `{result.SessionId}`", "system");
    }

    /// <summary>Ctrl+G — 推理深度选择器（对标 Crush reasoning.go）</summary>
    internal static void ShowReasoningPickerDemo(ChatScreen screen)
    {
        var result = ReasoningPicker.Show(currentLevel: "", modelName: "deepseek-v4-pro");
if (result != null)
        {
            var levelText = string.IsNullOrEmpty(result.Level) ? "默认（清除）" : result.Level;
            screen.AddMessage($"🧠 推理深度 → **{levelText}**", "system");
        }
        else
            screen.AddMessage("🚫 推理深度选择已取消", "system");
    }

    /// <summary>Ctrl+P — 命令面板（对标 Crush command palette）</summary>
    internal static void ShowCommandPaletteDemo(ChatScreen screen)
    {
        var commands = new List<CommandPalette.Command>
        {
            new("model", "🤖 切换模型", "模型", "Ctrl+M", "打开模型选择对话框",
                () => screen.AddMessage("📋 执行：切换模型", "system")),
            new("session", "📂 管理会话", "会话", "Ctrl+S", "打开会话管理器",
                () => screen.AddMessage("📋 执行：管理会话", "system")),
            new("reasoning", "🧠 推理深度", "模型", "Ctrl+G", "设置推理深度",
                () => screen.AddMessage("📋 执行：推理深度", "system")),
            new("file", "📁 打开文件", "文件", "Ctrl+O", "选择并打开文件",
                () => screen.AddMessage("📋 执行：打开文件", "system")),
            new("save", "💾 保存会话", "文件", "Ctrl+S", "保存当前会话到磁盘",
                () => screen.AddMessage("📋 执行：保存会话", "system")),
            new("compact", "🗜️ 压缩上下文", "工具", "", "压缩对话历史释放 Token",
                () => screen.AddMessage("📋 执行：压缩上下文", "system")),
            new("diff", "📊 查看差异", "工具", "", "显示当前变更的 diff",
                () => screen.AddMessage("📋 执行：查看差异", "system")),
            new("help", "❓ 帮助", "帮助", "Ctrl+H", "显示使用帮助",
                () => screen.AddMessage("📋 执行：帮助", "system")),
            new("quit", "🚪 退出", "系统", "Ctrl+Q", "退出 WayCoder",
                () => screen.AddMessage("📋 执行：退出", "system")),
            new("longlabel", "🔧 这条命令标签故意写得特别长用于验证溢出截断", "测试",
                "Ctrl+Shift+L", "这是一条同样非常长的描述文本，用来验证在较窄终端下标签、描述与快捷键三者都能正确截断而不撑破边框。",
                () => screen.AddMessage("📋 执行：长标签命令", "system")),
        };
        CommandPalette.Show(commands);
        screen.InvalidateView(); // 全屏 ANSI 对话框覆盖了 TUI 画面，强制全刷新
    }

    /// <summary>Ctrl+F — 文件选择器（对标 Crush filepicker）</summary>
    internal static void ShowFilePickerDemo(ChatScreen screen)
    {
        var result = FilePicker.Show(
            startDir: Environment.CurrentDirectory,
            filter: null,
            title: "选择文件 — TUI Demo");
if (result != null)
            screen.AddMessage($"📁 选择了文件：`{result}`", "system");
        else
            screen.AddMessage("🚫 文件选择已取消", "system");
    }

    // ════════════════════════════════════════════════════════════
    // TUI 控件对话框 —— Ctrl+B 触发
    // ════════════════════════════════════════════════════════════

    /// <summary>Ctrl+B — 按钮组 + 滚动条演示（对标 Crush button.go/scrollbar.go）</summary>
    internal static void ShowButtonGroupDemo(ChatScreen screen)
    {
        var vbox = new TuiVBox { Width = 46, Height = 22, X = 1, Y = 0 };

        var label1 = new TuiLabel("水平按钮组（Tab 导航 / 字母快捷键）：") { Fg = 37 };
        vbox.Add(label1);

        var hGroup = new TuiButtonGroup { Direction = TuiButtonGroup.LayoutMode.Horizontal, Width = 44, Height = 3 };
        hGroup.Add("编译 (C)", onClick: _ => screen.AddMessage("🔨 **编译**", "system"));
        hGroup.Add("运行 (R)", onClick: _ => screen.AddMessage("▶️ **运行**", "system"));
        hGroup.Add("测试 (T)", onClick: _ => screen.AddMessage("🧪 **测试**", "system"));
        hGroup.Add("调试 (D)", onClick: _ => screen.AddMessage("🐛 **调试**", "system"));
        vbox.Add(hGroup);

        vbox.Add(new TuiSeparator { Width = 44 });

        var label2 = new TuiLabel("垂直按钮组（↑↓ 导航 / Enter 确认）：") { Fg = 37 };
        vbox.Add(label2);

        var vGroup = new TuiButtonGroup { Direction = TuiButtonGroup.LayoutMode.Vertical, Width = 44, Height = 6 };
        vGroup.Add("📄 新建文件", onClick: _ => screen.AddMessage("📄 **新建文件**", "system"));
        vGroup.Add("📁 打开文件夹", onClick: _ => screen.AddMessage("📁 **打开文件夹**", "system"));
        vGroup.Add("💾 全部保存", onClick: _ => screen.AddMessage("💾 **全部保存**", "system"));
        vGroup.Add("🗑️ 删除选中", onClick: _ => screen.AddMessage("🗑️ **删除选中**", "system"));
        vbox.Add(vGroup);

        vbox.Add(new TuiSeparator { Width = 44 });

        var label3 = new TuiLabel("独立滚动条（拖拽滑块 / 鼠标滚轮）：") { Fg = 37 };
        vbox.Add(label3);

        var allLines = new[]
        {
            "  📄 第 1 行：项目概览", "  📄 第 2 行：架构设计文档",
            "  📄 第 3 行：API 接口规范", "  📄 第 4 行：数据库设计",
            "  📄 第 5 行：部署运维指南", "  📄 第 6 行：测试用例清单",
            "  📄 第 7 行：变更日志", "  📄 第 8 行：性能基准报告",
            "  📄 第 9 行：安全审计结果", "  📄 第 10 行：团队协作规范",
            "  📄 第 11 行：FAQ 常见问题", "  📄 第 12 行：贡献者指南",
        };

        var contentLabel = new TuiLabel(
            string.Join("\n", allLines.Take(7))) { Width = 40, Height = 7, Fg = 90 };

        var scrollRow = new TuiHBox { Width = 44, Height = 7 };
        scrollRow.Add(contentLabel);

        var scrollbar = new TuiScrollbar
            { ContentHeight = allLines.Length, ViewportHeight = 7, ScrollOffset = 0, Width = 1, Height = 7 };
        scrollbar.OnScroll = pos =>
        {
            var visible = allLines.Skip(pos).Take(7).ToList();
            while (visible.Count < 7) visible.Add("");
            contentLabel.Text = string.Join("\n", visible);
        };
        scrollRow.Add(scrollbar);
        vbox.Add(scrollRow);

        var win = new TuiWindow
        {
            Title = "🔘 按钮组 + 滚动条 — Ctrl+B 演示",
            RootView = vbox, Width = 48, Height = 24,
            X = 2, Y = 1,
            Modal = true, HasMask = false,
            Border = WindowBorder.Rounded,
            BorderColor = 36, WinBg = 7,
        };
        win.RegisterShortcut(ConsoleKey.Escape, () => win.OnClosed?.Invoke());
        screen.ShowWindow(win);
        hGroup.Focused = true; // 给首个按钮组初始焦点，使方向键 / Enter / Space 立即可用
    }

    // ═══════════════════════════════════════════════════════════════
    // 聊天内容类型 × 角色 演示 —— /types 触发
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// /types — 一次性输出聊天 listitem 支持的全部内容类型 × 4 种角色，
    /// 用于肉眼核对渲染效果（对标 Crush/Claude Code 的 item 内容类型）。
    /// </summary>
    internal static void ShowChatContentTypesDemo(ChatScreen screen)
    {
        // ① system —— 纯文本（系统消息）
        screen.AddMessage("⚙ 会话已保存 · 上下文压缩至 50% · 模型 deepseek-v4-pro", "system");

        // ② user —— Markdown（用户消息，含行内格式）
        screen.AddMessage(
            "请帮我在 **src/** 目录下新增一个 `健康检查` 接口，\n" +
            "返回 `{\"status\":\"ok\"}`，并用任务清单列出验收项。", "user");

        // ③ assistant —— 完整 Markdown（标题/任务清单/表格/分割线/引用/链接/删除线）
        screen.AddMessage(
            "## ✅ 已完成\n\n" +
            "### 改动清单\n" +
            "- [x] 新增 `HealthController.cs`\n" +
            "- [x] 注册 `/api/health` 路由\n" +
            "- [ ] 补充单元测试（~~下周三~~ 本周末前）\n\n" +
            "| 端点 | 方法 | 状态 |\n|------|------|------|\n" +
            "| `/api/health` | GET | ✅ |\n" +
            "| `/api/ready` | GET | 🚧 |\n\n" +
            "---\n\n" +
            "> 💡 提示：请运行 `/test` 验证。\n\n" +
            "参考 [WayCoder 安装与升级](https://gitee.com/aleckstygit/my-coder)，~~旧链接已废弃~~。", "assistant");

        // ④ assistant —— 单代码块（行号 + 语法高亮）
        screen.AddMessage(
            "```csharp\n" +
            "public class HealthController : ControllerBase\n" +
            "{\n" +
            "    [HttpGet(\"/api/health\")]\n" +
            "    public IActionResult Get()\n" +
            "        => Ok(new { status = \"ok\", now = DateTime.UtcNow });\n" +
            "}\n" +
            "```", "assistant");

        // ⑤ tool —— 控制台文本（ANSI 彩色，模拟 bash 输出）→ 嵌套子消息（indent=1，续接无角色头+左缩进）
        screen.AddMessage(
            "[1;36m$ dotnet build[0m\n" +
            "[32m  生成成功[0m，0 警告 0 错误\n" +
            "[1;36m$ dotnet test[0m\n" +
            "[32m  通过！1613/1613[0m", "tool", indent: 1);

        // ⑥ tool —— 错误输出（红色）→ 嵌套子消息
        screen.AddMessage(
            "[1;31merror CS1002:[0m 应输入 ;\n" +
            "[1;31m  →[0m HealthController.cs(12,34)", "tool", indent: 1);
    }

    /// <summary>
    /// /multi — 会话提问「多选」对话框（对标 ask_user_question 的 multi_select）
    /// </summary>
    internal static void ShowMultiSelectDemo(ChatScreen screen)
    {
        var items = new List<string>
        {
            "🔵 深海蓝主题",
            "🟢 翡翠绿主题",
            "🟠 琥珀橙主题",
            "🟣 薰衣紫主题",
            "🖤 暗黑模式",
        };
        var win = TuiDialog.MultiSelect("多选：选择要启用的主题（空格勾选）", items,
            onConfirm: picked =>
            {
                if (picked.Count == 0)
                    screen.AddMessage("🚫 未选择任何项", "system");
                else
                {
                    var names = picked.Select(i => items[i]).ToArray();
                    screen.AddMessage($"✅ 多选结果（{names.Length} 项）：**{string.Join("、", names)}**", "system");
                }
            },
            onCancel: () => screen.AddMessage("🚫 多选已取消", "system"));
        screen.ShowWindow(win);
    }

    /// <summary>
    /// /diff — 代码对比（DiffPreview 全屏逐 hunk 确认）
    /// </summary>
    internal static void ShowDiffDemo(ChatScreen screen)
    {
        const string oldContent =
            "public class HealthController\n" +
            "{\n" +
            "    public string Get()\n" +
            "    {\n" +
            "        return \"old\";\n" +
            "    }\n" +
            "}\n";

        const string newContent =
            "public class HealthController : ControllerBase\n" +
            "{\n" +
            "    public string Get()\n" +
            "    {\n" +
            "        return \"new\";\n" +
            "    }\n" +
            "\n" +
            "    public string Ready() => \"ready\";\n" +
            "}\n";

        var result = DiffPreview.Show(oldContent, newContent, "HealthController.cs");
        screen.InvalidateView(); // 全屏 diff 覆盖了 TUI 画面，强制全刷新

        var msg = result.Decision switch
        {
            DiffPreview.Decision.AcceptAll => "✅ 已接受全部变更",
            DiffPreview.Decision.RejectAll => "❌ 已拒绝全部变更",
            _ => $"🔍 部分接受（{result.AcceptedHunks?.Count ?? 0} 个 hunk）"
        };
        screen.AddMessage(msg, "system");
    }
}

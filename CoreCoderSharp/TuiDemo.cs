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

                if (screen.HasModal) return false; // 有模态窗口时不处理 F1-F5

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
}

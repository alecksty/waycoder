using WayCoder.UI.TUI.Base;
using WayCoder.UI.TUI.Custom;
using WayCoder.UI.Tui;
using WayCoder.UI.Tui.Controls;
using WayCoder.UI.Tui.Screens;

namespace WayCoder;

/// <summary>
/// 对话框巡检 —— 在运行中的 REPL 里把所有对话框挨个过一遍，肉眼核对排版/按钮/快捷键。
///
/// 与既有手段的分工：
///   /test dialog        本文件，交互式逐个弹（关一个自动弹下一个），结果回写聊天流
///   waycoder --tui-audit  非交互，把每个对话框渲染成纯文本帧输出，适合 diff 回归
///   waycoder --tui-demo   独立演示进程，F1-F12 手动点，不占用当前会话
///   waycoder --test       断言级自测，抓「按钮被挤出内容区」这类结构问题
///
/// 实现要点：TuiDialog 系对话框都在关闭时调 <c>win.OnClosed</c>（Esc 走
/// <c>TuiScreen.OnKey</c> 也会触发），且 <c>ShowWindow</c> 会保留调用方设的 OnClosed，
/// 所以「下一个」直接挂在 OnClosed 上就能串成链，不需要额外的状态机。
/// 全屏 ANSI 选择器（模型/会话/推理/命令面板/文件）是阻塞式的（自带 RenderWait 泵），
/// 串不进回调链，因此放在链之前顺序跑完 —— 与 /model、/session 的调用方式一致。
/// </summary>
public static class DialogWalk
{
    /// <summary>可单独指定的目标名（/test dialog &lt;名&gt;）。</summary>
    public static readonly string[] Targets =
    [
        "info", "success", "warn", "error", "confirm", "confirm3",
        "input", "inputline", "secret", "select", "multiselect", "ask", "askmulti",
        "perm", "toast", "menu",
        "model", "session", "reasoning", "palette", "file",
    ];

    /// <summary>
    /// 巡检对话框。<paramref name="only"/> 为空=全部走一遍，否则只弹指定的那个。
    /// </summary>
    public static void Run(ChatScreen screen, string only = "")
    {
        only = only.Trim().ToLowerInvariant();
        bool all = only.Length == 0 || only is "all" or "全部";

        if (!all && Array.IndexOf(Targets, only) < 0)
        {
            screen.AddMessage($"未知对话框「{only}」。可选：{string.Join("、", Targets)}", "tool");
            return;
        }

        // ── 阻塞式全屏选择器：先跑完（它们自带渲染泵，串不进回调链）──
        foreach (var name in Targets)
        {
            if (!IsBlocking(name)) continue;
            if (!all && name != only) continue;
            try { screen.AddMessage($"▶ {Title(name)} — {ShowBlocking(name)}", "tool"); }
            catch (Exception ex) { screen.AddMessage($"✗ {Title(name)} 异常：{ex.Message}", "tool"); }
        }

        // ── 窗口式对话框：OnClosed 串成链，关一个弹下一个 ──
        var queue = new List<string>();
        foreach (var name in Targets)
            if (!IsBlocking(name) && (all || name == only))
                queue.Add(name);
        if (queue.Count == 0) return;

        if (all)
            screen.AddMessage(
                $"开始对话框巡检：共 {queue.Count} 个窗口对话框，"
                + "关掉一个自动弹下一个，Esc 也算关闭（会记为取消）。",
                "tool");

        int idx = 0;
        void ShowNext()
        {
            if (idx >= queue.Count)
            {
                if (all) screen.AddMessage("✅ 对话框巡检结束。", "tool");
                return;
            }
            var name = queue[idx++];
            try
            {
                var win = Build(name, screen, r => screen.AddMessage($"▶ {Title(name)} — {r}", "tool"));
                if (win == null) { ShowNext(); return; }   // toast/menu 自己弹，直接进下一个
                // 必须在 ShowWindow 之前挂：ShowWindow 会把它包进「先 CloseWindow 再回调」的壳里
                win.OnClosed = ShowNext;
                screen.ShowWindow(win);
            }
            catch (Exception ex)
            {
                screen.AddMessage($"✗ {Title(name)} 构建异常：{ex.Message}", "tool");
                ShowNext();
            }
        }

        ShowNext();
    }

    // ── 分类 ──

    private static bool IsBlocking(string name)
        => name is "model" or "session" or "reasoning" or "palette" or "file";

    private static string Title(string name) => name switch
    {
        "info" => "信息框 Info", "success" => "成功框 Success",
        "warn" => "警告框 Warn", "error" => "错误框 Error",
        "confirm" => "确认框 Confirm", "confirm3" => "三选确认 Confirm3",
        "input" => "多行输入 Input", "inputline" => "单行输入 InputLine",
        "secret" => "密钥输入 Secret", "select" => "单选列表 Select",
        "multiselect" => "多选列表 MultiSelect", "ask" => "提问单选 Ask",
        "askmulti" => "提问多选 Ask(multi)", "perm" => "权限确认 Permission",
        "toast" => "浮动提示 Toast", "menu" => "弹出菜单 Menu",
        "model" => "模型选择器", "session" => "会话管理器",
        "reasoning" => "推理深度", "palette" => "命令面板", "file" => "文件选择器",
        _ => name,
    };

    // ── 阻塞式全屏选择器 ──

    private static string ShowBlocking(string name) => name switch
    {
        "model" => ModelPicker.Show() is { } m ? $"选中 {m.ModelId}" : "取消",
        "session" => SessionPicker.Show() is { } s ? $"选中 {s.SessionId}" : "取消",
        "reasoning" => ReasoningPicker.Show(null, "deepseek-v4-pro") is { } r ? $"选中 {r.Level}" : "取消",
        "palette" => CommandPalette.Show(SampleCommands()) ? "执行了命令" : "取消",
        "file" => FilePicker.Show(Environment.CurrentDirectory, null, "选择文件") is { } f ? f : "取消",
        _ => "跳过",
    };

    private static List<CommandPalette.Command> SampleCommands() =>
    [
        new("new", "新建会话", "会话", "Ctrl+N", "开一个空会话", () => { }),
        new("save", "保存会话", "会话", "Ctrl+S", "把当前对话存盘", () => { }),
        new("model", "切换模型", "模型", "Ctrl+M", "打开模型选择器", () => { }),
        new("theme", "切换主题", "外观", "", "在配色间轮换", () => { }),
        new("quit", "退出", "系统", "Ctrl+D", "结束当前会话", () => { }),
    ];

    // ── 窗口式对话框（返回 null = 自己弹完了，无需入链）──

    /// <summary>
    /// 按名构建对话框窗口。也是自测入口 —— 逐个构建能抓出「改了 .tui 里的 id 但没改 code-behind」
    /// 这类只在弹窗那一刻才炸的问题（LoadDialog 找不到控件会直接抛）。
    /// </summary>
    public static TuiWindow? Build(string name, ChatScreen screen, Action<string> report)
    {
        var items = new List<string> { "苹果", "香蕉", "橙子", "西瓜", "葡萄" };
        var poem = "床前明月光，疑是地上霜。\n举头望明月，低头思故乡。";

        switch (name)
        {
            case "info":
                return TuiDialog.Info("信息", "这是一条信息提示，用来核对单按钮消息框的换行与按钮位置。");
            case "success":
                return TuiDialog.Success("成功", "编译通过，3661 项自测全部通过。");
            case "warn":
                return TuiDialog.Warn("警告", "该操作会覆盖已有文件，请确认后再继续。");
            case "error":
                return TuiDialog.Error("错误", "连接超时：无法访问 api.example.com（重试 3 次后放弃）。");
            case "confirm":
                return TuiDialog.Confirm("确认", "是否继续执行该命令？", r => report(r ? "是" : "否"));
            case "confirm3":
                return TuiDialog.Confirm3("保存修改", "文件已修改，是否保存？", r => report(r.ToString()));
            case "input":
                return TuiDialog.Input("多行输入", "请输入提交说明：", "fix: ",
                    t => report($"输入「{t}」"), () => report("取消"));
            case "inputline":
                return TuiDialog.InputLine("单行输入", "分支名：", "feature/",
                    t => report($"输入「{t}」"), () => report("取消"));
            case "secret":
                return TuiDialog.Secret("输入密钥", "API Key：", "",
                    t => report($"长度 {t.Length} 的密钥"), () => report("取消"));
            case "select":
                return TuiDialog.Select("选一个水果", items,
                    i => report($"选中「{items[i]}」"), () => report("取消"));
            case "multiselect":
                return TuiDialog.MultiSelect("选几个水果", items,
                    set => report(set.Count == 0 ? "未选" : string.Join("、", set.Select(i => items[i]))),
                    () => report("取消"));
            case "ask":
                return TuiDialog.Ask("静夜思", poem, ["李白", "杜甫", "白居易"], false,
                    i => report($"选中第 {i + 1} 项"), _ => { }, () => report("取消"));
            case "askmulti":
                return TuiDialog.Ask("静夜思", poem, ["写景", "思乡", "送别", "咏史"], true,
                    _ => { }, set => report($"选了 {set.Count} 项"), () => report("取消"));
            case "perm":
                return TuiDialog.Permission("权限确认",
                    "命令：rm -rf /tmp/build\n目录：/home/user/project", r => report(r.ToString()));

            // 下面两个不是窗口：自己弹完就算过，链上直接跳到下一个
            case "toast":
                screen.ShowToast("✅ 浮动提示（2 秒自动消失）", 2000);
                report("已弹出");
                return null;
            case "menu":
                screen.ShowMenu("测试菜单", ["选项 A", "选项 B", "选项 C"]);
                report("已弹出");
                return null;
        }

        return null;
    }
}

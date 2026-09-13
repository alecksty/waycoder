using System.Text;
using WayCoder.UI.Shared.Terminal;
using WayCoder.Tools;
using WayCoder.UI.Tui.Controls;

using WayCoder.UI.Shared;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.TUI.Renderers;

namespace WayCoder.UI.Tui.Screens;


/// <summary>
/// 聊天 REPL 屏幕 —— 主交互界面。
///
/// 布局结构：
///   RootView (VBox)
///   ├─ StatusBar     TuiLabel       顶行状态栏
///   ├─ ChatList      TuiListView    聊天历史（每项为 TuiMarkdown）
///   ├─ SuggestPanel  TuiVBox        建议下拉（浮层，默认隐藏）
///   └─ InputArea     TuiTextArea    多行输入区
///
/// 可选右侧面板（SidePanel）和浮层窗口（对话框/Toast）。
/// </summary>
public partial class ChatScreen : TuiScreen
{

    // ── 高级操作 ──

    /// <summary>添加工具调用进度（嵌套子消息：工具输出归属在 assistant 消息下）。线程安全。</summary>
    public void AddToolProgress(string toolName, string brief)
    {
        var renderer = ToolRendererFactory.Get(toolName);
        string label = $"  {renderer.FormatHeader(brief)}";
        lock (_chatLock)
        {
            // 参数摘要按聊天区宽度截取（减一点留边距），不再依赖调用方提前砍短 ——
            // 之前调用方硬截 57 字符，bash 命令/文件路径一眼看不全参数。
            // 注意：bash 走 shellBlock 渲染会再前缀「│ 」（2 显示列），标题可用宽必须再减 2，
            // 否则长命令头超出 item 体内宽被右裁 2 列。item 体内宽 = (list-2)-padding(1+1) = list-4，故用 list-6。
            int avail = Math.Max(30, ChatList.Width - 6);
            if (AnsiHelper.DisplayWidth(label) > avail)
                label = AnsiHelper.TruncateByWidth(label, avail);
            var msg = new ChatMsg { Role = "tool", Content = label, Indent = 1, ShellBlock = toolName == "bash" };
            ChatMessages.Add(msg);
            // bash 输出走等宽竖线控制台块（模拟终端滚动区），其他工具保持普通纯文本。
            // ShellBlock 持久化到 ChatMsg：切换槽位/恢复会话重放时忠实重建竖线 gutter。
            AddMessage(label, "tool", indent: 1, shellBlock: toolName == "bash");
        }
        _toolOutputLineCount = 0;
    }

    /// <summary>同步 Todo 数据到侧栏</summary>
    public void SyncTodos()
    {
        RefreshSidePanel();
    }

    /// <summary>同步主题配色</summary>
    public void SyncTheme()
    {
        // 从环境变量重新读取显示风格（设置变更后生效）
        ChatDisplayStyle = Config.Instance.ChatDisplayStyle;
        // 主题配色已在 ThemeConfig 中管理，此方法为兼容旧 API
    }

    /// <summary>刷新主题样式</summary>
    public void RefreshTheme()
    {
        MarkDirty();
    }

    /// <summary>更新状态栏右侧显示：大/小模型上下文用量 + 累计花费 + 延迟</summary>
    public void UpdateTokenDisplayFull(int largeTokens, int smallTokens,
        double? estimatedCost, int contextTokens, int maxContext,
        double lastLatencyMs, double lastTokensPerSec)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"📊 大:{FormatNum(largeTokens)} · 小:{FormatNum(smallTokens)}");
        sb.Append(estimatedCost.HasValue
            ? $" · ¥{estimatedCost.Value * 7.25:F2}"
            : " · ¥-"); // 模型无定价表时显示占位，用户仍可见计费位置
        if (lastLatencyMs > 0)
            sb.Append($" · {lastLatencyMs / 1000:F1}s");
        StatusRight = sb.ToString();

        // 上下文占用百分比（供动态栏常驻显示，绿→黄→红）
        _contextPercent = maxContext > 0 ? contextTokens * 100.0 / maxContext : null;

        MarkDirty();
    }

    // ── 对话框快捷方法 ──

    /// <summary>显示选择菜单对话框，返回选中索引（-1=取消）</summary>
    public int ShowMenu(string title, List<string> choices)
    {
        var idx = UxHelper.RunModalDialogOnScreen<int>(this,
            onDone => TuiDialog.Select(title, choices,
                onSelect: i => onDone(i),
                onCancel: () => onDone(-1)));
        return idx ?? -1;
    }

    /// <summary>
    /// 权限确认 —— 输入框下方**行内文字选择**（不弹窗）：↑↓ 移动、Enter 确认、Esc 拒绝、Y/N/A 单键。
    /// 返回 0=允许 1=全部允许 2=拒绝（未应答/超时即 2）。
    ///
    /// 此方法由 Agent 后台线程调用（PermissionManager.ShowConfirmDialog）：行内栏经
    /// <see cref="UxHelper.RunInlineChoiceOnScreen"/> 投递到 UI 线程挂载，RenderWait 只等待
    /// ——「不阻塞主循环」的道理与旧弹窗路径一致，但少了一次 ShowWindow（窗口栈压力归零）。
    /// </summary>
    public int ShowPermissionDialog(string toolName, string argsSummary, string argsDetail, bool isDangerous)
    {
        var detail = argsDetail.Length > 800
            ? ContextManager.TruncateByRunes(argsDetail, 800) + "\n\n…（详情过长，已截断）"
            : argsDetail;

        // 待确认内容写进聊天流：行内栏一行放不下长命令，而用户在决定前必须看清「要执行什么」
        // （竞品同形：确认块长在被审批对象旁边）。后台线程调用 → PostToUI 投递到 UI 线程改控件树。
        PostToUI(() => AddSystemMsg($"{(isDangerous ? "⚠️ 危险操作" : "🔐 权限确认")} · {toolName}\n{detail}"));

        // 选项：危险操作不给「全部允许」（同旧弹窗：危险时只有 是/否 两项）
        var items = new List<PromptItem>();
        if (isDangerous)
        {
            items.Add(new PromptItem { Kind = EPromptKind.Choice, Label = "1. 允许", Detail = "仅本次执行", ResultCode = 0, IsDangerous = true });
            items.Add(new PromptItem { Kind = EPromptKind.Choice, Label = "2. 拒绝", Detail = "取消本次操作", ResultCode = 2, IsDangerous = true });
        }
        else
        {
            items.Add(new PromptItem { Kind = EPromptKind.Choice, Label = "1. 允许", Detail = AnsiHelper.TruncateByWidth(argsSummary, 40), ResultCode = 0 });
            items.Add(new PromptItem { Kind = EPromptKind.Choice, Label = "2. 全部允许", Detail = "本会话内不再询问同类操作", ResultCode = 1 });
            items.Add(new PromptItem { Kind = EPromptKind.Choice, Label = "3. 拒绝", Detail = "取消本次操作", ResultCode = 2 });
        }

        return UxHelper.RunInlineChoiceOnScreen(this, items);
    }

    /// <summary>
    /// 计划审批（Plan 模式审批门）—— 输入框下方**行内选择**（不弹窗）。
    /// 返回 0=批准并自动接受编辑 / 1=批准但每次编辑仍问 / 2=拒绝（未应答、超时同为 2）。
    /// 完整计划已在聊天流里，这里只把摘要再钉一条系统消息（行内栏一行放不下）。
    /// 由 Agent 后台线程调用：行内栏经 <see cref="UxHelper.RunInlineChoiceOnScreen"/> 投递到 UI 线程挂载。
    /// </summary>
    public int ShowPlanApproval(string planSummary, string planDetail)
    {
        var body = planDetail.Length > 600
            ? ContextManager.TruncateByRunes(planDetail, 600) + "\n\n…（完整计划见上方聊天记录）"
            : planDetail;
        PostToUI(() => AddSystemMsg($"📋 计划审批\n{body}"));

        // 三态对标 Claude Code 的 exit-plan：批准方式本身是用户的选择 ——
        // 「计划我认了」不等于「接下来每个文件都别再问我」，也不等于「必须一直问我」。
        return UxHelper.RunInlineChoiceOnScreen(this,
        [
            new PromptItem { Kind = EPromptKind.Choice, Label = "1. 批准并自动接受编辑", Detail = "本会话内改写文件不再逐次询问", ResultCode = 0 },
            new PromptItem { Kind = EPromptKind.Choice, Label = "2. 批准，但每次编辑都问我", Detail = "保留逐次确认", ResultCode = 1 },
            new PromptItem { Kind = EPromptKind.Choice, Label = "3. 继续规划", Detail = "不执行，保持计划模式", ResultCode = 2 },
        ]);
    }

    /// <summary>通用确认（1 确定 / 2 取消）—— 行内选择，不弹窗。
    /// UI 线程或 Agent 后台线程均可调用（<see cref="UxHelper.RunInlineChoiceOnScreen"/> 自动判定接管）。</summary>
    public bool ConfirmDialog(string title, string message)
    {
        PostToUI(() => AddSystemMsg($"{title}\n{message}"));

        return UxHelper.RunInlineChoiceOnScreen(this,
        [
            new PromptItem { Kind = EPromptKind.Choice, Label = "1. 确定", Detail = "继续", ResultCode = 0 },
            new PromptItem { Kind = EPromptKind.Choice, Label = "2. 取消", Detail = "放弃本次操作", ResultCode = 2 },
        ]) == 0;
    }

    // ── 工具 ──

    /// <summary>数字自动换算 K/M（如 128000→128K, 1000000→1M）</summary>
    private static string FormatNum(int n) => n switch
    {
        >= 1_000_000 => $"{n / 1_000_000.0:0.#}M",
        >= 1_000 => $"{n / 1_000.0:0.#}K",
        _ => n.ToString()
    };
}

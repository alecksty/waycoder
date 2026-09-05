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
    /// 显示权限确认对话框（模态弹框）—— Y=允许 A=全允 N/Esc=拒绝。
    /// 返回 0=允许 1=全部允许 2=拒绝。替代旧的行内权限块（InlinePermission）。
    /// 此方法由 Agent 后台线程调用（PermissionManager.CheckAsync）：<see cref="UxHelper.RunModalDialogOnScreen{TResult}"/>
    /// 检测到后台线程时 ShowWindow 经 PostToUI 投递 UI 线程，RenderWait 只等待（防窗口栈并发）。
    /// </summary>
    public int ShowPermissionDialog(string toolName, string argsSummary, string argsDetail, bool isDangerous)
    {
        var title = isDangerous ? $"⚠️ 危险操作 · {toolName}" : $"🔐 权限确认 · {toolName}";
        var body = argsDetail.Length > 800
            ? ContextManager.TruncateByRunes(argsDetail, 800) + "\n\n…（详情过长，已截断）"
            : argsDetail;

        var result = UxHelper.RunModalDialogOnScreen<int>(this,
            onDone => TuiDialog.Permission(title, body, r =>
                onDone(r switch
                {
                    TuiDialog.EDialogResult.Yes => 0,   // 允许
                    TuiDialog.EDialogResult.Ok => 1,    // 全部允许
                    _ => 2,                             // 拒绝（No / Closed）
                })));
        return result ?? 2; // 默认拒绝
    }

    /// <summary>
    /// 计划审批确认框（Plan 模式审批门）—— 展示计划摘要，用户批准后返回 true。
    /// 完整计划已在聊天流中展示，对话框内只放摘要避免超长溢出。
    /// 此方法由 Agent 后台线程调用（Agent 计划审批门）：后台线程经 <see cref="UxHelper.RunModalDialogOnScreen{TResult}"/>
    /// 投递 ShowWindow（同权限框，防窗口栈并发）。
    /// </summary>
    public bool ShowPlanApproval(string planSummary, string planDetail)
    {
        var dialogBody = planDetail.Length > 600
            ? ContextManager.TruncateByRunes(planDetail, 600) + "\n\n…（完整计划见上方聊天记录）"
            : planDetail;

        var approved = UxHelper.RunModalDialogOnScreen<bool>(this,
            onDone => TuiDialog.Confirm("📋 计划审批", dialogBody, r => onDone(r)));
        return approved ?? false;
    }

    /// <summary>通用确认框（Y 确认 / N 取消）。UI 线程或 Agent 后台线程均可调用
    /// （<see cref="UxHelper.RunModalDialogOnScreen{TResult}"/> 自动判定接管）。</summary>
    public bool ConfirmDialog(string title, string message)
    {
        var ok = UxHelper.RunModalDialogOnScreen<bool>(this,
            onDone => TuiDialog.Confirm(title, message, r => onDone(r)));
        return ok ?? false;
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

using System.Collections.Concurrent;
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
            int avail = Math.Max(30, ChatList.Width - 4);
            if (AnsiHelper.DisplayWidth(label) > avail)
                label = AnsiHelper.TruncateByWidth(label, avail);
            var msg = new ChatMsg { Role = "tool", Content = label, Indent = 1 };
            ChatMessages.Add(msg);
            AddMessage(label, "tool", indent: 1);
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
        using var evt = new ManualResetEventSlim(false);
        var win = TuiDialog.Select(title, choices,
            onSelect: _ => evt.Set(),
            onCancel: () => evt.Set());
        ShowWindow(win);
        RenderWait(evt);
        return win.Result is int idx ? idx : -1;
    }

    /// <summary>
    /// 显示权限确认对话框（模态弹框）—— Y=允许 A=全允 N/Esc=拒绝。
    /// 返回 0=允许 1=全部允许 2=拒绝。替代旧的行内权限块（InlinePermission）。
    /// </summary>
    public int ShowPermissionDialog(string toolName, string argsSummary, string argsDetail, bool isDangerous)
    {
        using var evt = new ManualResetEventSlim(false);
        int resolved = 2; // 默认拒绝

        var title = isDangerous ? $"⚠️ 危险操作 · {toolName}" : $"🔐 权限确认 · {toolName}";
        var body = argsDetail.Length > 800
            ? ContextManager.TruncateByRunes(argsDetail, 800) + "\n\n…（详情过长，已截断）"
            : argsDetail;

        var win = TuiDialog.Permission(title, body, r =>
        {
            resolved = r switch
            {
                TuiDialog.EDialogResult.Yes => 0,   // 允许
                TuiDialog.EDialogResult.Ok => 1,    // 全部允许
                _ => 2,                             // 拒绝（No / Closed）
            };
            evt.Set();
        });
        // 此方法由 Agent 后台线程调用（PermissionManager.CheckAsync）：ShowWindow 改窗口栈，
        // 投递到 UI 线程，避免与渲染循环并发遍历 Windows 列表（帧交错花屏）。RenderWait 在 agent 期只等待。
        PostToUI(() => ShowWindow(win));
        RenderWait(evt);
        return resolved;
    }

    /// <summary>
    /// 计划审批确认框（Plan 模式审批门）—— 展示计划摘要，用户批准后返回 true。
    /// 完整计划已在聊天流中展示，对话框内只放摘要避免超长溢出。
    /// </summary>
    public bool ShowPlanApproval(string planSummary, string planDetail)
    {
        using var evt = new ManualResetEventSlim(false);
        bool approved = false;

        var dialogBody = planDetail.Length > 600
            ? ContextManager.TruncateByRunes(planDetail, 600) + "\n\n…（完整计划见上方聊天记录）"
            : planDetail;

        var win = TuiDialog.Confirm("📋 计划审批", dialogBody, r =>
        {
            approved = r;
            evt.Set();
        });
        // 此方法由 Agent 后台线程调用（Agent 计划审批门）：ShowWindow 投递到 UI 线程（同权限框，防窗口栈并发）。
        PostToUI(() => ShowWindow(win));
        RenderWait(evt);
        return approved;
    }

    /// <summary>通用确认框（Y 确认 / N 取消）。UI 线程或 Agent 后台线程均可调用（RenderWait 自动判定接管）。</summary>
    public bool ConfirmDialog(string title, string message)
    {
        using var evt = new ManualResetEventSlim(false);
        bool ok = false;
        var win = TuiDialog.Confirm(title, message, r => { ok = r; evt.Set(); });
        PostToUI(() => ShowWindow(win));
        RenderWait(evt);
        return ok;
    }

    /// <summary>渲染循环等待对话框关闭。
    /// 统一走 <see cref="UxHelper.RenderWait"/>（共享 InputManager：paste/CSI/鼠标解析）——
    /// 此前裸 Console.ReadKey 与主循环双读竞态，且把粘贴前导 \x1b 当 Esc 关闭对话框（静默拒绝）。
    /// readKeys 自动判定（TuiScreen.IsUiThread）：UI 线程调用 → 本循环接管渲染+读键；
    /// 后台线程调用（Agent 请求权限/审批）→ 只等待，由常驻 REPL 主循环/外层渲染循环渲染+路由按键——
    /// 绝不让后台线程与主循环并发渲染/读键/改窗口栈（Windows 列表竞态 + 焦点丢失 + 输入被抢 = 卡死）。</summary>
    private void RenderWait(ManualResetEventSlim evt)
        => UxHelper.RenderWait(this, evt, timeoutMs: 0);
    // ── 工具 ──

    /// <summary>数字自动换算 K/M（如 128000→128K, 1000000→1M）</summary>
    private static string FormatNum(int n) => n switch
    {
        >= 1_000_000 => $"{n / 1_000_000.0:0.#}M",
        >= 1_000 => $"{n / 1_000.0:0.#}K",
        _ => n.ToString()
    };
}

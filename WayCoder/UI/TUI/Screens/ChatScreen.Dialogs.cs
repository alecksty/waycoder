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
        // 工具到来 = 思考块就地定稿（对齐 Web 的 onToolStart → endThink 兜底）
        FoldThink();

        // 参数**完整显示**：超宽时在 FormatHeader 里折行，不再截断 ——
        // bash 命令、文件路径截掉尾巴就看不全了。可用宽 = 聊天区宽 - 6：
        // 条目内宽是 list-4（左右 padding 各 1 + 列表自身 2），再留 2 列余量防右缘贴边。
        int avail = Math.Max(30, ChatList.Width - 6);
        // 不在这里手写缩进：条目缩进由 AddMessage(indent: 1) 统一加 —— 手写会与内容行
        // （同样 indent:1，但没有手写前缀）差出 2 列，工具行和它的输出看着对不齐。
        string label = ToolRendererFactory.FormatHeader(toolName, brief, avail);
        lock (_chatLock)
        {
            // 标题行**不走 shellBlock**：ShellBlock 的渲染路径故意不解码 «»（bash 输出是 OS 原文，
            // 那里的 «» 只是普通字符）—— 而标题是我们自己生成的 «bold»«yellow»Edit«/» 标记，
            // 跟着一起不解码就会把标记原样打在屏幕上（实测 `│ 🔧 «bold»«yellow»Bash«/»…`）。
            // 竖线 gutter 只属于**内容行**，由下面的 _pendingToolShell 决定。
            var msg = new ChatMsg { Role = "tool", Content = label, Indent = 1, ShellBlock = false };
            ChatMessages.Add(msg);
            AddMessage(label, "tool", indent: 1, shellBlock: false);
        }
        _toolOutputLineCount = 0;
        // 标题行已就位，其输出首个块要**另起一条消息**（见 AppendToLast）——
        // 直接接在后面时多行输出的首行会紧贴标题，看起来「工具行和内容混在一起」。
        _pendingToolBody = true;
        _pendingToolShell = toolName == "bash";
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

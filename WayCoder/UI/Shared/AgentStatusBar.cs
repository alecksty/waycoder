namespace WayCoder.UI.Shared;

/// <summary>
/// Agent 运行时状态（四端动态状态栏共用）：TUI/Web/GUI/MAUI 都编译本文件（UI/Shared 不被 MAUI/GUI 排除）。
/// 状态由各端从 Agent 信号（IsBusy / CurrentToolName / 压缩 / 权限等待 / 工作模式 / 完成瞬态）解析，
/// 纯函数无反射，AOT 安全。
/// </summary>
public enum AgentStatus
{
    /// <summary>空闲（动态栏由各端隐藏）</summary>
    Idle,
    /// <summary>思考中...</summary>
    Thinking,
    /// <summary>使用工具中 {工具名}...</summary>
    ToolRunning,
    /// <summary>压缩上下文中...</summary>
    Compressing,
    /// <summary>等待确认中...（权限弹框）</summary>
    WaitingPermission,
    /// <summary>等待用户回复中...（ask_user_question）</summary>
    WaitingUser,
    /// <summary>等待子代理完成中...（agent 工具）</summary>
    WaitingSubagent,
    /// <summary>任务完成 ✓（瞬态，约 2.5s）</summary>
    Complete,
    /// <summary>计划模式 🧠（非 Build 工作模式）</summary>
    Planning,
    /// <summary>错误</summary>
    Error,
}

/// <summary>状态解析输入（各端从 Agent/事件派生后传入）。</summary>
public readonly record struct AgentStatusInput(
    bool Busy,
    string? ToolName,
    bool Compressing,
    bool WaitingPermission,
    bool WaitingUser,
    bool WaitingSubagent,
    WorkMode Mode,
    bool RecentComplete = false);

/// <summary>解析结果：状态 + 显示文字 + 详情（如工具名，TUI 中段用）。</summary>
public readonly record struct AgentStatusView(AgentStatus Status, string Text, string? Detail);

/// <summary>
/// 统一 Agent 状态解析器 + Braille spinner 帧（跨端一致）。
/// 优先级：压缩 &gt; 等待确认 &gt; 忙（等用户 / 等子代理 / 工具 / 思考）&gt; 完成瞬态 &gt; 计划模式 &gt; 空闲。
/// 命名避开 AgentStatusBar（MAUI/GUI 的 XAML 状态栏 Border 元素名），防止同名遮蔽。
/// </summary>
public static class AgentStatusResolver
{
    /// <summary>经典 10 帧 Braille 等待动画（四端共用字符集，帧率按端保留）。</summary>
    public static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    public static AgentStatusView Resolve(in AgentStatusInput i)
    {
        if (i.Compressing)
            return new(AgentStatus.Compressing, "压缩上下文中...", i.ToolName);
        if (i.WaitingPermission)
            return new(AgentStatus.WaitingPermission, "等待确认中...", null);
        if (i.Busy)
        {
            // 等待用户/等待子代理：优先显式标志，其次按工具名自动推断（ask_user_question / agent）
            if (i.WaitingUser || i.ToolName == "ask_user_question")
                return new(AgentStatus.WaitingUser, "等待用户回复中...", null);
            if (i.WaitingSubagent || i.ToolName == "agent")
                return new(AgentStatus.WaitingSubagent, "等待子代理完成中...", null);
            if (!string.IsNullOrEmpty(i.ToolName))
                return new(AgentStatus.ToolRunning, $"使用工具中 {i.ToolName}...", i.ToolName);
            return new(AgentStatus.Thinking, "思考中...", null);
        }
        if (i.RecentComplete)
            return new(AgentStatus.Complete, "任务完成 ✓", null);
        if (i.Mode != WorkMode.Build)
            return new(AgentStatus.Planning, "计划模式 🧠", null);
        return new(AgentStatus.Idle, "空闲", null);
    }

    /// <summary>AOT 安全定名（switch，不反射枚举）：Web 序列化用。</summary>
    public static string StatusKey(AgentStatus s) => s switch
    {
        AgentStatus.Thinking => "thinking",
        AgentStatus.ToolRunning => "tool",
        AgentStatus.Compressing => "compressing",
        AgentStatus.WaitingPermission => "waiting_permission",
        AgentStatus.WaitingUser => "waiting_user",
        AgentStatus.WaitingSubagent => "waiting_subagent",
        AgentStatus.Complete => "complete",
        AgentStatus.Planning => "planning",
        AgentStatus.Error => "error",
        _ => "idle",
    };
}

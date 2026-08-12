// 项目目标为 win-x64，Console.Beep/Console.Title 仅 Windows 可用，CA1416 为误报
#pragma warning disable CA1416

namespace WayCoder;

/// <summary>
/// 桌面通知系统 —— Agent 完成、权限等待等事件触发终端/桌面通知。
///
/// 通知方式（多级回退）：
///   1. Windows Toast 通知（通过 PowerShell，如可用）
///   2. 终端标题闪烁 + 响铃
///
/// 配置：Config.DesktopNotifications（环境变量 WAYCODER_ENABLE_NOTIFICATIONS）
///       默认关闭，需手动开启。
/// </summary>
public static class DesktopNotifier
{
    /// <summary>通知类型</summary>
    public enum NotificationType
    {
        /// <summary>Agent 完成一轮</summary>
        AgentFinished,
        /// <summary>权限确认等待中</summary>
        PermissionWaiting,
        /// <summary>需要重新认证</summary>
        ReAuthenticate,
        /// <summary>后台任务完成</summary>
        BackgroundTaskFinished,
    }

    /// <summary>原始标题，供恢复用</summary>
    private static string? _originalTitle;

    /// <summary>
    /// 发送通知。
    /// </summary>
    public static void Notify(NotificationType type, string? title = null, string? message = null)
    {
        if (!Config.Instance.DesktopNotifications)
            return;

        var (displayTitle, displayMessage) = GetDisplayText(type, title, message);

        // 方法 1: Windows Toast（PowerShell）
        if (OperatingSystem.IsWindows() && TrySendWindowsToast(displayTitle, displayMessage))
            return;

        // 方法 2: 终端标题 + 响铃
        FlashTitle(displayTitle, displayMessage);
        Console.Beep(800, 200);
    }

    /// <summary>
    /// Agent 完成通知。如果启用了通知，在 Agent.Run 完成时调用。
    /// </summary>
    public static void NotifyAgentFinished(string? sessionName = null)
    {
        Notify(NotificationType.AgentFinished,
            title: sessionName,
            message: "Agent 已完成当前任务，等待输入");
    }

    /// <summary>
    /// 权限等待通知。在显示权限确认对话框时调用。
    /// </summary>
    public static void NotifyPermissionWaiting(string toolName)
    {
        Notify(NotificationType.PermissionWaiting,
            title: $"权限确认: {toolName}",
            message: $"WayCoder 正在等待对工具 '{toolName}' 的操作确认");
    }

    /// <summary>
    /// 后台任务完成通知。
    /// </summary>
    public static void NotifyBackgroundTaskFinished(string? taskId = null)
    {
        Notify(NotificationType.BackgroundTaskFinished,
            title: taskId ?? "后台任务",
            message: "后台任务已完成");
    }

    // ========================================================================
    // 内部实现
    // ========================================================================

    private static (string title, string message) GetDisplayText(NotificationType type, string? title, string? message)
    {
        return type switch
        {
            NotificationType.AgentFinished => (
                title ?? "WayCoder",
                message ?? "Agent 已完成当前任务"),

            NotificationType.PermissionWaiting => (
                title ?? "WayCoder — 权限确认",
                message ?? "等待操作确认"),

            NotificationType.ReAuthenticate => (
                title ?? "WayCoder — 认证",
                message ?? "需要重新认证"),

            NotificationType.BackgroundTaskFinished => (
                title ?? "WayCoder — 后台任务",
                message ?? "后台任务已完成"),

            _ => (title ?? "WayCoder", message ?? "")
        };
    }

    private static bool TrySendWindowsToast(string title, string message)
    {
        try
        {
            // 使用 AppID 的方式仅 Windows 10+ 有效
            var escapedTitle = title.Replace("'", "''");
            var escapedMessage = message.Replace("'", "''");

            var psScript = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
$template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
$textNodes = $template.GetElementsByTagName('text')
$textNodes.Item(0).AppendChild($template.CreateTextNode('{escapedTitle}')) > $null
$textNodes.Item(1).AppendChild($template.CreateTextNode('{escapedMessage}')) > $null
$toast = [Windows.UI.Notifications.ToastNotification]::new($template)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('WayCoder').Show($toast)
";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psScript.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            var proc = System.Diagnostics.Process.Start(psi);
            proc?.WaitForExit(2000);
            return proc?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 通过修改终端标题实现"闪烁"效果。
    /// </summary>
    private static void FlashTitle(string title, string message)
    {
        try
        {
            _originalTitle ??= Console.Title;
            var flashTitle = $"🔔 {title} — {message}";

            // 短暂切换标题
            Console.Title = flashTitle;

            // 异步恢复原标题
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000);
                if (_originalTitle != null)
                    Console.Title = _originalTitle;
            });
        }
        catch
        {
            // 某些终端不支持修改标题，忽略
        }
    }

    /// <summary>
    /// 简单响铃通知（总是可用）。
    /// </summary>
    public static void Beep()
    {
        if (!Config.Instance.DesktopNotifications)
            return;

        try
        {
            Console.Beep(800, 200);
        }
        catch
        {
            // 某些终端不支持响铃
        }
    }
}

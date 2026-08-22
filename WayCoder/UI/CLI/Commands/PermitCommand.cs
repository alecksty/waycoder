using WayCoder.UI.Tui.Screens;

namespace WayCoder.UI.Cli.Commands;

/// <summary>切换权限模式（问答ACK/自动AUTO/智能SMART/畅通YOLO）。纯聊天（tiny/chat）路由到工作模式 Chat。</summary>
public class PermitCommand : SlashCommand
{
    public override string Name => "/permit";
    public override string Description => "切换权限模式（问答ACK/自动AUTO/智能SMART/畅通YOLO）";
    public override string? Usage => "/permit <ack|auto|smart|yolo>";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            screen.AddSystemMsg($"当前权限: {PermissionManager.FormatMode()}\n可选: ack(问答) auto(自动) smart(智能) yolo(畅通) · tiny/chat=纯聊天工作模式");
            return Task.CompletedTask;
        }

        // 纯聊天别名 → 切工作模式 Chat（0 工具 + 0 提示词）
        if (PermissionManager.IsChatModeAlias(args))
        {
            WorkModeManager.SetMode(WorkMode.Chat);
            var activeSlot = Program.ActiveSlotIndex;
            var slots = Program.GetSlots();
            if (slots != null && activeSlot >= 0 && activeSlot < slots.Length)
                slots[activeSlot].WorkMode = WorkMode.Chat;
            Program.RefreshActiveSlotTools();
            screen.AddSystemMsg($"✅ 工作模式已切换: {WorkModeManager.Format(WorkMode.Chat)}（纯聊天 · 0 工具 0 提示词）");
            return Task.CompletedTask;
        }

        PermissionManager.SetMode(args);
        Program.RefreshActiveSlotTools(); // 权限模式可影响工具集（YOLO 换 YoloToolAllowList），切换后刷新
        screen.AddSystemMsg($"✅ 权限模式: {PermissionManager.FormatMode()}");
        return Task.CompletedTask;
    }
}

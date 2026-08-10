using WayCoder.UI.TuiScreens;

namespace WayCoder.Commands;

/// <summary>
/// /send — 向其他 Agent 槽位发送消息。
/// /broadcast — 向所有其他槽位广播消息。
///
/// 用途：多 Agent 协作——例如 F1 负责架构规划，F2 负责编码实现，
/// F3 负责代码审查，通过 /send 在工作流中传递上下文。
/// </summary>
public class SendCommand : SlashCommand
{
    public override string Name => "/send";
    public override string[] Aliases => ["/发送", "/to"];
    public override string Description => "向其他 Agent 槽位发送消息（多 Agent 协作）";
    public override string? Usage => "/send <槽位号> <消息>";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            ShowUsage(screen);
            return Task.CompletedTask;
        }

        // 解析: /send <slot> <message>
        var spaceIdx = args.IndexOf(' ');
        if (spaceIdx < 0)
        {
            ShowUsage(screen);
            return Task.CompletedTask;
        }

        var slotStr = args[..spaceIdx].Trim();
        var message = args[(spaceIdx + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            screen.AddSystemMsg("⚠ 消息不能为空。用法: /send <槽位号> <消息>");
            return Task.CompletedTask;
        }

        if (!int.TryParse(slotStr, out var slotNum) || slotNum < 1 || slotNum > AgentSlot.Count)
        {
            screen.AddSystemMsg($"⚠ 无效的槽位号: {slotStr}（有效范围: 1-{AgentSlot.Count}）");
            return Task.CompletedTask;
        }

        var targetIdx = slotNum - 1;
        var currentIdx = screen.ActiveSlotIndex;

        if (targetIdx == currentIdx)
        {
            screen.AddSystemMsg("⚠ 不能给自己发送消息。请指定其他槽位号。");
            return Task.CompletedTask;
        }

        // 投递消息到目标槽位
        var slots = Program.GetSlots();
        if (targetIdx >= slots.Length || slots[targetIdx] == null)
        {
            screen.AddSystemMsg($"⚠ 槽位 F{slotNum} 尚未初始化。请先切换到该槽位激活。");
            return Task.CompletedTask;
        }

        slots[targetIdx].DeliverMessage(currentIdx, message, screen, targetIdx);

        screen.AddSystemMsg($"📨 **已发送** → F{slotNum}: {message}");
        return Task.CompletedTask;
    }

    private static void ShowUsage(ChatScreen screen)
    {
        var lines = new List<string>
        {
            "**📨 /send — 跨槽位消息传递**",
            "",
            "用法: `/send <槽位号> <消息>`",
            "示例: `/send 2 帮我审查这段代码的安全性`",
            "示例: `/send 3 你那边编译通过了吗？`",
            "",
            "**相关命令**：",
            "- `/broadcast <消息>` — 向所有其他槽位广播",
            "- `F1-F10` 切换槽位查看接收的消息",
            "",
            $"有效槽位: F1 - F{AgentSlot.Count}",
        };
        screen.AddMessage(string.Join("\n", lines), "system");
    }
}

/// <summary>
/// /broadcast — 向所有其他 Agent 槽位广播消息。
/// </summary>
public class BroadcastCommand : SlashCommand
{
    public override string Name => "/broadcast";
    public override string[] Aliases => ["/广播", "/bc"];
    public override string Description => "向所有 Agent 槽位广播消息";
    public override string? Usage => "/broadcast <消息>";

    public override Task ExecuteAsync(string args, ChatScreen screen)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            screen.AddSystemMsg("⚠ 消息不能为空。用法: /broadcast <消息>");
            return Task.CompletedTask;
        }

        var message = args.Trim();
        var currentIdx = screen.ActiveSlotIndex;
        var slots = Program.GetSlots();
        var delivered = new List<int>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (i == currentIdx) continue;
            if (slots[i] == null) continue;

            slots[i].DeliverMessage(currentIdx, message, screen, i);
            delivered.Add(i + 1);
        }

        if (delivered.Count == 0)
        {
            screen.AddSystemMsg("⚠ 没有其他已初始化的槽位可接收消息。");
        }
        else
        {
            var slotList = string.Join(", ", delivered.Select(n => $"F{n}"));
            screen.AddSystemMsg($"📣 **广播已发送** → {slotList}: {message}");
        }

        return Task.CompletedTask;
    }
}

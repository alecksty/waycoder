namespace WayCoder.Tools;

using WayCoder.UI;

/// <summary>
/// Todo 任务追踪工具 —— Agent 可以创建和管理结构化任务列表。
/// </summary>
public class TodoTool : ITool
{
    public string Name => "todo";
    public string Description => "管理任务列表。支持 create（创建）、update（更新状态）、list（列出）。状态: pending / in_progress / completed / cancelled。用于追踪多步骤工作的进度。";

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["action"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "操作类型：create、update、list、clear",
            },
            ["id"] = new JsonObject
            {
                ["type"] = "integer",
                ["description"] = "任务 ID（update 时需要）",
            },
            ["title"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "任务标题（create 时需要）",
            },
            ["status"] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "任务状态：pending、in_progress、completed、cancelled（update 时需要）",
            },
        },
        ["required"] = new JsonArray("action"),
    };

    /// <summary>任务条目</summary>
    public class TodoItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>全局任务列表</summary>
    public static readonly List<TodoItem> Items = [];

    private static int _nextId = 1;

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var action = arguments.GetValueOrDefault("action")?.ToString() ?? "list";

        var result = action switch
        {
            "create" => Create(arguments),
            "update" => Update(arguments),
            "clear" => Clear(),
            _ => List(),
        };

        // 修改后刷新侧边栏 Todo 面板
        if (action is "create" or "update" or "clear")
            RefreshSidebar();

        return Task.FromResult(result);
    }

    /// <summary>刷新 TUI 侧边栏 Todo 面板（如果当前屏幕是 ChatScreen）</summary>
    private static void RefreshSidebar()
    {
        try
        {
            if (TuiManager.Instance?.ActiveScreen is WayCoder.UI.TuiScreens.ChatScreen screen)
                screen.RefreshSidePanel();
        }
        catch { /* 非 TUI 模式，静默忽略 */ }
    }

    private static string Create(Dictionary<string, object?> args)
    {
        var title = args.GetValueOrDefault("title")?.ToString();
        if (string.IsNullOrWhiteSpace(title))
            return "错误：创建任务需要提供 title";

        var item = new TodoItem { Id = _nextId++, Title = title, Status = "pending" };
        Items.Add(item);
        return $"✅ 已创建任务 #{item.Id}: {item.Title}";
    }

    private static string Update(Dictionary<string, object?> args)
    {
        if (!args.TryGetValue("id", out var idObj) || idObj is not int id)
            return "错误：需要提供有效的任务 ID";

        var status = args.GetValueOrDefault("status")?.ToString() ?? "";
        var validStatuses = new[] { "pending", "in_progress", "completed", "cancelled" };
        if (!validStatuses.Contains(status))
            return $"错误：无效状态 '{status}'（有效值: {string.Join(", ", validStatuses)}）";

        var item = Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return $"错误：未找到任务 #{id}";

        item.Status = status;
        return $"✅ 任务 #{id} 状态更新为 {status}: {item.Title}";
    }

    private static string List()
    {
        if (Items.Count == 0) return "（暂无任务）";

        var lines = new List<string>();
        var statusIcons = new Dictionary<string, string>
        {
            ["pending"] = "⏳",
            ["in_progress"] = "🔄",
            ["completed"] = "✅",
            ["cancelled"] = "❌",
        };

        foreach (var item in Items.OrderBy(i => i.Id))
        {
            var icon = statusIcons.GetValueOrDefault(item.Status, "❓");
            lines.Add($"  #{item.Id} {icon} [{item.Status}] {item.Title}");
        }

        return string.Join("\n", lines);
    }

    private static string Clear()
    {
        var count = Items.Count;
        Items.Clear();
        _nextId = 1;
        return $"✅ 已清除 {count} 个任务";
    }
}

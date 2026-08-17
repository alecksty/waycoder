namespace WayCoder.Tools;

/// <summary>
/// 结构化 Todo 工具 —— 支持依赖关系的任务管理。
/// 操作：create, update, list, delete。
/// TodoItem 包含 Id/Title/Status/DependsOn（前置任务列表）。
/// 持久化到 .waycoder/todos.json。
/// </summary>
public class StructTodoTool : ITool
{
    public string Name => "struct_todo";
    public string Description => "管理带依赖关系的结构化任务列表。操作：create(创建任务,可指定前置依赖), update(更新状态: pending/in_progress/completed/blocked), list(列出全部,可过滤状态), delete(删除)。支持依赖检测：blocked 状态的任务不会在其依赖完成前被标记为 in_progress。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("action", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array().Add("create").Add("update").Add("list").Add("delete"))
                .Set("description", "操作类型"))
            .Set("id", JNode.Object()
                .Set("type", "string")
                .Set("description", "任务 ID（create/update/delete 必填）"))
            .Set("title", JNode.Object()
                .Set("type", "string")
                .Set("description", "任务标题（create 必填）"))
            .Set("status", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array().Add("pending").Add("in_progress").Add("completed").Add("blocked"))
                .Set("description", "任务状态（update 操作）"))
            .Set("deps", JNode.Object()
                .Set("type", "array")
                .Set("items", JNode.Object().Set("type", "string"))
                .Set("description", "前置依赖任务 ID 列表（create 操作可选）"))
            .Set("filter", JNode.Object()
                .Set("type", "string")
                .Set("description", "状态过滤器，逗号分隔（list 操作可选）")))
        .Set("required", JNode.Array().Add("action"));

    private static string StorePath => Path.Combine(
        BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory(), ".waycoder", "todos.json"); // cd 后基于被跟踪工作目录，而非进程启动目录

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var action = arguments.GetValueOrDefault("action")?.ToString() ?? "list";
        return Task.FromResult(action switch
        {
            "create" => Create(arguments),
            "update" => Update(arguments),
            "list" => List(arguments),
            "delete" => Delete(arguments),
            _ => "错误：不支持的操作，可用 create/update/list/delete",
        });
    }

    // ── 操作 ──

    private static string Create(Dictionary<string, object?> args)
    {
        var id = args.GetValueOrDefault("id")?.ToString();
        var title = args.GetValueOrDefault("title")?.ToString();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
            return "错误：create 需要 id 和 title 参数";

        var todos = LoadTodos();
        if (todos.Any(t => t.Id == id))
            return $"错误：任务 ID '{id}' 已存在";

        var deps = new List<string>();
        if (args.TryGetValue("deps", out var depsObj) && depsObj != null)
        {
            if (depsObj is JNode arr)
                deps.AddRange(arr.Items.Select(n => n.AsString() ?? "").Where(s => s != ""));
            else if (depsObj is System.Collections.IEnumerable en)
                deps.AddRange(en.Cast<object>().Select(o => o?.ToString() ?? "").Where(s => s != ""));
        }

        // 验证依赖存在
        var allIds = todos.Select(t => t.Id).ToHashSet();
        var missing = deps.Where(d => !allIds.Contains(d)).ToList();
        if (missing.Count > 0)
            return $"警告：依赖任务不存在: {string.Join(", ", missing)}。任务已创建但依赖无效。";

        var todo = new TodoItem
        {
            Id = id,
            Title = title,
            Status = deps.Count > 0 ? "blocked" : "pending",
            DependsOn = deps,
            CreatedAt = DateTime.UtcNow,
        };
        todos.Add(todo);
        SaveTodos(todos);

        return $"✅ 创建任务: [{id}] {title} (状态={todo.Status}, 依赖={deps.Count})";
    }

    private static string Update(Dictionary<string, object?> args)
    {
        var id = args.GetValueOrDefault("id")?.ToString();
        var status = args.GetValueOrDefault("status")?.ToString();
        if (string.IsNullOrWhiteSpace(id))
            return "错误：update 需要 id 参数";

        var validStatuses = new[] { "pending", "in_progress", "completed", "blocked" };
        if (status != null && !validStatuses.Contains(status))
            return $"错误：无效状态 '{status}'，可用 {string.Join(", ", validStatuses)}";

        var todos = LoadTodos();
        var todo = todos.FirstOrDefault(t => t.Id == id);
        if (todo == null)
            return $"错误：任务 '{id}' 不存在";

        if (status != null)
        {
            // 依赖检查：blocked→in_progress 需要所有依赖已完成
            if (status == "in_progress" && todo.DependsOn.Count > 0)
            {
                var incomplete = todo.DependsOn
                    .Where(depId => !todos.Any(t => t.Id == depId && t.Status == "completed"))
                    .ToList();
                if (incomplete.Count > 0)
                    return $"⚠ 无法开始：依赖任务未完成: {string.Join(", ", incomplete)}。请先完成依赖任务。";
            }

            // 完成时解除 block 者
            if (status == "completed")
            {
                var blocked = todos.Where(t =>
                    t.Status == "blocked" &&
                    t.DependsOn.Contains(id) &&
                    t.DependsOn.All(depId =>
                        depId == id || todos.Any(t2 => t2.Id == depId && t2.Status == "completed")));
                foreach (var b in blocked)
                {
                    b.Status = "pending";
                }
            }

            todo.Status = status;
        }

        SaveTodos(todos);
        return $"✅ 更新任务: [{id}] {todo.Title} → {todo.Status}";
    }

    private static string List(Dictionary<string, object?> args)
    {
        var todos = LoadTodos();
        var filter = args.GetValueOrDefault("filter")?.ToString();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var statuses = filter.Split(',', StringSplitOptions.TrimEntries).ToHashSet();
            todos = todos.Where(t => statuses.Contains(t.Status)).ToList();
        }

        if (todos.Count == 0) return "📋 任务列表为空";

        var lines = new List<string> { $"📋 任务列表 ({todos.Count} 项)" };
        foreach (var t in todos.OrderBy(t => t.Status switch
                 {
                     "in_progress" => 0, "blocked" => 1, "pending" => 2, "completed" => 3, _ => 4
                 }).ThenBy(t => t.CreatedAt))
        {
            var emoji = t.Status switch
            {
                "in_progress" => "🔄", "completed" => "✅", "blocked" => "🚫", _ => "⏳"
            };
            var deps = t.DependsOn.Count > 0 ? $" (依赖: {string.Join(", ", t.DependsOn)})" : "";
            lines.Add($"  {emoji} [{t.Id}] {t.Title}{deps}");
        }
        return string.Join("\n", lines);
    }

    private static string Delete(Dictionary<string, object?> args)
    {
        var id = args.GetValueOrDefault("id")?.ToString();
        if (string.IsNullOrWhiteSpace(id)) return "错误：delete 需要 id 参数";

        var todos = LoadTodos();
        var removed = todos.RemoveAll(t => t.Id == id);
        if (removed == 0) return $"错误：任务 '{id}' 不存在";

        // 清理以被删任务为依赖的其他任务的依赖列表
        foreach (var t in todos)
            t.DependsOn.RemoveAll(d => d == id);

        SaveTodos(todos);
        return $"✅ 已删除任务: [{id}]";
    }

    // ── 持久化 ──

    private static List<TodoItem> LoadTodos()
    {
        try
        {
            if (!File.Exists(StorePath)) return [];
            var json = File.ReadAllText(StorePath);
            var node = Json.Parse(json);
            if (node is { Kind: JKind.Array } arr)
            {
                return arr.Items.Select(n => new TodoItem
                {
                    Id = n["id"]?.AsString() ?? "",
                    Title = n["title"]?.AsString() ?? "",
                    Description = n["description"]?.AsString() ?? "",
                    Status = n["status"]?.AsString() ?? "pending",
                    DependsOn = n["depends_on"]?.Items
                        .Select(d => d.AsString() ?? "").Where(s => s != "").ToList() ?? [],
                    CreatedAt = DateTime.TryParse(n["created_at"]?.AsString(), out var dt) ? dt : DateTime.UtcNow,
                }).ToList();
            }
        }
        catch { /* 文件损坏，返回空列表 */ }
        return [];
    }

    private static void SaveTodos(List<TodoItem> todos)
    {
        var dir = Path.GetDirectoryName(StorePath)!;
        Directory.CreateDirectory(dir);
        var arr = JNode.Array();
        foreach (var t in todos)
        {
            var dependsOn = JNode.Array();
            foreach (var d in t.DependsOn)
                dependsOn.Add(d);

            arr.Add(JNode.Object()
                .Set("id", t.Id)
                .Set("title", t.Title)
                .Set("description", t.Description)
                .Set("status", t.Status)
                .Set("depends_on", dependsOn)
                .Set("created_at", t.CreatedAt.ToString("O")));
        }
        File.WriteAllText(StorePath, arr.ToJson());
    }

    private class TodoItem
    {
        public string Id { get; init; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "pending";
        public List<string> DependsOn { get; set; } = [];
        public DateTime CreatedAt { get; set; }
    }
}

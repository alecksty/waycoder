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
    public string Description => "管理带依赖关系的结构化任务列表。操作：create(创建任务,可指定前置依赖), update(更新状态: pending/in_progress/completed/cancelled/blocked), list(列出全部,可过滤状态), delete(删除)。支持依赖检测：blocked 状态的任务不会在其依赖完成前被标记为 in_progress。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("action", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array("create", "update", "list", "delete"))
                .Set("description", "操作类型"))
            .Set("id", JNode.Param("string", "任务 ID（create/update/delete 必填）"))
            .Set("title", JNode.Param("string", "任务标题（create 必填）"))
            .Set("status", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array("pending", "in_progress", "completed", "cancelled", "blocked"))
                .Set("description", "任务状态（update 操作）"))
            .Set("deps", JNode.Object()
                .Set("type", "array")
                .Set("items", JNode.Object().Set("type", "string"))
                .Set("description", "前置依赖任务 ID 列表（create 操作可选）"))
            .Set("filter", JNode.Param("string", "状态过滤器，逗号分隔（list 操作可选）")))
        .Set("required", JNode.Array("action"));

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

        var todos = TodoStore.Load();
        if (todos.Any(t => t.Id == id))
            return $"错误：任务 ID '{id}' 已存在";

        var deps = TodoStore.ParseStringList(args, "deps");

        // 依赖缺失 → 拒绝创建（与 todo 同语义）。此处此前返回的是「任务已创建但依赖无效」——
        // 而代码在 todos.Add 之前就 return 了，任务并没有被创建，文案在骗模型。
        var missing = TodoStore.MissingDeps(todos, deps);
        if (missing.Count > 0)
            return $"错误：依赖任务不存在: {string.Join(", ", missing)}。请先创建这些任务或移除无效依赖。";

        // 条数上限：两个工具写同一份文件，上限也该一致（否则 struct_todo 就是无上限的那个口子）
        if (todos.Count >= Global.MaxTodos)
            return $"错误：任务已达上限（{Global.MaxTodos} 条），请先删除一些任务。";

        var todo = new TodoStore.Entry
        {
            Id = id,
            Title = title,
            Status = deps.Count > 0 ? "blocked" : "pending",
            DependsOn = deps,
            CreatedAt = DateTime.UtcNow,
        };
        todos.Add(todo);
        TodoStore.Save(todos);

        return $"✅ 创建任务: [{id}] {title} (状态={todo.Status}, 依赖={deps.Count})";
    }

    private static string Update(Dictionary<string, object?> args)
    {
        var id = args.GetValueOrDefault("id")?.ToString();
        var status = args.GetValueOrDefault("status")?.ToString();
        if (string.IsNullOrWhiteSpace(id))
            return "错误：update 需要 id 参数";

        if (status != null && !TodoStore.ValidStatuses.Contains(status))
            return $"错误：无效状态 '{status}'，可用 {string.Join(", ", TodoStore.ValidStatuses)}";

        var todos = TodoStore.Load();
        var todo = todos.FirstOrDefault(t => t.Id == id);
        if (todo == null)
            return $"错误：任务 '{id}' 不存在";

        if (status != null)
        {
            // 依赖检查：blocked→in_progress 需要所有依赖已完成
            if (status == "in_progress" && todo.DependsOn.Count > 0)
            {
                var incomplete = TodoStore.IncompleteDeps(todos, todo);
                if (incomplete.Count > 0)
                    return $"⚠ 无法开始：依赖任务未完成: {string.Join(", ", incomplete)}。请先完成依赖任务。";
            }

            // 完成时解除依赖它且依赖已齐的 blocked 任务（共享实现，与 todo 同源）
            if (status == "completed")
                TodoStore.UnblockDependents(todos, id);

            todo.Status = status;
        }

        TodoStore.Save(todos);
        return $"✅ 更新任务: [{id}] {todo.Title} → {todo.Status}";
    }

    private static string List(Dictionary<string, object?> args)
    {
        // 加载 + filter + 排序的共同前置已收敛到 TodoStore.LoadFiltered（与 todo 共用）
        var todos = TodoStore.LoadFiltered(args.GetValueOrDefault("filter")?.ToString());

        if (todos.Count == 0) return "📋 任务列表为空";

        var lines = new List<string> { $"📋 任务列表 ({todos.Count} 项)" };
        foreach (var t in todos)
        {
            var emoji = TodoStore.Emoji(t.Status);
            var deps = t.DependsOn.Count > 0 ? $" (依赖: {string.Join(", ", t.DependsOn)})" : "";
            lines.Add($"  {emoji} [{t.Id}] {t.Title}{deps}");
        }
        return string.Join("\n", lines);
    }

    private static string Delete(Dictionary<string, object?> args)
    {
        var id = args.GetValueOrDefault("id")?.ToString();
        if (string.IsNullOrWhiteSpace(id)) return "错误：delete 需要 id 参数";

        var todos = TodoStore.Load();
        var removed = todos.RemoveAll(t => t.Id == id);
        if (removed == 0) return $"错误：任务 '{id}' 不存在";

        // 清理以被删任务为依赖的其他任务的依赖列表
        foreach (var t in todos)
            t.DependsOn.RemoveAll(d => d == id);

        TodoStore.Save(todos);
        return $"✅ 已删除任务: [{id}]";
    }

}

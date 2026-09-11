using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;

namespace WayCoder.Tools;

/// <summary>
/// 增强任务管理工具 —— CRUD + 依赖关系 + 持久化。
/// 对标 Crush todos 工具和 Claude Code TaskCreate/TaskUpdate。
///
/// 操作：
///   create  — 创建任务（可指定前置依赖 deps）
///   update  — 更新状态/标题/描述
///   list    — 列出全部任务（可过滤状态）
///   delete  — 删除任务（自动清理被删任务在其他任务依赖中的引用）
///   clear   — 清空全部任务
///
/// 状态：pending / in_progress / completed / cancelled / blocked
/// 依赖检测：blocked→in_progress 需所有依赖已完成
/// 持久化：.waycoder/todos.json
/// </summary>
public class TodoTool : ITool
{
    public string Name => "todo";
    public string Description =>
        "管理结构化任务列表（带依赖关系）。操作：create(创建), update(更新状态/标题), list(列出,可按状态过滤), delete(删除), clear(清空)。" +
        "状态: pending/in_progress/completed/cancelled/blocked。创建时可指定 deps(前置依赖ID列表)。" +
        "blocked 任务在其依赖全部完成前无法开始(in_progress)。持久化到 .waycoder/todos.json。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("action", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array("create", "update", "list", "delete", "clear"))
                .Set("description", "操作类型"))
            .Set("id", JNode.Param("string", "任务 ID（create/update/delete 需要）。使用有意义的 kebab-case 名称，如 'fix-auth-bug'。"))
            .Set("title", JNode.Param("string", "任务标题（create/update 可选）"))
            .Set("description", JNode.Param("string", "任务详细描述（create/update 可选）"))
            .Set("status", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array("pending", "in_progress", "completed", "cancelled", "blocked"))
                .Set("description", "任务状态（update 操作）"))
            .Set("deps", JNode.Object()
                .Set("type", "array")
                .Set("items", JNode.Object().Set("type", "string"))
                .Set("description", "前置依赖任务 ID 列表（create 操作可选）。被依赖的任务必须全部 completed 后此任务才能开始。"))
            .Set("filter", JNode.Param("string", "状态过滤器，逗号分隔（list 操作可选）。如 'pending,in_progress'。")))
        .Set("required", JNode.Array("action"));

    // ── 公共访问（兼容旧代码：SelfTest、ChatScreen 侧栏、TodoCommand）──

    /// <summary>任务条目的公共视图（用于 TUI 侧栏和外部查询）。</summary>
    public class TodoItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "pending";
        public List<string> DependsOn { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>当前任务列表（从持久化存储加载，兼容旧 API）。</summary>
    public static List<TodoItem> Items
    {
        get
        {
            var entries = TodoStore.Load();
            return entries.Select(e => new TodoItem
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Status = e.Status,
                DependsOn = e.DependsOn,
                CreatedAt = e.CreatedAt,
            }).ToList();
        }
    }

    // ── 入口 ──

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var action = arguments.GetValueOrDefault("action")?.ToString() ?? "list";
        var result = action switch
        {
            "create" => Create(arguments),
            "update" => Update(arguments),
            "list" => List(arguments),
            "delete" => Delete(arguments),
            "clear" => Clear(),
            _ => "错误：不支持的操作，可用 create/update/list/delete/clear",
        };

        // 修改后刷新侧边栏 Todo 面板
        if (action is "create" or "update" or "delete" or "clear")
            RefreshSidebar();

        return Task.FromResult(result);
    }

    // ── create ──

    private static string Create(Dictionary<string, object?> args)
    {
        var id = args.GetValueOrDefault("id")?.ToString();
        var title = args.GetValueOrDefault("title")?.ToString();
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
            return "错误：create 需要 id 和 title 参数。示例: {\"action\":\"create\",\"id\":\"fix-bug\",\"title\":\"修复登录 Bug\"}";

        if (id.Length > 64)
            return "错误：id 最长 64 字符";

        var todos = TodoStore.Load();
        if (todos.Any(t => t.Id == id))
            return $"错误：任务 ID '{id}' 已存在。请使用 update 操作修改现有任务。";

        // 条数上限：todo 无限加会让 todos.json 无限涨（对齐 Global.MaxTodos）
        if (todos.Count >= Global.MaxTodos)
            return $"错误：任务已达上限（{Global.MaxTodos} 条），请先 clear 或 delete 一些任务。";

        var desc = args.GetValueOrDefault("description")?.ToString() ?? "";

        // 解析依赖
        var deps = TodoStore.ParseStringList(args, "deps");

        // 验证依赖存在
        var allIds = todos.Select(t => t.Id).ToHashSet();
        var missing = deps.Where(d => !allIds.Contains(d)).ToList();
        if (missing.Count > 0)
            return $"错误：依赖任务不存在: {string.Join(", ", missing)}。请先创建这些任务或移除无效依赖。";

        var todo = new TodoStore.Entry
        {
            Id = id,
            Title = title,
            Description = desc,
            Status = deps.Count > 0 ? "blocked" : "pending",
            DependsOn = deps,
            CreatedAt = DateTime.UtcNow,
        };
        todos.Add(todo);
        TodoStore.Save(todos);

        var depNote = deps.Count > 0 ? $"，依赖 [{string.Join(", ", deps)}]" : "";
        return $"✅ 创建任务 [{id}]: {title} | 状态={todo.Status}{depNote}";
    }

    // ── update ──

    private static string Update(Dictionary<string, object?> args)
    {
        var id = args.GetValueOrDefault("id")?.ToString();
        if (string.IsNullOrWhiteSpace(id))
            return "错误：update 需要 id 参数";

        var todos = TodoStore.Load();
        var todo = todos.FirstOrDefault(t => t.Id == id);
        if (todo == null)
            return $"错误：任务 '{id}' 不存在。使用 list 查看所有任务。";

        var changes = new List<string>();

        // 更新标题
        if (args.TryGetValue("title", out var titleObj) && titleObj is string newTitle && !string.IsNullOrWhiteSpace(newTitle))
        {
            todo.Title = newTitle;
            changes.Add($"标题→'{newTitle}'");
        }

        // 更新描述
        if (args.TryGetValue("description", out var descObj) && descObj is string newDesc)
        {
            todo.Description = newDesc;
            changes.Add("描述已更新");
        }

        // 更新状态
        if (args.TryGetValue("status", out var statusObj) && statusObj is string status && !string.IsNullOrWhiteSpace(status))
        {
            if (!TodoStore.ValidStatuses.Contains(status))
                return $"错误：无效状态 '{status}'，可用 {string.Join(", ", TodoStore.ValidStatuses)}";

            // 依赖检查：blocked→in_progress 需所有依赖已完成
            if (status == "in_progress" && todo.DependsOn.Count > 0)
            {
                var incomplete = TodoStore.IncompleteDeps(todos, todo);
                if (incomplete.Count > 0)
                    return $"⛔ 无法开始 [{id}]：依赖任务未完成 — {string.Join(", ", incomplete)}。请先完成依赖任务再重试。";
            }

            // 完成任务时：自动解除依赖此任务的其他 blocked 任务（共享实现，与 struct_todo 同源）
            if (status == "completed")
            {
                var unblocked = TodoStore.UnblockDependents(todos, id);
                if (unblocked.Count > 0)
                    changes.Add($"解除阻塞: [{string.Join(", ", unblocked)}]");
            }

            todo.Status = status;
            changes.Add($"状态→{status}");
        }

        if (changes.Count == 0)
            return $"ℹ️ 任务 [{id}] 无变更。请提供 title、description 或 status 参数。";

        TodoStore.Save(todos);
        return $"✅ 更新 [{id}] {todo.Title}: {string.Join(", ", changes)}";
    }

    // ── list ──

    private static string List(Dictionary<string, object?> args)
    {
        // 加载 + filter + 排序的共同前置已收敛到 TodoStore.LoadFiltered（与 struct_todo 共用）
        var todos = TodoStore.LoadFiltered(args.GetValueOrDefault("filter")?.ToString());

        if (todos.Count == 0)
            return "📋 任务列表为空。使用 create 操作创建新任务。";

        var lines = new List<string> { $"## 📋 任务列表 ({todos.Count} 项)" };
        lines.Add("| ID | 状态 | 标题 | 依赖 |");
        lines.Add("|----|------|------|------|");

        foreach (var t in todos)
        {
            var emoji = TodoStore.Emoji(t.Status);
            var deps = t.DependsOn.Count > 0 ? string.Join(", ", t.DependsOn) : "—";
            lines.Add($"| `{t.Id}` | {emoji} {t.Status} | {t.Title} | {deps} |");
        }
        return string.Join("\n", lines);
    }

    // ── delete ──

    private static string Delete(Dictionary<string, object?> args)
    {
        var id = args.GetValueOrDefault("id")?.ToString();
        if (string.IsNullOrWhiteSpace(id))
            return "错误：delete 需要 id 参数";

        var todos = TodoStore.Load();
        var removed = todos.RemoveAll(t => t.Id == id);
        if (removed == 0)
            return $"错误：任务 '{id}' 不存在。使用 list 查看所有任务。";

        // 清理以被删任务为依赖的其他任务的依赖列表
        int cleanedDeps = 0;
        foreach (var t in todos)
        {
            cleanedDeps += t.DependsOn.RemoveAll(d => d == id);
            // 如果依赖清空后任务是 blocked，转为 pending
            if (t.DependsOn.Count == 0 && t.Status == "blocked")
                t.Status = "pending";
        }

        TodoStore.Save(todos);

        var extra = cleanedDeps > 0 ? $"（已从 {cleanedDeps} 个任务的依赖列表中移除）" : "";
        return $"🗑️ 已删除任务 [{id}] {extra}";
    }

    // ── clear ──

    private static string Clear()
    {
        var todos = TodoStore.Load();
        var count = todos.Count;
        todos.Clear();
        TodoStore.Save(todos);
        return $"✅ 已清除 {count} 个任务";
    }

    // ── 辅助 ──

    /// <summary>刷新 TUI 侧边栏 Todo 面板。
    /// 工具在 Agent 后台线程执行——RefreshSidePanel 投递到 UI 线程（PostToUI 非 UI 线程入队，渲染循环 PumpUIQueue 消费）。</summary>
    private static void RefreshSidebar()
    {
        try
        {
            if (TuiManager.Instance?.ActiveScreen is WayCoder.UI.Tui.Screens.ChatScreen screen)
                screen.PostToUI(() => screen.RefreshSidePanel());
        }
        catch { /* 非 TUI 模式，静默忽略 */ }
    }

}

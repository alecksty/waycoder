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
                .Set("enum", JNode.Array().Add("create").Add("update").Add("list").Add("delete").Add("clear"))
                .Set("description", "操作类型"))
            .Set("id", JNode.Object()
                .Set("type", "string")
                .Set("description", "任务 ID（create/update/delete 需要）。使用有意义的 kebab-case 名称，如 'fix-auth-bug'。"))
            .Set("title", JNode.Object()
                .Set("type", "string")
                .Set("description", "任务标题（create/update 可选）"))
            .Set("description", JNode.Object()
                .Set("type", "string")
                .Set("description", "任务详细描述（create/update 可选）"))
            .Set("status", JNode.Object()
                .Set("type", "string")
                .Set("enum", JNode.Array().Add("pending").Add("in_progress").Add("completed").Add("cancelled").Add("blocked"))
                .Set("description", "任务状态（update 操作）"))
            .Set("deps", JNode.Object()
                .Set("type", "array")
                .Set("items", JNode.Object().Set("type", "string"))
                .Set("description", "前置依赖任务 ID 列表（create 操作可选）。被依赖的任务必须全部 completed 后此任务才能开始。"))
            .Set("filter", JNode.Object()
                .Set("type", "string")
                .Set("description", "状态过滤器，逗号分隔（list 操作可选）。如 'pending,in_progress'。")))
        .Set("required", JNode.Array().Add("action"));

    // ── 持久化路径 ──

    private static string StorePath => Path.Combine(
        BashTool.CurrentCwd.Value ?? Directory.GetCurrentDirectory(), ".waycoder", "todos.json"); // cd 后基于被跟踪工作目录，而非进程启动目录

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
            var entries = LoadTodos();
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

        var todos = LoadTodos();
        if (todos.Any(t => t.Id == id))
            return $"错误：任务 ID '{id}' 已存在。请使用 update 操作修改现有任务。";

        var desc = args.GetValueOrDefault("description")?.ToString() ?? "";

        // 解析依赖
        var deps = ParseStringList(args, "deps");

        // 验证依赖存在
        var allIds = todos.Select(t => t.Id).ToHashSet();
        var missing = deps.Where(d => !allIds.Contains(d)).ToList();
        if (missing.Count > 0)
            return $"错误：依赖任务不存在: {string.Join(", ", missing)}。请先创建这些任务或移除无效依赖。";

        var todo = new TodoEntry
        {
            Id = id,
            Title = title,
            Description = desc,
            Status = deps.Count > 0 ? "blocked" : "pending",
            DependsOn = deps,
            CreatedAt = DateTime.UtcNow,
        };
        todos.Add(todo);
        SaveTodos(todos);

        var depNote = deps.Count > 0 ? $"，依赖 [{string.Join(", ", deps)}]" : "";
        return $"✅ 创建任务 [{id}]: {title} | 状态={todo.Status}{depNote}";
    }

    // ── update ──

    private static string Update(Dictionary<string, object?> args)
    {
        var id = args.GetValueOrDefault("id")?.ToString();
        if (string.IsNullOrWhiteSpace(id))
            return "错误：update 需要 id 参数";

        var todos = LoadTodos();
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
            var validStatuses = new[] { "pending", "in_progress", "completed", "cancelled", "blocked" };
            if (!validStatuses.Contains(status))
                return $"错误：无效状态 '{status}'，可用 {string.Join(", ", validStatuses)}";

            // 依赖检查：blocked→in_progress 需所有依赖已完成
            if (status == "in_progress" && todo.DependsOn.Count > 0)
            {
                var incomplete = todo.DependsOn
                    .Where(depId => !todos.Any(t => t.Id == depId && t.Status == "completed"))
                    .ToList();
                if (incomplete.Count > 0)
                    return $"⛔ 无法开始 [{id}]：依赖任务未完成 — {string.Join(", ", incomplete)}。请先完成依赖任务再重试。";
            }

            // 完成任务时：自动解除依赖此任务的其他 blocked 任务
            if (status == "completed")
            {
                var unblocked = new List<string>();
                foreach (var t in todos.Where(t => t.Status == "blocked"))
                {
                    if (t.DependsOn.Contains(id) &&
                        t.DependsOn.All(depId =>
                            depId == id || todos.Any(t2 => t2.Id == depId && t2.Status == "completed")))
                    {
                        t.Status = "pending";
                        unblocked.Add(t.Id);
                    }
                }
                if (unblocked.Count > 0)
                    changes.Add($"解除阻塞: [{string.Join(", ", unblocked)}]");
            }

            todo.Status = status;
            changes.Add($"状态→{status}");
        }

        if (changes.Count == 0)
            return $"ℹ️ 任务 [{id}] 无变更。请提供 title、description 或 status 参数。";

        SaveTodos(todos);
        return $"✅ 更新 [{id}] {todo.Title}: {string.Join(", ", changes)}";
    }

    // ── list ──

    private static string List(Dictionary<string, object?> args)
    {
        var todos = LoadTodos();
        var filter = args.GetValueOrDefault("filter")?.ToString();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var statuses = filter.Split(',', StringSplitOptions.TrimEntries).ToHashSet();
            todos = todos.Where(t => statuses.Contains(t.Status)).ToList();
        }

        if (todos.Count == 0)
            return "📋 任务列表为空。使用 create 操作创建新任务。";

        var lines = new List<string> { $"## 📋 任务列表 ({todos.Count} 项)" };
        lines.Add("| ID | 状态 | 标题 | 依赖 |");
        lines.Add("|----|------|------|------|");

        // 排序：in_progress 最前 → blocked → pending → completed/cancelled 最后
        foreach (var t in todos.OrderBy(t => t.Status switch
                 {
                     "in_progress" => 0, "blocked" => 1, "pending" => 2, _ => 3
                 }).ThenBy(t => t.CreatedAt))
        {
            var emoji = t.Status switch
            {
                "in_progress" => "🔄", "completed" => "✅", "blocked" => "🚫",
                "cancelled" => "❌", _ => "⏳"
            };
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

        var todos = LoadTodos();
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

        SaveTodos(todos);

        var extra = cleanedDeps > 0 ? $"（已从 {cleanedDeps} 个任务的依赖列表中移除）" : "";
        return $"🗑️ 已删除任务 [{id}] {extra}";
    }

    // ── clear ──

    private static string Clear()
    {
        var todos = LoadTodos();
        var count = todos.Count;
        todos.Clear();
        SaveTodos(todos);
        return $"✅ 已清除 {count} 个任务";
    }

    // ── 辅助 ──

    /// <summary>从参数字典中解析字符串列表（支持 JsonArray 和 IEnumerable）</summary>
    private static List<string> ParseStringList(Dictionary<string, object?> args, string key)
    {
        var result = new List<string>();
        if (!args.TryGetValue(key, out var obj) || obj == null) return result;

        if (obj is JNode arr)
            result.AddRange(arr.Items.Select(n => n.AsString() ?? "").Where(s => s != ""));
        else if (obj is System.Collections.IEnumerable en)
            result.AddRange(en.Cast<object>().Select(o => o?.ToString() ?? "").Where(s => s != ""));

        return result;
    }

    /// <summary>刷新 TUI 侧边栏 Todo 面板</summary>
    private static void RefreshSidebar()
    {
        try
        {
            if (TuiManager.Instance?.ActiveScreen is WayCoder.UI.Tui.Screens.ChatScreen screen)
                screen.RefreshSidePanel();
        }
        catch { /* 非 TUI 模式，静默忽略 */ }
    }

    // ── 持久化 ──

    private static List<TodoEntry> LoadTodos()
    {
        try
        {
            var path = StorePath;
            if (!File.Exists(path)) return [];

            var json = File.ReadAllText(path);
            var node = Json.Parse(json);
            if (node is { Kind: JKind.Array } arr)
            {
                return arr.Items.Select(n => new TodoEntry
                {
                    Id = n["id"]?.AsString() ?? "",
                    Title = n["title"]?.AsString() ?? "",
                    Description = n["description"]?.AsString() ?? "",
                    Status = n["status"]?.AsString() ?? "pending",
                    DependsOn = n["depends_on"]?.Items
                        .Select(d => d.AsString() ?? "").Where(s => s != "").ToList() ?? [],
                    CreatedAt = DateTime.TryParse(n["created_at"]?.AsString(), out var dt)
                        ? dt : DateTime.UtcNow,
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            DebugLog.Log("todo", $"加载 todos.json 失败: {ex.Message}");
        }
        return [];
    }

    private static void SaveTodos(List<TodoEntry> todos)
    {
        try
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
        catch (Exception ex)
        {
            DebugLog.Log("todo", $"保存 todos.json 失败: {ex.Message}");
        }
    }

    // ── 数据模型 ──

    private class TodoEntry
    {
        public string Id { get; init; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "pending";
        public List<string> DependsOn { get; set; } = [];
        public DateTime CreatedAt { get; set; }
    }
}

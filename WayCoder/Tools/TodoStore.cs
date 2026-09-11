namespace WayCoder.Tools;

/// <summary>
/// `.waycoder/todos.json` 的**唯一**读写实现，外加任务实体与状态词表。
///
/// `todo` 与 `struct_todo` 两个工具操作的是**同一份文件**，此前各写了一套完整的
/// 「读 → 改 → 写 + 依赖图 CRUD」，于是已经双向漂移，且其中两条是真 bug：
/// ① **写盘耐久性**：`todo` 走 <see cref="Global.WriteAllTextAtomic"/> 且被读写锁包住，
///    `struct_todo` 是裸 <c>File.WriteAllText</c>、全文无锁 ⇒ 可撕裂对方原子写出的文件
///    （正是「多槽位 Agent 与 GUI 2s 定时器并发读写」那个场景，锁就是为它加的）；
/// ② **状态词表分裂**：`todo` 有 `cancelled`，`struct_todo` 没有 ⇒ 前者标的 `cancelled`
///    在后者里会判「无效状态」。词表现在只有 <see cref="ValidStatuses"/> 一份。
///
/// 三个工具侧的分歧（动作集、输出格式）仍各留各的，这里只收敛**共享的那部分**。
/// </summary>
internal static class TodoStore
{
    /// <summary>持久化路径（基于被跟踪工作目录，而非进程启动目录）。</summary>
    internal static string StorePath => Path.Combine(CwdContext.Root, ".waycoder", "todos.json");

    /// <summary>todos.json 读改写串行锁：多槽位 Agent 与 GUI 2s 定时器 / MAUI 状态栏并发读写时防撕裂/丢更新。</summary>
    private static readonly object FileLock = new();

    /// <summary>任务状态词表的**唯一真源**（含 `cancelled`）。</summary>
    internal static readonly string[] ValidStatuses =
        ["pending", "in_progress", "completed", "cancelled", "blocked"];

    /// <summary>状态图标（唯一真源：此前两处 switch，`struct_todo` 那份漏了 cancelled）。</summary>
    internal static string Emoji(string status) => status switch
    {
        "in_progress" => "🔄",
        "completed" => "✅",
        "cancelled" => "❌",
        "blocked" => "🚫",
        _ => "⏳",
    };

    /// <summary>列表排序键：进行中 → 阻塞 → 待办 → 已完成/已取消 → 未知。</summary>
    internal static int Order(string status) => status switch
    {
        "in_progress" => 0,
        "blocked" => 1,
        "pending" => 2,
        "completed" or "cancelled" => 3,
        _ => 4,
    };

    /// <summary>任务条目。</summary>
    internal sealed class Entry
    {
        public string Id { get; init; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "pending";
        public List<string> DependsOn { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>加载任务列表；文件不存在/损坏时返回空列表。</summary>
    internal static List<Entry> Load()
    {
        lock (FileLock)
        {
            try
            {
                var path = StorePath;
                if (!File.Exists(path)) return [];

                var node = Json.Parse(File.ReadAllText(path));
                if (node is { Kind: JKind.Array } arr)
                {
                    return arr.Items.Select(n => new Entry
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
    }

    /// <summary>保存任务列表（**原子写** + 锁；读方不会读到半写文件）。</summary>
    internal static void Save(List<Entry> todos)
    {
        lock (FileLock)
        {
            try
            {
                Global.EnsureDir(StorePath);

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

                // 原子写：先写临时文件再 move 覆盖，避免读方读到半写文件
                //（GUI 2s 定时器 / 多槽位 Agent 并发读）。两个工具都走这里，别再各写一份。
                Global.WriteAllTextAtomic(StorePath, arr.ToJson());
            }
            catch (Exception ex)
            {
                DebugLog.Log("todo", $"保存 todos.json 失败: {ex.Message}");
            }
        }
    }

    /// <summary>从参数字典解析字符串列表（兼容 JNode 数组与 IEnumerable 两种入参形态）。</summary>
    internal static List<string> ParseStringList(Dictionary<string, object?> args, string key)
    {
        var result = new List<string>();
        if (!args.TryGetValue(key, out var obj) || obj == null) return result;

        if (obj is JNode arr)
            result.AddRange(arr.Items.Select(n => n.AsString() ?? "").Where(s => s != ""));
        else if (obj is System.Collections.IEnumerable en)
            result.AddRange(en.Cast<object>().Select(o => o?.ToString() ?? "").Where(s => s != ""));

        return result;
    }

    /// <summary>校验依赖 ID 是否都存在；返回缺失列表（空 = 全部有效）。</summary>
    internal static List<string> MissingDeps(List<Entry> todos, List<string> deps)
    {
        var allIds = todos.Select(t => t.Id).ToHashSet();
        return deps.Where(d => !allIds.Contains(d)).ToList();
    }

    /// <summary>完成某任务后，自动解除「依赖它且依赖已全部完成」的 blocked 任务。返回被解除的 ID。</summary>
    internal static List<string> UnblockDependents(List<Entry> todos, string completedId)
    {
        var unblocked = new List<string>();
        foreach (var t in todos.Where(t => t.Status == "blocked"))
        {
            if (t.DependsOn.Contains(completedId) &&
                t.DependsOn.All(depId =>
                    depId == completedId ||
                    todos.Any(t2 => t2.Id == depId && t2.Status == "completed")))
            {
                t.Status = "pending";
                unblocked.Add(t.Id);
            }
        }
        return unblocked;
    }

    /// <summary>某任务启动前检查依赖是否都已完成的未完成项（空 = 可以开始）。</summary>
    internal static List<string> IncompleteDeps(List<Entry> todos, Entry todo)
        => todo.DependsOn
            .Where(depId => !todos.Any(t => t.Id == depId && t.Status == "completed"))
            .ToList();
}

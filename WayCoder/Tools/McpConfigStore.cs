using System.Text;

namespace WayCoder.Tools;

/// <summary>
/// `.waycoder/mcp_servers.json` 的**唯一**读写实现（对标 <see cref="TodoStore"/>）。
///
/// 此前三处各写一份完整的「读 → 去重 → 写」：
///   ① `Program.Commands`（初始化时写模板）、
///   ② `Tools/McpClient.AddServer`（`/mcp add`）、
///   ③ `Infra/ImportHelper.WriteMcpServersAsync`（从 Claude Code / OpenCode 导入）。
/// 和当年 todos.json 的情形一模一样，且**已经双向漂移**：
///
/// - **去重口径不同**：AddServer 用 `StringComparer.OrdinalIgnoreCase`，ImportHelper 用
///   `ToHashSet()`（默认区分大小写）⇒ 同一份配置，一处判「已存在」、另一处判「不存在」，
///   导入时会往同一个文件里写进重复条目。
/// - **三处都是 `File.WriteAllText(..., Encoding.UTF8)`**，同时踩两条铁律：
///   · **非原子**：崩溃/断电留下半截文件 = 整份 MCP 配置读不回来（`WriteAllTextAtomic` 就是为它写的）；
///   · **凭空带 BOM**：.NET 的 `Encoding.UTF8` 静态实例是带 BOM 的。这份文件用户会手编、
///     外部工具（jq / `python -c "json.load"`）也会解析，而它们对 BOM 不容忍 ——
///     与 NotebookEditTool 写坏 .ipynb 是同一类问题，且「自读自写」永远看不出来（双参
///     `File.ReadAllText` 会剥 BOM），只有外部消费者才暴露。
///
/// 三个调用点各自的**输出文案**仍各留各的，这里只收敛共享的那部分：读、去重、写。
/// </summary>
public static class McpConfigStore
{
    /// <summary>读改写串行锁：`/mcp add` 与导入可能并发（多槽位 Agent 同时用工具）。</summary>
    private static readonly object Lock = new();

    /// <summary>模板示例条目的标记：`_comment` 含此词的条目在读取时丢弃，不当成真服务器。</summary>
    private const string SampleMarker = "示例";

    /// <summary>读服务器列表（已剔除模板示例条目）；文件不存在或损坏返回空数组，不抛。</summary>
    public static JNode Load(string path)
    {
        lock (Lock)
        {
            var result = JNode.Array();
            try
            {
                if (!File.Exists(path)) return result;
                // 双参重载走 StreamReader(detectEncodingFromByteOrderMarks: true) → 会剥 BOM，
                // 用户手工编辑过的文件（或其编辑器加了 BOM）也能正常读
                if (Json.Parse(File.ReadAllText(path, Encoding.UTF8)) is { Kind: JKind.Array } arr)
                {
                    foreach (var item in arr.Items)
                    {
                        if (item == null) continue;
                        if ((item["_comment"]?.AsString() ?? "").Contains(SampleMarker)) continue;
                        result.Add(item.Clone() ?? item);
                    }
                }
            }
            catch { /* 旧配置损坏 → 当空配置处理，由调用方重建 */ }
            return result;
        }
    }

    /// <summary>原子写回（先写 .tmp 再 Move）+ 无 BOM：状态/配置文件不得裸写，也不该凭空加 BOM。</summary>
    public static void Save(string path, JNode servers)
    {
        lock (Lock)
        {
            Global.EnsureDir(path);
            Global.WriteAllTextAtomic(path, servers.ToJson(true));
        }
    }

    /// <summary>
    /// 按 name 去重追加一个服务器并落盘。去重**忽略大小写**（统一口径 —— 此前两处判定相反）。
    /// 已存在返回 false 并给出 <paramref name="error"/>。
    /// </summary>
    public static bool TryAdd(string path, JNode server, out string? error)
    {
        error = null;
        var name = server["name"]?.AsString();
        if (string.IsNullOrEmpty(name)) { error = "服务器配置缺少 name"; return false; }

        lock (Lock) // lock 可重入：Load/Save 内部同锁
        {
            var existing = Load(path);
            if (HasServer(existing, name))
            {
                error = $"服务器 {name} 已存在配置中";
                return false;
            }
            existing.Add(server);
            Save(path, existing);
            return true;
        }
    }

    /// <summary>批量去重追加（导入用），返回实际新增条数；全为重复时不写盘。</summary>
    public static int TryAddRange(string path, IEnumerable<JNode> servers)
    {
        lock (Lock)
        {
            var existing = Load(path);
            var names = existing.Items
                .Select(e => e?["name"]?.AsString())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var added = 0;
            foreach (var s in servers)
            {
                var name = s["name"]?.AsString();
                if (string.IsNullOrEmpty(name) || !names.Add(name)) continue;
                existing.Add(s);
                added++;
            }
            if (added > 0) Save(path, existing);
            return added;
        }
    }

    /// <summary>服务器名是否已存在（忽略大小写）——去重口径的唯一实现。</summary>
    public static bool HasServer(JNode servers, string name)
        => servers.Items.Any(e => string.Equals(e?["name"]?.AsString(), name, StringComparison.OrdinalIgnoreCase));
}

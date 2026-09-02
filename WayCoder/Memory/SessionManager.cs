using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 会话持久化 - 保存和恢复对话。
///
/// WayCoder 将会话状态提炼为：消息 + 模型配置的 JSON 转储。
///
/// 多智能体（槽位）隔离：各方法接受可选 <c>slot</c>（0-9），
/// 传入时会话记录写入 <c>sessions/slot{N}/</c> 子目录，互不干扰；
/// 缺省（-1）走全局共享目录（终端 TUI 等沿用旧行为）。
/// </summary>
public static class SessionManager
{
    private static string SessionsDir => Global.GlobalConfigPath("sessions");

    /// <summary>旧会话目录（向后兼容读取）</summary>
    private static readonly string LegacySessionsDir =
        Path.Combine(Global.Home, ".corecoder", "sessions");

    private static readonly Regex SafeSessionRegex = new(@"[^A-Za-z0-9._-]+", RegexOptions.None, TimeSpan.FromMilliseconds(100));
    private const int MaxSessionIdLen = 100;

    /// <summary>按槽位返回会话目录：slot&lt;0 用全局目录，否则用 sessions/slot{N}/。</summary>
    private static string SessionsDirFor(int slot)
        => slot < 0 ? SessionsDir : Path.Combine(SessionsDir, $"slot{slot}");

    /// <summary>原子写会话文件：先写临时文件再同卷替换，避免进程崩溃/断电留下半截损坏的会话 JSON。</summary>
    private static void AtomicWrite(string path, string content)
    {
        Global.EnsureDir(path);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        File.Move(tmp, path, overwrite: true); // 同卷原子替换
    }

    /// <summary>
    /// 将对话保存到磁盘。返回会话 ID。
    /// </summary>
    public static string SaveSession(List<JNode> messages, string model, string? sessionId = null, int slot = -1)
    {
        var dir = SessionsDirFor(slot);
        Directory.CreateDirectory(dir);
        PruneOldSessions(dir); // 保留最近 200 个会话 / 删 30 天前，防磁盘无限累积

        sessionId = NormalizeSessionId(sessionId);

        var data = JNode.Object()
            .Set("id", sessionId)
            .Set("model", model)
            .Set("saved_at", Global.LogStamp());

        var msgArr = JNode.Array();
        foreach (var m in messages) msgArr.Add(m);
        data.Set("messages", msgArr);

        var path = BuildSessionPath(dir, sessionId);
        AtomicWrite(path, data.ToJson(true));

        return sessionId;
    }

    /// <summary>
    /// 加载已保存的会话。返回 (messages, model) 或 null。
    /// 槽位隔离模式（slot&gt;=0）只读该槽位目录，不回退旧目录。
    /// </summary>
    public static (List<JNode> Messages, string Model)? LoadSession(string sessionId, int slot = -1)
    {
        var dir = SessionsDirFor(slot);
        var path = BuildSessionPath(dir, sessionId);

        // 槽位隔离模式不回退旧目录；全局模式先试新目录、回退旧目录
        if (!File.Exists(path) && slot < 0)
        {
            var legacyPath = BuildSessionPath(LegacySessionsDir, sessionId);
            if (File.Exists(legacyPath)) path = legacyPath;
            else return null;
        }
        else if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            var data = Json.Parse(json);
            if (data?["messages"] is { Kind: JKind.Array } arr && data["model"]?.AsString() is { } model)
            {
                var messages = arr.Items.ToList();
                return (messages, model);
            }
        }
        catch
        {
            // 损坏或截断的会话文件不应导致恢复崩溃
        }

        return null;
    }

    /// <summary>删除指定会话</summary>
    public static bool DeleteSession(string sessionId, int slot = -1)
    {
        var path = BuildSessionPath(SessionsDirFor(slot), sessionId);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    /// <summary>
    /// 清空会话记录，返回删除的文件数。
    /// 槽位隔离模式（slot&gt;=0）只清空该槽位目录；否则清空全局目录 + 旧目录。
    /// </summary>
    public static int DeleteAllSessions(int slot = -1)
    {
        var deleted = 0;
        if (slot >= 0)
        {
            var dir = SessionsDirFor(slot);
            if (!Directory.Exists(dir)) return 0;
            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                try { File.Delete(f); deleted++; } catch { }
            }
            return deleted;
        }

        foreach (var dir in new[] { SessionsDir, LegacySessionsDir })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                try { File.Delete(f); deleted++; } catch { }
            }
        }
        return deleted;
    }

    /// <summary>重命名会话</summary>
    public static bool RenameSession(string oldId, string newId, int slot = -1)
    {
        var dir = SessionsDirFor(slot);
        var oldPath = BuildSessionPath(dir, oldId);
        if (!File.Exists(oldPath)) return false;

        var newIdNormalized = NormalizeSessionId(newId);
        var newPath = BuildSessionPath(dir, newIdNormalized);

        // 如果新路径已存在，不覆盖
        if (File.Exists(newPath) && !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            // 更新文件中的 id 字段
            var json = File.ReadAllText(oldPath);
            var data = Json.Parse(json);
            if (data != null)
            {
                data.Set("id", newIdNormalized);
                data.Set("saved_at", Global.LogStamp());
            }
            AtomicWrite(newPath, data?.ToJson(true) ?? json);

            // 删除旧文件（如果路径不同）
            if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                File.Delete(oldPath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 列出可用会话，最新的在前。支持 limit/offset 分页。
    /// 槽位隔离模式（slot&gt;=0）只扫描该槽位目录。
    /// </summary>
    public static List<SessionInfo> ListSessions(int limit = 20, int offset = 0, int slot = -1)
    {
        var dirs = slot >= 0
            ? new[] { SessionsDirFor(slot) }
            : new[] { SessionsDir, LegacySessionsDir };

        // 先收集所有候选（含 saved_at），统一按 saved_at 降序（最新在前），
        // 再用 seen 集合去重（覆盖 offset 跳过的条目，避免跨目录同 id 重复）。
        var candidates = new List<(string Id, string Model, string SavedAt, string Preview, int Count)>();
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(f);
                    var data = Json.Parse(json);
                    if (data == null) continue;
                    var id = data["id"]?.AsString() ?? Path.GetFileNameWithoutExtension(f);

                    var preview = "";
                    int msgCount = 0;
                    if (data["messages"] is { Kind: JKind.Array } arr)
                    {
                        msgCount = arr.Count;
                        foreach (var m in arr.Items)
                        {
                            if (m["role"]?.AsString() == "user" && m["content"]?.AsString() is { } c)
                            {
                                preview = c.Length > 80 ? ContextManager.TruncateByRunes(c, 80) : c;
                                break;
                            }
                        }
                    }

                    candidates.Add((
                        id,
                        data["model"]?.AsString() ?? "?",
                        data["saved_at"]?.AsString() ?? "?",
                        preview,
                        msgCount));
                }
                catch
                {
                    continue;
                }
            }
        }

        // 最新在前（saved_at 为 yyyy-MM-dd HH:mm:ss，字符串降序即时间降序；缺失值排最后），
        // 去重后统一分页。
        var result = new List<SessionInfo>();
        var seen = new HashSet<string>();
        foreach (var c in candidates.OrderByDescending(c => c.SavedAt == "?" ? "" : c.SavedAt))
        {
            if (!seen.Add(c.Id)) continue;
            result.Add(new SessionInfo(c.Id, c.Model, c.SavedAt, c.Preview, c.Count));
        }

        return result.Skip(offset).Take(limit).ToList();
    }

    private static string NormalizeSessionId(string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return NewSessionId();

        var name = sessionId.Trim().Replace('\\', '/').Split('/')[^1];
        name = SafeSessionRegex.Replace(name, "-").Trim(".-_".ToCharArray());
        if (name.Length > MaxSessionIdLen)
            name = name[..MaxSessionIdLen].Trim(".-_".ToCharArray());

        return string.IsNullOrEmpty(name) ? NewSessionId() : name;
    }

    private static string NewSessionId()
    {
        return $"session_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
    }

    /// <summary>生成新的会话 ID（公开，供外部使用）</summary>
    public static string CreateNewSessionId() => NewSessionId();

    private static string BuildSessionPath(string dir, string sessionId)
    {
        var path = Path.GetFullPath(Path.Combine(dir, $"{NormalizeSessionId(sessionId)}.json"));
        var root = Path.GetFullPath(dir);

        // 路径穿越防护
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("无效的会话 ID");

        return path;
    }

    /// <summary>清理旧会话文件：保留最近 200 个 / 删 30 天前，防磁盘无限累积。</summary>
    private static void PruneOldSessions(string dir)
    {
        Global.EnforceMaxFiles(dir, "*.json", Global.MaxSessionsKeep);
        Global.CleanupOldFiles(dir, "*.json", Global.SessionRetentionDays);
    }
}

/// <summary>
/// 会话摘要信息。
/// </summary>
public record SessionInfo(string Id, string Model, string SavedAt, string Preview, int MessageCount = 0);

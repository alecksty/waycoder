using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 会话持久化 - 保存和恢复对话。
///
/// WayCoder 将会话状态提炼为：消息 + 模型配置的 JSON 转储。
/// </summary>
public static class SessionManager
{
    private static string SessionsDir => Global.GlobalConfigPath("sessions");

    /// <summary>旧会话目录（向后兼容读取）</summary>
    private static readonly string LegacySessionsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".corecoder", "sessions");

    private static readonly Regex SafeSessionRegex = new(@"[^A-Za-z0-9._-]+", RegexOptions.None, TimeSpan.FromMilliseconds(100));
    private const int MaxSessionIdLen = 100;

    /// <summary>
    /// 将对话保存到磁盘。返回会话 ID。
    /// </summary>
    public static string SaveSession(List<JNode> messages, string model, string? sessionId = null)
    {
        Directory.CreateDirectory(SessionsDir);

        sessionId = NormalizeSessionId(sessionId);

        var data = JNode.Object()
            .Set("id", sessionId)
            .Set("model", model)
            .Set("saved_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        var msgArr = JNode.Array();
        foreach (var m in messages) msgArr.Add(m);
        data.Set("messages", msgArr);

        var path = SessionPath(sessionId);
        File.WriteAllText(path, data.ToJson(true));

        return sessionId;
    }

    /// <summary>
    /// 加载已保存的会话。返回 (messages, model) 或 null。
    /// </summary>
    public static (List<JNode> Messages, string Model)? LoadSession(string sessionId)
    {
        // 先试新目录，回退旧目录
        var path = SessionPath(sessionId);
        if (!File.Exists(path))
        {
            var legacyPath = LegacySessionPath(sessionId);
            if (File.Exists(legacyPath)) path = legacyPath;
            else return null;
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
    public static bool DeleteSession(string sessionId)
    {
        var path = SessionPath(sessionId);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    /// <summary>清空全部会话记录（新目录 + 旧目录），返回删除的文件数。</summary>
    public static int DeleteAllSessions()
    {
        var deleted = 0;
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
    public static bool RenameSession(string oldId, string newId)
    {
        var oldPath = SessionPath(oldId);
        if (!File.Exists(oldPath)) return false;

        var newIdNormalized = NormalizeSessionId(newId);
        var newPath = SessionPath(newIdNormalized);

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
                data.Set("saved_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            File.WriteAllText(newPath, data?.ToJson(true) ?? json);

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
    /// </summary>
    public static List<SessionInfo> ListSessions(int limit = 20, int offset = 0)
    {
        var sessions = new List<SessionInfo>();
        var skipped = 0;
        // 扫描新目录和旧目录
        foreach (var dir in new[] { SessionsDir, LegacySessionsDir })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "*.json")
                         .OrderByDescending(f => f))
            {
                try
                {
                    var json = File.ReadAllText(f);
                    var data = Json.Parse(json);
                    if (data == null) continue;
                    var id = data["id"]?.AsString() ?? Path.GetFileNameWithoutExtension(f);
                    // 去重
                    if (sessions.Any(s => s.Id == id)) continue;

                    var preview = "";
                    int msgCount = 0;
                    if (data["messages"] is { Kind: JKind.Array } arr)
                    {
                        msgCount = arr.Count;
                        foreach (var m in arr.Items)
                        {
                            if (m["role"]?.AsString() == "user" && m["content"]?.AsString() is { } c)
                            {
                                preview = c.Length > 80 ? c[..80] : c;
                                break;
                            }
                        }
                    }

                    // 分页：跳过 offset 条再开始收集
                    if (skipped < offset)
                    {
                        skipped++;
                        continue;
                    }

                    sessions.Add(new SessionInfo(
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

                if (sessions.Count >= limit) break;
            }
            if (sessions.Count >= limit) break;
        }

        return sessions;
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

    private static string SessionPath(string sessionId)
    {
        return BuildSessionPath(SessionsDir, sessionId);
    }

    private static string LegacySessionPath(string sessionId)
    {
        return BuildSessionPath(LegacySessionsDir, sessionId);
    }

    private static string BuildSessionPath(string dir, string sessionId)
    {
        var path = Path.GetFullPath(Path.Combine(dir, $"{NormalizeSessionId(sessionId)}.json"));
        var root = Path.GetFullPath(dir);

        // 路径穿越防护
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("无效的会话 ID");

        return path;
    }
}

/// <summary>
/// 会话摘要信息。
/// </summary>
public record SessionInfo(string Id, string Model, string SavedAt, string Preview, int MessageCount = 0);

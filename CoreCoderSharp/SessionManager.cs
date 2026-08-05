using System.Text.Json;
using System.Text.RegularExpressions;

namespace CoreCoderSharp;

/// <summary>
/// 会话持久化 - 保存和恢复对话。
///
/// CoreCoder 将会话状态提炼为：消息 + 模型配置的 JSON 转储。
/// </summary>
public static class SessionManager
{
    private static readonly string SessionsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".corecoder", "sessions");

    private static readonly Regex SafeSessionRegex = new(@"[^A-Za-z0-9._-]+", RegexOptions.None, TimeSpan.FromMilliseconds(100));
    private const int MaxSessionIdLen = 100;

    /// <summary>
    /// 将对话保存到磁盘。返回会话 ID。
    /// </summary>
    public static string SaveSession(List<JsonObject> messages, string model, string? sessionId = null)
    {
        Directory.CreateDirectory(SessionsDir);

        sessionId = NormalizeSessionId(sessionId);

        var data = new JsonObject
        {
            ["id"] = sessionId,
            ["model"] = model,
            ["saved_at"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["messages"] = new JsonArray(messages.Select(m => (JsonNode?)m).ToArray()),
        };

        var path = SessionPath(sessionId);
        File.WriteAllText(path, data.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

        return sessionId;
    }

    /// <summary>
    /// 加载已保存的会话。返回 (messages, model) 或 null。
    /// </summary>
    public static (List<JsonObject> Messages, string Model)? LoadSession(string sessionId)
    {
        var path = SessionPath(sessionId);
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonNode.Parse(json);
            if (data?["messages"]?.AsArray() is { } arr && data["model"]?.GetValue<string>() is { } model)
            {
                var messages = arr.Select(n => n?.AsObject() ?? new JsonObject()).ToList();
                return (messages, model);
            }
        }
        catch
        {
            // 损坏或截断的会话文件不应导致恢复崩溃
        }

        return null;
    }

    /// <summary>
    /// 列出可用会话，最新的在前，最多 20 条。
    /// </summary>
    public static List<SessionInfo> ListSessions()
    {
        if (!Directory.Exists(SessionsDir)) return [];

        var sessions = new List<SessionInfo>();
        foreach (var f in Directory.GetFiles(SessionsDir, "*.json")
                     .OrderByDescending(f => f))
        {
            try
            {
                var json = File.ReadAllText(f);
                var data = JsonNode.Parse(json);
                if (data == null) continue;

                // 取第一条用户消息作为预览
                var preview = "";
                if (data["messages"]?.AsArray() is { } arr)
                {
                    foreach (var m in arr)
                    {
                        if (m?["role"]?.GetValue<string>() == "user" && m["content"]?.GetValue<string>() is { } c)
                        {
                            preview = c.Length > 80 ? c[..80] : c;
                            break;
                        }
                    }
                }

                sessions.Add(new SessionInfo(
                    data["id"]?.GetValue<string>() ?? Path.GetFileNameWithoutExtension(f),
                    data["model"]?.GetValue<string>() ?? "?",
                    data["saved_at"]?.GetValue<string>() ?? "?",
                    preview));
            }
            catch
            {
                continue;
            }

            if (sessions.Count >= 20) break;
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

    private static string SessionPath(string sessionId)
    {
        var path = Path.GetFullPath(Path.Combine(SessionsDir, $"{NormalizeSessionId(sessionId)}.json"));
        var root = Path.GetFullPath(SessionsDir);

        // 路径穿越防护
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("无效的会话 ID");

        return path;
    }
}

/// <summary>
/// 会话摘要信息。
/// </summary>
public record SessionInfo(string Id, string Model, string SavedAt, string Preview);

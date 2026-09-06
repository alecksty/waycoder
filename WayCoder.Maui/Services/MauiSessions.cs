using System.Globalization;
using WayCoder;
using WayCoder.Infra;
using WayCoder.Maui.Models;

namespace WayCoder.Maui.Services;

/// <summary>
/// 移动端多会话（替代单会话 <see cref="MauiSessionStore"/>）——复用桌面 <see cref="SessionManager"/>
/// 的 file-per-session JSON 格式（<c>Global.Home/.waycoder/sessions/*.json</c>，slot=-1 全局，桌面可互读）。
/// 只存 User / Assistant 正文 RawText（含 «» 中间格式，桌面侧原样保留）；不存思考/工具消息。
/// 当前打开会话 id 记在 MAUI Preferences（key <see cref="LastSessionKey"/>）。
/// </summary>
public static class MauiSessions
{
    /// <summary>Preferences key：当前打开会话 id（重启回同一会话）。</summary>
    public const string LastSessionKey = "maui_last_session";

    // ═══════ 消息互转（ChatMessage ⇄ JNode {role,content}） ═══════

    /// <summary>UI 消息 → 会话 JNode 数组（仅 User/Assistant 非空正文）。</summary>
    public static List<JNode> ToNodes(IEnumerable<ChatMessage> messages)
    {
        var list = new List<JNode>();
        foreach (var m in messages)
        {
            if (m.Role != ChatRole.User && m.Role != ChatRole.Assistant) continue;
            if (string.IsNullOrEmpty(m.RawText)) continue;
            list.Add(JNode.Object()
                .Set("role", m.Role == ChatRole.User ? "user" : "assistant")
                .Set("content", m.RawText));
        }
        return list;
    }

    /// <summary>会话 JNode → UI 消息（RawText；富文本由调用方惰性重建）。</summary>
    public static List<ChatMessage> FromNodes(IEnumerable<JNode> nodes)
    {
        var result = new List<ChatMessage>();
        foreach (var n in nodes)
        {
            var role = n["role"]?.AsString() ?? "";
            var content = n["content"]?.AsString();
            if (content == null) continue;
            result.Add(new ChatMessage
            {
                Role = role == "user" ? ChatRole.User : ChatRole.Assistant,
                RawText = content,
            });
        }
        return result;
    }

    // ═══════ CRUD（转发桌面 SessionManager，slot=-1 全局） ═══════

    public static List<SessionInfo> List(int limit = 50)
        => SessionManager.ListSessions(limit: limit, offset: 0, slot: -1);

    /// <summary>保存当前会话正文，返回 sessionId（原子写；内部自动清理 30 天前旧会话）。</summary>
    public static string Save(IEnumerable<ChatMessage> messages, string model, string sessionId)
        => SessionManager.SaveSession(ToNodes(messages), model, sessionId);

    /// <summary>加载会话（返回 (消息, model) 或 null——文件不存在/损坏）。</summary>
    public static (List<JNode> Messages, string Model)? Load(string sessionId)
        => SessionManager.LoadSession(sessionId, -1);

    public static bool Delete(string sessionId) => SessionManager.DeleteSession(sessionId, -1);

    public static bool Rename(string oldId, string newId) => SessionManager.RenameSession(oldId, newId, -1);

    public static string NewId() => SessionManager.CreateNewSessionId();

    /// <summary>会话是否存在（供启动恢复判断）。</summary>
    public static bool Exists(string sessionId)
    {
        try { return List(200).Any(s => s.Id == sessionId); } catch { return false; }
    }

    // ═══════ 当前会话 id（Preferences 持久） ═══════

    public static string CurrentSessionId()
    {
        try { return Preferences.Get(LastSessionKey, ""); } catch { return ""; }
    }

    public static void SetCurrentSessionId(string sessionId)
    {
        try { Preferences.Set(LastSessionKey, sessionId); } catch { }
    }

    // ═══════ 旧单会话迁移（一次） ═══════

    /// <summary>
    /// 首启迁移：旧 maui_session.txt（单会话）转成一个 SessionManager 会话并删除旧文件。
    /// 返回迁移出的会话 id（无旧数据返回 null）。
    /// </summary>
    public static string? MigrateLegacySingleSession(string model)
    {
        try
        {
            if (!MauiSessionStore.Exists()) return null;
            var legacy = MauiSessionStore.Load(); // 旧文件：User/Assistant RawText
            if (legacy.Count == 0) { MauiSessionStore.Clear(); return null; }
            var id = NewId();
            SessionManager.SaveSession(ToNodes(legacy), model, id);
            MauiSessionStore.Clear();
            return id;
        }
        catch { return null; }
    }

    // ═══════ 显示 helper ═══════

    /// <summary>saved_at(yyyy-MM-dd HH:mm:ss) → 相对时间（刚刚/x 分钟前/x 小时前/x 天前/日期）。</summary>
    public static string RelativeTime(string savedAt)
    {
        if (!DateTime.TryParseExact(savedAt, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var t)) return savedAt;
        var span = DateTime.Now - t;
        if (span.TotalSeconds < 60) return "刚刚";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} 分钟前";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} 小时前";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} 天前";
        return t.ToString("MM-dd HH:mm");
    }
}

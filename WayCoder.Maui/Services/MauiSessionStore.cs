using System.Text;
using WayCoder.Maui.Models;

namespace WayCoder.Maui.Services;

/// <summary>
/// 移动端会话持久化：把聊天的「对话正文」存到 <c>Global.Home/maui_session.txt</c>，
/// 供「继续会话 / 新的会话」恢复上次退出的会话。
/// 只保存 User / Assistant 消息的 RawText——**不保存**思考过程（Reasoning 本就不在 RawText）
/// 与工具调用的返回结果（ToolDetail/工具消息，太多会撑爆会话）。
/// 手写长度前缀格式（AOT 无反射）：
///   R{role}\n {rawLen}\n{raw}\n （每条消息 3 行）
/// </summary>
public static class MauiSessionStore
{
    private static string StorePath => Path.Combine(WayCoder.Global.Home, "maui_session.txt");

    public static bool Exists() => File.Exists(StorePath);

    /// <summary>保存对话正文（仅 User/Assistant，跳过思考/工具消息）。</summary>
    public static void Save(IEnumerable<ChatMessage> messages)
    {
        try
        {
            var sb = new StringBuilder();
            foreach (var m in messages)
            {
                if (m.Role != ChatRole.User && m.Role != ChatRole.Assistant) continue;
                if (string.IsNullOrEmpty(m.RawText)) continue;
                sb.Append('R').Append((int)m.Role).Append('\n');
                AppendField(sb, m.RawText);
            }
            File.WriteAllText(StorePath, sb.ToString());
        }
        catch { /* 落盘失败静默：不影响聊天 */ }
    }

    /// <summary>读取上次会话正文；无会话返回空列表。</summary>
    public static List<ChatMessage> Load()
    {
        var result = new List<ChatMessage>();
        try
        {
            if (!File.Exists(StorePath)) return result;
            var lines = File.ReadAllLines(StorePath);
            int i = 0;
            while (i < lines.Length)
            {
                if (lines[i].Length < 2 || lines[i][0] != 'R') { i++; continue; }
                var role = (ChatRole)int.Parse(lines[i][1..]);
                var raw = ReadField(lines, ref i);
                result.Add(new ChatMessage { Role = role, RawText = raw });
            }
        }
        catch { }
        return result;
    }

    /// <summary>清空会话（点「新的会话」后调用）。</summary>
    public static void Clear()
    {
        try { if (File.Exists(StorePath)) File.Delete(StorePath); } catch { }
    }

    private static void AppendField(StringBuilder sb, string s)
    {
        sb.Append(s.Length).Append('\n').Append(s).Append('\n');
    }

    private static string ReadField(string[] lines, ref int i)
    {
        if (i + 1 >= lines.Length) { i = lines.Length; return ""; }
        if (!int.TryParse(lines[i], out var len) || len < 0) { i++; return ""; }
        var field = len <= lines[i + 1].Length ? lines[i + 1][..len] : lines[i + 1];
        i += 2;
        return field;
    }
}

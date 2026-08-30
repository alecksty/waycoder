using System.Text;
using WayCoder;
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

    /// <summary>保存对话正文（仅 User/Assistant，跳过思考/工具消息）。
    /// 防无限增长：只存最近 <see cref="Config.MaxChatMessages"/> 条；文件超 <see cref="Global.MaxMauiSessionBytes"/>
    /// 按**完整消息**从最旧丢弃（截断落在消息边界，不会切半长度前缀流导致恢复乱码）。</summary>
    public static void Save(IEnumerable<ChatMessage> messages)
    {
        try
        {
            var list = messages.ToList();
            int max = Config.Instance.MaxChatMessages;
            if (max > 0 && list.Count > max)
                list.RemoveRange(0, list.Count - max); // 只存最近 N 条，防会话文件无限涨

            // 字节上限：逆序（最新在前）逐条凑预算，截断保证完整消息边界
            var keep = new List<ChatMessage>();
            long used = 0;
            long budget = Global.MaxMauiSessionBytes;
            bool capped = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var m = list[i];
                if (m.Role != ChatRole.User && m.Role != ChatRole.Assistant) continue;
                if (string.IsNullOrEmpty(m.RawText)) continue;
                long len = m.RawText.Length + 12; // 每条约 12 字节格式开销
                if (budget > 0 && used + len > budget) { capped = true; break; } // 超限：丢弃更旧的完整消息
                keep.Add(m);
                used += len;
            }
            keep.Reverse(); // 转回正序

            var sb = new StringBuilder();
            if (capped)
                sb.Append("… 会话过长，已截断（仅保留最近内容）…\n");
            foreach (var m in keep)
            {
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
            // 整串索引解析（不能用 ReadAllLines 分行——多行 RawText 会被 \n 拆散破坏长度前缀）
            var content = File.ReadAllText(StorePath);
            int i = 0;
            while (i < content.Length)
            {
                if (content[i] != 'R') { i++; continue; }
                i++;
                int roleStart = i;
                while (i < content.Length && content[i] != '\n') i++;
                var role = (ChatRole)int.Parse(content[roleStart..i]);
                i++; // 跳过 \n
                var raw = ReadField(content, ref i);
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

    /// <summary>按长度前缀精确读取字段（raw 含 \n 也能正确往返）。</summary>
    private static string ReadField(string content, ref int i)
    {
        int lenStart = i;
        while (i < content.Length && content[i] != '\n') i++;
        if (!int.TryParse(content[lenStart..i], out var len) || len < 0) { i++; return ""; }
        i++; // 跳过 \n
        if (i + len > content.Length) { i = content.Length; return ""; }
        var raw = content.Substring(i, len);
        i += len;
        if (i < content.Length && content[i] == '\n') i++; // 跳过字段尾 \n
        return raw;
    }
}

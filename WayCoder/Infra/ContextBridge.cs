using System.Diagnostics;
using System.Text;

namespace WayCoder.Infra;

/// <summary>
/// 跨工具上下文桥接 —— 从 Claude Code / Codex / OpenCode / Crush 会话「接着跑」。
///
/// 读取竞品会话的「聊天内容 + todo 清单」，叠加当前 git 状态，组装成交接文档注入 WayCoder 新会话，
/// 让用户从别的编程智能体切到 WayCoder 后能无缝续跑。
///
/// 数据源：
///   - Claude Code: ~/.claude/projects/*/*.jsonl（JSONL，逐行 JSON）
///   - Codex:       ~/.codex/sessions/**/*.jsonl（JSONL）
///   - OpenCode:    ~/.local/share/opencode/opencode.db（SQLite，session/message/part）
///   - Crush:       &lt;project&gt;/.crush/crush.db（SQLite，sessions/messages）+ projects.json 定位
///   - Aider:       &lt;project&gt;/.aider.chat.history.md（纯 Markdown）
///   - Gemini CLI:  ~/.gemini/{tmp,history}/&lt;slug&gt;/chats/session-*.jsonl（JSONL，slug=slugify(basename)，cwd 存 .project_root / projects.json）
///
/// 纯逻辑（无 LLM 依赖）：聊天直接搬文本，todo 取竞品自带的待办状态，git 执行只读命令。
/// SQLite 读取走 <see cref="SqliteReader"/>（零依赖手写解析器）。
///
/// 用法：
///   ContextBridge.FindSessions(cwd, tool)     → 候选会话列表（按更新时间倒序）
///   ContextBridge.BuildHandoffDoc(session, cwd) → 交接文档 Markdown
/// </summary>
public static class ContextBridge
{
    /// <summary>一条外部会话的定位信息。Path=数据文件路径，SessionId=db 内主键（JSONL 源为 null）。</summary>
    public record ExternalSession(string Tool, string Path, DateTime UpdatedAt, string Title, string Cwd, string? SessionId = null)
    {
        public string ToolLabel => Tool switch
        {
            "claude" => "Claude Code",
            "codex" => "Codex",
            "opencode" => "OpenCode",
            "crush" => "Crush",
            "aider" => "Aider",
            "gemini" => "Gemini CLI",
            _ => Tool,
        };
    }

    static readonly string Home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    static readonly string ClaudeProjectsDir = Path.Combine(Home, ".claude", "projects");
    static readonly string CodexSessionsDir = Path.Combine(Home, ".codex", "sessions");
    static readonly string OpenCodeDb = Path.Combine(Home, ".local", "share", "opencode", "opencode.db");
    static readonly string CrushProjectsJson = Path.Combine(Home, ".local", "share", "crush", "projects.json");
    static readonly string GeminiTmpDir = Path.Combine(Home, ".gemini", "tmp");
    static readonly string GeminiHistoryDir = Path.Combine(Home, ".gemini", "history");
    static readonly string GeminiProjectsJson = Path.Combine(Home, ".gemini", "projects.json");

    const int MaxChatLines = 60;      // 最多保留的聊天行数（user/assistant/工具）
    const int MaxLineRunes = 600;     // 单条聊天文本截断长度（按码点）
    const int MaxCwdScanLines = 120;  // 查找 cwd 时最多扫的行数

    // ─────────────────────────────────────────────────────────────
    // 会话查找
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 查找匹配当前 cwd 的竞品会话。toolFilter 为 "claude"/"codex"/"opencode"/"crush"/null（全部）。
    /// 匹配规则：会话记录的 cwd 是当前 cwd 或其祖先目录（从子目录启动也能找到项目根会话）。
    /// </summary>
    public static List<ExternalSession> FindSessions(string cwd, string? toolFilter = null)
    {
        var result = new List<ExternalSession>();
        var norm = Normalize(cwd);

        if (toolFilter is null or "claude" && Directory.Exists(ClaudeProjectsDir))
        {
            foreach (var projDir in SafeGetDirs(ClaudeProjectsDir))
            {
                foreach (var f in SafeGetFiles(projDir, "*.jsonl"))
                {
                    var s = ProbeClaude(f);
                    if (s != null && IsRelevant(norm, s.Cwd))
                        result.Add(s);
                }
            }
        }

        if (toolFilter is null or "codex" && Directory.Exists(CodexSessionsDir))
        {
            foreach (var f in SafeGetFilesRecursive(CodexSessionsDir, "*.jsonl"))
            {
                var s = ProbeCodex(f);
                if (s != null && IsRelevant(norm, s.Cwd))
                    result.Add(s);
            }
        }

        if (toolFilter is null or "opencode" && File.Exists(OpenCodeDb))
            FindOpencodeSessions(result, norm);

        if (toolFilter is null or "crush" && File.Exists(CrushProjectsJson))
            FindCrushSessions(result, norm);

        if (toolFilter is null or "aider")
            FindAiderSession(result, norm, cwd);

        if (toolFilter is null or "gemini")
            FindGeminiSessions(result, norm, cwd);

        result.Sort((a, b) => b.UpdatedAt.CompareTo(a.UpdatedAt));
        return result;
    }

    /// <summary>探测 Claude Code 会话文件，返回会话信息；不匹配/无法解析返回 null。</summary>
    static ExternalSession? ProbeClaude(string file)
    {
        try
        {
            string? cwd = null, title = null;
            int line = 0;
            foreach (var raw in File.ReadLines(file, Encoding.UTF8))
            {
                if (++line > MaxCwdScanLines) break;
                var node = Json.Parse(raw);
                if (node == null) continue;

                var type = node.GetString("type");
                if (type == "user" && cwd == null)
                {
                    cwd = node.GetString("cwd");
                    title = ExtractText(node["message"]?["content"]);
                    if (title != null) title = ContextManager.TruncateWithEllipsis(title, 60);
                }
                if (cwd != null && title != null) break;
            }
            if (cwd == null) return null;
            var updated = File.GetLastWriteTime(file);
            return new ExternalSession("claude", file, updated, title ?? Path.GetFileNameWithoutExtension(file), cwd);
        }
        catch { return null; }
    }

    /// <summary>探测 Codex 会话文件（从 session_meta 读 cwd）。</summary>
    static ExternalSession? ProbeCodex(string file)
    {
        try
        {
            string? cwd = null, title = null;
            int line = 0;
            foreach (var raw in File.ReadLines(file, Encoding.UTF8))
            {
                if (++line > MaxCwdScanLines) break;
                var node = Json.Parse(raw);
                if (node == null) continue;

                var type = node.GetString("type");
                if (type == "session_meta" && cwd == null)
                {
                    cwd = node["payload"]?.GetString("cwd");
                }
                else if (type == "response_item" && title == null)
                {
                    var payload = node["payload"];
                    if (payload?.GetString("type") == "message" && payload.GetString("role") == "user")
                    {
                        var text = ExtractTextCodex(payload["content"]);
                        if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("<environment_context>"))
                            title = ContextManager.TruncateWithEllipsis(text, 60);
                    }
                }
                if (cwd != null && title != null) break;
            }
            if (cwd == null) return null;
            var updated = File.GetLastWriteTime(file);
            return new ExternalSession("codex", file, updated, title ?? Path.GetFileNameWithoutExtension(file), cwd);
        }
        catch { return null; }
    }

    /// <summary>从 OpenCode db 的 session 表查找匹配 cwd 的会话。</summary>
    static void FindOpencodeSessions(List<ExternalSession> result, string norm)
    {
        var t = SqliteReader.Open(OpenCodeDb, "session");
        if (t == null) return;
        for (int r = 0; r < t.Rows.Count; r++)
        {
            var dir = t.GetString(r, "directory");
            if (dir == null || !IsRelevant(norm, dir)) continue;
            var id = t.GetString(r, "id") ?? "";
            var title = t.GetString(r, "title") ?? "Untitled";
            var updated = FromMs(t.GetLong(r, "time_updated"));
            result.Add(new ExternalSession("opencode", OpenCodeDb, updated, title, dir, id));
        }
    }

    /// <summary>从 crush projects.json 定位项目，读各项目 .crush/crush.db 的 sessions。</summary>
    static void FindCrushSessions(List<ExternalSession> result, string norm)
    {
        try
        {
            var node = Json.Parse(File.ReadAllText(CrushProjectsJson, Encoding.UTF8));
            var projects = node?["projects"];
            if (projects is not { Kind: JKind.Array }) return;

            foreach (var p in projects.Items)
            {
                var path = p?["path"]?.AsString();
                var dataDir = p?["data_dir"]?.AsString();
                if (path == null || dataDir == null || !IsRelevant(norm, path)) continue;

                var db = Path.Combine(dataDir, "crush.db");
                if (!File.Exists(db)) continue;

                var t = SqliteReader.Open(db, "sessions");
                if (t == null) continue;
                for (int r = 0; r < t.Rows.Count; r++)
                {
                    var id = t.GetString(r, "id") ?? "";
                    var title = t.GetString(r, "title") ?? "Untitled";
                    var updated = FromMs(t.GetLong(r, "updated_at"));
                    result.Add(new ExternalSession("crush", db, updated, title, path, id));
                }
            }
        }
        catch { }
    }

    /// <summary>从 cwd 向上找 .aider.chat.history.md（项目根），找到即加入。</summary>
    static void FindAiderSession(List<ExternalSession> result, string norm, string cwd)
    {
        try
        {
            var dir = new DirectoryInfo(cwd);
            while (dir != null)
            {
                var f = Path.Combine(dir.FullName, ".aider.chat.history.md");
                if (File.Exists(f))
                {
                    var s = ProbeAider(f);
                    if (s != null && IsRelevant(norm, s.Cwd))
                        result.Add(s);
                    return;
                }
                dir = dir.Parent;
            }
        }
        catch { }
    }

    /// <summary>探测 Aider 历史文件（.aider.chat.history.md，纯 Markdown）。</summary>
    static ExternalSession? ProbeAider(string file)
    {
        try
        {
            var cwd = Path.GetDirectoryName(file);
            if (string.IsNullOrEmpty(cwd)) return null;

            string? title = null;
            foreach (var line in File.ReadLines(file, Encoding.UTF8))
            {
                if (line.StartsWith("USER:", StringComparison.Ordinal))
                {
                    title = ContextManager.TruncateWithEllipsis(line[5..].Trim(), 60);
                    break;
                }
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
            }
            return new ExternalSession("aider", file, File.GetLastWriteTime(file), title ?? "Aider 会话", cwd);
        }
        catch { return null; }
    }

    // ─────────────────────────────────────────────────────────────
    // Gemini CLI（JSONL）
    // ─────────────────────────────────────────────────────────────

    /// <summary>从 ~/.gemini/{tmp,history} 定位 Gemini CLI 会话（项目目录 = slugify(basename)，cwd 存 .project_root / projects.json）。</summary>
    static void FindGeminiSessions(List<ExternalSession> result, string norm, string cwd)
    {
        var seen = new HashSet<string>();

        // 方式一：projects.json 精确映射 {cwd: slug} —— 从当前 cwd 向上找最近的祖先项目
        var cwdToSlug = LoadGeminiProjectsJson();
        if (cwdToSlug.Count > 0)
        {
            var dir = new DirectoryInfo(cwd);
            while (dir != null)
            {
                if (cwdToSlug.TryGetValue(Normalize(dir.FullName), out var slug))
                {
                    CollectGeminiBySlug(result, seen, slug, dir.FullName, norm);
                    break;
                }
                dir = dir.Parent;
            }
        }

        // 方式二：精确 slug 兜底 —— slugify(basename) 直接算（无 projects.json 时仍可命中）
        CollectGeminiBySlug(result, seen, Slugify(Path.GetFileName(cwd)), cwd, norm);

        // 方式三：全量枚举 —— 读每个项目目录的 .project_root 恢复 cwd 做相关性匹配（处理 slug 碰撞后缀等）
        foreach (var baseDir in new[] { GeminiTmpDir, GeminiHistoryDir })
        {
            if (!Directory.Exists(baseDir)) continue;
            foreach (var projDir in SafeGetDirs(baseDir))
            {
                var chatsDir = Path.Combine(projDir, "chats");
                if (!Directory.Exists(chatsDir)) continue;
                var projCwd = ReadGeminiProjectRoot(projDir);
                if (string.IsNullOrEmpty(projCwd) || !IsRelevant(norm, projCwd)) continue;
                foreach (var f in SafeGetFiles(chatsDir, "session-*.jsonl"))
                {
                    if (!seen.Add(f)) continue;
                    var s = ProbeGemini(f, projCwd);
                    if (s != null) result.Add(s);
                }
            }
        }
    }

    /// <summary>按 slug 收集某个项目在 tmp 与 history 两个 baseDir 下的会话。</summary>
    static void CollectGeminiBySlug(List<ExternalSession> result, HashSet<string> seen, string slug, string cwd, string norm)
    {
        if (string.IsNullOrEmpty(slug)) return;
        foreach (var baseDir in new[] { GeminiTmpDir, GeminiHistoryDir })
        {
            var chatsDir = Path.Combine(baseDir, slug, "chats");
            if (!Directory.Exists(chatsDir)) continue;
            foreach (var f in SafeGetFiles(chatsDir, "session-*.jsonl"))
            {
                if (!seen.Add(f)) continue;
                var s = ProbeGemini(f, cwd);
                if (s != null && IsRelevant(norm, s.Cwd))
                    result.Add(s);
            }
        }
    }

    /// <summary>读取 ~/.gemini/projects.json 的 {cwd: slug} 映射。</summary>
    static Dictionary<string, string> LoadGeminiProjectsJson()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(GeminiProjectsJson)) return map;
        try
        {
            var node = Json.Parse(File.ReadAllText(GeminiProjectsJson, Encoding.UTF8));
            var projects = node?["projects"];
            if (projects is { Kind: JKind.Object })
            {
                foreach (var (key, val) in projects.Entries)
                {
                    var slug = val.AsString();
                    if (!string.IsNullOrEmpty(slug) && !string.IsNullOrEmpty(key))
                        map[Normalize(key)] = slug;
                }
            }
        }
        catch { }
        return map;
    }

    /// <summary>读取项目目录下的 .project_root 标记文件（含完整 cwd 路径）。</summary>
    static string? ReadGeminiProjectRoot(string projDir)
    {
        var marker = Path.Combine(projDir, ".project_root");
        if (!File.Exists(marker)) return null;
        try
        {
            var text = File.ReadAllText(marker, Encoding.UTF8).Trim();
            return string.IsNullOrEmpty(text) ? null : Normalize(text);
        }
        catch { return null; }
    }

    /// <summary>探测 Gemini CLI 会话文件（JSONL：首行 metadata + 后续 message 记录）。cwd 由调用方传入。</summary>
    static ExternalSession? ProbeGemini(string file, string? fallbackCwd)
    {
        try
        {
            string? title = null;
            foreach (var raw in File.ReadLines(file, Encoding.UTF8))
            {
                var node = Json.Parse(raw);
                if (node == null) continue;

                if (node.GetString("id") != null)
                {
                    // message 记录：取第一条用户消息当标题
                    if (title == null && node.GetString("type") == "user")
                    {
                        title = ExtractGeminiText(node["content"]);
                        if (!string.IsNullOrWhiteSpace(title))
                            title = ContextManager.TruncateWithEllipsis(title, 60);
                    }
                }
                else
                {
                    // metadata 记录：summary 优先当标题
                    if (title == null)
                    {
                        var summary = node.GetString("summary");
                        if (!string.IsNullOrWhiteSpace(summary))
                            title = ContextManager.TruncateWithEllipsis(summary, 60);
                    }
                }
                if (title != null) break;
            }
            return new ExternalSession("gemini", file, File.GetLastWriteTime(file),
                title ?? Path.GetFileNameWithoutExtension(file), fallbackCwd ?? "");
        }
        catch { return null; }
    }

    /// <summary>复现 Gemini CLI 的 slugify：小写 + 非 [a-z0-9] 转 '-' + 折叠连续 '-' + 去首尾，空则 "project"。</summary>
    static string Slugify(string text)
    {
        var lower = text.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        bool prevDash = false;
        foreach (var r in lower.EnumerateRunes())
        {
            int v = r.Value;
            bool keep = v >= 'a' && v <= 'z' || v >= '0' && v <= '9';
            if (keep)
            {
                sb.Append((char)v);
                prevDash = false;
            }
            else if (!prevDash)
            {
                sb.Append('-');
                prevDash = true;
            }
        }
        var result = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(result) ? "project" : result;
    }

    // ─────────────────────────────────────────────────────────────
    // 交接文档生成
    // ─────────────────────────────────────────────────────────────

    /// <summary>读取外部会话，组装交接文档（聊天 + todo + git 状态）。</summary>
    public static string BuildHandoffDoc(ExternalSession session, string cwd)
    {
        var chat = new List<(string Role, string Text)>();
        var todos = new List<TodoItem>();

        switch (session.Tool)
        {
            case "claude": ParseClaude(session.Path, chat, todos); break;
            case "codex": ParseCodex(session.Path, chat, todos); break;
            case "opencode": ParseOpencode(session.Path, session.SessionId, chat, todos); break;
            case "crush": ParseCrush(session.Path, session.SessionId, chat, todos); break;
            case "aider": ParseAider(session.Path, chat); break;
            case "gemini": ParseGemini(session.Path, chat); break;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# 跨工具会话交接（来自 {session.ToolLabel}）");
        sb.AppendLine();
        sb.AppendLine($"- **会话**：`{session.Title}`");
        sb.AppendLine($"- **更新时间**：{session.UpdatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"- **工作目录**：`{session.Cwd}`");
        sb.AppendLine();

        // 待办清单放在最前 —— 是「接着跑」的第一抓手
        if (todos.Count > 0)
        {
            sb.AppendLine("## 待办清单（todo）");
            sb.AppendLine();
            foreach (var t in todos)
            {
                var mark = t.Status switch
                {
                    "completed" => "✅",
                    "in_progress" => "🔄",
                    _ => "⬜",
                };
                sb.AppendLine($"- {mark} {t.Content}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## 对话记录（最近）");
        sb.AppendLine();
        foreach (var (role, text) in chat)
        {
            sb.AppendLine($"### {role}");
            sb.AppendLine(text);
            sb.AppendLine();
        }

        var git = GitState(cwd);
        if (!string.IsNullOrEmpty(git))
        {
            sb.AppendLine("## 当前 Git 状态");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(git);
            sb.AppendLine("```");
        }

        sb.AppendLine("---");
        sb.AppendLine("> 以上是上一个智能体的工作交接。请先阅读，再继续未完成的任务。");
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────
    // Claude Code / Codex（JSONL）
    // ─────────────────────────────────────────────────────────────

    static void ParseClaude(string file, List<(string, string)> chat, List<TodoItem> todos)
    {
        try
        {
            foreach (var raw in File.ReadLines(file, Encoding.UTF8))
            {
                var node = Json.Parse(raw);
                if (node == null) continue;
                var type = node.GetString("type");
                if (type == null) continue;

                if (type == "user")
                {
                    if (node.GetBool("isSidechain")) continue; // 跳过侧链（子智能体）消息
                    var text = ExtractText(node["message"]?["content"]);
                    if (!string.IsNullOrWhiteSpace(text))
                        AddChat(chat, "用户", text);
                }
                else if (type == "assistant")
                {
                    var content = node["message"]?["content"];
                    if (content == null) continue;
                    if (content.Kind == JKind.Array)
                    {
                        foreach (var block in content.Items)
                        {
                            var bt = block?.GetString("type");
                            if (bt == "text")
                            {
                                var text = block?.GetString("text");
                                if (!string.IsNullOrWhiteSpace(text))
                                    AddChat(chat, "助手", text!);
                            }
                            else if (bt == "tool_use")
                            {
                                var name = block?.GetString("name") ?? "工具";
                                var input = block?["input"];
                                if (name.Contains("odo", StringComparison.OrdinalIgnoreCase))
                                    CollectTodoArray(input?["todos"], todos);
                                else
                                    AddChat(chat, "工具", SummarizeTool(name, input));
                            }
                        }
                    }
                    else
                    {
                        var text = ExtractText(content);
                        if (!string.IsNullOrWhiteSpace(text))
                            AddChat(chat, "助手", text);
                    }
                }
                else if (type == "summary")
                {
                    var summary = node.GetString("summary");
                    if (!string.IsNullOrWhiteSpace(summary))
                        AddChat(chat, "摘要", summary);
                }
            }
        }
        catch { }
    }

    static void ParseCodex(string file, List<(string, string)> chat, List<TodoItem> todos)
    {
        try
        {
            foreach (var raw in File.ReadLines(file, Encoding.UTF8))
            {
                var node = Json.Parse(raw);
                if (node == null) continue;
                if (node.GetString("type") != "response_item") continue;

                var payload = node["payload"];
                if (payload == null) continue;
                var pt = payload.GetString("type");

                if (pt == "message")
                {
                    var role = payload.GetString("role");
                    if (role == "developer" || role == "system") continue; // 跳过系统指令
                    var text = ExtractTextCodex(payload["content"]);
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    if (text.StartsWith("<environment_context>")) continue; // 跳过环境上下文注入
                    AddChat(chat, role == "assistant" ? "助手" : "用户", text);
                }
                else if (pt == "function_call")
                {
                    var name = payload.GetString("name") ?? "工具";
                    var args = payload.GetString("arguments") ?? "";
                    if (name.Contains("odo", StringComparison.OrdinalIgnoreCase))
                        CollectTodoArray(Json.Parse(args)?["todos"], todos);
                    else
                        AddChat(chat, "工具", SummarizeToolArgs(name, args));
                }
            }
        }
        catch { }
    }

    // ─────────────────────────────────────────────────────────────
    // OpenCode / Crush（SQLite）
    // ─────────────────────────────────────────────────────────────

    static void ParseOpencode(string db, string? sessionId, List<(string, string)> chat, List<TodoItem> todos)
    {
        if (sessionId == null) return;
        var msgs = SqliteReader.Open(db, "message");
        var parts = SqliteReader.Open(db, "part");
        if (msgs == null || parts == null) return;

        var msgList = new List<(string Id, string Role, long Time)>();
        for (int r = 0; r < msgs.Rows.Count; r++)
        {
            if (msgs.GetString(r, "session_id") != sessionId) continue;
            var id = msgs.GetString(r, "id");
            if (id == null) continue;
            var data = Json.Parse(msgs.GetString(r, "data") ?? "");
            var role = data?["role"]?.AsString() ?? "assistant";
            msgList.Add((id, role, msgs.GetLong(r, "time_created")));
        }
        msgList.Sort((a, b) => a.Time.CompareTo(b.Time));

        var partsByMsg = new Dictionary<string, List<(long Time, string Kind, string Text)>>();
        for (int r = 0; r < parts.Rows.Count; r++)
        {
            if (parts.GetString(r, "session_id") != sessionId) continue;
            var mid = parts.GetString(r, "message_id");
            if (mid == null) continue;
            var data = Json.Parse(parts.GetString(r, "data") ?? "");
            var type = data?["type"]?.AsString() ?? "";
            var time = parts.GetLong(r, "time_created");
            if (!partsByMsg.TryGetValue(mid, out var list)) partsByMsg[mid] = list = new();

            if (type == "text")
                list.Add((time, "text", data?["text"]?.AsString() ?? ""));
            else if (type == "tool")
                list.Add((time, "tool", $"[{data?["tool"]?.AsString() ?? "tool"}] {ContextManager.TruncateWithEllipsis(data?["state"]?["input"]?.ToJson() ?? "", 200)}"));
        }

        foreach (var (id, role, _) in msgList)
        {
            if (!partsByMsg.TryGetValue(id, out var pl)) continue;
            pl.Sort((a, b) => a.Time.CompareTo(b.Time));
            foreach (var (_, kind, text) in pl)
            {
                if (kind == "text" && !string.IsNullOrWhiteSpace(text))
                    AddChat(chat, role == "user" ? "用户" : "助手", text);
                else if (kind == "tool")
                    AddChat(chat, "工具", text);
            }
        }
    }

    static void ParseCrush(string db, string? sessionId, List<(string, string)> chat, List<TodoItem> todos)
    {
        if (sessionId == null) return;
        var sessions = SqliteReader.Open(db, "sessions");
        var msgs = SqliteReader.Open(db, "messages");
        if (msgs == null) return;

        // todo 从 sessions.todos（JSON 数组）
        if (sessions != null)
        {
            for (int r = 0; r < sessions.Rows.Count; r++)
            {
                if (sessions.GetString(r, "id") != sessionId) continue;
                var todosJson = sessions.GetString(r, "todos");
                if (!string.IsNullOrEmpty(todosJson))
                    CollectTodoArray(Json.Parse(todosJson), todos);
                break;
            }
        }

        // 消息按 created_at 排序
        var rows = new List<(long Time, string Role, string Parts)>();
        for (int r = 0; r < msgs.Rows.Count; r++)
        {
            if (msgs.GetString(r, "session_id") != sessionId) continue;
            if (msgs.GetLong(r, "is_summary_message") != 0) continue;
            rows.Add((msgs.GetLong(r, "created_at"), msgs.GetString(r, "role") ?? "", msgs.GetString(r, "parts") ?? ""));
        }
        rows.Sort((a, b) => a.Time.CompareTo(b.Time));

        foreach (var (_, role, partsJson) in rows)
        {
            var arr = Json.Parse(partsJson);
            if (arr is not { Kind: JKind.Array }) continue;
            foreach (var part in arr.Items)
            {
                var type = part?["type"]?.AsString();
                var data = part?["data"];
                if (type == "text")
                {
                    var text = data?["text"]?.AsString();
                    if (!string.IsNullOrWhiteSpace(text))
                        AddChat(chat, role == "user" ? "用户" : "助手", text);
                }
                else if (type == "tool_result")
                {
                    var name = data?["name"]?.AsString() ?? "tool";
                    var content = data?["content"]?.AsString() ?? "";
                    AddChat(chat, "工具", $"[{name}] {ContextManager.TruncateWithEllipsis(content, 200)}");
                }
            }
        }
    }

    /// <summary>解析 Aider 历史文件（USER:/ASSISTANT:/TOOL: 段落，纯 Markdown）。</summary>
    static void ParseAider(string file, List<(string, string)> chat)
    {
        try
        {
            string? role = null;
            var sb = new StringBuilder();

            void Flush()
            {
                if (role == null) return;
                var text = sb.ToString().Trim();
                if (text.Length > 0)
                    AddChat(chat, role, text);
                sb.Clear();
            }

            foreach (var line in File.ReadLines(file, Encoding.UTF8))
            {
                var t = line.TrimEnd();
                if (t.StartsWith("USER:", StringComparison.Ordinal))
                {
                    Flush();
                    role = "用户";
                    sb.AppendLine(t[5..].Trim());
                }
                else if (t.StartsWith("ASSISTANT:", StringComparison.Ordinal))
                {
                    Flush();
                    role = "助手";
                    sb.AppendLine(t[10..].Trim());
                }
                else if (t.StartsWith("TOOL:", StringComparison.Ordinal))
                {
                    Flush();
                    role = "工具";
                    sb.AppendLine(t[5..].Trim());
                }
                else if (t.StartsWith("# Aider", StringComparison.Ordinal))
                {
                    // 跳过标题行 "# Aider chat conversation"
                }
                else if (role != null && sb.Length > 0)
                {
                    sb.AppendLine(t); // 多行消息的续行
                }
            }
            Flush();
        }
        catch { }
    }

    /// <summary>解析 Gemini CLI 会话文件（JSONL：metadata + message 记录）。</summary>
    static void ParseGemini(string file, List<(string, string)> chat)
    {
        try
        {
            foreach (var raw in File.ReadLines(file, Encoding.UTF8))
            {
                var node = Json.Parse(raw);
                if (node == null) continue;

                var type = node.GetString("type");
                if (node.GetString("id") == null || type == null) continue; // 跳过 metadata / 非消息记录

                if (type == "user")
                {
                    var text = ExtractGeminiText(node["content"]);
                    if (!string.IsNullOrWhiteSpace(text))
                        AddChat(chat, "用户", text);
                }
                else if (type is "gemini" or "gemini_content")
                {
                    var text = ExtractGeminiText(node["content"]);
                    if (!string.IsNullOrWhiteSpace(text))
                        AddChat(chat, "助手", text);
                }
            }
        }
        catch { }
    }

    /// <summary>提取 Gemini 消息文本（content 为 parts 数组：text / functionCall / functionResponse 等）。</summary>
    static string ExtractGeminiText(JNode? content)
    {
        if (content == null) return "";
        if (content.Kind == JKind.String) return content.AsString() ?? "";
        if (content.Kind != JKind.Array) return "";

        var sb = new StringBuilder();
        foreach (var part in content.Items)
        {
            if (part == null) continue;
            var text = part.GetString("text");
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
                continue;
            }
            var call = part["functionCall"];
            if (call != null)
            {
                sb.AppendLine($"[工具调用: {call.GetString("name") ?? "工具"}]");
                continue;
            }
            var resp = part["functionResponse"];
            if (resp != null)
            {
                sb.AppendLine($"[工具结果: {resp.GetString("name") ?? "工具"}]");
                continue;
            }
            // thought / codeExecutionResult / inlineData 等非文本 part 忽略
        }
        return sb.ToString().Trim();
    }

    // ─────────────────────────────────────────────────────────────
    // 提取辅助
    // ─────────────────────────────────────────────────────────────

    static string? ExtractText(JNode? content)
    {
        if (content == null) return null;
        if (content.Kind == JKind.String) return content.AsString();
        if (content.Kind == JKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var block in content.Items)
            {
                if (block?.GetString("type") == "text")
                {
                    var t = block.GetString("text");
                    if (!string.IsNullOrWhiteSpace(t)) sb.AppendLine(t);
                }
            }
            return sb.ToString().Trim();
        }
        return null;
    }

    static string ExtractTextCodex(JNode? content)
    {
        if (content == null) return "";
        var sb = new StringBuilder();
        if (content.Kind == JKind.Array)
        {
            foreach (var block in content.Items)
            {
                if (block?.GetString("type") == "input_text")
                {
                    var t = block.GetString("text");
                    if (!string.IsNullOrWhiteSpace(t)) sb.AppendLine(t);
                }
            }
        }
        else if (content.Kind == JKind.String)
        {
            sb.Append(content.AsString());
        }
        return sb.ToString().Trim();
    }

    /// <summary>从 todos JSON 数组提取待办（取最后一次调用覆盖，todo 是增量更新）。</summary>
    static void CollectTodoArray(JNode? arr, List<TodoItem> todos)
    {
        if (arr is not { Kind: JKind.Array }) return;
        todos.Clear();
        foreach (var item in arr.Items)
        {
            var content = item?["content"]?.AsString();
            if (string.IsNullOrWhiteSpace(content)) continue;
            todos.Add(new TodoItem(content, item?["status"]?.AsString() ?? "pending"));
        }
    }

    static string SummarizeTool(string name, JNode? input)
    {
        var arg = input?["command"]?.AsString()
               ?? input?["path"]?.AsString()
               ?? input?["pattern"]?.AsString()
               ?? input?["description"]?.AsString()
               ?? input?.ToJson();
        if (arg != null) arg = ContextManager.TruncateWithEllipsis(arg, 200);
        return $"[{name}] {arg}";
    }

    static string SummarizeToolArgs(string name, string args)
    {
        var node = Json.Parse(args);
        var arg = node?["command"]?.AsString()
               ?? node?["path"]?.AsString()
               ?? node?["pattern"]?.AsString()
               ?? node?["description"]?.AsString();
        if (arg == null && args.Length > 0) arg = ContextManager.TruncateWithEllipsis(args, 200);
        return $"[{name}] {arg}";
    }

    static void AddChat(List<(string, string)> chat, string role, string text)
    {
        chat.Add((role, ContextManager.TruncateWithEllipsis(text, MaxLineRunes)));
        if (chat.Count > MaxChatLines)
            chat.RemoveAt(0);
    }

    // ─────────────────────────────────────────────────────────────
    // git 状态（只读命令）
    // ─────────────────────────────────────────────────────────────

    static string GitState(string cwd)
    {
        var sb = new StringBuilder();
        var status = RunGit(cwd, "status --short --branch");
        if (!string.IsNullOrEmpty(status))
        {
            sb.AppendLine("$ git status --short --branch");
            sb.AppendLine(TruncateLines(status, 60));
            sb.AppendLine();
        }
        var diffstat = RunGit(cwd, "diff --stat");
        if (!string.IsNullOrEmpty(diffstat))
        {
            sb.AppendLine("$ git diff --stat");
            sb.AppendLine(TruncateLines(diffstat, 60));
        }
        return sb.ToString().Trim();
    }

    static string RunGit(string cwd, string args)
    {
        try
        {
            var psi = new ProcessStartInfo("git", args)
            {
                WorkingDirectory = cwd,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = Process.Start(psi);
            if (p == null) return "";
            var outp = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return outp.Trim();
        }
        catch { return ""; }
    }

    // ─────────────────────────────────────────────────────────────
    // 通用辅助
    // ─────────────────────────────────────────────────────────────

    static bool IsRelevant(string normCwd, string sessionCwd)
    {
        if (string.IsNullOrEmpty(sessionCwd)) return false;
        var s = Normalize(sessionCwd);
        if (normCwd == s) return true;
        var sep = Path.DirectorySeparatorChar;
        return normCwd.StartsWith(s + sep, StringComparison.Ordinal);
    }

    static string Normalize(string path)
    {
        try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return path; }
    }

    static DateTime FromMs(long ms)
    {
        try { return ms > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime : DateTime.MinValue; }
        catch { return DateTime.MinValue; }
    }

    /// <summary>按码点安全截断（禁止 char 切片切半代理对，见 CLAUDE.md 约束）。</summary>

    static string TruncateLines(string s, int maxLines)
    {
        var lines = s.Split('\n');
        if (lines.Length <= maxLines) return s;
        return string.Join('\n', lines.Take(maxLines)) + $"\n…（共 {lines.Length} 行，截断）";
    }

    static IEnumerable<string> SafeGetDirs(string dir)
    {
        try { return Directory.GetDirectories(dir); }
        catch { return []; }
    }

    static IEnumerable<string> SafeGetFiles(string dir, string pattern)
    {
        try { return Directory.GetFiles(dir, pattern); }
        catch { return []; }
    }

    static IEnumerable<string> SafeGetFilesRecursive(string dir, string pattern)
    {
        try { return Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories); }
        catch { return []; }
    }

    record TodoItem(string Content, string Status);
}

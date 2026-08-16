using System.Collections.Concurrent;
using System.Text;
using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;

namespace WayCoder.UI.Web;

/// <summary>
/// 浏览器聊天桥接层：把 <see cref="Agent.ChatAsync"/> 的流式回调（onToken/onTool/onToolOutput）
/// 转为 SSE 事件广播给浏览器，接收浏览器 POST 的输入入队，支持中断。
/// 对标 DeepSeek Harness Web UI：多槽位（F1-F10）、换模型、输 key、设置、黑白主题。
/// </summary>
public sealed partial class WebChatServer : UxHelper.IWebInteraction
{

    // ═══════════════════════════════════════════════════════════
    //  序列化（纯函数，便于自测）
    // ═══════════════════════════════════════════════════════════

    /// <summary>序列化模型目录（前端按 provider 分组下拉）。</summary>
    public static string SerializeModels()
    {
        var arr = JNode.Array();
        foreach (var m in ModelCatalog.All)
        {
            arr.Add(JNode.Object()
                .Set("id", m.Id)
                .Set("name", m.DisplayName)
                .Set("provider", m.Provider)
                .Set("providerId", m.ProviderId)
                .Set("category", m.Category)
                .Set("context", m.ContextWindow)
                .Set("inputPrice", m.InputPrice)
                .Set("outputPrice", m.OutputPrice)
                .Set("hasKey", ApiKeyStore.HasKeyFor(m.ProviderId, m.Id)));
        }
        return arr.ToJson();
    }

    /// <summary>序列化连通性探测结果列表（供 /models/scan 返回）。</summary>
    public static string SerializeScan(List<ModelCli.EndpointProbe> probes)
    {
        var arr = JNode.Array();
        foreach (var p in probes)
        {
            var models = JNode.Array();
            foreach (var m in p.Models) models.Add(m);
            arr.Add(JNode.Object()
                .Set("providerId", p.ProviderId)
                .Set("display", p.Display)
                .Set("baseUrl", p.BaseUrl ?? "")
                .Set("ok", p.Ok)
                .Set("detail", p.Detail)
                .Set("models", models));
        }
        return arr.ToJson();
    }

    /// <summary>供应商是否有可用 key（local/custom 无需 key）。</summary>
    public static bool ProviderHasKey(string providerId)
    {
        if (providerId is "local" or "custom") return true;
        if (!string.IsNullOrEmpty(ApiKeyStore.Get(providerId))) return true;
        if (!string.IsNullOrEmpty(Config.Instance.ApiKey)) return true;
        return false;
    }

    /// <summary>序列化当前会话状态（活跃槽位、各槽位模型/是否有历史、当前模型/供应商、是否有 key）。</summary>
    public static string SerializeState(int activeSlot, Agent?[] slots)
    {
        var cfg = Config.Instance;
        var info = ModelCatalog.Find(cfg.Model);
        var providerId = info?.ProviderId ?? cfg.Provider;
        var hasKey = !string.IsNullOrEmpty(ApiKeyStore.Get(providerId))
                     || !string.IsNullOrEmpty(cfg.ApiKey)
                     || providerId is "local" or "custom";

        var slotArr = JNode.Array();
        for (int i = 0; i < slots.Length; i++)
        {
            var a = slots[i];
            slotArr.Add(JNode.Object()
                .Set("slot", i)
                .Set("model", a?.LlmClient.EffectiveModel ?? "")
                .Set("hasHistory", a != null && HasHistory(a)));
        }

        return JNode.Object()
            .Set("activeSlot", activeSlot)
            .Set("model", cfg.Model)
            .Set("smallModel", cfg.SmallModel)
            .Set("economy", cfg.EconomyMode.ToString().ToLowerInvariant())
            .Set("provider", providerId)
            .Set("providerName", ModelCatalog.Providers.TryGetValue(providerId, out var p) ? p.DisplayName : providerId)
            .Set("hasKey", hasKey)
            .Set("permMode", PermissionManager.CurrentMode.ToString().ToLowerInvariant())
            .Set("slots", slotArr)
            .ToJson();
    }

    /// <summary>序列化设置面板数据（SettingSchema 按 Category 分组，secret 显示 masked）。</summary>
    public static string SerializeSettings()
    {
        var schema = Config.SettingSchema();
        var groups = new List<(string Category, JNode Items)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var s in schema.OrderBy(s => s.Category).ThenBy(s => s.Order))
        {
            if (!seen.Add(s.Key)) continue; // 去重（防御重复 Key）
            var value = Config.GetPropValue(s.Key) ?? "";
            // secret 类型不泄露明文
            if (s.Type == "secret")
                value = string.IsNullOrEmpty(value) ? "" : "••••••••";

            JNode? items = null;
            foreach (var g in groups)
            {
                if (g.Category == s.Category) { items = g.Items; break; }
            }
            if (items == null)
            {
                items = JNode.Array();
                groups.Add((s.Category, items));
            }
            items.Add(JNode.Object()
                .Set("key", s.Key)
                .Set("label", s.Label)
                .Set("desc", s.Desc)
                .Set("type", s.Type)
                .Set("options", OptionsToJson(s.Options))
                .Set("value", value));
        }

        var groupArr = JNode.Array();
        foreach (var (cat, items) in groups)
        {
            groupArr.Add(JNode.Object().Set("category", cat).Set("items", items));
        }
        return groupArr.ToJson();
    }

    private static JNode OptionsToJson(string[]? options)
    {
        var arr = JNode.Array();
        if (options != null)
            foreach (var o in options) arr.Add(o);
        return arr;
    }

    /// <summary>序列化指定 Agent 的历史消息（role=user/assistant 且内容非空）。</summary>
    public static string SerializeHistory(Agent agent)
    {
        var arr = JNode.Array();
        // 快照读：主循环线程流式期间并发追加 Messages 时，避免 foreach 抛 InvalidOperationException
        List<JNode> snapshot;
        try { snapshot = agent.Messages.ToList(); }
        catch { return arr.ToJson(); }
        foreach (var m in snapshot)
        {
            var role = m["role"]?.AsString();
            if (role != "user" && role != "assistant") continue;
            var content = m["content"]?.AsString();
            if (string.IsNullOrEmpty(content)) continue;
            arr.Add(JNode.Object().Set("role", role).Set("content", content));
        }
        return arr.ToJson();
    }

    private static bool HasHistory(Agent agent)
    {
        List<JNode> snapshot;
        try { snapshot = agent.Messages.ToList(); }
        catch { return false; }
        foreach (var m in snapshot)
        {
            var role = m["role"]?.AsString();
            if (role != "user" && role != "assistant") continue;
            if (!string.IsNullOrEmpty(m["content"]?.AsString())) return true;
        }
        return false;
    }

    /// <summary>序列化右栏信息面板（任务/token/费用/修改文件/MCP/LSP）。纯静态便于自测。</summary>
    public static string SerializePanel(int activeSlot, Agent?[] slots)
    {
        // ── 任务（全局共享）──
        var todos = JNode.Array();
        try
        {
            foreach (var t in TodoTool.Items)
            {
                var deps = JNode.Array();
                foreach (var d in t.DependsOn) deps.Add(d);
                todos.Add(JNode.Object()
                    .Set("id", t.Id)
                    .Set("title", t.Title)
                    .Set("status", t.Status)
                    .Set("deps", deps));
            }
        }
        catch { /* todo 读取失败不阻塞面板 */ }

        // ── token / 费用（当前活跃槽位实例级）──
        var llm = (activeSlot >= 0 && activeSlot < slots.Length) ? slots[activeSlot]?.LlmClient : null;
        var tokens = JNode.Object();
        if (llm != null)
        {
            tokens.Set("totalPrompt", llm.TotalPromptTokens)
                  .Set("totalCompletion", llm.TotalCompletionTokens)
                  .Set("taskPrompt", llm.TaskPromptTokens)
                  .Set("taskCompletion", llm.TaskCompletionTokens)
                  .Set("totalRequests", llm.TotalRequests)
                  .Set("tokensPerSec", llm.LastTokensPerSec);
        }

        var cost = JNode.Object();
        if (llm != null)
        {
            cost.Set("task", llm.TaskCost.HasValue ? JNode.Num(llm.TaskCost.Value) : JNode.Null());
            cost.Set("estimated", llm.EstimatedCost.HasValue ? JNode.Num(llm.EstimatedCost.Value) : JNode.Null());
        }

        // ── 修改文件（全局共享，含 +新增/-删除 行数）──
        var files = JNode.Array();
        try
        {
            foreach (var f in EditFileTool.ChangedFiles.ToList())
            {
                EditFileTool.ChangedFileStats.TryGetValue(f, out var st);
                files.Add(JNode.Object()
                    .Set("path", f)
                    .Set("added", st.Added)
                    .Set("deleted", st.Deleted));
            }
        }
        catch { }

        // ── MCP 服务器（全局共享）──
        var mcp = JNode.Array();
        try
        {
            foreach (var s in McpManager.Servers)
            {
                mcp.Add(JNode.Object()
                    .Set("name", s.Name)
                    .Set("transport", s.Transport)
                    .Set("status", s.Status.ToString().ToLowerInvariant())
                    .Set("toolCount", s.ToolCount)
                    .Set("resourceCount", s.ResourceCount)
                    .Set("promptCount", s.PromptCount)
                    .Set("error", s.Error));
            }
        }
        catch { }

        // ── LSP 会话（全局共享）──
        var lsp = JNode.Array();
        try
        {
            foreach (var s in LspTool.ActiveSessions)
            {
                lsp.Add(JNode.Object()
                    .Set("command", s.Command)
                    .Set("root", s.Root)
                    .Set("initialized", s.Initialized)
                    .Set("hasExited", s.HasExited));
            }
        }
        catch { }

        return JNode.Object()
            .Set("todos", todos)
            .Set("tokens", tokens)
            .Set("cost", cost)
            .Set("files", files)
            .Set("mcp", mcp)
            .Set("lsp", lsp)
            .ToJson();
    }

    /// <summary>序列化历史会话列表（左栏）。纯静态便于自测。</summary>
    public static string SerializeSessions()
    {
        var arr = JNode.Array();
        try
        {
            foreach (var s in SessionManager.ListSessions(50))
            {
                arr.Add(JNode.Object()
                    .Set("id", s.Id)
                    .Set("model", s.Model)
                    .Set("savedAt", s.SavedAt)
                    .Set("preview", s.Preview)
                    .Set("msgCount", s.MessageCount));
            }
        }
        catch { }
        return arr.ToJson();
    }
}

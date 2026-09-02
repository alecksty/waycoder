using System.Text;
using WayCoder.Infra;

namespace WayCoder;

public static partial class ModelCli
{
    private static (bool Ok, string Detail) ProbeChat(string providerId, string modelId, string baseUrl, int timeoutSec = 60)
    {
        try
        {
            var key = ApiKeyStore.Get(providerId) ?? "";
            // timeoutSeconds 固定单次超时（不渐进加长重试）：连通性探测要快速可控，别被全局重试链拖住
            var llm = new LLM(modelId, key, baseUrl, maxTokens: 16, timeoutSeconds: timeoutSec);
            var resp = llm.ChatAsync(
                new List<JNode> { JNode.Object().Set("role", "user").Set("content", "只回复两个字：ok") },
                cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec)).Token
            ).GetAwaiter().GetResult();
            var content = (resp.Content ?? "").Trim();
            if (resp.IsFatalError) return (false, "致命错误");
            if (content.Length > 0) return (true, "回复: " + content[..Math.Min(content.Length, 20)]);
            if (resp.ToolCalls.Count > 0) return (true, "工具调用");
            // think 模型：内容在 reasoning_content（不并入 Content），有思考即算连通，别误判为不可用
            if (resp.ReasoningTokens > 0) return (true, $"思考 {resp.ReasoningTokens} tok");
            return (false, "空回复（模型可能只思考不输出）");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 测试所有 connect 的模型连通性，生成报告：
    /// 遍历每个 connect（有 key 的 + 本地无需 key 的），发简单请求，列出可用 / 失败原因。
    /// </summary>
    public static string Report(string? timeoutArg = null)
    {
        var connects = ConnectionConfig.ListConnects();
        if (connects.Count == 0) return "暂无 connect（--connect add <name> <providerId> <modelId> 添加）";
        var timeout = ParseTimeout(timeoutArg);
        var sb = new StringBuilder($"模型连通性报告（{connects.Count} 个 connect，单模型超时 {timeout}s）：\n");
        var seen = new HashSet<(string, string)>();
        int ok = 0, fail = 0, skip = 0, total = 0;
        foreach (var c in connects)
        {
            if (!seen.Add((c.ProviderId, c.ModelId))) continue;
            total++;
            var prov = ConnectionConfig.ResolveProvider(c.ProviderId);
            var baseUrl = prov?.BaseUrl ?? ModelCatalog.Find(c.ModelId, null)?.DefaultBaseUrl ?? "";
            // 本地服务（localhost/127.0.0.1）无需 key；Ollama 云端（ollama.com）也要 key——按地址判断，不看 providerId
            var isLocal = ModelCatalog.IsLocalUrl(baseUrl);
            var hasKey = ApiKeyStore.Has(c.ProviderId);
            if (!isLocal && !hasKey)
            {
                skip++;
                sb.AppendLine($"  ⏭ {c.Name}（{ModelCatalog.ShortDisplayName(c.ModelId)}）无 key");
                continue;
            }
            // 实时进度（stderr，不污染报告）：让用户知道正在扫哪个
            Console.Error.WriteLine($"正在测试 [{c.ProviderId}] 第 {total}/{connects.Count} 个（{ModelCatalog.ShortDisplayName(c.ModelId)}）...");
            var (ok2, detail) = ProbeChat(c.ProviderId, c.ModelId, baseUrl, timeout);
            if (ok2) { ok++; sb.AppendLine($"  ✅ {c.Name}（{ModelCatalog.ShortDisplayName(c.ModelId)}）{detail}"); }
            else { fail++; sb.AppendLine($"  ❌ {c.Name}（{ModelCatalog.ShortDisplayName(c.ModelId)}）{detail}"); }
        }
        sb.AppendLine($"\n汇总：✅ {ok} 可用　❌ {fail} 失败　⏭ {skip} 跳过(无key)");
        return sb.ToString();
    }

    /// <summary>
    /// 切换免费模型前记住的模型（/free-restore / --model restore 恢复）。
    /// 持久化到 config.json（freePrevProvider/Model/BaseUrl）：跨会话可恢复（CLI 一次性进程也能还原）。
    /// </summary>

    private static (ProbeTarget Target, bool Ok, string Detail)[] RunProbes(List<ProbeTarget> targets)
    {
        var results = new (ProbeTarget, bool, string)[targets.Count];
        var indexed = targets.Select((t, i) => (t, i)).ToArray();
        System.Threading.Tasks.Parallel.ForEach(indexed,
            new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 4 },
            item =>
            {
                var (t, i) = item;
                var (o, d) = ProbeEndpoint(t.BaseUrl, t.Key);
                results[i] = (t, o, d); // 各索引只写一次，无竞态；顺序由数组下标保证
            });
        return results;
    }

    /// <summary>
    /// 模型连通性测试：逐一测试所有「已存 API key」的端点 + 所有「本地模型」端点能否连上。
    /// 返回 Markdown 文本报告。
    /// </summary>
    public static string Test()
    {
        var sb = new StringBuilder();
        sb.AppendLine("**模型连通性测试**");
        int ok = 0, total = 0;

        var targets = new List<ProbeTarget>();

        // ── 1. 已存 API key：按供应商逐一测试（含目录内无模型的供应商） ──
        var keys = ApiKeyStore.ListAll().OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var (pid, key) in keys)
        {
            var baseUrl = ResolveProviderBaseUrl(pid);
            var models = ModelCatalog.ByProvider(pid).Select(m => m.Id).ToArray();
            var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
                ? p.DisplayName : pid;
            targets.Add(new ProbeTarget(pid, display, baseUrl, key, models, IsLocal: false));
        }

        // ── 2. 本地端点（无需 key）：按 base_url 分组探测 ──
        var localGroups = ModelCatalog.All
            .Where(m =>
            {
                var baseUrl = EffectiveBaseUrl(m);
                return m.ProviderId is "ollama" or "lmstudio" or "local" || ModelCatalog.IsOllamaBaseUrl(baseUrl);
            })
            .GroupBy(m => EffectiveBaseUrl(m) ?? "")
            .ToList();

        foreach (var g in localGroups)
            targets.Add(new ProbeTarget(g.First().ProviderId, g.First().Provider, g.Key, null,
                g.Select(m => m.Id).Distinct().ToArray(), IsLocal: true));

        if (targets.Count == 0)
            return "没有可测试的端点：既无已存 key，也无本地模型。\n" +
                   "  存 key: --model key <供应商> <key>　本地模型: --model connect <localhost:port>";

        if (targets.Any(t => !t.IsLocal))
        {
            sb.AppendLine();
            sb.AppendLine($"### API Key（{targets.Count(t => !t.IsLocal)} 个供应商）");
        }

        // 并发探测（每项独立 HttpClient+4s 超时），保持原顺序输出
        bool localHeaderShown = false;
        foreach (var (t, o, d) in RunProbes(targets))
        {
            if (t.IsLocal && !localHeaderShown)
            {
                sb.AppendLine();
                sb.AppendLine("### 本地端点（无需 key）");
                localHeaderShown = true;
            }
            total++;
            if (o) ok++;
            sb.AppendLine($"【{t.Display}】{(string.IsNullOrEmpty(t.BaseUrl) ? "" : " " + t.BaseUrl)}");
            sb.AppendLine($"  {(o ? "✅" : "❌")} {d}" + (t.Models.Length > 0 ? $"  —  {string.Join(", ", t.Models)}" : ""));
        }

        sb.AppendLine();
        sb.AppendLine($"**结论：{ok} / {total} 个端点可连接**");
        return sb.ToString().Trim();
    }

    /// <summary>连通性探测结果（结构化，供 Web 序列化为 JSON）。</summary>
    public record EndpointProbe(string ProviderId, string Display, string? BaseUrl, bool Ok, string Detail, string[] Models);

    /// <summary>
    /// 结构化连通性测试：返回所有「已存 API key 的供应商」+「本地端点」的探测结果列表。
    /// 与 <see cref="Test"/> 相同的数据源，但不生成 Markdown，供 Web /models/scan 直接序列化。
    /// </summary>
    public static List<EndpointProbe> TestList()
    {
        var targets = new List<ProbeTarget>();

        // ── 1. 已存 API key：按供应商逐一测试 ──
        var keys = ApiKeyStore.ListAll().OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var (pid, key) in keys)
        {
            var baseUrl = ResolveProviderBaseUrl(pid);
            var models = ModelCatalog.ByProvider(pid).Select(m => m.Id).ToArray();
            var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
                ? p.DisplayName : pid;
            targets.Add(new ProbeTarget(pid, display, baseUrl, key, models, IsLocal: false));
        }

        // ── 2. 本地端点（无需 key）：按 base_url 分组探测 ──
        var localGroups = ModelCatalog.All
            .Where(m =>
            {
                var baseUrl = EffectiveBaseUrl(m);
                return m.ProviderId is "ollama" or "lmstudio" or "local" || ModelCatalog.IsOllamaBaseUrl(baseUrl);
            })
            .GroupBy(m => EffectiveBaseUrl(m) ?? "")
            .ToList();

        foreach (var g in localGroups)
            targets.Add(new ProbeTarget(g.First().ProviderId, g.First().Provider, g.Key, null,
                g.Select(m => m.Id).Distinct().ToArray(), IsLocal: true));

        // 并发探测，保持原顺序
        return RunProbes(targets)
            .Select(r => new EndpointProbe(r.Target.ProviderId, r.Target.Display, r.Target.BaseUrl,
                r.Ok, r.Detail, r.Target.Models))
            .ToList();
    }

    /// <summary>
    /// 剪除失效供应商：逐一测试所有已存 API key，对失效供应商自动清理。
    /// 仅 key 无效（401/403）→ 只删 key、保留模型；无端点（供应商不存在/未配置 base_url）或无法连接（写错地址）→ 删 key + 所有自定义模型。
    /// 内置供应商/模型不删（仅删 key）；本地端点不参与。返回 Markdown 文本报告。
    /// </summary>
    public static string Prune()
    {
        var keys = ApiKeyStore.ListAll().OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        if (keys.Length == 0)
            return "没有已存 API key 可清理。存 key: --model key <供应商> <key>";

        var sb = new StringBuilder();
        sb.AppendLine("**清理失效供应商**");
        sb.AppendLine();
        int removedKeys = 0, removedModels = 0, kept = 0;

        foreach (var (pid, key) in keys)
        {
            var baseUrl = ResolveProviderBaseUrl(pid);
            var display = ModelCatalog.Providers.TryGetValue(pid, out var p) && !string.IsNullOrEmpty(p.DisplayName)
                ? p.DisplayName : pid;

            // 无端点：供应商不存在或未配置 base_url（写错地址/拼错供应商）→ 删模型，key 保留
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var n = ModelCatalog.RemoveCustomByProvider(pid);
                removedModels += n;
                sb.AppendLine($"🗑️  【{display}】无端点（供应商不存在或未配置 base_url）— 已删模型" + (n > 0 ? $" {n} 个" : "") + "（key 保留）");
                continue;
            }

            var (ok, detail) = ProbeEndpoint(baseUrl, key);
            if (ok)
            {
                kept++;
                sb.AppendLine($"✅ 【{display}】{detail} — 保留");
                continue;
            }

            // 无效 key：询问用户是否删除（交互确认）；非交互（管道/一次性）默认保留
            if (detail.StartsWith("密钥无效", StringComparison.Ordinal))
            {
                if (ConfirmDelete($"【{display}】检测到无效 API key（{pid}），是否删除？"))
                {
                    ApiKeyStore.Remove(pid);
                    removedKeys++;
                    sb.AppendLine($"🗑️  【{display}】{detail} — 已删除无效 key");
                }
                else
                {
                    sb.AppendLine($"⚠️  【{display}】{detail} — key 保留（--model key rm {pid} 显式删除）");
                }
                continue;
            }

            var m = ModelCatalog.RemoveCustomByProvider(pid);
            removedModels += m;
            sb.AppendLine($"🗑️  【{display}】{detail} — 已删模型" + (m > 0 ? $" {m} 个" : "") + "（key 保留）");
        }

        sb.AppendLine();
        sb.AppendLine($"**结论：删除 {removedKeys} 个失效供应商的 key，移除 {removedModels} 个自定义模型；保留 {kept} 个**");
        return sb.ToString().Trim();
    }

    /// <summary>解析模型的有效 base_url：provider 唯一地址优先 > 模型默认地址 > 本地(Ollama)默认 localhost:11434</summary>
}

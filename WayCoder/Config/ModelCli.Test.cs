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
                new List<JNode> { JNode.Object().Set("role", "user").Set("content", L.Pick("只回复两个字：ok", "Reply with just: ok")) },
                cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec)).Token
            ).GetAwaiter().GetResult();
            var content = (resp.Content ?? "").Trim();
            if (resp.IsFatalError) return (false, L.Pick("致命错误", "Fatal error"));
            if (content.Length > 0) return (true, L.Pick("回复: ", "Reply: ") + content[..Math.Min(content.Length, 20)]);
            if (resp.ToolCalls.Count > 0) return (true, L.Pick("工具调用", "Tool call"));
            // think 模型：内容在 reasoning_content（不并入 Content），有思考即算连通，别误判为不可用
            if (resp.ReasoningTokens > 0) return (true, L.Pick($"思考 {resp.ReasoningTokens} tok", $"Reasoning {resp.ReasoningTokens} tok"));
            return (false, L.Pick("空回复（模型可能只思考不输出）", "Empty reply (the model may reason without emitting anything)"));
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
        if (connects.Count == 0)
            return L.Pick("暂无 connect（--connect add <name> <providerId> <modelId> 添加）",
                          "No connects yet (add one with --connect add <name> <providerId> <modelId>)");
        var timeout = ParseTimeout(timeoutArg);
        var sb = new StringBuilder(L.Pick(
            $"模型连通性报告（{connects.Count} 个 connect，单模型超时 {timeout}s）：\n",
            $"Model connectivity report ({connects.Count} connects, {timeout}s timeout per model):\n"));
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
                sb.AppendLine(L.Pick($"  ⏭ {c.Name}（{ModelCatalog.ShortDisplayName(c.ModelId)}）无 key",
                                     $"  ⏭ {c.Name} ({ModelCatalog.ShortDisplayName(c.ModelId)}) no key"));
                continue;
            }
            // 实时进度（stderr，不污染报告）：让用户知道正在扫哪个
            Console.Error.WriteLine(L.Pick(
                $"正在测试 [{c.ProviderId}] 第 {total}/{connects.Count} 个（{ModelCatalog.ShortDisplayName(c.ModelId)}）...",
                $"Testing [{c.ProviderId}] {total}/{connects.Count} ({ModelCatalog.ShortDisplayName(c.ModelId)})..."));
            var (ok2, detail) = ProbeChat(c.ProviderId, c.ModelId, baseUrl, timeout);
            if (ok2) ok++; else fail++;
            sb.AppendLine(L.Pick($"  {(ok2 ? "✅" : "❌")} {c.Name}（{ModelCatalog.ShortDisplayName(c.ModelId)}）{detail}",
                                 $"  {(ok2 ? "✅" : "❌")} {c.Name} ({ModelCatalog.ShortDisplayName(c.ModelId)}) {detail}"));
        }
        sb.AppendLine(L.Pick($"\n汇总：✅ {ok} 可用　❌ {fail} 失败　⏭ {skip} 跳过(无key)",
                             $"\nSummary: ✅ {ok} usable   ❌ {fail} failed   ⏭ {skip} skipped (no key)"));
        return sb.ToString();
    }

    /// <summary>
    /// 切换免费模型前记住的模型（/free-restore / --model restore 恢复）。
    /// 持久化到 config.json（freePrevProvider/Model/BaseUrl）：跨会话可恢复（CLI 一次性进程也能还原）。
    /// </summary>

    private static (ProbeTarget Target, bool Ok, string Detail, EndpointStatus Status)[] RunProbes(List<ProbeTarget> targets)
    {
        var results = new (ProbeTarget, bool, string, EndpointStatus)[targets.Count];
        var indexed = targets.Select((t, i) => (t, i)).ToArray();
        System.Threading.Tasks.Parallel.ForEach(indexed,
            new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = 4 },
            item =>
            {
                var (t, i) = item;
                var (o, d, st) = ProbeEndpoint(t.BaseUrl, t.Key);
                results[i] = (t, o, d, st); // 各索引只写一次，无竞态；顺序由数组下标保证
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
        sb.AppendLine(L.Pick("**模型连通性测试**", "**Model connectivity test**"));
        int ok = 0, total = 0;

        var targets = new List<ProbeTarget>();

        // ── 1. 已存 API key：按供应商逐一测试（含目录内无模型的供应商） ──
        var keys = ApiKeyStore.ListAll().OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var (pid, key) in keys)
        {
            var baseUrl = ResolveProviderBaseUrl(pid);
            var models = ModelCatalog.ByProvider(pid).Select(m => m.Id).ToArray();
            var display = ModelCatalog.ProviderDisplayName(pid);
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
            return L.Pick("没有可测试的端点：既无已存 key，也无本地模型。\n"
                        + "  存 key: --model key <供应商> <key>　本地模型: --model connect <localhost:port>",
                          "No endpoints to test: no stored keys and no local models.\n"
                        + "  Store a key: --model key <provider> <key>   Local model: --model connect <localhost:port>");

        if (targets.Any(t => !t.IsLocal))
        {
            sb.AppendLine();
            sb.AppendLine(L.Pick($"### API Key（{targets.Count(t => !t.IsLocal)} 个供应商）",
                                 $"### API Keys ({targets.Count(t => !t.IsLocal)} providers)"));
        }

        // 并发探测（每项独立 HttpClient+4s 超时），保持原顺序输出
        bool localHeaderShown = false;
        foreach (var (t, o, d, _) in RunProbes(targets))
        {
            if (t.IsLocal && !localHeaderShown)
            {
                sb.AppendLine();
                sb.AppendLine(L.Pick("### 本地端点（无需 key）", "### Local endpoints (no key needed)"));
                localHeaderShown = true;
            }
            total++;
            if (o) ok++;
            sb.AppendLine(L.Pick($"【{t.Display}】", $"[{t.Display}]")
                          + (string.IsNullOrEmpty(t.BaseUrl) ? "" : " " + t.BaseUrl));
            sb.AppendLine($"  {(o ? "✅" : "❌")} {d}" + (t.Models.Length > 0 ? $"  —  {string.Join(", ", t.Models)}" : ""));
        }

        sb.AppendLine();
        sb.AppendLine(L.Pick($"**结论：{ok} / {total} 个端点可连接**",
                             $"**Result: {ok} / {total} endpoints reachable**"));
        return sb.ToString().Trim();
    }

    /// <summary>连通性探测结果（结构化，供 Web 序列化为 JSON）。
    /// ⚠ <paramref name="Status"/> 是**判据**，<paramref name="Detail"/> 只是文案 ——
    /// 消费方一律按 Status 分支，别对 Detail 做 Contains/StartsWith（理由见 <see cref="EndpointStatus"/>）。
    /// <para>
    /// ⚠ <c>Status</c> **刻意不给默认值**：留个 `= Unreachable` 的默认值就等于允许调用方
    /// 「忘了给状态」，而后果是静默降级（扫描列显示「不通」、剪除逻辑删错东西）。
    /// 不给默认值 ⇒ 每个构造点都得写出来，漏了**编不过**（同 `DrawCommand.Vector` 那条）。
    /// </para></summary>
    public record EndpointProbe(string ProviderId, string Display, string? BaseUrl, bool Ok, string Detail,
        string[] Models, EndpointStatus Status);

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
            var display = ModelCatalog.ProviderDisplayName(pid);
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
            targets.Add(new ProbeTarget(g.First().ProviderId, g.First().Provider,
                ProbeBaseUrlOverride ?? g.Key, null,
                g.Select(m => m.Id).Distinct().ToArray(), IsLocal: true));

        // 并发探测，保持原顺序
        return RunProbes(targets)
            .Select(r => new EndpointProbe(r.Target.ProviderId, r.Target.Display, r.Target.BaseUrl,
                r.Ok, r.Detail, r.Target.Models, r.Status))
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
            return L.Pick("没有已存 API key 可清理。存 key: --model key <供应商> <key>",
                          "No stored API keys to prune. Store one: --model key <provider> <key>");

        var sb = new StringBuilder();
        sb.AppendLine(L.Pick("**清理失效供应商**", "**Prune dead providers**"));
        sb.AppendLine();
        int removedKeys = 0, removedModels = 0, kept = 0;

        foreach (var (pid, key) in keys)
        {
            var baseUrl = ResolveProviderBaseUrl(pid);
            var display = ModelCatalog.ProviderDisplayName(pid);

            // 无端点：供应商不存在或未配置 base_url（写错地址/拼错供应商）→ 删模型，key 保留
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var n = ModelCatalog.RemoveCustomByProvider(pid);
                removedModels += n;
                sb.AppendLine(L.Pick($"🗑️  【{display}】无端点（供应商不存在或未配置 base_url）— 已删模型",
                                     $"🗑️  [{display}] no endpoint (provider missing or base_url unset) — models removed")
                              + (n > 0 ? L.Pick($" {n} 个", $" ({n})") : "")
                              + L.Pick("（key 保留）", " (key kept)"));
                continue;
            }

            var (ok, detail, status) = ProbeEndpoint(baseUrl, key);
            if (ok)
            {
                kept++;
                sb.AppendLine(L.Pick($"✅ 【{display}】{detail} — 保留", $"✅ [{display}] {detail} — kept"));
                continue;
            }

            // 无效 key：询问用户是否删除（交互确认）；非交互（管道/一次性）默认保留
            // ⚠ 判据是 Status（机器可读），**不是** detail 里有没有「密钥无效」四个字 ——
            //   按文案判定的话，英文界面下这一支恒假 ⇒ 「key 失效」被判成「地址写错」，
            //   于是走进下面的删模型分支（`RemoveCustomByProvider`），把好模型一起删掉。
            if (status == EndpointStatus.BadKey)
            {
                if (ConfirmDelete(L.Pick($"【{display}】检测到无效 API key（{pid}），是否删除？",
                                         $"[{display}] invalid API key detected ({pid}). Delete it?")))
                {
                    ApiKeyStore.Remove(pid);
                    removedKeys++;
                    sb.AppendLine(L.Pick($"🗑️  【{display}】{detail} — 已删除无效 key",
                                         $"🗑️  [{display}] {detail} — invalid key removed"));
                }
                else
                {
                    sb.AppendLine(L.Pick($"⚠️  【{display}】{detail} — key 保留（--model key rm {pid} 显式删除）",
                                         $"⚠️  [{display}] {detail} — key kept (delete explicitly with --model key rm {pid})"));
                }
                continue;
            }

            var m = ModelCatalog.RemoveCustomByProvider(pid);
            removedModels += m;
            sb.AppendLine(L.Pick($"🗑️  【{display}】{detail} — 已删模型",
                                 $"🗑️  [{display}] {detail} — models removed")
                          + (m > 0 ? L.Pick($" {m} 个", $" ({m})") : "")
                          + L.Pick("（key 保留）", " (key kept)"));
        }

        sb.AppendLine();
        sb.AppendLine(L.Pick(
            $"**结论：删除 {removedKeys} 个失效供应商的 key，移除 {removedModels} 个自定义模型；保留 {kept} 个**",
            $"**Result: {removedKeys} dead-provider keys deleted, {removedModels} custom models removed; {kept} kept**"));
        return sb.ToString().Trim();
    }

    /// <summary>解析模型的有效 base_url：provider 唯一地址优先 > 模型默认地址 > 本地(Ollama)默认 localhost:11434</summary>
}

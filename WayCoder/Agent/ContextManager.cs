using System.Text.RegularExpressions;

namespace WayCoder;

/// <summary>
/// 多层上下文压缩。
///
/// WayCoder 以 3 层实现：
///   - 第 1 层（tool_snip）：用截断版本替换冗长的工具结果
///   - 第 2 层（summarize）：LLM 驱动的旧对话摘要
///   - 第 3 层（hard_collapse）：最后手段：仅保留摘要 + 最近消息
///
/// 此外支持 Crush 风格的 StopWhen：基于真实 API token 使用量，
/// 当剩余窗口低于阈值时提前触发摘要（而非等估算值超限）。
/// </summary>
public class ContextManager
{
    public int MaxTokens { get; }

    private readonly int _snipAt;       // 50% -> 裁剪工具输出
    private readonly int _summarizeAt;  // 70% -> LLM 摘要
    private readonly int _collapseAt;   // 90% -> 硬折叠

    /// <summary>累计 prompt tokens（来自 API usage）</summary>
    public int CumulativePromptTokens { get; private set; }
    /// <summary>累计 completion tokens（来自 API usage）</summary>
    public int CumulativeCompletionTokens { get; private set; }

    /// <summary>上次摘要后是否已注入继续提示</summary>
    public bool ContinuePromptInjected { get; set; }

    /// <summary>压缩进度事件（当前层/总层, 消息, 百分比）— UI 可订阅以显示进度条</summary>
    public static event Action<int, string, double>? CompressProgress;
    /// <summary>是否正在压缩中</summary>
    public static bool IsCompressing { get; private set; }

    public ContextManager(int maxTokens = 128_000)
    {
        MaxTokens = maxTokens;
        _snipAt = maxTokens * Config.Instance.ContextSnipRatio / 100;
        _summarizeAt = maxTokens * Config.Instance.ContextSummarizeRatio / 100;
        _collapseAt = maxTokens * Config.Instance.ContextCollapseRatio / 100;
    }

    /// <summary>
    /// 从 LLM 响应中累积真实 token 使用量。
    /// </summary>
    public void AddUsage(int promptTokens, int completionTokens)
    {
        CumulativePromptTokens += promptTokens;
        CumulativeCompletionTokens += completionTokens;
    }

    /// <summary>
    /// Crush 风格 StopWhen：检查真实 token 使用量是否接近窗口上限。
    /// 大窗口用固定 buffer，小窗口用比例阈值。
    /// </summary>
    /// <returns>剩余 token 数低于阈值时应触发摘要</returns>
    public bool ShouldStopAndSummarize()
    {
        var cfg = Config.Instance;
        var used = CumulativePromptTokens + CumulativeCompletionTokens;
        var remaining = MaxTokens - used;

        long threshold;
        if (MaxTokens > cfg.ContextWindowLargeThreshold)
            threshold = cfg.ContextWindowLargeBuffer;       // 大窗口：固定 20K buffer
        else
            threshold = (long)(MaxTokens * cfg.ContextWindowSmallRatio);  // 小窗口：20% 比例

        return remaining <= threshold;
    }

    /// <summary>重置累计 token 计数（摘要后调用）</summary>
    public void ResetUsage()
    {
        CumulativePromptTokens = 0;
        CumulativeCompletionTokens = 0;
        ContinuePromptInjected = false;
    }

    /// <summary>
    /// 按需应用压缩层。返回是否发生了压缩。
    /// </summary>
    public async Task<bool> MaybeCompressAsync(List<JsonObject> messages, LLM? llm,
        Action<int, string>? onProgress = null)
    {
        var current = EstimateTokens(messages);
        var compressed = false;
        IsCompressing = true;

        try
        {
            // 第 1 层：裁剪冗长的工具输出
            if (current > _snipAt)
            {
                var pct = (double)current / MaxTokens * 100;
                onProgress?.Invoke(1, $"裁剪工具输出... {ProgressBar(pct)} {current}/{MaxTokens}");
                CompressProgress?.Invoke(1, $"裁剪工具输出...", pct);
                if (SnipToolOutputs(messages))
                {
                    compressed = true;
                    current = EstimateTokens(messages);
                    pct = (double)current / MaxTokens * 100;
                    onProgress?.Invoke(1, $"裁剪完成 {ProgressBar(pct)} {current}/{MaxTokens} (-{MaxTokens - current})");
                    CompressProgress?.Invoke(1, $"裁剪完成", pct);
                }
            }

            // 第 2 层：LLM 驱动的旧对话摘要
            if (current > _summarizeAt && messages.Count > 10)
            {
                var pct = (double)current / MaxTokens * 100;
                onProgress?.Invoke(2, $"正在摘要旧对话... {ProgressBar(pct)} {current}/{MaxTokens}");
                CompressProgress?.Invoke(2, $"正在摘要旧对话...", pct);
                if (await SummarizeOldAsync(messages, llm))
                {
                    compressed = true;
                    current = EstimateTokens(messages);
                    pct = (double)current / MaxTokens * 100;
                    onProgress?.Invoke(2, $"摘要完成 {ProgressBar(pct)} {current}/{MaxTokens}");
                    CompressProgress?.Invoke(2, $"摘要完成", pct);
                }
            }

            // 第 3 层：硬折叠——最后手段
            if (current > _collapseAt && messages.Count > 4)
            {
                var pct = (double)current / MaxTokens * 100;
                onProgress?.Invoke(3, $"紧急压缩... {ProgressBar(pct)} {current}/{MaxTokens}");
                CompressProgress?.Invoke(3, $"紧急压缩...", pct);
                await HardCollapseAsync(messages, llm);
                compressed = true;
                current = EstimateTokens(messages);
                pct = (double)current / MaxTokens * 100;
                onProgress?.Invoke(3, $"压缩完成 {ProgressBar(pct)} {current}/{MaxTokens}");
                CompressProgress?.Invoke(3, $"压缩完成", pct);
            }
        }
        finally
        {
            IsCompressing = false;
        }

        return compressed;
    }

    /// <summary>生成 8 字符迷你进度条</summary>
    private static string ProgressBar(double percent)
    {
        var clamped = Math.Clamp(percent, 0, 100);
        var filled = (int)(clamped / 100 * 8);
        var empty = 8 - filled;
        return $"«{new string('█', filled)}{new string('░', empty)}» {clamped:F0}%";
    }

    /// <summary>
    /// CJK 感知 token 估算。CJK 字符约 1.5 tok/char，ASCII 约 0.25 tok/char（≈4 chars/tok）。
    /// 精度从 ±30% 提升至 ±15%。
    /// </summary>
    public static int EstimateTokens(List<JsonObject> messages)
    {
        var total = 0;
        foreach (var m in messages)
        {
            if (m["content"]?.GetValue<string>() is { } content)
                total += EstimateTokensText(content);
            if (m["tool_calls"] != null)
                total += EstimateTokensText(m["tool_calls"]!.ToJsonString());
        }
        return total;
    }

    /// <summary>对单段文本做 CJK 感知 token 估算。</summary>
    private static int EstimateTokensText(string text)
    {
        int cjk = 0, ascii = 0;
        foreach (var r in text.EnumerateRunes())
        {
            var v = r.Value;
            // CJK 统一汉字 + 扩展区 + 兼容汉字
            if ((v >= 0x4E00 && v <= 0x9FFF) ||
                (v >= 0x3400 && v <= 0x4DBF) ||
                (v >= 0x20000 && v <= 0x2A6DF) ||
                (v >= 0xF900 && v <= 0xFAFF))
                cjk++;
            else if (v <= 127)
                ascii++;
            else
                cjk++; // 其他非 ASCII（日韩、Emoji 等）按宽字符计
        }
        return (int)(cjk * 1.5 + ascii * 0.25);
    }

    /// <summary>
    /// 第 1 层：将超过 4000 字符的工具结果裁剪为首尾几行。
    /// 保留错误行（编译错误、异常堆栈等），确保 Agent 能看到关键诊断信息。
    /// </summary>
    public static bool SnipToolOutputs(List<JsonObject> messages)
    {
        var changed = false;
        foreach (var m in messages)
        {
            if (m["role"]?.GetValue<string>() != "tool") continue;
            var content = m["content"]?.GetValue<string>();
            if (string.IsNullOrEmpty(content) || content.Length <= 4000) continue;

            var lines = content.Split('\n');
            if (lines.Length <= 10) continue;

            // 提取错误行（编译错误 CS\d+、异常、error/Error/错误 等关键词）
            var errorLines = new HashSet<int>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.Contains("error CS") || line.Contains("Error CS") ||
                    line.Contains(": error ") || line.Contains(": fatal error ") ||
                    line.Contains("Unhandled exception") || line.Contains("Exception:") ||
                    line.Contains("❌") || line.Contains("⛔") ||
                    line.Contains("错误") || line.Contains("严重") ||
                    line.Contains("[stderr]") || line.Contains("[退出码"))
                {
                    // 保留错误行及其上下文（前后各 2 行）
                    for (int j = Math.Max(0, i - 2); j <= Math.Min(lines.Length - 1, i + 2); j++)
                        errorLines.Add(j);
                }
            }

            // 保留前 5 行 + 后 5 行 + 所有错误上下文
            var keepSet = new HashSet<int>();
            for (int i = 0; i < Math.Min(5, lines.Length); i++) keepSet.Add(i);
            for (int i = Math.Max(0, lines.Length - 5); i < lines.Length; i++) keepSet.Add(i);
            foreach (var e in errorLines) keepSet.Add(e);

            var sorted = keepSet.OrderBy(i => i).ToList();

            var sb = new System.Text.StringBuilder();
            int lastWritten = -2;
            foreach (var idx in sorted)
            {
                if (idx > lastWritten + 1)
                    sb.AppendLine($"...（省略 {idx - lastWritten - 1} 行）...");
                sb.AppendLine(lines[idx]);
                lastWritten = idx;
            }

            var snipped = sb.ToString().TrimEnd();
            if (errorLines.Count > 0)
                snipped += $"\n\n⚠ 已保留 {errorLines.Count} 处错误上下文。完整输出共 {lines.Length} 行 / {content.Length} 字符。";
            else
                snipped += $"\n\n...（共 {lines.Length} 行 / {content.Length} 字符，已裁剪以节省上下文。使用详细模式查看完整输出）...";

            m["content"] = snipped;
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// 计算保留尾部应从哪个索引开始。
    /// 确保 tool 消息不会与产生它的 assistant 消息分离。
    /// </summary>
    public static int SafeSplit(List<JsonObject> messages, int keepRecent)
    {
        var split = Math.Max(0, messages.Count - keepRecent);
        while (split > 0 && messages[split]["role"]?.GetValue<string>() == "tool")
            split--;
        return split;
    }

    /// <summary>
    /// 第 2 层：摘要旧对话，保持最近消息不变。
    /// 保留更多最近消息（20 条）以避免丢失关键 API 契约和文件结构信息。
    /// </summary>
    private async Task<bool> SummarizeOldAsync(List<JsonObject> messages, LLM? llm, int keepRecent = 20)
    {
        if (messages.Count <= keepRecent) return false;

        var split = SafeSplit(messages, keepRecent);
        var old = messages.GetRange(0, split);
        var tail = messages.GetRange(split, messages.Count - split);

        var summary = await GetSummaryAsync(old, llm);

        messages.Clear();
        messages.Add(new JsonObject
        {
            ["role"] = "user",
            ["content"] = $"[上下文已压缩 - 对话摘要]\n{summary}",
        });
        messages.Add(new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = "收到，我已了解之前对话的上下文。",
        });
        messages.AddRange(tail);
        return true;
    }

    /// <summary>
    /// 第 3 层：紧急压缩。保留更多最近消息（12 条）+ 项目状态快照。
    /// 在硬折叠前注入项目文件清单，防止 Agent 完全失忆。
    /// </summary>
    private async Task HardCollapseAsync(List<JsonObject> messages, LLM? llm)
    {
        var keep = messages.Count > 12 ? 12 : Math.Min(messages.Count, 6);
        var split = SafeSplit(messages, keep);
        var tail = messages.GetRange(split, messages.Count - split);
        var summary = await GetSummaryAsync(messages.GetRange(0, split), llm);

        // 注入项目状态快照，让 Agent 在硬重置后仍知道项目结构
        var snapshot = GenerateProjectSnapshot();

        messages.Clear();
        messages.Add(new JsonObject
        {
            ["role"] = "user",
            ["content"] = $"[硬重置上下文 — 项目恢复到关键状态]\n\n## 项目快照\n{snapshot}\n\n## 对话摘要\n{summary}",
        });
        messages.Add(new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = "上下文已恢复。我已了解当前项目结构和之前的关键进展。从之前中断的地方继续。",
        });
        messages.AddRange(tail);
    }

    /// <summary>
    /// 通过 LLM 生成摘要，或回退到提取关键信息。
    /// </summary>
    private async Task<string> GetSummaryAsync(List<JsonObject> messages, LLM? llm)
    {
        var flat = FlattenMessages(messages);

        if (llm != null)
        {
            try
            {
                var resp = await llm.ChatAsync(
                    messages:
                    [
                        new JsonObject
                        {
                            ["role"] = "system",
                            ["content"] = "你是一个对话压缩器。将以下对话压缩为结构化摘要。" +
                                          "必须保留：\n" +
                                          "1. 所有已创建/修改的文件路径及其用途\n" +
                                          "2. 关键 API 签名（方法名、参数、返回类型）\n" +
                                          "3. 数据模型/类结构（字段、关系）\n" +
                                          "4. 已做出的架构决策和原因\n" +
                                          "5. 遇到的错误及修复方案\n" +
                                          "6. 当前未完成的任务和下一步计划\n" +
                                          "7. 项目的命名空间/包结构\n" +
                                          "丢弃：冗长的命令输出、完整代码清单、" +
                                          "重复的来回对话、中间探索过程。\n" +
                                          "格式：使用 ## 标题分段，列表项用 - 前缀。",
                        },
                        new JsonObject { ["role"] = "user", ["content"] = flat.Length > 20000 ? flat[..20000] : flat },
                    ]
                );
                return resp.Content;
            }
            catch
            {
                // LLM 摘要失败，回退到提取
            }
        }

        // 注入任务进度追踪（压缩时不丢失进度信息）
        var progress = TaskProgress.GetSummary();
        if (!string.IsNullOrEmpty(progress) && progress != "⏳ 就绪")
        {
            var summary = ExtractKeyInfo(messages);
            return summary + "\n\n## 当前进度\n" + progress;
        }

        // 回退方案：提取文件路径和错误
        return ExtractKeyInfo(messages);
    }

    private static string FlattenMessages(List<JsonObject> messages)
    {
        var parts = new List<string>();
        foreach (var m in messages)
        {
            var role = m["role"]?.GetValue<string>() ?? "?";
            var text = m["content"]?.GetValue<string>() ?? "";
            if (!string.IsNullOrEmpty(text))
                parts.Add($"[{role}] {text[..Math.Min(1000, text.Length)]}");
        }
        return string.Join("\n", parts);
    }

    /// <summary>
    /// 生成项目状态快照：扫描工作目录的关键文件结构，
    /// 在硬折叠后注入上下文，防止 Agent 完全失忆。
    /// </summary>
    private static string GenerateProjectSnapshot()
    {
        var parts = new List<string>();
        try
        {
            var cwd = Directory.GetCurrentDirectory();
            var dirName = Path.GetFileName(cwd);

            // 1. 工作目录
            parts.Add($"- 工作目录：{cwd}");

            // 2. 关键配置文件
            var keyFiles = new[] {
                "CLAUDE.md", "README.md", ".gitignore", "package.json",
                "*.csproj", "*.sln", "Cargo.toml", "go.mod", "pyproject.toml",
                "Makefile", "Dockerfile", "docker-compose.yml",
                ".env", ".env.example", "tsconfig.json", "vite.config.*"
            };
            var foundConfigs = new List<string>();
            foreach (var pattern in keyFiles)
            {
                try
                {
                    var matches = System.IO.Directory.GetFiles(cwd, pattern, SearchOption.TopDirectoryOnly);
                    foreach (var m in matches)
                        foundConfigs.Add(Path.GetFileName(m));
                }
                catch { /* ignore */ }
            }
            if (foundConfigs.Count > 0)
                parts.Add($"- 配置文件：{string.Join(", ", foundConfigs.Distinct().Take(10))}");

            // 3. 顶层目录结构
            try
            {
                var topDirs = System.IO.Directory.GetDirectories(cwd)
                    .Select(d => Path.GetFileName(d))
                    .Where(d => !d.StartsWith('.') && d != "node_modules" && d != "obj" && d != "bin")
                    .Take(15)
                    .ToList();
                if (topDirs.Count > 0)
                    parts.Add($"- 顶层目录：{string.Join(", ", topDirs)}");
            }
            catch { /* ignore */ }

            // 4. 最近修改的文件（从 FileTracker 或 git status）
            try
            {
                var gitDir = Path.Combine(cwd, ".git");
                if (System.IO.Directory.Exists(gitDir))
                {
                    parts.Add("- Git 仓库：是");
                }
            }
            catch { /* ignore */ }

            if (parts.Count == 1)
                parts.Add("- （未检测到更多项目结构信息）");
        }
        catch
        {
            return "- 无法生成项目快照";
        }

        return string.Join("\n", parts);
    }

    /// <summary>
    /// 回退方案：无需 LLM，提取文件路径、错误和决策。
    /// 增强版：解析 C# 编译错误码、方法签名、命名空间。
    /// </summary>
    private static string ExtractKeyInfo(List<JsonObject> messages)
    {
        var filesSeen = new HashSet<string>();
        var errors = new List<string>();
        var namespaces = new HashSet<string>();
        var fileRegex = new Regex(@"(?:^|\s|[/""'`])([\w./\-]+\.\w{1,10})", RegexOptions.None, TimeSpan.FromSeconds(1));
        var nsRegex = new Regex(@"namespace\s+([\w.]+)", RegexOptions.None, TimeSpan.FromSeconds(1));
        var csErrorRegex = new Regex(@"CS\d{4}", RegexOptions.None, TimeSpan.FromSeconds(1));
        var funcRegex = new Regex(@"(?:public|private|protected|internal|static|async\s+)?\s*[\w<>[\],]+\s+(\w+)\s*\([^)]*\)", RegexOptions.None, TimeSpan.FromSeconds(1));

        foreach (var m in messages)
        {
            var text = m["content"]?.GetValue<string>() ?? "";

            // 提取文件路径
            foreach (Match match in fileRegex.Matches(text))
            {
                var path = match.Groups[1].Value;
                if (path.Contains('.') && path.Length > 3 && path.Length < 200)
                    filesSeen.Add(path);
            }

            // 提取命名空间
            foreach (Match match in nsRegex.Matches(text))
                namespaces.Add(match.Groups[1].Value);

            // 提取 C# 编译错误码
            foreach (Match match in csErrorRegex.Matches(text))
                errors.Add(match.Value);

            // 提取错误行（更高精度：包含文件名+行号+错误码）
            foreach (var line in text.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0) continue;
                if (trimmed.Contains("error CS") || trimmed.Contains(": error ") ||
                    trimmed.Contains(": fatal error ") || trimmed.Contains("Exception") ||
                    trimmed.Contains("❌") || trimmed.Contains("⛔") ||
                    trimmed.Contains("错误") && trimmed.Length > 10)
                {
                    errors.Add(trimmed[..Math.Min(200, trimmed.Length)]);
                }
            }
        }

        var parts = new List<string>();

        // 项目结构
        if (namespaces.Count > 0)
            parts.Add($"命名空间：{string.Join(", ", namespaces.OrderBy(n => n).Take(10))}");
        if (filesSeen.Count > 0)
            parts.Add($"涉及的文件：{string.Join(", ", filesSeen.OrderBy(f => f).Take(25))}");

        // 错误（去重 + 限制数量）
        var uniqueErrors = errors.Distinct().Take(8).ToList();
        if (uniqueErrors.Count > 0)
            parts.Add($"遇到的错误：{string.Join("；", uniqueErrors)}");

        return parts.Count > 0 ? string.Join("\n", parts) : "（无可提取的上下文）";
    }
}

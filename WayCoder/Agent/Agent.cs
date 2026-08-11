using System.Text.RegularExpressions;
using WayCoder.Tools;

namespace WayCoder;

/// <summary>
/// 核心智能体循环。这是 WayCoder 的心脏。
///
/// 模式很简单：
///   用户消息 -> LLM（带工具）-> 有工具调用？-> 执行 -> 循环
///                             -> 文本回复？-> 返回给用户
///
/// 它会持续循环，直到 LLM 回复纯文本（没有工具调用），
/// 这意味着它已完成工作并准备报告结果。
/// </summary>
public class Agent
{
    /// <summary>LLM 客户端（大模型做复杂任务，小模型做压缩/摘要）</summary>
    public LLM LlmClient { get; }
    /// <summary>已注册的工具列表</summary>
    public List<ITool> Tools { get; }
    /// <summary>工具名 → 工具实例的快速查找字典</summary>
    public Dictionary<string, ITool> ToolByName { get; }
    /// <summary>对话消息历史（OpenAI 格式）</summary>
    public List<JsonObject> Messages { get; set; } = [];
    /// <summary>上下文管理器（三层压缩 + token 预算）</summary>
    public ContextManager Context { get; }

    private readonly int _maxRounds;
    private readonly double? _maxBudgetUsd;
    private readonly string _systemPrompt;

    private bool _autoCommit;

    /// <summary>本轮修改过的文件（用于精准 git add）</summary>
    private readonly HashSet<string> _modifiedFiles = [];

    /// <summary>Architect 双模型模式：大模型出计划，小模型执行</summary>
    public bool ArchitectMode { get; set; }

    /// <summary>最大对话轮次（-1 表示从 Config.Instance.MaxRounds 读取）</summary>
    private readonly int _effectiveMaxRounds;

    /// <summary>是否启用自动 Git Commit（可运行时切换）</summary>
    public bool AutoCommitEnabled
    {
        get => _autoCommit;
        set => _autoCommit = value;
    }

    /// <summary>
    /// 创建 Agent 实例。
    /// </summary>
    /// <param name="llm">LLM 客户端</param>
    /// <param name="tools">工具列表（默认使用 ToolRegistry.AllTools）</param>
    /// <param name="maxContextTokens">上下文窗口上限</param>
    /// <param name="maxRounds">最大对话轮次</param>
    /// <param name="maxBudgetUsd">最大美元预算（null=无限制）</param>
    /// <param name="autoCommit">工具执行后自动 git commit</param>
    public Agent(LLM llm, List<ITool>? tools = null,
        int maxContextTokens = 128_000, int maxRounds = -1,
        double? maxBudgetUsd = null, bool autoCommit = false)
    {
        LlmClient = llm;
        _maxBudgetUsd = maxBudgetUsd;
        _autoCommit = autoCommit;
        Tools = tools ?? ToolRegistry.AllTools;
        ToolByName = Tools.ToDictionary(t => t.Name);
        Context = new ContextManager(maxContextTokens);
        _maxRounds = maxRounds;
        _effectiveMaxRounds = maxRounds > 0 ? maxRounds : Config.Instance.MaxRounds;
        _systemPrompt = SystemPrompt.Generate(Tools);

        // 连接子智能体能力
        foreach (var t in Tools)
        {
            if (t is AgentTool agentTool)
                agentTool.ParentAgent = this;
        }
    }

    /// <summary>
    /// 构建完整消息列表（包含系统提示词 + 模式提示）。
    /// </summary>
    private List<JsonObject> FullMessages()
    {
        // 模式专用提示（Plan/Review/Auto 模式会在主提示词前注入约束）
        var modePrompt = WorkModeManager.GetModePrompt(WorkModeManager.CurrentMode);
        var systemContent = string.IsNullOrEmpty(modePrompt)
            ? _systemPrompt
            : modePrompt + "\n" + _systemPrompt;

        var result = new List<JsonObject>
        {
            new() { ["role"] = "system", ["content"] = systemContent },
        };
        // 深克隆消息，避免 JsonNode 的 Parent 冲突（同一消息不能加入两个树）
        foreach (var m in Messages)
            result.Add(JsonNode.Parse(m.ToJsonString())!.AsObject());
        return result;
    }

    /// <summary>
    /// 获取工具 schema 列表。
    /// </summary>
    private List<JsonObject> ToolSchemas() => Tools.Select(t => t.Schema()).ToList();

    /// <summary>
    /// 处理一条用户消息，执行 Agent 主循环（多轮 LLM/工具交互直到完成或超限）。
    /// </summary>
    /// <param name="userInput">用户输入文本</param>
    /// <param name="onToken">流式 token 回调</param>
    /// <param name="onTool">工具调用回调（工具名, 结果摘要）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Agent 最终回复文本</returns>
    public async Task<string> ChatAsync(
        string userInput,
        Action<string>? onToken = null,
        Action<string, string>? onTool = null,
        Action<string>? onToolOutput = null,
        CancellationToken cancellationToken = default)
    {
        Messages.Add(new JsonObject { ["role"] = "user", ["content"] = userInput });
        await CompressWithSmallModel();

        // ── Architect 模式：大模型出计划 → 小模型执行 ──
        if (ArchitectMode && LlmClient.EffectiveModel != LlmClient.SmallModel)
        {
            var plan = await GenerateArchitectPlanAsync(onToken, cancellationToken);
            if (plan == null)
            {
                Messages.RemoveAt(Messages.Count - 1); // 回滚用户消息
                return "⚠ Architect 模式：大模型计划生成失败，已取消。";
            }
            // 将计划作为 system 消息注入，小模型继续执行
            Messages.Add(new JsonObject
            {
                ["role"] = "system",
                ["content"] = $"## 执行计划\n\n以下是 Architect 的分析和执行计划，请按步骤逐一执行：\n\n{plan}",
            });
            // 切换回小模型执行
            LlmClient.ModelOverride = LlmClient.SmallModel;
            onToken?.Invoke("\n📋 **计划已生成，切换到小模型执行...**\n\n");
        }

        for (int round = 0; round < _effectiveMaxRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 预算检查：超过上限则停止
            if (_maxBudgetUsd != null)
            {
                var spent = LlmClient.EstimatedCost ?? 0;
                if (spent >= _maxBudgetUsd.Value)
                    return $"🛑 已达到预算上限 ${_maxBudgetUsd:F2}（已花费 ${spent:F4}，{round} 轮）。增加预算请使用 --max-budget-usd。";
            }

            var resp = await LlmClient.ChatAsync(
                messages: FullMessages(),
                tools: ToolSchemas(),
                onToken: onToken,
                cancellationToken: cancellationToken);

            // 没有工具调用 -> LLM 完成，返回文本
            if (resp.ToolCalls.Count == 0)
            {
                Messages.Add(resp.ToMessage());
                return resp.Content;
            }

            // 有工具调用 -> 执行（多个时并行）
            Messages.Add(resp.ToMessage());

            try
            {
                if (resp.ToolCalls.Count == 1)
                {
                    var tc = resp.ToolCalls[0];
                    onTool?.Invoke(tc.Name, FormatBrief(tc.Arguments));
                    var result = await ExecuteToolAsync(tc, onToolOutput);
                    // 自动 lint 反馈闭环：写文件后立即检查，错误注入工具结果
                    result = await AppendLintFeedbackAsync(tc, result);
                    // 自动 test 反馈闭环：写源码文件后跑测试，失败注入工具结果
                    result = await AppendTestFeedbackAsync(tc, result);
                    Messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = tc.Id,
                        ["content"] = result,
                    });
                }
                else
                {
                    // 多个工具调用时并行执行（不流式输出，避免交叉混乱）
                    var tasks = resp.ToolCalls.Select(async tc =>
                    {
                        onTool?.Invoke(tc.Name, FormatBrief(tc.Arguments));
                        var result = await ExecuteToolAsync(tc);
                        return (tc, result);
                    });

                    var results = await Task.WhenAll(tasks);
                    foreach (var (tc, result) in results)
                    {
                        // 自动 lint + test 反馈闭环
                        var finalResult = await AppendLintFeedbackAsync(tc, result);
                        finalResult = await AppendTestFeedbackAsync(tc, finalResult);
                        Messages.Add(new JsonObject
                        {
                            ["role"] = "tool",
                            ["tool_call_id"] = tc.Id,
                            ["content"] = finalResult,
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ctrl+C 中断执行，回填缺失的工具回复
                AnswerPendingToolCalls(resp.ToolCalls);
                throw;
            }

            // 自动 git commit（如果启用）
            if (_autoCommit) await AutoCommitAsync();

            // 如果工具输出太大则压缩上下文
            await CompressWithSmallModel();
        }

        return "（已达到最大工具调用轮次）";
    }

    /// <summary>使用小模型执行上下文压缩（省钱）</summary>
    private async Task CompressWithSmallModel()
    {
        var saved = LlmClient.ModelOverride;
        LlmClient.ModelOverride = LlmClient.SmallModel;
        try { await Context.MaybeCompressAsync(Messages, LlmClient); }
        finally { LlmClient.ModelOverride = saved; }
    }

    /// <summary>
    /// 执行单个工具调用，返回结果字符串。
    /// </summary>
    private async Task<string> ExecuteToolAsync(ToolCall tc, Action<string>? onToolOutput = null)
    {
        if (!ToolByName.TryGetValue(tc.Name, out var tool))
            return $"错误：未知工具 '{tc.Name}'";

        try
        {
            // 工作模式约束检查：Plan/Review 模式下阻止修改性工具
            var modeBlock = WorkModeManager.CheckToolAllowed(tc.Name, WorkModeManager.CurrentMode);
            if (modeBlock != null)
                return modeBlock;

            // 权限检查：危险操作需要用户确认
            if (!await PermissionManager.CheckAsync(tc.Name, tc.Arguments))
                return "用户取消了此操作。";

            // PreToolUse hook
            var hookBlock = await HooksManager.RunPreToolUseAsync(tc.Name, tc.Arguments);
            if (hookBlock != null)
                return $"操作被 Hook 阻止: {hookBlock}";

            var result = tool is BashTool bashTool && onToolOutput != null
                ? await bashTool.ExecuteStreamingAsync(tc.Arguments,
                    async line => { onToolOutput(line); await Task.CompletedTask; })
                : await tool.ExecuteAsync(tc.Arguments);

            // 追踪修改的文件（用于自动 commit 精准暂存）
            if (tc.Name is "write_file" or "edit_file" or "notebook_edit")
            {
                if (tc.Arguments.TryGetValue("file_path", out var fp) && fp is string path && !string.IsNullOrWhiteSpace(path))
                {
                    lock (_modifiedFiles)
                        _modifiedFiles.Add(path);
                }
            }

            // PostToolUse hook（可修改结果）
            var hookResult = await HooksManager.RunPostToolUseAsync(tc.Name, tc.Arguments, result);
            if (hookResult != null)
                result = hookResult;

            // 错误自恢复：工具返回错误时追加修正提示
            if (result.StartsWith("错误") || result.StartsWith("Error"))
                result += "\n[请分析错误原因，修正参数后重试]";

            DebugLog.LogToolResult(tc.Name, result);
            return result;
        }
        catch (Exception ex)
        {
            return $"执行 {tc.Name} 时出错：{ex.Message}\n[请分析错误原因，尝试其他方式完成目标]";
        }
    }

    /// <summary>
    /// 写文件后自动运行 lint，错误注入工具结果，形成自动修复闭环。
    /// 仅对 write_file / edit_file 触发，lint 无错误则不追加。
    /// </summary>
    private async Task<string> AppendLintFeedbackAsync(ToolCall tc, string toolResult)
    {
        if (tc.Name is not "write_file" and not "edit_file")
            return toolResult;

        // 文件修改后使仓库地图缓存失效
        RepoMapGenerator.Invalidate();

        var filePath = tc.Arguments.GetValueOrDefault("file_path")?.ToString();
        if (string.IsNullOrWhiteSpace(filePath))
            return toolResult;

        try
        {
            var lang = LintTool.DetectLanguage(filePath);
            if (lang == null) return toolResult;

            var lintTool = new LintTool();
            var lintArgs = new Dictionary<string, object?> { ["path"] = filePath };
            var lintResult = await lintTool.ExecuteAsync(lintArgs);

            // 仅当 lint 发现问题时才追加反馈
            if (!lintResult.Contains("✅") && !lintResult.Contains("⚠"))
                return toolResult;

            // 截断过长输出
            if (lintResult.Length > 1500)
                lintResult = lintResult[..1500] + "\n... (已截断)";

            return toolResult + $"\n\n--- Lint 自动检查 ({lang}) ---\n{lintResult}";
        }
        catch
        {
            return toolResult; // lint 失败不影响主流程
        }
    }

    /// <summary>上次自动跑测试的时间（防抖：同一项目 60s 内不重复跑）</summary>
    private static DateTime _lastTestRun;
    private static string? _lastTestProject;

    /// <summary>
    /// 写源码文件后自动运行项目测试，失败结果注入工具结果，形成自动修复闭环。
    /// </summary>
    private async Task<string> AppendTestFeedbackAsync(ToolCall tc, string toolResult)
    {
        if (tc.Name is not "write_file" and not "edit_file")
            return toolResult;

        var filePath = tc.Arguments.GetValueOrDefault("file_path")?.ToString();
        if (string.IsNullOrWhiteSpace(filePath)) return toolResult;

        // 仅源码文件触发测试
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var srcExts = new[] { ".cs", ".py", ".ts", ".js", ".go", ".rs", ".java", ".kt", ".swift", ".c", ".cpp", ".rb" };
        if (!srcExts.Contains(ext)) return toolResult;

        // 防抖：同一项目 60s 内不重复跑
        var cwd = Directory.GetCurrentDirectory();
        if (_lastTestProject == cwd && (DateTime.UtcNow - _lastTestRun).TotalSeconds < 60)
            return toolResult;

        var testCmd = DetectTestCommand();
        if (testCmd == null) return toolResult;

        // WayCoder 自己的自测可能很慢，跳过（编辑 WayCoder 自身时）
        if (testCmd.Contains("--test") && File.Exists(Path.Combine(cwd, "SelfTest.cs")))
            return toolResult;

        try
        {
            _lastTestProject = cwd;
            _lastTestRun = DateTime.UtcNow;

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = testCmd.Split(' ')[0],
                Arguments = string.Join(' ', testCmd.Split(' ').Skip(1)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc == null) return toolResult;

            // 最多等 30 秒
            var readTask = proc.StandardOutput.ReadToEndAsync();
            var timeoutTask = Task.Delay(30_000);
            var completed = await Task.WhenAny(readTask, timeoutTask);
            if (completed == timeoutTask)
            {
                try { proc.Kill(); } catch { }
                return toolResult;
            }

            var output = await readTask;
            var errorOutput = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            var fullOutput = output + errorOutput;
            if (proc.ExitCode == 0)
                return toolResult; // 测试通过，不追加

            // 截断
            if (fullOutput.Length > 2000)
                fullOutput = fullOutput[..2000] + $"\n... (共 {fullOutput.Length} 字符)";

            return toolResult + $"\n\n--- 🔴 自动测试失败 (exit={proc.ExitCode}) ---\n{fullOutput}\n[请修复代码使测试通过]";
        }
        catch
        {
            return toolResult; // 测试失败不影响主流程
        }
    }

    /// <summary>检测当前项目的测试命令</summary>
    private static string? DetectTestCommand()
    {
        var cwd = Directory.GetCurrentDirectory();

        // WayCoder 自测 (内置 SelfTest)
        if (File.Exists(Path.Combine(cwd, "SelfTest.cs")))
            return "dotnet run -c Release -- --test 2>&1";

        // .NET 测试项目
        var testProjects = Directory.GetFiles(cwd, "*.Tests.csproj", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(cwd, "*.Test.csproj", SearchOption.AllDirectories)).ToArray();
        if (testProjects.Length > 0)
            return "dotnet test --nologo -v q 2>&1";

        // Node.js
        if (File.Exists(Path.Combine(cwd, "package.json")))
        {
            try
            {
                var pkg = System.Text.Json.Nodes.JsonNode.Parse(
                    File.ReadAllText(Path.Combine(cwd, "package.json")));
                if (pkg?["scripts"]?["test"] != null)
                    return "npm test --silent 2>&1";
            }
            catch { }
        }

        // Go
        if (File.Exists(Path.Combine(cwd, "go.mod")))
            return "go test ./... 2>&1";

        // Rust
        if (File.Exists(Path.Combine(cwd, "Cargo.toml")))
            return "cargo test -q 2>&1";

        // Python
        if (Directory.GetFiles(cwd, "test_*.py", SearchOption.AllDirectories).Any() ||
            Directory.GetFiles(cwd, "*_test.py", SearchOption.AllDirectories).Any())
            return "python -m pytest -q 2>&1";

        return null;
    }

    /// <summary>
    /// 为每个未收到工具回复的工具调用回填一条工具回复。
    /// 确保在执行被中断时历史记录保持有效。
    /// </summary>
    private void AnswerPendingToolCalls(List<ToolCall> toolCalls)
    {
        var answered = new HashSet<string>(
            Messages.Where(m => m["role"]?.GetValue<string>() == "tool")
                    .Select(m => m["tool_call_id"]?.GetValue<string>() ?? ""));

        foreach (var tc in toolCalls)
        {
            if (!answered.Contains(tc.Id))
            {
                Messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = tc.Id,
                    ["content"] = "[已中断]",
                });
            }
        }
    }

    /// <summary>
    /// 清空对话历史。
    /// </summary>
    /// <summary>清空对话历史，重置 Agent 状态。</summary>
    public void Reset() => Messages.Clear();

    private static string FormatBrief(Dictionary<string, object?> args, int maxLen = 80)
    {
        var s = string.Join(", ", args.Select(kv => $"{kv.Key}={FormatValue(kv.Value)}"));
        return s.Length > maxLen ? s[..maxLen] + "..." : s;
    }

    private static string FormatValue(object? value)
    {
        var s = value?.ToString() ?? "null";
        return s.Length > 40 ? s[..40] + "..." : s;
    }

    /// <summary>工具执行后自动 git commit。用小模型生成提交信息。</summary>
    private async Task AutoCommitAsync()
    {
        try
        {
            var gitDir = await RunGitAsync("rev-parse --git-dir");
            if (string.IsNullOrWhiteSpace(gitDir)) return;

            // 收集本轮修改的文件列表
            string[] modifiedFiles;
            lock (_modifiedFiles)
            {
                modifiedFiles = _modifiedFiles.ToArray();
                _modifiedFiles.Clear();
            }

            // 如果没有追踪到文件修改，检查 git status 兜底
            if (modifiedFiles.Length == 0)
            {
                var status = await RunGitAsync("status --porcelain");
                if (string.IsNullOrWhiteSpace(status)) return;
                var lines = status.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                modifiedFiles = lines.Select(f => f.Length > 3 ? f[3..].Trim() : f.Trim()).Take(10).ToArray();
                if (modifiedFiles.Length == 0) return;
            }

            // 获取简要 diff 统计作为提交正文
            var fileList = string.Join(", ", modifiedFiles);
            var diffStat = await RunGitAsync($"diff --stat --cached {string.Join(" ", modifiedFiles.Select(EscArg))}");
            if (string.IsNullOrWhiteSpace(diffStat))
                diffStat = await RunGitAsync($"diff --stat {string.Join(" ", modifiedFiles.Select(EscArg))}");

            // 小模型生成 conventional-commit 标题
            var summary = await GenerateCommitMsgAsync(fileList);
            if (string.IsNullOrWhiteSpace(summary)) return;

            // 构建提交信息：标题 + 空行 + diff 统计
            var commitMsg = summary;
            if (!string.IsNullOrWhiteSpace(diffStat) && diffStat.Length < 500)
                commitMsg += "\n\n" + diffStat.Trim();

            // 精准暂存：只 add 实际修改的文件
            foreach (var f in modifiedFiles)
            {
                if (File.Exists(f))
                    await RunGitAsync($"add {EscArg(f)}");
            }

            // 提交
            var msgFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(msgFile, commitMsg);
            await RunGitAsync($"commit -F {EscArg(msgFile)}");
            try { File.Delete(msgFile); } catch { }

            // 用户反馈
            _onAutoCommit?.Invoke(summary, modifiedFiles.Length);

            DebugLog.Log("auto-commit", $"Committed: {summary} ({modifiedFiles.Length} files)");
        }
        catch (Exception ex) { DebugLog.Log("auto-commit", $"AutoCommitAsync failed: {ex.Message}"); }
    }

    /// <summary>给 shell 参数做安全转义（单引号包裹，内部单引号替换为 '\''）</summary>
    internal static string EscArg(string s) => $"'{s.Replace("'", "'\\''")}'";

    /// <summary>自动提交完成后的回调</summary>
    private Action<string, int>? _onAutoCommit;

    /// <summary>注册自动提交回调（用于 UI 反馈）</summary>
    public void OnAutoCommit(Action<string, int> callback) => _onAutoCommit = callback;

    private async Task<string> GenerateCommitMsgAsync(string fileList)
    {
        try
        {
            var saved = LlmClient.ModelOverride;
            LlmClient.ModelOverride = LlmClient.SmallModel;
            try
            {
                var msgs = new List<JsonObject>
                {
                    new() { ["role"] = "system", ["content"] = "You are a git commit message generator. Output a single line, English, conventional-commit style with a valid prefix (feat/fix/docs/style/refactor/perf/test/chore/build/ci/revert), <70 chars. Do NOT include any quotes, backticks, or extra text." },
                    new() { ["role"] = "user", ["content"] = "Modified files: " + fileList + "\n\nCommit message:" },
                };
                var result = await LlmClient.ChatAsync(msgs, tools: null);
                var msg = CleanCommitMsg(result?.Content ?? "");

                // 质量校验：不合格则重试一次
                if (!IsValidCommitMsg(msg))
                {
                    msgs[0]["content"] = "Your previous output was invalid. Strict rules: exactly one line, English, conventional-commit prefix (feat/fix/docs/style/refactor/perf/test/chore/build/ci/revert), no quotes/backticks/code fences, <70 chars. Output only the message.";
                    result = await LlmClient.ChatAsync(msgs, tools: null);
                    msg = CleanCommitMsg(result?.Content ?? "");
                }

                // 重试后仍不合格：回退到安全默认信息
                if (!IsValidCommitMsg(msg))
                {
                    var firstFile = fileList.Split(',')[0].Trim();
                    msg = "chore: update " + firstFile;
                }
                return msg.Length > 72 ? msg[..72] : msg;
            }
            finally { LlmClient.ModelOverride = saved; }
        }
        catch { return ""; }
    }

    /// <summary>清理提交信息：去引号/反引号/换行/多余空白。</summary>
    internal static string CleanCommitMsg(string raw)
    {
        var msg = (raw ?? "").Trim();
        // 去掉代码围栏和常见的模板包裹
        msg = msg.Replace("```", "").Replace("`", "");
        msg = msg.Replace("\"", "").Replace("'", "");
        msg = msg.Replace("\n", " ").Replace("\r", " ").Trim();
        // 去掉 "Here is the commit message:" 之类的 AI 模板前缀
        var colonIdx = msg.IndexOf(":");
        if (colonIdx > 0 && colonIdx < 30)
        {
            var prefix = msg[..colonIdx].ToLowerInvariant();
            if (prefix.Contains("commit") || prefix.Contains("message") || prefix.Contains("here"))
                msg = msg[(colonIdx + 1)..].Trim();
        }
        return msg;
    }

    /// <summary>校验提交信息质量：必须有合法 conventional 前缀、非空、不含中文。</summary>
    internal static bool IsValidCommitMsg(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return false;
        if (msg.Length < 5) return false;
        // 拒绝中文（提交信息约定为英文）
        if (System.Text.RegularExpressions.Regex.IsMatch(msg, @"[\u4e00-\u9fff]")) return false;
        // 拒绝继续出现的模板短语
        var lower = msg.ToLowerInvariant();
        if (lower.Contains("here is") || lower.Contains("generate a") || lower.Contains("i will"))
            return false;
        // 必须匹配 conventional-commit 前缀
        var firstWord = msg.Split(' ')[0].Split(':')[0].Trim().ToLowerInvariant();
        return firstWord is "feat" or "fix" or "docs" or "style" or "refactor"
            or "perf" or "test" or "chore" or "build" or "ci" or "revert";
    }

    private static async Task<string?> RunGitAsync(string args)
    {
        var (exitCode, stdout, _) = await GitRunner.RunAsync(args);
        return exitCode == 0 ? stdout.Trim() : null;
    }

    // ── Architect 双模型模式 ──

    /// <summary>
    /// 用大模型生成执行计划（无工具调用，纯思考输出）。
    /// 返回 null 表示失败/取消。
    /// </summary>
    private async Task<string?> GenerateArchitectPlanAsync(
        Action<string>? onToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var architectPrompt = SystemPrompt.GenerateArchitectPrompt();
            var msgs = new List<JsonObject>
            {
                new() { ["role"] = "system", ["content"] = architectPrompt },
            };
            // 克隆历史消息（不含工具往返）给 Architect 做上下文
            foreach (var m in Messages)
            {
                var role = (string?)m["role"];
                if (role is "tool" or "assistant_tool_calls") continue;
                msgs.Add(JsonNode.Parse(m.ToJsonString())!.AsObject());
            }

            // 切换到大模型，不带工具
            var savedOverride = LlmClient.ModelOverride;
            LlmClient.ModelOverride = LlmClient.Model; // 大模型

            var resp = await LlmClient.ChatAsync(
                messages: msgs,
                tools: null, // 不给工具，纯分析
                onToken: onToken,
                cancellationToken: cancellationToken);

            LlmClient.ModelOverride = savedOverride;

            return string.IsNullOrWhiteSpace(resp.Content) ? null : resp.Content.Trim();
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            DebugLog.Log("architect", $"计划生成异常: {ex.Message}");
            return null;
        }
    }

}

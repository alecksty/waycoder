using WayCoder.Tools;
using WayCoder.UI.Shared;
using WayCoder.UI.Tui;
using WayCoder.UI.TUI.Base;
using WayCoder.UI.Tui.Screens;

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
public partial class Agent
{

    /// <summary>工具执行后自动 git commit。用小模型生成提交信息。</summary>
    private async Task AutoCommitAsync(bool fallbackToGitStatus = true)
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

            // 如果没有追踪到文件修改，检查 git status 兜底（优雅暂停时关闭，避免卷入无关改动）
            if (modifiedFiles.Length == 0)
            {
                if (!fallbackToGitStatus) return;
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

            // 提交（finally 清理临时文件：git 抛异常时也不残留 %TEMP%）
            var msgFile = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(msgFile, commitMsg);
                await RunGitAsync($"commit -F {EscArg(msgFile)}");
            }
            finally { try { File.Delete(msgFile); } catch { } }

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
            return await WithModelOverrideAsync(LlmClient, LlmClient.SmallModel, async () =>
            {
                var msgs = new List<JNode>
                {
                    JNode.Object().Set("role", "system").Set("content", "You are a git commit message generator. Output a single line, English, conventional-commit style with a valid prefix (feat/fix/docs/style/refactor/perf/test/chore/build/ci/revert), <70 chars. Do NOT include any quotes, backticks, or extra text."),
                    JNode.Object().Set("role", "user").Set("content", "Modified files: " + fileList + "\n\nCommit message:"),
                };
                var result = await LlmClient.ChatAsync(msgs, tools: null);
                var msg = CleanCommitMsg(result?.Content ?? "");

                // 质量校验：不合格则重试一次
                if (!IsValidCommitMsg(msg))
                {
                    msgs[0].Set("content", "Your previous output was invalid. Strict rules: exactly one line, English, conventional-commit prefix (feat/fix/docs/style/refactor/perf/test/chore/build/ci/revert), no quotes/backticks/code fences, <70 chars. Output only the message.");
                    result = await LlmClient.ChatAsync(msgs, tools: null);
                    msg = CleanCommitMsg(result?.Content ?? "");
                }

                // 重试后仍不合格：回退到安全默认信息
                if (!IsValidCommitMsg(msg))
                {
                    var firstFile = fileList.Split(',')[0].Trim();
                    msg = "chore: update " + firstFile;
                }
                return msg.Length > 72 ? ContextManager.TruncateByRunes(msg, 72) : msg;
            }) ?? "";
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

    // ── 计划审批门（Plan 模式）──

    /// <summary>
    /// 是否应弹出计划审批（纯逻辑，便于自测）：
    /// 仅 Plan 模式 + 本轮有文本产出（计划）且无工具调用时触发。
    /// </summary>
    internal static bool ShouldPromptPlanApproval(WorkMode mode, int contentLength)
        => mode == WorkMode.Plan && contentLength > 0;

    /// <summary>
    /// 弹出计划审批确认框（Plan 模式）。返回 true 表示批准执行。
    /// 非 TUI 环境（一次性模式 / 管道 / 测试）默认自动批准，避免阻塞。
    /// </summary>
    private bool PromptPlanApproval(string plan)
    {
        var activeScreen = TuiManager.Instance.ActiveScreen as ChatScreen;
        if (activeScreen == null)
            return true; // 非交互环境自动批准

        var summary = plan.Length > 160 ? ContextManager.TruncateByRunes(plan, 160) + "…" : plan;
        return activeScreen.ShowPlanApproval(summary, plan);
    }

    // ── Architect 双模型模式 ──

    /// <summary>
    /// 临时切换 <see cref="LLM.ModelOverride"/> 执行操作，finally 保证恢复。
    /// 否则 action 内抛出异常会把 ModelOverride 永久污染成大/小模型，导致后续请求静默降级。
    /// </summary>
    internal static async Task<T?> WithModelOverrideAsync<T>(LLM llm, string? overrideModel, Func<Task<T>> action)
    {
        var saved = llm.ModelOverride;
        llm.ModelOverride = overrideModel;
        try { return await action(); }
        finally { llm.ModelOverride = saved; }
    }

    /// <summary>无返回值版本（压缩/摘要等副作用任务）。</summary>
    internal static Task WithModelOverrideAsync(LLM llm, string? overrideModel, Func<Task> action)
        => WithModelOverrideAsync<object?>(llm, overrideModel, async () => { await action(); return null; });

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
            var msgs = new List<JNode>
            {
                JNode.Object().Set("role", "system").Set("content", architectPrompt),
            };
            // 克隆历史消息（不含工具往返）给 Architect 做上下文
            foreach (var m in Messages)
            {
                var role = m["role"]?.AsString();
                if (role is "tool" or "assistant_tool_calls") continue;
                msgs.Add(Json.Parse(m.ToJson())!);
            }

            // 切换到大模型，不带工具；finally 恢复，异常不污染后续请求
            var resp = await WithModelOverrideAsync(LlmClient, LlmClient.Model, () =>
                LlmClient.ChatAsync(
                    messages: msgs,
                    tools: null, // 不给工具，纯分析
                    onToken: onToken,
                    cancellationToken: cancellationToken));

            return resp == null || string.IsNullOrWhiteSpace(resp.Content) ? null : resp.Content.Trim();
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            DebugLog.Log("architect", $"计划生成异常: {ex.Message}");
            return null;
        }
    }
}

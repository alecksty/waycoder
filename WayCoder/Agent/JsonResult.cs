namespace WayCoder;

/// <summary>
/// --json 输出模式的结果构建器（IDE / 脚本桥接）。
/// 一次性 `-p` 模式加 `--json` 后，stdout 只输出一个结构化 JSON 对象，
/// 供 VS Code 扩展、CI 脚本、外部工具直接解析，无需剥离 ANSI 动画。
///
/// 纯函数：输入原始值，输出 JNode，便于自测（不依赖 _llm/_agent 静态态）。
/// </summary>
public static class JsonResult
{
    /// <summary>结果 schema 版本（供 IDE 判断兼容性）。</summary>
    public const string SchemaVersion = "1.0";

    /// <summary>
    /// 构建一次性任务的结果 JSON。
    /// </summary>
    /// <param name="success">任务是否成功完成（false 表示中断/超时/异常）。</param>
    /// <param name="answer">Agent 最终回答文本。</param>
    /// <param name="error">失败原因（成功时为 null）。</param>
    /// <param name="model">实际使用的模型 ID。</param>
    /// <param name="promptTokens">本次任务输入 token 数。</param>
    /// <param name="completionTokens">本次任务输出 token 数。</param>
    /// <param name="costUsd">本次任务花费估算（美元，模型无定价时 null）。</param>
    /// <param name="durationMs">总耗时（毫秒）。</param>
    /// <param name="changedFiles">本次会话修改过的文件路径列表。</param>
    public static JNode Build(
        bool success,
        string answer,
        string? error,
        string? model,
        int promptTokens,
        int completionTokens,
        double? costUsd,
        long durationMs,
        IEnumerable<string>? changedFiles)
    {
        var files = JNode.Array();
        foreach (var f in changedFiles ?? [])
            files.Add(JNode.From(f));

        return JNode.Object()
            .Set("schema", SchemaVersion)
            .Set("success", success)
            .Set("answer", answer ?? "")
            .Set("error", error)
            .Set("model", model)
            .Set("usage", JNode.Object()
                .Set("prompt_tokens", promptTokens)
                .Set("completion_tokens", completionTokens)
                .Set("total_tokens", promptTokens + completionTokens))
            .Set("cost_usd", costUsd == null ? JNode.Null() : JNode.From(costUsd.Value))
            .Set("duration_ms", durationMs)
            .Set("changed_files", files);
    }
}

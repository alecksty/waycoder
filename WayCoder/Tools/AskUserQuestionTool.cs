using WayCoder.UI.Shared;
using WayCoder.UI.Tui;

namespace WayCoder.Tools;

/// <summary>
/// 用户交互工具 —— 允许 LLM 向用户提问（单选、多选、文本输入、确认）。
///
/// 触发条件：当 LLM 需要用户在多个方案间做选择、确认操作、或提供额外信息时调用。
/// 阻塞执行直到用户响应。
/// </summary>
public class AskUserQuestionTool : ITool
{
    public string Name => "ask_user_question";

    public string Description =>
        "向用户提出一个问题或多道问题，支持单选、多选和文本输入。当需要用户做出选择、确认或提供输入时使用此工具。可一次提出1-4个问题，每个问题按顺序依次显示。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("questions", JNode.Object()
                .Set("type", "array")
                .Set("description", "要问用户的问题列表（1-4个问题，依次显示）")
                .Set("items", JNode.Object()
                    .Set("type", "object")
                    .Set("properties", JNode.Object()
                        .Set("question", JNode.Object()
                            .Set("type", "string")
                            .Set("description", "完整的问题文本（向用户展示的问题内容）"))
                        .Set("header", JNode.Object()
                            .Set("type", "string")
                            .Set("description", "问题简短标签，用于在答案中标识此问题（最多12字符）。若不提供则用 question 前12字符。"))
                        .Set("options", JNode.Object()
                            .Set("type", "array")
                            .Set("description", "可选选项列表。若不提供则为自由文本输入，若提供则为选择列表。")
                            .Set("items", JNode.Object()
                                .Set("type", "object")
                                .Set("properties", JNode.Object()
                                    .Set("label", JNode.Object()
                                        .Set("type", "string")
                                        .Set("description", "选项显示文本（简短，1-5词）"))
                                    .Set("description", JNode.Object()
                                        .Set("type", "string")
                                        .Set("description", "选项说明（解释此选项的含义和影响，可选）")))
                                .Set("required", JNode.Array().Add("label"))))
                        .Set("multiSelect", JNode.Object()
                            .Set("type", "boolean")
                            .Set("description", "是否允许多选。默认 false（单选）。")))
                    .Set("required", JNode.Array().Add("question")))))
        .Set("required", JNode.Array().Add("questions"));

    /// <summary>
    /// 解析后的单个问题
    /// </summary>
    private class ParsedQuestion
    {
        public string Question { get; init; } = "";
        public string Header { get; init; } = "";
        public List<(string Label, string Description)> Options { get; init; } = new();
        public bool MultiSelect { get; init; }
    }

    public async Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        try
        {
            // ── 1. 解析 questions 数组 ──
            var questions = ParseQuestions(arguments);
            if (questions.Count == 0)
                return "错误：questions 数组为空，至少需要一个问题";

            // ── 2. 依次展示每个问题 ──
            var answers = JNode.Object();
            foreach (var q in questions)
            {
                object? answer;
                // YOLO（畅通）：无任何问答阻止，自动选第一个选项/文本留空，不弹框（Web/TUI/GUI 三端统一）
                if (PermissionManager.CurrentMode == PermissionManager.Mode.Yolo)
                {
                    if (q.Options.Count > 0)
                        answer = q.MultiSelect ? JNode.Array().Add(q.Options[0].Label) : q.Options[0].Label;
                    else
                        answer = "";
                }
                else
                {
                    try
                    {
                        answer = await ShowQuestionAsync(q);
                    }
                    catch (OperationCanceledException)
                    {
                        // 用户取消 → 剩余问题跳过
                        answers[q.Header] = JNode.From("已取消");
                        break;
                    }
                }

                if (answer == null)
                {
                    // 用户取消当前问题
                    answers[q.Header] = JNode.From((string?)null);
                    break;
                }

                // 存入答案
                if (answer is JNode arr)
                    answers[q.Header] = arr;
                else if (answer is string s)
                    answers[q.Header] = JNode.From(s);
                else
                    answers[q.Header] = JNode.From(answer?.ToString());
            }

            // ── 3. 返回 JSON 结果 ──
            return answers.ToJson();
        }
        catch (Exception ex)
        {
            return $"ask_user_question 执行出错：{ex.Message}";
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  解析
    // ═══════════════════════════════════════════════════════════════

    private static List<ParsedQuestion> ParseQuestions(Dictionary<string, object?> arguments)
    {
        var result = new List<ParsedQuestion>();
        if (!arguments.TryGetValue("questions", out var questionsObj))
            return result;

        // questions 是 JsonArray（来自 JSON 反序列化）
        if (questionsObj is JNode arr)
        {
            foreach (var item in arr.Items)
            {
                if (item.Kind != JKind.Object) continue;
                var q = ParseOneQuestion(item);
                if (q != null) result.Add(q);
            }
        }
        // 也可能是 List<object?>（来自测试直接构造）
        else if (questionsObj is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is Dictionary<string, object?> dict)
                {
                    var q = ParseOneQuestion(dict);
                    if (q != null) result.Add(q);
                }
            }
        }

        return result;
    }

    private static ParsedQuestion? ParseOneQuestion(JNode obj)
    {
        var question = obj["question"]?.AsString() ?? "";
        if (string.IsNullOrWhiteSpace(question)) return null;

        var header = obj["header"]?.AsString() ?? "";
        if (string.IsNullOrWhiteSpace(header))
            header = question.Length <= 12 ? question : ContextManager.TruncateByRunes(question, 12);

        // JSON 布尔值 multiSelect=true 时 AsString() 返回 null（JKind.Bool ≠ JKind.String），
        // 必须优先走 AsBool()，否则多选永远解析为 false。兜底兼容字符串 "true"（对齐 MultiEditTool）。
        var multiSelect = obj["multiSelect"] is { Kind: JKind.Bool } b
            ? b.AsBool()
            : obj["multiSelect"]?.AsString()?.ToLowerInvariant() == "true";

        var options = new List<(string Label, string Description)>();
        if (obj["options"] is { Kind: JKind.Array } optArr)
        {
            foreach (var opt in optArr.Items)
            {
                if (opt.Kind != JKind.Object) continue;
                var label = opt["label"]?.AsString() ?? "";
                var desc = opt["description"]?.AsString() ?? "";
                if (!string.IsNullOrWhiteSpace(label))
                    options.Add((label, desc));
            }
        }

        return new ParsedQuestion
        {
            Question = question,
            Header = header,
            Options = options,
            MultiSelect = multiSelect,
        };
    }

    private static ParsedQuestion? ParseOneQuestion(Dictionary<string, object?> dict)
    {
        var question = dict.GetValueOrDefault("question")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(question)) return null;

        var header = dict.GetValueOrDefault("header")?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(header))
            header = question.Length <= 12 ? question : ContextManager.TruncateByRunes(question, 12);

        var multiSelect = dict.GetValueOrDefault("multiSelect") is true;

        var options = new List<(string Label, string Description)>();
        if (dict.GetValueOrDefault("options") is System.Collections.IEnumerable opts)
        {
            foreach (var opt in opts)
            {
                if (opt is Dictionary<string, object?> optDict)
                {
                    var label = optDict.GetValueOrDefault("label")?.ToString() ?? "";
                    var desc = optDict.GetValueOrDefault("description")?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(label))
                        options.Add((label, desc));
                }
            }
        }

        return new ParsedQuestion
        {
            Question = question,
            Header = header,
            Options = options,
            MultiSelect = multiSelect,
        };
    }

    // ═══════════════════════════════════════════════════════════════
    //  对话框展示（阻塞等待用户响应）
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 展示单个问题并等待回答。返回：
    /// - string: 文本输入或单选结果
    /// - JsonArray: 多选结果（选中标签列表）
    /// - null: 用户取消
    /// </summary>
    private static async Task<object?> ShowQuestionAsync(ParsedQuestion q)
    {
        // 有选项 → 选择对话框（标题=header，消息=question 正文，选项=按钮）
        if (q.Options.Count > 0)
        {
            if (q.MultiSelect)
                return await ShowMultiSelectAsync(q.Question, q.Header, q.Options);
            else
                return await ShowSingleSelectAsync(q.Question, q.Header, q.Options);
        }

        // 无选项 → 文本输入
        return await ShowTextInputAsync(q.Question, q.Header);
    }

    /// <summary>单选对话框 —— 统一走 UxHelper.Ask（标题+消息+按钮），返回选中项的 label。</summary>
    private static async Task<string?> ShowSingleSelectAsync(
        string question, string header,
        List<(string Label, string Description)> options)
    {
        var timeoutMs = Config.Instance.AskUserTimeoutSec * 1000;
        if (UxHelper.WebInteraction != null)
        {
            var displayItems = BuildDisplayItems(options);
            var chosen = await UxHelper.WebInteraction.SelectAsync(question, displayItems, timeoutMs);
            return chosen == null ? null : ResolveLabel(chosen, displayItems, options);
        }

        var labels = options.Select(o => o.Label).ToList();
        var picked = UxHelper.Ask(header, question, labels, multiSelect: false, timeoutMs: timeoutMs);
        if (picked == null || picked.Count == 0) return null;
        return picked[0] >= 0 && picked[0] < options.Count ? options[picked[0]].Label : null;
    }

    /// <summary>多选对话框 —— 统一走 UxHelper.Ask（标题+消息+按钮），返回选中项的 label 列表。</summary>
    private static async Task<JNode?> ShowMultiSelectAsync(
        string question, string header,
        List<(string Label, string Description)> options)
    {
        var timeoutMs = Config.Instance.AskUserTimeoutSec * 1000;
        if (UxHelper.WebInteraction != null)
        {
            var displayItems = BuildDisplayItems(options);
            var chosen = await UxHelper.WebInteraction.MultiSelectAsync(question, displayItems, timeoutMs);
            if (chosen == null) return null; // 用户取消
            var arr = JNode.Array();
            foreach (var c in chosen)
                arr.Add(ResolveLabel(c, displayItems, options));
            return arr;
        }

        var labels = options.Select(o => o.Label).ToList();
        var picked = UxHelper.Ask(header, question, labels, multiSelect: true, timeoutMs: timeoutMs);
        if (picked == null) return null; // 用户取消
        var result = JNode.Array();
        foreach (var idx in picked)
            if (idx >= 0 && idx < options.Count)
                result.Add(options[idx].Label);
        return result;
    }

    /// <summary>构建含描述的显示文本（label — description），仅 Web 端使用。</summary>
    private static List<string> BuildDisplayItems(List<(string Label, string Description)> options)
        => options.Select(o =>
            string.IsNullOrEmpty(o.Description)
                ? o.Label
                : $"{o.Label}  —  {o.Description}"
        ).ToList();

    /// <summary>文本输入对话框 —— 统一走 UxHelper（TUI 弹框 / 非 TUI 行内），空输入视为取消。</summary>
    private static async Task<string?> ShowTextInputAsync(string question, string header)
    {
        var timeoutMs = Config.Instance.AskUserTimeoutSec * 1000;
        string? val;
        if (UxHelper.WebInteraction != null)
            val = await UxHelper.WebInteraction.AskAsync(question, null, timeoutMs);
        else
            val = UxHelper.Ask(question, timeoutMs: timeoutMs);
        return string.IsNullOrWhiteSpace(val) ? null : val;
    }

    /// <summary>把用户选中的显示文本解析回选项 label（displayItems 形如「label — desc」）。查不到时原样返回。</summary>
    internal static string ResolveLabel(
        string chosen,
        List<string> displayItems,
        List<(string Label, string Description)> options)
    {
        int idx = displayItems.IndexOf(chosen);
        return idx >= 0 && idx < options.Count ? options[idx].Label : chosen;
    }
}

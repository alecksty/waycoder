using WayCoder.Terminal;
using WayCoder.UI;
using WayCoder.UI.TuiControls;
using WayCoder.UI.TuiScreens;

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

    public JsonObject Parameters => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["questions"] = new JsonObject
            {
                ["type"] = "array",
                ["description"] = "要问用户的问题列表（1-4个问题，依次显示）",
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["question"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "完整的问题文本（向用户展示的问题内容）",
                        },
                        ["header"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["description"] = "问题简短标签，用于在答案中标识此问题（最多12字符）。若不提供则用 question 前12字符。",
                        },
                        ["options"] = new JsonObject
                        {
                            ["type"] = "array",
                            ["description"] = "可选选项列表。若不提供则为自由文本输入，若提供则为选择列表。",
                            ["items"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["properties"] = new JsonObject
                                {
                                    ["label"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                        ["description"] = "选项显示文本（简短，1-5词）",
                                    },
                                    ["description"] = new JsonObject
                                    {
                                        ["type"] = "string",
                                        ["description"] = "选项说明（解释此选项的含义和影响，可选）",
                                    },
                                },
                                ["required"] = new JsonArray("label"),
                            },
                        },
                        ["multiSelect"] = new JsonObject
                        {
                            ["type"] = "boolean",
                            ["description"] = "是否允许多选。默认 false（单选）。",
                        },
                    },
                    ["required"] = new JsonArray("question"),
                },
            },
        },
        ["required"] = new JsonArray("questions"),
    };

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

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        try
        {
            // ── 1. 解析 questions 数组 ──
            var questions = ParseQuestions(arguments);
            if (questions.Count == 0)
                return Task.FromResult("错误：questions 数组为空，至少需要一个问题");

            // ── 2. 依次展示每个问题 ──
            var answers = new JsonObject();
            foreach (var q in questions)
            {
                object? answer;
                try
                {
                    answer = ShowQuestion(q);
                }
                catch (OperationCanceledException)
                {
                    // 用户取消 → 剩余问题跳过
                    answers[q.Header] = JsonValue.Create("已取消");
                    break;
                }

                if (answer == null)
                {
                    // 用户取消当前问题
                    answers[q.Header] = JsonValue.Create((string?)null);
                    break;
                }

                // 存入答案
                if (answer is JsonArray arr)
                    answers[q.Header] = arr;
                else if (answer is string s)
                    answers[q.Header] = JsonValue.Create(s);
                else
                    answers[q.Header] = JsonValue.Create(answer?.ToString());
            }

            // ── 3. 返回 JSON 结果 ──
            var result = new Dictionary<string, object?>
            {
                ["answers"] = JsonNode.Parse(answers.ToJsonString()),
            };
            return Task.FromResult(answers.ToJsonString());
        }
        catch (Exception ex)
        {
            return Task.FromResult($"ask_user_question 执行出错：{ex.Message}");
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
        if (questionsObj is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is not JsonObject obj) continue;
                var q = ParseOneQuestion(obj);
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

    private static ParsedQuestion? ParseOneQuestion(JsonObject obj)
    {
        var question = obj["question"]?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(question)) return null;

        var header = obj["header"]?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(header))
            header = question.Length <= 12 ? question : question[..12];

        var multiSelect = obj["multiSelect"]?.ToString() == "true";

        var options = new List<(string Label, string Description)>();
        if (obj["options"] is JsonArray optArr)
        {
            foreach (var opt in optArr)
            {
                if (opt is not JsonObject optObj) continue;
                var label = optObj["label"]?.ToString() ?? "";
                var desc = optObj["description"]?.ToString() ?? "";
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
            header = question.Length <= 12 ? question : question[..12];

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
    private static object? ShowQuestion(ParsedQuestion q)
    {
        // 有选项 → 选择对话框
        if (q.Options.Count > 0)
        {
            // 构建显示文本（含描述）
            var displayItems = q.Options.Select(o =>
                string.IsNullOrEmpty(o.Description)
                    ? o.Label
                    : $"{o.Label}  —  {o.Description}"
            ).ToList();

            if (q.MultiSelect)
                return ShowMultiSelect(q.Question, q.Header, displayItems, q.Options);
            else
                return ShowSingleSelect(q.Question, q.Header, displayItems, q.Options);
        }

        // 无选项 → 文本输入
        return ShowTextInput(q.Question, q.Header);
    }

    /// <summary>单选对话框</summary>
    private static string? ShowSingleSelect(
        string question, string header,
        List<string> displayItems,
        List<(string Label, string Description)> options)
    {
        if (!UxHelper.IsTuiMode)
            return UxHelper.Select(question, displayItems);

        string? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var win = TuiDialog.Select(question, displayItems,
                onSelect: idx =>
                {
                    result = idx >= 0 && idx < options.Count ? options[idx].Label : null;
                    evt.Set();
                },
                onCancel: () => { result = null; evt.Set(); });
            ShowAndWait(win, evt);
        }
        catch { evt.Set(); }
        return result;
    }

    /// <summary>多选对话框</summary>
    private static JsonArray? ShowMultiSelect(
        string question, string header,
        List<string> displayItems,
        List<(string Label, string Description)> options)
    {
        if (!UxHelper.IsTuiMode)
        {
            // 非 TUI 模式回退：逐个确认
            var selected = new JsonArray();
            Console.WriteLine($"\x1b[1m{question}\x1b[0m (多选，输入 y/n 逐个确认)");
            foreach (var (label, desc) in options)
            {
                var descSuffix = string.IsNullOrEmpty(desc) ? "" : $" — {desc}";
                Console.Write($"  [{label}]{descSuffix} (y/n): ");
                var key = Console.ReadKey(intercept: false);
                Console.WriteLine();
                if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                {
                    JsonNode? node = JsonValue.Create(label);
                    selected.Add(node);
                }
            }
            return selected;
        }

        JsonArray? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var win = TuiDialog.MultiSelect(question, displayItems,
                onConfirm: indices =>
                {
                    var arr = new JsonArray();
                    foreach (var i in indices)
                    {
                        if (i >= 0 && i < options.Count)
                        {
                            JsonNode? node = JsonValue.Create(options[i].Label);
                            arr.Add(node);
                        }
                    }
                    result = arr;
                    evt.Set();
                },
                onCancel: () => { result = null; evt.Set(); });
            ShowAndWait(win, evt);
        }
        catch { evt.Set(); }
        return result;
    }

    /// <summary>文本输入对话框</summary>
    private static string? ShowTextInput(string question, string header)
    {
        if (!UxHelper.IsTuiMode)
        {
            Console.Write($"\x1b[1m{question}\x1b[0m ");
            var input = Console.ReadLine() ?? "";
            return string.IsNullOrWhiteSpace(input) ? null : input;
        }

        string? result = null;
        using var evt = new ManualResetEventSlim(false);
        try
        {
            var win = TuiDialog.Input(header, question, "",
                onConfirm: val =>
                {
                    result = string.IsNullOrWhiteSpace(val) ? null : val;
                    evt.Set();
                },
                onCancel: () => { result = null; evt.Set(); });
            ShowAndWait(win, evt);
        }
        catch { evt.Set(); }
        return result;
    }

    /// <summary>
    /// 显示窗口并阻塞等待用户响应（渲染 + 输入事件循环）。
    /// </summary>
    private static void ShowAndWait(TuiWindow win, ManualResetEventSlim evt)
    {
        var screen = TuiManager.Instance?.ActiveScreen;
        if (screen == null)
        {
            evt.Set();
            return;
        }

        screen.ShowWindow(win);
        // 用户交互等待（比常规确认框更长）
        UxHelper.RenderWait(screen, evt, timeoutMs: Config.Instance.AskUserTimeoutSec * 1000);
    }
}

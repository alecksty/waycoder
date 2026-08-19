// AgentMinimal —— 一个最小可运行的 Coding Agent 核心示例
// 用途：把「Agent Loop / Tool Calling / 上下文累积 / 轮次上限」这些概念
//       用最少的 C# 代码落地，作为 ai-coding-agent 资料的可执行附录。
// 设计约束：无反射、无第三方包、纯 BCL，AOT 安全。

namespace AgentMinimal;

// ---------------------------------------------------------------------------
// 1. 工具抽象：每个工具是「名称 + 描述 + 参数 JSON + 执行」。
//    模型通过 tool call 选择工具，Agent 负责执行并把结果回填。
// ---------------------------------------------------------------------------
public abstract class Tool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Execute(string argsJson);
}

// 列出当前目录文件
public sealed class ListDirTool : Tool
{
    public override string Name => "list_dir";
    public override string Description => "列出指定目录下的文件和子目录";
    public override string Execute(string argsJson)
    {
        var path = argsJson.Trim().Trim('"');
        if (string.IsNullOrEmpty(path)) path = Directory.GetCurrentDirectory();
        if (!Directory.Exists(path)) return $"错误：目录不存在 {path}";
        return string.Join("\n", Directory.EnumerateFileSystemEntries(path).Take(50));
    }
}

// 读取文本文件
public sealed class ReadFileTool : Tool
{
    public override string Name => "read_file";
    public override string Description => "读取文本文件内容";
    public override string Execute(string argsJson)
    {
        var path = argsJson.Trim().Trim('"');
        if (!File.Exists(path)) return $"错误：文件不存在 {path}";
        return File.ReadAllText(path);
    }
}

// ---------------------------------------------------------------------------
// 2. 模型抽象：真正的 agent 在这里接 LLM。本例用一个「规则 Mock 模型」，
//    模拟「返回 tool call 或返回最终答案」两种决策，演示完整流程。
// ---------------------------------------------------------------------------
public interface IModel
{
    // 返回 null 表示本轮是最终回答；否则返回要调用的工具名和参数。
    (string ToolName, string ArgsJson)? Decide(IReadOnlyList<string> messages);
}

public sealed class MockModel : IModel
{
    public (string, string)? Decide(IReadOnlyList<string> messages)
    {
        var last = messages.Count > 0 ? messages[^1] : "";
        // 极简规则：根据用户指令决定调用哪个工具
        if (last.Contains("列出", StringComparison.Ordinal) ||
            last.Contains("list", StringComparison.OrdinalIgnoreCase))
        {
            return ("list_dir", ".");
        }
        if (last.Contains("读", StringComparison.Ordinal) ||
            last.Contains("read", StringComparison.OrdinalIgnoreCase))
        {
            return ("read_file", "README.md");
        }
        return null; // 否则直接回答
    }
}

// ---------------------------------------------------------------------------
// 3. Agent 主循环：这是整个 coding agent 的心脏。
//    用户输入 → 模型决策 → [工具调用 → 执行 → 回填] → 循环 → 最终回答
// ---------------------------------------------------------------------------
public sealed class Agent
{
    private readonly IModel _model;
    private readonly Dictionary<string, Tool> _tools;
    private readonly List<string> _messages = new();
    public int MaxRounds { get; init; } = 8; // 轮次上限，防止无限循环

    public Agent(IModel model, IEnumerable<Tool> tools)
    {
        _model = model;
        _tools = tools.ToDictionary(t => t.Name);
    }

    public string Run(string userInput)
    {
        _messages.Add($"用户：{userInput}");

        for (int round = 1; round <= MaxRounds; round++)
        {
            var decision = _model.Decide(_messages);
            if (decision is null)
            {
                var answer = $"（第 {round} 轮，模型给出最终回答）完成。";
                _messages.Add(answer);
                return answer;
            }

            var (toolName, argsJson) = decision.Value;
            if (!_tools.TryGetValue(toolName, out var tool))
            {
                _messages.Add($"错误：未知工具 {toolName}");
                continue;
            }

            // 执行工具，把观察结果作为新消息回填给模型
            var observation = tool.Execute(argsJson);
            _messages.Add($"工具 {toolName}({argsJson}) 返回：\n{observation}");

            // 简单演示：工具结果直接作为最终输出的一部分
            if (round == MaxRounds) break;
        }

        // 组装本轮可见的工具观察结果
        var toolLines = _messages
            .Where(m => m.StartsWith("工具 ", StringComparison.Ordinal))
            .Select(m => m.Trim());
        return string.Join("\n\n", toolLines);
    }
}

// ---------------------------------------------------------------------------
// 4. 演示入口
// ---------------------------------------------------------------------------
public static class Program
{
    public static void Main()
    {
        var agent = new Agent(
            new MockModel(),
            new Tool[] { new ListDirTool(), new ReadFileTool() })
        {
            MaxRounds = 8,
        };

        Console.WriteLine("=== AgentMinimal：最小 Coding Agent 核心 ===\n");

        Console.WriteLine("--- 演示 1：让 agent 列出目录 ---");
        Console.WriteLine(agent.Run("请列出当前目录"));

        Console.WriteLine("\n--- 演示 2：让 agent 读文件 ---");
        Console.WriteLine(agent.Run("读一下 README.md"));

        Console.WriteLine("\n--- 演示 3：普通对话（不调用工具）---");
        Console.WriteLine(agent.Run("你好"));
    }
}

using WayCoder.Maui.Services;

namespace WayCoder.Tools;

/// <summary>
/// <c>vml</c> 工具 —— 在**进程内**汇编并运行一段 VML 程序，把它的控制台输出交回来。
///
/// 用途（用户提的）：让 AI **验证它自己写的东西能不能跑** —— 写完一段 VML 汇编就地跑一遍，
/// 而不是把代码甩给用户去别处试。
///
/// 两条设计约束，都不是随手写的：
///
/// 1. **`ExecutionMode = Exclusive`** —— VML 的 <c>DeviceManager</c> 是**进程级单例**，
///    两次运行并发会串 MMIO 地址（<c>MauiVml</c> 每次都 <c>Reset()</c> 也正是为此）。
///    并发跑两个 VML 程序 = 两个 Reset 互相踩。所以必须独占。
/// 2. **不按平台条件编译** —— 它走的是进程内调用，**iOS 上同样可用**
///    （`fork/exec` 那道坎根本不是这条路的一部分）。这与 <c>BashTool</c>（只有 Android）不同。
/// </summary>
public class VmlTool : ITool
{
    /// <summary>返回给模型的最大字符数 —— 一段死循环程序的输出能轻易撑爆上下文。</summary>
    private const int MaxOutputChars = 20_000;

    public string Name => "vml";

    /// <summary>VML 程序输出走等宽纯文本渲染（与 bash 同类），别当 markdown 解析。</summary>
    public bool RawOutput => true;

    public ToolExecutionMode ExecutionMode => ToolExecutionMode.Exclusive;

    public string Description =>
        "汇编并运行一段 VML 程序，返回它的输出。VML 是本机内置的虚拟机汇编语言" +
        "（VMLToolchain 工具链：22 种高级语言 → VML 汇编 → 虚拟机执行）。" +
        "用来**验证一段 VML 汇编能否跑通**、看它的实际输出。" +
        "参数二选一：source 直接给源码，或 file_path 给一个 .vml 文件路径。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("source", JNode.Object()
                .Set("type", "string")
                .Set("description", "VML 汇编源码"))
            .Set("file_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "要运行的 .vml 文件路径（与 source 二选一）"))
            .Set("timeout", JNode.Object()
                .Set("type", "integer")
                .Set("description", "超时秒数，默认 10，范围 1~60")));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var source = GetString(arguments, "source");
        var filePath = GetString(arguments, "file_path");

        if (string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult("⚠️ 需要 `source`（VML 源码）或 `file_path`（.vml 文件路径）二者之一。");

        // 读文件放在进后台线程之前：CwdContext 是 AsyncLocal，在别的线程上解析会拿到错的 cwd
        string code;
        if (!string.IsNullOrWhiteSpace(source))
        {
            code = source!;
        }
        else
        {
            var full = CwdContext.Resolve(filePath!);
            if (!File.Exists(full)) return Task.FromResult($"⚠️ 找不到文件：{full}");
            try { code = File.ReadAllText(full); }
            catch (Exception ex) { return Task.FromResult($"⚠️ 读文件失败：{ex.Message}"); }
        }

        var timeout = Math.Clamp(GetInt(arguments, "timeout", 10), 1, 60);

        // VmRuntime.Run 是**同步阻塞**的，必须丢到后台线程，否则卡住整个 Agent 循环
        return Task.Run(() =>
        {
            try
            {
                var output = MauiVml.RunAssembly(code, timeout);
                if (string.IsNullOrEmpty(output)) return "（程序正常结束，没有输出）";

                return output.Length <= MaxOutputChars
                    ? output
                    : output[..MaxOutputChars]
                      + $"\n\n⚠️ 输出过长已截断（共 {output.Length} 字符，只回传前 {MaxOutputChars}）";
            }
            catch (Exception ex)
            {
                // 汇编报错、VM 超时、被链接器裁掉的路径…都在这里兜住，别让 Agent 循环炸
                return $"⚠️ VML 执行失败：{ex.GetType().Name}: {ex.Message}";
            }
        });
    }

    /// <summary>从 arguments 里取字符串（工具参数是松散字典，类型不保证）。</summary>
    private static string? GetString(Dictionary<string, object?> args, string key)
        => args.TryGetValue(key, out var v) && v is not null ? v.ToString() : null;

    private static int GetInt(Dictionary<string, object?> args, string key, int fallback)
        => args.TryGetValue(key, out var v) && v is not null && int.TryParse(v.ToString(), out var n)
            ? n : fallback;
}

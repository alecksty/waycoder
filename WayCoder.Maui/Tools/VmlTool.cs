using WayCoder.Infra;
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
        "参数二选一：source 直接给源码，或 file_path 给一个文件路径" +
        "（按扩展名派发：.vml 汇编、.vmb 装载字节码、其余 22 种语言编译）。";

    public JNode Parameters => JNode.Object()
        .Set("type", "object")
        .Set("properties", JNode.Object()
            .Set("source", JNode.Object()
                .Set("type", "string")
                .Set("description", "VML 汇编源码"))
            .Set("file_path", JNode.Object()
                .Set("type", "string")
                .Set("description", "要运行的 .vml / .vmb 文件路径，或 .c/.py/.rs 等源文件（与 source 二选一）"))
            .Set("timeout", JNode.Object()
                .Set("type", "integer")
                .Set("description", "超时秒数，默认 10，范围 1~60"))
            .Set("stdin", JNode.Object()
                .Set("type", "string")
                .Set("description", "预置给程序的标准输入（多行用 \\n 分隔）。程序读 stdin 时按行喂；"
                                  + "读完之后再读会拿到空行。不填则程序读到空输入。")));

    public Task<string> ExecuteAsync(Dictionary<string, object?> arguments)
    {
        var source = GetString(arguments, "source");
        var filePath = GetString(arguments, "file_path");

        if (string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult("⚠️ 需要 `source`（VML 源码）或 `file_path`（.vml 文件路径）二者之一。");

        // 路径解析放在进后台线程之前，解析完把**绝对路径**交给 MauiVml.Run（那边不再碰 CwdContext）。
        // 这是「进后台前定型」的写法，而不是被迫的绕行：CwdContext 现在存盒子、能跨线程回传，
        // 但「本工具的语义 = 对本轮 cwd 取一次快照」仍然更清晰，也避免 VML 跑起来之后 cwd 被改。
        var resolved = string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(filePath)
            ? CwdContext.Resolve(filePath!)
            : null;

        var timeout = Math.Clamp(GetInt(arguments, "timeout", 10), 1, 60);

        // 汇编 / 编译的**派发不在这个工具里** —— 它是流水线的属性，不是工具的属性，
        // 唯一实现在 `MauiVml.Run`（命令行页也走那条，见该方法的注释）。
        //
        // ⚠ 那条判据的方向**极易写反**（我写反过一次，两个症状都很迷惑）：写成
        // 「没有源码就是汇编」会变成 —— 内联汇编被判成编译（`CwdContext.Resolve(null)`
        // 抛 ArgumentNullException），而 `.c` 文件被判成汇编（**C 源码被喂给汇编器**，
        // 产出一个空程序、不报错，用户看到的是「程序正常运行，就是没输出」）。
        // 两者都不是"参数报错"，而是各自跑到别的分支上，所以特别难看出来。

        // 编译/运行都是**同步阻塞**的，必须丢到后台线程，否则卡住整个 Agent 循环
        // 预置 stdin：按行喂，读完之后再读给空行（不是 null —— 程序可能不判 EOF）。
        // 用 Queue 而不是 index：`ReadString` 是**在 VM 线程上同步调用**的，
        // 但 Queue 的出队本身也不保证线程安全，所以下面加了锁。
        var stdinLines = (GetString(arguments, "stdin") ?? "")
            .Replace("\r\n", "\n").Split('\n');
        int stdinPos = 0;
        var stdinLock = new object();
        string? ReadLine()
        {
            lock (stdinLock)
                return stdinPos < stdinLines.Length ? stdinLines[stdinPos++] : "";
        }
        var readLine = string.IsNullOrEmpty(GetString(arguments, "stdin")) ? (Func<string>?)null : ReadLine;

        return Task.Run(() =>
        {
            try
            {
                var output = MauiVml.Run(source, resolved, timeout, readLine);
                var text = string.IsNullOrEmpty(output) ? "（程序正常结束，没有输出）" : output;

                if (text.Length > MaxOutputChars)
                    text = text[..MaxOutputChars]
                         + $"\n\n⚠️ 输出过长已截断（共 {text.Length} 字符，只回传前 {MaxOutputChars}）";

                // 图形程序：把「程序自己声明这一帧画完了」的画面导成 PNG，附在结果后面。
                // **「没崩」不等于「画对了」** —— 游戏是画出来的，只回控制台文本等于让 AI 盲写：
                // 位置偏了、颜色错了、某个图元根本没画出来，文本输出里一个字都看不出来。
                if (TryExportFrame())
                    text += $"\n\n🖼 本帧画面已导出到 {FrameFile}（工作区根目录）。"
                          + "若当前模型支持看图，**用 view_image 打开它确认画得对不对**"
                          + "（东西画在哪、颜色对不对、有没有该出现却没出现的）。"
                          + "没有这个提示 = 程序没开过绘图窗口、或没调过 ui_present（纯文本程序属正常）。";

                return text;
            }
            catch (Exception ex)
            {
                // 汇编报错、VM 超时、被链接器裁掉的路径…都在这里兜住，别让 Agent 循环炸。
                //
                // **必须带上堆栈**：只回 `类型: 消息` 在移动端几乎没法查 ——
                // AOT/裁剪会把异常消息里的**资源键原文**漏出来（形如
                // `ArgumentNull_Generic Arg_ParamName_Name, path`），光看那句话
                // 连是哪个 API 抛的都判断不了，而真正的位置只在堆栈里。
                // 截断到 1200 字符：够看到最上面几帧，又不至于把上下文撑爆。
                var stack = ex.StackTrace ?? "(无堆栈)";
                if (stack.Length > 1200) stack = stack[..1200] + " …";
                return $"⚠️ VML 执行失败：{ex.GetType().Name}: {ex.Message}\n{stack}";
            }
        });
    }

    /// <summary>导出画面的文件名（解析到当前工作目录下）。</summary>
    private const string FrameFile = "vml_frame.png";

    /// <summary>
    /// 把绘图窗口「最新呈现帧」渲染成 PNG 写到工作目录。
    /// 没开过窗、或程序从没调 <c>ui_present</c> → 返回 false（不产生文件、不报错，纯文本程序的正常路径）。
    /// 用户可在设置里关掉（<c>VmlExportFrame</c>）或限制最大边长（<c>VmlFrameMaxSide</c>）。
    /// </summary>
    private static bool TryExportFrame()
    {
        if (!Config.Instance.VmlExportFrame) return false;

        var dsl = VmlUiCalls.Current?.TryGetPresentedDsl();
        if (string.IsNullOrWhiteSpace(dsl)) return false;
        try
        {
            var doc = DrawRunner.Parse(dsl);
            var png = DrawRunner.ToPng(doc);

            // 先比尺寸再决定要不要缩 —— 正常场景（VML 窗口常规 320×480）走到这里就结束了，
            // **一次解码都不会做**，所以这个限制对日常使用是零开销。
            var longest = Math.Max(doc.Width, doc.Height);
            if (longest > Config.Instance.VmlFrameMaxSide)
                png = Downscale(png, longest, Config.Instance.VmlFrameMaxSide);

            File.WriteAllBytes(CwdContext.Resolve(FrameFile), png);
            return true;
        }
        catch (Exception ex)
        {
            // 画面只是**附加**信息，导出失败不该把整轮工具调用判失败 —— 记一行日志就够了。
            ErrorLog.Warning("VmlTool", "导出画面失败", ex);
            return false;
        }
    }

    /// <summary>
    /// 等比缩小到最大边长以内。用**最近邻**：这是给 AI 看整体布局用的缩略图，
    /// 不值得为平滑插值多写一套；真要逐像素判读，正解是把 <c>VmlFrameMaxSide</c> 调大，
    /// 而不是靠插值把细节猜出来。
    /// </summary>
    private static byte[] Downscale(byte[] png, int longest, int maxSide)
    {
        var img = PngDecoder.Decode(png);
        var scale = (double)maxSide / longest;
        var w = Math.Max(1, (int)(img.Width * scale));
        var h = Math.Max(1, (int)(img.Height * scale));
        var dst = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        {
            var sy = Math.Min(img.Height - 1, (int)(y / scale));
            for (var x = 0; x < w; x++)
            {
                var sx = Math.Min(img.Width - 1, (int)(x / scale));
                var si = (sy * img.Width + sx) * 4;
                var di = (y * w + x) * 4;
                dst[di] = img.Rgba[si];
                dst[di + 1] = img.Rgba[si + 1];
                dst[di + 2] = img.Rgba[si + 2];
                dst[di + 3] = img.Rgba[si + 3];
            }
        }
        return PngEncoder.Encode(w, h, dst);
    }

    /// <summary>从 arguments 里取字符串（工具参数是松散字典，类型不保证）。</summary>
    private static string? GetString(Dictionary<string, object?> args, string key)
        => args.TryGetValue(key, out var v) && v is not null ? v.ToString() : null;

    private static int GetInt(Dictionary<string, object?> args, string key, int fallback)
        => args.TryGetValue(key, out var v) && v is not null && int.TryParse(v.ToString(), out var n)
            ? n : fallback;
}

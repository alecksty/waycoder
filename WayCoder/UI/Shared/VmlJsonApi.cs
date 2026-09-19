using WayCoder.Infra;

namespace WayCoder.UI.Shared;

/// <summary>
/// VML 的**全能接口**（`CALLJSON` #573）：两个字符串进（函数名 + 参数 JSON）、一个 JSON 字符串出。
///
/// ## 为什么要有它
///
/// 每加一个能力就占一个 syscall 号、加一对 C 包装、重生成 22 种语言的绑定 ——
/// 对**不要求性能**的功能（查个版本、报个屏幕尺寸、读个偏好）来说这套太重了。
/// 这一个号把"加功能"从"改协议"降成"注册一个函数"：宿主侧
/// <see cref="Register"/> 一行，程序侧 `ui_call_json("函数名", "{...}")` 一行。
///
/// ⚠ **性能敏感的东西不要走这里**：每次调用要序列化/解析两趟 JSON、还要穿一次
/// "写进 VM 内存的缓冲区"。绘图、输入这类每帧/每个事件都发生的调用仍然走专用号。
///
/// ## 信封（返回值永远是对象）
///
/// · 成功：`{"ok":true,"result":<该函数返回的任意 JSON>}`
/// · 失败：`{"ok":false,"error":"未知函数: xxx"}`
///
/// **函数实现抛异常不会把 VM 打挂**：异常被这里接住、翻成 `ok:false` 交给程序自己判断 ——
/// 与宿主 syscall 处理器那条"出错记日志并回失败码"的约定一致。
///
/// ## 加一个函数
///
/// <code>
/// VmlJsonApi.Register("screen", _ =&gt; JNode.Object().Set("w", 360).Set("h", 620));
/// </code>
///
/// 参数为 <c>null</c> 表示调用方没传 / 传了空串；传了但**不是合法 JSON** 会被拦在这里报错，
/// 不会把半个对象交给实现去猜。
/// </summary>
public static class VmlJsonApi
{
    /// <summary>
    /// 函数名 → 实现。参数是**解析好的**参数 JSON（没传就是 null），返回任意 JSON。
    ///
    /// 用 `StringComparer.Ordinal` 而不是默认比较器：函数名是**跨语言契约**（C 里是字符串字面量），
    /// 大小写不敏感会让 `GetVersion` 和 `getversion` 撞成一个 —— 那种"能跑但跑的是别的实现"
    /// 最难查。
    /// </summary>
    private static readonly Dictionary<string, Func<JNode?, JNode>> Handlers = new(StringComparer.Ordinal);

    /// <summary>已注册的函数名（诊断/自测用）。</summary>
    public static IEnumerable<string> Names => Handlers.Keys;

    /// <summary>注册（或覆盖）一个函数。**同名覆盖**，与插件注册表同一套语义。</summary>
    public static void Register(string name, Func<JNode?, JNode> handler)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        Handlers[name] = handler;
    }

    /// <summary>清空（自测用；生产不走）。</summary>
    internal static void ClearForTest() => Handlers.Clear();

    /// <summary>调用一个函数并返回**信封** JSON 字符串。任何失败都翻成 `ok:false`，不抛。</summary>
    public static string Invoke(string? fn, string? argsJson)
    {
        if (string.IsNullOrWhiteSpace(fn)) return ErrorEnvelope("函数名为空");
        if (!Handlers.TryGetValue(fn, out var handler)) return ErrorEnvelope($"未知函数: {fn}");

        JNode? args = null;
        if (!string.IsNullOrWhiteSpace(argsJson))
        {
            // 参数不是合法 JSON 就**当场报错**，别把半个对象交给实现去猜 ——
            // "参数看着像但解析成了别的"是这里最难查的一类。
            if (!Json.TryParse(argsJson, out var parsed)) return ErrorEnvelope("参数不是合法 JSON");
            args = parsed;
        }

        try
        {
            return Json.Serialize(JNode.Object().Set("ok", true).Set("result", handler(args) ?? JNode.Null()));
        }
        catch (Exception ex)
        {
            return ErrorEnvelope(ex.Message);
        }
    }

    /// <summary>
    /// 宿主写不下结果时用的信封（缓冲区比结果小）。**必须由这里出** ——
    /// 宿主那边自己拼字符串就又是一张平行的格式表。
    /// </summary>
    public static string TooLongEnvelope(int neededBytes)
        => ErrorEnvelope($"结果太长：需要 {neededBytes} 字节，缓冲区装不下");

    private static string ErrorEnvelope(string message)
        => Json.Serialize(JNode.Object().Set("ok", false).Set("error", message));
}

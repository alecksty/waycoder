// 桌面 CLI 的**最小桩**：让 `WayCoder/UI/Shared/**` 里那几个文件能原样编进来。
//
// 为什么需要它：`VmlUiProtocol.cs`（号段表 + 场景模型）是**与手机端编同一份**的文件，
// 而它引用了一次 `WayCoder.Infra.ErrorLog`（图元/刷子超限时的告警）。
// 这个 CLI 刻意**不引用 WayCoder 核心**（见 vmlcli.csproj 的说明：只引 vendored 的
// third_party/vml），所以那一个类型得有个顶替 —— 20 行的空实现，
// 比"为了一个日志调用把整个核心拖进来"或"抄一份号段表过来"都便宜。
//
// ⚠ 与 `WayCoder.Maui/CoreStubs.cs` 是**同一个套路**（那边顶替的是 TUI 与一批工具）。
//   那个文件里记过一条要紧的教训：**核心每加一个被这些文件用到的 public 成员，
//   都要同步补桩，漏了只在某个平台构建时才 CS0117/CS1061**。
//   本文件顶的是 `ErrorLog` —— 只顶 `VmlUiProtocol` 用到的那几个方法，
//   真实现多了签名不影响这里（这里多一个少一个没有任何运行时后果）。
//
// ⚠ **命名空间必须是 `WayCoder`**：真 `ErrorLog` 就住在那儿（`Infra/ErrorLog.cs`），
//   而 `VmlUiProtocol.cs` 是**不带 using 直接用**它的 —— 靠的是 C# 的
//   "外层命名空间可见"（`WayCoder.UI.Shared` → … → `WayCoder`）。
//   放到 `WayCoder.Infra` 里会编译不过（实测就是那两个 CS0103）。
namespace WayCoder;

/// <summary>桌面 CLI 的空日志实现（不落盘、不打印）。</summary>
internal static class ErrorLog
{
    public static void Info(string source, string message, object? context = null) { }

    public static void Warning(string source, string message, Exception? ex = null, object? context = null) { }

    public static void Error(string source, string message, Exception? ex = null, object? context = null) { }
}

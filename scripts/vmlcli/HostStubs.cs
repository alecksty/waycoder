// 桌面 CLI 的**第二组最小桩** —— 让 `WayCoder/Infra/` 里那几个绘图文件能原样编进来。
//
// 为什么需要它：桌面端现在要**真出图**（把 VML 场景渲成 PNG），而那条链是
// `DrawRunner.Parse` + `ToPng`（手机端 `VmlTool.TryExportFrame` 走的也是它）——
// 见 `CliVmlHost.RenderFrame`。为了"不造第二份渲染器"，这条路只能编同一份 Infra 源码，
// 而 `Infra/TrueTypeFont.cs` 解析文字时要找系统字体目录，`Infra/FontFinder.cs` 的第一句就是
// `WayCoder.Global.Home`。
//
// ⚠ 与 `CoreStubs.cs` 是**同一个套路**（那边顶的是 `ErrorLog`，理由见那个文件）。
//   顶替的成员只要能编过就行 —— 多一个少一个没有运行时后果，真实现的签名变化不影响这里。
//
// ⚠ 命名空间必须是 `WayCoder`：真 `Global` 就住在那儿（`Config/Global.cs`），
//   而 `FontFinder.cs` 是不带 using 直接用 `WayCoder.Global` 的。放别处编译不过。
namespace WayCoder;

/// <summary>
/// 桌面 CLI 的空配置宿主。这里**只顶 `Home`**（`FontFinder` 用它找 <c>~/Library/Fonts</c>
/// 这个可选目录）。
///
/// 返回空串是**够用的**：字体搜索是"按目录逐个找、找到就收"，
/// macOS 上 `/System/Library/Fonts` 与 `/Library/Fonts` 两支已覆盖系统字体，
/// 用户自己装的字体只是多一层 fallback —— 一个开发脚手架不必为它去读真实用户目录。
/// </summary>
internal static class Global
{
    public static string Home => "";
}

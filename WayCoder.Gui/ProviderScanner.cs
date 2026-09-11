using Avalonia.Threading;
using WayCoder.UI.TUI.Custom; // ModelPicker（ScanStatus / ProbeStatus，GUI 复用 TUI 那份）

namespace WayCoder.UI.Gui;

/// <summary>
/// 供应商连通性扫描（GUI 端共享）。
///
/// <c>ModelWindow.ScanAsync</c> 与 <c>ProviderWindow.TestAllAsync</c> 此前各写一份**结构完全相同**
/// 的实现：`Task.Run(ModelCli.TestList)` → 构造 providerId→状态 字典 → `Dispatcher.UIThread.Post`
/// 回投结果 → catch 里回投错误。差别只有提示文案与刷新方式（一个带搜索框过滤、一个不带），
/// 那部分属于各窗口自己的事，留在这里的只有「扫描 + 回投」这段共享逻辑。
/// </summary>
internal static class ProviderScanner
{
    /// <summary>
    /// 后台扫描全部供应商连通性，结果在 UI 线程回调；异常时回调错误消息（不抛）。
    /// 需在 UI 线程调用（内部用 Dispatcher 回投）。
    /// </summary>
    public static async Task ScanAsync(
        Action<Dictionary<string, ModelPicker.ScanStatus>> onResult,
        Action<string> onError)
    {
        try
        {
            var probes = await Task.Run(ModelCli.TestList);
            var dict = new Dictionary<string, ModelPicker.ScanStatus>();
            foreach (var p in probes) dict[p.ProviderId] = ModelPicker.ProbeStatus(p);
            Dispatcher.UIThread.Post(() => onResult(dict));
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() => onError(ex.Message));
        }
    }
}

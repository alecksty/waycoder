using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace WayCoder.Maui;

/// <summary>
/// Entry 的处理器 —— 存在的唯一目的是：**让编辑器的那个输入框换成
/// <see cref="BackspaceAwareEditText"/>**（它才接得住软键盘的行首退格）。
///
/// <para>
/// **为什么要在 <c>CreatePlatformView</c> 里分流，而不是用 mapper 加东西**：
/// <c>EntryHandler.Mapper</c> 是**全局静态**的，挂上去就作用到 App 里每一个 Entry；
/// 而本例要换的是**平台视图的类型**，mapper 拿到的是已经建好的实例，换不了。
/// 于是这里覆写创建入口、按 <c>StyleId</c> 分流 —— 非编辑器那一支返回**原样的
/// <c>MauiAppCompatEditText</c>**，App 里另外那些输入框（聊天框、API Key、仓库地址…）
/// 走的就是 MAUI 原本那一行代码，一个字节都不差。
/// </para>
///
/// <para>
/// **为什么在 <c>CreatePlatformView</c> 里读 <c>StyleId</c> 是安全的**：
/// <c>ElementHandler.SetVirtualView</c> 的顺序是 <c>VirtualView = view;</c> **然后**才
/// <c>PlatformView = CreatePlatformElement();</c> —— 建平台视图时虚拟视图已经挂上了。
/// </para>
///
/// <para>
/// **不走 <c>LineEditor.Handler = new XxxHandler()</c> 那条路**（iOS 侧有用这种写法）：
/// Android 的 <c>CreatePlatformView</c> 需要 <c>Context</c>，而页面构造函数阶段
/// <c>MauiContext</c> 还没挂上，会拿到 null。
/// </para>
/// </summary>
internal sealed class EditorEntryHandler : EntryHandler
{
    protected override MauiAppCompatEditText CreatePlatformView()
    {
        if ((VirtualView as Microsoft.Maui.Controls.Element)?.StyleId == MauiProgram.EditorLineStyleId)
        {
            // **在这里就打开接管**，不要留给页面去「认类型再接线」：
            // 那条路依赖 `LineEditor.Handler?.PlatformView is BackspaceAwareEditText` 成立，
            // 只要它不成立（handler 没换掉、或建平台视图时 VirtualView 还没挂上）就**静默**
            // 退回普通输入框 —— 用户看到的正是「擦除键没反应」。这里我们已经确知是那个输入框了。
            return new BackspaceAwareEditText(Context) { InterceptEnabled = true };
        }

        return new MauiAppCompatEditText(Context);
    }
}

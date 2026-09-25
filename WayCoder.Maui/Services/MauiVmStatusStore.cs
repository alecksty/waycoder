namespace WayCoder.Maui.Services;

/// <summary>
/// 「VM 状态面板」的开关（Preferences 持久）。
///
/// <para>
/// **全局一个开关，不是每页一个** —— 用户要的是「所有程序都可以看虚拟机状态」：
/// 在命令行页打开之后进绘图窗口也该是开着的。每页各记一份必然出现
/// "这页开了那页没开"，而那种不一致用户说不清、也查不动。
/// </para>
/// <para>
/// 默认**关**（`false`）：它是一块盖在画面上的浮层，没人要的时候不该挡着程序。
/// </para>
/// </summary>
public static class MauiVmStatusStore
{
    private const string Key = "vml.status";
    private const string DetailKey = "vml.status.detailed";

    /// <summary>面板是不是开着。读写都吞异常 —— 存储不可用不该让页面崩（与 <see cref="MauiShellStore"/> 同）。</summary>
    public static bool Visible
    {
        get { try { return Preferences.Get(Key, false); } catch { return false; } }
        set { try { Preferences.Set(Key, value); } catch { } }
    }

    /// <summary>
    /// **大窗**（true）/ **小窗**（false）—— 面板标题栏那两个图标切的就是它。
    ///
    /// <para>
    /// 与 <see cref="Visible"/> 一样是**全局一个**（两个页面看到的形态必须一致），也同样持久：
    /// 用户在游戏里挑了"小窗"，下次开面板不该又变回一大块。
    /// </para>
    /// <para>
    /// 默认 <b>小窗</b>：它是一块盖在 VML 程序画面上的浮层，**第一印象应当是"不碍事"** ——
    /// 要看寄存器的人点一下"大窗"就有了，反过来（默认一大块挡住游戏、还得先想办法缩小）
    /// 是让所有人都先付一次代价。
    /// </para>
    /// </summary>
    public static bool Detailed
    {
        get { try { return Preferences.Get(DetailKey, false); } catch { return false; } }
        set { try { Preferences.Set(DetailKey, value); } catch { } }
    }

    /// <summary>翻转并返回新值（菜单点一下的语义）。</summary>
    public static bool Toggle()
    {
        Visible = !Visible;
        return Visible;
    }
}

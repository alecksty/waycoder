using WayCoder.UI.Shared;

namespace WayCoder.Maui.Services;

/// <summary>
/// <b>「全能版」解锁状态的唯一事实源</b> —— 全 App 只问 <see cref="IsFull"/>，
/// 别在别处再判一次（是否有内购、是否恢复过、是不是测试账号，一律收敛在这里）。
///
/// <para>
/// <b>它管什么</b>：<c>FreeTierPolicy</c> 定的是**口径**（免费版给什么、全能版给什么），
/// 本类定的是**这个人有没有买**。两个门（<see cref="MauiVml"/> 的语言门、
/// <see cref="MauiCompileStore.OptimizationLevel"/> 的优化器门）都是
/// 「口径 × 状态」两个输入，别再各自去读 Preferences。
/// </para>
///
/// <para>
/// <b>状态从哪来</b>：只有 <see cref="Set"/> 一个写入口，而调它的**只有 <see cref="Iap"/>**
/// （平台层拿到已完成的交易之后）。UI 不许直接调 <see cref="Set"/> ——
/// 「界面点了购买按钮就把自己标成已解锁」正是这类实现最经典的漏洞。
/// </para>
///
/// <para>
/// ⚠ <b>为什么缓存到 Preferences 而不是每次去问商店</b>：手机上编译、切设置页都要判它，
/// 每次走一趟 StoreKit/Billing 是几百毫秒且可能失败；而**没有网络时也应当保持上次的结果**
/// （用户买过就是买过）。所以本地缓存一份，由「购买 / 恢复购买」两处去刷新它 ——
/// 这也是「恢复购买」按钮为什么是硬要求：换设备/重装之后本地缓存是空的，只有它能把它填回来。
/// </para>
///
/// <para>
/// ⚠ <b>不要在这里写 <c>static readonly</c> 字段去调 <c>L.Pick</c></b> ——
/// 那会在首次访问时把界面语言冻死（见 <c>UI/Shared/Lang.cs</c> 的类注释与它的源码扫描护栏）。
/// </para>
/// </summary>
public static class EntitlementStore
{
    /// <summary>
    /// 非消耗型内购的产品 ID（<b>一次性买断、可跨设备恢复</b>）。
    ///
    /// <para>
    /// ⚠ <b>真源在 <see cref="FreeTierPolicy.FullEditionProductId"/></b>，这里只是转引 ——
    /// 放在 <c>UI/Shared</c> 才进得了桌面自测，字面量才有判据钉住（改错一处 = 买完不解锁）。
    /// </para>
    /// </summary>
    public const string ProductId = FreeTierPolicy.FullEditionProductId;

    private const string KeyFull = "waycoder.entitlement.full";

    // 缓存：Preferences 只在首次访问时读一次（见类注释"为什么缓存"）
    private static bool _loaded;
    private static bool _isFull;

    /// <summary>是否已解锁全能版。<b>这是唯一的读取点</b>。</summary>
    public static bool IsFull
    {
        get
        {
            EnsureLoaded();
            return _isFull;
        }
    }

    /// <summary>解锁状态变化时触发（设置页那张卡片据此刷新）。</summary>
    public static event Action? Changed;

    /// <summary>
    /// <b>唯一的写入口</b> —— 只有 <see cref="Iap"/> 在拿到<em>已完成的交易</em>之后调它。
    /// 恢复购买时也要能把它**写成 false**（同一个 Apple ID 下一件都没买过 = 本该没有资格，
    /// 只增不减的话，退款或换账号之后会永久留着解锁）。
    /// </summary>
    public static void Set(bool isFull)
    {
        EnsureLoaded();
        bool changed = _isFull != isFull;
        _isFull = isFull;
        try { Preferences.Set(KeyFull, isFull); } catch { /* 存不进去只是下次启动要重新恢复购买 */ }
        if (changed) Changed?.Invoke();
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        try { _isFull = Preferences.Get(KeyFull, false); }
        catch { _isFull = false; }
    }

    // ── 文案（显示与状态分离：文案改一个字不该动到存进去的值）──

    /// <summary>版本名（商店页、设置页、审核备注都用这一个说法）。</summary>
    public static string ProductName => L.Pick("全能版", "Full Edition");

    /// <summary>状态一行字（设置页那张卡片的副标题）。</summary>
    public static string StatusText => IsFull
        ? L.Pick("已解锁：22 门语言的编译器 + 优化器", "Unlocked: all 22 language compilers + the optimizer")
        : L.Pick("未解锁：免费版可用编辑器、C 语言与 VML 汇编", "Locked: the free edition covers the editor, C, and VML assembly");

    /// <summary>设置首页那张卡片的摘要行（与 <see cref="StatusText"/> 同一口径，只是更短）。</summary>
    public static string Summary() => IsFull
        ? L.Pick("已解锁 · 22 门语言 + 优化器", "Unlocked · 22 languages + optimizer")
        : L.Pick("未解锁 · 免费版（C + VML 汇编）", "Locked · free edition (C + VML assembly)");

    /// <summary>
    /// 拦下编译时给的那句话 —— 措辞要**同时说清「为什么被拦」和「怎么解开」**，
    /// 否则用户看到的是"这个 App 坏了"。
    /// </summary>
    public static string LockedMessage(string language)
        => L.Pick($"🔒 {language} 编译器属于「{ProductName}」。免费版可以用编辑器、编译运行 C 语言与 VML 汇编。"
                  + $"可在「设置 → {ProductName}」里一次性解锁（含 22 门语言与优化器）。",
                  $"🔒 The {language} compiler is part of the {ProductName}. The free edition includes the editor, "
                  + $"C, and VML assembly. Unlock it once in Settings → {ProductName} (all 22 languages plus the optimizer).");
}

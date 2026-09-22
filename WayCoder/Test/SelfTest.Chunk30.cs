using WayCoder.UI.Shared;

namespace WayCoder;

public static partial class SelfTest
{
    /// <summary>
    /// **第三种窗口（电脑屏）**的协议层判据。
    ///
    /// <para>
    /// 三种窗口的分工（用户 2026-09-22 定）：**主屏是命令行**（已有），
    /// **副屏才需要弹窗**，而弹窗两种 ——
    /// </para>
    /// <list type="bullet">
    /// <item>**图形窗口**（`WIN_OPEN` / `WIN_OPEN_EX`，已有）：触摸 + 手柄，自适应缩放；</item>
    /// <item>**电脑屏窗口**（`WIN_OPEN_PC` #582，本批）：给老程序用 ——
    ///   坐标系**固定**为它声明的分辨率、触摸**只当鼠标**、带屏幕键盘而不是手柄。</item>
    /// </list>
    ///
    /// <para>
    /// 这一层可测是因为它**全是纯逻辑**（号、枚举值、解码、谓词）——
    /// 真正的窗口行为要真机验，但那几个判断错了会静默变成另一种窗口，
    /// 那是最难从现象反推的一类，所以先把能钉的钉住。
    /// </para>
    /// </summary>
    private static void TestChunk30(Action<string> Section, Action<string, bool> Check, Action<string> Fail)
    {
        TestWinKindContract(Section, Check);
        TestWinKindDecode(Section, Check);
        TestWinOpenPcDispatch(Section, Check);
        _ = Fail;
    }

    // ═══ ① 跨语言契约值与号段护栏 ═══
    private static void TestWinKindContract(Action<string> Section, Action<string, bool> Check)
    {
        Section("[第三种窗口] 契约值与号段");

        // 枚举值**只能末尾追加**（与 VmlShape 同规矩）。这两条钉住"没人顺手改过"。
        Check("VmlWinKind: Graphic=0（老窗口的档，不能变）", (int)VmlWinKind.Graphic == 0);
        Check("VmlWinKind: PcScreen=1（追加值）", (int)VmlWinKind.PcScreen == 1);

        // 默认必须是 Graphic —— **老窗口开出来行为一字不改**是本批的硬要求
        Check("VmlScene: 默认 Kind=Graphic（老窗口不受影响）",
            new VmlScene().Kind == VmlWinKind.Graphic);

        /* ⚠ 这一条是**补既有遗漏的证据**：
           `DrawTextEx = 581` 早就定义了，却一直没进 `AllNumbers` ——
           正是那段注释警告的"护栏形同虚设"（清单漏了号，查重查不出来）。
           加 582 时把它一起补上了，这里钉住"两个都在"。 */
        var all = VmlUi.AllNumbers;
        Check("AllNumbers 里有 DrawTextEx(581)——补的既有遗漏", all.Contains(VmlUi.DrawTextEx));
        Check("AllNumbers 里有 WinOpenPc(582)", all.Contains(VmlUi.WinOpenPc));
        Check("WinOpenPc 落在保留号段里（否则运行时会以权限不足拒掉）", VmlUi.Handles(VmlUi.WinOpenPc));

        // 582 不能与既有号撞（撞了会静默变成另一个功能）
        Check("WinOpenPc 不与任何既有号重复",
            all.Count(n => n == VmlUi.WinOpenPc) == 1);
    }

    // ═══ ② 解码与谓词 ═══
    private static void TestWinKindDecode(Action<string> Section, Action<string, bool> Check)
    {
        Section("[第三种窗口] 解码与触摸策略");

        /* **种类由号决定，不是参数**。做一个统一的"种类"参数会把"分类"永久写进
           跨语言契约，而三种窗口的实现根本不在同一层 —— 见 `KindOfWinOpen` 的说明。 */
        Check("KindOfWinOpen(582) → PcScreen", VmlUi.KindOfWinOpen(VmlUi.WinOpenPc) == VmlWinKind.PcScreen);
        Check("KindOfWinOpen(520) → Graphic（老号行为一字不改）",
            VmlUi.KindOfWinOpen(VmlUi.WinOpen) == VmlWinKind.Graphic);
        Check("KindOfWinOpen(570) → Graphic", VmlUi.KindOfWinOpen(VmlUi.WinOpenEx) == VmlWinKind.Graphic);

        /* **宽容**：认不出来的号一律当 Graphic —— 那是"能跑"的那一档，
           而不是让程序撞进一个它没写过的模式（与 R3 "认不出的转屏值当 Follow" 同方向）。 */
        Check("KindOfWinOpen(9999) → Graphic（宽容，不抛不崩）",
            VmlUi.KindOfWinOpen(9999) == VmlWinKind.Graphic);

        // 屏幕键盘默认**关**：只有电脑屏窗口会置上，图形窗口那条路一个字不变
        Check("VmlScene: 默认 NeedKeyboard=false（图形窗口不受影响）",
            !new VmlScene().NeedKeyboard);

        /* **触摸策略**：电脑屏窗口只发鼠标，不发触摸 ——
           同时收到两对会让老程序把一次点击当两次输入。
           注意语义是"换一种消息"而不是"不发消息"。 */
        Check("SuppressTouch: 电脑屏抑制触摸", VmlUi.SuppressTouch(VmlWinKind.PcScreen));
        Check("SuppressTouch: 图形窗口不抑制（行为一字不改）",
            !VmlUi.SuppressTouch(VmlWinKind.Graphic));
    }

    // ═══ ③ 真开一次窗（不只是常量断言）═══
    //
    // 上面两条钉的是常量与谓词；这一条是**端到端**：走完整的 syscall 分派，
    // 看 `VmlScene` 上落下来的字段对不对。它用的是 Chunk28 那个假宿主，
    // 所以不需要 MAUI、也不需要真 VM。
    private static void TestWinOpenPcDispatch(Action<string> Section, Action<string, bool> Check)
    {
        Section("[第三种窗口] 开窗分派（#582）");

        var host = new FakeVmlHost();
        var rt = new VmlHostRuntime(host);
        var regs = new int[32];
        var mem = new byte[4096];

        regs[0] = WriteCStr(mem, 0, "老程序");
        regs[1] = 0; regs[2] = 0;              // 非法尺寸 → 兜底
        regs[3] = 1; regs[4] = 1;
        Check("WIN_OPEN_PC 返回句柄 1", rt.HandleSyscall(VmlUi.WinOpenPc, regs, mem) && regs[0] == 1);
        Check("种类落成 PcScreen", host.Opened?.Kind == VmlWinKind.PcScreen);
        Check("非法尺寸兜底 640×480（PC 上最眼熟那一档）", host.Opened is { Width: 640, Height: 480 });
        Check("读到 R4=要屏幕键盘", host.Opened?.NeedKeyboard == true);
        Check("电脑屏**没有**手柄区", host.Opened?.NeedGamepad == false);

        /* ⚠ **R3=1（`Rotatable`）在电脑屏这里落到 `Legacy`** ——
           这是本窗口最要紧的一条：坐标系**固定**，永不重排。
           老程序按声明的分辨率排的版，换空间就会画到框外。 */
        Check("R3=1 不换坐标系（落到 Legacy，不是 Follow）",
            host.Opened?.Rotation == WindowRotation.Legacy);

        // 锁方向照旧是那两档
        regs[3] = VmlUi.PortraitOnly; rt.HandleSyscall(VmlUi.WinOpenPc, regs, mem);
        Check("R3=0 锁竖屏", host.Opened?.Rotation == WindowRotation.PortraitOnly);
        regs[3] = VmlUi.LandscapeOnly; rt.HandleSyscall(VmlUi.WinOpenPc, regs, mem);
        Check("R3=2 锁横屏", host.Opened?.Rotation == WindowRotation.LandscapeOnly);

        // R4=0 → 只做鼠标交互的程序可以关掉键盘，画布吃满整屏
        regs[3] = 0; regs[4] = VmlUi.NoKeyboard; rt.HandleSyscall(VmlUi.WinOpenPc, regs, mem);
        Check("R4=0 → 不要屏幕键盘", host.Opened?.NeedKeyboard == false);

        /* **回归**：老号必须一字不改 —— 种类、转屏声明都不受新号影响。
           （`Kind` 默认 Graphic；老号不读 R3/R4，这条 Chunk28 也钉过，这里再钉一次
             是因为**新号的分支就在它旁边**，改错一个 case 就会串。） */
        regs[3] = VmlUi.PortraitOnly; regs[4] = VmlUi.NoGamepad;
        rt.HandleSyscall(VmlUi.WinOpen, regs, mem);
        Check("回归：老号照旧 Graphic + 不读 R3/R4 + 不要键盘",
            host.Opened is { Kind: VmlWinKind.Graphic, Rotation: WindowRotation.Legacy, NeedKeyboard: false });
    }
}

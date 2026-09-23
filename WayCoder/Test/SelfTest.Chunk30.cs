using WayCoder.UI.Shared;
using WayCoder.UI.Shared.Terminal;

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

        TestSetVAlignDispatch(Section, Check);
        TestViewTransform(Section, Check);
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
    /// <summary>
    /// 文字**竖对齐**做成状态式之后的判据（`ui_set_valign` #586）：设一次，之后
    /// `ui_text_cur`（#533）就用它 —— 四档 = 顶 / 中 / 底 / **基线**。
    ///
    /// 判据一路查到**产出的 DSL**：只查"状态设上了"不够，得证明它真的落到了那条
    /// `text` 命令上（`vbase`/`vcenter`/`vbottom` 记号）—— 这正是"设了没生效"最容易漏的一段。
    /// </summary>
    private static void TestSetVAlignDispatch(Action<string> Section, Action<string, bool> Check)
    {
        Section("[文字竖对齐] 状态式设置（#586）");

        var host = new FakeVmlHost();
        var rt = new VmlHostRuntime(host);
        var regs = new int[32];
        var mem = new byte[4096];

        // ⚠ **场景是"开窗"时才建的** —— 不开窗 `Scene()` 是 null，断言会全部落空
        //   （第一版就是这么红的：六条判据全挂，因为查的是一个还不存在的场景）。
        regs[0] = WriteCStr(mem, 0, "对齐"); regs[1] = 320; regs[2] = 480;
        regs[3] = 1; regs[4] = 1;
        rt.HandleSyscall(VmlUi.WinOpenPc, regs, mem);

        Check("默认 = 基线（0 = 老行为）", rt.Scene()?.FontVAlign == VmlScene.VAlignBase);

        regs[0] = VmlScene.VAlignBase;
        Check("设「基线」生效", rt.HandleSyscall(VmlUi.SetVAlign, regs, mem)
            && rt.Scene()?.FontVAlign == VmlScene.VAlignBase);

        // 状态必须真的落到 `ui_text_cur` 产出的那条 text 命令上。
        // ⚠ **基线档刻意不写记号**（`VAnchorName(0)` 返回空串）：它是老行为，
        //   产物必须与从前**逐字相同** —— 所以这里断言的是"没有竖档记号"，不是"有 vbase"。
        regs[0] = 10; regs[1] = 20; regs[2] = WriteCStr(mem, 100, "字");
        rt.HandleSyscall(VmlUi.Text, regs, mem);
        var dsl = rt.Scene()?.BuildDsl() ?? "";
        Check("基线档不写竖档记号（产物与老版本逐字相同）",
            !dsl.Contains(" vbase") && !dsl.Contains(" vtop")
            && !dsl.Contains(" vcenter") && !dsl.Contains(" vbottom"));

        regs[0] = VmlScene.VAlignMiddle;
        rt.HandleSyscall(VmlUi.SetVAlign, regs, mem);
        regs[0] = 10; regs[1] = 20; regs[2] = WriteCStr(mem, 100, "字");
        rt.HandleSyscall(VmlUi.Text, regs, mem);
        var dsl2 = rt.Scene()?.BuildDsl() ?? "";
        Check("中档产出的 text 命令带 `vcenter`", dsl2.Contains(" vcenter"));

        // 越界值要夹住（老程序可能传来没初始化的寄存器）。上界 = **编号最大的那一档**（顶）。
        regs[0] = 99;
        rt.HandleSyscall(VmlUi.SetVAlign, regs, mem);
        Check("越界值夹到上界（顶，不落到未定义档）", rt.Scene()?.FontVAlign == VmlScene.VAlignTop);

        regs[0] = -7;
        rt.HandleSyscall(VmlUi.SetVAlign, regs, mem);
        Check("负值夹到下界（基线）", rt.Scene()?.FontVAlign == VmlScene.VAlignBase);

        /* **状态跟随窗口**（用户定的语义）：字体属性 / 竖对齐 / 默认色 / 画图参数都挂在
           **场景**上（`VmlScene`）—— 开窗建场景、关窗销毁 ⇒ 新窗口自然回到默认值。
           判据钉住这条链：设过 → 关窗 → 再开窗必须又是默认。 */
        regs[0] = VmlScene.VAlignBottom;
        rt.HandleSyscall(VmlUi.SetVAlign, regs, mem);
        Check("（前置）设过之后是「底」", rt.Scene()?.FontVAlign == VmlScene.VAlignBottom);

        rt.HandleSyscall(VmlUi.WinClose, regs, mem);
        Check("关窗后场景销毁（状态不跨窗口留着）", rt.Scene() == null);

        regs[0] = WriteCStr(mem, 0, "再来"); regs[1] = 320; regs[2] = 480;
        regs[3] = 1; regs[4] = 1;
        rt.HandleSyscall(VmlUi.WinOpenPc, regs, mem);
        Check("新窗口 = 默认值（竖对齐回到基线）", rt.Scene()?.FontVAlign == VmlScene.VAlignBase);
    }

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

    // ═══ ④ 显示变换（双指缩放 / 单指平移）═══
    //
    // ⚠ 这套换算**错了的症状是"点哪儿偏哪儿"**，而且偏得不单调（缩放比越大偏得越多）——
    //   从现象几乎反推不出来。所以在这里钉密一点：居中、往返、锚点不动、边界钳制。
    private static void TestViewTransform(Action<string> Section, Action<string, bool> Check)
    {
        Section("[第三种窗口] 显示变换");

        // ── 基准：等比缩放 + 居中（**只缩一个维度必然把另一个切掉**）──
        var t = new VmlViewTransform();
        t.Fit(viewW: 400, viewH: 800, sceneW: 640, sceneH: 480);
        // min(400/640, 800/480) = min(0.625, 1.667) = 0.625 ⇒ 显示 400×300，垂直居中
        Check("Fit: 取两方向里小的那个（0.625）", Math.Abs(t.Scale - 0.625) < 1e-9);
        Check("Fit: 装得下的方向居中（OffsetY=250）", Math.Abs(t.OffsetY - 250) < 1e-9);
        Check("Fit: 正好铺满的方向贴边（OffsetX=0）", Math.Abs(t.OffsetX) < 1e-9);
        Check("Fit: 初始 Zoom=1（看全貌）", Math.Abs(t.Zoom - 1) < 1e-9);

        // ── 往返：两把尺子必须互为逆 —— 这条是"点哪儿偏哪儿"的直接护栏 ──
        var v = t.ToView(320, 240);            // 场景正中
        var back = t.ToScene(v.X, v.Y);
        Check("往返：ToScene(ToView(场景正中)) 回到原地",
            back is { } b && Math.Abs(b.X - 320) < 1e-6 && Math.Abs(b.Y - 240) < 1e-6);

        var v2 = t.ToView(600, 400);
        var back2 = t.ToScene(v2.X, v2.Y);
        Check("往返：任意一点也回得来（不是只有正中凑巧对）",
            back2 is { } b2 && Math.Abs(b2.X - 600) < 1e-6 && Math.Abs(b2.Y - 400) < 1e-6);

        // ── 图外返回 null：触摸落在黑边上不该当成"点了 (0,0)" ──
        Check("图外：负坐标返回 null", t.ToScene(-10, -10) is null);
        Check("图外：黑边那一侧返回 null", t.ToScene(10, 10) is null);   // y=10 落在上下黑边里

        // ── 锚点不动：双指捏合时，两指中间那块内容不该跑 ──
        var anchorX = 200.0; var anchorY = 400.0;
        var beforeScene = t.ToScene(anchorX, anchorY);
        t.ZoomAt(anchorX, anchorY, 2.0);
        var afterScene = t.ToScene(anchorX, anchorY);
        Check("ZoomAt: 锚点底下的场景内容**保持不动**",
            beforeScene is { } bs && afterScene is { } as_ &&
            Math.Abs(bs.X - as_.X) < 1e-6 && Math.Abs(bs.Y - as_.Y) < 1e-6);
        Check("ZoomAt: 缩放比真的变了（2 倍）", Math.Abs(t.Zoom - 2) < 1e-9);

        // ── 放大后的平移：内容**始终盖满视口**（两头都不露白）──
        t.Pan(10000, 10000);                   // 往右下猛推
        Check("平移：(0,0) 侧不露白（OffsetX<=0）", t.OffsetX <= 1e-9);
        Check("平移：右下侧不露白（内容右缘盖住视口）", t.OffsetX + 640 * t.Scale >= 400 - 1e-9);
        t.Pan(-10000, -10000);                 // 往左上猛推
        Check("平移：反方向也不露白", t.OffsetX >= 400 - 640 * t.Scale - 1e-9);

        // ── 缩放钳位：两端都要夹住（不夹的话会缩到看不见 / 放到溢出）──
        t.ZoomAt(200, 400, 1000);
        Check("缩放上限被钳在 MaxZoom", Math.Abs(t.Zoom - VmlViewTransform.MaxZoom) < 1e-9);
        t.ZoomAt(200, 400, 0.0001);
        Check("缩放下限被钳在 MinZoom（不会缩到比看全貌还小）",
            Math.Abs(t.Zoom - VmlViewTransform.MinZoom) < 1e-9);

        // ── 复位：双指双击回到看全貌 ──
        t.ZoomAt(200, 400, 4);
        t.Reset();
        Check("Reset: 回到 Zoom=1", Math.Abs(t.Zoom - 1) < 1e-9);
        Check("Reset: 且居中（不是停在放大时的偏移上）",
            Math.Abs(t.OffsetY - 250) < 1e-6 && Math.Abs(t.OffsetX) < 1e-6);

        /* ── 换视口**保持缩放比**：程序转屏/收键盘换了视口时，
              用户刚放大到看细节的那一档不该被扔掉 ── */
        var t2 = new VmlViewTransform();
        t2.Fit(400, 800, 640, 480);
        t2.ZoomAt(200, 400, 2);
        t2.Fit(800, 400, 640, 480);            // 视口变了
        Check("换视口保持 Zoom（不把用户放大的一档扔掉）", Math.Abs(t2.Zoom - 2) < 1e-9);
        Check("换视口后仍不露白",
            t2.OffsetX <= 1e-9 && t2.OffsetX + 640 * t2.Scale >= 800 - 1e-9);
    }
}

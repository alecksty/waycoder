#if ANDROID
using Android.Views;
using WayCoder.UI.Shared;

namespace WayCoder.Maui.Services;

/// <summary>
/// **物理键盘 → VML 窗口消息**的唯一通道（Android）。
///
/// <para>
/// 电脑屏窗口是给 PC/Linux 老程序用的，那些程序的输入就是键盘。手机上光有屏幕键盘不够 ——
/// 接个蓝牙键盘才是正经用法（屏幕上那块键盘只是没外设时的兜底，还占掉小半个画面）。
/// </para>
///
/// <para>
/// ⚠ **为什么挂在 `Activity.DispatchKeyEvent` 而不是某个输入框上**：
/// 命令行页那条路（`ShellPage.HookHardwareKeyboard`）是给一个 `EditText` 挂 `KeyPress`，
/// 靠"它有焦点"来收键。绘图窗口页**根本没有输入框** —— 它是一整块自绘画布，
/// 硬塞一个不可见的 `Entry` 进去只为了吃键，会顺带把 IME、抽取式编辑、输入法工具条
/// 一并带进这个页面（编辑器和绘图页都在这上面栽过）。物理键盘事件本来就是
/// Activity 级的（`DispatchKeyEvent`），挂在源头上，谁都不用假装是个输入框。
/// </para>
///
/// <para>
/// ⚠ **这是与 `ShellPage.AnsiForAndroidKey` 并列的第二张 Android 键表，不是重复** ——
/// 只因**目的地不同**：那边要把键翻成**终端 ANSI 字节序列**（`\x1b[A` 之类，
/// 给 curses/conio 程序读的），这边要翻成 **Win32 虚拟键码**（`VK_*`，
/// 给收 `VML_MSG_KEYDOWN` 的程序读的）。两套目标编码之间没有任何可共享的部分，
/// 合并只会得到一张"两个方向都别扭"的表。改动一边时请顺手看一眼另一边。
/// </para>
/// </summary>
public static class HardwareKeys
{
    /// <summary>
    /// 当前在吃物理键盘的那个窗口（null = 没有，事件照常往下传）。
    /// 参数是 `(虚拟键码, 是不是按下)`。
    ///
    /// ⚠ 只有一个槽位，因为 VML 执行是**排他**的（`VmlTool.ExecutionMode = Exclusive`），
    ///   同一时刻最多一个绘图窗口活着。将来若允许多窗口并发，这里要改成集合。
    /// </summary>
    public static Action<int, bool>? Sink;

    /// <summary>
    /// 当前**按着没放**的键。用来分辨"只收到 Up"（输入法吞了 Down）与正常的一按一放。
    /// 只在 UI 线程用（`DispatchKeyEvent` 本来就在主线程）。
    /// </summary>
    private static readonly HashSet<int> _down = new();

    /// <summary>
    /// Activity 把每个键事件递进来。返回 true = **已被吃掉**（不再往下传）。
    ///
    /// 吃掉的范围**只限认得出的 VML 键**：返回键（`Keycode.Back`）、音量键、Home 键
    /// 一律放行 —— 把返回键吃掉会让用户退不出这个窗口（`OnBackButtonPressed` 收不到）。
    /// </summary>
    public static bool TryDispatch(KeyEvent e)
    {
        if (Sink is null) return false;

        bool down;
        switch (e.Action)
        {
            case KeyEventActions.Down:
            case KeyEventActions.Multiple:   // 长按重复：对程序而言就是又一次 KeyDown
                down = true;
                break;
            case KeyEventActions.Up:
                down = false;
                break;
            default:
                return false;
        }

        int vk = VirtualKey(e);
        if (vk == 0) return false;          // 认不出 ⇒ 不动它，交给系统

        Emit(vk, down);
        return true;
    }

    /// <summary>
    /// 发一次（虚拟键码, 按下/抬起）。**键盘与摇杆两条路共用这一处** ——
    /// `_down` 那本账只有一份，两条路各写一遍必然对不上。
    /// </summary>
    private static void Emit(int vk, bool down)
    {
        var sink = Sink;
        if (sink is null) return;

        if (down)
        {
            _down.Add(vk);
            sink(vk, true);
        }
        else if (_down.Remove(vk))
        {
            sink(vk, false);                // 正常：先 Down 后 Up
        }
        else
        {
            /* ⚠ **只收到 Up** —— 这是真机实测出来的最常见形态，必须补一次合成的 KeyDown。
             *
             * 原因：**输入法在场时 Android 会把 ACTION_DOWN 交给 IME**（它要用 Down 生成字符），
             * 只有 ACTION_UP 会落到 Activity。实测（模拟器 logcat，`WCKEY`）：
             * 敲一整条命令 32 个键事件，**`action=Down` 一条都没有**。
             *
             * 只发 KeyUp 的后果很隐蔽：VML 那边的 `getch()` 只认 `KEYDOWN`（KeyUp 一律跳过，
             * 因为返回 0 会被老程序当成"窗口关了"）⇒ **按了完全没反应**，
             * 而"按键确实到了应用"这一点在日志里又看得见，查起来格外绕。
             *
             * 这里补一次 Down 再补一次 Up：对程序而言就是**干干净净地按了一下**，
             * 且不影响正常路径（有 Down 的时候走上面那条分支，不会重复发）。 */
            sink(vk, true);
            sink(vk, false);
        }
    }

    // ── 模拟摇杆 → 虚拟方向键（v0.96.496）──────────────────────────────────
    //
    // **为什么映射成方向键，而不是新开一套"轴"接口**：
    //   现有游戏全部是按 `VML_KEY_LEFT/UP/...` 写的（屏幕手柄就是发这个）。
    //   把摇杆翻成同样的键 ⇒ **所有游戏不用改一个字就支持物理手柄的摇杆**。
    //   这与"手柄面键映射到自然键盘键"是同一条原则：**不另造一套只有新程序认得的编号**。
    //
    // ⚠ **只取主导轴（4 向，不是 8 向）**：摇杆推到斜角时同时按住两个方向，
    //   对"为十字键写的游戏"是**没见过的输入**（十字键物理上按不出斜角）。
    //   `tetris` 那类会一下往两个方向掉 —— 与其让每个游戏各自处理，不如在这里收敛。
    //   要 8 向就得先把游戏改成能处理 —— 那是另一件事。

    /// <summary>摇杆的死区。手柄不在中心是常态（摇杆磨损、霍尔漂移），
    /// 不去掉的话游戏会"自己一直往一边走"。0.4 是常见取值。</summary>
    private const float StickDead = 0.4f;

    /// <summary>当前**由摇杆按着**的方向键（0 = 没有）。只在主线程用。</summary>
    private static int _stickVk;

    /// <summary>
    /// Activity 把手柄的**轴事件**递进来（`DispatchGenericMotionEvent`）。
    ///
    /// ⚠ 摇杆是**连续量**、每秒几百个事件：只在**方向真变了**的时候才发键，
    ///   否则一次推杆就是把消息队列灌满（而 VML 主循环一次只取一条）。
    /// </summary>
    public static bool TryDispatchMotion(MotionEvent e)
    {
        if (Sink is null) return false;

        // 左摇杆（X/Y）与十字键帽（HatX/HatY）取**绝对值大的那个** ——
        // 有些手柄把十字键也报成轴，两者行为一致，谁动听谁的。
        var lx = e.GetAxisValue(Axis.X);
        var ly = e.GetAxisValue(Axis.Y);
        var hx = e.GetAxisValue(Axis.HatX);
        var hy = e.GetAxisValue(Axis.HatY);
        if (MathF.Abs(hx) > MathF.Abs(lx)) lx = hx;
        if (MathF.Abs(hy) > MathF.Abs(ly)) ly = hy;

        var want = 0;
        if (MathF.Abs(lx) >= StickDead || MathF.Abs(ly) >= StickDead)
        {
            if (MathF.Abs(lx) >= MathF.Abs(ly))
                want = lx > 0 ? VmlKeys.Right : VmlKeys.Left;
            else
                // ⚠ Android 摇杆的 Y 轴**与屏幕同向**：向下为正、向上为负
                //   （`AXIS_Y` 是与触摸共用的坐标轴，不是数学坐标系）。
                //   写反的症状是"推上它往下走" —— 与 `tilt.c` 的轴符号同一类问题。
                want = ly > 0 ? VmlKeys.Down : VmlKeys.Up;
        }

        if (want == _stickVk) return want != 0;   // 没变 ⇒ 什么都不发（关键：别灌队列）

        if (_stickVk != 0) Emit(_stickVk, false);
        if (want != 0) Emit(want, true);
        _stickVk = want;
        return want != 0;
    }

    /// <summary>
    /// 松开摇杆方向键。**窗口关闭时必须调** —— 摇杆没有"抬起"事件，
    /// 不主动松的话，玩家推着杆关掉窗口，下一个程序会看到那个方向**一直是按着的**。
    /// （与"长按连发要自带刹车"是同一类：**凡是有状态的东西，都要问一句"谁来清"**。）
    /// </summary>
    public static void ReleaseStick()
    {
        if (_stickVk == 0) return;
        Emit(_stickVk, false);
        _stickVk = 0;
    }

    /// <summary>
    /// Android 键码 → **Win32 虚拟键码**。
    ///
    /// 取 Win32 而不是自编号，理由与手柄映射到自然键一样：程序里写 `key == VML_KEY_F1`，
    /// 拿到的必须与 PC 上真按 F1 一致，否则同一份老程序在手机和电脑上要写两套判断。
    ///
    /// 0 = 认不出（调用方据此放行）。
    /// </summary>
    public static int VirtualKey(KeyEvent e)
    {
        switch (e.KeyCode)
        {
            // ── 控制键 ──
            case Keycode.Escape:      return VmlKeys.Escape;
            case Keycode.Del:         return VmlKeys.Backspace;   // ⚠ Android 的 `Del` 是退格
            case Keycode.ForwardDel:  return VmlKeys.Delete;
            case Keycode.Enter:
            case Keycode.NumpadEnter: return VmlKeys.Enter;
            case Keycode.Tab:         return VmlKeys.Tab;
            case Keycode.Space:       return VmlKeys.Space;

            // ── 方向 / 翻页 / 编辑 ──
            // ⚠ `Keycode.Home` 是**安卓的 Home 键**（回桌面那个），不是键盘的 Home！
            //   键盘 Home 是 `Keycode.MoveHome`。写错了程序就永远收不到 Home，
            //   而现象是"按 Home 回到桌面"——完全不像键表的问题。
            case Keycode.DpadUp:      return VmlKeys.Up;
            case Keycode.DpadDown:    return VmlKeys.Down;
            case Keycode.DpadLeft:    return VmlKeys.Left;
            case Keycode.DpadRight:   return VmlKeys.Right;
            case Keycode.MoveHome:    return VmlKeys.Home;
            case Keycode.MoveEnd:     return VmlKeys.End;
            case Keycode.PageUp:      return VmlKeys.PageUp;
            case Keycode.PageDown:    return VmlKeys.PageDown;
            case Keycode.Insert:      return VmlKeys.Insert;

            // ── 修饰键 ──
            // 左右不分，都报同一个 VK（Win32 里左右由扩展位区分，老程序绝大多数不查）。
            // ⚠ 修饰键**必须发**：程序靠"先收到 VK_SHIFT 再收到 'A'"才知道是大写 A 还是
            //   方向键组合，不发的话 Ctrl+C 这类组合在老程序里全都对不上。
            case Keycode.CtrlLeft:
            case Keycode.CtrlRight:   return VmlKeys.Ctrl;
            case Keycode.ShiftLeft:
            case Keycode.ShiftRight:  return VmlKeys.Select;
            case Keycode.AltLeft:
            case Keycode.AltRight:    return VmlKeys.Alt;

            // ── **外接游戏手柄**的面键（蓝牙/USB 手柄）──────────────────────
            //
            // ⚠ 这一段是**补缺口**（v0.96.494）：Android 的手柄面键发的是
            //   `Keycode.ButtonA`(96) 这一套**自己的**键码，与 Win32 的 `'A'`(65) 不是一回事
            //   ⇒ 从前**外接手柄按了完全没反应**（而屏幕上的自绘手柄走的是我们自己发的
            //   Win32 键，所以一直是对的 —— 这个缺口只在真接一个物理手柄时才暴露）。
            //
            // 映射原则与上面那批**逐字相同**：**映射到自然键盘等价键**，不另造一套手柄编号。
            // 于是同一个 VML 程序在「屏幕手柄 / 物理键盘 / 外接手柄」三种输入下都能玩。
            //
            // ⚠ **只映射屏幕手柄上画得出来的那几个**（A/B/X/Y/START/SELECT）——
            //   与 `DrawWindowPad` 的布局保持一一对应。手柄的肩键（L1/R1/L2/R2）
            //   与摇杆按下（Thumb）**刻意不映射**：它们在键盘上**没有自然等价键**，
            //   硬造一个只有我们知道的值，等于让程序去认一个"从来没有过的键" ——
            //   而那种键在桌面（物理键盘）上永远按不出来，正是上面那段注释要避免的分叉。
            case Keycode.ButtonA:      return VmlKeys.PadA;
            case Keycode.ButtonB:      return VmlKeys.PadB;
            case Keycode.ButtonX:      return VmlKeys.PadX;
            case Keycode.ButtonY:      return VmlKeys.PadY;
            case Keycode.ButtonStart:  return VmlKeys.Start;
            case Keycode.ButtonSelect: return VmlKeys.Select;
            // 有些手柄的 START/SELECT 走的是另一组（`ButtonMode` 是中间的 Home，
            // **不映射** —— 系统拿它开游戏中心，抢过来会让玩家退不出去）。
            case Keycode.Button1:      return VmlKeys.PadX;   // 少数手柄把面键报成 1/2/3/4
            case Keycode.Button2:      return VmlKeys.PadA;
            case Keycode.Button3:      return VmlKeys.PadB;
            case Keycode.Button4:      return VmlKeys.PadY;

            default: break;
        }

        // F1..F12 在 Android 里也是连号的，与 Win32 同样连号 —— 直接偏移
        if (e.KeyCode >= Keycode.F1 && e.KeyCode <= Keycode.F12)
            return VmlKeys.F((int)(e.KeyCode - Keycode.F1) + 1);

        // ── 可打印字符 ──
        // Android 的字母键码是它自己的一套（`Keycode.A` = 29，不是 'A'），
        // 但 `UnicodeChar` 给的是**真实的那个字符**（且已经算进了 Shift）。
        int uni = e.UnicodeChar;
        if (uni == 0) return 0;
        char c = (char)uni;
        char up = char.ToUpperInvariant(c);

        // 主键盘字母/数字的 VK 恰好**就是它们的 ASCII 码**（VK_A='A'=65、VK_0='0'=48）
        if (up >= 'A' && up <= 'Z') return up;
        if (c >= '0' && c <= '9') return c;

        return PrintableVk(c);
    }

    /// <summary>
    /// 可打印标点 → VK。**按物理键**归位（`!` 归到 `VK_1`、`{` 归到 `VK_OEM_4`），
    /// 因为 Win32 的老程序查的就是物理键 + Shift 状态，不是那个字符本身
    /// （Shift 我们已经单独发过 `VK_SHIFT` 了）。
    /// </summary>
    private static int PrintableVk(char c) => c switch
    {
        // OEM 标点：Win32 给它们的编号**不是 ASCII 码**（`-` 是 189 不是 45）
        '-' or '_' => VmlKeys.OemMinus,
        '=' or '+' => VmlKeys.OemPlus,
        '[' or '{' => VmlKeys.OemOpenBracket,
        ']' or '}' => VmlKeys.OemCloseBracket,
        '\\' or '|' => VmlKeys.OemBackslash,
        ';' or ':' => VmlKeys.OemSemicolon,
        '\'' or '"' => VmlKeys.OemQuotes,
        ',' or '<' => VmlKeys.OemComma,
        '.' or '>' => VmlKeys.OemPeriod,
        '/' or '?' => VmlKeys.OemQuestion,
        '`' or '~' => VmlKeys.OemTilde,

        // 上档数字回到它那个物理键
        '!' => '1', '@' => '2', '#' => '3', '$' => '4', '%' => '5',
        '^' => '6', '&' => '7', '*' => '8', '(' => '9', ')' => '0',

        _ => 0,
    };
}
#endif

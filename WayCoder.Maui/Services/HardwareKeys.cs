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
    /// Activity 把每个键事件递进来。返回 true = **已被吃掉**（不再往下传）。
    ///
    /// 吃掉的范围**只限认得出的 VML 键**：返回键（`Keycode.Back`）、音量键、Home 键
    /// 一律放行 —— 把返回键吃掉会让用户退不出这个窗口（`OnBackButtonPressed` 收不到）。
    /// </summary>
    public static bool TryDispatch(KeyEvent e)
    {
        var sink = Sink;
        if (sink is null) return false;

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
        sink(vk, down);
        return true;
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

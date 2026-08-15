// =============================================================
// KeyEvent.cs —— 键盘与鼠标输入事件模型
//
// 统一描述终端输入：键盘按键（含修饰键）、粘贴文本、鼠标事件。
// 由 InputReader 解析原始字节流后转换为这些事件。
// =============================================================

namespace QBasic.Tui;

/// <summary>修饰键标志。</summary>
[Flags]
public enum KeyMods
{
    None = 0,
    Shift = 1,
    Alt = 2,
    Ctrl = 4,
}

/// <summary>特殊按键种类；Key 为 0 时表示普通字符。</summary>
public enum KeyCode
{
    None = 0,
    Enter = 1,
    Tab = 2,
    Backspace = 3,
    Esc = 4,
    Escape = 4,     // Esc 别名
    Delete = 5,
    Home = 6,
    End = 7,
    PgUp = 8,
    PgDn = 9,
    Insert = 10,
    Up = 11,
    Down = 12,
    Left = 13,
    Right = 14,
    F1 = 15, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    Space = 20,
    Mouse = 21,
    Paste = 22,
}

/// <summary>鼠标按钮/事件。</summary>
public enum MouseButton
{
    None = 0,
    Left = 1,
    Right = 2,
    Middle = 3,
    WheelUp = 4,
    WheelDown = 5,
    Move = 6,
}

/// <summary>一次键盘或鼠标事件。</summary>
public struct InputEvent
{
    public KeyCode Key;
    public char Ch;          // 普通字符（Key==None 时有效）
    public KeyMods Mods;
    public MouseButton Button;
    public int MouseRow;     // 1-based
    public int MouseCol;     // 1-based
    public string? Text;     // Paste 事件的内容

    public bool IsKey(KeyCode k) => Key == k && Mods == KeyMods.None;
    public bool IsCtrl(char c) => Key == KeyCode.None && Mods.HasFlag(KeyMods.Ctrl) && char.ToLowerInvariant(Ch) == char.ToLowerInvariant(c);
    public bool IsEsc => Key == KeyCode.Esc;
}

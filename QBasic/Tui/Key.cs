// QBasic/Tui/Key.cs
// 键盘输入模型：字符键、方向键、功能键、组合键。
namespace QBasic.Tui;

public enum KeyKind
{
    None,
    Character,
    Enter,
    Tab,
    BackTab,
    Backspace,
    Delete,
    Escape,
    ArrowUp,
    ArrowDown,
    ArrowLeft,
    ArrowRight,
    Home,
    End,
    PageUp,
    PageDown,
    Insert,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    MouseWheelUp,
    MouseWheelDown,
    MouseLeftClick,
    MouseMove,
}

public readonly struct KeyEvent
{
    public KeyKind Kind { get; init; }
    public char Character { get; init; }
    public bool Ctrl { get; init; }
    public bool Alt { get; init; }
    public bool Shift { get; init; }
    // 鼠标事件坐标（1 基）
    public int MouseX { get; init; }
    public int MouseY { get; init; }

    public static KeyEvent Of(KeyKind kind) => new() { Kind = kind };
    public static KeyEvent Char(char c) => new() { Kind = KeyKind.Character, Character = c };
    public static KeyEvent CtrlChar(char c) => new() { Kind = KeyKind.Character, Character = c, Ctrl = true };

    public bool IsCharacter => Kind == KeyKind.Character && !Ctrl;

    public override string ToString()
    {
        if (Kind == KeyKind.Character) return (Ctrl ? "Ctrl+" : "") + Character.ToString();
        return Kind.ToString();
    }
}

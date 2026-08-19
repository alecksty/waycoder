namespace WayCoder.UI.TUI.Base;

/// <summary>
/// 键位作用域 —— 分两级，规则只有一条：
///
///   <b>系统键</b>：任何时候最高优先级，穿透一切窗口。目前只有 <c>Ctrl+C</c>。
///   <b>窗口键</b>：其余全部。只在所属窗口是栈顶/焦点时生效；弹出子窗口即被子窗口屏蔽，
///                 子窗口关闭、焦点回到父窗口后自动恢复。
///
/// 由此得到一个重要性质：<b>子窗口与父窗口注册同一个键不冲突</b>（栈顶那个说了算），
/// 只有与系统键才会冲突。对话框的选项快捷键（Y/N/A…）正是靠这条才敢随便用字母。
///
/// 判定放在这里而不是散在各处 if-else，是为了让「哪些键能穿透对话框」这件事
/// 有唯一一份可自测的答案 —— 此前 REPL 主循环里有 6 组键自称「系统级」，
/// 后台线程弹确认框时它们会绕过窗口系统，按 F1 能把屏幕栈直接拆掉。
/// </summary>
public static class TuiKeyScope
{
    /// <summary>
    /// 是否系统键（任何时候有效，不受窗口栈约束）。
    ///
    /// 目前仅 <c>Ctrl+C</c>：它同时走 OS 的 <c>Console.CancelKeyPress</c>，
    /// 任何线程、任何收键循环（REPL / RenderWait / RunAgentWithRenderLoop）下都能触发，
    /// 本来就是全局的，这里只是把这个事实写进代码。
    ///
    /// <b>往这里加成员前先想清楚：多一个系统键，就多一个能穿透对话框的键。</b>
    /// </summary>
    public static bool IsSystemKey(ConsoleKeyInfo key) =>
        key.Key == ConsoleKey.C && key.Modifiers.HasFlag(ConsoleModifiers.Control);
}

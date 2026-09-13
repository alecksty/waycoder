namespace WayCoder.UI.Tui.Controls;

/// <summary>提示条目的类型</summary>
public enum EPromptKind
{
    Command,
    File,
    Shell,
    Slash,
    History,
    Recent,

    /// <summary>
    /// 行内选择项（权限确认 / 计划审批 / 通用确认）—— 无图标，靠 <c>Label</c> 自带序号。
    /// 与上面几种「输入提示」的区别：那些是「回填输入框」的候选，这个是「做决定」的选项。
    /// </summary>
    Choice
}
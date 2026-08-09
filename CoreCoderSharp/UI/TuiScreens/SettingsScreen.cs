namespace CoreCoderSharp.UI;

/// <summary>
/// 设置屏幕 —— 配置编辑器。
/// 当前转发到旧 SettingsPage.Show()。
/// 后续迁移为纯 TuiScreen + TuiControl 实现。
/// </summary>
public class SettingsScreen : TuiScreen
{
    public SettingsScreen()
    {
        Name = "settings";
    }

    public override void Activate()
    {
        base.Activate();
        // 转发到旧实现
        SettingsPage.Show();
    }
}

/// <summary>
/// 编辑屏幕 —— 终端内源码编辑器（预留）。
/// </summary>
public class EditorScreen : TuiScreen
{
    public string FilePath { get; set; } = "";

    public EditorScreen(string filePath = "")
    {
        Name = "editor";
        FilePath = filePath;
    }

    public override void Activate()
    {
        base.Activate();
        // 预留：后续集成 Edit/Editor.cs
    }
}

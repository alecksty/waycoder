namespace WayCoder.UI.Shared;

/// <summary>
/// promptbar 精选常用命令（输入框上方提示栏，四端共用）。
/// TUI/MAUI/GUI 编译 UI/Shared/** 共享；Web 是纯 JS 前端维护同款数组。
/// </summary>
public static class CommandBar
{
    /// <summary>精选常用命令（promptbar 一行显示）：命令名 + 简短描述。</summary>
    /// ⚠ 表达式体属性而非 `static readonly` 字段 —— 后者会把界面语言冻在类型初始化那一刻。
    public static (string Name, string Desc)[] Favorites =>
    [
        ("/help", L.Pick("帮助", "Help")),
        ("/model", L.Pick("选模型", "Pick model")),
        ("/provider", L.Pick("服务商", "Providers")),
        ("/review", L.Pick("代码审查", "Code review")),
        ("/reset", L.Pick("清空会话", "Clear session")),
        ("/tokens", "Token"),
        ("/session", L.Pick("会话管理", "Sessions")),
        ("/perm", L.Pick("权限", "Permission")),
        ("/mcp", "MCP"),
        ("/theme", L.Pick("主题", "Theme")),
    ];
}

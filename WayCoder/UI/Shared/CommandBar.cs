namespace WayCoder.UI.Shared;

/// <summary>
/// promptbar 精选常用命令（输入框上方提示栏，四端共用）。
/// TUI/MAUI/GUI 编译 UI/Shared/** 共享；Web 是纯 JS 前端维护同款数组。
/// </summary>
public static class CommandBar
{
    /// <summary>精选常用命令（promptbar 一行显示）：命令名 + 简短描述。</summary>
    public static readonly (string Name, string Desc)[] Favorites =
    [
        ("/help", "帮助"),
        ("/model", "选模型"),
        ("/provider", "服务商"),
        ("/review", "代码审查"),
        ("/reset", "清空会话"),
        ("/tokens", "Token"),
        ("/session", "会话管理"),
        ("/perm", "权限"),
        ("/mcp", "MCP"),
        ("/theme", "主题"),
    ];
}

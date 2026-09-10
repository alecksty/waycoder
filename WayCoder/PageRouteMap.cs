namespace WayCoder;

/// <summary>
/// `/topage &lt;页面&gt;` 的页面名 → Shell 路由纯映射（无 UI 依赖，供 MAUI 命令层调用并被主自测覆盖）。
/// Tab 用 `//xxx` 绝对路由，独立页用相对路由（push）。
/// </summary>
public static class PageRouteMap
{
    /// <summary>页面名（含中文/常用别名）→ 路由；无法识别返回 null。</summary>
    public static string? Resolve(string page)
    {
        var p = (page ?? "").Trim().ToLowerInvariant().TrimStart('/');
        return p switch
        {
            "home" => "//home",
            "chat" => "//chat",
            "files" => "//files",
            "settings" or "config" => "//settings",
            "session" or "sessions" or "history" => "sessions",
            "panel" or "menu" or "side" or "命令" => "panel",
            "model" or "modelpicker" or "模型" => "modelpicker",
            "provider" or "providers" or "models" or "供应商" => "models",
            "sync" or "gitsync" or "同步" => "gitsync",
            "about" or "关于" => "about",
            "editor" or "编辑器" => "editor",
            _ => null,
        };
    }

    /// <summary>可用页面名提示（无参时回显）。</summary>
    public const string Usage = "可用：/topage home|chat|files|settings|sessions|panel|modelpicker|providers|gitsync|about|editor";
}

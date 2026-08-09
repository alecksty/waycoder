namespace CoreCoderSharp;

/// <summary>
/// 全局常量 —— 应用名称、版本号、开发者信息等，全项目统一引用。
/// </summary>
public static class Global
{
    // ── 应用 ──
    /// <summary>应用品牌名（英文）</summary>
    public const string AppName = "WayCoder";
    /// <summary>应用中文名</summary>
    public const string AppNameCN = "道码";
    /// <summary>应用全称</summary>
    public const string AppFullName = "WayCoder 道码 · 中文版易用编程智能体";
    /// <summary>版本号</summary>
    public const string Version = "v0.20.0";
    /// <summary>应用名 + 版本号</summary>
    public static string AppNameVersion => $"{AppName} {Version} ({AppNameCN})";

    // ── 公司 / 开发者 ──
    /// <summary>公司名称</summary>
    public const string Company = "施探宇";
    /// <summary>开发者</summary>
    public const string Developer = "施探宇 (aleck)";
    /// <summary>开发者邮箱</summary>
    public const string Email = "aleckstygit@outlook.com";
    /// <summary>联系电话</summary>
    public const string Phone = "+86 138-xxxx-xxxx";
    /// <summary>地址</summary>
    public const string Address = "中国 · 天津";

    // ── 仓库 ──
    /// <summary>Git 仓库地址</summary>
    public const string RepoUrl = "https://gitee.com/aleckstygit/my-coder";
    /// <summary>开源协议</summary>
    public const string License = "MIT";
}

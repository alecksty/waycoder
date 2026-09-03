using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace WayCoder.UI.Gui;

/// <summary>
/// GUI 配色唯一代码入口。真正的颜色真源是 <c>App.axaml</c> 的 ThemeDictionaries（Dark/Light 键同名），
/// 切换主题 = App.ToggleTheme() 设置 RequestedThemeVariant，各键自动按当前变体解析。
/// 本类经 TryGetResource 读当前主题；<see cref="Fallback"/> 是资源缺失时的深色兜底，集中于此。
/// .cs 逻辑不写 <c>Color.Parse("#…")</c> 字面量，一概走本类。语法/状态/diff 色随主题切换（深浅各一套，浅底可读）。
/// </summary>
public static class GuiColors
{
    private static readonly ConcurrentDictionary<string, IBrush> BrushCache = new();
    private static readonly ConcurrentDictionary<string, Color> ColorCache = new();

    /// <summary>主题切换时清空缓存，下次按新主题重新解析。</summary>
    public static void Invalidate()
    {
        BrushCache.Clear();
        ColorCache.Clear();
    }

    /// <summary>深色兜底色（App.axaml 未加载/键缺失时使用）。与 ThemeDictionaries.Dark 值一致。</summary>
    private static readonly Dictionary<string, string> Fallback = new()
    {
        ["WindowBgBrush"]="#0f1117", ["PanelBgBrush"]="#171a23", ["Panel2BgBrush"]="#1d2230",
        ["BorderBrush"]="#262b3a", ["TextBrush"]="#e6e8ee", ["DimTextBrush"]="#8b93a7",
        ["AccentBrush"]="#4f8cff", ["UserBubbleBgBrush"]="#1f3a5f", ["ToolBubbleBgBrush"]="#2a2416",
        ["DangerBrush"]="#3a2a2a", ["SuccessBrush"]="#3fb950",
        ["ButtonPrimaryBg"]="#2f6bff", ["ButtonSecondaryBg"]="#5b6472", ["ButtonDangerBg"]="#d73a49",
        ["ButtonText"]="#ffffff",
        ["StatusRunning"]="#4f8cff", ["StatusSuccess"]="#3fb950", ["StatusDanger"]="#e5534b",
        ["StatusWarning"]="#e8b34b", ["StatusNeutral"]="#8b93a7",
        ["DiffAdd"]="#3fb950", ["DiffDel"]="#e5534b", ["DiffNormal"]="#c9d1d9", ["DiffHdr"]="#58a6ff",
        ["SyntaxPlain"]="#c9d1d9", ["SyntaxRed"]="#ff7b72", ["SyntaxGreen"]="#3fb950",
        ["SyntaxYellow"]="#d29922", ["SyntaxCyan"]="#39c5cf", ["SyntaxBlue"]="#58a6ff",
        ["SyntaxMagenta"]="#bc8cff", ["SyntaxDim"]="#6e7681", ["SyntaxStr"]="#a5d6ff",
        ["SyntaxNum"]="#79c0ff", ["SyntaxFn"]="#d2a8ff", ["SyntaxCom"]="#7d8590",
        ["CodeBg"]="#1d2230", ["CodeBlockBg"]="#161b22", ["CodeText"]="#c9d1d9",
        ["IdleText"]="#cccccc",
        ["CaretBrush"]="#4f8cff", ["SelectionBrush"]="#33518c",
        ["DiagErrorBg"]="#6e2222", ["DiagWarnBg"]="#6e5c2e",
    };

    /// <summary>读主题画刷（SolidColorBrush）。未加载主题/缺失返回 null。</summary>
    private static SolidColorBrush? Resolve(string key)
    {
        if (Application.Current is not { } app) return null;
        // 必须显式传当前变体：传 null 会落到 ThemeVariant.Default，而 ThemeDictionaries 无 "Default" 键 → 取不到 → 回退深色。
        // 用 App.IsDark（ToggleTheme/Initialize 维护）决定深浅；经 Application 资源管线与 {DynamicResource} 同路径命中 ThemeDictionaries。
        var theme = App.IsDark ? ThemeVariant.Dark : ThemeVariant.Light;
        if (app.TryGetResource(key, theme, out var v) && v is SolidColorBrush sb)
            return sb;
        return null;
    }

    /// <summary>读主题色（Color）。缺失回退深色兜底。</summary>
    public static Color ColorOf(string key)
    {
        if (ColorCache.TryGetValue(key, out var cached)) return cached;
        var c = Resolve(key)?.Color ?? Color.Parse(Fallback.TryGetValue(key, out var h) ? h : "#000000");
        ColorCache[key] = c;
        return c;
    }

    /// <summary>读主题画刷（IBrush）。缺失回退深色兜底 SolidColorBrush。</summary>
    public static IBrush BrushOf(string key)
    {
        if (BrushCache.TryGetValue(key, out var cached)) return cached;
        var b = Resolve(key) ?? new SolidColorBrush(Color.Parse(Fallback.TryGetValue(key, out var h) ? h : "#000000"));
        BrushCache[key] = b;
        return b;
    }

    // ── Core ──
    public static IBrush WindowBg => BrushOf("WindowBgBrush");
    public static IBrush PanelBg => BrushOf("PanelBgBrush");
    public static IBrush Panel2Bg => BrushOf("Panel2BgBrush");
    public static IBrush Border => BrushOf("BorderBrush");
    public static IBrush Text => BrushOf("TextBrush");
    public static IBrush DimText => BrushOf("DimTextBrush");
    public static IBrush Accent => BrushOf("AccentBrush");
    public static IBrush DangerBg => BrushOf("DangerBrush");
    public static IBrush Success => BrushOf("SuccessBrush");
    public static Color TextColor => ColorOf("TextBrush");
    public static Color DimColor => ColorOf("DimTextBrush");
    public static Color AccentColor => ColorOf("AccentBrush");
    public static Color BorderColor => ColorOf("BorderBrush");
    public static Color CodeBlockBgColor => ColorOf("CodeBlockBg");

    // ── 按钮 ──
    public static IBrush ButtonPrimary => BrushOf("ButtonPrimaryBg");
    public static IBrush ButtonSecondary => BrushOf("ButtonSecondaryBg");
    public static IBrush ButtonDanger => BrushOf("ButtonDangerBg");
    public static IBrush ButtonText => BrushOf("ButtonText");

    // ── 状态 ──
    public static IBrush StatusRunning => BrushOf("StatusRunning");
    public static IBrush StatusSuccess => BrushOf("StatusSuccess");
    public static IBrush StatusDanger => BrushOf("StatusDanger");
    public static IBrush StatusWarning => BrushOf("StatusWarning");
    public static IBrush StatusNeutral => BrushOf("StatusNeutral");

    // ── diff / 上下文 ──
    public static IBrush DiffAdd => BrushOf("DiffAdd");
    public static IBrush DiffDel => BrushOf("DiffDel");
    public static IBrush DiffNormal => BrushOf("DiffNormal");
    public static IBrush DiffHdr => BrushOf("DiffHdr");

    // ── 语法 ──
    public static IBrush SyntaxPlain => BrushOf("SyntaxPlain");
    public static IBrush SyntaxRed => BrushOf("SyntaxRed");
    public static IBrush SyntaxGreen => BrushOf("SyntaxGreen");
    public static IBrush SyntaxYellow => BrushOf("SyntaxYellow");
    public static IBrush SyntaxCyan => BrushOf("SyntaxCyan");
    public static IBrush SyntaxBlue => BrushOf("SyntaxBlue");
    public static IBrush SyntaxMagenta => BrushOf("SyntaxMagenta");
    public static IBrush SyntaxDim => BrushOf("SyntaxDim");
    public static IBrush SyntaxStr => BrushOf("SyntaxStr");
    public static IBrush SyntaxNum => BrushOf("SyntaxNum");
    public static IBrush SyntaxFn => BrushOf("SyntaxFn");
    public static IBrush SyntaxCom => BrushOf("SyntaxCom");

    // ── 代码 ──
    public static IBrush CodeBg => BrushOf("CodeBg");
    public static IBrush CodeBlockBg => BrushOf("CodeBlockBg");
    public static IBrush CodeText => BrushOf("CodeText");

    // ── 杂项 ──
    public static IBrush IdleText => BrushOf("IdleText");

    // ── 编辑器 ──
    public static IBrush Caret => BrushOf("CaretBrush");
    public static IBrush Selection => BrushOf("SelectionBrush");
    public static IBrush DiagErrorBg => BrushOf("DiagErrorBg");
    public static IBrush DiagWarnBg => BrushOf("DiagWarnBg");

    /// <summary>diff 行类型 → 前景画刷（'-' 减 '+' 加 ' ' 普通）。</summary>
    public static IBrush DiffFor(char kind) => kind switch
    {
        '+' => DiffAdd,
        '-' => DiffDel,
        _ => DiffNormal,
    };
}

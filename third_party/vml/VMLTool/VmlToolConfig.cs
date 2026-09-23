using System.Xml.Linq;
using VMLPlugins;

namespace VMLTool;

/// <summary>
/// vmltool.config.xml 配置加载 (AOT 兼容: 使用 XElement 替代 XmlSerializer)
/// 优先级: CLI 参数 > 配置文件 > 代码默认值
/// </summary>
public class VmlToolConfig
{
    private static VmlToolConfig? _instance;
    public static VmlToolConfig Instance => _instance ??= Load();

    // ═══ 运行模式 ═══
    public string Mode { get; set; } = "mcu";

    // ═══ 数值处理 ═══
    public string Float32 { get; set; } = "hard";
    public string Float64 { get; set; } = "hard";
    public string Int64 { get; set; } = "hard";

    // ═══ 优化与调试 ═══
    public int OptimizationLevel { get; set; }
    public int WarningLevel { get; set; }
    public bool WarningsAsErrors { get; set; }
    public bool Debug { get; set; }
    public bool GcSections { get; set; } = true;

    // ═══ 内存 ═══
    public string MemLevel { get; set; } = "RAM_M";
    public int StackSize { get; set; }

    // ═══ 链接 ═══
    public bool StaticLink { get; set; }
    public bool SharedLink { get; set; }
    public bool AutoIncludeStdLib { get; set; } = true;
    public bool UseSharedLibrary { get; set; } = true;

    // ═══ 输出 ═══
    public string OutputFormat { get; set; } = "";
    public bool SourceComment { get; set; } = true;
    public bool SaveTemps { get; set; }

    // ═══ 路径 ═══
    public string SharedPath { get; set; } = "Lib/shared";
    public string LibPath { get; set; } = "Lib";
    public string OutputPath { get; set; } = "";
    public List<string> IncludePaths { get; set; } = new();
    public List<string> Defines { get; set; } = new();

    // ═══ 语言标准 / 方言 ═══
    public string LanguageStandard { get; set; } = "";
    public string BasicDialect { get; set; } = "qbasic";
    /// <summary>BASIC 图形语句后端：ui（默认，宿主 ui_* 图元）/ pcgfx（老的写 DOS 显存 0xA0000）</summary>
    public string BasicGraphics { get; set; } = "ui";
    public string PascalDialect { get; set; } = "turbo";
    public List<string> CIncludePaths { get; set; } = new();

    // ═══ 库 ═══
    public List<string> DefaultLibs { get; set; } = new();
    public List<LangConfig> Languages { get; set; } = new();

    // ═══ 内部 ═══
    private string _configDir = "";

    // ═══════════════════════════════════════════

    /// <summary>
    /// 加载 vmltool.config.xml（三层搜索）:
    /// 1. CLI --config 指定路径
    /// 2. 当前工作目录 ./vmltool.config.xml
    /// 3. $VML_HOME/vmltool.config.xml
    /// </summary>
    public static VmlToolConfig Load(string? cliConfigPath = null)
    {
        if (_instance != null) return _instance;

        // 构建三层搜索路径（去重）
        var searchPaths = new List<string>();

        // 1. CLI 指定路径
        if (!string.IsNullOrEmpty(cliConfigPath))
            searchPaths.Add(Path.GetFullPath(cliConfigPath));

        // 2. 当前工作目录
        searchPaths.Add(Path.Combine(Directory.GetCurrentDirectory(), "vmltool.config.xml"));

        // 3. $VML_HOME (后备 $VML_TOOL_PATH)
        var vmlHome = Environment.GetEnvironmentVariable("VML_HOME")
            ?? Environment.GetEnvironmentVariable("VML_TOOL_PATH");
        if (!string.IsNullOrEmpty(vmlHome))
            searchPaths.Add(Path.Combine(vmlHome, "vmltool.config.xml"));

        // 搜索
        foreach (var path in searchPaths.Distinct())
        {
            if (File.Exists(path))
            {
                try
                {
                    var xml = XElement.Load(path);
                    _instance = Parse(xml);
                    _instance._configDir = Path.GetDirectoryName(Path.GetFullPath(path)) ?? "";
                    return _instance;
                }
                catch { /* 跳过损坏的配置文件 */ }
            }
        }
        _instance = new VmlToolConfig { _configDir = Directory.GetCurrentDirectory() };
        return _instance;
    }

    private static VmlToolConfig Parse(XElement root)
    {
        var c = new VmlToolConfig();
        c.Mode = Val(root, "Mode", "mcu");
        c.Float32 = Val(root, "Float32", "hard");
        c.Float64 = Val(root, "Float64", "hard");
        c.Int64 = Val(root, "Int64", "hard");
        c.OptimizationLevel = IntVal(root, "OptimizationLevel");
        c.WarningLevel = IntVal(root, "WarningLevel");
        c.WarningsAsErrors = BoolVal(root, "WarningsAsErrors");
        c.Debug = BoolVal(root, "Debug");
        c.GcSections = BoolVal(root, "GcSections", true);
        c.MemLevel = Val(root, "MemoryLevel", "RAM_M");
        c.StackSize = IntVal(root, "StackSize");
        c.StaticLink = BoolVal(root, "StaticLink");
        c.SharedLink = BoolVal(root, "SharedLink");
        c.AutoIncludeStdLib = BoolVal(root, "AutoIncludeStdLib", true);
        c.UseSharedLibrary = BoolVal(root, "UseSharedLibrary", true);
        c.OutputFormat = Val(root, "OutputFormat");
        c.SourceComment = BoolVal(root, "SourceComment", true);
        c.SaveTemps = BoolVal(root, "SaveTemps");
        c.SharedPath = Val(root, "SharedPath", "Lib/shared");
        c.LibPath = Val(root, "LibPath", "Lib");
        c.OutputPath = Val(root, "OutputPath");
        c.LanguageStandard = Val(root, "LanguageStandard");
        c.BasicDialect = Val(root, "BasicDialect", "qbasic");
        c.BasicGraphics = Val(root, "BasicGraphics", "ui");
        c.PascalDialect = Val(root, "PascalDialect", "turbo");

        // 列表元素
        c.IncludePaths = ListVal(root, "IncludePaths", "Path");
        c.Defines = ListVal(root, "Defines", "Define");
        c.CIncludePaths = ListVal(root, "CIncludePaths", "Path");
        c.DefaultLibs = ListVal(root, "DefaultLibs", "Lib");

        // 语言配置
        var langEl = root.Element("Languages");
        if (langEl != null)
        {
            foreach (var le in langEl.Elements("Language"))
            {
                c.Languages.Add(new LangConfig
                {
                    Name = le.Attribute("Name")?.Value ?? "",
                    Libs = le.Attribute("Libs")?.Value ?? ""
                });
            }
        }
        return c;
    }

    // ═══ 辅助 ═══
    private static string Val(XElement root, string name, string def = "") =>
        root.Element(name)?.Value ?? def;

    private static int IntVal(XElement root, string name) =>
        int.TryParse(root.Element(name)?.Value, out var v) ? v : 0;

    private static bool BoolVal(XElement root, string name, bool def = false) =>
        bool.TryParse(root.Element(name)?.Value, out var v) ? v : def;

    private static List<string> ListVal(XElement root, string parent, string child)
    {
        var r = new List<string>();
        var p = root.Element(parent);
        if (p == null) return r;
        foreach (var e in p.Elements(child))
        {
            var v = e.Value?.Trim();
            if (!string.IsNullOrEmpty(v)) r.Add(v);
        }
        return r;
    }

    // ═══════════════════════════════════════════

    public void ApplyTo(CommandLineOptions options, string? language = null)
    {
        // 运行模式
        if (options.TargetMode == TargetMode.MCU && Mode?.ToLower() == "os")
            options.TargetMode = TargetMode.OS;

        // 数值模式
        options.Float32Mode = NumericHelper.ApplyFloat32(options.Float32Mode, Float32);
        options.Float64Mode = NumericHelper.ApplyFloat64(options.Float64Mode, Float64);
        options.Int64Mode = NumericHelper.ApplyInt64(options.Int64Mode, Int64);

        // 优化
        if (options.OptimizationLevel == 0 && OptimizationLevel != 0)
            options.OptimizationLevel = OptimizationLevel;
        if (options.WarningLevel == 0 && WarningLevel != 0)
            options.WarningLevel = WarningLevel;
        if (!options.WarningsAsErrors && WarningsAsErrors)
            options.WarningsAsErrors = true;
        if (!options.DebugMode && Debug)
            options.DebugMode = true;
        if (options.GcSections && !GcSections)
            options.GcSections = false;

        // 内存
        ApplyMemory(options);
        if (options.StackSize == null && StackSize > 0)
            options.StackSize = StackSize;

        // 链接
        if (!options.StaticLink && StaticLink) options.StaticLink = true;
        if (!options.SharedLink && SharedLink) options.SharedLink = true;
        if (options.AutoIncludeStdLib && !AutoIncludeStdLib) options.AutoIncludeStdLib = false;
        if (options.UseSharedLibrary && !UseSharedLibrary) options.UseSharedLibrary = false;

        // 输出
        if (string.IsNullOrEmpty(options.OutputFormat) && !string.IsNullOrEmpty(OutputFormat))
            options.OutputFormat = OutputFormat;
        if (options.SourceComment && !SourceComment) options.SourceComment = false;
        if (!options.SaveTemps && SaveTemps) options.SaveTemps = true;

        // 路径 (相对于配置文件目录)
        ApplyPaths(options);
        foreach (var p in IncludePaths)
            if (!options.IncludePaths.Contains(p)) options.IncludePaths.Add(p);
        foreach (var d in Defines)
            if (!options.Defines.Contains(d)) options.Defines.Add(d);

        // 语言标准 / 方言
        if (string.IsNullOrEmpty(options.LanguageStandard) && !string.IsNullOrEmpty(LanguageStandard))
            options.LanguageStandard = LanguageStandard;
        if (string.IsNullOrEmpty(options.BasicType) && !string.IsNullOrEmpty(BasicDialect))
            options.BasicType = BasicDialect;
        // BASIC 图形后端：命令行 --basicgfx 优先，其次配置文件 <BasicGraphics>，默认 ui
        if (string.IsNullOrEmpty(options.BasicGfx) && !string.IsNullOrEmpty(BasicGraphics))
            options.BasicGfx = BasicGraphics;
        if (string.IsNullOrEmpty(options.PascalType) && !string.IsNullOrEmpty(PascalDialect))
            options.PascalType = PascalDialect;

        // C 包含路径
        foreach (var p in CIncludePaths)
            if (!options.IncludePaths.Contains(p)) options.IncludePaths.Add(p);

        // 自动链接库 (配置)
        foreach (var lib in ResolveLibs(language ?? "default"))
            if (!options.LibraryPaths.Contains(lib)) options.LibraryPaths.Add(lib);

        // -l 参数手动指定库
        foreach (var libName in options.LibraryNames)
        {
            var path = ResolveLibPath(libName, _configDir);
            if (path != null && !options.LibraryPaths.Contains(path))
                options.LibraryPaths.Add(path);
        }
    }

    private void ApplyMemory(CommandLineOptions options)
    {
        if (options.MemoryLevel != MemoryLevel.RAM_M) return;
        options.MemoryLevel = MemLevel?.ToUpper() switch
        {
            "RAM_K" => MemoryLevel.RAM_K,
            "RAM_M" => MemoryLevel.RAM_M,
            "RAM_G" => MemoryLevel.RAM_G,
            _ => options.MemoryLevel
        };
    }

    private void ApplyPaths(CommandLineOptions options)
    {
        // 仅添加已解析的库文件，不添加目录路径（避免全库链接）
        // 库文件通过 ResolveLibs 自动解析
    }

    public LangConfig GetLangConfig(string lang)
    {
        if (string.IsNullOrEmpty(lang)) lang = "default";
        return Languages.FirstOrDefault(l =>
            string.Equals(l.Name, lang, StringComparison.OrdinalIgnoreCase))
            ?? new LangConfig { Name = lang };
    }

    public List<string> ResolveLibs(string lang, string? baseDir = null)
    {
        var result = new List<string>();
        baseDir ??= _configDir;
        foreach (var lib in DefaultLibs)
        {
            var path = ResolveLibPath(lib, baseDir);
            if (path != null && !result.Contains(path)) result.Add(path);
        }
        var lc = GetLangConfig(lang);
        var libs = lc.Libs?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        foreach (var lib in libs)
        {
            var trimmed = lib.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var path = ResolveLibPath(trimmed, baseDir);
            if (path != null && !result.Contains(path)) result.Add(path);
        }
        return result;
    }

    private string? ResolveLibPath(string libName, string baseDir)
    {
        if (Path.IsPathRooted(libName) && File.Exists(libName)) return libName;
        if (!libName.EndsWith(".vml", StringComparison.OrdinalIgnoreCase))
            libName += ".vml";
        foreach (var dir in new[] { SharedPath, LibPath })
        {
            if (string.IsNullOrEmpty(dir)) continue;
            var fullPath = Path.GetFullPath(Path.Combine(baseDir, dir, libName));
            if (File.Exists(fullPath)) return fullPath;
        }
        return null;
    }
}

public class LangConfig
{
    public string Name { get; set; } = "";
    public string Libs { get; set; } = "";
}

internal static class NumericHelper
{
    public static Float32Mode ApplyFloat32(Float32Mode cur, string cfg) =>
        string.IsNullOrEmpty(cfg) || cur != Float32Mode.Hard ? cur :
        cfg.ToLower() switch { "soft" => Float32Mode.Soft, "none" => Float32Mode.None, _ => cur };

    public static Float64Mode ApplyFloat64(Float64Mode cur, string cfg) =>
        string.IsNullOrEmpty(cfg) || cur != Float64Mode.Hard ? cur :
        cfg.ToLower() switch { "soft" => Float64Mode.Soft, "none" => Float64Mode.None, _ => cur };

    public static Int64Mode ApplyInt64(Int64Mode cur, string cfg) =>
        string.IsNullOrEmpty(cfg) || cur != Int64Mode.Hard ? cur :
        cfg.ToLower() switch { "soft" => Int64Mode.Soft, "none" => Int64Mode.None, _ => cur };
}

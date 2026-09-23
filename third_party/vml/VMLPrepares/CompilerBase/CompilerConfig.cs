using System;
using System.Collections.Generic;
using VMLPlugins;

namespace CompilerBase
{
    /// <summary>
    /// 编译器统一配置 — 所有参数的集中管理
    /// 提供 SetConfig(key, value) 字符串索引器
    /// </summary>
    public class CompilerConfig
    {
        // ====== 路径参数 ======
        public List<string> IncludePaths { get; set; } = new();
        public List<string> LibraryPaths { get; set; } = new();
        public string? OutputPath { get; set; }

        // ====== 编译选项（原 CompilerOptions 字段） ======
        public List<string> Defines { get; set; } = new();
        public List<string> Undefines { get; set; } = new();
        public int WarningLevel { get; set; }
        public bool WarningsAsErrors { get; set; }
        public bool DebugMode { get; set; }
        public bool DumpMode { get; set; }
        public bool PrepareLogMode { get; set; }
        public bool DumpPreprocess { get; set; }
        public string? LanguageStandard { get; set; }
        public TargetMode TargetMode { get; set; } = TargetMode.MCU;
        public bool IsMCU => TargetMode == TargetMode.MCU;
        public MemoryLevel MemoryLevel { get; set; } = MemoryLevel.RAM_M;

        private int? _customStackSize;
        public int StackSize
        {
            get => _customStackSize ?? TargetConfig.GetDefaultStackSize(MemoryLevel);
            set => _customStackSize = value > 0 ? value : null;
        }

        public Float32Mode Float32Mode { get; set; } = Float32Mode.Hard;
        public Float64Mode Float64Mode { get; set; } = Float64Mode.Hard;
        public Int64Mode Int64Mode { get; set; } = Int64Mode.Hard;
        public bool SourceComment { get; set; } = true;

        /// <summary>
        /// BASIC 图形语句的后端：Ui（默认，宿主 ui_* 图元）/ PcGfx（老的 DOS 显存 0xA0000）。
        /// 旋钮键名 `basicgfx`；CLI 是 `--basicgfx`、配置文件是 `&lt;BasicGraphics&gt;`。
        /// </summary>
        public BasicGraphics BasicGraphics { get; set; } = BasicGraphics.Ui;

        // ====== 库链接选项 ======
        public bool AutoLinkStdLib { get; set; } = false;
        public bool UseSharedLibrary { get; set; } = true;

        // ====== 字符串索引器 ======

        public void SetConfig(string key, object value)
        {
            switch (key.ToLowerInvariant())
            {
                // 路径类
                case "includepaths":
                    IncludePaths = (value as List<string>) ?? new List<string>();
                    break;
                case "libpaths":
                case "librarypaths":
                    LibraryPaths = (value as List<string>) ?? new List<string>();
                    break;
                case "outputpath":
                case "output":
                    OutputPath = value?.ToString();
                    break;

                // 模式类
                case "mode":
                case "targetmode":
                    if (value is TargetMode tm) TargetMode = tm;
                    else if (value is string sm && Enum.TryParse<TargetMode>(sm, true, out var pm)) TargetMode = pm;
                    break;
                case "ram":
                case "memorylevel":
                    if (value is MemoryLevel ml) MemoryLevel = ml;
                    else if (value is string sml) MemoryLevel = TargetConfig.ParseMemoryLevel(sml);
                    break;
                case "stacksize":
                case "stack":
                    StackSize = Convert.ToInt32(value);
                    break;

                // 数值模式类
                case "int64":
                    if (value is Int64Mode i64) Int64Mode = i64;
                    else if (value is string si64 && Enum.TryParse<Int64Mode>(si64, true, out var pi64)) Int64Mode = pi64;
                    break;
                case "float64":
                    if (value is Float64Mode f64) Float64Mode = f64;
                    else if (value is string sf64 && Enum.TryParse<Float64Mode>(sf64, true, out var pf64)) Float64Mode = pf64;
                    break;
                case "float32":
                    if (value is Float32Mode f32) Float32Mode = f32;
                    else if (value is string sf32 && Enum.TryParse<Float32Mode>(sf32, true, out var pf32)) Float32Mode = pf32;
                    break;

                // 开关类
                case "debug":
                case "debugmode":
                    DebugMode = Convert.ToBoolean(value);
                    break;
                case "dump":
                case "dumpmode":
                    DumpMode = Convert.ToBoolean(value);
                    break;
                case "preparelog":
                case "preparelogmode":
                    PrepareLogMode = Convert.ToBoolean(value);
                    break;
                case "dumppreprocess":
                    DumpPreprocess = Convert.ToBoolean(value);
                    break;
                case "warnings":
                case "warninglevel":
                    WarningLevel = Convert.ToInt32(value);
                    break;
                case "warningsaserrors":
                    WarningsAsErrors = Convert.ToBoolean(value);
                    break;
                case "sourcecomment":
                    SourceComment = Convert.ToBoolean(value);
                    break;
                case "basicgfx":
                case "basicgraphics":
                    if (value is BasicGraphics bg) BasicGraphics = bg;
                    else if (value is string sbg && Enum.TryParse<BasicGraphics>(sbg, true, out var pbg)) BasicGraphics = pbg;
                    break;
                case "autolinkstdlib":
                    AutoLinkStdLib = Convert.ToBoolean(value);
                    break;
                case "usesharedlibrary":
                    UseSharedLibrary = Convert.ToBoolean(value);
                    break;

                // 标准类
                case "languagestandard":
                case "std":
                    LanguageStandard = value?.ToString();
                    break;

                // 宏定义
                case "defines":
                    Defines = (value as List<string>) ?? new List<string>();
                    break;
                case "undefines":
                    Undefines = (value as List<string>) ?? new List<string>();
                    break;
            }
        }

        public T? GetConfig<T>(string key)
        {
            object? result = key.ToLowerInvariant() switch
            {
                "includepaths" => IncludePaths,
                "libpaths" or "librarypaths" => LibraryPaths,
                "outputpath" or "output" => OutputPath,
                "mode" or "targetmode" => TargetMode,
                "ram" or "memorylevel" => MemoryLevel,
                "stacksize" or "stack" => StackSize,
                "int64" => Int64Mode,
                "float64" => Float64Mode,
                "float32" => Float32Mode,
                "debug" or "debugmode" => DebugMode,
                "dump" or "dumpmode" => DumpMode,
                "warnings" or "warninglevel" => WarningLevel,
                "warningsaserrors" => WarningsAsErrors,
                "sourcecomment" => SourceComment,
                "autolinkstdlib" => AutoLinkStdLib,
                "usesharedlibrary" => UseSharedLibrary,
                "languagestandard" or "std" => LanguageStandard,
                "defines" => Defines,
                "undefines" => Undefines,
                _ => null,
            };
            if (result is T typed) return typed;
            return default;
        }

        /// <summary>
        /// 转换为 CompilerOptions（向后兼容 CompilerOptionsContext）
        /// </summary>
        public CompilerOptions ToCompilerOptions()
        {
            return new CompilerOptions
            {
                Defines = Defines,
                Undefines = Undefines,
                WarningLevel = WarningLevel,
                WarningsAsErrors = WarningsAsErrors,
                DebugMode = DebugMode,
                DumpMode = DumpMode,
                PrepareLogMode = PrepareLogMode,
                DumpPreprocess = DumpPreprocess,
                LanguageStandard = LanguageStandard,
                TargetMode = TargetMode,
                MemoryLevel = MemoryLevel,
                StackSize = _customStackSize ?? 0,
                Float32Mode = Float32Mode,
                Float64Mode = Float64Mode,
                Int64Mode = Int64Mode,
                SourceComment = SourceComment,
                BasicGraphics = BasicGraphics,
            };
        }
    }
}

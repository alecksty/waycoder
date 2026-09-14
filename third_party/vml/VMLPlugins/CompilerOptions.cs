namespace VMLPlugins
{
    /// <summary>Pascal 方言枚举</summary>
    public enum PascalDialect
    {
        Turbo,       // 默认 — Borland Turbo Pascal 7.0
        Delphi,      // Delphi/Object Pascal (OOP)
        FreePascal,  // Free Pascal (FPC)
        IsoPascal,   // ISO 7185 Standard Pascal
        UcsdPascal,  // UCSD Pascal
        Oberon,      // Oberon (Wirth)
    }

    /// <summary>BASIC 方言枚举</summary>
    public enum BasicDialect
    {
        QBasic,      // 默认 — Microsoft QBasic/QuickBASIC 兼容
        TurboBasic,  // Borland Turbo Basic
        FreeBasic,   // FreeBasic 开源编译器
        TrueBasic,   // True BASIC (ANSI/ISO 标准)
        PureBasic,   // PureBasic (商业/游戏)
        ChipBasic,   // ChipBasic (MCU 优化)
        MiniBasic,   // MiniBasic (最小教学子集)
        GwBasic,     // GW-BASIC (MS-DOS 始祖, 行号必选, 2020微软开源)
        PowerBasic,  // PowerBASIC (TurboBasic 正统续作, 商业品质)
        VisualBasic  // Visual Basic 6 (事件驱动, GUI, 海量遗留代码)
    }

    /// <summary>
    /// 编译器选项 DTO — 内部使用，由 CompilerConfig.ToCompilerOptions() 生成。
    /// 新代码应使用 CompilerBase.CompilerConfig + CompilerBase.SetConfig() 接口。
    /// </summary>
    public class CompilerOptions
    {
        public List<string> Defines { get; set; } = new();
        public List<string> Undefines { get; set; } = new();
        public int WarningLevel { get; set; } = 0;
        public bool WarningsAsErrors { get; set; }
        public bool DebugMode { get; set; }
        public bool DumpMode { get; set; }
        public bool PrepareLogMode { get; set; }
        public bool DumpPreprocess { get; set; }  // --dump-preprocess: 输出预处理中间状态
        public string? LanguageStandard { get; set; }

        /// <summary>编译目标模式: MCU (跳过OS依赖特性) / OS (全部特性)</summary>
        public TargetMode TargetMode { get; set; } = TargetMode.MCU;

        /// <summary>是否为 MCU 模式 (便捷判断)</summary>
        public bool IsMCU => TargetMode == TargetMode.MCU;

        /// <summary>内存级别: RAM_K / RAM_M / RAM_G</summary>
        public MemoryLevel MemoryLevel { get; set; } = MemoryLevel.RAM_M;

        /// <summary>获取默认内存大小（字节）</summary>
        public int MemorySize => TargetConfig.GetDefaultMemorySize(MemoryLevel);

        private int? _customStackSize;
        /// <summary>栈大小（字节），0=自动根据 --ram 分配</summary>
        public int StackSize
        {
            get => _customStackSize ?? TargetConfig.GetDefaultStackSize(MemoryLevel);
            set => _customStackSize = value > 0 ? value : null;
        }

    /// <summary>32位浮点处理模式 (hard/soft/none)</summary>
    public Float32Mode Float32Mode { get; set; } = Float32Mode.Hard;

    /// <summary>64位浮点处理模式 (hard/soft/none) — 默认软模拟</summary>
    public Float64Mode Float64Mode { get; set; } = Float64Mode.Soft;

    /// <summary>64位整数处理模式 (hard/soft/none) — 默认库模拟</summary>
    public Int64Mode Int64Mode { get; set; } = Int64Mode.Soft;

    /// <summary>是否在生成的 VML 汇编中包含源码行注释（默认开启）</summary>
    public bool SourceComment { get; set; } = true;
    /// <summary>BASIC 语言方言 (默认 QBasic)</summary>
    public BasicDialect BasicDialect { get; set; } = BasicDialect.QBasic;
    /// <summary>Pascal 语言方言 (默认 Turbo Pascal)</summary>
    public PascalDialect PascalDialect { get; set; } = PascalDialect.Turbo;
    }
}

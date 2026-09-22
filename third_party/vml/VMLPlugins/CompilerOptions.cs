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

    /// <summary>
    /// 64位浮点处理模式 (hard/soft/none) —— **默认 Hard**（硬件双精度：DADD/DSUB/DMUL/DDIV）。
    ///
    /// ⚠ 这里原本是 `Soft`，而 `VMLPrepares/CompilerBase/CompilerConfig.cs` 是 `Hard`，
    ///   **两处默认值不一致**：走 `CompilerConfig.SyncToContext()` 的路径（22 个前端全都走）
    ///   拿到的其实是 Hard，只有**直接 new `CompilerOptions`** 的调用方才吃到 Soft。
    ///   两套默认值等于"同一件事两处实现"。现按用户 2026-09-22 的决定**统一为 Hard**。
    ///   软模拟那条路要靠 `softdouble.vml`，本平台本来就没链它。
    /// </summary>
    public Float64Mode Float64Mode { get; set; } = Float64Mode.Hard;

    /// <summary>
    /// 64位整数处理模式 (hard/soft/none) —— **默认 Hard**（硬件 64 位：ADDL/SUBL/MULL/DIVL/MODL）。
    ///
    /// ⚠ 理由同 <see cref="Float64Mode"/>：与 `CompilerConfig` 的默认值统一。
    ///   实测 Hard 这条路**本来就是好的**（`long` 的加/减/乘/除、比较、窄化全对），
    ///   而 Soft 那条要靠 `softint64.vml` —— 本平台没链它。**默认选那条走不通的路没有道理。**
    /// </summary>
    public Int64Mode Int64Mode { get; set; } = Int64Mode.Hard;

    /// <summary>是否在生成的 VML 汇编中包含源码行注释（默认开启）</summary>
    public bool SourceComment { get; set; } = true;
    /// <summary>BASIC 语言方言 (默认 QBasic)</summary>
    public BasicDialect BasicDialect { get; set; } = BasicDialect.QBasic;
    /// <summary>Pascal 语言方言 (默认 Turbo Pascal)</summary>
    public PascalDialect PascalDialect { get; set; } = PascalDialect.Turbo;
    }
}

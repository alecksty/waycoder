namespace VMLPlugins
{
    /// <summary>
    /// 编译目标模式
    /// MCU = 单片机模式（跳过 async/await/Thread/goroutine/异常/反射 等 OS 依赖特性）
    /// OS  = 操作系统模式（全部特性可用，为将来带 OS 硬件预留）
    /// </summary>
    public enum TargetMode
    {
        /// <summary>单片机模式（默认）— 只生成 MCU 兼容代码</summary>
        MCU = 0,
        /// <summary>操作系统模式 — 全部语言特性生效</summary>
        OS = 1,
    }

    /// <summary>
    /// 内存级别（决定运行时可用的内存大小）
    /// </summary>
    public enum MemoryLevel
    {
        /// <summary>KB级别 — 小容量单片机（典型 2KB~64KB，如 8051/PIC/AVR）</summary>
        RAM_K = 0,
        /// <summary>MB级别 — 中容量单片机（典型 64KB~1MB，如 ARM Cortex-M/STM32）</summary>
        RAM_M = 1,
        /// <summary>GB级别 — 大容量系统（如 x86/RISC-V 带 DDR）</summary>
        RAM_G = 2,
    }

    /// <summary>
    /// 32位浮点处理模式 (float)
    /// </summary>
    public enum Float32Mode
    {
        /// <summary>硬件浮点（默认）— 使用 FADD/FSUB/FMUL/FDIV 指令</summary>
        Hard = 0,
        /// <summary>软浮点 — 使用 softfloat.vml 库函数模拟</summary>
        Soft = 1,
        /// <summary>关闭浮点 — 遇到浮点代码报错</summary>
        None = 2,
    }

    /// <summary>
    /// 64位浮点处理模式 (double)
    /// </summary>
    public enum Float64Mode
    {
        /// <summary>硬件双精度 — 使用 DADD/DSUB/DMUL/DDIV 指令</summary>
        Hard = 0,
        /// <summary>软双精度 — 使用 softdouble.vml 库函数模拟（⚠ 本平台没链那个库；默认已是 Hard）</summary>
        Soft = 1,
        /// <summary>关闭双精度 — 遇到 double 代码降级或报错</summary>
        None = 2,
    }

    /// <summary>
    /// 64位整数处理模式
    /// </summary>
    public enum Int64Mode
    {
        /// <summary>硬件 64 位 — 使用 ADDL/SUBL/MULL/DIVL/MODL 指令</summary>
        Hard = 0,
        /// <summary>库模拟 — 使用 softint64.vml 库函数（⚠ 本平台没链那个库；默认已是 Hard）</summary>
        Soft = 1,
        /// <summary>关闭 64 位 — 遇到 64 位类型降级为 32 位</summary>
        None = 2,
    }

    /// <summary>
    /// 目标配置 — 整合 TargetMode + MemoryLevel
    /// </summary>
    public static class TargetConfig
    {
        /// <summary>根据内存级别获取默认内存大小（字节）</summary>
        public static int GetDefaultMemorySize(MemoryLevel level) => level switch
        {
            MemoryLevel.RAM_K => 64 * 1024,        // 64 KB
            MemoryLevel.RAM_M => 1024 * 1024,       // 1 MB
            MemoryLevel.RAM_G => 1024 * 1024 * 1024, // 1 GB
            _ => 64 * 1024,
        };

        /// <summary>根据内存级别获取默认栈大小（字节）</summary>
        public static int GetDefaultStackSize(MemoryLevel level) => level switch
        {
            MemoryLevel.RAM_K => 1024,              // 1 KB 栈
            MemoryLevel.RAM_M => 16 * 1024,         // 16 KB 栈
            MemoryLevel.RAM_G => 1024 * 1024,       // 1 MB 栈
            _ => 1024,
        };

        /// <summary>解析 --ram 参数</summary>
        public static MemoryLevel ParseMemoryLevel(string? arg) => arg?.ToLower() switch
        {
            "k" or "kb" or "ram_k" => MemoryLevel.RAM_K,
            "m" or "mb" or "ram_m" => MemoryLevel.RAM_M,
            "g" or "gb" or "ram_g" => MemoryLevel.RAM_G,
            _ => MemoryLevel.RAM_M,  // 默认 1MB（ConsoleEmulator 兼容）
        };
    }
}

using GenDev.Models;
using System.Text;

namespace GenDev.Generators;

/// <summary>
/// 设备代码生成器抽象基类 — 提取 24 个生成器的共同模板逻辑。
/// 各语言生成器仅需覆写格式方法，无需重复设备遍历逻辑。
///
/// 预计消除 ~2000 行重复代码 (当前 4243 → 约 2200)。
/// </summary>
public abstract class GeneratorBase : ICodeGenerator
{
    public abstract string Language { get; }
    public abstract string FileExtension { get; }

    // ═══════════════════════════════════════════════════════════════
    // 抽象方法 — 各语言必须覆写
    // ═══════════════════════════════════════════════════════════════

    /// <summary>注释前缀 (如 "//", ";", "#", "!")</summary>
    protected abstract string CommentPrefix { get; }

    /// <summary>生成头文件保护开始</summary>
    protected virtual void EmitGuardStart(StringBuilder code, string guardName)
        => code.AppendLine($"#ifndef {guardName}").AppendLine($"#define {guardName}").AppendLine();

    /// <summary>生成头文件保护结束</summary>
    protected virtual void EmitGuardEnd(StringBuilder code, string guardName)
        => code.AppendLine($"#endif // {guardName}");

    /// <summary>格式化寄存器定义</summary>
    protected abstract string FormatRegisterDef(string name, string type, string addr);

    /// <summary>格式化位域定义</summary>
    protected abstract string FormatBitFieldDef(string parent, string bitName, int bit);

    /// <summary>格式化内存段定义</summary>
    protected abstract string FormatMemorySegment(string name, string start, string end, string size);

    /// <summary>格式化外设基地址</summary>
    protected abstract string FormatPeripheralBase(string name, string baseAddr);

    /// <summary>格式化外设寄存器定义</summary>
    protected abstract string FormatPeripheralRegister(string periph, string reg, string type, string absAddr);

    /// <summary>寄存器大小 → 语言类型名</summary>
    protected abstract string GetTypeForSize(int size);

    /// <summary>格式化中断向量定义</summary>
    protected virtual string FormatInterruptVector(string name, int vector, string desc)
        => $"{CommentPrefix} IRQ #{vector}: {name} — {desc}";

    // ═══════════════════════════════════════════════════════════════
    // 模板方法 — 通用设备遍历逻辑
    // ═══════════════════════════════════════════════════════════════

    public virtual string Generate(DeviceModel device)
    {
        var code = new StringBuilder();
        string guardName = ComputeGuardName(device);

        EmitModuleStart(code, device);
        EmitGuardStart(code, guardName);
        EmitFileHeader(code, device);
        EmitCpuInfo(code, device);
        EmitRegisters(code, device);
        EmitMemorySegments(code, device);
        EmitPeripherals(code, device);
        EmitInterrupts(code, device);
        EmitPins(code, device);
        EmitFunctions(code, device);
        EmitGuardEnd(code, guardName);
        EmitModuleEnd(code, device);

        return code.ToString();
    }

    // ═══════════════════════════════════════════════════════════════
    // 可覆写方法 — 各语言可自定义输出
    // ═══════════════════════════════════════════════════════════════

    protected virtual string ComputeGuardName(DeviceModel device)
    {
        string name = CodeGeneratorHelper.SanitizeUpper(device.Metadata.Name) + "_H";
        if (char.IsDigit(name[0])) name = "DEVICE_" + name;
        return name;
    }

    protected virtual void EmitFileHeader(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"{CommentPrefix} {device.Metadata.Name} 寄存器定义");
        code.AppendLine($"{CommentPrefix} 生成自: {device.Metadata.Manufacturer}/{device.Metadata.Family}/{device.Metadata.Name}");
        code.AppendLine($"{CommentPrefix} 版本: {device.Metadata.Version}");
        code.AppendLine($"{CommentPrefix} 日期: {device.Metadata.Date:yyyy-MM-dd}");
        code.AppendLine($"{CommentPrefix} 作者: {device.Metadata.Author}");
        if (!string.IsNullOrEmpty(device.Metadata.Description))
            code.AppendLine($"{CommentPrefix} 描述: {device.Metadata.Description}");
        code.AppendLine();
    }

    protected virtual void EmitCpuInfo(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"{CommentPrefix} CPU架构: {device.Cpu.Architecture}");
        code.AppendLine($"{CommentPrefix} 位宽: {device.Cpu.Bits}位");
        code.AppendLine($"{CommentPrefix} 时钟频率: {device.Cpu.Clock.Default} Hz");
        code.AppendLine();
    }

    protected virtual void EmitRegisters(StringBuilder code, DeviceModel device)
    {
        if (device.Cpu.Registers.RegisterList.Count == 0) return;

        code.AppendLine($"{CommentPrefix} 寄存器定义");
        foreach (var reg in device.Cpu.Registers.RegisterList)
        {
            if (!string.IsNullOrEmpty(reg.Description))
                code.AppendLine($"{CommentPrefix} {reg.Description}");

            string type = GetTypeForSize(reg.Size);
            string name = CodeGeneratorHelper.SanitizeUpper(reg.Name);
            code.AppendLine(FormatRegisterDef(name, type, reg.Address));

            // 位域
            foreach (var bit in reg.BitFields)
            {
                string bitName = $"{name}_{CodeGeneratorHelper.SanitizeUpper(bit.Name)}";
                code.AppendLine($"{FormatBitFieldDef(bitName, bit.Name, bit.Bit)} {CommentPrefix} {bit.Description}");
            }
            code.AppendLine();
        }
    }

    protected virtual void EmitMemorySegments(StringBuilder code, DeviceModel device)
    {
        if (device.Memory.Segments.Count == 0) return;

        code.AppendLine($"{CommentPrefix} 内存段定义");
        foreach (var seg in device.Memory.Segments)
        {
            if (!string.IsNullOrEmpty(seg.Description))
                code.AppendLine($"{CommentPrefix} {seg.Description}");

            code.AppendLine(FormatMemorySegment(
                CodeGeneratorHelper.SanitizeUpper(seg.Name),
                seg.Start, seg.End, seg.Size));
            code.AppendLine();
        }
    }

    protected virtual void EmitPeripherals(StringBuilder code, DeviceModel device)
    {
        if (device.Peripherals?.PeripheralList.Count > 0 != true) return;

        code.AppendLine($"{CommentPrefix} 外设定义");
        foreach (var periph in device.Peripherals.PeripheralList)
        {
            if (!string.IsNullOrEmpty(periph.Description))
                code.AppendLine($"{CommentPrefix} {periph.Description}");

            string periName = CodeGeneratorHelper.SanitizeUpper(periph.Name);
            code.AppendLine(FormatPeripheralBase(periName, periph.Base));

            foreach (var reg in periph.Registers)
            {
                string type = GetTypeForSize(reg.Size);
                string regName = $"{periName}_{CodeGeneratorHelper.SanitizeUpper(reg.Name)}";
                string absAddr = ComputeAbsoluteAddress(periph.Base, reg.Address);
                code.AppendLine(FormatPeripheralRegister(periName, regName, type, absAddr));

                foreach (var bit in reg.BitFields)
                {
                    string bitName = $"{regName}_{CodeGeneratorHelper.SanitizeUpper(bit.Name)}";
                    code.AppendLine($"{FormatBitFieldDef(bitName, bit.Name, bit.Bit)} {CommentPrefix} {bit.Description}");
                }
            }
            code.AppendLine();
        }
    }

    protected virtual void EmitInterrupts(StringBuilder code, DeviceModel device)
    {
        if (device.Interrupts?.InterruptList.Count > 0 != true) return;

        code.AppendLine($"{CommentPrefix} 中断向量表");
        foreach (var irq in device.Interrupts.InterruptList)
        {
            string name = CodeGeneratorHelper.SanitizeUpper(irq.Name);
            code.AppendLine(FormatInterruptVector(name, irq.Vector, irq.Description ?? ""));
        }
        code.AppendLine();
    }

    /// <summary>生成初始化/访问函数 (默认实现为空，C/ASM 生成器可覆写)</summary>
    protected virtual void EmitFunctions(StringBuilder code, DeviceModel device) { }

    /// <summary>生成模块/类包装开始 (默认无操作，Lua/Python/Scheme 等覆写)</summary>
    protected virtual void EmitModuleStart(StringBuilder code, DeviceModel device) { }

    /// <summary>生成模块/类包装结束 (默认无操作)</summary>
    protected virtual void EmitModuleEnd(StringBuilder code, DeviceModel device) { }

    /// <summary>生成引脚定义 (默认实现)</summary>
    protected virtual void EmitPins(StringBuilder code, DeviceModel device)
    {
        if (device.Pins?.PinList.Count > 0 != true) return;

        code.AppendLine($"{CommentPrefix} 引脚定义");
        foreach (var pin in device.Pins.PinList)
        {
            string pinName = $"PIN_{CodeGeneratorHelper.SanitizeUpper(pin.Name)}";
            code.AppendLine(FormatPinDef(pinName, pin.Number, pin.Description ?? ""));
        }
        code.AppendLine();
    }

    /// <summary>格式化引脚定义</summary>
    protected virtual string FormatPinDef(string name, int number, string desc)
        => $"#define {name} {number}  {CommentPrefix} {desc}";

    // ═══════════════════════════════════════════════════════════════
    // 工具方法
    // ═══════════════════════════════════════════════════════════════

    public static string ComputeAbsoluteAddress(string baseAddr, string offset)
    {
        // 统一地址计算逻辑 — 消除各生成器间的重复
        if (baseAddr.StartsWith("0x") || baseAddr.StartsWith("0X"))
            baseAddr = baseAddr[2..];
        if (offset.StartsWith("0x") || offset.StartsWith("0X"))
            offset = offset[2..];

        if (long.TryParse(baseAddr, System.Globalization.NumberStyles.HexNumber, null, out long b) &&
            long.TryParse(offset, System.Globalization.NumberStyles.HexNumber, null, out long o))
        {
            return $"0x{(b + o):X}";
        }
        return $"{baseAddr}+{offset}";
    }
}

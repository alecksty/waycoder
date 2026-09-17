using GenDev.Models;
using System.Text;

namespace GenDev.Generators;

public class SchemeCodeGenerator : GeneratorBase
{
    public override string Language => "scheme";
    public override string FileExtension => ".scm";
    protected override string CommentPrefix => ";;";

    protected override string GetTypeForSize(int size) => "integer";

    protected override string FormatRegisterDef(string name, string type, string addr)
    {
        if (uint.TryParse(addr, System.Globalization.NumberStyles.HexNumber, null, out uint addrVal))
            return $"    (define {name} #x{addrVal:X})";
        return $"    (define {name} {addr})";
    }

    protected override string FormatBitFieldDef(string parent, string bitName, int bit)
        => $"    (define {parent}-{FormatKebabCase(bitName)} {bit})";

    protected override string FormatMemorySegment(string name, string start, string end, string size)
    {
        var sb = new StringBuilder();
        if (uint.TryParse(start, System.Globalization.NumberStyles.HexNumber, null, out uint s))
            sb.AppendLine($"    (define {name}-start #x{s:X})");
        else
            sb.AppendLine($"    (define {name}-start {start})");
        if (uint.TryParse(end, System.Globalization.NumberStyles.HexNumber, null, out uint e))
            sb.AppendLine($"    (define {name}-end   #x{e:X})");
        else
            sb.AppendLine($"    (define {name}-end   {end})");
        sb.Append($"    (define {name}-size {size})");
        return sb.ToString();
    }

    protected override string FormatPeripheralBase(string name, string baseAddr)
    {
        if (uint.TryParse(baseAddr, System.Globalization.NumberStyles.HexNumber, null, out uint val))
            return $"    (define {name}-base #x{val:X})";
        return $"    (define {name}-base {baseAddr})";
    }

    protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
    {
        if (uint.TryParse(absAddr, System.Globalization.NumberStyles.HexNumber, null, out uint val))
            return $"    (define {reg} #x{val:X})";
        return $"    (define {reg} {absAddr})";
    }

    protected override string FormatInterruptVector(string name, int vector, string desc)
        => $"    (define irq:{name} {vector})";

    protected override string FormatPinDef(string name, int number, string desc)
        => $"    (define PIN_{name} {number})  ;; {desc}";

    protected override void EmitModuleStart(StringBuilder code, DeviceModel device)
    {
        code.AppendLine("(define-library (device-registers)");
        code.AppendLine("  (export");
        code.AppendLine("    ;; 寄存器常量");
        code.AppendLine("    ;; 内存段常量");
        code.AppendLine("    ;; 外设常量");
        code.AppendLine("    ;; 中断向量");
        code.AppendLine("    ;; 访问函数");
        code.AppendLine("    read-reg write-reg init-device)");
        code.AppendLine();
    }

    protected override void EmitModuleEnd(StringBuilder code, DeviceModel device)
    {
        code.AppendLine(")");
    }

    protected override void EmitFileHeader(StringBuilder code, DeviceModel device)
    {
        code.AppendLine(";;;");
        code.AppendLine(";;; 设备寄存器定义");
        code.AppendLine($";;; 设备: {device.Metadata.Name}");
        code.AppendLine($";;; 生成自: {device.Metadata.Manufacturer}/{device.Metadata.Family}/{device.Metadata.Name}");
        code.AppendLine($";;; 版本: {device.Metadata.Version}");
        code.AppendLine($";;; 日期: {device.Metadata.Date:yyyy-MM-dd}");
        code.AppendLine($";;; 作者: {device.Metadata.Author}");
        if (!string.IsNullOrEmpty(device.Metadata.Description))
            code.AppendLine($";;; 描述: {device.Metadata.Description}");
        code.AppendLine(";;;");
        code.AppendLine();
    }

    protected override void EmitCpuInfo(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($";; CPU架构: {device.Cpu.Architecture}");
        code.AppendLine($";; 位宽: {device.Cpu.Bits}位");
        code.AppendLine($";; 时钟频率: {device.Cpu.Clock.Default} Hz");
        code.AppendLine();
    }

    protected override void EmitRegisters(StringBuilder code, DeviceModel device)
    {
        if (device.Cpu.Registers.RegisterList.Count == 0) return;

        code.AppendLine("  ;; 寄存器定义");
        code.AppendLine("  (begin");
        foreach (var reg in device.Cpu.Registers.RegisterList)
        {
            string regName = FormatKebabCase(reg.Name);
            if (uint.TryParse(reg.Address, System.Globalization.NumberStyles.HexNumber, null, out uint addrVal))
            {
                code.AppendLine($"    (define {regName} #x{addrVal:X})");
                foreach (var bitField in reg.BitFields)
                {
                    string bitName = FormatKebabCase(bitField.Name);
                    code.AppendLine($"    (define {regName}-{bitName} {bitField.Bit})");
                }
            }
        }
        code.AppendLine("  )");
        code.AppendLine();
    }

    protected override void EmitMemorySegments(StringBuilder code, DeviceModel device)
    {
        if (device.Memory?.Segments?.Count > 0 != true) return;

        code.AppendLine("  ;; 内存段定义");
        code.AppendLine("  (begin");
        foreach (var segment in device.Memory.Segments)
        {
            string segName = FormatKebabCase(segment.Name);
            if (uint.TryParse(segment.Start, System.Globalization.NumberStyles.HexNumber, null, out uint s) &&
                uint.TryParse(segment.End, System.Globalization.NumberStyles.HexNumber, null, out uint e))
            {
                code.AppendLine($"    (define {segName}-start #x{s:X})");
                code.AppendLine($"    (define {segName}-end   #x{e:X})");
                code.AppendLine($"    (define {segName}-size {uint.Parse(segment.Size)})");
            }
        }
        code.AppendLine("  )");
        code.AppendLine();
    }

    protected override void EmitPeripherals(StringBuilder code, DeviceModel device)
    {
        if (device.Peripherals?.PeripheralList.Count > 0 != true) return;

        code.AppendLine("  ;; 外设定义");
        code.AppendLine("  (begin");
        foreach (var peripheral in device.Peripherals.PeripheralList)
        {
            string periName = FormatKebabCase(peripheral.Name);
            if (uint.TryParse(peripheral.Base, System.Globalization.NumberStyles.HexNumber, null, out uint baseVal))
            {
                code.AppendLine($"    (define {periName}-base #x{baseVal:X})");
                foreach (var reg in peripheral.Registers)
                {
                    string regName = FormatKebabCase(reg.Name);
                    if (uint.TryParse(reg.Address, System.Globalization.NumberStyles.HexNumber, null, out uint regVal))
                    {
                        code.AppendLine($"    (define {periName}-{regName} #x{regVal:X})");
                    }
                }
            }
        }
        code.AppendLine("  )");
        code.AppendLine();
    }

    protected override void EmitInterrupts(StringBuilder code, DeviceModel device)
    {
        if (device.Interrupts?.InterruptList.Count > 0 != true) return;

        code.AppendLine("  ;; 中断向量定义");
        code.AppendLine("  (begin");
        foreach (var interrupt in device.Interrupts.InterruptList)
        {
            string irqName = FormatKebabCase(interrupt.Name);
            code.AppendLine($"    (define irq:{irqName} {interrupt.Vector})");
        }
        code.AppendLine("  )");
        code.AppendLine();
    }

    protected override void EmitFunctions(StringBuilder code, DeviceModel device)
    {
        code.AppendLine("  ;; 寄存器访问函数");
        code.AppendLine("  (define (read-reg addr)");
        code.AppendLine("    ;; 读取寄存器值");
        code.AppendLine("    (error \"read-reg: not implemented\")");
        code.AppendLine("  )");
        code.AppendLine();
        code.AppendLine("  (define (write-reg addr value)");
        code.AppendLine("    ;; 写入寄存器值");
        code.AppendLine("    (error \"write-reg: not implemented\")");
        code.AppendLine("  )");
        code.AppendLine();
        code.AppendLine("  (define (init-device)");
        code.AppendLine("    ;; 设备初始化");
        code.Append("    (display \"Initializing device: ")
            .Append(device.Metadata.Name)
            .AppendLine("\")");
        code.AppendLine("    (newline)");
        code.AppendLine("  )");
        code.AppendLine();
    }

    protected override void EmitGuardStart(StringBuilder code, string guardName) { }
    protected override void EmitGuardEnd(StringBuilder code, string guardName) { }

    private static string FormatKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        string formatted = CodeGeneratorHelper.SanitizeLower(name);
        if (char.IsDigit(formatted[0]))
            formatted = "reg-" + formatted;
        return formatted;
    }
}

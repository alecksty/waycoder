using GenDev.Models;
using System.Text;

namespace GenDev.Generators;

public class ForthCodeGenerator : GeneratorBase
{
    public override string Language => "forth";
    public override string FileExtension => ".fth";
    protected override string CommentPrefix => "\\";

    protected override string GetTypeForSize(int size) => size switch
    {
        1 => "C",
        2 => "",
        4 => "L",
        8 => "XL",
        _ => $" {size} CHARS"
    };

    protected override string FormatRegisterDef(string name, string type, string addr)
        => $"{addr} CONSTANT {name}";

    protected override string FormatBitFieldDef(string parent, string bitName, int bit)
        => $"{bit} CONSTANT {parent}-{CodeGeneratorHelper.SanitizeUpper(bitName)}";

    protected override string FormatMemorySegment(string name, string start, string end, string size)
        => $"{start} CONSTANT {name}-START\n{end} CONSTANT {name}-END\n{size} CONSTANT {name}-SIZE";

    protected override string FormatPeripheralBase(string name, string baseAddr)
        => $"{baseAddr} CONSTANT {name}-BASE";

    protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
        => $"{absAddr} CONSTANT {reg}";

    protected override string FormatInterruptVector(string name, int vector, string desc)
        => $"{vector} CONSTANT INT-{name}  \\ {desc}";

    protected override string FormatPinDef(string name, int number, string desc)
        => $"{number} CONSTANT {name}  \\ {desc}";

    protected override void EmitFileHeader(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"\\ =========================================");
        code.AppendLine($"\\ {device.Metadata.Name}设备定义 - Forth文件");
        code.AppendLine($"\\ =========================================");
        code.AppendLine($"\\ 生成自: {device.Metadata.Manufacturer}/{device.Metadata.Family}/{device.Metadata.Name}");
        code.AppendLine($"\\ 版本: {device.Metadata.Version}");
        code.AppendLine($"\\ 日期: {device.Metadata.Date:yyyy-MM-dd}");
        code.AppendLine($"\\ 作者: {device.Metadata.Author}");
        if (!string.IsNullOrEmpty(device.Metadata.Description))
            code.AppendLine($"\\ 描述: {device.Metadata.Description}");
        code.AppendLine($"\\ CPU架构: {device.Cpu.Architecture}");
        code.AppendLine($"\\ 位宽: {device.Cpu.Bits}位");
        code.AppendLine($"\\ 时钟频率: {device.Cpu.Clock.Default} Hz");
        code.AppendLine();
    }

    protected override void EmitCpuInfo(StringBuilder code, DeviceModel device)
    {
        // Forth设备信息常量
        code.AppendLine($"\\ 设备信息");
        code.AppendLine($": DEVICE-NAME   S\" {device.Metadata.Name}\" ;");
        code.AppendLine($": MANUFACTURER  S\" {device.Metadata.Manufacturer}\" ;");
        code.AppendLine($": FAMILY        S\" {device.Metadata.Family}\" ;");
        code.AppendLine($": VERSION       S\" {device.Metadata.Version}\" ;");
        code.AppendLine($": ARCHITECTURE  S\" {device.Cpu.Architecture}\" ;");
        code.AppendLine($"{device.Cpu.Bits} CONSTANT BITS");
        code.AppendLine($"{device.Cpu.Clock.Default} CONSTANT CLOCK-FREQ");
        code.AppendLine();
    }

    protected override void EmitFunctions(StringBuilder code, DeviceModel device)
    {
        EmitRegisterAccessWords(code, device);
        EmitPeripheralAccessWords(code, device);
        EmitDeviceInit(code, device);
        EmitDeviceInfoDisplay(code, device);
        EmitRegisterDisplay(code, device);
        EmitPinOperations(code, device);
        EmitInterruptHandlers(code, device);
        EmitExample(code, device);
    }

    private void EmitRegisterAccessWords(StringBuilder code, DeviceModel device)
    {
        if (device.Cpu.Registers.RegisterList.Count == 0) return;

        code.AppendLine($"\\ =========================================");
        code.AppendLine($"\\ 寄存器访问字");
        code.AppendLine($"\\ =========================================");
        code.AppendLine();
        code.AppendLine($"\\ 通用寄存器访问");

        foreach (var reg in device.Cpu.Registers.RegisterList)
        {
            string regName = CodeGeneratorHelper.SanitizeUpper(reg.Name);
            string sizeWord = GetForthSizeWord(reg.Size);
            code.AppendLine($": {regName}@ ( -- n ) {regName} {sizeWord}@ ;");
            code.AppendLine($": {regName}! ( n -- ) {regName} {sizeWord}! ;");

            if (reg.BitFields.Count > 0)
            {
                foreach (var bit in reg.BitFields)
                {
                    string bitName = $"{regName}-{CodeGeneratorHelper.SanitizeUpper(bit.Name)}";
                    code.AppendLine($": {bitName}@ ( -- flag ) {regName}@ {bit.Bit} BIT@ ;");
                    code.AppendLine($": {bitName}! ( flag -- ) {regName}@ {bit.Bit} BIT! {regName}! ;");
                    code.AppendLine($": {bitName}-SET ( -- ) TRUE {bitName}! ;");
                    code.AppendLine($": {bitName}-CLR ( -- ) FALSE {bitName}! ;");
                }
            }
            code.AppendLine();
        }
    }

    private void EmitPeripheralAccessWords(StringBuilder code, DeviceModel device)
    {
        if (device.Peripherals?.PeripheralList.Count > 0 != true) return;

        code.AppendLine($"\\ 外设访问");
        foreach (var peripheral in device.Peripherals.PeripheralList)
        {
            code.AppendLine($"\\ {peripheral.Name}外设");
            foreach (var reg in peripheral.Registers)
            {
                string regName = $"{CodeGeneratorHelper.SanitizeUpper(peripheral.Name)}-{CodeGeneratorHelper.SanitizeUpper(reg.Name)}";
                string sizeWord = GetForthSizeWord(reg.Size);
                code.AppendLine($": {regName}@ ( -- n ) {regName} {sizeWord}@ ;");
                code.AppendLine($": {regName}! ( n -- ) {regName} {sizeWord}! ;");

                if (reg.BitFields.Count > 0)
                {
                    foreach (var bit in reg.BitFields)
                    {
                        string bitName = $"{regName}-{CodeGeneratorHelper.SanitizeUpper(bit.Name)}";
                        code.AppendLine($": {bitName}@ ( -- flag ) {regName}@ {bit.Bit} BIT@ ;");
                        code.AppendLine($": {bitName}! ( flag -- ) {regName}@ {bit.Bit} BIT! {regName}! ;");
                    }
                }
            }
            code.AppendLine();
        }
    }

    private void EmitDeviceInit(StringBuilder code, DeviceModel device)
    {
        string devName = CodeGeneratorHelper.SanitizeUpper(device.Metadata.Name);
        code.AppendLine($"\\ =========================================");
        code.AppendLine($"\\ 设备初始化");
        code.AppendLine($"\\ =========================================");
        code.AppendLine();
        code.AppendLine($": {devName}-INIT ( -- )");
        code.AppendLine($"  \\ 初始化{device.Metadata.Name}设备");
        code.AppendLine($"  .\" 初始化{device.Metadata.Name}...\" CR");
        code.AppendLine();

        if (device.Cpu.Registers.RegisterList.Count > 0)
        {
            code.AppendLine($"  \\ 初始化寄存器");
            foreach (var reg in device.Cpu.Registers.RegisterList)
                code.AppendLine($"  0 {CodeGeneratorHelper.SanitizeUpper(reg.Name)}!  \\ {reg.Description}");
        }
        code.AppendLine();

        if (device.Peripherals?.PeripheralList.Count > 0)
        {
            code.AppendLine($"  \\ 初始化外设");
            foreach (var peripheral in device.Peripherals.PeripheralList)
            {
                code.AppendLine($"  \\ 初始化{peripheral.Name}");
                foreach (var reg in peripheral.Registers)
                {
                    string regName = $"{CodeGeneratorHelper.SanitizeUpper(peripheral.Name)}-{CodeGeneratorHelper.SanitizeUpper(reg.Name)}";
                    code.AppendLine($"  0 {regName}!  \\ {reg.Name}寄存器");
                }
            }
        }
        code.AppendLine();
        code.AppendLine($"  .\" {device.Metadata.Name}初始化完成\" CR");
        code.AppendLine($";");
        code.AppendLine();
    }

    private void EmitDeviceInfoDisplay(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"\\ =========================================");
        code.AppendLine($"\\ 设备信息显示");
        code.AppendLine($"\\ =========================================");
        code.AppendLine();
        code.AppendLine($": .DEVICE-INFO ( -- )");
        code.AppendLine($"  CR");
        code.AppendLine($"  .\" 设备: \" DEVICE-NAME TYPE CR");
        code.AppendLine($"  .\" 厂商: \" MANUFACTURER TYPE CR");
        code.AppendLine($"  .\" 系列: \" FAMILY TYPE CR");
        code.AppendLine($"  .\" 版本: \" VERSION TYPE CR");
        code.AppendLine($"  .\" 架构: \" ARCHITECTURE TYPE CR");
        code.AppendLine($"  .\" 位宽: \" BITS . CR");
        code.AppendLine($"  .\" 时钟: \" CLOCK-FREQ . .\" Hz\" CR");
        code.AppendLine($";");
        code.AppendLine();
    }

    private void EmitRegisterDisplay(StringBuilder code, DeviceModel device)
    {
        if (device.Cpu.Registers.RegisterList.Count == 0) return;

        code.AppendLine($": .REGISTERS ( -- )");
        code.AppendLine($"  CR .\" 寄存器状态:\" CR");
        code.AppendLine($"  .\" ----------\" CR");
        foreach (var reg in device.Cpu.Registers.RegisterList)
        {
            string regName = CodeGeneratorHelper.SanitizeUpper(reg.Name);
            code.AppendLine($"  {regName}@ {regName} .R 8 .R SPACE .\"  {reg.Name}: \" {regName}@ .");
        }
        code.AppendLine($";");
        code.AppendLine();
    }

    private void EmitPinOperations(StringBuilder code, DeviceModel device)
    {
        if (device.Pins?.PinList.Count > 0 != true) return;

        code.AppendLine($"\\ =========================================");
        code.AppendLine($"\\ 引脚操作");
        code.AppendLine($"\\ =========================================");
        code.AppendLine();

        foreach (var pin in device.Pins.PinList)
        {
            string pinName = $"PIN-{CodeGeneratorHelper.SanitizeUpper(pin.Name)}";
            if (pin.Type.ToLower().Contains("output"))
            {
                code.AppendLine($": {pinName}-HIGH ( -- ) {pinName} 1 GPIO! ;");
                code.AppendLine($": {pinName}-LOW  ( -- ) {pinName} 0 GPIO! ;");
                code.AppendLine($": {pinName}-TOGGLE ( -- ) {pinName} GPIO@ 0= {pinName} GPIO! ;");
                code.AppendLine();
            }
            else if (pin.Type.ToLower().Contains("input"))
            {
                code.AppendLine($": {pinName}@ ( -- flag ) {pinName} GPIO@ ;");
                code.AppendLine();
            }
        }
    }

    private void EmitInterruptHandlers(StringBuilder code, DeviceModel device)
    {
        if (device.Interrupts?.InterruptList.Count > 0 != true) return;

        code.AppendLine($"\\ =========================================");
        code.AppendLine($"\\ 中断处理");
        code.AppendLine($"\\ =========================================");
        code.AppendLine();

        foreach (var interrupt in device.Interrupts.InterruptList)
        {
            string intName = $"INT-{CodeGeneratorHelper.SanitizeUpper(interrupt.Name)}";
            code.AppendLine($"\\ {interrupt.Description}");
            code.AppendLine($": {intName}-HANDLER ( -- )");
            code.AppendLine($"  .\" {interrupt.Name}中断处理\" CR");
            code.AppendLine($"  \\ 添加具体的中断处理代码");
            code.AppendLine($";");
            code.AppendLine();
            code.AppendLine($": {intName}-ENABLE ( -- )");
            code.AppendLine($"  {intName} INT-ENABLE");
            code.AppendLine($";");
            code.AppendLine();
            code.AppendLine($": {intName}-DISABLE ( -- )");
            code.AppendLine($"  {intName} INT-DISABLE");
            code.AppendLine($";");
            code.AppendLine();
        }
    }

    private void EmitExample(StringBuilder code, DeviceModel device)
    {
        string devName = CodeGeneratorHelper.SanitizeUpper(device.Metadata.Name);
        code.AppendLine($"\\ =========================================");
        code.AppendLine($"\\ 示例程序");
        code.AppendLine($"\\ =========================================");
        code.AppendLine();
        code.AppendLine($": EXAMPLE ( -- )");
        code.AppendLine($"  {devName}-INIT");
        code.AppendLine($"  .DEVICE-INFO");
        code.AppendLine($"  .REGISTERS");
        code.AppendLine($"  CR .\" 示例程序运行完成\" CR");
        code.AppendLine($";");
        code.AppendLine();
        code.AppendLine($"\\ 自动运行示例");
        code.AppendLine($"( EXAMPLE )");
    }

    private string GetForthSizeWord(int size)
    {
        return size switch
        {
            1 => "C",
            2 => "",
            4 => "L",
            8 => "XL",
            _ => $" {size} CHARS"
        };
    }
}

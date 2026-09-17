using GenDev.Models;
using System.Text;

namespace GenDev.Generators;

/// <summary>
/// Dart 设备常量生成器 (static const).
/// 迁移到 GeneratorBase — 122 → 55 行 (~55% 减少).
/// </summary>
public class DartCodeGenerator : GeneratorBase
{
    public override string Language => "dart";
    public override string FileExtension => ".dart";
    protected override string CommentPrefix => "//";

    // Dart 使用 class 包装，不需要 C 风格的 include guard
    protected override void EmitGuardStart(StringBuilder code, string guardName)
    {
        string className = _deviceName!.Replace("-", "_");
        code.AppendLine($"class {className}Device {{");
    }

    protected override void EmitGuardEnd(StringBuilder code, string guardName)
        => code.AppendLine("}");

    protected override void EmitFileHeader(StringBuilder code, DeviceModel device)
    {
        _deviceName = device.Metadata.Name;
        base.EmitFileHeader(code, device);

        // 设备信息常量
        code.AppendLine($"  static const String deviceName = \"{device.Metadata.Name}\";");
        code.AppendLine($"  static const String manufacturer = \"{device.Metadata.Manufacturer}\";");
        code.AppendLine($"  static const String family = \"{device.Metadata.Family}\";");
        code.AppendLine($"  static const String version = \"{device.Metadata.Version}\";");
        code.AppendLine($"  static const String architecture = \"{device.Cpu.Architecture}\";");
        code.AppendLine($"  static const int bits = {device.Cpu.Bits};");
        code.AppendLine($"  static const int clockFrequency = {device.Cpu.Clock.Default};");
        code.AppendLine();
    }

    protected override void EmitFunctions(StringBuilder code, DeviceModel device)
    {
        // 引脚常量
        if (device.Pins?.PinList.Count > 0)
        {
            code.AppendLine("  // 引脚定义");
            foreach (var pin in device.Pins.PinList)
                code.AppendLine($"  static const int PIN_{CodeGeneratorHelper.SanitizeUpper(pin.Name)} = {pin.Number};  // {pin.Description}");
            code.AppendLine();
        }
    }

    protected override string GetTypeForSize(int size) => "int";

    protected override string FormatRegisterDef(string name, string type, string addr)
        => $"  static const int {name}_ADDR = {addr};";

    protected override string FormatBitFieldDef(string parent, string bitName, int bit)
        => $"  static const int {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit};";

    protected override string FormatMemorySegment(string name, string start, string end, string size)
        => $"  static const int {name}_START = {start};\n  static const int {name}_END = {end};\n  static const int {name}_SIZE = {size};";

    protected override string FormatPeripheralBase(string name, string baseAddr)
        => $"  static const int {name}_BASE = {baseAddr};";

    protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
        => $"  static const int {reg}_ADDR = {absAddr};";

    protected override string FormatInterruptVector(string name, int vector, string desc)
        => $"  static const int INT_{name} = {vector};  // {desc}";

    private string? _deviceName;
}

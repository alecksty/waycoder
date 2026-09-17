using GenDev.Models;
using System.Text;

namespace GenDev.Generators;

public class PythonCodeGenerator : GeneratorBase
{
    public override string Language => "python";
    public override string FileExtension => ".py";
    protected override string CommentPrefix => "#";

    private string _className = "";

    protected override string GetTypeForSize(int size) => size switch
    {
        1 => "uint8",
        2 => "uint16",
        4 => "uint32",
        8 => "uint64",
        _ => $"bytes[{size}]"
    };

    protected override string FormatRegisterDef(string name, string type, string addr)
        => $"    {name}_ADDR = {addr}";

    protected override string FormatBitFieldDef(string parent, string bitName, int bit)
        => $"    {parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit}";

    protected override string FormatMemorySegment(string name, string start, string end, string size)
        => $"    {name}_START = {start}\n    {name}_END = {end}\n    {name}_SIZE = {size}";

    protected override string FormatPeripheralBase(string name, string baseAddr)
        => $"    {name}_BASE = {baseAddr}";

    protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
        => $"    {reg}_ADDR = {absAddr}";

    protected override string FormatInterruptVector(string name, int vector, string desc)
        => $"    INT_{name} = {vector}";

    protected override string FormatPinDef(string name, int number, string desc)
        => $"    PIN_{CodeGeneratorHelper.SanitizeUpper(name)} = {number}  # {desc}";

    protected override void EmitModuleStart(StringBuilder code, DeviceModel device)
    {
        _className = device.Metadata.Name.Replace("-", "_");
        code.AppendLine($"import ctypes");
        code.AppendLine($"import struct");
        code.AppendLine($"from typing import Union, Optional");
        code.AppendLine();
        code.AppendLine($"class {_className}:");
        code.AppendLine($"    \"\"\"{device.Metadata.Name}设备类\"\"\"");
        code.AppendLine();
    }

    protected override void EmitFileHeader(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"\"\"\"");
        code.AppendLine($"{device.Metadata.Name}设备定义 - Python模块");
        code.AppendLine($"生成自: {device.Metadata.Manufacturer}/{device.Metadata.Family}/{device.Metadata.Name}");
        code.AppendLine($"版本: {device.Metadata.Version}");
        code.AppendLine($"日期: {device.Metadata.Date:yyyy-MM-dd}");
        code.AppendLine($"作者: {device.Metadata.Author}");
        if (!string.IsNullOrEmpty(device.Metadata.Description))
            code.AppendLine($"描述: {device.Metadata.Description}");
        code.AppendLine($"CPU架构: {device.Cpu.Architecture}");
        code.AppendLine($"位宽: {device.Cpu.Bits}位");
        code.AppendLine($"时钟频率: {device.Cpu.Clock.Default} Hz");
        code.AppendLine($"\"\"\"");
        code.AppendLine();
    }

    protected override void EmitCpuInfo(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"    # 设备信息");
        code.AppendLine($"    DEVICE_NAME = \"{device.Metadata.Name}\"");
        code.AppendLine($"    MANUFACTURER = \"{device.Metadata.Manufacturer}\"");
        code.AppendLine($"    FAMILY = \"{device.Metadata.Family}\"");
        code.AppendLine($"    VERSION = \"{device.Metadata.Version}\"");
        code.AppendLine($"    ARCHITECTURE = \"{device.Cpu.Architecture}\"");
        code.AppendLine($"    BITS = {device.Cpu.Bits}");
        code.AppendLine($"    CLOCK_FREQUENCY = {device.Cpu.Clock.Default}");
        code.AppendLine();
    }

    protected override void EmitPins(StringBuilder code, DeviceModel device)
    {
        if (device.Pins?.PinList.Count > 0 != true) return;

        code.AppendLine($"    # 引脚定义");
        foreach (var pin in device.Pins.PinList)
        {
            code.AppendLine($"    PIN_{CodeGeneratorHelper.SanitizeUpper(pin.Name)} = {pin.Number}  # {pin.Description}");
        }
        code.AppendLine();
    }

    protected override void EmitFunctions(StringBuilder code, DeviceModel device)
    {
        EmitInit(code, device);
        EmitRegisterInit(code, device);
        EmitPeripheralInit(code, device);
        EmitReadWriteMethods(code);
        EmitBitMethods(code);
        EmitInfoMethods(code);
        EmitReset(code);
        EmitStrRepr(code);
        EmitMainBlock(code, device);
    }

    private void EmitInit(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"    def __init__(self, memory_base: int = 0):");
        code.AppendLine($"        \"\"\"初始化设备\"\"\"");
        code.AppendLine($"        self.memory_base = memory_base");
        code.AppendLine($"        self._registers = {{}}");
        code.AppendLine($"        self._peripherals = {{}}");
        code.AppendLine($"        self._initialize_registers()");
        code.AppendLine($"        self._initialize_peripherals()");
        code.AppendLine();
    }

    private void EmitRegisterInit(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"    def _initialize_registers(self):");
        code.AppendLine($"        \"\"\"初始化寄存器\"\"\"\"");
        if (device.Cpu.Registers.RegisterList.Count > 0)
        {
            foreach (var reg in device.Cpu.Registers.RegisterList)
            {
                string type = GetTypeForSize(reg.Size);
                code.AppendLine($"        self._registers[\"{reg.Name}\"] = {{");
                code.AppendLine($"            \"address\": {reg.Address},");
                code.AppendLine($"            \"size\": {reg.Size},");
                code.AppendLine($"            \"type\": \"{type}\",");
                code.AppendLine($"            \"access\": \"{reg.Access}\",");
                code.AppendLine($"            \"description\": \"{reg.Description}\",");
                code.AppendLine($"            \"value\": 0");
                code.AppendLine($"        }}");
            }
        }
        code.AppendLine();
    }

    private void EmitPeripheralInit(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"    def _initialize_peripherals(self):");
        code.AppendLine($"        \"\"\"初始化外设\"\"\"");
        if (device.Peripherals?.PeripheralList.Count > 0)
        {
            foreach (var peripheral in device.Peripherals.PeripheralList)
            {
                code.AppendLine($"        self._peripherals[\"{peripheral.Name}\"] = {{");
                code.AppendLine($"            \"base\": {peripheral.Base},");
                code.AppendLine($"            \"type\": \"{peripheral.Type}\",");
                code.AppendLine($"            \"description\": \"{peripheral.Description}\",");
                code.AppendLine($"            \"registers\": {{");
                foreach (var reg in peripheral.Registers)
                {
                    string type = GetTypeForSize(reg.Size);
                    code.AppendLine($"                \"{reg.Name}\": {{");
                    code.AppendLine($"                    \"address\": {reg.Address},");
                    code.AppendLine($"                    \"size\": {reg.Size},");
                    code.AppendLine($"                    \"type\": \"{type}\",");
                    code.AppendLine($"                    \"value\": 0");
                    code.AppendLine($"                }},");
                }
                code.AppendLine($"            }}");
                code.AppendLine($"        }}");
            }
        }
        code.AppendLine();
    }

    private void EmitReadWriteMethods(StringBuilder code)
    {
        code.AppendLine($"    def read_register(self, name: str) -> int:");
        code.AppendLine($"        \"\"\"读取寄存器值\"\"\"");
        code.AppendLine($"        if name in self._registers:");
        code.AppendLine($"            return self._registers[name][\"value\"]");
        code.AppendLine($"        raise KeyError(f\"寄存器 {{name}} 不存在\")");
        code.AppendLine();
        code.AppendLine($"    def write_register(self, name: str, value: int):");
        code.AppendLine($"        \"\"\"写入寄存器值\"\"\"");
        code.AppendLine($"        if name in self._registers:");
        code.AppendLine($"            reg = self._registers[name]");
        code.AppendLine($"            max_value = (1 << (reg[\"size\"] * 8)) - 1");
        code.AppendLine($"            if value < 0 or value > max_value:");
        code.AppendLine($"                raise ValueError(f\"值 {{value}} 超出范围 [0, {{max_value}}]\")");
        code.AppendLine($"            reg[\"value\"] = value");
        code.AppendLine($"        else:");
        code.AppendLine($"            raise KeyError(f\"寄存器 {{name}} 不存在\")");
        code.AppendLine();
    }

    private void EmitBitMethods(StringBuilder code)
    {
        code.AppendLine($"    def set_bit(self, register_name: str, bit: int, value: bool):");
        code.AppendLine($"        \"\"\"设置寄存器位\"\"\"");
        code.AppendLine($"        if register_name in self._registers:");
        code.AppendLine($"            reg = self._registers[register_name]");
        code.AppendLine($"            if value:");
        code.AppendLine($"                reg[\"value\"] |= (1 << bit)");
        code.AppendLine($"            else:");
        code.AppendLine($"                reg[\"value\"] &= ~(1 << bit)");
        code.AppendLine($"        else:");
        code.AppendLine($"            raise KeyError(f\"寄存器 {{register_name}} 不存在\")");
        code.AppendLine();
        code.AppendLine($"    def get_bit(self, register_name: str, bit: int) -> bool:");
        code.AppendLine($"        \"\"\"获取寄存器位\"\"\"");
        code.AppendLine($"        if register_name in self._registers:");
        code.AppendLine($"            reg = self._registers[register_name]");
        code.AppendLine($"            return (reg[\"value\"] >> bit) & 1 == 1");
        code.AppendLine($"        raise KeyError(f\"寄存器 {{register_name}} 不存在\")");
        code.AppendLine();
    }

    private void EmitInfoMethods(StringBuilder code)
    {
        code.AppendLine($"    def get_device_info(self) -> dict:");
        code.AppendLine($"        \"\"\"获取设备信息\"\"\"");
        code.AppendLine($"        return {{");
        code.AppendLine($"            \"name\": self.DEVICE_NAME,");
        code.AppendLine($"            \"manufacturer\": self.MANUFACTURER,");
        code.AppendLine($"            \"family\": self.FAMILY,");
        code.AppendLine($"            \"version\": self.VERSION,");
        code.AppendLine($"            \"architecture\": self.ARCHITECTURE,");
        code.AppendLine($"            \"bits\": self.BITS,");
        code.AppendLine($"            \"clock_frequency\": self.CLOCK_FREQUENCY");
        code.AppendLine($"        }}");
        code.AppendLine();
        code.AppendLine($"    def get_register_info(self, name: str) -> Optional[dict]:");
        code.AppendLine($"        \"\"\"获取寄存器信息\"\"\"");
        code.AppendLine($"        return self._registers.get(name)");
        code.AppendLine();
        code.AppendLine($"    def get_peripheral_info(self, name: str) -> Optional[dict]:");
        code.AppendLine($"        \"\"\"获取外设信息\"\"\"");
        code.AppendLine($"        return self._peripherals.get(name)");
        code.AppendLine();
    }

    private void EmitReset(StringBuilder code)
    {
        code.AppendLine($"    def reset(self):");
        code.AppendLine($"        \"\"\"重置设备\"\"\"");
        code.AppendLine($"        for reg in self._registers.values():");
        code.AppendLine($"            reg[\"value\"] = 0");
        code.AppendLine($"        for peripheral in self._peripherals.values():");
        code.AppendLine($"            for reg in peripheral[\"registers\"].values():");
        code.AppendLine($"                reg[\"value\"] = 0");
        code.AppendLine();
    }

    private void EmitStrRepr(StringBuilder code)
    {
        code.AppendLine($"    def __str__(self) -> str:");
        code.AppendLine($"        \"\"\"字符串表示\"\"\"");
        code.AppendLine($"        info = self.get_device_info()");
        code.AppendLine($"        return f\"{_className}({{info['name']}} v{{info['version']}})\"");
        code.AppendLine();
    }

    private void EmitMainBlock(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"if __name__ == \"__main__\":");
        code.AppendLine($"    # 使用示例");
        code.AppendLine($"    device = {_className}()");
        code.AppendLine($"    print(f\"设备: {{device}}\")");
        code.AppendLine($"    print(f\"设备信息: {{device.get_device_info()}}\")");
        code.AppendLine($"    print()");
        code.AppendLine($"    ");
        code.AppendLine($"    # 演示寄存器操作");
        if (device.Cpu.Registers.RegisterList.Count > 0)
        {
            var firstReg = device.Cpu.Registers.RegisterList[0].Name;
            code.AppendLine($"    first_reg = \"{firstReg}\"");
            code.AppendLine($"    print(f\"第一个寄存器: {{first_reg}}\")");
            code.AppendLine($"    device.write_register(first_reg, 0x55)");
            code.AppendLine($"    value = device.read_register(first_reg)");
            code.AppendLine($"    print(f\"读取值: 0x{{value:X}}\")");
        }
    }

    protected override void EmitGuardStart(StringBuilder code, string guardName) { }
    protected override void EmitGuardEnd(StringBuilder code, string guardName) { }
}

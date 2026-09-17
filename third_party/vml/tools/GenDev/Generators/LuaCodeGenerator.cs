using GenDev.Models;
using System.Text;

namespace GenDev.Generators;

public class LuaCodeGenerator : GeneratorBase
{
    public override string Language => "lua";
    public override string FileExtension => ".lua";
    protected override string CommentPrefix => "--";

    private string _moduleName = "";

    protected override string GetTypeForSize(int size) => "number";

    protected override string FormatRegisterDef(string name, string type, string addr)
        => $"{_moduleName}.{name}_ADDR = {addr}";

    protected override string FormatBitFieldDef(string parent, string bitName, int bit)
        => $"{_moduleName}.{parent}_{CodeGeneratorHelper.SanitizeUpper(bitName)}_BIT = {bit}";

    protected override string FormatMemorySegment(string name, string start, string end, string size)
        => $"{_moduleName}.{name}_START = {start}\n{_moduleName}.{name}_END = {end}\n{_moduleName}.{name}_SIZE = {size}";

    protected override string FormatPeripheralBase(string name, string baseAddr)
        => $"{_moduleName}.{name}_BASE = {baseAddr}";

    protected override string FormatPeripheralRegister(string periph, string reg, string type, string absAddr)
        => $"{_moduleName}.{reg}_ADDR = {absAddr}";

    protected override string FormatInterruptVector(string name, int vector, string desc)
        => $"{_moduleName}.INT_{name} = {vector}";

    protected override string FormatPinDef(string name, int number, string desc)
        => $"{_moduleName}.PIN_{CodeGeneratorHelper.SanitizeUpper(name)} = {number}  -- {desc}";

    protected override void EmitModuleStart(StringBuilder code, DeviceModel device)
    {
        _moduleName = device.Metadata.Name.Replace("-", "_");
    }

    protected override void EmitFileHeader(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"--[[");
        code.AppendLine($"  {device.Metadata.Name}设备定义 - Lua模块");
        code.AppendLine($"  生成自: {device.Metadata.Manufacturer}/{device.Metadata.Family}/{device.Metadata.Name}");
        code.AppendLine($"  版本: {device.Metadata.Version}");
        code.AppendLine($"  日期: {device.Metadata.Date:yyyy-MM-dd}");
        code.AppendLine($"  作者: {device.Metadata.Author}");
        if (!string.IsNullOrEmpty(device.Metadata.Description))
            code.AppendLine($"  描述: {device.Metadata.Description}");
        code.AppendLine($"  CPU架构: {device.Cpu.Architecture}");
        code.AppendLine($"  位宽: {device.Cpu.Bits}位");
        code.AppendLine($"  时钟频率: {device.Cpu.Clock.Default} Hz");
        code.AppendLine($"]]");
        code.AppendLine();
        code.AppendLine($"local {_moduleName} = {{}}");
        code.AppendLine();
    }

    protected override void EmitCpuInfo(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"-- 设备信息");
        code.AppendLine($"{_moduleName}.DEVICE_NAME = \"{device.Metadata.Name}\"");
        code.AppendLine($"{_moduleName}.MANUFACTURER = \"{device.Metadata.Manufacturer}\"");
        code.AppendLine($"{_moduleName}.FAMILY = \"{device.Metadata.Family}\"");
        code.AppendLine($"{_moduleName}.VERSION = \"{device.Metadata.Version}\"");
        code.AppendLine($"{_moduleName}.ARCHITECTURE = \"{device.Cpu.Architecture}\"");
        code.AppendLine($"{_moduleName}.BITS = {device.Cpu.Bits}");
        code.AppendLine($"{_moduleName}.CLOCK_FREQUENCY = {device.Cpu.Clock.Default}");
        code.AppendLine();
    }

    protected override void EmitPins(StringBuilder code, DeviceModel device)
    {
        if (device.Pins?.PinList.Count > 0 != true) return;

        code.AppendLine($"-- 引脚定义");
        foreach (var pin in device.Pins.PinList)
        {
            code.AppendLine($"{_moduleName}.PIN_{CodeGeneratorHelper.SanitizeUpper(pin.Name)} = {pin.Number}  -- {pin.Description}");
        }
        code.AppendLine();
    }

    protected override void EmitFunctions(StringBuilder code, DeviceModel device)
    {
        EmitDeviceClass(code, device);
        EmitUtilityFunctions(code, device);
        EmitExample(code, device);
    }

    private void EmitDeviceClass(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"-- 设备类");
        code.AppendLine($"function {_moduleName}.new(memory_base)");
        code.AppendLine($"    memory_base = memory_base or 0");
        code.AppendLine($"    ");
        code.AppendLine($"    local self = {{");
        code.AppendLine($"        memory_base = memory_base,");
        code.AppendLine($"        registers = {{}},");
        code.AppendLine($"        peripherals = {{}}");
        code.AppendLine($"    }}");
        code.AppendLine($"    ");

        // Init registers
        code.AppendLine($"    -- 初始化寄存器");
        code.AppendLine($"    function self:_init_registers()");
        if (device.Cpu.Registers.RegisterList.Count > 0)
        {
            foreach (var reg in device.Cpu.Registers.RegisterList)
            {
                code.AppendLine($"        self.registers[\"{reg.Name}\"] = {{");
                code.AppendLine($"            address = {reg.Address},");
                code.AppendLine($"            size = {reg.Size},");
                code.AppendLine($"            access = \"{reg.Access}\",");
                code.AppendLine($"            description = \"{reg.Description}\",");
                code.AppendLine($"            value = 0");
                code.AppendLine($"        }}");
            }
        }
        code.AppendLine($"    end");
        code.AppendLine($"    ");

        // Init peripherals
        code.AppendLine($"    -- 初始化外设");
        code.AppendLine($"    function self:_init_peripherals()");
        if (device.Peripherals?.PeripheralList.Count > 0)
        {
            foreach (var peripheral in device.Peripherals.PeripheralList)
            {
                code.AppendLine($"        self.peripherals[\"{peripheral.Name}\"] = {{");
                code.AppendLine($"            base = {peripheral.Base},");
                code.AppendLine($"            type = \"{peripheral.Type}\",");
                code.AppendLine($"            description = \"{peripheral.Description}\",");
                code.AppendLine($"            registers = {{}}");
                code.AppendLine($"        }}");
                code.AppendLine($"        ");
                code.AppendLine($"        local p = self.peripherals[\"{peripheral.Name}\"]");

                foreach (var reg in peripheral.Registers)
                {
                    code.AppendLine($"        p.registers[\"{reg.Name}\"] = {{");
                    code.AppendLine($"            address = {reg.Address},");
                    code.AppendLine($"            size = {reg.Size},");
                    code.AppendLine($"            value = 0");
                    code.AppendLine($"        }}");
                }
            }
        }
        code.AppendLine($"    end");
        code.AppendLine($"    ");

        // Standard methods
        code.AppendLine($"    -- 读取寄存器");
        code.AppendLine($"    function self:read_register(name)");
        code.AppendLine($"        local reg = self.registers[name]");
        code.AppendLine($"        if reg then return reg.value end");
        code.AppendLine($"        error(\"寄存器 \" .. name .. \" 不存在\")");
        code.AppendLine($"    end");
        code.AppendLine($"    ");
        code.AppendLine($"    -- 写入寄存器");
        code.AppendLine($"    function self:write_register(name, value)");
        code.AppendLine($"        local reg = self.registers[name]");
        code.AppendLine($"        if reg then");
        code.AppendLine($"            local max_value = bit.lshift(1, reg.size * 8) - 1");
        code.AppendLine($"            if value < 0 or value > max_value then");
        code.AppendLine($"                error(\"值 \" .. value .. \" 超出范围 [0, \" .. max_value .. \"]\")");
        code.AppendLine($"            end");
        code.AppendLine($"            reg.value = value");
        code.AppendLine($"        else");
        code.AppendLine($"            error(\"寄存器 \" .. name .. \" 不存在\")");
        code.AppendLine($"        end");
        code.AppendLine($"    end");
        code.AppendLine($"    ");
        code.AppendLine($"    -- 设置位");
        code.AppendLine($"    function self:set_bit(register_name, bit, value)");
        code.AppendLine($"        local reg = self.registers[register_name]");
        code.AppendLine($"        if reg then");
        code.AppendLine($"            if value then");
        code.AppendLine($"                reg.value = bit.bor(reg.value, bit.lshift(1, bit))");
        code.AppendLine($"            else");
        code.AppendLine($"                reg.value = bit.band(reg.value, bit.bnot(bit.lshift(1, bit)))");
        code.AppendLine($"            end");
        code.AppendLine($"        else");
        code.AppendLine($"            error(\"寄存器 \" .. register_name .. \" 不存在\")");
        code.AppendLine($"        end");
        code.AppendLine($"    end");
        code.AppendLine($"    ");
        code.AppendLine($"    -- 获取位");
        code.AppendLine($"    function self:get_bit(register_name, bit)");
        code.AppendLine($"        local reg = self.registers[register_name]");
        code.AppendLine($"        if reg then");
        code.AppendLine($"            return bit.band(bit.rshift(reg.value, bit), 1) == 1");
        code.AppendLine($"        end");
        code.AppendLine($"        error(\"寄存器 \" .. register_name .. \" 不存在\")");
        code.AppendLine($"    end");
        code.AppendLine($"    ");
        code.AppendLine($"    -- 获取设备信息");
        code.AppendLine($"    function self:get_device_info()");
        code.AppendLine($"        return {{");
        code.AppendLine($"            name = {_moduleName}.DEVICE_NAME,");
        code.AppendLine($"            manufacturer = {_moduleName}.MANUFACTURER,");
        code.AppendLine($"            family = {_moduleName}.FAMILY,");
        code.AppendLine($"            version = {_moduleName}.VERSION,");
        code.AppendLine($"            architecture = {_moduleName}.ARCHITECTURE,");
        code.AppendLine($"            bits = {_moduleName}.BITS,");
        code.AppendLine($"            clock_frequency = {_moduleName}.CLOCK_FREQUENCY");
        code.AppendLine($"        }}");
        code.AppendLine($"    end");
        code.AppendLine($"    ");
        code.AppendLine($"    -- 获取寄存器信息");
        code.AppendLine($"    function self:get_register_info(name)");
        code.AppendLine($"        return self.registers[name]");
        code.AppendLine($"    end");
        code.AppendLine($"    ");
        code.AppendLine($"    -- 获取外设信息");
        code.AppendLine($"    function self:get_peripheral_info(name)");
        code.AppendLine($"        return self.peripherals[name]");
        code.AppendLine($"    end");
        code.AppendLine($"    ");
        code.AppendLine($"    -- 重置设备");
        code.AppendLine($"    function self:reset()");
        code.AppendLine($"        for _, reg in pairs(self.registers) do reg.value = 0 end");
        code.AppendLine($"        for _, peripheral in pairs(self.peripherals) do");
        code.AppendLine($"            for _, reg in pairs(peripheral.registers) do reg.value = 0 end");
        code.AppendLine($"        end");
        code.AppendLine($"    end");
        code.AppendLine($"    ");
        code.AppendLine($"    -- 字符串表示");
        code.AppendLine($"    function self:__tostring()");
        code.AppendLine($"        local info = self:get_device_info()");
        code.AppendLine($"        return string.format(\"{_moduleName}(%s v%s)\", info.name, info.version)");
        code.AppendLine($"    end");
        code.AppendLine($"    ");
        code.AppendLine($"    -- 初始化");
        code.AppendLine($"    self:_init_registers()");
        code.AppendLine($"    self:_init_peripherals()");
        code.AppendLine($"    ");
        code.AppendLine($"    setmetatable(self, {{ __tostring = self.__tostring }})");
        code.AppendLine($"    ");
        code.AppendLine($"    return self");
        code.AppendLine($"end");
        code.AppendLine();
    }

    private void EmitUtilityFunctions(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"-- 工具函数");
        code.AppendLine($"function {_moduleName}.hex(value, width)");
        code.AppendLine($"    width = width or 2");
        code.AppendLine($"    return string.format(\"0x%0\" .. width .. \"X\", value)");
        code.AppendLine($"end");
        code.AppendLine($"");
        code.AppendLine($"function {_moduleName}.bin(value, width)");
        code.AppendLine($"    width = width or 8");
        code.AppendLine($"    local result = \"\"");
        code.AppendLine($"    for i = width-1, 0, -1 do");
        code.AppendLine($"        result = result .. (bit.band(bit.rshift(value, i), 1))");
        code.AppendLine($"    end");
        code.AppendLine($"    return \"0b\" .. result");
        code.AppendLine($"end");
        code.AppendLine($"");
        code.AppendLine($"function {_moduleName}.print_device_info(device)");
        code.AppendLine($"    device = device or {_moduleName}.new()");
        code.AppendLine($"    local info = device:get_device_info()");
        code.AppendLine($"    print(\"设备信息:\")");
        code.AppendLine($"    print(\"  名称: \" .. info.name)");
        code.AppendLine($"    print(\"  厂商: \" .. info.manufacturer)");
        code.AppendLine($"    print(\"  系列: \" .. info.family)");
        code.AppendLine($"    print(\"  版本: \" .. info.version)");
        code.AppendLine($"    print(\"  架构: \" .. info.architecture)");
        code.AppendLine($"    print(\"  位宽: \" .. info.bits)");
        code.AppendLine($"    print(\"  时钟: \" .. info.clock_frequency .. \" Hz\")");
        code.AppendLine($"end");
        code.AppendLine($"");
        code.AppendLine($"function {_moduleName}.print_registers(device)");
        code.AppendLine($"    device = device or {_moduleName}.new()");
        code.AppendLine($"    print(\"寄存器状态:\")");
        code.AppendLine($"    for name, reg in pairs(device.registers) do");
        code.AppendLine($"        print(string.format(\"  %-8s: %s (%s)\",");
        code.AppendLine($"            name,");
        code.AppendLine($"            {_moduleName}.hex(reg.value, reg.size * 2),");
        code.AppendLine($"            reg.description))");
        code.AppendLine($"    end");
        code.AppendLine($"end");
        code.AppendLine();
    }

    private void EmitExample(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"-- 示例代码");
        code.AppendLine($"function {_moduleName}.example()");
        code.AppendLine($"    print(\"=== {device.Metadata.Name}设备示例 ===\")");
        code.AppendLine($"    local device = {_moduleName}.new()");
        code.AppendLine($"    print(\"创建设备: \" .. tostring(device))");
        code.AppendLine($"    {_moduleName}.print_device_info(device)");

        if (device.Cpu.Registers.RegisterList.Count > 0)
        {
            var firstReg = device.Cpu.Registers.RegisterList[0];
            code.AppendLine($"    if device.registers[\"{firstReg.Name}\"] then");
            code.AppendLine($"        print(\"\\n演示寄存器操作:\")");
            code.AppendLine($"        device:write_register(\"{firstReg.Name}\", 0x55)");
            code.AppendLine($"        print(\"写入 {firstReg.Name}: \" .. {_moduleName}.hex(0x55))");
            code.AppendLine($"        local value = device:read_register(\"{firstReg.Name}\")");
            code.AppendLine($"        print(\"读取 {firstReg.Name}: \" .. {_moduleName}.hex(value))");
            code.AppendLine($"        device:set_bit(\"{firstReg.Name}\", 0, true)");
            code.AppendLine($"        local bit0 = device:get_bit(\"{firstReg.Name}\", 0)");
            code.AppendLine($"        print(\"位0: \" .. tostring(bit0))");
            code.AppendLine($"    end");
        }

        code.AppendLine($"    {_moduleName}.print_registers(device)");
        code.AppendLine($"    device:reset()");
        code.AppendLine($"    print(\"\\n设备已重置\")");
        code.AppendLine($"    print(\"=== 示例完成 ===\")");
        code.AppendLine($"end");
        code.AppendLine();
        code.AppendLine($"-- 如果直接运行此文件，执行示例");
        code.AppendLine($"if arg and arg[0]:find(\"{_moduleName}.lua$\") then");
        code.AppendLine($"    {_moduleName}.example()");
        code.AppendLine($"end");
        code.AppendLine();
    }

    protected override void EmitModuleEnd(StringBuilder code, DeviceModel device)
    {
        code.AppendLine($"return {_moduleName}");
    }

    protected override void EmitGuardStart(StringBuilder code, string guardName) { }
    protected override void EmitGuardEnd(StringBuilder code, string guardName) { }
}

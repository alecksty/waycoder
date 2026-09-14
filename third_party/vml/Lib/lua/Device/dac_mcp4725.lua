--[[
  MCP4725设备定义 - Lua模块
  生成自: Microchip/DAC/MCP4725
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: MCP4725 12-bit I2C DAC (single channel, EEPROM)
  CPU架构: DAC
  位宽: 12位
  时钟频率: 400000 Hz
]]

local MCP4725 = {}

-- 设备信息
MCP4725.DEVICE_NAME = "MCP4725"
MCP4725.MANUFACTURER = "Microchip"
MCP4725.FAMILY = "DAC"
MCP4725.VERSION = "1.0"
MCP4725.ARCHITECTURE = "DAC"
MCP4725.BITS = 12
MCP4725.CLOCK_FREQUENCY = 400000

-- 内存段定义
MCP4725.EEPROM_START = 0x00
MCP4725.EEPROM_END = 0x01
MCP4725.EEPROM_SIZE = 2  -- Power-on default DAC value

-- 外设定义
-- MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)
MCP4725.MCP4725_BASE = 0x60
MCP4725.MCP4725_DAC_VALUE_ADDR = 0x00
MCP4725.MCP4725_DAC_VALUE_PD_BIT = 12  -- Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ
MCP4725.MCP4725_WRITE_EEPROM_ADDR = 0x60

-- 设备类
function MCP4725.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["MCP4725"] = {
            base = 0x60,
            type = "I2C",
            description = "MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)",
            registers = {}
        }
        
        local p = self.peripherals["MCP4725"]
        p.registers["DAC_VALUE"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["WRITE_EEPROM"] = {
            address = 0x60,
            size = 2,
            value = 0
        }
    end
    
    -- 读取寄存器
    function self:read_register(name)
        local reg = self.registers[name]
        if reg then
            return reg.value
        end
        error("寄存器 " .. name .. " 不存在")
    end
    
    -- 写入寄存器
    function self:write_register(name, value)
        local reg = self.registers[name]
        if reg then
            local max_value = bit.lshift(1, reg.size * 8) - 1
            if value < 0 or value > max_value then
                error("值 " .. value .. " 超出范围 [0, " .. max_value .. "]")
            end
            reg.value = value
        else
            error("寄存器 " .. name .. " 不存在")
        end
    end
    
    -- 设置位
    function self:set_bit(register_name, bit, value)
        local reg = self.registers[register_name]
        if reg then
            if value then
                reg.value = bit.bor(reg.value, bit.lshift(1, bit))
            else
                reg.value = bit.band(reg.value, bit.bnot(bit.lshift(1, bit)))
            end
        else
            error("寄存器 " .. register_name .. " 不存在")
        end
    end
    
    -- 获取位
    function self:get_bit(register_name, bit)
        local reg = self.registers[register_name]
        if reg then
            return bit.band(bit.rshift(reg.value, bit), 1) == 1
        end
        error("寄存器 " .. register_name .. " 不存在")
    end
    
    -- 获取设备信息
    function self:get_device_info()
        return {
            name = MCP4725.DEVICE_NAME,
            manufacturer = MCP4725.MANUFACTURER,
            family = MCP4725.FAMILY,
            version = MCP4725.VERSION,
            architecture = MCP4725.ARCHITECTURE,
            bits = MCP4725.BITS,
            clock_frequency = MCP4725.CLOCK_FREQUENCY
        }
    end
    
    -- 获取寄存器信息
    function self:get_register_info(name)
        return self.registers[name]
    end
    
    -- 获取外设信息
    function self:get_peripheral_info(name)
        return self.peripherals[name]
    end
    
    -- 重置设备
    function self:reset()
        for _, reg in pairs(self.registers) do
            reg.value = 0
        end
        
        for _, peripheral in pairs(self.peripherals) do
            for _, reg in pairs(peripheral.registers) do
                reg.value = 0
            end
        end
    end
    
    -- 字符串表示
    function self:__tostring()
        local info = self:get_device_info()
        return string.format("MCP4725(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function MCP4725.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function MCP4725.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function MCP4725.print_device_info(device)
    device = device or MCP4725.new()
    local info = device:get_device_info()
    
    print("设备信息:")
    print("  名称: " .. info.name)
    print("  厂商: " .. info.manufacturer)
    print("  系列: " .. info.family)
    print("  版本: " .. info.version)
    print("  架构: " .. info.architecture)
    print("  位宽: " .. info.bits)
    print("  时钟: " .. info.clock_frequency .. " Hz")
end

function MCP4725.print_registers(device)
    device = device or MCP4725.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            MCP4725.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function MCP4725.example()
    print("=== MCP4725设备示例 ===")
    
    -- 创建设备实例
    local device = MCP4725.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    MCP4725.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    MCP4725.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("MCP4725.lua$") then
    MCP4725.example()
end

return MCP4725

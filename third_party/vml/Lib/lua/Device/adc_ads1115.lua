--[[
  ADS1115设备定义 - Lua模块
  生成自: Texas Instruments/ADC/ADS1115
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: ADS1115 16-bit I2C ADC (4-channel, PGA, 860SPS)
  CPU架构: ADC
  位宽: 16位
  时钟频率: 400000 Hz
]]

local ADS1115 = {}

-- 设备信息
ADS1115.DEVICE_NAME = "ADS1115"
ADS1115.MANUFACTURER = "Texas Instruments"
ADS1115.FAMILY = "ADC"
ADS1115.VERSION = "1.0"
ADS1115.ARCHITECTURE = "ADC"
ADS1115.BITS = 16
ADS1115.CLOCK_FREQUENCY = 400000

-- 外设定义
-- ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V)
ADS1115.ADS1115_BASE = 0x48
ADS1115.ADS1115_CONV_RESULT_ADDR = 0x00
ADS1115.ADS1115_CONFIG_ADDR = 0x01
ADS1115.ADS1115_CONFIG_OS_BIT = 15  -- Operational status/start single-shot
ADS1115.ADS1115_CONFIG_MUX_BIT = 12  -- Input multiplexer: 0=A0-A1,1=A0-A3,2=A1-A3,3=A2-A3,4=A0,5=A1,6=A2,7=A3
ADS1115.ADS1115_CONFIG_PGA_BIT = 9  -- PGA gain: 0=±6.144V,1=±4.096V,2=±2.048V,3=±1.024V,4=±0.512V,5=±0.256V
ADS1115.ADS1115_CONFIG_MODE_BIT = 8  -- 0=continuous, 1=single-shot
ADS1115.ADS1115_CONFIG_DR_BIT = 5  -- Data rate: 0=8,1=16,2=32,3=64,4=128,5=250,6=475,7=860 SPS
ADS1115.ADS1115_CONFIG_COMP_MODE_BIT = 4  -- Comparator mode (0=traditional, 1=window)
ADS1115.ADS1115_CONFIG_COMP_POL_BIT = 3  -- Comparator polarity (0=active low, 1=active high)
ADS1115.ADS1115_LO_THRESH_ADDR = 0x02
ADS1115.ADS1115_HI_THRESH_ADDR = 0x03

-- 设备类
function ADS1115.new(memory_base)
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
        self.peripherals["ADS1115"] = {
            base = 0x48,
            type = "I2C",
            description = "ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V)",
            registers = {}
        }
        
        local p = self.peripherals["ADS1115"]
        p.registers["CONV_RESULT"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["CONFIG"] = {
            address = 0x01,
            size = 2,
            value = 0
        }
        p.registers["LO_THRESH"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["HI_THRESH"] = {
            address = 0x03,
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
            name = ADS1115.DEVICE_NAME,
            manufacturer = ADS1115.MANUFACTURER,
            family = ADS1115.FAMILY,
            version = ADS1115.VERSION,
            architecture = ADS1115.ARCHITECTURE,
            bits = ADS1115.BITS,
            clock_frequency = ADS1115.CLOCK_FREQUENCY
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
        return string.format("ADS1115(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ADS1115.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ADS1115.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ADS1115.print_device_info(device)
    device = device or ADS1115.new()
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

function ADS1115.print_registers(device)
    device = device or ADS1115.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ADS1115.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ADS1115.example()
    print("=== ADS1115设备示例 ===")
    
    -- 创建设备实例
    local device = ADS1115.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ADS1115.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    ADS1115.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ADS1115.lua$") then
    ADS1115.example()
end

return ADS1115

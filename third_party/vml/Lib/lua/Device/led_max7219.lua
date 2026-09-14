--[[
  MAX7219设备定义 - Lua模块
  生成自: Maxim/LED/MAX7219
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: MAX7219 8-Digit LED Display Driver (SPI, daisy-chainable, 8x8 matrix)
  CPU架构: LED
  位宽: 8位
  时钟频率: 10000000 Hz
]]

local MAX7219 = {}

-- 设备信息
MAX7219.DEVICE_NAME = "MAX7219"
MAX7219.MANUFACTURER = "Maxim"
MAX7219.FAMILY = "LED"
MAX7219.VERSION = "1.0"
MAX7219.ARCHITECTURE = "LED"
MAX7219.BITS = 8
MAX7219.CLOCK_FREQUENCY = 10000000

-- 外设定义
-- MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)
MAX7219.MAX7219_BASE = 0x00
MAX7219.MAX7219_DIGIT0_ADDR = 0x01
MAX7219.MAX7219_DIGIT1_ADDR = 0x02
MAX7219.MAX7219_DIGIT2_ADDR = 0x03
MAX7219.MAX7219_DIGIT3_ADDR = 0x04
MAX7219.MAX7219_DIGIT4_ADDR = 0x05
MAX7219.MAX7219_DIGIT5_ADDR = 0x06
MAX7219.MAX7219_DIGIT6_ADDR = 0x07
MAX7219.MAX7219_DIGIT7_ADDR = 0x08
MAX7219.MAX7219_DECODE_ADDR = 0x09
MAX7219.MAX7219_INTENSITY_ADDR = 0x0A
MAX7219.MAX7219_SCAN_LIMIT_ADDR = 0x0B
MAX7219.MAX7219_SHUTDOWN_ADDR = 0x0C
MAX7219.MAX7219_TEST_ADDR = 0x0F

-- 设备类
function MAX7219.new(memory_base)
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
        self.peripherals["MAX7219"] = {
            base = 0x00,
            type = "SPI",
            description = "MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)",
            registers = {}
        }
        
        local p = self.peripherals["MAX7219"]
        p.registers["DIGIT0"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["DIGIT1"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["DIGIT2"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["DIGIT3"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["DIGIT4"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["DIGIT5"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["DIGIT6"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["DIGIT7"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["DECODE"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["INTENSITY"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["SCAN_LIMIT"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        p.registers["SHUTDOWN"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        p.registers["TEST"] = {
            address = 0x0F,
            size = 1,
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
            name = MAX7219.DEVICE_NAME,
            manufacturer = MAX7219.MANUFACTURER,
            family = MAX7219.FAMILY,
            version = MAX7219.VERSION,
            architecture = MAX7219.ARCHITECTURE,
            bits = MAX7219.BITS,
            clock_frequency = MAX7219.CLOCK_FREQUENCY
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
        return string.format("MAX7219(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function MAX7219.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function MAX7219.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function MAX7219.print_device_info(device)
    device = device or MAX7219.new()
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

function MAX7219.print_registers(device)
    device = device or MAX7219.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            MAX7219.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function MAX7219.example()
    print("=== MAX7219设备示例 ===")
    
    -- 创建设备实例
    local device = MAX7219.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    MAX7219.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    MAX7219.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("MAX7219.lua$") then
    MAX7219.example()
end

return MAX7219

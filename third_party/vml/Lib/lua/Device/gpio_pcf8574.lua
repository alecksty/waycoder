--[[
  PCF8574设备定义 - Lua模块
  生成自: NXP/TI/GPIO/PCF8574
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: PCF8574 8-bit I2C GPIO Expander (quasi-bidirectional, interrupt)
  CPU架构: GPIO
  位宽: 8位
  时钟频率: 100000 Hz
]]

local PCF8574 = {}

-- 设备信息
PCF8574.DEVICE_NAME = "PCF8574"
PCF8574.MANUFACTURER = "NXP/TI"
PCF8574.FAMILY = "GPIO"
PCF8574.VERSION = "1.0"
PCF8574.ARCHITECTURE = "GPIO"
PCF8574.BITS = 8
PCF8574.CLOCK_FREQUENCY = 100000

-- 外设定义
-- PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)
PCF8574.PCF8574_BASE = 0x20
PCF8574.PCF8574_INPUT_ADDR = 0x00
PCF8574.PCF8574_INPUT_P0_BIT = 0  -- Pin P0
PCF8574.PCF8574_INPUT_P1_BIT = 1  -- Pin P1
PCF8574.PCF8574_INPUT_P2_BIT = 2  -- Pin P2
PCF8574.PCF8574_INPUT_P3_BIT = 3  -- Pin P3
PCF8574.PCF8574_INPUT_P4_BIT = 4  -- Pin P4
PCF8574.PCF8574_INPUT_P5_BIT = 5  -- Pin P5
PCF8574.PCF8574_INPUT_P6_BIT = 6  -- Pin P6
PCF8574.PCF8574_INPUT_P7_BIT = 7  -- Pin P7
PCF8574.PCF8574_OUTPUT_ADDR = 0x01
PCF8574.PCF8574_POLARITY_ADDR = 0x02

-- 中断向量定义
PCF8574.INT_INT = 0  -- Pin change interrupt (open-drain, active low)

-- 设备类
function PCF8574.new(memory_base)
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
        self.peripherals["PCF8574"] = {
            base = 0x20,
            type = "I2C",
            description = "PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)",
            registers = {}
        }
        
        local p = self.peripherals["PCF8574"]
        p.registers["INPUT"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["OUTPUT"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["POLARITY"] = {
            address = 0x02,
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
            name = PCF8574.DEVICE_NAME,
            manufacturer = PCF8574.MANUFACTURER,
            family = PCF8574.FAMILY,
            version = PCF8574.VERSION,
            architecture = PCF8574.ARCHITECTURE,
            bits = PCF8574.BITS,
            clock_frequency = PCF8574.CLOCK_FREQUENCY
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
        return string.format("PCF8574(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function PCF8574.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function PCF8574.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function PCF8574.print_device_info(device)
    device = device or PCF8574.new()
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

function PCF8574.print_registers(device)
    device = device or PCF8574.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            PCF8574.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function PCF8574.example()
    print("=== PCF8574设备示例 ===")
    
    -- 创建设备实例
    local device = PCF8574.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    PCF8574.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    PCF8574.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("PCF8574.lua$") then
    PCF8574.example()
end

return PCF8574

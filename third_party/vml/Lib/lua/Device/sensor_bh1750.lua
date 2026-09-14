--[[
  BH1750设备定义 - Lua模块
  生成自: ROHM/Sensor/BH1750
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: BH1750FVI Digital Ambient Light Sensor (I2C, 1-65535 lux, 16-bit)
  CPU架构: Sensor
  位宽: 16位
  时钟频率: 400000 Hz
]]

local BH1750 = {}

-- 设备信息
BH1750.DEVICE_NAME = "BH1750"
BH1750.MANUFACTURER = "ROHM"
BH1750.FAMILY = "Sensor"
BH1750.VERSION = "1.0"
BH1750.ARCHITECTURE = "Sensor"
BH1750.BITS = 16
BH1750.CLOCK_FREQUENCY = 400000

-- 外设定义
-- BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)
BH1750.BH1750_BASE = 0x23
BH1750.BH1750_LUX_ADDR = 0x00
BH1750.BH1750_MODE_ADDR = 0x01
BH1750.BH1750_MODE_CONT_H_BIT = 0  -- Continuous High Res (1lx, 120ms)
BH1750.BH1750_MODE_CONT_H2_BIT = 1  -- Continuous High Res 2 (0.5lx, 120ms)
BH1750.BH1750_MODE_CONT_L_BIT = 2  -- Continuous Low Res (4lx, 16ms)
BH1750.BH1750_MODE_ONCE_H_BIT = 3  -- One-time High Res (1lx, 120ms)
BH1750.BH1750_MODE_ONCE_H2_BIT = 4  -- One-time High Res 2 (0.5lx, 120ms)
BH1750.BH1750_MODE_ONCE_L_BIT = 5  -- One-time Low Res (4lx, 16ms)
BH1750.BH1750_CMD_POWER_ON_ADDR = 0x01
BH1750.BH1750_CMD_POWER_OFF_ADDR = 0x00
BH1750.BH1750_CMD_RESET_ADDR = 0x07

-- 设备类
function BH1750.new(memory_base)
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
        self.peripherals["BH1750"] = {
            base = 0x23,
            type = "I2C",
            description = "BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)",
            registers = {}
        }
        
        local p = self.peripherals["BH1750"]
        p.registers["LUX"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["MODE"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["CMD_POWER_ON"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["CMD_POWER_OFF"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["CMD_RESET"] = {
            address = 0x07,
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
            name = BH1750.DEVICE_NAME,
            manufacturer = BH1750.MANUFACTURER,
            family = BH1750.FAMILY,
            version = BH1750.VERSION,
            architecture = BH1750.ARCHITECTURE,
            bits = BH1750.BITS,
            clock_frequency = BH1750.CLOCK_FREQUENCY
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
        return string.format("BH1750(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function BH1750.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function BH1750.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function BH1750.print_device_info(device)
    device = device or BH1750.new()
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

function BH1750.print_registers(device)
    device = device or BH1750.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            BH1750.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function BH1750.example()
    print("=== BH1750设备示例 ===")
    
    -- 创建设备实例
    local device = BH1750.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    BH1750.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    BH1750.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("BH1750.lua$") then
    BH1750.example()
end

return BH1750

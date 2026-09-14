--[[
  APDS9960设备定义 - Lua模块
  生成自: Broadcom/Avago/Sensor/APDS9960
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: APDS9960 Gesture/Proximity/Ambient Light/RGB Sensor (I2C)
  CPU架构: Sensor
  位宽: 8位
  时钟频率: 400000 Hz
]]

local APDS9960 = {}

-- 设备信息
APDS9960.DEVICE_NAME = "APDS9960"
APDS9960.MANUFACTURER = "Broadcom/Avago"
APDS9960.FAMILY = "Sensor"
APDS9960.VERSION = "1.0"
APDS9960.ARCHITECTURE = "Sensor"
APDS9960.BITS = 8
APDS9960.CLOCK_FREQUENCY = 400000

-- 外设定义
-- APDS9960 Gesture/RGB Sensor (0x39, 3.3V)
APDS9960.APDS9960_BASE = 0x39
APDS9960.APDS9960_ENABLE_ADDR = 0x80
APDS9960.APDS9960_GESTURE_ADDR = 0xFC
APDS9960.APDS9960_PROXIMITY_ADDR = 0x9C
APDS9960.APDS9960_AMBIENT_ADDR = 0x96
APDS9960.APDS9960_RED_ADDR = 0x98
APDS9960.APDS9960_GREEN_ADDR = 0x9A
APDS9960.APDS9960_BLUE_ADDR = 0x9C
APDS9960.APDS9960_GESTURE_FIFO_ADDR = 0xFC
APDS9960.APDS9960_GESTURE_COUNT_ADDR = 0xFD

-- 中断向量定义
APDS9960.INT_INT = 0  -- Gesture/Proximity/Light interrupt

-- 设备类
function APDS9960.new(memory_base)
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
        self.peripherals["APDS9960"] = {
            base = 0x39,
            type = "I2C",
            description = "APDS9960 Gesture/RGB Sensor (0x39, 3.3V)",
            registers = {}
        }
        
        local p = self.peripherals["APDS9960"]
        p.registers["ENABLE"] = {
            address = 0x80,
            size = 1,
            value = 0
        }
        p.registers["GESTURE"] = {
            address = 0xFC,
            size = 1,
            value = 0
        }
        p.registers["PROXIMITY"] = {
            address = 0x9C,
            size = 1,
            value = 0
        }
        p.registers["AMBIENT"] = {
            address = 0x96,
            size = 2,
            value = 0
        }
        p.registers["RED"] = {
            address = 0x98,
            size = 2,
            value = 0
        }
        p.registers["GREEN"] = {
            address = 0x9A,
            size = 2,
            value = 0
        }
        p.registers["BLUE"] = {
            address = 0x9C,
            size = 2,
            value = 0
        }
        p.registers["GESTURE_FIFO"] = {
            address = 0xFC,
            size = 4,
            value = 0
        }
        p.registers["GESTURE_COUNT"] = {
            address = 0xFD,
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
            name = APDS9960.DEVICE_NAME,
            manufacturer = APDS9960.MANUFACTURER,
            family = APDS9960.FAMILY,
            version = APDS9960.VERSION,
            architecture = APDS9960.ARCHITECTURE,
            bits = APDS9960.BITS,
            clock_frequency = APDS9960.CLOCK_FREQUENCY
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
        return string.format("APDS9960(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function APDS9960.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function APDS9960.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function APDS9960.print_device_info(device)
    device = device or APDS9960.new()
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

function APDS9960.print_registers(device)
    device = device or APDS9960.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            APDS9960.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function APDS9960.example()
    print("=== APDS9960设备示例 ===")
    
    -- 创建设备实例
    local device = APDS9960.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    APDS9960.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    APDS9960.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("APDS9960.lua$") then
    APDS9960.example()
end

return APDS9960

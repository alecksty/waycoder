--[[
  L298N设备定义 - Lua模块
  生成自: STMicroelectronics/Motor/L298N
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: L298N Dual H-Bridge DC Motor Driver (2A per channel, 5V-35V)
  CPU架构: Motor
  位宽: 8位
  时钟频率: 0 Hz
]]

local L298N = {}

-- 设备信息
L298N.DEVICE_NAME = "L298N"
L298N.MANUFACTURER = "STMicroelectronics"
L298N.FAMILY = "Motor"
L298N.VERSION = "1.0"
L298N.ARCHITECTURE = "Motor"
L298N.BITS = 8
L298N.CLOCK_FREQUENCY = 0

-- 外设定义
-- L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)
L298N.L298N_BASE = 0x00
L298N.L298N_MOTOR_A_ADDR = 0x00
L298N.L298N_MOTOR_A_IN1_BIT = 0  -- Motor A Input 1
L298N.L298N_MOTOR_A_IN2_BIT = 1  -- Motor A Input 2
L298N.L298N_MOTOR_A_ENA_BIT = 2  -- Motor A Enable/PWM
L298N.L298N_MOTOR_B_ADDR = 0x01
L298N.L298N_MOTOR_B_IN3_BIT = 0  -- Motor B Input 3
L298N.L298N_MOTOR_B_IN4_BIT = 1  -- Motor B Input 4
L298N.L298N_MOTOR_B_ENB_BIT = 2  -- Motor B Enable/PWM
L298N.L298N_SPEED_A_ADDR = 0x02
L298N.L298N_SPEED_B_ADDR = 0x03
L298N.L298N_STATUS_ADDR = 0x04

-- 设备类
function L298N.new(memory_base)
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
        self.peripherals["L298N"] = {
            base = 0x00,
            type = "GPIO",
            description = "L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)",
            registers = {}
        }
        
        local p = self.peripherals["L298N"]
        p.registers["MOTOR_A"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["MOTOR_B"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["SPEED_A"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["SPEED_B"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x04,
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
            name = L298N.DEVICE_NAME,
            manufacturer = L298N.MANUFACTURER,
            family = L298N.FAMILY,
            version = L298N.VERSION,
            architecture = L298N.ARCHITECTURE,
            bits = L298N.BITS,
            clock_frequency = L298N.CLOCK_FREQUENCY
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
        return string.format("L298N(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function L298N.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function L298N.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function L298N.print_device_info(device)
    device = device or L298N.new()
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

function L298N.print_registers(device)
    device = device or L298N.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            L298N.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function L298N.example()
    print("=== L298N设备示例 ===")
    
    -- 创建设备实例
    local device = L298N.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    L298N.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    L298N.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("L298N.lua$") then
    L298N.example()
end

return L298N

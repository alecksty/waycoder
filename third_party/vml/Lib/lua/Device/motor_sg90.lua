--[[
  SG90设备定义 - Lua模块
  生成自: Tower Pro/Motor/SG90
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: SG90 Micro Servo Motor (0-180°, 4.8V-6V)
  CPU架构: Motor
  位宽: 8位
  时钟频率: 0 Hz
]]

local SG90 = {}

-- 设备信息
SG90.DEVICE_NAME = "SG90"
SG90.MANUFACTURER = "Tower Pro"
SG90.FAMILY = "Motor"
SG90.VERSION = "1.0"
SG90.ARCHITECTURE = "Motor"
SG90.BITS = 8
SG90.CLOCK_FREQUENCY = 0

-- 外设定义
-- SG90 Micro Servo (500-2500us pulse, 50Hz)
SG90.SG90_BASE = 0x00
SG90.SG90_ANGLE_ADDR = 0x00
SG90.SG90_PULSE_MIN_ADDR = 0x01
SG90.SG90_PULSE_MAX_ADDR = 0x03
SG90.SG90_CURRENT_ANGLE_ADDR = 0x05
SG90.SG90_SPEED_ADDR = 0x06

-- 设备类
function SG90.new(memory_base)
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
        self.peripherals["SG90"] = {
            base = 0x00,
            type = "PWM",
            description = "SG90 Micro Servo (500-2500us pulse, 50Hz)",
            registers = {}
        }
        
        local p = self.peripherals["SG90"]
        p.registers["ANGLE"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["PULSE_MIN"] = {
            address = 0x01,
            size = 2,
            value = 0
        }
        p.registers["PULSE_MAX"] = {
            address = 0x03,
            size = 2,
            value = 0
        }
        p.registers["CURRENT_ANGLE"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["SPEED"] = {
            address = 0x06,
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
            name = SG90.DEVICE_NAME,
            manufacturer = SG90.MANUFACTURER,
            family = SG90.FAMILY,
            version = SG90.VERSION,
            architecture = SG90.ARCHITECTURE,
            bits = SG90.BITS,
            clock_frequency = SG90.CLOCK_FREQUENCY
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
        return string.format("SG90(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function SG90.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function SG90.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function SG90.print_device_info(device)
    device = device or SG90.new()
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

function SG90.print_registers(device)
    device = device or SG90.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            SG90.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function SG90.example()
    print("=== SG90设备示例 ===")
    
    -- 创建设备实例
    local device = SG90.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    SG90.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    SG90.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("SG90.lua$") then
    SG90.example()
end

return SG90

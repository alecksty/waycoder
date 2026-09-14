--[[
  A4988设备定义 - Lua模块
  生成自: Allegro/Motor/A4988
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: A4988 Stepper Motor Driver (up to 1/16 microstepping, 2A, 8V-35V)
  CPU架构: Motor
  位宽: 8位
  时钟频率: 0 Hz
]]

local A4988 = {}

-- 设备信息
A4988.DEVICE_NAME = "A4988"
A4988.MANUFACTURER = "Allegro"
A4988.FAMILY = "Motor"
A4988.VERSION = "1.0"
A4988.ARCHITECTURE = "Motor"
A4988.BITS = 8
A4988.CLOCK_FREQUENCY = 0

-- 外设定义
-- A4988 Stepper Motor Driver (3.3V/5V logic)
A4988.A4988_BASE = 0x00
A4988.A4988_CTRL_ADDR = 0x00
A4988.A4988_CTRL_STEP_BIT = 0  -- Step pulse (rising edge)
A4988.A4988_CTRL_DIR_BIT = 1  -- Direction (0=CW, 1=CCW)
A4988.A4988_CTRL_ENABLE_BIT = 2  -- Enable (active low)
A4988.A4988_CTRL_SLEEP_BIT = 3  -- Sleep mode (active low)
A4988.A4988_CTRL_RESET_BIT = 4  -- Reset (active low)
A4988.A4988_MICROSTEP_ADDR = 0x01
A4988.A4988_MICROSTEP_MS1_BIT = 0  -- Microstep select 1
A4988.A4988_MICROSTEP_MS2_BIT = 1  -- Microstep select 2
A4988.A4988_MICROSTEP_MS3_BIT = 2  -- Microstep select 3
A4988.A4988_STEPS_ADDR = 0x02
A4988.A4988_DELAY_US_ADDR = 0x06

-- 设备类
function A4988.new(memory_base)
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
        self.peripherals["A4988"] = {
            base = 0x00,
            type = "GPIO",
            description = "A4988 Stepper Motor Driver (3.3V/5V logic)",
            registers = {}
        }
        
        local p = self.peripherals["A4988"]
        p.registers["CTRL"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["MICROSTEP"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["STEPS"] = {
            address = 0x02,
            size = 4,
            value = 0
        }
        p.registers["DELAY_US"] = {
            address = 0x06,
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
            name = A4988.DEVICE_NAME,
            manufacturer = A4988.MANUFACTURER,
            family = A4988.FAMILY,
            version = A4988.VERSION,
            architecture = A4988.ARCHITECTURE,
            bits = A4988.BITS,
            clock_frequency = A4988.CLOCK_FREQUENCY
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
        return string.format("A4988(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function A4988.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function A4988.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function A4988.print_device_info(device)
    device = device or A4988.new()
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

function A4988.print_registers(device)
    device = device or A4988.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            A4988.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function A4988.example()
    print("=== A4988设备示例 ===")
    
    -- 创建设备实例
    local device = A4988.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    A4988.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    A4988.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("A4988.lua$") then
    A4988.example()
end

return A4988

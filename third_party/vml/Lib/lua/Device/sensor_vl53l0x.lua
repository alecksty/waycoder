--[[
  VL53L0X设备定义 - Lua模块
  生成自: STMicroelectronics/Sensor/VL53L0X
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: VL53L0X ToF Laser Distance Sensor (I2C, 2cm-200cm, 940nm VCSEL)
  CPU架构: Sensor
  位宽: 16位
  时钟频率: 400000 Hz
]]

local VL53L0X = {}

-- 设备信息
VL53L0X.DEVICE_NAME = "VL53L0X"
VL53L0X.MANUFACTURER = "STMicroelectronics"
VL53L0X.FAMILY = "Sensor"
VL53L0X.VERSION = "1.0"
VL53L0X.ARCHITECTURE = "Sensor"
VL53L0X.BITS = 16
VL53L0X.CLOCK_FREQUENCY = 400000

-- 外设定义
-- VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
VL53L0X.VL53L0X_BASE = 0x29
VL53L0X.VL53L0X_DISTANCE_ADDR = 0x00
VL53L0X.VL53L0X_SIGNAL_RATE_ADDR = 0x02
VL53L0X.VL53L0X_AMBIENT_RATE_ADDR = 0x04
VL53L0X.VL53L0X_SPAD_COUNT_ADDR = 0x06
VL53L0X.VL53L0X_RANGE_STATUS_ADDR = 0x08
VL53L0X.VL53L0X_TIMING_BUDGET_ADDR = 0x09
VL53L0X.VL53L0X_INTER_MEAS_ADDR = 0x0D
VL53L0X.VL53L0X_MODE_ADDR = 0x0E

-- 引脚定义
VL53L0X.PIN_XSHUT = 1  -- Shutdown pin (active low)
VL53L0X.PIN_INT = 2  -- Interrupt (open-drain)

-- 设备类
function VL53L0X.new(memory_base)
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
        self.peripherals["VL53L0X"] = {
            base = 0x29,
            type = "I2C",
            description = "VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)",
            registers = {}
        }
        
        local p = self.peripherals["VL53L0X"]
        p.registers["DISTANCE"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["SIGNAL_RATE"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["AMBIENT_RATE"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["SPAD_COUNT"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        p.registers["RANGE_STATUS"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["TIMING_BUDGET"] = {
            address = 0x09,
            size = 4,
            value = 0
        }
        p.registers["INTER_MEAS"] = {
            address = 0x0D,
            size = 4,
            value = 0
        }
        p.registers["MODE"] = {
            address = 0x0E,
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
            name = VL53L0X.DEVICE_NAME,
            manufacturer = VL53L0X.MANUFACTURER,
            family = VL53L0X.FAMILY,
            version = VL53L0X.VERSION,
            architecture = VL53L0X.ARCHITECTURE,
            bits = VL53L0X.BITS,
            clock_frequency = VL53L0X.CLOCK_FREQUENCY
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
        return string.format("VL53L0X(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function VL53L0X.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function VL53L0X.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function VL53L0X.print_device_info(device)
    device = device or VL53L0X.new()
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

function VL53L0X.print_registers(device)
    device = device or VL53L0X.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            VL53L0X.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function VL53L0X.example()
    print("=== VL53L0X设备示例 ===")
    
    -- 创建设备实例
    local device = VL53L0X.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    VL53L0X.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    VL53L0X.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("VL53L0X.lua$") then
    VL53L0X.example()
end

return VL53L0X

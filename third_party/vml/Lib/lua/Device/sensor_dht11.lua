--[[
  DHT11设备定义 - Lua模块
  生成自: Aosong/Sensor/DHT11
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: Digital Temperature and Humidity Sensor (1-Wire)
  CPU架构: Sensor
  位宽: 8位
  时钟频率: 500000 Hz
]]

local DHT11 = {}

-- 设备信息
DHT11.DEVICE_NAME = "DHT11"
DHT11.MANUFACTURER = "Aosong"
DHT11.FAMILY = "Sensor"
DHT11.VERSION = "1.0"
DHT11.ARCHITECTURE = "Sensor"
DHT11.BITS = 8
DHT11.CLOCK_FREQUENCY = 500000

-- 内存段定义
DHT11.PACKAGE_START = 0x00
DHT11.PACKAGE_END = 0x00
DHT11.PACKAGE_SIZE = 4  -- DIP-4/SMD-4

-- 外设定义
-- DHT11 1-Wire Sensor (3.0V-5.5V)
DHT11.DHT11_BASE = 0x00
DHT11.DHT11_HUMIDITY_INT_ADDR = 0x00
DHT11.DHT11_HUMIDITY_DEC_ADDR = 0x01
DHT11.DHT11_TEMP_INT_ADDR = 0x02
DHT11.DHT11_TEMP_DEC_ADDR = 0x03
DHT11.DHT11_CHECKSUM_ADDR = 0x04

-- 设备类
function DHT11.new(memory_base)
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
        self.peripherals["DHT11"] = {
            base = 0x00,
            type = "1Wire",
            description = "DHT11 1-Wire Sensor (3.0V-5.5V)",
            registers = {}
        }
        
        local p = self.peripherals["DHT11"]
        p.registers["HUMIDITY_INT"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["HUMIDITY_DEC"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["TEMP_INT"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["TEMP_DEC"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["CHECKSUM"] = {
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
            name = DHT11.DEVICE_NAME,
            manufacturer = DHT11.MANUFACTURER,
            family = DHT11.FAMILY,
            version = DHT11.VERSION,
            architecture = DHT11.ARCHITECTURE,
            bits = DHT11.BITS,
            clock_frequency = DHT11.CLOCK_FREQUENCY
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
        return string.format("DHT11(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function DHT11.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function DHT11.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function DHT11.print_device_info(device)
    device = device or DHT11.new()
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

function DHT11.print_registers(device)
    device = device or DHT11.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            DHT11.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function DHT11.example()
    print("=== DHT11设备示例 ===")
    
    -- 创建设备实例
    local device = DHT11.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    DHT11.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    DHT11.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("DHT11.lua$") then
    DHT11.example()
end

return DHT11

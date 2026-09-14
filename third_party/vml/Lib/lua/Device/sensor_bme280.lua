--[[
  BME280设备定义 - Lua模块
  生成自: Bosch/Sensor/BME280
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: BME280 Combined Humidity, Pressure, and Temperature Sensor (I2C/SPI)
  CPU架构: Sensor
  位宽: 8位
  时钟频率: 400000 Hz
]]

local BME280 = {}

-- 设备信息
BME280.DEVICE_NAME = "BME280"
BME280.MANUFACTURER = "Bosch"
BME280.FAMILY = "Sensor"
BME280.VERSION = "1.0"
BME280.ARCHITECTURE = "Sensor"
BME280.BITS = 8
BME280.CLOCK_FREQUENCY = 400000

-- 外设定义
-- BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
BME280.BME280_BASE = 0x76
BME280.BME280_CHIP_ID_ADDR = 0xD0
BME280.BME280_RESET_ADDR = 0xE0
BME280.BME280_CTRL_HUM_ADDR = 0xF2
BME280.BME280_STATUS_ADDR = 0xF3
BME280.BME280_CTRL_MEAS_ADDR = 0xF4
BME280.BME280_CONFIG_ADDR = 0xF5
BME280.BME280_PRESS_ADDR = 0xF7
BME280.BME280_TEMP_ADDR = 0xFA
BME280.BME280_HUM_ADDR = 0xFD

-- 设备类
function BME280.new(memory_base)
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
        self.peripherals["BME280"] = {
            base = 0x76,
            type = "I2C",
            description = "BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)",
            registers = {}
        }
        
        local p = self.peripherals["BME280"]
        p.registers["CHIP_ID"] = {
            address = 0xD0,
            size = 1,
            value = 0
        }
        p.registers["RESET"] = {
            address = 0xE0,
            size = 1,
            value = 0
        }
        p.registers["CTRL_HUM"] = {
            address = 0xF2,
            size = 1,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0xF3,
            size = 1,
            value = 0
        }
        p.registers["CTRL_MEAS"] = {
            address = 0xF4,
            size = 1,
            value = 0
        }
        p.registers["CONFIG"] = {
            address = 0xF5,
            size = 1,
            value = 0
        }
        p.registers["PRESS"] = {
            address = 0xF7,
            size = 3,
            value = 0
        }
        p.registers["TEMP"] = {
            address = 0xFA,
            size = 3,
            value = 0
        }
        p.registers["HUM"] = {
            address = 0xFD,
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
            name = BME280.DEVICE_NAME,
            manufacturer = BME280.MANUFACTURER,
            family = BME280.FAMILY,
            version = BME280.VERSION,
            architecture = BME280.ARCHITECTURE,
            bits = BME280.BITS,
            clock_frequency = BME280.CLOCK_FREQUENCY
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
        return string.format("BME280(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function BME280.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function BME280.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function BME280.print_device_info(device)
    device = device or BME280.new()
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

function BME280.print_registers(device)
    device = device or BME280.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            BME280.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function BME280.example()
    print("=== BME280设备示例 ===")
    
    -- 创建设备实例
    local device = BME280.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    BME280.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    BME280.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("BME280.lua$") then
    BME280.example()
end

return BME280

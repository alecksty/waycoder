--[[
  BMP280设备定义 - Lua模块
  生成自: Bosch/Sensor/BMP280
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: Digital Barometric Pressure and Temperature Sensor (I2C/SPI)
  CPU架构: Sensor
  位宽: 8位
  时钟频率: 3400000 Hz
]]

local BMP280 = {}

-- 设备信息
BMP280.DEVICE_NAME = "BMP280"
BMP280.MANUFACTURER = "Bosch"
BMP280.FAMILY = "Sensor"
BMP280.VERSION = "1.0"
BMP280.ARCHITECTURE = "Sensor"
BMP280.BITS = 8
BMP280.CLOCK_FREQUENCY = 3400000

-- 内存段定义
BMP280.PACKAGE_START = 0x00
BMP280.PACKAGE_END = 0x00
BMP280.PACKAGE_SIZE = 8  -- LGA-8 (2.0x2.5x0.95mm)

-- 外设定义
-- BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
BMP280.BMP280_BASE = 0x76
BMP280.BMP280_TEMP_XLSB_ADDR = 0xFC
BMP280.BMP280_TEMP_LSB_ADDR = 0xFB
BMP280.BMP280_TEMP_MSB_ADDR = 0xFA
BMP280.BMP280_PRESS_XLSB_ADDR = 0xF9
BMP280.BMP280_PRESS_LSB_ADDR = 0xF8
BMP280.BMP280_PRESS_MSB_ADDR = 0xF7
BMP280.BMP280_CONFIG_ADDR = 0xF5
BMP280.BMP280_CONFIG_T_SB_BIT = 5  -- Standby time in normal mode
BMP280.BMP280_CONFIG_FILTER_BIT = 2  -- Filter coefficient
BMP280.BMP280_CONFIG_SPI3W_EN_BIT = 0  -- Enable 3-wire SPI
BMP280.BMP280_CTRL_MEAS_ADDR = 0xF4
BMP280.BMP280_CTRL_MEAS_MODE_BIT = 0  -- 0=sleep, 1/2=forced, 3=normal
BMP280.BMP280_CTRL_MEAS_OSRS_P_BIT = 2  -- Pressure oversampling
BMP280.BMP280_CTRL_MEAS_OSRS_T_BIT = 5  -- Temperature oversampling
BMP280.BMP280_STATUS_ADDR = 0xF3
BMP280.BMP280_STATUS_IM_UPDATE_BIT = 0  -- 1=Image register update in progress
BMP280.BMP280_STATUS_MEASURING_BIT = 3  -- 1=Conversion is running
BMP280.BMP280_CHIP_ID_ADDR = 0xD0
BMP280.BMP280_RESET_ADDR = 0xE0

-- 设备类
function BMP280.new(memory_base)
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
        self.peripherals["BMP280"] = {
            base = 0x76,
            type = "I2C",
            description = "BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)",
            registers = {}
        }
        
        local p = self.peripherals["BMP280"]
        p.registers["TEMP_XLSB"] = {
            address = 0xFC,
            size = 1,
            value = 0
        }
        p.registers["TEMP_LSB"] = {
            address = 0xFB,
            size = 1,
            value = 0
        }
        p.registers["TEMP_MSB"] = {
            address = 0xFA,
            size = 1,
            value = 0
        }
        p.registers["PRESS_XLSB"] = {
            address = 0xF9,
            size = 1,
            value = 0
        }
        p.registers["PRESS_LSB"] = {
            address = 0xF8,
            size = 1,
            value = 0
        }
        p.registers["PRESS_MSB"] = {
            address = 0xF7,
            size = 1,
            value = 0
        }
        p.registers["CONFIG"] = {
            address = 0xF5,
            size = 1,
            value = 0
        }
        p.registers["CTRL_MEAS"] = {
            address = 0xF4,
            size = 1,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0xF3,
            size = 1,
            value = 0
        }
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
            name = BMP280.DEVICE_NAME,
            manufacturer = BMP280.MANUFACTURER,
            family = BMP280.FAMILY,
            version = BMP280.VERSION,
            architecture = BMP280.ARCHITECTURE,
            bits = BMP280.BITS,
            clock_frequency = BMP280.CLOCK_FREQUENCY
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
        return string.format("BMP280(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function BMP280.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function BMP280.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function BMP280.print_device_info(device)
    device = device or BMP280.new()
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

function BMP280.print_registers(device)
    device = device or BMP280.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            BMP280.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function BMP280.example()
    print("=== BMP280设备示例 ===")
    
    -- 创建设备实例
    local device = BMP280.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    BMP280.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    BMP280.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("BMP280.lua$") then
    BMP280.example()
end

return BMP280

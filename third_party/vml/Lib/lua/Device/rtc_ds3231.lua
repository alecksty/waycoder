--[[
  DS3231设备定义 - Lua模块
  生成自: Maxim/Dallas/RTC/DS3231
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: DS3231 I2C High-Precision RTC (±2ppm, temperature compensated, 32K EEPROM)
  CPU架构: RTC
  位宽: 8位
  时钟频率: 400000 Hz
]]

local DS3231 = {}

-- 设备信息
DS3231.DEVICE_NAME = "DS3231"
DS3231.MANUFACTURER = "Maxim/Dallas"
DS3231.FAMILY = "RTC"
DS3231.VERSION = "1.0"
DS3231.ARCHITECTURE = "RTC"
DS3231.BITS = 8
DS3231.CLOCK_FREQUENCY = 400000

-- 内存段定义
DS3231.EEPROM_START = 0x14
DS3231.EEPROM_END = 0xFF
DS3231.EEPROM_SIZE = 236  -- AT24C32 EEPROM (32Kbit)

-- 外设定义
-- DS3231 Precision RTC (0x68, 3.3V-5.5V)
DS3231.DS3231_BASE = 0x68
DS3231.DS3231_SEC_ADDR = 0x00
DS3231.DS3231_MIN_ADDR = 0x01
DS3231.DS3231_HOUR_ADDR = 0x02
DS3231.DS3231_DAY_ADDR = 0x03
DS3231.DS3231_DATE_ADDR = 0x04
DS3231.DS3231_MONTH_CENT_ADDR = 0x05
DS3231.DS3231_YEAR_ADDR = 0x06
DS3231.DS3231_ALARM1_SEC_ADDR = 0x07
DS3231.DS3231_ALARM1_MIN_ADDR = 0x08
DS3231.DS3231_ALARM1_HOUR_ADDR = 0x09
DS3231.DS3231_ALARM2_MIN_ADDR = 0x0B
DS3231.DS3231_ALARM2_HOUR_ADDR = 0x0C
DS3231.DS3231_CTRL_ADDR = 0x0E
DS3231.DS3231_CTRL_STATUS_ADDR = 0x0F
DS3231.DS3231_TEMP_MSB_ADDR = 0x11
DS3231.DS3231_TEMP_LSB_ADDR = 0x12

-- 设备类
function DS3231.new(memory_base)
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
        self.peripherals["DS3231"] = {
            base = 0x68,
            type = "I2C",
            description = "DS3231 Precision RTC (0x68, 3.3V-5.5V)",
            registers = {}
        }
        
        local p = self.peripherals["DS3231"]
        p.registers["SEC"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["MIN"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["HOUR"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["DAY"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["DATE"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["MONTH_CENT"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["YEAR"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["ALARM1_SEC"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["ALARM1_MIN"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["ALARM1_HOUR"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["ALARM2_MIN"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        p.registers["ALARM2_HOUR"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        p.registers["CTRL"] = {
            address = 0x0E,
            size = 1,
            value = 0
        }
        p.registers["CTRL_STATUS"] = {
            address = 0x0F,
            size = 1,
            value = 0
        }
        p.registers["TEMP_MSB"] = {
            address = 0x11,
            size = 1,
            value = 0
        }
        p.registers["TEMP_LSB"] = {
            address = 0x12,
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
            name = DS3231.DEVICE_NAME,
            manufacturer = DS3231.MANUFACTURER,
            family = DS3231.FAMILY,
            version = DS3231.VERSION,
            architecture = DS3231.ARCHITECTURE,
            bits = DS3231.BITS,
            clock_frequency = DS3231.CLOCK_FREQUENCY
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
        return string.format("DS3231(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function DS3231.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function DS3231.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function DS3231.print_device_info(device)
    device = device or DS3231.new()
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

function DS3231.print_registers(device)
    device = device or DS3231.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            DS3231.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function DS3231.example()
    print("=== DS3231设备示例 ===")
    
    -- 创建设备实例
    local device = DS3231.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    DS3231.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    DS3231.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("DS3231.lua$") then
    DS3231.example()
end

return DS3231

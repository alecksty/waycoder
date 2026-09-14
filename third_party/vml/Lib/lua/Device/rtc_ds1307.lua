--[[
  DS1307设备定义 - Lua模块
  生成自: Maxim/Dallas/RTC/DS1307
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: DS1307 I2C Real-Time Clock (56-byte NVRAM, battery backup)
  CPU架构: RTC
  位宽: 8位
  时钟频率: 100000 Hz
]]

local DS1307 = {}

-- 设备信息
DS1307.DEVICE_NAME = "DS1307"
DS1307.MANUFACTURER = "Maxim/Dallas"
DS1307.FAMILY = "RTC"
DS1307.VERSION = "1.0"
DS1307.ARCHITECTURE = "RTC"
DS1307.BITS = 8
DS1307.CLOCK_FREQUENCY = 100000

-- 内存段定义
DS1307.NVRAM_START = 0x08
DS1307.NVRAM_END = 0x3F
DS1307.NVRAM_SIZE = 56  -- Non-volatile RAM (56 bytes)

-- 外设定义
-- DS1307 RTC (0x68, 5V, DIP-8)
DS1307.DS1307_BASE = 0x68
DS1307.DS1307_SEC_ADDR = 0x00
DS1307.DS1307_MIN_ADDR = 0x01
DS1307.DS1307_HOUR_ADDR = 0x02
DS1307.DS1307_DAY_ADDR = 0x03
DS1307.DS1307_DATE_ADDR = 0x04
DS1307.DS1307_MONTH_ADDR = 0x05
DS1307.DS1307_YEAR_ADDR = 0x06
DS1307.DS1307_CTRL_ADDR = 0x07

-- 设备类
function DS1307.new(memory_base)
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
        self.peripherals["DS1307"] = {
            base = 0x68,
            type = "I2C",
            description = "DS1307 RTC (0x68, 5V, DIP-8)",
            registers = {}
        }
        
        local p = self.peripherals["DS1307"]
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
        p.registers["MONTH"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["YEAR"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["CTRL"] = {
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
            name = DS1307.DEVICE_NAME,
            manufacturer = DS1307.MANUFACTURER,
            family = DS1307.FAMILY,
            version = DS1307.VERSION,
            architecture = DS1307.ARCHITECTURE,
            bits = DS1307.BITS,
            clock_frequency = DS1307.CLOCK_FREQUENCY
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
        return string.format("DS1307(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function DS1307.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function DS1307.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function DS1307.print_device_info(device)
    device = device or DS1307.new()
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

function DS1307.print_registers(device)
    device = device or DS1307.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            DS1307.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function DS1307.example()
    print("=== DS1307设备示例 ===")
    
    -- 创建设备实例
    local device = DS1307.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    DS1307.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    DS1307.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("DS1307.lua$") then
    DS1307.example()
end

return DS1307

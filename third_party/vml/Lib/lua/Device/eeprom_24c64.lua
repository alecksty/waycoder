--[[
  24C64设备定义 - Lua模块
  生成自: Generic/Memory/24C64
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: 24C64 64Kbit I2C Serial EEPROM (8K×8, 32-byte page write)
  CPU架构: Memory
  位宽: 8位
  时钟频率: 400000 Hz
]]

local 24C64 = {}

-- 设备信息
24C64.DEVICE_NAME = "24C64"
24C64.MANUFACTURER = "Generic"
24C64.FAMILY = "Memory"
24C64.VERSION = "1.0"
24C64.ARCHITECTURE = "Memory"
24C64.BITS = 8
24C64.CLOCK_FREQUENCY = 400000

-- 内存段定义
24C64.EEPROM_START = 0x00
24C64.EEPROM_END = 0x1FFF
24C64.EEPROM_SIZE = 8192  -- EEPROM main memory array (8KB, 32-byte page write)

-- 外设定义
-- 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
24C64._24C64_BASE = 0x50
24C64._24C64_ADDR_H_ADDR = 0x00
24C64._24C64_ADDR_L_ADDR = 0x01
24C64._24C64_DATA_ADDR = 0x02
24C64._24C64_PAGE_SIZE_ADDR = 0xFE
24C64._24C64_SIZE_ADDR = 0xFD

-- 设备类
function 24C64.new(memory_base)
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
        self.peripherals["24C64"] = {
            base = 0x50,
            type = "I2C",
            description = "24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)",
            registers = {}
        }
        
        local p = self.peripherals["24C64"]
        p.registers["ADDR_H"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["ADDR_L"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["PAGE_SIZE"] = {
            address = 0xFE,
            size = 1,
            value = 0
        }
        p.registers["SIZE"] = {
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
            name = 24C64.DEVICE_NAME,
            manufacturer = 24C64.MANUFACTURER,
            family = 24C64.FAMILY,
            version = 24C64.VERSION,
            architecture = 24C64.ARCHITECTURE,
            bits = 24C64.BITS,
            clock_frequency = 24C64.CLOCK_FREQUENCY
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
        return string.format("24C64(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function 24C64.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function 24C64.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function 24C64.print_device_info(device)
    device = device or 24C64.new()
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

function 24C64.print_registers(device)
    device = device or 24C64.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            24C64.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function 24C64.example()
    print("=== 24C64设备示例 ===")
    
    -- 创建设备实例
    local device = 24C64.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    24C64.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    24C64.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("24C64.lua$") then
    24C64.example()
end

return 24C64

--[[
  74HC595设备定义 - Lua模块
  生成自: TI/NXP/GPIO/74HC595
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: 74HC595 8-bit Shift Register (SPI-compatible, serial-in parallel-out, daisy-chainable)
  CPU架构: GPIO
  位宽: 8位
  时钟频率: 10000000 Hz
]]

local 74HC595 = {}

-- 设备信息
74HC595.DEVICE_NAME = "74HC595"
74HC595.MANUFACTURER = "TI/NXP"
74HC595.FAMILY = "GPIO"
74HC595.VERSION = "1.0"
74HC595.ARCHITECTURE = "GPIO"
74HC595.BITS = 8
74HC595.CLOCK_FREQUENCY = 10000000

-- 外设定义
-- 74HC595 8-bit Shift Register (2V-6V, DIP-16)
74HC595._74HC595_BASE = 0x00
74HC595._74HC595_DATA_ADDR = 0x00
74HC595._74HC595_LATCH_ADDR = 0x01
74HC595._74HC595_CHAIN_COUNT_ADDR = 0x02
74HC595._74HC595_OE_ADDR = 0x03
74HC595._74HC595_CLEAR_ADDR = 0x04

-- 设备类
function 74HC595.new(memory_base)
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
        self.peripherals["74HC595"] = {
            base = 0x00,
            type = "SPI",
            description = "74HC595 8-bit Shift Register (2V-6V, DIP-16)",
            registers = {}
        }
        
        local p = self.peripherals["74HC595"]
        p.registers["DATA"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["LATCH"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["CHAIN_COUNT"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["OE"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["CLEAR"] = {
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
            name = 74HC595.DEVICE_NAME,
            manufacturer = 74HC595.MANUFACTURER,
            family = 74HC595.FAMILY,
            version = 74HC595.VERSION,
            architecture = 74HC595.ARCHITECTURE,
            bits = 74HC595.BITS,
            clock_frequency = 74HC595.CLOCK_FREQUENCY
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
        return string.format("74HC595(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function 74HC595.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function 74HC595.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function 74HC595.print_device_info(device)
    device = device or 74HC595.new()
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

function 74HC595.print_registers(device)
    device = device or 74HC595.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            74HC595.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function 74HC595.example()
    print("=== 74HC595设备示例 ===")
    
    -- 创建设备实例
    local device = 74HC595.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    74HC595.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    74HC595.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("74HC595.lua$") then
    74HC595.example()
end

return 74HC595

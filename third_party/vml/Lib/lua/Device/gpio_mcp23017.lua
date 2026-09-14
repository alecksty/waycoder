--[[
  MCP23017设备定义 - Lua模块
  生成自: Microchip/GPIO/MCP23017
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)
  CPU架构: GPIO
  位宽: 16位
  时钟频率: 400000 Hz
]]

local MCP23017 = {}

-- 设备信息
MCP23017.DEVICE_NAME = "MCP23017"
MCP23017.MANUFACTURER = "Microchip"
MCP23017.FAMILY = "GPIO"
MCP23017.VERSION = "1.0"
MCP23017.ARCHITECTURE = "GPIO"
MCP23017.BITS = 16
MCP23017.CLOCK_FREQUENCY = 400000

-- 外设定义
-- MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
MCP23017.MCP23017_BASE = 0x20
MCP23017.MCP23017_IODIRA_ADDR = 0x00
MCP23017.MCP23017_IODIRB_ADDR = 0x01
MCP23017.MCP23017_GPIOA_ADDR = 0x12
MCP23017.MCP23017_GPIOB_ADDR = 0x13
MCP23017.MCP23017_GPINTENA_ADDR = 0x04
MCP23017.MCP23017_GPINTENB_ADDR = 0x05
MCP23017.MCP23017_INTCONA_ADDR = 0x08
MCP23017.MCP23017_IOCON_ADDR = 0x0A
MCP23017.MCP23017_GPPUA_ADDR = 0x0C
MCP23017.MCP23017_GPPUB_ADDR = 0x0D

-- 中断向量定义
MCP23017.INT_INTA = 0  -- Port A interrupt
MCP23017.INT_INTB = 1  -- Port B interrupt

-- 设备类
function MCP23017.new(memory_base)
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
        self.peripherals["MCP23017"] = {
            base = 0x20,
            type = "I2C",
            description = "MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)",
            registers = {}
        }
        
        local p = self.peripherals["MCP23017"]
        p.registers["IODIRA"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["IODIRB"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["GPIOA"] = {
            address = 0x12,
            size = 1,
            value = 0
        }
        p.registers["GPIOB"] = {
            address = 0x13,
            size = 1,
            value = 0
        }
        p.registers["GPINTENA"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["GPINTENB"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["INTCONA"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["IOCON"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["GPPUA"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        p.registers["GPPUB"] = {
            address = 0x0D,
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
            name = MCP23017.DEVICE_NAME,
            manufacturer = MCP23017.MANUFACTURER,
            family = MCP23017.FAMILY,
            version = MCP23017.VERSION,
            architecture = MCP23017.ARCHITECTURE,
            bits = MCP23017.BITS,
            clock_frequency = MCP23017.CLOCK_FREQUENCY
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
        return string.format("MCP23017(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function MCP23017.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function MCP23017.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function MCP23017.print_device_info(device)
    device = device or MCP23017.new()
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

function MCP23017.print_registers(device)
    device = device or MCP23017.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            MCP23017.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function MCP23017.example()
    print("=== MCP23017设备示例 ===")
    
    -- 创建设备实例
    local device = MCP23017.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    MCP23017.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    MCP23017.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("MCP23017.lua$") then
    MCP23017.example()
end

return MCP23017

--[[
  WS2812B设备定义 - Lua模块
  生成自: Worldsemi/LED/WS2812B
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: WS2812B Intelligent RGB LED (single-wire, 800KHz, daisy-chainable)
  CPU架构: LED
  位宽: 24位
  时钟频率: 800000 Hz
]]

local WS2812B = {}

-- 设备信息
WS2812B.DEVICE_NAME = "WS2812B"
WS2812B.MANUFACTURER = "Worldsemi"
WS2812B.FAMILY = "LED"
WS2812B.VERSION = "1.0"
WS2812B.ARCHITECTURE = "LED"
WS2812B.BITS = 24
WS2812B.CLOCK_FREQUENCY = 800000

-- 内存段定义
WS2812B.LED_FB_START = 0x00
WS2812B.LED_FB_END = 0xFF
WS2812B.LED_FB_SIZE = 256  -- Frame buffer (up to 256 LEDs × 3 bytes)

-- 外设定义
-- WS2812B RGB LED Strip (5V, 60mA/led)
WS2812B.WS2812B_BASE = 0x00
WS2812B.WS2812B_LED_COUNT_ADDR = 0x00
WS2812B.WS2812B_LED_DATA_ADDR = 0x02
WS2812B.WS2812B_BRIGHTNESS_ADDR = 0x05
WS2812B.WS2812B_SHOW_ADDR = 0x06
WS2812B.WS2812B_CLEAR_ADDR = 0x07

-- 设备类
function WS2812B.new(memory_base)
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
        self.peripherals["WS2812B"] = {
            base = 0x00,
            type = "1Wire",
            description = "WS2812B RGB LED Strip (5V, 60mA/led)",
            registers = {}
        }
        
        local p = self.peripherals["WS2812B"]
        p.registers["LED_COUNT"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["LED_DATA"] = {
            address = 0x02,
            size = 3,
            value = 0
        }
        p.registers["BRIGHTNESS"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["SHOW"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["CLEAR"] = {
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
            name = WS2812B.DEVICE_NAME,
            manufacturer = WS2812B.MANUFACTURER,
            family = WS2812B.FAMILY,
            version = WS2812B.VERSION,
            architecture = WS2812B.ARCHITECTURE,
            bits = WS2812B.BITS,
            clock_frequency = WS2812B.CLOCK_FREQUENCY
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
        return string.format("WS2812B(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function WS2812B.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function WS2812B.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function WS2812B.print_device_info(device)
    device = device or WS2812B.new()
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

function WS2812B.print_registers(device)
    device = device or WS2812B.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            WS2812B.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function WS2812B.example()
    print("=== WS2812B设备示例 ===")
    
    -- 创建设备实例
    local device = WS2812B.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    WS2812B.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    WS2812B.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("WS2812B.lua$") then
    WS2812B.example()
end

return WS2812B

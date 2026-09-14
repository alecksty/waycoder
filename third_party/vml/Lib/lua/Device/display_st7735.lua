--[[
  ST7735设备定义 - Lua模块
  生成自: Sitronix/Display/ST7735
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: ST7735 1.8" 128x160 TFT LCD Display (SPI, 16-bit color)
  CPU架构: Display
  位宽: 16位
  时钟频率: 16000000 Hz
]]

local ST7735 = {}

-- 设备信息
ST7735.DEVICE_NAME = "ST7735"
ST7735.MANUFACTURER = "Sitronix"
ST7735.FAMILY = "Display"
ST7735.VERSION = "1.0"
ST7735.ARCHITECTURE = "Display"
ST7735.BITS = 16
ST7735.CLOCK_FREQUENCY = 16000000

-- 内存段定义
ST7735.GRAM_START = 0x00
ST7735.GRAM_END = 0x4FFF
ST7735.GRAM_SIZE = 20480  -- Graphics RAM (128x160x16bit)

-- 外设定义
-- ST7735 128x160 TFT (SPI, 3.3V-5V)
ST7735.ST7735_BASE = 0x00
ST7735.ST7735_CMD_ADDR = 0x00
ST7735.ST7735_DATA_ADDR = 0x01
ST7735.ST7735_COL_START_ADDR = 0x2A
ST7735.ST7735_ROW_START_ADDR = 0x2B
ST7735.ST7735_WRITE_RAM_ADDR = 0x2C
ST7735.ST7735_MADCTL_ADDR = 0x36
ST7735.ST7735_COLMOD_ADDR = 0x3A
ST7735.ST7735_INVON_ADDR = 0x21
ST7735.ST7735_SLEEP_OUT_ADDR = 0x11
ST7735.ST7735_DISP_ON_ADDR = 0x29

-- 设备类
function ST7735.new(memory_base)
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
        self.peripherals["ST7735"] = {
            base = 0x00,
            type = "SPI",
            description = "ST7735 128x160 TFT (SPI, 3.3V-5V)",
            registers = {}
        }
        
        local p = self.peripherals["ST7735"]
        p.registers["CMD"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["COL_START"] = {
            address = 0x2A,
            size = 2,
            value = 0
        }
        p.registers["ROW_START"] = {
            address = 0x2B,
            size = 2,
            value = 0
        }
        p.registers["WRITE_RAM"] = {
            address = 0x2C,
            size = 2,
            value = 0
        }
        p.registers["MADCTL"] = {
            address = 0x36,
            size = 1,
            value = 0
        }
        p.registers["COLMOD"] = {
            address = 0x3A,
            size = 1,
            value = 0
        }
        p.registers["INVON"] = {
            address = 0x21,
            size = 0,
            value = 0
        }
        p.registers["SLEEP_OUT"] = {
            address = 0x11,
            size = 0,
            value = 0
        }
        p.registers["DISP_ON"] = {
            address = 0x29,
            size = 0,
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
            name = ST7735.DEVICE_NAME,
            manufacturer = ST7735.MANUFACTURER,
            family = ST7735.FAMILY,
            version = ST7735.VERSION,
            architecture = ST7735.ARCHITECTURE,
            bits = ST7735.BITS,
            clock_frequency = ST7735.CLOCK_FREQUENCY
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
        return string.format("ST7735(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ST7735.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ST7735.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ST7735.print_device_info(device)
    device = device or ST7735.new()
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

function ST7735.print_registers(device)
    device = device or ST7735.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ST7735.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ST7735.example()
    print("=== ST7735设备示例 ===")
    
    -- 创建设备实例
    local device = ST7735.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ST7735.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    ST7735.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ST7735.lua$") then
    ST7735.example()
end

return ST7735

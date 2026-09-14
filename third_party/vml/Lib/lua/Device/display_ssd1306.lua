--[[
  SSD1306设备定义 - Lua模块
  生成自: Solomon Systech/Display/SSD1306
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: SSD1306 128x64 OLED Display Controller (I2C/SPI)
  CPU架构: Display
  位宽: 8位
  时钟频率: 400000 Hz
]]

local SSD1306 = {}

-- 设备信息
SSD1306.DEVICE_NAME = "SSD1306"
SSD1306.MANUFACTURER = "Solomon Systech"
SSD1306.FAMILY = "Display"
SSD1306.VERSION = "1.0"
SSD1306.ARCHITECTURE = "Display"
SSD1306.BITS = 8
SSD1306.CLOCK_FREQUENCY = 400000

-- 内存段定义
SSD1306.GDDRAM_START = 0x00
SSD1306.GDDRAM_END = 0x3FF
SSD1306.GDDRAM_SIZE = 1024  -- Graphic Display Data RAM (128x64 = 1024 bytes)

-- 外设定义
-- SSD1306 128x64 OLED (0x3C/0x3D I2C, 3.3V-5V)
SSD1306.SSD1306_BASE = 0x3C
SSD1306.SSD1306_CMD_ADDR = 0x00
SSD1306.SSD1306_DATA_ADDR = 0x40
SSD1306.SSD1306_DISPLAY_OFF_ADDR = 0xAE
SSD1306.SSD1306_DISPLAY_ON_ADDR = 0xAF
SSD1306.SSD1306_CONTRAST_ADDR = 0x81
SSD1306.SSD1306_SEG_REMAP_ADDR = 0xA1
SSD1306.SSD1306_COM_SCAN_ADDR = 0xC8
SSD1306.SSD1306_ADDR_MODE_ADDR = 0x20
SSD1306.SSD1306_COL_START_ADDR = 0x21
SSD1306.SSD1306_PAGE_START_ADDR = 0x22

-- 设备类
function SSD1306.new(memory_base)
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
        self.peripherals["SSD1306"] = {
            base = 0x3C,
            type = "I2C",
            description = "SSD1306 128x64 OLED (0x3C/0x3D I2C, 3.3V-5V)",
            registers = {}
        }
        
        local p = self.peripherals["SSD1306"]
        p.registers["CMD"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x40,
            size = 1,
            value = 0
        }
        p.registers["DISPLAY_OFF"] = {
            address = 0xAE,
            size = 1,
            value = 0
        }
        p.registers["DISPLAY_ON"] = {
            address = 0xAF,
            size = 1,
            value = 0
        }
        p.registers["CONTRAST"] = {
            address = 0x81,
            size = 1,
            value = 0
        }
        p.registers["SEG_REMAP"] = {
            address = 0xA1,
            size = 1,
            value = 0
        }
        p.registers["COM_SCAN"] = {
            address = 0xC8,
            size = 1,
            value = 0
        }
        p.registers["ADDR_MODE"] = {
            address = 0x20,
            size = 1,
            value = 0
        }
        p.registers["COL_START"] = {
            address = 0x21,
            size = 1,
            value = 0
        }
        p.registers["PAGE_START"] = {
            address = 0x22,
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
            name = SSD1306.DEVICE_NAME,
            manufacturer = SSD1306.MANUFACTURER,
            family = SSD1306.FAMILY,
            version = SSD1306.VERSION,
            architecture = SSD1306.ARCHITECTURE,
            bits = SSD1306.BITS,
            clock_frequency = SSD1306.CLOCK_FREQUENCY
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
        return string.format("SSD1306(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function SSD1306.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function SSD1306.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function SSD1306.print_device_info(device)
    device = device or SSD1306.new()
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

function SSD1306.print_registers(device)
    device = device or SSD1306.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            SSD1306.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function SSD1306.example()
    print("=== SSD1306设备示例 ===")
    
    -- 创建设备实例
    local device = SSD1306.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    SSD1306.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    SSD1306.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("SSD1306.lua$") then
    SSD1306.example()
end

return SSD1306

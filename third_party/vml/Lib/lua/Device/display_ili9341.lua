--[[
  ILI9341设备定义 - Lua模块
  生成自: Ilitek/Display/ILI9341
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: ILI9341 2.8" 240x320 TFT LCD Display (SPI, 18-bit color, touch)
  CPU架构: Display
  位宽: 18位
  时钟频率: 20000000 Hz
]]

local ILI9341 = {}

-- 设备信息
ILI9341.DEVICE_NAME = "ILI9341"
ILI9341.MANUFACTURER = "Ilitek"
ILI9341.FAMILY = "Display"
ILI9341.VERSION = "1.0"
ILI9341.ARCHITECTURE = "Display"
ILI9341.BITS = 18
ILI9341.CLOCK_FREQUENCY = 20000000

-- 内存段定义
ILI9341.GRAM_START = 0x00
ILI9341.GRAM_END = 0xBCFF
ILI9341.GRAM_SIZE = 156672  -- Graphics RAM (240x320x18bit)

-- 外设定义
-- ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)
ILI9341.ILI9341_BASE = 0x00
ILI9341.ILI9341_CMD_ADDR = 0x00
ILI9341.ILI9341_DATA_ADDR = 0x01
ILI9341.ILI9341_COL_START_ADDR = 0x2A
ILI9341.ILI9341_PAGE_START_ADDR = 0x2B
ILI9341.ILI9341_WRITE_RAM_ADDR = 0x2C
ILI9341.ILI9341_MADCTL_ADDR = 0x36
ILI9341.ILI9341_PIXFMT_ADDR = 0x3A
ILI9341.ILI9341_FRMCTL_ADDR = 0xB1
ILI9341.ILI9341_GAMMA_ADDR = 0x26
ILI9341.ILI9341_SLEEP_OUT_ADDR = 0x11
ILI9341.ILI9341_DISP_ON_ADDR = 0x29

-- 设备类
function ILI9341.new(memory_base)
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
        self.peripherals["ILI9341"] = {
            base = 0x00,
            type = "SPI",
            description = "ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)",
            registers = {}
        }
        
        local p = self.peripherals["ILI9341"]
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
        p.registers["PAGE_START"] = {
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
        p.registers["PIXFMT"] = {
            address = 0x3A,
            size = 1,
            value = 0
        }
        p.registers["FRMCTL"] = {
            address = 0xB1,
            size = 2,
            value = 0
        }
        p.registers["GAMMA"] = {
            address = 0x26,
            size = 1,
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
            name = ILI9341.DEVICE_NAME,
            manufacturer = ILI9341.MANUFACTURER,
            family = ILI9341.FAMILY,
            version = ILI9341.VERSION,
            architecture = ILI9341.ARCHITECTURE,
            bits = ILI9341.BITS,
            clock_frequency = ILI9341.CLOCK_FREQUENCY
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
        return string.format("ILI9341(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ILI9341.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ILI9341.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ILI9341.print_device_info(device)
    device = device or ILI9341.new()
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

function ILI9341.print_registers(device)
    device = device or ILI9341.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ILI9341.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ILI9341.example()
    print("=== ILI9341设备示例 ===")
    
    -- 创建设备实例
    local device = ILI9341.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ILI9341.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    ILI9341.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ILI9341.lua$") then
    ILI9341.example()
end

return ILI9341

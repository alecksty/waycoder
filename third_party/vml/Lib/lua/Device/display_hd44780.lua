--[[
  HD44780设备定义 - Lua模块
  生成自: Hitachi/Display/HD44780
  版本: 1.0
  日期: 2026-05-06
  作者: VML Team
  描述: HD44780 16x2 Character LCD Controller (4-bit/8-bit parallel or I2C via PCF8574)
  CPU架构: Display
  位宽: 8位
  时钟频率: 0 Hz
]]

local HD44780 = {}

-- 设备信息
HD44780.DEVICE_NAME = "HD44780"
HD44780.MANUFACTURER = "Hitachi"
HD44780.FAMILY = "Display"
HD44780.VERSION = "1.0"
HD44780.ARCHITECTURE = "Display"
HD44780.BITS = 8
HD44780.CLOCK_FREQUENCY = 0

-- 内存段定义
HD44780.DDRAM_START = 0x00
HD44780.DDRAM_END = 0x4F
HD44780.DDRAM_SIZE = 80  -- Display Data RAM (80 bytes, 2 lines)
HD44780.CGRAM_START = 0x00
HD44780.CGRAM_END = 0x3F
HD44780.CGRAM_SIZE = 64  -- Character Generator RAM (8 custom chars x 8 bytes)

-- 外设定义
-- HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
HD44780.HD44780_BASE = 0x27
HD44780.HD44780_CMD_ADDR = 0x00
HD44780.HD44780_DATA_ADDR = 0x01
HD44780.HD44780_CTRL_RS_ADDR = 0x00
HD44780.HD44780_CTRL_RW_ADDR = 0x01
HD44780.HD44780_CTRL_EN_ADDR = 0x02
HD44780.HD44780_CTRL_BL_ADDR = 0x03
HD44780.HD44780_ADDR_DDRAM_ADDR = 0x80
HD44780.HD44780_ADDR_CGRAM_ADDR = 0x40

-- 设备类
function HD44780.new(memory_base)
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
        self.peripherals["HD44780"] = {
            base = 0x27,
            type = "Parallel/I2C",
            description = "HD44780 16x2 LCD (0x27/0x3F I2C, 5V)",
            registers = {}
        }
        
        local p = self.peripherals["HD44780"]
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
        p.registers["CTRL_RS"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["CTRL_RW"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["CTRL_EN"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["CTRL_BL"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["ADDR_DDRAM"] = {
            address = 0x80,
            size = 1,
            value = 0
        }
        p.registers["ADDR_CGRAM"] = {
            address = 0x40,
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
            name = HD44780.DEVICE_NAME,
            manufacturer = HD44780.MANUFACTURER,
            family = HD44780.FAMILY,
            version = HD44780.VERSION,
            architecture = HD44780.ARCHITECTURE,
            bits = HD44780.BITS,
            clock_frequency = HD44780.CLOCK_FREQUENCY
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
        return string.format("HD44780(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function HD44780.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function HD44780.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function HD44780.print_device_info(device)
    device = device or HD44780.new()
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

function HD44780.print_registers(device)
    device = device or HD44780.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            HD44780.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function HD44780.example()
    print("=== HD44780设备示例 ===")
    
    -- 创建设备实例
    local device = HD44780.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    HD44780.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    HD44780.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("HD44780.lua$") then
    HD44780.example()
end

return HD44780

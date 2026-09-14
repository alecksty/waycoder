--[[
  RP2350设备定义 - Lua模块
  生成自: Raspberry/RP2/RP2350
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: Dual Cortex-M33 + RISC-V Hazard3 MCU with 520KB SRAM, 150MHz
  CPU架构: ARM-Cortex-M33
  位宽: 32位
  时钟频率: 150000000 Hz
]]

local RP2350 = {}

-- 设备信息
RP2350.DEVICE_NAME = "RP2350"
RP2350.MANUFACTURER = "Raspberry"
RP2350.FAMILY = "RP2"
RP2350.VERSION = "1.0"
RP2350.ARCHITECTURE = "ARM-Cortex-M33"
RP2350.BITS = 32
RP2350.CLOCK_FREQUENCY = 150000000

-- 寄存器地址定义
RP2350.R0_ADDR = 0x00  -- 
RP2350.R1_ADDR = 0x04  -- 
RP2350.R2_ADDR = 0x08  -- 
RP2350.R3_ADDR = 0x0C  -- 
RP2350.R4_ADDR = 0x10  -- 
RP2350.R5_ADDR = 0x14  -- 
RP2350.SP_ADDR = 0x34  -- 
RP2350.LR_ADDR = 0x38  -- 
RP2350.PC_ADDR = 0x3C  -- 

-- 内存段定义
RP2350.FLASH_START = 0x10000000
RP2350.FLASH_END = 0x107FFFFF
RP2350.FLASH_SIZE = 8388608  -- XIP Flash
RP2350.SRAM_START = 0x20000000
RP2350.SRAM_END = 0x20081FFF
RP2350.SRAM_SIZE = 532480  -- Total SRAM
RP2350.PERIPHERAL_START = 0x40000000
RP2350.PERIPHERAL_END = 0x5000FFFF
RP2350.PERIPHERAL_SIZE = 16777216  -- 

-- 外设定义
-- Single-Cycle I/O (GPIO)
RP2350.SIO_BASE = 0xD0000000
RP2350.SIO_GPIO_IN_ADDR = 0x004
RP2350.SIO_GPIO_OUT_ADDR = 0x010
RP2350.SIO_GPIO_OUT_SET_ADDR = 0x014
RP2350.SIO_GPIO_OUT_CLR_ADDR = 0x018
RP2350.SIO_GPIO_OUT_XOR_ADDR = 0x01C
RP2350.SIO_GPIO_OE_ADDR = 0x020
RP2350.SIO_GPIO_OE_SET_ADDR = 0x024
RP2350.SIO_GPIO_OE_CLR_ADDR = 0x028
-- IO Bank 0 (GPIO control)
RP2350.IO_BANK0_BASE = 0x40028000
RP2350.IO_BANK0_GPIO0_STATUS_ADDR = 0x000
RP2350.IO_BANK0_GPIO0_CTRL_ADDR = 0x004
RP2350.IO_BANK0_GPIO1_STATUS_ADDR = 0x008
RP2350.IO_BANK0_GPIO1_CTRL_ADDR = 0x00C
-- Pad controls for GPIO 0-29
RP2350.PADS_BANK0_BASE = 0x4002C000
RP2350.PADS_BANK0_GPIO0_ADDR = 0x000
RP2350.PADS_BANK0_GPIO1_ADDR = 0x004
-- Reset Controller
RP2350.RESETS_BASE = 0x4000C000
RP2350.RESETS_RESET_ADDR = 0x000
RP2350.RESETS_RESET_DONE_ADDR = 0x008

-- 中断向量定义
RP2350.INT_RESET = 0  -- 
RP2350.INT_SVCALL = 11  -- 

-- 设备类
function RP2350.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["R0"] = {
            address = 0x00,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["LR"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["SIO"] = {
            base = 0xD0000000,
            type = "GPIO",
            description = "Single-Cycle I/O (GPIO)",
            registers = {}
        }
        
        local p = self.peripherals["SIO"]
        p.registers["GPIO_IN"] = {
            address = 0x004,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT"] = {
            address = 0x010,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT_SET"] = {
            address = 0x014,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT_CLR"] = {
            address = 0x018,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT_XOR"] = {
            address = 0x01C,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OE"] = {
            address = 0x020,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OE_SET"] = {
            address = 0x024,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OE_CLR"] = {
            address = 0x028,
            size = 4,
            value = 0
        }
        self.peripherals["IO_BANK0"] = {
            base = 0x40028000,
            type = "IOMUX",
            description = "IO Bank 0 (GPIO control)",
            registers = {}
        }
        
        local p = self.peripherals["IO_BANK0"]
        p.registers["GPIO0_STATUS"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["GPIO0_CTRL"] = {
            address = 0x004,
            size = 4,
            value = 0
        }
        p.registers["GPIO1_STATUS"] = {
            address = 0x008,
            size = 4,
            value = 0
        }
        p.registers["GPIO1_CTRL"] = {
            address = 0x00C,
            size = 4,
            value = 0
        }
        self.peripherals["PADS_BANK0"] = {
            base = 0x4002C000,
            type = "PADS",
            description = "Pad controls for GPIO 0-29",
            registers = {}
        }
        
        local p = self.peripherals["PADS_BANK0"]
        p.registers["GPIO0"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["GPIO1"] = {
            address = 0x004,
            size = 4,
            value = 0
        }
        self.peripherals["RESETS"] = {
            base = 0x4000C000,
            type = "ResetControl",
            description = "Reset Controller",
            registers = {}
        }
        
        local p = self.peripherals["RESETS"]
        p.registers["RESET"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["RESET_DONE"] = {
            address = 0x008,
            size = 4,
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
            name = RP2350.DEVICE_NAME,
            manufacturer = RP2350.MANUFACTURER,
            family = RP2350.FAMILY,
            version = RP2350.VERSION,
            architecture = RP2350.ARCHITECTURE,
            bits = RP2350.BITS,
            clock_frequency = RP2350.CLOCK_FREQUENCY
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
        return string.format("RP2350(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function RP2350.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function RP2350.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function RP2350.print_device_info(device)
    device = device or RP2350.new()
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

function RP2350.print_registers(device)
    device = device or RP2350.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            RP2350.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function RP2350.example()
    print("=== RP2350设备示例 ===")
    
    -- 创建设备实例
    local device = RP2350.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    RP2350.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. RP2350.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. RP2350.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    RP2350.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("RP2350.lua$") then
    RP2350.example()
end

return RP2350

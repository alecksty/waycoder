--[[
  CH32V003设备定义 - Lua模块
  生成自: WCH/CH32V0/CH32V003
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit RISC-V RV32EC MCU with 16KB Flash, 2KB RAM, 48MHz, ultra-low-cost
  CPU架构: RISC-V
  位宽: 32位
  时钟频率: 48000000 Hz
]]

local CH32V003 = {}

-- 设备信息
CH32V003.DEVICE_NAME = "CH32V003"
CH32V003.MANUFACTURER = "WCH"
CH32V003.FAMILY = "CH32V0"
CH32V003.VERSION = "1.0"
CH32V003.ARCHITECTURE = "RISC-V"
CH32V003.BITS = 32
CH32V003.CLOCK_FREQUENCY = 48000000

-- 寄存器地址定义
CH32V003.X1_ADDR = 0x04  -- Return Address
CH32V003.X2_ADDR = 0x08  -- Stack Pointer (SP)
CH32V003.X3_ADDR = 0x0C  -- Global Pointer (GP)
CH32V003.PC_ADDR = 0x3C  -- Program Counter

-- 内存段定义
CH32V003.FLASH_START = 0x08000000
CH32V003.FLASH_END = 0x08003FFF
CH32V003.FLASH_SIZE = 16384  -- 
CH32V003.SRAM_START = 0x20000000
CH32V003.SRAM_END = 0x200007FF
CH32V003.SRAM_SIZE = 2048  -- 
CH32V003.PERIPHERAL_START = 0x40000000
CH32V003.PERIPHERAL_END = 0x40003FFF
CH32V003.PERIPHERAL_SIZE = 16384  -- 

-- 外设定义
-- Reset and Clock Control
CH32V003.RCC_BASE = 0x40021000
CH32V003.RCC_CTLR_ADDR = 0x00
CH32V003.RCC_CFGR0_ADDR = 0x04
CH32V003.RCC_APB2PCENR_ADDR = 0x18
CH32V003.RCC_APB2PCENR_IOPAEN_BIT = 2  -- GPIOA clock enable
CH32V003.RCC_APB2PCENR_IOPCEN_BIT = 4  -- GPIOC clock enable
CH32V003.RCC_APB2PCENR_IOPDEN_BIT = 5  -- GPIOD clock enable
-- General Purpose I/O Port A
CH32V003.GPIOA_BASE = 0x40010800
CH32V003.GPIOA_CFGLR_ADDR = 0x00
CH32V003.GPIOA_CFGHR_ADDR = 0x04
CH32V003.GPIOA_INDR_ADDR = 0x08
CH32V003.GPIOA_OUTDR_ADDR = 0x0C
CH32V003.GPIOA_BSHR_ADDR = 0x10
CH32V003.GPIOA_BCR_ADDR = 0x14
-- General Purpose I/O Port C
CH32V003.GPIOC_BASE = 0x40011000
CH32V003.GPIOC_CFGLR_ADDR = 0x00
CH32V003.GPIOC_CFGHR_ADDR = 0x04
CH32V003.GPIOC_INDR_ADDR = 0x08
CH32V003.GPIOC_OUTDR_ADDR = 0x0C
CH32V003.GPIOC_BSHR_ADDR = 0x10
CH32V003.GPIOC_BCR_ADDR = 0x14
-- General Purpose I/O Port D
CH32V003.GPIOD_BASE = 0x40011400
CH32V003.GPIOD_CFGLR_ADDR = 0x00
CH32V003.GPIOD_CFGHR_ADDR = 0x04
CH32V003.GPIOD_INDR_ADDR = 0x08
CH32V003.GPIOD_OUTDR_ADDR = 0x0C
CH32V003.GPIOD_BSHR_ADDR = 0x10
CH32V003.GPIOD_BCR_ADDR = 0x14
-- USART1
CH32V003.USART1_BASE = 0x40013800
CH32V003.USART1_STATR_ADDR = 0x00
CH32V003.USART1_DATAR_ADDR = 0x04
CH32V003.USART1_BRR_ADDR = 0x08
CH32V003.USART1_CTLR1_ADDR = 0x0C

-- 中断向量定义
CH32V003.INT_RESET = 1  -- 
CH32V003.INT_MACHINESOFTWARE = 3  -- 
CH32V003.INT_MACHINETIMER = 7  -- 
CH32V003.INT_MACHINEEXTERNAL = 11  -- 
CH32V003.INT_USART1 = 25  -- USART1 Global Interrupt

-- 设备类
function CH32V003.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["x1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "Return Address",
            value = 0
        }
        self.registers["x2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "Stack Pointer (SP)",
            value = 0
        }
        self.registers["x3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "Global Pointer (GP)",
            value = 0
        }
        self.registers["pc"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["RCC"] = {
            base = 0x40021000,
            type = "ResetClock",
            description = "Reset and Clock Control",
            registers = {}
        }
        
        local p = self.peripherals["RCC"]
        p.registers["CTLR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CFGR0"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["APB2PCENR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOA"] = {
            base = 0x40010800,
            type = "GPIO",
            description = "General Purpose I/O Port A",
            registers = {}
        }
        
        local p = self.peripherals["GPIOA"]
        p.registers["CFGLR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CFGHR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["INDR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["OUTDR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BSHR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["BCR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOC"] = {
            base = 0x40011000,
            type = "GPIO",
            description = "General Purpose I/O Port C",
            registers = {}
        }
        
        local p = self.peripherals["GPIOC"]
        p.registers["CFGLR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CFGHR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["INDR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["OUTDR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BSHR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["BCR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOD"] = {
            base = 0x40011400,
            type = "GPIO",
            description = "General Purpose I/O Port D",
            registers = {}
        }
        
        local p = self.peripherals["GPIOD"]
        p.registers["CFGLR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CFGHR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["INDR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["OUTDR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BSHR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["BCR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["USART1"] = {
            base = 0x40013800,
            type = "UART",
            description = "USART1",
            registers = {}
        }
        
        local p = self.peripherals["USART1"]
        p.registers["STATR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["DATAR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CTLR1"] = {
            address = 0x0C,
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
            name = CH32V003.DEVICE_NAME,
            manufacturer = CH32V003.MANUFACTURER,
            family = CH32V003.FAMILY,
            version = CH32V003.VERSION,
            architecture = CH32V003.ARCHITECTURE,
            bits = CH32V003.BITS,
            clock_frequency = CH32V003.CLOCK_FREQUENCY
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
        return string.format("CH32V003(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function CH32V003.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function CH32V003.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function CH32V003.print_device_info(device)
    device = device or CH32V003.new()
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

function CH32V003.print_registers(device)
    device = device or CH32V003.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            CH32V003.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function CH32V003.example()
    print("=== CH32V003设备示例 ===")
    
    -- 创建设备实例
    local device = CH32V003.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    CH32V003.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["x1"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("x1", 0x55)
        print("写入 x1: " .. CH32V003.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("x1")
        print("读取 x1: " .. CH32V003.hex(value))
        
        -- 位操作
        device:set_bit("x1", 0, true)
        local bit0 = device:get_bit("x1", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    CH32V003.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("CH32V003.lua$") then
    CH32V003.example()
end

return CH32V003

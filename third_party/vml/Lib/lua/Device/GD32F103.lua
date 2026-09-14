--[[
  GD32F103设备定义 - Lua模块
  生成自: GigaDevice/GD32/GD32F103
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit ARM Cortex-M3 MCU, 108MHz, STM32F103 compatible
  CPU架构: ARM-Cortex-M3
  位宽: 32位
  时钟频率: 108000000 Hz
]]

local GD32F103 = {}

-- 设备信息
GD32F103.DEVICE_NAME = "GD32F103"
GD32F103.MANUFACTURER = "GigaDevice"
GD32F103.FAMILY = "GD32"
GD32F103.VERSION = "1.0"
GD32F103.ARCHITECTURE = "ARM-Cortex-M3"
GD32F103.BITS = 32
GD32F103.CLOCK_FREQUENCY = 108000000

-- 寄存器地址定义
GD32F103.R0_ADDR = 0x00  -- 
GD32F103.R1_ADDR = 0x04  -- 
GD32F103.R2_ADDR = 0x08  -- 
GD32F103.R3_ADDR = 0x0C  -- 
GD32F103.R4_ADDR = 0x10  -- 
GD32F103.R5_ADDR = 0x14  -- 
GD32F103.SP_ADDR = 0x34  -- 
GD32F103.LR_ADDR = 0x38  -- 
GD32F103.PC_ADDR = 0x3C  -- 

-- 内存段定义
GD32F103.FLASH_START = 0x08000000
GD32F103.FLASH_END = 0x0801FFFF
GD32F103.FLASH_SIZE = 131072  -- 
GD32F103.SRAM_START = 0x20000000
GD32F103.SRAM_END = 0x20004FFF
GD32F103.SRAM_SIZE = 20480  -- 
GD32F103.PERIPHERAL_START = 0x40000000
GD32F103.PERIPHERAL_END = 0x4003FFFF
GD32F103.PERIPHERAL_SIZE = 262144  -- 

-- 外设定义
-- Reset and Clock Control
GD32F103.RCC_BASE = 0x40021000
GD32F103.RCC_CTLR_ADDR = 0x00
GD32F103.RCC_CFGR0_ADDR = 0x04
GD32F103.RCC_APB2PCENR_ADDR = 0x18
GD32F103.RCC_APB2PCENR_IOPAEN_BIT = 2  -- GPIOA clock enable
GD32F103.RCC_APB2PCENR_IOPBEN_BIT = 3  -- GPIOB clock enable
GD32F103.RCC_APB2PCENR_IOPCEN_BIT = 4  -- GPIOC clock enable
GD32F103.RCC_APB2PCENR_USART0EN_BIT = 14  -- USART0 clock enable
GD32F103.RCC_APB1PCENR_ADDR = 0x1C
GD32F103.RCC_APB1PCENR_USART1EN_BIT = 17  -- USART1 clock enable
-- General Purpose I/O Port A
GD32F103.GPIOA_BASE = 0x40010800
GD32F103.GPIOA_CTL0_ADDR = 0x00
GD32F103.GPIOA_CTL1_ADDR = 0x04
GD32F103.GPIOA_ISTAT_ADDR = 0x08
GD32F103.GPIOA_OCTL_ADDR = 0x0C
GD32F103.GPIOA_BOP_ADDR = 0x10
GD32F103.GPIOA_BC_ADDR = 0x14
-- General Purpose I/O Port B
GD32F103.GPIOB_BASE = 0x40010C00
GD32F103.GPIOB_CTL0_ADDR = 0x00
GD32F103.GPIOB_CTL1_ADDR = 0x04
GD32F103.GPIOB_ISTAT_ADDR = 0x08
GD32F103.GPIOB_OCTL_ADDR = 0x0C
GD32F103.GPIOB_BOP_ADDR = 0x10
GD32F103.GPIOB_BC_ADDR = 0x14
-- General Purpose I/O Port C
GD32F103.GPIOC_BASE = 0x40011000
GD32F103.GPIOC_CTL0_ADDR = 0x00
GD32F103.GPIOC_CTL1_ADDR = 0x04
GD32F103.GPIOC_ISTAT_ADDR = 0x08
GD32F103.GPIOC_OCTL_ADDR = 0x0C
GD32F103.GPIOC_BOP_ADDR = 0x10
GD32F103.GPIOC_BC_ADDR = 0x14
-- USART0
GD32F103.USART0_BASE = 0x40013800
GD32F103.USART0_STATR_ADDR = 0x00
GD32F103.USART0_DATAR_ADDR = 0x04
GD32F103.USART0_BRR_ADDR = 0x08
GD32F103.USART0_CTLR1_ADDR = 0x0C
-- USART1
GD32F103.USART1_BASE = 0x40004400
GD32F103.USART1_STATR_ADDR = 0x00
GD32F103.USART1_DATAR_ADDR = 0x04
GD32F103.USART1_BRR_ADDR = 0x08
GD32F103.USART1_CTLR1_ADDR = 0x0C

-- 中断向量定义
GD32F103.INT_RESET = 0  -- 
GD32F103.INT_SVCALL = 11  -- 
GD32F103.INT_USART0 = 25  -- USART0 Global Interrupt
GD32F103.INT_USART1 = 37  -- USART1 Global Interrupt

-- 设备类
function GD32F103.new(memory_base)
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
        p.registers["APB1PCENR"] = {
            address = 0x1C,
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
        p.registers["CTL0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CTL1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["ISTAT"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["OCTL"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BOP"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["BC"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOB"] = {
            base = 0x40010C00,
            type = "GPIO",
            description = "General Purpose I/O Port B",
            registers = {}
        }
        
        local p = self.peripherals["GPIOB"]
        p.registers["CTL0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CTL1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["ISTAT"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["OCTL"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BOP"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["BC"] = {
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
        p.registers["CTL0"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CTL1"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["ISTAT"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["OCTL"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["BOP"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["BC"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["USART0"] = {
            base = 0x40013800,
            type = "UART",
            description = "USART0",
            registers = {}
        }
        
        local p = self.peripherals["USART0"]
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
        self.peripherals["USART1"] = {
            base = 0x40004400,
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
            name = GD32F103.DEVICE_NAME,
            manufacturer = GD32F103.MANUFACTURER,
            family = GD32F103.FAMILY,
            version = GD32F103.VERSION,
            architecture = GD32F103.ARCHITECTURE,
            bits = GD32F103.BITS,
            clock_frequency = GD32F103.CLOCK_FREQUENCY
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
        return string.format("GD32F103(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function GD32F103.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function GD32F103.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function GD32F103.print_device_info(device)
    device = device or GD32F103.new()
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

function GD32F103.print_registers(device)
    device = device or GD32F103.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            GD32F103.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function GD32F103.example()
    print("=== GD32F103设备示例 ===")
    
    -- 创建设备实例
    local device = GD32F103.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    GD32F103.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. GD32F103.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. GD32F103.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    GD32F103.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("GD32F103.lua$") then
    GD32F103.example()
end

return GD32F103

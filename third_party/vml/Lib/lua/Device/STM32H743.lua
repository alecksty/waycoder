--[[
  STM32H743设备定义 - Lua模块
  生成自: STMicroelectronics/STM32/STM32H743
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit ARM Cortex-M7 MCU with 2MB Flash, 1MB RAM, 400MHz
  CPU架构: ARM-Cortex-M7
  位宽: 32位
  时钟频率: 400000000 Hz
]]

local STM32H743 = {}

-- 设备信息
STM32H743.DEVICE_NAME = "STM32H743"
STM32H743.MANUFACTURER = "STMicroelectronics"
STM32H743.FAMILY = "STM32"
STM32H743.VERSION = "1.0"
STM32H743.ARCHITECTURE = "ARM-Cortex-M7"
STM32H743.BITS = 32
STM32H743.CLOCK_FREQUENCY = 400000000

-- 寄存器地址定义
STM32H743.R0_ADDR = 0x00  -- 
STM32H743.R1_ADDR = 0x04  -- 
STM32H743.R2_ADDR = 0x08  -- 
STM32H743.R3_ADDR = 0x0C  -- 
STM32H743.R4_ADDR = 0x10  -- 
STM32H743.R5_ADDR = 0x14  -- 
STM32H743.SP_ADDR = 0x34  -- 
STM32H743.LR_ADDR = 0x38  -- 
STM32H743.PC_ADDR = 0x3C  -- 

-- 内存段定义
STM32H743.FLASH_START = 0x08000000
STM32H743.FLASH_END = 0x081FFFFF
STM32H743.FLASH_SIZE = 2097152  -- 
STM32H743.DTCM_START = 0x20000000
STM32H743.DTCM_END = 0x2001FFFF
STM32H743.DTCM_SIZE = 131072  -- DTCM RAM
STM32H743.ITCM_START = 0x00000000
STM32H743.ITCM_END = 0x0000FFFF
STM32H743.ITCM_SIZE = 65536  -- ITCM RAM
STM32H743.SRAM_AXI_START = 0x24000000
STM32H743.SRAM_AXI_END = 0x2407FFFF
STM32H743.SRAM_AXI_SIZE = 524288  -- AXI SRAM
STM32H743.SRAM_SRAM_START = 0x30000000
STM32H743.SRAM_SRAM_END = 0x3003FFFF
STM32H743.SRAM_SRAM_SIZE = 262144  -- SRAM1-3
STM32H743.PERIPHERAL_START = 0x40000000
STM32H743.PERIPHERAL_END = 0x4FFFFFFF
STM32H743.PERIPHERAL_SIZE = 268435456  -- 

-- 外设定义
-- Reset and Clock Control
STM32H743.RCC_BASE = 0x58024400
STM32H743.RCC_CR_ADDR = 0x00
STM32H743.RCC_CFGR_ADDR = 0x04
STM32H743.RCC_PLL1CFGR_ADDR = 0x0C
STM32H743.RCC_AHB1ENR_ADDR = 0x30
STM32H743.RCC_AHB1ENR_GPIOAEN_BIT = 0  -- GPIOA clock enable
STM32H743.RCC_AHB1ENR_GPIOBEN_BIT = 1  -- GPIOB clock enable
STM32H743.RCC_AHB1ENR_GPIOCEN_BIT = 2  -- GPIOC clock enable
STM32H743.RCC_AHB1ENR_GPIODEN_BIT = 3  -- GPIOD clock enable
STM32H743.RCC_AHB1ENR_GPIOEEN_BIT = 4  -- GPIOE clock enable
STM32H743.RCC_AHB1ENR_DMA1EN_BIT = 21  -- DMA1 clock enable
STM32H743.RCC_AHB1ENR_DMA2EN_BIT = 22  -- DMA2 clock enable
STM32H743.RCC_AHB2ENR_ADDR = 0x34
STM32H743.RCC_AHB4ENR_ADDR = 0x3C
STM32H743.RCC_APB1LENR_ADDR = 0x50
STM32H743.RCC_APB2ENR_ADDR = 0x58
-- General Purpose I/O Port A
STM32H743.GPIOA_BASE = 0x58020000
STM32H743.GPIOA_MODER_ADDR = 0x00
STM32H743.GPIOA_OTYPER_ADDR = 0x04
STM32H743.GPIOA_OSPEEDR_ADDR = 0x08
STM32H743.GPIOA_PUPDR_ADDR = 0x0C
STM32H743.GPIOA_IDR_ADDR = 0x10
STM32H743.GPIOA_ODR_ADDR = 0x14
STM32H743.GPIOA_BSRR_ADDR = 0x18
STM32H743.GPIOA_BRR_ADDR = 0x28
-- General Purpose I/O Port B
STM32H743.GPIOB_BASE = 0x58020400
STM32H743.GPIOB_MODER_ADDR = 0x00
STM32H743.GPIOB_OTYPER_ADDR = 0x04
STM32H743.GPIOB_OSPEEDR_ADDR = 0x08
STM32H743.GPIOB_PUPDR_ADDR = 0x0C
STM32H743.GPIOB_IDR_ADDR = 0x10
STM32H743.GPIOB_ODR_ADDR = 0x14
STM32H743.GPIOB_BSRR_ADDR = 0x18
STM32H743.GPIOB_BRR_ADDR = 0x28
-- General Purpose I/O Port C
STM32H743.GPIOC_BASE = 0x58020800
STM32H743.GPIOC_MODER_ADDR = 0x00
STM32H743.GPIOC_OTYPER_ADDR = 0x04
STM32H743.GPIOC_IDR_ADDR = 0x10
STM32H743.GPIOC_ODR_ADDR = 0x14
STM32H743.GPIOC_BSRR_ADDR = 0x18
-- General Purpose I/O Port D
STM32H743.GPIOD_BASE = 0x58020C00
STM32H743.GPIOD_MODER_ADDR = 0x00
STM32H743.GPIOD_OTYPER_ADDR = 0x04
STM32H743.GPIOD_IDR_ADDR = 0x10
STM32H743.GPIOD_ODR_ADDR = 0x14
STM32H743.GPIOD_BSRR_ADDR = 0x18
-- General Purpose I/O Port E
STM32H743.GPIOE_BASE = 0x58021000
STM32H743.GPIOE_MODER_ADDR = 0x00
STM32H743.GPIOE_OTYPER_ADDR = 0x04
STM32H743.GPIOE_IDR_ADDR = 0x10
STM32H743.GPIOE_ODR_ADDR = 0x14
STM32H743.GPIOE_BSRR_ADDR = 0x18
-- USART1
STM32H743.USART1_BASE = 0x40011000
STM32H743.USART1_CR1_ADDR = 0x00
STM32H743.USART1_BRR_ADDR = 0x0C
STM32H743.USART1_RDR_ADDR = 0x24
STM32H743.USART1_TDR_ADDR = 0x28

-- 中断向量定义
STM32H743.INT_RESET = 0  -- 
STM32H743.INT_SVCALL = 11  -- 
STM32H743.INT_SYSTICK = 15  -- 
STM32H743.INT_USART1 = 56  -- USART1 Global Interrupt

-- 设备类
function STM32H743.new(memory_base)
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
            base = 0x58024400,
            type = "ResetClock",
            description = "Reset and Clock Control",
            registers = {}
        }
        
        local p = self.peripherals["RCC"]
        p.registers["CR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CFGR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["PLL1CFGR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["AHB1ENR"] = {
            address = 0x30,
            size = 4,
            value = 0
        }
        p.registers["AHB2ENR"] = {
            address = 0x34,
            size = 4,
            value = 0
        }
        p.registers["AHB4ENR"] = {
            address = 0x3C,
            size = 4,
            value = 0
        }
        p.registers["APB1LENR"] = {
            address = 0x50,
            size = 4,
            value = 0
        }
        p.registers["APB2ENR"] = {
            address = 0x58,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOA"] = {
            base = 0x58020000,
            type = "GPIO",
            description = "General Purpose I/O Port A",
            registers = {}
        }
        
        local p = self.peripherals["GPIOA"]
        p.registers["MODER"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["OTYPER"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["OSPEEDR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["PUPDR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["IDR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["ODR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["BSRR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOB"] = {
            base = 0x58020400,
            type = "GPIO",
            description = "General Purpose I/O Port B",
            registers = {}
        }
        
        local p = self.peripherals["GPIOB"]
        p.registers["MODER"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["OTYPER"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["OSPEEDR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["PUPDR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["IDR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["ODR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["BSRR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOC"] = {
            base = 0x58020800,
            type = "GPIO",
            description = "General Purpose I/O Port C",
            registers = {}
        }
        
        local p = self.peripherals["GPIOC"]
        p.registers["MODER"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["OTYPER"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IDR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["ODR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["BSRR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOD"] = {
            base = 0x58020C00,
            type = "GPIO",
            description = "General Purpose I/O Port D",
            registers = {}
        }
        
        local p = self.peripherals["GPIOD"]
        p.registers["MODER"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["OTYPER"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IDR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["ODR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["BSRR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOE"] = {
            base = 0x58021000,
            type = "GPIO",
            description = "General Purpose I/O Port E",
            registers = {}
        }
        
        local p = self.peripherals["GPIOE"]
        p.registers["MODER"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["OTYPER"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["IDR"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["ODR"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["BSRR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        self.peripherals["USART1"] = {
            base = 0x40011000,
            type = "UART",
            description = "USART1",
            registers = {}
        }
        
        local p = self.peripherals["USART1"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["RDR"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["TDR"] = {
            address = 0x28,
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
            name = STM32H743.DEVICE_NAME,
            manufacturer = STM32H743.MANUFACTURER,
            family = STM32H743.FAMILY,
            version = STM32H743.VERSION,
            architecture = STM32H743.ARCHITECTURE,
            bits = STM32H743.BITS,
            clock_frequency = STM32H743.CLOCK_FREQUENCY
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
        return string.format("STM32H743(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function STM32H743.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function STM32H743.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function STM32H743.print_device_info(device)
    device = device or STM32H743.new()
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

function STM32H743.print_registers(device)
    device = device or STM32H743.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            STM32H743.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function STM32H743.example()
    print("=== STM32H743设备示例 ===")
    
    -- 创建设备实例
    local device = STM32H743.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    STM32H743.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. STM32H743.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. STM32H743.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    STM32H743.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("STM32H743.lua$") then
    STM32H743.example()
end

return STM32H743

--[[
  STM32F303CCT6设备定义 - Lua模块
  生成自: STMicroelectronics/STM32/STM32F303CCT6
  版本: 1.0
  日期: 2026-04-29
  作者: VML Team
  描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 48KB SRAM, 72MHz, FPU+DSP
  CPU架构: ARM-Cortex-M4F
  位宽: 32位
  时钟频率: 72000000 Hz
]]

local STM32F303CCT6 = {}

-- 设备信息
STM32F303CCT6.DEVICE_NAME = "STM32F303CCT6"
STM32F303CCT6.MANUFACTURER = "STMicroelectronics"
STM32F303CCT6.FAMILY = "STM32"
STM32F303CCT6.VERSION = "1.0"
STM32F303CCT6.ARCHITECTURE = "ARM-Cortex-M4F"
STM32F303CCT6.BITS = 32
STM32F303CCT6.CLOCK_FREQUENCY = 72000000

-- 外设定义
-- USART 1
STM32F303CCT6.USART1_BASE = 0x40013800
STM32F303CCT6.USART1_SR_ADDR = 0x00
STM32F303CCT6.USART1_DR_ADDR = 0x04
STM32F303CCT6.USART1_BRR_ADDR = 0x08
STM32F303CCT6.USART1_CR1_ADDR = 0x0C
STM32F303CCT6.USART1_CR2_ADDR = 0x10
STM32F303CCT6.USART1_CR3_ADDR = 0x14
-- USART 2
STM32F303CCT6.USART2_BASE = 0x40004400
STM32F303CCT6.USART2_SR_ADDR = 0x00
STM32F303CCT6.USART2_DR_ADDR = 0x04
STM32F303CCT6.USART2_BRR_ADDR = 0x08
STM32F303CCT6.USART2_CR1_ADDR = 0x0C
-- USART 3
STM32F303CCT6.USART3_BASE = 0x40004800
STM32F303CCT6.USART3_SR_ADDR = 0x00
STM32F303CCT6.USART3_DR_ADDR = 0x04
STM32F303CCT6.USART3_BRR_ADDR = 0x08
STM32F303CCT6.USART3_CR1_ADDR = 0x0C
-- GPIO Port A
STM32F303CCT6.GPIOA_BASE = 0x48000000
STM32F303CCT6.GPIOA_MODER_ADDR = 0x00
STM32F303CCT6.GPIOA_OTYPER_ADDR = 0x04
STM32F303CCT6.GPIOA_OSPEEDR_ADDR = 0x08
STM32F303CCT6.GPIOA_PUPDR_ADDR = 0x0C
STM32F303CCT6.GPIOA_IDR_ADDR = 0x10
STM32F303CCT6.GPIOA_ODR_ADDR = 0x14
STM32F303CCT6.GPIOA_BSRR_ADDR = 0x18
STM32F303CCT6.GPIOA_AFRL_ADDR = 0x20
STM32F303CCT6.GPIOA_AFRH_ADDR = 0x24
-- 高级定时器 1
STM32F303CCT6.TIM1_BASE = 0x40012C00
STM32F303CCT6.TIM1_CR1_ADDR = 0x00
STM32F303CCT6.TIM1_CNT_ADDR = 0x24
STM32F303CCT6.TIM1_PSC_ADDR = 0x28
STM32F303CCT6.TIM1_ARR_ADDR = 0x2C
STM32F303CCT6.TIM1_CCR1_ADDR = 0x34
-- ADC 1
STM32F303CCT6.ADC1_BASE = 0x50000000
STM32F303CCT6.ADC1_SR_ADDR = 0x00
STM32F303CCT6.ADC1_CR_ADDR = 0x08
STM32F303CCT6.ADC1_CFGR_ADDR = 0x0C
STM32F303CCT6.ADC1_SMPR1_ADDR = 0x14
STM32F303CCT6.ADC1_DR_ADDR = 0x40

-- 设备类
function STM32F303CCT6.new(memory_base)
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
        self.peripherals["USART1"] = {
            base = 0x40013800,
            type = "uart",
            description = "USART 1",
            registers = {}
        }
        
        local p = self.peripherals["USART1"]
        p.registers["SR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["CR3"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        self.peripherals["USART2"] = {
            base = 0x40004400,
            type = "uart",
            description = "USART 2",
            registers = {}
        }
        
        local p = self.peripherals["USART2"]
        p.registers["SR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["USART3"] = {
            base = 0x40004800,
            type = "uart",
            description = "USART 3",
            registers = {}
        }
        
        local p = self.peripherals["USART3"]
        p.registers["SR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOA"] = {
            base = 0x48000000,
            type = "gpio",
            description = "GPIO Port A",
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
        p.registers["AFRL"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["AFRH"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        self.peripherals["TIM1"] = {
            base = 0x40012C00,
            type = "timer",
            description = "高级定时器 1",
            registers = {}
        }
        
        local p = self.peripherals["TIM1"]
        p.registers["CR1"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CNT"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["PSC"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        p.registers["ARR"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        p.registers["CCR1"] = {
            address = 0x34,
            size = 4,
            value = 0
        }
        self.peripherals["ADC1"] = {
            base = 0x50000000,
            type = "adc",
            description = "ADC 1",
            registers = {}
        }
        
        local p = self.peripherals["ADC1"]
        p.registers["SR"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["CR"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["CFGR"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["SMPR1"] = {
            address = 0x14,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x40,
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
            name = STM32F303CCT6.DEVICE_NAME,
            manufacturer = STM32F303CCT6.MANUFACTURER,
            family = STM32F303CCT6.FAMILY,
            version = STM32F303CCT6.VERSION,
            architecture = STM32F303CCT6.ARCHITECTURE,
            bits = STM32F303CCT6.BITS,
            clock_frequency = STM32F303CCT6.CLOCK_FREQUENCY
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
        return string.format("STM32F303CCT6(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function STM32F303CCT6.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function STM32F303CCT6.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function STM32F303CCT6.print_device_info(device)
    device = device or STM32F303CCT6.new()
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

function STM32F303CCT6.print_registers(device)
    device = device or STM32F303CCT6.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            STM32F303CCT6.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function STM32F303CCT6.example()
    print("=== STM32F303CCT6设备示例 ===")
    
    -- 创建设备实例
    local device = STM32F303CCT6.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    STM32F303CCT6.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    STM32F303CCT6.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("STM32F303CCT6.lua$") then
    STM32F303CCT6.example()
end

return STM32F303CCT6

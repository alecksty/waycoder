--[[
  TM4C123GH6PM设备定义 - Lua模块
  生成自: Texas Instruments/Tiva C/TM4C123GH6PM
  版本: 1.0
  日期: 2026-04-29
  作者: VML Team
  描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 32KB SRAM, 80MHz, USB
  CPU架构: ARM-Cortex-M4F
  位宽: 32位
  时钟频率: 80000000 Hz
]]

local TM4C123GH6PM = {}

-- 设备信息
TM4C123GH6PM.DEVICE_NAME = "TM4C123GH6PM"
TM4C123GH6PM.MANUFACTURER = "Texas Instruments"
TM4C123GH6PM.FAMILY = "Tiva C"
TM4C123GH6PM.VERSION = "1.0"
TM4C123GH6PM.ARCHITECTURE = "ARM-Cortex-M4F"
TM4C123GH6PM.BITS = 32
TM4C123GH6PM.CLOCK_FREQUENCY = 80000000

-- 外设定义
-- UART 0
TM4C123GH6PM.UART0_BASE = 0x4000C000
TM4C123GH6PM.UART0_DR_ADDR = 0x000
TM4C123GH6PM.UART0_FR_ADDR = 0x018
TM4C123GH6PM.UART0_IBRD_ADDR = 0x024
TM4C123GH6PM.UART0_FBRD_ADDR = 0x028
TM4C123GH6PM.UART0_LCRH_ADDR = 0x02C
TM4C123GH6PM.UART0_CTL_ADDR = 0x030
TM4C123GH6PM.UART0_IM_ADDR = 0x038
TM4C123GH6PM.UART0_RIS_ADDR = 0x03C
TM4C123GH6PM.UART0_ICR_ADDR = 0x044
-- UART 1
TM4C123GH6PM.UART1_BASE = 0x4000D000
TM4C123GH6PM.UART1_DR_ADDR = 0x000
TM4C123GH6PM.UART1_FR_ADDR = 0x018
TM4C123GH6PM.UART1_IBRD_ADDR = 0x024
TM4C123GH6PM.UART1_FBRD_ADDR = 0x028
TM4C123GH6PM.UART1_LCRH_ADDR = 0x02C
TM4C123GH6PM.UART1_CTL_ADDR = 0x030
-- GPIO Port A
TM4C123GH6PM.GPIOA_BASE = 0x40004000
TM4C123GH6PM.GPIOA_DATA_ADDR = 0x3FC
TM4C123GH6PM.GPIOA_DIR_ADDR = 0x400
TM4C123GH6PM.GPIOA_IS_ADDR = 0x404
TM4C123GH6PM.GPIOA_IBE_ADDR = 0x408
TM4C123GH6PM.GPIOA_IEV_ADDR = 0x40C
TM4C123GH6PM.GPIOA_IM_ADDR = 0x410
TM4C123GH6PM.GPIOA_RIS_ADDR = 0x414
TM4C123GH6PM.GPIOA_MIS_ADDR = 0x418
TM4C123GH6PM.GPIOA_ICR_ADDR = 0x41C
TM4C123GH6PM.GPIOA_AFSEL_ADDR = 0x420
TM4C123GH6PM.GPIOA_DEN_ADDR = 0x51C
-- 16/32-bit Timer 0
TM4C123GH6PM.TIMER0_BASE = 0x40030000
TM4C123GH6PM.TIMER0_CFG_ADDR = 0x000
TM4C123GH6PM.TIMER0_TAMR_ADDR = 0x004
TM4C123GH6PM.TIMER0_CTL_ADDR = 0x00C
TM4C123GH6PM.TIMER0_ILR_ADDR = 0x028
TM4C123GH6PM.TIMER0_V_ADDR = 0x038
TM4C123GH6PM.TIMER0_ICR_ADDR = 0x024
-- ADC 0
TM4C123GH6PM.ADC0_BASE = 0x40038000
TM4C123GH6PM.ADC0_ACTSS_ADDR = 0x000
TM4C123GH6PM.ADC0_EMUX_ADDR = 0x014
TM4C123GH6PM.ADC0_SSMUX0_ADDR = 0x040
TM4C123GH6PM.ADC0_SSFIFO0_ADDR = 0x048
TM4C123GH6PM.ADC0_PROC_ADDR = 0x030

-- 设备类
function TM4C123GH6PM.new(memory_base)
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
        self.peripherals["UART0"] = {
            base = 0x4000C000,
            type = "uart",
            description = "UART 0",
            registers = {}
        }
        
        local p = self.peripherals["UART0"]
        p.registers["DR"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["FR"] = {
            address = 0x018,
            size = 4,
            value = 0
        }
        p.registers["IBRD"] = {
            address = 0x024,
            size = 4,
            value = 0
        }
        p.registers["FBRD"] = {
            address = 0x028,
            size = 4,
            value = 0
        }
        p.registers["LCRH"] = {
            address = 0x02C,
            size = 4,
            value = 0
        }
        p.registers["CTL"] = {
            address = 0x030,
            size = 4,
            value = 0
        }
        p.registers["IM"] = {
            address = 0x038,
            size = 4,
            value = 0
        }
        p.registers["RIS"] = {
            address = 0x03C,
            size = 4,
            value = 0
        }
        p.registers["ICR"] = {
            address = 0x044,
            size = 4,
            value = 0
        }
        self.peripherals["UART1"] = {
            base = 0x4000D000,
            type = "uart",
            description = "UART 1",
            registers = {}
        }
        
        local p = self.peripherals["UART1"]
        p.registers["DR"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["FR"] = {
            address = 0x018,
            size = 4,
            value = 0
        }
        p.registers["IBRD"] = {
            address = 0x024,
            size = 4,
            value = 0
        }
        p.registers["FBRD"] = {
            address = 0x028,
            size = 4,
            value = 0
        }
        p.registers["LCRH"] = {
            address = 0x02C,
            size = 4,
            value = 0
        }
        p.registers["CTL"] = {
            address = 0x030,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOA"] = {
            base = 0x40004000,
            type = "gpio",
            description = "GPIO Port A",
            registers = {}
        }
        
        local p = self.peripherals["GPIOA"]
        p.registers["DATA"] = {
            address = 0x3FC,
            size = 4,
            value = 0
        }
        p.registers["DIR"] = {
            address = 0x400,
            size = 4,
            value = 0
        }
        p.registers["IS"] = {
            address = 0x404,
            size = 4,
            value = 0
        }
        p.registers["IBE"] = {
            address = 0x408,
            size = 4,
            value = 0
        }
        p.registers["IEV"] = {
            address = 0x40C,
            size = 4,
            value = 0
        }
        p.registers["IM"] = {
            address = 0x410,
            size = 4,
            value = 0
        }
        p.registers["RIS"] = {
            address = 0x414,
            size = 4,
            value = 0
        }
        p.registers["MIS"] = {
            address = 0x418,
            size = 4,
            value = 0
        }
        p.registers["ICR"] = {
            address = 0x41C,
            size = 4,
            value = 0
        }
        p.registers["AFSEL"] = {
            address = 0x420,
            size = 4,
            value = 0
        }
        p.registers["DEN"] = {
            address = 0x51C,
            size = 4,
            value = 0
        }
        self.peripherals["TIMER0"] = {
            base = 0x40030000,
            type = "timer",
            description = "16/32-bit Timer 0",
            registers = {}
        }
        
        local p = self.peripherals["TIMER0"]
        p.registers["CFG"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["TAMR"] = {
            address = 0x004,
            size = 4,
            value = 0
        }
        p.registers["CTL"] = {
            address = 0x00C,
            size = 4,
            value = 0
        }
        p.registers["ILR"] = {
            address = 0x028,
            size = 4,
            value = 0
        }
        p.registers["V"] = {
            address = 0x038,
            size = 4,
            value = 0
        }
        p.registers["ICR"] = {
            address = 0x024,
            size = 4,
            value = 0
        }
        self.peripherals["ADC0"] = {
            base = 0x40038000,
            type = "adc",
            description = "ADC 0",
            registers = {}
        }
        
        local p = self.peripherals["ADC0"]
        p.registers["ACTSS"] = {
            address = 0x000,
            size = 4,
            value = 0
        }
        p.registers["EMUX"] = {
            address = 0x014,
            size = 4,
            value = 0
        }
        p.registers["SSMUX0"] = {
            address = 0x040,
            size = 4,
            value = 0
        }
        p.registers["SSFIFO0"] = {
            address = 0x048,
            size = 4,
            value = 0
        }
        p.registers["PROC"] = {
            address = 0x030,
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
            name = TM4C123GH6PM.DEVICE_NAME,
            manufacturer = TM4C123GH6PM.MANUFACTURER,
            family = TM4C123GH6PM.FAMILY,
            version = TM4C123GH6PM.VERSION,
            architecture = TM4C123GH6PM.ARCHITECTURE,
            bits = TM4C123GH6PM.BITS,
            clock_frequency = TM4C123GH6PM.CLOCK_FREQUENCY
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
        return string.format("TM4C123GH6PM(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function TM4C123GH6PM.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function TM4C123GH6PM.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function TM4C123GH6PM.print_device_info(device)
    device = device or TM4C123GH6PM.new()
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

function TM4C123GH6PM.print_registers(device)
    device = device or TM4C123GH6PM.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            TM4C123GH6PM.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function TM4C123GH6PM.example()
    print("=== TM4C123GH6PM设备示例 ===")
    
    -- 创建设备实例
    local device = TM4C123GH6PM.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    TM4C123GH6PM.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    TM4C123GH6PM.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("TM4C123GH6PM.lua$") then
    TM4C123GH6PM.example()
end

return TM4C123GH6PM

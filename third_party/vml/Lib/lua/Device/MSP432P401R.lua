--[[
  MSP432P401R设备定义 - Lua模块
  生成自: Texas Instruments/MSP432/MSP432P401R
  版本: 1.0
  日期: 2026-04-29
  作者: VML Team
  描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 64KB SRAM, 48MHz, FPU
  CPU架构: ARM-Cortex-M4F
  位宽: 32位
  时钟频率: 48000000 Hz
]]

local MSP432P401R = {}

-- 设备信息
MSP432P401R.DEVICE_NAME = "MSP432P401R"
MSP432P401R.MANUFACTURER = "Texas Instruments"
MSP432P401R.FAMILY = "MSP432"
MSP432P401R.VERSION = "1.0"
MSP432P401R.ARCHITECTURE = "ARM-Cortex-M4F"
MSP432P401R.BITS = 32
MSP432P401R.CLOCK_FREQUENCY = 48000000

-- 外设定义
-- eUSCI_A0 UART
MSP432P401R.UART0_BASE = 0x40001000
MSP432P401R.UART0_CTLW0_ADDR = 0x00
MSP432P401R.UART0_BRW_ADDR = 0x06
MSP432P401R.UART0_UCA0TXBUF_ADDR = 0x08
MSP432P401R.UART0_UCA0RXBUF_ADDR = 0x0A
MSP432P401R.UART0_IFG_ADDR = 0x0C
MSP432P401R.UART0_IE_ADDR = 0x0E
-- eUSCI_A1 UART
MSP432P401R.UART1_BASE = 0x40002000
MSP432P401R.UART1_CTLW0_ADDR = 0x00
MSP432P401R.UART1_BRW_ADDR = 0x06
MSP432P401R.UART1_TXBUF_ADDR = 0x08
MSP432P401R.UART1_RXBUF_ADDR = 0x0A
MSP432P401R.UART1_IFG_ADDR = 0x0C
MSP432P401R.UART1_IE_ADDR = 0x0E
-- Timer_A0 16bit
MSP432P401R.TIMER0_BASE = 0x40003000
MSP432P401R.TIMER0_CTL_ADDR = 0x00
MSP432P401R.TIMER0_R_ADDR = 0x10
MSP432P401R.TIMER0_CCR0_ADDR = 0x12
MSP432P401R.TIMER0_CCR1_ADDR = 0x14
MSP432P401R.TIMER0_CCR2_ADDR = 0x16
MSP432P401R.TIMER0_EX0_ADDR = 0x20
-- ADC14 14-bit
MSP432P401R.ADC14_BASE = 0x40006000
MSP432P401R.ADC14_CTL0_ADDR = 0x00
MSP432P401R.ADC14_CTL1_ADDR = 0x02
MSP432P401R.ADC14_LO_ADDR = 0x04
MSP432P401R.ADC14_HI_ADDR = 0x06
MSP432P401R.ADC14_MCTL0_ADDR = 0x08
MSP432P401R.ADC14_MEM0_ADDR = 0x20

-- 设备类
function MSP432P401R.new(memory_base)
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
            base = 0x40001000,
            type = "uart",
            description = "eUSCI_A0 UART",
            registers = {}
        }
        
        local p = self.peripherals["UART0"]
        p.registers["CTLW0"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["BRW"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        p.registers["UCA0TXBUF"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["UCA0RXBUF"] = {
            address = 0x0A,
            size = 2,
            value = 0
        }
        p.registers["IFG"] = {
            address = 0x0C,
            size = 2,
            value = 0
        }
        p.registers["IE"] = {
            address = 0x0E,
            size = 2,
            value = 0
        }
        self.peripherals["UART1"] = {
            base = 0x40002000,
            type = "uart",
            description = "eUSCI_A1 UART",
            registers = {}
        }
        
        local p = self.peripherals["UART1"]
        p.registers["CTLW0"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["BRW"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        p.registers["TXBUF"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["RXBUF"] = {
            address = 0x0A,
            size = 2,
            value = 0
        }
        p.registers["IFG"] = {
            address = 0x0C,
            size = 2,
            value = 0
        }
        p.registers["IE"] = {
            address = 0x0E,
            size = 2,
            value = 0
        }
        self.peripherals["TIMER0"] = {
            base = 0x40003000,
            type = "timer",
            description = "Timer_A0 16bit",
            registers = {}
        }
        
        local p = self.peripherals["TIMER0"]
        p.registers["CTL"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["R"] = {
            address = 0x10,
            size = 2,
            value = 0
        }
        p.registers["CCR0"] = {
            address = 0x12,
            size = 2,
            value = 0
        }
        p.registers["CCR1"] = {
            address = 0x14,
            size = 2,
            value = 0
        }
        p.registers["CCR2"] = {
            address = 0x16,
            size = 2,
            value = 0
        }
        p.registers["EX0"] = {
            address = 0x20,
            size = 2,
            value = 0
        }
        self.peripherals["ADC14"] = {
            base = 0x40006000,
            type = "adc",
            description = "ADC14 14-bit",
            registers = {}
        }
        
        local p = self.peripherals["ADC14"]
        p.registers["CTL0"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["CTL1"] = {
            address = 0x02,
            size = 2,
            value = 0
        }
        p.registers["LO"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["HI"] = {
            address = 0x06,
            size = 2,
            value = 0
        }
        p.registers["MCTL0"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["MEM0"] = {
            address = 0x20,
            size = 2,
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
            name = MSP432P401R.DEVICE_NAME,
            manufacturer = MSP432P401R.MANUFACTURER,
            family = MSP432P401R.FAMILY,
            version = MSP432P401R.VERSION,
            architecture = MSP432P401R.ARCHITECTURE,
            bits = MSP432P401R.BITS,
            clock_frequency = MSP432P401R.CLOCK_FREQUENCY
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
        return string.format("MSP432P401R(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function MSP432P401R.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function MSP432P401R.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function MSP432P401R.print_device_info(device)
    device = device or MSP432P401R.new()
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

function MSP432P401R.print_registers(device)
    device = device or MSP432P401R.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            MSP432P401R.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function MSP432P401R.example()
    print("=== MSP432P401R设备示例 ===")
    
    -- 创建设备实例
    local device = MSP432P401R.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    MSP432P401R.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    MSP432P401R.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("MSP432P401R.lua$") then
    MSP432P401R.example()
end

return MSP432P401R

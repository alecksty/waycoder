--[[
  ATmega2560设备定义 - Lua模块
  生成自: Atmel/AVR/ATmega2560
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 8-bit AVR MCU with 256KB Flash, 8KB RAM, 4KB EEPROM, 16MHz, Arduino Mega
  CPU架构: AVR
  位宽: 8位
  时钟频率: 16000000 Hz
]]

local ATmega2560 = {}

-- 设备信息
ATmega2560.DEVICE_NAME = "ATmega2560"
ATmega2560.MANUFACTURER = "Atmel"
ATmega2560.FAMILY = "AVR"
ATmega2560.VERSION = "1.0"
ATmega2560.ARCHITECTURE = "AVR"
ATmega2560.BITS = 8
ATmega2560.CLOCK_FREQUENCY = 16000000

-- 寄存器地址定义
ATmega2560.R0_ADDR = 0x00  -- 
ATmega2560.R1_ADDR = 0x01  -- 
ATmega2560.R2_ADDR = 0x02  -- 
ATmega2560.SPL_ADDR = 0x5D  -- 
ATmega2560.SPH_ADDR = 0x5E  -- 
ATmega2560.SREG_ADDR = 0x5F  -- 

-- 内存段定义
ATmega2560.FLASH_START = 0x0000
ATmega2560.FLASH_END = 0x3FFFF
ATmega2560.FLASH_SIZE = 262144  -- 
ATmega2560.SRAM_START = 0x0200
ATmega2560.SRAM_END = 0x21FF
ATmega2560.SRAM_SIZE = 8192  -- 
ATmega2560.EEPROM_START = 0x0000
ATmega2560.EEPROM_END = 0x0FFF
ATmega2560.EEPROM_SIZE = 4096  -- 
ATmega2560.IO_START = 0x00
ATmega2560.IO_END = 0x3F
ATmega2560.IO_SIZE = 64  -- 
ATmega2560.EXTIO_START = 0x40
ATmega2560.EXTIO_END = 0xFF
ATmega2560.EXTIO_SIZE = 192  -- 

-- 外设定义
-- Port A
ATmega2560.PORTA_BASE = 0x22
ATmega2560.PORTA_DDRA_ADDR = 0x21
ATmega2560.PORTA_PORTA_ADDR = 0x22
ATmega2560.PORTA_PINA_ADDR = 0x20
-- Port B
ATmega2560.PORTB_BASE = 0x25
ATmega2560.PORTB_DDRB_ADDR = 0x24
ATmega2560.PORTB_PORTB_ADDR = 0x25
ATmega2560.PORTB_PINB_ADDR = 0x23
-- Port C
ATmega2560.PORTC_BASE = 0x28
ATmega2560.PORTC_DDRC_ADDR = 0x27
ATmega2560.PORTC_PORTC_ADDR = 0x28
ATmega2560.PORTC_PINC_ADDR = 0x26
-- Port D
ATmega2560.PORTD_BASE = 0x2B
ATmega2560.PORTD_DDRD_ADDR = 0x2A
ATmega2560.PORTD_PORTD_ADDR = 0x2B
ATmega2560.PORTD_PIND_ADDR = 0x29
-- Port E
ATmega2560.PORTE_BASE = 0x2E
ATmega2560.PORTE_DDRE_ADDR = 0x2D
ATmega2560.PORTE_PORTE_ADDR = 0x2E
ATmega2560.PORTE_PINE_ADDR = 0x2C
-- Port F
ATmega2560.PORTF_BASE = 0x31
ATmega2560.PORTF_DDRF_ADDR = 0x30
ATmega2560.PORTF_PORTF_ADDR = 0x31
ATmega2560.PORTF_PINF_ADDR = 0x2F
-- Port G
ATmega2560.PORTG_BASE = 0x34
ATmega2560.PORTG_DDRG_ADDR = 0x33
ATmega2560.PORTG_PORTG_ADDR = 0x34
ATmega2560.PORTG_PING_ADDR = 0x32
-- USART 0
ATmega2560.USART0_BASE = 0xC0
ATmega2560.USART0_UDR0_ADDR = 0xC6
ATmega2560.USART0_UCSR0A_ADDR = 0xC0
ATmega2560.USART0_UCSR0B_ADDR = 0xC1
ATmega2560.USART0_UCSR0C_ADDR = 0xC2
ATmega2560.USART0_UBRR0L_ADDR = 0xC4
ATmega2560.USART0_UBRR0H_ADDR = 0xC5

-- 中断向量定义
ATmega2560.INT_RESET = 1  -- 
ATmega2560.INT_INT0 = 2  -- 
ATmega2560.INT_INT1 = 3  -- 
ATmega2560.INT_INT2 = 4  -- 
ATmega2560.INT_INT3 = 5  -- 
ATmega2560.INT_INT4 = 6  -- 
ATmega2560.INT_INT5 = 7  -- 
ATmega2560.INT_INT6 = 8  -- 
ATmega2560.INT_INT7 = 9  -- 
ATmega2560.INT_PCINT0 = 10  -- 
ATmega2560.INT_PCINT1 = 11  -- 
ATmega2560.INT_PCINT2 = 12  -- 
ATmega2560.INT_WDT = 13  -- 
ATmega2560.INT_TIM2_COMPA = 14  -- 
ATmega2560.INT_TIM2_COMPB = 15  -- 
ATmega2560.INT_TIM2_OVF = 16  -- 
ATmega2560.INT_TIM1_CAPT = 17  -- 
ATmega2560.INT_TIM1_COMPA = 18  -- 
ATmega2560.INT_TIM1_COMPB = 19  -- 
ATmega2560.INT_TIM1_OVF = 20  -- 
ATmega2560.INT_TIM0_COMPA = 21  -- 
ATmega2560.INT_TIM0_COMPB = 22  -- 
ATmega2560.INT_TIM0_OVF = 23  -- 
ATmega2560.INT_SPI_STC = 24  -- 
ATmega2560.INT_USART0_RX = 25  -- 
ATmega2560.INT_USART0_UDRE = 26  -- 
ATmega2560.INT_USART0_TX = 27  -- 

-- 设备类
function ATmega2560.new(memory_base)
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
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["SPL"] = {
            address = 0x5D,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["SPH"] = {
            address = 0x5E,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["SREG"] = {
            address = 0x5F,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PORTA"] = {
            base = 0x22,
            type = "GPIO",
            description = "Port A",
            registers = {}
        }
        
        local p = self.peripherals["PORTA"]
        p.registers["DDRA"] = {
            address = 0x21,
            size = 1,
            value = 0
        }
        p.registers["PORTA"] = {
            address = 0x22,
            size = 1,
            value = 0
        }
        p.registers["PINA"] = {
            address = 0x20,
            size = 1,
            value = 0
        }
        self.peripherals["PORTB"] = {
            base = 0x25,
            type = "GPIO",
            description = "Port B",
            registers = {}
        }
        
        local p = self.peripherals["PORTB"]
        p.registers["DDRB"] = {
            address = 0x24,
            size = 1,
            value = 0
        }
        p.registers["PORTB"] = {
            address = 0x25,
            size = 1,
            value = 0
        }
        p.registers["PINB"] = {
            address = 0x23,
            size = 1,
            value = 0
        }
        self.peripherals["PORTC"] = {
            base = 0x28,
            type = "GPIO",
            description = "Port C",
            registers = {}
        }
        
        local p = self.peripherals["PORTC"]
        p.registers["DDRC"] = {
            address = 0x27,
            size = 1,
            value = 0
        }
        p.registers["PORTC"] = {
            address = 0x28,
            size = 1,
            value = 0
        }
        p.registers["PINC"] = {
            address = 0x26,
            size = 1,
            value = 0
        }
        self.peripherals["PORTD"] = {
            base = 0x2B,
            type = "GPIO",
            description = "Port D",
            registers = {}
        }
        
        local p = self.peripherals["PORTD"]
        p.registers["DDRD"] = {
            address = 0x2A,
            size = 1,
            value = 0
        }
        p.registers["PORTD"] = {
            address = 0x2B,
            size = 1,
            value = 0
        }
        p.registers["PIND"] = {
            address = 0x29,
            size = 1,
            value = 0
        }
        self.peripherals["PORTE"] = {
            base = 0x2E,
            type = "GPIO",
            description = "Port E",
            registers = {}
        }
        
        local p = self.peripherals["PORTE"]
        p.registers["DDRE"] = {
            address = 0x2D,
            size = 1,
            value = 0
        }
        p.registers["PORTE"] = {
            address = 0x2E,
            size = 1,
            value = 0
        }
        p.registers["PINE"] = {
            address = 0x2C,
            size = 1,
            value = 0
        }
        self.peripherals["PORTF"] = {
            base = 0x31,
            type = "GPIO",
            description = "Port F",
            registers = {}
        }
        
        local p = self.peripherals["PORTF"]
        p.registers["DDRF"] = {
            address = 0x30,
            size = 1,
            value = 0
        }
        p.registers["PORTF"] = {
            address = 0x31,
            size = 1,
            value = 0
        }
        p.registers["PINF"] = {
            address = 0x2F,
            size = 1,
            value = 0
        }
        self.peripherals["PORTG"] = {
            base = 0x34,
            type = "GPIO",
            description = "Port G",
            registers = {}
        }
        
        local p = self.peripherals["PORTG"]
        p.registers["DDRG"] = {
            address = 0x33,
            size = 1,
            value = 0
        }
        p.registers["PORTG"] = {
            address = 0x34,
            size = 1,
            value = 0
        }
        p.registers["PING"] = {
            address = 0x32,
            size = 1,
            value = 0
        }
        self.peripherals["USART0"] = {
            base = 0xC0,
            type = "UART",
            description = "USART 0",
            registers = {}
        }
        
        local p = self.peripherals["USART0"]
        p.registers["UDR0"] = {
            address = 0xC6,
            size = 1,
            value = 0
        }
        p.registers["UCSR0A"] = {
            address = 0xC0,
            size = 1,
            value = 0
        }
        p.registers["UCSR0B"] = {
            address = 0xC1,
            size = 1,
            value = 0
        }
        p.registers["UCSR0C"] = {
            address = 0xC2,
            size = 1,
            value = 0
        }
        p.registers["UBRR0L"] = {
            address = 0xC4,
            size = 1,
            value = 0
        }
        p.registers["UBRR0H"] = {
            address = 0xC5,
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
            name = ATmega2560.DEVICE_NAME,
            manufacturer = ATmega2560.MANUFACTURER,
            family = ATmega2560.FAMILY,
            version = ATmega2560.VERSION,
            architecture = ATmega2560.ARCHITECTURE,
            bits = ATmega2560.BITS,
            clock_frequency = ATmega2560.CLOCK_FREQUENCY
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
        return string.format("ATmega2560(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ATmega2560.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ATmega2560.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ATmega2560.print_device_info(device)
    device = device or ATmega2560.new()
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

function ATmega2560.print_registers(device)
    device = device or ATmega2560.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ATmega2560.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ATmega2560.example()
    print("=== ATmega2560设备示例 ===")
    
    -- 创建设备实例
    local device = ATmega2560.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ATmega2560.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. ATmega2560.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. ATmega2560.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ATmega2560.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ATmega2560.lua$") then
    ATmega2560.example()
end

return ATmega2560

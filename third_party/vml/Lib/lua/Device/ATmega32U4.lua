--[[
  ATmega32U4设备定义 - Lua模块
  生成自: Atmel/AVR/ATmega32U4
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 8-bit AVR microcontroller with USB, 32KB Flash, 2.5KB SRAM, 16MHz
  CPU架构: AVR
  位宽: 8位
  时钟频率: 16000000 Hz
]]

local ATmega32U4 = {}

-- 设备信息
ATmega32U4.DEVICE_NAME = "ATmega32U4"
ATmega32U4.MANUFACTURER = "Atmel"
ATmega32U4.FAMILY = "AVR"
ATmega32U4.VERSION = "1.0"
ATmega32U4.ARCHITECTURE = "AVR"
ATmega32U4.BITS = 8
ATmega32U4.CLOCK_FREQUENCY = 16000000

-- 寄存器地址定义
ATmega32U4.R0_ADDR = 0x00  -- 
ATmega32U4.R1_ADDR = 0x01  -- 
ATmega32U4.R2_ADDR = 0x02  -- 
ATmega32U4.R3_ADDR = 0x03  -- 
ATmega32U4.R4_ADDR = 0x04  -- 
ATmega32U4.R5_ADDR = 0x05  -- 
ATmega32U4.R6_ADDR = 0x06  -- 
ATmega32U4.R7_ADDR = 0x07  -- 
ATmega32U4.R8_ADDR = 0x08  -- 
ATmega32U4.R9_ADDR = 0x09  -- 
ATmega32U4.R10_ADDR = 0x0A  -- 
ATmega32U4.R11_ADDR = 0x0B  -- 
ATmega32U4.R12_ADDR = 0x0C  -- 
ATmega32U4.R13_ADDR = 0x0D  -- 
ATmega32U4.R14_ADDR = 0x0E  -- 
ATmega32U4.R15_ADDR = 0x0F  -- 
ATmega32U4.R16_ADDR = 0x10  -- 
ATmega32U4.R17_ADDR = 0x11  -- 
ATmega32U4.R18_ADDR = 0x12  -- 
ATmega32U4.R19_ADDR = 0x13  -- 
ATmega32U4.R20_ADDR = 0x14  -- 
ATmega32U4.R21_ADDR = 0x15  -- 
ATmega32U4.R22_ADDR = 0x16  -- 
ATmega32U4.R23_ADDR = 0x17  -- 
ATmega32U4.R24_ADDR = 0x18  -- 
ATmega32U4.R25_ADDR = 0x19  -- 
ATmega32U4.R26_ADDR = 0x1A  -- 
ATmega32U4.R27_ADDR = 0x1B  -- 
ATmega32U4.R28_ADDR = 0x1C  -- 
ATmega32U4.R29_ADDR = 0x1D  -- 
ATmega32U4.R30_ADDR = 0x1E  -- 
ATmega32U4.R31_ADDR = 0x1F  -- 
ATmega32U4.SPL_ADDR = 0x5D  -- 
ATmega32U4.SPH_ADDR = 0x5E  -- 
ATmega32U4.SREG_ADDR = 0x5F  -- 

-- 内存段定义
ATmega32U4.FLASH_START = 0x0000
ATmega32U4.FLASH_END = 0x7FFF
ATmega32U4.FLASH_SIZE = 32768  -- Program Flash Memory
ATmega32U4.SRAM_START = 0x0100
ATmega32U4.SRAM_END = 0x0AFF
ATmega32U4.SRAM_SIZE = 2560  -- Static RAM
ATmega32U4.EEPROM_START = 0x0000
ATmega32U4.EEPROM_END = 0x03FF
ATmega32U4.EEPROM_SIZE = 1024  -- EEPROM
ATmega32U4.IO_START = 0x00
ATmega32U4.IO_END = 0x3F
ATmega32U4.IO_SIZE = 64  -- I/O Registers
ATmega32U4.EXTIO_START = 0x40
ATmega32U4.EXTIO_END = 0xFF
ATmega32U4.EXTIO_SIZE = 192  -- Extended I/O Registers

-- 外设定义
-- Port B
ATmega32U4.PORTB_BASE = 0x23
ATmega32U4.PORTB_PORTB_ADDR = 0x25
ATmega32U4.PORTB_DDRB_ADDR = 0x24
ATmega32U4.PORTB_PINB_ADDR = 0x23
-- Port C
ATmega32U4.PORTC_BASE = 0x26
ATmega32U4.PORTC_PORTC_ADDR = 0x28
ATmega32U4.PORTC_DDRC_ADDR = 0x27
ATmega32U4.PORTC_PINC_ADDR = 0x26
-- Port D
ATmega32U4.PORTD_BASE = 0x29
ATmega32U4.PORTD_PORTD_ADDR = 0x2B
ATmega32U4.PORTD_DDRD_ADDR = 0x2A
ATmega32U4.PORTD_PIND_ADDR = 0x29
-- Port E
ATmega32U4.PORTE_BASE = 0x2C
ATmega32U4.PORTE_PORTE_ADDR = 0x2E
ATmega32U4.PORTE_DDRE_ADDR = 0x2D
ATmega32U4.PORTE_PINE_ADDR = 0x2C
-- USART1
ATmega32U4.UART1_BASE = 0xC8
ATmega32U4.UART1_UDR1_ADDR = 0xCE
ATmega32U4.UART1_UCSR1A_ADDR = 0xC8
ATmega32U4.UART1_UCSR1B_ADDR = 0xC9
ATmega32U4.UART1_UCSR1C_ADDR = 0xCA
ATmega32U4.UART1_UBRR1_ADDR = 0xCC
-- USB Controller
ATmega32U4.USB_BASE = 0xD0
ATmega32U4.USB_UDCON_ADDR = 0xD0
ATmega32U4.USB_UDIEN_ADDR = 0xD1
ATmega32U4.USB_UDINT_ADDR = 0xD2

-- 中断向量定义
ATmega32U4.INT_INT0 = 1  -- External Interrupt 0
ATmega32U4.INT_INT1 = 2  -- External Interrupt 1
ATmega32U4.INT_INT2 = 3  -- External Interrupt 2
ATmega32U4.INT_INT3 = 4  -- External Interrupt 3
ATmega32U4.INT_INT4 = 5  -- External Interrupt 4
ATmega32U4.INT_INT5 = 6  -- External Interrupt 5
ATmega32U4.INT_INT6 = 7  -- External Interrupt 6
ATmega32U4.INT_PCINT0 = 8  -- Pin Change Interrupt 0
ATmega32U4.INT_USB_GENERAL = 9  -- USB General
ATmega32U4.INT_USB_ENDPOINT = 10  -- USB Endpoint
ATmega32U4.INT_WDT = 11  -- Watchdog Timeout
ATmega32U4.INT_TIMER1_CAPT = 12  -- Timer1 Capture
ATmega32U4.INT_TIMER1_COMPA = 13  -- Timer1 Compare A
ATmega32U4.INT_TIMER1_COMPB = 14  -- Timer1 Compare B
ATmega32U4.INT_TIMER1_OVF = 15  -- Timer1 Overflow
ATmega32U4.INT_TIMER0_COMPA = 16  -- Timer0 Compare A
ATmega32U4.INT_TIMER0_COMPB = 17  -- Timer0 Compare B
ATmega32U4.INT_TIMER0_OVF = 18  -- Timer0 Overflow
ATmega32U4.INT_SPI_STC = 19  -- SPI Transfer Complete
ATmega32U4.INT_UART1_RX = 20  -- UART1 Receive
ATmega32U4.INT_UART1_UDRE = 21  -- UART1 Data Register Empty
ATmega32U4.INT_UART1_TX = 22  -- UART1 Transmit
ATmega32U4.INT_ADC = 23  -- ADC Conversion Complete

-- 设备类
function ATmega32U4.new(memory_base)
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
        self.registers["R3"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x04,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x05,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R6"] = {
            address = 0x06,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R7"] = {
            address = 0x07,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R8"] = {
            address = 0x08,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R9"] = {
            address = 0x09,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R10"] = {
            address = 0x0A,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R11"] = {
            address = 0x0B,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R12"] = {
            address = 0x0C,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R13"] = {
            address = 0x0D,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R14"] = {
            address = 0x0E,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R15"] = {
            address = 0x0F,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R16"] = {
            address = 0x10,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R17"] = {
            address = 0x11,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R18"] = {
            address = 0x12,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R19"] = {
            address = 0x13,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R20"] = {
            address = 0x14,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R21"] = {
            address = 0x15,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R22"] = {
            address = 0x16,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R23"] = {
            address = 0x17,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R24"] = {
            address = 0x18,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R25"] = {
            address = 0x19,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R26"] = {
            address = 0x1A,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R27"] = {
            address = 0x1B,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R28"] = {
            address = 0x1C,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R29"] = {
            address = 0x1D,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R30"] = {
            address = 0x1E,
            size = 1,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R31"] = {
            address = 0x1F,
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
        self.peripherals["PORTB"] = {
            base = 0x23,
            type = "GPIO",
            description = "Port B",
            registers = {}
        }
        
        local p = self.peripherals["PORTB"]
        p.registers["PORTB"] = {
            address = 0x25,
            size = 1,
            value = 0
        }
        p.registers["DDRB"] = {
            address = 0x24,
            size = 1,
            value = 0
        }
        p.registers["PINB"] = {
            address = 0x23,
            size = 1,
            value = 0
        }
        self.peripherals["PORTC"] = {
            base = 0x26,
            type = "GPIO",
            description = "Port C",
            registers = {}
        }
        
        local p = self.peripherals["PORTC"]
        p.registers["PORTC"] = {
            address = 0x28,
            size = 1,
            value = 0
        }
        p.registers["DDRC"] = {
            address = 0x27,
            size = 1,
            value = 0
        }
        p.registers["PINC"] = {
            address = 0x26,
            size = 1,
            value = 0
        }
        self.peripherals["PORTD"] = {
            base = 0x29,
            type = "GPIO",
            description = "Port D",
            registers = {}
        }
        
        local p = self.peripherals["PORTD"]
        p.registers["PORTD"] = {
            address = 0x2B,
            size = 1,
            value = 0
        }
        p.registers["DDRD"] = {
            address = 0x2A,
            size = 1,
            value = 0
        }
        p.registers["PIND"] = {
            address = 0x29,
            size = 1,
            value = 0
        }
        self.peripherals["PORTE"] = {
            base = 0x2C,
            type = "GPIO",
            description = "Port E",
            registers = {}
        }
        
        local p = self.peripherals["PORTE"]
        p.registers["PORTE"] = {
            address = 0x2E,
            size = 1,
            value = 0
        }
        p.registers["DDRE"] = {
            address = 0x2D,
            size = 1,
            value = 0
        }
        p.registers["PINE"] = {
            address = 0x2C,
            size = 1,
            value = 0
        }
        self.peripherals["UART1"] = {
            base = 0xC8,
            type = "UART",
            description = "USART1",
            registers = {}
        }
        
        local p = self.peripherals["UART1"]
        p.registers["UDR1"] = {
            address = 0xCE,
            size = 1,
            value = 0
        }
        p.registers["UCSR1A"] = {
            address = 0xC8,
            size = 1,
            value = 0
        }
        p.registers["UCSR1B"] = {
            address = 0xC9,
            size = 1,
            value = 0
        }
        p.registers["UCSR1C"] = {
            address = 0xCA,
            size = 1,
            value = 0
        }
        p.registers["UBRR1"] = {
            address = 0xCC,
            size = 2,
            value = 0
        }
        self.peripherals["USB"] = {
            base = 0xD0,
            type = "USB",
            description = "USB Controller",
            registers = {}
        }
        
        local p = self.peripherals["USB"]
        p.registers["UDCON"] = {
            address = 0xD0,
            size = 1,
            value = 0
        }
        p.registers["UDIEN"] = {
            address = 0xD1,
            size = 1,
            value = 0
        }
        p.registers["UDINT"] = {
            address = 0xD2,
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
            name = ATmega32U4.DEVICE_NAME,
            manufacturer = ATmega32U4.MANUFACTURER,
            family = ATmega32U4.FAMILY,
            version = ATmega32U4.VERSION,
            architecture = ATmega32U4.ARCHITECTURE,
            bits = ATmega32U4.BITS,
            clock_frequency = ATmega32U4.CLOCK_FREQUENCY
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
        return string.format("ATmega32U4(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ATmega32U4.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ATmega32U4.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ATmega32U4.print_device_info(device)
    device = device or ATmega32U4.new()
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

function ATmega32U4.print_registers(device)
    device = device or ATmega32U4.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ATmega32U4.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ATmega32U4.example()
    print("=== ATmega32U4设备示例 ===")
    
    -- 创建设备实例
    local device = ATmega32U4.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ATmega32U4.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. ATmega32U4.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. ATmega32U4.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ATmega32U4.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ATmega32U4.lua$") then
    ATmega32U4.example()
end

return ATmega32U4

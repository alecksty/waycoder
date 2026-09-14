--[[
  ATtiny13设备定义 - Lua模块
  生成自: Atmel/AVR/ATtiny13
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
  CPU架构: AVR
  位宽: 8位
  时钟频率: 20000000 Hz
]]

local ATtiny13 = {}

-- 设备信息
ATtiny13.DEVICE_NAME = "ATtiny13"
ATtiny13.MANUFACTURER = "Atmel"
ATtiny13.FAMILY = "AVR"
ATtiny13.VERSION = "1.0"
ATtiny13.ARCHITECTURE = "AVR"
ATtiny13.BITS = 8
ATtiny13.CLOCK_FREQUENCY = 20000000

-- 寄存器地址定义
ATtiny13.R0_ADDR = 0x00  -- 
ATtiny13.R1_ADDR = 0x01  -- 
ATtiny13.R2_ADDR = 0x02  -- 
ATtiny13.R16_ADDR = 0x10  -- 
ATtiny13.R17_ADDR = 0x11  -- 
ATtiny13.R26_ADDR = 0x1A  -- XL
ATtiny13.R27_ADDR = 0x1B  -- XH
ATtiny13.R28_ADDR = 0x1C  -- YL
ATtiny13.R29_ADDR = 0x1D  -- YH
ATtiny13.R30_ADDR = 0x1E  -- ZL
ATtiny13.R31_ADDR = 0x1F  -- ZH
ATtiny13.SPL_ADDR = 0x5D  -- Stack Pointer Low
ATtiny13.SPH_ADDR = 0x5E  -- Stack Pointer High
ATtiny13.SREG_ADDR = 0x5F  -- Status Register

-- 内存段定义
ATtiny13.FLASH_START = 0x0000
ATtiny13.FLASH_END = 0x03FF
ATtiny13.FLASH_SIZE = 1024  -- 
ATtiny13.SRAM_START = 0x0060
ATtiny13.SRAM_END = 0x009F
ATtiny13.SRAM_SIZE = 64  -- 
ATtiny13.EEPROM_START = 0x0000
ATtiny13.EEPROM_END = 0x003F
ATtiny13.EEPROM_SIZE = 64  -- 
ATtiny13.IO_START = 0x00
ATtiny13.IO_END = 0x1F
ATtiny13.IO_SIZE = 32  -- 
ATtiny13.EXTIO_START = 0x20
ATtiny13.EXTIO_END = 0x5F
ATtiny13.EXTIO_SIZE = 64  -- 

-- 外设定义
-- Port B (only port)
ATtiny13.PORTB_BASE = 0x18
ATtiny13.PORTB_DDRB_ADDR = 0x17
ATtiny13.PORTB_PORTB_ADDR = 0x18
ATtiny13.PORTB_PINB_ADDR = 0x19
-- 8-bit Timer/Counter0
ATtiny13.TIMER0_BASE = 0x33
ATtiny13.TIMER0_TCCR0A_ADDR = 0x33
ATtiny13.TIMER0_TCCR0B_ADDR = 0x33
ATtiny13.TIMER0_TCNT0_ADDR = 0x32
ATtiny13.TIMER0_OCR0A_ADDR = 0x36
ATtiny13.TIMER0_OCR0B_ADDR = 0x35
ATtiny13.TIMER0_TIMSK0_ADDR = 0x39
ATtiny13.TIMER0_TIFR0_ADDR = 0x38
-- Analog-to-Digital
ATtiny13.ADC_BASE = 0x04
ATtiny13.ADC_ADMUX_ADDR = 0x07
ATtiny13.ADC_ADCSRA_ADDR = 0x06
ATtiny13.ADC_ADCL_ADDR = 0x04
ATtiny13.ADC_ADCH_ADDR = 0x05

-- 中断向量定义
ATtiny13.INT_RESET = 1  -- 
ATtiny13.INT_INT0 = 2  -- External Interrupt 0
ATtiny13.INT_PCINT0 = 3  -- Pin Change Interrupt
ATtiny13.INT_TIM0_OVF = 4  -- Timer0 Overflow
ATtiny13.INT_TIM0_COMPA = 5  -- Timer0 Compare A
ATtiny13.INT_WDT = 6  -- Watchdog Timeout
ATtiny13.INT_ADC = 7  -- ADC Conversion Complete

-- 设备类
function ATtiny13.new(memory_base)
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
        self.registers["R26"] = {
            address = 0x1A,
            size = 1,
            access = "rw",
            description = "XL",
            value = 0
        }
        self.registers["R27"] = {
            address = 0x1B,
            size = 1,
            access = "rw",
            description = "XH",
            value = 0
        }
        self.registers["R28"] = {
            address = 0x1C,
            size = 1,
            access = "rw",
            description = "YL",
            value = 0
        }
        self.registers["R29"] = {
            address = 0x1D,
            size = 1,
            access = "rw",
            description = "YH",
            value = 0
        }
        self.registers["R30"] = {
            address = 0x1E,
            size = 1,
            access = "rw",
            description = "ZL",
            value = 0
        }
        self.registers["R31"] = {
            address = 0x1F,
            size = 1,
            access = "rw",
            description = "ZH",
            value = 0
        }
        self.registers["SPL"] = {
            address = 0x5D,
            size = 1,
            access = "rw",
            description = "Stack Pointer Low",
            value = 0
        }
        self.registers["SPH"] = {
            address = 0x5E,
            size = 1,
            access = "rw",
            description = "Stack Pointer High",
            value = 0
        }
        self.registers["SREG"] = {
            address = 0x5F,
            size = 1,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PORTB"] = {
            base = 0x18,
            type = "GPIO",
            description = "Port B (only port)",
            registers = {}
        }
        
        local p = self.peripherals["PORTB"]
        p.registers["DDRB"] = {
            address = 0x17,
            size = 1,
            value = 0
        }
        p.registers["PORTB"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        p.registers["PINB"] = {
            address = 0x19,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER0"] = {
            base = 0x33,
            type = "Timer",
            description = "8-bit Timer/Counter0",
            registers = {}
        }
        
        local p = self.peripherals["TIMER0"]
        p.registers["TCCR0A"] = {
            address = 0x33,
            size = 1,
            value = 0
        }
        p.registers["TCCR0B"] = {
            address = 0x33,
            size = 1,
            value = 0
        }
        p.registers["TCNT0"] = {
            address = 0x32,
            size = 1,
            value = 0
        }
        p.registers["OCR0A"] = {
            address = 0x36,
            size = 1,
            value = 0
        }
        p.registers["OCR0B"] = {
            address = 0x35,
            size = 1,
            value = 0
        }
        p.registers["TIMSK0"] = {
            address = 0x39,
            size = 1,
            value = 0
        }
        p.registers["TIFR0"] = {
            address = 0x38,
            size = 1,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = 0x04,
            type = "ADC",
            description = "Analog-to-Digital",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["ADMUX"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["ADCSRA"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["ADCL"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["ADCH"] = {
            address = 0x05,
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
            name = ATtiny13.DEVICE_NAME,
            manufacturer = ATtiny13.MANUFACTURER,
            family = ATtiny13.FAMILY,
            version = ATtiny13.VERSION,
            architecture = ATtiny13.ARCHITECTURE,
            bits = ATtiny13.BITS,
            clock_frequency = ATtiny13.CLOCK_FREQUENCY
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
        return string.format("ATtiny13(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ATtiny13.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ATtiny13.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ATtiny13.print_device_info(device)
    device = device or ATtiny13.new()
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

function ATtiny13.print_registers(device)
    device = device or ATtiny13.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ATtiny13.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ATtiny13.example()
    print("=== ATtiny13设备示例 ===")
    
    -- 创建设备实例
    local device = ATtiny13.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ATtiny13.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. ATtiny13.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. ATtiny13.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ATtiny13.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ATtiny13.lua$") then
    ATtiny13.example()
end

return ATtiny13

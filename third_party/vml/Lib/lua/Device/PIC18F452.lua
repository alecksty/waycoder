--[[
  PIC18F452设备定义 - Lua模块
  生成自: Microchip Technology/PIC18/PIC18F452
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: PIC18F452 8-bit microcontroller with 32KB Flash, 1.5KB RAM, 256B EEPROM
  CPU架构: PIC18
  位宽: 8位
  时钟频率: 20000000 Hz
]]

local PIC18F452 = {}

-- 设备信息
PIC18F452.DEVICE_NAME = "PIC18F452"
PIC18F452.MANUFACTURER = "Microchip Technology"
PIC18F452.FAMILY = "PIC18"
PIC18F452.VERSION = "1.0"
PIC18F452.ARCHITECTURE = "PIC18"
PIC18F452.BITS = 8
PIC18F452.CLOCK_FREQUENCY = 20000000

-- 寄存器地址定义
PIC18F452.WREG_ADDR = 0xFE8  -- Working Register
PIC18F452.STATUS_ADDR = 0xFD8  -- Status Register
PIC18F452.BSR_ADDR = 0xFE0  -- Bank Select Register
PIC18F452.PCL_ADDR = 0xFF9  -- Program Counter Low
PIC18F452.PCLATH_ADDR = 0xFFA  -- Program Counter Latch High
PIC18F452.PCLATU_ADDR = 0xFFB  -- Program Counter Latch Upper
PIC18F452.TOSU_ADDR = 0xFFF  -- Top of Stack Upper
PIC18F452.TOSH_ADDR = 0xFFE  -- Top of Stack High
PIC18F452.TOSL_ADDR = 0xFFD  -- Top of Stack Low

-- 外设定义
-- Port A
PIC18F452.PORTA_BASE = 
PIC18F452.PORTA_PORTA_ADDR = 0xF80
PIC18F452.PORTA_TRISA_ADDR = 0xF92
PIC18F452.PORTA_LATA_ADDR = 0xF89
-- Port B
PIC18F452.PORTB_BASE = 
PIC18F452.PORTB_PORTB_ADDR = 0xF81
PIC18F452.PORTB_TRISB_ADDR = 0xF93
PIC18F452.PORTB_LATB_ADDR = 0xF8A
-- Port C
PIC18F452.PORTC_BASE = 
PIC18F452.PORTC_PORTC_ADDR = 0xF82
PIC18F452.PORTC_TRISC_ADDR = 0xF94
PIC18F452.PORTC_LATC_ADDR = 0xF8B
-- Port D
PIC18F452.PORTD_BASE = 
PIC18F452.PORTD_PORTD_ADDR = 0xF83
PIC18F452.PORTD_TRISD_ADDR = 0xF95
PIC18F452.PORTD_LATD_ADDR = 0xF8C
-- Port E
PIC18F452.PORTE_BASE = 
PIC18F452.PORTE_PORTE_ADDR = 0xF84
PIC18F452.PORTE_TRISE_ADDR = 0xF96
PIC18F452.PORTE_LATE_ADDR = 0xF8D
-- Timer0
PIC18F452.TMR0_BASE = 
PIC18F452.TMR0_TMR0L_ADDR = 0xFD6
PIC18F452.TMR0_TMR0H_ADDR = 0xFD7
PIC18F452.TMR0_T0CON_ADDR = 0xFD5
-- Timer1
PIC18F452.TMR1_BASE = 
PIC18F452.TMR1_TMR1L_ADDR = 0xFCE
PIC18F452.TMR1_TMR1H_ADDR = 0xFCF
PIC18F452.TMR1_T1CON_ADDR = 0xFCD
-- Timer2
PIC18F452.TMR2_BASE = 
PIC18F452.TMR2_TMR2_ADDR = 0xFCC
PIC18F452.TMR2_PR2_ADDR = 0xFCB
PIC18F452.TMR2_T2CON_ADDR = 0xFCA
-- Timer3
PIC18F452.TMR3_BASE = 
PIC18F452.TMR3_TMR3L_ADDR = 0xFB2
PIC18F452.TMR3_TMR3H_ADDR = 0xFB3
PIC18F452.TMR3_T3CON_ADDR = 0xFB1
-- Analog-to-Digital Converter
PIC18F452.ADC_BASE = 
PIC18F452.ADC_ADRESL_ADDR = 0xFC3
PIC18F452.ADC_ADRESH_ADDR = 0xFC4
PIC18F452.ADC_ADCON0_ADDR = 0xFC2
PIC18F452.ADC_ADCON1_ADDR = 0xFC1
-- Universal Synchronous Asynchronous Receiver Transmitter
PIC18F452.USART_BASE = 
PIC18F452.USART_TXREG_ADDR = 0xFAC
PIC18F452.USART_RCREG_ADDR = 0xFAB
PIC18F452.USART_SPBRG_ADDR = 0xFAF
PIC18F452.USART_TXSTA_ADDR = 0xFAD
PIC18F452.USART_RCSTA_ADDR = 0xFAE
-- Synchronous Serial Port
PIC18F452.SSP_BASE = 
PIC18F452.SSP_SSPBUF_ADDR = 0xFC9
PIC18F452.SSP_SSPADD_ADDR = 0xFC8
PIC18F452.SSP_SSPSTAT_ADDR = 0xFC7
PIC18F452.SSP_SSPCON1_ADDR = 0xFC6
PIC18F452.SSP_SSPCON2_ADDR = 0xFC5
-- Capture/Compare/PWM 1
PIC18F452.CCP1_BASE = 
PIC18F452.CCP1_CCPR1L_ADDR = 0xFBE
PIC18F452.CCP1_CCPR1H_ADDR = 0xFBF
PIC18F452.CCP1_CCP1CON_ADDR = 0xFBD
-- Capture/Compare/PWM 2
PIC18F452.CCP2_BASE = 
PIC18F452.CCP2_CCPR2L_ADDR = 0xFBA
PIC18F452.CCP2_CCPR2H_ADDR = 0xFBB
PIC18F452.CCP2_CCP2CON_ADDR = 0xFB9

-- 中断向量定义
PIC18F452.INT_HIGH_PRIORITY = 8  -- High priority interrupt
PIC18F452.INT_LOW_PRIORITY = 24  -- Low priority interrupt
PIC18F452.INT_RESET = 0  -- Reset vector

-- 设备类
function PIC18F452.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["WREG"] = {
            address = 0xFE8,
            size = 1,
            access = "rw",
            description = "Working Register",
            value = 0
        }
        self.registers["STATUS"] = {
            address = 0xFD8,
            size = 1,
            access = "rw",
            description = "Status Register",
            value = 0
        }
        self.registers["BSR"] = {
            address = 0xFE0,
            size = 1,
            access = "rw",
            description = "Bank Select Register",
            value = 0
        }
        self.registers["PCL"] = {
            address = 0xFF9,
            size = 1,
            access = "rw",
            description = "Program Counter Low",
            value = 0
        }
        self.registers["PCLATH"] = {
            address = 0xFFA,
            size = 1,
            access = "rw",
            description = "Program Counter Latch High",
            value = 0
        }
        self.registers["PCLATU"] = {
            address = 0xFFB,
            size = 1,
            access = "rw",
            description = "Program Counter Latch Upper",
            value = 0
        }
        self.registers["TOSU"] = {
            address = 0xFFF,
            size = 1,
            access = "rw",
            description = "Top of Stack Upper",
            value = 0
        }
        self.registers["TOSH"] = {
            address = 0xFFE,
            size = 1,
            access = "rw",
            description = "Top of Stack High",
            value = 0
        }
        self.registers["TOSL"] = {
            address = 0xFFD,
            size = 1,
            access = "rw",
            description = "Top of Stack Low",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PORTA"] = {
            base = ,
            type = "GPIO",
            description = "Port A",
            registers = {}
        }
        
        local p = self.peripherals["PORTA"]
        p.registers["PORTA"] = {
            address = 0xF80,
            size = 1,
            value = 0
        }
        p.registers["TRISA"] = {
            address = 0xF92,
            size = 1,
            value = 0
        }
        p.registers["LATA"] = {
            address = 0xF89,
            size = 1,
            value = 0
        }
        self.peripherals["PORTB"] = {
            base = ,
            type = "GPIO",
            description = "Port B",
            registers = {}
        }
        
        local p = self.peripherals["PORTB"]
        p.registers["PORTB"] = {
            address = 0xF81,
            size = 1,
            value = 0
        }
        p.registers["TRISB"] = {
            address = 0xF93,
            size = 1,
            value = 0
        }
        p.registers["LATB"] = {
            address = 0xF8A,
            size = 1,
            value = 0
        }
        self.peripherals["PORTC"] = {
            base = ,
            type = "GPIO",
            description = "Port C",
            registers = {}
        }
        
        local p = self.peripherals["PORTC"]
        p.registers["PORTC"] = {
            address = 0xF82,
            size = 1,
            value = 0
        }
        p.registers["TRISC"] = {
            address = 0xF94,
            size = 1,
            value = 0
        }
        p.registers["LATC"] = {
            address = 0xF8B,
            size = 1,
            value = 0
        }
        self.peripherals["PORTD"] = {
            base = ,
            type = "GPIO",
            description = "Port D",
            registers = {}
        }
        
        local p = self.peripherals["PORTD"]
        p.registers["PORTD"] = {
            address = 0xF83,
            size = 1,
            value = 0
        }
        p.registers["TRISD"] = {
            address = 0xF95,
            size = 1,
            value = 0
        }
        p.registers["LATD"] = {
            address = 0xF8C,
            size = 1,
            value = 0
        }
        self.peripherals["PORTE"] = {
            base = ,
            type = "GPIO",
            description = "Port E",
            registers = {}
        }
        
        local p = self.peripherals["PORTE"]
        p.registers["PORTE"] = {
            address = 0xF84,
            size = 1,
            value = 0
        }
        p.registers["TRISE"] = {
            address = 0xF96,
            size = 1,
            value = 0
        }
        p.registers["LATE"] = {
            address = 0xF8D,
            size = 1,
            value = 0
        }
        self.peripherals["TMR0"] = {
            base = ,
            type = "Timer",
            description = "Timer0",
            registers = {}
        }
        
        local p = self.peripherals["TMR0"]
        p.registers["TMR0L"] = {
            address = 0xFD6,
            size = 1,
            value = 0
        }
        p.registers["TMR0H"] = {
            address = 0xFD7,
            size = 1,
            value = 0
        }
        p.registers["T0CON"] = {
            address = 0xFD5,
            size = 1,
            value = 0
        }
        self.peripherals["TMR1"] = {
            base = ,
            type = "Timer",
            description = "Timer1",
            registers = {}
        }
        
        local p = self.peripherals["TMR1"]
        p.registers["TMR1L"] = {
            address = 0xFCE,
            size = 1,
            value = 0
        }
        p.registers["TMR1H"] = {
            address = 0xFCF,
            size = 1,
            value = 0
        }
        p.registers["T1CON"] = {
            address = 0xFCD,
            size = 1,
            value = 0
        }
        self.peripherals["TMR2"] = {
            base = ,
            type = "Timer",
            description = "Timer2",
            registers = {}
        }
        
        local p = self.peripherals["TMR2"]
        p.registers["TMR2"] = {
            address = 0xFCC,
            size = 1,
            value = 0
        }
        p.registers["PR2"] = {
            address = 0xFCB,
            size = 1,
            value = 0
        }
        p.registers["T2CON"] = {
            address = 0xFCA,
            size = 1,
            value = 0
        }
        self.peripherals["TMR3"] = {
            base = ,
            type = "Timer",
            description = "Timer3",
            registers = {}
        }
        
        local p = self.peripherals["TMR3"]
        p.registers["TMR3L"] = {
            address = 0xFB2,
            size = 1,
            value = 0
        }
        p.registers["TMR3H"] = {
            address = 0xFB3,
            size = 1,
            value = 0
        }
        p.registers["T3CON"] = {
            address = 0xFB1,
            size = 1,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = ,
            type = "ADC",
            description = "Analog-to-Digital Converter",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["ADRESL"] = {
            address = 0xFC3,
            size = 1,
            value = 0
        }
        p.registers["ADRESH"] = {
            address = 0xFC4,
            size = 1,
            value = 0
        }
        p.registers["ADCON0"] = {
            address = 0xFC2,
            size = 1,
            value = 0
        }
        p.registers["ADCON1"] = {
            address = 0xFC1,
            size = 1,
            value = 0
        }
        self.peripherals["USART"] = {
            base = ,
            type = "Serial",
            description = "Universal Synchronous Asynchronous Receiver Transmitter",
            registers = {}
        }
        
        local p = self.peripherals["USART"]
        p.registers["TXREG"] = {
            address = 0xFAC,
            size = 1,
            value = 0
        }
        p.registers["RCREG"] = {
            address = 0xFAB,
            size = 1,
            value = 0
        }
        p.registers["SPBRG"] = {
            address = 0xFAF,
            size = 1,
            value = 0
        }
        p.registers["TXSTA"] = {
            address = 0xFAD,
            size = 1,
            value = 0
        }
        p.registers["RCSTA"] = {
            address = 0xFAE,
            size = 1,
            value = 0
        }
        self.peripherals["SSP"] = {
            base = ,
            type = "SPI",
            description = "Synchronous Serial Port",
            registers = {}
        }
        
        local p = self.peripherals["SSP"]
        p.registers["SSPBUF"] = {
            address = 0xFC9,
            size = 1,
            value = 0
        }
        p.registers["SSPADD"] = {
            address = 0xFC8,
            size = 1,
            value = 0
        }
        p.registers["SSPSTAT"] = {
            address = 0xFC7,
            size = 1,
            value = 0
        }
        p.registers["SSPCON1"] = {
            address = 0xFC6,
            size = 1,
            value = 0
        }
        p.registers["SSPCON2"] = {
            address = 0xFC5,
            size = 1,
            value = 0
        }
        self.peripherals["CCP1"] = {
            base = ,
            type = "PWM",
            description = "Capture/Compare/PWM 1",
            registers = {}
        }
        
        local p = self.peripherals["CCP1"]
        p.registers["CCPR1L"] = {
            address = 0xFBE,
            size = 1,
            value = 0
        }
        p.registers["CCPR1H"] = {
            address = 0xFBF,
            size = 1,
            value = 0
        }
        p.registers["CCP1CON"] = {
            address = 0xFBD,
            size = 1,
            value = 0
        }
        self.peripherals["CCP2"] = {
            base = ,
            type = "PWM",
            description = "Capture/Compare/PWM 2",
            registers = {}
        }
        
        local p = self.peripherals["CCP2"]
        p.registers["CCPR2L"] = {
            address = 0xFBA,
            size = 1,
            value = 0
        }
        p.registers["CCPR2H"] = {
            address = 0xFBB,
            size = 1,
            value = 0
        }
        p.registers["CCP2CON"] = {
            address = 0xFB9,
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
            name = PIC18F452.DEVICE_NAME,
            manufacturer = PIC18F452.MANUFACTURER,
            family = PIC18F452.FAMILY,
            version = PIC18F452.VERSION,
            architecture = PIC18F452.ARCHITECTURE,
            bits = PIC18F452.BITS,
            clock_frequency = PIC18F452.CLOCK_FREQUENCY
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
        return string.format("PIC18F452(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function PIC18F452.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function PIC18F452.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function PIC18F452.print_device_info(device)
    device = device or PIC18F452.new()
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

function PIC18F452.print_registers(device)
    device = device or PIC18F452.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            PIC18F452.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function PIC18F452.example()
    print("=== PIC18F452设备示例 ===")
    
    -- 创建设备实例
    local device = PIC18F452.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    PIC18F452.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["WREG"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("WREG", 0x55)
        print("写入 WREG: " .. PIC18F452.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("WREG")
        print("读取 WREG: " .. PIC18F452.hex(value))
        
        -- 位操作
        device:set_bit("WREG", 0, true)
        local bit0 = device:get_bit("WREG", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    PIC18F452.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("PIC18F452.lua$") then
    PIC18F452.example()
end

return PIC18F452

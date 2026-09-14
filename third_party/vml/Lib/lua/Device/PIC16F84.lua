--[[
  PIC16F84设备定义 - Lua模块
  生成自: Microchip Technology/PIC16/PIC16F84
  版本: 
  日期: 
  作者: 
  描述: Microchip PIC16F84 8-bit microcontroller with EEPROM
  CPU架构: PIC16
  位宽: 0位
  时钟频率: 0 Hz
]]

local PIC16F84 = {}

-- 设备信息
PIC16F84.DEVICE_NAME = "PIC16F84"
PIC16F84.MANUFACTURER = "Microchip Technology"
PIC16F84.FAMILY = "PIC16"
PIC16F84.VERSION = ""
PIC16F84.ARCHITECTURE = "PIC16"
PIC16F84.BITS = 0
PIC16F84.CLOCK_FREQUENCY = 0

-- 外设定义
-- 8-bit timer/counter with prescaler
PIC16F84.TIMER0_BASE = 
PIC16F84.TIMER0_TMR0_ADDR = 0x01
-- 16-bit timer/counter with prescaler
PIC16F84.TIMER1_BASE = 
PIC16F84.TIMER1_TMR1L_ADDR = 0x0E
PIC16F84.TIMER1_TMR1H_ADDR = 0x0F
PIC16F84.TIMER1_T1CON_ADDR = 0x10
PIC16F84.TIMER1_T1CON_TMR1ON_BIT = 0  -- Timer1 On
PIC16F84.TIMER1_T1CON_TMR1CS_BIT = 1  -- Timer1 Clock Source
PIC16F84.TIMER1_T1CON_T1SYNC_BIT = 2  -- Timer1 External Clock Input Synchronization
PIC16F84.TIMER1_T1CON_T1OSCEN_BIT = 3  -- Timer1 Oscillator Enable
PIC16F84.TIMER1_T1CON_T1CKPS0_BIT = 4  -- Timer1 Input Clock Prescale Select bit 0
PIC16F84.TIMER1_T1CON_T1CKPS1_BIT = 5  -- Timer1 Input Clock Prescale Select bit 1
-- Watchdog Timer
PIC16F84.WATCHDOG_BASE = 
PIC16F84.WATCHDOG_WDTCON_ADDR = 0x07
PIC16F84.WATCHDOG_WDTCON_SWDTEN_BIT = 0  -- Software Watchdog Timer Enable
-- 64-byte EEPROM data memory
PIC16F84.EEPROM_BASE = 
PIC16F84.EEPROM_EEDATA_ADDR = 0x08
PIC16F84.EEPROM_EEADR_ADDR = 0x09
PIC16F84.EEPROM_EECON1_ADDR = 0x88
PIC16F84.EEPROM_EECON2_ADDR = 0x89
-- General Purpose I/O
PIC16F84.GPIO_BASE = 
PIC16F84.GPIO_PORTA_ADDR = 0x05
PIC16F84.GPIO_PORTB_ADDR = 0x06
PIC16F84.GPIO_TRISA_ADDR = 0x85
PIC16F84.GPIO_TRISB_ADDR = 0x86

-- 中断向量定义
PIC16F84.INT_INT = 4  -- External interrupt on RB0/INT pin
PIC16F84.INT_TMR0 = 4  -- Timer0 overflow interrupt
PIC16F84.INT_PORTB = 4  -- PORTB change interrupt (RB4-RB7)
PIC16F84.INT_EEPROM = 4  -- EEPROM write complete interrupt

-- 设备类
function PIC16F84.new(memory_base)
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
        self.peripherals["Timer0"] = {
            base = ,
            type = "Timer",
            description = "8-bit timer/counter with prescaler",
            registers = {}
        }
        
        local p = self.peripherals["Timer0"]
        p.registers["TMR0"] = {
            address = 0x01,
            size = 8,
            value = 0
        }
        self.peripherals["Timer1"] = {
            base = ,
            type = "Timer",
            description = "16-bit timer/counter with prescaler",
            registers = {}
        }
        
        local p = self.peripherals["Timer1"]
        p.registers["TMR1L"] = {
            address = 0x0E,
            size = 8,
            value = 0
        }
        p.registers["TMR1H"] = {
            address = 0x0F,
            size = 8,
            value = 0
        }
        p.registers["T1CON"] = {
            address = 0x10,
            size = 8,
            value = 0
        }
        self.peripherals["Watchdog"] = {
            base = ,
            type = "Watchdog",
            description = "Watchdog Timer",
            registers = {}
        }
        
        local p = self.peripherals["Watchdog"]
        p.registers["WDTCON"] = {
            address = 0x07,
            size = 8,
            value = 0
        }
        self.peripherals["EEPROM"] = {
            base = ,
            type = "EEPROM",
            description = "64-byte EEPROM data memory",
            registers = {}
        }
        
        local p = self.peripherals["EEPROM"]
        p.registers["EEDATA"] = {
            address = 0x08,
            size = 8,
            value = 0
        }
        p.registers["EEADR"] = {
            address = 0x09,
            size = 8,
            value = 0
        }
        p.registers["EECON1"] = {
            address = 0x88,
            size = 8,
            value = 0
        }
        p.registers["EECON2"] = {
            address = 0x89,
            size = 8,
            value = 0
        }
        self.peripherals["GPIO"] = {
            base = ,
            type = "GPIO",
            description = "General Purpose I/O",
            registers = {}
        }
        
        local p = self.peripherals["GPIO"]
        p.registers["PORTA"] = {
            address = 0x05,
            size = 8,
            value = 0
        }
        p.registers["PORTB"] = {
            address = 0x06,
            size = 8,
            value = 0
        }
        p.registers["TRISA"] = {
            address = 0x85,
            size = 8,
            value = 0
        }
        p.registers["TRISB"] = {
            address = 0x86,
            size = 8,
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
            name = PIC16F84.DEVICE_NAME,
            manufacturer = PIC16F84.MANUFACTURER,
            family = PIC16F84.FAMILY,
            version = PIC16F84.VERSION,
            architecture = PIC16F84.ARCHITECTURE,
            bits = PIC16F84.BITS,
            clock_frequency = PIC16F84.CLOCK_FREQUENCY
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
        return string.format("PIC16F84(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function PIC16F84.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function PIC16F84.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function PIC16F84.print_device_info(device)
    device = device or PIC16F84.new()
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

function PIC16F84.print_registers(device)
    device = device or PIC16F84.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            PIC16F84.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function PIC16F84.example()
    print("=== PIC16F84设备示例 ===")
    
    -- 创建设备实例
    local device = PIC16F84.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    PIC16F84.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    PIC16F84.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("PIC16F84.lua$") then
    PIC16F84.example()
end

return PIC16F84

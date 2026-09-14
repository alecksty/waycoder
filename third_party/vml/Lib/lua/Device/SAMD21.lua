--[[
  SAMD21设备定义 - Lua模块
  生成自: Atmel (Microchip)/SAM D/SAMD21
  版本: 
  日期: 
  作者: 
  描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
  CPU架构: ARM Cortex-M0+
  位宽: 0位
  时钟频率: 0 Hz
]]

local SAMD21 = {}

-- 设备信息
SAMD21.DEVICE_NAME = "SAMD21"
SAMD21.MANUFACTURER = "Atmel (Microchip)"
SAMD21.FAMILY = "SAM D"
SAMD21.VERSION = ""
SAMD21.ARCHITECTURE = "ARM Cortex-M0+"
SAMD21.BITS = 0
SAMD21.CLOCK_FREQUENCY = 0

-- 外设定义
-- Power Manager
SAMD21.PM_BASE = 
SAMD21.PM_PM_CTRL_ADDR = 0x40000400
-- System Controller
SAMD21.SYSCTRL_BASE = 
SAMD21.SYSCTRL_SYSCTRL_INTENCLR_ADDR = 0x40000800
-- Generic Clock Generator
SAMD21.GCLK_BASE = 
SAMD21.GCLK_GCLK_CTRL_ADDR = 0x40000C00
-- Watchdog Timer
SAMD21.WDT_BASE = 
SAMD21.WDT_WDT_CTRL_ADDR = 0x40001000
-- Real-Time Clock
SAMD21.RTC_BASE = 
SAMD21.RTC_RTC_CTRL_ADDR = 0x40001400
-- External Interrupt Controller
SAMD21.EIC_BASE = 
SAMD21.EIC_EIC_CTRL_ADDR = 0x40001800
-- Serial Communication Interface 0
SAMD21.SERCOM0_BASE = 
SAMD21.SERCOM0_SERCOM0_I2CM_CTRLA_ADDR = 0x42000800
-- Analog-to-Digital Converter
SAMD21.ADC_BASE = 
SAMD21.ADC_ADC_CTRLA_ADDR = 0x42002000
-- Digital-to-Analog Converter
SAMD21.DAC_BASE = 
SAMD21.DAC_DAC_CTRLA_ADDR = 0x42002400
-- General Purpose I/O
SAMD21.PORT_BASE = 
SAMD21.PORT_PORT_DIR_ADDR = 0x41004400
-- Timer/Counter 0
SAMD21.TC0_BASE = 
SAMD21.TC0_TC0_CTRLA_ADDR = 0x42002800
-- USB Device Controller
SAMD21.USB_BASE = 
SAMD21.USB_USB_CTRLA_ADDR = 0x41005000

-- 中断向量定义
SAMD21.INT_RESET = 0  -- Reset vector
SAMD21.INT_NONMASKABLEINT = 1  -- Non-maskable interrupt
SAMD21.INT_HARDFAULT = 2  -- Hard fault
SAMD21.INT_SVCALL = 3  -- Supervisor call
SAMD21.INT_PENDSV = 4  -- Pendable service call
SAMD21.INT_SYSTICK = 5  -- System tick timer
SAMD21.INT_PM = 6  -- Power Manager
SAMD21.INT_SYSCTRL = 7  -- System Controller
SAMD21.INT_WDT = 8  -- Watchdog Timer
SAMD21.INT_RTC = 9  -- Real-Time Clock
SAMD21.INT_EIC = 10  -- External Interrupt Controller
SAMD21.INT_NVMCTRL = 11  -- Non-Volatile Memory Controller
SAMD21.INT_DMAC = 12  -- Direct Memory Access Controller
SAMD21.INT_USB = 13  -- USB Device Controller
SAMD21.INT_EVSYS = 14  -- Event System
SAMD21.INT_SERCOM0 = 15  -- Serial Communication Interface 0
SAMD21.INT_SERCOM1 = 16  -- Serial Communication Interface 1
SAMD21.INT_SERCOM2 = 17  -- Serial Communication Interface 2
SAMD21.INT_SERCOM3 = 18  -- Serial Communication Interface 3
SAMD21.INT_SERCOM4 = 19  -- Serial Communication Interface 4
SAMD21.INT_SERCOM5 = 20  -- Serial Communication Interface 5
SAMD21.INT_TCC0 = 21  -- Timer/Counter for Control 0
SAMD21.INT_TCC1 = 22  -- Timer/Counter for Control 1
SAMD21.INT_TCC2 = 23  -- Timer/Counter for Control 2
SAMD21.INT_TC3 = 24  -- Timer/Counter 3
SAMD21.INT_TC4 = 25  -- Timer/Counter 4
SAMD21.INT_TC5 = 26  -- Timer/Counter 5
SAMD21.INT_TC6 = 27  -- Timer/Counter 6
SAMD21.INT_TC7 = 28  -- Timer/Counter 7
SAMD21.INT_ADC = 29  -- Analog-to-Digital Converter
SAMD21.INT_AC = 30  -- Analog Comparator
SAMD21.INT_DAC = 31  -- Digital-to-Analog Converter
SAMD21.INT_PTC = 32  -- Peripheral Touch Controller
SAMD21.INT_I2S = 33  -- Inter-IC Sound Interface

-- 设备类
function SAMD21.new(memory_base)
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
        self.peripherals["PM"] = {
            base = ,
            type = "PowerManager",
            description = "Power Manager",
            registers = {}
        }
        
        local p = self.peripherals["PM"]
        p.registers["PM_CTRL"] = {
            address = 0x40000400,
            size = 8,
            value = 0
        }
        self.peripherals["SYSCTRL"] = {
            base = ,
            type = "SystemController",
            description = "System Controller",
            registers = {}
        }
        
        local p = self.peripherals["SYSCTRL"]
        p.registers["SYSCTRL_INTENCLR"] = {
            address = 0x40000800,
            size = 32,
            value = 0
        }
        self.peripherals["GCLK"] = {
            base = ,
            type = "ClockGenerator",
            description = "Generic Clock Generator",
            registers = {}
        }
        
        local p = self.peripherals["GCLK"]
        p.registers["GCLK_CTRL"] = {
            address = 0x40000C00,
            size = 8,
            value = 0
        }
        self.peripherals["WDT"] = {
            base = ,
            type = "Watchdog",
            description = "Watchdog Timer",
            registers = {}
        }
        
        local p = self.peripherals["WDT"]
        p.registers["WDT_CTRL"] = {
            address = 0x40001000,
            size = 8,
            value = 0
        }
        self.peripherals["RTC"] = {
            base = ,
            type = "RealTimeClock",
            description = "Real-Time Clock",
            registers = {}
        }
        
        local p = self.peripherals["RTC"]
        p.registers["RTC_CTRL"] = {
            address = 0x40001400,
            size = 16,
            value = 0
        }
        self.peripherals["EIC"] = {
            base = ,
            type = "ExternalInterrupt",
            description = "External Interrupt Controller",
            registers = {}
        }
        
        local p = self.peripherals["EIC"]
        p.registers["EIC_CTRL"] = {
            address = 0x40001800,
            size = 8,
            value = 0
        }
        self.peripherals["SERCOM0"] = {
            base = ,
            type = "SerialCommunication",
            description = "Serial Communication Interface 0",
            registers = {}
        }
        
        local p = self.peripherals["SERCOM0"]
        p.registers["SERCOM0_I2CM_CTRLA"] = {
            address = 0x42000800,
            size = 32,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = ,
            type = "AnalogDigitalConverter",
            description = "Analog-to-Digital Converter",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["ADC_CTRLA"] = {
            address = 0x42002000,
            size = 8,
            value = 0
        }
        self.peripherals["DAC"] = {
            base = ,
            type = "DigitalAnalogConverter",
            description = "Digital-to-Analog Converter",
            registers = {}
        }
        
        local p = self.peripherals["DAC"]
        p.registers["DAC_CTRLA"] = {
            address = 0x42002400,
            size = 8,
            value = 0
        }
        self.peripherals["PORT"] = {
            base = ,
            type = "GPIO",
            description = "General Purpose I/O",
            registers = {}
        }
        
        local p = self.peripherals["PORT"]
        p.registers["PORT_DIR"] = {
            address = 0x41004400,
            size = 32,
            value = 0
        }
        self.peripherals["TC0"] = {
            base = ,
            type = "TimerCounter",
            description = "Timer/Counter 0",
            registers = {}
        }
        
        local p = self.peripherals["TC0"]
        p.registers["TC0_CTRLA"] = {
            address = 0x42002800,
            size = 16,
            value = 0
        }
        self.peripherals["USB"] = {
            base = ,
            type = "UniversalSerialBus",
            description = "USB Device Controller",
            registers = {}
        }
        
        local p = self.peripherals["USB"]
        p.registers["USB_CTRLA"] = {
            address = 0x41005000,
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
            name = SAMD21.DEVICE_NAME,
            manufacturer = SAMD21.MANUFACTURER,
            family = SAMD21.FAMILY,
            version = SAMD21.VERSION,
            architecture = SAMD21.ARCHITECTURE,
            bits = SAMD21.BITS,
            clock_frequency = SAMD21.CLOCK_FREQUENCY
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
        return string.format("SAMD21(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function SAMD21.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function SAMD21.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function SAMD21.print_device_info(device)
    device = device or SAMD21.new()
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

function SAMD21.print_registers(device)
    device = device or SAMD21.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            SAMD21.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function SAMD21.example()
    print("=== SAMD21设备示例 ===")
    
    -- 创建设备实例
    local device = SAMD21.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    SAMD21.print_device_info(device)
    
    -- 演示寄存器操作
    
    -- 显示寄存器状态
    SAMD21.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("SAMD21.lua$") then
    SAMD21.example()
end

return SAMD21

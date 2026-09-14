--[[
  8051设备定义 - Lua模块
  生成自: Intel/MCS-51/8051
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: 8-bit microcontroller with 4KB ROM, 128B RAM, 32 I/O lines
  CPU架构: MCS-51
  位宽: 8位
  时钟频率: 11059200 Hz
]]

local 8051 = {}

-- 设备信息
8051.DEVICE_NAME = "8051"
8051.MANUFACTURER = "Intel"
8051.FAMILY = "MCS-51"
8051.VERSION = "1.0"
8051.ARCHITECTURE = "MCS-51"
8051.BITS = 8
8051.CLOCK_FREQUENCY = 11059200

-- 寄存器地址定义
8051.ACC_ADDR = 0xE0  -- Accumulator
8051.B_ADDR = 0xF0  -- B Register
8051.PSW_ADDR = 0xD0  -- Program Status Word
8051.PSW_P_BIT = 0  -- Parity Flag
8051.PSW_OV_BIT = 2  -- Overflow Flag
8051.PSW_RS0_BIT = 3  -- Register Bank Select 0
8051.PSW_RS1_BIT = 4  -- Register Bank Select 1
8051.PSW_F0_BIT = 5  -- Flag 0
8051.PSW_AC_BIT = 6  -- Auxiliary Carry Flag
8051.PSW_CY_BIT = 7  -- Carry Flag
8051.SP_ADDR = 0x81  -- Stack Pointer
8051.DPTR_ADDR = 0x82  -- Data Pointer (DPL/DPH)

-- 内存段定义
8051.CODE_START = 0x0000
8051.CODE_END = 0x0FFF
8051.CODE_SIZE = 4096  -- Program Memory
8051.IDATA_START = 0x00
8051.IDATA_END = 0x7F
8051.IDATA_SIZE = 128  -- Internal Data Memory
8051.SFR_START = 0x80
8051.SFR_END = 0xFF
8051.SFR_SIZE = 128  -- Special Function Registers
8051.XDATA_START = 0x0000
8051.XDATA_END = 0xFFFF
8051.XDATA_SIZE = 65536  -- External Data Memory

-- 外设定义
-- Port 0
8051.PORT0_BASE = 0x80
8051.PORT0_P0_ADDR = 0x80
-- Port 1
8051.PORT1_BASE = 0x90
8051.PORT1_P1_ADDR = 0x90
-- Port 2
8051.PORT2_BASE = 0xA0
8051.PORT2_P2_ADDR = 0xA0
-- Port 3
8051.PORT3_BASE = 0xB0
8051.PORT3_P3_ADDR = 0xB0
-- Timer/Counter 0
8051.TIMER0_BASE = 0x8A
8051.TIMER0_TH0_ADDR = 0x8C
8051.TIMER0_TL0_ADDR = 0x8A
8051.TIMER0_TMOD_ADDR = 0x89
8051.TIMER0_TMOD_M0_0_BIT = 0  -- Timer 0 Mode bit 0
8051.TIMER0_TMOD_M1_0_BIT = 1  -- Timer 0 Mode bit 1
8051.TIMER0_TMOD_C_T0_BIT = 2  -- Timer 0 Counter/Timer Select
8051.TIMER0_TMOD_GATE0_BIT = 3  -- Timer 0 Gate Control
8051.TIMER0_TCON_ADDR = 0x88
8051.TIMER0_TCON_TR0_BIT = 4  -- Timer 0 Run Control
8051.TIMER0_TCON_TF0_BIT = 5  -- Timer 0 Overflow Flag
-- Serial Port
8051.UART_BASE = 0x98
8051.UART_SBUF_ADDR = 0x99
8051.UART_SCON_ADDR = 0x98
8051.UART_SCON_RI_BIT = 0  -- Receive Interrupt Flag
8051.UART_SCON_TI_BIT = 1  -- Transmit Interrupt Flag
8051.UART_SCON_REN_BIT = 4  -- Receive Enable
8051.UART_SCON_SM0_BIT = 6  -- Serial Mode bit 0
8051.UART_SCON_SM1_BIT = 7  -- Serial Mode bit 1

-- 中断向量定义
8051.INT_RESET = 0  -- Reset Vector
8051.INT_INT0 = 1  -- External Interrupt 0
8051.INT_TIMER0 = 2  -- Timer 0 Interrupt
8051.INT_INT1 = 3  -- External Interrupt 1
8051.INT_TIMER1 = 4  -- Timer 1 Interrupt
8051.INT_UART = 5  -- Serial Port Interrupt

-- 引脚定义
8051.PIN_P1_0 = 1  -- Port 1, bit 0
8051.PIN_P1_1 = 2  -- Port 1, bit 1
8051.PIN_P1_2 = 3  -- Port 1, bit 2
8051.PIN_P1_3 = 4  -- Port 1, bit 3
8051.PIN_P1_4 = 5  -- Port 1, bit 4
8051.PIN_P1_5 = 6  -- Port 1, bit 5
8051.PIN_P1_6 = 7  -- Port 1, bit 6
8051.PIN_P1_7 = 8  -- Port 1, bit 7
8051.PIN_RST = 9  -- Reset Pin
8051.PIN_RX = 10  -- Serial Receive (P3.0)
8051.PIN_TX = 11  -- Serial Transmit (P3.1)
8051.PIN_INT0 = 12  -- External Interrupt 0 (P3.2)
8051.PIN_INT1 = 13  -- External Interrupt 1 (P3.3)
8051.PIN_T0 = 14  -- Timer 0 Input (P3.4)
8051.PIN_T1 = 15  -- Timer 1 Input (P3.5)
8051.PIN_WR = 16  -- External Memory Write Strobe (P3.6)
8051.PIN_RD = 17  -- External Memory Read Strobe (P3.7)
8051.PIN_XTAL1 = 18  -- Crystal Oscillator Input
8051.PIN_XTAL2 = 19  -- Crystal Oscillator Output
8051.PIN_VCC = 20  -- Power Supply (+5V)
8051.PIN_GND = 21  -- Ground

-- 设备类
function 8051.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["ACC"] = {
            address = 0xE0,
            size = 1,
            access = "rw",
            description = "Accumulator",
            value = 0
        }
        self.registers["B"] = {
            address = 0xF0,
            size = 1,
            access = "rw",
            description = "B Register",
            value = 0
        }
        self.registers["PSW"] = {
            address = 0xD0,
            size = 1,
            access = "rw",
            description = "Program Status Word",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x81,
            size = 1,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["DPTR"] = {
            address = 0x82,
            size = 2,
            access = "rw",
            description = "Data Pointer (DPL/DPH)",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PORT0"] = {
            base = 0x80,
            type = "GPIO",
            description = "Port 0",
            registers = {}
        }
        
        local p = self.peripherals["PORT0"]
        p.registers["P0"] = {
            address = 0x80,
            size = 1,
            value = 0
        }
        self.peripherals["PORT1"] = {
            base = 0x90,
            type = "GPIO",
            description = "Port 1",
            registers = {}
        }
        
        local p = self.peripherals["PORT1"]
        p.registers["P1"] = {
            address = 0x90,
            size = 1,
            value = 0
        }
        self.peripherals["PORT2"] = {
            base = 0xA0,
            type = "GPIO",
            description = "Port 2",
            registers = {}
        }
        
        local p = self.peripherals["PORT2"]
        p.registers["P2"] = {
            address = 0xA0,
            size = 1,
            value = 0
        }
        self.peripherals["PORT3"] = {
            base = 0xB0,
            type = "GPIO",
            description = "Port 3",
            registers = {}
        }
        
        local p = self.peripherals["PORT3"]
        p.registers["P3"] = {
            address = 0xB0,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER0"] = {
            base = 0x8A,
            type = "Timer",
            description = "Timer/Counter 0",
            registers = {}
        }
        
        local p = self.peripherals["TIMER0"]
        p.registers["TH0"] = {
            address = 0x8C,
            size = 1,
            value = 0
        }
        p.registers["TL0"] = {
            address = 0x8A,
            size = 1,
            value = 0
        }
        p.registers["TMOD"] = {
            address = 0x89,
            size = 1,
            value = 0
        }
        p.registers["TCON"] = {
            address = 0x88,
            size = 1,
            value = 0
        }
        self.peripherals["UART"] = {
            base = 0x98,
            type = "UART",
            description = "Serial Port",
            registers = {}
        }
        
        local p = self.peripherals["UART"]
        p.registers["SBUF"] = {
            address = 0x99,
            size = 1,
            value = 0
        }
        p.registers["SCON"] = {
            address = 0x98,
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
            name = 8051.DEVICE_NAME,
            manufacturer = 8051.MANUFACTURER,
            family = 8051.FAMILY,
            version = 8051.VERSION,
            architecture = 8051.ARCHITECTURE,
            bits = 8051.BITS,
            clock_frequency = 8051.CLOCK_FREQUENCY
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
        return string.format("8051(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function 8051.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function 8051.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function 8051.print_device_info(device)
    device = device or 8051.new()
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

function 8051.print_registers(device)
    device = device or 8051.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            8051.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function 8051.example()
    print("=== 8051设备示例 ===")
    
    -- 创建设备实例
    local device = 8051.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    8051.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["ACC"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("ACC", 0x55)
        print("写入 ACC: " .. 8051.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("ACC")
        print("读取 ACC: " .. 8051.hex(value))
        
        -- 位操作
        device:set_bit("ACC", 0, true)
        local bit0 = device:get_bit("ACC", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    8051.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("8051.lua$") then
    8051.example()
end

return 8051

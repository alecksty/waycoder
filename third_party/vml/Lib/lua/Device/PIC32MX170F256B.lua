--[[
  PIC32MX170F256B设备定义 - Lua模块
  生成自: Microchip/PIC32/PIC32MX170F256B
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit MIPS32 M4K MCU with 256KB Flash, 64KB RAM, 50MHz
  CPU架构: MIPS32-M4K
  位宽: 32位
  时钟频率: 50000000 Hz
]]

local PIC32MX170F256B = {}

-- 设备信息
PIC32MX170F256B.DEVICE_NAME = "PIC32MX170F256B"
PIC32MX170F256B.MANUFACTURER = "Microchip"
PIC32MX170F256B.FAMILY = "PIC32"
PIC32MX170F256B.VERSION = "1.0"
PIC32MX170F256B.ARCHITECTURE = "MIPS32-M4K"
PIC32MX170F256B.BITS = 32
PIC32MX170F256B.CLOCK_FREQUENCY = 50000000

-- 寄存器地址定义
PIC32MX170F256B._0_ADDR = 0x00  -- Hard-wired zero
PIC32MX170F256B._1_ADDR = 0x04  -- AT
PIC32MX170F256B._2_ADDR = 0x08  -- V0
PIC32MX170F256B._3_ADDR = 0x0C  -- V1
PIC32MX170F256B._4_ADDR = 0x10  -- A0
PIC32MX170F256B._5_ADDR = 0x14  -- A1
PIC32MX170F256B._29_ADDR = 0x74  -- Stack Pointer (SP)
PIC32MX170F256B._31_ADDR = 0x7C  -- Return Address (RA)
PIC32MX170F256B.PC_ADDR = 0x80  -- Program Counter

-- 内存段定义
PIC32MX170F256B.FLASH_START = 0x9D000000
PIC32MX170F256B.FLASH_END = 0x9D03FFFF
PIC32MX170F256B.FLASH_SIZE = 262144  -- Program Flash
PIC32MX170F256B.SRAM_START = 0xA0000000
PIC32MX170F256B.SRAM_END = 0xA000FFFF
PIC32MX170F256B.SRAM_SIZE = 65536  -- 
PIC32MX170F256B.PERIPHERAL_START = 0xBF800000
PIC32MX170F256B.PERIPHERAL_END = 0xBF8FFFFF
PIC32MX170F256B.PERIPHERAL_SIZE = 1048576  -- 
PIC32MX170F256B.BOOTFLASH_START = 0xBFC00000
PIC32MX170F256B.BOOTFLASH_END = 0xBFC02FFF
PIC32MX170F256B.BOOTFLASH_SIZE = 12288  -- Boot Flash

-- 外设定义
-- General Purpose I/O Port A
PIC32MX170F256B.PORTA_BASE = 0xBF886000
PIC32MX170F256B.PORTA_TRISA_ADDR = 0x00
PIC32MX170F256B.PORTA_PORTA_ADDR = 0x10
PIC32MX170F256B.PORTA_LATA_ADDR = 0x20
PIC32MX170F256B.PORTA_ODCA_ADDR = 0x30
-- General Purpose I/O Port B
PIC32MX170F256B.PORTB_BASE = 0xBF886100
PIC32MX170F256B.PORTB_TRISB_ADDR = 0x00
PIC32MX170F256B.PORTB_PORTB_ADDR = 0x10
PIC32MX170F256B.PORTB_LATB_ADDR = 0x20
PIC32MX170F256B.PORTB_ODCB_ADDR = 0x30
-- UART1
PIC32MX170F256B.UART1_BASE = 0xBF822000
PIC32MX170F256B.UART1_UXMODE_ADDR = 0x00
PIC32MX170F256B.UART1_UXSTA_ADDR = 0x04
PIC32MX170F256B.UART1_UXTXREG_ADDR = 0x08
PIC32MX170F256B.UART1_UXRXREG_ADDR = 0x0C
PIC32MX170F256B.UART1_UXBRG_ADDR = 0x10

-- 中断向量定义
PIC32MX170F256B.INT_RESET = 0  -- 
PIC32MX170F256B.INT_UART1 = 8  -- UART1 Interrupt

-- 设备类
function PIC32MX170F256B.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["$0"] = {
            address = 0x00,
            size = 4,
            access = "rw",
            description = "Hard-wired zero",
            value = 0
        }
        self.registers["$1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "AT",
            value = 0
        }
        self.registers["$2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "V0",
            value = 0
        }
        self.registers["$3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "V1",
            value = 0
        }
        self.registers["$4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "A0",
            value = 0
        }
        self.registers["$5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "A1",
            value = 0
        }
        self.registers["$29"] = {
            address = 0x74,
            size = 4,
            access = "rw",
            description = "Stack Pointer (SP)",
            value = 0
        }
        self.registers["$31"] = {
            address = 0x7C,
            size = 4,
            access = "rw",
            description = "Return Address (RA)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x80,
            size = 4,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PORTA"] = {
            base = 0xBF886000,
            type = "GPIO",
            description = "General Purpose I/O Port A",
            registers = {}
        }
        
        local p = self.peripherals["PORTA"]
        p.registers["TRISA"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PORTA"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["LATA"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["ODCA"] = {
            address = 0x30,
            size = 4,
            value = 0
        }
        self.peripherals["PORTB"] = {
            base = 0xBF886100,
            type = "GPIO",
            description = "General Purpose I/O Port B",
            registers = {}
        }
        
        local p = self.peripherals["PORTB"]
        p.registers["TRISB"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["PORTB"] = {
            address = 0x10,
            size = 4,
            value = 0
        }
        p.registers["LATB"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["ODCB"] = {
            address = 0x30,
            size = 4,
            value = 0
        }
        self.peripherals["UART1"] = {
            base = 0xBF822000,
            type = "UART",
            description = "UART1",
            registers = {}
        }
        
        local p = self.peripherals["UART1"]
        p.registers["UXMODE"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["UXSTA"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["UXTXREG"] = {
            address = 0x08,
            size = 4,
            value = 0
        }
        p.registers["UXRXREG"] = {
            address = 0x0C,
            size = 4,
            value = 0
        }
        p.registers["UXBRG"] = {
            address = 0x10,
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
            name = PIC32MX170F256B.DEVICE_NAME,
            manufacturer = PIC32MX170F256B.MANUFACTURER,
            family = PIC32MX170F256B.FAMILY,
            version = PIC32MX170F256B.VERSION,
            architecture = PIC32MX170F256B.ARCHITECTURE,
            bits = PIC32MX170F256B.BITS,
            clock_frequency = PIC32MX170F256B.CLOCK_FREQUENCY
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
        return string.format("PIC32MX170F256B(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function PIC32MX170F256B.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function PIC32MX170F256B.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function PIC32MX170F256B.print_device_info(device)
    device = device or PIC32MX170F256B.new()
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

function PIC32MX170F256B.print_registers(device)
    device = device or PIC32MX170F256B.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            PIC32MX170F256B.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function PIC32MX170F256B.example()
    print("=== PIC32MX170F256B设备示例 ===")
    
    -- 创建设备实例
    local device = PIC32MX170F256B.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    PIC32MX170F256B.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["$0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("$0", 0x55)
        print("写入 $0: " .. PIC32MX170F256B.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("$0")
        print("读取 $0: " .. PIC32MX170F256B.hex(value))
        
        -- 位操作
        device:set_bit("$0", 0, true)
        local bit0 = device:get_bit("$0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    PIC32MX170F256B.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("PIC32MX170F256B.lua$") then
    PIC32MX170F256B.example()
end

return PIC32MX170F256B

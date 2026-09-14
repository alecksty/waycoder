--[[
  RA4M2设备定义 - Lua模块
  生成自: Renesas/RA/RA4M2
  版本: 1.0
  日期: 2026-04-28
  作者: VML Team
  描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 128KB RAM, 100MHz
  CPU架构: ARM-Cortex-M4
  位宽: 32位
  时钟频率: 100000000 Hz
]]

local RA4M2 = {}

-- 设备信息
RA4M2.DEVICE_NAME = "RA4M2"
RA4M2.MANUFACTURER = "Renesas"
RA4M2.FAMILY = "RA"
RA4M2.VERSION = "1.0"
RA4M2.ARCHITECTURE = "ARM-Cortex-M4"
RA4M2.BITS = 32
RA4M2.CLOCK_FREQUENCY = 100000000

-- 寄存器地址定义
RA4M2.R0_ADDR = 0x00  -- 
RA4M2.R1_ADDR = 0x04  -- 
RA4M2.R2_ADDR = 0x08  -- 
RA4M2.R3_ADDR = 0x0C  -- 
RA4M2.R4_ADDR = 0x10  -- 
RA4M2.R5_ADDR = 0x14  -- 
RA4M2.SP_ADDR = 0x34  -- 
RA4M2.LR_ADDR = 0x38  -- 
RA4M2.PC_ADDR = 0x3C  -- 

-- 内存段定义
RA4M2.FLASH_START = 0x00000000
RA4M2.FLASH_END = 0x0003FFFF
RA4M2.FLASH_SIZE = 262144  -- 
RA4M2.SRAM_START = 0x1FFE0000
RA4M2.SRAM_END = 0x1FFE7FFF
RA4M2.SRAM_SIZE = 32768  -- SRAM0
RA4M2.SRAM1_START = 0x20000000
RA4M2.SRAM1_END = 0x20017FFF
RA4M2.SRAM1_SIZE = 98304  -- SRAM1
RA4M2.PERIPHERAL_START = 0x40000000
RA4M2.PERIPHERAL_END = 0x400FFFFF
RA4M2.PERIPHERAL_SIZE = 1048576  -- 

-- 外设定义
-- Module Stop Control
RA4M2.MSTP_BASE = 0x40020000
RA4M2.MSTP_MSTPCR_A_ADDR = 0x20
RA4M2.MSTP_MSTPCR_A_MSTP41_BIT = 9  -- GPIO A stop
RA4M2.MSTP_MSTPCR_A_MSTP42_BIT = 10  -- GPIO B stop
RA4M2.MSTP_MSTPCR_B_ADDR = 0x24
RA4M2.MSTP_MSTPCR_C_ADDR = 0x28
RA4M2.MSTP_MSTPCR_D_ADDR = 0x2C
-- Interrupt Controller Unit
RA4M2.ICU_BASE = 0x40030000
RA4M2.ICU_IRQCR0_ADDR = 0x600
RA4M2.ICU_IRQCR1_ADDR = 0x602
-- General Purpose I/O Port A
RA4M2.GPIOA_BASE = 0x40040000
RA4M2.GPIOA_PDR_ADDR = 0x00
RA4M2.GPIOA_PODR_ADDR = 0x04
RA4M2.GPIOA_PIDR_ADDR = 0x08
RA4M2.GPIOA_PMR_ADDR = 0x10
RA4M2.GPIOA_PCR_ADDR = 0x18
-- General Purpose I/O Port B
RA4M2.GPIOB_BASE = 0x40040020
RA4M2.GPIOB_PDR_ADDR = 0x00
RA4M2.GPIOB_PODR_ADDR = 0x04
RA4M2.GPIOB_PIDR_ADDR = 0x08
RA4M2.GPIOB_PMR_ADDR = 0x10
-- SCI UART 0
RA4M2.SCIUART0_BASE = 0x40070000
RA4M2.SCIUART0_SCR_ADDR = 0x00
RA4M2.SCIUART0_BRR_ADDR = 0x04
RA4M2.SCIUART0_TDR_ADDR = 0x08
RA4M2.SCIUART0_RDR_ADDR = 0x0C
RA4M2.SCIUART0_SSR_ADDR = 0x10

-- 中断向量定义
RA4M2.INT_RESET = 0  -- 
RA4M2.INT_SVCALL = 11  -- 
RA4M2.INT_SCIUART0_RXI = 24  -- SCI UART0 Receive Interrupt
RA4M2.INT_SCIUART0_TXI = 25  -- SCI UART0 Transmit Interrupt

-- 设备类
function RA4M2.new(memory_base)
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
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["LR"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["MSTP"] = {
            base = 0x40020000,
            type = "ClockControl",
            description = "Module Stop Control",
            registers = {}
        }
        
        local p = self.peripherals["MSTP"]
        p.registers["MSTPCR_A"] = {
            address = 0x20,
            size = 4,
            value = 0
        }
        p.registers["MSTPCR_B"] = {
            address = 0x24,
            size = 4,
            value = 0
        }
        p.registers["MSTPCR_C"] = {
            address = 0x28,
            size = 4,
            value = 0
        }
        p.registers["MSTPCR_D"] = {
            address = 0x2C,
            size = 4,
            value = 0
        }
        self.peripherals["ICU"] = {
            base = 0x40030000,
            type = "InterruptControl",
            description = "Interrupt Controller Unit",
            registers = {}
        }
        
        local p = self.peripherals["ICU"]
        p.registers["IRQCR0"] = {
            address = 0x600,
            size = 2,
            value = 0
        }
        p.registers["IRQCR1"] = {
            address = 0x602,
            size = 2,
            value = 0
        }
        self.peripherals["GPIOA"] = {
            base = 0x40040000,
            type = "GPIO",
            description = "General Purpose I/O Port A",
            registers = {}
        }
        
        local p = self.peripherals["GPIOA"]
        p.registers["PDR"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["PODR"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["PIDR"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["PMR"] = {
            address = 0x10,
            size = 2,
            value = 0
        }
        p.registers["PCR"] = {
            address = 0x18,
            size = 4,
            value = 0
        }
        self.peripherals["GPIOB"] = {
            base = 0x40040020,
            type = "GPIO",
            description = "General Purpose I/O Port B",
            registers = {}
        }
        
        local p = self.peripherals["GPIOB"]
        p.registers["PDR"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["PODR"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["PIDR"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["PMR"] = {
            address = 0x10,
            size = 2,
            value = 0
        }
        self.peripherals["SCIUART0"] = {
            base = 0x40070000,
            type = "UART",
            description = "SCI UART 0",
            registers = {}
        }
        
        local p = self.peripherals["SCIUART0"]
        p.registers["SCR"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["BRR"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["TDR"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["RDR"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        p.registers["SSR"] = {
            address = 0x10,
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
            name = RA4M2.DEVICE_NAME,
            manufacturer = RA4M2.MANUFACTURER,
            family = RA4M2.FAMILY,
            version = RA4M2.VERSION,
            architecture = RA4M2.ARCHITECTURE,
            bits = RA4M2.BITS,
            clock_frequency = RA4M2.CLOCK_FREQUENCY
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
        return string.format("RA4M2(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function RA4M2.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function RA4M2.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function RA4M2.print_device_info(device)
    device = device or RA4M2.new()
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

function RA4M2.print_registers(device)
    device = device or RA4M2.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            RA4M2.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function RA4M2.example()
    print("=== RA4M2设备示例 ===")
    
    -- 创建设备实例
    local device = RA4M2.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    RA4M2.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. RA4M2.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. RA4M2.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    RA4M2.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("RA4M2.lua$") then
    RA4M2.example()
end

return RA4M2

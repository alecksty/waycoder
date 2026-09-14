--[[
  8086设备定义 - Lua模块
  生成自: Intel/x86/8086
  版本: 1.0
  日期: 2026-04-17
  作者: VML Team
  描述: 16-bit microprocessor, first x86 processor
  CPU架构: x86
  位宽: 16位
  时钟频率: 5000000 Hz
]]

local 8086 = {}

-- 设备信息
8086.DEVICE_NAME = "8086"
8086.MANUFACTURER = "Intel"
8086.FAMILY = "x86"
8086.VERSION = "1.0"
8086.ARCHITECTURE = "x86"
8086.BITS = 16
8086.CLOCK_FREQUENCY = 5000000

-- 寄存器地址定义
8086.AX_ADDR = 0  -- Accumulator
8086.AX_AH_BIT = 8  -- High byte of AX
8086.AX_AL_BIT = 0  -- Low byte of AX
8086.BX_ADDR = 1  -- Base
8086.BX_BH_BIT = 8  -- High byte of BX
8086.BX_BL_BIT = 0  -- Low byte of BX
8086.CX_ADDR = 2  -- Counter
8086.CX_CH_BIT = 8  -- High byte of CX
8086.CX_CL_BIT = 0  -- Low byte of CX
8086.DX_ADDR = 3  -- Data
8086.DX_DH_BIT = 8  -- High byte of DX
8086.DX_DL_BIT = 0  -- Low byte of DX
8086.SI_ADDR = 4  -- Source Index
8086.DI_ADDR = 5  -- Destination Index
8086.BP_ADDR = 6  -- Base Pointer
8086.SP_ADDR = 7  -- Stack Pointer
8086.IP_ADDR = 8  -- Instruction Pointer
8086.CS_ADDR = 9  -- Code Segment
8086.DS_ADDR = 10  -- Data Segment
8086.ES_ADDR = 11  -- Extra Segment
8086.SS_ADDR = 12  -- Stack Segment
8086.FLAGS_ADDR = 13  -- Flags Register
8086.FLAGS_CF_BIT = 0  -- Carry Flag
8086.FLAGS_PF_BIT = 2  -- Parity Flag
8086.FLAGS_AF_BIT = 4  -- Auxiliary Flag
8086.FLAGS_ZF_BIT = 6  -- Zero Flag
8086.FLAGS_SF_BIT = 7  -- Sign Flag
8086.FLAGS_TF_BIT = 8  -- Trap Flag
8086.FLAGS_IF_BIT = 9  -- Interrupt Enable Flag
8086.FLAGS_DF_BIT = 10  -- Direction Flag
8086.FLAGS_OF_BIT = 11  -- Overflow Flag

-- 内存段定义
8086.CODE_START = 0x00000
8086.CODE_END = 0xFFFFF
8086.CODE_SIZE = 1048576  -- 1MB address space
8086.DATA_START = 0x00000
8086.DATA_END = 0xFFFFF
8086.DATA_SIZE = 1048576  -- Data memory
8086.STACK_START = 0xF0000
8086.STACK_END = 0xFFFFF
8086.STACK_SIZE = 65536  -- Stack memory
8086.BIOS_START = 0xF0000
8086.BIOS_END = 0xFFFFF
8086.BIOS_SIZE = 65536  -- BIOS ROM

-- 外设定义
-- Programmable Interrupt Controller
8086.PIC_BASE = 0x0020
8086.PIC_PIC1_CMD_ADDR = 0x0020
8086.PIC_PIC1_DATA_ADDR = 0x0021
8086.PIC_PIC2_CMD_ADDR = 0x00A0
8086.PIC_PIC2_DATA_ADDR = 0x00A1
-- Programmable Interval Timer
8086.PIT_BASE = 0x0040
8086.PIT_PIT_CH0_ADDR = 0x0040
8086.PIT_PIT_CH1_ADDR = 0x0041
8086.PIT_PIT_CH2_ADDR = 0x0042
8086.PIT_PIT_CMD_ADDR = 0x0043
-- Programmable Peripheral Interface
8086.PPI_BASE = 0x0060
8086.PPI_PPI_PA_ADDR = 0x0060
8086.PPI_PPI_PB_ADDR = 0x0061
8086.PPI_PPI_PC_ADDR = 0x0062
8086.PPI_PPI_CMD_ADDR = 0x0063

-- 中断向量定义
8086.INT_DIVIDE_ERROR = 0  -- Divide by zero
8086.INT_DEBUG = 1  -- Single step
8086.INT_NMI = 2  -- Non-maskable interrupt
8086.INT_BREAKPOINT = 3  -- Breakpoint
8086.INT_OVERFLOW = 4  -- INTO detected overflow
8086.INT_IRQ0 = 8  -- Timer interrupt
8086.INT_IRQ1 = 9  -- Keyboard interrupt
8086.INT_IRQ2 = 10  -- Cascade
8086.INT_IRQ3 = 11  -- COM2
8086.INT_IRQ4 = 12  -- COM1
8086.INT_IRQ5 = 13  -- LPT2
8086.INT_IRQ6 = 14  -- Floppy disk
8086.INT_IRQ7 = 15  -- LPT1

-- 设备类
function 8086.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["AX"] = {
            address = 0,
            size = 2,
            access = "rw",
            description = "Accumulator",
            value = 0
        }
        self.registers["BX"] = {
            address = 1,
            size = 2,
            access = "rw",
            description = "Base",
            value = 0
        }
        self.registers["CX"] = {
            address = 2,
            size = 2,
            access = "rw",
            description = "Counter",
            value = 0
        }
        self.registers["DX"] = {
            address = 3,
            size = 2,
            access = "rw",
            description = "Data",
            value = 0
        }
        self.registers["SI"] = {
            address = 4,
            size = 2,
            access = "rw",
            description = "Source Index",
            value = 0
        }
        self.registers["DI"] = {
            address = 5,
            size = 2,
            access = "rw",
            description = "Destination Index",
            value = 0
        }
        self.registers["BP"] = {
            address = 6,
            size = 2,
            access = "rw",
            description = "Base Pointer",
            value = 0
        }
        self.registers["SP"] = {
            address = 7,
            size = 2,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["IP"] = {
            address = 8,
            size = 2,
            access = "rw",
            description = "Instruction Pointer",
            value = 0
        }
        self.registers["CS"] = {
            address = 9,
            size = 2,
            access = "rw",
            description = "Code Segment",
            value = 0
        }
        self.registers["DS"] = {
            address = 10,
            size = 2,
            access = "rw",
            description = "Data Segment",
            value = 0
        }
        self.registers["ES"] = {
            address = 11,
            size = 2,
            access = "rw",
            description = "Extra Segment",
            value = 0
        }
        self.registers["SS"] = {
            address = 12,
            size = 2,
            access = "rw",
            description = "Stack Segment",
            value = 0
        }
        self.registers["FLAGS"] = {
            address = 13,
            size = 2,
            access = "rw",
            description = "Flags Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PIC"] = {
            base = 0x0020,
            type = "InterruptController",
            description = "Programmable Interrupt Controller",
            registers = {}
        }
        
        local p = self.peripherals["PIC"]
        p.registers["PIC1_CMD"] = {
            address = 0x0020,
            size = 1,
            value = 0
        }
        p.registers["PIC1_DATA"] = {
            address = 0x0021,
            size = 1,
            value = 0
        }
        p.registers["PIC2_CMD"] = {
            address = 0x00A0,
            size = 1,
            value = 0
        }
        p.registers["PIC2_DATA"] = {
            address = 0x00A1,
            size = 1,
            value = 0
        }
        self.peripherals["PIT"] = {
            base = 0x0040,
            type = "Timer",
            description = "Programmable Interval Timer",
            registers = {}
        }
        
        local p = self.peripherals["PIT"]
        p.registers["PIT_CH0"] = {
            address = 0x0040,
            size = 1,
            value = 0
        }
        p.registers["PIT_CH1"] = {
            address = 0x0041,
            size = 1,
            value = 0
        }
        p.registers["PIT_CH2"] = {
            address = 0x0042,
            size = 1,
            value = 0
        }
        p.registers["PIT_CMD"] = {
            address = 0x0043,
            size = 1,
            value = 0
        }
        self.peripherals["PPI"] = {
            base = 0x0060,
            type = "IO",
            description = "Programmable Peripheral Interface",
            registers = {}
        }
        
        local p = self.peripherals["PPI"]
        p.registers["PPI_PA"] = {
            address = 0x0060,
            size = 1,
            value = 0
        }
        p.registers["PPI_PB"] = {
            address = 0x0061,
            size = 1,
            value = 0
        }
        p.registers["PPI_PC"] = {
            address = 0x0062,
            size = 1,
            value = 0
        }
        p.registers["PPI_CMD"] = {
            address = 0x0063,
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
            name = 8086.DEVICE_NAME,
            manufacturer = 8086.MANUFACTURER,
            family = 8086.FAMILY,
            version = 8086.VERSION,
            architecture = 8086.ARCHITECTURE,
            bits = 8086.BITS,
            clock_frequency = 8086.CLOCK_FREQUENCY
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
        return string.format("8086(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function 8086.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function 8086.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function 8086.print_device_info(device)
    device = device or 8086.new()
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

function 8086.print_registers(device)
    device = device or 8086.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            8086.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function 8086.example()
    print("=== 8086设备示例 ===")
    
    -- 创建设备实例
    local device = 8086.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    8086.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["AX"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("AX", 0x55)
        print("写入 AX: " .. 8086.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("AX")
        print("读取 AX: " .. 8086.hex(value))
        
        -- 位操作
        device:set_bit("AX", 0, true)
        local bit0 = device:get_bit("AX", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    8086.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("8086.lua$") then
    8086.example()
end

return 8086

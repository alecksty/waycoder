--[[
  IBM-PC-5150设备定义 - Lua模块
  生成自: IBM/Personal Computer/IBM-PC-5150
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Original IBM Personal Computer Model 5150
  CPU架构: x86
  位宽: 16位
  时钟频率: 4772727 Hz
]]

local IBM_PC_5150 = {}

-- 设备信息
IBM_PC_5150.DEVICE_NAME = "IBM-PC-5150"
IBM_PC_5150.MANUFACTURER = "IBM"
IBM_PC_5150.FAMILY = "Personal Computer"
IBM_PC_5150.VERSION = "1.0"
IBM_PC_5150.ARCHITECTURE = "x86"
IBM_PC_5150.BITS = 16
IBM_PC_5150.CLOCK_FREQUENCY = 4772727

-- 寄存器地址定义
IBM_PC_5150.AX_ADDR = 0x0  -- Accumulator Register
IBM_PC_5150.BX_ADDR = 0x1  -- Base Register
IBM_PC_5150.CX_ADDR = 0x2  -- Count Register
IBM_PC_5150.DX_ADDR = 0x3  -- Data Register
IBM_PC_5150.SI_ADDR = 0x4  -- Source Index
IBM_PC_5150.DI_ADDR = 0x5  -- Destination Index
IBM_PC_5150.BP_ADDR = 0x6  -- Base Pointer
IBM_PC_5150.SP_ADDR = 0x7  -- Stack Pointer
IBM_PC_5150.CS_ADDR = 0x8  -- Code Segment
IBM_PC_5150.DS_ADDR = 0x9  -- Data Segment
IBM_PC_5150.ES_ADDR = 0xA  -- Extra Segment
IBM_PC_5150.SS_ADDR = 0xB  -- Stack Segment
IBM_PC_5150.IP_ADDR = 0xC  -- Instruction Pointer
IBM_PC_5150.FLAGS_ADDR = 0xD  -- Flags Register
IBM_PC_5150.FLAGS_CF_BIT = 0  -- Carry Flag
IBM_PC_5150.FLAGS_PF_BIT = 2  -- Parity Flag
IBM_PC_5150.FLAGS_AF_BIT = 4  -- Auxiliary Carry Flag
IBM_PC_5150.FLAGS_ZF_BIT = 6  -- Zero Flag
IBM_PC_5150.FLAGS_SF_BIT = 7  -- Sign Flag
IBM_PC_5150.FLAGS_TF_BIT = 8  -- Trap Flag
IBM_PC_5150.FLAGS_IF_BIT = 9  -- Interrupt Enable Flag
IBM_PC_5150.FLAGS_DF_BIT = 10  -- Direction Flag
IBM_PC_5150.FLAGS_OF_BIT = 11  -- Overflow Flag

-- 内存段定义
IBM_PC_5150.BIOS_START = 0xF0000
IBM_PC_5150.BIOS_END = 0xFFFFF
IBM_PC_5150.BIOS_SIZE = 65536  -- BIOS ROM
IBM_PC_5150.VIDEO_START = 0xB8000
IBM_PC_5150.VIDEO_END = 0xBFFFF
IBM_PC_5150.VIDEO_SIZE = 32768  -- Video Memory
IBM_PC_5150.CONVENTIONAL_START = 0x00000
IBM_PC_5150.CONVENTIONAL_END = 0x9FFFF
IBM_PC_5150.CONVENTIONAL_SIZE = 640  -- Conventional Memory (640KB)
IBM_PC_5150.EXTENDED_START = 0x100000
IBM_PC_5150.EXTENDED_END = 0x10FFFF
IBM_PC_5150.EXTENDED_SIZE = 64  -- Extended Memory (64KB)

-- 外设定义
-- Programmable Interrupt Controller
IBM_PC_5150.PIC_BASE = 0x20
IBM_PC_5150.PIC_PIC1_CMD_ADDR = 0x20
IBM_PC_5150.PIC_PIC1_DATA_ADDR = 0x21
IBM_PC_5150.PIC_PIC2_CMD_ADDR = 0xA0
IBM_PC_5150.PIC_PIC2_DATA_ADDR = 0xA1
-- Programmable Interval Timer
IBM_PC_5150.PIT_BASE = 0x40
IBM_PC_5150.PIT_PIT_CH0_ADDR = 0x40
IBM_PC_5150.PIT_PIT_CH1_ADDR = 0x41
IBM_PC_5150.PIT_PIT_CH2_ADDR = 0x42
IBM_PC_5150.PIT_PIT_CTRL_ADDR = 0x43
-- Programmable Peripheral Interface
IBM_PC_5150.PPI_BASE = 0x60
IBM_PC_5150.PPI_PPI_PA_ADDR = 0x60
IBM_PC_5150.PPI_PPI_PB_ADDR = 0x61
IBM_PC_5150.PPI_PPI_PC_ADDR = 0x62
IBM_PC_5150.PPI_PPI_CTRL_ADDR = 0x63
-- Direct Memory Access Controller
IBM_PC_5150.DMA_BASE = 0x00
IBM_PC_5150.DMA_DMA_CH0_ADDR_ADDR = 0x00
IBM_PC_5150.DMA_DMA_CH0_COUNT_ADDR = 0x01
IBM_PC_5150.DMA_DMA_CMD_ADDR = 0x08
IBM_PC_5150.DMA_DMA_MASK_ADDR = 0x0A
IBM_PC_5150.DMA_DMA_MODE_ADDR = 0x0B
-- Color Graphics Adapter
IBM_PC_5150.CGA_BASE = 0x3D4
IBM_PC_5150.CGA_CGA_INDEX_ADDR = 0x3D4
IBM_PC_5150.CGA_CGA_DATA_ADDR = 0x3D5
IBM_PC_5150.CGA_CGA_MODE_ADDR = 0x3D8
IBM_PC_5150.CGA_CGA_COLOR_ADDR = 0x3D9

-- 中断向量定义
IBM_PC_5150.INT_DIVIDE_ERROR = 0  -- Divide Error
IBM_PC_5150.INT_SINGLE_STEP = 1  -- Single Step
IBM_PC_5150.INT_NMI = 2  -- Non-Maskable Interrupt
IBM_PC_5150.INT_BREAKPOINT = 3  -- Breakpoint
IBM_PC_5150.INT_OVERFLOW = 4  -- Overflow
IBM_PC_5150.INT_PRINT_SCREEN = 5  -- Print Screen
IBM_PC_5150.INT_IRQ0 = 8  -- Timer Interrupt
IBM_PC_5150.INT_IRQ1 = 9  -- Keyboard Interrupt
IBM_PC_5150.INT_IRQ2 = 10  -- Cascade (8259A)
IBM_PC_5150.INT_IRQ3 = 11  -- COM2
IBM_PC_5150.INT_IRQ4 = 12  -- COM1
IBM_PC_5150.INT_IRQ5 = 13  -- LPT2
IBM_PC_5150.INT_IRQ6 = 14  -- Floppy Disk
IBM_PC_5150.INT_IRQ7 = 15  -- LPT1
IBM_PC_5150.INT_IRQ8 = 16  -- Real Time Clock
IBM_PC_5150.INT_IRQ11 = 19  -- Reserved
IBM_PC_5150.INT_IRQ13 = 21  -- Coprocessor
IBM_PC_5150.INT_IRQ15 = 31  -- Reserved

-- 引脚定义
IBM_PC_5150.PIN_VCC = 1  -- +5V Power Supply
IBM_PC_5150.PIN_GND = 2  -- Ground
IBM_PC_5150.PIN_RESET = 3  -- System Reset
IBM_PC_5150.PIN_CLK = 4  -- System Clock (4.77MHz)
IBM_PC_5150.PIN_READY = 5  -- CPU Ready Signal
IBM_PC_5150.PIN_NMI = 6  -- Non-Maskable Interrupt
IBM_PC_5150.PIN_INTR = 7  -- Interrupt Request
IBM_PC_5150.PIN_HLDA = 8  -- Hold Acknowledge
IBM_PC_5150.PIN_HOLD = 9  -- Hold Request
IBM_PC_5150.PIN_MEMR = 10  -- Memory Read
IBM_PC_5150.PIN_MEMW = 11  -- Memory Write
IBM_PC_5150.PIN_IOR = 12  -- I/O Read
IBM_PC_5150.PIN_IOW = 13  -- I/O Write
IBM_PC_5150.PIN_ALE = 14  -- Address Latch Enable
IBM_PC_5150.PIN_DTR = 15  -- Data Terminal Ready (Serial)
IBM_PC_5150.PIN_RTS = 16  -- Request To Send (Serial)
IBM_PC_5150.PIN_CTS = 17  -- Clear To Send (Serial)
IBM_PC_5150.PIN_DSR = 18  -- Data Set Ready (Serial)
IBM_PC_5150.PIN_RI = 19  -- Ring Indicator (Serial)
IBM_PC_5150.PIN_DCD = 20  -- Data Carrier Detect (Serial)

-- 设备类
function IBM_PC_5150.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["AX"] = {
            address = 0x0,
            size = 2,
            access = "rw",
            description = "Accumulator Register",
            value = 0
        }
        self.registers["BX"] = {
            address = 0x1,
            size = 2,
            access = "rw",
            description = "Base Register",
            value = 0
        }
        self.registers["CX"] = {
            address = 0x2,
            size = 2,
            access = "rw",
            description = "Count Register",
            value = 0
        }
        self.registers["DX"] = {
            address = 0x3,
            size = 2,
            access = "rw",
            description = "Data Register",
            value = 0
        }
        self.registers["SI"] = {
            address = 0x4,
            size = 2,
            access = "rw",
            description = "Source Index",
            value = 0
        }
        self.registers["DI"] = {
            address = 0x5,
            size = 2,
            access = "rw",
            description = "Destination Index",
            value = 0
        }
        self.registers["BP"] = {
            address = 0x6,
            size = 2,
            access = "rw",
            description = "Base Pointer",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x7,
            size = 2,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["CS"] = {
            address = 0x8,
            size = 2,
            access = "rw",
            description = "Code Segment",
            value = 0
        }
        self.registers["DS"] = {
            address = 0x9,
            size = 2,
            access = "rw",
            description = "Data Segment",
            value = 0
        }
        self.registers["ES"] = {
            address = 0xA,
            size = 2,
            access = "rw",
            description = "Extra Segment",
            value = 0
        }
        self.registers["SS"] = {
            address = 0xB,
            size = 2,
            access = "rw",
            description = "Stack Segment",
            value = 0
        }
        self.registers["IP"] = {
            address = 0xC,
            size = 2,
            access = "rw",
            description = "Instruction Pointer",
            value = 0
        }
        self.registers["FLAGS"] = {
            address = 0xD,
            size = 2,
            access = "rw",
            description = "Flags Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PIC"] = {
            base = 0x20,
            type = "InterruptController",
            description = "Programmable Interrupt Controller",
            registers = {}
        }
        
        local p = self.peripherals["PIC"]
        p.registers["PIC1_CMD"] = {
            address = 0x20,
            size = 1,
            value = 0
        }
        p.registers["PIC1_DATA"] = {
            address = 0x21,
            size = 1,
            value = 0
        }
        p.registers["PIC2_CMD"] = {
            address = 0xA0,
            size = 1,
            value = 0
        }
        p.registers["PIC2_DATA"] = {
            address = 0xA1,
            size = 1,
            value = 0
        }
        self.peripherals["PIT"] = {
            base = 0x40,
            type = "Timer",
            description = "Programmable Interval Timer",
            registers = {}
        }
        
        local p = self.peripherals["PIT"]
        p.registers["PIT_CH0"] = {
            address = 0x40,
            size = 1,
            value = 0
        }
        p.registers["PIT_CH1"] = {
            address = 0x41,
            size = 1,
            value = 0
        }
        p.registers["PIT_CH2"] = {
            address = 0x42,
            size = 1,
            value = 0
        }
        p.registers["PIT_CTRL"] = {
            address = 0x43,
            size = 1,
            value = 0
        }
        self.peripherals["PPI"] = {
            base = 0x60,
            type = "GPIO",
            description = "Programmable Peripheral Interface",
            registers = {}
        }
        
        local p = self.peripherals["PPI"]
        p.registers["PPI_PA"] = {
            address = 0x60,
            size = 1,
            value = 0
        }
        p.registers["PPI_PB"] = {
            address = 0x61,
            size = 1,
            value = 0
        }
        p.registers["PPI_PC"] = {
            address = 0x62,
            size = 1,
            value = 0
        }
        p.registers["PPI_CTRL"] = {
            address = 0x63,
            size = 1,
            value = 0
        }
        self.peripherals["DMA"] = {
            base = 0x00,
            type = "DMA",
            description = "Direct Memory Access Controller",
            registers = {}
        }
        
        local p = self.peripherals["DMA"]
        p.registers["DMA_CH0_ADDR"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["DMA_CH0_COUNT"] = {
            address = 0x01,
            size = 2,
            value = 0
        }
        p.registers["DMA_CMD"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["DMA_MASK"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["DMA_MODE"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        self.peripherals["CGA"] = {
            base = 0x3D4,
            type = "Video",
            description = "Color Graphics Adapter",
            registers = {}
        }
        
        local p = self.peripherals["CGA"]
        p.registers["CGA_INDEX"] = {
            address = 0x3D4,
            size = 1,
            value = 0
        }
        p.registers["CGA_DATA"] = {
            address = 0x3D5,
            size = 1,
            value = 0
        }
        p.registers["CGA_MODE"] = {
            address = 0x3D8,
            size = 1,
            value = 0
        }
        p.registers["CGA_COLOR"] = {
            address = 0x3D9,
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
            name = IBM_PC_5150.DEVICE_NAME,
            manufacturer = IBM_PC_5150.MANUFACTURER,
            family = IBM_PC_5150.FAMILY,
            version = IBM_PC_5150.VERSION,
            architecture = IBM_PC_5150.ARCHITECTURE,
            bits = IBM_PC_5150.BITS,
            clock_frequency = IBM_PC_5150.CLOCK_FREQUENCY
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
        return string.format("IBM_PC_5150(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function IBM_PC_5150.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function IBM_PC_5150.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function IBM_PC_5150.print_device_info(device)
    device = device or IBM_PC_5150.new()
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

function IBM_PC_5150.print_registers(device)
    device = device or IBM_PC_5150.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            IBM_PC_5150.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function IBM_PC_5150.example()
    print("=== IBM-PC-5150设备示例 ===")
    
    -- 创建设备实例
    local device = IBM_PC_5150.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    IBM_PC_5150.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["AX"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("AX", 0x55)
        print("写入 AX: " .. IBM_PC_5150.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("AX")
        print("读取 AX: " .. IBM_PC_5150.hex(value))
        
        -- 位操作
        device:set_bit("AX", 0, true)
        local bit0 = device:get_bit("AX", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    IBM_PC_5150.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("IBM_PC_5150.lua$") then
    IBM_PC_5150.example()
end

return IBM_PC_5150

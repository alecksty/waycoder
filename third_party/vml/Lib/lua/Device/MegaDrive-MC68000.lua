--[[
  Motorola-68000设备定义 - Lua模块
  生成自: Motorola/68000/Motorola-68000
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: 16/32-bit microprocessor used in Sega Genesis, Amiga, Atari ST, Macintosh
  CPU架构: MC68000
  位宽: 32位
  时钟频率: 7670452 Hz
]]

local Motorola_68000 = {}

-- 设备信息
Motorola_68000.DEVICE_NAME = "Motorola-68000"
Motorola_68000.MANUFACTURER = "Motorola"
Motorola_68000.FAMILY = "68000"
Motorola_68000.VERSION = "1.0"
Motorola_68000.ARCHITECTURE = "MC68000"
Motorola_68000.BITS = 32
Motorola_68000.CLOCK_FREQUENCY = 7670452

-- 寄存器地址定义
Motorola_68000.D0_ADDR = 0x00  -- Data Register 0
Motorola_68000.D1_ADDR = 0x04  -- Data Register 1
Motorola_68000.D2_ADDR = 0x08  -- Data Register 2
Motorola_68000.D3_ADDR = 0x0C  -- Data Register 3
Motorola_68000.D4_ADDR = 0x10  -- Data Register 4
Motorola_68000.D5_ADDR = 0x14  -- Data Register 5
Motorola_68000.D6_ADDR = 0x18  -- Data Register 6
Motorola_68000.D7_ADDR = 0x1C  -- Data Register 7
Motorola_68000.A0_ADDR = 0x20  -- Address Register 0
Motorola_68000.A1_ADDR = 0x24  -- Address Register 1
Motorola_68000.A2_ADDR = 0x28  -- Address Register 2
Motorola_68000.A3_ADDR = 0x2C  -- Address Register 3
Motorola_68000.A4_ADDR = 0x30  -- Address Register 4
Motorola_68000.A5_ADDR = 0x34  -- Address Register 5
Motorola_68000.A6_ADDR = 0x38  -- Address Register 6
Motorola_68000.A7_ADDR = 0x3C  -- Stack Pointer (USP)
Motorola_68000.PC_ADDR = 0x40  -- Program Counter
Motorola_68000.SR_ADDR = 0x44  -- Status Register
Motorola_68000.SR_C_BIT = 0  -- Carry
Motorola_68000.SR_V_BIT = 1  -- Overflow
Motorola_68000.SR_Z_BIT = 2  -- Zero
Motorola_68000.SR_N_BIT = 3  -- Negative
Motorola_68000.SR_X_BIT = 4  -- Extend
Motorola_68000.SR_I0_BIT = 8  -- Interrupt Mask 0
Motorola_68000.SR_I1_BIT = 9  -- Interrupt Mask 1
Motorola_68000.SR_I2_BIT = 10  -- Interrupt Mask 2
Motorola_68000.SR_M_BIT = 11  -- Master/Interrupt
Motorola_68000.SR_S_BIT = 13  -- Supervisor/User
Motorola_68000.SR_T0_BIT = 14  -- Trace Mode 0
Motorola_68000.SR_T1_BIT = 15  -- Trace Mode 1

-- 内存段定义
Motorola_68000.RAM_START = 0x000000
Motorola_68000.RAM_END = 0x3FFFFF
Motorola_68000.RAM_SIZE = 4194304  -- System RAM (4MB)
Motorola_68000.ROM_START = 0x000000
Motorola_68000.ROM_END = 0x3FFFFF
Motorola_68000.ROM_SIZE = 4194304  -- Cartridge ROM
Motorola_68000.IO_START = 0xA00000
Motorola_68000.IO_END = 0xA1FFFF
Motorola_68000.IO_SIZE = 131072  -- I/O Register Area
Motorola_68000.VDP_START = 0xC00000
Motorola_68000.VDP_END = 0xC0001F
Motorola_68000.VDP_SIZE = 32  -- VDP Registers
Motorola_68000.VRAM_START = 0xE00000
Motorola_68000.VRAM_END = 0xE3FFFF
Motorola_68000.VRAM_SIZE = 262144  -- Video RAM (256KB)

-- 外设定义
-- Video Display Processor (TMS9918A variant)
Motorola_68000.VDP_BASE = 0xC00000
Motorola_68000.VDP_DATA_ADDR = 0x00
Motorola_68000.VDP_CTRL_ADDR = 0x04
Motorola_68000.VDP_HVCOUNT_ADDR = 0x08
Motorola_68000.VDP_HVB_STATUS_ADDR = 0x0A
-- Programmable Sound Generator (AY-3-8910)
Motorola_68000.PSG_BASE = 0xC00011
Motorola_68000.PSG_CH_A_FREQ_ADDR = 0x00
Motorola_68000.PSG_CH_A_VOL_ADDR = 0x08
Motorola_68000.PSG_CH_B_FREQ_ADDR = 0x02
Motorola_68000.PSG_CH_B_VOL_ADDR = 0x09
Motorola_68000.PSG_CH_C_FREQ_ADDR = 0x04
Motorola_68000.PSG_CH_C_VOL_ADDR = 0x0A
Motorola_68000.PSG_NOISE_FREQ_ADDR = 0x06
Motorola_68000.PSG_MIXER_ADDR = 0x07
Motorola_68000.PSG_ENV_FREQ_ADDR = 0x0D
Motorola_68000.PSG_ENV_SHAPE_ADDR = 0x0B
-- Z80 Secondary CPU (Sound)
Motorola_68000.Z80_BASE = 0xA00000
Motorola_68000.Z80_Z80_RESET_ADDR = 0x00
Motorola_68000.Z80_Z80_BUSREQ_ADDR = 0x04
Motorola_68000.Z80_Z80_STATUS_ADDR = 0x08
-- Bank Register
Motorola_68000.BANK_REG_BASE = 0xA12000
Motorola_68000.BANK_REG_ROM_BANK_ADDR = 0x00
Motorola_68000.BANK_REG_RAM_BANK_ADDR = 0x04
-- Hardware Version
Motorola_68000.HW_VERSION_BASE = 0xA10001
Motorola_68000.HW_VERSION_VERSION_ADDR = 0x00
-- Controller Port 1
Motorola_68000.CONTROLLER1_BASE = 0xA10003
Motorola_68000.CONTROLLER1_DATA_ADDR = 0x00
Motorola_68000.CONTROLLER1_CTRL_ADDR = 0x04
-- Controller Port 2
Motorola_68000.CONTROLLER2_BASE = 0xA10005
Motorola_68000.CONTROLLER2_DATA_ADDR = 0x00
Motorola_68000.CONTROLLER2_CTRL_ADDR = 0x04
-- External Port
Motorola_68000.EXT_PORT_BASE = 0xA10007
Motorola_68000.EXT_PORT_DATA_ADDR = 0x00
-- DMA Controller
Motorola_68000.DMA_BASE = 0xA10008
Motorola_68000.DMA_SOURCE_ADDR = 0x00
Motorola_68000.DMA_DEST_ADDR = 0x04
Motorola_68000.DMA_COUNT_ADDR = 0x08
Motorola_68000.DMA_CTRL_ADDR = 0x0A
-- Hardware Timer
Motorola_68000.TIMER_BASE = 0xA1000E
Motorola_68000.TIMER_H_COUNTER_ADDR = 0x00
Motorola_68000.TIMER_V_COUNTER_ADDR = 0x04

-- 中断向量定义
Motorola_68000.INT_RESET_SP = 1  -- Reset Initial Stack Pointer
Motorola_68000.INT_RESET_PC = 2  -- Reset Initial PC
Motorola_68000.INT_BUS_ERROR = 3  -- Bus Error
Motorola_68000.INT_ADDRESS_ERROR = 4  -- Address Error
Motorola_68000.INT_ILLEGAL_INSTR = 5  -- Illegal Instruction
Motorola_68000.INT_ZERO_DIVIDE = 6  -- Zero Divide
Motorola_68000.INT_CHK_EXCEPTION = 7  -- CHK Exception
Motorola_68000.INT_TRAPV = 8  -- TRAPV Exception
Motorola_68000.INT_PRIVILEGE = 9  -- Privilege Violation
Motorola_68000.INT_TRACE = 10  -- Trace
Motorola_68000.INT_LINE_A = 11  -- Line 1010 Emulator
Motorola_68000.INT_LINE_F = 12  -- Line 1111 Emulator
Motorola_68000.INT_IRQ1 = 24  -- External Interrupt 1 (H-Blank)
Motorola_68000.INT_IRQ2 = 25  -- External Interrupt 2 (V-Blank)
Motorola_68000.INT_IRQ3 = 26  -- External Interrupt 3
Motorola_68000.INT_IRQ4 = 27  -- External Interrupt 4 (D-Req)
Motorola_68000.INT_IRQ5 = 28  -- External Interrupt 5
Motorola_68000.INT_IRQ6 = 29  -- External Interrupt 6
Motorola_68000.INT_IRQ7 = 30  -- External Interrupt 7
Motorola_68000.INT_TRAP0 = 32  -- TRAP #0
Motorola_68000.INT_TRAP1 = 33  -- TRAP #1
Motorola_68000.INT_TRAP15 = 47  -- TRAP #15

-- 设备类
function Motorola_68000.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["D0"] = {
            address = 0x00,
            size = 4,
            access = "rw",
            description = "Data Register 0",
            value = 0
        }
        self.registers["D1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "Data Register 1",
            value = 0
        }
        self.registers["D2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "Data Register 2",
            value = 0
        }
        self.registers["D3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "Data Register 3",
            value = 0
        }
        self.registers["D4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "Data Register 4",
            value = 0
        }
        self.registers["D5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "Data Register 5",
            value = 0
        }
        self.registers["D6"] = {
            address = 0x18,
            size = 4,
            access = "rw",
            description = "Data Register 6",
            value = 0
        }
        self.registers["D7"] = {
            address = 0x1C,
            size = 4,
            access = "rw",
            description = "Data Register 7",
            value = 0
        }
        self.registers["A0"] = {
            address = 0x20,
            size = 4,
            access = "rw",
            description = "Address Register 0",
            value = 0
        }
        self.registers["A1"] = {
            address = 0x24,
            size = 4,
            access = "rw",
            description = "Address Register 1",
            value = 0
        }
        self.registers["A2"] = {
            address = 0x28,
            size = 4,
            access = "rw",
            description = "Address Register 2",
            value = 0
        }
        self.registers["A3"] = {
            address = 0x2C,
            size = 4,
            access = "rw",
            description = "Address Register 3",
            value = 0
        }
        self.registers["A4"] = {
            address = 0x30,
            size = 4,
            access = "rw",
            description = "Address Register 4",
            value = 0
        }
        self.registers["A5"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "Address Register 5",
            value = 0
        }
        self.registers["A6"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "Address Register 6",
            value = 0
        }
        self.registers["A7"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "Stack Pointer (USP)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x40,
            size = 4,
            access = "r",
            description = "Program Counter",
            value = 0
        }
        self.registers["SR"] = {
            address = 0x44,
            size = 2,
            access = "rw",
            description = "Status Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["VDP"] = {
            base = 0xC00000,
            type = "video",
            description = "Video Display Processor (TMS9918A variant)",
            registers = {}
        }
        
        local p = self.peripherals["VDP"]
        p.registers["DATA"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["CTRL"] = {
            address = 0x04,
            size = 2,
            value = 0
        }
        p.registers["HVCOUNT"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["HVB_STATUS"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        self.peripherals["PSG"] = {
            base = 0xC00011,
            type = "audio",
            description = "Programmable Sound Generator (AY-3-8910)",
            registers = {}
        }
        
        local p = self.peripherals["PSG"]
        p.registers["CH_A_FREQ"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["CH_A_VOL"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["CH_B_FREQ"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["CH_B_VOL"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["CH_C_FREQ"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["CH_C_VOL"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["NOISE_FREQ"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["MIXER"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["ENV_FREQ"] = {
            address = 0x0D,
            size = 1,
            value = 0
        }
        p.registers["ENV_SHAPE"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        self.peripherals["Z80"] = {
            base = 0xA00000,
            type = "cpu",
            description = "Z80 Secondary CPU (Sound)",
            registers = {}
        }
        
        local p = self.peripherals["Z80"]
        p.registers["Z80_RESET"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["Z80_BUSREQ"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["Z80_STATUS"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        self.peripherals["BANK_REG"] = {
            base = 0xA12000,
            type = "memory",
            description = "Bank Register",
            registers = {}
        }
        
        local p = self.peripherals["BANK_REG"]
        p.registers["ROM_BANK"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["RAM_BANK"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        self.peripherals["HW_VERSION"] = {
            base = 0xA10001,
            type = "system",
            description = "Hardware Version",
            registers = {}
        }
        
        local p = self.peripherals["HW_VERSION"]
        p.registers["VERSION"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        self.peripherals["CONTROLLER1"] = {
            base = 0xA10003,
            type = "input",
            description = "Controller Port 1",
            registers = {}
        }
        
        local p = self.peripherals["CONTROLLER1"]
        p.registers["DATA"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["CTRL"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        self.peripherals["CONTROLLER2"] = {
            base = 0xA10005,
            type = "input",
            description = "Controller Port 2",
            registers = {}
        }
        
        local p = self.peripherals["CONTROLLER2"]
        p.registers["DATA"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["CTRL"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        self.peripherals["EXT_PORT"] = {
            base = 0xA10007,
            type = "io",
            description = "External Port",
            registers = {}
        }
        
        local p = self.peripherals["EXT_PORT"]
        p.registers["DATA"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        self.peripherals["DMA"] = {
            base = 0xA10008,
            type = "dma",
            description = "DMA Controller",
            registers = {}
        }
        
        local p = self.peripherals["DMA"]
        p.registers["SOURCE"] = {
            address = 0x00,
            size = 4,
            value = 0
        }
        p.registers["DEST"] = {
            address = 0x04,
            size = 4,
            value = 0
        }
        p.registers["COUNT"] = {
            address = 0x08,
            size = 2,
            value = 0
        }
        p.registers["CTRL"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER"] = {
            base = 0xA1000E,
            type = "timer",
            description = "Hardware Timer",
            registers = {}
        }
        
        local p = self.peripherals["TIMER"]
        p.registers["H_COUNTER"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["V_COUNTER"] = {
            address = 0x04,
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
            name = Motorola_68000.DEVICE_NAME,
            manufacturer = Motorola_68000.MANUFACTURER,
            family = Motorola_68000.FAMILY,
            version = Motorola_68000.VERSION,
            architecture = Motorola_68000.ARCHITECTURE,
            bits = Motorola_68000.BITS,
            clock_frequency = Motorola_68000.CLOCK_FREQUENCY
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
        return string.format("Motorola_68000(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Motorola_68000.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Motorola_68000.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Motorola_68000.print_device_info(device)
    device = device or Motorola_68000.new()
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

function Motorola_68000.print_registers(device)
    device = device or Motorola_68000.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Motorola_68000.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Motorola_68000.example()
    print("=== Motorola-68000设备示例 ===")
    
    -- 创建设备实例
    local device = Motorola_68000.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Motorola_68000.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["D0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("D0", 0x55)
        print("写入 D0: " .. Motorola_68000.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("D0")
        print("读取 D0: " .. Motorola_68000.hex(value))
        
        -- 位操作
        device:set_bit("D0", 0, true)
        local bit0 = device:get_bit("D0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Motorola_68000.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Motorola_68000.lua$") then
    Motorola_68000.example()
end

return Motorola_68000

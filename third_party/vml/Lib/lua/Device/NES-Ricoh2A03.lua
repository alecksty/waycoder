--[[
  Ricoh-2A03设备定义 - Lua模块
  生成自: Ricoh/MOS-6502/Ricoh-2A03
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: NES (Famicom) main processor - 8-bit MOS 6502 variant with audio/video support
  CPU架构: MOS-6502
  位宽: 8位
  时钟频率: 10765930 Hz
]]

local Ricoh_2A03 = {}

-- 设备信息
Ricoh_2A03.DEVICE_NAME = "Ricoh-2A03"
Ricoh_2A03.MANUFACTURER = "Ricoh"
Ricoh_2A03.FAMILY = "MOS-6502"
Ricoh_2A03.VERSION = "1.0"
Ricoh_2A03.ARCHITECTURE = "MOS-6502"
Ricoh_2A03.BITS = 8
Ricoh_2A03.CLOCK_FREQUENCY = 10765930

-- 寄存器地址定义
Ricoh_2A03.A_ADDR = 0x00  -- Accumulator
Ricoh_2A03.X_ADDR = 0x01  -- X Index
Ricoh_2A03.Y_ADDR = 0x02  -- Y Index
Ricoh_2A03.SP_ADDR = 0x03  -- Stack Pointer
Ricoh_2A03.PC_ADDR = 0x04  -- Program Counter (16-bit)
Ricoh_2A03.P_ADDR = 0x06  -- Processor Status
Ricoh_2A03.P_C_BIT = 0  -- Carry
Ricoh_2A03.P_Z_BIT = 1  -- Zero
Ricoh_2A03.P_I_BIT = 2  -- Interrupt Disable
Ricoh_2A03.P_D_BIT = 3  -- Decimal Mode
Ricoh_2A03.P_B_BIT = 4  -- Break
Ricoh_2A03.P_U_BIT = 5  -- Unused
Ricoh_2A03.P_V_BIT = 6  -- Overflow
Ricoh_2A03.P_N_BIT = 7  -- Negative

-- 内存段定义
Ricoh_2A03.CPU_RAM_START = 0x0000
Ricoh_2A03.CPU_RAM_END = 0x07FF
Ricoh_2A03.CPU_RAM_SIZE = 2048  -- CPU 2KB RAM (mirrored)
Ricoh_2A03.PPU_REGISTERS_START = 0x2000
Ricoh_2A03.PPU_REGISTERS_END = 0x3FFF
Ricoh_2A03.PPU_REGISTERS_SIZE = 8192  -- PPU Registers (mirrored every 8 bytes)
Ricoh_2A03.APU_REGISTERS_START = 0x4000
Ricoh_2A03.APU_REGISTERS_END = 0x401F
Ricoh_2A03.APU_REGISTERS_SIZE = 32  -- APU and I/O Registers
Ricoh_2A03.EXPANSION_START = 0x4020
Ricoh_2A03.EXPANSION_END = 0x5FFF
Ricoh_2A03.EXPANSION_SIZE = 8160  -- Expansion ROM
Ricoh_2A03.SRAM_START = 0x6000
Ricoh_2A03.SRAM_END = 0x7FFF
Ricoh_2A03.SRAM_SIZE = 8192  -- Save RAM
Ricoh_2A03.PRG_ROM_LOW_START = 0x8000
Ricoh_2A03.PRG_ROM_LOW_END = 0xBFFF
Ricoh_2A03.PRG_ROM_LOW_SIZE = 16384  -- PRG ROM Lower Bank (16KB)
Ricoh_2A03.PRG_ROM_HIGH_START = 0xC000
Ricoh_2A03.PRG_ROM_HIGH_END = 0xFFFF
Ricoh_2A03.PRG_ROM_HIGH_SIZE = 16384  -- PRG ROM Higher Bank (16KB)

-- 外设定义
-- Picture Processing Unit
Ricoh_2A03.PPU_BASE = 0x2000
Ricoh_2A03.PPU_PPUCTRL_ADDR = 0x2000
Ricoh_2A03.PPU_PPUMASK_ADDR = 0x2001
Ricoh_2A03.PPU_PPUSTATUS_ADDR = 0x2002
Ricoh_2A03.PPU_OAMADDR_ADDR = 0x2003
Ricoh_2A03.PPU_OAMDATA_ADDR = 0x2004
Ricoh_2A03.PPU_PPUSCROLL_ADDR = 0x2005
Ricoh_2A03.PPU_PPUADDR_ADDR = 0x2006
Ricoh_2A03.PPU_PPUDATA_ADDR = 0x2007
-- Audio Processing Unit
Ricoh_2A03.APU_BASE = 0x4000
Ricoh_2A03.APU_PULSE1_VOL_ADDR = 0x4000
Ricoh_2A03.APU_PULSE1_SWEEP_ADDR = 0x4001
Ricoh_2A03.APU_PULSE1_LO_ADDR = 0x4002
Ricoh_2A03.APU_PULSE1_HI_ADDR = 0x4003
Ricoh_2A03.APU_PULSE2_VOL_ADDR = 0x4004
Ricoh_2A03.APU_PULSE2_SWEEP_ADDR = 0x4005
Ricoh_2A03.APU_PULSE2_LO_ADDR = 0x4006
Ricoh_2A03.APU_PULSE2_HI_ADDR = 0x4007
Ricoh_2A03.APU_TRIANGLE_ADDR = 0x4008
Ricoh_2A03.APU_TRIANGLE_HI_ADDR = 0x400B
Ricoh_2A03.APU_NOISE_VOL_ADDR = 0x400C
Ricoh_2A03.APU_NOISE_HI_ADDR = 0x400E
Ricoh_2A03.APU_NOISE_LENGTH_ADDR = 0x400F
Ricoh_2A03.APU_DMC_RATE_ADDR = 0x4010
Ricoh_2A03.APU_DMC_RAW_ADDR = 0x4011
Ricoh_2A03.APU_DMC_START_ADDR = 0x4012
Ricoh_2A03.APU_DMC_LENGTH_ADDR = 0x4013
Ricoh_2A03.APU_OAMDMA_ADDR = 0x4014
Ricoh_2A03.APU_SNDCHN_ADDR = 0x4015
Ricoh_2A03.APU_JOY1_ADDR = 0x4016
Ricoh_2A03.APU_JOY2_ADDR = 0x4017
-- Controller Port 1
Ricoh_2A03.INPUT1_BASE = 0x4016
Ricoh_2A03.INPUT1_JOYPAD1_ADDR = 0x4016
-- Controller Port 2
Ricoh_2A03.INPUT2_BASE = 0x4017
Ricoh_2A03.INPUT2_JOYPAD2_ADDR = 0x4017

-- 中断向量定义
Ricoh_2A03.INT_RESET = 0  -- Reset
Ricoh_2A03.INT_NMI = 1  -- Non-Maskable Interrupt (VBlank)
Ricoh_2A03.INT_IRQ = 2  -- IRQ / BRK

-- 设备类
function Ricoh_2A03.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["A"] = {
            address = 0x00,
            size = 1,
            access = "rw",
            description = "Accumulator",
            value = 0
        }
        self.registers["X"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "X Index",
            value = 0
        }
        self.registers["Y"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "Y Index",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x04,
            size = 2,
            access = "rw",
            description = "Program Counter (16-bit)",
            value = 0
        }
        self.registers["P"] = {
            address = 0x06,
            size = 1,
            access = "rw",
            description = "Processor Status",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PPU"] = {
            base = 0x2000,
            type = "video",
            description = "Picture Processing Unit",
            registers = {}
        }
        
        local p = self.peripherals["PPU"]
        p.registers["PPUCTRL"] = {
            address = 0x2000,
            size = 1,
            value = 0
        }
        p.registers["PPUMASK"] = {
            address = 0x2001,
            size = 1,
            value = 0
        }
        p.registers["PPUSTATUS"] = {
            address = 0x2002,
            size = 1,
            value = 0
        }
        p.registers["OAMADDR"] = {
            address = 0x2003,
            size = 1,
            value = 0
        }
        p.registers["OAMDATA"] = {
            address = 0x2004,
            size = 1,
            value = 0
        }
        p.registers["PPUSCROLL"] = {
            address = 0x2005,
            size = 1,
            value = 0
        }
        p.registers["PPUADDR"] = {
            address = 0x2006,
            size = 1,
            value = 0
        }
        p.registers["PPUDATA"] = {
            address = 0x2007,
            size = 1,
            value = 0
        }
        self.peripherals["APU"] = {
            base = 0x4000,
            type = "audio",
            description = "Audio Processing Unit",
            registers = {}
        }
        
        local p = self.peripherals["APU"]
        p.registers["PULSE1_VOL"] = {
            address = 0x4000,
            size = 1,
            value = 0
        }
        p.registers["PULSE1_SWEEP"] = {
            address = 0x4001,
            size = 1,
            value = 0
        }
        p.registers["PULSE1_LO"] = {
            address = 0x4002,
            size = 1,
            value = 0
        }
        p.registers["PULSE1_HI"] = {
            address = 0x4003,
            size = 1,
            value = 0
        }
        p.registers["PULSE2_VOL"] = {
            address = 0x4004,
            size = 1,
            value = 0
        }
        p.registers["PULSE2_SWEEP"] = {
            address = 0x4005,
            size = 1,
            value = 0
        }
        p.registers["PULSE2_LO"] = {
            address = 0x4006,
            size = 1,
            value = 0
        }
        p.registers["PULSE2_HI"] = {
            address = 0x4007,
            size = 1,
            value = 0
        }
        p.registers["TRIANGLE"] = {
            address = 0x4008,
            size = 1,
            value = 0
        }
        p.registers["TRIANGLE_HI"] = {
            address = 0x400B,
            size = 1,
            value = 0
        }
        p.registers["NOISE_VOL"] = {
            address = 0x400C,
            size = 1,
            value = 0
        }
        p.registers["NOISE_HI"] = {
            address = 0x400E,
            size = 1,
            value = 0
        }
        p.registers["NOISE_LENGTH"] = {
            address = 0x400F,
            size = 1,
            value = 0
        }
        p.registers["DMC_RATE"] = {
            address = 0x4010,
            size = 1,
            value = 0
        }
        p.registers["DMC_RAW"] = {
            address = 0x4011,
            size = 1,
            value = 0
        }
        p.registers["DMC_START"] = {
            address = 0x4012,
            size = 1,
            value = 0
        }
        p.registers["DMC_LENGTH"] = {
            address = 0x4013,
            size = 1,
            value = 0
        }
        p.registers["OAMDMA"] = {
            address = 0x4014,
            size = 1,
            value = 0
        }
        p.registers["SNDCHN"] = {
            address = 0x4015,
            size = 1,
            value = 0
        }
        p.registers["JOY1"] = {
            address = 0x4016,
            size = 1,
            value = 0
        }
        p.registers["JOY2"] = {
            address = 0x4017,
            size = 1,
            value = 0
        }
        self.peripherals["INPUT1"] = {
            base = 0x4016,
            type = "input",
            description = "Controller Port 1",
            registers = {}
        }
        
        local p = self.peripherals["INPUT1"]
        p.registers["JOYPAD1"] = {
            address = 0x4016,
            size = 1,
            value = 0
        }
        self.peripherals["INPUT2"] = {
            base = 0x4017,
            type = "input",
            description = "Controller Port 2",
            registers = {}
        }
        
        local p = self.peripherals["INPUT2"]
        p.registers["JOYPAD2"] = {
            address = 0x4017,
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
            name = Ricoh_2A03.DEVICE_NAME,
            manufacturer = Ricoh_2A03.MANUFACTURER,
            family = Ricoh_2A03.FAMILY,
            version = Ricoh_2A03.VERSION,
            architecture = Ricoh_2A03.ARCHITECTURE,
            bits = Ricoh_2A03.BITS,
            clock_frequency = Ricoh_2A03.CLOCK_FREQUENCY
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
        return string.format("Ricoh_2A03(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Ricoh_2A03.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Ricoh_2A03.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Ricoh_2A03.print_device_info(device)
    device = device or Ricoh_2A03.new()
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

function Ricoh_2A03.print_registers(device)
    device = device or Ricoh_2A03.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Ricoh_2A03.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Ricoh_2A03.example()
    print("=== Ricoh-2A03设备示例 ===")
    
    -- 创建设备实例
    local device = Ricoh_2A03.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Ricoh_2A03.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Ricoh_2A03.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Ricoh_2A03.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Ricoh_2A03.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Ricoh_2A03.lua$") then
    Ricoh_2A03.example()
end

return Ricoh_2A03

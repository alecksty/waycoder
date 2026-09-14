--[[
  Zilog-Z80设备定义 - Lua模块
  生成自: Zilog/Z80/Zilog-Z80
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Sega Master System (Mark III) main processor - Zilog Z80A @ 3.58MHz
  CPU架构: Z80
  位宽: 8位
  时钟频率: 3580000 Hz
]]

local Zilog_Z80 = {}

-- 设备信息
Zilog_Z80.DEVICE_NAME = "Zilog-Z80"
Zilog_Z80.MANUFACTURER = "Zilog"
Zilog_Z80.FAMILY = "Z80"
Zilog_Z80.VERSION = "1.0"
Zilog_Z80.ARCHITECTURE = "Z80"
Zilog_Z80.BITS = 8
Zilog_Z80.CLOCK_FREQUENCY = 3580000

-- 寄存器地址定义
Zilog_Z80.A_ADDR = 0x00  -- Accumulator
Zilog_Z80.F_ADDR = 0x01  -- Flags Register
Zilog_Z80.F_C_BIT = 0  -- Carry
Zilog_Z80.F_N_BIT = 1  -- Subtract
Zilog_Z80.F_P_BIT = 2  -- Parity/Overflow
Zilog_Z80.F_H_BIT = 4  -- Half Carry
Zilog_Z80.F_Z_BIT = 6  -- Zero
Zilog_Z80.F_S_BIT = 7  -- Sign/Negative
Zilog_Z80.B_ADDR = 0x02  -- B Register
Zilog_Z80.C_ADDR = 0x03  -- C Register
Zilog_Z80.D_ADDR = 0x04  -- D Register
Zilog_Z80.E_ADDR = 0x05  -- E Register
Zilog_Z80.H_ADDR = 0x06  -- H Register
Zilog_Z80.L_ADDR = 0x07  -- L Register
Zilog_Z80.AF_ADDR = 0x08  -- Alternate AF
Zilog_Z80.BC_ADDR = 0x0A  -- Alternate BC
Zilog_Z80.DE_ADDR = 0x0C  -- Alternate DE
Zilog_Z80.HL_ADDR = 0x0E  -- Alternate HL
Zilog_Z80.IX_ADDR = 0x10  -- Index Register X
Zilog_Z80.IY_ADDR = 0x12  -- Index Register Y
Zilog_Z80.SP_ADDR = 0x14  -- Stack Pointer
Zilog_Z80.PC_ADDR = 0x16  -- Program Counter
Zilog_Z80.I_ADDR = 0x18  -- Interrupt Vector Register
Zilog_Z80.R_ADDR = 0x19  -- Memory Refresh Register
Zilog_Z80.IM_ADDR = 0x1A  -- Interrupt Mode (0/1/2)

-- 内存段定义
Zilog_Z80.WRAM_START = 0xC000
Zilog_Z80.WRAM_END = 0xC7FF
Zilog_Z80.WRAM_SIZE = 2048  -- Work RAM (2KB internal)
Zilog_Z80.WRAM_SHADOW_START = 0xE000
Zilog_Z80.WRAM_SHADOW_END = 0xE7FF
Zilog_Z80.WRAM_SHADOW_SIZE = 2048  -- Work RAM Shadow (Echo RAM)
Zilog_Z80.VRAM_START = 0x4000
Zilog_Z80.VRAM_END = 0x7FFF
Zilog_Z80.VRAM_SIZE = 16384  -- Video RAM (16KB)
Zilog_Z80.SRAM_START = 0x8000
Zilog_Z80.SRAM_END = 0xBFFF
Zilog_Z80.SRAM_SIZE = 16384  -- Cartridge SRAM (if present)
Zilog_Z80.CART_ROM_START = 0x0000
Zilog_Z80.CART_ROM_END = 0x7FFF
Zilog_Z80.CART_ROM_SIZE = 32768  -- Cartridge ROM (up to 48KB)
Zilog_Z80.BIOS_START = 0x0000
Zilog_Z80.BIOS_END = 0x1FFF
Zilog_Z80.BIOS_SIZE = 8192  -- BIOS ROM (Master System built-in, 8KB)
Zilog_Z80.IO_REGS_START = 0x3F00
Zilog_Z80.IO_REGS_END = 0x3FFF
Zilog_Z80.IO_REGS_SIZE = 256  -- I/O Register Area

-- 外设定义
-- Video Display Processor (TMS9918A variant)
Zilog_Z80.VDP_BASE = 0xBE
Zilog_Z80.VDP_VDP_CTRL_ADDR = 0xBF
Zilog_Z80.VDP_VDP_DATA_ADDR = 0xBE
Zilog_Z80.VDP_VDP_STATUS_ADDR = 0xBF
Zilog_Z80.VDP_VDP_STATUS_FIFO_FULL_BIT = 0  -- VRAM to CPU Transfer Pending
Zilog_Z80.VDP_VDP_STATUS_FIFO_EMPTY_BIT = 1  -- VRAM Write FIFO Empty
Zilog_Z80.VDP_VDP_STATUS_INT_FLAG_BIT = 7  -- V-Blank / Sprite Collision Flag
Zilog_Z80.VDP_R0_ADDR = 0x00
Zilog_Z80.VDP_R0_M3_BIT = 0  -- Mode 3 Enable
Zilog_Z80.VDP_R0_M2_BIT = 1  -- Mode 2 Enable
Zilog_Z80.VDP_R0_M1_BIT = 2  -- Mode 1 Enable
Zilog_Z80.VDP_R0_DISPLAY_DISABLE_BIT = 3  -- Display Disable (1=blank screen)
Zilog_Z80.VDP_R0_VIRQ_EN_BIT = 4  -- Vertical Interrupt Enable
Zilog_Z80.VDP_R0_M4_BIT = 5  -- Mode 4 Enable (SMS2 only)
Zilog_Z80.VDP_R0_SPRITE_SHIFT_BIT = 6  -- Sprite Double Height
Zilog_Z80.VDP_R0_HVC_LATCH_BIT = 7  -- H-Counter Latch Enable
Zilog_Z80.VDP_R1_ADDR = 0x01
Zilog_Z80.VDP_R1_DISPLAY_BIT = 3  -- Display Enable (1=active)
Zilog_Z80.VDP_R1_FRAME_INT_BIT = 4  -- Frame Interrupt (V-Blank) Enable
Zilog_Z80.VDP_R1_M4_BIT = 5  -- Mode 4 (256-color)
Zilog_Z80.VDP_R1_SMS_MODE_BIT = 6  -- SMS Display Mode (vs Coleco)
Zilog_Z80.VDP_R1_EXT_VIDEO_BIT = 7  -- External Video Enable
Zilog_Z80.VDP_R2_ADDR = 0x02
Zilog_Z80.VDP_R3_ADDR = 0x03
Zilog_Z80.VDP_R4_ADDR = 0x04
Zilog_Z80.VDP_R5_ADDR = 0x05
Zilog_Z80.VDP_R6_ADDR = 0x06
Zilog_Z80.VDP_R7_ADDR = 0x07
Zilog_Z80.VDP_R8_ADDR = 0x08
Zilog_Z80.VDP_R8_HSCROLL_EN_BIT = 0  -- Horizontal Scroll Enable
Zilog_Z80.VDP_R8_VSCROLL_EN_BIT = 1  -- Vertical Scroll Enable
Zilog_Z80.VDP_R8_LINE_INT_BIT = 4  -- Line Interrupt Enable
Zilog_Z80.VDP_R8_VSCROLL_2X_BIT = 7  -- Vertical Scroll 2x Speed
Zilog_Z80.VDP_R9_ADDR = 0x09
Zilog_Z80.VDP_R10_ADDR = 0x0A
Zilog_Z80.VDP_R11_ADDR = 0x0B
Zilog_Z80.VDP_R12_ADDR = 0x0C
Zilog_Z80.VDP_R13_ADDR = 0x0D
Zilog_Z80.VDP_R14_ADDR = 0x0E
Zilog_Z80.VDP_R15_ADDR = 0x0F
Zilog_Z80.VDP_VCOUNTER_ADDR = 0x7E
Zilog_Z80.VDP_HCOUNTER_ADDR = 0x7F
-- SN76489 Programmable Sound Generator (3 Square + 1 Noise)
Zilog_Z80.PSG_BASE = 0x7F
Zilog_Z80.PSG_CH0_FREQ_ADDR = 0x00
Zilog_Z80.PSG_CH1_FREQ_ADDR = 0x02
Zilog_Z80.PSG_CH2_FREQ_ADDR = 0x04
Zilog_Z80.PSG_CH3_CONFIG_ADDR = 0x06
Zilog_Z80.PSG_CH3_CONFIG_TYPE_BIT = 0  -- Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
Zilog_Z80.PSG_CH3_CONFIG_VOLUME_BIT = 0  -- Volume (0-15)
Zilog_Z80.PSG_CH0_VOLUME_ADDR = 0x01
Zilog_Z80.PSG_CH1_VOLUME_ADDR = 0x03
Zilog_Z80.PSG_CH2_VOLUME_ADDR = 0x05
-- I/O Port Registers
Zilog_Z80.PORTS_BASE = 0x3F
Zilog_Z80.PORTS_PORT_A_ADDR = 0x3F
Zilog_Z80.PORTS_PORT_A_UP_BIT = 0  -- Up (0=pressed)
Zilog_Z80.PORTS_PORT_A_DOWN_BIT = 1  -- Down (0=pressed)
Zilog_Z80.PORTS_PORT_A_LEFT_BIT = 2  -- Left (0=pressed)
Zilog_Z80.PORTS_PORT_A_RIGHT_BIT = 3  -- Right (0=pressed)
Zilog_Z80.PORTS_PORT_A_TR_BIT = 4  -- Button TR (0=pressed)
Zilog_Z80.PORTS_PORT_A_TL_BIT = 5  -- Button TL (0=pressed)
Zilog_Z80.PORTS_PORT_B_ADDR = 0x3F
Zilog_Z80.PORTS_PORT_B_UP_BIT = 0  -- Up (0=pressed)
Zilog_Z80.PORTS_PORT_B_DOWN_BIT = 1  -- Down (0=pressed)
Zilog_Z80.PORTS_PORT_B_LEFT_BIT = 2  -- Left (0=pressed)
Zilog_Z80.PORTS_PORT_B_RIGHT_BIT = 3  -- Right (0=pressed)
Zilog_Z80.PORTS_PORT_B_TR_BIT = 4  -- Button TR (0=pressed)
Zilog_Z80.PORTS_PORT_B_TL_BIT = 5  -- Button TL (0=pressed)
Zilog_Z80.PORTS_PORT_A_DDR_ADDR = 0x3F
Zilog_Z80.PORTS_PORT_B_DDR_ADDR = 0x3F
-- Sega Mapper (Memory Bank Switching)
Zilog_Z80.SEGAMAPPER_BASE = 0xFFFD
Zilog_Z80.SEGAMAPPER_ROM_BANK0_ADDR = 0xFFFD
Zilog_Z80.SEGAMAPPER_ROM_BANK1_ADDR = 0xFFFE
Zilog_Z80.SEGAMAPPER_ROM_BANK2_ADDR = 0xFFFF
-- Memory Mapper Control
Zilog_Z80.MAPPER_BASE = 0xFFFF
Zilog_Z80.MAPPER_SRAM_BANK_ADDR = 0xFFF8

-- 中断向量定义
Zilog_Z80.INT_NMI = 0  -- Non-Maskable Interrupt (Pause button / V-Blank)
Zilog_Z80.INT_INT_VBLANK = 1  -- V-Blank Interrupt (Frame end)
Zilog_Z80.INT_INT_LINE = 2  -- Scanline Interrupt (Line counter match)
Zilog_Z80.INT_INT_EXT = 3  -- External I/O Interrupt

-- 引脚定义
Zilog_Z80.PIN_A = 1  -- Power Supply
Zilog_Z80.PIN_GND = 2  -- Ground
Zilog_Z80.PIN_PHI = 3  -- System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
Zilog_Z80.PIN_RESET = 4  -- Reset (active low)
Zilog_Z80.PIN_M1 = 5  -- Machine Cycle 1 (instruction fetch)
Zilog_Z80.PIN_MREQ = 6  -- Memory Request
Zilog_Z80.PIN_IORQ = 7  -- I/O Request
Zilog_Z80.PIN_RD = 8  -- Read Strobe
Zilog_Z80.PIN_WR = 9  -- Write Strobe
Zilog_Z80.PIN_HALT = 10  -- Halt State
Zilog_Z80.PIN_WAIT = 11  -- Wait State Request
Zilog_Z80.PIN_INT = 12  -- Interrupt Request (active low)
Zilog_Z80.PIN_NMI = 13  -- Non-Maskable Interrupt (active low)
Zilog_Z80.PIN_BUSRQ = 14  -- Bus Request (active low)
Zilog_Z80.PIN_BUSAK = 15  -- Bus Acknowledge (active low)
Zilog_Z80.PIN_A0 = 16  -- Address Bus Bit 0
Zilog_Z80.PIN_A1 = 17  -- Address Bus Bit 1
Zilog_Z80.PIN_A2 = 18  -- Address Bus Bit 2
Zilog_Z80.PIN_A3 = 19  -- Address Bus Bit 3
Zilog_Z80.PIN_A4 = 20  -- Address Bus Bit 4
Zilog_Z80.PIN_A5 = 21  -- Address Bus Bit 5
Zilog_Z80.PIN_A6 = 22  -- Address Bus Bit 6
Zilog_Z80.PIN_A7 = 23  -- Address Bus Bit 7
Zilog_Z80.PIN_A8 = 24  -- Address Bus Bit 8
Zilog_Z80.PIN_A9 = 25  -- Address Bus Bit 9
Zilog_Z80.PIN_A10 = 26  -- Address Bus Bit 10
Zilog_Z80.PIN_A11 = 27  -- Address Bus Bit 11
Zilog_Z80.PIN_A12 = 28  -- Address Bus Bit 12
Zilog_Z80.PIN_A13 = 29  -- Address Bus Bit 13
Zilog_Z80.PIN_A14 = 30  -- Address Bus Bit 14
Zilog_Z80.PIN_A15 = 31  -- Address Bus Bit 15
Zilog_Z80.PIN_D0 = 32  -- Data Bus Bit 0
Zilog_Z80.PIN_D1 = 33  -- Data Bus Bit 1
Zilog_Z80.PIN_D2 = 34  -- Data Bus Bit 2
Zilog_Z80.PIN_D3 = 35  -- Data Bus Bit 3
Zilog_Z80.PIN_D4 = 36  -- Data Bus Bit 4
Zilog_Z80.PIN_D5 = 37  -- Data Bus Bit 5
Zilog_Z80.PIN_D6 = 38  -- Data Bus Bit 6
Zilog_Z80.PIN_D7 = 39  -- Data Bus Bit 7
Zilog_Z80.PIN_AUDIO_OUT = 40  -- Audio Output
Zilog_Z80.PIN_VIDEO_SYNC = 41  -- Composite Video Sync
Zilog_Z80.PIN_VIDEO_OUT = 42  -- Composite Video Output

-- 设备类
function Zilog_Z80.new(memory_base)
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
        self.registers["F"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "Flags Register",
            value = 0
        }
        self.registers["B"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "B Register",
            value = 0
        }
        self.registers["C"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "C Register",
            value = 0
        }
        self.registers["D"] = {
            address = 0x04,
            size = 1,
            access = "rw",
            description = "D Register",
            value = 0
        }
        self.registers["E"] = {
            address = 0x05,
            size = 1,
            access = "rw",
            description = "E Register",
            value = 0
        }
        self.registers["H"] = {
            address = 0x06,
            size = 1,
            access = "rw",
            description = "H Register",
            value = 0
        }
        self.registers["L"] = {
            address = 0x07,
            size = 1,
            access = "rw",
            description = "L Register",
            value = 0
        }
        self.registers["AF_"] = {
            address = 0x08,
            size = 2,
            access = "rw",
            description = "Alternate AF",
            value = 0
        }
        self.registers["BC_"] = {
            address = 0x0A,
            size = 2,
            access = "rw",
            description = "Alternate BC",
            value = 0
        }
        self.registers["DE_"] = {
            address = 0x0C,
            size = 2,
            access = "rw",
            description = "Alternate DE",
            value = 0
        }
        self.registers["HL_"] = {
            address = 0x0E,
            size = 2,
            access = "rw",
            description = "Alternate HL",
            value = 0
        }
        self.registers["IX"] = {
            address = 0x10,
            size = 2,
            access = "rw",
            description = "Index Register X",
            value = 0
        }
        self.registers["IY"] = {
            address = 0x12,
            size = 2,
            access = "rw",
            description = "Index Register Y",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x14,
            size = 2,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x16,
            size = 2,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["I"] = {
            address = 0x18,
            size = 1,
            access = "rw",
            description = "Interrupt Vector Register",
            value = 0
        }
        self.registers["R"] = {
            address = 0x19,
            size = 1,
            access = "rw",
            description = "Memory Refresh Register",
            value = 0
        }
        self.registers["IM"] = {
            address = 0x1A,
            size = 1,
            access = "rw",
            description = "Interrupt Mode (0/1/2)",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["VDP"] = {
            base = 0xBE,
            type = "video",
            description = "Video Display Processor (TMS9918A variant)",
            registers = {}
        }
        
        local p = self.peripherals["VDP"]
        p.registers["VDP_CTRL"] = {
            address = 0xBF,
            size = 1,
            value = 0
        }
        p.registers["VDP_DATA"] = {
            address = 0xBE,
            size = 1,
            value = 0
        }
        p.registers["VDP_STATUS"] = {
            address = 0xBF,
            size = 1,
            value = 0
        }
        p.registers["R0"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["R1"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["R2"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["R3"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["R4"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["R5"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["R6"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["R7"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["R8"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["R9"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["R10"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["R11"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        p.registers["R12"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        p.registers["R13"] = {
            address = 0x0D,
            size = 1,
            value = 0
        }
        p.registers["R14"] = {
            address = 0x0E,
            size = 1,
            value = 0
        }
        p.registers["R15"] = {
            address = 0x0F,
            size = 1,
            value = 0
        }
        p.registers["VCOUNTER"] = {
            address = 0x7E,
            size = 1,
            value = 0
        }
        p.registers["HCOUNTER"] = {
            address = 0x7F,
            size = 1,
            value = 0
        }
        self.peripherals["PSG"] = {
            base = 0x7F,
            type = "audio",
            description = "SN76489 Programmable Sound Generator (3 Square + 1 Noise)",
            registers = {}
        }
        
        local p = self.peripherals["PSG"]
        p.registers["CH0_FREQ"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["CH1_FREQ"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["CH2_FREQ"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["CH3_CONFIG"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["CH0_VOLUME"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["CH1_VOLUME"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["CH2_VOLUME"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        self.peripherals["PORTS"] = {
            base = 0x3F,
            type = "io",
            description = "I/O Port Registers",
            registers = {}
        }
        
        local p = self.peripherals["PORTS"]
        p.registers["PORT_A"] = {
            address = 0x3F,
            size = 1,
            value = 0
        }
        p.registers["PORT_B"] = {
            address = 0x3F,
            size = 1,
            value = 0
        }
        p.registers["PORT_A_DDR"] = {
            address = 0x3F,
            size = 1,
            value = 0
        }
        p.registers["PORT_B_DDR"] = {
            address = 0x3F,
            size = 1,
            value = 0
        }
        self.peripherals["SegaMapper"] = {
            base = 0xFFFD,
            type = "memory",
            description = "Sega Mapper (Memory Bank Switching)",
            registers = {}
        }
        
        local p = self.peripherals["SegaMapper"]
        p.registers["ROM_BANK0"] = {
            address = 0xFFFD,
            size = 1,
            value = 0
        }
        p.registers["ROM_BANK1"] = {
            address = 0xFFFE,
            size = 1,
            value = 0
        }
        p.registers["ROM_BANK2"] = {
            address = 0xFFFF,
            size = 1,
            value = 0
        }
        self.peripherals["MAPPER"] = {
            base = 0xFFFF,
            type = "memory",
            description = "Memory Mapper Control",
            registers = {}
        }
        
        local p = self.peripherals["MAPPER"]
        p.registers["SRAM_BANK"] = {
            address = 0xFFF8,
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
            name = Zilog_Z80.DEVICE_NAME,
            manufacturer = Zilog_Z80.MANUFACTURER,
            family = Zilog_Z80.FAMILY,
            version = Zilog_Z80.VERSION,
            architecture = Zilog_Z80.ARCHITECTURE,
            bits = Zilog_Z80.BITS,
            clock_frequency = Zilog_Z80.CLOCK_FREQUENCY
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
        return string.format("Zilog_Z80(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Zilog_Z80.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Zilog_Z80.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Zilog_Z80.print_device_info(device)
    device = device or Zilog_Z80.new()
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

function Zilog_Z80.print_registers(device)
    device = device or Zilog_Z80.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Zilog_Z80.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Zilog_Z80.example()
    print("=== Zilog-Z80设备示例 ===")
    
    -- 创建设备实例
    local device = Zilog_Z80.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Zilog_Z80.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Zilog_Z80.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Zilog_Z80.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Zilog_Z80.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Zilog_Z80.lua$") then
    Zilog_Z80.example()
end

return Zilog_Z80

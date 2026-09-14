--[[
  Sharp-LR35902设备定义 - Lua模块
  生成自: Sharp/Z80/Sharp-LR35902
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Game Boy (DMG-01) main processor - Sharp LR35902 (Z80-like) @ 4.19MHz
  CPU架构: LR35902
  位宽: 8位
  时钟频率: 4194304 Hz
]]

local Sharp_LR35902 = {}

-- 设备信息
Sharp_LR35902.DEVICE_NAME = "Sharp-LR35902"
Sharp_LR35902.MANUFACTURER = "Sharp"
Sharp_LR35902.FAMILY = "Z80"
Sharp_LR35902.VERSION = "1.0"
Sharp_LR35902.ARCHITECTURE = "LR35902"
Sharp_LR35902.BITS = 8
Sharp_LR35902.CLOCK_FREQUENCY = 4194304

-- 寄存器地址定义
Sharp_LR35902.A_ADDR = 0x00  -- Accumulator
Sharp_LR35902.B_ADDR = 0x01  -- B Register
Sharp_LR35902.C_ADDR = 0x02  -- C Register
Sharp_LR35902.D_ADDR = 0x03  -- D Register
Sharp_LR35902.E_ADDR = 0x04  -- E Register
Sharp_LR35902.F_ADDR = 0x05  -- Flags Register
Sharp_LR35902.F_C_BIT = 4  -- Carry
Sharp_LR35902.F_H_BIT = 5  -- Half Carry
Sharp_LR35902.F_N_BIT = 6  -- Subtract
Sharp_LR35902.F_Z_BIT = 7  -- Zero
Sharp_LR35902.H_ADDR = 0x06  -- H Register
Sharp_LR35902.L_ADDR = 0x07  -- L Register
Sharp_LR35902.AF_ADDR = 0x08  -- AF Register Pair (Accumulator + Flags)
Sharp_LR35902.BC_ADDR = 0x0A  -- BC Register Pair
Sharp_LR35902.DE_ADDR = 0x0C  -- DE Register Pair
Sharp_LR35902.HL_ADDR = 0x0E  -- HL Register Pair
Sharp_LR35902.SP_ADDR = 0x10  -- Stack Pointer
Sharp_LR35902.PC_ADDR = 0x12  -- Program Counter

-- 内存段定义
Sharp_LR35902.WRAM_START = 0xC000
Sharp_LR35902.WRAM_END = 0xCFFF
Sharp_LR35902.WRAM_SIZE = 4096  -- Work RAM (4KB)
Sharp_LR35902.WRAM_SHADOW_START = 0xE000
Sharp_LR35902.WRAM_SHADOW_END = 0xEFFF
Sharp_LR35902.WRAM_SHADOW_SIZE = 4096  -- Work RAM Shadow (Echo RAM)
Sharp_LR35902.HRAM_START = 0xFF80
Sharp_LR35902.HRAM_END = 0xFFFE
Sharp_LR35902.HRAM_SIZE = 127  -- High RAM (127 bytes)
Sharp_LR35902.IO_REGISTERS_START = 0xFF00
Sharp_LR35902.IO_REGISTERS_END = 0xFF7F
Sharp_LR35902.IO_REGISTERS_SIZE = 128  -- I/O Registers
Sharp_LR35902.OAM_START = 0xFE00
Sharp_LR35902.OAM_END = 0xFE9F
Sharp_LR35902.OAM_SIZE = 160  -- Sprite Attribute Table (OAM)
Sharp_LR35902.VRAM_START = 0x8000
Sharp_LR35902.VRAM_END = 0x9FFF
Sharp_LR35902.VRAM_SIZE = 8192  -- Video RAM (8KB)
Sharp_LR35902.BG_MAP_1_START = 0x9800
Sharp_LR35902.BG_MAP_1_END = 0x9BFF
Sharp_LR35902.BG_MAP_1_SIZE = 1024  -- Background Map 1
Sharp_LR35902.BG_MAP_2_START = 0x9C00
Sharp_LR35902.BG_MAP_2_END = 0x9FFF
Sharp_LR35902.BG_MAP_2_SIZE = 1024  -- Background Map 2
Sharp_LR35902.ROM_BANK0_START = 0x0000
Sharp_LR35902.ROM_BANK0_END = 0x3FFF
Sharp_LR35902.ROM_BANK0_SIZE = 16384  -- ROM Bank 0 (Cartridge Header)
Sharp_LR35902.ROM_BANK1_START = 0x4000
Sharp_LR35902.ROM_BANK1_END = 0x7FFF
Sharp_LR35902.ROM_BANK1_SIZE = 16384  -- ROM Bank 1 (Switchable)
Sharp_LR35902.CART_RAM_START = 0xA000
Sharp_LR35902.CART_RAM_END = 0xBFFF
Sharp_LR35902.CART_RAM_SIZE = 8192  -- Cartridge RAM / MBC

-- 外设定义
-- LCD Controller / Picture Processing Unit
Sharp_LR35902.PPU_BASE = 0xFF40
Sharp_LR35902.PPU_LCDC_ADDR = 0xFF40
Sharp_LR35902.PPU_LCDC_BG_ENABLE_BIT = 0  -- Background Display Enable
Sharp_LR35902.PPU_LCDC_SPRITE_ENABLE_BIT = 1  -- Sprite Display Enable
Sharp_LR35902.PPU_LCDC_SPRITE_SIZE_BIT = 2  -- Sprite Size (0=8x8, 1=8x16)
Sharp_LR35902.PPU_LCDC_BG_TILE_MAP_BIT = 3  -- BG Tile Map Area (0=9800, 1=9C00)
Sharp_LR35902.PPU_LCDC_TILE_DATA_BIT = 4  -- Tile Data Area (0=8800, 1=8000)
Sharp_LR35902.PPU_LCDC_WINDOW_ENABLE_BIT = 5  -- Window Display Enable
Sharp_LR35902.PPU_LCDC_WINDOW_MAP_BIT = 6  -- Window Tile Map Area (0=9800, 1=9C00)
Sharp_LR35902.PPU_LCDC_LCD_ENABLE_BIT = 7  -- LCD Display Enable
Sharp_LR35902.PPU_STAT_ADDR = 0xFF41
Sharp_LR35902.PPU_STAT_MODE_BIT = 0  -- LCD Mode (0=H-Blank, 1=V-Blank, 2=OAM, 3=VRAM)
Sharp_LR35902.PPU_STAT_LYC_FLAG_BIT = 2  -- LY=LYC Compare Flag
Sharp_LR35902.PPU_STAT_HBLANK_IRQ_BIT = 3  -- H-Blank Interrupt Enable
Sharp_LR35902.PPU_STAT_VBLANK_IRQ_BIT = 4  -- V-Blank Interrupt Enable
Sharp_LR35902.PPU_STAT_OAM_IRQ_BIT = 5  -- OAM Interrupt Enable
Sharp_LR35902.PPU_STAT_LYC_IRQ_BIT = 6  -- LYC Interrupt Enable
Sharp_LR35902.PPU_SCY_ADDR = 0xFF42
Sharp_LR35902.PPU_SCX_ADDR = 0xFF43
Sharp_LR35902.PPU_LY_ADDR = 0xFF44
Sharp_LR35902.PPU_LYC_ADDR = 0xFF45
Sharp_LR35902.PPU_DMA_ADDR = 0xFF46
Sharp_LR35902.PPU_BGP_ADDR = 0xFF47
Sharp_LR35902.PPU_OBP0_ADDR = 0xFF48
Sharp_LR35902.PPU_OBP1_ADDR = 0xFF49
Sharp_LR35902.PPU_WY_ADDR = 0xFF4A
Sharp_LR35902.PPU_WX_ADDR = 0xFF4B
-- Audio Processing Unit
Sharp_LR35902.APU_BASE = 0xFF10
Sharp_LR35902.APU_NR10_ADDR = 0xFF10
Sharp_LR35902.APU_NR10_SWEEP_TIME_BIT = 0  -- Sweep Time
Sharp_LR35902.APU_NR10_SWEEP_INCREASE_BIT = 3  -- Sweep Increase/Decrease
Sharp_LR35902.APU_NR10_SWEEP_SHIFTS_BIT = 0  -- Sweep Number of Shifts
Sharp_LR35902.APU_NR11_ADDR = 0xFF11
Sharp_LR35902.APU_NR12_ADDR = 0xFF12
Sharp_LR35902.APU_NR13_ADDR = 0xFF13
Sharp_LR35902.APU_NR14_ADDR = 0xFF14
Sharp_LR35902.APU_NR21_ADDR = 0xFF16
Sharp_LR35902.APU_NR22_ADDR = 0xFF17
Sharp_LR35902.APU_NR23_ADDR = 0xFF18
Sharp_LR35902.APU_NR24_ADDR = 0xFF19
Sharp_LR35902.APU_NR30_ADDR = 0xFF1A
Sharp_LR35902.APU_NR31_ADDR = 0xFF1B
Sharp_LR35902.APU_NR32_ADDR = 0xFF1C
Sharp_LR35902.APU_NR33_ADDR = 0xFF1D
Sharp_LR35902.APU_NR34_ADDR = 0xFF1E
Sharp_LR35902.APU_NR41_ADDR = 0xFF20
Sharp_LR35902.APU_NR42_ADDR = 0xFF21
Sharp_LR35902.APU_NR43_ADDR = 0xFF22
Sharp_LR35902.APU_NR44_ADDR = 0xFF23
Sharp_LR35902.APU_NR50_ADDR = 0xFF24
Sharp_LR35902.APU_NR51_ADDR = 0xFF25
Sharp_LR35902.APU_NR52_ADDR = 0xFF26
Sharp_LR35902.APU_NR52_CH1_ON_BIT = 0  -- Channel 1 ON
Sharp_LR35902.APU_NR52_CH2_ON_BIT = 1  -- Channel 2 ON
Sharp_LR35902.APU_NR52_CH3_ON_BIT = 2  -- Channel 3 ON
Sharp_LR35902.APU_NR52_CH4_ON_BIT = 3  -- Channel 4 ON
Sharp_LR35902.APU_NR52_ALL_ON_BIT = 7  -- All Sound ON
-- Timer Unit
Sharp_LR35902.TIMER_BASE = 0xFF04
Sharp_LR35902.TIMER_DIV_ADDR = 0xFF04
Sharp_LR35902.TIMER_TIMA_ADDR = 0xFF05
Sharp_LR35902.TIMER_TMA_ADDR = 0xFF06
Sharp_LR35902.TIMER_TAC_ADDR = 0xFF07
Sharp_LR35902.TIMER_TAC_TIMER_ENABLE_BIT = 2  -- Timer Enable
Sharp_LR35902.TIMER_TAC_CLOCK_SEL_BIT = 0  -- Clock Select (00=4kHz, 01=262kHz, 10=65kHz, 11=16kHz)
-- Joypad Controller
Sharp_LR35902.JOYPAD_BASE = 0xFF00
Sharp_LR35902.JOYPAD_P1_ADDR = 0xFF00
Sharp_LR35902.JOYPAD_P1_A_BTN_BIT = 0  -- A Button (1=Pressed when selected)
Sharp_LR35902.JOYPAD_P1_B_BTN_BIT = 1  -- B Button (1=Pressed when selected)
Sharp_LR35902.JOYPAD_P1_SELECT_BIT = 2  -- Select Button (1=Pressed)
Sharp_LR35902.JOYPAD_P1_START_BIT = 3  -- Start Button (1=Pressed)
Sharp_LR35902.JOYPAD_P1_DIR_DOWN_BIT = 4  -- Direction Down (1=Pressed when selected)
Sharp_LR35902.JOYPAD_P1_DIR_UP_BIT = 5  -- Direction Up (1=Pressed when selected)
Sharp_LR35902.JOYPAD_P1_DIR_LEFT_BIT = 6  -- Direction Left (1=Pressed when selected)
Sharp_LR35902.JOYPAD_P1_DIR_RIGHT_BIT = 7  -- Direction Right (1=Pressed when selected)
-- Serial I/O (Link Cable)
Sharp_LR35902.SERIAL_BASE = 0xFF01
Sharp_LR35902.SERIAL_SB_ADDR = 0xFF01
Sharp_LR35902.SERIAL_SC_ADDR = 0xFF02
Sharp_LR35902.SERIAL_SC_TRANSFER_START_BIT = 7  -- Transfer Start
Sharp_LR35902.SERIAL_SC_CLOCK_SPEED_BIT = 1  -- Clock Select (0=External, 1=Internal 8192Hz)
-- Interrupt Flag Register
Sharp_LR35902.INTERRUPT_BASE = 0xFF0F
Sharp_LR35902.INTERRUPT_IF_ADDR = 0xFF0F
Sharp_LR35902.INTERRUPT_IF_VBLANK_BIT = 0  -- V-Blank Interrupt Request
Sharp_LR35902.INTERRUPT_IF_LCDC_BIT = 1  -- LCDC Status Interrupt Request
Sharp_LR35902.INTERRUPT_IF_TIMER_BIT = 2  -- Timer Overflow Interrupt Request
Sharp_LR35902.INTERRUPT_IF_SERIAL_BIT = 3  -- Serial Transfer Complete Interrupt Request
Sharp_LR35902.INTERRUPT_IF_JOYPAD_BIT = 4  -- Joypad Interrupt Request
-- Interrupt Enable Register
Sharp_LR35902.IE_BASE = 0xFFFF
Sharp_LR35902.IE_IE_ADDR = 0xFFFF
Sharp_LR35902.IE_IE_VBLANK_IE_BIT = 0  -- V-Blank Interrupt Enable
Sharp_LR35902.IE_IE_LCDC_IE_BIT = 1  -- LCDC Status Interrupt Enable
Sharp_LR35902.IE_IE_TIMER_IE_BIT = 2  -- Timer Interrupt Enable
Sharp_LR35902.IE_IE_SERIAL_IE_BIT = 3  -- Serial Interrupt Enable
Sharp_LR35902.IE_IE_JOYPAD_IE_BIT = 4  -- Joypad Interrupt Enable

-- 中断向量定义
Sharp_LR35902.INT_VBLANK = 0  -- V-Blank Interrupt (LY=144, during vertical blanking)
Sharp_LR35902.INT_LCDC_STATUS = 1  -- LCDC Status Interrupt (H-Blank/OAM/V-Count match)
Sharp_LR35902.INT_TIMER_OVERFLOW = 2  -- Timer Overflow Interrupt (TIMA overflow)
Sharp_LR35902.INT_SERIAL_COMPLETE = 3  -- Serial Transfer Complete Interrupt
Sharp_LR35902.INT_JOYPAD = 4  -- Joypad Interrupt (button press/release)

-- 引脚定义
Sharp_LR35902.PIN_VSS = 1  -- Ground
Sharp_LR35902.PIN_VDD = 2  -- Power Supply
Sharp_LR35902.PIN_PHI = 3  -- System Clock Output (4.19MHz / 2 = 2.1MHz CPU)
Sharp_LR35902.PIN_RESET = 4  -- Reset Signal (active low)
Sharp_LR35902.PIN_INT = 5  -- Interrupt Request
Sharp_LR35902.PIN_BUSREQ = 6  -- Bus Request (external DMA access)
Sharp_LR35902.PIN_A0 = 7  -- Address Bus Bit 0
Sharp_LR35902.PIN_A1 = 8  -- Address Bus Bit 1
Sharp_LR35902.PIN_A2 = 9  -- Address Bus Bit 2
Sharp_LR35902.PIN_A3 = 10  -- Address Bus Bit 3
Sharp_LR35902.PIN_A4 = 11  -- Address Bus Bit 4
Sharp_LR35902.PIN_A5 = 12  -- Address Bus Bit 5
Sharp_LR35902.PIN_A6 = 13  -- Address Bus Bit 6
Sharp_LR35902.PIN_A7 = 14  -- Address Bus Bit 7
Sharp_LR35902.PIN_A8 = 15  -- Address Bus Bit 8
Sharp_LR35902.PIN_A9 = 16  -- Address Bus Bit 9
Sharp_LR35902.PIN_A10 = 17  -- Address Bus Bit 10
Sharp_LR35902.PIN_A11 = 18  -- Address Bus Bit 11
Sharp_LR35902.PIN_A12 = 19  -- Address Bus Bit 12
Sharp_LR35902.PIN_A13 = 20  -- Address Bus Bit 13
Sharp_LR35902.PIN_A14 = 21  -- Address Bus Bit 14
Sharp_LR35902.PIN_A15 = 22  -- Address Bus Bit 15
Sharp_LR35902.PIN_D0 = 23  -- Data Bus Bit 0
Sharp_LR35902.PIN_D1 = 24  -- Data Bus Bit 1
Sharp_LR35902.PIN_D2 = 25  -- Data Bus Bit 2
Sharp_LR35902.PIN_D3 = 26  -- Data Bus Bit 3
Sharp_LR35902.PIN_D4 = 27  -- Data Bus Bit 4
Sharp_LR35902.PIN_D5 = 28  -- Data Bus Bit 5
Sharp_LR35902.PIN_D6 = 29  -- Data Bus Bit 6
Sharp_LR35902.PIN_D7 = 30  -- Data Bus Bit 7
Sharp_LR35902.PIN_RD = 31  -- Read Strobe (active low)
Sharp_LR35902.PIN_WR = 32  -- Write Strobe (active low)
Sharp_LR35902.PIN_CS = 33  -- Chip Select (active low)
Sharp_LR35902.PIN_SOUND_OUT = 34  -- Audio Output
Sharp_LR35902.PIN_LCD_DATA0 = 35  -- LCD Data Bus Bit 0
Sharp_LR35902.PIN_LCD_DATA1 = 36  -- LCD Data Bus Bit 1
Sharp_LR35902.PIN_LCD_DATA2 = 37  -- LCD Data Bus Bit 2
Sharp_LR35902.PIN_LCD_DATA3 = 38  -- LCD Data Bus Bit 3
Sharp_LR35902.PIN_LCD_DATA4 = 39  -- LCD Data Bus Bit 4
Sharp_LR35902.PIN_LCD_DATA5 = 40  -- LCD Data Bus Bit 5
Sharp_LR35902.PIN_LCD_DATA6 = 41  -- LCD Data Bus Bit 6
Sharp_LR35902.PIN_LCD_DATA7 = 42  -- LCD Data Bus Bit 7
Sharp_LR35902.PIN_IR = 43  -- Infrared Port (DMG-CGB-01)

-- 设备类
function Sharp_LR35902.new(memory_base)
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
        self.registers["B"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "B Register",
            value = 0
        }
        self.registers["C"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "C Register",
            value = 0
        }
        self.registers["D"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "D Register",
            value = 0
        }
        self.registers["E"] = {
            address = 0x04,
            size = 1,
            access = "rw",
            description = "E Register",
            value = 0
        }
        self.registers["F"] = {
            address = 0x05,
            size = 1,
            access = "rw",
            description = "Flags Register",
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
        self.registers["AF"] = {
            address = 0x08,
            size = 2,
            access = "rw",
            description = "AF Register Pair (Accumulator + Flags)",
            value = 0
        }
        self.registers["BC"] = {
            address = 0x0A,
            size = 2,
            access = "rw",
            description = "BC Register Pair",
            value = 0
        }
        self.registers["DE"] = {
            address = 0x0C,
            size = 2,
            access = "rw",
            description = "DE Register Pair",
            value = 0
        }
        self.registers["HL"] = {
            address = 0x0E,
            size = 2,
            access = "rw",
            description = "HL Register Pair",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x10,
            size = 2,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x12,
            size = 2,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PPU"] = {
            base = 0xFF40,
            type = "video",
            description = "LCD Controller / Picture Processing Unit",
            registers = {}
        }
        
        local p = self.peripherals["PPU"]
        p.registers["LCDC"] = {
            address = 0xFF40,
            size = 1,
            value = 0
        }
        p.registers["STAT"] = {
            address = 0xFF41,
            size = 1,
            value = 0
        }
        p.registers["SCY"] = {
            address = 0xFF42,
            size = 1,
            value = 0
        }
        p.registers["SCX"] = {
            address = 0xFF43,
            size = 1,
            value = 0
        }
        p.registers["LY"] = {
            address = 0xFF44,
            size = 1,
            value = 0
        }
        p.registers["LYC"] = {
            address = 0xFF45,
            size = 1,
            value = 0
        }
        p.registers["DMA"] = {
            address = 0xFF46,
            size = 1,
            value = 0
        }
        p.registers["BGP"] = {
            address = 0xFF47,
            size = 1,
            value = 0
        }
        p.registers["OBP0"] = {
            address = 0xFF48,
            size = 1,
            value = 0
        }
        p.registers["OBP1"] = {
            address = 0xFF49,
            size = 1,
            value = 0
        }
        p.registers["WY"] = {
            address = 0xFF4A,
            size = 1,
            value = 0
        }
        p.registers["WX"] = {
            address = 0xFF4B,
            size = 1,
            value = 0
        }
        self.peripherals["apu"] = {
            base = 0xFF10,
            type = "audio",
            description = "Audio Processing Unit",
            registers = {}
        }
        
        local p = self.peripherals["apu"]
        p.registers["NR10"] = {
            address = 0xFF10,
            size = 1,
            value = 0
        }
        p.registers["NR11"] = {
            address = 0xFF11,
            size = 1,
            value = 0
        }
        p.registers["NR12"] = {
            address = 0xFF12,
            size = 1,
            value = 0
        }
        p.registers["NR13"] = {
            address = 0xFF13,
            size = 1,
            value = 0
        }
        p.registers["NR14"] = {
            address = 0xFF14,
            size = 1,
            value = 0
        }
        p.registers["NR21"] = {
            address = 0xFF16,
            size = 1,
            value = 0
        }
        p.registers["NR22"] = {
            address = 0xFF17,
            size = 1,
            value = 0
        }
        p.registers["NR23"] = {
            address = 0xFF18,
            size = 1,
            value = 0
        }
        p.registers["NR24"] = {
            address = 0xFF19,
            size = 1,
            value = 0
        }
        p.registers["NR30"] = {
            address = 0xFF1A,
            size = 1,
            value = 0
        }
        p.registers["NR31"] = {
            address = 0xFF1B,
            size = 1,
            value = 0
        }
        p.registers["NR32"] = {
            address = 0xFF1C,
            size = 1,
            value = 0
        }
        p.registers["NR33"] = {
            address = 0xFF1D,
            size = 1,
            value = 0
        }
        p.registers["NR34"] = {
            address = 0xFF1E,
            size = 1,
            value = 0
        }
        p.registers["NR41"] = {
            address = 0xFF20,
            size = 1,
            value = 0
        }
        p.registers["NR42"] = {
            address = 0xFF21,
            size = 1,
            value = 0
        }
        p.registers["NR43"] = {
            address = 0xFF22,
            size = 1,
            value = 0
        }
        p.registers["NR44"] = {
            address = 0xFF23,
            size = 1,
            value = 0
        }
        p.registers["NR50"] = {
            address = 0xFF24,
            size = 1,
            value = 0
        }
        p.registers["NR51"] = {
            address = 0xFF25,
            size = 1,
            value = 0
        }
        p.registers["NR52"] = {
            address = 0xFF26,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER"] = {
            base = 0xFF04,
            type = "timer",
            description = "Timer Unit",
            registers = {}
        }
        
        local p = self.peripherals["TIMER"]
        p.registers["DIV"] = {
            address = 0xFF04,
            size = 1,
            value = 0
        }
        p.registers["TIMA"] = {
            address = 0xFF05,
            size = 1,
            value = 0
        }
        p.registers["TMA"] = {
            address = 0xFF06,
            size = 1,
            value = 0
        }
        p.registers["TAC"] = {
            address = 0xFF07,
            size = 1,
            value = 0
        }
        self.peripherals["JOYPAD"] = {
            base = 0xFF00,
            type = "input",
            description = "Joypad Controller",
            registers = {}
        }
        
        local p = self.peripherals["JOYPAD"]
        p.registers["P1"] = {
            address = 0xFF00,
            size = 1,
            value = 0
        }
        self.peripherals["SERIAL"] = {
            base = 0xFF01,
            type = "uart",
            description = "Serial I/O (Link Cable)",
            registers = {}
        }
        
        local p = self.peripherals["SERIAL"]
        p.registers["SB"] = {
            address = 0xFF01,
            size = 1,
            value = 0
        }
        p.registers["SC"] = {
            address = 0xFF02,
            size = 1,
            value = 0
        }
        self.peripherals["INTERRUPT"] = {
            base = 0xFF0F,
            type = "system",
            description = "Interrupt Flag Register",
            registers = {}
        }
        
        local p = self.peripherals["INTERRUPT"]
        p.registers["IF"] = {
            address = 0xFF0F,
            size = 1,
            value = 0
        }
        self.peripherals["IE"] = {
            base = 0xFFFF,
            type = "system",
            description = "Interrupt Enable Register",
            registers = {}
        }
        
        local p = self.peripherals["IE"]
        p.registers["IE"] = {
            address = 0xFFFF,
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
            name = Sharp_LR35902.DEVICE_NAME,
            manufacturer = Sharp_LR35902.MANUFACTURER,
            family = Sharp_LR35902.FAMILY,
            version = Sharp_LR35902.VERSION,
            architecture = Sharp_LR35902.ARCHITECTURE,
            bits = Sharp_LR35902.BITS,
            clock_frequency = Sharp_LR35902.CLOCK_FREQUENCY
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
        return string.format("Sharp_LR35902(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Sharp_LR35902.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Sharp_LR35902.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Sharp_LR35902.print_device_info(device)
    device = device or Sharp_LR35902.new()
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

function Sharp_LR35902.print_registers(device)
    device = device or Sharp_LR35902.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Sharp_LR35902.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Sharp_LR35902.example()
    print("=== Sharp-LR35902设备示例 ===")
    
    -- 创建设备实例
    local device = Sharp_LR35902.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Sharp_LR35902.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Sharp_LR35902.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Sharp_LR35902.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Sharp_LR35902.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Sharp_LR35902.lua$") then
    Sharp_LR35902.example()
end

return Sharp_LR35902

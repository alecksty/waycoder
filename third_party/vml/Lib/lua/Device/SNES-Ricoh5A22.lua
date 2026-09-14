--[[
  Ricoh-5A22设备定义 - Lua模块
  生成自: Ricoh/MOS-6502/Ricoh-5A22
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Super Nintendo Entertainment System (SNES) main processor - 16-bit 6502 variant with enhanced capabilities
  CPU架构: Ricoh-5A22
  位宽: 16位
  时钟频率: 3580000 Hz
]]

local Ricoh_5A22 = {}

-- 设备信息
Ricoh_5A22.DEVICE_NAME = "Ricoh-5A22"
Ricoh_5A22.MANUFACTURER = "Ricoh"
Ricoh_5A22.FAMILY = "MOS-6502"
Ricoh_5A22.VERSION = "1.0"
Ricoh_5A22.ARCHITECTURE = "Ricoh-5A22"
Ricoh_5A22.BITS = 16
Ricoh_5A22.CLOCK_FREQUENCY = 3580000

-- 寄存器地址定义
Ricoh_5A22.A_ADDR = 0x00  -- Accumulator (8-bit, expandable to 16-bit)
Ricoh_5A22.B_ADDR = 0x01  -- Accumulator high byte when 16-bit
Ricoh_5A22.X_ADDR = 0x02  -- X Index Register (8/16-bit)
Ricoh_5A22.Y_ADDR = 0x03  -- Y Index Register (8/16-bit)
Ricoh_5A22.SP_ADDR = 0x04  -- Stack Pointer (8-bit, banked)
Ricoh_5A22.PC_ADDR = 0x06  -- Program Counter (16-bit)
Ricoh_5A22.D_ADDR = 0x08  -- Direct Page Register
Ricoh_5A22.P_ADDR = 0x0A  -- Processor Status
Ricoh_5A22.P_N_BIT = 0  -- Negative
Ricoh_5A22.P_V_BIT = 1  -- Overflow
Ricoh_5A22.P_M_BIT = 2  -- Memory/Accumulator Select (0=16-bit, 1=8-bit)
Ricoh_5A22.P_X_BIT = 3  -- Index Select (0=16-bit, 1=8-bit)
Ricoh_5A22.P_B_BIT = 4  -- Break
Ricoh_5A22.P_D_BIT = 5  -- Decimal Mode
Ricoh_5A22.P_I_BIT = 6  -- Interrupt Disable
Ricoh_5A22.P_Z_BIT = 7  -- Zero
Ricoh_5A22.P_C_BIT = 8  -- Carry

-- 内存段定义
Ricoh_5A22.WRAM_START = 0x7E0000
Ricoh_5A22.WRAM_END = 0x7FFFFF
Ricoh_5A22.WRAM_SIZE = 131072  -- Work RAM (128KB internal)
Ricoh_5A22.SRAM_START = 0x600000
Ricoh_5A22.SRAM_END = 0x6FFFFF
Ricoh_5A22.SRAM_SIZE = 1048576  -- Save RAM / Cartridge SRAM
Ricoh_5A22.CART_ROM_START = 0x800000
Ricoh_5A22.CART_ROM_END = 0xFFFFFF
Ricoh_5A22.CART_ROM_SIZE = 8388608  -- Cartridge ROM (LoROM/HiROM mapping)
Ricoh_5A22.PPU1_REGS_START = 0x2100
Ricoh_5A22.PPU1_REGS_END = 0x213F
Ricoh_5A22.PPU1_REGS_SIZE = 64  -- PPU1 Registers (background)
Ricoh_5A22.PPU2_REGS_START = 0x2140
Ricoh_5A22.PPU2_REGS_END = 0x217F
Ricoh_5A22.PPU2_REGS_SIZE = 64  -- PPU2 Registers (sprites)
Ricoh_5A22.PPU3_REGS_START = 0x2180
Ricoh_5A22.PPU3_REGS_END = 0x21FF
Ricoh_5A22.PPU3_REGS_SIZE = 128  -- PPU3 Registers (extra)
Ricoh_5A22.APU_REGS_START = 0x2140
Ricoh_5A22.APU_REGS_END = 0x217F
Ricoh_5A22.APU_REGS_SIZE = 64  -- APU I/O Registers
Ricoh_5A22.CPU_IO_START = 0x2000
Ricoh_5A22.CPU_IO_END = 0x20FF
Ricoh_5A22.CPU_IO_SIZE = 256  -- CPU I/O Ports
Ricoh_5A22.DMA_REGS_START = 0x4300
Ricoh_5A22.DMA_REGS_END = 0x437F
Ricoh_5A22.DMA_REGS_SIZE = 128  -- DMA Channel Registers
Ricoh_5A22.HDMA_REGS_START = 0x4380
Ricoh_5A22.HDMA_REGS_END = 0x43FF
Ricoh_5A22.HDMA_REGS_SIZE = 128  -- HDMA Channel Registers

-- 外设定义
-- Picture Processing Unit 1 - Background Rendering
Ricoh_5A22.PPU1_BASE = 0x2100
Ricoh_5A22.PPU1_INIDISP_ADDR = 0x2100
Ricoh_5A22.PPU1_OBSEL_ADDR = 0x2101
Ricoh_5A22.PPU1_OAMADDL_ADDR = 0x2102
Ricoh_5A22.PPU1_OAMADDH_ADDR = 0x2103
Ricoh_5A22.PPU1_OAMDATA_ADDR = 0x2104
Ricoh_5A22.PPU1_BGMODE_ADDR = 0x2105
Ricoh_5A22.PPU1_MOSAIC_ADDR = 0x2106
Ricoh_5A22.PPU1_BG1SC_ADDR = 0x2107
Ricoh_5A22.PPU1_BG2SC_ADDR = 0x2108
Ricoh_5A22.PPU1_BG3SC_ADDR = 0x2109
Ricoh_5A22.PPU1_BG4SC_ADDR = 0x210A
Ricoh_5A22.PPU1_BG12NBA_ADDR = 0x210B
Ricoh_5A22.PPU1_BG34NBA_ADDR = 0x210C
Ricoh_5A22.PPU1_BG1HOFS_ADDR = 0x210D
Ricoh_5A22.PPU1_BG1VOFS_ADDR = 0x210E
Ricoh_5A22.PPU1_BG2HOFS_ADDR = 0x210F
Ricoh_5A22.PPU1_BG2VOFS_ADDR = 0x2110
Ricoh_5A22.PPU1_BG3HOFS_ADDR = 0x2111
Ricoh_5A22.PPU1_BG3VOFS_ADDR = 0x2112
Ricoh_5A22.PPU1_BG4HOFS_ADDR = 0x2113
Ricoh_5A22.PPU1_BG4VOFS_ADDR = 0x2114
Ricoh_5A22.PPU1_VMAIN_ADDR = 0x2115
Ricoh_5A22.PPU1_VMADDL_ADDR = 0x2116
Ricoh_5A22.PPU1_VMADDH_ADDR = 0x2117
Ricoh_5A22.PPU1_VMDATAL_ADDR = 0x2118
Ricoh_5A22.PPU1_VMDATAH_ADDR = 0x2119
Ricoh_5A22.PPU1_M7SEL_ADDR = 0x211A
Ricoh_5A22.PPU1_M7A_ADDR = 0x211B
Ricoh_5A22.PPU1_M7B_ADDR = 0x211C
Ricoh_5A22.PPU1_M7C_ADDR = 0x211D
Ricoh_5A22.PPU1_M7D_ADDR = 0x211E
Ricoh_5A22.PPU1_M7X_ADDR = 0x211F
Ricoh_5A22.PPU1_M7Y_ADDR = 0x2120
Ricoh_5A22.PPU1_CGADD_ADDR = 0x2121
Ricoh_5A22.PPU1_CGDATA_ADDR = 0x2122
Ricoh_5A22.PPU1_W12SEL_ADDR = 0x2123
Ricoh_5A22.PPU1_W34SEL_ADDR = 0x2124
Ricoh_5A22.PPU1_WOBJSEL_ADDR = 0x2125
Ricoh_5A22.PPU1_WH0_ADDR = 0x2126
Ricoh_5A22.PPU1_WH1_ADDR = 0x2127
Ricoh_5A22.PPU1_WH2_ADDR = 0x2128
Ricoh_5A22.PPU1_WH3_ADDR = 0x2129
Ricoh_5A22.PPU1_WBGLOG_ADDR = 0x212A
Ricoh_5A22.PPU1_WOBJLOG_ADDR = 0x212B
Ricoh_5A22.PPU1_TM_ADDR = 0x212C
Ricoh_5A22.PPU1_TS_ADDR = 0x212D
Ricoh_5A22.PPU1_TMW_ADDR = 0x212E
Ricoh_5A22.PPU1_TSW_ADDR = 0x212F
Ricoh_5A22.PPU1_CGSWSEL_ADDR = 0x2130
Ricoh_5A22.PPU1_CGADSUB_ADDR = 0x2131
Ricoh_5A22.PPU1_SETINI_ADDR = 0x2133
-- Picture Processing Unit 2 - Sprite Rendering
Ricoh_5A22.PPU2_BASE = 0x2140
Ricoh_5A22.PPU2_OAMDATAREAD_ADDR = 0x2138
Ricoh_5A22.PPU2_VMDATAREAD_ADDR = 0x2139
Ricoh_5A22.PPU2_VMDATAHREAD_ADDR = 0x213A
Ricoh_5A22.PPU2_CGDATAREAD_ADDR = 0x213B
Ricoh_5A22.PPU2_OPHCT_ADDR = 0x213C
Ricoh_5A22.PPU2_OPVCT_ADDR = 0x213D
Ricoh_5A22.PPU2_STAT78_ADDR = 0x213F
-- Sony SPC700 Audio CPU (8-bit)
Ricoh_5A22.SPC700_BASE = 0x00
Ricoh_5A22.SPC700_PC_ADDR = 0x00
Ricoh_5A22.SPC700_A_ADDR = 0x02
Ricoh_5A22.SPC700_X_ADDR = 0x03
Ricoh_5A22.SPC700_Y_ADDR = 0x04
Ricoh_5A22.SPC700_SP_ADDR = 0x05
Ricoh_5A22.SPC700_PSW_ADDR = 0x06
Ricoh_5A22.SPC700_TEST_ADDR = 0x0F
-- S-DSP Audio DSP (8-channel ADPCM)
Ricoh_5A22.DSP_BASE = 0x00
Ricoh_5A22.DSP_MVOL_L_ADDR = 0x0C
Ricoh_5A22.DSP_MVOL_R_ADDR = 0x1C
Ricoh_5A22.DSP_EVOL_L_ADDR = 0x2C
Ricoh_5A22.DSP_EVOL_R_ADDR = 0x3C
Ricoh_5A22.DSP_KON_ADDR = 0x4C
Ricoh_5A22.DSP_KOFF_ADDR = 0x5C
Ricoh_5A22.DSP_KONKOFF_ADDR = 0x4D
Ricoh_5A22.DSP_FLG_ADDR = 0x6C
Ricoh_5A22.DSP_ENDX_ADDR = 0x7D
Ricoh_5A22.DSP_EBUST_ADDR = 0x6D
Ricoh_5A22.DSP_EDL_ADDR = 0x7D
Ricoh_5A22.DSP_ENV0_ADDR = 0x00
Ricoh_5A22.DSP_OUT0_ADDR = 0x1C
Ricoh_5A22.DSP_ENV1_ADDR = 0x01
Ricoh_5A22.DSP_OUT1_ADDR = 0x2C
Ricoh_5A22.DSP_ENV2_ADDR = 0x02
Ricoh_5A22.DSP_OUT2_ADDR = 0x3C
Ricoh_5A22.DSP_ENV3_ADDR = 0x03
Ricoh_5A22.DSP_OUT3_ADDR = 0x4C
Ricoh_5A22.DSP_ENV4_ADDR = 0x04
Ricoh_5A22.DSP_OUT4_ADDR = 0x5C
Ricoh_5A22.DSP_ENV5_ADDR = 0x05
Ricoh_5A22.DSP_OUT5_ADDR = 0x6C
Ricoh_5A22.DSP_ENV6_ADDR = 0x06
Ricoh_5A22.DSP_OUT6_ADDR = 0x7C
Ricoh_5A22.DSP_ENV7_ADDR = 0x07
Ricoh_5A22.DSP_OUT7_ADDR = 0x0D
Ricoh_5A22.DSP_V0SRC_ADDR = 0x08
Ricoh_5A22.DSP_V1SRC_ADDR = 0x09
Ricoh_5A22.DSP_V2SRC_ADDR = 0x0A
Ricoh_5A22.DSP_V3SRC_ADDR = 0x0B
Ricoh_5A22.DSP_V4SRC_ADDR = 0x18
Ricoh_5A22.DSP_V5SRC_ADDR = 0x19
Ricoh_5A22.DSP_V6SRC_ADDR = 0x1A
Ricoh_5A22.DSP_V7SRC_ADDR = 0x1B
Ricoh_5A22.DSP_V0PITCHL_ADDR = 0x02
Ricoh_5A22.DSP_V0PITCHH_ADDR = 0x03
Ricoh_5A22.DSP_V1PITCHL_ADDR = 0x12
Ricoh_5A22.DSP_V1PITCHH_ADDR = 0x13
Ricoh_5A22.DSP_V2PITCHL_ADDR = 0x22
Ricoh_5A22.DSP_V2PITCHH_ADDR = 0x23
Ricoh_5A22.DSP_V3PITCHL_ADDR = 0x32
Ricoh_5A22.DSP_V3PITCHH_ADDR = 0x33
Ricoh_5A22.DSP_V4PITCHL_ADDR = 0x42
Ricoh_5A22.DSP_V4PITCHH_ADDR = 0x43
Ricoh_5A22.DSP_V5PITCHL_ADDR = 0x52
Ricoh_5A22.DSP_V5PITCHH_ADDR = 0x53
Ricoh_5A22.DSP_V6PITCHL_ADDR = 0x62
Ricoh_5A22.DSP_V6PITCHH_ADDR = 0x63
Ricoh_5A22.DSP_V7PITCHL_ADDR = 0x72
Ricoh_5A22.DSP_V7PITCHH_ADDR = 0x73
Ricoh_5A22.DSP_V0ADSR0_ADDR = 0x04
Ricoh_5A22.DSP_V0ADSR1_ADDR = 0x05
Ricoh_5A22.DSP_V0ADSR2_ADDR = 0x06
Ricoh_5A22.DSP_V1ADSR0_ADDR = 0x14
Ricoh_5A22.DSP_V1ADSR1_ADDR = 0x15
Ricoh_5A22.DSP_V1ADSR2_ADDR = 0x16
Ricoh_5A22.DSP_V2ADSR0_ADDR = 0x24
Ricoh_5A22.DSP_V2ADSR1_ADDR = 0x25
Ricoh_5A22.DSP_V2ADSR2_ADDR = 0x26
Ricoh_5A22.DSP_V3ADSR0_ADDR = 0x34
Ricoh_5A22.DSP_V3ADSR1_ADDR = 0x35
Ricoh_5A22.DSP_V3ADSR2_ADDR = 0x36
Ricoh_5A22.DSP_V4ADSR0_ADDR = 0x44
Ricoh_5A22.DSP_V4ADSR1_ADDR = 0x45
Ricoh_5A22.DSP_V4ADSR2_ADDR = 0x46
Ricoh_5A22.DSP_V5ADSR0_ADDR = 0x54
Ricoh_5A22.DSP_V5ADSR1_ADDR = 0x55
Ricoh_5A22.DSP_V5ADSR2_ADDR = 0x56
Ricoh_5A22.DSP_V6ADSR0_ADDR = 0x64
Ricoh_5A22.DSP_V6ADSR1_ADDR = 0x65
Ricoh_5A22.DSP_V6ADSR2_ADDR = 0x66
Ricoh_5A22.DSP_V7ADSR0_ADDR = 0x74
Ricoh_5A22.DSP_V7ADSR1_ADDR = 0x75
Ricoh_5A22.DSP_V7ADSR2_ADDR = 0x76
Ricoh_5A22.DSP_V0GAIN_ADDR = 0x07
Ricoh_5A22.DSP_V1GAIN_ADDR = 0x17
Ricoh_5A22.DSP_V2GAIN_ADDR = 0x27
Ricoh_5A22.DSP_V3GAIN_ADDR = 0x37
Ricoh_5A22.DSP_V4GAIN_ADDR = 0x47
Ricoh_5A22.DSP_V5GAIN_ADDR = 0x57
Ricoh_5A22.DSP_V6GAIN_ADDR = 0x67
Ricoh_5A22.DSP_V7GAIN_ADDR = 0x77
Ricoh_5A22.DSP_V0WAVE_ADDR = 0x0D
Ricoh_5A22.DSP_V1WAVE_ADDR = 0x1D
Ricoh_5A22.DSP_V2WAVE_ADDR = 0x2D
Ricoh_5A22.DSP_V3WAVE_ADDR = 0x3D
Ricoh_5A22.DSP_V4WAVE_ADDR = 0x4D
Ricoh_5A22.DSP_V5WAVE_ADDR = 0x5D
Ricoh_5A22.DSP_V6WAVE_ADDR = 0x6D
Ricoh_5A22.DSP_V7WAVE_ADDR = 0x7D
-- Direct Memory Access Controller
Ricoh_5A22.DMA_BASE = 0x4300
Ricoh_5A22.DMA_DMAP0_ADDR = 0x4300
Ricoh_5A22.DMA_BBAD0_ADDR = 0x4301
Ricoh_5A22.DMA_A1T0L_ADDR = 0x4302
Ricoh_5A22.DMA_A1T0H_ADDR = 0x4303
Ricoh_5A22.DMA_A1B0_ADDR = 0x4304
Ricoh_5A22.DMA_DAS0L_ADDR = 0x4305
Ricoh_5A22.DMA_DAS0H_ADDR = 0x4306
Ricoh_5A22.DMA_DASB0_ADDR = 0x4307
Ricoh_5A22.DMA_A2A0_ADDR = 0x4308
Ricoh_5A22.DMA_A2A1_ADDR = 0x4309
Ricoh_5A22.DMA_A2B0_ADDR = 0x430A
Ricoh_5A22.DMA_NTT0_ADDR = 0x430B
Ricoh_5A22.DMA_DMAP1_ADDR = 0x4310
Ricoh_5A22.DMA_BBAD1_ADDR = 0x4311
Ricoh_5A22.DMA_A1T1L_ADDR = 0x4312
Ricoh_5A22.DMA_A1T1H_ADDR = 0x4313
Ricoh_5A22.DMA_A1B1_ADDR = 0x4314
Ricoh_5A22.DMA_DAS1L_ADDR = 0x4315
Ricoh_5A22.DMA_DAS1H_ADDR = 0x4316
Ricoh_5A22.DMA_DASB1_ADDR = 0x4317
Ricoh_5A22.DMA_DMAP2_ADDR = 0x4320
Ricoh_5A22.DMA_BBAD2_ADDR = 0x4321
Ricoh_5A22.DMA_A1T2L_ADDR = 0x4322
Ricoh_5A22.DMA_A1T2H_ADDR = 0x4323
Ricoh_5A22.DMA_A1B2_ADDR = 0x4324
Ricoh_5A22.DMA_DAS2L_ADDR = 0x4325
Ricoh_5A22.DMA_DAS2H_ADDR = 0x4326
Ricoh_5A22.DMA_DASB2_ADDR = 0x4327
Ricoh_5A22.DMA_DMAP3_ADDR = 0x4330
Ricoh_5A22.DMA_BBAD3_ADDR = 0x4331
Ricoh_5A22.DMA_A1T3L_ADDR = 0x4332
Ricoh_5A22.DMA_A1T3H_ADDR = 0x4333
Ricoh_5A22.DMA_A1B3_ADDR = 0x4334
Ricoh_5A22.DMA_DAS3L_ADDR = 0x4335
Ricoh_5A22.DMA_DAS3H_ADDR = 0x4336
Ricoh_5A22.DMA_DASB3_ADDR = 0x4337
Ricoh_5A22.DMA_MDMAEN_ADDR = 0x4350
-- Horizontal DMA (scanline-based)
Ricoh_5A22.HDMA_BASE = 0x4380
Ricoh_5A22.HDMA_HDMAP0_ADDR = 0x4380
Ricoh_5A22.HDMA_HBAD0_ADDR = 0x4381
Ricoh_5A22.HDMA_A1T0L_ADDR = 0x4382
Ricoh_5A22.HDMA_A1T0H_ADDR = 0x4383
Ricoh_5A22.HDMA_A1B0_ADDR = 0x4384
Ricoh_5A22.HDMA_DAS0L_ADDR = 0x4385
Ricoh_5A22.HDMA_DAS0H_ADDR = 0x4386
Ricoh_5A22.HDMA_HDMAP1_ADDR = 0x4388
Ricoh_5A22.HDMA_HBAD1_ADDR = 0x4389
Ricoh_5A22.HDMA_A1T1L_ADDR = 0x438A
Ricoh_5A22.HDMA_A1T1H_ADDR = 0x438B
Ricoh_5A22.HDMA_A1B1_ADDR = 0x438C
Ricoh_5A22.HDMA_DAS1L_ADDR = 0x438D
Ricoh_5A22.HDMA_DAS1H_ADDR = 0x438E
Ricoh_5A22.HDMA_HDMAP2_ADDR = 0x4390
Ricoh_5A22.HDMA_HBAD2_ADDR = 0x4391
Ricoh_5A22.HDMA_A1T2L_ADDR = 0x4392
Ricoh_5A22.HDMA_A1T2H_ADDR = 0x4393
Ricoh_5A22.HDMA_A1B2_ADDR = 0x4394
Ricoh_5A22.HDMA_DAS2L_ADDR = 0x4395
Ricoh_5A22.HDMA_DAS2H_ADDR = 0x4396
Ricoh_5A22.HDMA_HDMAP3_ADDR = 0x4398
Ricoh_5A22.HDMA_HBAD3_ADDR = 0x4399
Ricoh_5A22.HDMA_A1T3L_ADDR = 0x439A
Ricoh_5A22.HDMA_A1T3H_ADDR = 0x439B
Ricoh_5A22.HDMA_A1B3_ADDR = 0x439C
Ricoh_5A22.HDMA_DAS3L_ADDR = 0x439D
Ricoh_5A22.HDMA_DAS3H_ADDR = 0x439E
Ricoh_5A22.HDMA_HDMAEN_ADDR = 0x43F0
-- Controller Port 1
Ricoh_5A22.CONTROLLER1_BASE = 0x4016
Ricoh_5A22.CONTROLLER1_JOYPAD1_ADDR = 0x4016
Ricoh_5A22.CONTROLLER1_JOYSTROBE_ADDR = 0x4016
-- Controller Port 2
Ricoh_5A22.CONTROLLER2_BASE = 0x4017
Ricoh_5A22.CONTROLLER2_JOYPAD2_ADDR = 0x4017
Ricoh_5A22.CONTROLLER2_RDNMI_ADDR = 0x4210
Ricoh_5A22.CONTROLLER2_TIMEUP_ADDR = 0x4211
Ricoh_5A22.CONTROLLER2_HVBJOY_ADDR = 0x4212
-- Timer / IRQ Control
Ricoh_5A22.TIMER_BASE = 0x4200
Ricoh_5A22.TIMER_NMITIMEN_ADDR = 0x4200
Ricoh_5A22.TIMER_NMITIMEN_VBLANK_NMI_BIT = 7  -- V-Blank NMI Enable
Ricoh_5A22.TIMER_NMITIMEN_HTIMER_EN_BIT = 4  -- H-Counter IRQ Enable
Ricoh_5A22.TIMER_NMITIMEN_VTIMER_EN_BIT = 5  -- V-Counter IRQ Enable
Ricoh_5A22.TIMER_WRI00_ADDR = 0x4201
Ricoh_5A22.TIMER_HTIMEL_ADDR = 0x4202
Ricoh_5A22.TIMER_HTIMEH_ADDR = 0x4203
Ricoh_5A22.TIMER_VTIMEL_ADDR = 0x4204
Ricoh_5A22.TIMER_VTIMEH_ADDR = 0x4205
Ricoh_5A22.TIMER_MEMSEL_ADDR = 0x420D

-- 中断向量定义
Ricoh_5A22.INT_RESET = 0  -- Reset
Ricoh_5A22.INT_NMI = 1  -- Non-Maskable Interrupt (V-Blank)
Ricoh_5A22.INT_IRQ = 2  -- IRQ / BRK (Timer, HDMA, Controller)
Ricoh_5A22.INT_TIMER_IRQ = 3  -- H/V Counter Timer IRQ

-- 引脚定义
Ricoh_5A22.PIN_VCC = 1  -- Power Supply
Ricoh_5A22.PIN_GND = 2  -- Ground
Ricoh_5A22.PIN_CLK = 3  -- System Clock Input (21.47727 MHz)
Ricoh_5A22.PIN_RESET = 4  -- Reset Signal
Ricoh_5A22.PIN_NMI = 5  -- Non-Maskable Interrupt
Ricoh_5A22.PIN_IRQ = 6  -- Interrupt Request
Ricoh_5A22.PIN_RDY = 7  -- Ready / Wait State
Ricoh_5A22.PIN_AB0 = 8  -- Address Bus Bit 0
Ricoh_5A22.PIN_AB1 = 9  -- Address Bus Bit 1
Ricoh_5A22.PIN_AB2 = 10  -- Address Bus Bit 2
Ricoh_5A22.PIN_AB3 = 11  -- Address Bus Bit 3
Ricoh_5A22.PIN_AB4 = 12  -- Address Bus Bit 4
Ricoh_5A22.PIN_AB5 = 13  -- Address Bus Bit 5
Ricoh_5A22.PIN_AB6 = 14  -- Address Bus Bit 6
Ricoh_5A22.PIN_AB7 = 15  -- Address Bus Bit 7
Ricoh_5A22.PIN_AB8 = 16  -- Address Bus Bit 8
Ricoh_5A22.PIN_AB9 = 17  -- Address Bus Bit 9
Ricoh_5A22.PIN_AB10 = 18  -- Address Bus Bit 10
Ricoh_5A22.PIN_AB11 = 19  -- Address Bus Bit 11
Ricoh_5A22.PIN_AB12 = 20  -- Address Bus Bit 12
Ricoh_5A22.PIN_AB13 = 21  -- Address Bus Bit 13
Ricoh_5A22.PIN_AB14 = 22  -- Address Bus Bit 14
Ricoh_5A22.PIN_AB15 = 23  -- Address Bus Bit 15
Ricoh_5A22.PIN_AB16 = 24  -- Address Bus Bit 16
Ricoh_5A22.PIN_AB17 = 25  -- Address Bus Bit 17
Ricoh_5A22.PIN_AB18 = 26  -- Address Bus Bit 18
Ricoh_5A22.PIN_AB19 = 27  -- Address Bus Bit 19
Ricoh_5A22.PIN_AB20 = 28  -- Address Bus Bit 20
Ricoh_5A22.PIN_AB21 = 29  -- Address Bus Bit 21
Ricoh_5A22.PIN_AB22 = 30  -- Address Bus Bit 22
Ricoh_5A22.PIN_AB23 = 31  -- Address Bus Bit 23
Ricoh_5A22.PIN_DB0 = 32  -- Data Bus Bit 0
Ricoh_5A22.PIN_DB1 = 33  -- Data Bus Bit 1
Ricoh_5A22.PIN_DB2 = 34  -- Data Bus Bit 2
Ricoh_5A22.PIN_DB3 = 35  -- Data Bus Bit 3
Ricoh_5A22.PIN_DB4 = 36  -- Data Bus Bit 4
Ricoh_5A22.PIN_DB5 = 37  -- Data Bus Bit 5
Ricoh_5A22.PIN_DB6 = 38  -- Data Bus Bit 6
Ricoh_5A22.PIN_DB7 = 39  -- Data Bus Bit 7
Ricoh_5A22.PIN_PHI = 40  -- Phase Out Clock

-- 设备类
function Ricoh_5A22.new(memory_base)
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
            description = "Accumulator (8-bit, expandable to 16-bit)",
            value = 0
        }
        self.registers["B"] = {
            address = 0x01,
            size = 1,
            access = "rw",
            description = "Accumulator high byte when 16-bit",
            value = 0
        }
        self.registers["X"] = {
            address = 0x02,
            size = 1,
            access = "rw",
            description = "X Index Register (8/16-bit)",
            value = 0
        }
        self.registers["Y"] = {
            address = 0x03,
            size = 1,
            access = "rw",
            description = "Y Index Register (8/16-bit)",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x04,
            size = 1,
            access = "rw",
            description = "Stack Pointer (8-bit, banked)",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x06,
            size = 2,
            access = "rw",
            description = "Program Counter (16-bit)",
            value = 0
        }
        self.registers["D"] = {
            address = 0x08,
            size = 1,
            access = "rw",
            description = "Direct Page Register",
            value = 0
        }
        self.registers["P"] = {
            address = 0x0A,
            size = 1,
            access = "rw",
            description = "Processor Status",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["PPU1"] = {
            base = 0x2100,
            type = "video",
            description = "Picture Processing Unit 1 - Background Rendering",
            registers = {}
        }
        
        local p = self.peripherals["PPU1"]
        p.registers["INIDISP"] = {
            address = 0x2100,
            size = 1,
            value = 0
        }
        p.registers["OBSEL"] = {
            address = 0x2101,
            size = 1,
            value = 0
        }
        p.registers["OAMADDL"] = {
            address = 0x2102,
            size = 1,
            value = 0
        }
        p.registers["OAMADDH"] = {
            address = 0x2103,
            size = 1,
            value = 0
        }
        p.registers["OAMDATA"] = {
            address = 0x2104,
            size = 1,
            value = 0
        }
        p.registers["BGMODE"] = {
            address = 0x2105,
            size = 1,
            value = 0
        }
        p.registers["MOSAIC"] = {
            address = 0x2106,
            size = 1,
            value = 0
        }
        p.registers["BG1SC"] = {
            address = 0x2107,
            size = 1,
            value = 0
        }
        p.registers["BG2SC"] = {
            address = 0x2108,
            size = 1,
            value = 0
        }
        p.registers["BG3SC"] = {
            address = 0x2109,
            size = 1,
            value = 0
        }
        p.registers["BG4SC"] = {
            address = 0x210A,
            size = 1,
            value = 0
        }
        p.registers["BG12NBA"] = {
            address = 0x210B,
            size = 1,
            value = 0
        }
        p.registers["BG34NBA"] = {
            address = 0x210C,
            size = 1,
            value = 0
        }
        p.registers["BG1HOFS"] = {
            address = 0x210D,
            size = 2,
            value = 0
        }
        p.registers["BG1VOFS"] = {
            address = 0x210E,
            size = 2,
            value = 0
        }
        p.registers["BG2HOFS"] = {
            address = 0x210F,
            size = 2,
            value = 0
        }
        p.registers["BG2VOFS"] = {
            address = 0x2110,
            size = 2,
            value = 0
        }
        p.registers["BG3HOFS"] = {
            address = 0x2111,
            size = 2,
            value = 0
        }
        p.registers["BG3VOFS"] = {
            address = 0x2112,
            size = 2,
            value = 0
        }
        p.registers["BG4HOFS"] = {
            address = 0x2113,
            size = 2,
            value = 0
        }
        p.registers["BG4VOFS"] = {
            address = 0x2114,
            size = 2,
            value = 0
        }
        p.registers["VMAIN"] = {
            address = 0x2115,
            size = 1,
            value = 0
        }
        p.registers["VMADDL"] = {
            address = 0x2116,
            size = 1,
            value = 0
        }
        p.registers["VMADDH"] = {
            address = 0x2117,
            size = 1,
            value = 0
        }
        p.registers["VMDATAL"] = {
            address = 0x2118,
            size = 1,
            value = 0
        }
        p.registers["VMDATAH"] = {
            address = 0x2119,
            size = 1,
            value = 0
        }
        p.registers["M7SEL"] = {
            address = 0x211A,
            size = 1,
            value = 0
        }
        p.registers["M7A"] = {
            address = 0x211B,
            size = 2,
            value = 0
        }
        p.registers["M7B"] = {
            address = 0x211C,
            size = 2,
            value = 0
        }
        p.registers["M7C"] = {
            address = 0x211D,
            size = 2,
            value = 0
        }
        p.registers["M7D"] = {
            address = 0x211E,
            size = 2,
            value = 0
        }
        p.registers["M7X"] = {
            address = 0x211F,
            size = 2,
            value = 0
        }
        p.registers["M7Y"] = {
            address = 0x2120,
            size = 2,
            value = 0
        }
        p.registers["CGADD"] = {
            address = 0x2121,
            size = 1,
            value = 0
        }
        p.registers["CGDATA"] = {
            address = 0x2122,
            size = 1,
            value = 0
        }
        p.registers["W12SEL"] = {
            address = 0x2123,
            size = 1,
            value = 0
        }
        p.registers["W34SEL"] = {
            address = 0x2124,
            size = 1,
            value = 0
        }
        p.registers["WOBJSEL"] = {
            address = 0x2125,
            size = 1,
            value = 0
        }
        p.registers["WH0"] = {
            address = 0x2126,
            size = 1,
            value = 0
        }
        p.registers["WH1"] = {
            address = 0x2127,
            size = 1,
            value = 0
        }
        p.registers["WH2"] = {
            address = 0x2128,
            size = 1,
            value = 0
        }
        p.registers["WH3"] = {
            address = 0x2129,
            size = 1,
            value = 0
        }
        p.registers["WBGLOG"] = {
            address = 0x212A,
            size = 1,
            value = 0
        }
        p.registers["WOBJLOG"] = {
            address = 0x212B,
            size = 1,
            value = 0
        }
        p.registers["TM"] = {
            address = 0x212C,
            size = 1,
            value = 0
        }
        p.registers["TS"] = {
            address = 0x212D,
            size = 1,
            value = 0
        }
        p.registers["TMW"] = {
            address = 0x212E,
            size = 1,
            value = 0
        }
        p.registers["TSW"] = {
            address = 0x212F,
            size = 1,
            value = 0
        }
        p.registers["CGSWSEL"] = {
            address = 0x2130,
            size = 1,
            value = 0
        }
        p.registers["CGADSUB"] = {
            address = 0x2131,
            size = 1,
            value = 0
        }
        p.registers["SETINI"] = {
            address = 0x2133,
            size = 1,
            value = 0
        }
        self.peripherals["PPU2"] = {
            base = 0x2140,
            type = "video",
            description = "Picture Processing Unit 2 - Sprite Rendering",
            registers = {}
        }
        
        local p = self.peripherals["PPU2"]
        p.registers["OAMDATAREAD"] = {
            address = 0x2138,
            size = 1,
            value = 0
        }
        p.registers["VMDATAREAD"] = {
            address = 0x2139,
            size = 1,
            value = 0
        }
        p.registers["VMDATAHREAD"] = {
            address = 0x213A,
            size = 1,
            value = 0
        }
        p.registers["CGDATAREAD"] = {
            address = 0x213B,
            size = 1,
            value = 0
        }
        p.registers["OPHCT"] = {
            address = 0x213C,
            size = 1,
            value = 0
        }
        p.registers["OPVCT"] = {
            address = 0x213D,
            size = 1,
            value = 0
        }
        p.registers["STAT78"] = {
            address = 0x213F,
            size = 1,
            value = 0
        }
        self.peripherals["SPC700"] = {
            base = 0x00,
            type = "audio_cpu",
            description = "Sony SPC700 Audio CPU (8-bit)",
            registers = {}
        }
        
        local p = self.peripherals["SPC700"]
        p.registers["PC"] = {
            address = 0x00,
            size = 2,
            value = 0
        }
        p.registers["A"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["X"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["Y"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["SP"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["PSW"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["TEST"] = {
            address = 0x0F,
            size = 1,
            value = 0
        }
        self.peripherals["DSP"] = {
            base = 0x00,
            type = "audio",
            description = "S-DSP Audio DSP (8-channel ADPCM)",
            registers = {}
        }
        
        local p = self.peripherals["DSP"]
        p.registers["MVOL_L"] = {
            address = 0x0C,
            size = 1,
            value = 0
        }
        p.registers["MVOL_R"] = {
            address = 0x1C,
            size = 1,
            value = 0
        }
        p.registers["EVOL_L"] = {
            address = 0x2C,
            size = 1,
            value = 0
        }
        p.registers["EVOL_R"] = {
            address = 0x3C,
            size = 1,
            value = 0
        }
        p.registers["KON"] = {
            address = 0x4C,
            size = 1,
            value = 0
        }
        p.registers["KOFF"] = {
            address = 0x5C,
            size = 1,
            value = 0
        }
        p.registers["KONKOFF"] = {
            address = 0x4D,
            size = 1,
            value = 0
        }
        p.registers["FLG"] = {
            address = 0x6C,
            size = 1,
            value = 0
        }
        p.registers["ENDX"] = {
            address = 0x7D,
            size = 1,
            value = 0
        }
        p.registers["EBUST"] = {
            address = 0x6D,
            size = 1,
            value = 0
        }
        p.registers["EDL"] = {
            address = 0x7D,
            size = 1,
            value = 0
        }
        p.registers["ENV0"] = {
            address = 0x00,
            size = 1,
            value = 0
        }
        p.registers["OUT0"] = {
            address = 0x1C,
            size = 1,
            value = 0
        }
        p.registers["ENV1"] = {
            address = 0x01,
            size = 1,
            value = 0
        }
        p.registers["OUT1"] = {
            address = 0x2C,
            size = 1,
            value = 0
        }
        p.registers["ENV2"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["OUT2"] = {
            address = 0x3C,
            size = 1,
            value = 0
        }
        p.registers["ENV3"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["OUT3"] = {
            address = 0x4C,
            size = 1,
            value = 0
        }
        p.registers["ENV4"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["OUT4"] = {
            address = 0x5C,
            size = 1,
            value = 0
        }
        p.registers["ENV5"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["OUT5"] = {
            address = 0x6C,
            size = 1,
            value = 0
        }
        p.registers["ENV6"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["OUT6"] = {
            address = 0x7C,
            size = 1,
            value = 0
        }
        p.registers["ENV7"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["OUT7"] = {
            address = 0x0D,
            size = 1,
            value = 0
        }
        p.registers["V0SRC"] = {
            address = 0x08,
            size = 1,
            value = 0
        }
        p.registers["V1SRC"] = {
            address = 0x09,
            size = 1,
            value = 0
        }
        p.registers["V2SRC"] = {
            address = 0x0A,
            size = 1,
            value = 0
        }
        p.registers["V3SRC"] = {
            address = 0x0B,
            size = 1,
            value = 0
        }
        p.registers["V4SRC"] = {
            address = 0x18,
            size = 1,
            value = 0
        }
        p.registers["V5SRC"] = {
            address = 0x19,
            size = 1,
            value = 0
        }
        p.registers["V6SRC"] = {
            address = 0x1A,
            size = 1,
            value = 0
        }
        p.registers["V7SRC"] = {
            address = 0x1B,
            size = 1,
            value = 0
        }
        p.registers["V0PITCHL"] = {
            address = 0x02,
            size = 1,
            value = 0
        }
        p.registers["V0PITCHH"] = {
            address = 0x03,
            size = 1,
            value = 0
        }
        p.registers["V1PITCHL"] = {
            address = 0x12,
            size = 1,
            value = 0
        }
        p.registers["V1PITCHH"] = {
            address = 0x13,
            size = 1,
            value = 0
        }
        p.registers["V2PITCHL"] = {
            address = 0x22,
            size = 1,
            value = 0
        }
        p.registers["V2PITCHH"] = {
            address = 0x23,
            size = 1,
            value = 0
        }
        p.registers["V3PITCHL"] = {
            address = 0x32,
            size = 1,
            value = 0
        }
        p.registers["V3PITCHH"] = {
            address = 0x33,
            size = 1,
            value = 0
        }
        p.registers["V4PITCHL"] = {
            address = 0x42,
            size = 1,
            value = 0
        }
        p.registers["V4PITCHH"] = {
            address = 0x43,
            size = 1,
            value = 0
        }
        p.registers["V5PITCHL"] = {
            address = 0x52,
            size = 1,
            value = 0
        }
        p.registers["V5PITCHH"] = {
            address = 0x53,
            size = 1,
            value = 0
        }
        p.registers["V6PITCHL"] = {
            address = 0x62,
            size = 1,
            value = 0
        }
        p.registers["V6PITCHH"] = {
            address = 0x63,
            size = 1,
            value = 0
        }
        p.registers["V7PITCHL"] = {
            address = 0x72,
            size = 1,
            value = 0
        }
        p.registers["V7PITCHH"] = {
            address = 0x73,
            size = 1,
            value = 0
        }
        p.registers["V0ADSR0"] = {
            address = 0x04,
            size = 1,
            value = 0
        }
        p.registers["V0ADSR1"] = {
            address = 0x05,
            size = 1,
            value = 0
        }
        p.registers["V0ADSR2"] = {
            address = 0x06,
            size = 1,
            value = 0
        }
        p.registers["V1ADSR0"] = {
            address = 0x14,
            size = 1,
            value = 0
        }
        p.registers["V1ADSR1"] = {
            address = 0x15,
            size = 1,
            value = 0
        }
        p.registers["V1ADSR2"] = {
            address = 0x16,
            size = 1,
            value = 0
        }
        p.registers["V2ADSR0"] = {
            address = 0x24,
            size = 1,
            value = 0
        }
        p.registers["V2ADSR1"] = {
            address = 0x25,
            size = 1,
            value = 0
        }
        p.registers["V2ADSR2"] = {
            address = 0x26,
            size = 1,
            value = 0
        }
        p.registers["V3ADSR0"] = {
            address = 0x34,
            size = 1,
            value = 0
        }
        p.registers["V3ADSR1"] = {
            address = 0x35,
            size = 1,
            value = 0
        }
        p.registers["V3ADSR2"] = {
            address = 0x36,
            size = 1,
            value = 0
        }
        p.registers["V4ADSR0"] = {
            address = 0x44,
            size = 1,
            value = 0
        }
        p.registers["V4ADSR1"] = {
            address = 0x45,
            size = 1,
            value = 0
        }
        p.registers["V4ADSR2"] = {
            address = 0x46,
            size = 1,
            value = 0
        }
        p.registers["V5ADSR0"] = {
            address = 0x54,
            size = 1,
            value = 0
        }
        p.registers["V5ADSR1"] = {
            address = 0x55,
            size = 1,
            value = 0
        }
        p.registers["V5ADSR2"] = {
            address = 0x56,
            size = 1,
            value = 0
        }
        p.registers["V6ADSR0"] = {
            address = 0x64,
            size = 1,
            value = 0
        }
        p.registers["V6ADSR1"] = {
            address = 0x65,
            size = 1,
            value = 0
        }
        p.registers["V6ADSR2"] = {
            address = 0x66,
            size = 1,
            value = 0
        }
        p.registers["V7ADSR0"] = {
            address = 0x74,
            size = 1,
            value = 0
        }
        p.registers["V7ADSR1"] = {
            address = 0x75,
            size = 1,
            value = 0
        }
        p.registers["V7ADSR2"] = {
            address = 0x76,
            size = 1,
            value = 0
        }
        p.registers["V0GAIN"] = {
            address = 0x07,
            size = 1,
            value = 0
        }
        p.registers["V1GAIN"] = {
            address = 0x17,
            size = 1,
            value = 0
        }
        p.registers["V2GAIN"] = {
            address = 0x27,
            size = 1,
            value = 0
        }
        p.registers["V3GAIN"] = {
            address = 0x37,
            size = 1,
            value = 0
        }
        p.registers["V4GAIN"] = {
            address = 0x47,
            size = 1,
            value = 0
        }
        p.registers["V5GAIN"] = {
            address = 0x57,
            size = 1,
            value = 0
        }
        p.registers["V6GAIN"] = {
            address = 0x67,
            size = 1,
            value = 0
        }
        p.registers["V7GAIN"] = {
            address = 0x77,
            size = 1,
            value = 0
        }
        p.registers["V0WAVE"] = {
            address = 0x0D,
            size = 1,
            value = 0
        }
        p.registers["V1WAVE"] = {
            address = 0x1D,
            size = 1,
            value = 0
        }
        p.registers["V2WAVE"] = {
            address = 0x2D,
            size = 1,
            value = 0
        }
        p.registers["V3WAVE"] = {
            address = 0x3D,
            size = 1,
            value = 0
        }
        p.registers["V4WAVE"] = {
            address = 0x4D,
            size = 1,
            value = 0
        }
        p.registers["V5WAVE"] = {
            address = 0x5D,
            size = 1,
            value = 0
        }
        p.registers["V6WAVE"] = {
            address = 0x6D,
            size = 1,
            value = 0
        }
        p.registers["V7WAVE"] = {
            address = 0x7D,
            size = 1,
            value = 0
        }
        self.peripherals["DMA"] = {
            base = 0x4300,
            type = "dma",
            description = "Direct Memory Access Controller",
            registers = {}
        }
        
        local p = self.peripherals["DMA"]
        p.registers["DMAP0"] = {
            address = 0x4300,
            size = 1,
            value = 0
        }
        p.registers["BBAD0"] = {
            address = 0x4301,
            size = 1,
            value = 0
        }
        p.registers["A1T0L"] = {
            address = 0x4302,
            size = 1,
            value = 0
        }
        p.registers["A1T0H"] = {
            address = 0x4303,
            size = 1,
            value = 0
        }
        p.registers["A1B0"] = {
            address = 0x4304,
            size = 1,
            value = 0
        }
        p.registers["DAS0L"] = {
            address = 0x4305,
            size = 1,
            value = 0
        }
        p.registers["DAS0H"] = {
            address = 0x4306,
            size = 1,
            value = 0
        }
        p.registers["DASB0"] = {
            address = 0x4307,
            size = 1,
            value = 0
        }
        p.registers["A2A0"] = {
            address = 0x4308,
            size = 1,
            value = 0
        }
        p.registers["A2A1"] = {
            address = 0x4309,
            size = 1,
            value = 0
        }
        p.registers["A2B0"] = {
            address = 0x430A,
            size = 1,
            value = 0
        }
        p.registers["NTT0"] = {
            address = 0x430B,
            size = 1,
            value = 0
        }
        p.registers["DMAP1"] = {
            address = 0x4310,
            size = 1,
            value = 0
        }
        p.registers["BBAD1"] = {
            address = 0x4311,
            size = 1,
            value = 0
        }
        p.registers["A1T1L"] = {
            address = 0x4312,
            size = 1,
            value = 0
        }
        p.registers["A1T1H"] = {
            address = 0x4313,
            size = 1,
            value = 0
        }
        p.registers["A1B1"] = {
            address = 0x4314,
            size = 1,
            value = 0
        }
        p.registers["DAS1L"] = {
            address = 0x4315,
            size = 1,
            value = 0
        }
        p.registers["DAS1H"] = {
            address = 0x4316,
            size = 1,
            value = 0
        }
        p.registers["DASB1"] = {
            address = 0x4317,
            size = 1,
            value = 0
        }
        p.registers["DMAP2"] = {
            address = 0x4320,
            size = 1,
            value = 0
        }
        p.registers["BBAD2"] = {
            address = 0x4321,
            size = 1,
            value = 0
        }
        p.registers["A1T2L"] = {
            address = 0x4322,
            size = 1,
            value = 0
        }
        p.registers["A1T2H"] = {
            address = 0x4323,
            size = 1,
            value = 0
        }
        p.registers["A1B2"] = {
            address = 0x4324,
            size = 1,
            value = 0
        }
        p.registers["DAS2L"] = {
            address = 0x4325,
            size = 1,
            value = 0
        }
        p.registers["DAS2H"] = {
            address = 0x4326,
            size = 1,
            value = 0
        }
        p.registers["DASB2"] = {
            address = 0x4327,
            size = 1,
            value = 0
        }
        p.registers["DMAP3"] = {
            address = 0x4330,
            size = 1,
            value = 0
        }
        p.registers["BBAD3"] = {
            address = 0x4331,
            size = 1,
            value = 0
        }
        p.registers["A1T3L"] = {
            address = 0x4332,
            size = 1,
            value = 0
        }
        p.registers["A1T3H"] = {
            address = 0x4333,
            size = 1,
            value = 0
        }
        p.registers["A1B3"] = {
            address = 0x4334,
            size = 1,
            value = 0
        }
        p.registers["DAS3L"] = {
            address = 0x4335,
            size = 1,
            value = 0
        }
        p.registers["DAS3H"] = {
            address = 0x4336,
            size = 1,
            value = 0
        }
        p.registers["DASB3"] = {
            address = 0x4337,
            size = 1,
            value = 0
        }
        p.registers["MDMAEN"] = {
            address = 0x4350,
            size = 1,
            value = 0
        }
        self.peripherals["HDMA"] = {
            base = 0x4380,
            type = "dma",
            description = "Horizontal DMA (scanline-based)",
            registers = {}
        }
        
        local p = self.peripherals["HDMA"]
        p.registers["HDMAP0"] = {
            address = 0x4380,
            size = 1,
            value = 0
        }
        p.registers["HBAD0"] = {
            address = 0x4381,
            size = 1,
            value = 0
        }
        p.registers["A1T0L"] = {
            address = 0x4382,
            size = 1,
            value = 0
        }
        p.registers["A1T0H"] = {
            address = 0x4383,
            size = 1,
            value = 0
        }
        p.registers["A1B0"] = {
            address = 0x4384,
            size = 1,
            value = 0
        }
        p.registers["DAS0L"] = {
            address = 0x4385,
            size = 1,
            value = 0
        }
        p.registers["DAS0H"] = {
            address = 0x4386,
            size = 1,
            value = 0
        }
        p.registers["HDMAP1"] = {
            address = 0x4388,
            size = 1,
            value = 0
        }
        p.registers["HBAD1"] = {
            address = 0x4389,
            size = 1,
            value = 0
        }
        p.registers["A1T1L"] = {
            address = 0x438A,
            size = 1,
            value = 0
        }
        p.registers["A1T1H"] = {
            address = 0x438B,
            size = 1,
            value = 0
        }
        p.registers["A1B1"] = {
            address = 0x438C,
            size = 1,
            value = 0
        }
        p.registers["DAS1L"] = {
            address = 0x438D,
            size = 1,
            value = 0
        }
        p.registers["DAS1H"] = {
            address = 0x438E,
            size = 1,
            value = 0
        }
        p.registers["HDMAP2"] = {
            address = 0x4390,
            size = 1,
            value = 0
        }
        p.registers["HBAD2"] = {
            address = 0x4391,
            size = 1,
            value = 0
        }
        p.registers["A1T2L"] = {
            address = 0x4392,
            size = 1,
            value = 0
        }
        p.registers["A1T2H"] = {
            address = 0x4393,
            size = 1,
            value = 0
        }
        p.registers["A1B2"] = {
            address = 0x4394,
            size = 1,
            value = 0
        }
        p.registers["DAS2L"] = {
            address = 0x4395,
            size = 1,
            value = 0
        }
        p.registers["DAS2H"] = {
            address = 0x4396,
            size = 1,
            value = 0
        }
        p.registers["HDMAP3"] = {
            address = 0x4398,
            size = 1,
            value = 0
        }
        p.registers["HBAD3"] = {
            address = 0x4399,
            size = 1,
            value = 0
        }
        p.registers["A1T3L"] = {
            address = 0x439A,
            size = 1,
            value = 0
        }
        p.registers["A1T3H"] = {
            address = 0x439B,
            size = 1,
            value = 0
        }
        p.registers["A1B3"] = {
            address = 0x439C,
            size = 1,
            value = 0
        }
        p.registers["DAS3L"] = {
            address = 0x439D,
            size = 1,
            value = 0
        }
        p.registers["DAS3H"] = {
            address = 0x439E,
            size = 1,
            value = 0
        }
        p.registers["HDMAEN"] = {
            address = 0x43F0,
            size = 1,
            value = 0
        }
        self.peripherals["CONTROLLER1"] = {
            base = 0x4016,
            type = "input",
            description = "Controller Port 1",
            registers = {}
        }
        
        local p = self.peripherals["CONTROLLER1"]
        p.registers["JOYPAD1"] = {
            address = 0x4016,
            size = 1,
            value = 0
        }
        p.registers["JOYSTROBE"] = {
            address = 0x4016,
            size = 1,
            value = 0
        }
        self.peripherals["CONTROLLER2"] = {
            base = 0x4017,
            type = "input",
            description = "Controller Port 2",
            registers = {}
        }
        
        local p = self.peripherals["CONTROLLER2"]
        p.registers["JOYPAD2"] = {
            address = 0x4017,
            size = 1,
            value = 0
        }
        p.registers["RDNMI"] = {
            address = 0x4210,
            size = 1,
            value = 0
        }
        p.registers["TIMEUP"] = {
            address = 0x4211,
            size = 1,
            value = 0
        }
        p.registers["HVBJOY"] = {
            address = 0x4212,
            size = 1,
            value = 0
        }
        self.peripherals["TIMER"] = {
            base = 0x4200,
            type = "timer",
            description = "Timer / IRQ Control",
            registers = {}
        }
        
        local p = self.peripherals["TIMER"]
        p.registers["NMITIMEN"] = {
            address = 0x4200,
            size = 1,
            value = 0
        }
        p.registers["WRI00"] = {
            address = 0x4201,
            size = 1,
            value = 0
        }
        p.registers["HTIMEL"] = {
            address = 0x4202,
            size = 1,
            value = 0
        }
        p.registers["HTIMEH"] = {
            address = 0x4203,
            size = 1,
            value = 0
        }
        p.registers["VTIMEL"] = {
            address = 0x4204,
            size = 1,
            value = 0
        }
        p.registers["VTIMEH"] = {
            address = 0x4205,
            size = 1,
            value = 0
        }
        p.registers["MEMSEL"] = {
            address = 0x420D,
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
            name = Ricoh_5A22.DEVICE_NAME,
            manufacturer = Ricoh_5A22.MANUFACTURER,
            family = Ricoh_5A22.FAMILY,
            version = Ricoh_5A22.VERSION,
            architecture = Ricoh_5A22.ARCHITECTURE,
            bits = Ricoh_5A22.BITS,
            clock_frequency = Ricoh_5A22.CLOCK_FREQUENCY
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
        return string.format("Ricoh_5A22(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function Ricoh_5A22.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function Ricoh_5A22.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function Ricoh_5A22.print_device_info(device)
    device = device or Ricoh_5A22.new()
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

function Ricoh_5A22.print_registers(device)
    device = device or Ricoh_5A22.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            Ricoh_5A22.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function Ricoh_5A22.example()
    print("=== Ricoh-5A22设备示例 ===")
    
    -- 创建设备实例
    local device = Ricoh_5A22.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    Ricoh_5A22.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["A"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("A", 0x55)
        print("写入 A: " .. Ricoh_5A22.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("A")
        print("读取 A: " .. Ricoh_5A22.hex(value))
        
        -- 位操作
        device:set_bit("A", 0, true)
        local bit0 = device:get_bit("A", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    Ricoh_5A22.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("Ricoh_5A22.lua$") then
    Ricoh_5A22.example()
end

return Ricoh_5A22

--[[
  ARM7TDMI设备定义 - Lua模块
  生成自: ARM/ARM7/ARM7TDMI
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Game Boy Advance main processor - ARM7TDMI @ 16.78MHz with 32-bit ARM + 16-bit Thumb instruction sets
  CPU架构: ARM7TDMI
  位宽: 32位
  时钟频率: 16780000 Hz
]]

local ARM7TDMI = {}

-- 设备信息
ARM7TDMI.DEVICE_NAME = "ARM7TDMI"
ARM7TDMI.MANUFACTURER = "ARM"
ARM7TDMI.FAMILY = "ARM7"
ARM7TDMI.VERSION = "1.0"
ARM7TDMI.ARCHITECTURE = "ARM7TDMI"
ARM7TDMI.BITS = 32
ARM7TDMI.CLOCK_FREQUENCY = 16780000

-- 寄存器地址定义
ARM7TDMI.R0_ADDR = 0x00  -- General Purpose Register 0
ARM7TDMI.R1_ADDR = 0x04  -- General Purpose Register 1
ARM7TDMI.R2_ADDR = 0x08  -- General Purpose Register 2
ARM7TDMI.R3_ADDR = 0x0C  -- General Purpose Register 3
ARM7TDMI.R4_ADDR = 0x10  -- General Purpose Register 4
ARM7TDMI.R5_ADDR = 0x14  -- General Purpose Register 5
ARM7TDMI.R6_ADDR = 0x18  -- General Purpose Register 6
ARM7TDMI.R7_ADDR = 0x1C  -- General Purpose Register 7
ARM7TDMI.R8_ADDR = 0x20  -- General Purpose Register 8
ARM7TDMI.R9_ADDR = 0x24  -- General Purpose Register 9 / SB
ARM7TDMI.R10_ADDR = 0x28  -- General Purpose Register 10 / SL
ARM7TDMI.R11_ADDR = 0x2C  -- Frame Pointer / FP
ARM7TDMI.R12_ADDR = 0x30  -- Intra-Procedure-call Scratch Register / IP
ARM7TDMI.R13_ADDR = 0x34  -- Stack Pointer / SP
ARM7TDMI.R14_ADDR = 0x38  -- Link Register / LR
ARM7TDMI.R15_ADDR = 0x3C  -- Program Counter / PC
ARM7TDMI.CPSR_ADDR = 0x40  -- Current Program Status Register
ARM7TDMI.CPSR_MODE_BIT = 0  -- Processor Mode (10000=User, 10001=FIQ, 10010=IRQ, 10011=SVC, 10111=ABT, 11011=UND, 11111=SYS)
ARM7TDMI.CPSR_T_BIT = 5  -- Thumb State Bit (1=Thumb mode)
ARM7TDMI.CPSR_F_BIT = 6  -- FIQ Disable
ARM7TDMI.CPSR_I_BIT = 7  -- IRQ Disable
ARM7TDMI.CPSR_A_BIT = 8  -- Imprecise Data Abort Disable
ARM7TDMI.CPSR_E_BIT = 9  -- Endianness (0=Little)
ARM7TDMI.CPSR_GE_BIT = 0  -- Greater-than-or-Equal flags
ARM7TDMI.CPSR_N_BIT = 31  -- Negative
ARM7TDMI.CPSR_Z_BIT = 30  -- Zero
ARM7TDMI.CPSR_C_BIT = 29  -- Carry
ARM7TDMI.CPSR_V_BIT = 28  -- Overflow
ARM7TDMI.SPSR_SVC_ADDR = 0x44  -- Saved PSR (Supervisor Mode)
ARM7TDMI.SPSR_ABT_ADDR = 0x48  -- Saved PSR (Abort Mode)
ARM7TDMI.SPSR_IRQ_ADDR = 0x4C  -- Saved PSR (IRQ Mode)
ARM7TDMI.SPSR_FIQ_ADDR = 0x50  -- Saved PSR (FIQ Mode)

-- 内存段定义
ARM7TDMI.IWRAM_START = 0x03000000
ARM7TDMI.IWRAM_END = 0x03007FFF
ARM7TDMI.IWRAM_SIZE = 32768  -- Internal Work RAM (32KB, 2-cycle access)
ARM7TDMI.IWRAM_FAST_START = 0x03008000
ARM7TDMI.IWRAM_FAST_END = 0x03FFFFFF
ARM7TDMI.IWRAM_FAST_SIZE = 32752  -- Internal Work RAM Fast (high-speed region)
ARM7TDMI.VRAM_START = 0x06000000
ARM7TDMI.VRAM_END = 0x06017FFF
ARM7TDMI.VRAM_SIZE = 98304  -- Video RAM (96KB + 64KB OBJ VRAM)
ARM7TDMI.PALETTE_START = 0x05000200
ARM7TDMI.PALETTE_END = 0x050003FF
ARM7TDMI.PALETTE_SIZE = 512  -- BG Palette RAM (256 colors x 2 bytes)
ARM7TDMI.OBJ_PALETTE_START = 0x05000400
ARM7TDMI.OBJ_PALETTE_END = 0x050005FF
ARM7TDMI.OBJ_PALETTE_SIZE = 512  -- Object Palette RAM
ARM7TDMI.OAM_START = 0x07000000
ARM7TDMI.OAM_END = 0x070003FF
ARM7TDMI.OAM_SIZE = 1024  -- Object Attribute Memory (OAM, 128 sprites)
ARM7TDMI.ROM_START = 0x08000000
ARM7TDMI.ROM_END = 0x09FFFFFF
ARM7TDMI.ROM_SIZE = 33554432  -- Cartridge ROM (max 32MB)
ARM7TDMI.CART_RAM_START = 0x0E000000
ARM7TDMI.CART_RAM_END = 0x0E00FFFF
ARM7TDMI.CART_RAM_SIZE = 65536  -- Cartridge SRAM / Flash
ARM7TDMI.BIOS_START = 0x00000000
ARM7TDMI.BIOS_END = 0x00003FFF
ARM7TDMI.BIOS_SIZE = 16384  -- GBA BIOS (16KB)
ARM7TDMI.IO_REGS_START = 0x04000000
ARM7TDMI.IO_REGS_END = 0x04FFFFFF
ARM7TDMI.IO_REGS_SIZE = 16777216  -- I/O Registers (MMIO)

-- 外设定义
-- LCD Controller
ARM7TDMI.LCD_BASE = 0x04000000
ARM7TDMI.LCD_DISPCNT_ADDR = 0x04000000
ARM7TDMI.LCD_DISPCNT_BG_MODE_BIT = 0  -- BG Mode (0-6)
ARM7TDMI.LCD_DISPCNT_GB_WINDOW_BIT = 5  -- Game Boy Window Enable
ARM7TDMI.LCD_DISPCNT_WIN0_ENABLE_BIT = 13  -- Window 0 Enable
ARM7TDMI.LCD_DISPCNT_WIN1_ENABLE_BIT = 14  -- Window 1 Enable
ARM7TDMI.LCD_DISPCNT_OBJ_WIN_BIT = 15  -- Object Window Enable
ARM7TDMI.LCD_DISPCNT_BG0_ENABLE_BIT = 8  -- BG0 Enable
ARM7TDMI.LCD_DISPCNT_BG1_ENABLE_BIT = 9  -- BG1 Enable
ARM7TDMI.LCD_DISPCNT_BG2_ENABLE_BIT = 10  -- BG2 Enable
ARM7TDMI.LCD_DISPCNT_BG3_ENABLE_BIT = 11  -- BG3 Enable
ARM7TDMI.LCD_DISPCNT_OBJ_ENABLE_BIT = 12  -- Object/Sprite Enable
ARM7TDMI.LCD_GREEN_SWAP_ADDR = 0x04000002
ARM7TDMI.LCD_DISPSTAT_ADDR = 0x04000004
ARM7TDMI.LCD_DISPSTAT_V_COUNT_BIT = 0  -- Vertical Line Counter
ARM7TDMI.LCD_DISPSTAT_VBLANK_FLAG_BIT = 0  -- V-Blank Flag (read-only)
ARM7TDMI.LCD_DISPSTAT_HBLANK_FLAG_BIT = 1  -- H-Blank Flag (read-only)
ARM7TDMI.LCD_DISPSTAT_V_COUNT_FLAG_BIT = 2  -- V-Count Flag (LY==LYC)
ARM7TDMI.LCD_DISPSTAT_VBLANK_IRQ_BIT = 3  -- V-Blank IRQ Enable
ARM7TDMI.LCD_DISPSTAT_HBLANK_IRQ_BIT = 4  -- H-Blank IRQ Enable
ARM7TDMI.LCD_DISPSTAT_VCOUNT_IRQ_BIT = 5  -- V-Count IRQ Enable
ARM7TDMI.LCD_VCOUNT_ADDR = 0x04000006
ARM7TDMI.LCD_BG0CNT_ADDR = 0x04000008
ARM7TDMI.LCD_BG1CNT_ADDR = 0x0400000A
ARM7TDMI.LCD_BG2CNT_ADDR = 0x0400000C
ARM7TDMI.LCD_BG3CNT_ADDR = 0x0400000E
ARM7TDMI.LCD_BG0HOFS_ADDR = 0x04000010
ARM7TDMI.LCD_BG0VOFS_ADDR = 0x04000012
ARM7TDMI.LCD_BG1HOFS_ADDR = 0x04000014
ARM7TDMI.LCD_BG1VOFS_ADDR = 0x04000016
ARM7TDMI.LCD_BG2HOFS_ADDR = 0x04000018
ARM7TDMI.LCD_BG2VOFS_ADDR = 0x0400001A
ARM7TDMI.LCD_BG3HOFS_ADDR = 0x0400001C
ARM7TDMI.LCD_BG3VOFS_ADDR = 0x0400001E
ARM7TDMI.LCD_BG2PA_ADDR = 0x04000020
ARM7TDMI.LCD_BG2PB_ADDR = 0x04000022
ARM7TDMI.LCD_BG2PC_ADDR = 0x04000024
ARM7TDMI.LCD_BG2PD_ADDR = 0x04000026
ARM7TDMI.LCD_BG2X_ADDR = 0x04000028
ARM7TDMI.LCD_BG2Y_ADDR = 0x0400002C
ARM7TDMI.LCD_BG3PA_ADDR = 0x04000030
ARM7TDMI.LCD_BG3PB_ADDR = 0x04000032
ARM7TDMI.LCD_BG3PC_ADDR = 0x04000034
ARM7TDMI.LCD_BG3PD_ADDR = 0x04000036
ARM7TDMI.LCD_BG3X_ADDR = 0x04000038
ARM7TDMI.LCD_BG3Y_ADDR = 0x0400003C
ARM7TDMI.LCD_WIN0H_ADDR = 0x04000040
ARM7TDMI.LCD_WIN1H_ADDR = 0x04000042
ARM7TDMI.LCD_WIN0V_ADDR = 0x04000044
ARM7TDMI.LCD_WIN1V_ADDR = 0x04000046
ARM7TDMI.LCD_WININ_ADDR = 0x04000048
ARM7TDMI.LCD_WINOUT_ADDR = 0x04000049
ARM7TDMI.LCD_MOSAIC_ADDR = 0x0400004C
ARM7TDMI.LCD_BLDCNT_ADDR = 0x04000050
ARM7TDMI.LCD_BLDCNT_BG1ST_BIT = 0  -- BG1 1st Target
ARM7TDMI.LCD_BLDCNT_BG2ST_BIT = 1  -- BG2 1st Target
ARM7TDMI.LCD_BLDCNT_BG3ST_BIT = 2  -- BG3 1st Target
ARM7TDMI.LCD_BLDCNT_OBJST_BIT = 3  -- Object 1st Target
ARM7TDMI.LCD_BLDCNT_BDST_BIT = 4  -- Backdrop 1st Target
ARM7TDMI.LCD_BLDCNT_BLEND_MODE_BIT = 0  -- Blend Mode (0=None, 1=Alpha, 2=Increase, 3=Decrease)
ARM7TDMI.LCD_BLDCNT_BG1ST2_BIT = 8  -- BG1 2nd Target
ARM7TDMI.LCD_BLDCNT_BG2ST2_BIT = 9  -- BG2 2nd Target
ARM7TDMI.LCD_BLDCNT_BG3ST2_BIT = 10  -- BG3 2nd Target
ARM7TDMI.LCD_BLDCNT_OBJST2_BIT = 11  -- Object 2nd Target
ARM7TDMI.LCD_BLDCNT_BDST2_BIT = 12  -- Backdrop 2nd Target
ARM7TDMI.LCD_BLDALPHA_ADDR = 0x04000052
ARM7TDMI.LCD_BLDY_ADDR = 0x04000054
-- Direct Memory Access Controller
ARM7TDMI.DMA_BASE = 0x040000B0
ARM7TDMI.DMA_DMA0SAD_ADDR = 0x040000B0
ARM7TDMI.DMA_DMA0DAD_ADDR = 0x040000B4
ARM7TDMI.DMA_DMA0CNT_L_ADDR = 0x040000B8
ARM7TDMI.DMA_DMA0CNT_H_ADDR = 0x040000BA
ARM7TDMI.DMA_DMA0CNT_H_TRANSFER_COUNT_BIT = 0  -- Number of Transfers
ARM7TDMI.DMA_DMA0CNT_H_DEST_ADD_MODE_BIT = 0  -- Dest Address Control (0=fix, 1=inc, 2=dec, 3=inc+reload)
ARM7TDMI.DMA_DMA0CNT_H_SRC_ADD_MODE_BIT = 0  -- Source Address Control (0=fix, 1=inc, 2=dec)
ARM7TDMI.DMA_DMA0CNT_H_REPEAT_BIT = 18  -- Repeat (for 16-bit repeat mode)
ARM7TDMI.DMA_DMA0CNT_H_WORD_SIZE_BIT = 20  -- Word Size (0=16-bit, 1=32-bit)
ARM7TDMI.DMA_DMA0CNT_H_DRQ_BIT = 27  -- DRQ Trigger (DMA from external source)
ARM7TDMI.DMA_DMA0CNT_H_TIMING_BIT = 0  -- Start Timing (0=Now, 1=V-Blank, 2=H-Blank, 3=Special)
ARM7TDMI.DMA_DMA0CNT_H_ENABLE_BIT = 31  -- DMA Enable
ARM7TDMI.DMA_DMA1SAD_ADDR = 0x040000BC
ARM7TDMI.DMA_DMA1DAD_ADDR = 0x040000C0
ARM7TDMI.DMA_DMA1CNT_L_ADDR = 0x040000C4
ARM7TDMI.DMA_DMA1CNT_H_ADDR = 0x040000C6
ARM7TDMI.DMA_DMA2SAD_ADDR = 0x040000C8
ARM7TDMI.DMA_DMA2DAD_ADDR = 0x040000CC
ARM7TDMI.DMA_DMA2CNT_L_ADDR = 0x040000D0
ARM7TDMI.DMA_DMA2CNT_H_ADDR = 0x040000D2
ARM7TDMI.DMA_DMA3SAD_ADDR = 0x040000D4
ARM7TDMI.DMA_DMA3DAD_ADDR = 0x040000D8
ARM7TDMI.DMA_DMA3CNT_L_ADDR = 0x040000DC
ARM7TDMI.DMA_DMA3CNT_H_ADDR = 0x040000DE
-- Timer Units (4 timers)
ARM7TDMI.TIMER_BASE = 0x04000100
ARM7TDMI.TIMER_TM0CNT_L_ADDR = 0x04000100
ARM7TDMI.TIMER_TM0CNT_H_ADDR = 0x04000102
ARM7TDMI.TIMER_TM0CNT_H_PRESCALER_BIT = 0  -- Prescaler (0=1, 1=64, 2=256, 3=1024)
ARM7TDMI.TIMER_TM0CNT_H_COUNT_UP_BIT = 2  -- Count Up (cascade mode)
ARM7TDMI.TIMER_TM0CNT_H_IRQ_ENABLE_BIT = 6  -- Timer IRQ Enable
ARM7TDMI.TIMER_TM0CNT_H_ENABLE_BIT = 7  -- Timer Enable
ARM7TDMI.TIMER_TM1CNT_L_ADDR = 0x04000104
ARM7TDMI.TIMER_TM1CNT_H_ADDR = 0x04000106
ARM7TDMI.TIMER_TM2CNT_L_ADDR = 0x04000108
ARM7TDMI.TIMER_TM2CNT_H_ADDR = 0x0400010A
ARM7TDMI.TIMER_TM3CNT_L_ADDR = 0x0400010C
ARM7TDMI.TIMER_TM3CNT_H_ADDR = 0x0400010E
-- Serial I/O (JOY BUS / Link Cable)
ARM7TDMI.SIO_BASE = 0x04000120
ARM7TDMI.SIO_SIOCNT_ADDR = 0x04000120
ARM7TDMI.SIO_SIOCNT_CLOCK_SEL_BIT = 0  -- Baud Rate Clock (0=9600, 1=57600, 2=115200, 3=768000)
ARM7TDMI.SIO_SIOCNT_SO_ENABLE_BIT = 3  -- SO Output Enable
ARM7TDMI.SIO_SIOCNT_RECV_ENABLE_BIT = 5  -- Receive Enable
ARM7TDMI.SIO_SIOCNT_SEND_ENABLE_BIT = 6  -- Send Enable
ARM7TDMI.SIO_SIOCNT_START_BIT_BIT = 7  -- Start Transfer
ARM7TDMI.SIO_SIODATA8_ADDR = 0x0400012A
ARM7TDMI.SIO_JOYCNT_ADDR = 0x04000130
ARM7TDMI.SIO_JOYSTAT_ADDR = 0x04000134
ARM7TDMI.SIO_JOY_RECV_ADDR = 0x04000150
ARM7TDMI.SIO_JOY_TRANS_ADDR = 0x04000154
-- Key Input
ARM7TDMI.KEYINPUT_BASE = 0x04000130
ARM7TDMI.KEYINPUT_KEYINPUT_ADDR = 0x04000130
ARM7TDMI.KEYINPUT_KEYINPUT_A_BIT = 0  -- A Button (0=Pressed)
ARM7TDMI.KEYINPUT_KEYINPUT_B_BIT = 1  -- B Button (0=Pressed)
ARM7TDMI.KEYINPUT_KEYINPUT_SELECT_BIT = 2  -- Select Button (0=Pressed)
ARM7TDMI.KEYINPUT_KEYINPUT_START_BIT = 3  -- Start Button (0=Pressed)
ARM7TDMI.KEYINPUT_KEYINPUT_RIGHT_BIT = 4  -- D-Pad Right (0=Pressed)
ARM7TDMI.KEYINPUT_KEYINPUT_LEFT_BIT = 5  -- D-Pad Left (0=Pressed)
ARM7TDMI.KEYINPUT_KEYINPUT_UP_BIT = 6  -- D-Pad Up (0=Pressed)
ARM7TDMI.KEYINPUT_KEYINPUT_DOWN_BIT = 7  -- D-Pad Down (0=Pressed)
ARM7TDMI.KEYINPUT_KEYINPUT_R_BIT = 8  -- R Shoulder Button (0=Pressed)
ARM7TDMI.KEYINPUT_KEYINPUT_L_BIT = 9  -- L Shoulder Button (0=Pressed)
ARM7TDMI.KEYINPUT_KEYCNT_ADDR = 0x04000132
ARM7TDMI.KEYINPUT_KEYCNT_KEY_MASK_BIT = 0  -- Key Interrupt Enable Mask
ARM7TDMI.KEYINPUT_KEYCNT_IRQ_ENABLE_BIT = 14  -- Key Interrupt Enable
-- Interrupt Control
ARM7TDMI.INTERRUPT_BASE = 0x04000200
ARM7TDMI.INTERRUPT_IME_ADDR = 0x04000208
ARM7TDMI.INTERRUPT_IE_ADDR = 0x04000210
ARM7TDMI.INTERRUPT_IE_VBLANK_BIT = 0  -- V-Blank Interrupt Enable
ARM7TDMI.INTERRUPT_IE_HBLANK_BIT = 1  -- H-Blank Interrupt Enable
ARM7TDMI.INTERRUPT_IE_VCOUNT_BIT = 2  -- V-Count Match Interrupt Enable
ARM7TDMI.INTERRUPT_IE_TIMER0_BIT = 3  -- Timer 0 Interrupt Enable
ARM7TDMI.INTERRUPT_IE_TIMER1_BIT = 4  -- Timer 1 Interrupt Enable
ARM7TDMI.INTERRUPT_IE_TIMER2_BIT = 5  -- Timer 2 Interrupt Enable
ARM7TDMI.INTERRUPT_IE_TIMER3_BIT = 6  -- Timer 3 Interrupt Enable
ARM7TDMI.INTERRUPT_IE_SIO_BIT = 7  -- Serial I/O Interrupt Enable
ARM7TDMI.INTERRUPT_IE_DMA0_BIT = 8  -- DMA 0 Interrupt Enable
ARM7TDMI.INTERRUPT_IE_DMA1_BIT = 9  -- DMA 1 Interrupt Enable
ARM7TDMI.INTERRUPT_IE_DMA2_BIT = 10  -- DMA 2 Interrupt Enable
ARM7TDMI.INTERRUPT_IE_DMA3_BIT = 11  -- DMA 3 Interrupt Enable
ARM7TDMI.INTERRUPT_IE_KEYPAD_BIT = 12  -- Keypad Interrupt Enable
ARM7TDMI.INTERRUPT_IE_CART_BIT = 13  -- Game Pak Interrupt Enable
ARM7TDMI.INTERRUPT_IF_ADDR = 0x04000214
-- Waitstate Control
ARM7TDMI.WAITCNT_BASE = 0x04000204
ARM7TDMI.WAITCNT_WAITCNT_ADDR = 0x04000204
ARM7TDMI.WAITCNT_WAITCNT_PHI_OD_BIT = 0  -- PHI Terminal Output (0=Disable)
ARM7TDMI.WAITCNT_WAITCNT_SRAM_WS_BIT = 0  -- SRAM Wait State (0=4, 1=3, 2=2, 3=8 cycles)
ARM7TDMI.WAITCNT_WAITCNT_WS0_N_BIT = 0  -- Wait State 0 (ROM/SRAM 1st access)
ARM7TDMI.WAITCNT_WAITCNT_WS0_S_BIT = 5  -- Wait State 0 (ROM/SRAM 2nd access)
ARM7TDMI.WAITCNT_WAITCNT_WS1_N_BIT = 0  -- Wait State 1 (ROM 2nd access)
ARM7TDMI.WAITCNT_WAITCNT_WS1_S_BIT = 8  -- Wait State 1 (ROM 2nd access short)
ARM7TDMI.WAITCNT_WAITCNT_WS2_N_BIT = 0  -- Wait State 2 (ROM 3rd access)
ARM7TDMI.WAITCNT_WAITCNT_WS2_S_BIT = 11  -- Wait State 2 (ROM 3rd access short)
ARM7TDMI.WAITCNT_WAITCNT_PREFE_BIT = 12  -- Prefetch Enable (GBA SP only)

-- 中断向量定义
ARM7TDMI.INT_VBLANK = 0  -- V-Blank Interrupt
ARM7TDMI.INT_HBLANK = 1  -- H-Blank Interrupt
ARM7TDMI.INT_VCOUNT = 2  -- V-Count Match Interrupt
ARM7TDMI.INT_TIMER0 = 3  -- Timer 0 Overflow Interrupt
ARM7TDMI.INT_TIMER1 = 4  -- Timer 1 Overflow Interrupt
ARM7TDMI.INT_TIMER2 = 5  -- Timer 2 Overflow Interrupt
ARM7TDMI.INT_TIMER3 = 6  -- Timer 3 Overflow Interrupt
ARM7TDMI.INT_SIO = 7  -- Serial I/O Interrupt
ARM7TDMI.INT_DMA0 = 8  -- DMA 0 Complete Interrupt
ARM7TDMI.INT_DMA1 = 9  -- DMA 1 Complete Interrupt
ARM7TDMI.INT_DMA2 = 10  -- DMA 2 Complete Interrupt
ARM7TDMI.INT_DMA3 = 11  -- DMA 3 Complete Interrupt
ARM7TDMI.INT_KEYPAD = 12  -- Keypad Interrupt
ARM7TDMI.INT_CART = 13  -- Game Pak Interrupt

-- 引脚定义
ARM7TDMI.PIN_VSS = 1  -- Ground
ARM7TDMI.PIN_VDD = 2  -- Power Supply
ARM7TDMI.PIN_CLK = 3  -- System Clock Input (16.78MHz)
ARM7TDMI.PIN_RESET = 4  -- Reset Signal
ARM7TDMI.PIN_NMI = 5  -- Non-Maskable Interrupt
ARM7TDMI.PIN_IRQ = 6  -- Interrupt Request
ARM7TDMI.PIN_AB0 = 7  -- Address Bus Bit 0
ARM7TDMI.PIN_AB1 = 8  -- Address Bus Bit 1
ARM7TDMI.PIN_AB2 = 9  -- Address Bus Bit 2
ARM7TDMI.PIN_AB3 = 10  -- Address Bus Bit 3
ARM7TDMI.PIN_AB4 = 11  -- Address Bus Bit 4
ARM7TDMI.PIN_AB5 = 12  -- Address Bus Bit 5
ARM7TDMI.PIN_AB6 = 13  -- Address Bus Bit 6
ARM7TDMI.PIN_AB7 = 14  -- Address Bus Bit 7
ARM7TDMI.PIN_AB8 = 15  -- Address Bus Bit 8
ARM7TDMI.PIN_AB9 = 16  -- Address Bus Bit 9
ARM7TDMI.PIN_AB10 = 17  -- Address Bus Bit 10
ARM7TDMI.PIN_AB11 = 18  -- Address Bus Bit 11
ARM7TDMI.PIN_AB12 = 19  -- Address Bus Bit 12
ARM7TDMI.PIN_AB13 = 20  -- Address Bus Bit 13
ARM7TDMI.PIN_AB14 = 21  -- Address Bus Bit 14
ARM7TDMI.PIN_AB15 = 22  -- Address Bus Bit 15
ARM7TDMI.PIN_AB16 = 23  -- Address Bus Bit 16
ARM7TDMI.PIN_AB17 = 24  -- Address Bus Bit 17
ARM7TDMI.PIN_AB18 = 25  -- Address Bus Bit 18
ARM7TDMI.PIN_AB19 = 26  -- Address Bus Bit 19
ARM7TDMI.PIN_AB20 = 27  -- Address Bus Bit 20
ARM7TDMI.PIN_AB21 = 28  -- Address Bus Bit 21
ARM7TDMI.PIN_AB22 = 29  -- Address Bus Bit 22
ARM7TDMI.PIN_AB23 = 30  -- Address Bus Bit 23
ARM7TDMI.PIN_AB24 = 31  -- Address Bus Bit 24
ARM7TDMI.PIN_AB25 = 32  -- Address Bus Bit 25
ARM7TDMI.PIN_AB26 = 33  -- Address Bus Bit 26
ARM7TDMI.PIN_AB27 = 34  -- Address Bus Bit 27
ARM7TDMI.PIN_AB28 = 35  -- Address Bus Bit 28
ARM7TDMI.PIN_AB29 = 36  -- Address Bus Bit 29
ARM7TDMI.PIN_AB30 = 37  -- Address Bus Bit 30
ARM7TDMI.PIN_AB31 = 38  -- Address Bus Bit 31
ARM7TDMI.PIN_DB0 = 39  -- Data Bus Bit 0
ARM7TDMI.PIN_DB1 = 40  -- Data Bus Bit 1
ARM7TDMI.PIN_DB2 = 41  -- Data Bus Bit 2
ARM7TDMI.PIN_DB3 = 42  -- Data Bus Bit 3
ARM7TDMI.PIN_DB4 = 43  -- Data Bus Bit 4
ARM7TDMI.PIN_DB5 = 44  -- Data Bus Bit 5
ARM7TDMI.PIN_DB6 = 45  -- Data Bus Bit 6
ARM7TDMI.PIN_DB7 = 46  -- Data Bus Bit 7
ARM7TDMI.PIN_DB8 = 47  -- Data Bus Bit 8
ARM7TDMI.PIN_DB9 = 48  -- Data Bus Bit 9
ARM7TDMI.PIN_DB10 = 49  -- Data Bus Bit 10
ARM7TDMI.PIN_DB11 = 50  -- Data Bus Bit 11
ARM7TDMI.PIN_DB12 = 51  -- Data Bus Bit 12
ARM7TDMI.PIN_DB13 = 52  -- Data Bus Bit 13
ARM7TDMI.PIN_DB14 = 53  -- Data Bus Bit 14
ARM7TDMI.PIN_DB15 = 54  -- Data Bus Bit 15
ARM7TDMI.PIN_DB16 = 55  -- Data Bus Bit 16
ARM7TDMI.PIN_DB17 = 56  -- Data Bus Bit 17
ARM7TDMI.PIN_DB18 = 57  -- Data Bus Bit 18
ARM7TDMI.PIN_DB19 = 58  -- Data Bus Bit 19
ARM7TDMI.PIN_DB20 = 59  -- Data Bus Bit 20
ARM7TDMI.PIN_DB21 = 60  -- Data Bus Bit 21
ARM7TDMI.PIN_DB22 = 61  -- Data Bus Bit 22
ARM7TDMI.PIN_DB23 = 62  -- Data Bus Bit 23
ARM7TDMI.PIN_DB24 = 63  -- Data Bus Bit 24
ARM7TDMI.PIN_DB25 = 64  -- Data Bus Bit 25
ARM7TDMI.PIN_DB26 = 65  -- Data Bus Bit 26
ARM7TDMI.PIN_DB27 = 66  -- Data Bus Bit 27
ARM7TDMI.PIN_DB28 = 67  -- Data Bus Bit 28
ARM7TDMI.PIN_DB29 = 68  -- Data Bus Bit 29
ARM7TDMI.PIN_DB30 = 69  -- Data Bus Bit 30
ARM7TDMI.PIN_DB31 = 70  -- Data Bus Bit 31
ARM7TDMI.PIN_NCS0 = 71  -- Chip Select 0 (ROM)
ARM7TDMI.PIN_NCS1 = 72  -- Chip Select 1 (RAM)
ARM7TDMI.PIN_NWR = 73  -- Write Enable (active low)
ARM7TDMI.PIN_NRD = 74  -- Read Enable (active low)
ARM7TDMI.PIN_ADV = 75  -- Address Valid (for external DMA)
ARM7TDMI.PIN_BE0 = 76  -- Byte Enable 0
ARM7TDMI.PIN_BE1 = 77  -- Byte Enable 1
ARM7TDMI.PIN_BREQ = 78  -- Bus Request (from external master)
ARM7TDMI.PIN_BACK = 79  -- Bus Acknowledge
ARM7TDMI.PIN_EKO = 80  -- Serial Data Out (Link Cable)
ARM7TDMI.PIN_EKI = 81  -- Serial Data In (Link Cable)
ARM7TDMI.PIN_SOUND = 82  -- Stereo Audio Output (L+R)

-- 设备类
function ARM7TDMI.new(memory_base)
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
            description = "General Purpose Register 0",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x04,
            size = 4,
            access = "rw",
            description = "General Purpose Register 1",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x08,
            size = 4,
            access = "rw",
            description = "General Purpose Register 2",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x0C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 3",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x10,
            size = 4,
            access = "rw",
            description = "General Purpose Register 4",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x14,
            size = 4,
            access = "rw",
            description = "General Purpose Register 5",
            value = 0
        }
        self.registers["R6"] = {
            address = 0x18,
            size = 4,
            access = "rw",
            description = "General Purpose Register 6",
            value = 0
        }
        self.registers["R7"] = {
            address = 0x1C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 7",
            value = 0
        }
        self.registers["R8"] = {
            address = 0x20,
            size = 4,
            access = "rw",
            description = "General Purpose Register 8",
            value = 0
        }
        self.registers["R9"] = {
            address = 0x24,
            size = 4,
            access = "rw",
            description = "General Purpose Register 9 / SB",
            value = 0
        }
        self.registers["R10"] = {
            address = 0x28,
            size = 4,
            access = "rw",
            description = "General Purpose Register 10 / SL",
            value = 0
        }
        self.registers["R11"] = {
            address = 0x2C,
            size = 4,
            access = "rw",
            description = "Frame Pointer / FP",
            value = 0
        }
        self.registers["R12"] = {
            address = 0x30,
            size = 4,
            access = "rw",
            description = "Intra-Procedure-call Scratch Register / IP",
            value = 0
        }
        self.registers["R13"] = {
            address = 0x34,
            size = 4,
            access = "rw",
            description = "Stack Pointer / SP",
            value = 0
        }
        self.registers["R14"] = {
            address = 0x38,
            size = 4,
            access = "rw",
            description = "Link Register / LR",
            value = 0
        }
        self.registers["R15"] = {
            address = 0x3C,
            size = 4,
            access = "rw",
            description = "Program Counter / PC",
            value = 0
        }
        self.registers["CPSR"] = {
            address = 0x40,
            size = 4,
            access = "rw",
            description = "Current Program Status Register",
            value = 0
        }
        self.registers["SPSR_SVC"] = {
            address = 0x44,
            size = 4,
            access = "rw",
            description = "Saved PSR (Supervisor Mode)",
            value = 0
        }
        self.registers["SPSR_ABT"] = {
            address = 0x48,
            size = 4,
            access = "rw",
            description = "Saved PSR (Abort Mode)",
            value = 0
        }
        self.registers["SPSR_IRQ"] = {
            address = 0x4C,
            size = 4,
            access = "rw",
            description = "Saved PSR (IRQ Mode)",
            value = 0
        }
        self.registers["SPSR_FIQ"] = {
            address = 0x50,
            size = 4,
            access = "rw",
            description = "Saved PSR (FIQ Mode)",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["LCD"] = {
            base = 0x04000000,
            type = "video",
            description = "LCD Controller",
            registers = {}
        }
        
        local p = self.peripherals["LCD"]
        p.registers["DISPCNT"] = {
            address = 0x04000000,
            size = 2,
            value = 0
        }
        p.registers["GREEN_SWAP"] = {
            address = 0x04000002,
            size = 1,
            value = 0
        }
        p.registers["DISPSTAT"] = {
            address = 0x04000004,
            size = 2,
            value = 0
        }
        p.registers["VCOUNT"] = {
            address = 0x04000006,
            size = 2,
            value = 0
        }
        p.registers["BG0CNT"] = {
            address = 0x04000008,
            size = 2,
            value = 0
        }
        p.registers["BG1CNT"] = {
            address = 0x0400000A,
            size = 2,
            value = 0
        }
        p.registers["BG2CNT"] = {
            address = 0x0400000C,
            size = 2,
            value = 0
        }
        p.registers["BG3CNT"] = {
            address = 0x0400000E,
            size = 2,
            value = 0
        }
        p.registers["BG0HOFS"] = {
            address = 0x04000010,
            size = 2,
            value = 0
        }
        p.registers["BG0VOFS"] = {
            address = 0x04000012,
            size = 2,
            value = 0
        }
        p.registers["BG1HOFS"] = {
            address = 0x04000014,
            size = 2,
            value = 0
        }
        p.registers["BG1VOFS"] = {
            address = 0x04000016,
            size = 2,
            value = 0
        }
        p.registers["BG2HOFS"] = {
            address = 0x04000018,
            size = 2,
            value = 0
        }
        p.registers["BG2VOFS"] = {
            address = 0x0400001A,
            size = 2,
            value = 0
        }
        p.registers["BG3HOFS"] = {
            address = 0x0400001C,
            size = 2,
            value = 0
        }
        p.registers["BG3VOFS"] = {
            address = 0x0400001E,
            size = 2,
            value = 0
        }
        p.registers["BG2PA"] = {
            address = 0x04000020,
            size = 2,
            value = 0
        }
        p.registers["BG2PB"] = {
            address = 0x04000022,
            size = 2,
            value = 0
        }
        p.registers["BG2PC"] = {
            address = 0x04000024,
            size = 2,
            value = 0
        }
        p.registers["BG2PD"] = {
            address = 0x04000026,
            size = 2,
            value = 0
        }
        p.registers["BG2X"] = {
            address = 0x04000028,
            size = 4,
            value = 0
        }
        p.registers["BG2Y"] = {
            address = 0x0400002C,
            size = 4,
            value = 0
        }
        p.registers["BG3PA"] = {
            address = 0x04000030,
            size = 2,
            value = 0
        }
        p.registers["BG3PB"] = {
            address = 0x04000032,
            size = 2,
            value = 0
        }
        p.registers["BG3PC"] = {
            address = 0x04000034,
            size = 2,
            value = 0
        }
        p.registers["BG3PD"] = {
            address = 0x04000036,
            size = 2,
            value = 0
        }
        p.registers["BG3X"] = {
            address = 0x04000038,
            size = 4,
            value = 0
        }
        p.registers["BG3Y"] = {
            address = 0x0400003C,
            size = 4,
            value = 0
        }
        p.registers["WIN0H"] = {
            address = 0x04000040,
            size = 2,
            value = 0
        }
        p.registers["WIN1H"] = {
            address = 0x04000042,
            size = 2,
            value = 0
        }
        p.registers["WIN0V"] = {
            address = 0x04000044,
            size = 2,
            value = 0
        }
        p.registers["WIN1V"] = {
            address = 0x04000046,
            size = 2,
            value = 0
        }
        p.registers["WININ"] = {
            address = 0x04000048,
            size = 1,
            value = 0
        }
        p.registers["WINOUT"] = {
            address = 0x04000049,
            size = 1,
            value = 0
        }
        p.registers["MOSAIC"] = {
            address = 0x0400004C,
            size = 2,
            value = 0
        }
        p.registers["BLDCNT"] = {
            address = 0x04000050,
            size = 2,
            value = 0
        }
        p.registers["BLDALPHA"] = {
            address = 0x04000052,
            size = 2,
            value = 0
        }
        p.registers["BLDY"] = {
            address = 0x04000054,
            size = 1,
            value = 0
        }
        self.peripherals["DMA"] = {
            base = 0x040000B0,
            type = "dma",
            description = "Direct Memory Access Controller",
            registers = {}
        }
        
        local p = self.peripherals["DMA"]
        p.registers["DMA0SAD"] = {
            address = 0x040000B0,
            size = 4,
            value = 0
        }
        p.registers["DMA0DAD"] = {
            address = 0x040000B4,
            size = 4,
            value = 0
        }
        p.registers["DMA0CNT_L"] = {
            address = 0x040000B8,
            size = 2,
            value = 0
        }
        p.registers["DMA0CNT_H"] = {
            address = 0x040000BA,
            size = 2,
            value = 0
        }
        p.registers["DMA1SAD"] = {
            address = 0x040000BC,
            size = 4,
            value = 0
        }
        p.registers["DMA1DAD"] = {
            address = 0x040000C0,
            size = 4,
            value = 0
        }
        p.registers["DMA1CNT_L"] = {
            address = 0x040000C4,
            size = 2,
            value = 0
        }
        p.registers["DMA1CNT_H"] = {
            address = 0x040000C6,
            size = 2,
            value = 0
        }
        p.registers["DMA2SAD"] = {
            address = 0x040000C8,
            size = 4,
            value = 0
        }
        p.registers["DMA2DAD"] = {
            address = 0x040000CC,
            size = 4,
            value = 0
        }
        p.registers["DMA2CNT_L"] = {
            address = 0x040000D0,
            size = 2,
            value = 0
        }
        p.registers["DMA2CNT_H"] = {
            address = 0x040000D2,
            size = 2,
            value = 0
        }
        p.registers["DMA3SAD"] = {
            address = 0x040000D4,
            size = 4,
            value = 0
        }
        p.registers["DMA3DAD"] = {
            address = 0x040000D8,
            size = 4,
            value = 0
        }
        p.registers["DMA3CNT_L"] = {
            address = 0x040000DC,
            size = 2,
            value = 0
        }
        p.registers["DMA3CNT_H"] = {
            address = 0x040000DE,
            size = 2,
            value = 0
        }
        self.peripherals["TIMER"] = {
            base = 0x04000100,
            type = "timer",
            description = "Timer Units (4 timers)",
            registers = {}
        }
        
        local p = self.peripherals["TIMER"]
        p.registers["TM0CNT_L"] = {
            address = 0x04000100,
            size = 2,
            value = 0
        }
        p.registers["TM0CNT_H"] = {
            address = 0x04000102,
            size = 2,
            value = 0
        }
        p.registers["TM1CNT_L"] = {
            address = 0x04000104,
            size = 2,
            value = 0
        }
        p.registers["TM1CNT_H"] = {
            address = 0x04000106,
            size = 2,
            value = 0
        }
        p.registers["TM2CNT_L"] = {
            address = 0x04000108,
            size = 2,
            value = 0
        }
        p.registers["TM2CNT_H"] = {
            address = 0x0400010A,
            size = 2,
            value = 0
        }
        p.registers["TM3CNT_L"] = {
            address = 0x0400010C,
            size = 2,
            value = 0
        }
        p.registers["TM3CNT_H"] = {
            address = 0x0400010E,
            size = 2,
            value = 0
        }
        self.peripherals["SIO"] = {
            base = 0x04000120,
            type = "uart",
            description = "Serial I/O (JOY BUS / Link Cable)",
            registers = {}
        }
        
        local p = self.peripherals["SIO"]
        p.registers["SIOCNT"] = {
            address = 0x04000120,
            size = 2,
            value = 0
        }
        p.registers["SIODATA8"] = {
            address = 0x0400012A,
            size = 1,
            value = 0
        }
        p.registers["JOYCNT"] = {
            address = 0x04000130,
            size = 2,
            value = 0
        }
        p.registers["JOYSTAT"] = {
            address = 0x04000134,
            size = 2,
            value = 0
        }
        p.registers["JOY_RECV"] = {
            address = 0x04000150,
            size = 4,
            value = 0
        }
        p.registers["JOY_TRANS"] = {
            address = 0x04000154,
            size = 4,
            value = 0
        }
        self.peripherals["KEYINPUT"] = {
            base = 0x04000130,
            type = "input",
            description = "Key Input",
            registers = {}
        }
        
        local p = self.peripherals["KEYINPUT"]
        p.registers["KEYINPUT"] = {
            address = 0x04000130,
            size = 2,
            value = 0
        }
        p.registers["KEYCNT"] = {
            address = 0x04000132,
            size = 2,
            value = 0
        }
        self.peripherals["INTERRUPT"] = {
            base = 0x04000200,
            type = "system",
            description = "Interrupt Control",
            registers = {}
        }
        
        local p = self.peripherals["INTERRUPT"]
        p.registers["IME"] = {
            address = 0x04000208,
            size = 4,
            value = 0
        }
        p.registers["IE"] = {
            address = 0x04000210,
            size = 4,
            value = 0
        }
        p.registers["IF"] = {
            address = 0x04000214,
            size = 4,
            value = 0
        }
        self.peripherals["WAITCNT"] = {
            base = 0x04000204,
            type = "memory",
            description = "Waitstate Control",
            registers = {}
        }
        
        local p = self.peripherals["WAITCNT"]
        p.registers["WAITCNT"] = {
            address = 0x04000204,
            size = 2,
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
            name = ARM7TDMI.DEVICE_NAME,
            manufacturer = ARM7TDMI.MANUFACTURER,
            family = ARM7TDMI.FAMILY,
            version = ARM7TDMI.VERSION,
            architecture = ARM7TDMI.ARCHITECTURE,
            bits = ARM7TDMI.BITS,
            clock_frequency = ARM7TDMI.CLOCK_FREQUENCY
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
        return string.format("ARM7TDMI(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ARM7TDMI.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ARM7TDMI.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ARM7TDMI.print_device_info(device)
    device = device or ARM7TDMI.new()
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

function ARM7TDMI.print_registers(device)
    device = device or ARM7TDMI.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ARM7TDMI.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ARM7TDMI.example()
    print("=== ARM7TDMI设备示例 ===")
    
    -- 创建设备实例
    local device = ARM7TDMI.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ARM7TDMI.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. ARM7TDMI.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. ARM7TDMI.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ARM7TDMI.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ARM7TDMI.lua$") then
    ARM7TDMI.example()
end

return ARM7TDMI

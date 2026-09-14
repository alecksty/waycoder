# ARM7TDMI 设备定义 - Ruby 模块
# 生成自: ARM/ARM7/ARM7TDMI
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: Game Boy Advance main processor - ARM7TDMI @ 16.78MHz with 32-bit ARM + 16-bit Thumb instruction sets
# CPU架构: ARM7TDMI
# 位宽: 32位
# 时钟频率: 16780000 Hz

module ARM7TDMI

  # 寄存器地址定义
  R0_ADDR = 0x00  # General Purpose Register 0
  R1_ADDR = 0x04  # General Purpose Register 1
  R2_ADDR = 0x08  # General Purpose Register 2
  R3_ADDR = 0x0C  # General Purpose Register 3
  R4_ADDR = 0x10  # General Purpose Register 4
  R5_ADDR = 0x14  # General Purpose Register 5
  R6_ADDR = 0x18  # General Purpose Register 6
  R7_ADDR = 0x1C  # General Purpose Register 7
  R8_ADDR = 0x20  # General Purpose Register 8
  R9_ADDR = 0x24  # General Purpose Register 9 / SB
  R10_ADDR = 0x28  # General Purpose Register 10 / SL
  R11_ADDR = 0x2C  # Frame Pointer / FP
  R12_ADDR = 0x30  # Intra-Procedure-call Scratch Register / IP
  R13_ADDR = 0x34  # Stack Pointer / SP
  R14_ADDR = 0x38  # Link Register / LR
  R15_ADDR = 0x3C  # Program Counter / PC
  CPSR_ADDR = 0x40  # Current Program Status Register
  CPSR_MODE_BIT = 0  # Processor Mode (10000=User, 10001=FIQ, 10010=IRQ, 10011=SVC, 10111=ABT, 11011=UND, 11111=SYS)
  CPSR_T_BIT = 5  # Thumb State Bit (1=Thumb mode)
  CPSR_F_BIT = 6  # FIQ Disable
  CPSR_I_BIT = 7  # IRQ Disable
  CPSR_A_BIT = 8  # Imprecise Data Abort Disable
  CPSR_E_BIT = 9  # Endianness (0=Little)
  CPSR_GE_BIT = 0  # Greater-than-or-Equal flags
  CPSR_N_BIT = 31  # Negative
  CPSR_Z_BIT = 30  # Zero
  CPSR_C_BIT = 29  # Carry
  CPSR_V_BIT = 28  # Overflow
  SPSR_SVC_ADDR = 0x44  # Saved PSR (Supervisor Mode)
  SPSR_ABT_ADDR = 0x48  # Saved PSR (Abort Mode)
  SPSR_IRQ_ADDR = 0x4C  # Saved PSR (IRQ Mode)
  SPSR_FIQ_ADDR = 0x50  # Saved PSR (FIQ Mode)

  # 内存段定义
  IWRAM_START = 0x03000000
  IWRAM_END = 0x03007FFF
  IWRAM_SIZE = 32768  # Internal Work RAM (32KB, 2-cycle access)
  IWRAM_FAST_START = 0x03008000
  IWRAM_FAST_END = 0x03FFFFFF
  IWRAM_FAST_SIZE = 32752  # Internal Work RAM Fast (high-speed region)
  VRAM_START = 0x06000000
  VRAM_END = 0x06017FFF
  VRAM_SIZE = 98304  # Video RAM (96KB + 64KB OBJ VRAM)
  PALETTE_START = 0x05000200
  PALETTE_END = 0x050003FF
  PALETTE_SIZE = 512  # BG Palette RAM (256 colors x 2 bytes)
  OBJ_PALETTE_START = 0x05000400
  OBJ_PALETTE_END = 0x050005FF
  OBJ_PALETTE_SIZE = 512  # Object Palette RAM
  OAM_START = 0x07000000
  OAM_END = 0x070003FF
  OAM_SIZE = 1024  # Object Attribute Memory (OAM, 128 sprites)
  ROM_START = 0x08000000
  ROM_END = 0x09FFFFFF
  ROM_SIZE = 33554432  # Cartridge ROM (max 32MB)
  CART_RAM_START = 0x0E000000
  CART_RAM_END = 0x0E00FFFF
  CART_RAM_SIZE = 65536  # Cartridge SRAM / Flash
  BIOS_START = 0x00000000
  BIOS_END = 0x00003FFF
  BIOS_SIZE = 16384  # GBA BIOS (16KB)
  IO_REGS_START = 0x04000000
  IO_REGS_END = 0x04FFFFFF
  IO_REGS_SIZE = 16777216  # I/O Registers (MMIO)

  # 外设定义
  # LCD Controller
  LCD_BASE = 0x04000000
  LCD_DISPCNT_ADDR = 0x04000000
  LCD_DISPCNT_BG_MODE_BIT = 0  # BG Mode (0-6)
  LCD_DISPCNT_GB_WINDOW_BIT = 5  # Game Boy Window Enable
  LCD_DISPCNT_WIN0_ENABLE_BIT = 13  # Window 0 Enable
  LCD_DISPCNT_WIN1_ENABLE_BIT = 14  # Window 1 Enable
  LCD_DISPCNT_OBJ_WIN_BIT = 15  # Object Window Enable
  LCD_DISPCNT_BG0_ENABLE_BIT = 8  # BG0 Enable
  LCD_DISPCNT_BG1_ENABLE_BIT = 9  # BG1 Enable
  LCD_DISPCNT_BG2_ENABLE_BIT = 10  # BG2 Enable
  LCD_DISPCNT_BG3_ENABLE_BIT = 11  # BG3 Enable
  LCD_DISPCNT_OBJ_ENABLE_BIT = 12  # Object/Sprite Enable
  LCD_GREEN_SWAP_ADDR = 0x04000002
  LCD_DISPSTAT_ADDR = 0x04000004
  LCD_DISPSTAT_V_COUNT_BIT = 0  # Vertical Line Counter
  LCD_DISPSTAT_VBLANK_FLAG_BIT = 0  # V-Blank Flag (read-only)
  LCD_DISPSTAT_HBLANK_FLAG_BIT = 1  # H-Blank Flag (read-only)
  LCD_DISPSTAT_V_COUNT_FLAG_BIT = 2  # V-Count Flag (LY==LYC)
  LCD_DISPSTAT_VBLANK_IRQ_BIT = 3  # V-Blank IRQ Enable
  LCD_DISPSTAT_HBLANK_IRQ_BIT = 4  # H-Blank IRQ Enable
  LCD_DISPSTAT_VCOUNT_IRQ_BIT = 5  # V-Count IRQ Enable
  LCD_VCOUNT_ADDR = 0x04000006
  LCD_BG0CNT_ADDR = 0x04000008
  LCD_BG1CNT_ADDR = 0x0400000A
  LCD_BG2CNT_ADDR = 0x0400000C
  LCD_BG3CNT_ADDR = 0x0400000E
  LCD_BG0HOFS_ADDR = 0x04000010
  LCD_BG0VOFS_ADDR = 0x04000012
  LCD_BG1HOFS_ADDR = 0x04000014
  LCD_BG1VOFS_ADDR = 0x04000016
  LCD_BG2HOFS_ADDR = 0x04000018
  LCD_BG2VOFS_ADDR = 0x0400001A
  LCD_BG3HOFS_ADDR = 0x0400001C
  LCD_BG3VOFS_ADDR = 0x0400001E
  LCD_BG2PA_ADDR = 0x04000020
  LCD_BG2PB_ADDR = 0x04000022
  LCD_BG2PC_ADDR = 0x04000024
  LCD_BG2PD_ADDR = 0x04000026
  LCD_BG2X_ADDR = 0x04000028
  LCD_BG2Y_ADDR = 0x0400002C
  LCD_BG3PA_ADDR = 0x04000030
  LCD_BG3PB_ADDR = 0x04000032
  LCD_BG3PC_ADDR = 0x04000034
  LCD_BG3PD_ADDR = 0x04000036
  LCD_BG3X_ADDR = 0x04000038
  LCD_BG3Y_ADDR = 0x0400003C
  LCD_WIN0H_ADDR = 0x04000040
  LCD_WIN1H_ADDR = 0x04000042
  LCD_WIN0V_ADDR = 0x04000044
  LCD_WIN1V_ADDR = 0x04000046
  LCD_WININ_ADDR = 0x04000048
  LCD_WINOUT_ADDR = 0x04000049
  LCD_MOSAIC_ADDR = 0x0400004C
  LCD_BLDCNT_ADDR = 0x04000050
  LCD_BLDCNT_BG1ST_BIT = 0  # BG1 1st Target
  LCD_BLDCNT_BG2ST_BIT = 1  # BG2 1st Target
  LCD_BLDCNT_BG3ST_BIT = 2  # BG3 1st Target
  LCD_BLDCNT_OBJST_BIT = 3  # Object 1st Target
  LCD_BLDCNT_BDST_BIT = 4  # Backdrop 1st Target
  LCD_BLDCNT_BLEND_MODE_BIT = 0  # Blend Mode (0=None, 1=Alpha, 2=Increase, 3=Decrease)
  LCD_BLDCNT_BG1ST2_BIT = 8  # BG1 2nd Target
  LCD_BLDCNT_BG2ST2_BIT = 9  # BG2 2nd Target
  LCD_BLDCNT_BG3ST2_BIT = 10  # BG3 2nd Target
  LCD_BLDCNT_OBJST2_BIT = 11  # Object 2nd Target
  LCD_BLDCNT_BDST2_BIT = 12  # Backdrop 2nd Target
  LCD_BLDALPHA_ADDR = 0x04000052
  LCD_BLDY_ADDR = 0x04000054
  # Direct Memory Access Controller
  DMA_BASE = 0x040000B0
  DMA_DMA0SAD_ADDR = 0x040000B0
  DMA_DMA0DAD_ADDR = 0x040000B4
  DMA_DMA0CNT_L_ADDR = 0x040000B8
  DMA_DMA0CNT_H_ADDR = 0x040000BA
  DMA_DMA0CNT_H_TRANSFER_COUNT_BIT = 0  # Number of Transfers
  DMA_DMA0CNT_H_DEST_ADD_MODE_BIT = 0  # Dest Address Control (0=fix, 1=inc, 2=dec, 3=inc+reload)
  DMA_DMA0CNT_H_SRC_ADD_MODE_BIT = 0  # Source Address Control (0=fix, 1=inc, 2=dec)
  DMA_DMA0CNT_H_REPEAT_BIT = 18  # Repeat (for 16-bit repeat mode)
  DMA_DMA0CNT_H_WORD_SIZE_BIT = 20  # Word Size (0=16-bit, 1=32-bit)
  DMA_DMA0CNT_H_DRQ_BIT = 27  # DRQ Trigger (DMA from external source)
  DMA_DMA0CNT_H_TIMING_BIT = 0  # Start Timing (0=Now, 1=V-Blank, 2=H-Blank, 3=Special)
  DMA_DMA0CNT_H_ENABLE_BIT = 31  # DMA Enable
  DMA_DMA1SAD_ADDR = 0x040000BC
  DMA_DMA1DAD_ADDR = 0x040000C0
  DMA_DMA1CNT_L_ADDR = 0x040000C4
  DMA_DMA1CNT_H_ADDR = 0x040000C6
  DMA_DMA2SAD_ADDR = 0x040000C8
  DMA_DMA2DAD_ADDR = 0x040000CC
  DMA_DMA2CNT_L_ADDR = 0x040000D0
  DMA_DMA2CNT_H_ADDR = 0x040000D2
  DMA_DMA3SAD_ADDR = 0x040000D4
  DMA_DMA3DAD_ADDR = 0x040000D8
  DMA_DMA3CNT_L_ADDR = 0x040000DC
  DMA_DMA3CNT_H_ADDR = 0x040000DE
  # Timer Units (4 timers)
  TIMER_BASE = 0x04000100
  TIMER_TM0CNT_L_ADDR = 0x04000100
  TIMER_TM0CNT_H_ADDR = 0x04000102
  TIMER_TM0CNT_H_PRESCALER_BIT = 0  # Prescaler (0=1, 1=64, 2=256, 3=1024)
  TIMER_TM0CNT_H_COUNT_UP_BIT = 2  # Count Up (cascade mode)
  TIMER_TM0CNT_H_IRQ_ENABLE_BIT = 6  # Timer IRQ Enable
  TIMER_TM0CNT_H_ENABLE_BIT = 7  # Timer Enable
  TIMER_TM1CNT_L_ADDR = 0x04000104
  TIMER_TM1CNT_H_ADDR = 0x04000106
  TIMER_TM2CNT_L_ADDR = 0x04000108
  TIMER_TM2CNT_H_ADDR = 0x0400010A
  TIMER_TM3CNT_L_ADDR = 0x0400010C
  TIMER_TM3CNT_H_ADDR = 0x0400010E
  # Serial I/O (JOY BUS / Link Cable)
  SIO_BASE = 0x04000120
  SIO_SIOCNT_ADDR = 0x04000120
  SIO_SIOCNT_CLOCK_SEL_BIT = 0  # Baud Rate Clock (0=9600, 1=57600, 2=115200, 3=768000)
  SIO_SIOCNT_SO_ENABLE_BIT = 3  # SO Output Enable
  SIO_SIOCNT_RECV_ENABLE_BIT = 5  # Receive Enable
  SIO_SIOCNT_SEND_ENABLE_BIT = 6  # Send Enable
  SIO_SIOCNT_START_BIT_BIT = 7  # Start Transfer
  SIO_SIODATA8_ADDR = 0x0400012A
  SIO_JOYCNT_ADDR = 0x04000130
  SIO_JOYSTAT_ADDR = 0x04000134
  SIO_JOY_RECV_ADDR = 0x04000150
  SIO_JOY_TRANS_ADDR = 0x04000154
  # Key Input
  KEYINPUT_BASE = 0x04000130
  KEYINPUT_KEYINPUT_ADDR = 0x04000130
  KEYINPUT_KEYINPUT_A_BIT = 0  # A Button (0=Pressed)
  KEYINPUT_KEYINPUT_B_BIT = 1  # B Button (0=Pressed)
  KEYINPUT_KEYINPUT_SELECT_BIT = 2  # Select Button (0=Pressed)
  KEYINPUT_KEYINPUT_START_BIT = 3  # Start Button (0=Pressed)
  KEYINPUT_KEYINPUT_RIGHT_BIT = 4  # D-Pad Right (0=Pressed)
  KEYINPUT_KEYINPUT_LEFT_BIT = 5  # D-Pad Left (0=Pressed)
  KEYINPUT_KEYINPUT_UP_BIT = 6  # D-Pad Up (0=Pressed)
  KEYINPUT_KEYINPUT_DOWN_BIT = 7  # D-Pad Down (0=Pressed)
  KEYINPUT_KEYINPUT_R_BIT = 8  # R Shoulder Button (0=Pressed)
  KEYINPUT_KEYINPUT_L_BIT = 9  # L Shoulder Button (0=Pressed)
  KEYINPUT_KEYCNT_ADDR = 0x04000132
  KEYINPUT_KEYCNT_KEY_MASK_BIT = 0  # Key Interrupt Enable Mask
  KEYINPUT_KEYCNT_IRQ_ENABLE_BIT = 14  # Key Interrupt Enable
  # Interrupt Control
  INTERRUPT_BASE = 0x04000200
  INTERRUPT_IME_ADDR = 0x04000208
  INTERRUPT_IE_ADDR = 0x04000210
  INTERRUPT_IE_VBLANK_BIT = 0  # V-Blank Interrupt Enable
  INTERRUPT_IE_HBLANK_BIT = 1  # H-Blank Interrupt Enable
  INTERRUPT_IE_VCOUNT_BIT = 2  # V-Count Match Interrupt Enable
  INTERRUPT_IE_TIMER0_BIT = 3  # Timer 0 Interrupt Enable
  INTERRUPT_IE_TIMER1_BIT = 4  # Timer 1 Interrupt Enable
  INTERRUPT_IE_TIMER2_BIT = 5  # Timer 2 Interrupt Enable
  INTERRUPT_IE_TIMER3_BIT = 6  # Timer 3 Interrupt Enable
  INTERRUPT_IE_SIO_BIT = 7  # Serial I/O Interrupt Enable
  INTERRUPT_IE_DMA0_BIT = 8  # DMA 0 Interrupt Enable
  INTERRUPT_IE_DMA1_BIT = 9  # DMA 1 Interrupt Enable
  INTERRUPT_IE_DMA2_BIT = 10  # DMA 2 Interrupt Enable
  INTERRUPT_IE_DMA3_BIT = 11  # DMA 3 Interrupt Enable
  INTERRUPT_IE_KEYPAD_BIT = 12  # Keypad Interrupt Enable
  INTERRUPT_IE_CART_BIT = 13  # Game Pak Interrupt Enable
  INTERRUPT_IF_ADDR = 0x04000214
  # Waitstate Control
  WAITCNT_BASE = 0x04000204
  WAITCNT_WAITCNT_ADDR = 0x04000204
  WAITCNT_WAITCNT_PHI_OD_BIT = 0  # PHI Terminal Output (0=Disable)
  WAITCNT_WAITCNT_SRAM_WS_BIT = 0  # SRAM Wait State (0=4, 1=3, 2=2, 3=8 cycles)
  WAITCNT_WAITCNT_WS0_N_BIT = 0  # Wait State 0 (ROM/SRAM 1st access)
  WAITCNT_WAITCNT_WS0_S_BIT = 5  # Wait State 0 (ROM/SRAM 2nd access)
  WAITCNT_WAITCNT_WS1_N_BIT = 0  # Wait State 1 (ROM 2nd access)
  WAITCNT_WAITCNT_WS1_S_BIT = 8  # Wait State 1 (ROM 2nd access short)
  WAITCNT_WAITCNT_WS2_N_BIT = 0  # Wait State 2 (ROM 3rd access)
  WAITCNT_WAITCNT_WS2_S_BIT = 11  # Wait State 2 (ROM 3rd access short)
  WAITCNT_WAITCNT_PREFE_BIT = 12  # Prefetch Enable (GBA SP only)

  # 中断向量定义
  INT_VBLANK = 0  # V-Blank Interrupt
  INT_HBLANK = 1  # H-Blank Interrupt
  INT_VCOUNT = 2  # V-Count Match Interrupt
  INT_TIMER0 = 3  # Timer 0 Overflow Interrupt
  INT_TIMER1 = 4  # Timer 1 Overflow Interrupt
  INT_TIMER2 = 5  # Timer 2 Overflow Interrupt
  INT_TIMER3 = 6  # Timer 3 Overflow Interrupt
  INT_SIO = 7  # Serial I/O Interrupt
  INT_DMA0 = 8  # DMA 0 Complete Interrupt
  INT_DMA1 = 9  # DMA 1 Complete Interrupt
  INT_DMA2 = 10  # DMA 2 Complete Interrupt
  INT_DMA3 = 11  # DMA 3 Complete Interrupt
  INT_KEYPAD = 12  # Keypad Interrupt
  INT_CART = 13  # Game Pak Interrupt

  # 引脚定义
  PIN_VSS = 1  # Ground
  PIN_VDD = 2  # Power Supply
  PIN_CLK = 3  # System Clock Input (16.78MHz)
  PIN_RESET = 4  # Reset Signal
  PIN_NMI = 5  # Non-Maskable Interrupt
  PIN_IRQ = 6  # Interrupt Request
  PIN_AB0 = 7  # Address Bus Bit 0
  PIN_AB1 = 8  # Address Bus Bit 1
  PIN_AB2 = 9  # Address Bus Bit 2
  PIN_AB3 = 10  # Address Bus Bit 3
  PIN_AB4 = 11  # Address Bus Bit 4
  PIN_AB5 = 12  # Address Bus Bit 5
  PIN_AB6 = 13  # Address Bus Bit 6
  PIN_AB7 = 14  # Address Bus Bit 7
  PIN_AB8 = 15  # Address Bus Bit 8
  PIN_AB9 = 16  # Address Bus Bit 9
  PIN_AB10 = 17  # Address Bus Bit 10
  PIN_AB11 = 18  # Address Bus Bit 11
  PIN_AB12 = 19  # Address Bus Bit 12
  PIN_AB13 = 20  # Address Bus Bit 13
  PIN_AB14 = 21  # Address Bus Bit 14
  PIN_AB15 = 22  # Address Bus Bit 15
  PIN_AB16 = 23  # Address Bus Bit 16
  PIN_AB17 = 24  # Address Bus Bit 17
  PIN_AB18 = 25  # Address Bus Bit 18
  PIN_AB19 = 26  # Address Bus Bit 19
  PIN_AB20 = 27  # Address Bus Bit 20
  PIN_AB21 = 28  # Address Bus Bit 21
  PIN_AB22 = 29  # Address Bus Bit 22
  PIN_AB23 = 30  # Address Bus Bit 23
  PIN_AB24 = 31  # Address Bus Bit 24
  PIN_AB25 = 32  # Address Bus Bit 25
  PIN_AB26 = 33  # Address Bus Bit 26
  PIN_AB27 = 34  # Address Bus Bit 27
  PIN_AB28 = 35  # Address Bus Bit 28
  PIN_AB29 = 36  # Address Bus Bit 29
  PIN_AB30 = 37  # Address Bus Bit 30
  PIN_AB31 = 38  # Address Bus Bit 31
  PIN_DB0 = 39  # Data Bus Bit 0
  PIN_DB1 = 40  # Data Bus Bit 1
  PIN_DB2 = 41  # Data Bus Bit 2
  PIN_DB3 = 42  # Data Bus Bit 3
  PIN_DB4 = 43  # Data Bus Bit 4
  PIN_DB5 = 44  # Data Bus Bit 5
  PIN_DB6 = 45  # Data Bus Bit 6
  PIN_DB7 = 46  # Data Bus Bit 7
  PIN_DB8 = 47  # Data Bus Bit 8
  PIN_DB9 = 48  # Data Bus Bit 9
  PIN_DB10 = 49  # Data Bus Bit 10
  PIN_DB11 = 50  # Data Bus Bit 11
  PIN_DB12 = 51  # Data Bus Bit 12
  PIN_DB13 = 52  # Data Bus Bit 13
  PIN_DB14 = 53  # Data Bus Bit 14
  PIN_DB15 = 54  # Data Bus Bit 15
  PIN_DB16 = 55  # Data Bus Bit 16
  PIN_DB17 = 56  # Data Bus Bit 17
  PIN_DB18 = 57  # Data Bus Bit 18
  PIN_DB19 = 58  # Data Bus Bit 19
  PIN_DB20 = 59  # Data Bus Bit 20
  PIN_DB21 = 60  # Data Bus Bit 21
  PIN_DB22 = 61  # Data Bus Bit 22
  PIN_DB23 = 62  # Data Bus Bit 23
  PIN_DB24 = 63  # Data Bus Bit 24
  PIN_DB25 = 64  # Data Bus Bit 25
  PIN_DB26 = 65  # Data Bus Bit 26
  PIN_DB27 = 66  # Data Bus Bit 27
  PIN_DB28 = 67  # Data Bus Bit 28
  PIN_DB29 = 68  # Data Bus Bit 29
  PIN_DB30 = 69  # Data Bus Bit 30
  PIN_DB31 = 70  # Data Bus Bit 31
  PIN_NCS0 = 71  # Chip Select 0 (ROM)
  PIN_NCS1 = 72  # Chip Select 1 (RAM)
  PIN_NWR = 73  # Write Enable (active low)
  PIN_NRD = 74  # Read Enable (active low)
  PIN_ADV = 75  # Address Valid (for external DMA)
  PIN_BE0 = 76  # Byte Enable 0
  PIN_BE1 = 77  # Byte Enable 1
  PIN_BREQ = 78  # Bus Request (from external master)
  PIN_BACK = 79  # Bus Acknowledge
  PIN_EKO = 80  # Serial Data Out (Link Cable)
  PIN_EKI = 81  # Serial Data In (Link Cable)
  PIN_SOUND = 82  # Stereo Audio Output (L+R)

end

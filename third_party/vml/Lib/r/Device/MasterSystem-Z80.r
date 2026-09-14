# Zilog-Z80 设备定义 - R 脚本
# 生成自: Zilog/Z80/Zilog-Z80
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: Sega Master System (Mark III) main processor - Zilog Z80A @ 3.58MHz
# CPU架构: Z80
# 位宽: 8位
# 时钟频率: 3580000 Hz

# 寄存器地址定义
A_ADDR <- 0x00  # Accumulator
F_ADDR <- 0x01  # Flags Register
F_C_BIT <- 0  # Carry
F_N_BIT <- 1  # Subtract
F_P_BIT <- 2  # Parity/Overflow
F_H_BIT <- 4  # Half Carry
F_Z_BIT <- 6  # Zero
F_S_BIT <- 7  # Sign/Negative
B_ADDR <- 0x02  # B Register
C_ADDR <- 0x03  # C Register
D_ADDR <- 0x04  # D Register
E_ADDR <- 0x05  # E Register
H_ADDR <- 0x06  # H Register
L_ADDR <- 0x07  # L Register
AF_ADDR <- 0x08  # Alternate AF
BC_ADDR <- 0x0A  # Alternate BC
DE_ADDR <- 0x0C  # Alternate DE
HL_ADDR <- 0x0E  # Alternate HL
IX_ADDR <- 0x10  # Index Register X
IY_ADDR <- 0x12  # Index Register Y
SP_ADDR <- 0x14  # Stack Pointer
PC_ADDR <- 0x16  # Program Counter
I_ADDR <- 0x18  # Interrupt Vector Register
R_ADDR <- 0x19  # Memory Refresh Register
IM_ADDR <- 0x1A  # Interrupt Mode (0/1/2)

# 内存段定义
WRAM_START <- 0xC000
WRAM_END <- 0xC7FF
WRAM_SIZE <- 2048  # Work RAM (2KB internal)
WRAM_SHADOW_START <- 0xE000
WRAM_SHADOW_END <- 0xE7FF
WRAM_SHADOW_SIZE <- 2048  # Work RAM Shadow (Echo RAM)
VRAM_START <- 0x4000
VRAM_END <- 0x7FFF
VRAM_SIZE <- 16384  # Video RAM (16KB)
SRAM_START <- 0x8000
SRAM_END <- 0xBFFF
SRAM_SIZE <- 16384  # Cartridge SRAM (if present)
CART_ROM_START <- 0x0000
CART_ROM_END <- 0x7FFF
CART_ROM_SIZE <- 32768  # Cartridge ROM (up to 48KB)
BIOS_START <- 0x0000
BIOS_END <- 0x1FFF
BIOS_SIZE <- 8192  # BIOS ROM (Master System built-in, 8KB)
IO_REGS_START <- 0x3F00
IO_REGS_END <- 0x3FFF
IO_REGS_SIZE <- 256  # I/O Register Area

# 外设定义
# Video Display Processor (TMS9918A variant)
VDP_BASE <- 0xBE
VDP_VDP_CTRL_ADDR <- 0xBF
VDP_VDP_DATA_ADDR <- 0xBE
VDP_VDP_STATUS_ADDR <- 0xBF
VDP_VDP_STATUS_FIFO_FULL_BIT <- 0  # VRAM to CPU Transfer Pending
VDP_VDP_STATUS_FIFO_EMPTY_BIT <- 1  # VRAM Write FIFO Empty
VDP_VDP_STATUS_INT_FLAG_BIT <- 7  # V-Blank / Sprite Collision Flag
VDP_R0_ADDR <- 0x00
VDP_R0_M3_BIT <- 0  # Mode 3 Enable
VDP_R0_M2_BIT <- 1  # Mode 2 Enable
VDP_R0_M1_BIT <- 2  # Mode 1 Enable
VDP_R0_DISPLAY_DISABLE_BIT <- 3  # Display Disable (1=blank screen)
VDP_R0_VIRQ_EN_BIT <- 4  # Vertical Interrupt Enable
VDP_R0_M4_BIT <- 5  # Mode 4 Enable (SMS2 only)
VDP_R0_SPRITE_SHIFT_BIT <- 6  # Sprite Double Height
VDP_R0_HVC_LATCH_BIT <- 7  # H-Counter Latch Enable
VDP_R1_ADDR <- 0x01
VDP_R1_DISPLAY_BIT <- 3  # Display Enable (1=active)
VDP_R1_FRAME_INT_BIT <- 4  # Frame Interrupt (V-Blank) Enable
VDP_R1_M4_BIT <- 5  # Mode 4 (256-color)
VDP_R1_SMS_MODE_BIT <- 6  # SMS Display Mode (vs Coleco)
VDP_R1_EXT_VIDEO_BIT <- 7  # External Video Enable
VDP_R2_ADDR <- 0x02
VDP_R3_ADDR <- 0x03
VDP_R4_ADDR <- 0x04
VDP_R5_ADDR <- 0x05
VDP_R6_ADDR <- 0x06
VDP_R7_ADDR <- 0x07
VDP_R8_ADDR <- 0x08
VDP_R8_HSCROLL_EN_BIT <- 0  # Horizontal Scroll Enable
VDP_R8_VSCROLL_EN_BIT <- 1  # Vertical Scroll Enable
VDP_R8_LINE_INT_BIT <- 4  # Line Interrupt Enable
VDP_R8_VSCROLL_2X_BIT <- 7  # Vertical Scroll 2x Speed
VDP_R9_ADDR <- 0x09
VDP_R10_ADDR <- 0x0A
VDP_R11_ADDR <- 0x0B
VDP_R12_ADDR <- 0x0C
VDP_R13_ADDR <- 0x0D
VDP_R14_ADDR <- 0x0E
VDP_R15_ADDR <- 0x0F
VDP_VCOUNTER_ADDR <- 0x7E
VDP_HCOUNTER_ADDR <- 0x7F
# SN76489 Programmable Sound Generator (3 Square + 1 Noise)
PSG_BASE <- 0x7F
PSG_CH0_FREQ_ADDR <- 0x00
PSG_CH1_FREQ_ADDR <- 0x02
PSG_CH2_FREQ_ADDR <- 0x04
PSG_CH3_CONFIG_ADDR <- 0x06
PSG_CH3_CONFIG_TYPE_BIT <- 0  # Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
PSG_CH3_CONFIG_VOLUME_BIT <- 0  # Volume (0-15)
PSG_CH0_VOLUME_ADDR <- 0x01
PSG_CH1_VOLUME_ADDR <- 0x03
PSG_CH2_VOLUME_ADDR <- 0x05
# I/O Port Registers
PORTS_BASE <- 0x3F
PORTS_PORT_A_ADDR <- 0x3F
PORTS_PORT_A_UP_BIT <- 0  # Up (0=pressed)
PORTS_PORT_A_DOWN_BIT <- 1  # Down (0=pressed)
PORTS_PORT_A_LEFT_BIT <- 2  # Left (0=pressed)
PORTS_PORT_A_RIGHT_BIT <- 3  # Right (0=pressed)
PORTS_PORT_A_TR_BIT <- 4  # Button TR (0=pressed)
PORTS_PORT_A_TL_BIT <- 5  # Button TL (0=pressed)
PORTS_PORT_B_ADDR <- 0x3F
PORTS_PORT_B_UP_BIT <- 0  # Up (0=pressed)
PORTS_PORT_B_DOWN_BIT <- 1  # Down (0=pressed)
PORTS_PORT_B_LEFT_BIT <- 2  # Left (0=pressed)
PORTS_PORT_B_RIGHT_BIT <- 3  # Right (0=pressed)
PORTS_PORT_B_TR_BIT <- 4  # Button TR (0=pressed)
PORTS_PORT_B_TL_BIT <- 5  # Button TL (0=pressed)
PORTS_PORT_A_DDR_ADDR <- 0x3F
PORTS_PORT_B_DDR_ADDR <- 0x3F
# Sega Mapper (Memory Bank Switching)
SEGAMAPPER_BASE <- 0xFFFD
SEGAMAPPER_ROM_BANK0_ADDR <- 0xFFFD
SEGAMAPPER_ROM_BANK1_ADDR <- 0xFFFE
SEGAMAPPER_ROM_BANK2_ADDR <- 0xFFFF
# Memory Mapper Control
MAPPER_BASE <- 0xFFFF
MAPPER_SRAM_BANK_ADDR <- 0xFFF8

# 中断向量定义
INT_NMI <- 0  # Non-Maskable Interrupt (Pause button / V-Blank)
INT_INT_VBLANK <- 1  # V-Blank Interrupt (Frame end)
INT_INT_LINE <- 2  # Scanline Interrupt (Line counter match)
INT_INT_EXT <- 3  # External I/O Interrupt

# 引脚定义
PIN_A <- 1  # Power Supply
PIN_GND <- 2  # Ground
PIN_PHI <- 3  # System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
PIN_RESET <- 4  # Reset (active low)
PIN_M1 <- 5  # Machine Cycle 1 (instruction fetch)
PIN_MREQ <- 6  # Memory Request
PIN_IORQ <- 7  # I/O Request
PIN_RD <- 8  # Read Strobe
PIN_WR <- 9  # Write Strobe
PIN_HALT <- 10  # Halt State
PIN_WAIT <- 11  # Wait State Request
PIN_INT <- 12  # Interrupt Request (active low)
PIN_NMI <- 13  # Non-Maskable Interrupt (active low)
PIN_BUSRQ <- 14  # Bus Request (active low)
PIN_BUSAK <- 15  # Bus Acknowledge (active low)
PIN_A0 <- 16  # Address Bus Bit 0
PIN_A1 <- 17  # Address Bus Bit 1
PIN_A2 <- 18  # Address Bus Bit 2
PIN_A3 <- 19  # Address Bus Bit 3
PIN_A4 <- 20  # Address Bus Bit 4
PIN_A5 <- 21  # Address Bus Bit 5
PIN_A6 <- 22  # Address Bus Bit 6
PIN_A7 <- 23  # Address Bus Bit 7
PIN_A8 <- 24  # Address Bus Bit 8
PIN_A9 <- 25  # Address Bus Bit 9
PIN_A10 <- 26  # Address Bus Bit 10
PIN_A11 <- 27  # Address Bus Bit 11
PIN_A12 <- 28  # Address Bus Bit 12
PIN_A13 <- 29  # Address Bus Bit 13
PIN_A14 <- 30  # Address Bus Bit 14
PIN_A15 <- 31  # Address Bus Bit 15
PIN_D0 <- 32  # Data Bus Bit 0
PIN_D1 <- 33  # Data Bus Bit 1
PIN_D2 <- 34  # Data Bus Bit 2
PIN_D3 <- 35  # Data Bus Bit 3
PIN_D4 <- 36  # Data Bus Bit 4
PIN_D5 <- 37  # Data Bus Bit 5
PIN_D6 <- 38  # Data Bus Bit 6
PIN_D7 <- 39  # Data Bus Bit 7
PIN_AUDIO_OUT <- 40  # Audio Output
PIN_VIDEO_SYNC <- 41  # Composite Video Sync
PIN_VIDEO_OUT <- 42  # Composite Video Output


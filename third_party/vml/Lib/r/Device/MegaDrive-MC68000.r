# Motorola-68000 设备定义 - R 脚本
# 生成自: Motorola/68000/Motorola-68000
# 版本: 1.0
# 日期: 2026-04-16
# 作者: VML Team
# 描述: 16/32-bit microprocessor used in Sega Genesis, Amiga, Atari ST, Macintosh
# CPU架构: MC68000
# 位宽: 32位
# 时钟频率: 7670452 Hz

# 寄存器地址定义
D0_ADDR <- 0x00  # Data Register 0
D1_ADDR <- 0x04  # Data Register 1
D2_ADDR <- 0x08  # Data Register 2
D3_ADDR <- 0x0C  # Data Register 3
D4_ADDR <- 0x10  # Data Register 4
D5_ADDR <- 0x14  # Data Register 5
D6_ADDR <- 0x18  # Data Register 6
D7_ADDR <- 0x1C  # Data Register 7
A0_ADDR <- 0x20  # Address Register 0
A1_ADDR <- 0x24  # Address Register 1
A2_ADDR <- 0x28  # Address Register 2
A3_ADDR <- 0x2C  # Address Register 3
A4_ADDR <- 0x30  # Address Register 4
A5_ADDR <- 0x34  # Address Register 5
A6_ADDR <- 0x38  # Address Register 6
A7_ADDR <- 0x3C  # Stack Pointer (USP)
PC_ADDR <- 0x40  # Program Counter
SR_ADDR <- 0x44  # Status Register
SR_C_BIT <- 0  # Carry
SR_V_BIT <- 1  # Overflow
SR_Z_BIT <- 2  # Zero
SR_N_BIT <- 3  # Negative
SR_X_BIT <- 4  # Extend
SR_I0_BIT <- 8  # Interrupt Mask 0
SR_I1_BIT <- 9  # Interrupt Mask 1
SR_I2_BIT <- 10  # Interrupt Mask 2
SR_M_BIT <- 11  # Master/Interrupt
SR_S_BIT <- 13  # Supervisor/User
SR_T0_BIT <- 14  # Trace Mode 0
SR_T1_BIT <- 15  # Trace Mode 1

# 内存段定义
RAM_START <- 0x000000
RAM_END <- 0x3FFFFF
RAM_SIZE <- 4194304  # System RAM (4MB)
ROM_START <- 0x000000
ROM_END <- 0x3FFFFF
ROM_SIZE <- 4194304  # Cartridge ROM
IO_START <- 0xA00000
IO_END <- 0xA1FFFF
IO_SIZE <- 131072  # I/O Register Area
VDP_START <- 0xC00000
VDP_END <- 0xC0001F
VDP_SIZE <- 32  # VDP Registers
VRAM_START <- 0xE00000
VRAM_END <- 0xE3FFFF
VRAM_SIZE <- 262144  # Video RAM (256KB)

# 外设定义
# Video Display Processor (TMS9918A variant)
VDP_BASE <- 0xC00000
VDP_DATA_ADDR <- 0x00
VDP_CTRL_ADDR <- 0x04
VDP_HVCOUNT_ADDR <- 0x08
VDP_HVB_STATUS_ADDR <- 0x0A
# Programmable Sound Generator (AY-3-8910)
PSG_BASE <- 0xC00011
PSG_CH_A_FREQ_ADDR <- 0x00
PSG_CH_A_VOL_ADDR <- 0x08
PSG_CH_B_FREQ_ADDR <- 0x02
PSG_CH_B_VOL_ADDR <- 0x09
PSG_CH_C_FREQ_ADDR <- 0x04
PSG_CH_C_VOL_ADDR <- 0x0A
PSG_NOISE_FREQ_ADDR <- 0x06
PSG_MIXER_ADDR <- 0x07
PSG_ENV_FREQ_ADDR <- 0x0D
PSG_ENV_SHAPE_ADDR <- 0x0B
# Z80 Secondary CPU (Sound)
Z80_BASE <- 0xA00000
Z80_Z80_RESET_ADDR <- 0x00
Z80_Z80_BUSREQ_ADDR <- 0x04
Z80_Z80_STATUS_ADDR <- 0x08
# Bank Register
BANK_REG_BASE <- 0xA12000
BANK_REG_ROM_BANK_ADDR <- 0x00
BANK_REG_RAM_BANK_ADDR <- 0x04
# Hardware Version
HW_VERSION_BASE <- 0xA10001
HW_VERSION_VERSION_ADDR <- 0x00
# Controller Port 1
CONTROLLER1_BASE <- 0xA10003
CONTROLLER1_DATA_ADDR <- 0x00
CONTROLLER1_CTRL_ADDR <- 0x04
# Controller Port 2
CONTROLLER2_BASE <- 0xA10005
CONTROLLER2_DATA_ADDR <- 0x00
CONTROLLER2_CTRL_ADDR <- 0x04
# External Port
EXT_PORT_BASE <- 0xA10007
EXT_PORT_DATA_ADDR <- 0x00
# DMA Controller
DMA_BASE <- 0xA10008
DMA_SOURCE_ADDR <- 0x00
DMA_DEST_ADDR <- 0x04
DMA_COUNT_ADDR <- 0x08
DMA_CTRL_ADDR <- 0x0A
# Hardware Timer
TIMER_BASE <- 0xA1000E
TIMER_H_COUNTER_ADDR <- 0x00
TIMER_V_COUNTER_ADDR <- 0x04

# 中断向量定义
INT_RESET_SP <- 1  # Reset Initial Stack Pointer
INT_RESET_PC <- 2  # Reset Initial PC
INT_BUS_ERROR <- 3  # Bus Error
INT_ADDRESS_ERROR <- 4  # Address Error
INT_ILLEGAL_INSTR <- 5  # Illegal Instruction
INT_ZERO_DIVIDE <- 6  # Zero Divide
INT_CHK_EXCEPTION <- 7  # CHK Exception
INT_TRAPV <- 8  # TRAPV Exception
INT_PRIVILEGE <- 9  # Privilege Violation
INT_TRACE <- 10  # Trace
INT_LINE_A <- 11  # Line 1010 Emulator
INT_LINE_F <- 12  # Line 1111 Emulator
INT_IRQ1 <- 24  # External Interrupt 1 (H-Blank)
INT_IRQ2 <- 25  # External Interrupt 2 (V-Blank)
INT_IRQ3 <- 26  # External Interrupt 3
INT_IRQ4 <- 27  # External Interrupt 4 (D-Req)
INT_IRQ5 <- 28  # External Interrupt 5
INT_IRQ6 <- 29  # External Interrupt 6
INT_IRQ7 <- 30  # External Interrupt 7
INT_TRAP0 <- 32  # TRAP #0
INT_TRAP1 <- 33  # TRAP #1
INT_TRAP15 <- 47  # TRAP #15


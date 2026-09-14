# ZX-Spectrum 设备定义 - R 脚本
# 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: ZX Spectrum 48K home computer with Z80 CPU, 48KB RAM, and color graphics
# CPU架构: Zilog Z80
# 位宽: 8位
# 时钟频率: 3500000 Hz

# 寄存器地址定义
A_ADDR <- 0  # Accumulator
F_ADDR <- 0  # Flags
B_ADDR <- 0  # B
C_ADDR <- 0  # C
D_ADDR <- 0  # D
E_ADDR <- 0  # E
H_ADDR <- 0  # H
L_ADDR <- 0  # L
IX_ADDR <- 0  # Index Register X
IY_ADDR <- 0  # Index Register Y
SP_ADDR <- 0  # Stack Pointer
PC_ADDR <- 0  # Program Counter
I_ADDR <- 0  # Interrupt Vector
R_ADDR <- 0  # Memory Refresh
AF_ADDR <- 0  # Alternate AF
BC_ADDR <- 0  # Alternate BC
DE_ADDR <- 0  # Alternate DE
HL_ADDR <- 0  # Alternate HL

# 外设定义
# Uncommitted Logic Array (video and I/O)
ULA_BASE <- 
ULA_ULA_PORT_FE_ADDR <- 0xFE
ULA_ULA_BORDER_ADDR <- 0xFE
ULA_ULA_BEEPER_ADDR <- 0xFE
ULA_ULA_MIC_ADDR <- 0xFE
# General Instruments AY-3-8912 sound chip
AY_3_8912_BASE <- 
AY_3_8912_AY_REG_SEL_ADDR <- 0xFFFD
AY_3_8912_AY_DATA_ADDR <- 0xBFFD
AY_3_8912_AY_READ_ADDR <- 0xFFFD
# 40-key rubber keyboard
KEYBOARD_BASE <- 
KEYBOARD_KEY_ROW0_ADDR <- 0xFEFE
KEYBOARD_KEY_ROW1_ADDR <- 0xFDFE
KEYBOARD_KEY_ROW2_ADDR <- 0xFBFE
KEYBOARD_KEY_ROW3_ADDR <- 0xF7FE
KEYBOARD_KEY_ROW4_ADDR <- 0xEFFE
KEYBOARD_KEY_ROW5_ADDR <- 0xDFFE
KEYBOARD_KEY_ROW6_ADDR <- 0xBFFE
KEYBOARD_KEY_ROW7_ADDR <- 0x7FFE
# Kempston joystick interface
KEMPSTON_BASE <- 
KEMPSTON_KEMPSTON_JOY_ADDR <- 0x1F
# ZX Interface 1 (RS-232 and Microdrive)
INTERFACE1_BASE <- 
INTERFACE1_IF1_STATUS_ADDR <- 0x1FFD
INTERFACE1_IF1_DATA_ADDR <- 0x3FFD
# ZX Interface 2 (joystick and ROM cartridge)
INTERFACE2_BASE <- 
INTERFACE2_IF2_JOY1_ADDR <- 0x1F
INTERFACE2_IF2_JOY2_ADDR <- 0x37

# 中断向量定义
INT_IM1 <- 56  # Interrupt Mode 1
INT_RST_00 <- 0  # Restart 00h
INT_RST_08 <- 8  # Restart 08h
INT_RST_10 <- 16  # Restart 10h
INT_RST_18 <- 24  # Restart 18h
INT_RST_20 <- 32  # Restart 20h
INT_RST_28 <- 40  # Restart 28h
INT_RST_30 <- 48  # Restart 30h
INT_RST_38 <- 56  # Restart 38h


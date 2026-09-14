! Motorola-68000 设备定义 - Fortran 模块
! 生成自: Motorola/68000/Motorola-68000
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: 16/32-bit microprocessor used in Sega Genesis, Amiga, Atari ST, Macintosh
! CPU架构: MC68000
! 位宽: 32位
! 时钟频率: 7670452 Hz

module motorola_68000_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: D0_ADDR = 0x00  ! Data Register 0
  integer, parameter :: D1_ADDR = 0x04  ! Data Register 1
  integer, parameter :: D2_ADDR = 0x08  ! Data Register 2
  integer, parameter :: D3_ADDR = 0x0C  ! Data Register 3
  integer, parameter :: D4_ADDR = 0x10  ! Data Register 4
  integer, parameter :: D5_ADDR = 0x14  ! Data Register 5
  integer, parameter :: D6_ADDR = 0x18  ! Data Register 6
  integer, parameter :: D7_ADDR = 0x1C  ! Data Register 7
  integer, parameter :: A0_ADDR = 0x20  ! Address Register 0
  integer, parameter :: A1_ADDR = 0x24  ! Address Register 1
  integer, parameter :: A2_ADDR = 0x28  ! Address Register 2
  integer, parameter :: A3_ADDR = 0x2C  ! Address Register 3
  integer, parameter :: A4_ADDR = 0x30  ! Address Register 4
  integer, parameter :: A5_ADDR = 0x34  ! Address Register 5
  integer, parameter :: A6_ADDR = 0x38  ! Address Register 6
  integer, parameter :: A7_ADDR = 0x3C  ! Stack Pointer (USP)
  integer, parameter :: PC_ADDR = 0x40  ! Program Counter
  integer, parameter :: SR_ADDR = 0x44  ! Status Register
  integer, parameter :: SR_C_BIT = 0  ! Carry
  integer, parameter :: SR_V_BIT = 1  ! Overflow
  integer, parameter :: SR_Z_BIT = 2  ! Zero
  integer, parameter :: SR_N_BIT = 3  ! Negative
  integer, parameter :: SR_X_BIT = 4  ! Extend
  integer, parameter :: SR_I0_BIT = 8  ! Interrupt Mask 0
  integer, parameter :: SR_I1_BIT = 9  ! Interrupt Mask 1
  integer, parameter :: SR_I2_BIT = 10  ! Interrupt Mask 2
  integer, parameter :: SR_M_BIT = 11  ! Master/Interrupt
  integer, parameter :: SR_S_BIT = 13  ! Supervisor/User
  integer, parameter :: SR_T0_BIT = 14  ! Trace Mode 0
  integer, parameter :: SR_T1_BIT = 15  ! Trace Mode 1

  ! 内存段定义
  integer, parameter :: RAM_START = 0x000000
  integer, parameter :: RAM_END = 0x3FFFFF
  integer, parameter :: RAM_SIZE = 4194304  ! System RAM (4MB)
  integer, parameter :: ROM_START = 0x000000
  integer, parameter :: ROM_END = 0x3FFFFF
  integer, parameter :: ROM_SIZE = 4194304  ! Cartridge ROM
  integer, parameter :: IO_START = 0xA00000
  integer, parameter :: IO_END = 0xA1FFFF
  integer, parameter :: IO_SIZE = 131072  ! I/O Register Area
  integer, parameter :: VDP_START = 0xC00000
  integer, parameter :: VDP_END = 0xC0001F
  integer, parameter :: VDP_SIZE = 32  ! VDP Registers
  integer, parameter :: VRAM_START = 0xE00000
  integer, parameter :: VRAM_END = 0xE3FFFF
  integer, parameter :: VRAM_SIZE = 262144  ! Video RAM (256KB)

  ! 外设定义
  ! Video Display Processor (TMS9918A variant)
  integer, parameter :: VDP_BASE = 0xC00000
  integer, parameter :: VDP_DATA_ADDR = 0x00
  integer, parameter :: VDP_CTRL_ADDR = 0x04
  integer, parameter :: VDP_HVCOUNT_ADDR = 0x08
  integer, parameter :: VDP_HVB_STATUS_ADDR = 0x0A
  ! Programmable Sound Generator (AY-3-8910)
  integer, parameter :: PSG_BASE = 0xC00011
  integer, parameter :: PSG_CH_A_FREQ_ADDR = 0x00
  integer, parameter :: PSG_CH_A_VOL_ADDR = 0x08
  integer, parameter :: PSG_CH_B_FREQ_ADDR = 0x02
  integer, parameter :: PSG_CH_B_VOL_ADDR = 0x09
  integer, parameter :: PSG_CH_C_FREQ_ADDR = 0x04
  integer, parameter :: PSG_CH_C_VOL_ADDR = 0x0A
  integer, parameter :: PSG_NOISE_FREQ_ADDR = 0x06
  integer, parameter :: PSG_MIXER_ADDR = 0x07
  integer, parameter :: PSG_ENV_FREQ_ADDR = 0x0D
  integer, parameter :: PSG_ENV_SHAPE_ADDR = 0x0B
  ! Z80 Secondary CPU (Sound)
  integer, parameter :: Z80_BASE = 0xA00000
  integer, parameter :: Z80_Z80_RESET_ADDR = 0x00
  integer, parameter :: Z80_Z80_BUSREQ_ADDR = 0x04
  integer, parameter :: Z80_Z80_STATUS_ADDR = 0x08
  ! Bank Register
  integer, parameter :: BANK_REG_BASE = 0xA12000
  integer, parameter :: BANK_REG_ROM_BANK_ADDR = 0x00
  integer, parameter :: BANK_REG_RAM_BANK_ADDR = 0x04
  ! Hardware Version
  integer, parameter :: HW_VERSION_BASE = 0xA10001
  integer, parameter :: HW_VERSION_VERSION_ADDR = 0x00
  ! Controller Port 1
  integer, parameter :: CONTROLLER1_BASE = 0xA10003
  integer, parameter :: CONTROLLER1_DATA_ADDR = 0x00
  integer, parameter :: CONTROLLER1_CTRL_ADDR = 0x04
  ! Controller Port 2
  integer, parameter :: CONTROLLER2_BASE = 0xA10005
  integer, parameter :: CONTROLLER2_DATA_ADDR = 0x00
  integer, parameter :: CONTROLLER2_CTRL_ADDR = 0x04
  ! External Port
  integer, parameter :: EXT_PORT_BASE = 0xA10007
  integer, parameter :: EXT_PORT_DATA_ADDR = 0x00
  ! DMA Controller
  integer, parameter :: DMA_BASE = 0xA10008
  integer, parameter :: DMA_SOURCE_ADDR = 0x00
  integer, parameter :: DMA_DEST_ADDR = 0x04
  integer, parameter :: DMA_COUNT_ADDR = 0x08
  integer, parameter :: DMA_CTRL_ADDR = 0x0A
  ! Hardware Timer
  integer, parameter :: TIMER_BASE = 0xA1000E
  integer, parameter :: TIMER_H_COUNTER_ADDR = 0x00
  integer, parameter :: TIMER_V_COUNTER_ADDR = 0x04

  ! 中断向量定义
  integer, parameter :: INT_RESET_SP = 1  ! Reset Initial Stack Pointer
  integer, parameter :: INT_RESET_PC = 2  ! Reset Initial PC
  integer, parameter :: INT_BUS_ERROR = 3  ! Bus Error
  integer, parameter :: INT_ADDRESS_ERROR = 4  ! Address Error
  integer, parameter :: INT_ILLEGAL_INSTR = 5  ! Illegal Instruction
  integer, parameter :: INT_ZERO_DIVIDE = 6  ! Zero Divide
  integer, parameter :: INT_CHK_EXCEPTION = 7  ! CHK Exception
  integer, parameter :: INT_TRAPV = 8  ! TRAPV Exception
  integer, parameter :: INT_PRIVILEGE = 9  ! Privilege Violation
  integer, parameter :: INT_TRACE = 10  ! Trace
  integer, parameter :: INT_LINE_A = 11  ! Line 1010 Emulator
  integer, parameter :: INT_LINE_F = 12  ! Line 1111 Emulator
  integer, parameter :: INT_IRQ1 = 24  ! External Interrupt 1 (H-Blank)
  integer, parameter :: INT_IRQ2 = 25  ! External Interrupt 2 (V-Blank)
  integer, parameter :: INT_IRQ3 = 26  ! External Interrupt 3
  integer, parameter :: INT_IRQ4 = 27  ! External Interrupt 4 (D-Req)
  integer, parameter :: INT_IRQ5 = 28  ! External Interrupt 5
  integer, parameter :: INT_IRQ6 = 29  ! External Interrupt 6
  integer, parameter :: INT_IRQ7 = 30  ! External Interrupt 7
  integer, parameter :: INT_TRAP0 = 32  ! TRAP #0
  integer, parameter :: INT_TRAP1 = 33  ! TRAP #1
  integer, parameter :: INT_TRAP15 = 47  ! TRAP #15

end module motorola_68000_device

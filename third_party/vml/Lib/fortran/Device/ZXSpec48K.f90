! ZX-Spectrum-48K 设备定义 - Fortran 模块
! 生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics
! CPU架构: Z80A
! 位宽: 8位
! 时钟频率: 3500000 Hz

module zx_spectrum_48k_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0x00  ! Accumulator
  integer, parameter :: F_ADDR = 0x01  ! Flags Register
  integer, parameter :: F_C_BIT = 0  ! Carry
  integer, parameter :: F_N_BIT = 1  ! Add/Subtract
  integer, parameter :: F_PV_BIT = 2  ! Parity/Overflow
  integer, parameter :: F_H_BIT = 4  ! Half Carry
  integer, parameter :: F_Z_BIT = 6  ! Zero
  integer, parameter :: F_S_BIT = 7  ! Sign
  integer, parameter :: B_ADDR = 0x02  ! B Register
  integer, parameter :: C_ADDR = 0x03  ! C Register
  integer, parameter :: D_ADDR = 0x04  ! D Register
  integer, parameter :: E_ADDR = 0x05  ! E Register
  integer, parameter :: H_ADDR = 0x06  ! H Register
  integer, parameter :: L_ADDR = 0x07  ! L Register
  integer, parameter :: AF_ADDR = 0x08  ! Alternate AF
  integer, parameter :: BC_ADDR = 0x0A  ! Alternate BC
  integer, parameter :: DE_ADDR = 0x0C  ! Alternate DE
  integer, parameter :: HL_ADDR = 0x0E  ! Alternate HL
  integer, parameter :: I_ADDR = 0x10  ! Interrupt Vector Register
  integer, parameter :: R_ADDR = 0x11  ! Refresh Counter
  integer, parameter :: IX_ADDR = 0x12  ! Index X
  integer, parameter :: IY_ADDR = 0x14  ! Index Y
  integer, parameter :: SP_ADDR = 0x16  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0x18  ! Program Counter

  ! 内存段定义
  integer, parameter :: ROM_START = 0x0000
  integer, parameter :: ROM_END = 0x3FFF
  integer, parameter :: ROM_SIZE = 16384  ! 48KB ZX Spectrum ROM (BASIC + monitor)
  integer, parameter :: VIDEO_RAM_START = 0x4000
  integer, parameter :: VIDEO_RAM_END = 0x57FF
  integer, parameter :: VIDEO_RAM_SIZE = 6144  ! Display file (256x192 bitmap)
  integer, parameter :: ATTR_RAM_START = 0x5800
  integer, parameter :: ATTR_RAM_END = 0x5AFF
  integer, parameter :: ATTR_RAM_SIZE = 768  ! Attribute file (32x24 color cells)
  integer, parameter :: USER_RAM_START = 0x5B00
  integer, parameter :: USER_RAM_END = 0xFFFF
  integer, parameter :: USER_RAM_SIZE = 40960  ! User RAM (40KB)

  ! 外设定义
  ! Uncommitted Logic Array - Sinclair custom IC
  integer, parameter :: ULA_BASE = 0xFE
  integer, parameter :: ULA_BORDER_ADDR = 0xFE
  integer, parameter :: ULA_KBD_ROW0_ADDR = 0xFE
  integer, parameter :: ULA_KBD_ROW1_ADDR = 0xFE
  integer, parameter :: ULA_KBD_ROW2_ADDR = 0xFE
  integer, parameter :: ULA_KBD_ROW3_ADDR = 0xFE
  integer, parameter :: ULA_KBD_ROW4_ADDR = 0xFE
  integer, parameter :: ULA_KBD_ROW5_ADDR = 0xFE
  integer, parameter :: ULA_KBD_ROW6_ADDR = 0xFE
  integer, parameter :: ULA_KBD_ROW7_ADDR = 0xFE
  integer, parameter :: ULA_KBD_ROW8_ADDR = 0xFE
  ! Keyboard Matrix (40 keys, 8 rows x 5 cols)
  integer, parameter :: KEYBOARD_BASE = 0xFE
  integer, parameter :: KEYBOARD_KBD_IN_ADDR = 0xFE
  ! Internal Beeper
  integer, parameter :: BEEPER_BASE = 0xFE
  integer, parameter :: BEEPER_BEEP_ADDR = 0xFE
  ! Tape Interface
  integer, parameter :: TAPE_BASE = 0xFE
  integer, parameter :: TAPE_EAR_IN_ADDR = 0xFE
  integer, parameter :: TAPE_MIC_OUT_ADDR = 0xFE
  ! Kempston Joystick Interface
  integer, parameter :: JOYSTICK_BASE = 0xF7FE
  integer, parameter :: JOYSTICK_KEMPSTON_ADDR = 0xF7FE

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Power-on / Reset
  integer, parameter :: INT_NMI = 1  ! Non-Maskable Interrupt (BREAK key)
  integer, parameter :: INT_INT = 2  ! Maskable Interrupt (ULA vertical blank, 50Hz)

  ! 引脚定义
  integer, parameter :: PIN_VCC = 1  ! +5V Power
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_CLK = 3  ! Z80 Clock (3.5MHz)
  integer, parameter :: PIN_M1 = 4  ! Machine Cycle 1
  integer, parameter :: PIN_MREQ = 5  ! Memory Request
  integer, parameter :: PIN_IORQ = 6  ! I/O Request
  integer, parameter :: PIN_RD = 7  ! Read
  integer, parameter :: PIN_WR = 8  ! Write
  integer, parameter :: PIN_HALT = 9  ! Halt State
  integer, parameter :: PIN_BUSAK = 10  ! Bus Acknowledge
  integer, parameter :: PIN_WAIT = 11  ! Wait State (ULA inserts)
  integer, parameter :: PIN_INT = 12  ! Interrupt Request
  integer, parameter :: PIN_NMI = 13  ! Non-Maskable Interrupt
  integer, parameter :: PIN_RESET = 14  ! Reset
  integer, parameter :: PIN_A0_A15 = 15  ! Address Bus (16-bit)
  integer, parameter :: PIN_D0_D7 = 16  ! Data Bus (8-bit)

end module zx_spectrum_48k_device

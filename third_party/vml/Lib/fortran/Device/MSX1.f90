! MSX1 设备定义 - Fortran 模块
! 生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: MSX - Standardized 8-bit home computer with Z80A CPU, TMS9918A graphics, and AY-3-8910 audio
! CPU架构: Z80A
! 位宽: 8位
! 时钟频率: 3579545 Hz

module msx1_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0x00  ! Accumulator
  integer, parameter :: F_ADDR = 0x01  ! Flags
  integer, parameter :: F_C_BIT = 0  ! Carry
  integer, parameter :: F_N_BIT = 1  ! Subtract
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
  integer, parameter :: I_ADDR = 0x10  ! Interrupt Vector
  integer, parameter :: R_ADDR = 0x11  ! Refresh
  integer, parameter :: IX_ADDR = 0x12  ! Index X
  integer, parameter :: IY_ADDR = 0x14  ! Index Y (usually = 0xF38F)
  integer, parameter :: SP_ADDR = 0x16  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0x18  ! Program Counter

  ! 内存段定义
  integer, parameter :: SLOT0_ROM_START = 0x0000
  integer, parameter :: SLOT0_ROM_END = 0x7FFF
  integer, parameter :: SLOT0_ROM_SIZE = 32768  ! Cartridge/SUB-ROM / Main-ROM
  integer, parameter :: SYSROM_START = 0x0000
  integer, parameter :: SYSROM_END = 0x3FFF
  integer, parameter :: SYSROM_SIZE = 16384  ! MSX-BIOS ROM
  integer, parameter :: EXTROM_START = 0x4000
  integer, parameter :: EXTROM_END = 0x7FFF
  integer, parameter :: EXTROM_SIZE = 16384  ! Extension ROM (cartridge)
  integer, parameter :: MAIN_RAM_START = 0x4000
  integer, parameter :: MAIN_RAM_END = 0xC000
  integer, parameter :: MAIN_RAM_SIZE = 32768  ! Main RAM (32KB working area)
  integer, parameter :: WORK_RAM_START = 0xC000
  integer, parameter :: WORK_RAM_END = 0xFFFF
  integer, parameter :: WORK_RAM_SIZE = 16384  ! Work RAM (16KB)
  integer, parameter :: SYSVAR_START = 0xF000
  integer, parameter :: SYSVAR_END = 0xFCA0
  integer, parameter :: SYSVAR_SIZE = 3232  ! System variables area
  integer, parameter :: SLOTS_START = 0x8000
  integer, parameter :: SLOTS_END = 0xFFFF
  integer, parameter :: SLOTS_SIZE = 32768  ! Slot-mapped memory

  ! 外设定义
  ! TMS9918A Video Display Processor
  integer, parameter :: VDP_BASE = 0x98
  integer, parameter :: VDP_VDP_REG0_ADDR = 0x99
  integer, parameter :: VDP_VDP_REG1_ADDR = 0x99
  integer, parameter :: VDP_VDP_REG2_ADDR = 0x99
  integer, parameter :: VDP_VDP_REG3_ADDR = 0x99
  integer, parameter :: VDP_VDP_REG4_ADDR = 0x99
  integer, parameter :: VDP_VDP_REG5_ADDR = 0x99
  integer, parameter :: VDP_VDP_REG6_ADDR = 0x99
  integer, parameter :: VDP_VDP_REG7_ADDR = 0x99
  integer, parameter :: VDP_VDP_STATUS_ADDR = 0x99
  integer, parameter :: VDP_VDP_DATA_ADDR = 0x98
  integer, parameter :: VDP_VDP_POT_ADDR = 0x98
  ! AY-3-8910 Programmable Sound Generator
  integer, parameter :: PSG_BASE = 0xA0
  integer, parameter :: PSG_PSG_REG_ADDR = 0xA1
  integer, parameter :: PSG_PSG_DATA_ADDR = 0xA3
  integer, parameter :: PSG_FREQ_A_LO_ADDR = 0xA0
  integer, parameter :: PSG_FREQ_A_HI_ADDR = 0xA1
  integer, parameter :: PSG_FREQ_B_LO_ADDR = 0xA2
  integer, parameter :: PSG_FREQ_B_HI_ADDR = 0xA3
  integer, parameter :: PSG_FREQ_C_LO_ADDR = 0xA4
  integer, parameter :: PSG_FREQ_C_HI_ADDR = 0xA5
  integer, parameter :: PSG_NOISE_FREQ_ADDR = 0xA6
  integer, parameter :: PSG_ENABLE_ADDR = 0xA7
  integer, parameter :: PSG_VOL_A_ADDR = 0xA8
  integer, parameter :: PSG_VOL_B_ADDR = 0xA9
  integer, parameter :: PSG_VOL_C_ADDR = 0xAA
  integer, parameter :: PSG_ENV_FREQ_LO_ADDR = 0xAB
  integer, parameter :: PSG_ENV_FREQ_HI_ADDR = 0xAC
  integer, parameter :: PSG_ENV_SHAPE_ADDR = 0xAD
  integer, parameter :: PSG_PORT_A_ADDR = 0xAE
  integer, parameter :: PSG_PORT_B_ADDR = 0xAF
  ! PPI 8255 Programmable Peripheral Interface
  integer, parameter :: PPI_BASE = 0xA8
  integer, parameter :: PPI_PPI_PA_ADDR = 0xA8
  integer, parameter :: PPI_PPI_PB_ADDR = 0xA9
  integer, parameter :: PPI_PPI_PC_ADDR = 0xAA
  integer, parameter :: PPI_PPI_CTRL_ADDR = 0xAB
  ! MSX Slot Expansion System
  integer, parameter :: SLOTEXP_BASE = 0x0000
  integer, parameter :: SLOTEXP_SLOT0_ADDR = 0xFCC0
  integer, parameter :: SLOTEXP_SLOT1_ADDR = 0xFCC1
  integer, parameter :: SLOTEXP_SLOT2_ADDR = 0xFCC2
  integer, parameter :: SLOTEXP_SLOT3_ADDR = 0xFCC3
  integer, parameter :: SLOTEXP_EXPTBL0_ADDR = 0xFCC4
  integer, parameter :: SLOTEXP_EXPTBL1_ADDR = 0xFCC5
  integer, parameter :: SLOTEXP_EXPTBL2_ADDR = 0xFCC6
  integer, parameter :: SLOTEXP_EXPTBL3_ADDR = 0xFCC7

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Power-on / Reset
  integer, parameter :: INT_NMI = 1  ! Non-Maskable Interrupt
  integer, parameter :: INT_INT = 2  ! VDP Vertical Interrupt (frame)

  ! 引脚定义
  integer, parameter :: PIN_VCC = 1  ! +5V Power
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_CLK = 3  ! Z80 Clock (3.58MHz)
  integer, parameter :: PIN_A0_A15 = 4  ! Address Bus
  integer, parameter :: PIN_D0_D7 = 5  ! Data Bus
  integer, parameter :: PIN_MREQ = 6  ! Memory Request
  integer, parameter :: PIN_IORQ = 7  ! I/O Request
  integer, parameter :: PIN_RD = 8  ! Read
  integer, parameter :: PIN_WR = 9  ! Write
  integer, parameter :: PIN_INT = 10  ! Interrupt Request
  integer, parameter :: PIN_NMI = 11  ! Non-Maskable Interrupt
  integer, parameter :: PIN_RESET = 12  ! Reset
  integer, parameter :: PIN_SLTSL = 13  ! Slot select (for memory mapping)
  integer, parameter :: PIN_WAIT = 14  ! Wait (for slow I/O)

end module msx1_device

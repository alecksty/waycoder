! Apple-IIe 设备定义 - Fortran 模块
! 生成自: Apple Computer/Apple II/Apple-IIe
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Apple II Enhanced - 8-bit personal computer with MOS 6502 CPU
! CPU架构: MOS-6502
! 位宽: 8位
! 时钟频率: 1021800 Hz

module apple_iie_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0x00  ! Accumulator
  integer, parameter :: X_ADDR = 0x01  ! X Index Register
  integer, parameter :: Y_ADDR = 0x02  ! Y Index Register
  integer, parameter :: SP_ADDR = 0x03  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0x04  ! Program Counter
  integer, parameter :: P_ADDR = 0x06  ! Processor Status
  integer, parameter :: P_C_BIT = 0  ! Carry Flag
  integer, parameter :: P_Z_BIT = 1  ! Zero Flag
  integer, parameter :: P_I_BIT = 2  ! Interrupt Disable
  integer, parameter :: P_D_BIT = 3  ! Decimal Mode
  integer, parameter :: P_B_BIT = 4  ! Break Command
  integer, parameter :: P_U_BIT = 5  ! Unused
  integer, parameter :: P_V_BIT = 6  ! Overflow Flag
  integer, parameter :: P_N_BIT = 7  ! Negative Flag

  ! 内存段定义
  integer, parameter :: MAIN_RAM_START = 0x0000
  integer, parameter :: MAIN_RAM_END = 0xBFFF
  integer, parameter :: MAIN_RAM_SIZE = 49152  ! Main RAM (48KB base, up to 64KB with slot RAM)
  integer, parameter :: TEXT_RAM_START = 0x0400
  integer, parameter :: TEXT_RAM_END = 0x07FF
  integer, parameter :: TEXT_RAM_SIZE = 1024  ! Text screen buffer (40x24)
  integer, parameter :: HIRES_RAM_START = 0x2000
  integer, parameter :: HIRES_RAM_END = 0x5FFF
  integer, parameter :: HIRES_RAM_SIZE = 16384  ! High-resolution graphics buffer
  integer, parameter :: AUX_RAM_START = 0x0400
  integer, parameter :: AUX_RAM_END = 0x09FF
  integer, parameter :: AUX_RAM_SIZE = 1536  ! 80-column text auxiliary RAM
  integer, parameter :: MONITOR_ROM_START = 0xC100
  integer, parameter :: MONITOR_ROM_END = 0xCFFF
  integer, parameter :: MONITOR_ROM_SIZE = 3840  ! Monitor ROM (applesoft/Integer)
  integer, parameter :: BASIC_ROM_START = 0xD000
  integer, parameter :: BASIC_ROM_END = 0xFFFF
  integer, parameter :: BASIC_ROM_SIZE = 12288  ! Applesoft BASIC ROM
  integer, parameter :: SLOT_ROM_START = 0xC100
  integer, parameter :: SLOT_ROM_END = 0xC7FF
  integer, parameter :: SLOT_ROM_SIZE = 768  ! Expansion Slot ROM
  integer, parameter :: MMIO_START = 0xC080
  integer, parameter :: MMIO_END = 0xC0FF
  integer, parameter :: MMIO_SIZE = 128  ! I/O Select (slot space)

  ! 外设定义
  ! Versatile Interface Adapter (6522)
  integer, parameter :: VIA_BASE = 0xC000
  integer, parameter :: VIA_ORB_ADDR = 0xC000
  integer, parameter :: VIA_ORA_ADDR = 0xC001
  integer, parameter :: VIA_DDRB_ADDR = 0xC002
  integer, parameter :: VIA_DDRA_ADDR = 0xC003
  integer, parameter :: VIA_T1C_ADDR = 0xC004
  integer, parameter :: VIA_T1L_ADDR = 0xC006
  integer, parameter :: VIA_T2C_ADDR = 0xC008
  integer, parameter :: VIA_SR_ADDR = 0xC00A
  integer, parameter :: VIA_ACR_ADDR = 0xC00B
  integer, parameter :: VIA_PCR_ADDR = 0xC00C
  integer, parameter :: VIA_IFG_ADDR = 0xC00D
  integer, parameter :: VIA_IER_ADDR = 0xC00E
  integer, parameter :: VIA_ORA_NH_ADDR = 0xC00F
  ! Peripheral Interface Adapter (6520)
  integer, parameter :: PIA_BASE = 0xC010
  integer, parameter :: PIA_PA_ADDR = 0xC010
  integer, parameter :: PIA_PB_ADDR = 0xC011
  integer, parameter :: PIA_DDRA_ADDR = 0xC012
  integer, parameter :: PIA_DDRB_ADDR = 0xC013
  integer, parameter :: PIA_CA1_ADDR = 0xC014
  integer, parameter :: PIA_CA2_ADDR = 0xC015
  integer, parameter :: PIA_CB1_ADDR = 0xC016
  integer, parameter :: PIA_CB2_ADDR = 0xC017
  ! Keyboard (via PIA)
  integer, parameter :: KBD_BASE = 0xC000
  integer, parameter :: KBD_KEYDATA_ADDR = 0xC000
  integer, parameter :: KBD_KEYSTROBE_ADDR = 0xC010
  integer, parameter :: KBD_KBDCTRL_ADDR = 0xC025
  integer, parameter :: KBD_KBDERR_ADDR = 0xC026
  ! Speaker
  integer, parameter :: SPEAKER_BASE = 0xC030
  integer, parameter :: SPEAKER_SPKR_ADDR = 0xC030
  ! Game I/O Port
  integer, parameter :: GAME_PORT_BASE = 0xC050
  integer, parameter :: GAME_PORT_GAME_SW0_ADDR = 0xC061
  integer, parameter :: GAME_PORT_GAME_SW1_ADDR = 0xC062
  integer, parameter :: GAME_PORT_GAME_AN0_ADDR = 0xC064
  integer, parameter :: GAME_PORT_GAME_AN1_ADDR = 0xC065
  integer, parameter :: GAME_PORT_GAME_AN2_ADDR = 0xC066
  integer, parameter :: GAME_PORT_GAME_AN3_ADDR = 0xC067
  integer, parameter :: GAME_PORT_GAME_TRIG_ADDR = 0xC070
  ! Disk II Controller
  integer, parameter :: DISKII_BASE = 0xC0E0
  integer, parameter :: DISKII_PHASE0_ADDR = 0xC0E0
  integer, parameter :: DISKII_PHASE1_ADDR = 0xC0E1
  integer, parameter :: DISKII_PHASE2_ADDR = 0xC0E2
  integer, parameter :: DISKII_PHASE3_ADDR = 0xC0E3
  integer, parameter :: DISKII_Q6L_ADDR = 0xC0EC
  integer, parameter :: DISKII_Q7L_ADDR = 0xC0ED
  integer, parameter :: DISKII_Q6R_ADDR = 0xC0EE
  integer, parameter :: DISKII_Q7R_ADDR = 0xC0EF
  ! Video Display Generator
  integer, parameter :: VIDEO_BASE = 0xC050
  integer, parameter :: VIDEO_TXTCLR_ADDR = 0xC050
  integer, parameter :: VIDEO_MIXCLR_ADDR = 0xC051
  integer, parameter :: VIDEO_TXTPAGE2_ADDR = 0xC054
  integer, parameter :: VIDEO_TXTPAGE1_ADDR = 0xC055
  integer, parameter :: VIDEO_LORES_ADDR = 0xC056
  integer, parameter :: VIDEO_HIRES_ADDR = 0xC057
  integer, parameter :: VIDEO_DHIRESON_ADDR = 0xC05E
  integer, parameter :: VIDEO_AN0_ADDR = 0xC058
  integer, parameter :: VIDEO_AN1_ADDR = 0xC059
  integer, parameter :: VIDEO_AN2_ADDR = 0xC05A
  integer, parameter :: VIDEO_AN3_ADDR = 0xC05B
  integer, parameter :: VIDEO__80STORE_ADDR = 0xC000
  ! RAM Read/Write Control
  integer, parameter :: RAMRD_BASE = 0xC080
  integer, parameter :: RAMRD_INTCXROM_ADDR = 0xCFFF

  ! 中断向量定义
  integer, parameter :: INT_RESET = 0  ! Power-on Reset
  integer, parameter :: INT_NMI = 1  ! Non-Maskable Interrupt (from VIA)
  integer, parameter :: INT_IRQ = 2  ! IRQ from VIA/timer/slot
  integer, parameter :: INT_BRK = 3  ! BRK Instruction

  ! 引脚定义
  integer, parameter :: PIN_VCC = 1  ! +5V Power
  integer, parameter :: PIN_GND = 2  ! Ground
  integer, parameter :: PIN_RESET = 3  ! System Reset
  integer, parameter :: PIN_CLK = 4  ! System Clock (1.023MHz NTSC)
  integer, parameter :: PIN_RDY = 5  ! CPU Ready
  integer, parameter :: PIN_NMI = 6  ! Non-Maskable Interrupt
  integer, parameter :: PIN_IRQ = 7  ! Interrupt Request
  integer, parameter :: PIN_SO = 8  ! Set Overflow
  integer, parameter :: PIN_RWB = 9  ! Read/Write Bar
  integer, parameter :: PIN_SYNC = 10  ! Instruction Sync
  integer, parameter :: PIN_A0_A15 = 11  ! Address Bus (16-bit)
  integer, parameter :: PIN_D0_D7 = 12  ! Data Bus (8-bit)
  integer, parameter :: PIN_PHASE0 = 13  ! Phase 0 (4MHz system)
  integer, parameter :: PIN_PHASE1 = 14  ! Phase 1
  integer, parameter :: PIN_PHASE2 = 15  ! Phase 2
  integer, parameter :: PIN_PHASE3 = 16  ! Phase 3

end module apple_iie_device

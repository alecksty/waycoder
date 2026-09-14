! Apple-II 设备定义 - Fortran 模块
! 生成自: Apple Computer/Apple II/Apple-II
! 版本: 1.0
! 日期: 2026-04-17
! 作者: VML Team
! 描述: Apple II personal computer with MOS 6502 CPU, 48KB RAM, and color graphics
! CPU架构: MOS 6502
! 位宽: 8位
! 时钟频率: 1023000 Hz

module apple_ii_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: A_ADDR = 0  ! Accumulator
  integer, parameter :: X_ADDR = 0  ! Index Register X
  integer, parameter :: Y_ADDR = 0  ! Index Register Y
  integer, parameter :: SP_ADDR = 0  ! Stack Pointer
  integer, parameter :: PC_ADDR = 0  ! Program Counter
  integer, parameter :: P_ADDR = 0  ! Status Register

  ! 外设定义
  ! Apple II keyboard
  integer, parameter :: KEYBOARD_BASE = 
  integer, parameter :: KEYBOARD_KBD_ADDR = 0xC000
  integer, parameter :: KEYBOARD_KBDSTRB_ADDR = 0xC010
  ! Built-in speaker
  integer, parameter :: SPEAKER_BASE = 
  integer, parameter :: SPEAKER_SPKR_ADDR = 0xC030
  ! Cassette tape interface
  integer, parameter :: CASSETTE_BASE = 
  integer, parameter :: CASSETTE_TAPEIN_ADDR = 0xC060
  integer, parameter :: CASSETTE_TAPEOUT_ADDR = 0xC020
  ! Game controller port
  integer, parameter :: GAMEPORT_BASE = 
  integer, parameter :: GAMEPORT_PADDLE0_ADDR = 0xC064
  integer, parameter :: GAMEPORT_PADDLE1_ADDR = 0xC065
  integer, parameter :: GAMEPORT_PADDLE2_ADDR = 0xC066
  integer, parameter :: GAMEPORT_PADDLE3_ADDR = 0xC067
  integer, parameter :: GAMEPORT_BUTTON0_ADDR = 0xC061
  integer, parameter :: GAMEPORT_BUTTON1_ADDR = 0xC062
  ! Disk II controller
  integer, parameter :: DISKCONTROLLER_BASE = 
  integer, parameter :: DISKCONTROLLER_DISKUNIT_ADDR = 0xC0E0
  integer, parameter :: DISKCONTROLLER_DISKCMD_ADDR = 0xC0E8
  integer, parameter :: DISKCONTROLLER_DISKSTAT_ADDR = 0xC0E9
  integer, parameter :: DISKCONTROLLER_DISKDATA_ADDR = 0xC0EA

  ! 中断向量定义
  integer, parameter :: INT_NMI = 65526  ! Non-maskable interrupt
  integer, parameter :: INT_RESET = 65528  ! Reset vector
  integer, parameter :: INT_IRQ = 65530  ! Interrupt request
  integer, parameter :: INT_BRK = 65532  ! Break instruction

end module apple_ii_device

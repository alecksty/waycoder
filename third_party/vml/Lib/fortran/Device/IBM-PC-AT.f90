! IBM PC/AT 设备定义 - Fortran 模块
! 生成自: IBM/IBM PC/IBM PC/AT
! 版本: 
! 日期: 
! 作者: 
! 描述: IBM Personal Computer/Advanced Technology (Model 5170)
! CPU架构: x86-16
! 位宽: 0位
! 时钟频率: 0 Hz

module ibm pc/at_device
  implicit none

  ! 外设定义
  ! Programmable Interrupt Controller
  integer, parameter :: _8259A_BASE = 
  integer, parameter :: _8259A_ICW1_ADDR = 0x20
  integer, parameter :: _8259A_ICW2_ADDR = 0x21
  integer, parameter :: _8259A_ICW3_ADDR = 0x21
  integer, parameter :: _8259A_ICW4_ADDR = 0x21
  integer, parameter :: _8259A_OCW1_ADDR = 0x21
  integer, parameter :: _8259A_OCW2_ADDR = 0x20
  integer, parameter :: _8259A_OCW3_ADDR = 0x20
  ! Programmable Interval Timer
  integer, parameter :: _8253_BASE = 
  integer, parameter :: _8253_COUNTER0_ADDR = 0x40
  integer, parameter :: _8253_COUNTER1_ADDR = 0x41
  integer, parameter :: _8253_COUNTER2_ADDR = 0x42
  integer, parameter :: _8253_CONTROL_ADDR = 0x43
  ! Direct Memory Access Controller
  integer, parameter :: _8237_BASE = 
  integer, parameter :: _8237_CHANNEL0_ADDR = 0x00
  integer, parameter :: _8237_CHANNEL1_ADDR = 0x02
  integer, parameter :: _8237_CHANNEL2_ADDR = 0x04
  integer, parameter :: _8237_CHANNEL3_ADDR = 0x06
  integer, parameter :: _8237_STATUS_ADDR = 0x08
  integer, parameter :: _8237_COMMAND_ADDR = 0x08
  integer, parameter :: _8237_REQUEST_ADDR = 0x09
  integer, parameter :: _8237_MASK_ADDR = 0x0A
  integer, parameter :: _8237_MODE_ADDR = 0x0B
  ! Keyboard Controller
  integer, parameter :: _8042_BASE = 
  integer, parameter :: _8042_DATA_ADDR = 0x60
  integer, parameter :: _8042_STATUS_ADDR = 0x64
  ! Real-Time Clock with CMOS RAM
  integer, parameter :: CMOS_BASE = 
  integer, parameter :: CMOS_ADDRESS_ADDR = 0x70
  integer, parameter :: CMOS_DATA_ADDR = 0x71
  ! Floppy Disk Controller
  integer, parameter :: FDC_BASE = 
  integer, parameter :: FDC_SRA_ADDR = 0x3F2
  integer, parameter :: FDC_MSR_ADDR = 0x3F4
  integer, parameter :: FDC_DATA_ADDR = 0x3F5
  integer, parameter :: FDC_DIR_ADDR = 0x3F7
  integer, parameter :: FDC_CCR_ADDR = 0x3F7
  ! Hard Disk Controller (ST-506/412)
  integer, parameter :: HDC_BASE = 
  integer, parameter :: HDC_DATA_ADDR = 0x1F0
  integer, parameter :: HDC_ERROR_ADDR = 0x1F1
  integer, parameter :: HDC_SECTORCOUNT_ADDR = 0x1F2
  integer, parameter :: HDC_SECTORNUMBER_ADDR = 0x1F3
  integer, parameter :: HDC_CYLINDERLOW_ADDR = 0x1F4
  integer, parameter :: HDC_CYLINDERHIGH_ADDR = 0x1F5
  integer, parameter :: HDC_DRIVEHEAD_ADDR = 0x1F6
  integer, parameter :: HDC_STATUS_ADDR = 0x1F7
  integer, parameter :: HDC_COMMAND_ADDR = 0x1F7
  ! Color Graphics Adapter
  integer, parameter :: CGA_BASE = 
  integer, parameter :: CGA_CRTC_INDEX_ADDR = 0x3D4
  integer, parameter :: CGA_CRTC_DATA_ADDR = 0x3D5
  integer, parameter :: CGA_MODECONTROL_ADDR = 0x3D8
  integer, parameter :: CGA_COLORSELECT_ADDR = 0x3D9
  integer, parameter :: CGA_STATUS_ADDR = 0x3DA
  ! Enhanced Graphics Adapter
  integer, parameter :: EGA_BASE = 
  integer, parameter :: EGA_CRTC_INDEX_ADDR = 0x3D4
  integer, parameter :: EGA_CRTC_DATA_ADDR = 0x3D5
  integer, parameter :: EGA_FEATURECONTROL_ADDR = 0x3DA
  integer, parameter :: EGA_GRAPHICS1POS_ADDR = 0x3CC
  integer, parameter :: EGA_GRAPHICS2POS_ADDR = 0x3CA
  integer, parameter :: EGA_SEQUENCERINDEX_ADDR = 0x3C4
  integer, parameter :: EGA_SEQUENCERDATA_ADDR = 0x3C5
  integer, parameter :: EGA_GRAPHICSINDEX_ADDR = 0x3CE
  integer, parameter :: EGA_GRAPHICSDATA_ADDR = 0x3CF
  integer, parameter :: EGA_ATTRIBUTEINDEX_ADDR = 0x3C0
  integer, parameter :: EGA_ATTRIBUTEDATA_ADDR = 0x3C1
  ! Video Graphics Array
  integer, parameter :: VGA_BASE = 
  integer, parameter :: VGA_CRTC_INDEX_ADDR = 0x3D4
  integer, parameter :: VGA_CRTC_DATA_ADDR = 0x3D5
  integer, parameter :: VGA_INPUTSTATUS1_ADDR = 0x3DA
  integer, parameter :: VGA_FEATURECONTROL_ADDR = 0x3DA
  integer, parameter :: VGA_MISCOUTPUT_ADDR = 0x3C2
  integer, parameter :: VGA_SEQUENCERINDEX_ADDR = 0x3C4
  integer, parameter :: VGA_SEQUENCERDATA_ADDR = 0x3C5
  integer, parameter :: VGA_GRAPHICSINDEX_ADDR = 0x3CE
  integer, parameter :: VGA_GRAPHICSDATA_ADDR = 0x3CF
  integer, parameter :: VGA_ATTRIBUTEINDEX_ADDR = 0x3C0
  integer, parameter :: VGA_ATTRIBUTEDATA_ADDR = 0x3C1
  integer, parameter :: VGA_DACMASK_ADDR = 0x3C6
  integer, parameter :: VGA_DACREADINDEX_ADDR = 0x3C7
  integer, parameter :: VGA_DACWRITEINDEX_ADDR = 0x3C8
  integer, parameter :: VGA_DACDATA_ADDR = 0x3C9
  ! Game Port
  integer, parameter :: GAMEPORT_BASE = 
  integer, parameter :: GAMEPORT_DATA_ADDR = 0x201
  ! Parallel Printer Port
  integer, parameter :: PARALLELPORT_BASE = 
  integer, parameter :: PARALLELPORT_DATA_ADDR = 0x378
  integer, parameter :: PARALLELPORT_STATUS_ADDR = 0x379
  integer, parameter :: PARALLELPORT_CONTROL_ADDR = 0x37A
  ! Serial Communications Port
  integer, parameter :: SERIALPORT_BASE = 
  integer, parameter :: SERIALPORT_DATA_ADDR = 0x3F8
  integer, parameter :: SERIALPORT_IER_ADDR = 0x3F9
  integer, parameter :: SERIALPORT_IIR_ADDR = 0x3FA
  integer, parameter :: SERIALPORT_LCR_ADDR = 0x3FB
  integer, parameter :: SERIALPORT_MCR_ADDR = 0x3FC
  integer, parameter :: SERIALPORT_LSR_ADDR = 0x3FD
  integer, parameter :: SERIALPORT_MSR_ADDR = 0x3FE
  integer, parameter :: SERIALPORT_SCR_ADDR = 0x3FF
  ! PC Speaker
  integer, parameter :: SPEAKER_BASE = 
  integer, parameter :: SPEAKER_CONTROL_ADDR = 0x61

  ! 中断向量定义
  integer, parameter :: INT_DIVIDE_ERROR = 0  ! Division by zero
  integer, parameter :: INT_SINGLE_STEP = 1  ! Debug single step
  integer, parameter :: INT_NMI = 2  ! Non-maskable interrupt
  integer, parameter :: INT_BREAKPOINT = 3  ! INT 3 instruction
  integer, parameter :: INT_OVERFLOW = 4  ! INTO instruction
  integer, parameter :: INT_PRINT_SCREEN = 5  ! Print screen key
  integer, parameter :: INT_IRQ0 = 8  ! Timer interrupt
  integer, parameter :: INT_IRQ1 = 9  ! Keyboard interrupt
  integer, parameter :: INT_IRQ2 = 10  ! Cascade to IRQ8-15
  integer, parameter :: INT_IRQ3 = 11  ! COM2 interrupt
  integer, parameter :: INT_IRQ4 = 12  ! COM1 interrupt
  integer, parameter :: INT_IRQ5 = 13  ! LPT2 interrupt
  integer, parameter :: INT_IRQ6 = 14  ! Floppy disk interrupt
  integer, parameter :: INT_IRQ7 = 15  ! LPT1 interrupt
  integer, parameter :: INT_IRQ8 = 112  ! Real-time clock interrupt
  integer, parameter :: INT_IRQ9 = 113  ! Redirected IRQ2
  integer, parameter :: INT_IRQ10 = 114  ! Reserved
  integer, parameter :: INT_IRQ11 = 115  ! Reserved
  integer, parameter :: INT_IRQ12 = 116  ! PS/2 mouse interrupt
  integer, parameter :: INT_IRQ13 = 117  ! Coprocessor interrupt
  integer, parameter :: INT_IRQ14 = 118  ! Primary IDE interrupt
  integer, parameter :: INT_IRQ15 = 119  ! Secondary IDE interrupt
  integer, parameter :: INT_VIDEO_SERVICES = 16  ! Video BIOS services
  integer, parameter :: INT_DISK_SERVICES = 19  ! Disk BIOS services
  integer, parameter :: INT_DOS_SERVICES = 21  ! DOS function calls

end module ibm pc/at_device

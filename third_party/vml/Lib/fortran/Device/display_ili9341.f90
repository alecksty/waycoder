! ILI9341 设备定义 - Fortran 模块
! 生成自: Ilitek/Display/ILI9341
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: ILI9341 2.8" 240x320 TFT LCD Display (SPI, 18-bit color, touch)
! CPU架构: Display
! 位宽: 18位
! 时钟频率: 20000000 Hz

module ili9341_device
  implicit none

  ! 内存段定义
  integer, parameter :: GRAM_START = 0x00
  integer, parameter :: GRAM_END = 0xBCFF
  integer, parameter :: GRAM_SIZE = 156672  ! Graphics RAM (240x320x18bit)

  ! 外设定义
  ! ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)
  integer, parameter :: ILI9341_BASE = 0x00
  integer, parameter :: ILI9341_CMD_ADDR = 0x00
  integer, parameter :: ILI9341_DATA_ADDR = 0x01
  integer, parameter :: ILI9341_COL_START_ADDR = 0x2A
  integer, parameter :: ILI9341_PAGE_START_ADDR = 0x2B
  integer, parameter :: ILI9341_WRITE_RAM_ADDR = 0x2C
  integer, parameter :: ILI9341_MADCTL_ADDR = 0x36
  integer, parameter :: ILI9341_PIXFMT_ADDR = 0x3A
  integer, parameter :: ILI9341_FRMCTL_ADDR = 0xB1
  integer, parameter :: ILI9341_GAMMA_ADDR = 0x26
  integer, parameter :: ILI9341_SLEEP_OUT_ADDR = 0x11
  integer, parameter :: ILI9341_DISP_ON_ADDR = 0x29

end module ili9341_device

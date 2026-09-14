! MCP23017 设备定义 - Fortran 模块
! 生成自: Microchip/GPIO/MCP23017
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)
! CPU架构: GPIO
! 位宽: 16位
! 时钟频率: 400000 Hz

module mcp23017_device
  implicit none

  ! 外设定义
  ! MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
  integer, parameter :: MCP23017_BASE = 0x20
  integer, parameter :: MCP23017_IODIRA_ADDR = 0x00
  integer, parameter :: MCP23017_IODIRB_ADDR = 0x01
  integer, parameter :: MCP23017_GPIOA_ADDR = 0x12
  integer, parameter :: MCP23017_GPIOB_ADDR = 0x13
  integer, parameter :: MCP23017_GPINTENA_ADDR = 0x04
  integer, parameter :: MCP23017_GPINTENB_ADDR = 0x05
  integer, parameter :: MCP23017_INTCONA_ADDR = 0x08
  integer, parameter :: MCP23017_IOCON_ADDR = 0x0A
  integer, parameter :: MCP23017_GPPUA_ADDR = 0x0C
  integer, parameter :: MCP23017_GPPUB_ADDR = 0x0D

  ! 中断向量定义
  integer, parameter :: INT_INTA = 0  ! Port A interrupt
  integer, parameter :: INT_INTB = 1  ! Port B interrupt

end module mcp23017_device

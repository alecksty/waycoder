! DS18B20 设备定义 - Fortran 模块
! 生成自: Maxim/Dallas/Sensor/DS18B20
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: Programmable Resolution 1-Wire Digital Thermometer
! CPU架构: Sensor
! 位宽: 8位
! 时钟频率: 100000 Hz

module ds18b20_device
  implicit none

  ! 内存段定义
  integer, parameter :: SCRATCHPAD_START = 0x00
  integer, parameter :: SCRATCHPAD_END = 0x08
  integer, parameter :: SCRATCHPAD_SIZE = 9  ! Scratchpad memory (9 bytes)
  integer, parameter :: EEPROM_START = 0x00
  integer, parameter :: EEPROM_END = 0x02
  integer, parameter :: EEPROM_SIZE = 3  ! EEPROM (TH, TL, config bytes)

  ! 外设定义
  ! DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92)
  integer, parameter :: DS18B20_BASE = 0x00
  integer, parameter :: DS18B20_TEMP_LSB_ADDR = 0x00
  integer, parameter :: DS18B20_TEMP_MSB_ADDR = 0x01
  integer, parameter :: DS18B20_TH_REG_ADDR = 0x02
  integer, parameter :: DS18B20_TL_REG_ADDR = 0x03
  integer, parameter :: DS18B20_CONFIG_ADDR = 0x04
  integer, parameter :: DS18B20_CONFIG_R0_BIT = 5  ! Resolution select bit 0
  integer, parameter :: DS18B20_CONFIG_R1_BIT = 6  ! Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit)
  integer, parameter :: DS18B20_COUNT_REMAIN_ADDR = 0x06
  integer, parameter :: DS18B20_COUNT_PER_C_ADDR = 0x07
  integer, parameter :: DS18B20_CRC_ADDR = 0x08

end module ds18b20_device

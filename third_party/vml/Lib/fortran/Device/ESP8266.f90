! ESP8266 设备定义 - Fortran 模块
! 生成自: Espressif Systems/ESP8266/ESP8266
! 版本: 
! 日期: 
! 作者: 
! 描述: Espressif ESP8266 Wi-Fi SoC with integrated TCP/IP stack
! CPU架构: Xtensa LX106
! 位宽: 0位
! 时钟频率: 0 Hz

module esp8266_device
  implicit none

  ! 外设定义
  ! Wi-Fi 802.11 b/g/n
  integer, parameter :: WIFI_BASE = 
  integer, parameter :: WIFI_WIFI_MAC_ADDR = 0x60000800
  integer, parameter :: WIFI_WIFI_MODE_ADDR = 0x60000804
  integer, parameter :: WIFI_WIFI_CHANNEL_ADDR = 0x60000808
  integer, parameter :: WIFI_WIFI_RATE_ADDR = 0x6000080C
  ! Universal Asynchronous Receiver/Transmitter 0
  integer, parameter :: UART0_BASE = 
  integer, parameter :: UART0_UART0_FIFO_ADDR = 0x60000000
  integer, parameter :: UART0_UART0_INT_RAW_ADDR = 0x60000004
  integer, parameter :: UART0_UART0_INT_ST_ADDR = 0x60000008
  integer, parameter :: UART0_UART0_INT_ENA_ADDR = 0x6000000C
  integer, parameter :: UART0_UART0_INT_CLR_ADDR = 0x60000010
  integer, parameter :: UART0_UART0_CLKDIV_ADDR = 0x60000014
  integer, parameter :: UART0_UART0_AUTOBAUD_ADDR = 0x60000018
  integer, parameter :: UART0_UART0_STATUS_ADDR = 0x6000001C
  integer, parameter :: UART0_UART0_CONF0_ADDR = 0x60000020
  integer, parameter :: UART0_UART0_CONF1_ADDR = 0x60000024
  integer, parameter :: UART0_UART0_LOWPULSE_ADDR = 0x60000028
  integer, parameter :: UART0_UART0_HIGHPULSE_ADDR = 0x6000002C
  integer, parameter :: UART0_UART0_RXD_CNT_ADDR = 0x60000030
  ! Serial Peripheral Interface
  integer, parameter :: SPI_BASE = 
  integer, parameter :: SPI_SPI_CMD_ADDR = 0x60000200
  integer, parameter :: SPI_SPI_ADDR_ADDR = 0x60000204
  integer, parameter :: SPI_SPI_CTRL_ADDR = 0x60000208
  integer, parameter :: SPI_SPI_RD_STATUS_ADDR = 0x6000020C
  integer, parameter :: SPI_SPI_CTRL2_ADDR = 0x60000210
  integer, parameter :: SPI_SPI_CLOCK_ADDR = 0x60000214
  integer, parameter :: SPI_SPI_USER_ADDR = 0x60000218
  integer, parameter :: SPI_SPI_USER1_ADDR = 0x6000021C
  integer, parameter :: SPI_SPI_USER2_ADDR = 0x60000220
  integer, parameter :: SPI_SPI_W0_ADDR = 0x60000280
  ! Inter-Integrated Circuit
  integer, parameter :: I2C_BASE = 
  integer, parameter :: I2C_I2C_SCL_LOW_ADDR = 0x60000C00
  integer, parameter :: I2C_I2C_SCL_HIGH_ADDR = 0x60000C04
  integer, parameter :: I2C_I2C_SDA_HOLD_ADDR = 0x60000C08
  integer, parameter :: I2C_I2C_SCL_START_HOLD_ADDR = 0x60000C0C
  integer, parameter :: I2C_I2C_SCL_STOP_HOLD_ADDR = 0x60000C10
  integer, parameter :: I2C_I2C_INT_RAW_ADDR = 0x60000C14
  integer, parameter :: I2C_I2C_INT_ST_ADDR = 0x60000C18
  integer, parameter :: I2C_I2C_INT_ENA_ADDR = 0x60000C1C
  integer, parameter :: I2C_I2C_INT_CLR_ADDR = 0x60000C20
  integer, parameter :: I2C_I2C_CMD_ADDR = 0x60000C24
  integer, parameter :: I2C_I2C_FIFO_DATA_ADDR = 0x60000C28
  integer, parameter :: I2C_I2C_FIFO_CNT_ADDR = 0x60000C2C
  ! General Purpose I/O
  integer, parameter :: GPIO_BASE = 
  integer, parameter :: GPIO_GPIO_OUT_ADDR = 0x60000300
  integer, parameter :: GPIO_GPIO_OUT_W1TS_ADDR = 0x60000304
  integer, parameter :: GPIO_GPIO_OUT_W1TC_ADDR = 0x60000308
  integer, parameter :: GPIO_GPIO_ENABLE_ADDR = 0x6000030C
  integer, parameter :: GPIO_GPIO_ENABLE_W1TS_ADDR = 0x60000310
  integer, parameter :: GPIO_GPIO_ENABLE_W1TC_ADDR = 0x60000314
  integer, parameter :: GPIO_GPIO_IN_ADDR = 0x60000318
  integer, parameter :: GPIO_GPIO_STATUS_ADDR = 0x6000031C
  integer, parameter :: GPIO_GPIO_STATUS_W1TS_ADDR = 0x60000320
  integer, parameter :: GPIO_GPIO_STATUS_W1TC_ADDR = 0x60000324
  integer, parameter :: GPIO_GPIO_PIN_ADDR = 0x60000328
  ! Hardware Timer
  integer, parameter :: TIMER_BASE = 
  integer, parameter :: TIMER_TIMER_LOAD_ADDR = 0x60000600
  integer, parameter :: TIMER_TIMER_COUNT_ADDR = 0x60000604
  integer, parameter :: TIMER_TIMER_CTRL_ADDR = 0x60000608
  integer, parameter :: TIMER_TIMER_INT_ADDR = 0x6000060C
  integer, parameter :: TIMER_TIMER_ALARM_ADDR = 0x60000610
  ! Analog-to-Digital Converter
  integer, parameter :: ADC_BASE = 
  integer, parameter :: ADC_ADC_CTRL_ADDR = 0x60000E00
  integer, parameter :: ADC_ADC_DATA_ADDR = 0x60000E04
  ! Pulse Width Modulation
  integer, parameter :: PWM_BASE = 
  integer, parameter :: PWM_PWM_CTRL_ADDR = 0x60000F00
  integer, parameter :: PWM_PWM_PERIOD_ADDR = 0x60000F04
  integer, parameter :: PWM_PWM_DUTY_ADDR = 0x60000F08

  ! 中断向量定义
  integer, parameter :: INT_NMI = 1  ! Non-maskable interrupt
  integer, parameter :: INT_LEVEL1 = 3  ! Level 1 interrupt
  integer, parameter :: INT_LEVEL2 = 4  ! Level 2 interrupt
  integer, parameter :: INT_LEVEL3 = 5  ! Level 3 interrupt
  integer, parameter :: INT_LEVEL4 = 6  ! Level 4 interrupt
  integer, parameter :: INT_LEVEL5 = 7  ! Level 5 interrupt
  integer, parameter :: INT_TIMER0 = 8  ! Timer 0 interrupt
  integer, parameter :: INT_TIMER1 = 9  ! Timer 1 interrupt
  integer, parameter :: INT_UART0 = 10  ! UART0 interrupt
  integer, parameter :: INT_UART1 = 11  ! UART1 interrupt
  integer, parameter :: INT_GPIO = 12  ! GPIO interrupt
  integer, parameter :: INT_PWM = 13  ! PWM interrupt
  integer, parameter :: INT_I2C = 14  ! I2C interrupt
  integer, parameter :: INT_SPI = 15  ! SPI interrupt
  integer, parameter :: INT_ADC = 16  ! ADC interrupt
  integer, parameter :: INT_WIFI = 17  ! Wi-Fi interrupt
  integer, parameter :: INT_RTC = 18  ! RTC interrupt

end module esp8266_device

! ESP32-WROOM-32 设备定义 - Fortran 模块
! 生成自: Espressif/ESP32/ESP32-WROOM-32
! 版本: 1.0
! 日期: 2026-04-16
! 作者: VML Team
! 描述: Dual-core Xtensa LX6 Wi-Fi and Bluetooth/BLE SoC with 4MB Flash
! CPU架构: Xtensa-LX6
! 位宽: 32位
! 时钟频率: 160000000 Hz

module esp32_wroom_32_device
  implicit none

  ! 寄存器地址定义
  integer, parameter :: PC_ADDR = 0x00000000  ! Program Counter
  integer, parameter :: A0_ADDR = 0x00000004  ! General Purpose Register 0
  integer, parameter :: A1_ADDR = 0x00000008  ! Stack Pointer
  integer, parameter :: A2_ADDR = 0x0000000C  ! General Purpose Register 2
  integer, parameter :: A3_ADDR = 0x00000010  ! General Purpose Register 3
  integer, parameter :: A4_ADDR = 0x00000014  ! General Purpose Register 4
  integer, parameter :: A5_ADDR = 0x00000018  ! General Purpose Register 5
  integer, parameter :: A6_ADDR = 0x0000001C  ! General Purpose Register 6
  integer, parameter :: A7_ADDR = 0x00000020  ! General Purpose Register 7
  integer, parameter :: A8_ADDR = 0x00000024  ! General Purpose Register 8
  integer, parameter :: A9_ADDR = 0x00000028  ! General Purpose Register 9
  integer, parameter :: A10_ADDR = 0x0000002C  ! General Purpose Register 10
  integer, parameter :: A11_ADDR = 0x00000030  ! General Purpose Register 11
  integer, parameter :: A12_ADDR = 0x00000034  ! General Purpose Register 12
  integer, parameter :: A13_ADDR = 0x00000038  ! General Purpose Register 13
  integer, parameter :: A14_ADDR = 0x0000003C  ! General Purpose Register 14
  integer, parameter :: A15_ADDR = 0x00000040  ! General Purpose Register 15
  integer, parameter :: SAREG1_ADDR = 0x00000044  ! Special Address Register 1
  integer, parameter :: SAREG2_ADDR = 0x00000048  ! Special Address Register 2
  integer, parameter :: LBEG_ADDR = 0x0000004C  ! Loop Beginning
  integer, parameter :: LEND_ADDR = 0x00000050  ! Loop End
  integer, parameter :: LCOUNT_ADDR = 0x00000054  ! Loop Counter
  integer, parameter :: PS_ADDR = 0x00000058  ! Processor Status
  integer, parameter :: WINDOWBASE_ADDR = 0x0000005C  ! Window Base
  integer, parameter :: WINDOWSTART_ADDR = 0x00000060  ! Window Start
  integer, parameter :: PTEBASE_ADDR = 0x00000064  ! Page Table Base
  integer, parameter :: PTESIZE_ADDR = 0x00000068  ! Page Table Entry Size
  integer, parameter :: SCOMPARE1_ADDR = 0x0000006C  ! Special Compare 1
  integer, parameter :: ATOMCTL_ADDR = 0x00000070  ! Atomic Operation Control
  integer, parameter :: DDR_ADDR = 0x00000074  ! Data Destination Register

  ! 内存段定义
  integer, parameter :: ROM_START = 0x40000000
  integer, parameter :: ROM_END = 0x4003FFFF
  integer, parameter :: ROM_SIZE = 262144  ! ROM (224KB)
  integer, parameter :: SRAM_START = 0x3FF00000
  integer, parameter :: SRAM_END = 0x3FF7FFFF
  integer, parameter :: SRAM_SIZE = 524288  ! SRAM (320KB total)
  integer, parameter :: DRAM0_START = 0x3FF80000
  integer, parameter :: DRAM0_END = 0x3FF9FFFF
  integer, parameter :: DRAM0_SIZE = 131072  ! DRAM0 (128KB)
  integer, parameter :: IRAM0_START = 0x40000000
  integer, parameter :: IRAM0_END = 0x401FFFFF
  integer, parameter :: IRAM0_SIZE = 2097152  ! IRAM0
  integer, parameter :: FLASH_START = 0x40200000
  integer, parameter :: FLASH_END = 0x405FFFFF
  integer, parameter :: FLASH_SIZE = 4194304  ! External Flash (4MB)
  integer, parameter :: PERIPHERAL_START = 0x3FF00000
  integer, parameter :: PERIPHERAL_END = 0x3FFBFFFF
  integer, parameter :: PERIPHERAL_SIZE = 786432  ! Peripheral Registers
  integer, parameter :: GPIO_START = 0x3FF44000
  integer, parameter :: GPIO_END = 0x3FF44FFF
  integer, parameter :: GPIO_SIZE = 4096  ! GPIO

  ! 外设定义
  ! GPIO
  integer, parameter :: GPIO_BASE = 0x3FF44000
  integer, parameter :: GPIO_OUT_ADDR = 0x0000
  integer, parameter :: GPIO_OUT_W1TS_ADDR = 0x0008
  integer, parameter :: GPIO_OUT_W1TC_ADDR = 0x000C
  integer, parameter :: GPIO_IN_ADDR = 0x003C
  integer, parameter :: GPIO_STATUS_ADDR = 0x0024
  integer, parameter :: GPIO_STATUS_W1TS_ADDR = 0x0028
  integer, parameter :: GPIO_STATUS_W1TC_ADDR = 0x002C
  integer, parameter :: GPIO_PIN_ADDR = 0x0040
  integer, parameter :: GPIO_ENABLE_ADDR = 0x0020
  integer, parameter :: GPIO_STRAP_ADDR = 0x0038
  integer, parameter :: GPIO_IN_NEXT_ADDR = 0x0044
  ! RTC GPIO
  integer, parameter :: RTC_GPIO_BASE = 0x3FF48000
  integer, parameter :: RTC_GPIO_OUT_ADDR = 0x0000
  integer, parameter :: RTC_GPIO_OUT_W1TS_ADDR = 0x0008
  integer, parameter :: RTC_GPIO_OUT_W1TC_ADDR = 0x000C
  integer, parameter :: RTC_GPIO_IN_ADDR = 0x0044
  integer, parameter :: RTC_GPIO_STATUS_ADDR = 0x0024
  integer, parameter :: RTC_GPIO_PIN_ADDR = 0x0048
  integer, parameter :: RTC_GPIO_ENABLE_ADDR = 0x0020
  ! IO MUX
  integer, parameter :: IO_MUX_BASE = 0x3FF49000
  integer, parameter :: IO_MUX_GPIO0_ADDR = 0x0000
  integer, parameter :: IO_MUX_GPIO1_ADDR = 0x0004
  integer, parameter :: IO_MUX_GPIO2_ADDR = 0x0008
  integer, parameter :: IO_MUX_GPIO3_ADDR = 0x000C
  integer, parameter :: IO_MUX_GPIO4_ADDR = 0x0010
  integer, parameter :: IO_MUX_GPIO5_ADDR = 0x0014
  integer, parameter :: IO_MUX_GPIO6_ADDR = 0x0018
  integer, parameter :: IO_MUX_GPIO7_ADDR = 0x001C
  integer, parameter :: IO_MUX_GPIO8_ADDR = 0x0020
  integer, parameter :: IO_MUX_GPIO9_ADDR = 0x0024
  integer, parameter :: IO_MUX_GPIO10_ADDR = 0x0028
  integer, parameter :: IO_MUX_GPIO11_ADDR = 0x002C
  integer, parameter :: IO_MUX_GPIO12_ADDR = 0x0030
  integer, parameter :: IO_MUX_GPIO13_ADDR = 0x0034
  integer, parameter :: IO_MUX_GPIO14_ADDR = 0x0038
  integer, parameter :: IO_MUX_GPIO15_ADDR = 0x003C
  integer, parameter :: IO_MUX_GPIO16_ADDR = 0x0040
  integer, parameter :: IO_MUX_GPIO17_ADDR = 0x0044
  integer, parameter :: IO_MUX_GPIO18_ADDR = 0x0048
  integer, parameter :: IO_MUX_GPIO19_ADDR = 0x004C
  integer, parameter :: IO_MUX_GPIO20_ADDR = 0x0050
  integer, parameter :: IO_MUX_GPIO21_ADDR = 0x0054
  integer, parameter :: IO_MUX_GPIO22_ADDR = 0x0058
  integer, parameter :: IO_MUX_GPIO23_ADDR = 0x005C
  integer, parameter :: IO_MUX_GPIO24_ADDR = 0x0060
  integer, parameter :: IO_MUX_GPIO25_ADDR = 0x0064
  integer, parameter :: IO_MUX_GPIO26_ADDR = 0x0068
  integer, parameter :: IO_MUX_GPIO27_ADDR = 0x006C
  ! UART 0
  integer, parameter :: UART0_BASE = 0x3FF40000
  integer, parameter :: UART0_FIFO_ADDR = 0x0000
  integer, parameter :: UART0_INT_RAW_ADDR = 0x0004
  integer, parameter :: UART0_INT_ST_ADDR = 0x0008
  integer, parameter :: UART0_INT_ENA_ADDR = 0x000C
  integer, parameter :: UART0_INT_CLR_ADDR = 0x0010
  integer, parameter :: UART0_CONF0_ADDR = 0x0020
  integer, parameter :: UART0_CONF1_ADDR = 0x0024
  integer, parameter :: UART0_LOWPULSE_ADDR = 0x0028
  integer, parameter :: UART0_HIGHPULSE_ADDR = 0x002C
  integer, parameter :: UART0_PULSE_CNT_ADDR = 0x0030
  integer, parameter :: UART0_DATE_ADDR = 0x0078
  integer, parameter :: UART0_AHB_BIT_ADDR = 0x007C
  ! UART 1
  integer, parameter :: UART1_BASE = 0x3FF50000
  integer, parameter :: UART1_FIFO_ADDR = 0x0000
  integer, parameter :: UART1_INT_RAW_ADDR = 0x0004
  integer, parameter :: UART1_INT_ST_ADDR = 0x0008
  integer, parameter :: UART1_INT_ENA_ADDR = 0x000C
  integer, parameter :: UART1_INT_CLR_ADDR = 0x0010
  integer, parameter :: UART1_CONF0_ADDR = 0x0020
  integer, parameter :: UART1_CONF1_ADDR = 0x0024
  ! UART 2
  integer, parameter :: UART2_BASE = 0x3FF6E000
  integer, parameter :: UART2_FIFO_ADDR = 0x0000
  integer, parameter :: UART2_INT_RAW_ADDR = 0x0004
  integer, parameter :: UART2_INT_ST_ADDR = 0x0008
  integer, parameter :: UART2_INT_ENA_ADDR = 0x000C
  integer, parameter :: UART2_INT_CLR_ADDR = 0x0010
  integer, parameter :: UART2_CONF0_ADDR = 0x0020
  integer, parameter :: UART2_CONF1_ADDR = 0x0024
  ! SPI0 (Flash)
  integer, parameter :: SPI0_BASE = 0x3FF42000
  integer, parameter :: SPI0_CMD_ADDR = 0x0000
  integer, parameter :: SPI0_ADDR_ADDR = 0x0004
  integer, parameter :: SPI0_CONTROL_ADDR = 0x0008
  integer, parameter :: SPI0_CONTROL1_ADDR = 0x000C
  integer, parameter :: SPI0_STATUS_ADDR = 0x0010
  integer, parameter :: SPI0_STATUS1_ADDR = 0x0014
  integer, parameter :: SPI0_DATA_ADDR = 0x0020
  integer, parameter :: SPI0_USER_ADDR = 0x003C
  integer, parameter :: SPI0_USER1_ADDR = 0x0040
  integer, parameter :: SPI0_USER2_ADDR = 0x0044
  integer, parameter :: SPI0_PIN_ADDR = 0x0048
  integer, parameter :: SPI0_SLAVE_ADDR = 0x004C
  integer, parameter :: SPI0_CACHE_FLASH_ADDR = 0x0050
  integer, parameter :: SPI0_CLOCK_ADDR = 0x0058
  integer, parameter :: SPI0_FIFO_ADDR = 0x0060
  ! SPI1
  integer, parameter :: SPI1_BASE = 0x3FF43000
  integer, parameter :: SPI1_CMD_ADDR = 0x0000
  integer, parameter :: SPI1_ADDR_ADDR = 0x0004
  integer, parameter :: SPI1_CONTROL_ADDR = 0x0008
  integer, parameter :: SPI1_STATUS_ADDR = 0x0010
  integer, parameter :: SPI1_DATA_ADDR = 0x0020
  integer, parameter :: SPI1_USER_ADDR = 0x003C
  integer, parameter :: SPI1_CLOCK_ADDR = 0x0058
  ! SPI2 (HSPI)
  integer, parameter :: SPI2_BASE = 0x3FF64000
  integer, parameter :: SPI2_CMD_ADDR = 0x0000
  integer, parameter :: SPI2_ADDR_ADDR = 0x0004
  integer, parameter :: SPI2_CONTROL_ADDR = 0x0008
  integer, parameter :: SPI2_STATUS_ADDR = 0x0010
  integer, parameter :: SPI2_DATA_ADDR = 0x0020
  integer, parameter :: SPI2_USER_ADDR = 0x003C
  integer, parameter :: SPI2_CLOCK_ADDR = 0x0058
  integer, parameter :: SPI2_FIFO_ADDR = 0x0060
  ! I2C 0
  integer, parameter :: I2C0_BASE = 0x3FF53000
  integer, parameter :: I2C0_SCL_START_ADDR = 0x0000
  integer, parameter :: I2C0_SCL_LOW_ADDR = 0x0004
  integer, parameter :: I2C0_SDA_START_ADDR = 0x0008
  integer, parameter :: I2C0_SDA_LOW_ADDR = 0x000C
  integer, parameter :: I2C0_INT_ENA_ADDR = 0x0010
  integer, parameter :: I2C0_INT_CLR_ADDR = 0x0014
  integer, parameter :: I2C0_INT_RAW_ADDR = 0x0018
  integer, parameter :: I2C0_INT_STATUS_ADDR = 0x001C
  integer, parameter :: I2C0_SCL_HIGH_PERIOD_ADDR = 0x0020
  integer, parameter :: I2C0_SCL_HIGH_PERIOD_S_ADDR = 0x0024
  integer, parameter :: I2C0_SCL_START_HOLD_ADDR = 0x0028
  integer, parameter :: I2C0_SDA_START_HOLD_ADDR = 0x002C
  integer, parameter :: I2C0_SCL_LAST_HOLD_ADDR = 0x0030
  integer, parameter :: I2C0_SCL_WAIT_PERIOD_ADDR = 0x0034
  integer, parameter :: I2C0_CTR_ADDR = 0x0050
  integer, parameter :: I2C0_STATUS_ADDR = 0x0054
  integer, parameter :: I2C0_FINISH_INT_ENA_ADDR = 0x0058
  integer, parameter :: I2C0_COMMAND0_ADDR = 0x0060
  integer, parameter :: I2C0_COMMAND1_ADDR = 0x0064
  integer, parameter :: I2C0_COMMAND2_ADDR = 0x0068
  integer, parameter :: I2C0_COMMAND3_ADDR = 0x006C
  integer, parameter :: I2C0_DATA_ADDR = 0x0080
  ! I2C 1
  integer, parameter :: I2C1_BASE = 0x3FF67000
  integer, parameter :: I2C1_CTR_ADDR = 0x0050
  integer, parameter :: I2C1_DATA_ADDR = 0x0080
  integer, parameter :: I2C1_COMMAND0_ADDR = 0x0060
  integer, parameter :: I2C1_COMMAND1_ADDR = 0x0064
  ! Timer Group 0
  integer, parameter :: TIMG0_BASE = 0x3FF5F000
  integer, parameter :: TIMG0_T0CONFIG_ADDR = 0x0000
  integer, parameter :: TIMG0_T0LO_ADDR = 0x0004
  integer, parameter :: TIMG0_T0HI_ADDR = 0x0008
  integer, parameter :: TIMG0_T0UPDATE_ADDR = 0x000C
  integer, parameter :: TIMG0_T0ALARM_ADDR = 0x0010
  integer, parameter :: TIMG0_T0LOAD_ADDR = 0x0014
  integer, parameter :: TIMG0_T0LOAD_REG_ADDR = 0x0018
  ! Timer Group 1
  integer, parameter :: TIMG1_BASE = 0x3FF60000
  integer, parameter :: TIMG1_T0CONFIG_ADDR = 0x0000
  integer, parameter :: TIMG1_T0LO_ADDR = 0x0004
  integer, parameter :: TIMG1_T0HI_ADDR = 0x0008
  integer, parameter :: TIMG1_T0ALARM_ADDR = 0x0010
  integer, parameter :: TIMG1_T0LOAD_ADDR = 0x0014
  ! Motor Control PWM 0
  integer, parameter :: PWM0_BASE = 0x3FF59000
  integer, parameter :: PWM0_CNT_ADDR = 0x0000
  integer, parameter :: PWM0_PERIOD_ADDR = 0x0004
  integer, parameter :: PWM0_DUTY_ADDR = 0x0008
  integer, parameter :: PWM0_CONFIG0_ADDR = 0x0010
  integer, parameter :: PWM0_CONFIG1_ADDR = 0x0014
  integer, parameter :: PWM0_CONFIG2_ADDR = 0x0018
  integer, parameter :: PWM0_UPDATE_ADDR = 0x0020
  ! Motor Control PWM 1
  integer, parameter :: PWM1_BASE = 0x3FF5A000
  integer, parameter :: PWM1_CNT_ADDR = 0x0000
  integer, parameter :: PWM1_PERIOD_ADDR = 0x0004
  integer, parameter :: PWM1_DUTY_ADDR = 0x0008
  ! LED PWM Controller
  integer, parameter :: LEDC_BASE = 0x3FF59000
  integer, parameter :: LEDC_CONFIG0_ADDR = 0x0000
  integer, parameter :: LEDC_HPOINT0_ADDR = 0x0018
  integer, parameter :: LEDC_DUTY0_ADDR = 0x001C
  integer, parameter :: LEDC_HPOINT1_ADDR = 0x0028
  integer, parameter :: LEDC_DUTY1_ADDR = 0x002C
  integer, parameter :: LEDC_HPOINT2_ADDR = 0x0038
  integer, parameter :: LEDC_DUTY2_ADDR = 0x003C
  integer, parameter :: LEDC_HPOINT3_ADDR = 0x0048
  integer, parameter :: LEDC_DUTY3_ADDR = 0x004C
  integer, parameter :: LEDC_TIMER0_CONF_ADDR = 0x0000
  integer, parameter :: LEDC_TIMER0_LOAD_ADDR = 0x0004
  ! RTC Controller
  integer, parameter :: RTC_BASE = 0x3FF48000
  integer, parameter :: RTC_RTC_CNTL_ADDR = 0x0000
  integer, parameter :: RTC_RTC_TIMER_ADDR = 0x000C
  integer, parameter :: RTC_RTC_UPDATE_ADDR = 0x0010
  integer, parameter :: RTC_RTC_STATE0_ADDR = 0x0080
  ! Wi-Fi
  integer, parameter :: WIFI_BASE = 0x3FFAE000
  integer, parameter :: WIFI_MAC_ADDR = 0x0000
  integer, parameter :: WIFI_CONFIG_ADDR = 0x0100
  ! Bluetooth/BLE
  integer, parameter :: BT_BASE = 0x3FFB0000
  integer, parameter :: BT_CONFIG_ADDR = 0x0000
  ! SHA Hardware Accelerator
  integer, parameter :: SHA_BASE = 0x3FF67000
  integer, parameter :: SHA_MODE_ADDR = 0x0000
  integer, parameter :: SHA_DATA_ADDR = 0x0004
  integer, parameter :: SHA_HASH_ADDR = 0x0008
  ! AES Hardware Accelerator
  integer, parameter :: AES_BASE = 0x3FF68000
  integer, parameter :: AES_KEY_ADDR = 0x0000
  integer, parameter :: AES_DATA_IN_ADDR = 0x0004
  integer, parameter :: AES_DATA_OUT_ADDR = 0x0008
  integer, parameter :: AES_MODE_ADDR = 0x000C
  ! Random Number Generator
  integer, parameter :: RNG_BASE = 0x3FF75000
  integer, parameter :: RNG_DATA_ADDR = 0x0000
  ! eFuse Controller
  integer, parameter :: EFUSE_BASE = 0x3FF5A000
  integer, parameter :: EFUSE_DATA0_ADDR = 0x0000
  integer, parameter :: EFUSE_DATA1_ADDR = 0x0004
  integer, parameter :: EFUSE_DATA2_ADDR = 0x0008
  integer, parameter :: EFUSE_DATA3_ADDR = 0x000C

  ! 中断向量定义
  integer, parameter :: INT_NMI = 0  ! Non-maskable interrupt
  integer, parameter :: INT_SYS_SOFT = 1  ! Software interrupt
  integer, parameter :: INT_TIMER_INTR0 = 2  ! Hardware timer 0
  integer, parameter :: INT_TIMER_INTR1 = 3  ! Hardware timer 1
  integer, parameter :: INT_TIMER_INTR2 = 4  ! Hardware timer 2
  integer, parameter :: INT_TIMER_GROUP0 = 5  ! TG0 interrupt
  integer, parameter :: INT_TIMER_GROUP1 = 6  ! TG1 interrupt
  integer, parameter :: INT_GPIO = 7  ! GPIO interrupt
  integer, parameter :: INT_GPIO_NMI = 8  ! GPIO NMI interrupt
  integer, parameter :: INT_SPI0 = 9  ! SPI0 interrupt
  integer, parameter :: INT_SPI1 = 10  ! SPI1 interrupt
  integer, parameter :: INT_SPI2 = 11  ! SPI2 interrupt
  integer, parameter :: INT_I2C0 = 12  ! I2C0 interrupt
  integer, parameter :: INT_I2C1 = 13  ! I2C1 interrupt
  integer, parameter :: INT_UART0 = 14  ! UART0 interrupt
  integer, parameter :: INT_UART1 = 15  ! UART1 interrupt
  integer, parameter :: INT_UART2 = 16  ! UART2 interrupt
  integer, parameter :: INT_WDT = 17  ! Watchdog interrupt
  integer, parameter :: INT_RTC = 18  ! RTC interrupt
  integer, parameter :: INT_PWM0 = 19  ! PWM0 interrupt
  integer, parameter :: INT_PWM1 = 20  ! PWM1 interrupt
  integer, parameter :: INT_LEDC = 21  ! LEDC interrupt
  integer, parameter :: INT_TOUCH = 22  ! Touch sensor interrupt
  integer, parameter :: INT_SARADC = 23  ! SARADC interrupt
  integer, parameter :: INT_MAX = 24  ! No. of CPU interrupts
  integer, parameter :: INT_CORE_INTR0 = 25  ! Core 0 interrupt 0
  integer, parameter :: INT_CORE_INTR1 = 26  ! Core 0 interrupt 1
  integer, parameter :: INT_CORE_INTR2 = 27  ! Core 0 interrupt 2
  integer, parameter :: INT_CORE_INTR3 = 28  ! Core 0 interrupt 3
  integer, parameter :: INT_CORE_INTR4 = 29  ! Core 0 interrupt 4
  integer, parameter :: INT_CORE_INTR5 = 30  ! Core 0 interrupt 5
  integer, parameter :: INT_CORE_INTR6 = 31  ! Core 0 interrupt 6
  integer, parameter :: INT_GPIO_INTERRUPT = 32  ! GPIO interrupt
  integer, parameter :: INT_GPIO_INTERRUPT_NMI = 33  ! GPIO NMI interrupt

  ! 引脚定义
  integer, parameter :: PIN_VDD = 1  ! 3.3V Power Supply
  integer, parameter :: PIN_EN = 2  ! Enable ( CHIP_PU )
  integer, parameter :: PIN_SENSOR_VP = 3  ! GPIO36 - ADC1_CH0 - SENSOR_VP
  integer, parameter :: PIN_SENSOR_VN = 4  ! GPIO37 - ADC1_CH1 - SENSOR_VN
  integer, parameter :: PIN_IO34 = 5  ! GPIO34 - ADC1_CH6
  integer, parameter :: PIN_IO35 = 6  ! GPIO35 - ADC1_CH7
  integer, parameter :: PIN_IO32 = 7  ! GPIO32 - ADC1_CH4 - TOUCH_CH9
  integer, parameter :: PIN_IO33 = 8  ! GPIO33 - ADC1_CH5 - TOUCH_CH8
  integer, parameter :: PIN_IO25 = 9  ! GPIO25 - DAC1 - ADC2_CH8
  integer, parameter :: PIN_IO26 = 10  ! GPIO26 - DAC2 - ADC2_CH9
  integer, parameter :: PIN_IO27 = 11  ! GPIO27 - TOUCH_CH7 - ADC2_CH7
  integer, parameter :: PIN_IO14 = 12  ! GPIO14 - ADC2_CH6 - TOUCH_CH6 - HSPI CLK
  integer, parameter :: PIN_IO12 = 13  ! GPIO12 - ADC2_CH5 - TOUCH_CH5 - HSPI Q
  integer, parameter :: PIN_GND = 14  ! Ground
  integer, parameter :: PIN_IO13 = 15  ! GPIO13 - ADC2_CH4 - TOUCH_CH4 - HSPI D
  integer, parameter :: PIN_SD2 = 16  ! GPIO9 - SD_DATA2
  integer, parameter :: PIN_SD3 = 17  ! GPIO10 - SD_DATA3
  integer, parameter :: PIN_CMD = 18  ! GPIO11 - SD_CMD
  integer, parameter :: PIN_CLK = 19  ! GPIO6 - SD_CLK
  integer, parameter :: PIN_SD0 = 20  ! GPIO7 - SD_DATA0
  integer, parameter :: PIN_SD1 = 21  ! GPIO8 - SD_DATA1
  integer, parameter :: PIN_IO15 = 22  ! GPIO15 - ADC2_CH3 - TOUCH_CH3 - VSPID
  integer, parameter :: PIN_IO2 = 23  ! GPIO2 - ADC2_CH2 - TOUCH_CH2 - I2C SDA
  integer, parameter :: PIN_IO0 = 24  ! GPIO0 - ADC2_CH1 - TOUCH_CH0 - I2C SCL
  integer, parameter :: PIN_IO4 = 25  ! GPIO4 - ADC2_CH0 - TOUCH_CH1
  integer, parameter :: PIN_IO16 = 26  ! GPIO16 - HSPI WP
  integer, parameter :: PIN_IO17 = 27  ! GPIO17 - HSPI HD
  integer, parameter :: PIN_IO5 = 28  ! GPIO5 - HSPI CS0
  integer, parameter :: PIN_IO18 = 29  ! GPIO18 - VSPICLK
  integer, parameter :: PIN_IO19 = 30  ! GPIO19 - VSPIQ
  integer, parameter :: PIN_NC = 31  ! Not Connected
  integer, parameter :: PIN_IO21 = 32  ! GPIO21
  integer, parameter :: PIN_RXD0 = 33  ! GPIO3 - U0RXD
  integer, parameter :: PIN_TXD0 = 34  ! GPIO1 - U0TXD
  integer, parameter :: PIN_IO22 = 35  ! GPIO22
  integer, parameter :: PIN_IO23 = 36  ! GPIO23 - VSPID
  integer, parameter :: PIN_GND = 37  ! Ground
  integer, parameter :: PIN_GND = 38  ! Ground

end module esp32_wroom_32_device

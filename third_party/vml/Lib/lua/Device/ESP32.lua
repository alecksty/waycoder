--[[
  ESP32-WROOM-32设备定义 - Lua模块
  生成自: Espressif/ESP32/ESP32-WROOM-32
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Dual-core Xtensa LX6 Wi-Fi and Bluetooth/BLE SoC with 4MB Flash
  CPU架构: Xtensa-LX6
  位宽: 32位
  时钟频率: 160000000 Hz
]]

local ESP32_WROOM_32 = {}

-- 设备信息
ESP32_WROOM_32.DEVICE_NAME = "ESP32-WROOM-32"
ESP32_WROOM_32.MANUFACTURER = "Espressif"
ESP32_WROOM_32.FAMILY = "ESP32"
ESP32_WROOM_32.VERSION = "1.0"
ESP32_WROOM_32.ARCHITECTURE = "Xtensa-LX6"
ESP32_WROOM_32.BITS = 32
ESP32_WROOM_32.CLOCK_FREQUENCY = 160000000

-- 寄存器地址定义
ESP32_WROOM_32.PC_ADDR = 0x00000000  -- Program Counter
ESP32_WROOM_32.A0_ADDR = 0x00000004  -- General Purpose Register 0
ESP32_WROOM_32.A1_ADDR = 0x00000008  -- Stack Pointer
ESP32_WROOM_32.A2_ADDR = 0x0000000C  -- General Purpose Register 2
ESP32_WROOM_32.A3_ADDR = 0x00000010  -- General Purpose Register 3
ESP32_WROOM_32.A4_ADDR = 0x00000014  -- General Purpose Register 4
ESP32_WROOM_32.A5_ADDR = 0x00000018  -- General Purpose Register 5
ESP32_WROOM_32.A6_ADDR = 0x0000001C  -- General Purpose Register 6
ESP32_WROOM_32.A7_ADDR = 0x00000020  -- General Purpose Register 7
ESP32_WROOM_32.A8_ADDR = 0x00000024  -- General Purpose Register 8
ESP32_WROOM_32.A9_ADDR = 0x00000028  -- General Purpose Register 9
ESP32_WROOM_32.A10_ADDR = 0x0000002C  -- General Purpose Register 10
ESP32_WROOM_32.A11_ADDR = 0x00000030  -- General Purpose Register 11
ESP32_WROOM_32.A12_ADDR = 0x00000034  -- General Purpose Register 12
ESP32_WROOM_32.A13_ADDR = 0x00000038  -- General Purpose Register 13
ESP32_WROOM_32.A14_ADDR = 0x0000003C  -- General Purpose Register 14
ESP32_WROOM_32.A15_ADDR = 0x00000040  -- General Purpose Register 15
ESP32_WROOM_32.SAREG1_ADDR = 0x00000044  -- Special Address Register 1
ESP32_WROOM_32.SAREG2_ADDR = 0x00000048  -- Special Address Register 2
ESP32_WROOM_32.LBEG_ADDR = 0x0000004C  -- Loop Beginning
ESP32_WROOM_32.LEND_ADDR = 0x00000050  -- Loop End
ESP32_WROOM_32.LCOUNT_ADDR = 0x00000054  -- Loop Counter
ESP32_WROOM_32.PS_ADDR = 0x00000058  -- Processor Status
ESP32_WROOM_32.WINDOWBASE_ADDR = 0x0000005C  -- Window Base
ESP32_WROOM_32.WINDOWSTART_ADDR = 0x00000060  -- Window Start
ESP32_WROOM_32.PTEBASE_ADDR = 0x00000064  -- Page Table Base
ESP32_WROOM_32.PTESIZE_ADDR = 0x00000068  -- Page Table Entry Size
ESP32_WROOM_32.SCOMPARE1_ADDR = 0x0000006C  -- Special Compare 1
ESP32_WROOM_32.ATOMCTL_ADDR = 0x00000070  -- Atomic Operation Control
ESP32_WROOM_32.DDR_ADDR = 0x00000074  -- Data Destination Register

-- 内存段定义
ESP32_WROOM_32.ROM_START = 0x40000000
ESP32_WROOM_32.ROM_END = 0x4003FFFF
ESP32_WROOM_32.ROM_SIZE = 262144  -- ROM (224KB)
ESP32_WROOM_32.SRAM_START = 0x3FF00000
ESP32_WROOM_32.SRAM_END = 0x3FF7FFFF
ESP32_WROOM_32.SRAM_SIZE = 524288  -- SRAM (320KB total)
ESP32_WROOM_32.DRAM0_START = 0x3FF80000
ESP32_WROOM_32.DRAM0_END = 0x3FF9FFFF
ESP32_WROOM_32.DRAM0_SIZE = 131072  -- DRAM0 (128KB)
ESP32_WROOM_32.IRAM0_START = 0x40000000
ESP32_WROOM_32.IRAM0_END = 0x401FFFFF
ESP32_WROOM_32.IRAM0_SIZE = 2097152  -- IRAM0
ESP32_WROOM_32.FLASH_START = 0x40200000
ESP32_WROOM_32.FLASH_END = 0x405FFFFF
ESP32_WROOM_32.FLASH_SIZE = 4194304  -- External Flash (4MB)
ESP32_WROOM_32.PERIPHERAL_START = 0x3FF00000
ESP32_WROOM_32.PERIPHERAL_END = 0x3FFBFFFF
ESP32_WROOM_32.PERIPHERAL_SIZE = 786432  -- Peripheral Registers
ESP32_WROOM_32.GPIO_START = 0x3FF44000
ESP32_WROOM_32.GPIO_END = 0x3FF44FFF
ESP32_WROOM_32.GPIO_SIZE = 4096  -- GPIO

-- 外设定义
-- GPIO
ESP32_WROOM_32.GPIO_BASE = 0x3FF44000
ESP32_WROOM_32.GPIO_OUT_ADDR = 0x0000
ESP32_WROOM_32.GPIO_OUT_W1TS_ADDR = 0x0008
ESP32_WROOM_32.GPIO_OUT_W1TC_ADDR = 0x000C
ESP32_WROOM_32.GPIO_IN_ADDR = 0x003C
ESP32_WROOM_32.GPIO_STATUS_ADDR = 0x0024
ESP32_WROOM_32.GPIO_STATUS_W1TS_ADDR = 0x0028
ESP32_WROOM_32.GPIO_STATUS_W1TC_ADDR = 0x002C
ESP32_WROOM_32.GPIO_PIN_ADDR = 0x0040
ESP32_WROOM_32.GPIO_ENABLE_ADDR = 0x0020
ESP32_WROOM_32.GPIO_STRAP_ADDR = 0x0038
ESP32_WROOM_32.GPIO_IN_NEXT_ADDR = 0x0044
-- RTC GPIO
ESP32_WROOM_32.RTC_GPIO_BASE = 0x3FF48000
ESP32_WROOM_32.RTC_GPIO_OUT_ADDR = 0x0000
ESP32_WROOM_32.RTC_GPIO_OUT_W1TS_ADDR = 0x0008
ESP32_WROOM_32.RTC_GPIO_OUT_W1TC_ADDR = 0x000C
ESP32_WROOM_32.RTC_GPIO_IN_ADDR = 0x0044
ESP32_WROOM_32.RTC_GPIO_STATUS_ADDR = 0x0024
ESP32_WROOM_32.RTC_GPIO_PIN_ADDR = 0x0048
ESP32_WROOM_32.RTC_GPIO_ENABLE_ADDR = 0x0020
-- IO MUX
ESP32_WROOM_32.IO_MUX_BASE = 0x3FF49000
ESP32_WROOM_32.IO_MUX_GPIO0_ADDR = 0x0000
ESP32_WROOM_32.IO_MUX_GPIO1_ADDR = 0x0004
ESP32_WROOM_32.IO_MUX_GPIO2_ADDR = 0x0008
ESP32_WROOM_32.IO_MUX_GPIO3_ADDR = 0x000C
ESP32_WROOM_32.IO_MUX_GPIO4_ADDR = 0x0010
ESP32_WROOM_32.IO_MUX_GPIO5_ADDR = 0x0014
ESP32_WROOM_32.IO_MUX_GPIO6_ADDR = 0x0018
ESP32_WROOM_32.IO_MUX_GPIO7_ADDR = 0x001C
ESP32_WROOM_32.IO_MUX_GPIO8_ADDR = 0x0020
ESP32_WROOM_32.IO_MUX_GPIO9_ADDR = 0x0024
ESP32_WROOM_32.IO_MUX_GPIO10_ADDR = 0x0028
ESP32_WROOM_32.IO_MUX_GPIO11_ADDR = 0x002C
ESP32_WROOM_32.IO_MUX_GPIO12_ADDR = 0x0030
ESP32_WROOM_32.IO_MUX_GPIO13_ADDR = 0x0034
ESP32_WROOM_32.IO_MUX_GPIO14_ADDR = 0x0038
ESP32_WROOM_32.IO_MUX_GPIO15_ADDR = 0x003C
ESP32_WROOM_32.IO_MUX_GPIO16_ADDR = 0x0040
ESP32_WROOM_32.IO_MUX_GPIO17_ADDR = 0x0044
ESP32_WROOM_32.IO_MUX_GPIO18_ADDR = 0x0048
ESP32_WROOM_32.IO_MUX_GPIO19_ADDR = 0x004C
ESP32_WROOM_32.IO_MUX_GPIO20_ADDR = 0x0050
ESP32_WROOM_32.IO_MUX_GPIO21_ADDR = 0x0054
ESP32_WROOM_32.IO_MUX_GPIO22_ADDR = 0x0058
ESP32_WROOM_32.IO_MUX_GPIO23_ADDR = 0x005C
ESP32_WROOM_32.IO_MUX_GPIO24_ADDR = 0x0060
ESP32_WROOM_32.IO_MUX_GPIO25_ADDR = 0x0064
ESP32_WROOM_32.IO_MUX_GPIO26_ADDR = 0x0068
ESP32_WROOM_32.IO_MUX_GPIO27_ADDR = 0x006C
-- UART 0
ESP32_WROOM_32.UART0_BASE = 0x3FF40000
ESP32_WROOM_32.UART0_FIFO_ADDR = 0x0000
ESP32_WROOM_32.UART0_INT_RAW_ADDR = 0x0004
ESP32_WROOM_32.UART0_INT_ST_ADDR = 0x0008
ESP32_WROOM_32.UART0_INT_ENA_ADDR = 0x000C
ESP32_WROOM_32.UART0_INT_CLR_ADDR = 0x0010
ESP32_WROOM_32.UART0_CONF0_ADDR = 0x0020
ESP32_WROOM_32.UART0_CONF1_ADDR = 0x0024
ESP32_WROOM_32.UART0_LOWPULSE_ADDR = 0x0028
ESP32_WROOM_32.UART0_HIGHPULSE_ADDR = 0x002C
ESP32_WROOM_32.UART0_PULSE_CNT_ADDR = 0x0030
ESP32_WROOM_32.UART0_DATE_ADDR = 0x0078
ESP32_WROOM_32.UART0_AHB_BIT_ADDR = 0x007C
-- UART 1
ESP32_WROOM_32.UART1_BASE = 0x3FF50000
ESP32_WROOM_32.UART1_FIFO_ADDR = 0x0000
ESP32_WROOM_32.UART1_INT_RAW_ADDR = 0x0004
ESP32_WROOM_32.UART1_INT_ST_ADDR = 0x0008
ESP32_WROOM_32.UART1_INT_ENA_ADDR = 0x000C
ESP32_WROOM_32.UART1_INT_CLR_ADDR = 0x0010
ESP32_WROOM_32.UART1_CONF0_ADDR = 0x0020
ESP32_WROOM_32.UART1_CONF1_ADDR = 0x0024
-- UART 2
ESP32_WROOM_32.UART2_BASE = 0x3FF6E000
ESP32_WROOM_32.UART2_FIFO_ADDR = 0x0000
ESP32_WROOM_32.UART2_INT_RAW_ADDR = 0x0004
ESP32_WROOM_32.UART2_INT_ST_ADDR = 0x0008
ESP32_WROOM_32.UART2_INT_ENA_ADDR = 0x000C
ESP32_WROOM_32.UART2_INT_CLR_ADDR = 0x0010
ESP32_WROOM_32.UART2_CONF0_ADDR = 0x0020
ESP32_WROOM_32.UART2_CONF1_ADDR = 0x0024
-- SPI0 (Flash)
ESP32_WROOM_32.SPI0_BASE = 0x3FF42000
ESP32_WROOM_32.SPI0_CMD_ADDR = 0x0000
ESP32_WROOM_32.SPI0_ADDR_ADDR = 0x0004
ESP32_WROOM_32.SPI0_CONTROL_ADDR = 0x0008
ESP32_WROOM_32.SPI0_CONTROL1_ADDR = 0x000C
ESP32_WROOM_32.SPI0_STATUS_ADDR = 0x0010
ESP32_WROOM_32.SPI0_STATUS1_ADDR = 0x0014
ESP32_WROOM_32.SPI0_DATA_ADDR = 0x0020
ESP32_WROOM_32.SPI0_USER_ADDR = 0x003C
ESP32_WROOM_32.SPI0_USER1_ADDR = 0x0040
ESP32_WROOM_32.SPI0_USER2_ADDR = 0x0044
ESP32_WROOM_32.SPI0_PIN_ADDR = 0x0048
ESP32_WROOM_32.SPI0_SLAVE_ADDR = 0x004C
ESP32_WROOM_32.SPI0_CACHE_FLASH_ADDR = 0x0050
ESP32_WROOM_32.SPI0_CLOCK_ADDR = 0x0058
ESP32_WROOM_32.SPI0_FIFO_ADDR = 0x0060
-- SPI1
ESP32_WROOM_32.SPI1_BASE = 0x3FF43000
ESP32_WROOM_32.SPI1_CMD_ADDR = 0x0000
ESP32_WROOM_32.SPI1_ADDR_ADDR = 0x0004
ESP32_WROOM_32.SPI1_CONTROL_ADDR = 0x0008
ESP32_WROOM_32.SPI1_STATUS_ADDR = 0x0010
ESP32_WROOM_32.SPI1_DATA_ADDR = 0x0020
ESP32_WROOM_32.SPI1_USER_ADDR = 0x003C
ESP32_WROOM_32.SPI1_CLOCK_ADDR = 0x0058
-- SPI2 (HSPI)
ESP32_WROOM_32.SPI2_BASE = 0x3FF64000
ESP32_WROOM_32.SPI2_CMD_ADDR = 0x0000
ESP32_WROOM_32.SPI2_ADDR_ADDR = 0x0004
ESP32_WROOM_32.SPI2_CONTROL_ADDR = 0x0008
ESP32_WROOM_32.SPI2_STATUS_ADDR = 0x0010
ESP32_WROOM_32.SPI2_DATA_ADDR = 0x0020
ESP32_WROOM_32.SPI2_USER_ADDR = 0x003C
ESP32_WROOM_32.SPI2_CLOCK_ADDR = 0x0058
ESP32_WROOM_32.SPI2_FIFO_ADDR = 0x0060
-- I2C 0
ESP32_WROOM_32.I2C0_BASE = 0x3FF53000
ESP32_WROOM_32.I2C0_SCL_START_ADDR = 0x0000
ESP32_WROOM_32.I2C0_SCL_LOW_ADDR = 0x0004
ESP32_WROOM_32.I2C0_SDA_START_ADDR = 0x0008
ESP32_WROOM_32.I2C0_SDA_LOW_ADDR = 0x000C
ESP32_WROOM_32.I2C0_INT_ENA_ADDR = 0x0010
ESP32_WROOM_32.I2C0_INT_CLR_ADDR = 0x0014
ESP32_WROOM_32.I2C0_INT_RAW_ADDR = 0x0018
ESP32_WROOM_32.I2C0_INT_STATUS_ADDR = 0x001C
ESP32_WROOM_32.I2C0_SCL_HIGH_PERIOD_ADDR = 0x0020
ESP32_WROOM_32.I2C0_SCL_HIGH_PERIOD_S_ADDR = 0x0024
ESP32_WROOM_32.I2C0_SCL_START_HOLD_ADDR = 0x0028
ESP32_WROOM_32.I2C0_SDA_START_HOLD_ADDR = 0x002C
ESP32_WROOM_32.I2C0_SCL_LAST_HOLD_ADDR = 0x0030
ESP32_WROOM_32.I2C0_SCL_WAIT_PERIOD_ADDR = 0x0034
ESP32_WROOM_32.I2C0_CTR_ADDR = 0x0050
ESP32_WROOM_32.I2C0_STATUS_ADDR = 0x0054
ESP32_WROOM_32.I2C0_FINISH_INT_ENA_ADDR = 0x0058
ESP32_WROOM_32.I2C0_COMMAND0_ADDR = 0x0060
ESP32_WROOM_32.I2C0_COMMAND1_ADDR = 0x0064
ESP32_WROOM_32.I2C0_COMMAND2_ADDR = 0x0068
ESP32_WROOM_32.I2C0_COMMAND3_ADDR = 0x006C
ESP32_WROOM_32.I2C0_DATA_ADDR = 0x0080
-- I2C 1
ESP32_WROOM_32.I2C1_BASE = 0x3FF67000
ESP32_WROOM_32.I2C1_CTR_ADDR = 0x0050
ESP32_WROOM_32.I2C1_DATA_ADDR = 0x0080
ESP32_WROOM_32.I2C1_COMMAND0_ADDR = 0x0060
ESP32_WROOM_32.I2C1_COMMAND1_ADDR = 0x0064
-- Timer Group 0
ESP32_WROOM_32.TIMG0_BASE = 0x3FF5F000
ESP32_WROOM_32.TIMG0_T0CONFIG_ADDR = 0x0000
ESP32_WROOM_32.TIMG0_T0LO_ADDR = 0x0004
ESP32_WROOM_32.TIMG0_T0HI_ADDR = 0x0008
ESP32_WROOM_32.TIMG0_T0UPDATE_ADDR = 0x000C
ESP32_WROOM_32.TIMG0_T0ALARM_ADDR = 0x0010
ESP32_WROOM_32.TIMG0_T0LOAD_ADDR = 0x0014
ESP32_WROOM_32.TIMG0_T0LOAD_REG_ADDR = 0x0018
-- Timer Group 1
ESP32_WROOM_32.TIMG1_BASE = 0x3FF60000
ESP32_WROOM_32.TIMG1_T0CONFIG_ADDR = 0x0000
ESP32_WROOM_32.TIMG1_T0LO_ADDR = 0x0004
ESP32_WROOM_32.TIMG1_T0HI_ADDR = 0x0008
ESP32_WROOM_32.TIMG1_T0ALARM_ADDR = 0x0010
ESP32_WROOM_32.TIMG1_T0LOAD_ADDR = 0x0014
-- Motor Control PWM 0
ESP32_WROOM_32.PWM0_BASE = 0x3FF59000
ESP32_WROOM_32.PWM0_CNT_ADDR = 0x0000
ESP32_WROOM_32.PWM0_PERIOD_ADDR = 0x0004
ESP32_WROOM_32.PWM0_DUTY_ADDR = 0x0008
ESP32_WROOM_32.PWM0_CONFIG0_ADDR = 0x0010
ESP32_WROOM_32.PWM0_CONFIG1_ADDR = 0x0014
ESP32_WROOM_32.PWM0_CONFIG2_ADDR = 0x0018
ESP32_WROOM_32.PWM0_UPDATE_ADDR = 0x0020
-- Motor Control PWM 1
ESP32_WROOM_32.PWM1_BASE = 0x3FF5A000
ESP32_WROOM_32.PWM1_CNT_ADDR = 0x0000
ESP32_WROOM_32.PWM1_PERIOD_ADDR = 0x0004
ESP32_WROOM_32.PWM1_DUTY_ADDR = 0x0008
-- LED PWM Controller
ESP32_WROOM_32.LEDC_BASE = 0x3FF59000
ESP32_WROOM_32.LEDC_CONFIG0_ADDR = 0x0000
ESP32_WROOM_32.LEDC_HPOINT0_ADDR = 0x0018
ESP32_WROOM_32.LEDC_DUTY0_ADDR = 0x001C
ESP32_WROOM_32.LEDC_HPOINT1_ADDR = 0x0028
ESP32_WROOM_32.LEDC_DUTY1_ADDR = 0x002C
ESP32_WROOM_32.LEDC_HPOINT2_ADDR = 0x0038
ESP32_WROOM_32.LEDC_DUTY2_ADDR = 0x003C
ESP32_WROOM_32.LEDC_HPOINT3_ADDR = 0x0048
ESP32_WROOM_32.LEDC_DUTY3_ADDR = 0x004C
ESP32_WROOM_32.LEDC_TIMER0_CONF_ADDR = 0x0000
ESP32_WROOM_32.LEDC_TIMER0_LOAD_ADDR = 0x0004
-- RTC Controller
ESP32_WROOM_32.RTC_BASE = 0x3FF48000
ESP32_WROOM_32.RTC_RTC_CNTL_ADDR = 0x0000
ESP32_WROOM_32.RTC_RTC_TIMER_ADDR = 0x000C
ESP32_WROOM_32.RTC_RTC_UPDATE_ADDR = 0x0010
ESP32_WROOM_32.RTC_RTC_STATE0_ADDR = 0x0080
-- Wi-Fi
ESP32_WROOM_32.WIFI_BASE = 0x3FFAE000
ESP32_WROOM_32.WIFI_MAC_ADDR = 0x0000
ESP32_WROOM_32.WIFI_CONFIG_ADDR = 0x0100
-- Bluetooth/BLE
ESP32_WROOM_32.BT_BASE = 0x3FFB0000
ESP32_WROOM_32.BT_CONFIG_ADDR = 0x0000
-- SHA Hardware Accelerator
ESP32_WROOM_32.SHA_BASE = 0x3FF67000
ESP32_WROOM_32.SHA_MODE_ADDR = 0x0000
ESP32_WROOM_32.SHA_DATA_ADDR = 0x0004
ESP32_WROOM_32.SHA_HASH_ADDR = 0x0008
-- AES Hardware Accelerator
ESP32_WROOM_32.AES_BASE = 0x3FF68000
ESP32_WROOM_32.AES_KEY_ADDR = 0x0000
ESP32_WROOM_32.AES_DATA_IN_ADDR = 0x0004
ESP32_WROOM_32.AES_DATA_OUT_ADDR = 0x0008
ESP32_WROOM_32.AES_MODE_ADDR = 0x000C
-- Random Number Generator
ESP32_WROOM_32.RNG_BASE = 0x3FF75000
ESP32_WROOM_32.RNG_DATA_ADDR = 0x0000
-- eFuse Controller
ESP32_WROOM_32.EFUSE_BASE = 0x3FF5A000
ESP32_WROOM_32.EFUSE_DATA0_ADDR = 0x0000
ESP32_WROOM_32.EFUSE_DATA1_ADDR = 0x0004
ESP32_WROOM_32.EFUSE_DATA2_ADDR = 0x0008
ESP32_WROOM_32.EFUSE_DATA3_ADDR = 0x000C

-- 中断向量定义
ESP32_WROOM_32.INT_NMI = 0  -- Non-maskable interrupt
ESP32_WROOM_32.INT_SYS_SOFT = 1  -- Software interrupt
ESP32_WROOM_32.INT_TIMER_INTR0 = 2  -- Hardware timer 0
ESP32_WROOM_32.INT_TIMER_INTR1 = 3  -- Hardware timer 1
ESP32_WROOM_32.INT_TIMER_INTR2 = 4  -- Hardware timer 2
ESP32_WROOM_32.INT_TIMER_GROUP0 = 5  -- TG0 interrupt
ESP32_WROOM_32.INT_TIMER_GROUP1 = 6  -- TG1 interrupt
ESP32_WROOM_32.INT_GPIO = 7  -- GPIO interrupt
ESP32_WROOM_32.INT_GPIO_NMI = 8  -- GPIO NMI interrupt
ESP32_WROOM_32.INT_SPI0 = 9  -- SPI0 interrupt
ESP32_WROOM_32.INT_SPI1 = 10  -- SPI1 interrupt
ESP32_WROOM_32.INT_SPI2 = 11  -- SPI2 interrupt
ESP32_WROOM_32.INT_I2C0 = 12  -- I2C0 interrupt
ESP32_WROOM_32.INT_I2C1 = 13  -- I2C1 interrupt
ESP32_WROOM_32.INT_UART0 = 14  -- UART0 interrupt
ESP32_WROOM_32.INT_UART1 = 15  -- UART1 interrupt
ESP32_WROOM_32.INT_UART2 = 16  -- UART2 interrupt
ESP32_WROOM_32.INT_WDT = 17  -- Watchdog interrupt
ESP32_WROOM_32.INT_RTC = 18  -- RTC interrupt
ESP32_WROOM_32.INT_PWM0 = 19  -- PWM0 interrupt
ESP32_WROOM_32.INT_PWM1 = 20  -- PWM1 interrupt
ESP32_WROOM_32.INT_LEDC = 21  -- LEDC interrupt
ESP32_WROOM_32.INT_TOUCH = 22  -- Touch sensor interrupt
ESP32_WROOM_32.INT_SARADC = 23  -- SARADC interrupt
ESP32_WROOM_32.INT_MAX = 24  -- No. of CPU interrupts
ESP32_WROOM_32.INT_CORE_INTR0 = 25  -- Core 0 interrupt 0
ESP32_WROOM_32.INT_CORE_INTR1 = 26  -- Core 0 interrupt 1
ESP32_WROOM_32.INT_CORE_INTR2 = 27  -- Core 0 interrupt 2
ESP32_WROOM_32.INT_CORE_INTR3 = 28  -- Core 0 interrupt 3
ESP32_WROOM_32.INT_CORE_INTR4 = 29  -- Core 0 interrupt 4
ESP32_WROOM_32.INT_CORE_INTR5 = 30  -- Core 0 interrupt 5
ESP32_WROOM_32.INT_CORE_INTR6 = 31  -- Core 0 interrupt 6
ESP32_WROOM_32.INT_GPIO_INTERRUPT = 32  -- GPIO interrupt
ESP32_WROOM_32.INT_GPIO_INTERRUPT_NMI = 33  -- GPIO NMI interrupt

-- 引脚定义
ESP32_WROOM_32.PIN_VDD = 1  -- 3.3V Power Supply
ESP32_WROOM_32.PIN_EN = 2  -- Enable ( CHIP_PU )
ESP32_WROOM_32.PIN_SENSOR_VP = 3  -- GPIO36 - ADC1_CH0 - SENSOR_VP
ESP32_WROOM_32.PIN_SENSOR_VN = 4  -- GPIO37 - ADC1_CH1 - SENSOR_VN
ESP32_WROOM_32.PIN_IO34 = 5  -- GPIO34 - ADC1_CH6
ESP32_WROOM_32.PIN_IO35 = 6  -- GPIO35 - ADC1_CH7
ESP32_WROOM_32.PIN_IO32 = 7  -- GPIO32 - ADC1_CH4 - TOUCH_CH9
ESP32_WROOM_32.PIN_IO33 = 8  -- GPIO33 - ADC1_CH5 - TOUCH_CH8
ESP32_WROOM_32.PIN_IO25 = 9  -- GPIO25 - DAC1 - ADC2_CH8
ESP32_WROOM_32.PIN_IO26 = 10  -- GPIO26 - DAC2 - ADC2_CH9
ESP32_WROOM_32.PIN_IO27 = 11  -- GPIO27 - TOUCH_CH7 - ADC2_CH7
ESP32_WROOM_32.PIN_IO14 = 12  -- GPIO14 - ADC2_CH6 - TOUCH_CH6 - HSPI CLK
ESP32_WROOM_32.PIN_IO12 = 13  -- GPIO12 - ADC2_CH5 - TOUCH_CH5 - HSPI Q
ESP32_WROOM_32.PIN_GND = 14  -- Ground
ESP32_WROOM_32.PIN_IO13 = 15  -- GPIO13 - ADC2_CH4 - TOUCH_CH4 - HSPI D
ESP32_WROOM_32.PIN_SD2 = 16  -- GPIO9 - SD_DATA2
ESP32_WROOM_32.PIN_SD3 = 17  -- GPIO10 - SD_DATA3
ESP32_WROOM_32.PIN_CMD = 18  -- GPIO11 - SD_CMD
ESP32_WROOM_32.PIN_CLK = 19  -- GPIO6 - SD_CLK
ESP32_WROOM_32.PIN_SD0 = 20  -- GPIO7 - SD_DATA0
ESP32_WROOM_32.PIN_SD1 = 21  -- GPIO8 - SD_DATA1
ESP32_WROOM_32.PIN_IO15 = 22  -- GPIO15 - ADC2_CH3 - TOUCH_CH3 - VSPID
ESP32_WROOM_32.PIN_IO2 = 23  -- GPIO2 - ADC2_CH2 - TOUCH_CH2 - I2C SDA
ESP32_WROOM_32.PIN_IO0 = 24  -- GPIO0 - ADC2_CH1 - TOUCH_CH0 - I2C SCL
ESP32_WROOM_32.PIN_IO4 = 25  -- GPIO4 - ADC2_CH0 - TOUCH_CH1
ESP32_WROOM_32.PIN_IO16 = 26  -- GPIO16 - HSPI WP
ESP32_WROOM_32.PIN_IO17 = 27  -- GPIO17 - HSPI HD
ESP32_WROOM_32.PIN_IO5 = 28  -- GPIO5 - HSPI CS0
ESP32_WROOM_32.PIN_IO18 = 29  -- GPIO18 - VSPICLK
ESP32_WROOM_32.PIN_IO19 = 30  -- GPIO19 - VSPIQ
ESP32_WROOM_32.PIN_NC = 31  -- Not Connected
ESP32_WROOM_32.PIN_IO21 = 32  -- GPIO21
ESP32_WROOM_32.PIN_RXD0 = 33  -- GPIO3 - U0RXD
ESP32_WROOM_32.PIN_TXD0 = 34  -- GPIO1 - U0TXD
ESP32_WROOM_32.PIN_IO22 = 35  -- GPIO22
ESP32_WROOM_32.PIN_IO23 = 36  -- GPIO23 - VSPID
ESP32_WROOM_32.PIN_GND = 37  -- Ground
ESP32_WROOM_32.PIN_GND = 38  -- Ground

-- 设备类
function ESP32_WROOM_32.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["PC"] = {
            address = 0x00000000,
            size = 4,
            access = "r",
            description = "Program Counter",
            value = 0
        }
        self.registers["A0"] = {
            address = 0x00000004,
            size = 4,
            access = "rw",
            description = "General Purpose Register 0",
            value = 0
        }
        self.registers["A1"] = {
            address = 0x00000008,
            size = 4,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["A2"] = {
            address = 0x0000000C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 2",
            value = 0
        }
        self.registers["A3"] = {
            address = 0x00000010,
            size = 4,
            access = "rw",
            description = "General Purpose Register 3",
            value = 0
        }
        self.registers["A4"] = {
            address = 0x00000014,
            size = 4,
            access = "rw",
            description = "General Purpose Register 4",
            value = 0
        }
        self.registers["A5"] = {
            address = 0x00000018,
            size = 4,
            access = "rw",
            description = "General Purpose Register 5",
            value = 0
        }
        self.registers["A6"] = {
            address = 0x0000001C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 6",
            value = 0
        }
        self.registers["A7"] = {
            address = 0x00000020,
            size = 4,
            access = "rw",
            description = "General Purpose Register 7",
            value = 0
        }
        self.registers["A8"] = {
            address = 0x00000024,
            size = 4,
            access = "rw",
            description = "General Purpose Register 8",
            value = 0
        }
        self.registers["A9"] = {
            address = 0x00000028,
            size = 4,
            access = "rw",
            description = "General Purpose Register 9",
            value = 0
        }
        self.registers["A10"] = {
            address = 0x0000002C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 10",
            value = 0
        }
        self.registers["A11"] = {
            address = 0x00000030,
            size = 4,
            access = "rw",
            description = "General Purpose Register 11",
            value = 0
        }
        self.registers["A12"] = {
            address = 0x00000034,
            size = 4,
            access = "rw",
            description = "General Purpose Register 12",
            value = 0
        }
        self.registers["A13"] = {
            address = 0x00000038,
            size = 4,
            access = "rw",
            description = "General Purpose Register 13",
            value = 0
        }
        self.registers["A14"] = {
            address = 0x0000003C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 14",
            value = 0
        }
        self.registers["A15"] = {
            address = 0x00000040,
            size = 4,
            access = "rw",
            description = "General Purpose Register 15",
            value = 0
        }
        self.registers["SAREG1"] = {
            address = 0x00000044,
            size = 4,
            access = "rw",
            description = "Special Address Register 1",
            value = 0
        }
        self.registers["SAREG2"] = {
            address = 0x00000048,
            size = 4,
            access = "rw",
            description = "Special Address Register 2",
            value = 0
        }
        self.registers["LBEG"] = {
            address = 0x0000004C,
            size = 4,
            access = "rw",
            description = "Loop Beginning",
            value = 0
        }
        self.registers["LEND"] = {
            address = 0x00000050,
            size = 4,
            access = "rw",
            description = "Loop End",
            value = 0
        }
        self.registers["LCOUNT"] = {
            address = 0x00000054,
            size = 4,
            access = "rw",
            description = "Loop Counter",
            value = 0
        }
        self.registers["PS"] = {
            address = 0x00000058,
            size = 4,
            access = "rw",
            description = "Processor Status",
            value = 0
        }
        self.registers["WINDOWBASE"] = {
            address = 0x0000005C,
            size = 4,
            access = "rw",
            description = "Window Base",
            value = 0
        }
        self.registers["WINDOWSTART"] = {
            address = 0x00000060,
            size = 4,
            access = "rw",
            description = "Window Start",
            value = 0
        }
        self.registers["PTEBASE"] = {
            address = 0x00000064,
            size = 4,
            access = "rw",
            description = "Page Table Base",
            value = 0
        }
        self.registers["PTESIZE"] = {
            address = 0x00000068,
            size = 4,
            access = "rw",
            description = "Page Table Entry Size",
            value = 0
        }
        self.registers["SCOMPARE1"] = {
            address = 0x0000006C,
            size = 4,
            access = "rw",
            description = "Special Compare 1",
            value = 0
        }
        self.registers["ATOMCTL"] = {
            address = 0x00000070,
            size = 4,
            access = "rw",
            description = "Atomic Operation Control",
            value = 0
        }
        self.registers["DDR"] = {
            address = 0x00000074,
            size = 4,
            access = "rw",
            description = "Data Destination Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["GPIO"] = {
            base = 0x3FF44000,
            type = "gpio",
            description = "GPIO",
            registers = {}
        }
        
        local p = self.peripherals["GPIO"]
        p.registers["OUT"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["OUT_W1TS"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["OUT_W1TC"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["IN"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["STATUS_W1TS"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["STATUS_W1TC"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["PIN"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["ENABLE"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["STRAP"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["IN_NEXT"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        self.peripherals["RTC_GPIO"] = {
            base = 0x3FF48000,
            type = "gpio",
            description = "RTC GPIO",
            registers = {}
        }
        
        local p = self.peripherals["RTC_GPIO"]
        p.registers["OUT"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["OUT_W1TS"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["OUT_W1TC"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["IN"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["PIN"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        p.registers["ENABLE"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        self.peripherals["IO_MUX"] = {
            base = 0x3FF49000,
            type = "gpio",
            description = "IO MUX",
            registers = {}
        }
        
        local p = self.peripherals["IO_MUX"]
        p.registers["GPIO0"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["GPIO1"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["GPIO2"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["GPIO3"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["GPIO4"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["GPIO5"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["GPIO6"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["GPIO7"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["GPIO8"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["GPIO9"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["GPIO10"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["GPIO11"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["GPIO12"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["GPIO13"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["GPIO14"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["GPIO15"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["GPIO16"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["GPIO17"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["GPIO18"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        p.registers["GPIO19"] = {
            address = 0x004C,
            size = 4,
            value = 0
        }
        p.registers["GPIO20"] = {
            address = 0x0050,
            size = 4,
            value = 0
        }
        p.registers["GPIO21"] = {
            address = 0x0054,
            size = 4,
            value = 0
        }
        p.registers["GPIO22"] = {
            address = 0x0058,
            size = 4,
            value = 0
        }
        p.registers["GPIO23"] = {
            address = 0x005C,
            size = 4,
            value = 0
        }
        p.registers["GPIO24"] = {
            address = 0x0060,
            size = 4,
            value = 0
        }
        p.registers["GPIO25"] = {
            address = 0x0064,
            size = 4,
            value = 0
        }
        p.registers["GPIO26"] = {
            address = 0x0068,
            size = 4,
            value = 0
        }
        p.registers["GPIO27"] = {
            address = 0x006C,
            size = 4,
            value = 0
        }
        self.peripherals["UART0"] = {
            base = 0x3FF40000,
            type = "uart",
            description = "UART 0",
            registers = {}
        }
        
        local p = self.peripherals["UART0"]
        p.registers["FIFO"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["INT_RAW"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["INT_ST"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["INT_ENA"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["INT_CLR"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["CONF0"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["CONF1"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["LOWPULSE"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["HIGHPULSE"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["PULSE_CNT"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["DATE"] = {
            address = 0x0078,
            size = 4,
            value = 0
        }
        p.registers["AHB_BIT"] = {
            address = 0x007C,
            size = 4,
            value = 0
        }
        self.peripherals["UART1"] = {
            base = 0x3FF50000,
            type = "uart",
            description = "UART 1",
            registers = {}
        }
        
        local p = self.peripherals["UART1"]
        p.registers["FIFO"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["INT_RAW"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["INT_ST"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["INT_ENA"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["INT_CLR"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["CONF0"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["CONF1"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        self.peripherals["UART2"] = {
            base = 0x3FF6E000,
            type = "uart",
            description = "UART 2",
            registers = {}
        }
        
        local p = self.peripherals["UART2"]
        p.registers["FIFO"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["INT_RAW"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["INT_ST"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["INT_ENA"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["INT_CLR"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["CONF0"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["CONF1"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        self.peripherals["SPI0"] = {
            base = 0x3FF42000,
            type = "spi",
            description = "SPI0 (Flash)",
            registers = {}
        }
        
        local p = self.peripherals["SPI0"]
        p.registers["CMD"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["ADDR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["CONTROL"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["CONTROL1"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["STATUS1"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["USER"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["USER1"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["USER2"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["PIN"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        p.registers["SLAVE"] = {
            address = 0x004C,
            size = 4,
            value = 0
        }
        p.registers["CACHE_FLASH"] = {
            address = 0x0050,
            size = 4,
            value = 0
        }
        p.registers["CLOCK"] = {
            address = 0x0058,
            size = 4,
            value = 0
        }
        p.registers["FIFO"] = {
            address = 0x0060,
            size = 4,
            value = 0
        }
        self.peripherals["SPI1"] = {
            base = 0x3FF43000,
            type = "spi",
            description = "SPI1",
            registers = {}
        }
        
        local p = self.peripherals["SPI1"]
        p.registers["CMD"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["ADDR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["CONTROL"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["USER"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["CLOCK"] = {
            address = 0x0058,
            size = 4,
            value = 0
        }
        self.peripherals["SPI2"] = {
            base = 0x3FF64000,
            type = "spi",
            description = "SPI2 (HSPI)",
            registers = {}
        }
        
        local p = self.peripherals["SPI2"]
        p.registers["CMD"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["ADDR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["CONTROL"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["USER"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["CLOCK"] = {
            address = 0x0058,
            size = 4,
            value = 0
        }
        p.registers["FIFO"] = {
            address = 0x0060,
            size = 4,
            value = 0
        }
        self.peripherals["I2C0"] = {
            base = 0x3FF53000,
            type = "i2c",
            description = "I2C 0",
            registers = {}
        }
        
        local p = self.peripherals["I2C0"]
        p.registers["SCL_START"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["SCL_LOW"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["SDA_START"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["SDA_LOW"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["INT_ENA"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["INT_CLR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["INT_RAW"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["INT_STATUS"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["SCL_HIGH_PERIOD"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["SCL_HIGH_PERIOD_S"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["SCL_START_HOLD"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["SDA_START_HOLD"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["SCL_LAST_HOLD"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["SCL_WAIT_PERIOD"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["CTR"] = {
            address = 0x0050,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x0054,
            size = 4,
            value = 0
        }
        p.registers["FINISH_INT_ENA"] = {
            address = 0x0058,
            size = 4,
            value = 0
        }
        p.registers["COMMAND0"] = {
            address = 0x0060,
            size = 4,
            value = 0
        }
        p.registers["COMMAND1"] = {
            address = 0x0064,
            size = 4,
            value = 0
        }
        p.registers["COMMAND2"] = {
            address = 0x0068,
            size = 4,
            value = 0
        }
        p.registers["COMMAND3"] = {
            address = 0x006C,
            size = 4,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x0080,
            size = 4,
            value = 0
        }
        self.peripherals["I2C1"] = {
            base = 0x3FF67000,
            type = "i2c",
            description = "I2C 1",
            registers = {}
        }
        
        local p = self.peripherals["I2C1"]
        p.registers["CTR"] = {
            address = 0x0050,
            size = 4,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x0080,
            size = 4,
            value = 0
        }
        p.registers["COMMAND0"] = {
            address = 0x0060,
            size = 4,
            value = 0
        }
        p.registers["COMMAND1"] = {
            address = 0x0064,
            size = 4,
            value = 0
        }
        self.peripherals["TIMG0"] = {
            base = 0x3FF5F000,
            type = "timer",
            description = "Timer Group 0",
            registers = {}
        }
        
        local p = self.peripherals["TIMG0"]
        p.registers["T0CONFIG"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["T0LO"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["T0HI"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["T0UPDATE"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["T0ALARM"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["T0LOAD"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["T0LOAD_REG"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        self.peripherals["TIMG1"] = {
            base = 0x3FF60000,
            type = "timer",
            description = "Timer Group 1",
            registers = {}
        }
        
        local p = self.peripherals["TIMG1"]
        p.registers["T0CONFIG"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["T0LO"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["T0HI"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["T0ALARM"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["T0LOAD"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        self.peripherals["PWM0"] = {
            base = 0x3FF59000,
            type = "pwm",
            description = "Motor Control PWM 0",
            registers = {}
        }
        
        local p = self.peripherals["PWM0"]
        p.registers["CNT"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["PERIOD"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["DUTY"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["CONFIG0"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["CONFIG1"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["CONFIG2"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["UPDATE"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        self.peripherals["PWM1"] = {
            base = 0x3FF5A000,
            type = "pwm",
            description = "Motor Control PWM 1",
            registers = {}
        }
        
        local p = self.peripherals["PWM1"]
        p.registers["CNT"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["PERIOD"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["DUTY"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        self.peripherals["LEDC"] = {
            base = 0x3FF59000,
            type = "pwm",
            description = "LED PWM Controller",
            registers = {}
        }
        
        local p = self.peripherals["LEDC"]
        p.registers["CONFIG0"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["HPOINT0"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["DUTY0"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["HPOINT1"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["DUTY1"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["HPOINT2"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["DUTY2"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["HPOINT3"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        p.registers["DUTY3"] = {
            address = 0x004C,
            size = 4,
            value = 0
        }
        p.registers["TIMER0_CONF"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TIMER0_LOAD"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        self.peripherals["RTC"] = {
            base = 0x3FF48000,
            type = "rtc",
            description = "RTC Controller",
            registers = {}
        }
        
        local p = self.peripherals["RTC"]
        p.registers["RTC_CNTL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["RTC_TIMER"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["RTC_UPDATE"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["RTC_STATE0"] = {
            address = 0x0080,
            size = 4,
            value = 0
        }
        self.peripherals["WIFI"] = {
            base = 0x3FFAE000,
            type = "wifi",
            description = "Wi-Fi",
            registers = {}
        }
        
        local p = self.peripherals["WIFI"]
        p.registers["MAC"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["CONFIG"] = {
            address = 0x0100,
            size = 4,
            value = 0
        }
        self.peripherals["BT"] = {
            base = 0x3FFB0000,
            type = "bluetooth",
            description = "Bluetooth/BLE",
            registers = {}
        }
        
        local p = self.peripherals["BT"]
        p.registers["CONFIG"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        self.peripherals["SHA"] = {
            base = 0x3FF67000,
            type = "crypto",
            description = "SHA Hardware Accelerator",
            registers = {}
        }
        
        local p = self.peripherals["SHA"]
        p.registers["MODE"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DATA"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["HASH"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        self.peripherals["AES"] = {
            base = 0x3FF68000,
            type = "crypto",
            description = "AES Hardware Accelerator",
            registers = {}
        }
        
        local p = self.peripherals["AES"]
        p.registers["KEY"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DATA_IN"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["DATA_OUT"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["MODE"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        self.peripherals["RNG"] = {
            base = 0x3FF75000,
            type = "rng",
            description = "Random Number Generator",
            registers = {}
        }
        
        local p = self.peripherals["RNG"]
        p.registers["DATA"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        self.peripherals["EFUSE"] = {
            base = 0x3FF5A000,
            type = "efuse",
            description = "eFuse Controller",
            registers = {}
        }
        
        local p = self.peripherals["EFUSE"]
        p.registers["DATA0"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DATA1"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["DATA2"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["DATA3"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
    end
    
    -- 读取寄存器
    function self:read_register(name)
        local reg = self.registers[name]
        if reg then
            return reg.value
        end
        error("寄存器 " .. name .. " 不存在")
    end
    
    -- 写入寄存器
    function self:write_register(name, value)
        local reg = self.registers[name]
        if reg then
            local max_value = bit.lshift(1, reg.size * 8) - 1
            if value < 0 or value > max_value then
                error("值 " .. value .. " 超出范围 [0, " .. max_value .. "]")
            end
            reg.value = value
        else
            error("寄存器 " .. name .. " 不存在")
        end
    end
    
    -- 设置位
    function self:set_bit(register_name, bit, value)
        local reg = self.registers[register_name]
        if reg then
            if value then
                reg.value = bit.bor(reg.value, bit.lshift(1, bit))
            else
                reg.value = bit.band(reg.value, bit.bnot(bit.lshift(1, bit)))
            end
        else
            error("寄存器 " .. register_name .. " 不存在")
        end
    end
    
    -- 获取位
    function self:get_bit(register_name, bit)
        local reg = self.registers[register_name]
        if reg then
            return bit.band(bit.rshift(reg.value, bit), 1) == 1
        end
        error("寄存器 " .. register_name .. " 不存在")
    end
    
    -- 获取设备信息
    function self:get_device_info()
        return {
            name = ESP32_WROOM_32.DEVICE_NAME,
            manufacturer = ESP32_WROOM_32.MANUFACTURER,
            family = ESP32_WROOM_32.FAMILY,
            version = ESP32_WROOM_32.VERSION,
            architecture = ESP32_WROOM_32.ARCHITECTURE,
            bits = ESP32_WROOM_32.BITS,
            clock_frequency = ESP32_WROOM_32.CLOCK_FREQUENCY
        }
    end
    
    -- 获取寄存器信息
    function self:get_register_info(name)
        return self.registers[name]
    end
    
    -- 获取外设信息
    function self:get_peripheral_info(name)
        return self.peripherals[name]
    end
    
    -- 重置设备
    function self:reset()
        for _, reg in pairs(self.registers) do
            reg.value = 0
        end
        
        for _, peripheral in pairs(self.peripherals) do
            for _, reg in pairs(peripheral.registers) do
                reg.value = 0
            end
        end
    end
    
    -- 字符串表示
    function self:__tostring()
        local info = self:get_device_info()
        return string.format("ESP32_WROOM_32(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function ESP32_WROOM_32.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function ESP32_WROOM_32.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function ESP32_WROOM_32.print_device_info(device)
    device = device or ESP32_WROOM_32.new()
    local info = device:get_device_info()
    
    print("设备信息:")
    print("  名称: " .. info.name)
    print("  厂商: " .. info.manufacturer)
    print("  系列: " .. info.family)
    print("  版本: " .. info.version)
    print("  架构: " .. info.architecture)
    print("  位宽: " .. info.bits)
    print("  时钟: " .. info.clock_frequency .. " Hz")
end

function ESP32_WROOM_32.print_registers(device)
    device = device or ESP32_WROOM_32.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            ESP32_WROOM_32.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function ESP32_WROOM_32.example()
    print("=== ESP32-WROOM-32设备示例 ===")
    
    -- 创建设备实例
    local device = ESP32_WROOM_32.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    ESP32_WROOM_32.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["PC"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("PC", 0x55)
        print("写入 PC: " .. ESP32_WROOM_32.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("PC")
        print("读取 PC: " .. ESP32_WROOM_32.hex(value))
        
        -- 位操作
        device:set_bit("PC", 0, true)
        local bit0 = device:get_bit("PC", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    ESP32_WROOM_32.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("ESP32_WROOM_32.lua$") then
    ESP32_WROOM_32.example()
end

return ESP32_WROOM_32

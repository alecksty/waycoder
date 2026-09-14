"""
ESP32-WROOM-32设备定义 - Python模块
生成自: Espressif/ESP32/ESP32-WROOM-32
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: Dual-core Xtensa LX6 Wi-Fi and Bluetooth/BLE SoC with 4MB Flash
CPU架构: Xtensa-LX6
位宽: 32位
时钟频率: 160000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class ESP32_WROOM_32:
    """ESP32-WROOM-32设备类"""

    # 设备信息
    DEVICE_NAME = "ESP32-WROOM-32"
    MANUFACTURER = "Espressif"
    FAMILY = "ESP32"
    VERSION = "1.0"
    ARCHITECTURE = "Xtensa-LX6"
    BITS = 32
    CLOCK_FREQUENCY = 160000000

    # 寄存器地址定义
    PC_ADDR = 0x00000000  # Program Counter
    A0_ADDR = 0x00000004  # General Purpose Register 0
    A1_ADDR = 0x00000008  # Stack Pointer
    A2_ADDR = 0x0000000C  # General Purpose Register 2
    A3_ADDR = 0x00000010  # General Purpose Register 3
    A4_ADDR = 0x00000014  # General Purpose Register 4
    A5_ADDR = 0x00000018  # General Purpose Register 5
    A6_ADDR = 0x0000001C  # General Purpose Register 6
    A7_ADDR = 0x00000020  # General Purpose Register 7
    A8_ADDR = 0x00000024  # General Purpose Register 8
    A9_ADDR = 0x00000028  # General Purpose Register 9
    A10_ADDR = 0x0000002C  # General Purpose Register 10
    A11_ADDR = 0x00000030  # General Purpose Register 11
    A12_ADDR = 0x00000034  # General Purpose Register 12
    A13_ADDR = 0x00000038  # General Purpose Register 13
    A14_ADDR = 0x0000003C  # General Purpose Register 14
    A15_ADDR = 0x00000040  # General Purpose Register 15
    SAREG1_ADDR = 0x00000044  # Special Address Register 1
    SAREG2_ADDR = 0x00000048  # Special Address Register 2
    LBEG_ADDR = 0x0000004C  # Loop Beginning
    LEND_ADDR = 0x00000050  # Loop End
    LCOUNT_ADDR = 0x00000054  # Loop Counter
    PS_ADDR = 0x00000058  # Processor Status
    WINDOWBASE_ADDR = 0x0000005C  # Window Base
    WINDOWSTART_ADDR = 0x00000060  # Window Start
    PTEBASE_ADDR = 0x00000064  # Page Table Base
    PTESIZE_ADDR = 0x00000068  # Page Table Entry Size
    SCOMPARE1_ADDR = 0x0000006C  # Special Compare 1
    ATOMCTL_ADDR = 0x00000070  # Atomic Operation Control
    DDR_ADDR = 0x00000074  # Data Destination Register

    # 内存段定义
    ROM_START = 0x40000000
    ROM_END = 0x4003FFFF
    ROM_SIZE = 262144  # ROM (224KB)
    SRAM_START = 0x3FF00000
    SRAM_END = 0x3FF7FFFF
    SRAM_SIZE = 524288  # SRAM (320KB total)
    DRAM0_START = 0x3FF80000
    DRAM0_END = 0x3FF9FFFF
    DRAM0_SIZE = 131072  # DRAM0 (128KB)
    IRAM0_START = 0x40000000
    IRAM0_END = 0x401FFFFF
    IRAM0_SIZE = 2097152  # IRAM0
    FLASH_START = 0x40200000
    FLASH_END = 0x405FFFFF
    FLASH_SIZE = 4194304  # External Flash (4MB)
    PERIPHERAL_START = 0x3FF00000
    PERIPHERAL_END = 0x3FFBFFFF
    PERIPHERAL_SIZE = 786432  # Peripheral Registers
    GPIO_START = 0x3FF44000
    GPIO_END = 0x3FF44FFF
    GPIO_SIZE = 4096  # GPIO

    # 外设定义
    # GPIO
    GPIO_BASE = 0x3FF44000
    GPIO_OUT_ADDR = 0x0000
    GPIO_OUT_W1TS_ADDR = 0x0008
    GPIO_OUT_W1TC_ADDR = 0x000C
    GPIO_IN_ADDR = 0x003C
    GPIO_STATUS_ADDR = 0x0024
    GPIO_STATUS_W1TS_ADDR = 0x0028
    GPIO_STATUS_W1TC_ADDR = 0x002C
    GPIO_PIN_ADDR = 0x0040
    GPIO_ENABLE_ADDR = 0x0020
    GPIO_STRAP_ADDR = 0x0038
    GPIO_IN_NEXT_ADDR = 0x0044
    # RTC GPIO
    RTC_GPIO_BASE = 0x3FF48000
    RTC_GPIO_OUT_ADDR = 0x0000
    RTC_GPIO_OUT_W1TS_ADDR = 0x0008
    RTC_GPIO_OUT_W1TC_ADDR = 0x000C
    RTC_GPIO_IN_ADDR = 0x0044
    RTC_GPIO_STATUS_ADDR = 0x0024
    RTC_GPIO_PIN_ADDR = 0x0048
    RTC_GPIO_ENABLE_ADDR = 0x0020
    # IO MUX
    IO_MUX_BASE = 0x3FF49000
    IO_MUX_GPIO0_ADDR = 0x0000
    IO_MUX_GPIO1_ADDR = 0x0004
    IO_MUX_GPIO2_ADDR = 0x0008
    IO_MUX_GPIO3_ADDR = 0x000C
    IO_MUX_GPIO4_ADDR = 0x0010
    IO_MUX_GPIO5_ADDR = 0x0014
    IO_MUX_GPIO6_ADDR = 0x0018
    IO_MUX_GPIO7_ADDR = 0x001C
    IO_MUX_GPIO8_ADDR = 0x0020
    IO_MUX_GPIO9_ADDR = 0x0024
    IO_MUX_GPIO10_ADDR = 0x0028
    IO_MUX_GPIO11_ADDR = 0x002C
    IO_MUX_GPIO12_ADDR = 0x0030
    IO_MUX_GPIO13_ADDR = 0x0034
    IO_MUX_GPIO14_ADDR = 0x0038
    IO_MUX_GPIO15_ADDR = 0x003C
    IO_MUX_GPIO16_ADDR = 0x0040
    IO_MUX_GPIO17_ADDR = 0x0044
    IO_MUX_GPIO18_ADDR = 0x0048
    IO_MUX_GPIO19_ADDR = 0x004C
    IO_MUX_GPIO20_ADDR = 0x0050
    IO_MUX_GPIO21_ADDR = 0x0054
    IO_MUX_GPIO22_ADDR = 0x0058
    IO_MUX_GPIO23_ADDR = 0x005C
    IO_MUX_GPIO24_ADDR = 0x0060
    IO_MUX_GPIO25_ADDR = 0x0064
    IO_MUX_GPIO26_ADDR = 0x0068
    IO_MUX_GPIO27_ADDR = 0x006C
    # UART 0
    UART0_BASE = 0x3FF40000
    UART0_FIFO_ADDR = 0x0000
    UART0_INT_RAW_ADDR = 0x0004
    UART0_INT_ST_ADDR = 0x0008
    UART0_INT_ENA_ADDR = 0x000C
    UART0_INT_CLR_ADDR = 0x0010
    UART0_CONF0_ADDR = 0x0020
    UART0_CONF1_ADDR = 0x0024
    UART0_LOWPULSE_ADDR = 0x0028
    UART0_HIGHPULSE_ADDR = 0x002C
    UART0_PULSE_CNT_ADDR = 0x0030
    UART0_DATE_ADDR = 0x0078
    UART0_AHB_BIT_ADDR = 0x007C
    # UART 1
    UART1_BASE = 0x3FF50000
    UART1_FIFO_ADDR = 0x0000
    UART1_INT_RAW_ADDR = 0x0004
    UART1_INT_ST_ADDR = 0x0008
    UART1_INT_ENA_ADDR = 0x000C
    UART1_INT_CLR_ADDR = 0x0010
    UART1_CONF0_ADDR = 0x0020
    UART1_CONF1_ADDR = 0x0024
    # UART 2
    UART2_BASE = 0x3FF6E000
    UART2_FIFO_ADDR = 0x0000
    UART2_INT_RAW_ADDR = 0x0004
    UART2_INT_ST_ADDR = 0x0008
    UART2_INT_ENA_ADDR = 0x000C
    UART2_INT_CLR_ADDR = 0x0010
    UART2_CONF0_ADDR = 0x0020
    UART2_CONF1_ADDR = 0x0024
    # SPI0 (Flash)
    SPI0_BASE = 0x3FF42000
    SPI0_CMD_ADDR = 0x0000
    SPI0_ADDR_ADDR = 0x0004
    SPI0_CONTROL_ADDR = 0x0008
    SPI0_CONTROL1_ADDR = 0x000C
    SPI0_STATUS_ADDR = 0x0010
    SPI0_STATUS1_ADDR = 0x0014
    SPI0_DATA_ADDR = 0x0020
    SPI0_USER_ADDR = 0x003C
    SPI0_USER1_ADDR = 0x0040
    SPI0_USER2_ADDR = 0x0044
    SPI0_PIN_ADDR = 0x0048
    SPI0_SLAVE_ADDR = 0x004C
    SPI0_CACHE_FLASH_ADDR = 0x0050
    SPI0_CLOCK_ADDR = 0x0058
    SPI0_FIFO_ADDR = 0x0060
    # SPI1
    SPI1_BASE = 0x3FF43000
    SPI1_CMD_ADDR = 0x0000
    SPI1_ADDR_ADDR = 0x0004
    SPI1_CONTROL_ADDR = 0x0008
    SPI1_STATUS_ADDR = 0x0010
    SPI1_DATA_ADDR = 0x0020
    SPI1_USER_ADDR = 0x003C
    SPI1_CLOCK_ADDR = 0x0058
    # SPI2 (HSPI)
    SPI2_BASE = 0x3FF64000
    SPI2_CMD_ADDR = 0x0000
    SPI2_ADDR_ADDR = 0x0004
    SPI2_CONTROL_ADDR = 0x0008
    SPI2_STATUS_ADDR = 0x0010
    SPI2_DATA_ADDR = 0x0020
    SPI2_USER_ADDR = 0x003C
    SPI2_CLOCK_ADDR = 0x0058
    SPI2_FIFO_ADDR = 0x0060
    # I2C 0
    I2C0_BASE = 0x3FF53000
    I2C0_SCL_START_ADDR = 0x0000
    I2C0_SCL_LOW_ADDR = 0x0004
    I2C0_SDA_START_ADDR = 0x0008
    I2C0_SDA_LOW_ADDR = 0x000C
    I2C0_INT_ENA_ADDR = 0x0010
    I2C0_INT_CLR_ADDR = 0x0014
    I2C0_INT_RAW_ADDR = 0x0018
    I2C0_INT_STATUS_ADDR = 0x001C
    I2C0_SCL_HIGH_PERIOD_ADDR = 0x0020
    I2C0_SCL_HIGH_PERIOD_S_ADDR = 0x0024
    I2C0_SCL_START_HOLD_ADDR = 0x0028
    I2C0_SDA_START_HOLD_ADDR = 0x002C
    I2C0_SCL_LAST_HOLD_ADDR = 0x0030
    I2C0_SCL_WAIT_PERIOD_ADDR = 0x0034
    I2C0_CTR_ADDR = 0x0050
    I2C0_STATUS_ADDR = 0x0054
    I2C0_FINISH_INT_ENA_ADDR = 0x0058
    I2C0_COMMAND0_ADDR = 0x0060
    I2C0_COMMAND1_ADDR = 0x0064
    I2C0_COMMAND2_ADDR = 0x0068
    I2C0_COMMAND3_ADDR = 0x006C
    I2C0_DATA_ADDR = 0x0080
    # I2C 1
    I2C1_BASE = 0x3FF67000
    I2C1_CTR_ADDR = 0x0050
    I2C1_DATA_ADDR = 0x0080
    I2C1_COMMAND0_ADDR = 0x0060
    I2C1_COMMAND1_ADDR = 0x0064
    # Timer Group 0
    TIMG0_BASE = 0x3FF5F000
    TIMG0_T0CONFIG_ADDR = 0x0000
    TIMG0_T0LO_ADDR = 0x0004
    TIMG0_T0HI_ADDR = 0x0008
    TIMG0_T0UPDATE_ADDR = 0x000C
    TIMG0_T0ALARM_ADDR = 0x0010
    TIMG0_T0LOAD_ADDR = 0x0014
    TIMG0_T0LOAD_REG_ADDR = 0x0018
    # Timer Group 1
    TIMG1_BASE = 0x3FF60000
    TIMG1_T0CONFIG_ADDR = 0x0000
    TIMG1_T0LO_ADDR = 0x0004
    TIMG1_T0HI_ADDR = 0x0008
    TIMG1_T0ALARM_ADDR = 0x0010
    TIMG1_T0LOAD_ADDR = 0x0014
    # Motor Control PWM 0
    PWM0_BASE = 0x3FF59000
    PWM0_CNT_ADDR = 0x0000
    PWM0_PERIOD_ADDR = 0x0004
    PWM0_DUTY_ADDR = 0x0008
    PWM0_CONFIG0_ADDR = 0x0010
    PWM0_CONFIG1_ADDR = 0x0014
    PWM0_CONFIG2_ADDR = 0x0018
    PWM0_UPDATE_ADDR = 0x0020
    # Motor Control PWM 1
    PWM1_BASE = 0x3FF5A000
    PWM1_CNT_ADDR = 0x0000
    PWM1_PERIOD_ADDR = 0x0004
    PWM1_DUTY_ADDR = 0x0008
    # LED PWM Controller
    LEDC_BASE = 0x3FF59000
    LEDC_CONFIG0_ADDR = 0x0000
    LEDC_HPOINT0_ADDR = 0x0018
    LEDC_DUTY0_ADDR = 0x001C
    LEDC_HPOINT1_ADDR = 0x0028
    LEDC_DUTY1_ADDR = 0x002C
    LEDC_HPOINT2_ADDR = 0x0038
    LEDC_DUTY2_ADDR = 0x003C
    LEDC_HPOINT3_ADDR = 0x0048
    LEDC_DUTY3_ADDR = 0x004C
    LEDC_TIMER0_CONF_ADDR = 0x0000
    LEDC_TIMER0_LOAD_ADDR = 0x0004
    # RTC Controller
    RTC_BASE = 0x3FF48000
    RTC_RTC_CNTL_ADDR = 0x0000
    RTC_RTC_TIMER_ADDR = 0x000C
    RTC_RTC_UPDATE_ADDR = 0x0010
    RTC_RTC_STATE0_ADDR = 0x0080
    # Wi-Fi
    WIFI_BASE = 0x3FFAE000
    WIFI_MAC_ADDR = 0x0000
    WIFI_CONFIG_ADDR = 0x0100
    # Bluetooth/BLE
    BT_BASE = 0x3FFB0000
    BT_CONFIG_ADDR = 0x0000
    # SHA Hardware Accelerator
    SHA_BASE = 0x3FF67000
    SHA_MODE_ADDR = 0x0000
    SHA_DATA_ADDR = 0x0004
    SHA_HASH_ADDR = 0x0008
    # AES Hardware Accelerator
    AES_BASE = 0x3FF68000
    AES_KEY_ADDR = 0x0000
    AES_DATA_IN_ADDR = 0x0004
    AES_DATA_OUT_ADDR = 0x0008
    AES_MODE_ADDR = 0x000C
    # Random Number Generator
    RNG_BASE = 0x3FF75000
    RNG_DATA_ADDR = 0x0000
    # eFuse Controller
    EFUSE_BASE = 0x3FF5A000
    EFUSE_DATA0_ADDR = 0x0000
    EFUSE_DATA1_ADDR = 0x0004
    EFUSE_DATA2_ADDR = 0x0008
    EFUSE_DATA3_ADDR = 0x000C

    # 中断向量定义
    INT_NMI = 0  # Non-maskable interrupt
    INT_SYS_SOFT = 1  # Software interrupt
    INT_TIMER_INTR0 = 2  # Hardware timer 0
    INT_TIMER_INTR1 = 3  # Hardware timer 1
    INT_TIMER_INTR2 = 4  # Hardware timer 2
    INT_TIMER_GROUP0 = 5  # TG0 interrupt
    INT_TIMER_GROUP1 = 6  # TG1 interrupt
    INT_GPIO = 7  # GPIO interrupt
    INT_GPIO_NMI = 8  # GPIO NMI interrupt
    INT_SPI0 = 9  # SPI0 interrupt
    INT_SPI1 = 10  # SPI1 interrupt
    INT_SPI2 = 11  # SPI2 interrupt
    INT_I2C0 = 12  # I2C0 interrupt
    INT_I2C1 = 13  # I2C1 interrupt
    INT_UART0 = 14  # UART0 interrupt
    INT_UART1 = 15  # UART1 interrupt
    INT_UART2 = 16  # UART2 interrupt
    INT_WDT = 17  # Watchdog interrupt
    INT_RTC = 18  # RTC interrupt
    INT_PWM0 = 19  # PWM0 interrupt
    INT_PWM1 = 20  # PWM1 interrupt
    INT_LEDC = 21  # LEDC interrupt
    INT_TOUCH = 22  # Touch sensor interrupt
    INT_SARADC = 23  # SARADC interrupt
    INT_MAX = 24  # No. of CPU interrupts
    INT_CORE_INTR0 = 25  # Core 0 interrupt 0
    INT_CORE_INTR1 = 26  # Core 0 interrupt 1
    INT_CORE_INTR2 = 27  # Core 0 interrupt 2
    INT_CORE_INTR3 = 28  # Core 0 interrupt 3
    INT_CORE_INTR4 = 29  # Core 0 interrupt 4
    INT_CORE_INTR5 = 30  # Core 0 interrupt 5
    INT_CORE_INTR6 = 31  # Core 0 interrupt 6
    INT_GPIO_INTERRUPT = 32  # GPIO interrupt
    INT_GPIO_INTERRUPT_NMI = 33  # GPIO NMI interrupt

    # 引脚定义
    PIN_VDD = 1  # 3.3V Power Supply
    PIN_EN = 2  # Enable ( CHIP_PU )
    PIN_SENSOR_VP = 3  # GPIO36 - ADC1_CH0 - SENSOR_VP
    PIN_SENSOR_VN = 4  # GPIO37 - ADC1_CH1 - SENSOR_VN
    PIN_IO34 = 5  # GPIO34 - ADC1_CH6
    PIN_IO35 = 6  # GPIO35 - ADC1_CH7
    PIN_IO32 = 7  # GPIO32 - ADC1_CH4 - TOUCH_CH9
    PIN_IO33 = 8  # GPIO33 - ADC1_CH5 - TOUCH_CH8
    PIN_IO25 = 9  # GPIO25 - DAC1 - ADC2_CH8
    PIN_IO26 = 10  # GPIO26 - DAC2 - ADC2_CH9
    PIN_IO27 = 11  # GPIO27 - TOUCH_CH7 - ADC2_CH7
    PIN_IO14 = 12  # GPIO14 - ADC2_CH6 - TOUCH_CH6 - HSPI CLK
    PIN_IO12 = 13  # GPIO12 - ADC2_CH5 - TOUCH_CH5 - HSPI Q
    PIN_GND = 14  # Ground
    PIN_IO13 = 15  # GPIO13 - ADC2_CH4 - TOUCH_CH4 - HSPI D
    PIN_SD2 = 16  # GPIO9 - SD_DATA2
    PIN_SD3 = 17  # GPIO10 - SD_DATA3
    PIN_CMD = 18  # GPIO11 - SD_CMD
    PIN_CLK = 19  # GPIO6 - SD_CLK
    PIN_SD0 = 20  # GPIO7 - SD_DATA0
    PIN_SD1 = 21  # GPIO8 - SD_DATA1
    PIN_IO15 = 22  # GPIO15 - ADC2_CH3 - TOUCH_CH3 - VSPID
    PIN_IO2 = 23  # GPIO2 - ADC2_CH2 - TOUCH_CH2 - I2C SDA
    PIN_IO0 = 24  # GPIO0 - ADC2_CH1 - TOUCH_CH0 - I2C SCL
    PIN_IO4 = 25  # GPIO4 - ADC2_CH0 - TOUCH_CH1
    PIN_IO16 = 26  # GPIO16 - HSPI WP
    PIN_IO17 = 27  # GPIO17 - HSPI HD
    PIN_IO5 = 28  # GPIO5 - HSPI CS0
    PIN_IO18 = 29  # GPIO18 - VSPICLK
    PIN_IO19 = 30  # GPIO19 - VSPIQ
    PIN_NC = 31  # Not Connected
    PIN_IO21 = 32  # GPIO21
    PIN_RXD0 = 33  # GPIO3 - U0RXD
    PIN_TXD0 = 34  # GPIO1 - U0TXD
    PIN_IO22 = 35  # GPIO22
    PIN_IO23 = 36  # GPIO23 - VSPID
    PIN_GND = 37  # Ground
    PIN_GND = 38  # Ground

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["PC"] = {
            "address": 0x00000000,
            "size": 4,
            "type": "uint32",
            "access": "r",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["A0"] = {
            "address": 0x00000004,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 0",
            "value": 0
        }
        self._registers["A1"] = {
            "address": 0x00000008,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["A2"] = {
            "address": 0x0000000C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 2",
            "value": 0
        }
        self._registers["A3"] = {
            "address": 0x00000010,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 3",
            "value": 0
        }
        self._registers["A4"] = {
            "address": 0x00000014,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 4",
            "value": 0
        }
        self._registers["A5"] = {
            "address": 0x00000018,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 5",
            "value": 0
        }
        self._registers["A6"] = {
            "address": 0x0000001C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 6",
            "value": 0
        }
        self._registers["A7"] = {
            "address": 0x00000020,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 7",
            "value": 0
        }
        self._registers["A8"] = {
            "address": 0x00000024,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 8",
            "value": 0
        }
        self._registers["A9"] = {
            "address": 0x00000028,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 9",
            "value": 0
        }
        self._registers["A10"] = {
            "address": 0x0000002C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 10",
            "value": 0
        }
        self._registers["A11"] = {
            "address": 0x00000030,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 11",
            "value": 0
        }
        self._registers["A12"] = {
            "address": 0x00000034,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 12",
            "value": 0
        }
        self._registers["A13"] = {
            "address": 0x00000038,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 13",
            "value": 0
        }
        self._registers["A14"] = {
            "address": 0x0000003C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 14",
            "value": 0
        }
        self._registers["A15"] = {
            "address": 0x00000040,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 15",
            "value": 0
        }
        self._registers["SAREG1"] = {
            "address": 0x00000044,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Special Address Register 1",
            "value": 0
        }
        self._registers["SAREG2"] = {
            "address": 0x00000048,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Special Address Register 2",
            "value": 0
        }
        self._registers["LBEG"] = {
            "address": 0x0000004C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Loop Beginning",
            "value": 0
        }
        self._registers["LEND"] = {
            "address": 0x00000050,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Loop End",
            "value": 0
        }
        self._registers["LCOUNT"] = {
            "address": 0x00000054,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Loop Counter",
            "value": 0
        }
        self._registers["PS"] = {
            "address": 0x00000058,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Processor Status",
            "value": 0
        }
        self._registers["WINDOWBASE"] = {
            "address": 0x0000005C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Window Base",
            "value": 0
        }
        self._registers["WINDOWSTART"] = {
            "address": 0x00000060,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Window Start",
            "value": 0
        }
        self._registers["PTEBASE"] = {
            "address": 0x00000064,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Page Table Base",
            "value": 0
        }
        self._registers["PTESIZE"] = {
            "address": 0x00000068,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Page Table Entry Size",
            "value": 0
        }
        self._registers["SCOMPARE1"] = {
            "address": 0x0000006C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Special Compare 1",
            "value": 0
        }
        self._registers["ATOMCTL"] = {
            "address": 0x00000070,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Atomic Operation Control",
            "value": 0
        }
        self._registers["DDR"] = {
            "address": 0x00000074,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Destination Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["GPIO"] = {
            "base": 0x3FF44000,
            "type": "gpio",
            "description": "GPIO",
            "registers": {
                "OUT": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUT_W1TS": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUT_W1TC": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IN": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS_W1TS": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS_W1TC": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ENABLE": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STRAP": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IN_NEXT": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RTC_GPIO"] = {
            "base": 0x3FF48000,
            "type": "gpio",
            "description": "RTC GPIO",
            "registers": {
                "OUT": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUT_W1TS": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUT_W1TC": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IN": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ENABLE": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["IO_MUX"] = {
            "base": 0x3FF49000,
            "type": "gpio",
            "description": "IO MUX",
            "registers": {
                "GPIO0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO2": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO3": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO4": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO5": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO6": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO7": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO8": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO9": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO10": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO11": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO12": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO13": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO14": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO15": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO16": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO17": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO18": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO19": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO20": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO21": {
                    "address": 0x0054,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO22": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO23": {
                    "address": 0x005C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO24": {
                    "address": 0x0060,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO25": {
                    "address": 0x0064,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO26": {
                    "address": 0x0068,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO27": {
                    "address": 0x006C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART0"] = {
            "base": 0x3FF40000,
            "type": "uart",
            "description": "UART 0",
            "registers": {
                "FIFO": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_RAW": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_ST": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_ENA": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_CLR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONF0": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONF1": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LOWPULSE": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HIGHPULSE": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PULSE_CNT": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATE": {
                    "address": 0x0078,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AHB_BIT": {
                    "address": 0x007C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART1"] = {
            "base": 0x3FF50000,
            "type": "uart",
            "description": "UART 1",
            "registers": {
                "FIFO": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_RAW": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_ST": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_ENA": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_CLR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONF0": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONF1": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART2"] = {
            "base": 0x3FF6E000,
            "type": "uart",
            "description": "UART 2",
            "registers": {
                "FIFO": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_RAW": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_ST": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_ENA": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_CLR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONF0": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONF1": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI0"] = {
            "base": 0x3FF42000,
            "type": "spi",
            "description": "SPI0 (Flash)",
            "registers": {
                "CMD": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADDR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONTROL": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONTROL1": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS1": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USER": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USER1": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USER2": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SLAVE": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CACHE_FLASH": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLOCK": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FIFO": {
                    "address": 0x0060,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI1"] = {
            "base": 0x3FF43000,
            "type": "spi",
            "description": "SPI1",
            "registers": {
                "CMD": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADDR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONTROL": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USER": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLOCK": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI2"] = {
            "base": 0x3FF64000,
            "type": "spi",
            "description": "SPI2 (HSPI)",
            "registers": {
                "CMD": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADDR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONTROL": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USER": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLOCK": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FIFO": {
                    "address": 0x0060,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C0"] = {
            "base": 0x3FF53000,
            "type": "i2c",
            "description": "I2C 0",
            "registers": {
                "SCL_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SCL_LOW": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SDA_START": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SDA_LOW": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_ENA": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_CLR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_RAW": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INT_STATUS": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SCL_HIGH_PERIOD": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SCL_HIGH_PERIOD_S": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SCL_START_HOLD": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SDA_START_HOLD": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SCL_LAST_HOLD": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SCL_WAIT_PERIOD": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTR": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x0054,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FINISH_INT_ENA": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "COMMAND0": {
                    "address": 0x0060,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "COMMAND1": {
                    "address": 0x0064,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "COMMAND2": {
                    "address": 0x0068,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "COMMAND3": {
                    "address": 0x006C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA": {
                    "address": 0x0080,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C1"] = {
            "base": 0x3FF67000,
            "type": "i2c",
            "description": "I2C 1",
            "registers": {
                "CTR": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA": {
                    "address": 0x0080,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "COMMAND0": {
                    "address": 0x0060,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "COMMAND1": {
                    "address": 0x0064,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMG0"] = {
            "base": 0x3FF5F000,
            "type": "timer",
            "description": "Timer Group 0",
            "registers": {
                "T0CONFIG": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0LO": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0HI": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0UPDATE": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0ALARM": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0LOAD": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0LOAD_REG": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMG1"] = {
            "base": 0x3FF60000,
            "type": "timer",
            "description": "Timer Group 1",
            "registers": {
                "T0CONFIG": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0LO": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0HI": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0ALARM": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "T0LOAD": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWM0"] = {
            "base": 0x3FF59000,
            "type": "pwm",
            "description": "Motor Control PWM 0",
            "registers": {
                "CNT": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PERIOD": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DUTY": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG0": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG1": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG2": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UPDATE": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWM1"] = {
            "base": 0x3FF5A000,
            "type": "pwm",
            "description": "Motor Control PWM 1",
            "registers": {
                "CNT": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PERIOD": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DUTY": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["LEDC"] = {
            "base": 0x3FF59000,
            "type": "pwm",
            "description": "LED PWM Controller",
            "registers": {
                "CONFIG0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HPOINT0": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DUTY0": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HPOINT1": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DUTY1": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HPOINT2": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DUTY2": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HPOINT3": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DUTY3": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMER0_CONF": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMER0_LOAD": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RTC"] = {
            "base": 0x3FF48000,
            "type": "rtc",
            "description": "RTC Controller",
            "registers": {
                "RTC_CNTL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTC_TIMER": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTC_UPDATE": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTC_STATE0": {
                    "address": 0x0080,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["WIFI"] = {
            "base": 0x3FFAE000,
            "type": "wifi",
            "description": "Wi-Fi",
            "registers": {
                "MAC": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["BT"] = {
            "base": 0x3FFB0000,
            "type": "bluetooth",
            "description": "Bluetooth/BLE",
            "registers": {
                "CONFIG": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SHA"] = {
            "base": 0x3FF67000,
            "type": "crypto",
            "description": "SHA Hardware Accelerator",
            "registers": {
                "MODE": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HASH": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["AES"] = {
            "base": 0x3FF68000,
            "type": "crypto",
            "description": "AES Hardware Accelerator",
            "registers": {
                "KEY": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA_IN": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA_OUT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MODE": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RNG"] = {
            "base": 0x3FF75000,
            "type": "rng",
            "description": "Random Number Generator",
            "registers": {
                "DATA": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["EFUSE"] = {
            "base": 0x3FF5A000,
            "type": "efuse",
            "description": "eFuse Controller",
            "registers": {
                "DATA0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA2": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA3": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }

    def read_register(self, name: str) -> int:
        """读取寄存器值"""
        if name in self._registers:
            return self._registers[name]["value"]
        raise KeyError(f"寄存器 {name} 不存在")

    def write_register(self, name: str, value: int):
        """写入寄存器值"""
        if name in self._registers:
            reg = self._registers[name]
            max_value = (1 << (reg["size"] * 8)) - 1
            if value < 0 or value > max_value:
                raise ValueError(f"值 {value} 超出范围 [0, {max_value}]")
            reg["value"] = value
        else:
            raise KeyError(f"寄存器 {name} 不存在")

    def set_bit(self, register_name: str, bit: int, value: bool):
        """设置寄存器位"""
        if register_name in self._registers:
            reg = self._registers[register_name]
            if value:
                reg["value"] |= (1 << bit)
            else:
                reg["value"] &= ~(1 << bit)
        else:
            raise KeyError(f"寄存器 {register_name} 不存在")

    def get_bit(self, register_name: str, bit: int) -> bool:
        """获取寄存器位"""
        if register_name in self._registers:
            reg = self._registers[register_name]
            return (reg["value"] >> bit) & 1 == 1
        raise KeyError(f"寄存器 {register_name} 不存在")

    def get_device_info(self) -> dict:
        """获取设备信息"""
        return {
            "name": self.DEVICE_NAME,
            "manufacturer": self.MANUFACTURER,
            "family": self.FAMILY,
            "version": self.VERSION,
            "architecture": self.ARCHITECTURE,
            "bits": self.BITS,
            "clock_frequency": self.CLOCK_FREQUENCY
        }

    def get_register_info(self, name: str) -> Optional[dict]:
        """获取寄存器信息"""
        return self._registers.get(name)

    def get_peripheral_info(self, name: str) -> Optional[dict]:
        """获取外设信息"""
        return self._peripherals.get(name)

    def reset(self):
        """重置设备"""
        for reg in self._registers.values():
            reg["value"] = 0
        for peripheral in self._peripherals.values():
            for reg in peripheral["registers"].values():
                reg["value"] = 0

    def __str__(self) -> str:
        """字符串表示"""
        info = self.get_device_info()
        return f"ESP32_WROOM_32({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = ESP32_WROOM_32()
    print(f"设备: {device}")
    print(f"设备信息: {device.get_device_info()}")
    print()
    
    # 演示寄存器操作
    if device.Cpu.Registers.RegisterList.Count > 0:
        first_reg = device.Cpu.Registers.RegisterList[0].Name
        print(f"第一个寄存器: {first_reg}")
        device.write_register(first_reg, 0x55)
        value = device.read_register(first_reg)
        print(f"读取值: 0x{value:X}")

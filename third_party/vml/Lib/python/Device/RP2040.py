"""
RP2040设备定义 - Python模块
生成自: Raspberry Pi/RP/RP2040
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: Dual-core ARM Cortex-M0+ up to 133MHz with 264KB SRAM
CPU架构: ARM-Cortex-M0+
位宽: 32位
时钟频率: 12000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class RP2040:
    """RP2040设备类"""

    # 设备信息
    DEVICE_NAME = "RP2040"
    MANUFACTURER = "Raspberry Pi"
    FAMILY = "RP"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M0+"
    BITS = 32
    CLOCK_FREQUENCY = 12000000

    # 寄存器地址定义
    R0_ADDR = 0x00000000  # General Purpose Register 0
    R1_ADDR = 0x00000004  # General Purpose Register 1
    R2_ADDR = 0x00000008  # General Purpose Register 2
    R3_ADDR = 0x0000000C  # General Purpose Register 3
    R4_ADDR = 0x00000010  # General Purpose Register 4
    R5_ADDR = 0x00000014  # General Purpose Register 5
    R6_ADDR = 0x00000018  # General Purpose Register 6
    R7_ADDR = 0x0000001C  # General Purpose Register 7
    R8_ADDR = 0x00000020  # General Purpose Register 8
    R9_ADDR = 0x00000024  # General Purpose Register 9
    R10_ADDR = 0x00000028  # General Purpose Register 10
    R11_ADDR = 0x0000002C  # General Purpose Register 11
    R12_ADDR = 0x00000030  # General Purpose Register 12
    SP_ADDR = 0x00000034  # Stack Pointer
    LR_ADDR = 0x00000038  # Link Register
    PC_ADDR = 0x0000003C  # Program Counter
    XPSR_ADDR = 0x00000040  # Program Status Register
    XPSR_N_BIT = 31  # Negative Flag
    XPSR_Z_BIT = 30  # Zero Flag
    XPSR_C_BIT = 29  # Carry Flag
    XPSR_V_BIT = 28  # Overflow Flag
    XPSR_Q_BIT = 27  # Saturation Flag
    XPSR_ICI_BIT = 0  # ICI execution state
    XPSR_IT_BIT = 0  # If-Then execution state
    XPSR_T_BIT = 24  # Thumb bit
    XPSR_IPSR_BIT = 0  # Exception number
    PRIMASK_ADDR = 0xE0000E20  # Priority Mask Register
    CONTROL_ADDR = 0xE0000E24  # Control Register
    FAULTMASK_ADDR = 0xE0000E28  # Fault Mask Register

    # 内存段定义
    ROM_START = 0x00000000
    ROM_END = 0x00001000
    ROM_SIZE = 4096  # ROM (bootloader)
    SRAM0_START = 0x20000000
    SRAM0_END = 0x20003FFF
    SRAM0_SIZE = 16384  # SRAM0 (16KB)
    SRAM1_START = 0x20004000
    SRAM1_END = 0x20007FFF
    SRAM1_SIZE = 16384  # SRAM1 (16KB)
    SRAM2_START = 0x20008000
    SRAM2_END = 0x2000BFFF
    SRAM2_SIZE = 16384  # SRAM2 (16KB)
    SRAM3_START = 0x2000C000
    SRAM3_END = 0x2000FFFF
    SRAM3_SIZE = 16384  # SRAM3 (16KB)
    SRAM4_START = 0x20010000
    SRAM4_END = 0x20013FFF
    SRAM4_SIZE = 16384  # SRAM4 (16KB)
    APB_START = 0x40000000
    APB_END = 0x400FFFFF
    APB_SIZE = 1048576  # APB Peripherals
    AHB_START = 0x50000000
    AHB_END = 0x500FFFFF
    AHB_SIZE = 1048576  # AHB Peripherals

    # 外设定义
    # IO Bank 0
    IO_BANK0_BASE = 0x40014000
    IO_BANK0_GPIO0_STATUS_ADDR = 0x0000
    IO_BANK0_GPIO0_CTRL_ADDR = 0x0004
    IO_BANK0_GPIO1_STATUS_ADDR = 0x0008
    IO_BANK0_GPIO1_CTRL_ADDR = 0x000C
    IO_BANK0_GPIO2_STATUS_ADDR = 0x0010
    IO_BANK0_GPIO2_CTRL_ADDR = 0x0014
    IO_BANK0_GPIO3_STATUS_ADDR = 0x0018
    IO_BANK0_GPIO3_CTRL_ADDR = 0x001C
    IO_BANK0_GPIO4_STATUS_ADDR = 0x0020
    IO_BANK0_GPIO4_CTRL_ADDR = 0x0024
    IO_BANK0_GPIO5_STATUS_ADDR = 0x0028
    IO_BANK0_GPIO5_CTRL_ADDR = 0x002C
    IO_BANK0_GPIO6_STATUS_ADDR = 0x0030
    IO_BANK0_GPIO6_CTRL_ADDR = 0x0034
    IO_BANK0_GPIO7_STATUS_ADDR = 0x0038
    IO_BANK0_GPIO7_CTRL_ADDR = 0x003C
    IO_BANK0_GPIO8_STATUS_ADDR = 0x0040
    IO_BANK0_GPIO8_CTRL_ADDR = 0x0044
    IO_BANK0_GPIO9_STATUS_ADDR = 0x0048
    IO_BANK0_GPIO9_CTRL_ADDR = 0x004C
    IO_BANK0_GPIO10_STATUS_ADDR = 0x0050
    IO_BANK0_GPIO10_CTRL_ADDR = 0x0054
    IO_BANK0_GPIO11_STATUS_ADDR = 0x0058
    IO_BANK0_GPIO11_CTRL_ADDR = 0x005C
    IO_BANK0_GPIO12_STATUS_ADDR = 0x0060
    IO_BANK0_GPIO12_CTRL_ADDR = 0x0064
    IO_BANK0_GPIO13_STATUS_ADDR = 0x0068
    IO_BANK0_GPIO13_CTRL_ADDR = 0x006C
    IO_BANK0_GPIO14_STATUS_ADDR = 0x0070
    IO_BANK0_GPIO14_CTRL_ADDR = 0x0074
    IO_BANK0_GPIO15_STATUS_ADDR = 0x0078
    IO_BANK0_GPIO15_CTRL_ADDR = 0x007C
    IO_BANK0_GPIO16_STATUS_ADDR = 0x0080
    IO_BANK0_GPIO16_CTRL_ADDR = 0x0084
    IO_BANK0_GPIO17_STATUS_ADDR = 0x0088
    IO_BANK0_GPIO17_CTRL_ADDR = 0x008C
    IO_BANK0_GPIO18_STATUS_ADDR = 0x0090
    IO_BANK0_GPIO18_CTRL_ADDR = 0x0094
    IO_BANK0_GPIO19_STATUS_ADDR = 0x0098
    IO_BANK0_GPIO19_CTRL_ADDR = 0x009C
    IO_BANK0_GPIO20_STATUS_ADDR = 0x00A0
    IO_BANK0_GPIO20_CTRL_ADDR = 0x00A4
    IO_BANK0_GPIO21_STATUS_ADDR = 0x00A8
    IO_BANK0_GPIO21_CTRL_ADDR = 0x00AC
    IO_BANK0_GPIO22_STATUS_ADDR = 0x00B0
    IO_BANK0_GPIO22_CTRL_ADDR = 0x00B4
    IO_BANK0_GPIO23_STATUS_ADDR = 0x00B8
    IO_BANK0_GPIO23_CTRL_ADDR = 0x00BC
    IO_BANK0_GPIO24_STATUS_ADDR = 0x00C0
    IO_BANK0_GPIO24_CTRL_ADDR = 0x00C4
    IO_BANK0_GPIO25_STATUS_ADDR = 0x00C8
    IO_BANK0_GPIO25_CTRL_ADDR = 0x00CC
    IO_BANK0_GPIO26_STATUS_ADDR = 0x00D0
    IO_BANK0_GPIO26_CTRL_ADDR = 0x00D4
    IO_BANK0_GPIO27_STATUS_ADDR = 0x00D8
    IO_BANK0_GPIO27_CTRL_ADDR = 0x00DC
    IO_BANK0_GPIO28_STATUS_ADDR = 0x00E0
    IO_BANK0_GPIO28_CTRL_ADDR = 0x00E4
    IO_BANK0_GPIO29_STATUS_ADDR = 0x00E8
    IO_BANK0_GPIO29_CTRL_ADDR = 0x00EC
    IO_BANK0_INTR_ADDR = 0x00F0
    IO_BANK0_PROC0_INTE_ADDR = 0x00F4
    IO_BANK0_PROC1_INTE_ADDR = 0x00F8
    IO_BANK0_PROC0_INTF_ADDR = 0x00FC
    IO_BANK0_PROC1_INTF_ADDR = 0x0100
    IO_BANK0_PROC0_INTS_ADDR = 0x0104
    IO_BANK0_PROC1_INTS_ADDR = 0x0108
    IO_BANK0_DORMANT_WAKE_INTE_ADDR = 0x010C
    IO_BANK0_DORMANT_WAKE_INTF_ADDR = 0x0110
    IO_BANK0_DORMANT_WAKE_INTS_ADDR = 0x0114
    # Pads
    PADS_BASE = 0x4001E000
    PADS_GPIO_VOLT_ADDR = 0x00E0
    # SIO (Single-cycle I/O)
    SIO_BASE = 0xD0000000
    SIO_CPUID_ADDR = 0x0000
    SIO_GPIO_OUT_ADDR = 0x0004
    SIO_GPIO_OUT_SET_ADDR = 0x0008
    SIO_GPIO_OUT_CLR_ADDR = 0x000C
    SIO_GPIO_OUT_XOR_ADDR = 0x0010
    SIO_GPIO_OE_ADDR = 0x0014
    SIO_GPIO_OE_SET_ADDR = 0x0018
    SIO_GPIO_OE_CLR_ADDR = 0x001C
    SIO_GPIO_OE_XOR_ADDR = 0x0020
    SIO_GPIO_IN_ADDR = 0x0024
    SIO_FIFO_ST_ADDR = 0x0040
    SIO_FIFO_WR_ADDR = 0x0044
    SIO_FIFO_RD_ADDR = 0x0048
    SIO_SPINLOCK_ST_ADDR = 0x004C
    SIO_INTERRUPT_ST_ADDR = 0x0050
    # UART0
    UART0_BASE = 0x40034000
    UART0_UARTDR_ADDR = 0x0000
    UART0_UARTRSR_ADDR = 0x0004
    UART0_UARTECR_ADDR = 0x0004
    UART0_UARTFR_ADDR = 0x0018
    UART0_UARTILPR_ADDR = 0x0020
    UART0_UARTIBRD_ADDR = 0x0024
    UART0_UARTFBRD_ADDR = 0x0028
    UART0_UARTLCR_H_ADDR = 0x002C
    UART0_UARTCR_ADDR = 0x0030
    UART0_UARTIFLS_ADDR = 0x0034
    UART0_UARTIMSC_ADDR = 0x0038
    UART0_UARTRIS_ADDR = 0x003C
    UART0_UARTMIS_ADDR = 0x0040
    UART0_UARTICR_ADDR = 0x0044
    UART0_UARTDMACR_ADDR = 0x0048
    # UART1
    UART1_BASE = 0x40038000
    UART1_UARTDR_ADDR = 0x0000
    UART1_UARTRSR_ADDR = 0x0004
    UART1_UARTFR_ADDR = 0x0018
    UART1_UARTIBRD_ADDR = 0x0024
    UART1_UARTFBRD_ADDR = 0x0028
    UART1_UARTLCR_H_ADDR = 0x002C
    UART1_UARTCR_ADDR = 0x0030
    UART1_UARTIFLS_ADDR = 0x0034
    UART1_UARTIMSC_ADDR = 0x0038
    UART1_UARTICR_ADDR = 0x0044
    # SPI0
    SPI0_BASE = 0x4003C000
    SPI0_SSPCR0_ADDR = 0x0000
    SPI0_SSPCR1_ADDR = 0x0004
    SPI0_SSPDR_ADDR = 0x0008
    SPI0_SSPSR_ADDR = 0x000C
    SPI0_SSPCPSR_ADDR = 0x0010
    SPI0_SSPIMSC_ADDR = 0x0014
    SPI0_SSPRIS_ADDR = 0x0018
    SPI0_SSPMIS_ADDR = 0x001C
    SPI0_SSPICR_ADDR = 0x0020
    SPI0_SSPDMACR_ADDR = 0x0024
    # SPI1
    SPI1_BASE = 0x4003C000
    SPI1_SSPCR0_ADDR = 0x0000
    SPI1_SSPCR1_ADDR = 0x0004
    SPI1_SSPDR_ADDR = 0x0008
    SPI1_SSPSR_ADDR = 0x000C
    SPI1_SSPCPSR_ADDR = 0x0010
    SPI1_SSPIMSC_ADDR = 0x0014
    # I2C0
    I2C0_BASE = 0x40044000
    I2C0_IC_CON_ADDR = 0x0000
    I2C0_IC_TAR_ADDR = 0x0004
    I2C0_IC_SAR_ADDR = 0x0008
    I2C0_IC_DATA_CMD_ADDR = 0x0010
    I2C0_IC_SS_SCL_HCNT_ADDR = 0x0014
    I2C0_IC_SS_SCL_LCNT_ADDR = 0x0018
    I2C0_IC_FS_SCL_HCNT_ADDR = 0x001C
    I2C0_IC_FS_SCL_LCNT_ADDR = 0x0020
    I2C0_IC_RAW_INTR_STAT_ADDR = 0x0024
    I2C0_IC_ENABLE_ADDR = 0x002C
    I2C0_IC_STATUS_ADDR = 0x0030
    I2C0_IC_TXFLR_ADDR = 0x0034
    I2C0_IC_RXFLR_ADDR = 0x0038
    I2C0_IC_TX_ABRT_ADDR = 0x003C
    I2C0_IC_DMA_CR_ADDR = 0x0040
    I2C0_IC_DMA_TDLR_ADDR = 0x0044
    I2C0_IC_DMA_RDLR_ADDR = 0x0048
    # I2C1
    I2C1_BASE = 0x40048000
    I2C1_IC_CON_ADDR = 0x0000
    I2C1_IC_TAR_ADDR = 0x0004
    I2C1_IC_ENABLE_ADDR = 0x002C
    I2C1_IC_STATUS_ADDR = 0x0030
    I2C1_IC_TX_ABRT_ADDR = 0x003C
    # PWM0
    PWM0_BASE = 0x40050000
    PWM0_CS_ADDR = 0x0000
    PWM0_CMPR0_ADDR = 0x0004
    PWM0_CMPR1_ADDR = 0x0008
    PWM0_CMPR2_ADDR = 0x000C
    PWM0_CMPR3_ADDR = 0x0010
    PWM0_CC_ADDR = 0x0014
    PWM0_TOP_ADDR = 0x0018
    PWM0_INTR_ADDR = 0x001C
    PWM0_INTE_ADDR = 0x0020
    PWM0_INTF_ADDR = 0x0024
    PWM0_INTS_ADDR = 0x0028
    PWM0_PHS0_ADDR = 0x0034
    PWM0_PHS1_ADDR = 0x0038
    PWM0_PHS2_ADDR = 0x003C
    PWM0_PHS3_ADDR = 0x0040
    PWM0_DIV_ADDR = 0x0044
    PWM0_PHASE_ADDR = 0x0048
    # PWM1
    PWM1_BASE = 0x40051000
    PWM1_CS_ADDR = 0x0000
    PWM1_CMPR0_ADDR = 0x0004
    PWM1_CMPR1_ADDR = 0x0008
    PWM1_CMPR2_ADDR = 0x000C
    PWM1_CMPR3_ADDR = 0x0010
    PWM1_CC_ADDR = 0x0014
    PWM1_TOP_ADDR = 0x0018
    PWM1_DIV_ADDR = 0x0044
    # ADC
    ADC_BASE = 0x4004C000
    ADC_ADC_CS_ADDR = 0x0000
    ADC_ADC_RESULT_ADDR = 0x0004
    ADC_ADC_FCS_ADDR = 0x0008
    ADC_ADC_FIFO_ADDR = 0x000C
    ADC_ADC_TS_ADDR = 0x0010
    ADC_ADC_OFFSET_ADDR = 0x0014
    ADC_ADC_TRIG_ADDR = 0x0018
    # Timer0
    TIMER0_BASE = 0x40054000
    TIMER0_TIMEHW_ADDR = 0x0000
    TIMER0_TIMELW_ADDR = 0x0004
    TIMER0_TIMEHA_ADDR = 0x0008
    TIMER0_TIMELA_ADDR = 0x000C
    TIMER0_TIMERA_ADDR = 0x0010
    TIMER0_TIMERIQ_ADDR = 0x0014
    TIMER0_TIMEREAD_ADDR = 0x0018
    # Timer1
    TIMER1_BASE = 0x40058000
    TIMER1_TIMEHW_ADDR = 0x0000
    TIMER1_TIMELW_ADDR = 0x0004
    TIMER1_TIMEHA_ADDR = 0x0008
    TIMER1_TIMELA_ADDR = 0x000C
    TIMER1_TIMERA_ADDR = 0x0010
    # RTC
    RTC_BASE = 0x4005C000
    RTC_RTC_CLKS_ADDR = 0x0000
    RTC_RTC_SET_ADDR = 0x0004
    RTC_RTC_WR_ADDR = 0x0008
    RTC_RTC_DATE_ADDR = 0x000C
    RTC_RTC_TOTAL_ADDR = 0x0010
    RTC_RTC_HASH_ADDR = 0x0014
    RTC_RTC_RTC_ADDR = 0x0018
    RTC_INTR_ADDR = 0x001C
    RTC_INTE_ADDR = 0x0020
    RTC_INTF_ADDR = 0x0024
    RTC_INTS_ADDR = 0x0028
    # Watchdog
    WATCHDOG_BASE = 0x40060000
    WATCHDOG_WATCHDOG_CTL_ADDR = 0x0000
    WATCHDOG_WATCHDOG_MOD_ADDR = 0x0004
    WATCHDOG_WATCHDOG_FR_ADDR = 0x0008
    WATCHDOG_WATCHDOG_LOAD_ADDR = 0x000C
    # USB
    USB_BASE = 0x50100000
    USB_USB_CTRL_ADDR = 0x0000
    USB_USB_ADDR_ADDR = 0x0004
    USB_USB_PWR_ADDR = 0x0008
    USB_USB_TXFIFO_ADDR = 0x0010
    USB_USB_RXFIFO_ADDR = 0x0014
    USB_USB_TXIE_ADDR = 0x0018
    USB_USB_RXIE_ADDR = 0x001C
    USB_USB_IS_ADDR = 0x0020
    USB_USB_IM_ADDR = 0x0024
    USB_USB_IE_ADDR = 0x0028
    USB_USB_REVO_ADDR = 0x002C
    USB_USB_EP_ADDR = 0x0030
    USB_USB_BUFF_ADDR = 0x0034
    USB_USB_MPS_ADDR = 0x0038
    # PIO0
    PIO0_BASE = 0x50200000
    PIO0_CTRL_ADDR = 0x0000
    PIO0_FSTAT_ADDR = 0x0004
    PIO0_FDEBUG_ADDR = 0x0008
    PIO0_FCTRL_ADDR = 0x000C
    PIO0_RXF0_ADDR = 0x0010
    PIO0_RXF1_ADDR = 0x0014
    PIO0_RXF2_ADDR = 0x0018
    PIO0_RXF3_ADDR = 0x001C
    PIO0_TXF0_ADDR = 0x0020
    PIO0_TXF1_ADDR = 0x0024
    PIO0_TXF2_ADDR = 0x0028
    PIO0_TXF3_ADDR = 0x002C
    PIO0_IRQ_ADDR = 0x0030
    PIO0_IRQ_FORCE_ADDR = 0x0034
    PIO0_IRQ_INTF_ADDR = 0x0038
    PIO0_IRQ_INTS_ADDR = 0x003C
    PIO0_SM0_CLKDIV_ADDR = 0x00C8
    PIO0_SM0_EXECCTRL_ADDR = 0x00CC
    PIO0_SM0_SHIFTCTRL_ADDR = 0x00D0
    PIO0_SM0_ADDR_ADDR = 0x00D4
    PIO0_SM0_INSTR_ADDR = 0x00D8
    PIO0_SM0_PINCTRL_ADDR = 0x00DC
    # PIO1
    PIO1_BASE = 0x50201000
    PIO1_CTRL_ADDR = 0x0000
    PIO1_FSTAT_ADDR = 0x0004
    PIO1_IRQ_ADDR = 0x0030
    PIO1_SM0_CLKDIV_ADDR = 0x00C8
    PIO1_SM0_EXECCTRL_ADDR = 0x00CC
    PIO1_SM0_SHIFTCTRL_ADDR = 0x00D0
    PIO1_SM0_ADDR_ADDR = 0x00D4
    PIO1_SM0_INSTR_ADDR = 0x00D8
    # Clock Manager
    CLOCKS_BASE = 0x40008000
    CLOCKS_CLK_GP0DIV_ADDR = 0x0000
    CLOCKS_CLK_GP0CTRL_ADDR = 0x0004
    CLOCKS_CLK_GP1DIV_ADDR = 0x0008
    CLOCKS_CLK_GP1CTRL_ADDR = 0x000C
    CLOCKS_CLK_GP2DIV_ADDR = 0x0010
    CLOCKS_CLK_GP2CTRL_ADDR = 0x0014
    CLOCKS_CLK_REF_ADDR = 0x001C
    CLOCKS_CLK_SYS_ADDR = 0x0020
    CLOCKS_CLK_PERI_ADDR = 0x0024
    # Crystal Oscillator
    XOSC_BASE = 0x40020000
    XOSC_XOSC_CTRL_ADDR = 0x0000
    XOSC_XOSC_STATUS_ADDR = 0x0004
    XOSC_XOSC_COUNT_ADDR = 0x0008
    # Ring Oscillator
    ROSC_BASE = 0x40010000
    ROSC_ROSC_CTRL_ADDR = 0x0000
    ROSC_ROSC_FREQA_ADDR = 0x0004
    ROSC_ROSC_FREQB_ADDR = 0x0008
    ROSC_ROSC_FREQC_ADDR = 0x000C
    ROSC_ROSC_FREQD_ADDR = 0x0010
    ROSC_ROSC_STATUS_ADDR = 0x0014
    ROSC_ROSC_DR_ADDR = 0x0018
    # System PLL
    PLL_SYS_BASE = 0x40028000
    PLL_SYS_PLL_CS_ADDR = 0x0000
    PLL_SYS_PLL_PWR_ADDR = 0x0004
    PLL_SYS_PLL_FBDIV_ADDR = 0x0008
    PLL_SYS_PLL_PRIMARY_ADDR = 0x000C
    PLL_SYS_PLL_POSTDIV1_ADDR = 0x0010
    PLL_SYS_PLL_POSTDIV2_ADDR = 0x0014
    # USB PLL
    PLL_USB_BASE = 0x4002C000
    PLL_USB_PLL_CS_ADDR = 0x0000
    PLL_USB_PLL_PWR_ADDR = 0x0004
    PLL_USB_PLL_FBDIV_ADDR = 0x0008
    PLL_USB_PLL_PRIMARY_ADDR = 0x000C
    # Resets
    RESETS_BASE = 0x4000C000
    RESETS_RESET_ADDR = 0x0000
    RESETS_RESET_DONE_ADDR = 0x0004
    RESETS_WD_RESET_ADDR = 0x0008

    # 中断向量定义
    INT_RESERVED = 0  # Reserved
    INT_TIMER0_IRQ_0 = 1  # Timer 0 IRQ 0
    INT_TIMER0_IRQ_1 = 2  # Timer 0 IRQ 1
    INT_TIMER1_IRQ_0 = 3  # Timer 1 IRQ 0
    INT_TIMER1_IRQ_1 = 4  # Timer 1 IRQ 1
    INT_TIMER2_IRQ_0 = 5  # Timer 2 IRQ 0
    INT_TIMER2_IRQ_1 = 6  # Timer 2 IRQ 1
    INT_TIMER3_IRQ_0 = 7  # Timer 3 IRQ 0
    INT_TIMER3_IRQ_1 = 8  # Timer 3 IRQ 1
    INT_PWM_IRQ_WRAP = 9  # PWM IRQ wrap
    INT_USB_CTRL_IRQ = 10  # USB ctrl IRQ
    INT_USB_DMA_IRQ = 11  # USB dma IRQ
    INT_USB_VBUS_DETECT = 12  # USB VBUS detect IRQ
    INT_USB_RESUME_IRQ = 13  # USB resume IRQ
    INT_ADC_IRQ_FIFO = 14  # ADC IRQ FIFO
    INT_ADC_IRQ_TRIGGER = 15  # ADC IRQ trigger
    INT_I2C0_IRQ = 16  # I2C 0 IRQ
    INT_I2C1_IRQ = 17  # I2C 1 IRQ
    INT_SPI0_IRQ = 18  # SPI 0 IRQ
    INT_SPI1_IRQ = 19  # SPI 1 IRQ
    INT_UART0_IRQ = 20  # UART 0 IRQ
    INT_UART0_IRQ_TX = 21  # UART 0 IRQ TX
    INT_UART1_IRQ = 22  # UART 1 IRQ
    INT_UART1_IRQ_TX = 23  # UART 1 IRQ TX
    INT_PIO0_IRQ_0 = 24  # PIO 0 IRQ 0
    INT_PIO0_IRQ_1 = 25  # PIO 0 IRQ 1
    INT_PIO1_IRQ_0 = 26  # PIO 1 IRQ 0
    INT_PIO1_IRQ_1 = 27  # PIO 1 IRQ 1
    INT_RTC_IRQ = 28  # RTC IRQ

    # 引脚定义
    PIN_GP0 = 1  # UART0 TX / GP0
    PIN_GP1 = 2  # UART0 RX / GP1
    PIN_GP2 = 3  # SPI0 TX / GP2
    PIN_GP3 = 4  # SPI0 RX / GP3
    PIN_GP4 = 5  # SPI0 CSn / GP4
    PIN_GP5 = 6  # SPI0 SCK / GP5
    PIN_GP6 = 7  # PWM6 / GP6
    PIN_GP7 = 8  # PWM7 / GP7
    PIN_GP8 = 9  # PWM8 / GP8
    PIN_GP9 = 10  # PWM9 / GP9
    PIN_GP10 = 11  # SPI1 TX / GP10
    PIN_GP11 = 12  # SPI1 RX / GP11
    PIN_GP12 = 13  # SPI1 CSn / GP12
    PIN_GP13 = 14  # SPI1 SCK / GP13
    PIN_GP14 = 15  # PWM14 / GP14
    PIN_GP15 = 16  # PWM15 / GP15
    PIN_GP16 = 17  # UART1 TX / GP16
    PIN_GP17 = 18  # UART1 RX / GP17
    PIN_GP18 = 19  # I2C0 SDA / GP18
    PIN_GP19 = 20  # I2C0 SCL / GP19
    PIN_GP20 = 21  # I2C1 SDA / GP20
    PIN_GP21 = 22  # I2C1 SCL / GP21
    PIN_GP22 = 23  # GP22
    PIN_RUN = 24  # Run enable
    PIN_AGND = 25  # Analog ground
    PIN_GP26 = 26  # ADC0 / GP26
    PIN_GP27 = 27  # ADC1 / GP27
    PIN_GP28 = 28  # ADC2 / GP28
    PIN_ADC_VREF = 29  # ADC voltage reference
    PIN_GP35 = 30  # GP35
    PIN_GP34 = 31  # GP34
    PIN_GP33 = 32  # GP33
    PIN_GP36 = 37  # GP36
    PIN_GP37 = 38  # GP37
    PIN_GP38 = 39  # GP38
    PIN_GP39 = 40  # GP39
    PIN_GP40 = 41  # GP40
    PIN_GP41 = 42  # GP41
    PIN_SWCLK = 43  # SWD Clock
    PIN_SWDIO = 44  # SWD Data I/O

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["R0"] = {
            "address": 0x00000000,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 0",
            "value": 0
        }
        self._registers["R1"] = {
            "address": 0x00000004,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 1",
            "value": 0
        }
        self._registers["R2"] = {
            "address": 0x00000008,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 2",
            "value": 0
        }
        self._registers["R3"] = {
            "address": 0x0000000C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 3",
            "value": 0
        }
        self._registers["R4"] = {
            "address": 0x00000010,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 4",
            "value": 0
        }
        self._registers["R5"] = {
            "address": 0x00000014,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 5",
            "value": 0
        }
        self._registers["R6"] = {
            "address": 0x00000018,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 6",
            "value": 0
        }
        self._registers["R7"] = {
            "address": 0x0000001C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 7",
            "value": 0
        }
        self._registers["R8"] = {
            "address": 0x00000020,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 8",
            "value": 0
        }
        self._registers["R9"] = {
            "address": 0x00000024,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 9",
            "value": 0
        }
        self._registers["R10"] = {
            "address": 0x00000028,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 10",
            "value": 0
        }
        self._registers["R11"] = {
            "address": 0x0000002C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 11",
            "value": 0
        }
        self._registers["R12"] = {
            "address": 0x00000030,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 12",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x00000034,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["LR"] = {
            "address": 0x00000038,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Link Register",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x0000003C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["xPSR"] = {
            "address": 0x00000040,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Status Register",
            "value": 0
        }
        self._registers["PRIMASK"] = {
            "address": 0xE0000E20,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Priority Mask Register",
            "value": 0
        }
        self._registers["CONTROL"] = {
            "address": 0xE0000E24,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Control Register",
            "value": 0
        }
        self._registers["FAULTMASK"] = {
            "address": 0xE0000E28,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Fault Mask Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["IO_BANK0"] = {
            "base": 0x40014000,
            "type": "gpio",
            "description": "IO Bank 0",
            "registers": {
                "GPIO0_STATUS": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO0_CTRL": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO1_STATUS": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO1_CTRL": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO2_STATUS": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO2_CTRL": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO3_STATUS": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO3_CTRL": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO4_STATUS": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO4_CTRL": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO5_STATUS": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO5_CTRL": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO6_STATUS": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO6_CTRL": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO7_STATUS": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO7_CTRL": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO8_STATUS": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO8_CTRL": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO9_STATUS": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO9_CTRL": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO10_STATUS": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO10_CTRL": {
                    "address": 0x0054,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO11_STATUS": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO11_CTRL": {
                    "address": 0x005C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO12_STATUS": {
                    "address": 0x0060,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO12_CTRL": {
                    "address": 0x0064,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO13_STATUS": {
                    "address": 0x0068,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO13_CTRL": {
                    "address": 0x006C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO14_STATUS": {
                    "address": 0x0070,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO14_CTRL": {
                    "address": 0x0074,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO15_STATUS": {
                    "address": 0x0078,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO15_CTRL": {
                    "address": 0x007C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO16_STATUS": {
                    "address": 0x0080,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO16_CTRL": {
                    "address": 0x0084,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO17_STATUS": {
                    "address": 0x0088,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO17_CTRL": {
                    "address": 0x008C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO18_STATUS": {
                    "address": 0x0090,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO18_CTRL": {
                    "address": 0x0094,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO19_STATUS": {
                    "address": 0x0098,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO19_CTRL": {
                    "address": 0x009C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO20_STATUS": {
                    "address": 0x00A0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO20_CTRL": {
                    "address": 0x00A4,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO21_STATUS": {
                    "address": 0x00A8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO21_CTRL": {
                    "address": 0x00AC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO22_STATUS": {
                    "address": 0x00B0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO22_CTRL": {
                    "address": 0x00B4,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO23_STATUS": {
                    "address": 0x00B8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO23_CTRL": {
                    "address": 0x00BC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO24_STATUS": {
                    "address": 0x00C0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO24_CTRL": {
                    "address": 0x00C4,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO25_STATUS": {
                    "address": 0x00C8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO25_CTRL": {
                    "address": 0x00CC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO26_STATUS": {
                    "address": 0x00D0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO26_CTRL": {
                    "address": 0x00D4,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO27_STATUS": {
                    "address": 0x00D8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO27_CTRL": {
                    "address": 0x00DC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO28_STATUS": {
                    "address": 0x00E0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO28_CTRL": {
                    "address": 0x00E4,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO29_STATUS": {
                    "address": 0x00E8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO29_CTRL": {
                    "address": 0x00EC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTR": {
                    "address": 0x00F0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PROC0_INTE": {
                    "address": 0x00F4,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PROC1_INTE": {
                    "address": 0x00F8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PROC0_INTF": {
                    "address": 0x00FC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PROC1_INTF": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PROC0_INTS": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PROC1_INTS": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DORMANT_WAKE_INTE": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DORMANT_WAKE_INTF": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DORMANT_WAKE_INTS": {
                    "address": 0x0114,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["Pads"] = {
            "base": 0x4001E000,
            "type": "gpio",
            "description": "Pads",
            "registers": {
                "GPIO_VOLT": {
                    "address": 0x00E0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SIO"] = {
            "base": 0xD0000000,
            "type": "sysio",
            "description": "SIO (Single-cycle I/O)",
            "registers": {
                "CPUID": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT_SET": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT_CLR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OUT_XOR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OE": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OE_SET": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OE_CLR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_OE_XOR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GPIO_IN": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FIFO_ST": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FIFO_WR": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FIFO_RD": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SPINLOCK_ST": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTERRUPT_ST": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART0"] = {
            "base": 0x40034000,
            "type": "uart",
            "description": "UART0",
            "registers": {
                "UARTDR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTRSR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTECR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTFR": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTILPR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTIBRD": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTFBRD": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTLCR_H": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTCR": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTIFLS": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTIMSC": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTRIS": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTMIS": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTICR": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTDMACR": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART1"] = {
            "base": 0x40038000,
            "type": "uart",
            "description": "UART1",
            "registers": {
                "UARTDR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTRSR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTFR": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTIBRD": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTFBRD": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTLCR_H": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTCR": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTIFLS": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTIMSC": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "UARTICR": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI0"] = {
            "base": 0x4003C000,
            "type": "spi",
            "description": "SPI0",
            "registers": {
                "SSPCR0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPCR1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPDR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPSR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPCPSR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPIMSC": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPRIS": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPMIS": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPICR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPDMACR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI1"] = {
            "base": 0x4003C000,
            "type": "spi",
            "description": "SPI1",
            "registers": {
                "SSPCR0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPCR1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPDR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPSR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPCPSR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSPIMSC": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C0"] = {
            "base": 0x40044000,
            "type": "i2c",
            "description": "I2C0",
            "registers": {
                "IC_CON": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_TAR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_SAR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_DATA_CMD": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_SS_SCL_HCNT": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_SS_SCL_LCNT": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_FS_SCL_HCNT": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_FS_SCL_LCNT": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_RAW_INTR_STAT": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_ENABLE": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_STATUS": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_TXFLR": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_RXFLR": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_TX_ABRT": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_DMA_CR": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_DMA_TDLR": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_DMA_RDLR": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C1"] = {
            "base": 0x40048000,
            "type": "i2c",
            "description": "I2C1",
            "registers": {
                "IC_CON": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_TAR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_ENABLE": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_STATUS": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IC_TX_ABRT": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWM0"] = {
            "base": 0x40050000,
            "type": "pwm",
            "description": "PWM0",
            "registers": {
                "CS": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMPR0": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMPR1": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMPR2": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMPR3": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TOP": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTE": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTF": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTS": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PHS0": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PHS1": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PHS2": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PHS3": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIV": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PHASE": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWM1"] = {
            "base": 0x40051000,
            "type": "pwm",
            "description": "PWM1",
            "registers": {
                "CS": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMPR0": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMPR1": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMPR2": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMPR3": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TOP": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIV": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC"] = {
            "base": 0x4004C000,
            "type": "adc",
            "description": "ADC",
            "registers": {
                "ADC_CS": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADC_RESULT": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADC_FCS": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADC_FIFO": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADC_TS": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADC_OFFSET": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADC_TRIG": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER0"] = {
            "base": 0x40054000,
            "type": "timer",
            "description": "Timer0",
            "registers": {
                "TIMEHW": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMELW": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMEHA": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMELA": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMERA": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMERIQ": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMEREAD": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER1"] = {
            "base": 0x40058000,
            "type": "timer",
            "description": "Timer1",
            "registers": {
                "TIMEHW": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMELW": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMEHA": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMELA": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TIMERA": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RTC"] = {
            "base": 0x4005C000,
            "type": "rtc",
            "description": "RTC",
            "registers": {
                "RTC_CLKS": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTC_SET": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTC_WR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTC_DATE": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTC_TOTAL": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTC_HASH": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTC_RTC": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTE": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTF": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTS": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["WATCHDOG"] = {
            "base": 0x40060000,
            "type": "wdt",
            "description": "Watchdog",
            "registers": {
                "WATCHDOG_CTL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "WATCHDOG_MOD": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "WATCHDOG_FR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "WATCHDOG_LOAD": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USB"] = {
            "base": 0x50100000,
            "type": "usb",
            "description": "USB",
            "registers": {
                "USB_CTRL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_ADDR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_PWR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_TXFIFO": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_RXFIFO": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_TXIE": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_RXIE": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_IS": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_IM": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_IE": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_REVO": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_EP": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_BUFF": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USB_MPS": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PIO0"] = {
            "base": 0x50200000,
            "type": "pio",
            "description": "PIO0",
            "registers": {
                "CTRL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FSTAT": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FDEBUG": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FCTRL": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXF0": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXF1": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXF2": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXF3": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXF0": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXF1": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXF2": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXF3": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IRQ": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IRQ_FORCE": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IRQ_INTF": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IRQ_INTS": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_CLKDIV": {
                    "address": 0x00C8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_EXECCTRL": {
                    "address": 0x00CC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_SHIFTCTRL": {
                    "address": 0x00D0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_ADDR": {
                    "address": 0x00D4,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_INSTR": {
                    "address": 0x00D8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_PINCTRL": {
                    "address": 0x00DC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PIO1"] = {
            "base": 0x50201000,
            "type": "pio",
            "description": "PIO1",
            "registers": {
                "CTRL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FSTAT": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IRQ": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_CLKDIV": {
                    "address": 0x00C8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_EXECCTRL": {
                    "address": 0x00CC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_SHIFTCTRL": {
                    "address": 0x00D0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_ADDR": {
                    "address": 0x00D4,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SM0_INSTR": {
                    "address": 0x00D8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["CLOCKS"] = {
            "base": 0x40008000,
            "type": "clock",
            "description": "Clock Manager",
            "registers": {
                "CLK_GP0DIV": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLK_GP0CTRL": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLK_GP1DIV": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLK_GP1CTRL": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLK_GP2DIV": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLK_GP2CTRL": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLK_REF": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLK_SYS": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLK_PERI": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["XOSC"] = {
            "base": 0x40020000,
            "type": "osc",
            "description": "Crystal Oscillator",
            "registers": {
                "XOSC_CTRL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "XOSC_STATUS": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "XOSC_COUNT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ROSC"] = {
            "base": 0x40010000,
            "type": "osc",
            "description": "Ring Oscillator",
            "registers": {
                "ROSC_CTRL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ROSC_FREQA": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ROSC_FREQB": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ROSC_FREQC": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ROSC_FREQD": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ROSC_STATUS": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ROSC_DR": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PLL_SYS"] = {
            "base": 0x40028000,
            "type": "pll",
            "description": "System PLL",
            "registers": {
                "PLL_CS": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL_PWR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL_FBDIV": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL_PRIMARY": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL_POSTDIV1": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL_POSTDIV2": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PLL_USB"] = {
            "base": 0x4002C000,
            "type": "pll",
            "description": "USB PLL",
            "registers": {
                "PLL_CS": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL_PWR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL_FBDIV": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL_PRIMARY": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RESETS"] = {
            "base": 0x4000C000,
            "type": "reset",
            "description": "Resets",
            "registers": {
                "RESET": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RESET_DONE": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "WD_RESET": {
                    "address": 0x0008,
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
        return f"RP2040({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = RP2040()
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

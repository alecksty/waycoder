"""
LPC1768设备定义 - Python模块
生成自: NXP/LPC17xx/LPC1768
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: ARM Cortex-M3 up to 100MHz with 512KB Flash, 64KB SRAM
CPU架构: ARM-Cortex-M3
位宽: 32位
时钟频率: 12000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class LPC1768:
    """LPC1768设备类"""

    # 设备信息
    DEVICE_NAME = "LPC1768"
    MANUFACTURER = "NXP"
    FAMILY = "LPC17xx"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M3"
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
    PSR_ADDR = 0x00000040  # Program Status Register
    PSR_N_BIT = 31  # Negative Flag
    PSR_Z_BIT = 30  # Zero Flag
    PSR_C_BIT = 29  # Carry Flag
    PSR_V_BIT = 28  # Overflow Flag
    PSR_Q_BIT = 27  # Saturation Flag
    PSR_ICI1_BIT = 0  # Interrupt Continue State
    PSR_GE_BIT = 0  # Greater than or Equal
    PSR_IT_BIT = 0  # If-Then execution state
    PSR_APSR_BIT = 0  # Application Program Status
    PRIMASK_ADDR = 0xE0000E20  # Priority Mask Register
    FAULTMASK_ADDR = 0xE0000E28  # Fault Mask Register
    BASEPRI_ADDR = 0xE0000E24  # Base Priority Register
    CONTROL_ADDR = 0xE0000E2C  # Control Register

    # 内存段定义
    FLASH_START = 0x00000000
    FLASH_END = 0x0007FFFF
    FLASH_SIZE = 524288  # Main Flash (512KB)
    FLASH_BOOT_START = 0x00080000
    FLASH_BOOT_END = 0x0007FFFF
    FLASH_BOOT_SIZE = 32768  # Boot Flash (32KB)
    SRAM_START = 0x10000000
    SRAM_END = 0x1000FFFF
    SRAM_SIZE = 65536  # SRAM (64KB)
    AHB1_START = 0x20000000
    AHB1_END = 0x200FFFFF
    AHB1_SIZE = 1048576  # AHB1 Peripherals
    APB0_START = 0x40000000
    APB0_END = 0x400FFFFF
    APB0_SIZE = 1048576  # APB0 Peripherals
    APB1_START = 0x50000000
    APB1_END = 0x500FFFFF
    APB1_SIZE = 1048576  # APB1 Peripherals

    # 外设定义
    # GPIO
    GPIO_BASE = 0x2009C000
    GPIO_FIODIR_ADDR = 0x0000
    GPIO_FIOMASK_ADDR = 0x0004
    GPIO_FIOPIN_ADDR = 0x0008
    GPIO_FIOSET_ADDR = 0x000C
    GPIO_FIOCLR_ADDR = 0x0010
    GPIO_P0_ADDR = 0x0014
    GPIO_P1_ADDR = 0x0018
    GPIO_P2_ADDR = 0x001C
    GPIO_P3_ADDR = 0x0020
    GPIO_P4_ADDR = 0x0024
    # UART0
    UART0_BASE = 0x4000C000
    UART0_RBR_ADDR = 0x0000
    UART0_THR_ADDR = 0x0000
    UART0_DLL_ADDR = 0x0000
    UART0_DLM_ADDR = 0x0004
    UART0_IER_ADDR = 0x0004
    UART0_IIR_ADDR = 0x0008
    UART0_FCR_ADDR = 0x0008
    UART0_LCR_ADDR = 0x000C
    UART0_LSR_ADDR = 0x0014
    UART0_SCR_ADDR = 0x001C
    UART0_ACR_ADDR = 0x0020
    UART0_ICR_ADDR = 0x0024
    UART0_FDR_ADDR = 0x0028
    UART0_TER_ADDR = 0x0030
    # UART1
    UART1_BASE = 0x4000D000
    UART1_RBR_ADDR = 0x0000
    UART1_THR_ADDR = 0x0000
    UART1_DLL_ADDR = 0x0000
    UART1_DLM_ADDR = 0x0004
    UART1_IER_ADDR = 0x0004
    UART1_IIR_ADDR = 0x0008
    UART1_LCR_ADDR = 0x000C
    UART1_LSR_ADDR = 0x0014
    UART1_SCR_ADDR = 0x001C
    UART1_MSR_ADDR = 0x0020
    UART1_SCR_ADDR = 0x0024
    # UART2
    UART2_BASE = 0x40098000
    UART2_RBR_ADDR = 0x0000
    UART2_THR_ADDR = 0x0000
    UART2_DLL_ADDR = 0x0000
    UART2_DLM_ADDR = 0x0004
    UART2_IER_ADDR = 0x0004
    UART2_IIR_ADDR = 0x0008
    UART2_LCR_ADDR = 0x000C
    UART2_LSR_ADDR = 0x0014
    # UART3
    UART3_BASE = 0x4009C000
    UART3_RBR_ADDR = 0x0000
    UART3_THR_ADDR = 0x0000
    UART3_DLL_ADDR = 0x0000
    UART3_DLM_ADDR = 0x0004
    UART3_IER_ADDR = 0x0004
    UART3_IIR_ADDR = 0x0008
    UART3_LCR_ADDR = 0x000C
    UART3_LSR_ADDR = 0x0014
    # SPI0
    SPI0_BASE = 0x40088000
    SPI0_CR0_ADDR = 0x0000
    SPI0_CR1_ADDR = 0x0004
    SPI0_DR_ADDR = 0x0008
    SPI0_SR_ADDR = 0x000C
    SPI0_CPSR_ADDR = 0x0010
    SPI0_IMSC_ADDR = 0x0014
    SPI0_RIS_ADDR = 0x0018
    SPI0_MIS_ADDR = 0x001C
    SPI0_ICR_ADDR = 0x0020
    # SPI1
    SPI1_BASE = 0x4008C000
    SPI1_CR0_ADDR = 0x0000
    SPI1_CR1_ADDR = 0x0004
    SPI1_DR_ADDR = 0x0008
    SPI1_SR_ADDR = 0x000C
    SPI1_CPSR_ADDR = 0x0010
    # I2C0
    I2C0_BASE = 0x4001C000
    I2C0_CON_ADDR = 0x0000
    I2C0_TAR_ADDR = 0x0004
    I2C0_DAT_ADDR = 0x0008
    I2C0_SSHC_ADDR = 0x000C
    I2C0_HSH_ADDR = 0x0010
    I2C0_INTX_ADDR = 0x0014
    I2C0_INTM_ADDR = 0x0018
    I2C0_AR_ADDR = 0x001C
    I2C0_SR_ADDR = 0x0020
    I2C0_TXFL_ADDR = 0x0024
    I2C0_RXFL_ADDR = 0x0028
    I2C0_COMP_ADDR = 0x002C
    I2C0_RXFI_ADDR = 0x0030
    I2C0_RXFT_ADDR = 0x0030
    # I2C1
    I2C1_BASE = 0x4001C000
    I2C1_CON_ADDR = 0x0000
    I2C1_TAR_ADDR = 0x0004
    I2C1_DAT_ADDR = 0x0008
    I2C1_SR_ADDR = 0x0020
    # Timer0
    TIMER0_BASE = 0x40004000
    TIMER0_IR_ADDR = 0x0000
    TIMER0_TCR_ADDR = 0x0004
    TIMER0_TC_ADDR = 0x0008
    TIMER0_PR_ADDR = 0x000C
    TIMER0_PC_ADDR = 0x0010
    TIMER0_MCR_ADDR = 0x0014
    TIMER0_MR0_ADDR = 0x0018
    TIMER0_MR1_ADDR = 0x001C
    TIMER0_MR2_ADDR = 0x0020
    TIMER0_MR3_ADDR = 0x0024
    TIMER0_CCR_ADDR = 0x0028
    TIMER0_CR0_ADDR = 0x002C
    TIMER0_CR1_ADDR = 0x0030
    TIMER0_CR2_ADDR = 0x0034
    TIMER0_CR3_ADDR = 0x0038
    TIMER0_EMR_ADDR = 0x003C
    TIMER0_CTCR_ADDR = 0x0070
    TIMER0_EW_ADDR = 0x0074
    # Timer1
    TIMER1_BASE = 0x40008000
    TIMER1_IR_ADDR = 0x0000
    TIMER1_TCR_ADDR = 0x0004
    TIMER1_TC_ADDR = 0x0008
    TIMER1_PR_ADDR = 0x000C
    TIMER1_MCR_ADDR = 0x0014
    TIMER1_MR0_ADDR = 0x0018
    TIMER1_MR1_ADDR = 0x001C
    TIMER1_MR2_ADDR = 0x0020
    TIMER1_MR3_ADDR = 0x0024
    TIMER1_CCR_ADDR = 0x0028
    TIMER1_CR0_ADDR = 0x002C
    TIMER1_CR1_ADDR = 0x0030
    TIMER1_EMR_ADDR = 0x003C
    # Timer2
    TIMER2_BASE = 0x400A4000
    TIMER2_IR_ADDR = 0x0000
    TIMER2_TCR_ADDR = 0x0004
    TIMER2_TC_ADDR = 0x0008
    TIMER2_PR_ADDR = 0x000C
    TIMER2_MCR_ADDR = 0x0014
    TIMER2_MR0_ADDR = 0x0018
    TIMER2_CCR_ADDR = 0x0028
    TIMER2_CR0_ADDR = 0x002C
    # Timer3
    TIMER3_BASE = 0x400A8000
    TIMER3_IR_ADDR = 0x0000
    TIMER3_TCR_ADDR = 0x0004
    TIMER3_TC_ADDR = 0x0008
    TIMER3_PR_ADDR = 0x000C
    TIMER3_MCR_ADDR = 0x0014
    TIMER3_MR0_ADDR = 0x0018
    TIMER3_CCR_ADDR = 0x0028
    # PWM0
    PWM0_BASE = 0x40014000
    PWM0_IR_ADDR = 0x0000
    PWM0_TCR_ADDR = 0x0004
    PWM0_TC_ADDR = 0x0008
    PWM0_PR_ADDR = 0x000C
    PWM0_PC_ADDR = 0x0010
    PWM0_MCR_ADDR = 0x0014
    PWM0_MR0_ADDR = 0x0018
    PWM0_MR1_ADDR = 0x001C
    PWM0_MR2_ADDR = 0x0020
    PWM0_MR3_ADDR = 0x0024
    PWM0_MR4_ADDR = 0x0040
    PWM0_MR5_ADDR = 0x0044
    PWM0_MR6_ADDR = 0x0048
    PWM0_CCR_ADDR = 0x0028
    PWM0_CR0_ADDR = 0x002C
    PWM0_PCR_ADDR = 0x004C
    PWM0_LER_ADDR = 0x0050
    PWM0_CTCR_ADDR = 0x0070
    # ADC
    ADC_BASE = 0x400E4000
    ADC_CR_ADDR = 0x0000
    ADC_GDR_ADDR = 0x0004
    ADC_INTEN_ADDR = 0x000C
    ADC_STATUS_ADDR = 0x0010
    ADC_TR_ADDR = 0x0014
    # DAC
    DAC_BASE = 0x400E5000
    DAC_CR_ADDR = 0x0000
    DAC_CTRL_ADDR = 0x0004
    # Ethernet
    ETH_BASE = 0x50000000
    ETH_MAC1_ADDR = 0x0000
    ETH_MAC2_ADDR = 0x0004
    ETH_IPGT_ADDR = 0x0008
    ETH_IPGR_ADDR = 0x000C
    ETH_CLRT_ADDR = 0x0010
    ETH_MAXF_ADDR = 0x0014
    ETH_SUPP_ADDR = 0x0018
    ETH_TEST_ADDR = 0x001C
    ETH_MCFG_ADDR = 0x0020
    ETH_MCMD_ADDR = 0x0024
    ETH_MADR_ADDR = 0x0028
    ETH_MWTD_ADDR = 0x002C
    ETH_MRDD_ADDR = 0x0030
    ETH_IND_ADDR = 0x0034
    # USB Controller
    USB_BASE = 0x50000000
    USB_HCCHAR_ADDR = 
    USB_HCINT_ADDR = 
    USB_HCINTMSK_ADDR = 
    USB_HCTSIZ_ADDR = 
    USB_HCDMA_ADDR = 
    USB_HCDMAB_ADDR = 
    USB_OTGINTST_ADDR = 
    USB_OTGINTEN_ADDR = 
    USB_OTGINTSEL_ADDR = 
    # DMA Controller
    DMA_BASE = 0x50004000
    DMA_INTSTAT_ADDR = 0x0000
    DMA_INTTCSTAT_ADDR = 0x0004
    DMA_INTTCCLEAR_ADDR = 0x0008
    DMA_INTERRSTAT_ADDR = 0x000C
    DMA_INTERRCLR_ADDR = 0x0010
    DMA_RAWINTSTAT_ADDR = 0x0014
    DMA_RAWINTTCSTAT_ADDR = 0x0018
    DMA_ENBLDCHNS_ADDR = 0x001C
    DMA_SOFTBREQ_ADDR = 0x0020
    DMA_SOFTSREQ_ADDR = 0x0024
    DMA_CONFIG_ADDR = 0x0028
    DMA_SYNC_ADDR = 0x002C
    # Watchdog Timer
    WDT_BASE = 0x40000000
    WDT_WDMOD_ADDR = 0x0000
    WDT_WDTC_ADDR = 0x0004
    WDT_WDFEED_ADDR = 0x0008
    WDT_WDTV_ADDR = 0x000C
    # RTC
    RTC_BASE = 0x40024000
    RTC_ILR_ADDR = 0x0000
    RTC_CCR_ADDR = 0x0004
    RTC_CIIR_ADDR = 0x0008
    RTC_CWR_ADDR = 0x000C
    RTC_PREINT_ADDR = 0x0010
    RTC_PREFRAC_ADDR = 0x0014
    RTC_CRT_ADDR = 0x0018
    RTC_SEC_ADDR = 0x001C
    RTC_MIN_ADDR = 0x0020
    RTC_HOUR_ADDR = 0x0024
    RTC_DOM_ADDR = 0x0028
    RTC_DOW_ADDR = 0x002C
    RTC_DOY_ADDR = 0x0030
    RTC_MONTH_ADDR = 0x0034
    RTC_YEAR_ADDR = 0x0038
    # System Control
    SC_BASE = 0x400FC000
    SC_PLL0CON_ADDR = 0x0000
    SC_PLL0CFG_ADDR = 0x0004
    SC_PLL0STAT_ADDR = 0x0008
    SC_PLL0FEED_ADDR = 0x000C
    SC_PLL1CON_ADDR = 0x0010
    SC_PLL1CFG_ADDR = 0x0014
    SC_PLL1STAT_ADDR = 0x0018
    SC_PLL1FEED_ADDR = 0x001C
    SC_CCLKCFG_ADDR = 0x0020
    SC_USBCLKCFG_ADDR = 0x0024
    SC_CLKSRC_ADDR = 0x0028
    SC_PCLKSEL0_ADDR = 0x002C
    SC_PCLKSEL1_ADDR = 0x0030
    SC_BOSC_ADDR = 0x0050
    SC_EXTINT_ADDR = 0x0054
    SC_EXTMODE_ADDR = 0x0058
    SC_EXTPOL_ADDR = 0x005C
    # Pin Connect Block
    PINCONNECTBLOCK_BASE = 0x4002C000
    PINCONNECTBLOCK_PINSEL0_ADDR = 0x0000
    PINCONNECTBLOCK_PINSEL1_ADDR = 0x0004
    PINCONNECTBLOCK_PINSEL2_ADDR = 0x0008
    PINCONNECTBLOCK_PINSEL3_ADDR = 0x000C
    PINCONNECTBLOCK_PINSEL4_ADDR = 0x0010
    PINCONNECTBLOCK_PINSEL5_ADDR = 0x0014
    PINCONNECTBLOCK_PINSEL6_ADDR = 0x0018
    PINCONNECTBLOCK_PINSEL7_ADDR = 0x001C
    PINCONNECTBLOCK_PINSEL8_ADDR = 0x0020
    PINCONNECTBLOCK_PINSEL9_ADDR = 0x0024
    PINCONNECTBLOCK_PINMODE0_ADDR = 0x0040
    PINCONNECTBLOCK_PINMODE1_ADDR = 0x0044
    PINCONNECTBLOCK_PINMODE2_ADDR = 0x0048
    PINCONNECTBLOCK_PINMODE3_ADDR = 0x004C
    PINCONNECTBLOCK_PINMODE4_ADDR = 0x0050
    PINCONNECTBLOCK_PINMODE5_ADDR = 0x0054
    PINCONNECTBLOCK_PINMODE6_ADDR = 0x0058
    PINCONNECTBLOCK_PINMODE7_ADDR = 0x005C
    PINCONNECTBLOCK_PINMODE8_ADDR = 0x0060
    PINCONNECTBLOCK_PINMODE9_ADDR = 0x0064
    PINCONNECTBLOCK_PINOD0_ADDR = 0x0080
    PINCONNECTBLOCK_PINOD1_ADDR = 0x0084
    PINCONNECTBLOCK_PINOD2_ADDR = 0x0088
    PINCONNECTBLOCK_PINOD3_ADDR = 0x008C

    # 中断向量定义
    INT_WDT = 0  # Watchdog Timer
    INT_RESERVED = 1  # Reserved
    INT_DEBUG_MON = 2  # ARM Debug Mon
    INT_RESERVED = 3  # Reserved
    INT_TIMER0 = 4  # Timer 0
    INT_TIMER1 = 5  # Timer 1
    INT_PWM0 = 6  # PWM 0
    INT_UART0 = 7  # UART 0
    INT_UART1 = 8  # UART 1
    INT_PWM1 = 9  # PWM 1
    INT_I2C0 = 10  # I2C 0
    INT_I2C1 = 11  # I2C 1
    INT_SPI0 = 12  # SPI 0
    INT_SPI1 = 13  # SPI 1
    INT_RTC = 14  # RTC
    INT_EINT0 = 15  # External Interrupt 0
    INT_EINT1 = 16  # External Interrupt 1
    INT_EINT2 = 17  # External Interrupt 2
    INT_EINT3 = 18  # External Interrupt 3
    INT_RESERVED = 19  # Reserved
    INT_ADC = 20  # A/D Converter
    INT_BOD = 21  # Brown-Out Detect
    INT_USB = 22  # USB
    INT_CAN = 23  # CAN
    INT_GP = 24  # General Purpose DMA
    INT_I2S = 25  # I2S
    INT_ETHERNET = 26  # Ethernet
    INT_RIT = 27  # Repetitive Interrupt Timer
    INT_QM = 28  # Quadrature Encoder
    INT_RESERVED = 29  # Reserved
    INT_RESERVED = 30  # Reserved

    # 引脚定义
    PIN_RESET = 1  # External Reset
    PIN_P0_0 = 2  # GPIO Port 0.0
    PIN_P0_1 = 3  # GPIO Port 0.1
    PIN_VSSA = 4  # Analog Ground
    PIN_VDDA = 5  # Analog 3.3V
    PIN_P0_2 = 6  # GPIO Port 0.2
    PIN_P0_3 = 7  # GPIO Port 0.3
    PIN_P0_4 = 8  # GPIO Port 0.4
    PIN_P0_5 = 9  # GPIO Port 0.5
    PIN_P0_6 = 10  # GPIO Port 0.6
    PIN_P0_7 = 11  # GPIO Port 0.7
    PIN_P0_8 = 12  # GPIO Port 0.8
    PIN_P0_9 = 13  # GPIO Port 0.9
    PIN_P0_10 = 14  # GPIO Port 0.10
    PIN_VSS = 15  # Ground
    PIN_VDD = 16  # 3.3V
    PIN_P0_11 = 17  # GPIO Port 0.11
    PIN_P0_12 = 18  # GPIO Port 0.12
    PIN_P0_13 = 19  # GPIO Port 0.13
    PIN_P0_14 = 20  # GPIO Port 0.14
    PIN_P0_15 = 21  # GPIO Port 0.15
    PIN_P0_16 = 22  # GPIO Port 0.16
    PIN_P0_17 = 23  # GPIO Port 0.17
    PIN_P0_18 = 24  # GPIO Port 0.18
    PIN_P0_19 = 25  # GPIO Port 0.19
    PIN_P0_20 = 26  # GPIO Port 0.20
    PIN_P0_21 = 27  # GPIO Port 0.21
    PIN_P0_22 = 28  # GPIO Port 0.22
    PIN_P0_23 = 29  # GPIO Port 0.23
    PIN_VSS = 30  # Ground
    PIN_VDD = 31  # 3.3V
    PIN_RTCX1 = 32  # RTC Crystal Input
    PIN_RTCX2 = 33  # RTC Crystal Output
    PIN_P1_0 = 34  # GPIO Port 1.0
    PIN_P1_1 = 35  # GPIO Port 1.1
    PIN_P1_2 = 36  # GPIO Port 1.2
    PIN_P1_3 = 37  # GPIO Port 1.3
    PIN_P1_4 = 38  # GPIO Port 1.4
    PIN_P1_5 = 39  # GPIO Port 1.5
    PIN_P1_6 = 40  # GPIO Port 1.6
    PIN_P1_7 = 41  # GPIO Port 1.7
    PIN_P1_8 = 42  # GPIO Port 1.8
    PIN_P1_9 = 43  # GPIO Port 1.9
    PIN_P1_10 = 44  # GPIO Port 1.10
    PIN_P1_11 = 45  # GPIO Port 1.11
    PIN_P1_12 = 46  # GPIO Port 1.12
    PIN_P1_13 = 47  # GPIO Port 1.13
    PIN_P1_14 = 48  # GPIO Port 1.14
    PIN_P1_15 = 49  # GPIO Port 1.15
    PIN_P1_16 = 50  # GPIO Port 1.16
    PIN_P1_17 = 51  # GPIO Port 1.17
    PIN_P1_18 = 52  # GPIO Port 1.18
    PIN_P1_19 = 53  # GPIO Port 1.19
    PIN_P1_20 = 54  # GPIO Port 1.20
    PIN_P1_21 = 55  # GPIO Port 1.21
    PIN_P1_22 = 56  # GPIO Port 1.22
    PIN_P1_23 = 57  # GPIO Port 1.23
    PIN_P1_24 = 58  # GPIO Port 1.24
    PIN_P1_25 = 59  # GPIO Port 1.25
    PIN_P1_26 = 60  # GPIO Port 1.26
    PIN_P1_27 = 61  # GPIO Port 1.27
    PIN_P1_28 = 62  # GPIO Port 1.28
    PIN_P1_29 = 63  # GPIO Port 1.29
    PIN_P1_30 = 64  # GPIO Port 1.30
    PIN_P1_31 = 65  # GPIO Port 1.31
    PIN_P2_0 = 66  # GPIO Port 2.0
    PIN_P2_1 = 67  # GPIO Port 2.1
    PIN_P2_2 = 68  # GPIO Port 2.2
    PIN_P2_3 = 69  # GPIO Port 2.3
    PIN_P2_4 = 70  # GPIO Port 2.4
    PIN_P2_5 = 71  # GPIO Port 2.5
    PIN_P2_6 = 72  # GPIO Port 2.6
    PIN_P2_7 = 73  # GPIO Port 2.7
    PIN_P2_8 = 74  # GPIO Port 2.8
    PIN_P2_9 = 75  # GPIO Port 2.9
    PIN_P2_10 = 76  # GPIO Port 2.10
    PIN_P2_11 = 77  # GPIO Port 2.11
    PIN_P2_12 = 78  # GPIO Port 2.12
    PIN_P2_13 = 79  # GPIO Port 2.13
    PIN_P2_14 = 80  # GPIO Port 2.14
    PIN_P2_15 = 81  # GPIO Port 2.15
    PIN_VSS = 82  # Ground
    PIN_VDD = 83  # 3.3V

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
        self._registers["PSR"] = {
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
        self._registers["FAULTMASK"] = {
            "address": 0xE0000E28,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Fault Mask Register",
            "value": 0
        }
        self._registers["BASEPRI"] = {
            "address": 0xE0000E24,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Base Priority Register",
            "value": 0
        }
        self._registers["CONTROL"] = {
            "address": 0xE0000E2C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Control Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["GPIO"] = {
            "base": 0x2009C000,
            "type": "gpio",
            "description": "GPIO",
            "registers": {
                "FIODIR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FIOMASK": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FIOPIN": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FIOSET": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FIOCLR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "P0": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "P1": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "P2": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "P3": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "P4": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART0"] = {
            "base": 0x4000C000,
            "type": "uart",
            "description": "UART0",
            "registers": {
                "RBR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "THR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLM": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IER": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IIR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FCR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LCR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LSR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SCR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ACR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ICR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FDR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TER": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART1"] = {
            "base": 0x4000D000,
            "type": "uart",
            "description": "UART1",
            "registers": {
                "RBR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "THR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLM": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IER": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IIR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LCR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LSR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SCR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MSR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SCR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART2"] = {
            "base": 0x40098000,
            "type": "uart",
            "description": "UART2",
            "registers": {
                "RBR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "THR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLM": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IER": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IIR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LCR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LSR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART3"] = {
            "base": 0x4009C000,
            "type": "uart",
            "description": "UART3",
            "registers": {
                "RBR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "THR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DLM": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IER": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IIR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LCR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LSR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI0"] = {
            "base": 0x40088000,
            "type": "spi",
            "description": "SPI0",
            "registers": {
                "CR0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CPSR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IMSC": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RIS": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MIS": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ICR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI1"] = {
            "base": 0x4008C000,
            "type": "spi",
            "description": "SPI1",
            "registers": {
                "CR0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CPSR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C0"] = {
            "base": 0x4001C000,
            "type": "i2c",
            "description": "I2C0",
            "registers": {
                "CON": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TAR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DAT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSHC": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HSH": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTX": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTM": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXFL": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXFL": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "COMP": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXFI": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXFT": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C1"] = {
            "base": 0x4001C000,
            "type": "i2c",
            "description": "I2C1",
            "registers": {
                "CON": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TAR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DAT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER0"] = {
            "base": 0x40004000,
            "type": "timer",
            "description": "Timer0",
            "registers": {
                "IR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TCR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TC": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PC": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MCR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR0": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR1": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR2": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR3": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR0": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR3": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EMR": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTCR": {
                    "address": 0x0070,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EW": {
                    "address": 0x0074,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER1"] = {
            "base": 0x40008000,
            "type": "timer",
            "description": "Timer1",
            "registers": {
                "IR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TCR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TC": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MCR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR0": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR1": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR2": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR3": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR0": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EMR": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER2"] = {
            "base": 0x400A4000,
            "type": "timer",
            "description": "Timer2",
            "registers": {
                "IR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TCR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TC": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MCR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR0": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR0": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER3"] = {
            "base": 0x400A8000,
            "type": "timer",
            "description": "Timer3",
            "registers": {
                "IR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TCR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TC": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MCR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR0": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWM0"] = {
            "base": 0x40014000,
            "type": "pwm",
            "description": "PWM0",
            "registers": {
                "IR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TCR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TC": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PC": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MCR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR0": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR1": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR2": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR3": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR4": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR5": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MR6": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR0": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PCR": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LER": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTCR": {
                    "address": 0x0070,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC"] = {
            "base": 0x400E4000,
            "type": "adc",
            "description": "ADC",
            "registers": {
                "CR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GDR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
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
                "TR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["DAC"] = {
            "base": 0x400E5000,
            "type": "dac",
            "description": "DAC",
            "registers": {
                "CR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTRL": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ETH"] = {
            "base": 0x50000000,
            "type": "ethernet",
            "description": "Ethernet",
            "registers": {
                "MAC1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MAC2": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IPGT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IPGR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLRT": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MAXF": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SUPP": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TEST": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MCFG": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MCMD": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MADR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MWTD": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MRDD": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IND": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USB"] = {
            "base": 0x50000000,
            "type": "usb",
            "description": "USB Controller",
            "registers": {
                "HCCHAR": {
                    "address": ,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HCINT": {
                    "address": ,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HCINTMSK": {
                    "address": ,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HCTSIZ": {
                    "address": ,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HCDMA": {
                    "address": ,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HCDMAB": {
                    "address": ,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OTGIntSt": {
                    "address": ,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OTGIntEn": {
                    "address": ,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OTGIntSel": {
                    "address": ,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["DMA"] = {
            "base": 0x50004000,
            "type": "dma",
            "description": "DMA Controller",
            "registers": {
                "IntStat": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IntTCStat": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IntTCClear": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IntErrStat": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IntErrClr": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RawIntStat": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RawIntTCStat": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EnbldChns": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SoftBReq": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SoftSReq": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "Config": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "Sync": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["WDT"] = {
            "base": 0x40000000,
            "type": "wdt",
            "description": "Watchdog Timer",
            "registers": {
                "WDMOD": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "WDTC": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "WDFEED": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "WDTV": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RTC"] = {
            "base": 0x40024000,
            "type": "rtc",
            "description": "RTC",
            "registers": {
                "ILR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CIIR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CWR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PREINT": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PREFRAC": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRT": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEC": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MIN": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HOUR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DOM": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DOW": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DOY": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MONTH": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "YEAR": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SC"] = {
            "base": 0x400FC000,
            "type": "syscon",
            "description": "System Control",
            "registers": {
                "PLL0CON": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL0CFG": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL0STAT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL0FEED": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL1CON": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL1CFG": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL1STAT": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL1FEED": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCLKCFG": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USBCLKCFG": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLKSRC": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PCLKSEL0": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PCLKSEL1": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BOSC": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTINT": {
                    "address": 0x0054,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTMODE": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTPOL": {
                    "address": 0x005C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PinConnectBlock"] = {
            "base": 0x4002C000,
            "type": "pin",
            "description": "Pin Connect Block",
            "registers": {
                "PINSEL0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINSEL1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINSEL2": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINSEL3": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINSEL4": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINSEL5": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINSEL6": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINSEL7": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINSEL8": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINSEL9": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE0": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE1": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE2": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE3": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE4": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE5": {
                    "address": 0x0054,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE6": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE7": {
                    "address": 0x005C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE8": {
                    "address": 0x0060,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINMODE9": {
                    "address": 0x0064,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINOD0": {
                    "address": 0x0080,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINOD1": {
                    "address": 0x0084,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINOD2": {
                    "address": 0x0088,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PINOD3": {
                    "address": 0x008C,
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
        return f"LPC1768({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = LPC1768()
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

"""
nRF52840设备定义 - Python模块
生成自: Nordic Semiconductor/nRF52/nRF52840
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: ARM Cortex-M4F up to 64MHz with Bluetooth 5.0, 1MB Flash, 256KB RAM
CPU架构: ARM-Cortex-M4F
位宽: 32位
时钟频率: 32000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class nRF52840:
    """nRF52840设备类"""

    # 设备信息
    DEVICE_NAME = "nRF52840"
    MANUFACTURER = "Nordic Semiconductor"
    FAMILY = "nRF52"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M4F"
    BITS = 32
    CLOCK_FREQUENCY = 32000000

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
    XPSR_ICI1_BIT = 0  # Interrupt Continue State
    XPSR_GE_BIT = 0  # Greater than or Equal
    XPSR_IT_BIT = 0  # If-Then execution state
    XPSR_T_BIT = 24  # Thumb bit
    XPSR_IPSR_BIT = 0  # Exception number
    PRIMASK_ADDR = 0xE0000E20  # Priority Mask Register
    BASEPRI_ADDR = 0xE0000E24  # Base Priority Register
    FAULTMASK_ADDR = 0xE0000E28  # Fault Mask Register
    CONTROL_ADDR = 0xE0000E2C  # Control Register
    FPSCR_ADDR = 0xE0000EF34  # FPU Status Control
    S0_ADDR = 0xE0000EF00  # FPU Register S0
    S1_ADDR = 0xE0000EF04  # FPU Register S1
    S2_ADDR = 0xE0000EF08  # FPU Register S2
    S3_ADDR = 0xE0000EF0C  # FPU Register S3
    S4_ADDR = 0xE0000EF10  # FPU Register S4
    S5_ADDR = 0xE0000EF14  # FPU Register S5
    S6_ADDR = 0xE0000EF18  # FPU Register S6
    S7_ADDR = 0xE0000EF1C  # FPU Register S7
    S8_ADDR = 0xE0000EF20  # FPU Register S8
    S9_ADDR = 0xE0000EF24  # FPU Register S9
    S10_ADDR = 0xE0000EF28  # FPU Register S10
    S11_ADDR = 0xE0000EF2C  # FPU Register S11
    S12_ADDR = 0xE0000EF30  # FPU Register S12
    S13_ADDR = 0xE0000EF34  # FPU Register S13
    S14_ADDR = 0xE0000EF38  # FPU Register S14
    S15_ADDR = 0xE0000EF3C  # FPU Register S15
    S16_ADDR = 0xE0000EF40  # FPU Register S16
    S17_ADDR = 0xE0000EF44  # FPU Register S17
    S18_ADDR = 0xE0000EF48  # FPU Register S18
    S19_ADDR = 0xE0000EF4C  # FPU Register S19
    S20_ADDR = 0xE0000EF50  # FPU Register S20
    S21_ADDR = 0xE0000EF54  # FPU Register S21
    S22_ADDR = 0xE0000EF58  # FPU Register S22
    S23_ADDR = 0xE0000EF5C  # FPU Register S23
    S24_ADDR = 0xE0000EF60  # FPU Register S24
    S25_ADDR = 0xE0000EF64  # FPU Register S25
    S26_ADDR = 0xE0000EF68  # FPU Register S26
    S27_ADDR = 0xE0000EF6C  # FPU Register S27
    S28_ADDR = 0xE0000EF70  # FPU Register S28
    S29_ADDR = 0xE0000EF74  # FPU Register S29
    S30_ADDR = 0xE0000EF78  # FPU Register S30
    S31_ADDR = 0xE0000EF7C  # FPU Register S31

    # 内存段定义
    FLASH_START = 0x00000000
    FLASH_END = 0x0FFFFF
    FLASH_SIZE = 1048576  # Flash (1MB)
    SRAM_START = 0x20000000
    SRAM_END = 0x2003FFFF
    SRAM_SIZE = 262144  # SRAM (256KB)
    SRAM_LOW_START = 0x20000000
    SRAM_LOW_END = 0x2001FFFF
    SRAM_LOW_SIZE = 131072  # SRAM Low (128KB)
    SRAM_HIGH_START = 0x20020000
    SRAM_HIGH_END = 0x2003FFFF
    SRAM_HIGH_SIZE = 131072  # SRAM High (128KB)
    FICR_START = 0x10000000
    FICR_END = 0x10001000
    FICR_SIZE = 4096  # Factory Information Configuration
    UICR_START = 0x10001000
    UICR_END = 0x10001000
    UICR_SIZE = 4096  # User Information Configuration
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x50000000
    PERIPHERAL_SIZE = 268435456  # Peripheral Space

    # 外设定义
    # GPIO
    GPIO_BASE = 0x50000000
    GPIO_OUT_ADDR = 0x0000
    GPIO_OUTSET_ADDR = 0x0004
    GPIO_OUTCLR_ADDR = 0x0008
    GPIO_IN_ADDR = 0x000C
    GPIO_DIR_ADDR = 0x0010
    GPIO_DIRSET_ADDR = 0x0014
    GPIO_DIRCLR_ADDR = 0x0018
    GPIO_PIN_CNF0_ADDR = 0x0300
    GPIO_PIN_CNF1_ADDR = 0x0304
    GPIO_PIN_CNF2_ADDR = 0x0308
    GPIO_PIN_CNF3_ADDR = 0x030C
    GPIO_PIN_CNF4_ADDR = 0x0310
    GPIO_PIN_CNF5_ADDR = 0x0314
    GPIO_PIN_CNF6_ADDR = 0x0318
    GPIO_PIN_CNF7_ADDR = 0x031C
    GPIO_PIN_CNF8_ADDR = 0x0320
    GPIO_PIN_CNF9_ADDR = 0x0324
    GPIO_PIN_CNF10_ADDR = 0x0328
    GPIO_PIN_CNF11_ADDR = 0x032C
    GPIO_PIN_CNF12_ADDR = 0x0330
    GPIO_PIN_CNF13_ADDR = 0x0334
    GPIO_PIN_CNF14_ADDR = 0x0338
    GPIO_PIN_CNF15_ADDR = 0x033C
    GPIO_PIN_CNF16_ADDR = 0x0340
    GPIO_PIN_CNF17_ADDR = 0x0344
    GPIO_PIN_CNF18_ADDR = 0x0348
    GPIO_PIN_CNF19_ADDR = 0x034C
    GPIO_PIN_CNF20_ADDR = 0x0350
    GPIO_PIN_CNF21_ADDR = 0x0354
    GPIO_PIN_CNF22_ADDR = 0x0358
    GPIO_PIN_CNF23_ADDR = 0x035C
    GPIO_PIN_CNF24_ADDR = 0x0360
    GPIO_PIN_CNF25_ADDR = 0x0364
    GPIO_PIN_CNF26_ADDR = 0x0368
    GPIO_PIN_CNF27_ADDR = 0x036C
    GPIO_PIN_CNF28_ADDR = 0x0370
    GPIO_PIN_CNF29_ADDR = 0x0374
    GPIO_PIN_CNF30_ADDR = 0x0378
    GPIO_PIN_CNF31_ADDR = 0x037C
    # UART0
    UART0_BASE = 0x40002000
    UART0_TASKS_STARTRX_ADDR = 0x0000
    UART0_TASKS_STARTTX_ADDR = 0x0004
    UART0_TASKS_STOPRX_ADDR = 0x0008
    UART0_TASKS_STOPTX_ADDR = 0x000C
    UART0_SUBSCRIBED_STARTRX_ADDR = 0x0010
    UART0_SUBSCRIBED_STARTTX_ADDR = 0x0014
    UART0_EVENTS_CTS_ADDR = 0x0100
    UART0_EVENTS_NCTS_ADDR = 0x0104
    UART0_EVENTS_RXDRDY_ADDR = 0x0108
    UART0_EVENTS_TXDRDY_ADDR = 0x010C
    UART0_EVENTS_ERROR_ADDR = 0x0110
    UART0_RXD_ADDR = 0x0518
    UART0_TXD_ADDR = 0x051C
    UART0_BAUDRATE_ADDR = 0x0524
    UART0_CONFIG_ADDR = 0x056C
    UART0_INTEN_ADDR = 0x0700
    UART0_INTENSET_ADDR = 0x0704
    UART0_INTENCLR_ADDR = 0x0708
    # UART1
    UART1_BASE = 0x40003000
    UART1_TASKS_STARTRX_ADDR = 0x0000
    UART1_TASKS_STARTTX_ADDR = 0x0004
    UART1_EVENTS_RXDRDY_ADDR = 0x0108
    UART1_EVENTS_TXDRDY_ADDR = 0x010C
    UART1_RXD_ADDR = 0x0518
    UART1_TXD_ADDR = 0x051C
    UART1_BAUDRATE_ADDR = 0x0524
    # SPI0
    SPI0_BASE = 0x40003000
    SPI0_EVENTS_READY_ADDR = 0x0108
    SPI0_RXD_ADDR = 0x0518
    SPI0_TXD_ADDR = 0x051C
    SPI0_FREQUENCY_ADDR = 0x0524
    SPI0_CONFIG_ADDR = 0x056C
    SPI0_INTEN_ADDR = 0x0700
    # SPI1
    SPI1_BASE = 0x40004000
    SPI1_EVENTS_READY_ADDR = 0x0108
    SPI1_RXD_ADDR = 0x0518
    SPI1_TXD_ADDR = 0x051C
    SPI1_FREQUENCY_ADDR = 0x0524
    SPI1_CONFIG_ADDR = 0x056C
    # SPI2
    SPI2_BASE = 0x40005000
    SPI2_EVENTS_READY_ADDR = 0x0108
    SPI2_RXD_ADDR = 0x0518
    SPI2_TXD_ADDR = 0x051C
    SPI2_FREQUENCY_ADDR = 0x0524
    # I2C0
    I2C0_BASE = 0x40003000
    I2C0_TASKS_STARTRX_ADDR = 0x0000
    I2C0_TASKS_STARTTX_ADDR = 0x0008
    I2C0_TASKS_STOP_ADDR = 0x0014
    I2C0_EVENTS_DONE_ADDR = 0x0108
    I2C0_EVENTS_TXDSENT_ADDR = 0x010C
    I2C0_EVENTS_ERROR_ADDR = 0x0110
    I2C0_RXD_ADDR = 0x0518
    I2C0_TXD_ADDR = 0x051C
    I2C0_ADDRESS_ADDR = 0x0524
    # I2C1
    I2C1_BASE = 0x40004000
    I2C1_TASKS_STARTRX_ADDR = 0x0000
    I2C1_TASKS_STARTTX_ADDR = 0x0008
    I2C1_TASKS_STOP_ADDR = 0x0014
    I2C1_EVENTS_DONE_ADDR = 0x0108
    I2C1_RXD_ADDR = 0x0518
    I2C1_TXD_ADDR = 0x051C
    I2C1_ADDRESS_ADDR = 0x0524
    # Timer0
    TIMER0_BASE = 0x40008000
    TIMER0_TASKS_START_ADDR = 0x0000
    TIMER0_TASKS_STOP_ADDR = 0x0004
    TIMER0_TASKS_COUNT_ADDR = 0x0008
    TIMER0_TASKS_CLEAR_ADDR = 0x000C
    TIMER0_CC0_ADDR = 0x0400
    TIMER0_CC1_ADDR = 0x0404
    TIMER0_CC2_ADDR = 0x0408
    TIMER0_CC3_ADDR = 0x040C
    TIMER0_SHORTS_ADDR = 0x0200
    TIMER0_INTEN_ADDR = 0x0700
    TIMER0_MODE_ADDR = 0x0510
    TIMER0_BITMODE_ADDR = 0x0514
    # Timer1
    TIMER1_BASE = 0x40009000
    TIMER1_TASKS_START_ADDR = 0x0000
    TIMER1_TASKS_STOP_ADDR = 0x0004
    TIMER1_TASKS_COUNT_ADDR = 0x0008
    TIMER1_TASKS_CLEAR_ADDR = 0x000C
    TIMER1_CC0_ADDR = 0x0400
    TIMER1_CC1_ADDR = 0x0404
    TIMER1_CC2_ADDR = 0x0408
    TIMER1_CC3_ADDR = 0x040C
    TIMER1_SHORTS_ADDR = 0x0200
    # Timer2
    TIMER2_BASE = 0x4000A000
    TIMER2_TASKS_START_ADDR = 0x0000
    TIMER2_TASKS_STOP_ADDR = 0x0004
    TIMER2_TASKS_COUNT_ADDR = 0x0008
    TIMER2_TASKS_CLEAR_ADDR = 0x000C
    TIMER2_CC0_ADDR = 0x0400
    TIMER2_CC1_ADDR = 0x0404
    TIMER2_CC2_ADDR = 0x0408
    TIMER2_CC3_ADDR = 0x040C
    # Timer3
    TIMER3_BASE = 0x4000B000
    TIMER3_TASKS_START_ADDR = 0x0000
    TIMER3_TASKS_STOP_ADDR = 0x0004
    TIMER3_TASKS_COUNT_ADDR = 0x0008
    TIMER3_TASKS_CLEAR_ADDR = 0x000C
    TIMER3_CC0_ADDR = 0x0400
    TIMER3_CC1_ADDR = 0x0404
    TIMER3_CC2_ADDR = 0x0408
    TIMER3_CC3_ADDR = 0x040C
    # Timer4
    TIMER4_BASE = 0x4000C000
    TIMER4_TASKS_START_ADDR = 0x0000
    TIMER4_TASKS_STOP_ADDR = 0x0004
    TIMER4_TASKS_COUNT_ADDR = 0x0008
    TIMER4_TASKS_CLEAR_ADDR = 0x000C
    TIMER4_CC0_ADDR = 0x0400
    TIMER4_CC1_ADDR = 0x0404
    TIMER4_CC2_ADDR = 0x0408
    TIMER4_CC3_ADDR = 0x040C
    # RTC0
    RTC0_BASE = 0x4000B000
    RTC0_TASKS_START_ADDR = 0x0000
    RTC0_TASKS_STOP_ADDR = 0x0004
    RTC0_TASKS_TRIGOVRFLW_ADDR = 0x0010
    RTC0_EVENTS_TICK_ADDR = 0x0100
    RTC0_EVENTS_OVRFLW_ADDR = 0x0104
    RTC0_EVENTS_COMPARE0_ADDR = 0x0140
    RTC0_EVENTS_COMPARE1_ADDR = 0x0144
    RTC0_EVENTS_COMPARE2_ADDR = 0x0148
    RTC0_EVENTS_COMPARE3_ADDR = 0x014C
    RTC0_CC0_ADDR = 0x0400
    RTC0_CC1_ADDR = 0x0404
    RTC0_CC2_ADDR = 0x0408
    RTC0_CC3_ADDR = 0x040C
    RTC0_CNT_ADDR = 0x0500
    RTC0_PRESCALER_ADDR = 0x0504
    RTC0_TICK_ADDR = 0x0510
    # RTC1
    RTC1_BASE = 0x4000D000
    RTC1_TASKS_START_ADDR = 0x0000
    RTC1_TASKS_STOP_ADDR = 0x0004
    RTC1_EVENTS_TICK_ADDR = 0x0100
    RTC1_EVENTS_OVRFLW_ADDR = 0x0104
    RTC1_EVENTS_COMPARE0_ADDR = 0x0140
    RTC1_EVENTS_COMPARE1_ADDR = 0x0144
    RTC1_CC0_ADDR = 0x0400
    RTC1_CC1_ADDR = 0x0404
    RTC1_CNT_ADDR = 0x0500
    RTC1_PRESCALER_ADDR = 0x0504
    # PWM0
    PWM0_BASE = 0x4001C000
    PWM0_TASKS_START_ADDR = 0x0000
    PWM0_TASKS_STOP_ADDR = 0x0004
    PWM0_TASKS_SEQSTART0_ADDR = 0x0008
    PWM0_TASKS_SEQSTART1_ADDR = 0x000C
    PWM0_TASKS_NEXTSTEP_ADDR = 0x0010
    PWM0_EVENTS_PWMPERIODEND_ADDR = 0x0108
    PWM0_EVENTS_LOOPEND_ADDR = 0x010C
    PWM0_EVENTS_SEQEND0_ADDR = 0x0110
    PWM0_EVENTS_SEQEND1_ADDR = 0x0114
    PWM0_SEQ0_PTR_ADDR = 0x0510
    PWM0_SEQ1_PTR_ADDR = 0x0514
    PWM0_SEQ0_CNT_ADDR = 0x0528
    PWM0_SEQ1_CNT_ADDR = 0x052C
    PWM0_SEQ0_REFRESH_ADDR = 0x0530
    PWM0_SEQ1_REFRESH_ADDR = 0x0534
    PWM0_DECODER_ADDR = 0x0540
    PWM0_LOOP_ADDR = 0x0544
    PWM0_MODE_ADDR = 0x0500
    PWM0_CLKEN_ADDR = 0x0504
    PWM0_CNT_ADDR = 0x0548
    # PWM1
    PWM1_BASE = 0x4001D000
    PWM1_TASKS_START_ADDR = 0x0000
    PWM1_TASKS_STOP_ADDR = 0x0004
    PWM1_SEQ0_PTR_ADDR = 0x0510
    PWM1_SEQ1_PTR_ADDR = 0x0514
    PWM1_MODE_ADDR = 0x0500
    PWM1_CNT_ADDR = 0x0548
    # PWM2
    PWM2_BASE = 0x4001E000
    PWM2_TASKS_START_ADDR = 0x0000
    PWM2_SEQ0_PTR_ADDR = 0x0510
    PWM2_MODE_ADDR = 0x0500
    # PWM3
    PWM3_BASE = 0x4001F000
    PWM3_TASKS_START_ADDR = 0x0000
    PWM3_SEQ0_PTR_ADDR = 0x0510
    PWM3_MODE_ADDR = 0x0500
    # ADC
    ADC_BASE = 0x40012000
    ADC_TASKS_START_ADDR = 0x0000
    ADC_TASKS_STOP_ADDR = 0x0004
    ADC_EVENTS_DONE_ADDR = 0x0108
    ADC_EVENTS_RESULTDONE_ADDR = 0x010C
    ADC_EVENTS_CALIBRATEDONE_ADDR = 0x0110
    ADC_EVENTS_CH_LIMITH_ADDR = 0x0114
    ADC_EVENTS_CH_LIMITL_ADDR = 0x0118
    ADC_RESULT_ADDR = 0x0400
    ADC_CH0_CONFIG_ADDR = 0x0510
    ADC_CH1_CONFIG_ADDR = 0x0514
    ADC_CH2_CONFIG_ADDR = 0x0518
    ADC_CH3_CONFIG_ADDR = 0x051C
    ADC_CH4_CONFIG_ADDR = 0x0520
    ADC_CH5_CONFIG_ADDR = 0x0524
    ADC_CH6_CONFIG_ADDR = 0x0528
    ADC_CH7_CONFIG_ADDR = 0x052C
    ADC_CONFIG_ADDR = 0x0530
    ADC_TASKS_CALIBRATELOAD_ADDR = 0x0034
    ADC_INTEN_ADDR = 0x0700
    # DAC
    DAC_BASE = 0x40013000
    DAC_TASKS_START_ADDR = 0x0000
    DAC_TASKS_STOP_ADDR = 0x0004
    DAC_EVENTS_DONE_ADDR = 0x0108
    DAC_VALUE_ADDR = 0x0400
    DAC_CEN_ADDR = 0x0504
    # Analog Comparator
    COMP_BASE = 0x40013000
    COMP_TASKS_START_ADDR = 0x0000
    COMP_TASKS_STOP_ADDR = 0x0004
    COMP_TASKS_SETTLE_ADDR = 0x0010
    COMP_EVENTS_READY_ADDR = 0x0108
    COMP_EVENTS_DOWN_ADDR = 0x010C
    COMP_EVENTS_UP_ADDR = 0x0110
    COMP_EVENTS_CROSS_ADDR = 0x0114
    COMP_RESULT_ADDR = 0x0400
    COMP_EN_ADDR = 0x0500
    COMP_TASK_MODE_ADDR = 0x0504
    COMP_REFSEL_ADDR = 0x0508
    COMP_EXTREFSEL_ADDR = 0x050C
    COMP_THD_ADDR = 0x0510
    COMP_HYST_ADDR = 0x0514
    COMP_SPEED_ADDR = 0x0518
    COMP_ISOURCE_ADDR = 0x051C
    COMP_PSEL_ADDR = 0x0520
    COMP_NOREF_ADDR = 0x0524
    COMP_INTEN_ADDR = 0x0700
    # Quadrature Decoder
    QDEC_BASE = 0x40014000
    QDEC_TASKS_START_ADDR = 0x0000
    QDEC_TASKS_STOP_ADDR = 0x0004
    QDEC_TASKS_RDCLRACC_ADDR = 0x0008
    QDEC_TASKS_RDCLRDBL_ADDR = 0x000C
    QDEC_TASKS_RDCLRPH_ADDR = 0x0010
    QDEC_EVENTS_READY_ADDR = 0x0108
    QDEC_EVENTS_DBLRDY_ADDR = 0x010C
    QDEC_EVENTS_QCLR_ADDR = 0x0110
    QDEC_ACC_ADDR = 0x0404
    QDEC_ACCREAD_ADDR = 0x0408
    QDEC_DBLINC_ADDR = 0x0410
    QDEC_DBL_ADDR = 0x0418
    QDEC_DBLREAD_ADDR = 0x041C
    QDEC_PHASE_ADDR = 0x0420
    QDEC_PHASEREAD_ADDR = 0x0424
    QDEC_LEFLL_ADDR = 0x0428
    QDEC_INTEN_ADDR = 0x0700
    # Event Generators Unit 0
    EGU0_BASE = 0x40014000
    EGU0_TASKS_TRIGGER0_ADDR = 0x0000
    EGU0_TASKS_TRIGGER1_ADDR = 0x0004
    EGU0_TASKS_TRIGGER2_ADDR = 0x0008
    EGU0_TASKS_TRIGGER3_ADDR = 0x000C
    EGU0_TASKS_TRIGGER4_ADDR = 0x0010
    EGU0_TASKS_TRIGGER5_ADDR = 0x0014
    EGU0_TASKS_TRIGGER6_ADDR = 0x0018
    EGU0_TASKS_TRIGGER7_ADDR = 0x001C
    EGU0_TASKS_TRIGGER8_ADDR = 0x0020
    EGU0_TASKS_TRIGGER9_ADDR = 0x0024
    EGU0_TASKS_TRIGGER10_ADDR = 0x0028
    EGU0_TASKS_TRIGGER11_ADDR = 0x002C
    EGU0_TASKS_TRIGGER12_ADDR = 0x0030
    EGU0_TASKS_TRIGGER13_ADDR = 0x0034
    EGU0_TASKS_TRIGGER14_ADDR = 0x0038
    EGU0_TASKS_TRIGGER15_ADDR = 0x003C
    EGU0_EVENTS_EVENT0_ADDR = 0x0100
    EGU0_EVENTS_EVENT1_ADDR = 0x0104
    EGU0_EVENTS_EVENT2_ADDR = 0x0108
    EGU0_EVENTS_EVENT3_ADDR = 0x010C
    EGU0_EVENTS_EVENT4_ADDR = 0x0110
    EGU0_EVENTS_EVENT5_ADDR = 0x0114
    EGU0_INTEN_ADDR = 0x0700
    # Random Number Generator
    RNG_BASE = 0x40006000
    RNG_TASKS_START_ADDR = 0x0000
    RNG_TASKS_STOP_ADDR = 0x0004
    RNG_EVENTS_VALRDY_ADDR = 0x0108
    RNG_VALUE_ADDR = 0x0400
    RNG_CONFIG_ADDR = 0x0504
    RNG_INTEN_ADDR = 0x0700
    # AES ECB
    AES_BASE = 0x40005000
    AES_TASKS_START_ADDR = 0x0000
    AES_TASKS_STOP_ADDR = 0x0004
    AES_EVENTS_END_ADDR = 0x0108
    AES_EVENTS_ERROR_ADDR = 0x010C
    AES_CRYPTCNTXT_ADDR = 0x0400
    AES_CRYPTCNTCPY_ADDR = 0x0404
    AES_CRYPTCMD_ADDR = 0x0500
    AES_INTEN_ADDR = 0x0700
    # Cryptocell
    CRYPTO_BASE = 0x4000E000
    CRYPTO_TASKS_START_ADDR = 0x0000
    CRYPTO_TASKS_STOP_ADDR = 0x0004
    CRYPTO_EVENTS_DONE_ADDR = 0x0108
    CRYPTO_EVENTS_ERROR_ADDR = 0x010C
    CRYPTO_DMA_ADDR = 0x0400
    CRYPTO_CMDS_ADDR = 0x0404
    CRYPTO_CMDS_AMOUNT_ADDR = 0x0408
    CRYPTO_INTENSET_ADDR = 0x0704
    CRYPTO_INTENCLR_ADDR = 0x0708
    CRYPTO_INTCONTEXT_ADDR = 0x0710
    # USB
    USB_BASE = 0x40027000
    USB_TASKS_STARTUP_ADDR = 0x0000
    USB_TASKS_SUSPEND_ADDR = 0x0004
    USB_TASKS_RESUME_ADDR = 0x0008
    USB_EVENTS_ENDRDY_ADDR = 0x0108
    USB_EVENTS_SUSPENDED_ADDR = 0x010C
    USB_EVENTS_RESUMED_ADDR = 0x0110
    USB_EVENTS_SOF_ADDR = 0x0114
    USB_EVENTS_EPOF_ADDR = 0x0118
    USB_EVENTS_DATA_ADDR = 0x011C
    USB_EVENTS_EP0DATADONE_ADDR = 0x0120
    USB_EVENTS_EP0SETUP_ADDR = 0x0124
    USB_EVENTS_EP0HALTD_ADDR = 0x0128
    USB_EVENTS_EP1DMA_ADDR = 0x0134
    USB_EVENTS_EP2DMA_ADDR = 0x0138
    USB_EVENTS_EP3DMA_ADDR = 0x013C
    USB_EVENTS_EP4DMA_ADDR = 0x0140
    USB_EVENTS_EP1_ADDR = 0x0158
    USB_EVENTS_EP2_ADDR = 0x015C
    USB_EVENTS_EP3_ADDR = 0x0160
    USB_EVENTS_EP4_ADDR = 0x0164
    USB_EVENTS_EP5_ADDR = 0x0168
    USB_EVENTS_EP6_ADDR = 0x016C
    USB_EVENTS_EP7_ADDR = 0x0170
    USB_EVENTS_EP8_ADDR = 0x0174
    USB_EVENTS_EP9_ADDR = 0x0178
    USB_EVENTS_EP10_ADDR = 0x017C
    USB_EVENTS_EP11_ADDR = 0x0180
    USB_EVENTS_EP12_ADDR = 0x0184
    USB_EVENTS_EP13_ADDR = 0x0188
    USB_EVENTS_EP14_ADDR = 0x018C
    USB_EVENTS_EP15_ADDR = 0x0190
    USB_USBADDR_ADDR = 0x0500
    USB_USBREQ_ADDR = 0x0504
    USB_USBVAL_ADDR = 0x0508
    USB_USBINDEX_ADDR = 0x050C
    USB_USBCONFIG_ADDR = 0x0510
    USB_EPIN_ADDR = 0x0514
    USB_EPOUT_ADDR = 0x0518
    USB_EPLEN_ADDR = 0x0520
    USB_EPSIZE_ADDR = 0x0524
    USB_EPDMA_ADDR = 0x0500
    USB_EPDMA_ADDR = 0x0504
    USB_INTEN_ADDR = 0x0700
    USB_INTENSET_ADDR = 0x0704
    USB_INTENCLR_ADDR = 0x0708
    # Watchdog Timer
    WDT_BASE = 0x40011000
    WDT_TASKS_START_ADDR = 0x0000
    WDT_TASKS_KEEP_ADDR = 0x0004
    WDT_TASKS_STOP_ADDR = 0x0008
    WDT_EVENTS_TIMEOUT_ADDR = 0x0108
    WDT_RUNSTATUS_ADDR = 0x0404
    WDT_REQSTATUS_ADDR = 0x0408
    WDT_CRV_ADDR = 0x0504
    WDT_RCV_ADDR = 0x0508
    WDT_CONFIG_ADDR = 0x050C
    WDT_INTEN_ADDR = 0x0700
    # Reset
    NRF_RESET_BASE = 0x40000000
    NRF_RESET_RESET_ADDR = 0x0000
    NRF_RESET_RESET_FAC_ADDR = 0x0400
    NRF_RESET_RESET_NFAC_ADDR = 0x0500
    # Clock
    CLOCK_BASE = 0x40000000
    CLOCK_TASKS_HFCLKSTART_ADDR = 0x0000
    CLOCK_TASKS_HFCLKSTOP_ADDR = 0x0004
    CLOCK_TASKS_LFCLKSTART_ADDR = 0x0008
    CLOCK_TASKS_LFCLKSTOP_ADDR = 0x000C
    CLOCK_TASKS_CAL_ADDR = 0x0010
    CLOCK_TASKS_CTTO_ADDR = 0x0010
    CLOCK_EVENTS_HFCLKSTATED_ADDR = 0x0100
    CLOCK_EVENTS_LFCLKSTATED_ADDR = 0x0104
    CLOCK_EVENTS_DONE_ADDR = 0x0108
    CLOCK_EVENTS_CTTO_ADDR = 0x010C
    CLOCK_HFCLKSTAT_ADDR = 0x0400
    CLOCK_LFCLKSTAT_ADDR = 0x0404
    CLOCK_LFCLKSRC_ADDR = 0x0508
    CLOCK_CTIV_ADDR = 0x050C
    CLOCK_INTEN_ADDR = 0x0700
    # Power
    POWER_BASE = 0x40000000
    POWER_TASKS_CONSTLAT_ADDR = 0x0000
    POWER_TASKS_LOWPWR_ADDR = 0x0004
    POWER_EVENTS_POWERDEBUG_ADDR = 0x0100
    POWER_EVENTS_SLEEPDEBUG_ADDR = 0x0104
    POWER_INTEN_ADDR = 0x0700
    # GPIO Tasks and Events
    GPIOTE_BASE = 0x40006000
    GPIOTE_TASKS_SET0_ADDR = 0x0000
    GPIOTE_TASKS_SET1_ADDR = 0x0004
    GPIOTE_TASKS_SET2_ADDR = 0x0008
    GPIOTE_TASKS_SET3_ADDR = 0x000C
    GPIOTE_TASKS_CLR0_ADDR = 0x0010
    GPIOTE_TASKS_CLR1_ADDR = 0x0014
    GPIOTE_TASKS_CLR2_ADDR = 0x0018
    GPIOTE_TASKS_CLR3_ADDR = 0x001C
    GPIOTE_EVENTS_IN0_ADDR = 0x0100
    GPIOTE_EVENTS_IN1_ADDR = 0x0104
    GPIOTE_EVENTS_IN2_ADDR = 0x0108
    GPIOTE_EVENTS_IN3_ADDR = 0x010C
    GPIOTE_EVENTS_IN4_ADDR = 0x0110
    GPIOTE_EVENTS_IN5_ADDR = 0x0114
    GPIOTE_EVENTS_IN6_ADDR = 0x0118
    GPIOTE_EVENTS_IN7_ADDR = 0x011C
    GPIOTE_EVENTS_TOUCH_ADDR = 0x0140
    GPIOTE_EVENTS_LISR_ADDR = 0x0144
    GPIOTE_EVENTS_LISF_ADDR = 0x0148
    GPIOTE_EVENTS_COUNT_ADDR = 0x014C
    GPIOTE_CONFIG0_ADDR = 0x0510
    GPIOTE_CONFIG1_ADDR = 0x0514
    GPIOTE_CONFIG2_ADDR = 0x0518
    GPIOTE_CONFIG3_ADDR = 0x051C
    GPIOTE_CONFIG4_ADDR = 0x0520
    GPIOTE_CONFIG5_ADDR = 0x0524
    GPIOTE_CONFIG6_ADDR = 0x0528
    GPIOTE_CONFIG7_ADDR = 0x052C
    GPIOTE_INTEN_ADDR = 0x0700
    # Real Time Timer
    RTT_BASE = 0x40009000
    RTT_TASKS_START_ADDR = 0x0000
    RTT_TASKS_STOP_ADDR = 0x0004
    RTT_TASKS_TRIGOVRFLW_ADDR = 0x0010
    RTT_EVENTS_TICK_ADDR = 0x0100
    RTT_EVENTS_OVRFLW_ADDR = 0x0104
    RTT_EVENTS_COMPARE0_ADDR = 0x0140
    RTT_CC0_ADDR = 0x0400
    RTT_CC1_ADDR = 0x0404
    RTT_CC2_ADDR = 0x0408
    RTT_CC3_ADDR = 0x040C
    RTT_CNT_ADDR = 0x0500
    RTT_PRESCALER_ADDR = 0x0504
    # Inter-Process Communication
    IPC_BASE = 0x40014000
    IPC_TASKS_SEND0_ADDR = 0x0000
    IPC_TASKS_SEND1_ADDR = 0x0004
    IPC_TASKS_SEND2_ADDR = 0x0008
    IPC_TASKS_SEND3_ADDR = 0x000C
    IPC_TASKS_SEND4_ADDR = 0x0010
    IPC_TASKS_SEND5_ADDR = 0x0014
    IPC_TASKS_SEND6_ADDR = 0x0018
    IPC_TASKS_SEND7_ADDR = 0x001C
    IPC_TASKS_RECEIVE0_ADDR = 0x0080
    IPC_TASKS_RECEIVE1_ADDR = 0x0084
    IPC_TASKS_RECEIVE2_ADDR = 0x0088
    IPC_TASKS_RECEIVE3_ADDR = 0x008C
    IPC_TASKS_RECEIVE4_ADDR = 0x0090
    IPC_TASKS_RECEIVE5_ADDR = 0x0094
    IPC_TASKS_RECEIVE6_ADDR = 0x0098
    IPC_TASKS_RECEIVE7_ADDR = 0x009C
    IPC_EVENTS_SENT0_ADDR = 0x0100
    IPC_EVENTS_SENT1_ADDR = 0x0104
    IPC_EVENTS_SENT2_ADDR = 0x0108
    IPC_EVENTS_SENT3_ADDR = 0x010C
    IPC_EVENTS_SENT4_ADDR = 0x0110
    IPC_EVENTS_SENT5_ADDR = 0x0114
    IPC_EVENTS_SENT6_ADDR = 0x0118
    IPC_EVENTS_SENT7_ADDR = 0x011C
    IPC_EVENTS_RECEIVE0_ADDR = 0x0180
    IPC_EVENTS_RECEIVE1_ADDR = 0x0184
    IPC_EVENTS_RECEIVE2_ADDR = 0x0188
    IPC_EVENTS_RECEIVE3_ADDR = 0x018C
    IPC_EVENTS_RECEIVE4_ADDR = 0x0190
    IPC_EVENTS_RECEIVE5_ADDR = 0x0194
    IPC_EVENTS_RECEIVE6_ADDR = 0x0198
    IPC_EVENTS_RECEIVE7_ADDR = 0x019C
    IPC_CH0_ADDR = 0x0500
    IPC_CH1_ADDR = 0x0504
    IPC_CH2_ADDR = 0x0508
    IPC_CH3_ADDR = 0x050C
    IPC_CH4_ADDR = 0x0510
    IPC_CH5_ADDR = 0x0514
    IPC_CH6_ADDR = 0x0518
    IPC_CH7_ADDR = 0x051C
    IPC_INTEN_ADDR = 0x0700

    # 中断向量定义
    INT_POWER = 0  # Power
    INT_RADIO = 1  # RADIO
    INT_UART0 = 2  # UART0
    INT_UART1 = 3  # UART1
    INT_SPI0 = 4  # SPI0
    INT_SPI1 = 5  # SPI1
    INT_SPI2 = 6  # SPI2
    INT_GPIOTE = 7  # GPIOTE
    INT_ADC = 8  # ADC
    INT_TIMER0 = 9  # TIMER0
    INT_TIMER1 = 10  # TIMER1
    INT_TIMER2 = 11  # TIMER2
    INT_TIMER3 = 12  # TIMER3
    INT_TIMER4 = 13  # TIMER4
    INT_RTC0 = 14  # RTC0
    INT_RTC1 = 15  # RTC1
    INT_TEMP = 16  # TEMP
    INT_RNG = 17  # RNG
    INT_WDT = 18  # WDT
    INT_IPC = 19  # IPC
    INT_PWM0 = 20  # PWM0
    INT_PWM1 = 21  # PWM1
    INT_PWM2 = 22  # PWM2
    INT_PWM3 = 23  # PWM3
    INT_ZAR = 24  # RESERVED
    INT_EGU0 = 25  # EGU0
    INT_EGU1 = 26  # EGU1
    INT_EGU2 = 27  # EGU2
    INT_EGU3 = 28  # EGU3
    INT_EGU4 = 29  # EGU4
    INT_EGU5 = 30  # EGU5
    INT_RESERVED = 31  # RESERVED
    INT_SPIM0 = 32  # SPIM0
    INT_SPIM1 = 33  # SPIM1
    INT_SPIM2 = 34  # SPIM2
    INT_RESERVED = 35  # RESERVED
    INT_RESERVED = 36  # RESERVED
    INT_USB = 37  # USB
    INT_RESERVED = 38  # RESERVED
    INT_RESERVED = 39  # RESERVED
    INT_RESERVED = 40  # RESERVED
    INT_RESERVED = 41  # RESERVED
    INT_RESERVED = 42  # RESERVED
    INT_CRYPTOCELL = 43  # CRYPTOCELL
    INT_RESERVED = 44  # RESERVED
    INT_RESERVED = 45  # RESERVED
    INT_RESERVED = 46  # RESERVED
    INT_RESERVED = 47  # RESERVED

    # 引脚定义
    PIN_VDD = 1  # 3.3V power supply
    PIN_VDD = 2  # 3.3V power supply
    PIN_DEC4 = 3  # Decoupling 4
    PIN_DEC5 = 4  # Decoupling 5
    PIN_P0_01 = 5  # GPIO Port 0.01
    PIN_P0_02 = 6  # GPIO Port 0.02
    PIN_P0_03 = 7  # GPIO Port 0.03
    PIN_P0_04 = 8  # GPIO Port 0.04
    PIN_P0_05 = 9  # GPIO Port 0.05
    PIN_P0_06 = 10  # GPIO Port 0.06
    PIN_P0_07 = 11  # GPIO Port 0.07
    PIN_P0_08 = 12  # GPIO Port 0.08
    PIN_P0_09 = 13  # GPIO Port 0.09
    PIN_P0_10 = 14  # GPIO Port 0.10
    PIN_P0_11 = 15  # GPIO Port 0.11
    PIN_P0_12 = 16  # GPIO Port 0.12
    PIN_P0_13 = 17  # GPIO Port 0.13
    PIN_P0_14 = 18  # GPIO Port 0.14
    PIN_P0_15 = 19  # GPIO Port 0.15
    PIN_P0_16 = 20  # GPIO Port 0.16
    PIN_P0_17 = 21  # GPIO Port 0.17
    PIN_P0_18 = 22  # GPIO Port 0.18
    PIN_P0_19 = 23  # GPIO Port 0.19
    PIN_P0_20 = 24  # GPIO Port 0.20
    PIN_P0_21 = 25  # GPIO Port 0.21
    PIN_P0_22 = 26  # GPIO Port 0.22
    PIN_P0_23 = 27  # GPIO Port 0.23
    PIN_P0_24 = 28  # GPIO Port 0.24
    PIN_P0_25 = 29  # GPIO Port 0.25
    PIN_P0_26 = 30  # GPIO Port 0.26
    PIN_P0_27 = 31  # GPIO Port 0.27
    PIN_P0_28 = 32  # GPIO Port 0.28
    PIN_P0_29 = 33  # GPIO Port 0.29
    PIN_P0_30 = 34  # GPIO Port 0.30
    PIN_P0_31 = 35  # GPIO Port 0.31
    PIN_P1_00 = 36  # GPIO Port 1.00
    PIN_P1_01 = 37  # GPIO Port 1.01
    PIN_P1_02 = 38  # GPIO Port 1.02
    PIN_P1_03 = 39  # GPIO Port 1.03
    PIN_P1_04 = 40  # GPIO Port 1.04
    PIN_P1_05 = 41  # GPIO Port 1.05
    PIN_P1_06 = 42  # GPIO Port 1.06
    PIN_P1_07 = 43  # GPIO Port 1.07
    PIN_P1_08 = 44  # GPIO Port 1.08
    PIN_P1_09 = 45  # GPIO Port 1.09
    PIN_P1_10 = 46  # GPIO Port 1.10
    PIN_P1_11 = 47  # GPIO Port 1.11
    PIN_P1_12 = 48  # GPIO Port 1.12
    PIN_P1_13 = 49  # GPIO Port 1.13
    PIN_P1_14 = 50  # GPIO Port 1.14
    PIN_P1_15 = 51  # GPIO Port 1.15
    PIN_VDD = 52  # 3.3V power supply
    PIN_VDD = 53  # 3.3V power supply
    PIN_DEC1 = 54  # Decoupling 1
    PIN_DEC2 = 55  # Decoupling 2
    PIN_DEC3 = 56  # Decoupling 3
    PIN_NFC1 = 57  # NFC 1
    PIN_NFC2 = 58  # NFC 2
    PIN_P0_16 = 59  # GPIO Port 0.16
    PIN_SWDIO = 60  # SWD I/O
    PIN_SWDCLK = 61  # SWD Clock
    PIN_RESET = 62  # Reset

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
        self._registers["BASEPRI"] = {
            "address": 0xE0000E24,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Base Priority Register",
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
        self._registers["CONTROL"] = {
            "address": 0xE0000E2C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Control Register",
            "value": 0
        }
        self._registers["FPSCR"] = {
            "address": 0xE0000EF34,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Status Control",
            "value": 0
        }
        self._registers["S0"] = {
            "address": 0xE0000EF00,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S0",
            "value": 0
        }
        self._registers["S1"] = {
            "address": 0xE0000EF04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S1",
            "value": 0
        }
        self._registers["S2"] = {
            "address": 0xE0000EF08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S2",
            "value": 0
        }
        self._registers["S3"] = {
            "address": 0xE0000EF0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S3",
            "value": 0
        }
        self._registers["S4"] = {
            "address": 0xE0000EF10,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S4",
            "value": 0
        }
        self._registers["S5"] = {
            "address": 0xE0000EF14,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S5",
            "value": 0
        }
        self._registers["S6"] = {
            "address": 0xE0000EF18,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S6",
            "value": 0
        }
        self._registers["S7"] = {
            "address": 0xE0000EF1C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S7",
            "value": 0
        }
        self._registers["S8"] = {
            "address": 0xE0000EF20,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S8",
            "value": 0
        }
        self._registers["S9"] = {
            "address": 0xE0000EF24,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S9",
            "value": 0
        }
        self._registers["S10"] = {
            "address": 0xE0000EF28,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S10",
            "value": 0
        }
        self._registers["S11"] = {
            "address": 0xE0000EF2C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S11",
            "value": 0
        }
        self._registers["S12"] = {
            "address": 0xE0000EF30,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S12",
            "value": 0
        }
        self._registers["S13"] = {
            "address": 0xE0000EF34,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S13",
            "value": 0
        }
        self._registers["S14"] = {
            "address": 0xE0000EF38,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S14",
            "value": 0
        }
        self._registers["S15"] = {
            "address": 0xE0000EF3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S15",
            "value": 0
        }
        self._registers["S16"] = {
            "address": 0xE0000EF40,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S16",
            "value": 0
        }
        self._registers["S17"] = {
            "address": 0xE0000EF44,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S17",
            "value": 0
        }
        self._registers["S18"] = {
            "address": 0xE0000EF48,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S18",
            "value": 0
        }
        self._registers["S19"] = {
            "address": 0xE0000EF4C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S19",
            "value": 0
        }
        self._registers["S20"] = {
            "address": 0xE0000EF50,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S20",
            "value": 0
        }
        self._registers["S21"] = {
            "address": 0xE0000EF54,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S21",
            "value": 0
        }
        self._registers["S22"] = {
            "address": 0xE0000EF58,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S22",
            "value": 0
        }
        self._registers["S23"] = {
            "address": 0xE0000EF5C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S23",
            "value": 0
        }
        self._registers["S24"] = {
            "address": 0xE0000EF60,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S24",
            "value": 0
        }
        self._registers["S25"] = {
            "address": 0xE0000EF64,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S25",
            "value": 0
        }
        self._registers["S26"] = {
            "address": 0xE0000EF68,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S26",
            "value": 0
        }
        self._registers["S27"] = {
            "address": 0xE0000EF6C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S27",
            "value": 0
        }
        self._registers["S28"] = {
            "address": 0xE0000EF70,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S28",
            "value": 0
        }
        self._registers["S29"] = {
            "address": 0xE0000EF74,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S29",
            "value": 0
        }
        self._registers["S30"] = {
            "address": 0xE0000EF78,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S30",
            "value": 0
        }
        self._registers["S31"] = {
            "address": 0xE0000EF7C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FPU Register S31",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["GPIO"] = {
            "base": 0x50000000,
            "type": "gpio",
            "description": "GPIO",
            "registers": {
                "OUT": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUTSET": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OUTCLR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IN": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIRSET": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIRCLR": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF0": {
                    "address": 0x0300,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF1": {
                    "address": 0x0304,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF2": {
                    "address": 0x0308,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF3": {
                    "address": 0x030C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF4": {
                    "address": 0x0310,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF5": {
                    "address": 0x0314,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF6": {
                    "address": 0x0318,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF7": {
                    "address": 0x031C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF8": {
                    "address": 0x0320,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF9": {
                    "address": 0x0324,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF10": {
                    "address": 0x0328,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF11": {
                    "address": 0x032C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF12": {
                    "address": 0x0330,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF13": {
                    "address": 0x0334,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF14": {
                    "address": 0x0338,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF15": {
                    "address": 0x033C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF16": {
                    "address": 0x0340,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF17": {
                    "address": 0x0344,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF18": {
                    "address": 0x0348,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF19": {
                    "address": 0x034C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF20": {
                    "address": 0x0350,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF21": {
                    "address": 0x0354,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF22": {
                    "address": 0x0358,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF23": {
                    "address": 0x035C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF24": {
                    "address": 0x0360,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF25": {
                    "address": 0x0364,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF26": {
                    "address": 0x0368,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF27": {
                    "address": 0x036C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF28": {
                    "address": 0x0370,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF29": {
                    "address": 0x0374,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF30": {
                    "address": 0x0378,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PIN_CNF31": {
                    "address": 0x037C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART0"] = {
            "base": 0x40002000,
            "type": "uart",
            "description": "UART0",
            "registers": {
                "TASKS_STARTRX": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STARTTX": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOPRX": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOPTX": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SUBSCRIBED_STARTRX": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SUBSCRIBED_STARTTX": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_CTS": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_NCTS": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RXDRDY": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_TXDRDY": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_ERROR": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXD": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXD": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BAUDRATE": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0x056C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTENSET": {
                    "address": 0x0704,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTENCLR": {
                    "address": 0x0708,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART1"] = {
            "base": 0x40003000,
            "type": "uart",
            "description": "UART1",
            "registers": {
                "TASKS_STARTRX": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STARTTX": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RXDRDY": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_TXDRDY": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXD": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXD": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BAUDRATE": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI0"] = {
            "base": 0x40003000,
            "type": "spi",
            "description": "SPI0",
            "registers": {
                "EVENTS_READY": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXD": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXD": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FREQUENCY": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0x056C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI1"] = {
            "base": 0x40004000,
            "type": "spi",
            "description": "SPI1",
            "registers": {
                "EVENTS_READY": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXD": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXD": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FREQUENCY": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0x056C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI2"] = {
            "base": 0x40005000,
            "type": "spi",
            "description": "SPI2",
            "registers": {
                "EVENTS_READY": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXD": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXD": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FREQUENCY": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C0"] = {
            "base": 0x40003000,
            "type": "i2c",
            "description": "I2C0",
            "registers": {
                "TASKS_STARTRX": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STARTTX": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_DONE": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_TXDSENT": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_ERROR": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXD": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXD": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADDRESS": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C1"] = {
            "base": 0x40004000,
            "type": "i2c",
            "description": "I2C1",
            "registers": {
                "TASKS_STARTRX": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STARTTX": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_DONE": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXD": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXD": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ADDRESS": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER0"] = {
            "base": 0x40008000,
            "type": "timer",
            "description": "Timer0",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_COUNT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CLEAR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC0": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC1": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC2": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC3": {
                    "address": 0x040C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SHORTS": {
                    "address": 0x0200,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MODE": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BITMODE": {
                    "address": 0x0514,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER1"] = {
            "base": 0x40009000,
            "type": "timer",
            "description": "Timer1",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_COUNT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CLEAR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC0": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC1": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC2": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC3": {
                    "address": 0x040C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SHORTS": {
                    "address": 0x0200,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER2"] = {
            "base": 0x4000A000,
            "type": "timer",
            "description": "Timer2",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_COUNT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CLEAR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC0": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC1": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC2": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC3": {
                    "address": 0x040C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER3"] = {
            "base": 0x4000B000,
            "type": "timer",
            "description": "Timer3",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_COUNT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CLEAR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC0": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC1": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC2": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC3": {
                    "address": 0x040C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER4"] = {
            "base": 0x4000C000,
            "type": "timer",
            "description": "Timer4",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_COUNT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CLEAR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC0": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC1": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC2": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC3": {
                    "address": 0x040C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RTC0"] = {
            "base": 0x4000B000,
            "type": "rtc",
            "description": "RTC0",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGOVRFLW": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_TICK": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_OVRFLW": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_COMPARE0": {
                    "address": 0x0140,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_COMPARE1": {
                    "address": 0x0144,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_COMPARE2": {
                    "address": 0x0148,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_COMPARE3": {
                    "address": 0x014C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC0": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC1": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC2": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC3": {
                    "address": 0x040C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PRESCALER": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TICK": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RTC1"] = {
            "base": 0x4000D000,
            "type": "rtc",
            "description": "RTC1",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_TICK": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_OVRFLW": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_COMPARE0": {
                    "address": 0x0140,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_COMPARE1": {
                    "address": 0x0144,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC0": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC1": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PRESCALER": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWM0"] = {
            "base": 0x4001C000,
            "type": "pwm",
            "description": "PWM0",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SEQSTART0": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SEQSTART1": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_NEXTSTEP": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_PWMPERIODEND": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_LOOPEND": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SEQEND0": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SEQEND1": {
                    "address": 0x0114,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ0_PTR": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ1_PTR": {
                    "address": 0x0514,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ0_CNT": {
                    "address": 0x0528,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ1_CNT": {
                    "address": 0x052C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ0_REFRESH": {
                    "address": 0x0530,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ1_REFRESH": {
                    "address": 0x0534,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DECODER": {
                    "address": 0x0540,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LOOP": {
                    "address": 0x0544,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MODE": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLKEN": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0548,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWM1"] = {
            "base": 0x4001D000,
            "type": "pwm",
            "description": "PWM1",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ0_PTR": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ1_PTR": {
                    "address": 0x0514,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MODE": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0548,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWM2"] = {
            "base": 0x4001E000,
            "type": "pwm",
            "description": "PWM2",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ0_PTR": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MODE": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWM3"] = {
            "base": 0x4001F000,
            "type": "pwm",
            "description": "PWM3",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SEQ0_PTR": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MODE": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC"] = {
            "base": 0x40012000,
            "type": "adc",
            "description": "ADC",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_DONE": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RESULTDONE": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_CALIBRATEDONE": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_CH_LIMITH": {
                    "address": 0x0114,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_CH_LIMITL": {
                    "address": 0x0118,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RESULT": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH0_CONFIG": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH1_CONFIG": {
                    "address": 0x0514,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH2_CONFIG": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH3_CONFIG": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH4_CONFIG": {
                    "address": 0x0520,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH5_CONFIG": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH6_CONFIG": {
                    "address": 0x0528,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH7_CONFIG": {
                    "address": 0x052C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0x0530,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CALIBRATELOAD": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["DAC"] = {
            "base": 0x40013000,
            "type": "dac",
            "description": "DAC",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_DONE": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "VALUE": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CEN": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["COMP"] = {
            "base": 0x40013000,
            "type": "analog",
            "description": "Analog Comparator",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SETTLE": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_READY": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_DOWN": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_UP": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_CROSS": {
                    "address": 0x0114,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RESULT": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EN": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASK_MODE": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "REFSEL": {
                    "address": 0x0508,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTREFSEL": {
                    "address": 0x050C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "THD": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HYST": {
                    "address": 0x0514,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SPEED": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ISOURCE": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSEL": {
                    "address": 0x0520,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "NOREF": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["QDEC"] = {
            "base": 0x40014000,
            "type": " quadrature",
            "description": "Quadrature Decoder",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RDCLRACC": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RDCLRDBL": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RDCLRPH": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_ready": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_DBLRDY": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_QCLR": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ACC": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ACCREAD": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DBLINC": {
                    "address": 0x0410,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DBL": {
                    "address": 0x0418,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DBLREAD": {
                    "address": 0x041C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PHASE": {
                    "address": 0x0420,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PHASEREAD": {
                    "address": 0x0424,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LEFLL": {
                    "address": 0x0428,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["EGU0"] = {
            "base": 0x40014000,
            "type": "egu",
            "description": "Event Generators Unit 0",
            "registers": {
                "TASKS_TRIGGER0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER2": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER3": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER4": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER5": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER6": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER7": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER8": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER9": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER10": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER11": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER12": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER13": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER14": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGGER15": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EVENT0": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EVENT1": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EVENT2": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EVENT3": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EVENT4": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EVENT5": {
                    "address": 0x0114,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RNG"] = {
            "base": 0x40006000,
            "type": "rng",
            "description": "Random Number Generator",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_VALRDY": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "VALUE": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["AES"] = {
            "base": 0x40005000,
            "type": "crypto",
            "description": "AES ECB",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_END": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_ERROR": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRYPTCNTXT": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRYPTCNTCPY": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRYPTCMD": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["CRYPTO"] = {
            "base": 0x4000E000,
            "type": "crypto",
            "description": "Cryptocell",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_DONE": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_ERROR": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMA": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMDS": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMDS_AMOUNT": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTENSET": {
                    "address": 0x0704,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTENCLR": {
                    "address": 0x0708,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTCONTEXT": {
                    "address": 0x0710,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USB"] = {
            "base": 0x40027000,
            "type": "usb",
            "description": "USB",
            "registers": {
                "TASKS_STARTUP": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SUSPEND": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RESUME": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_ENDRDY": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SUSPENDED": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RESUMED": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SOF": {
                    "address": 0x0114,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EPOF": {
                    "address": 0x0118,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_DATA": {
                    "address": 0x011C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP0DATADONE": {
                    "address": 0x0120,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP0SETUP": {
                    "address": 0x0124,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP0HALTD": {
                    "address": 0x0128,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP1DMA": {
                    "address": 0x0134,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP2DMA": {
                    "address": 0x0138,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP3DMA": {
                    "address": 0x013C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP4DMA": {
                    "address": 0x0140,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP1": {
                    "address": 0x0158,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP2": {
                    "address": 0x015C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP3": {
                    "address": 0x0160,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP4": {
                    "address": 0x0164,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP5": {
                    "address": 0x0168,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP6": {
                    "address": 0x016C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP7": {
                    "address": 0x0170,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP8": {
                    "address": 0x0174,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP9": {
                    "address": 0x0178,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP10": {
                    "address": 0x017C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP11": {
                    "address": 0x0180,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP12": {
                    "address": 0x0184,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP13": {
                    "address": 0x0188,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP14": {
                    "address": 0x018C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_EP15": {
                    "address": 0x0190,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USBADDR": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USBREQ": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USBVAL": {
                    "address": 0x0508,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USBINDEX": {
                    "address": 0x050C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USBCONFIG": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EPIN": {
                    "address": 0x0514,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EPOUT": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EPLEN": {
                    "address": 0x0520,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EPSIZE": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EPDMA": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EPDMA": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTENSET": {
                    "address": 0x0704,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTENCLR": {
                    "address": 0x0708,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["WDT"] = {
            "base": 0x40011000,
            "type": "wdt",
            "description": "Watchdog Timer",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_KEEP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_TIMEOUT": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RUNSTATUS": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "REQSTATUS": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRV": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RCV": {
                    "address": 0x0508,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG": {
                    "address": 0x050C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["NRF_RESET"] = {
            "base": 0x40000000,
            "type": "reset",
            "description": "Reset",
            "registers": {
                "RESET": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RESET_FAC": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RESET_NFAC": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["CLOCK"] = {
            "base": 0x40000000,
            "type": "clock",
            "description": "Clock",
            "registers": {
                "TASKS_HFCLKSTART": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_HFCLKSTOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_LFCLKSTART": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_LFCLKSTOP": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CAL": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CTTO": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_HFCLKSTATED": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_LFCLKSTATED": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_DONE": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_CTTO": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HFCLKSTAT": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LFCLKSTAT": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LFCLKSRC": {
                    "address": 0x0508,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTIV": {
                    "address": 0x050C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["POWER"] = {
            "base": 0x40000000,
            "type": "power",
            "description": "Power",
            "registers": {
                "TASKS_CONSTLAT": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_LOWPWR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_POWERDEBUG": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SLEEPDEBUG": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOTE"] = {
            "base": 0x40006000,
            "type": "gpiote",
            "description": "GPIO Tasks and Events",
            "registers": {
                "TASKS_SET0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SET1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SET2": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SET3": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CLR0": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CLR1": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CLR2": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_CLR3": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_IN0": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_IN1": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_IN2": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_IN3": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_IN4": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_IN5": {
                    "address": 0x0114,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_IN6": {
                    "address": 0x0118,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_IN7": {
                    "address": 0x011C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_TOUCH": {
                    "address": 0x0140,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_LISR": {
                    "address": 0x0144,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_LISF": {
                    "address": 0x0148,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_COUNT": {
                    "address": 0x014C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG0": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG1": {
                    "address": 0x0514,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG2": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG3": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG4": {
                    "address": 0x0520,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG5": {
                    "address": 0x0524,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG6": {
                    "address": 0x0528,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CONFIG7": {
                    "address": 0x052C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RTT"] = {
            "base": 0x40009000,
            "type": "rtt",
            "description": "Real Time Timer",
            "registers": {
                "TASKS_START": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_STOP": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_TRIGOVRFLW": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_TICK": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_OVRFLW": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_COMPARE0": {
                    "address": 0x0140,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC0": {
                    "address": 0x0400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC1": {
                    "address": 0x0404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC2": {
                    "address": 0x0408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC3": {
                    "address": 0x040C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PRESCALER": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["IPC"] = {
            "base": 0x40014000,
            "type": "ipc",
            "description": "Inter-Process Communication",
            "registers": {
                "TASKS_SEND0": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SEND1": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SEND2": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SEND3": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SEND4": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SEND5": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SEND6": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_SEND7": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RECEIVE0": {
                    "address": 0x0080,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RECEIVE1": {
                    "address": 0x0084,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RECEIVE2": {
                    "address": 0x0088,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RECEIVE3": {
                    "address": 0x008C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RECEIVE4": {
                    "address": 0x0090,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RECEIVE5": {
                    "address": 0x0094,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RECEIVE6": {
                    "address": 0x0098,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TASKS_RECEIVE7": {
                    "address": 0x009C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SENT0": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SENT1": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SENT2": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SENT3": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SENT4": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SENT5": {
                    "address": 0x0114,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SENT6": {
                    "address": 0x0118,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_SENT7": {
                    "address": 0x011C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RECEIVE0": {
                    "address": 0x0180,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RECEIVE1": {
                    "address": 0x0184,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RECEIVE2": {
                    "address": 0x0188,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RECEIVE3": {
                    "address": 0x018C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RECEIVE4": {
                    "address": 0x0190,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RECEIVE5": {
                    "address": 0x0194,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RECEIVE6": {
                    "address": 0x0198,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EVENTS_RECEIVE7": {
                    "address": 0x019C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH0": {
                    "address": 0x0500,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH1": {
                    "address": 0x0504,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH2": {
                    "address": 0x0508,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH3": {
                    "address": 0x050C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH4": {
                    "address": 0x0510,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH5": {
                    "address": 0x0514,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH6": {
                    "address": 0x0518,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CH7": {
                    "address": 0x051C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "INTEN": {
                    "address": 0x0700,
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
        return f"nRF52840({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = nRF52840()
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

"""
STM32F103C8T6设备定义 - Python模块
生成自: STMicroelectronics/STM32/STM32F103C8T6
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: 32-bit ARM Cortex-M3 MCU with 64KB Flash, 20KB RAM, 72MHz
CPU架构: ARM-Cortex-M3
位宽: 32位
时钟频率: 72000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class STM32F103C8T6:
    """STM32F103C8T6设备类"""

    # 设备信息
    DEVICE_NAME = "STM32F103C8T6"
    MANUFACTURER = "STMicroelectronics"
    FAMILY = "STM32"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M3"
    BITS = 32
    CLOCK_FREQUENCY = 72000000

    # 寄存器地址定义
    R0_ADDR = 0x00  # General Purpose Register 0
    R1_ADDR = 0x04  # General Purpose Register 1
    R2_ADDR = 0x08  # General Purpose Register 2
    R3_ADDR = 0x0C  # General Purpose Register 3
    R4_ADDR = 0x10  # General Purpose Register 4
    R5_ADDR = 0x14  # General Purpose Register 5
    R6_ADDR = 0x18  # General Purpose Register 6
    R7_ADDR = 0x1C  # General Purpose Register 7
    R8_ADDR = 0x20  # General Purpose Register 8
    R9_ADDR = 0x24  # General Purpose Register 9
    R10_ADDR = 0x28  # General Purpose Register 10
    R11_ADDR = 0x2C  # General Purpose Register 11
    R12_ADDR = 0x30  # General Purpose Register 12
    SP_ADDR = 0x34  # Stack Pointer
    LR_ADDR = 0x38  # Link Register
    PC_ADDR = 0x3C  # Program Counter
    XPSR_ADDR = 0x40  # Program Status Register

    # 内存段定义
    FLASH_START = 0x08000000
    FLASH_END = 0x0800FFFF
    FLASH_SIZE = 65536  # Main Flash Memory (64KB)
    SYSTEM_MEMORY_START = 0x1FFFF000
    SYSTEM_MEMORY_END = 0x1FFFF7FF
    SYSTEM_MEMORY_SIZE = 2048  # System Memory (2KB)
    SRAM_START = 0x20000000
    SRAM_END = 0x20004FFF
    SRAM_SIZE = 20480  # SRAM (20KB)
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x40023FFF
    PERIPHERAL_SIZE = 143360  # Peripheral Registers
    CORTEX_M_START = 0xE0000000
    CORTEX_M_END = 0xE00FFFFF
    CORTEX_M_SIZE = 1048576  # Core Peripheral Registers

    # 外设定义
    # Reset and Clock Control
    RCC_BASE = 0x40021000
    RCC_CR_ADDR = 0x00
    RCC_CFGR_ADDR = 0x04
    RCC_APB2ENR_ADDR = 0x18
    RCC_APB1ENR_ADDR = 0x1C
    # GPIO Port A
    GPIOA_BASE = 0x40010800
    GPIOA_CRL_ADDR = 0x00
    GPIOA_CRH_ADDR = 0x04
    GPIOA_IDR_ADDR = 0x08
    GPIOA_ODR_ADDR = 0x0C
    GPIOA_BSRR_ADDR = 0x10
    GPIOA_BRR_ADDR = 0x14
    GPIOA_LCKR_ADDR = 0x18
    # GPIO Port B
    GPIOB_BASE = 0x40010C00
    GPIOB_CRL_ADDR = 0x00
    GPIOB_CRH_ADDR = 0x04
    GPIOB_IDR_ADDR = 0x08
    GPIOB_ODR_ADDR = 0x0C
    GPIOB_BSRR_ADDR = 0x10
    GPIOB_BRR_ADDR = 0x14
    # GPIO Port C
    GPIOC_BASE = 0x40011000
    GPIOC_CRL_ADDR = 0x00
    GPIOC_CRH_ADDR = 0x04
    GPIOC_IDR_ADDR = 0x08
    GPIOC_ODR_ADDR = 0x0C
    GPIOC_BSRR_ADDR = 0x10
    # USART 1
    USART1_BASE = 0x40013800
    USART1_SR_ADDR = 0x00
    USART1_DR_ADDR = 0x04
    USART1_BRR_ADDR = 0x08
    USART1_CR1_ADDR = 0x0C
    USART1_CR2_ADDR = 0x10
    USART1_CR3_ADDR = 0x14
    USART1_GTPR_ADDR = 0x18
    # USART 2
    USART2_BASE = 0x40004400
    USART2_SR_ADDR = 0x00
    USART2_DR_ADDR = 0x04
    USART2_BRR_ADDR = 0x08
    USART2_CR1_ADDR = 0x0C
    # SPI 1
    SPI1_BASE = 0x40013000
    SPI1_CR1_ADDR = 0x00
    SPI1_CR2_ADDR = 0x04
    SPI1_SR_ADDR = 0x08
    SPI1_DR_ADDR = 0x0C
    # SPI 2
    SPI2_BASE = 0x40003800
    SPI2_CR1_ADDR = 0x00
    SPI2_CR2_ADDR = 0x04
    SPI2_SR_ADDR = 0x08
    SPI2_DR_ADDR = 0x0C
    # I2C 1
    I2C1_BASE = 0x40005400
    I2C1_CR1_ADDR = 0x00
    I2C1_CR2_ADDR = 0x04
    I2C1_SR1_ADDR = 0x08
    I2C1_SR2_ADDR = 0x0C
    I2C1_DR_ADDR = 0x10
    I2C1_CCR_ADDR = 0x14
    I2C1_TRISE_ADDR = 0x18
    # I2C 2
    I2C2_BASE = 0x40005800
    I2C2_CR1_ADDR = 0x00
    I2C2_CR2_ADDR = 0x04
    I2C2_SR1_ADDR = 0x08
    I2C2_SR2_ADDR = 0x0C
    I2C2_DR_ADDR = 0x10
    I2C2_CCR_ADDR = 0x14
    # Advanced Timer 1
    TIM1_BASE = 0x40012C00
    TIM1_CR1_ADDR = 0x00
    TIM1_CR2_ADDR = 0x04
    TIM1_SMCR_ADDR = 0x08
    TIM1_DIER_ADDR = 0x0C
    TIM1_SR_ADDR = 0x10
    TIM1_EGR_ADDR = 0x14
    TIM1_CCMR1_ADDR = 0x18
    TIM1_CCMR2_ADDR = 0x1C
    TIM1_CCER_ADDR = 0x20
    TIM1_CNT_ADDR = 0x24
    TIM1_PSC_ADDR = 0x28
    TIM1_ARR_ADDR = 0x2C
    TIM1_RCR_ADDR = 0x30
    TIM1_CCR1_ADDR = 0x34
    TIM1_CCR2_ADDR = 0x38
    TIM1_CCR3_ADDR = 0x3C
    TIM1_CCR4_ADDR = 0x40
    TIM1_BDTR_ADDR = 0x44
    # General Purpose Timer 2
    TIM2_BASE = 0x40000400
    TIM2_CR1_ADDR = 0x00
    TIM2_CNT_ADDR = 0x24
    TIM2_PSC_ADDR = 0x28
    TIM2_ARR_ADDR = 0x2C
    TIM2_CCR1_ADDR = 0x34
    TIM2_CCR2_ADDR = 0x38
    TIM2_CCR3_ADDR = 0x3C
    TIM2_CCR4_ADDR = 0x40
    # General Purpose Timer 3
    TIM3_BASE = 0x40000400
    TIM3_CR1_ADDR = 0x00
    TIM3_CNT_ADDR = 0x24
    TIM3_ARR_ADDR = 0x2C
    TIM3_CCR1_ADDR = 0x34
    TIM3_CCR2_ADDR = 0x38
    TIM3_CCR3_ADDR = 0x3C
    TIM3_CCR4_ADDR = 0x40
    # General Purpose Timer 4
    TIM4_BASE = 0x40000800
    TIM4_CR1_ADDR = 0x00
    TIM4_CNT_ADDR = 0x24
    TIM4_ARR_ADDR = 0x2C
    TIM4_CCR1_ADDR = 0x34
    TIM4_CCR2_ADDR = 0x38
    TIM4_CCR3_ADDR = 0x3C
    TIM4_CCR4_ADDR = 0x40
    # ADC 1
    ADC1_BASE = 0x40012400
    ADC1_SR_ADDR = 0x00
    ADC1_CR1_ADDR = 0x04
    ADC1_CR2_ADDR = 0x08
    ADC1_SMPR1_ADDR = 0x0C
    ADC1_SMPR2_ADDR = 0x10
    ADC1_JOFR1_ADDR = 0x14
    ADC1_JOFR2_ADDR = 0x18
    ADC1_JOFR3_ADDR = 0x1C
    ADC1_JOFR4_ADDR = 0x20
    ADC1_HTR_ADDR = 0x24
    ADC1_LTR_ADDR = 0x28
    ADC1_SQRT1_ADDR = 0x2C
    ADC1_SQRT2_ADDR = 0x30
    ADC1_SQRT3_ADDR = 0x34
    ADC1_JSQR_ADDR = 0x38
    ADC1_JDR1_ADDR = 0x3C
    ADC1_JDR2_ADDR = 0x40
    ADC1_JDR3_ADDR = 0x44
    ADC1_JDR4_ADDR = 0x48
    ADC1_DR_ADDR = 0x4C
    # DMA Controller 1
    DMA1_BASE = 0x40020000
    DMA1_ISR_ADDR = 0x00
    DMA1_IFCR_ADDR = 0x04
    DMA1_CCR1_ADDR = 0x08
    DMA1_CNDTR1_ADDR = 0x0C
    DMA1_CPAR1_ADDR = 0x10
    DMA1_CMAR1_ADDR = 0x14
    DMA1_CCR2_ADDR = 0x1C
    DMA1_CNDTR2_ADDR = 0x20
    DMA1_CPAR2_ADDR = 0x24
    DMA1_CMAR2_ADDR = 0x28
    # Power Control
    PWR_BASE = 0x40007000
    PWR_CR_ADDR = 0x00
    PWR_CSR_ADDR = 0x04
    # Backup Registers
    BKP_BASE = 0x40006C00
    BKP_DR1_ADDR = 0x04
    BKP_DR2_ADDR = 0x08
    BKP_CSR_ADDR = 0x2C
    # Window Watchdog
    WWDG_BASE = 0x40002C00
    WWDG_CR_ADDR = 0x00
    WWDG_CFR_ADDR = 0x04
    WWDG_SR_ADDR = 0x08
    # Independent Watchdog
    IWDG_BASE = 0x40003000
    IWDG_KR_ADDR = 0x00
    IWDG_PR_ADDR = 0x04
    IWDG_RLR_ADDR = 0x08
    # External Interrupt/Event Controller
    EXTI_BASE = 0x40010400
    EXTI_IMR_ADDR = 0x00
    EXTI_EMR_ADDR = 0x04
    EXTI_RTSR_ADDR = 0x08
    EXTI_FTSR_ADDR = 0x0C
    EXTI_SWIER_ADDR = 0x10
    EXTI_PR_ADDR = 0x14
    # Alternate Function IO
    AFIO_BASE = 0x40010000
    AFIO_EVCR_ADDR = 0x00
    AFIO_MAPR_ADDR = 0x04
    AFIO_EXTICR1_ADDR = 0x08
    AFIO_EXTICR2_ADDR = 0x0C
    AFIO_EXTICR3_ADDR = 0x10
    AFIO_MAPR2_ADDR = 0x1C

    # 中断向量定义
    INT_WWDG = 0  # Window Watchdog Interrupt
    INT_PVD = 1  # PVD through EXTI Line detection
    INT_TAMPER = 2  # Tamper Interrupt
    INT_RTC = 3  # RTC Global Interrupt
    INT_FLASH = 4  # FLASH Global Interrupt
    INT_RCC = 5  # RCC Global Interrupt
    INT_EXTI0 = 6  # EXTI Line 0 Interrupt
    INT_EXTI1 = 7  # EXTI Line 1 Interrupt
    INT_EXTI2 = 8  # EXTI Line 2 Interrupt
    INT_EXTI3 = 9  # EXTI Line 3 Interrupt
    INT_EXTI4 = 10  # EXTI Line 4 Interrupt
    INT_DMA1_CHANNEL1 = 11  # DMA1 Channel 1 Interrupt
    INT_DMA1_CHANNEL2 = 12  # DMA1 Channel 2 Interrupt
    INT_DMA1_CHANNEL3 = 13  # DMA1 Channel 3 Interrupt
    INT_DMA1_CHANNEL4 = 14  # DMA1 Channel 4 Interrupt
    INT_DMA1_CHANNEL5 = 15  # DMA1 Channel 5 Interrupt
    INT_DMA1_CHANNEL6 = 16  # DMA1 Channel 6 Interrupt
    INT_DMA1_CHANNEL7 = 17  # DMA1 Channel 7 Interrupt
    INT_ADC1_2 = 18  # ADC1 and ADC2 Global Interrupt
    INT_USB_HP_CAN_TX = 19  # USB HP/CAN TX Interrupts
    INT_USB_LP_CAN_RX0 = 20  # USB LP/CAN RX0 Interrupt
    INT_CAN_RX1 = 21  # CAN RX1 Interrupt
    INT_CAN_SCE = 22  # CAN SCE Interrupt
    INT_EXTI9_5 = 23  # EXTI Line 9..5 Interrupt
    INT_TIM1_BRK = 25  # TIM1 Break Interrupt
    INT_TIM1_UP = 26  # TIM1 Update Interrupt
    INT_TIM1_TRG_COM = 27  # TIM1 Trigger and Commutation
    INT_TIM1_CC = 28  # TIM1 Capture Compare Interrupt
    INT_TIM2 = 29  # TIM2 Global Interrupt
    INT_TIM3 = 30  # TIM3 Global Interrupt
    INT_TIM4 = 31  # TIM4 Global Interrupt
    INT_I2C1_EV = 32  # I2C1 Event Interrupt
    INT_I2C1_ER = 33  # I2C1 Error Interrupt
    INT_I2C2_EV = 34  # I2C2 Event Interrupt
    INT_I2C2_ER = 35  # I2C2 Error Interrupt
    INT_SPI1 = 35  # SPI1 Global Interrupt
    INT_SPI2 = 36  # SPI2 Global Interrupt
    INT_USART1 = 37  # USART1 Global Interrupt
    INT_USART2 = 38  # USART2 Global Interrupt
    INT_USART3 = 39  # USART3 Global Interrupt
    INT_EXTI15_10 = 40  # EXTI Line 15..10 Interrupt
    INT_RTCALARM = 41  # RTC Alarm through EXTI
    INT_USBWAKEUP = 42  # USB Wakeup from suspend

    # 引脚定义
    PIN_VBAT = 1  # Battery Supply
    PIN_PC13 = 2  # GPIO Port C Pin 13
    PIN_PC14 = 3  # GPIO Port C Pin 14
    PIN_PC15 = 4  # GPIO Port C Pin 15
    PIN_PD0 = 5  # GPIO Port D Pin 0
    PIN_PD1 = 6  # GPIO Port D Pin 1
    PIN_NRST = 7  # Reset
    PIN_VSSA = 8  # Analog Ground
    PIN_VDDA = 9  # Analog Supply
    PIN_PA0 = 10  # GPIO Port A Pin 0 / ADC1_IN0
    PIN_PA1 = 11  # GPIO Port A Pin 1 / ADC1_IN1
    PIN_PA2 = 12  # GPIO Port A Pin 2 / ADC1_IN2 / USART2_TX
    PIN_PA3 = 13  # GPIO Port A Pin 3 / ADC1_IN3 / USART2_RX
    PIN_PA4 = 14  # GPIO Port A Pin 4 / DAC_OUT1 / SPI1_NSS
    PIN_PA5 = 15  # GPIO Port A Pin 5 / DAC_OUT2 / SPI1_SCK
    PIN_PA6 = 16  # GPIO Port A Pin 6 / ADC1_IN6 / SPI1_MISO / TIM3_CH1
    PIN_PA7 = 17  # GPIO Port A Pin 7 / ADC1_IN7 / SPI1_MOSI / TIM3_CH2
    PIN_PB0 = 18  # GPIO Port B Pin 0 / ADC1_IN8 / TIM3_CH3
    PIN_PB1 = 19  # GPIO Port B Pin 1 / ADC1_IN9 / TIM3_CH4
    PIN_PB2 = 20  # GPIO Port B Pin 2
    PIN_PB10 = 21  # GPIO Port B Pin 10 / I2C2_SCL / USART3_TX
    PIN_PB11 = 22  # GPIO Port B Pin 11 / I2C2_SDA / USART3_RX
    PIN_VSS = 23  # Ground
    PIN_VDD = 24  # Digital Supply
    PIN_PB12 = 25  # GPIO Port B Pin 12 / SPI2_NSS / I2C2_SMBA
    PIN_PB13 = 26  # GPIO Port B Pin 13 / SPI2_SCK / USART3_CK
    PIN_PB14 = 27  # GPIO Port B Pin 14 / SPI2_MISO / USART3_RTS
    PIN_PB15 = 28  # GPIO Port B Pin 15 / SPI2_MOSI / USART3_CTS
    PIN_PA8 = 29  # GPIO Port A Pin 8 / USART1_CK / TIM1_CH1 / MCO
    PIN_PA9 = 30  # GPIO Port A Pin 9 / USART1_TX / TIM1_CH2
    PIN_PA10 = 31  # GPIO Port A Pin 10 / USART1_RX / TIM1_CH3
    PIN_PA11 = 32  # GPIO Port A Pin 11 / USART1_CT / TIM1_CH4 / CAN_RX
    PIN_PA12 = 33  # GPIO Port A Pin 12 / USART1_RT / TIM1_ETR / CAN_TX
    PIN_PA13 = 34  # JTMS/SWDIO
    PIN_PA14 = 37  # JTCK/SWCLK
    PIN_PA15 = 38  # GPIO Port A Pin 15 / JTDI / TIM2_CH1_ETR / SPI1_NSS
    PIN_PB3 = 39  # GPIO Port B Pin 3 / JTDO / TIM2_CH2 / SPI1_SCK
    PIN_PB4 = 40  # GPIO Port B Pin 4 / JNTRST / TIM3_CH1 / SPI1_MISO
    PIN_PB5 = 41  # GPIO Port B Pin 5 / TIM3_CH2 / SPI1_MOSI / I2C1_SMBA
    PIN_PB6 = 42  # GPIO Port B Pin 6 / TIM4_CH1 / I2C1_SCL / USART1_TX
    PIN_PB7 = 43  # GPIO Port B Pin 7 / TIM4_CH2 / I2C1_SDA / USART1_RX
    PIN_BOOT0 = 44  # Boot Selection
    PIN_PB8 = 45  # GPIO Port B Pin 8 / TIM4_CH3 / I2C1_SCL / CAN_RX
    PIN_PB9 = 46  # GPIO Port B Pin 9 / TIM4_CH4 / I2C1_SDA / CAN_TX
    PIN_VSS = 47  # Ground
    PIN_VDD = 48  # Digital Supply

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
            "address": 0x00,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 0",
            "value": 0
        }
        self._registers["R1"] = {
            "address": 0x04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 1",
            "value": 0
        }
        self._registers["R2"] = {
            "address": 0x08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 2",
            "value": 0
        }
        self._registers["R3"] = {
            "address": 0x0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 3",
            "value": 0
        }
        self._registers["R4"] = {
            "address": 0x10,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 4",
            "value": 0
        }
        self._registers["R5"] = {
            "address": 0x14,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 5",
            "value": 0
        }
        self._registers["R6"] = {
            "address": 0x18,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 6",
            "value": 0
        }
        self._registers["R7"] = {
            "address": 0x1C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 7",
            "value": 0
        }
        self._registers["R8"] = {
            "address": 0x20,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 8",
            "value": 0
        }
        self._registers["R9"] = {
            "address": 0x24,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 9",
            "value": 0
        }
        self._registers["R10"] = {
            "address": 0x28,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 10",
            "value": 0
        }
        self._registers["R11"] = {
            "address": 0x2C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 11",
            "value": 0
        }
        self._registers["R12"] = {
            "address": 0x30,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "General Purpose Register 12",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x34,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["LR"] = {
            "address": 0x38,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Link Register",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["xPSR"] = {
            "address": 0x40,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Status Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["RCC"] = {
            "base": 0x40021000,
            "type": "clock",
            "description": "Reset and Clock Control",
            "registers": {
                "CR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB2ENR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB1ENR": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOA"] = {
            "base": 0x40010800,
            "type": "gpio",
            "description": "GPIO Port A",
            "registers": {
                "CRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRH": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ODR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSRR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LCKR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOB"] = {
            "base": 0x40010C00,
            "type": "gpio",
            "description": "GPIO Port B",
            "registers": {
                "CRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRH": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ODR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSRR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOC"] = {
            "base": 0x40011000,
            "type": "gpio",
            "description": "GPIO Port C",
            "registers": {
                "CRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRH": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ODR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSRR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART1"] = {
            "base": 0x40013800,
            "type": "uart",
            "description": "USART 1",
            "registers": {
                "SR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR3": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GTPR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART2"] = {
            "base": 0x40004400,
            "type": "uart",
            "description": "USART 2",
            "registers": {
                "SR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI1"] = {
            "base": 0x40013000,
            "type": "spi",
            "description": "SPI 1",
            "registers": {
                "CR1": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI2"] = {
            "base": 0x40003800,
            "type": "spi",
            "description": "SPI 2",
            "registers": {
                "CR1": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C1"] = {
            "base": 0x40005400,
            "type": "i2c",
            "description": "I2C 1",
            "registers": {
                "CR1": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR1": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR2": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TRISE": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C2"] = {
            "base": 0x40005800,
            "type": "i2c",
            "description": "I2C 2",
            "registers": {
                "CR1": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR1": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR2": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIM1"] = {
            "base": 0x40012C00,
            "type": "timer",
            "description": "Advanced Timer 1",
            "registers": {
                "CR1": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMCR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIER": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EGR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR1": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR2": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCER": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x2C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RCR": {
                    "address": 0x30,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x34,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x38,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR3": {
                    "address": 0x3C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR4": {
                    "address": 0x40,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BDTR": {
                    "address": 0x44,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIM2"] = {
            "base": 0x40000400,
            "type": "timer",
            "description": "General Purpose Timer 2",
            "registers": {
                "CR1": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x2C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x34,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x38,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR3": {
                    "address": 0x3C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR4": {
                    "address": 0x40,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIM3"] = {
            "base": 0x40000400,
            "type": "timer",
            "description": "General Purpose Timer 3",
            "registers": {
                "CR1": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x2C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x34,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x38,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR3": {
                    "address": 0x3C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR4": {
                    "address": 0x40,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIM4"] = {
            "base": 0x40000800,
            "type": "timer",
            "description": "General Purpose Timer 4",
            "registers": {
                "CR1": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x2C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x34,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x38,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR3": {
                    "address": 0x3C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR4": {
                    "address": 0x40,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC1"] = {
            "base": 0x40012400,
            "type": "adc",
            "description": "ADC 1",
            "registers": {
                "SR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMPR1": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMPR2": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JOFR1": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JOFR2": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JOFR3": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JOFR4": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HTR": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LTR": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SQRT1": {
                    "address": 0x2C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SQRT2": {
                    "address": 0x30,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SQRT3": {
                    "address": 0x34,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JSQR": {
                    "address": 0x38,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JDR1": {
                    "address": 0x3C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JDR2": {
                    "address": 0x40,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JDR3": {
                    "address": 0x44,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JDR4": {
                    "address": 0x48,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x4C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["DMA1"] = {
            "base": 0x40020000,
            "type": "dma",
            "description": "DMA Controller 1",
            "registers": {
                "ISR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IFCR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNDTR1": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CPAR1": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMAR1": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNDTR2": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CPAR2": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMAR2": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PWR"] = {
            "base": 0x40007000,
            "type": "power",
            "description": "Power Control",
            "registers": {
                "CR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CSR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["BKP"] = {
            "base": 0x40006C00,
            "type": "backup",
            "description": "Backup Registers",
            "registers": {
                "DR1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR2": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CSR": {
                    "address": 0x2C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["WWDG"] = {
            "base": 0x40002C00,
            "type": "watchdog",
            "description": "Window Watchdog",
            "registers": {
                "CR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["IWDG"] = {
            "base": 0x40003000,
            "type": "watchdog",
            "description": "Independent Watchdog",
            "registers": {
                "KR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RLR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["EXTI"] = {
            "base": 0x40010400,
            "type": "exti",
            "description": "External Interrupt/Event Controller",
            "registers": {
                "IMR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EMR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTSR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FTSR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SWIER": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["AFIO"] = {
            "base": 0x40010000,
            "type": "gpio",
            "description": "Alternate Function IO",
            "registers": {
                "EVCR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MAPR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTICR1": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTICR2": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTICR3": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MAPR2": {
                    "address": 0x1C,
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
        return f"STM32F103C8T6({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = STM32F103C8T6()
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

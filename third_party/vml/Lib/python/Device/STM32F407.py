"""
STM32F407VGT6设备定义 - Python模块
生成自: STMicroelectronics/STM32F4/STM32F407VGT6
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: High-performance ARM Cortex-M4 with FPU, 168MHz, 1MB Flash, 192KB SRAM
CPU架构: ARM-Cortex-M4
位宽: 32位
时钟频率: 16000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class STM32F407VGT6:
    """STM32F407VGT6设备类"""

    # 设备信息
    DEVICE_NAME = "STM32F407VGT6"
    MANUFACTURER = "STMicroelectronics"
    FAMILY = "STM32F4"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M4"
    BITS = 32
    CLOCK_FREQUENCY = 16000000

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
    PSR_Q_BIT = 27  # SAT Flag
    PSR_ICI_BIT = 0  # ICI/IT
    PSR_GE_BIT = 0  # Greater than or Equal
    PSR_IT_BIT = 0  # IT status
    PSR_Q_BIT = 9  # APSR
    PSR_IPSR_BIT = 0  # IPSR
    PRIMASK_ADDR = 0xE0000E20  # Priority Mask Register
    BASEPRI_ADDR = 0xE0000E24  # Base Priority Register
    FAULTMASK_ADDR = 0xE0000E28  # Fault Mask Register
    CONTROL_ADDR = 0xE0000E2C  # Control Register
    FPSCR_ADDR = E0000EF34  # FPU Status Control
    S0_ADDR = 0xE0000EF00  # FP Register 0
    S1_ADDR = 0xE0000EF04  # FP Register 1
    S2_ADDR = 0xE0000EF08  # FP Register 2
    S3_ADDR = 0xE0000EF0C  # FP Register 3
    S4_ADDR = 0xE0000EF10  # FP Register 4
    S5_ADDR = 0xE0000EF14  # FP Register 5
    S6_ADDR = 0xE0000EF18  # FP Register 6
    S7_ADDR = 0xE0000EF1C  # FP Register 7
    S8_ADDR = 0xE0000EF20  # FP Register 8
    S9_ADDR = 0xE0000EF24  # FP Register 9
    S10_ADDR = 0xE0000EF28  # FP Register 10
    S11_ADDR = 0xE0000EF2C  # FP Register 11
    S12_ADDR = 0xE0000EF30  # FP Register 12
    S13_ADDR = 0xE0000EF34  # FP Register 13
    S14_ADDR = 0xE0000EF38  # FP Register 14
    S15_ADDR = 0xE0000EF3C  # FP Register 15
    S16_ADDR = 0xE0000EF40  # FP Register 16
    S17_ADDR = 0xE0000EF44  # FP Register 17
    S18_ADDR = 0xE0000EF48  # FP Register 18
    S19_ADDR = 0xE0000EF4C  # FP Register 19
    S20_ADDR = 0xE0000EF50  # FP Register 20
    S21_ADDR = 0xE0000EF54  # FP Register 21
    S22_ADDR = 0xE0000EF58  # FP Register 22
    S23_ADDR = 0xE0000EF5C  # FP Register 23
    S24_ADDR = 0xE0000EF60  # FP Register 24
    S25_ADDR = 0xE0000EF64  # FP Register 25
    S26_ADDR = 0xE0000EF68  # FP Register 26
    S27_ADDR = 0xE0000EF6C  # FP Register 27
    S28_ADDR = 0xE0000EF70  # FP Register 28
    S29_ADDR = 0xE0000EF74  # FP Register 29
    S30_ADDR = 0xE0000EF78  # FP Register 30
    S31_ADDR = 0xE0000EF7C  # FP Register 31

    # 内存段定义
    FLASH_START = 0x08000000
    FLASH_END = 0x080FFFFF
    FLASH_SIZE = 1048576  # Main Flash (1MB)
    SYSTEM_START = 0x1FFF0000
    SYSTEM_END = 0x1FFF7FFF
    SYSTEM_SIZE = 32768  # System Flash (32KB)
    OPTION_START = 0x1FFF8000
    OPTION_END = 0x1FFFC000
    OPTION_SIZE = 16384  # Option Bytes (16KB)
    SRAM1_START = 0x20000000
    SRAM1_END = 0x2001FFFF
    SRAM1_SIZE = 131072  # SRAM1 (128KB)
    SRAM2_START = 0x20020000
    SRAM2_END = 0x2002FFFF
    SRAM2_SIZE = 65536  # SRAM2 (64KB)
    CCM_START = 0x20030000
    CCM_END = 0x2003FFFF
    CCM_SIZE = 65536  # CCM RAM (64KB)
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x40023FFF
    PERIPHERAL_SIZE = 147456  # Peripheral Registers
    FMC_START = 0x40020000
    FMC_END = 0x40023FFF
    FMC_SIZE = 16384  # FMC Registers
    FSMC_START = 0x40000000
    FSMC_END = 0x400003FF
    FSMC_SIZE = 1024  # FSMC Registers
    GPIOA_START = 0x40020000
    GPIOA_END = 0x400203FF
    GPIOA_SIZE = 1024  # GPIOA Registers

    # 外设定义
    # Reset and Clock Control
    RCC_BASE = 0x40023800
    RCC_CR_ADDR = 0x0000
    RCC_PLLCFGR_ADDR = 0x0004
    RCC_CFGR_ADDR = 0x0008
    RCC_CIR_ADDR = 0x000C
    RCC_APB2RSTR_ADDR = 0x0010
    RCC_APB1RSTR_ADDR = 0x0014
    RCC_AHB1ENR_ADDR = 0x0030
    RCC_AHB2ENR_ADDR = 0x0034
    RCC_AHB3ENR_ADDR = 0x0038
    RCC_APB2ENR_ADDR = 0x0040
    RCC_APB1ENR_ADDR = 0x0044
    RCC_BDCR_ADDR = 0x0050
    RCC_CSR_ADDR = 0x0054
    RCC_AHB1RSTR_ADDR = 0x0020
    RCC_AHB2RSTR_ADDR = 0x0024
    RCC_AHB3RSTR_ADDR = 0x0028
    RCC_CFGR2_ADDR = 0x0018
    RCC_CFGR3_ADDR = 0x001C
    RCC_PLL2CFGR_ADDR = 0x0060
    RCC_PLL2DIV_ADDR = 0x0064
    RCC_PLL3CFGR_ADDR = 0x0068
    RCC_PLL3DIV_ADDR = 0x006C
    # Flash Interface
    FLASH_BASE = 0x40023C00
    FLASH_ACR_ADDR = 0x0000
    FLASH_KEYR_ADDR = 0x0004
    FLASH_OPTKEYR_ADDR = 0x0008
    FLASH_SR_ADDR = 0x000C
    FLASH_CR_ADDR = 0x0010
    FLASH_OPTCR_ADDR = 0x0014
    FLASH_OPTCR1_ADDR = 0x0018
    # Power Control
    PWR_BASE = 0x40007000
    PWR_CR_ADDR = 0x0000
    PWR_CSR_ADDR = 0x0004
    # DMA1 Controller
    DMA1_BASE = 0x40026000
    DMA1_LIFCR_ADDR = 0x0000
    DMA1_HIFCR_ADDR = 0x0004
    DMA1_LISR_ADDR = 0x0008
    DMA1_HISR_ADDR = 0x000C
    DMA1_S0CR_ADDR = 0x0010
    DMA1_S0NDTR_ADDR = 0x0014
    DMA1_S0PAR_ADDR = 0x0018
    DMA1_S0M0AR_ADDR = 0x001C
    DMA1_S0M1AR_ADDR = 0x0020
    DMA1_S0FCR_ADDR = 0x0024
    DMA1_S1CR_ADDR = 0x0028
    DMA1_S1NDTR_ADDR = 0x002C
    DMA1_S2CR_ADDR = 0x0040
    DMA1_S3CR_ADDR = 0x0058
    DMA1_S4CR_ADDR = 0x0070
    DMA1_S5CR_ADDR = 0x0088
    DMA1_S6CR_ADDR = 0x00A0
    DMA1_S7CR_ADDR = 0x00B8
    # DMA2 Controller
    DMA2_BASE = 0x40026400
    DMA2_LIFCR_ADDR = 0x0000
    DMA2_HIFCR_ADDR = 0x0004
    DMA2_LISR_ADDR = 0x0008
    DMA2_HISR_ADDR = 0x000C
    DMA2_S0CR_ADDR = 0x0010
    DMA2_S1CR_ADDR = 0x0028
    DMA2_S2CR_ADDR = 0x0040
    DMA2_S3CR_ADDR = 0x0058
    DMA2_S4CR_ADDR = 0x0070
    DMA2_S5CR_ADDR = 0x0088
    DMA2_S6CR_ADDR = 0x00A0
    DMA2_S7CR_ADDR = 0x00B8
    # USART1
    USART1_BASE = 0x40011000
    USART1_SR_ADDR = 0x0000
    USART1_DR_ADDR = 0x0004
    USART1_BRR_ADDR = 0x0008
    USART1_CR1_ADDR = 0x000C
    USART1_CR2_ADDR = 0x0010
    USART1_CR3_ADDR = 0x0014
    USART1_GTPR_ADDR = 0x0018
    # USART2
    USART2_BASE = 0x40004400
    USART2_SR_ADDR = 0x0000
    USART2_DR_ADDR = 0x0004
    USART2_BRR_ADDR = 0x0008
    USART2_CR1_ADDR = 0x000C
    USART2_CR2_ADDR = 0x0010
    USART2_CR3_ADDR = 0x0014
    # USART3
    USART3_BASE = 0x40004800
    USART3_SR_ADDR = 0x0000
    USART3_DR_ADDR = 0x0004
    USART3_BRR_ADDR = 0x0008
    USART3_CR1_ADDR = 0x000C
    USART3_CR2_ADDR = 0x0010
    USART3_CR3_ADDR = 0x0014
    # SPI1
    SPI1_BASE = 0x40013000
    SPI1_CR1_ADDR = 0x0000
    SPI1_CR2_ADDR = 0x0004
    SPI1_SR_ADDR = 0x0008
    SPI1_DR_ADDR = 0x000C
    SPI1_CRCPR_ADDR = 0x0010
    SPI1_RXCRCR_ADDR = 0x0014
    SPI1_TXCRCR_ADDR = 0x0018
    SPI1_I2SCFGR_ADDR = 0x001C
    # SPI2
    SPI2_BASE = 0x40003800
    SPI2_CR1_ADDR = 0x0000
    SPI2_CR2_ADDR = 0x0004
    SPI2_SR_ADDR = 0x0008
    SPI2_DR_ADDR = 0x000C
    SPI2_CRCPR_ADDR = 0x0010
    SPI2_RXCRCR_ADDR = 0x0014
    # SPI3
    SPI3_BASE = 0x40003C00
    SPI3_CR1_ADDR = 0x0000
    SPI3_CR2_ADDR = 0x0004
    SPI3_SR_ADDR = 0x0008
    SPI3_DR_ADDR = 0x000C
    # I2C1
    I2C1_BASE = 0x40005400
    I2C1_CR1_ADDR = 0x0000
    I2C1_CR2_ADDR = 0x0004
    I2C1_OAR1_ADDR = 0x0008
    I2C1_OAR2_ADDR = 0x000C
    I2C1_DR_ADDR = 0x0010
    I2C1_SR1_ADDR = 0x0014
    I2C1_SR2_ADDR = 0x0018
    I2C1_CCR_ADDR = 0x001C
    I2C1_TRISE_ADDR = 0x0020
    I2C1_FLTR_ADDR = 0x0024
    # I2C2
    I2C2_BASE = 0x40005800
    I2C2_CR1_ADDR = 0x0000
    I2C2_CR2_ADDR = 0x0004
    I2C2_OAR1_ADDR = 0x0008
    I2C2_OAR2_ADDR = 0x000C
    I2C2_DR_ADDR = 0x0010
    I2C2_SR1_ADDR = 0x0014
    I2C2_SR2_ADDR = 0x0018
    I2C2_CCR_ADDR = 0x001C
    I2C2_TRISE_ADDR = 0x0020
    # I2C3
    I2C3_BASE = 0x40005C00
    I2C3_CR1_ADDR = 0x0000
    I2C3_CR2_ADDR = 0x0004
    I2C3_OAR1_ADDR = 0x0008
    I2C3_DR_ADDR = 0x0010
    I2C3_SR1_ADDR = 0x0014
    I2C3_SR2_ADDR = 0x0018
    I2C3_CCR_ADDR = 0x001C
    # Advanced Timer 1
    TIM1_BASE = 0x40012C00
    TIM1_CR1_ADDR = 0x0000
    TIM1_CR2_ADDR = 0x0004
    TIM1_SMCR_ADDR = 0x0008
    TIM1_DIER_ADDR = 0x000C
    TIM1_SR_ADDR = 0x0010
    TIM1_EGR_ADDR = 0x0014
    TIM1_CCMR1_ADDR = 0x0018
    TIM1_CCMR2_ADDR = 0x001C
    TIM1_CCER_ADDR = 0x0020
    TIM1_CNT_ADDR = 0x0024
    TIM1_PSC_ADDR = 0x0028
    TIM1_ARR_ADDR = 0x002C
    TIM1_RCR_ADDR = 0x0030
    TIM1_CCR1_ADDR = 0x0034
    TIM1_CCR2_ADDR = 0x0038
    TIM1_CCR3_ADDR = 0x003C
    TIM1_CCR4_ADDR = 0x0040
    TIM1_BDTR_ADDR = 0x0044
    TIM1_DCR_ADDR = 0x0048
    TIM1_DMAR_ADDR = 0x004C
    # General Purpose Timer 2
    TIM2_BASE = 0x40000000
    TIM2_CR1_ADDR = 0x0000
    TIM2_CR2_ADDR = 0x0004
    TIM2_SMCR_ADDR = 0x0008
    TIM2_DIER_ADDR = 0x000C
    TIM2_SR_ADDR = 0x0010
    TIM2_EGR_ADDR = 0x0014
    TIM2_CCMR1_ADDR = 0x0018
    TIM2_CCMR2_ADDR = 0x001C
    TIM2_CCER_ADDR = 0x0020
    TIM2_CNT_ADDR = 0x0024
    TIM2_PSC_ADDR = 0x0028
    TIM2_ARR_ADDR = 0x002C
    TIM2_CCR1_ADDR = 0x0034
    TIM2_CCR2_ADDR = 0x0038
    TIM2_CCR3_ADDR = 0x003C
    TIM2_CCR4_ADDR = 0x0040
    # General Purpose Timer 3
    TIM3_BASE = 0x40000400
    TIM3_CR1_ADDR = 0x0000
    TIM3_SR_ADDR = 0x0010
    TIM3_CCMR1_ADDR = 0x0018
    TIM3_CCER_ADDR = 0x0020
    TIM3_CNT_ADDR = 0x0024
    TIM3_PSC_ADDR = 0x0028
    TIM3_ARR_ADDR = 0x002C
    TIM3_CCR1_ADDR = 0x0034
    TIM3_CCR2_ADDR = 0x0038
    TIM3_CCR3_ADDR = 0x003C
    TIM3_CCR4_ADDR = 0x0040
    # General Purpose Timer 4
    TIM4_BASE = 0x40000800
    TIM4_CR1_ADDR = 0x0000
    TIM4_SR_ADDR = 0x0010
    TIM4_CCMR1_ADDR = 0x0018
    TIM4_CCER_ADDR = 0x0020
    TIM4_CNT_ADDR = 0x0024
    TIM4_PSC_ADDR = 0x0028
    TIM4_ARR_ADDR = 0x002C
    TIM4_CCR1_ADDR = 0x0034
    TIM4_CCR2_ADDR = 0x0038
    TIM4_CCR3_ADDR = 0x003C
    TIM4_CCR4_ADDR = 0x0040
    # General Purpose Timer 5
    TIM5_BASE = 0x40000C00
    TIM5_CR1_ADDR = 0x0000
    TIM5_SR_ADDR = 0x0010
    TIM5_CNT_ADDR = 0x0024
    TIM5_PSC_ADDR = 0x0028
    TIM5_ARR_ADDR = 0x002C
    TIM5_CCR1_ADDR = 0x0034
    TIM5_CCR2_ADDR = 0x0038
    TIM5_CCR3_ADDR = 0x003C
    TIM5_CCR4_ADDR = 0x0040
    # General Purpose Timer 9
    TIM9_BASE = 0x40014C00
    TIM9_CR1_ADDR = 0x0000
    TIM9_SMCR_ADDR = 0x0008
    TIM9_DIER_ADDR = 0x000C
    TIM9_SR_ADDR = 0x0010
    TIM9_EGR_ADDR = 0x0014
    TIM9_CCMR1_ADDR = 0x0018
    TIM9_CCER_ADDR = 0x0020
    TIM9_CNT_ADDR = 0x0024
    TIM9_PSC_ADDR = 0x0028
    TIM9_ARR_ADDR = 0x002C
    TIM9_CCR1_ADDR = 0x0034
    TIM9_CCR2_ADDR = 0x0038
    # General Purpose Timer 10
    TIM10_BASE = 0x40015000
    TIM10_CR1_ADDR = 0x0000
    TIM10_DIER_ADDR = 0x000C
    TIM10_SR_ADDR = 0x0010
    TIM10_EGR_ADDR = 0x0014
    TIM10_CCMR1_ADDR = 0x0018
    TIM10_CCER_ADDR = 0x0020
    TIM10_CNT_ADDR = 0x0024
    TIM10_PSC_ADDR = 0x0028
    TIM10_ARR_ADDR = 0x002C
    TIM10_CCR1_ADDR = 0x0034
    # ADC1
    ADC1_BASE = 0x40012000
    ADC1_SR_ADDR = 0x0000
    ADC1_CR1_ADDR = 0x0004
    ADC1_CR2_ADDR = 0x0008
    ADC1_SMPR1_ADDR = 0x000C
    ADC1_SMPR2_ADDR = 0x0010
    ADC1_JOFR1_ADDR = 0x0014
    ADC1_JOFR2_ADDR = 0x0018
    ADC1_JOFR3_ADDR = 0x001C
    ADC1_JOFR4_ADDR = 0x0020
    ADC1_HTR_ADDR = 0x0024
    ADC1_LTR_ADDR = 0x0028
    ADC1_SQR1_ADDR = 0x002C
    ADC1_SQR2_ADDR = 0x0030
    ADC1_SQR3_ADDR = 0x0034
    ADC1_JSQR_ADDR = 0x003C
    ADC1_JDR1_ADDR = 0x0040
    ADC1_JDR2_ADDR = 0x0044
    ADC1_JDR3_ADDR = 0x0048
    ADC1_JDR4_ADDR = 0x004C
    ADC1_DR_ADDR = 0x0050
    # ADC2
    ADC2_BASE = 0x40012100
    ADC2_SR_ADDR = 0x0000
    ADC2_CR1_ADDR = 0x0004
    ADC2_CR2_ADDR = 0x0008
    ADC2_SMPR1_ADDR = 0x000C
    ADC2_SMPR2_ADDR = 0x0010
    ADC2_SQR1_ADDR = 0x002C
    ADC2_DR_ADDR = 0x0050
    # ADC3
    ADC3_BASE = 0x40012200
    ADC3_SR_ADDR = 0x0000
    ADC3_CR1_ADDR = 0x0004
    ADC3_CR2_ADDR = 0x0008
    ADC3_SMPR1_ADDR = 0x000C
    ADC3_SMPR2_ADDR = 0x0010
    ADC3_SQR1_ADDR = 0x002C
    ADC3_DR_ADDR = 0x0050
    # System Configuration Controller
    SYSCFG_BASE = 0x40013800
    SYSCFG_CFGR_ADDR = 0x0000
    SYSCFG_EXTICR1_ADDR = 0x0008
    SYSCFG_EXTICR2_ADDR = 0x000C
    SYSCFG_EXTICR3_ADDR = 0x0010
    SYSCFG_EXTICR4_ADDR = 0x0014
    SYSCFG_CBR_ADDR = 0x001C
    # External Interrupt/Event Controller
    EXTI_BASE = 0x40013C00
    EXTI_IMR_ADDR = 0x0000
    EXTI_EMR_ADDR = 0x0004
    EXTI_RTSR_ADDR = 0x0008
    EXTI_FTSR_ADDR = 0x000C
    EXTI_SWIER_ADDR = 0x0010
    EXTI_PR_ADDR = 0x0014
    # Random Number Generator
    RNG_BASE = 0x50060800
    RNG_CR_ADDR = 0x0000
    RNG_SR_ADDR = 0x0004
    RNG_DR_ADDR = 0x0008
    # CRYP Accelerator
    CRYP_BASE = 0x50060000
    CRYP_CR_ADDR = 0x0000
    CRYP_SR_ADDR = 0x0004
    CRYP_DIN_ADDR = 0x0008
    CRYP_DOUT_ADDR = 0x000C
    CRYP_DMACR_ADDR = 0x0010
    CRYP_IMSCR_ADDR = 0x0014
    CRYP_RISR_ADDR = 0x0018
    CRYP_MISR_ADDR = 0x001C
    CRYP_K0LR_ADDR = 0x0020
    CRYP_K0RR_ADDR = 0x0024
    CRYP_K1LR_ADDR = 0x0028
    CRYP_K1RR_ADDR = 0x002C
    CRYP_K2LR_ADDR = 0x0030
    CRYP_K2RR_ADDR = 0x0034
    CRYP_K3LR_ADDR = 0x0038
    CRYP_K3RR_ADDR = 0x003C
    CRYP_IV0LR_ADDR = 0x0040
    CRYP_IV0RR_ADDR = 0x0044
    CRYP_IV1LR_ADDR = 0x0048
    CRYP_IV1RR_ADDR = 0x004C
    # HASH Accelerator
    HASH_BASE = 0x50060400
    HASH_CR_ADDR = 0x0000
    HASH_DIN_ADDR = 0x0004
    HASH_DINSTAT_ADDR = 0x0008
    HASH_HR_ADDR = 0x000C
    HASH_IMR_ADDR = 0x0020
    HASH_SR_ADDR = 0x0024
    # Digital Camera Interface
    DCMI_BASE = 0x50050000
    DCMI_CR_ADDR = 0x0000
    DCMI_SR_ADDR = 0x0004
    DCMI_RISR_ADDR = 0x0008
    DCMI_IER_ADDR = 0x000C
    DCMI_MISR_ADDR = 0x0010
    DCMI_ICR_ADDR = 0x0014
    DCMI_MFISH_ADDR = 0x001C
    DCMI_CWSTRT_ADDR = 0x0020
    DCMI_CWSIZE_ADDR = 0x0024
    DCMI_DR_ADDR = 0x0028
    DCMI_OR_ADDR = 0x002C
    # USB OTG High Speed
    USB_OTG_HS_BASE = 0x40040000
    USB_OTG_HS_GOTGCTL_ADDR = 0x0000
    USB_OTG_HS_GOTGINT_ADDR = 0x0004
    USB_OTG_HS_GINTMSK_ADDR = 0x0008
    USB_OTG_HS_GRSTCTL_ADDR = 0x000C
    USB_OTG_HS_GINTSTS_ADDR = 0x0010
    USB_OTG_HS_GRXSTSR_ADDR = 0x0014
    USB_OTG_HS_GRXFSIZ_ADDR = 0x0024
    USB_OTG_HS_HNPTXFSIZ_ADDR = 0x0028
    USB_OTG_HS_HNPTXSTS_ADDR = 0x002C
    USB_OTG_HS_GCCFG_ADDR = 0x0038
    USB_OTG_HS_CID_ADDR = 0x003C
    USB_OTG_HS_HPTXFSIZ_ADDR = 0x0100
    USB_OTG_HS_DIEPTXF_ADDR = 0x0200
    # Ethernet
    ETH_BASE = 0x40028000
    ETH_MACCR_ADDR = 0x0000
    ETH_MACFFR_ADDR = 0x0004
    ETH_MACHTHR_ADDR = 0x0008
    ETH_MACHTLR_ADDR = 0x000C
    ETH_MACMIIAR_ADDR = 0x0010
    ETH_MACMIIDR_ADDR = 0x0014
    ETH_MACCR_ADDR = 0x0018
    ETH_MACVLANTR_ADDR = 0x001C
    ETH_MACRWUFFR_ADDR = 0x0028
    ETH_MACPMTCSR_ADDR = 0x002C
    ETH_MACSR_ADDR = 0x0030
    ETH_MACIMR_ADDR = 0x0034
    ETH_MACA0HR_ADDR = 0x0040
    ETH_MACA0LR_ADDR = 0x0044
    ETH_MACA1HR_ADDR = 0x0048
    ETH_MACA1LR_ADDR = 0x004C
    ETH_MMCCR_ADDR = 0x0100
    ETH_MMCRIR_ADDR = 0x0104
    ETH_MMCTIR_ADDR = 0x0108
    ETH_MMCRIMR_ADDR = 0x010C
    ETH_MMCTIMR_ADDR = 0x0110
    ETH_MMCTGBSCCR_ADDR = 0x0114
    ETH_MMCRGUFCCR_ADDR = 0x0118
    ETH_PTPTSCR_ADDR = 0x0700
    ETH_PTPSSIR_ADDR = 0x0704
    ETH_PTPTSHR_ADDR = 0x0708
    ETH_PTPTSLR_ADDR = 0x070C
    ETH_PTPTSHUR_ADDR = 0x0710
    ETH_PTPTSLUR_ADDR = 0x0714
    ETH_PTPTSAR_ADDR = 0x0718
    ETH_PTPTTHR_ADDR = 0x071C
    ETH_PTPTTLR_ADDR = 0x0720
    ETH_PTPTSR_ADDR = 0x0728
    ETH_DMABMR_ADDR = 0x1000
    ETH_DMASR_ADDR = 0x1004
    ETH_DMAOMR_ADDR = 0x1008
    ETH_DMAIER_ADDR = 0x100C
    ETH_DMAMFBOCR_ADDR = 0x1010
    ETH_DMACHTDR_ADDR = 0x1014
    ETH_DMACHRDR_ADDR = 0x1018
    ETH_DMACHTBAR_ADDR = 0x101C
    ETH_DMACHRBAR_ADDR = 0x1020

    # 中断向量定义
    INT_WWDG = 0  # Window WatchDog interrupt
    INT_PVD = 1  # PVD through EXTI line detection interrupt
    INT_TAMPER = 2  # Tamper interrupt
    INT_RTC_WKUP = 3  # RTC Wakeup interrupt
    INT_FLASH = 4  # FLASH global interrupt
    INT_RCC = 5  # RCC global interrupt
    INT_EXTI0 = 6  # EXTI Line0 interrupt
    INT_EXTI1 = 7  # EXTI Line1 interrupt
    INT_EXTI2 = 8  # EXTI Line2 interrupt
    INT_EXTI3 = 9  # EXTI Line3 interrupt
    INT_EXTI4 = 10  # EXTI Line4 interrupt
    INT_DMA1_STREAM0 = 11  # DMA1 Stream0 global interrupt
    INT_DMA1_STREAM1 = 12  # DMA1 Stream1 global interrupt
    INT_DMA1_STREAM2 = 13  # DMA1 Stream2 global interrupt
    INT_DMA1_STREAM3 = 14  # DMA1 Stream3 global interrupt
    INT_DMA1_STREAM4 = 15  # DMA1 Stream4 global interrupt
    INT_DMA1_STREAM5 = 16  # DMA1 Stream5 global interrupt
    INT_DMA1_STREAM6 = 17  # DMA1 Stream6 global interrupt
    INT_ADC = 18  # ADC1 global interrupt
    INT_CAN1_TX = 19  # CAN1 TX interrupts
    INT_CAN1_RX0 = 20  # CAN1 RX0 interrupts
    INT_CAN1_RX1 = 21  # CAN1 RX1 interrupt
    INT_CAN1_SCE = 22  # CAN1 SCE interrupt
    INT_EXTI9_5 = 23  # EXTI Line[9:5] interrupt
    INT_TIM1_BRK_TIM9 = 24  # TIM1 Break interrupt and TIM9 global interrupt
    INT_TIM1_UP_TIM10 = 25  # TIM1 Update interrupt and TIM10 global interrupt
    INT_TIM1_TRG_COM_TIM11 = 26  # TIM1 Trigger and commutation interrupt and TIM11 global interrupt
    INT_TIM1_CC = 27  # TIM1 Capture Compare interrupt
    INT_TIM2 = 28  # TIM2 global interrupt
    INT_TIM3 = 29  # TIM3 global interrupt
    INT_TIM4 = 30  # TIM4 global interrupt
    INT_I2C1_EV = 31  # I2C1 Event interrupt
    INT_I2C1_ER = 32  # I2C1 Error interrupt
    INT_I2C2_EV = 33  # I2C2 Event interrupt
    INT_I2C2_ER = 34  # I2C2 Error interrupt
    INT_SPI1 = 35  # SPI1 global interrupt
    INT_SPI2 = 36  # SPI2 global interrupt
    INT_USART1 = 37  # USART1 global interrupt
    INT_USART2 = 38  # USART2 global interrupt
    INT_USART3 = 39  # USART3 global interrupt
    INT_EXTI15_10 = 40  # EXTI Line[15:10] interrupts
    INT_RTC_ALARM = 41  # RTC Alarm (A and B) through EXTI Line interrupt
    INT_OTG_FS_WKUP = 42  # USB OTG FS Wakeup through EXTI interrupt
    INT_TIM8_BRK_TIM12 = 43  # TIM8 Break interrupt and TIM12 global interrupt
    INT_TIM8_UP_TIM13 = 44  # TIM8 Update interrupt and TIM13 global interrupt
    INT_TIM8_TRG_COM_TIM14 = 45  # TIM8 Trigger and commutation interrupt and TIM14 global interrupt
    INT_TIM8_CC = 46  # TIM8 Capture Compare interrupt
    INT_SPI3 = 47  # SPI3 global interrupt
    INT_UART4 = 48  # UART4 global interrupt
    INT_UART5 = 49  # UART5 global interrupt
    INT_TIM6 = 50  # TIM6 global interrupt
    INT_TIM7 = 51  # TIM7 global interrupt
    INT_DMA2_STREAM0 = 52  # DMA2 Stream0 global interrupt
    INT_DMA2_STREAM1 = 53  # DMA2 Stream1 global interrupt
    INT_DMA2_STREAM2 = 54  # DMA2 Stream2 global interrupt
    INT_DMA2_STREAM3 = 55  # DMA2 Stream3 global interrupt
    INT_DMA2_STREAM4 = 56  # DMA2 Stream4 global interrupt
    INT_ETH = 57  # Ethernet global interrupt
    INT_ETH_WKUP = 58  # Ethernet Wakeup through EXTI interrupt
    INT_CAN2_TX = 59  # CAN2 TX interrupts
    INT_CAN2_RX0 = 60  # CAN2 RX0 interrupts
    INT_CAN2_RX1 = 61  # CAN2 RX1 interrupt
    INT_CAN2_SCE = 62  # CAN2 SCE interrupt
    INT_NA = 63  # Not Available
    INT_OTG_FS = 64  # USB OTG FS global interrupt
    INT_DCMI = 65  # DCMI global interrupt
    INT_CRYP = 66  # CRYP global interrupt
    INT_HASH_RNG = 67  # HASH and RNG global interrupt
    INT_FPU = 68  # FPU global interrupt

    # 引脚定义
    PIN_PE2 = 1  # Tristate - any function
    PIN_PE3 = 2  # Tristate - any function
    PIN_PE4 = 3  # Tristate - any function
    PIN_PE5 = 4  # Tristate - any function
    PIN_PE6 = 5  # Tristate - any function
    PIN_VCAP1 = 6  # 1.2V voltage supply
    PIN_VBAT = 7  # Battery voltage supply
    PIN_PC13 = 8  # TAMPER-RTC
    PIN_PC14 = 9  # OSC32_IN
    PIN_PC15 = 10  # OSC32_OUT
    PIN_PH0 = 11  # OSC_IN
    PIN_PH1 = 12  # OSC_OUT
    PIN_NRST = 13  # External reset
    PIN_VSSA = 14  # Analog ground
    PIN_VDDA = 15  # Analog supply
    PIN_PA0 = 16  # WKUP/USART_CTS
    PIN_PA1 = 17  # USART_RTS
    PIN_PA2 = 18  # USART_TX
    PIN_PA3 = 19  # USART_RX
    PIN_PA4 = 20  # DAC1_OUT
    PIN_PA5 = 21  # DAC2_OUT
    PIN_PA6 = 22  # SPI1_MISO
    PIN_PA7 = 23  # SPI1_MOSI
    PIN_PA8 = 24  # MCO1
    PIN_PA9 = 25  # USART1_TX
    PIN_PA10 = 26  # USART1_RX
    PIN_PA11 = 27  # USB_DM
    PIN_PA12 = 28  # USB_DP
    PIN_PA13 = 29  # JTMS/SWDIO
    PIN_VCAP2 = 30  # 1.2V voltage supply
    PIN_PA14 = 31  # JTCK/SWCLK
    PIN_PA15 = 32  # JTDI
    PIN_PB0 = 33  # SPI1_CS/TIM3_CH3
    PIN_PB1 = 34  # TIM3_CH4
    PIN_PB2 = 35  # BOOT1
    PIN_PB3 = 36  # JTDO/TRACESWO
    PIN_PB4 = 37  # NJTRST
    PIN_PB5 = 38  # CAN2_RX/TIM3_CH2
    PIN_PB6 = 39  # USART1_TX
    PIN_PB7 = 40  # USART1_RX
    PIN_PB8 = 41  # I2C1_SCL/TIM4_CH1
    PIN_PB9 = 42  # I2C1_SDA/TIM4_CH2
    PIN_PB10 = 43  # I2C2_SCL/USART3_TX
    PIN_PB11 = 44  # I2C2_SDA/USART3_RX
    PIN_PB12 = 45  # SPI2_CS/I2C2_SMBA
    PIN_PB13 = 46  # SPI2_SCK
    PIN_PB14 = 47  # SPI2_MISO
    PIN_PB15 = 48  # SPI2_MOSI
    PIN_PD0 = 49  # FSMC_D2
    PIN_PD1 = 50  # FSMC_D3
    PIN_PD2 = 51  # TIM3_ETR/UART4_RX
    PIN_PD3 = 52  # FSMC_CLK/UART4_CTS
    PIN_PD4 = 53  # FSMC_NOE
    PIN_PD5 = 54  # FSMC_NWE
    PIN_PD6 = 55  # FSMC_NWAIT
    PIN_PD7 = 56  # FSMC_NE1/FSMC_NCE2
    PIN_PD8 = 57  # FSMC_D13/USART3_TX
    PIN_PD9 = 58  # FSMC_D14/USART3_RX
    PIN_PD10 = 59  # FSMC_D15/USART3_CK
    PIN_PD11 = 60  # FSMC_A16/USART3_CTS
    PIN_PD12 = 61  # FSMC_A17/TIM4_CH1
    PIN_PD13 = 62  # FSMC_A18/TIM4_CH2
    PIN_PD14 = 63  # FSMC_D0/TIM4_CH3
    PIN_PD15 = 64  # FSMC_D1/TIM4_CH4
    PIN_PG0 = 65  # FSMC_A10/TIM4_ETR
    PIN_PG1 = 66  # FSMC_A11
    PIN_PE0 = 67  # FSMC_NBL0/TIM4_CH3
    PIN_PE1 = 68  # FSMC_NBL1/TIM4_CH4
    PIN_PE3 = 69  # FSMC_A19
    PIN_PE4 = 70  # FSMC_A20
    PIN_PE5 = 71  # FSMC_A21
    PIN_PE6 = 72  # FSMC_A22/TIM4_CH1
    PIN_PE7 = 73  # FSMC_D4/USART6_RX
    PIN_PE8 = 74  # FSMC_D5/USART6_TX
    PIN_PE9 = 75  # FSMC_D6/TIM4_CH1
    PIN_PE10 = 76  # FSMC_D7/USART6_RTS
    PIN_PE11 = 77  # FSMC_D8
    PIN_PE12 = 78  # FSMC_D9
    PIN_PE13 = 79  # FSMC_D10
    PIN_PE14 = 80  # FSMC_D11
    PIN_PE15 = 81  # FSMC_D12
    PIN_PB10 = 82  # I2C2_SCL/USART3_TX
    PIN_PB11 = 83  # I2C2_SDA/USART3_RX
    PIN_PB12 = 84  # SPI2_CS/I2C2_SMBA
    PIN_PB13 = 85  # SPI2_SCK
    PIN_PB14 = 86  # SPI2_MISO
    PIN_PB15 = 87  # SPI2_MOSI
    PIN_PD8 = 88  # FSMC_D13/USART3_TX
    PIN_PD9 = 89  # FSMC_D14/USART3_RX
    PIN_PD10 = 90  # FSMC_D15/USART3_CK
    PIN_PD11 = 91  # FSMC_A16/USART3_CTS
    PIN_PD12 = 92  # FSMC_A17/TIM4_CH1
    PIN_PD13 = 93  # FSMC_A18/TIM4_CH2
    PIN_PD14 = 94  # FSMC_D0/TIM4_CH3
    PIN_PD15 = 95  # FSMC_D1/TIM4_CH4
    PIN_PG2 = 96  # FSMC_A12
    PIN_PG3 = 97  # FSMC_A13
    PIN_PG4 = 98  # FSMC_A14
    PIN_PG5 = 99  # FSMC_A15
    PIN_PG6 = 100  # FSMC_INT2
    PIN_PG7 = 101  # FSMC_INT3
    PIN_PG8 = 102  # FSMC_INT4
    PIN_PG9 = 103  # FSMC_NE2/FSMC_NCE3
    PIN_PG10 = 104  # FSMC_NCE4_1
    PIN_PG11 = 105  # FSMC_NCE4_2
    PIN_PG12 = 106  # FSMC_NCE4_3
    PIN_PG13 = 107  # FSMC_A24
    PIN_PG14 = 108  # FSMC_A25
    PIN_PG15 = 109  # FSMC_INT2
    PIN_VSS = 110  # Ground
    PIN_VDD = 111  # 3.3V Supply

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
            "address": E0000EF34,
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
            "description": "FP Register 0",
            "value": 0
        }
        self._registers["S1"] = {
            "address": 0xE0000EF04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 1",
            "value": 0
        }
        self._registers["S2"] = {
            "address": 0xE0000EF08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 2",
            "value": 0
        }
        self._registers["S3"] = {
            "address": 0xE0000EF0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 3",
            "value": 0
        }
        self._registers["S4"] = {
            "address": 0xE0000EF10,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 4",
            "value": 0
        }
        self._registers["S5"] = {
            "address": 0xE0000EF14,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 5",
            "value": 0
        }
        self._registers["S6"] = {
            "address": 0xE0000EF18,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 6",
            "value": 0
        }
        self._registers["S7"] = {
            "address": 0xE0000EF1C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 7",
            "value": 0
        }
        self._registers["S8"] = {
            "address": 0xE0000EF20,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 8",
            "value": 0
        }
        self._registers["S9"] = {
            "address": 0xE0000EF24,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 9",
            "value": 0
        }
        self._registers["S10"] = {
            "address": 0xE0000EF28,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 10",
            "value": 0
        }
        self._registers["S11"] = {
            "address": 0xE0000EF2C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 11",
            "value": 0
        }
        self._registers["S12"] = {
            "address": 0xE0000EF30,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 12",
            "value": 0
        }
        self._registers["S13"] = {
            "address": 0xE0000EF34,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 13",
            "value": 0
        }
        self._registers["S14"] = {
            "address": 0xE0000EF38,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 14",
            "value": 0
        }
        self._registers["S15"] = {
            "address": 0xE0000EF3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 15",
            "value": 0
        }
        self._registers["S16"] = {
            "address": 0xE0000EF40,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 16",
            "value": 0
        }
        self._registers["S17"] = {
            "address": 0xE0000EF44,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 17",
            "value": 0
        }
        self._registers["S18"] = {
            "address": 0xE0000EF48,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 18",
            "value": 0
        }
        self._registers["S19"] = {
            "address": 0xE0000EF4C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 19",
            "value": 0
        }
        self._registers["S20"] = {
            "address": 0xE0000EF50,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 20",
            "value": 0
        }
        self._registers["S21"] = {
            "address": 0xE0000EF54,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 21",
            "value": 0
        }
        self._registers["S22"] = {
            "address": 0xE0000EF58,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 22",
            "value": 0
        }
        self._registers["S23"] = {
            "address": 0xE0000EF5C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 23",
            "value": 0
        }
        self._registers["S24"] = {
            "address": 0xE0000EF60,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 24",
            "value": 0
        }
        self._registers["S25"] = {
            "address": 0xE0000EF64,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 25",
            "value": 0
        }
        self._registers["S26"] = {
            "address": 0xE0000EF68,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 26",
            "value": 0
        }
        self._registers["S27"] = {
            "address": 0xE0000EF6C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 27",
            "value": 0
        }
        self._registers["S28"] = {
            "address": 0xE0000EF70,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 28",
            "value": 0
        }
        self._registers["S29"] = {
            "address": 0xE0000EF74,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 29",
            "value": 0
        }
        self._registers["S30"] = {
            "address": 0xE0000EF78,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 30",
            "value": 0
        }
        self._registers["S31"] = {
            "address": 0xE0000EF7C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "FP Register 31",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["RCC"] = {
            "base": 0x40023800,
            "type": "clock",
            "description": "Reset and Clock Control",
            "registers": {
                "CR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLLCFGR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CIR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB2RSTR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB1RSTR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AHB1ENR": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AHB2ENR": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AHB3ENR": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB2ENR": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB1ENR": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BDCR": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CSR": {
                    "address": 0x0054,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AHB1RSTR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AHB2RSTR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AHB3RSTR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGR2": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGR3": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL2CFGR": {
                    "address": 0x0060,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL2DIV": {
                    "address": 0x0064,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL3CFGR": {
                    "address": 0x0068,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLL3DIV": {
                    "address": 0x006C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["FLASH"] = {
            "base": 0x40023C00,
            "type": "flash",
            "description": "Flash Interface",
            "registers": {
                "ACR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "KEYR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OPTKEYR": {
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
                "CR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OPTCR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OPTCR1": {
                    "address": 0x0018,
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
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CSR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["DMA1"] = {
            "base": 0x40026000,
            "type": "dma",
            "description": "DMA1 Controller",
            "registers": {
                "LIFCR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HIFCR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LISR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HISR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S0CR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S0NDTR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S0PAR": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S0M0AR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S0M1AR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S0FCR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S1CR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S1NDTR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S2CR": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S3CR": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S4CR": {
                    "address": 0x0070,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S5CR": {
                    "address": 0x0088,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S6CR": {
                    "address": 0x00A0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S7CR": {
                    "address": 0x00B8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["DMA2"] = {
            "base": 0x40026400,
            "type": "dma",
            "description": "DMA2 Controller",
            "registers": {
                "LIFCR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HIFCR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LISR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HISR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S0CR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S1CR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S2CR": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S3CR": {
                    "address": 0x0058,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S4CR": {
                    "address": 0x0070,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S5CR": {
                    "address": 0x0088,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S6CR": {
                    "address": 0x00A0,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "S7CR": {
                    "address": 0x00B8,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART1"] = {
            "base": 0x40011000,
            "type": "uart",
            "description": "USART1",
            "registers": {
                "SR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR3": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GTPR": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART2"] = {
            "base": 0x40004400,
            "type": "uart",
            "description": "USART2",
            "registers": {
                "SR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR3": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART3"] = {
            "base": 0x40004800,
            "type": "uart",
            "description": "USART3",
            "registers": {
                "SR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR3": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI1"] = {
            "base": 0x40013000,
            "type": "spi",
            "description": "SPI1",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRCPR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXCRCR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXCRCR": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "I2SCFGR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI2"] = {
            "base": 0x40003800,
            "type": "spi",
            "description": "SPI2",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CRCPR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXCRCR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SPI3"] = {
            "base": 0x40003C00,
            "type": "spi",
            "description": "SPI3",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C1"] = {
            "base": 0x40005400,
            "type": "i2c",
            "description": "I2C1",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OAR1": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OAR2": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR1": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR2": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TRISE": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FLTR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C2"] = {
            "base": 0x40005800,
            "type": "i2c",
            "description": "I2C2",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OAR1": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OAR2": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR1": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR2": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TRISE": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C3"] = {
            "base": 0x40005C00,
            "type": "i2c",
            "description": "I2C3",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OAR1": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR1": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR2": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR": {
                    "address": 0x001C,
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
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMCR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIER": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EGR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR1": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR2": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCER": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RCR": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR3": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR4": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BDTR": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DCR": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMAR": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIM2"] = {
            "base": 0x40000000,
            "type": "timer",
            "description": "General Purpose Timer 2",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMCR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIER": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EGR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR1": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR2": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCER": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR3": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR4": {
                    "address": 0x0040,
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
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR1": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCER": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR3": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR4": {
                    "address": 0x0040,
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
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR1": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCER": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR3": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR4": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIM5"] = {
            "base": 0x40000C00,
            "type": "timer",
            "description": "General Purpose Timer 5",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR3": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR4": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIM9"] = {
            "base": 0x40014C00,
            "type": "timer",
            "description": "General Purpose Timer 9",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMCR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIER": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EGR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR1": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCER": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIM10"] = {
            "base": 0x40015000,
            "type": "timer",
            "description": "General Purpose Timer 10",
            "registers": {
                "CR1": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIER": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EGR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCMR1": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCER": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC1"] = {
            "base": 0x40012000,
            "type": "adc",
            "description": "ADC1",
            "registers": {
                "SR": {
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
                "CR2": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMPR1": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMPR2": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JOFR1": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JOFR2": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JOFR3": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JOFR4": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HTR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LTR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SQR1": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SQR2": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SQR3": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JSQR": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JDR1": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JDR2": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JDR3": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "JDR4": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC2"] = {
            "base": 0x40012100,
            "type": "adc",
            "description": "ADC2",
            "registers": {
                "SR": {
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
                "CR2": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMPR1": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMPR2": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SQR1": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC3"] = {
            "base": 0x40012200,
            "type": "adc",
            "description": "ADC3",
            "registers": {
                "SR": {
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
                "CR2": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMPR1": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMPR2": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SQR1": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0050,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["SYSCFG"] = {
            "base": 0x40013800,
            "type": "syscfg",
            "description": "System Configuration Controller",
            "registers": {
                "CFGR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTICR1": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTICR2": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTICR3": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EXTICR4": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CBR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["EXTI"] = {
            "base": 0x40013C00,
            "type": "exti",
            "description": "External Interrupt/Event Controller",
            "registers": {
                "IMR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EMR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RTSR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FTSR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SWIER": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["RNG"] = {
            "base": 0x50060800,
            "type": "rng",
            "description": "Random Number Generator",
            "registers": {
                "CR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
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
            }
        }
        self._peripherals["CRYP"] = {
            "base": 0x50060000,
            "type": "crypto",
            "description": "CRYP Accelerator",
            "registers": {
                "CR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIN": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DOUT": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMACR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IMSCR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RISR": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MISR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "K0LR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "K0RR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "K1LR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "K1RR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "K2LR": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "K2RR": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "K3LR": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "K3RR": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IV0LR": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IV0RR": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IV1LR": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IV1RR": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["HASH"] = {
            "base": 0x50060400,
            "type": "hash",
            "description": "HASH Accelerator",
            "registers": {
                "CR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIN": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DINSTAT": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IMR": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["DCMI"] = {
            "base": 0x50050000,
            "type": "camera",
            "description": "Digital Camera Interface",
            "registers": {
                "CR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RISR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IER": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MISR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ICR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "Mfish": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CWSTRT": {
                    "address": 0x0020,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CWSIZE": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USB_OTG_HS"] = {
            "base": 0x40040000,
            "type": "usb",
            "description": "USB OTG High Speed",
            "registers": {
                "GOTGCTL": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GOTGINT": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GINTMSK": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GRSTCTL": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GINTSTS": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GRXSTSR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GRXFSIZ": {
                    "address": 0x0024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HNPTXFSIZ": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HNPTXSTS": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GCCFG": {
                    "address": 0x0038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CID": {
                    "address": 0x003C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HPTXFSIZ": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIEPTXF": {
                    "address": 0x0200,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ETH"] = {
            "base": 0x40028000,
            "type": "ethernet",
            "description": "Ethernet",
            "registers": {
                "MACCR": {
                    "address": 0x0000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACFFR": {
                    "address": 0x0004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACHTHR": {
                    "address": 0x0008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACHTLR": {
                    "address": 0x000C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACMIIAR": {
                    "address": 0x0010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACMIIDR": {
                    "address": 0x0014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACCR": {
                    "address": 0x0018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACVLANTR": {
                    "address": 0x001C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACRWUFFR": {
                    "address": 0x0028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACPMTCSR": {
                    "address": 0x002C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACSR": {
                    "address": 0x0030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACIMR": {
                    "address": 0x0034,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACA0HR": {
                    "address": 0x0040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACA0LR": {
                    "address": 0x0044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACA1HR": {
                    "address": 0x0048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MACA1LR": {
                    "address": 0x004C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MMCCR": {
                    "address": 0x0100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MMCRIR": {
                    "address": 0x0104,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MMCTIR": {
                    "address": 0x0108,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MMCRIMR": {
                    "address": 0x010C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MMCTIMR": {
                    "address": 0x0110,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MMCTGBSCCR": {
                    "address": 0x0114,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MMCRGUFCCR": {
                    "address": 0x0118,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPTSCR": {
                    "address": 0x0700,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPSSIR": {
                    "address": 0x0704,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPTSHR": {
                    "address": 0x0708,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPTSLR": {
                    "address": 0x070C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPTSHUR": {
                    "address": 0x0710,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPTSLUR": {
                    "address": 0x0714,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPTSAR": {
                    "address": 0x0718,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPTTHR": {
                    "address": 0x071C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPTTLR": {
                    "address": 0x0720,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PTPTSR": {
                    "address": 0x0728,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMABMR": {
                    "address": 0x1000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMASR": {
                    "address": 0x1004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMAOMR": {
                    "address": 0x1008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMAIER": {
                    "address": 0x100C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMAMFBOCR": {
                    "address": 0x1010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMACHTDR": {
                    "address": 0x1014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMACHRDR": {
                    "address": 0x1018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMACHTBAR": {
                    "address": 0x101C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DMACHRBAR": {
                    "address": 0x1020,
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
        return f"STM32F407VGT6({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = STM32F407VGT6()
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

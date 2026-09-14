--[[
  LPC1768设备定义 - Lua模块
  生成自: NXP/LPC17xx/LPC1768
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: ARM Cortex-M3 up to 100MHz with 512KB Flash, 64KB SRAM
  CPU架构: ARM-Cortex-M3
  位宽: 32位
  时钟频率: 12000000 Hz
]]

local LPC1768 = {}

-- 设备信息
LPC1768.DEVICE_NAME = "LPC1768"
LPC1768.MANUFACTURER = "NXP"
LPC1768.FAMILY = "LPC17xx"
LPC1768.VERSION = "1.0"
LPC1768.ARCHITECTURE = "ARM-Cortex-M3"
LPC1768.BITS = 32
LPC1768.CLOCK_FREQUENCY = 12000000

-- 寄存器地址定义
LPC1768.R0_ADDR = 0x00000000  -- General Purpose Register 0
LPC1768.R1_ADDR = 0x00000004  -- General Purpose Register 1
LPC1768.R2_ADDR = 0x00000008  -- General Purpose Register 2
LPC1768.R3_ADDR = 0x0000000C  -- General Purpose Register 3
LPC1768.R4_ADDR = 0x00000010  -- General Purpose Register 4
LPC1768.R5_ADDR = 0x00000014  -- General Purpose Register 5
LPC1768.R6_ADDR = 0x00000018  -- General Purpose Register 6
LPC1768.R7_ADDR = 0x0000001C  -- General Purpose Register 7
LPC1768.R8_ADDR = 0x00000020  -- General Purpose Register 8
LPC1768.R9_ADDR = 0x00000024  -- General Purpose Register 9
LPC1768.R10_ADDR = 0x00000028  -- General Purpose Register 10
LPC1768.R11_ADDR = 0x0000002C  -- General Purpose Register 11
LPC1768.R12_ADDR = 0x00000030  -- General Purpose Register 12
LPC1768.SP_ADDR = 0x00000034  -- Stack Pointer
LPC1768.LR_ADDR = 0x00000038  -- Link Register
LPC1768.PC_ADDR = 0x0000003C  -- Program Counter
LPC1768.PSR_ADDR = 0x00000040  -- Program Status Register
LPC1768.PSR_N_BIT = 31  -- Negative Flag
LPC1768.PSR_Z_BIT = 30  -- Zero Flag
LPC1768.PSR_C_BIT = 29  -- Carry Flag
LPC1768.PSR_V_BIT = 28  -- Overflow Flag
LPC1768.PSR_Q_BIT = 27  -- Saturation Flag
LPC1768.PSR_ICI1_BIT = 0  -- Interrupt Continue State
LPC1768.PSR_GE_BIT = 0  -- Greater than or Equal
LPC1768.PSR_IT_BIT = 0  -- If-Then execution state
LPC1768.PSR_APSR_BIT = 0  -- Application Program Status
LPC1768.PRIMASK_ADDR = 0xE0000E20  -- Priority Mask Register
LPC1768.FAULTMASK_ADDR = 0xE0000E28  -- Fault Mask Register
LPC1768.BASEPRI_ADDR = 0xE0000E24  -- Base Priority Register
LPC1768.CONTROL_ADDR = 0xE0000E2C  -- Control Register

-- 内存段定义
LPC1768.FLASH_START = 0x00000000
LPC1768.FLASH_END = 0x0007FFFF
LPC1768.FLASH_SIZE = 524288  -- Main Flash (512KB)
LPC1768.FLASH_BOOT_START = 0x00080000
LPC1768.FLASH_BOOT_END = 0x0007FFFF
LPC1768.FLASH_BOOT_SIZE = 32768  -- Boot Flash (32KB)
LPC1768.SRAM_START = 0x10000000
LPC1768.SRAM_END = 0x1000FFFF
LPC1768.SRAM_SIZE = 65536  -- SRAM (64KB)
LPC1768.AHB1_START = 0x20000000
LPC1768.AHB1_END = 0x200FFFFF
LPC1768.AHB1_SIZE = 1048576  -- AHB1 Peripherals
LPC1768.APB0_START = 0x40000000
LPC1768.APB0_END = 0x400FFFFF
LPC1768.APB0_SIZE = 1048576  -- APB0 Peripherals
LPC1768.APB1_START = 0x50000000
LPC1768.APB1_END = 0x500FFFFF
LPC1768.APB1_SIZE = 1048576  -- APB1 Peripherals

-- 外设定义
-- GPIO
LPC1768.GPIO_BASE = 0x2009C000
LPC1768.GPIO_FIODIR_ADDR = 0x0000
LPC1768.GPIO_FIOMASK_ADDR = 0x0004
LPC1768.GPIO_FIOPIN_ADDR = 0x0008
LPC1768.GPIO_FIOSET_ADDR = 0x000C
LPC1768.GPIO_FIOCLR_ADDR = 0x0010
LPC1768.GPIO_P0_ADDR = 0x0014
LPC1768.GPIO_P1_ADDR = 0x0018
LPC1768.GPIO_P2_ADDR = 0x001C
LPC1768.GPIO_P3_ADDR = 0x0020
LPC1768.GPIO_P4_ADDR = 0x0024
-- UART0
LPC1768.UART0_BASE = 0x4000C000
LPC1768.UART0_RBR_ADDR = 0x0000
LPC1768.UART0_THR_ADDR = 0x0000
LPC1768.UART0_DLL_ADDR = 0x0000
LPC1768.UART0_DLM_ADDR = 0x0004
LPC1768.UART0_IER_ADDR = 0x0004
LPC1768.UART0_IIR_ADDR = 0x0008
LPC1768.UART0_FCR_ADDR = 0x0008
LPC1768.UART0_LCR_ADDR = 0x000C
LPC1768.UART0_LSR_ADDR = 0x0014
LPC1768.UART0_SCR_ADDR = 0x001C
LPC1768.UART0_ACR_ADDR = 0x0020
LPC1768.UART0_ICR_ADDR = 0x0024
LPC1768.UART0_FDR_ADDR = 0x0028
LPC1768.UART0_TER_ADDR = 0x0030
-- UART1
LPC1768.UART1_BASE = 0x4000D000
LPC1768.UART1_RBR_ADDR = 0x0000
LPC1768.UART1_THR_ADDR = 0x0000
LPC1768.UART1_DLL_ADDR = 0x0000
LPC1768.UART1_DLM_ADDR = 0x0004
LPC1768.UART1_IER_ADDR = 0x0004
LPC1768.UART1_IIR_ADDR = 0x0008
LPC1768.UART1_LCR_ADDR = 0x000C
LPC1768.UART1_LSR_ADDR = 0x0014
LPC1768.UART1_SCR_ADDR = 0x001C
LPC1768.UART1_MSR_ADDR = 0x0020
LPC1768.UART1_SCR_ADDR = 0x0024
-- UART2
LPC1768.UART2_BASE = 0x40098000
LPC1768.UART2_RBR_ADDR = 0x0000
LPC1768.UART2_THR_ADDR = 0x0000
LPC1768.UART2_DLL_ADDR = 0x0000
LPC1768.UART2_DLM_ADDR = 0x0004
LPC1768.UART2_IER_ADDR = 0x0004
LPC1768.UART2_IIR_ADDR = 0x0008
LPC1768.UART2_LCR_ADDR = 0x000C
LPC1768.UART2_LSR_ADDR = 0x0014
-- UART3
LPC1768.UART3_BASE = 0x4009C000
LPC1768.UART3_RBR_ADDR = 0x0000
LPC1768.UART3_THR_ADDR = 0x0000
LPC1768.UART3_DLL_ADDR = 0x0000
LPC1768.UART3_DLM_ADDR = 0x0004
LPC1768.UART3_IER_ADDR = 0x0004
LPC1768.UART3_IIR_ADDR = 0x0008
LPC1768.UART3_LCR_ADDR = 0x000C
LPC1768.UART3_LSR_ADDR = 0x0014
-- SPI0
LPC1768.SPI0_BASE = 0x40088000
LPC1768.SPI0_CR0_ADDR = 0x0000
LPC1768.SPI0_CR1_ADDR = 0x0004
LPC1768.SPI0_DR_ADDR = 0x0008
LPC1768.SPI0_SR_ADDR = 0x000C
LPC1768.SPI0_CPSR_ADDR = 0x0010
LPC1768.SPI0_IMSC_ADDR = 0x0014
LPC1768.SPI0_RIS_ADDR = 0x0018
LPC1768.SPI0_MIS_ADDR = 0x001C
LPC1768.SPI0_ICR_ADDR = 0x0020
-- SPI1
LPC1768.SPI1_BASE = 0x4008C000
LPC1768.SPI1_CR0_ADDR = 0x0000
LPC1768.SPI1_CR1_ADDR = 0x0004
LPC1768.SPI1_DR_ADDR = 0x0008
LPC1768.SPI1_SR_ADDR = 0x000C
LPC1768.SPI1_CPSR_ADDR = 0x0010
-- I2C0
LPC1768.I2C0_BASE = 0x4001C000
LPC1768.I2C0_CON_ADDR = 0x0000
LPC1768.I2C0_TAR_ADDR = 0x0004
LPC1768.I2C0_DAT_ADDR = 0x0008
LPC1768.I2C0_SSHC_ADDR = 0x000C
LPC1768.I2C0_HSH_ADDR = 0x0010
LPC1768.I2C0_INTX_ADDR = 0x0014
LPC1768.I2C0_INTM_ADDR = 0x0018
LPC1768.I2C0_AR_ADDR = 0x001C
LPC1768.I2C0_SR_ADDR = 0x0020
LPC1768.I2C0_TXFL_ADDR = 0x0024
LPC1768.I2C0_RXFL_ADDR = 0x0028
LPC1768.I2C0_COMP_ADDR = 0x002C
LPC1768.I2C0_RXFI_ADDR = 0x0030
LPC1768.I2C0_RXFT_ADDR = 0x0030
-- I2C1
LPC1768.I2C1_BASE = 0x4001C000
LPC1768.I2C1_CON_ADDR = 0x0000
LPC1768.I2C1_TAR_ADDR = 0x0004
LPC1768.I2C1_DAT_ADDR = 0x0008
LPC1768.I2C1_SR_ADDR = 0x0020
-- Timer0
LPC1768.TIMER0_BASE = 0x40004000
LPC1768.TIMER0_IR_ADDR = 0x0000
LPC1768.TIMER0_TCR_ADDR = 0x0004
LPC1768.TIMER0_TC_ADDR = 0x0008
LPC1768.TIMER0_PR_ADDR = 0x000C
LPC1768.TIMER0_PC_ADDR = 0x0010
LPC1768.TIMER0_MCR_ADDR = 0x0014
LPC1768.TIMER0_MR0_ADDR = 0x0018
LPC1768.TIMER0_MR1_ADDR = 0x001C
LPC1768.TIMER0_MR2_ADDR = 0x0020
LPC1768.TIMER0_MR3_ADDR = 0x0024
LPC1768.TIMER0_CCR_ADDR = 0x0028
LPC1768.TIMER0_CR0_ADDR = 0x002C
LPC1768.TIMER0_CR1_ADDR = 0x0030
LPC1768.TIMER0_CR2_ADDR = 0x0034
LPC1768.TIMER0_CR3_ADDR = 0x0038
LPC1768.TIMER0_EMR_ADDR = 0x003C
LPC1768.TIMER0_CTCR_ADDR = 0x0070
LPC1768.TIMER0_EW_ADDR = 0x0074
-- Timer1
LPC1768.TIMER1_BASE = 0x40008000
LPC1768.TIMER1_IR_ADDR = 0x0000
LPC1768.TIMER1_TCR_ADDR = 0x0004
LPC1768.TIMER1_TC_ADDR = 0x0008
LPC1768.TIMER1_PR_ADDR = 0x000C
LPC1768.TIMER1_MCR_ADDR = 0x0014
LPC1768.TIMER1_MR0_ADDR = 0x0018
LPC1768.TIMER1_MR1_ADDR = 0x001C
LPC1768.TIMER1_MR2_ADDR = 0x0020
LPC1768.TIMER1_MR3_ADDR = 0x0024
LPC1768.TIMER1_CCR_ADDR = 0x0028
LPC1768.TIMER1_CR0_ADDR = 0x002C
LPC1768.TIMER1_CR1_ADDR = 0x0030
LPC1768.TIMER1_EMR_ADDR = 0x003C
-- Timer2
LPC1768.TIMER2_BASE = 0x400A4000
LPC1768.TIMER2_IR_ADDR = 0x0000
LPC1768.TIMER2_TCR_ADDR = 0x0004
LPC1768.TIMER2_TC_ADDR = 0x0008
LPC1768.TIMER2_PR_ADDR = 0x000C
LPC1768.TIMER2_MCR_ADDR = 0x0014
LPC1768.TIMER2_MR0_ADDR = 0x0018
LPC1768.TIMER2_CCR_ADDR = 0x0028
LPC1768.TIMER2_CR0_ADDR = 0x002C
-- Timer3
LPC1768.TIMER3_BASE = 0x400A8000
LPC1768.TIMER3_IR_ADDR = 0x0000
LPC1768.TIMER3_TCR_ADDR = 0x0004
LPC1768.TIMER3_TC_ADDR = 0x0008
LPC1768.TIMER3_PR_ADDR = 0x000C
LPC1768.TIMER3_MCR_ADDR = 0x0014
LPC1768.TIMER3_MR0_ADDR = 0x0018
LPC1768.TIMER3_CCR_ADDR = 0x0028
-- PWM0
LPC1768.PWM0_BASE = 0x40014000
LPC1768.PWM0_IR_ADDR = 0x0000
LPC1768.PWM0_TCR_ADDR = 0x0004
LPC1768.PWM0_TC_ADDR = 0x0008
LPC1768.PWM0_PR_ADDR = 0x000C
LPC1768.PWM0_PC_ADDR = 0x0010
LPC1768.PWM0_MCR_ADDR = 0x0014
LPC1768.PWM0_MR0_ADDR = 0x0018
LPC1768.PWM0_MR1_ADDR = 0x001C
LPC1768.PWM0_MR2_ADDR = 0x0020
LPC1768.PWM0_MR3_ADDR = 0x0024
LPC1768.PWM0_MR4_ADDR = 0x0040
LPC1768.PWM0_MR5_ADDR = 0x0044
LPC1768.PWM0_MR6_ADDR = 0x0048
LPC1768.PWM0_CCR_ADDR = 0x0028
LPC1768.PWM0_CR0_ADDR = 0x002C
LPC1768.PWM0_PCR_ADDR = 0x004C
LPC1768.PWM0_LER_ADDR = 0x0050
LPC1768.PWM0_CTCR_ADDR = 0x0070
-- ADC
LPC1768.ADC_BASE = 0x400E4000
LPC1768.ADC_CR_ADDR = 0x0000
LPC1768.ADC_GDR_ADDR = 0x0004
LPC1768.ADC_INTEN_ADDR = 0x000C
LPC1768.ADC_STATUS_ADDR = 0x0010
LPC1768.ADC_TR_ADDR = 0x0014
-- DAC
LPC1768.DAC_BASE = 0x400E5000
LPC1768.DAC_CR_ADDR = 0x0000
LPC1768.DAC_CTRL_ADDR = 0x0004
-- Ethernet
LPC1768.ETH_BASE = 0x50000000
LPC1768.ETH_MAC1_ADDR = 0x0000
LPC1768.ETH_MAC2_ADDR = 0x0004
LPC1768.ETH_IPGT_ADDR = 0x0008
LPC1768.ETH_IPGR_ADDR = 0x000C
LPC1768.ETH_CLRT_ADDR = 0x0010
LPC1768.ETH_MAXF_ADDR = 0x0014
LPC1768.ETH_SUPP_ADDR = 0x0018
LPC1768.ETH_TEST_ADDR = 0x001C
LPC1768.ETH_MCFG_ADDR = 0x0020
LPC1768.ETH_MCMD_ADDR = 0x0024
LPC1768.ETH_MADR_ADDR = 0x0028
LPC1768.ETH_MWTD_ADDR = 0x002C
LPC1768.ETH_MRDD_ADDR = 0x0030
LPC1768.ETH_IND_ADDR = 0x0034
-- USB Controller
LPC1768.USB_BASE = 0x50000000
LPC1768.USB_HCCHAR_ADDR = 
LPC1768.USB_HCINT_ADDR = 
LPC1768.USB_HCINTMSK_ADDR = 
LPC1768.USB_HCTSIZ_ADDR = 
LPC1768.USB_HCDMA_ADDR = 
LPC1768.USB_HCDMAB_ADDR = 
LPC1768.USB_OTGINTST_ADDR = 
LPC1768.USB_OTGINTEN_ADDR = 
LPC1768.USB_OTGINTSEL_ADDR = 
-- DMA Controller
LPC1768.DMA_BASE = 0x50004000
LPC1768.DMA_INTSTAT_ADDR = 0x0000
LPC1768.DMA_INTTCSTAT_ADDR = 0x0004
LPC1768.DMA_INTTCCLEAR_ADDR = 0x0008
LPC1768.DMA_INTERRSTAT_ADDR = 0x000C
LPC1768.DMA_INTERRCLR_ADDR = 0x0010
LPC1768.DMA_RAWINTSTAT_ADDR = 0x0014
LPC1768.DMA_RAWINTTCSTAT_ADDR = 0x0018
LPC1768.DMA_ENBLDCHNS_ADDR = 0x001C
LPC1768.DMA_SOFTBREQ_ADDR = 0x0020
LPC1768.DMA_SOFTSREQ_ADDR = 0x0024
LPC1768.DMA_CONFIG_ADDR = 0x0028
LPC1768.DMA_SYNC_ADDR = 0x002C
-- Watchdog Timer
LPC1768.WDT_BASE = 0x40000000
LPC1768.WDT_WDMOD_ADDR = 0x0000
LPC1768.WDT_WDTC_ADDR = 0x0004
LPC1768.WDT_WDFEED_ADDR = 0x0008
LPC1768.WDT_WDTV_ADDR = 0x000C
-- RTC
LPC1768.RTC_BASE = 0x40024000
LPC1768.RTC_ILR_ADDR = 0x0000
LPC1768.RTC_CCR_ADDR = 0x0004
LPC1768.RTC_CIIR_ADDR = 0x0008
LPC1768.RTC_CWR_ADDR = 0x000C
LPC1768.RTC_PREINT_ADDR = 0x0010
LPC1768.RTC_PREFRAC_ADDR = 0x0014
LPC1768.RTC_CRT_ADDR = 0x0018
LPC1768.RTC_SEC_ADDR = 0x001C
LPC1768.RTC_MIN_ADDR = 0x0020
LPC1768.RTC_HOUR_ADDR = 0x0024
LPC1768.RTC_DOM_ADDR = 0x0028
LPC1768.RTC_DOW_ADDR = 0x002C
LPC1768.RTC_DOY_ADDR = 0x0030
LPC1768.RTC_MONTH_ADDR = 0x0034
LPC1768.RTC_YEAR_ADDR = 0x0038
-- System Control
LPC1768.SC_BASE = 0x400FC000
LPC1768.SC_PLL0CON_ADDR = 0x0000
LPC1768.SC_PLL0CFG_ADDR = 0x0004
LPC1768.SC_PLL0STAT_ADDR = 0x0008
LPC1768.SC_PLL0FEED_ADDR = 0x000C
LPC1768.SC_PLL1CON_ADDR = 0x0010
LPC1768.SC_PLL1CFG_ADDR = 0x0014
LPC1768.SC_PLL1STAT_ADDR = 0x0018
LPC1768.SC_PLL1FEED_ADDR = 0x001C
LPC1768.SC_CCLKCFG_ADDR = 0x0020
LPC1768.SC_USBCLKCFG_ADDR = 0x0024
LPC1768.SC_CLKSRC_ADDR = 0x0028
LPC1768.SC_PCLKSEL0_ADDR = 0x002C
LPC1768.SC_PCLKSEL1_ADDR = 0x0030
LPC1768.SC_BOSC_ADDR = 0x0050
LPC1768.SC_EXTINT_ADDR = 0x0054
LPC1768.SC_EXTMODE_ADDR = 0x0058
LPC1768.SC_EXTPOL_ADDR = 0x005C
-- Pin Connect Block
LPC1768.PINCONNECTBLOCK_BASE = 0x4002C000
LPC1768.PINCONNECTBLOCK_PINSEL0_ADDR = 0x0000
LPC1768.PINCONNECTBLOCK_PINSEL1_ADDR = 0x0004
LPC1768.PINCONNECTBLOCK_PINSEL2_ADDR = 0x0008
LPC1768.PINCONNECTBLOCK_PINSEL3_ADDR = 0x000C
LPC1768.PINCONNECTBLOCK_PINSEL4_ADDR = 0x0010
LPC1768.PINCONNECTBLOCK_PINSEL5_ADDR = 0x0014
LPC1768.PINCONNECTBLOCK_PINSEL6_ADDR = 0x0018
LPC1768.PINCONNECTBLOCK_PINSEL7_ADDR = 0x001C
LPC1768.PINCONNECTBLOCK_PINSEL8_ADDR = 0x0020
LPC1768.PINCONNECTBLOCK_PINSEL9_ADDR = 0x0024
LPC1768.PINCONNECTBLOCK_PINMODE0_ADDR = 0x0040
LPC1768.PINCONNECTBLOCK_PINMODE1_ADDR = 0x0044
LPC1768.PINCONNECTBLOCK_PINMODE2_ADDR = 0x0048
LPC1768.PINCONNECTBLOCK_PINMODE3_ADDR = 0x004C
LPC1768.PINCONNECTBLOCK_PINMODE4_ADDR = 0x0050
LPC1768.PINCONNECTBLOCK_PINMODE5_ADDR = 0x0054
LPC1768.PINCONNECTBLOCK_PINMODE6_ADDR = 0x0058
LPC1768.PINCONNECTBLOCK_PINMODE7_ADDR = 0x005C
LPC1768.PINCONNECTBLOCK_PINMODE8_ADDR = 0x0060
LPC1768.PINCONNECTBLOCK_PINMODE9_ADDR = 0x0064
LPC1768.PINCONNECTBLOCK_PINOD0_ADDR = 0x0080
LPC1768.PINCONNECTBLOCK_PINOD1_ADDR = 0x0084
LPC1768.PINCONNECTBLOCK_PINOD2_ADDR = 0x0088
LPC1768.PINCONNECTBLOCK_PINOD3_ADDR = 0x008C

-- 中断向量定义
LPC1768.INT_WDT = 0  -- Watchdog Timer
LPC1768.INT_RESERVED = 1  -- Reserved
LPC1768.INT_DEBUG_MON = 2  -- ARM Debug Mon
LPC1768.INT_RESERVED = 3  -- Reserved
LPC1768.INT_TIMER0 = 4  -- Timer 0
LPC1768.INT_TIMER1 = 5  -- Timer 1
LPC1768.INT_PWM0 = 6  -- PWM 0
LPC1768.INT_UART0 = 7  -- UART 0
LPC1768.INT_UART1 = 8  -- UART 1
LPC1768.INT_PWM1 = 9  -- PWM 1
LPC1768.INT_I2C0 = 10  -- I2C 0
LPC1768.INT_I2C1 = 11  -- I2C 1
LPC1768.INT_SPI0 = 12  -- SPI 0
LPC1768.INT_SPI1 = 13  -- SPI 1
LPC1768.INT_RTC = 14  -- RTC
LPC1768.INT_EINT0 = 15  -- External Interrupt 0
LPC1768.INT_EINT1 = 16  -- External Interrupt 1
LPC1768.INT_EINT2 = 17  -- External Interrupt 2
LPC1768.INT_EINT3 = 18  -- External Interrupt 3
LPC1768.INT_RESERVED = 19  -- Reserved
LPC1768.INT_ADC = 20  -- A/D Converter
LPC1768.INT_BOD = 21  -- Brown-Out Detect
LPC1768.INT_USB = 22  -- USB
LPC1768.INT_CAN = 23  -- CAN
LPC1768.INT_GP = 24  -- General Purpose DMA
LPC1768.INT_I2S = 25  -- I2S
LPC1768.INT_ETHERNET = 26  -- Ethernet
LPC1768.INT_RIT = 27  -- Repetitive Interrupt Timer
LPC1768.INT_QM = 28  -- Quadrature Encoder
LPC1768.INT_RESERVED = 29  -- Reserved
LPC1768.INT_RESERVED = 30  -- Reserved

-- 引脚定义
LPC1768.PIN_RESET = 1  -- External Reset
LPC1768.PIN_P0_0 = 2  -- GPIO Port 0.0
LPC1768.PIN_P0_1 = 3  -- GPIO Port 0.1
LPC1768.PIN_VSSA = 4  -- Analog Ground
LPC1768.PIN_VDDA = 5  -- Analog 3.3V
LPC1768.PIN_P0_2 = 6  -- GPIO Port 0.2
LPC1768.PIN_P0_3 = 7  -- GPIO Port 0.3
LPC1768.PIN_P0_4 = 8  -- GPIO Port 0.4
LPC1768.PIN_P0_5 = 9  -- GPIO Port 0.5
LPC1768.PIN_P0_6 = 10  -- GPIO Port 0.6
LPC1768.PIN_P0_7 = 11  -- GPIO Port 0.7
LPC1768.PIN_P0_8 = 12  -- GPIO Port 0.8
LPC1768.PIN_P0_9 = 13  -- GPIO Port 0.9
LPC1768.PIN_P0_10 = 14  -- GPIO Port 0.10
LPC1768.PIN_VSS = 15  -- Ground
LPC1768.PIN_VDD = 16  -- 3.3V
LPC1768.PIN_P0_11 = 17  -- GPIO Port 0.11
LPC1768.PIN_P0_12 = 18  -- GPIO Port 0.12
LPC1768.PIN_P0_13 = 19  -- GPIO Port 0.13
LPC1768.PIN_P0_14 = 20  -- GPIO Port 0.14
LPC1768.PIN_P0_15 = 21  -- GPIO Port 0.15
LPC1768.PIN_P0_16 = 22  -- GPIO Port 0.16
LPC1768.PIN_P0_17 = 23  -- GPIO Port 0.17
LPC1768.PIN_P0_18 = 24  -- GPIO Port 0.18
LPC1768.PIN_P0_19 = 25  -- GPIO Port 0.19
LPC1768.PIN_P0_20 = 26  -- GPIO Port 0.20
LPC1768.PIN_P0_21 = 27  -- GPIO Port 0.21
LPC1768.PIN_P0_22 = 28  -- GPIO Port 0.22
LPC1768.PIN_P0_23 = 29  -- GPIO Port 0.23
LPC1768.PIN_VSS = 30  -- Ground
LPC1768.PIN_VDD = 31  -- 3.3V
LPC1768.PIN_RTCX1 = 32  -- RTC Crystal Input
LPC1768.PIN_RTCX2 = 33  -- RTC Crystal Output
LPC1768.PIN_P1_0 = 34  -- GPIO Port 1.0
LPC1768.PIN_P1_1 = 35  -- GPIO Port 1.1
LPC1768.PIN_P1_2 = 36  -- GPIO Port 1.2
LPC1768.PIN_P1_3 = 37  -- GPIO Port 1.3
LPC1768.PIN_P1_4 = 38  -- GPIO Port 1.4
LPC1768.PIN_P1_5 = 39  -- GPIO Port 1.5
LPC1768.PIN_P1_6 = 40  -- GPIO Port 1.6
LPC1768.PIN_P1_7 = 41  -- GPIO Port 1.7
LPC1768.PIN_P1_8 = 42  -- GPIO Port 1.8
LPC1768.PIN_P1_9 = 43  -- GPIO Port 1.9
LPC1768.PIN_P1_10 = 44  -- GPIO Port 1.10
LPC1768.PIN_P1_11 = 45  -- GPIO Port 1.11
LPC1768.PIN_P1_12 = 46  -- GPIO Port 1.12
LPC1768.PIN_P1_13 = 47  -- GPIO Port 1.13
LPC1768.PIN_P1_14 = 48  -- GPIO Port 1.14
LPC1768.PIN_P1_15 = 49  -- GPIO Port 1.15
LPC1768.PIN_P1_16 = 50  -- GPIO Port 1.16
LPC1768.PIN_P1_17 = 51  -- GPIO Port 1.17
LPC1768.PIN_P1_18 = 52  -- GPIO Port 1.18
LPC1768.PIN_P1_19 = 53  -- GPIO Port 1.19
LPC1768.PIN_P1_20 = 54  -- GPIO Port 1.20
LPC1768.PIN_P1_21 = 55  -- GPIO Port 1.21
LPC1768.PIN_P1_22 = 56  -- GPIO Port 1.22
LPC1768.PIN_P1_23 = 57  -- GPIO Port 1.23
LPC1768.PIN_P1_24 = 58  -- GPIO Port 1.24
LPC1768.PIN_P1_25 = 59  -- GPIO Port 1.25
LPC1768.PIN_P1_26 = 60  -- GPIO Port 1.26
LPC1768.PIN_P1_27 = 61  -- GPIO Port 1.27
LPC1768.PIN_P1_28 = 62  -- GPIO Port 1.28
LPC1768.PIN_P1_29 = 63  -- GPIO Port 1.29
LPC1768.PIN_P1_30 = 64  -- GPIO Port 1.30
LPC1768.PIN_P1_31 = 65  -- GPIO Port 1.31
LPC1768.PIN_P2_0 = 66  -- GPIO Port 2.0
LPC1768.PIN_P2_1 = 67  -- GPIO Port 2.1
LPC1768.PIN_P2_2 = 68  -- GPIO Port 2.2
LPC1768.PIN_P2_3 = 69  -- GPIO Port 2.3
LPC1768.PIN_P2_4 = 70  -- GPIO Port 2.4
LPC1768.PIN_P2_5 = 71  -- GPIO Port 2.5
LPC1768.PIN_P2_6 = 72  -- GPIO Port 2.6
LPC1768.PIN_P2_7 = 73  -- GPIO Port 2.7
LPC1768.PIN_P2_8 = 74  -- GPIO Port 2.8
LPC1768.PIN_P2_9 = 75  -- GPIO Port 2.9
LPC1768.PIN_P2_10 = 76  -- GPIO Port 2.10
LPC1768.PIN_P2_11 = 77  -- GPIO Port 2.11
LPC1768.PIN_P2_12 = 78  -- GPIO Port 2.12
LPC1768.PIN_P2_13 = 79  -- GPIO Port 2.13
LPC1768.PIN_P2_14 = 80  -- GPIO Port 2.14
LPC1768.PIN_P2_15 = 81  -- GPIO Port 2.15
LPC1768.PIN_VSS = 82  -- Ground
LPC1768.PIN_VDD = 83  -- 3.3V

-- 设备类
function LPC1768.new(memory_base)
    memory_base = memory_base or 0
    
    local self = {
        memory_base = memory_base,
        registers = {},
        peripherals = {}
    }
    
    -- 初始化寄存器
    function self:_init_registers()
        self.registers["R0"] = {
            address = 0x00000000,
            size = 4,
            access = "rw",
            description = "General Purpose Register 0",
            value = 0
        }
        self.registers["R1"] = {
            address = 0x00000004,
            size = 4,
            access = "rw",
            description = "General Purpose Register 1",
            value = 0
        }
        self.registers["R2"] = {
            address = 0x00000008,
            size = 4,
            access = "rw",
            description = "General Purpose Register 2",
            value = 0
        }
        self.registers["R3"] = {
            address = 0x0000000C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 3",
            value = 0
        }
        self.registers["R4"] = {
            address = 0x00000010,
            size = 4,
            access = "rw",
            description = "General Purpose Register 4",
            value = 0
        }
        self.registers["R5"] = {
            address = 0x00000014,
            size = 4,
            access = "rw",
            description = "General Purpose Register 5",
            value = 0
        }
        self.registers["R6"] = {
            address = 0x00000018,
            size = 4,
            access = "rw",
            description = "General Purpose Register 6",
            value = 0
        }
        self.registers["R7"] = {
            address = 0x0000001C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 7",
            value = 0
        }
        self.registers["R8"] = {
            address = 0x00000020,
            size = 4,
            access = "rw",
            description = "General Purpose Register 8",
            value = 0
        }
        self.registers["R9"] = {
            address = 0x00000024,
            size = 4,
            access = "rw",
            description = "General Purpose Register 9",
            value = 0
        }
        self.registers["R10"] = {
            address = 0x00000028,
            size = 4,
            access = "rw",
            description = "General Purpose Register 10",
            value = 0
        }
        self.registers["R11"] = {
            address = 0x0000002C,
            size = 4,
            access = "rw",
            description = "General Purpose Register 11",
            value = 0
        }
        self.registers["R12"] = {
            address = 0x00000030,
            size = 4,
            access = "rw",
            description = "General Purpose Register 12",
            value = 0
        }
        self.registers["SP"] = {
            address = 0x00000034,
            size = 4,
            access = "rw",
            description = "Stack Pointer",
            value = 0
        }
        self.registers["LR"] = {
            address = 0x00000038,
            size = 4,
            access = "rw",
            description = "Link Register",
            value = 0
        }
        self.registers["PC"] = {
            address = 0x0000003C,
            size = 4,
            access = "rw",
            description = "Program Counter",
            value = 0
        }
        self.registers["PSR"] = {
            address = 0x00000040,
            size = 4,
            access = "rw",
            description = "Program Status Register",
            value = 0
        }
        self.registers["PRIMASK"] = {
            address = 0xE0000E20,
            size = 4,
            access = "rw",
            description = "Priority Mask Register",
            value = 0
        }
        self.registers["FAULTMASK"] = {
            address = 0xE0000E28,
            size = 4,
            access = "rw",
            description = "Fault Mask Register",
            value = 0
        }
        self.registers["BASEPRI"] = {
            address = 0xE0000E24,
            size = 4,
            access = "rw",
            description = "Base Priority Register",
            value = 0
        }
        self.registers["CONTROL"] = {
            address = 0xE0000E2C,
            size = 4,
            access = "rw",
            description = "Control Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["GPIO"] = {
            base = 0x2009C000,
            type = "gpio",
            description = "GPIO",
            registers = {}
        }
        
        local p = self.peripherals["GPIO"]
        p.registers["FIODIR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["FIOMASK"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["FIOPIN"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["FIOSET"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["FIOCLR"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["P0"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["P1"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["P2"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["P3"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["P4"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        self.peripherals["UART0"] = {
            base = 0x4000C000,
            type = "uart",
            description = "UART0",
            registers = {}
        }
        
        local p = self.peripherals["UART0"]
        p.registers["RBR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["THR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DLL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DLM"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IER"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IIR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["FCR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["LCR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["LSR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["SCR"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["ACR"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["ICR"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["FDR"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["TER"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        self.peripherals["UART1"] = {
            base = 0x4000D000,
            type = "uart",
            description = "UART1",
            registers = {}
        }
        
        local p = self.peripherals["UART1"]
        p.registers["RBR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["THR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DLL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DLM"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IER"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IIR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["LCR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["LSR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["SCR"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["MSR"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["SCR"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        self.peripherals["UART2"] = {
            base = 0x40098000,
            type = "uart",
            description = "UART2",
            registers = {}
        }
        
        local p = self.peripherals["UART2"]
        p.registers["RBR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["THR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DLL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DLM"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IER"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IIR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["LCR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["LSR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        self.peripherals["UART3"] = {
            base = 0x4009C000,
            type = "uart",
            description = "UART3",
            registers = {}
        }
        
        local p = self.peripherals["UART3"]
        p.registers["RBR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["THR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DLL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["DLM"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IER"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IIR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["LCR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["LSR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        self.peripherals["SPI0"] = {
            base = 0x40088000,
            type = "spi",
            description = "SPI0",
            registers = {}
        }
        
        local p = self.peripherals["SPI0"]
        p.registers["CR0"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["SR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["CPSR"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["IMSC"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["RIS"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["MIS"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["ICR"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        self.peripherals["SPI1"] = {
            base = 0x4008C000,
            type = "spi",
            description = "SPI1",
            registers = {}
        }
        
        local p = self.peripherals["SPI1"]
        p.registers["CR0"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["DR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["SR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["CPSR"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        self.peripherals["I2C0"] = {
            base = 0x4001C000,
            type = "i2c",
            description = "I2C0",
            registers = {}
        }
        
        local p = self.peripherals["I2C0"]
        p.registers["CON"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TAR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["DAT"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["SSHC"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["HSH"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["INTX"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["INTM"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["AR"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["SR"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["TXFL"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["RXFL"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["COMP"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["RXFI"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["RXFT"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        self.peripherals["I2C1"] = {
            base = 0x4001C000,
            type = "i2c",
            description = "I2C1",
            registers = {}
        }
        
        local p = self.peripherals["I2C1"]
        p.registers["CON"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TAR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["DAT"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["SR"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        self.peripherals["TIMER0"] = {
            base = 0x40004000,
            type = "timer",
            description = "Timer0",
            registers = {}
        }
        
        local p = self.peripherals["TIMER0"]
        p.registers["IR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TCR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["TC"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["PR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["PC"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["MCR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["MR0"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["MR1"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["MR2"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["MR3"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["CCR"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["CR0"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["CR2"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["CR3"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["EMR"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["CTCR"] = {
            address = 0x0070,
            size = 4,
            value = 0
        }
        p.registers["EW"] = {
            address = 0x0074,
            size = 4,
            value = 0
        }
        self.peripherals["TIMER1"] = {
            base = 0x40008000,
            type = "timer",
            description = "Timer1",
            registers = {}
        }
        
        local p = self.peripherals["TIMER1"]
        p.registers["IR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TCR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["TC"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["PR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["MCR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["MR0"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["MR1"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["MR2"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["MR3"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["CCR"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["CR0"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["CR1"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["EMR"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        self.peripherals["TIMER2"] = {
            base = 0x400A4000,
            type = "timer",
            description = "Timer2",
            registers = {}
        }
        
        local p = self.peripherals["TIMER2"]
        p.registers["IR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TCR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["TC"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["PR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["MCR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["MR0"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["CCR"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["CR0"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        self.peripherals["TIMER3"] = {
            base = 0x400A8000,
            type = "timer",
            description = "Timer3",
            registers = {}
        }
        
        local p = self.peripherals["TIMER3"]
        p.registers["IR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TCR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["TC"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["PR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["MCR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["MR0"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["CCR"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        self.peripherals["PWM0"] = {
            base = 0x40014000,
            type = "pwm",
            description = "PWM0",
            registers = {}
        }
        
        local p = self.peripherals["PWM0"]
        p.registers["IR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TCR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["TC"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["PR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["PC"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["MCR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["MR0"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["MR1"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["MR2"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["MR3"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["MR4"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["MR5"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["MR6"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        p.registers["CCR"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["CR0"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["PCR"] = {
            address = 0x004C,
            size = 4,
            value = 0
        }
        p.registers["LER"] = {
            address = 0x0050,
            size = 4,
            value = 0
        }
        p.registers["CTCR"] = {
            address = 0x0070,
            size = 4,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = 0x400E4000,
            type = "adc",
            description = "ADC",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["CR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["GDR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["INTEN"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["STATUS"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["TR"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        self.peripherals["DAC"] = {
            base = 0x400E5000,
            type = "dac",
            description = "DAC",
            registers = {}
        }
        
        local p = self.peripherals["DAC"]
        p.registers["CR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["CTRL"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        self.peripherals["ETH"] = {
            base = 0x50000000,
            type = "ethernet",
            description = "Ethernet",
            registers = {}
        }
        
        local p = self.peripherals["ETH"]
        p.registers["MAC1"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["MAC2"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IPGT"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["IPGR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["CLRT"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["MAXF"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["SUPP"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["TEST"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["MCFG"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["MCMD"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["MADR"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["MWTD"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["MRDD"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["IND"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        self.peripherals["USB"] = {
            base = 0x50000000,
            type = "usb",
            description = "USB Controller",
            registers = {}
        }
        
        local p = self.peripherals["USB"]
        p.registers["HCCHAR"] = {
            address = ,
            size = 4,
            value = 0
        }
        p.registers["HCINT"] = {
            address = ,
            size = 4,
            value = 0
        }
        p.registers["HCINTMSK"] = {
            address = ,
            size = 4,
            value = 0
        }
        p.registers["HCTSIZ"] = {
            address = ,
            size = 4,
            value = 0
        }
        p.registers["HCDMA"] = {
            address = ,
            size = 4,
            value = 0
        }
        p.registers["HCDMAB"] = {
            address = ,
            size = 4,
            value = 0
        }
        p.registers["OTGIntSt"] = {
            address = ,
            size = 4,
            value = 0
        }
        p.registers["OTGIntEn"] = {
            address = ,
            size = 4,
            value = 0
        }
        p.registers["OTGIntSel"] = {
            address = ,
            size = 4,
            value = 0
        }
        self.peripherals["DMA"] = {
            base = 0x50004000,
            type = "dma",
            description = "DMA Controller",
            registers = {}
        }
        
        local p = self.peripherals["DMA"]
        p.registers["IntStat"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["IntTCStat"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IntTCClear"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["IntErrStat"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["IntErrClr"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["RawIntStat"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["RawIntTCStat"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["EnbldChns"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["SoftBReq"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["SoftSReq"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["Config"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["Sync"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        self.peripherals["WDT"] = {
            base = 0x40000000,
            type = "wdt",
            description = "Watchdog Timer",
            registers = {}
        }
        
        local p = self.peripherals["WDT"]
        p.registers["WDMOD"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["WDTC"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["WDFEED"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["WDTV"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        self.peripherals["RTC"] = {
            base = 0x40024000,
            type = "rtc",
            description = "RTC",
            registers = {}
        }
        
        local p = self.peripherals["RTC"]
        p.registers["ILR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["CCR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["CIIR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["CWR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["PREINT"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["PREFRAC"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["CRT"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["SEC"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["MIN"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["HOUR"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["DOM"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["DOW"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["DOY"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["MONTH"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["YEAR"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        self.peripherals["SC"] = {
            base = 0x400FC000,
            type = "syscon",
            description = "System Control",
            registers = {}
        }
        
        local p = self.peripherals["SC"]
        p.registers["PLL0CON"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["PLL0CFG"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["PLL0STAT"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["PLL0FEED"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["PLL1CON"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["PLL1CFG"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["PLL1STAT"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["PLL1FEED"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["CCLKCFG"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["USBCLKCFG"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["CLKSRC"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["PCLKSEL0"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["PCLKSEL1"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["BOSC"] = {
            address = 0x0050,
            size = 4,
            value = 0
        }
        p.registers["EXTINT"] = {
            address = 0x0054,
            size = 4,
            value = 0
        }
        p.registers["EXTMODE"] = {
            address = 0x0058,
            size = 4,
            value = 0
        }
        p.registers["EXTPOL"] = {
            address = 0x005C,
            size = 4,
            value = 0
        }
        self.peripherals["PinConnectBlock"] = {
            base = 0x4002C000,
            type = "pin",
            description = "Pin Connect Block",
            registers = {}
        }
        
        local p = self.peripherals["PinConnectBlock"]
        p.registers["PINSEL0"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["PINSEL1"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["PINSEL2"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["PINSEL3"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["PINSEL4"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["PINSEL5"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["PINSEL6"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["PINSEL7"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["PINSEL8"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["PINSEL9"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["PINMODE0"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["PINMODE1"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["PINMODE2"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        p.registers["PINMODE3"] = {
            address = 0x004C,
            size = 4,
            value = 0
        }
        p.registers["PINMODE4"] = {
            address = 0x0050,
            size = 4,
            value = 0
        }
        p.registers["PINMODE5"] = {
            address = 0x0054,
            size = 4,
            value = 0
        }
        p.registers["PINMODE6"] = {
            address = 0x0058,
            size = 4,
            value = 0
        }
        p.registers["PINMODE7"] = {
            address = 0x005C,
            size = 4,
            value = 0
        }
        p.registers["PINMODE8"] = {
            address = 0x0060,
            size = 4,
            value = 0
        }
        p.registers["PINMODE9"] = {
            address = 0x0064,
            size = 4,
            value = 0
        }
        p.registers["PINOD0"] = {
            address = 0x0080,
            size = 4,
            value = 0
        }
        p.registers["PINOD1"] = {
            address = 0x0084,
            size = 4,
            value = 0
        }
        p.registers["PINOD2"] = {
            address = 0x0088,
            size = 4,
            value = 0
        }
        p.registers["PINOD3"] = {
            address = 0x008C,
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
            name = LPC1768.DEVICE_NAME,
            manufacturer = LPC1768.MANUFACTURER,
            family = LPC1768.FAMILY,
            version = LPC1768.VERSION,
            architecture = LPC1768.ARCHITECTURE,
            bits = LPC1768.BITS,
            clock_frequency = LPC1768.CLOCK_FREQUENCY
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
        return string.format("LPC1768(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function LPC1768.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function LPC1768.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function LPC1768.print_device_info(device)
    device = device or LPC1768.new()
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

function LPC1768.print_registers(device)
    device = device or LPC1768.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            LPC1768.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function LPC1768.example()
    print("=== LPC1768设备示例 ===")
    
    -- 创建设备实例
    local device = LPC1768.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    LPC1768.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. LPC1768.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. LPC1768.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    LPC1768.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("LPC1768.lua$") then
    LPC1768.example()
end

return LPC1768

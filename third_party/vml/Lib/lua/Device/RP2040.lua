--[[
  RP2040设备定义 - Lua模块
  生成自: Raspberry Pi/RP/RP2040
  版本: 1.0
  日期: 2026-04-16
  作者: VML Team
  描述: Dual-core ARM Cortex-M0+ up to 133MHz with 264KB SRAM
  CPU架构: ARM-Cortex-M0+
  位宽: 32位
  时钟频率: 12000000 Hz
]]

local RP2040 = {}

-- 设备信息
RP2040.DEVICE_NAME = "RP2040"
RP2040.MANUFACTURER = "Raspberry Pi"
RP2040.FAMILY = "RP"
RP2040.VERSION = "1.0"
RP2040.ARCHITECTURE = "ARM-Cortex-M0+"
RP2040.BITS = 32
RP2040.CLOCK_FREQUENCY = 12000000

-- 寄存器地址定义
RP2040.R0_ADDR = 0x00000000  -- General Purpose Register 0
RP2040.R1_ADDR = 0x00000004  -- General Purpose Register 1
RP2040.R2_ADDR = 0x00000008  -- General Purpose Register 2
RP2040.R3_ADDR = 0x0000000C  -- General Purpose Register 3
RP2040.R4_ADDR = 0x00000010  -- General Purpose Register 4
RP2040.R5_ADDR = 0x00000014  -- General Purpose Register 5
RP2040.R6_ADDR = 0x00000018  -- General Purpose Register 6
RP2040.R7_ADDR = 0x0000001C  -- General Purpose Register 7
RP2040.R8_ADDR = 0x00000020  -- General Purpose Register 8
RP2040.R9_ADDR = 0x00000024  -- General Purpose Register 9
RP2040.R10_ADDR = 0x00000028  -- General Purpose Register 10
RP2040.R11_ADDR = 0x0000002C  -- General Purpose Register 11
RP2040.R12_ADDR = 0x00000030  -- General Purpose Register 12
RP2040.SP_ADDR = 0x00000034  -- Stack Pointer
RP2040.LR_ADDR = 0x00000038  -- Link Register
RP2040.PC_ADDR = 0x0000003C  -- Program Counter
RP2040.XPSR_ADDR = 0x00000040  -- Program Status Register
RP2040.XPSR_N_BIT = 31  -- Negative Flag
RP2040.XPSR_Z_BIT = 30  -- Zero Flag
RP2040.XPSR_C_BIT = 29  -- Carry Flag
RP2040.XPSR_V_BIT = 28  -- Overflow Flag
RP2040.XPSR_Q_BIT = 27  -- Saturation Flag
RP2040.XPSR_ICI_BIT = 0  -- ICI execution state
RP2040.XPSR_IT_BIT = 0  -- If-Then execution state
RP2040.XPSR_T_BIT = 24  -- Thumb bit
RP2040.XPSR_IPSR_BIT = 0  -- Exception number
RP2040.PRIMASK_ADDR = 0xE0000E20  -- Priority Mask Register
RP2040.CONTROL_ADDR = 0xE0000E24  -- Control Register
RP2040.FAULTMASK_ADDR = 0xE0000E28  -- Fault Mask Register

-- 内存段定义
RP2040.ROM_START = 0x00000000
RP2040.ROM_END = 0x00001000
RP2040.ROM_SIZE = 4096  -- ROM (bootloader)
RP2040.SRAM0_START = 0x20000000
RP2040.SRAM0_END = 0x20003FFF
RP2040.SRAM0_SIZE = 16384  -- SRAM0 (16KB)
RP2040.SRAM1_START = 0x20004000
RP2040.SRAM1_END = 0x20007FFF
RP2040.SRAM1_SIZE = 16384  -- SRAM1 (16KB)
RP2040.SRAM2_START = 0x20008000
RP2040.SRAM2_END = 0x2000BFFF
RP2040.SRAM2_SIZE = 16384  -- SRAM2 (16KB)
RP2040.SRAM3_START = 0x2000C000
RP2040.SRAM3_END = 0x2000FFFF
RP2040.SRAM3_SIZE = 16384  -- SRAM3 (16KB)
RP2040.SRAM4_START = 0x20010000
RP2040.SRAM4_END = 0x20013FFF
RP2040.SRAM4_SIZE = 16384  -- SRAM4 (16KB)
RP2040.APB_START = 0x40000000
RP2040.APB_END = 0x400FFFFF
RP2040.APB_SIZE = 1048576  -- APB Peripherals
RP2040.AHB_START = 0x50000000
RP2040.AHB_END = 0x500FFFFF
RP2040.AHB_SIZE = 1048576  -- AHB Peripherals

-- 外设定义
-- IO Bank 0
RP2040.IO_BANK0_BASE = 0x40014000
RP2040.IO_BANK0_GPIO0_STATUS_ADDR = 0x0000
RP2040.IO_BANK0_GPIO0_CTRL_ADDR = 0x0004
RP2040.IO_BANK0_GPIO1_STATUS_ADDR = 0x0008
RP2040.IO_BANK0_GPIO1_CTRL_ADDR = 0x000C
RP2040.IO_BANK0_GPIO2_STATUS_ADDR = 0x0010
RP2040.IO_BANK0_GPIO2_CTRL_ADDR = 0x0014
RP2040.IO_BANK0_GPIO3_STATUS_ADDR = 0x0018
RP2040.IO_BANK0_GPIO3_CTRL_ADDR = 0x001C
RP2040.IO_BANK0_GPIO4_STATUS_ADDR = 0x0020
RP2040.IO_BANK0_GPIO4_CTRL_ADDR = 0x0024
RP2040.IO_BANK0_GPIO5_STATUS_ADDR = 0x0028
RP2040.IO_BANK0_GPIO5_CTRL_ADDR = 0x002C
RP2040.IO_BANK0_GPIO6_STATUS_ADDR = 0x0030
RP2040.IO_BANK0_GPIO6_CTRL_ADDR = 0x0034
RP2040.IO_BANK0_GPIO7_STATUS_ADDR = 0x0038
RP2040.IO_BANK0_GPIO7_CTRL_ADDR = 0x003C
RP2040.IO_BANK0_GPIO8_STATUS_ADDR = 0x0040
RP2040.IO_BANK0_GPIO8_CTRL_ADDR = 0x0044
RP2040.IO_BANK0_GPIO9_STATUS_ADDR = 0x0048
RP2040.IO_BANK0_GPIO9_CTRL_ADDR = 0x004C
RP2040.IO_BANK0_GPIO10_STATUS_ADDR = 0x0050
RP2040.IO_BANK0_GPIO10_CTRL_ADDR = 0x0054
RP2040.IO_BANK0_GPIO11_STATUS_ADDR = 0x0058
RP2040.IO_BANK0_GPIO11_CTRL_ADDR = 0x005C
RP2040.IO_BANK0_GPIO12_STATUS_ADDR = 0x0060
RP2040.IO_BANK0_GPIO12_CTRL_ADDR = 0x0064
RP2040.IO_BANK0_GPIO13_STATUS_ADDR = 0x0068
RP2040.IO_BANK0_GPIO13_CTRL_ADDR = 0x006C
RP2040.IO_BANK0_GPIO14_STATUS_ADDR = 0x0070
RP2040.IO_BANK0_GPIO14_CTRL_ADDR = 0x0074
RP2040.IO_BANK0_GPIO15_STATUS_ADDR = 0x0078
RP2040.IO_BANK0_GPIO15_CTRL_ADDR = 0x007C
RP2040.IO_BANK0_GPIO16_STATUS_ADDR = 0x0080
RP2040.IO_BANK0_GPIO16_CTRL_ADDR = 0x0084
RP2040.IO_BANK0_GPIO17_STATUS_ADDR = 0x0088
RP2040.IO_BANK0_GPIO17_CTRL_ADDR = 0x008C
RP2040.IO_BANK0_GPIO18_STATUS_ADDR = 0x0090
RP2040.IO_BANK0_GPIO18_CTRL_ADDR = 0x0094
RP2040.IO_BANK0_GPIO19_STATUS_ADDR = 0x0098
RP2040.IO_BANK0_GPIO19_CTRL_ADDR = 0x009C
RP2040.IO_BANK0_GPIO20_STATUS_ADDR = 0x00A0
RP2040.IO_BANK0_GPIO20_CTRL_ADDR = 0x00A4
RP2040.IO_BANK0_GPIO21_STATUS_ADDR = 0x00A8
RP2040.IO_BANK0_GPIO21_CTRL_ADDR = 0x00AC
RP2040.IO_BANK0_GPIO22_STATUS_ADDR = 0x00B0
RP2040.IO_BANK0_GPIO22_CTRL_ADDR = 0x00B4
RP2040.IO_BANK0_GPIO23_STATUS_ADDR = 0x00B8
RP2040.IO_BANK0_GPIO23_CTRL_ADDR = 0x00BC
RP2040.IO_BANK0_GPIO24_STATUS_ADDR = 0x00C0
RP2040.IO_BANK0_GPIO24_CTRL_ADDR = 0x00C4
RP2040.IO_BANK0_GPIO25_STATUS_ADDR = 0x00C8
RP2040.IO_BANK0_GPIO25_CTRL_ADDR = 0x00CC
RP2040.IO_BANK0_GPIO26_STATUS_ADDR = 0x00D0
RP2040.IO_BANK0_GPIO26_CTRL_ADDR = 0x00D4
RP2040.IO_BANK0_GPIO27_STATUS_ADDR = 0x00D8
RP2040.IO_BANK0_GPIO27_CTRL_ADDR = 0x00DC
RP2040.IO_BANK0_GPIO28_STATUS_ADDR = 0x00E0
RP2040.IO_BANK0_GPIO28_CTRL_ADDR = 0x00E4
RP2040.IO_BANK0_GPIO29_STATUS_ADDR = 0x00E8
RP2040.IO_BANK0_GPIO29_CTRL_ADDR = 0x00EC
RP2040.IO_BANK0_INTR_ADDR = 0x00F0
RP2040.IO_BANK0_PROC0_INTE_ADDR = 0x00F4
RP2040.IO_BANK0_PROC1_INTE_ADDR = 0x00F8
RP2040.IO_BANK0_PROC0_INTF_ADDR = 0x00FC
RP2040.IO_BANK0_PROC1_INTF_ADDR = 0x0100
RP2040.IO_BANK0_PROC0_INTS_ADDR = 0x0104
RP2040.IO_BANK0_PROC1_INTS_ADDR = 0x0108
RP2040.IO_BANK0_DORMANT_WAKE_INTE_ADDR = 0x010C
RP2040.IO_BANK0_DORMANT_WAKE_INTF_ADDR = 0x0110
RP2040.IO_BANK0_DORMANT_WAKE_INTS_ADDR = 0x0114
-- Pads
RP2040.PADS_BASE = 0x4001E000
RP2040.PADS_GPIO_VOLT_ADDR = 0x00E0
-- SIO (Single-cycle I/O)
RP2040.SIO_BASE = 0xD0000000
RP2040.SIO_CPUID_ADDR = 0x0000
RP2040.SIO_GPIO_OUT_ADDR = 0x0004
RP2040.SIO_GPIO_OUT_SET_ADDR = 0x0008
RP2040.SIO_GPIO_OUT_CLR_ADDR = 0x000C
RP2040.SIO_GPIO_OUT_XOR_ADDR = 0x0010
RP2040.SIO_GPIO_OE_ADDR = 0x0014
RP2040.SIO_GPIO_OE_SET_ADDR = 0x0018
RP2040.SIO_GPIO_OE_CLR_ADDR = 0x001C
RP2040.SIO_GPIO_OE_XOR_ADDR = 0x0020
RP2040.SIO_GPIO_IN_ADDR = 0x0024
RP2040.SIO_FIFO_ST_ADDR = 0x0040
RP2040.SIO_FIFO_WR_ADDR = 0x0044
RP2040.SIO_FIFO_RD_ADDR = 0x0048
RP2040.SIO_SPINLOCK_ST_ADDR = 0x004C
RP2040.SIO_INTERRUPT_ST_ADDR = 0x0050
-- UART0
RP2040.UART0_BASE = 0x40034000
RP2040.UART0_UARTDR_ADDR = 0x0000
RP2040.UART0_UARTRSR_ADDR = 0x0004
RP2040.UART0_UARTECR_ADDR = 0x0004
RP2040.UART0_UARTFR_ADDR = 0x0018
RP2040.UART0_UARTILPR_ADDR = 0x0020
RP2040.UART0_UARTIBRD_ADDR = 0x0024
RP2040.UART0_UARTFBRD_ADDR = 0x0028
RP2040.UART0_UARTLCR_H_ADDR = 0x002C
RP2040.UART0_UARTCR_ADDR = 0x0030
RP2040.UART0_UARTIFLS_ADDR = 0x0034
RP2040.UART0_UARTIMSC_ADDR = 0x0038
RP2040.UART0_UARTRIS_ADDR = 0x003C
RP2040.UART0_UARTMIS_ADDR = 0x0040
RP2040.UART0_UARTICR_ADDR = 0x0044
RP2040.UART0_UARTDMACR_ADDR = 0x0048
-- UART1
RP2040.UART1_BASE = 0x40038000
RP2040.UART1_UARTDR_ADDR = 0x0000
RP2040.UART1_UARTRSR_ADDR = 0x0004
RP2040.UART1_UARTFR_ADDR = 0x0018
RP2040.UART1_UARTIBRD_ADDR = 0x0024
RP2040.UART1_UARTFBRD_ADDR = 0x0028
RP2040.UART1_UARTLCR_H_ADDR = 0x002C
RP2040.UART1_UARTCR_ADDR = 0x0030
RP2040.UART1_UARTIFLS_ADDR = 0x0034
RP2040.UART1_UARTIMSC_ADDR = 0x0038
RP2040.UART1_UARTICR_ADDR = 0x0044
-- SPI0
RP2040.SPI0_BASE = 0x4003C000
RP2040.SPI0_SSPCR0_ADDR = 0x0000
RP2040.SPI0_SSPCR1_ADDR = 0x0004
RP2040.SPI0_SSPDR_ADDR = 0x0008
RP2040.SPI0_SSPSR_ADDR = 0x000C
RP2040.SPI0_SSPCPSR_ADDR = 0x0010
RP2040.SPI0_SSPIMSC_ADDR = 0x0014
RP2040.SPI0_SSPRIS_ADDR = 0x0018
RP2040.SPI0_SSPMIS_ADDR = 0x001C
RP2040.SPI0_SSPICR_ADDR = 0x0020
RP2040.SPI0_SSPDMACR_ADDR = 0x0024
-- SPI1
RP2040.SPI1_BASE = 0x4003C000
RP2040.SPI1_SSPCR0_ADDR = 0x0000
RP2040.SPI1_SSPCR1_ADDR = 0x0004
RP2040.SPI1_SSPDR_ADDR = 0x0008
RP2040.SPI1_SSPSR_ADDR = 0x000C
RP2040.SPI1_SSPCPSR_ADDR = 0x0010
RP2040.SPI1_SSPIMSC_ADDR = 0x0014
-- I2C0
RP2040.I2C0_BASE = 0x40044000
RP2040.I2C0_IC_CON_ADDR = 0x0000
RP2040.I2C0_IC_TAR_ADDR = 0x0004
RP2040.I2C0_IC_SAR_ADDR = 0x0008
RP2040.I2C0_IC_DATA_CMD_ADDR = 0x0010
RP2040.I2C0_IC_SS_SCL_HCNT_ADDR = 0x0014
RP2040.I2C0_IC_SS_SCL_LCNT_ADDR = 0x0018
RP2040.I2C0_IC_FS_SCL_HCNT_ADDR = 0x001C
RP2040.I2C0_IC_FS_SCL_LCNT_ADDR = 0x0020
RP2040.I2C0_IC_RAW_INTR_STAT_ADDR = 0x0024
RP2040.I2C0_IC_ENABLE_ADDR = 0x002C
RP2040.I2C0_IC_STATUS_ADDR = 0x0030
RP2040.I2C0_IC_TXFLR_ADDR = 0x0034
RP2040.I2C0_IC_RXFLR_ADDR = 0x0038
RP2040.I2C0_IC_TX_ABRT_ADDR = 0x003C
RP2040.I2C0_IC_DMA_CR_ADDR = 0x0040
RP2040.I2C0_IC_DMA_TDLR_ADDR = 0x0044
RP2040.I2C0_IC_DMA_RDLR_ADDR = 0x0048
-- I2C1
RP2040.I2C1_BASE = 0x40048000
RP2040.I2C1_IC_CON_ADDR = 0x0000
RP2040.I2C1_IC_TAR_ADDR = 0x0004
RP2040.I2C1_IC_ENABLE_ADDR = 0x002C
RP2040.I2C1_IC_STATUS_ADDR = 0x0030
RP2040.I2C1_IC_TX_ABRT_ADDR = 0x003C
-- PWM0
RP2040.PWM0_BASE = 0x40050000
RP2040.PWM0_CS_ADDR = 0x0000
RP2040.PWM0_CMPR0_ADDR = 0x0004
RP2040.PWM0_CMPR1_ADDR = 0x0008
RP2040.PWM0_CMPR2_ADDR = 0x000C
RP2040.PWM0_CMPR3_ADDR = 0x0010
RP2040.PWM0_CC_ADDR = 0x0014
RP2040.PWM0_TOP_ADDR = 0x0018
RP2040.PWM0_INTR_ADDR = 0x001C
RP2040.PWM0_INTE_ADDR = 0x0020
RP2040.PWM0_INTF_ADDR = 0x0024
RP2040.PWM0_INTS_ADDR = 0x0028
RP2040.PWM0_PHS0_ADDR = 0x0034
RP2040.PWM0_PHS1_ADDR = 0x0038
RP2040.PWM0_PHS2_ADDR = 0x003C
RP2040.PWM0_PHS3_ADDR = 0x0040
RP2040.PWM0_DIV_ADDR = 0x0044
RP2040.PWM0_PHASE_ADDR = 0x0048
-- PWM1
RP2040.PWM1_BASE = 0x40051000
RP2040.PWM1_CS_ADDR = 0x0000
RP2040.PWM1_CMPR0_ADDR = 0x0004
RP2040.PWM1_CMPR1_ADDR = 0x0008
RP2040.PWM1_CMPR2_ADDR = 0x000C
RP2040.PWM1_CMPR3_ADDR = 0x0010
RP2040.PWM1_CC_ADDR = 0x0014
RP2040.PWM1_TOP_ADDR = 0x0018
RP2040.PWM1_DIV_ADDR = 0x0044
-- ADC
RP2040.ADC_BASE = 0x4004C000
RP2040.ADC_ADC_CS_ADDR = 0x0000
RP2040.ADC_ADC_RESULT_ADDR = 0x0004
RP2040.ADC_ADC_FCS_ADDR = 0x0008
RP2040.ADC_ADC_FIFO_ADDR = 0x000C
RP2040.ADC_ADC_TS_ADDR = 0x0010
RP2040.ADC_ADC_OFFSET_ADDR = 0x0014
RP2040.ADC_ADC_TRIG_ADDR = 0x0018
-- Timer0
RP2040.TIMER0_BASE = 0x40054000
RP2040.TIMER0_TIMEHW_ADDR = 0x0000
RP2040.TIMER0_TIMELW_ADDR = 0x0004
RP2040.TIMER0_TIMEHA_ADDR = 0x0008
RP2040.TIMER0_TIMELA_ADDR = 0x000C
RP2040.TIMER0_TIMERA_ADDR = 0x0010
RP2040.TIMER0_TIMERIQ_ADDR = 0x0014
RP2040.TIMER0_TIMEREAD_ADDR = 0x0018
-- Timer1
RP2040.TIMER1_BASE = 0x40058000
RP2040.TIMER1_TIMEHW_ADDR = 0x0000
RP2040.TIMER1_TIMELW_ADDR = 0x0004
RP2040.TIMER1_TIMEHA_ADDR = 0x0008
RP2040.TIMER1_TIMELA_ADDR = 0x000C
RP2040.TIMER1_TIMERA_ADDR = 0x0010
-- RTC
RP2040.RTC_BASE = 0x4005C000
RP2040.RTC_RTC_CLKS_ADDR = 0x0000
RP2040.RTC_RTC_SET_ADDR = 0x0004
RP2040.RTC_RTC_WR_ADDR = 0x0008
RP2040.RTC_RTC_DATE_ADDR = 0x000C
RP2040.RTC_RTC_TOTAL_ADDR = 0x0010
RP2040.RTC_RTC_HASH_ADDR = 0x0014
RP2040.RTC_RTC_RTC_ADDR = 0x0018
RP2040.RTC_INTR_ADDR = 0x001C
RP2040.RTC_INTE_ADDR = 0x0020
RP2040.RTC_INTF_ADDR = 0x0024
RP2040.RTC_INTS_ADDR = 0x0028
-- Watchdog
RP2040.WATCHDOG_BASE = 0x40060000
RP2040.WATCHDOG_WATCHDOG_CTL_ADDR = 0x0000
RP2040.WATCHDOG_WATCHDOG_MOD_ADDR = 0x0004
RP2040.WATCHDOG_WATCHDOG_FR_ADDR = 0x0008
RP2040.WATCHDOG_WATCHDOG_LOAD_ADDR = 0x000C
-- USB
RP2040.USB_BASE = 0x50100000
RP2040.USB_USB_CTRL_ADDR = 0x0000
RP2040.USB_USB_ADDR_ADDR = 0x0004
RP2040.USB_USB_PWR_ADDR = 0x0008
RP2040.USB_USB_TXFIFO_ADDR = 0x0010
RP2040.USB_USB_RXFIFO_ADDR = 0x0014
RP2040.USB_USB_TXIE_ADDR = 0x0018
RP2040.USB_USB_RXIE_ADDR = 0x001C
RP2040.USB_USB_IS_ADDR = 0x0020
RP2040.USB_USB_IM_ADDR = 0x0024
RP2040.USB_USB_IE_ADDR = 0x0028
RP2040.USB_USB_REVO_ADDR = 0x002C
RP2040.USB_USB_EP_ADDR = 0x0030
RP2040.USB_USB_BUFF_ADDR = 0x0034
RP2040.USB_USB_MPS_ADDR = 0x0038
-- PIO0
RP2040.PIO0_BASE = 0x50200000
RP2040.PIO0_CTRL_ADDR = 0x0000
RP2040.PIO0_FSTAT_ADDR = 0x0004
RP2040.PIO0_FDEBUG_ADDR = 0x0008
RP2040.PIO0_FCTRL_ADDR = 0x000C
RP2040.PIO0_RXF0_ADDR = 0x0010
RP2040.PIO0_RXF1_ADDR = 0x0014
RP2040.PIO0_RXF2_ADDR = 0x0018
RP2040.PIO0_RXF3_ADDR = 0x001C
RP2040.PIO0_TXF0_ADDR = 0x0020
RP2040.PIO0_TXF1_ADDR = 0x0024
RP2040.PIO0_TXF2_ADDR = 0x0028
RP2040.PIO0_TXF3_ADDR = 0x002C
RP2040.PIO0_IRQ_ADDR = 0x0030
RP2040.PIO0_IRQ_FORCE_ADDR = 0x0034
RP2040.PIO0_IRQ_INTF_ADDR = 0x0038
RP2040.PIO0_IRQ_INTS_ADDR = 0x003C
RP2040.PIO0_SM0_CLKDIV_ADDR = 0x00C8
RP2040.PIO0_SM0_EXECCTRL_ADDR = 0x00CC
RP2040.PIO0_SM0_SHIFTCTRL_ADDR = 0x00D0
RP2040.PIO0_SM0_ADDR_ADDR = 0x00D4
RP2040.PIO0_SM0_INSTR_ADDR = 0x00D8
RP2040.PIO0_SM0_PINCTRL_ADDR = 0x00DC
-- PIO1
RP2040.PIO1_BASE = 0x50201000
RP2040.PIO1_CTRL_ADDR = 0x0000
RP2040.PIO1_FSTAT_ADDR = 0x0004
RP2040.PIO1_IRQ_ADDR = 0x0030
RP2040.PIO1_SM0_CLKDIV_ADDR = 0x00C8
RP2040.PIO1_SM0_EXECCTRL_ADDR = 0x00CC
RP2040.PIO1_SM0_SHIFTCTRL_ADDR = 0x00D0
RP2040.PIO1_SM0_ADDR_ADDR = 0x00D4
RP2040.PIO1_SM0_INSTR_ADDR = 0x00D8
-- Clock Manager
RP2040.CLOCKS_BASE = 0x40008000
RP2040.CLOCKS_CLK_GP0DIV_ADDR = 0x0000
RP2040.CLOCKS_CLK_GP0CTRL_ADDR = 0x0004
RP2040.CLOCKS_CLK_GP1DIV_ADDR = 0x0008
RP2040.CLOCKS_CLK_GP1CTRL_ADDR = 0x000C
RP2040.CLOCKS_CLK_GP2DIV_ADDR = 0x0010
RP2040.CLOCKS_CLK_GP2CTRL_ADDR = 0x0014
RP2040.CLOCKS_CLK_REF_ADDR = 0x001C
RP2040.CLOCKS_CLK_SYS_ADDR = 0x0020
RP2040.CLOCKS_CLK_PERI_ADDR = 0x0024
-- Crystal Oscillator
RP2040.XOSC_BASE = 0x40020000
RP2040.XOSC_XOSC_CTRL_ADDR = 0x0000
RP2040.XOSC_XOSC_STATUS_ADDR = 0x0004
RP2040.XOSC_XOSC_COUNT_ADDR = 0x0008
-- Ring Oscillator
RP2040.ROSC_BASE = 0x40010000
RP2040.ROSC_ROSC_CTRL_ADDR = 0x0000
RP2040.ROSC_ROSC_FREQA_ADDR = 0x0004
RP2040.ROSC_ROSC_FREQB_ADDR = 0x0008
RP2040.ROSC_ROSC_FREQC_ADDR = 0x000C
RP2040.ROSC_ROSC_FREQD_ADDR = 0x0010
RP2040.ROSC_ROSC_STATUS_ADDR = 0x0014
RP2040.ROSC_ROSC_DR_ADDR = 0x0018
-- System PLL
RP2040.PLL_SYS_BASE = 0x40028000
RP2040.PLL_SYS_PLL_CS_ADDR = 0x0000
RP2040.PLL_SYS_PLL_PWR_ADDR = 0x0004
RP2040.PLL_SYS_PLL_FBDIV_ADDR = 0x0008
RP2040.PLL_SYS_PLL_PRIMARY_ADDR = 0x000C
RP2040.PLL_SYS_PLL_POSTDIV1_ADDR = 0x0010
RP2040.PLL_SYS_PLL_POSTDIV2_ADDR = 0x0014
-- USB PLL
RP2040.PLL_USB_BASE = 0x4002C000
RP2040.PLL_USB_PLL_CS_ADDR = 0x0000
RP2040.PLL_USB_PLL_PWR_ADDR = 0x0004
RP2040.PLL_USB_PLL_FBDIV_ADDR = 0x0008
RP2040.PLL_USB_PLL_PRIMARY_ADDR = 0x000C
-- Resets
RP2040.RESETS_BASE = 0x4000C000
RP2040.RESETS_RESET_ADDR = 0x0000
RP2040.RESETS_RESET_DONE_ADDR = 0x0004
RP2040.RESETS_WD_RESET_ADDR = 0x0008

-- 中断向量定义
RP2040.INT_RESERVED = 0  -- Reserved
RP2040.INT_TIMER0_IRQ_0 = 1  -- Timer 0 IRQ 0
RP2040.INT_TIMER0_IRQ_1 = 2  -- Timer 0 IRQ 1
RP2040.INT_TIMER1_IRQ_0 = 3  -- Timer 1 IRQ 0
RP2040.INT_TIMER1_IRQ_1 = 4  -- Timer 1 IRQ 1
RP2040.INT_TIMER2_IRQ_0 = 5  -- Timer 2 IRQ 0
RP2040.INT_TIMER2_IRQ_1 = 6  -- Timer 2 IRQ 1
RP2040.INT_TIMER3_IRQ_0 = 7  -- Timer 3 IRQ 0
RP2040.INT_TIMER3_IRQ_1 = 8  -- Timer 3 IRQ 1
RP2040.INT_PWM_IRQ_WRAP = 9  -- PWM IRQ wrap
RP2040.INT_USB_CTRL_IRQ = 10  -- USB ctrl IRQ
RP2040.INT_USB_DMA_IRQ = 11  -- USB dma IRQ
RP2040.INT_USB_VBUS_DETECT = 12  -- USB VBUS detect IRQ
RP2040.INT_USB_RESUME_IRQ = 13  -- USB resume IRQ
RP2040.INT_ADC_IRQ_FIFO = 14  -- ADC IRQ FIFO
RP2040.INT_ADC_IRQ_TRIGGER = 15  -- ADC IRQ trigger
RP2040.INT_I2C0_IRQ = 16  -- I2C 0 IRQ
RP2040.INT_I2C1_IRQ = 17  -- I2C 1 IRQ
RP2040.INT_SPI0_IRQ = 18  -- SPI 0 IRQ
RP2040.INT_SPI1_IRQ = 19  -- SPI 1 IRQ
RP2040.INT_UART0_IRQ = 20  -- UART 0 IRQ
RP2040.INT_UART0_IRQ_TX = 21  -- UART 0 IRQ TX
RP2040.INT_UART1_IRQ = 22  -- UART 1 IRQ
RP2040.INT_UART1_IRQ_TX = 23  -- UART 1 IRQ TX
RP2040.INT_PIO0_IRQ_0 = 24  -- PIO 0 IRQ 0
RP2040.INT_PIO0_IRQ_1 = 25  -- PIO 0 IRQ 1
RP2040.INT_PIO1_IRQ_0 = 26  -- PIO 1 IRQ 0
RP2040.INT_PIO1_IRQ_1 = 27  -- PIO 1 IRQ 1
RP2040.INT_RTC_IRQ = 28  -- RTC IRQ

-- 引脚定义
RP2040.PIN_GP0 = 1  -- UART0 TX / GP0
RP2040.PIN_GP1 = 2  -- UART0 RX / GP1
RP2040.PIN_GP2 = 3  -- SPI0 TX / GP2
RP2040.PIN_GP3 = 4  -- SPI0 RX / GP3
RP2040.PIN_GP4 = 5  -- SPI0 CSn / GP4
RP2040.PIN_GP5 = 6  -- SPI0 SCK / GP5
RP2040.PIN_GP6 = 7  -- PWM6 / GP6
RP2040.PIN_GP7 = 8  -- PWM7 / GP7
RP2040.PIN_GP8 = 9  -- PWM8 / GP8
RP2040.PIN_GP9 = 10  -- PWM9 / GP9
RP2040.PIN_GP10 = 11  -- SPI1 TX / GP10
RP2040.PIN_GP11 = 12  -- SPI1 RX / GP11
RP2040.PIN_GP12 = 13  -- SPI1 CSn / GP12
RP2040.PIN_GP13 = 14  -- SPI1 SCK / GP13
RP2040.PIN_GP14 = 15  -- PWM14 / GP14
RP2040.PIN_GP15 = 16  -- PWM15 / GP15
RP2040.PIN_GP16 = 17  -- UART1 TX / GP16
RP2040.PIN_GP17 = 18  -- UART1 RX / GP17
RP2040.PIN_GP18 = 19  -- I2C0 SDA / GP18
RP2040.PIN_GP19 = 20  -- I2C0 SCL / GP19
RP2040.PIN_GP20 = 21  -- I2C1 SDA / GP20
RP2040.PIN_GP21 = 22  -- I2C1 SCL / GP21
RP2040.PIN_GP22 = 23  -- GP22
RP2040.PIN_RUN = 24  -- Run enable
RP2040.PIN_AGND = 25  -- Analog ground
RP2040.PIN_GP26 = 26  -- ADC0 / GP26
RP2040.PIN_GP27 = 27  -- ADC1 / GP27
RP2040.PIN_GP28 = 28  -- ADC2 / GP28
RP2040.PIN_ADC_VREF = 29  -- ADC voltage reference
RP2040.PIN_GP35 = 30  -- GP35
RP2040.PIN_GP34 = 31  -- GP34
RP2040.PIN_GP33 = 32  -- GP33
RP2040.PIN_GP36 = 37  -- GP36
RP2040.PIN_GP37 = 38  -- GP37
RP2040.PIN_GP38 = 39  -- GP38
RP2040.PIN_GP39 = 40  -- GP39
RP2040.PIN_GP40 = 41  -- GP40
RP2040.PIN_GP41 = 42  -- GP41
RP2040.PIN_SWCLK = 43  -- SWD Clock
RP2040.PIN_SWDIO = 44  -- SWD Data I/O

-- 设备类
function RP2040.new(memory_base)
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
        self.registers["xPSR"] = {
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
        self.registers["CONTROL"] = {
            address = 0xE0000E24,
            size = 4,
            access = "rw",
            description = "Control Register",
            value = 0
        }
        self.registers["FAULTMASK"] = {
            address = 0xE0000E28,
            size = 4,
            access = "rw",
            description = "Fault Mask Register",
            value = 0
        }
    end
    
    -- 初始化外设
    function self:_init_peripherals()
        self.peripherals["IO_BANK0"] = {
            base = 0x40014000,
            type = "gpio",
            description = "IO Bank 0",
            registers = {}
        }
        
        local p = self.peripherals["IO_BANK0"]
        p.registers["GPIO0_STATUS"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["GPIO0_CTRL"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["GPIO1_STATUS"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["GPIO1_CTRL"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["GPIO2_STATUS"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["GPIO2_CTRL"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["GPIO3_STATUS"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["GPIO3_CTRL"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["GPIO4_STATUS"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["GPIO4_CTRL"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["GPIO5_STATUS"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["GPIO5_CTRL"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["GPIO6_STATUS"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["GPIO6_CTRL"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["GPIO7_STATUS"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["GPIO7_CTRL"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["GPIO8_STATUS"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["GPIO8_CTRL"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["GPIO9_STATUS"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        p.registers["GPIO9_CTRL"] = {
            address = 0x004C,
            size = 4,
            value = 0
        }
        p.registers["GPIO10_STATUS"] = {
            address = 0x0050,
            size = 4,
            value = 0
        }
        p.registers["GPIO10_CTRL"] = {
            address = 0x0054,
            size = 4,
            value = 0
        }
        p.registers["GPIO11_STATUS"] = {
            address = 0x0058,
            size = 4,
            value = 0
        }
        p.registers["GPIO11_CTRL"] = {
            address = 0x005C,
            size = 4,
            value = 0
        }
        p.registers["GPIO12_STATUS"] = {
            address = 0x0060,
            size = 4,
            value = 0
        }
        p.registers["GPIO12_CTRL"] = {
            address = 0x0064,
            size = 4,
            value = 0
        }
        p.registers["GPIO13_STATUS"] = {
            address = 0x0068,
            size = 4,
            value = 0
        }
        p.registers["GPIO13_CTRL"] = {
            address = 0x006C,
            size = 4,
            value = 0
        }
        p.registers["GPIO14_STATUS"] = {
            address = 0x0070,
            size = 4,
            value = 0
        }
        p.registers["GPIO14_CTRL"] = {
            address = 0x0074,
            size = 4,
            value = 0
        }
        p.registers["GPIO15_STATUS"] = {
            address = 0x0078,
            size = 4,
            value = 0
        }
        p.registers["GPIO15_CTRL"] = {
            address = 0x007C,
            size = 4,
            value = 0
        }
        p.registers["GPIO16_STATUS"] = {
            address = 0x0080,
            size = 4,
            value = 0
        }
        p.registers["GPIO16_CTRL"] = {
            address = 0x0084,
            size = 4,
            value = 0
        }
        p.registers["GPIO17_STATUS"] = {
            address = 0x0088,
            size = 4,
            value = 0
        }
        p.registers["GPIO17_CTRL"] = {
            address = 0x008C,
            size = 4,
            value = 0
        }
        p.registers["GPIO18_STATUS"] = {
            address = 0x0090,
            size = 4,
            value = 0
        }
        p.registers["GPIO18_CTRL"] = {
            address = 0x0094,
            size = 4,
            value = 0
        }
        p.registers["GPIO19_STATUS"] = {
            address = 0x0098,
            size = 4,
            value = 0
        }
        p.registers["GPIO19_CTRL"] = {
            address = 0x009C,
            size = 4,
            value = 0
        }
        p.registers["GPIO20_STATUS"] = {
            address = 0x00A0,
            size = 4,
            value = 0
        }
        p.registers["GPIO20_CTRL"] = {
            address = 0x00A4,
            size = 4,
            value = 0
        }
        p.registers["GPIO21_STATUS"] = {
            address = 0x00A8,
            size = 4,
            value = 0
        }
        p.registers["GPIO21_CTRL"] = {
            address = 0x00AC,
            size = 4,
            value = 0
        }
        p.registers["GPIO22_STATUS"] = {
            address = 0x00B0,
            size = 4,
            value = 0
        }
        p.registers["GPIO22_CTRL"] = {
            address = 0x00B4,
            size = 4,
            value = 0
        }
        p.registers["GPIO23_STATUS"] = {
            address = 0x00B8,
            size = 4,
            value = 0
        }
        p.registers["GPIO23_CTRL"] = {
            address = 0x00BC,
            size = 4,
            value = 0
        }
        p.registers["GPIO24_STATUS"] = {
            address = 0x00C0,
            size = 4,
            value = 0
        }
        p.registers["GPIO24_CTRL"] = {
            address = 0x00C4,
            size = 4,
            value = 0
        }
        p.registers["GPIO25_STATUS"] = {
            address = 0x00C8,
            size = 4,
            value = 0
        }
        p.registers["GPIO25_CTRL"] = {
            address = 0x00CC,
            size = 4,
            value = 0
        }
        p.registers["GPIO26_STATUS"] = {
            address = 0x00D0,
            size = 4,
            value = 0
        }
        p.registers["GPIO26_CTRL"] = {
            address = 0x00D4,
            size = 4,
            value = 0
        }
        p.registers["GPIO27_STATUS"] = {
            address = 0x00D8,
            size = 4,
            value = 0
        }
        p.registers["GPIO27_CTRL"] = {
            address = 0x00DC,
            size = 4,
            value = 0
        }
        p.registers["GPIO28_STATUS"] = {
            address = 0x00E0,
            size = 4,
            value = 0
        }
        p.registers["GPIO28_CTRL"] = {
            address = 0x00E4,
            size = 4,
            value = 0
        }
        p.registers["GPIO29_STATUS"] = {
            address = 0x00E8,
            size = 4,
            value = 0
        }
        p.registers["GPIO29_CTRL"] = {
            address = 0x00EC,
            size = 4,
            value = 0
        }
        p.registers["INTR"] = {
            address = 0x00F0,
            size = 4,
            value = 0
        }
        p.registers["PROC0_INTE"] = {
            address = 0x00F4,
            size = 4,
            value = 0
        }
        p.registers["PROC1_INTE"] = {
            address = 0x00F8,
            size = 4,
            value = 0
        }
        p.registers["PROC0_INTF"] = {
            address = 0x00FC,
            size = 4,
            value = 0
        }
        p.registers["PROC1_INTF"] = {
            address = 0x0100,
            size = 4,
            value = 0
        }
        p.registers["PROC0_INTS"] = {
            address = 0x0104,
            size = 4,
            value = 0
        }
        p.registers["PROC1_INTS"] = {
            address = 0x0108,
            size = 4,
            value = 0
        }
        p.registers["DORMANT_WAKE_INTE"] = {
            address = 0x010C,
            size = 4,
            value = 0
        }
        p.registers["DORMANT_WAKE_INTF"] = {
            address = 0x0110,
            size = 4,
            value = 0
        }
        p.registers["DORMANT_WAKE_INTS"] = {
            address = 0x0114,
            size = 4,
            value = 0
        }
        self.peripherals["Pads"] = {
            base = 0x4001E000,
            type = "gpio",
            description = "Pads",
            registers = {}
        }
        
        local p = self.peripherals["Pads"]
        p.registers["GPIO_VOLT"] = {
            address = 0x00E0,
            size = 4,
            value = 0
        }
        self.peripherals["SIO"] = {
            base = 0xD0000000,
            type = "sysio",
            description = "SIO (Single-cycle I/O)",
            registers = {}
        }
        
        local p = self.peripherals["SIO"]
        p.registers["CPUID"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT_SET"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT_CLR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OUT_XOR"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OE"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OE_SET"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OE_CLR"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["GPIO_OE_XOR"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["GPIO_IN"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["FIFO_ST"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["FIFO_WR"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["FIFO_RD"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        p.registers["SPINLOCK_ST"] = {
            address = 0x004C,
            size = 4,
            value = 0
        }
        p.registers["INTERRUPT_ST"] = {
            address = 0x0050,
            size = 4,
            value = 0
        }
        self.peripherals["UART0"] = {
            base = 0x40034000,
            type = "uart",
            description = "UART0",
            registers = {}
        }
        
        local p = self.peripherals["UART0"]
        p.registers["UARTDR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["UARTRSR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["UARTECR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["UARTFR"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["UARTILPR"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["UARTIBRD"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["UARTFBRD"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["UARTLCR_H"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["UARTCR"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["UARTIFLS"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["UARTIMSC"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["UARTRIS"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["UARTMIS"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["UARTICR"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["UARTDMACR"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        self.peripherals["UART1"] = {
            base = 0x40038000,
            type = "uart",
            description = "UART1",
            registers = {}
        }
        
        local p = self.peripherals["UART1"]
        p.registers["UARTDR"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["UARTRSR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["UARTFR"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["UARTIBRD"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["UARTFBRD"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["UARTLCR_H"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["UARTCR"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["UARTIFLS"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["UARTIMSC"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["UARTICR"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        self.peripherals["SPI0"] = {
            base = 0x4003C000,
            type = "spi",
            description = "SPI0",
            registers = {}
        }
        
        local p = self.peripherals["SPI0"]
        p.registers["SSPCR0"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["SSPCR1"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["SSPDR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["SSPSR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["SSPCPSR"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["SSPIMSC"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["SSPRIS"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["SSPMIS"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["SSPICR"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["SSPDMACR"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        self.peripherals["SPI1"] = {
            base = 0x4003C000,
            type = "spi",
            description = "SPI1",
            registers = {}
        }
        
        local p = self.peripherals["SPI1"]
        p.registers["SSPCR0"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["SSPCR1"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["SSPDR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["SSPSR"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["SSPCPSR"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["SSPIMSC"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        self.peripherals["I2C0"] = {
            base = 0x40044000,
            type = "i2c",
            description = "I2C0",
            registers = {}
        }
        
        local p = self.peripherals["I2C0"]
        p.registers["IC_CON"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["IC_TAR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IC_SAR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["IC_DATA_CMD"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["IC_SS_SCL_HCNT"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["IC_SS_SCL_LCNT"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["IC_FS_SCL_HCNT"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["IC_FS_SCL_LCNT"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["IC_RAW_INTR_STAT"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["IC_ENABLE"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["IC_STATUS"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["IC_TXFLR"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["IC_RXFLR"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["IC_TX_ABRT"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["IC_DMA_CR"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["IC_DMA_TDLR"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["IC_DMA_RDLR"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        self.peripherals["I2C1"] = {
            base = 0x40048000,
            type = "i2c",
            description = "I2C1",
            registers = {}
        }
        
        local p = self.peripherals["I2C1"]
        p.registers["IC_CON"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["IC_TAR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IC_ENABLE"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["IC_STATUS"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["IC_TX_ABRT"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        self.peripherals["PWM0"] = {
            base = 0x40050000,
            type = "pwm",
            description = "PWM0",
            registers = {}
        }
        
        local p = self.peripherals["PWM0"]
        p.registers["CS"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["CMPR0"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["CMPR1"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["CMPR2"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["CMPR3"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["CC"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["TOP"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["INTR"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["INTE"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["INTF"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["INTS"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["PHS0"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["PHS1"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["PHS2"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["PHS3"] = {
            address = 0x0040,
            size = 4,
            value = 0
        }
        p.registers["DIV"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        p.registers["PHASE"] = {
            address = 0x0048,
            size = 4,
            value = 0
        }
        self.peripherals["PWM1"] = {
            base = 0x40051000,
            type = "pwm",
            description = "PWM1",
            registers = {}
        }
        
        local p = self.peripherals["PWM1"]
        p.registers["CS"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["CMPR0"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["CMPR1"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["CMPR2"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["CMPR3"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["CC"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["TOP"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["DIV"] = {
            address = 0x0044,
            size = 4,
            value = 0
        }
        self.peripherals["ADC"] = {
            base = 0x4004C000,
            type = "adc",
            description = "ADC",
            registers = {}
        }
        
        local p = self.peripherals["ADC"]
        p.registers["ADC_CS"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["ADC_RESULT"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["ADC_FCS"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["ADC_FIFO"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["ADC_TS"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["ADC_OFFSET"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["ADC_TRIG"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        self.peripherals["TIMER0"] = {
            base = 0x40054000,
            type = "timer",
            description = "Timer0",
            registers = {}
        }
        
        local p = self.peripherals["TIMER0"]
        p.registers["TIMEHW"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TIMELW"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["TIMEHA"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["TIMELA"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["TIMERA"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["TIMERIQ"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["TIMEREAD"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        self.peripherals["TIMER1"] = {
            base = 0x40058000,
            type = "timer",
            description = "Timer1",
            registers = {}
        }
        
        local p = self.peripherals["TIMER1"]
        p.registers["TIMEHW"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["TIMELW"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["TIMEHA"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["TIMELA"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["TIMERA"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        self.peripherals["RTC"] = {
            base = 0x4005C000,
            type = "rtc",
            description = "RTC",
            registers = {}
        }
        
        local p = self.peripherals["RTC"]
        p.registers["RTC_CLKS"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["RTC_SET"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["RTC_WR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["RTC_DATE"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["RTC_TOTAL"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["RTC_HASH"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["RTC_RTC"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["INTR"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["INTE"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["INTF"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["INTS"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        self.peripherals["WATCHDOG"] = {
            base = 0x40060000,
            type = "wdt",
            description = "Watchdog",
            registers = {}
        }
        
        local p = self.peripherals["WATCHDOG"]
        p.registers["WATCHDOG_CTL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["WATCHDOG_MOD"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["WATCHDOG_FR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["WATCHDOG_LOAD"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        self.peripherals["USB"] = {
            base = 0x50100000,
            type = "usb",
            description = "USB",
            registers = {}
        }
        
        local p = self.peripherals["USB"]
        p.registers["USB_CTRL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["USB_ADDR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["USB_PWR"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["USB_TXFIFO"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["USB_RXFIFO"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["USB_TXIE"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["USB_RXIE"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["USB_IS"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["USB_IM"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["USB_IE"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["USB_REVO"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["USB_EP"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["USB_BUFF"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["USB_MPS"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        self.peripherals["PIO0"] = {
            base = 0x50200000,
            type = "pio",
            description = "PIO0",
            registers = {}
        }
        
        local p = self.peripherals["PIO0"]
        p.registers["CTRL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["FSTAT"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["FDEBUG"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["FCTRL"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["RXF0"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["RXF1"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["RXF2"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        p.registers["RXF3"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["TXF0"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["TXF1"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        p.registers["TXF2"] = {
            address = 0x0028,
            size = 4,
            value = 0
        }
        p.registers["TXF3"] = {
            address = 0x002C,
            size = 4,
            value = 0
        }
        p.registers["IRQ"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["IRQ_FORCE"] = {
            address = 0x0034,
            size = 4,
            value = 0
        }
        p.registers["IRQ_INTF"] = {
            address = 0x0038,
            size = 4,
            value = 0
        }
        p.registers["IRQ_INTS"] = {
            address = 0x003C,
            size = 4,
            value = 0
        }
        p.registers["SM0_CLKDIV"] = {
            address = 0x00C8,
            size = 4,
            value = 0
        }
        p.registers["SM0_EXECCTRL"] = {
            address = 0x00CC,
            size = 4,
            value = 0
        }
        p.registers["SM0_SHIFTCTRL"] = {
            address = 0x00D0,
            size = 4,
            value = 0
        }
        p.registers["SM0_ADDR"] = {
            address = 0x00D4,
            size = 4,
            value = 0
        }
        p.registers["SM0_INSTR"] = {
            address = 0x00D8,
            size = 4,
            value = 0
        }
        p.registers["SM0_PINCTRL"] = {
            address = 0x00DC,
            size = 4,
            value = 0
        }
        self.peripherals["PIO1"] = {
            base = 0x50201000,
            type = "pio",
            description = "PIO1",
            registers = {}
        }
        
        local p = self.peripherals["PIO1"]
        p.registers["CTRL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["FSTAT"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["IRQ"] = {
            address = 0x0030,
            size = 4,
            value = 0
        }
        p.registers["SM0_CLKDIV"] = {
            address = 0x00C8,
            size = 4,
            value = 0
        }
        p.registers["SM0_EXECCTRL"] = {
            address = 0x00CC,
            size = 4,
            value = 0
        }
        p.registers["SM0_SHIFTCTRL"] = {
            address = 0x00D0,
            size = 4,
            value = 0
        }
        p.registers["SM0_ADDR"] = {
            address = 0x00D4,
            size = 4,
            value = 0
        }
        p.registers["SM0_INSTR"] = {
            address = 0x00D8,
            size = 4,
            value = 0
        }
        self.peripherals["CLOCKS"] = {
            base = 0x40008000,
            type = "clock",
            description = "Clock Manager",
            registers = {}
        }
        
        local p = self.peripherals["CLOCKS"]
        p.registers["CLK_GP0DIV"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["CLK_GP0CTRL"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["CLK_GP1DIV"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["CLK_GP1CTRL"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["CLK_GP2DIV"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["CLK_GP2CTRL"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["CLK_REF"] = {
            address = 0x001C,
            size = 4,
            value = 0
        }
        p.registers["CLK_SYS"] = {
            address = 0x0020,
            size = 4,
            value = 0
        }
        p.registers["CLK_PERI"] = {
            address = 0x0024,
            size = 4,
            value = 0
        }
        self.peripherals["XOSC"] = {
            base = 0x40020000,
            type = "osc",
            description = "Crystal Oscillator",
            registers = {}
        }
        
        local p = self.peripherals["XOSC"]
        p.registers["XOSC_CTRL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["XOSC_STATUS"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["XOSC_COUNT"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        self.peripherals["ROSC"] = {
            base = 0x40010000,
            type = "osc",
            description = "Ring Oscillator",
            registers = {}
        }
        
        local p = self.peripherals["ROSC"]
        p.registers["ROSC_CTRL"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["ROSC_FREQA"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["ROSC_FREQB"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["ROSC_FREQC"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["ROSC_FREQD"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["ROSC_STATUS"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        p.registers["ROSC_DR"] = {
            address = 0x0018,
            size = 4,
            value = 0
        }
        self.peripherals["PLL_SYS"] = {
            base = 0x40028000,
            type = "pll",
            description = "System PLL",
            registers = {}
        }
        
        local p = self.peripherals["PLL_SYS"]
        p.registers["PLL_CS"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["PLL_PWR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["PLL_FBDIV"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["PLL_PRIMARY"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        p.registers["PLL_POSTDIV1"] = {
            address = 0x0010,
            size = 4,
            value = 0
        }
        p.registers["PLL_POSTDIV2"] = {
            address = 0x0014,
            size = 4,
            value = 0
        }
        self.peripherals["PLL_USB"] = {
            base = 0x4002C000,
            type = "pll",
            description = "USB PLL",
            registers = {}
        }
        
        local p = self.peripherals["PLL_USB"]
        p.registers["PLL_CS"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["PLL_PWR"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["PLL_FBDIV"] = {
            address = 0x0008,
            size = 4,
            value = 0
        }
        p.registers["PLL_PRIMARY"] = {
            address = 0x000C,
            size = 4,
            value = 0
        }
        self.peripherals["RESETS"] = {
            base = 0x4000C000,
            type = "reset",
            description = "Resets",
            registers = {}
        }
        
        local p = self.peripherals["RESETS"]
        p.registers["RESET"] = {
            address = 0x0000,
            size = 4,
            value = 0
        }
        p.registers["RESET_DONE"] = {
            address = 0x0004,
            size = 4,
            value = 0
        }
        p.registers["WD_RESET"] = {
            address = 0x0008,
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
            name = RP2040.DEVICE_NAME,
            manufacturer = RP2040.MANUFACTURER,
            family = RP2040.FAMILY,
            version = RP2040.VERSION,
            architecture = RP2040.ARCHITECTURE,
            bits = RP2040.BITS,
            clock_frequency = RP2040.CLOCK_FREQUENCY
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
        return string.format("RP2040(%s v%s)", info.name, info.version)
    end
    
    -- 初始化
    self:_init_registers()
    self:_init_peripherals()
    
    setmetatable(self, { __tostring = self.__tostring })
    
    return self
end

-- 工具函数
function RP2040.hex(value, width)
    width = width or 2
    return string.format("0x%0" .. width .. "X", value)
end

function RP2040.bin(value, width)
    width = width or 8
    local result = ""
    for i = width-1, 0, -1 do
        result = result .. (bit.band(bit.rshift(value, i), 1))
    end
    return "0b" .. result
end

function RP2040.print_device_info(device)
    device = device or RP2040.new()
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

function RP2040.print_registers(device)
    device = device or RP2040.new()
    
    print("寄存器状态:")
    for name, reg in pairs(device.registers) do
        print(string.format("  %-8s: %s (%s)", 
            name, 
            RP2040.hex(reg.value, reg.size * 2), 
            reg.description))
    end
end

-- 示例代码
function RP2040.example()
    print("=== RP2040设备示例 ===")
    
    -- 创建设备实例
    local device = RP2040.new()
    print("创建设备: " .. tostring(device))
    
    -- 显示设备信息
    RP2040.print_device_info(device)
    
    -- 演示寄存器操作
    if device.registers["R0"] then
        print("\n演示寄存器操作:")
        
        -- 写入寄存器
        device:write_register("R0", 0x55)
        print("写入 R0: " .. RP2040.hex(0x55))
        
        -- 读取寄存器
        local value = device:read_register("R0")
        print("读取 R0: " .. RP2040.hex(value))
        
        -- 位操作
        device:set_bit("R0", 0, true)
        local bit0 = device:get_bit("R0", 0)
        print("位0: " .. tostring(bit0))
    end
    
    -- 显示寄存器状态
    RP2040.print_registers(device)
    
    -- 重置设备
    device:reset()
    print("\n设备已重置")
    
    print("=== 示例完成 ===")
end

-- 如果直接运行此文件，执行示例
if arg and arg[0]:find("RP2040.lua$") then
    RP2040.example()
end

return RP2040

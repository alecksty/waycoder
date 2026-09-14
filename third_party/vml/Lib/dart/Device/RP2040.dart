// RP2040 设备定义 - Dart 库
// 生成自: Raspberry Pi/RP/RP2040
// 版本: 1.0
// 日期: 2026-04-16
// 作者: VML Team
// 描述: Dual-core ARM Cortex-M0+ up to 133MHz with 264KB SRAM
// CPU架构: ARM-Cortex-M0+
// 位宽: 32位
// 时钟频率: 12000000 Hz

class RP2040Device {
  static const String deviceName = "RP2040";
  static const String manufacturer = "Raspberry Pi";
  static const String family = "RP";
  static const String version = "1.0";
  static const String architecture = "ARM-Cortex-M0+";
  static const int bits = 32;
  static const int clockFrequency = 12000000;

  // 寄存器地址定义
  static const int R0_ADDR = 0x00000000;  // General Purpose Register 0
  static const int R1_ADDR = 0x00000004;  // General Purpose Register 1
  static const int R2_ADDR = 0x00000008;  // General Purpose Register 2
  static const int R3_ADDR = 0x0000000C;  // General Purpose Register 3
  static const int R4_ADDR = 0x00000010;  // General Purpose Register 4
  static const int R5_ADDR = 0x00000014;  // General Purpose Register 5
  static const int R6_ADDR = 0x00000018;  // General Purpose Register 6
  static const int R7_ADDR = 0x0000001C;  // General Purpose Register 7
  static const int R8_ADDR = 0x00000020;  // General Purpose Register 8
  static const int R9_ADDR = 0x00000024;  // General Purpose Register 9
  static const int R10_ADDR = 0x00000028;  // General Purpose Register 10
  static const int R11_ADDR = 0x0000002C;  // General Purpose Register 11
  static const int R12_ADDR = 0x00000030;  // General Purpose Register 12
  static const int SP_ADDR = 0x00000034;  // Stack Pointer
  static const int LR_ADDR = 0x00000038;  // Link Register
  static const int PC_ADDR = 0x0000003C;  // Program Counter
  static const int XPSR_ADDR = 0x00000040;  // Program Status Register
  static const int XPSR_N_BIT = 31;  // Negative Flag
  static const int XPSR_Z_BIT = 30;  // Zero Flag
  static const int XPSR_C_BIT = 29;  // Carry Flag
  static const int XPSR_V_BIT = 28;  // Overflow Flag
  static const int XPSR_Q_BIT = 27;  // Saturation Flag
  static const int XPSR_ICI_BIT = 0;  // ICI execution state
  static const int XPSR_IT_BIT = 0;  // If-Then execution state
  static const int XPSR_T_BIT = 24;  // Thumb bit
  static const int XPSR_IPSR_BIT = 0;  // Exception number
  static const int PRIMASK_ADDR = 0xE0000E20;  // Priority Mask Register
  static const int CONTROL_ADDR = 0xE0000E24;  // Control Register
  static const int FAULTMASK_ADDR = 0xE0000E28;  // Fault Mask Register

  // 内存段定义
  static const int ROM_START = 0x00000000;
  static const int ROM_END = 0x00001000;
  static const int ROM_SIZE = 4096;  // ROM (bootloader)
  static const int SRAM0_START = 0x20000000;
  static const int SRAM0_END = 0x20003FFF;
  static const int SRAM0_SIZE = 16384;  // SRAM0 (16KB)
  static const int SRAM1_START = 0x20004000;
  static const int SRAM1_END = 0x20007FFF;
  static const int SRAM1_SIZE = 16384;  // SRAM1 (16KB)
  static const int SRAM2_START = 0x20008000;
  static const int SRAM2_END = 0x2000BFFF;
  static const int SRAM2_SIZE = 16384;  // SRAM2 (16KB)
  static const int SRAM3_START = 0x2000C000;
  static const int SRAM3_END = 0x2000FFFF;
  static const int SRAM3_SIZE = 16384;  // SRAM3 (16KB)
  static const int SRAM4_START = 0x20010000;
  static const int SRAM4_END = 0x20013FFF;
  static const int SRAM4_SIZE = 16384;  // SRAM4 (16KB)
  static const int APB_START = 0x40000000;
  static const int APB_END = 0x400FFFFF;
  static const int APB_SIZE = 1048576;  // APB Peripherals
  static const int AHB_START = 0x50000000;
  static const int AHB_END = 0x500FFFFF;
  static const int AHB_SIZE = 1048576;  // AHB Peripherals

  // 外设定义
  // IO Bank 0
  static const int IO_BANK0_BASE = 0x40014000;
  static const int IO_BANK0_GPIO0_STATUS_ADDR = 0x0000;
  static const int IO_BANK0_GPIO0_CTRL_ADDR = 0x0004;
  static const int IO_BANK0_GPIO1_STATUS_ADDR = 0x0008;
  static const int IO_BANK0_GPIO1_CTRL_ADDR = 0x000C;
  static const int IO_BANK0_GPIO2_STATUS_ADDR = 0x0010;
  static const int IO_BANK0_GPIO2_CTRL_ADDR = 0x0014;
  static const int IO_BANK0_GPIO3_STATUS_ADDR = 0x0018;
  static const int IO_BANK0_GPIO3_CTRL_ADDR = 0x001C;
  static const int IO_BANK0_GPIO4_STATUS_ADDR = 0x0020;
  static const int IO_BANK0_GPIO4_CTRL_ADDR = 0x0024;
  static const int IO_BANK0_GPIO5_STATUS_ADDR = 0x0028;
  static const int IO_BANK0_GPIO5_CTRL_ADDR = 0x002C;
  static const int IO_BANK0_GPIO6_STATUS_ADDR = 0x0030;
  static const int IO_BANK0_GPIO6_CTRL_ADDR = 0x0034;
  static const int IO_BANK0_GPIO7_STATUS_ADDR = 0x0038;
  static const int IO_BANK0_GPIO7_CTRL_ADDR = 0x003C;
  static const int IO_BANK0_GPIO8_STATUS_ADDR = 0x0040;
  static const int IO_BANK0_GPIO8_CTRL_ADDR = 0x0044;
  static const int IO_BANK0_GPIO9_STATUS_ADDR = 0x0048;
  static const int IO_BANK0_GPIO9_CTRL_ADDR = 0x004C;
  static const int IO_BANK0_GPIO10_STATUS_ADDR = 0x0050;
  static const int IO_BANK0_GPIO10_CTRL_ADDR = 0x0054;
  static const int IO_BANK0_GPIO11_STATUS_ADDR = 0x0058;
  static const int IO_BANK0_GPIO11_CTRL_ADDR = 0x005C;
  static const int IO_BANK0_GPIO12_STATUS_ADDR = 0x0060;
  static const int IO_BANK0_GPIO12_CTRL_ADDR = 0x0064;
  static const int IO_BANK0_GPIO13_STATUS_ADDR = 0x0068;
  static const int IO_BANK0_GPIO13_CTRL_ADDR = 0x006C;
  static const int IO_BANK0_GPIO14_STATUS_ADDR = 0x0070;
  static const int IO_BANK0_GPIO14_CTRL_ADDR = 0x0074;
  static const int IO_BANK0_GPIO15_STATUS_ADDR = 0x0078;
  static const int IO_BANK0_GPIO15_CTRL_ADDR = 0x007C;
  static const int IO_BANK0_GPIO16_STATUS_ADDR = 0x0080;
  static const int IO_BANK0_GPIO16_CTRL_ADDR = 0x0084;
  static const int IO_BANK0_GPIO17_STATUS_ADDR = 0x0088;
  static const int IO_BANK0_GPIO17_CTRL_ADDR = 0x008C;
  static const int IO_BANK0_GPIO18_STATUS_ADDR = 0x0090;
  static const int IO_BANK0_GPIO18_CTRL_ADDR = 0x0094;
  static const int IO_BANK0_GPIO19_STATUS_ADDR = 0x0098;
  static const int IO_BANK0_GPIO19_CTRL_ADDR = 0x009C;
  static const int IO_BANK0_GPIO20_STATUS_ADDR = 0x00A0;
  static const int IO_BANK0_GPIO20_CTRL_ADDR = 0x00A4;
  static const int IO_BANK0_GPIO21_STATUS_ADDR = 0x00A8;
  static const int IO_BANK0_GPIO21_CTRL_ADDR = 0x00AC;
  static const int IO_BANK0_GPIO22_STATUS_ADDR = 0x00B0;
  static const int IO_BANK0_GPIO22_CTRL_ADDR = 0x00B4;
  static const int IO_BANK0_GPIO23_STATUS_ADDR = 0x00B8;
  static const int IO_BANK0_GPIO23_CTRL_ADDR = 0x00BC;
  static const int IO_BANK0_GPIO24_STATUS_ADDR = 0x00C0;
  static const int IO_BANK0_GPIO24_CTRL_ADDR = 0x00C4;
  static const int IO_BANK0_GPIO25_STATUS_ADDR = 0x00C8;
  static const int IO_BANK0_GPIO25_CTRL_ADDR = 0x00CC;
  static const int IO_BANK0_GPIO26_STATUS_ADDR = 0x00D0;
  static const int IO_BANK0_GPIO26_CTRL_ADDR = 0x00D4;
  static const int IO_BANK0_GPIO27_STATUS_ADDR = 0x00D8;
  static const int IO_BANK0_GPIO27_CTRL_ADDR = 0x00DC;
  static const int IO_BANK0_GPIO28_STATUS_ADDR = 0x00E0;
  static const int IO_BANK0_GPIO28_CTRL_ADDR = 0x00E4;
  static const int IO_BANK0_GPIO29_STATUS_ADDR = 0x00E8;
  static const int IO_BANK0_GPIO29_CTRL_ADDR = 0x00EC;
  static const int IO_BANK0_INTR_ADDR = 0x00F0;
  static const int IO_BANK0_PROC0_INTE_ADDR = 0x00F4;
  static const int IO_BANK0_PROC1_INTE_ADDR = 0x00F8;
  static const int IO_BANK0_PROC0_INTF_ADDR = 0x00FC;
  static const int IO_BANK0_PROC1_INTF_ADDR = 0x0100;
  static const int IO_BANK0_PROC0_INTS_ADDR = 0x0104;
  static const int IO_BANK0_PROC1_INTS_ADDR = 0x0108;
  static const int IO_BANK0_DORMANT_WAKE_INTE_ADDR = 0x010C;
  static const int IO_BANK0_DORMANT_WAKE_INTF_ADDR = 0x0110;
  static const int IO_BANK0_DORMANT_WAKE_INTS_ADDR = 0x0114;
  // Pads
  static const int PADS_BASE = 0x4001E000;
  static const int PADS_GPIO_VOLT_ADDR = 0x00E0;
  // SIO (Single-cycle I/O)
  static const int SIO_BASE = 0xD0000000;
  static const int SIO_CPUID_ADDR = 0x0000;
  static const int SIO_GPIO_OUT_ADDR = 0x0004;
  static const int SIO_GPIO_OUT_SET_ADDR = 0x0008;
  static const int SIO_GPIO_OUT_CLR_ADDR = 0x000C;
  static const int SIO_GPIO_OUT_XOR_ADDR = 0x0010;
  static const int SIO_GPIO_OE_ADDR = 0x0014;
  static const int SIO_GPIO_OE_SET_ADDR = 0x0018;
  static const int SIO_GPIO_OE_CLR_ADDR = 0x001C;
  static const int SIO_GPIO_OE_XOR_ADDR = 0x0020;
  static const int SIO_GPIO_IN_ADDR = 0x0024;
  static const int SIO_FIFO_ST_ADDR = 0x0040;
  static const int SIO_FIFO_WR_ADDR = 0x0044;
  static const int SIO_FIFO_RD_ADDR = 0x0048;
  static const int SIO_SPINLOCK_ST_ADDR = 0x004C;
  static const int SIO_INTERRUPT_ST_ADDR = 0x0050;
  // UART0
  static const int UART0_BASE = 0x40034000;
  static const int UART0_UARTDR_ADDR = 0x0000;
  static const int UART0_UARTRSR_ADDR = 0x0004;
  static const int UART0_UARTECR_ADDR = 0x0004;
  static const int UART0_UARTFR_ADDR = 0x0018;
  static const int UART0_UARTILPR_ADDR = 0x0020;
  static const int UART0_UARTIBRD_ADDR = 0x0024;
  static const int UART0_UARTFBRD_ADDR = 0x0028;
  static const int UART0_UARTLCR_H_ADDR = 0x002C;
  static const int UART0_UARTCR_ADDR = 0x0030;
  static const int UART0_UARTIFLS_ADDR = 0x0034;
  static const int UART0_UARTIMSC_ADDR = 0x0038;
  static const int UART0_UARTRIS_ADDR = 0x003C;
  static const int UART0_UARTMIS_ADDR = 0x0040;
  static const int UART0_UARTICR_ADDR = 0x0044;
  static const int UART0_UARTDMACR_ADDR = 0x0048;
  // UART1
  static const int UART1_BASE = 0x40038000;
  static const int UART1_UARTDR_ADDR = 0x0000;
  static const int UART1_UARTRSR_ADDR = 0x0004;
  static const int UART1_UARTFR_ADDR = 0x0018;
  static const int UART1_UARTIBRD_ADDR = 0x0024;
  static const int UART1_UARTFBRD_ADDR = 0x0028;
  static const int UART1_UARTLCR_H_ADDR = 0x002C;
  static const int UART1_UARTCR_ADDR = 0x0030;
  static const int UART1_UARTIFLS_ADDR = 0x0034;
  static const int UART1_UARTIMSC_ADDR = 0x0038;
  static const int UART1_UARTICR_ADDR = 0x0044;
  // SPI0
  static const int SPI0_BASE = 0x4003C000;
  static const int SPI0_SSPCR0_ADDR = 0x0000;
  static const int SPI0_SSPCR1_ADDR = 0x0004;
  static const int SPI0_SSPDR_ADDR = 0x0008;
  static const int SPI0_SSPSR_ADDR = 0x000C;
  static const int SPI0_SSPCPSR_ADDR = 0x0010;
  static const int SPI0_SSPIMSC_ADDR = 0x0014;
  static const int SPI0_SSPRIS_ADDR = 0x0018;
  static const int SPI0_SSPMIS_ADDR = 0x001C;
  static const int SPI0_SSPICR_ADDR = 0x0020;
  static const int SPI0_SSPDMACR_ADDR = 0x0024;
  // SPI1
  static const int SPI1_BASE = 0x4003C000;
  static const int SPI1_SSPCR0_ADDR = 0x0000;
  static const int SPI1_SSPCR1_ADDR = 0x0004;
  static const int SPI1_SSPDR_ADDR = 0x0008;
  static const int SPI1_SSPSR_ADDR = 0x000C;
  static const int SPI1_SSPCPSR_ADDR = 0x0010;
  static const int SPI1_SSPIMSC_ADDR = 0x0014;
  // I2C0
  static const int I2C0_BASE = 0x40044000;
  static const int I2C0_IC_CON_ADDR = 0x0000;
  static const int I2C0_IC_TAR_ADDR = 0x0004;
  static const int I2C0_IC_SAR_ADDR = 0x0008;
  static const int I2C0_IC_DATA_CMD_ADDR = 0x0010;
  static const int I2C0_IC_SS_SCL_HCNT_ADDR = 0x0014;
  static const int I2C0_IC_SS_SCL_LCNT_ADDR = 0x0018;
  static const int I2C0_IC_FS_SCL_HCNT_ADDR = 0x001C;
  static const int I2C0_IC_FS_SCL_LCNT_ADDR = 0x0020;
  static const int I2C0_IC_RAW_INTR_STAT_ADDR = 0x0024;
  static const int I2C0_IC_ENABLE_ADDR = 0x002C;
  static const int I2C0_IC_STATUS_ADDR = 0x0030;
  static const int I2C0_IC_TXFLR_ADDR = 0x0034;
  static const int I2C0_IC_RXFLR_ADDR = 0x0038;
  static const int I2C0_IC_TX_ABRT_ADDR = 0x003C;
  static const int I2C0_IC_DMA_CR_ADDR = 0x0040;
  static const int I2C0_IC_DMA_TDLR_ADDR = 0x0044;
  static const int I2C0_IC_DMA_RDLR_ADDR = 0x0048;
  // I2C1
  static const int I2C1_BASE = 0x40048000;
  static const int I2C1_IC_CON_ADDR = 0x0000;
  static const int I2C1_IC_TAR_ADDR = 0x0004;
  static const int I2C1_IC_ENABLE_ADDR = 0x002C;
  static const int I2C1_IC_STATUS_ADDR = 0x0030;
  static const int I2C1_IC_TX_ABRT_ADDR = 0x003C;
  // PWM0
  static const int PWM0_BASE = 0x40050000;
  static const int PWM0_CS_ADDR = 0x0000;
  static const int PWM0_CMPR0_ADDR = 0x0004;
  static const int PWM0_CMPR1_ADDR = 0x0008;
  static const int PWM0_CMPR2_ADDR = 0x000C;
  static const int PWM0_CMPR3_ADDR = 0x0010;
  static const int PWM0_CC_ADDR = 0x0014;
  static const int PWM0_TOP_ADDR = 0x0018;
  static const int PWM0_INTR_ADDR = 0x001C;
  static const int PWM0_INTE_ADDR = 0x0020;
  static const int PWM0_INTF_ADDR = 0x0024;
  static const int PWM0_INTS_ADDR = 0x0028;
  static const int PWM0_PHS0_ADDR = 0x0034;
  static const int PWM0_PHS1_ADDR = 0x0038;
  static const int PWM0_PHS2_ADDR = 0x003C;
  static const int PWM0_PHS3_ADDR = 0x0040;
  static const int PWM0_DIV_ADDR = 0x0044;
  static const int PWM0_PHASE_ADDR = 0x0048;
  // PWM1
  static const int PWM1_BASE = 0x40051000;
  static const int PWM1_CS_ADDR = 0x0000;
  static const int PWM1_CMPR0_ADDR = 0x0004;
  static const int PWM1_CMPR1_ADDR = 0x0008;
  static const int PWM1_CMPR2_ADDR = 0x000C;
  static const int PWM1_CMPR3_ADDR = 0x0010;
  static const int PWM1_CC_ADDR = 0x0014;
  static const int PWM1_TOP_ADDR = 0x0018;
  static const int PWM1_DIV_ADDR = 0x0044;
  // ADC
  static const int ADC_BASE = 0x4004C000;
  static const int ADC_ADC_CS_ADDR = 0x0000;
  static const int ADC_ADC_RESULT_ADDR = 0x0004;
  static const int ADC_ADC_FCS_ADDR = 0x0008;
  static const int ADC_ADC_FIFO_ADDR = 0x000C;
  static const int ADC_ADC_TS_ADDR = 0x0010;
  static const int ADC_ADC_OFFSET_ADDR = 0x0014;
  static const int ADC_ADC_TRIG_ADDR = 0x0018;
  // Timer0
  static const int TIMER0_BASE = 0x40054000;
  static const int TIMER0_TIMEHW_ADDR = 0x0000;
  static const int TIMER0_TIMELW_ADDR = 0x0004;
  static const int TIMER0_TIMEHA_ADDR = 0x0008;
  static const int TIMER0_TIMELA_ADDR = 0x000C;
  static const int TIMER0_TIMERA_ADDR = 0x0010;
  static const int TIMER0_TIMERIQ_ADDR = 0x0014;
  static const int TIMER0_TIMEREAD_ADDR = 0x0018;
  // Timer1
  static const int TIMER1_BASE = 0x40058000;
  static const int TIMER1_TIMEHW_ADDR = 0x0000;
  static const int TIMER1_TIMELW_ADDR = 0x0004;
  static const int TIMER1_TIMEHA_ADDR = 0x0008;
  static const int TIMER1_TIMELA_ADDR = 0x000C;
  static const int TIMER1_TIMERA_ADDR = 0x0010;
  // RTC
  static const int RTC_BASE = 0x4005C000;
  static const int RTC_RTC_CLKS_ADDR = 0x0000;
  static const int RTC_RTC_SET_ADDR = 0x0004;
  static const int RTC_RTC_WR_ADDR = 0x0008;
  static const int RTC_RTC_DATE_ADDR = 0x000C;
  static const int RTC_RTC_TOTAL_ADDR = 0x0010;
  static const int RTC_RTC_HASH_ADDR = 0x0014;
  static const int RTC_RTC_RTC_ADDR = 0x0018;
  static const int RTC_INTR_ADDR = 0x001C;
  static const int RTC_INTE_ADDR = 0x0020;
  static const int RTC_INTF_ADDR = 0x0024;
  static const int RTC_INTS_ADDR = 0x0028;
  // Watchdog
  static const int WATCHDOG_BASE = 0x40060000;
  static const int WATCHDOG_WATCHDOG_CTL_ADDR = 0x0000;
  static const int WATCHDOG_WATCHDOG_MOD_ADDR = 0x0004;
  static const int WATCHDOG_WATCHDOG_FR_ADDR = 0x0008;
  static const int WATCHDOG_WATCHDOG_LOAD_ADDR = 0x000C;
  // USB
  static const int USB_BASE = 0x50100000;
  static const int USB_USB_CTRL_ADDR = 0x0000;
  static const int USB_USB_ADDR_ADDR = 0x0004;
  static const int USB_USB_PWR_ADDR = 0x0008;
  static const int USB_USB_TXFIFO_ADDR = 0x0010;
  static const int USB_USB_RXFIFO_ADDR = 0x0014;
  static const int USB_USB_TXIE_ADDR = 0x0018;
  static const int USB_USB_RXIE_ADDR = 0x001C;
  static const int USB_USB_IS_ADDR = 0x0020;
  static const int USB_USB_IM_ADDR = 0x0024;
  static const int USB_USB_IE_ADDR = 0x0028;
  static const int USB_USB_REVO_ADDR = 0x002C;
  static const int USB_USB_EP_ADDR = 0x0030;
  static const int USB_USB_BUFF_ADDR = 0x0034;
  static const int USB_USB_MPS_ADDR = 0x0038;
  // PIO0
  static const int PIO0_BASE = 0x50200000;
  static const int PIO0_CTRL_ADDR = 0x0000;
  static const int PIO0_FSTAT_ADDR = 0x0004;
  static const int PIO0_FDEBUG_ADDR = 0x0008;
  static const int PIO0_FCTRL_ADDR = 0x000C;
  static const int PIO0_RXF0_ADDR = 0x0010;
  static const int PIO0_RXF1_ADDR = 0x0014;
  static const int PIO0_RXF2_ADDR = 0x0018;
  static const int PIO0_RXF3_ADDR = 0x001C;
  static const int PIO0_TXF0_ADDR = 0x0020;
  static const int PIO0_TXF1_ADDR = 0x0024;
  static const int PIO0_TXF2_ADDR = 0x0028;
  static const int PIO0_TXF3_ADDR = 0x002C;
  static const int PIO0_IRQ_ADDR = 0x0030;
  static const int PIO0_IRQ_FORCE_ADDR = 0x0034;
  static const int PIO0_IRQ_INTF_ADDR = 0x0038;
  static const int PIO0_IRQ_INTS_ADDR = 0x003C;
  static const int PIO0_SM0_CLKDIV_ADDR = 0x00C8;
  static const int PIO0_SM0_EXECCTRL_ADDR = 0x00CC;
  static const int PIO0_SM0_SHIFTCTRL_ADDR = 0x00D0;
  static const int PIO0_SM0_ADDR_ADDR = 0x00D4;
  static const int PIO0_SM0_INSTR_ADDR = 0x00D8;
  static const int PIO0_SM0_PINCTRL_ADDR = 0x00DC;
  // PIO1
  static const int PIO1_BASE = 0x50201000;
  static const int PIO1_CTRL_ADDR = 0x0000;
  static const int PIO1_FSTAT_ADDR = 0x0004;
  static const int PIO1_IRQ_ADDR = 0x0030;
  static const int PIO1_SM0_CLKDIV_ADDR = 0x00C8;
  static const int PIO1_SM0_EXECCTRL_ADDR = 0x00CC;
  static const int PIO1_SM0_SHIFTCTRL_ADDR = 0x00D0;
  static const int PIO1_SM0_ADDR_ADDR = 0x00D4;
  static const int PIO1_SM0_INSTR_ADDR = 0x00D8;
  // Clock Manager
  static const int CLOCKS_BASE = 0x40008000;
  static const int CLOCKS_CLK_GP0DIV_ADDR = 0x0000;
  static const int CLOCKS_CLK_GP0CTRL_ADDR = 0x0004;
  static const int CLOCKS_CLK_GP1DIV_ADDR = 0x0008;
  static const int CLOCKS_CLK_GP1CTRL_ADDR = 0x000C;
  static const int CLOCKS_CLK_GP2DIV_ADDR = 0x0010;
  static const int CLOCKS_CLK_GP2CTRL_ADDR = 0x0014;
  static const int CLOCKS_CLK_REF_ADDR = 0x001C;
  static const int CLOCKS_CLK_SYS_ADDR = 0x0020;
  static const int CLOCKS_CLK_PERI_ADDR = 0x0024;
  // Crystal Oscillator
  static const int XOSC_BASE = 0x40020000;
  static const int XOSC_XOSC_CTRL_ADDR = 0x0000;
  static const int XOSC_XOSC_STATUS_ADDR = 0x0004;
  static const int XOSC_XOSC_COUNT_ADDR = 0x0008;
  // Ring Oscillator
  static const int ROSC_BASE = 0x40010000;
  static const int ROSC_ROSC_CTRL_ADDR = 0x0000;
  static const int ROSC_ROSC_FREQA_ADDR = 0x0004;
  static const int ROSC_ROSC_FREQB_ADDR = 0x0008;
  static const int ROSC_ROSC_FREQC_ADDR = 0x000C;
  static const int ROSC_ROSC_FREQD_ADDR = 0x0010;
  static const int ROSC_ROSC_STATUS_ADDR = 0x0014;
  static const int ROSC_ROSC_DR_ADDR = 0x0018;
  // System PLL
  static const int PLL_SYS_BASE = 0x40028000;
  static const int PLL_SYS_PLL_CS_ADDR = 0x0000;
  static const int PLL_SYS_PLL_PWR_ADDR = 0x0004;
  static const int PLL_SYS_PLL_FBDIV_ADDR = 0x0008;
  static const int PLL_SYS_PLL_PRIMARY_ADDR = 0x000C;
  static const int PLL_SYS_PLL_POSTDIV1_ADDR = 0x0010;
  static const int PLL_SYS_PLL_POSTDIV2_ADDR = 0x0014;
  // USB PLL
  static const int PLL_USB_BASE = 0x4002C000;
  static const int PLL_USB_PLL_CS_ADDR = 0x0000;
  static const int PLL_USB_PLL_PWR_ADDR = 0x0004;
  static const int PLL_USB_PLL_FBDIV_ADDR = 0x0008;
  static const int PLL_USB_PLL_PRIMARY_ADDR = 0x000C;
  // Resets
  static const int RESETS_BASE = 0x4000C000;
  static const int RESETS_RESET_ADDR = 0x0000;
  static const int RESETS_RESET_DONE_ADDR = 0x0004;
  static const int RESETS_WD_RESET_ADDR = 0x0008;

  // 中断向量定义
  static const int INT_RESERVED = 0;  // Reserved
  static const int INT_TIMER0_IRQ_0 = 1;  // Timer 0 IRQ 0
  static const int INT_TIMER0_IRQ_1 = 2;  // Timer 0 IRQ 1
  static const int INT_TIMER1_IRQ_0 = 3;  // Timer 1 IRQ 0
  static const int INT_TIMER1_IRQ_1 = 4;  // Timer 1 IRQ 1
  static const int INT_TIMER2_IRQ_0 = 5;  // Timer 2 IRQ 0
  static const int INT_TIMER2_IRQ_1 = 6;  // Timer 2 IRQ 1
  static const int INT_TIMER3_IRQ_0 = 7;  // Timer 3 IRQ 0
  static const int INT_TIMER3_IRQ_1 = 8;  // Timer 3 IRQ 1
  static const int INT_PWM_IRQ_WRAP = 9;  // PWM IRQ wrap
  static const int INT_USB_CTRL_IRQ = 10;  // USB ctrl IRQ
  static const int INT_USB_DMA_IRQ = 11;  // USB dma IRQ
  static const int INT_USB_VBUS_DETECT = 12;  // USB VBUS detect IRQ
  static const int INT_USB_RESUME_IRQ = 13;  // USB resume IRQ
  static const int INT_ADC_IRQ_FIFO = 14;  // ADC IRQ FIFO
  static const int INT_ADC_IRQ_TRIGGER = 15;  // ADC IRQ trigger
  static const int INT_I2C0_IRQ = 16;  // I2C 0 IRQ
  static const int INT_I2C1_IRQ = 17;  // I2C 1 IRQ
  static const int INT_SPI0_IRQ = 18;  // SPI 0 IRQ
  static const int INT_SPI1_IRQ = 19;  // SPI 1 IRQ
  static const int INT_UART0_IRQ = 20;  // UART 0 IRQ
  static const int INT_UART0_IRQ_TX = 21;  // UART 0 IRQ TX
  static const int INT_UART1_IRQ = 22;  // UART 1 IRQ
  static const int INT_UART1_IRQ_TX = 23;  // UART 1 IRQ TX
  static const int INT_PIO0_IRQ_0 = 24;  // PIO 0 IRQ 0
  static const int INT_PIO0_IRQ_1 = 25;  // PIO 0 IRQ 1
  static const int INT_PIO1_IRQ_0 = 26;  // PIO 1 IRQ 0
  static const int INT_PIO1_IRQ_1 = 27;  // PIO 1 IRQ 1
  static const int INT_RTC_IRQ = 28;  // RTC IRQ

  // 引脚定义
  static const int PIN_GP0 = 1;  // UART0 TX / GP0
  static const int PIN_GP1 = 2;  // UART0 RX / GP1
  static const int PIN_GP2 = 3;  // SPI0 TX / GP2
  static const int PIN_GP3 = 4;  // SPI0 RX / GP3
  static const int PIN_GP4 = 5;  // SPI0 CSn / GP4
  static const int PIN_GP5 = 6;  // SPI0 SCK / GP5
  static const int PIN_GP6 = 7;  // PWM6 / GP6
  static const int PIN_GP7 = 8;  // PWM7 / GP7
  static const int PIN_GP8 = 9;  // PWM8 / GP8
  static const int PIN_GP9 = 10;  // PWM9 / GP9
  static const int PIN_GP10 = 11;  // SPI1 TX / GP10
  static const int PIN_GP11 = 12;  // SPI1 RX / GP11
  static const int PIN_GP12 = 13;  // SPI1 CSn / GP12
  static const int PIN_GP13 = 14;  // SPI1 SCK / GP13
  static const int PIN_GP14 = 15;  // PWM14 / GP14
  static const int PIN_GP15 = 16;  // PWM15 / GP15
  static const int PIN_GP16 = 17;  // UART1 TX / GP16
  static const int PIN_GP17 = 18;  // UART1 RX / GP17
  static const int PIN_GP18 = 19;  // I2C0 SDA / GP18
  static const int PIN_GP19 = 20;  // I2C0 SCL / GP19
  static const int PIN_GP20 = 21;  // I2C1 SDA / GP20
  static const int PIN_GP21 = 22;  // I2C1 SCL / GP21
  static const int PIN_GP22 = 23;  // GP22
  static const int PIN_RUN = 24;  // Run enable
  static const int PIN_AGND = 25;  // Analog ground
  static const int PIN_GP26 = 26;  // ADC0 / GP26
  static const int PIN_GP27 = 27;  // ADC1 / GP27
  static const int PIN_GP28 = 28;  // ADC2 / GP28
  static const int PIN_ADC_VREF = 29;  // ADC voltage reference
  static const int PIN_GP35 = 30;  // GP35
  static const int PIN_GP34 = 31;  // GP34
  static const int PIN_GP33 = 32;  // GP33
  static const int PIN_GP36 = 37;  // GP36
  static const int PIN_GP37 = 38;  // GP37
  static const int PIN_GP38 = 39;  // GP38
  static const int PIN_GP39 = 40;  // GP39
  static const int PIN_GP40 = 41;  // GP40
  static const int PIN_GP41 = 42;  // GP41
  static const int PIN_SWCLK = 43;  // SWD Clock
  static const int PIN_SWDIO = 44;  // SWD Data I/O

}

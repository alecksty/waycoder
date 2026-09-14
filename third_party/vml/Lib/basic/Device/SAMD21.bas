' SAMD21寄存器定义
' 生成自: Atmel (Microchip)/SAM D/SAMD21
' 版本: 
' 日期: 
' 作者: 
' 描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller

' CPU架构: ARM Cortex-M0+
' 位宽: 0位
' 时钟频率: 0 Hz

' 外设定义
' Power Manager
CONST PM_BASE = 
CONST PM_PM_CTRL = 0x40000400

' System Controller
CONST SYSCTRL_BASE = 
CONST SYSCTRL_SYSCTRL_INTENCLR = 0x40000800

' Generic Clock Generator
CONST GCLK_BASE = 
CONST GCLK_GCLK_CTRL = 0x40000C00

' Watchdog Timer
CONST WDT_BASE = 
CONST WDT_WDT_CTRL = 0x40001000

' Real-Time Clock
CONST RTC_BASE = 
CONST RTC_RTC_CTRL = 0x40001400

' External Interrupt Controller
CONST EIC_BASE = 
CONST EIC_EIC_CTRL = 0x40001800

' Serial Communication Interface 0
CONST SERCOM0_BASE = 
CONST SERCOM0_SERCOM0_I2CM_CTRLA = 0x42000800

' Analog-to-Digital Converter
CONST ADC_BASE = 
CONST ADC_ADC_CTRLA = 0x42002000

' Digital-to-Analog Converter
CONST DAC_BASE = 
CONST DAC_DAC_CTRLA = 0x42002400

' General Purpose I/O
CONST PORT_BASE = 
CONST PORT_PORT_DIR = 0x41004400

' Timer/Counter 0
CONST TC0_BASE = 
CONST TC0_TC0_CTRLA = 0x42002800

' USB Device Controller
CONST USB_BASE = 
CONST USB_USB_CTRLA = 0x41005000

' 中断向量定义
CONST RESET_VECTOR = 0  ' Reset vector
CONST NONMASKABLEINT_VECTOR = 1  ' Non-maskable interrupt
CONST HARDFAULT_VECTOR = 2  ' Hard fault
CONST SVCALL_VECTOR = 3  ' Supervisor call
CONST PENDSV_VECTOR = 4  ' Pendable service call
CONST SYSTICK_VECTOR = 5  ' System tick timer
CONST PM_VECTOR = 6  ' Power Manager
CONST SYSCTRL_VECTOR = 7  ' System Controller
CONST WDT_VECTOR = 8  ' Watchdog Timer
CONST RTC_VECTOR = 9  ' Real-Time Clock
CONST EIC_VECTOR = 10  ' External Interrupt Controller
CONST NVMCTRL_VECTOR = 11  ' Non-Volatile Memory Controller
CONST DMAC_VECTOR = 12  ' Direct Memory Access Controller
CONST USB_VECTOR = 13  ' USB Device Controller
CONST EVSYS_VECTOR = 14  ' Event System
CONST SERCOM0_VECTOR = 15  ' Serial Communication Interface 0
CONST SERCOM1_VECTOR = 16  ' Serial Communication Interface 1
CONST SERCOM2_VECTOR = 17  ' Serial Communication Interface 2
CONST SERCOM3_VECTOR = 18  ' Serial Communication Interface 3
CONST SERCOM4_VECTOR = 19  ' Serial Communication Interface 4
CONST SERCOM5_VECTOR = 20  ' Serial Communication Interface 5
CONST TCC0_VECTOR = 21  ' Timer/Counter for Control 0
CONST TCC1_VECTOR = 22  ' Timer/Counter for Control 1
CONST TCC2_VECTOR = 23  ' Timer/Counter for Control 2
CONST TC3_VECTOR = 24  ' Timer/Counter 3
CONST TC4_VECTOR = 25  ' Timer/Counter 4
CONST TC5_VECTOR = 26  ' Timer/Counter 5
CONST TC6_VECTOR = 27  ' Timer/Counter 6
CONST TC7_VECTOR = 28  ' Timer/Counter 7
CONST ADC_VECTOR = 29  ' Analog-to-Digital Converter
CONST AC_VECTOR = 30  ' Analog Comparator
CONST DAC_VECTOR = 31  ' Digital-to-Analog Converter
CONST PTC_VECTOR = 32  ' Peripheral Touch Controller
CONST I2S_VECTOR = 33  ' Inter-IC Sound Interface

' 设备初始化子程序
SUB samd21_init()
    ' 初始化代码
END SUB

' 常用函数
FUNCTION read_register(addr AS INTEGER) AS INTEGER
    ' 读取寄存器值
    RETURN PEEK(addr)
END FUNCTION

SUB write_register(addr AS INTEGER, value AS INTEGER)
    ' 写入寄存器值
    POKE addr, value
END SUB

\ SAMD21设备定义 - Forth文件
\ 生成自: Atmel (Microchip)/SAM D/SAMD21
\ 版本: 
\ 日期: 
\ 作者: 
\ 描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
\ CPU架构: ARM Cortex-M0+
\ 位宽: 0位
\ 时钟频率: 0 Hz

\ =========================================
\ SAMD21设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" SAMD21" ;
: MANUFACTURER  S" Atmel (Microchip)" ;
: FAMILY        S" SAM D" ;
: VERSION       S" " ;
: ARCHITECTURE  S" ARM Cortex-M0+" ;
0 CONSTANT BITS
0 CONSTANT CLOCK-FREQ

\ 外设定义
\ Power Manager
 CONSTANT PM-BASE
0x40000400 CONSTANT PM-PM_CTRL
\ System Controller
 CONSTANT SYSCTRL-BASE
0x40000800 CONSTANT SYSCTRL-SYSCTRL_INTENCLR
\ Generic Clock Generator
 CONSTANT GCLK-BASE
0x40000C00 CONSTANT GCLK-GCLK_CTRL
\ Watchdog Timer
 CONSTANT WDT-BASE
0x40001000 CONSTANT WDT-WDT_CTRL
\ Real-Time Clock
 CONSTANT RTC-BASE
0x40001400 CONSTANT RTC-RTC_CTRL
\ External Interrupt Controller
 CONSTANT EIC-BASE
0x40001800 CONSTANT EIC-EIC_CTRL
\ Serial Communication Interface 0
 CONSTANT SERCOM0-BASE
0x42000800 CONSTANT SERCOM0-SERCOM0_I2CM_CTRLA
\ Analog-to-Digital Converter
 CONSTANT ADC-BASE
0x42002000 CONSTANT ADC-ADC_CTRLA
\ Digital-to-Analog Converter
 CONSTANT DAC-BASE
0x42002400 CONSTANT DAC-DAC_CTRLA
\ General Purpose I/O
 CONSTANT PORT-BASE
0x41004400 CONSTANT PORT-PORT_DIR
\ Timer/Counter 0
 CONSTANT TC0-BASE
0x42002800 CONSTANT TC0-TC0_CTRLA
\ USB Device Controller
 CONSTANT USB-BASE
0x41005000 CONSTANT USB-USB_CTRLA

\ 中断向量定义
0 CONSTANT INT-RESET  \ Reset vector
1 CONSTANT INT-NONMASKABLEINT  \ Non-maskable interrupt
2 CONSTANT INT-HARDFAULT  \ Hard fault
3 CONSTANT INT-SVCALL  \ Supervisor call
4 CONSTANT INT-PENDSV  \ Pendable service call
5 CONSTANT INT-SYSTICK  \ System tick timer
6 CONSTANT INT-PM  \ Power Manager
7 CONSTANT INT-SYSCTRL  \ System Controller
8 CONSTANT INT-WDT  \ Watchdog Timer
9 CONSTANT INT-RTC  \ Real-Time Clock
10 CONSTANT INT-EIC  \ External Interrupt Controller
11 CONSTANT INT-NVMCTRL  \ Non-Volatile Memory Controller
12 CONSTANT INT-DMAC  \ Direct Memory Access Controller
13 CONSTANT INT-USB  \ USB Device Controller
14 CONSTANT INT-EVSYS  \ Event System
15 CONSTANT INT-SERCOM0  \ Serial Communication Interface 0
16 CONSTANT INT-SERCOM1  \ Serial Communication Interface 1
17 CONSTANT INT-SERCOM2  \ Serial Communication Interface 2
18 CONSTANT INT-SERCOM3  \ Serial Communication Interface 3
19 CONSTANT INT-SERCOM4  \ Serial Communication Interface 4
20 CONSTANT INT-SERCOM5  \ Serial Communication Interface 5
21 CONSTANT INT-TCC0  \ Timer/Counter for Control 0
22 CONSTANT INT-TCC1  \ Timer/Counter for Control 1
23 CONSTANT INT-TCC2  \ Timer/Counter for Control 2
24 CONSTANT INT-TC3  \ Timer/Counter 3
25 CONSTANT INT-TC4  \ Timer/Counter 4
26 CONSTANT INT-TC5  \ Timer/Counter 5
27 CONSTANT INT-TC6  \ Timer/Counter 6
28 CONSTANT INT-TC7  \ Timer/Counter 7
29 CONSTANT INT-ADC  \ Analog-to-Digital Converter
30 CONSTANT INT-AC  \ Analog Comparator
31 CONSTANT INT-DAC  \ Digital-to-Analog Converter
32 CONSTANT INT-PTC  \ Peripheral Touch Controller
33 CONSTANT INT-I2S  \ Inter-IC Sound Interface

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ PM外设
: PM-PM_CTRL@ ( -- n ) PM-PM_CTRL XL@ ;
: PM-PM_CTRL! ( n -- ) PM-PM_CTRL XL! ;

\ SYSCTRL外设
: SYSCTRL-SYSCTRL_INTENCLR@ ( -- n ) SYSCTRL-SYSCTRL_INTENCLR  32 CHARS@ ;
: SYSCTRL-SYSCTRL_INTENCLR! ( n -- ) SYSCTRL-SYSCTRL_INTENCLR  32 CHARS! ;

\ GCLK外设
: GCLK-GCLK_CTRL@ ( -- n ) GCLK-GCLK_CTRL XL@ ;
: GCLK-GCLK_CTRL! ( n -- ) GCLK-GCLK_CTRL XL! ;

\ WDT外设
: WDT-WDT_CTRL@ ( -- n ) WDT-WDT_CTRL XL@ ;
: WDT-WDT_CTRL! ( n -- ) WDT-WDT_CTRL XL! ;

\ RTC外设
: RTC-RTC_CTRL@ ( -- n ) RTC-RTC_CTRL  16 CHARS@ ;
: RTC-RTC_CTRL! ( n -- ) RTC-RTC_CTRL  16 CHARS! ;

\ EIC外设
: EIC-EIC_CTRL@ ( -- n ) EIC-EIC_CTRL XL@ ;
: EIC-EIC_CTRL! ( n -- ) EIC-EIC_CTRL XL! ;

\ SERCOM0外设
: SERCOM0-SERCOM0_I2CM_CTRLA@ ( -- n ) SERCOM0-SERCOM0_I2CM_CTRLA  32 CHARS@ ;
: SERCOM0-SERCOM0_I2CM_CTRLA! ( n -- ) SERCOM0-SERCOM0_I2CM_CTRLA  32 CHARS! ;

\ ADC外设
: ADC-ADC_CTRLA@ ( -- n ) ADC-ADC_CTRLA XL@ ;
: ADC-ADC_CTRLA! ( n -- ) ADC-ADC_CTRLA XL! ;

\ DAC外设
: DAC-DAC_CTRLA@ ( -- n ) DAC-DAC_CTRLA XL@ ;
: DAC-DAC_CTRLA! ( n -- ) DAC-DAC_CTRLA XL! ;

\ PORT外设
: PORT-PORT_DIR@ ( -- n ) PORT-PORT_DIR  32 CHARS@ ;
: PORT-PORT_DIR! ( n -- ) PORT-PORT_DIR  32 CHARS! ;

\ TC0外设
: TC0-TC0_CTRLA@ ( -- n ) TC0-TC0_CTRLA  16 CHARS@ ;
: TC0-TC0_CTRLA! ( n -- ) TC0-TC0_CTRLA  16 CHARS! ;

\ USB外设
: USB-USB_CTRLA@ ( -- n ) USB-USB_CTRLA XL@ ;
: USB-USB_CTRLA! ( n -- ) USB-USB_CTRLA XL! ;

\ =========================================
\ 设备初始化
\ =========================================

: SAMD21-INIT ( -- )
  \ 初始化SAMD21设备
  ." 初始化SAMD21..." CR


  \ 初始化外设
  \ 初始化PM
  0 PM-PM_CTRL!  \ PM_CTRL寄存器
  \ 初始化SYSCTRL
  0 SYSCTRL-SYSCTRL_INTENCLR!  \ SYSCTRL_INTENCLR寄存器
  \ 初始化GCLK
  0 GCLK-GCLK_CTRL!  \ GCLK_CTRL寄存器
  \ 初始化WDT
  0 WDT-WDT_CTRL!  \ WDT_CTRL寄存器
  \ 初始化RTC
  0 RTC-RTC_CTRL!  \ RTC_CTRL寄存器
  \ 初始化EIC
  0 EIC-EIC_CTRL!  \ EIC_CTRL寄存器
  \ 初始化SERCOM0
  0 SERCOM0-SERCOM0_I2CM_CTRLA!  \ SERCOM0_I2CM_CTRLA寄存器
  \ 初始化ADC
  0 ADC-ADC_CTRLA!  \ ADC_CTRLA寄存器
  \ 初始化DAC
  0 DAC-DAC_CTRLA!  \ DAC_CTRLA寄存器
  \ 初始化PORT
  0 PORT-PORT_DIR!  \ PORT_DIR寄存器
  \ 初始化TC0
  0 TC0-TC0_CTRLA!  \ TC0_CTRLA寄存器
  \ 初始化USB
  0 USB-USB_CTRLA!  \ USB_CTRLA寄存器

  ." SAMD21初始化完成" CR
;

\ =========================================
\ 设备信息显示
\ =========================================

: .DEVICE-INFO ( -- )
  CR
  ." 设备: " DEVICE-NAME TYPE CR
  ." 厂商: " MANUFACTURER TYPE CR
  ." 系列: " FAMILY TYPE CR
  ." 版本: " VERSION TYPE CR
  ." 架构: " ARCHITECTURE TYPE CR
  ." 位宽: " BITS . CR
  ." 时钟: " CLOCK-FREQ . ." Hz" CR
;

\ =========================================
\ 中断处理
\ =========================================

\ Reset vector
: INT-RESET-HANDLER ( -- )
  ." Reset中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET-ENABLE ( -- )
  INT-RESET INT-ENABLE
;

: INT-RESET-DISABLE ( -- )
  INT-RESET INT-DISABLE
;

\ Non-maskable interrupt
: INT-NONMASKABLEINT-HANDLER ( -- )
  ." NonMaskableInt中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-NONMASKABLEINT-ENABLE ( -- )
  INT-NONMASKABLEINT INT-ENABLE
;

: INT-NONMASKABLEINT-DISABLE ( -- )
  INT-NONMASKABLEINT INT-DISABLE
;

\ Hard fault
: INT-HARDFAULT-HANDLER ( -- )
  ." HardFault中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-HARDFAULT-ENABLE ( -- )
  INT-HARDFAULT INT-ENABLE
;

: INT-HARDFAULT-DISABLE ( -- )
  INT-HARDFAULT INT-DISABLE
;

\ Supervisor call
: INT-SVCALL-HANDLER ( -- )
  ." SVCall中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SVCALL-ENABLE ( -- )
  INT-SVCALL INT-ENABLE
;

: INT-SVCALL-DISABLE ( -- )
  INT-SVCALL INT-DISABLE
;

\ Pendable service call
: INT-PENDSV-HANDLER ( -- )
  ." PendSV中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PENDSV-ENABLE ( -- )
  INT-PENDSV INT-ENABLE
;

: INT-PENDSV-DISABLE ( -- )
  INT-PENDSV INT-DISABLE
;

\ System tick timer
: INT-SYSTICK-HANDLER ( -- )
  ." SysTick中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SYSTICK-ENABLE ( -- )
  INT-SYSTICK INT-ENABLE
;

: INT-SYSTICK-DISABLE ( -- )
  INT-SYSTICK INT-DISABLE
;

\ Power Manager
: INT-PM-HANDLER ( -- )
  ." PM中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PM-ENABLE ( -- )
  INT-PM INT-ENABLE
;

: INT-PM-DISABLE ( -- )
  INT-PM INT-DISABLE
;

\ System Controller
: INT-SYSCTRL-HANDLER ( -- )
  ." SYSCTRL中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SYSCTRL-ENABLE ( -- )
  INT-SYSCTRL INT-ENABLE
;

: INT-SYSCTRL-DISABLE ( -- )
  INT-SYSCTRL INT-DISABLE
;

\ Watchdog Timer
: INT-WDT-HANDLER ( -- )
  ." WDT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-WDT-ENABLE ( -- )
  INT-WDT INT-ENABLE
;

: INT-WDT-DISABLE ( -- )
  INT-WDT INT-DISABLE
;

\ Real-Time Clock
: INT-RTC-HANDLER ( -- )
  ." RTC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RTC-ENABLE ( -- )
  INT-RTC INT-ENABLE
;

: INT-RTC-DISABLE ( -- )
  INT-RTC INT-DISABLE
;

\ External Interrupt Controller
: INT-EIC-HANDLER ( -- )
  ." EIC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EIC-ENABLE ( -- )
  INT-EIC INT-ENABLE
;

: INT-EIC-DISABLE ( -- )
  INT-EIC INT-DISABLE
;

\ Non-Volatile Memory Controller
: INT-NVMCTRL-HANDLER ( -- )
  ." NVMCTRL中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-NVMCTRL-ENABLE ( -- )
  INT-NVMCTRL INT-ENABLE
;

: INT-NVMCTRL-DISABLE ( -- )
  INT-NVMCTRL INT-DISABLE
;

\ Direct Memory Access Controller
: INT-DMAC-HANDLER ( -- )
  ." DMAC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DMAC-ENABLE ( -- )
  INT-DMAC INT-ENABLE
;

: INT-DMAC-DISABLE ( -- )
  INT-DMAC INT-DISABLE
;

\ USB Device Controller
: INT-USB-HANDLER ( -- )
  ." USB中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USB-ENABLE ( -- )
  INT-USB INT-ENABLE
;

: INT-USB-DISABLE ( -- )
  INT-USB INT-DISABLE
;

\ Event System
: INT-EVSYS-HANDLER ( -- )
  ." EVSYS中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EVSYS-ENABLE ( -- )
  INT-EVSYS INT-ENABLE
;

: INT-EVSYS-DISABLE ( -- )
  INT-EVSYS INT-DISABLE
;

\ Serial Communication Interface 0
: INT-SERCOM0-HANDLER ( -- )
  ." SERCOM0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SERCOM0-ENABLE ( -- )
  INT-SERCOM0 INT-ENABLE
;

: INT-SERCOM0-DISABLE ( -- )
  INT-SERCOM0 INT-DISABLE
;

\ Serial Communication Interface 1
: INT-SERCOM1-HANDLER ( -- )
  ." SERCOM1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SERCOM1-ENABLE ( -- )
  INT-SERCOM1 INT-ENABLE
;

: INT-SERCOM1-DISABLE ( -- )
  INT-SERCOM1 INT-DISABLE
;

\ Serial Communication Interface 2
: INT-SERCOM2-HANDLER ( -- )
  ." SERCOM2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SERCOM2-ENABLE ( -- )
  INT-SERCOM2 INT-ENABLE
;

: INT-SERCOM2-DISABLE ( -- )
  INT-SERCOM2 INT-DISABLE
;

\ Serial Communication Interface 3
: INT-SERCOM3-HANDLER ( -- )
  ." SERCOM3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SERCOM3-ENABLE ( -- )
  INT-SERCOM3 INT-ENABLE
;

: INT-SERCOM3-DISABLE ( -- )
  INT-SERCOM3 INT-DISABLE
;

\ Serial Communication Interface 4
: INT-SERCOM4-HANDLER ( -- )
  ." SERCOM4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SERCOM4-ENABLE ( -- )
  INT-SERCOM4 INT-ENABLE
;

: INT-SERCOM4-DISABLE ( -- )
  INT-SERCOM4 INT-DISABLE
;

\ Serial Communication Interface 5
: INT-SERCOM5-HANDLER ( -- )
  ." SERCOM5中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SERCOM5-ENABLE ( -- )
  INT-SERCOM5 INT-ENABLE
;

: INT-SERCOM5-DISABLE ( -- )
  INT-SERCOM5 INT-DISABLE
;

\ Timer/Counter for Control 0
: INT-TCC0-HANDLER ( -- )
  ." TCC0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TCC0-ENABLE ( -- )
  INT-TCC0 INT-ENABLE
;

: INT-TCC0-DISABLE ( -- )
  INT-TCC0 INT-DISABLE
;

\ Timer/Counter for Control 1
: INT-TCC1-HANDLER ( -- )
  ." TCC1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TCC1-ENABLE ( -- )
  INT-TCC1 INT-ENABLE
;

: INT-TCC1-DISABLE ( -- )
  INT-TCC1 INT-DISABLE
;

\ Timer/Counter for Control 2
: INT-TCC2-HANDLER ( -- )
  ." TCC2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TCC2-ENABLE ( -- )
  INT-TCC2 INT-ENABLE
;

: INT-TCC2-DISABLE ( -- )
  INT-TCC2 INT-DISABLE
;

\ Timer/Counter 3
: INT-TC3-HANDLER ( -- )
  ." TC3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TC3-ENABLE ( -- )
  INT-TC3 INT-ENABLE
;

: INT-TC3-DISABLE ( -- )
  INT-TC3 INT-DISABLE
;

\ Timer/Counter 4
: INT-TC4-HANDLER ( -- )
  ." TC4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TC4-ENABLE ( -- )
  INT-TC4 INT-ENABLE
;

: INT-TC4-DISABLE ( -- )
  INT-TC4 INT-DISABLE
;

\ Timer/Counter 5
: INT-TC5-HANDLER ( -- )
  ." TC5中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TC5-ENABLE ( -- )
  INT-TC5 INT-ENABLE
;

: INT-TC5-DISABLE ( -- )
  INT-TC5 INT-DISABLE
;

\ Timer/Counter 6
: INT-TC6-HANDLER ( -- )
  ." TC6中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TC6-ENABLE ( -- )
  INT-TC6 INT-ENABLE
;

: INT-TC6-DISABLE ( -- )
  INT-TC6 INT-DISABLE
;

\ Timer/Counter 7
: INT-TC7-HANDLER ( -- )
  ." TC7中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-TC7-ENABLE ( -- )
  INT-TC7 INT-ENABLE
;

: INT-TC7-DISABLE ( -- )
  INT-TC7 INT-DISABLE
;

\ Analog-to-Digital Converter
: INT-ADC-HANDLER ( -- )
  ." ADC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-ADC-ENABLE ( -- )
  INT-ADC INT-ENABLE
;

: INT-ADC-DISABLE ( -- )
  INT-ADC INT-DISABLE
;

\ Analog Comparator
: INT-AC-HANDLER ( -- )
  ." AC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-AC-ENABLE ( -- )
  INT-AC INT-ENABLE
;

: INT-AC-DISABLE ( -- )
  INT-AC INT-DISABLE
;

\ Digital-to-Analog Converter
: INT-DAC-HANDLER ( -- )
  ." DAC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-DAC-ENABLE ( -- )
  INT-DAC INT-ENABLE
;

: INT-DAC-DISABLE ( -- )
  INT-DAC INT-DISABLE
;

\ Peripheral Touch Controller
: INT-PTC-HANDLER ( -- )
  ." PTC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-PTC-ENABLE ( -- )
  INT-PTC INT-ENABLE
;

: INT-PTC-DISABLE ( -- )
  INT-PTC INT-DISABLE
;

\ Inter-IC Sound Interface
: INT-I2S-HANDLER ( -- )
  ." I2S中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-I2S-ENABLE ( -- )
  INT-I2S INT-ENABLE
;

: INT-I2S-DISABLE ( -- )
  INT-I2S INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  SAMD21-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

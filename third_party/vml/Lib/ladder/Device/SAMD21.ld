(*
 * SAMD21梯形图定义
 * 生成自: Atmel (Microchip)/SAM D/SAMD21
 * 版本: 
 * 日期: 
 * 作者: 
 * 描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
 * CPU架构: ARM Cortex-M0+
 * 位宽: 0位
 * 时钟频率: 0 Hz
 *)

PROGRAM SAMD21
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* Power Manager *)
    PM_BASE AT %MW : WORD;
    PM_PM_CTRL AT %MW0x40000400 : LWORD;
    (* System Controller *)
    SYSCTRL_BASE AT %MW : WORD;
    SYSCTRL_SYSCTRL_INTENCLR AT %MW0x40000800 : ARRAY[0..31] OF BOOL;
    (* Generic Clock Generator *)
    GCLK_BASE AT %MW : WORD;
    GCLK_GCLK_CTRL AT %MW0x40000C00 : LWORD;
    (* Watchdog Timer *)
    WDT_BASE AT %MW : WORD;
    WDT_WDT_CTRL AT %MW0x40001000 : LWORD;
    (* Real-Time Clock *)
    RTC_BASE AT %MW : WORD;
    RTC_RTC_CTRL AT %MW0x40001400 : ARRAY[0..15] OF BOOL;
    (* External Interrupt Controller *)
    EIC_BASE AT %MW : WORD;
    EIC_EIC_CTRL AT %MW0x40001800 : LWORD;
    (* Serial Communication Interface 0 *)
    SERCOM0_BASE AT %MW : WORD;
    SERCOM0_SERCOM0_I2CM_CTRLA AT %MW0x42000800 : ARRAY[0..31] OF BOOL;
    (* Analog-to-Digital Converter *)
    ADC_BASE AT %MW : WORD;
    ADC_ADC_CTRLA AT %MW0x42002000 : LWORD;
    (* Digital-to-Analog Converter *)
    DAC_BASE AT %MW : WORD;
    DAC_DAC_CTRLA AT %MW0x42002400 : LWORD;
    (* General Purpose I/O *)
    PORT_BASE AT %MW : WORD;
    PORT_PORT_DIR AT %MW0x41004400 : ARRAY[0..31] OF BOOL;
    (* Timer/Counter 0 *)
    TC0_BASE AT %MW : WORD;
    TC0_TC0_CTRLA AT %MW0x42002800 : ARRAY[0..15] OF BOOL;
    (* USB Device Controller *)
    USB_BASE AT %MW : WORD;
    USB_USB_CTRLA AT %MW0x41005000 : LWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Reset vector *)
    INT_NONMASKABLEINT : BOOL; (* Non-maskable interrupt *)
    INT_HARDFAULT : BOOL; (* Hard fault *)
    INT_SVCALL : BOOL; (* Supervisor call *)
    INT_PENDSV : BOOL; (* Pendable service call *)
    INT_SYSTICK : BOOL; (* System tick timer *)
    INT_PM : BOOL; (* Power Manager *)
    INT_SYSCTRL : BOOL; (* System Controller *)
    INT_WDT : BOOL; (* Watchdog Timer *)
    INT_RTC : BOOL; (* Real-Time Clock *)
    INT_EIC : BOOL; (* External Interrupt Controller *)
    INT_NVMCTRL : BOOL; (* Non-Volatile Memory Controller *)
    INT_DMAC : BOOL; (* Direct Memory Access Controller *)
    INT_USB : BOOL; (* USB Device Controller *)
    INT_EVSYS : BOOL; (* Event System *)
    INT_SERCOM0 : BOOL; (* Serial Communication Interface 0 *)
    INT_SERCOM1 : BOOL; (* Serial Communication Interface 1 *)
    INT_SERCOM2 : BOOL; (* Serial Communication Interface 2 *)
    INT_SERCOM3 : BOOL; (* Serial Communication Interface 3 *)
    INT_SERCOM4 : BOOL; (* Serial Communication Interface 4 *)
    INT_SERCOM5 : BOOL; (* Serial Communication Interface 5 *)
    INT_TCC0 : BOOL; (* Timer/Counter for Control 0 *)
    INT_TCC1 : BOOL; (* Timer/Counter for Control 1 *)
    INT_TCC2 : BOOL; (* Timer/Counter for Control 2 *)
    INT_TC3 : BOOL; (* Timer/Counter 3 *)
    INT_TC4 : BOOL; (* Timer/Counter 4 *)
    INT_TC5 : BOOL; (* Timer/Counter 5 *)
    INT_TC6 : BOOL; (* Timer/Counter 6 *)
    INT_TC7 : BOOL; (* Timer/Counter 7 *)
    INT_ADC : BOOL; (* Analog-to-Digital Converter *)
    INT_AC : BOOL; (* Analog Comparator *)
    INT_DAC : BOOL; (* Digital-to-Analog Converter *)
    INT_PTC : BOOL; (* Peripheral Touch Controller *)
    INT_I2S : BOOL; (* Inter-IC Sound Interface *)

END_VAR

BEGIN
    (* SAMD21初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_NONMASKABLEINT|--[中断处理]--
    --|INT_HARDFAULT|--[中断处理]--
    --|INT_SVCALL|--[中断处理]--
    --|INT_PENDSV|--[中断处理]--
    --|INT_SYSTICK|--[中断处理]--
    --|INT_PM|--[中断处理]--
    --|INT_SYSCTRL|--[中断处理]--
    --|INT_WDT|--[中断处理]--
    --|INT_RTC|--[中断处理]--
    --|INT_EIC|--[中断处理]--
    --|INT_NVMCTRL|--[中断处理]--
    --|INT_DMAC|--[中断处理]--
    --|INT_USB|--[中断处理]--
    --|INT_EVSYS|--[中断处理]--
    --|INT_SERCOM0|--[中断处理]--
    --|INT_SERCOM1|--[中断处理]--
    --|INT_SERCOM2|--[中断处理]--
    --|INT_SERCOM3|--[中断处理]--
    --|INT_SERCOM4|--[中断处理]--
    --|INT_SERCOM5|--[中断处理]--
    --|INT_TCC0|--[中断处理]--
    --|INT_TCC1|--[中断处理]--
    --|INT_TCC2|--[中断处理]--
    --|INT_TC3|--[中断处理]--
    --|INT_TC4|--[中断处理]--
    --|INT_TC5|--[中断处理]--
    --|INT_TC6|--[中断处理]--
    --|INT_TC7|--[中断处理]--
    --|INT_ADC|--[中断处理]--
    --|INT_AC|--[中断处理]--
    --|INT_DAC|--[中断处理]--
    --|INT_PTC|--[中断处理]--
    --|INT_I2S|--[中断处理]--

END_PROGRAM

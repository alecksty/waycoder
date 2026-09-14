\ LPC54606设备定义 - Forth文件
\ 生成自: NXP/LPC/LPC54606
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 136KB SRAM, 180MHz
\ CPU架构: ARM-Cortex-M4
\ 位宽: 32位
\ 时钟频率: 180000000 Hz

\ =========================================
\ LPC54606设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" LPC54606" ;
: MANUFACTURER  S" NXP" ;
: FAMILY        S" LPC" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M4" ;
32 CONSTANT BITS
180000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT R0  \ 
0x04 CONSTANT R1  \ 
0x08 CONSTANT R2  \ 
0x0C CONSTANT R3  \ 
0x10 CONSTANT R4  \ 
0x14 CONSTANT R5  \ 
0x34 CONSTANT SP  \ 
0x38 CONSTANT LR  \ 
0x3C CONSTANT PC  \ 

\ 内存段定义
0x00000000 CONSTANT FLASH-START
0x0003FFFF CONSTANT FLASH-END
262144 CONSTANT FLASH-SIZE  \ 
0x20000000 CONSTANT SRAM-START
0x20021FFF CONSTANT SRAM-END
139264 CONSTANT SRAM-SIZE  \ 
0x40000000 CONSTANT PERIPHERAL-START
0x401FFFFF CONSTANT PERIPHERAL-END
2097152 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ System Control
0x40000000 CONSTANT SYSCON-BASE
0x80 CONSTANT SYSCON-SYSAHBCLKCTRL
0x04 CONSTANT SYSCON-MAINCLKSEL
0x08 CONSTANT SYSCON-MAINCLKUEN
0x0C CONSTANT SYSCON-SYSPLLCTRL
\ General Purpose I/O
0x400F4000 CONSTANT GPIO-BASE
0x0000 CONSTANT GPIO-DIR0
0x1000 CONSTANT GPIO-PIN0
0x2000 CONSTANT GPIO-SET0
0x3000 CONSTANT GPIO-CLR0
0x4000 CONSTANT GPIO-NOT0
0x0004 CONSTANT GPIO-DIR1
0x1004 CONSTANT GPIO-PIN1
0x2004 CONSTANT GPIO-SET1
0x3004 CONSTANT GPIO-CLR1
0x4004 CONSTANT GPIO-NOT1
\ USART0
0x40086000 CONSTANT USART0-BASE
0x00 CONSTANT USART0-CFG
0x04 CONSTANT USART0-CTRL
0x08 CONSTANT USART0-STAT
0x10 CONSTANT USART0-TXDAT
0x14 CONSTANT USART0-RXDAT
0x20 CONSTANT USART0-BRG

\ 中断向量定义
0 CONSTANT INT-RESET  \ 
11 CONSTANT INT-SVCALL  \ 
24 CONSTANT INT-USART0  \ USART0 Interrupt

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: R0@ ( -- n ) R0 L@ ;
: R0! ( n -- ) R0 L! ;

: R1@ ( -- n ) R1 L@ ;
: R1! ( n -- ) R1 L! ;

: R2@ ( -- n ) R2 L@ ;
: R2! ( n -- ) R2 L! ;

: R3@ ( -- n ) R3 L@ ;
: R3! ( n -- ) R3 L! ;

: R4@ ( -- n ) R4 L@ ;
: R4! ( n -- ) R4 L! ;

: R5@ ( -- n ) R5 L@ ;
: R5! ( n -- ) R5 L! ;

: SP@ ( -- n ) SP L@ ;
: SP! ( n -- ) SP L! ;

: LR@ ( -- n ) LR L@ ;
: LR! ( n -- ) LR L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

\ 外设访问
\ SYSCON外设
: SYSCON-SYSAHBCLKCTRL@ ( -- n ) SYSCON-SYSAHBCLKCTRL L@ ;
: SYSCON-SYSAHBCLKCTRL! ( n -- ) SYSCON-SYSAHBCLKCTRL L! ;
: SYSCON-MAINCLKSEL@ ( -- n ) SYSCON-MAINCLKSEL L@ ;
: SYSCON-MAINCLKSEL! ( n -- ) SYSCON-MAINCLKSEL L! ;
: SYSCON-MAINCLKUEN@ ( -- n ) SYSCON-MAINCLKUEN L@ ;
: SYSCON-MAINCLKUEN! ( n -- ) SYSCON-MAINCLKUEN L! ;
: SYSCON-SYSPLLCTRL@ ( -- n ) SYSCON-SYSPLLCTRL L@ ;
: SYSCON-SYSPLLCTRL! ( n -- ) SYSCON-SYSPLLCTRL L! ;

\ GPIO外设
: GPIO-DIR0@ ( -- n ) GPIO-DIR0 L@ ;
: GPIO-DIR0! ( n -- ) GPIO-DIR0 L! ;
: GPIO-PIN0@ ( -- n ) GPIO-PIN0 L@ ;
: GPIO-PIN0! ( n -- ) GPIO-PIN0 L! ;
: GPIO-SET0@ ( -- n ) GPIO-SET0 L@ ;
: GPIO-SET0! ( n -- ) GPIO-SET0 L! ;
: GPIO-CLR0@ ( -- n ) GPIO-CLR0 L@ ;
: GPIO-CLR0! ( n -- ) GPIO-CLR0 L! ;
: GPIO-NOT0@ ( -- n ) GPIO-NOT0 L@ ;
: GPIO-NOT0! ( n -- ) GPIO-NOT0 L! ;
: GPIO-DIR1@ ( -- n ) GPIO-DIR1 L@ ;
: GPIO-DIR1! ( n -- ) GPIO-DIR1 L! ;
: GPIO-PIN1@ ( -- n ) GPIO-PIN1 L@ ;
: GPIO-PIN1! ( n -- ) GPIO-PIN1 L! ;
: GPIO-SET1@ ( -- n ) GPIO-SET1 L@ ;
: GPIO-SET1! ( n -- ) GPIO-SET1 L! ;
: GPIO-CLR1@ ( -- n ) GPIO-CLR1 L@ ;
: GPIO-CLR1! ( n -- ) GPIO-CLR1 L! ;
: GPIO-NOT1@ ( -- n ) GPIO-NOT1 L@ ;
: GPIO-NOT1! ( n -- ) GPIO-NOT1 L! ;

\ USART0外设
: USART0-CFG@ ( -- n ) USART0-CFG L@ ;
: USART0-CFG! ( n -- ) USART0-CFG L! ;
: USART0-CTRL@ ( -- n ) USART0-CTRL L@ ;
: USART0-CTRL! ( n -- ) USART0-CTRL L! ;
: USART0-STAT@ ( -- n ) USART0-STAT L@ ;
: USART0-STAT! ( n -- ) USART0-STAT L! ;
: USART0-TXDAT@ ( -- n ) USART0-TXDAT L@ ;
: USART0-TXDAT! ( n -- ) USART0-TXDAT L! ;
: USART0-RXDAT@ ( -- n ) USART0-RXDAT L@ ;
: USART0-RXDAT! ( n -- ) USART0-RXDAT L! ;
: USART0-BRG@ ( -- n ) USART0-BRG L@ ;
: USART0-BRG! ( n -- ) USART0-BRG L! ;

\ =========================================
\ 设备初始化
\ =========================================

: LPC54606-INIT ( -- )
  \ 初始化LPC54606设备
  ." 初始化LPC54606..." CR

  \ 初始化寄存器
  0 R0!  \ 
  0 R1!  \ 
  0 R2!  \ 
  0 R3!  \ 
  0 R4!  \ 
  0 R5!  \ 
  0 SP!  \ 
  0 LR!  \ 
  0 PC!  \ 

  \ 初始化外设
  \ 初始化SYSCON
  0 SYSCON-SYSAHBCLKCTRL!  \ SYSAHBCLKCTRL寄存器
  0 SYSCON-MAINCLKSEL!  \ MAINCLKSEL寄存器
  0 SYSCON-MAINCLKUEN!  \ MAINCLKUEN寄存器
  0 SYSCON-SYSPLLCTRL!  \ SYSPLLCTRL寄存器
  \ 初始化GPIO
  0 GPIO-DIR0!  \ DIR0寄存器
  0 GPIO-PIN0!  \ PIN0寄存器
  0 GPIO-SET0!  \ SET0寄存器
  0 GPIO-CLR0!  \ CLR0寄存器
  0 GPIO-NOT0!  \ NOT0寄存器
  0 GPIO-DIR1!  \ DIR1寄存器
  0 GPIO-PIN1!  \ PIN1寄存器
  0 GPIO-SET1!  \ SET1寄存器
  0 GPIO-CLR1!  \ CLR1寄存器
  0 GPIO-NOT1!  \ NOT1寄存器
  \ 初始化USART0
  0 USART0-CFG!  \ CFG寄存器
  0 USART0-CTRL!  \ CTRL寄存器
  0 USART0-STAT!  \ STAT寄存器
  0 USART0-TXDAT!  \ TXDAT寄存器
  0 USART0-RXDAT!  \ RXDAT寄存器
  0 USART0-BRG!  \ BRG寄存器

  ." LPC54606初始化完成" CR
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

: .REGISTERS ( -- )
  CR ." 寄存器状态:" CR
  ." ----------" CR
  R0@ R0 .R 8 .R SPACE ."  R0: " R0@ .
  R1@ R1 .R 8 .R SPACE ."  R1: " R1@ .
  R2@ R2 .R 8 .R SPACE ."  R2: " R2@ .
  R3@ R3 .R 8 .R SPACE ."  R3: " R3@ .
  R4@ R4 .R 8 .R SPACE ."  R4: " R4@ .
  R5@ R5 .R 8 .R SPACE ."  R5: " R5@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  LR@ LR .R 8 .R SPACE ."  LR: " LR@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ 
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

\ 
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

\ USART0 Interrupt
: INT-USART0-HANDLER ( -- )
  ." USART0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART0-ENABLE ( -- )
  INT-USART0 INT-ENABLE
;

: INT-USART0-DISABLE ( -- )
  INT-USART0 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  LPC54606-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

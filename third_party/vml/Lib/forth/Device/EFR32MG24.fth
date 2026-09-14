\ EFR32MG24设备定义 - Forth文件
\ 生成自: Silicon Labs/EFR32/EFR32MG24
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M33 MCU with 1536KB Flash, 256KB RAM, 78MHz, Zigbee/Thread/Matter
\ CPU架构: ARM-Cortex-M33
\ 位宽: 32位
\ 时钟频率: 78000000 Hz

\ =========================================
\ EFR32MG24设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" EFR32MG24" ;
: MANUFACTURER  S" Silicon Labs" ;
: FAMILY        S" EFR32" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M33" ;
32 CONSTANT BITS
78000000 CONSTANT CLOCK-FREQ

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
0x08000000 CONSTANT FLASH-START
0x0817FFFF CONSTANT FLASH-END
1572864 CONSTANT FLASH-SIZE  \ 
0x20000000 CONSTANT SRAM-START
0x2003FFFF CONSTANT SRAM-END
262144 CONSTANT SRAM-SIZE  \ 
0x40000000 CONSTANT PERIPHERAL-START
0x4007FFFF CONSTANT PERIPHERAL-END
524288 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ Clock Management Unit
0x40080000 CONSTANT CMU-BASE
0x00 CONSTANT CMU-CTRL
0x08 CONSTANT CMU-HFCORECLKCFG
0x10 CONSTANT CMU-HFPERCLKEN0
4 CONSTANT CMU-HFPERCLKEN0-GPIOEN  \ GPIO clock enable
12 CONSTANT CMU-HFPERCLKEN0-USART0EN  \ USART0 clock enable
13 CONSTANT CMU-HFPERCLKEN0-USART1EN  \ USART1 clock enable
0x20 CONSTANT CMU-LFBCLKEN0
\ GPIO Controller
0x40088000 CONSTANT GPIO-BASE
0x00 CONSTANT GPIO-PORT_A_CTRL
0x04 CONSTANT GPIO-PORT_B_CTRL
0x08 CONSTANT GPIO-PORT_C_CTRL
0x0C CONSTANT GPIO-PORT_D_CTRL
0x10 CONSTANT GPIO-MODEL
0x14 CONSTANT GPIO-MODEH
0x1C CONSTANT GPIO-DOUT
0x20 CONSTANT GPIO-DOUTSET
0x24 CONSTANT GPIO-DOUTCLR
0x28 CONSTANT GPIO-DOUTTGL
0x2C CONSTANT GPIO-DIN
\ GPIO Port A extended
0x40088400 CONSTANT GPIO_PA-BASE
0x00 CONSTANT GPIO_PA-PA_CFG
0x04 CONSTANT GPIO_PA-PA_PINOUT
\ GPIO Port B extended
0x40088800 CONSTANT GPIO_PB-BASE
0x00 CONSTANT GPIO_PB-PB_CFG
\ USART 0
0x40060000 CONSTANT USART0-BASE
0x00 CONSTANT USART0-CTRL
0x04 CONSTANT USART0-CMD
0x08 CONSTANT USART0-STATUS
0x0C CONSTANT USART0-RXDATA
0x10 CONSTANT USART0-TXDATA
0x14 CONSTANT USART0-CLKDIV

\ 中断向量定义
0 CONSTANT INT-RESET  \ 
11 CONSTANT INT-SVCALL  \ 
12 CONSTANT INT-USART0_RX  \ USART0 Receive Interrupt
13 CONSTANT INT-USART0_TX  \ USART0 Transmit Interrupt

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
\ CMU外设
: CMU-CTRL@ ( -- n ) CMU-CTRL L@ ;
: CMU-CTRL! ( n -- ) CMU-CTRL L! ;
: CMU-HFCORECLKCFG@ ( -- n ) CMU-HFCORECLKCFG L@ ;
: CMU-HFCORECLKCFG! ( n -- ) CMU-HFCORECLKCFG L! ;
: CMU-HFPERCLKEN0@ ( -- n ) CMU-HFPERCLKEN0 L@ ;
: CMU-HFPERCLKEN0! ( n -- ) CMU-HFPERCLKEN0 L! ;
: CMU-HFPERCLKEN0-GPIOEN@ ( -- flag ) CMU-HFPERCLKEN0@ 4 BIT@ ;
: CMU-HFPERCLKEN0-GPIOEN! ( flag -- ) CMU-HFPERCLKEN0@ 4 BIT! CMU-HFPERCLKEN0! ;
: CMU-HFPERCLKEN0-USART0EN@ ( -- flag ) CMU-HFPERCLKEN0@ 12 BIT@ ;
: CMU-HFPERCLKEN0-USART0EN! ( flag -- ) CMU-HFPERCLKEN0@ 12 BIT! CMU-HFPERCLKEN0! ;
: CMU-HFPERCLKEN0-USART1EN@ ( -- flag ) CMU-HFPERCLKEN0@ 13 BIT@ ;
: CMU-HFPERCLKEN0-USART1EN! ( flag -- ) CMU-HFPERCLKEN0@ 13 BIT! CMU-HFPERCLKEN0! ;
: CMU-LFBCLKEN0@ ( -- n ) CMU-LFBCLKEN0 L@ ;
: CMU-LFBCLKEN0! ( n -- ) CMU-LFBCLKEN0 L! ;

\ GPIO外设
: GPIO-PORT_A_CTRL@ ( -- n ) GPIO-PORT_A_CTRL L@ ;
: GPIO-PORT_A_CTRL! ( n -- ) GPIO-PORT_A_CTRL L! ;
: GPIO-PORT_B_CTRL@ ( -- n ) GPIO-PORT_B_CTRL L@ ;
: GPIO-PORT_B_CTRL! ( n -- ) GPIO-PORT_B_CTRL L! ;
: GPIO-PORT_C_CTRL@ ( -- n ) GPIO-PORT_C_CTRL L@ ;
: GPIO-PORT_C_CTRL! ( n -- ) GPIO-PORT_C_CTRL L! ;
: GPIO-PORT_D_CTRL@ ( -- n ) GPIO-PORT_D_CTRL L@ ;
: GPIO-PORT_D_CTRL! ( n -- ) GPIO-PORT_D_CTRL L! ;
: GPIO-MODEL@ ( -- n ) GPIO-MODEL L@ ;
: GPIO-MODEL! ( n -- ) GPIO-MODEL L! ;
: GPIO-MODEH@ ( -- n ) GPIO-MODEH L@ ;
: GPIO-MODEH! ( n -- ) GPIO-MODEH L! ;
: GPIO-DOUT@ ( -- n ) GPIO-DOUT L@ ;
: GPIO-DOUT! ( n -- ) GPIO-DOUT L! ;
: GPIO-DOUTSET@ ( -- n ) GPIO-DOUTSET L@ ;
: GPIO-DOUTSET! ( n -- ) GPIO-DOUTSET L! ;
: GPIO-DOUTCLR@ ( -- n ) GPIO-DOUTCLR L@ ;
: GPIO-DOUTCLR! ( n -- ) GPIO-DOUTCLR L! ;
: GPIO-DOUTTGL@ ( -- n ) GPIO-DOUTTGL L@ ;
: GPIO-DOUTTGL! ( n -- ) GPIO-DOUTTGL L! ;
: GPIO-DIN@ ( -- n ) GPIO-DIN L@ ;
: GPIO-DIN! ( n -- ) GPIO-DIN L! ;

\ GPIO_PA外设
: GPIO_PA-PA_CFG@ ( -- n ) GPIO_PA-PA_CFG L@ ;
: GPIO_PA-PA_CFG! ( n -- ) GPIO_PA-PA_CFG L! ;
: GPIO_PA-PA_PINOUT@ ( -- n ) GPIO_PA-PA_PINOUT L@ ;
: GPIO_PA-PA_PINOUT! ( n -- ) GPIO_PA-PA_PINOUT L! ;

\ GPIO_PB外设
: GPIO_PB-PB_CFG@ ( -- n ) GPIO_PB-PB_CFG L@ ;
: GPIO_PB-PB_CFG! ( n -- ) GPIO_PB-PB_CFG L! ;

\ USART0外设
: USART0-CTRL@ ( -- n ) USART0-CTRL L@ ;
: USART0-CTRL! ( n -- ) USART0-CTRL L! ;
: USART0-CMD@ ( -- n ) USART0-CMD L@ ;
: USART0-CMD! ( n -- ) USART0-CMD L! ;
: USART0-STATUS@ ( -- n ) USART0-STATUS L@ ;
: USART0-STATUS! ( n -- ) USART0-STATUS L! ;
: USART0-RXDATA@ ( -- n ) USART0-RXDATA L@ ;
: USART0-RXDATA! ( n -- ) USART0-RXDATA L! ;
: USART0-TXDATA@ ( -- n ) USART0-TXDATA L@ ;
: USART0-TXDATA! ( n -- ) USART0-TXDATA L! ;
: USART0-CLKDIV@ ( -- n ) USART0-CLKDIV L@ ;
: USART0-CLKDIV! ( n -- ) USART0-CLKDIV L! ;

\ =========================================
\ 设备初始化
\ =========================================

: EFR32MG24-INIT ( -- )
  \ 初始化EFR32MG24设备
  ." 初始化EFR32MG24..." CR

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
  \ 初始化CMU
  0 CMU-CTRL!  \ CTRL寄存器
  0 CMU-HFCORECLKCFG!  \ HFCORECLKCFG寄存器
  0 CMU-HFPERCLKEN0!  \ HFPERCLKEN0寄存器
  0 CMU-LFBCLKEN0!  \ LFBCLKEN0寄存器
  \ 初始化GPIO
  0 GPIO-PORT_A_CTRL!  \ PORT_A_CTRL寄存器
  0 GPIO-PORT_B_CTRL!  \ PORT_B_CTRL寄存器
  0 GPIO-PORT_C_CTRL!  \ PORT_C_CTRL寄存器
  0 GPIO-PORT_D_CTRL!  \ PORT_D_CTRL寄存器
  0 GPIO-MODEL!  \ MODEL寄存器
  0 GPIO-MODEH!  \ MODEH寄存器
  0 GPIO-DOUT!  \ DOUT寄存器
  0 GPIO-DOUTSET!  \ DOUTSET寄存器
  0 GPIO-DOUTCLR!  \ DOUTCLR寄存器
  0 GPIO-DOUTTGL!  \ DOUTTGL寄存器
  0 GPIO-DIN!  \ DIN寄存器
  \ 初始化GPIO_PA
  0 GPIO_PA-PA_CFG!  \ PA_CFG寄存器
  0 GPIO_PA-PA_PINOUT!  \ PA_PINOUT寄存器
  \ 初始化GPIO_PB
  0 GPIO_PB-PB_CFG!  \ PB_CFG寄存器
  \ 初始化USART0
  0 USART0-CTRL!  \ CTRL寄存器
  0 USART0-CMD!  \ CMD寄存器
  0 USART0-STATUS!  \ STATUS寄存器
  0 USART0-RXDATA!  \ RXDATA寄存器
  0 USART0-TXDATA!  \ TXDATA寄存器
  0 USART0-CLKDIV!  \ CLKDIV寄存器

  ." EFR32MG24初始化完成" CR
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

\ USART0 Receive Interrupt
: INT-USART0_RX-HANDLER ( -- )
  ." USART0_RX中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART0_RX-ENABLE ( -- )
  INT-USART0_RX INT-ENABLE
;

: INT-USART0_RX-DISABLE ( -- )
  INT-USART0_RX INT-DISABLE
;

\ USART0 Transmit Interrupt
: INT-USART0_TX-HANDLER ( -- )
  ." USART0_TX中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART0_TX-ENABLE ( -- )
  INT-USART0_TX INT-ENABLE
;

: INT-USART0_TX-DISABLE ( -- )
  INT-USART0_TX INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  EFR32MG24-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

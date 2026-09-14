\ CH32V003设备定义 - Forth文件
\ 生成自: WCH/CH32V0/CH32V003
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit RISC-V RV32EC MCU with 16KB Flash, 2KB RAM, 48MHz, ultra-low-cost
\ CPU架构: RISC-V
\ 位宽: 32位
\ 时钟频率: 48000000 Hz

\ =========================================
\ CH32V003设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" CH32V003" ;
: MANUFACTURER  S" WCH" ;
: FAMILY        S" CH32V0" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" RISC-V" ;
32 CONSTANT BITS
48000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x04 CONSTANT X1  \ Return Address
0x08 CONSTANT X2  \ Stack Pointer (SP)
0x0C CONSTANT X3  \ Global Pointer (GP)
0x3C CONSTANT PC  \ Program Counter

\ 内存段定义
0x08000000 CONSTANT FLASH-START
0x08003FFF CONSTANT FLASH-END
16384 CONSTANT FLASH-SIZE  \ 
0x20000000 CONSTANT SRAM-START
0x200007FF CONSTANT SRAM-END
2048 CONSTANT SRAM-SIZE  \ 
0x40000000 CONSTANT PERIPHERAL-START
0x40003FFF CONSTANT PERIPHERAL-END
16384 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ Reset and Clock Control
0x40021000 CONSTANT RCC-BASE
0x00 CONSTANT RCC-CTLR
0x04 CONSTANT RCC-CFGR0
0x18 CONSTANT RCC-APB2PCENR
2 CONSTANT RCC-APB2PCENR-IOPAEN  \ GPIOA clock enable
4 CONSTANT RCC-APB2PCENR-IOPCEN  \ GPIOC clock enable
5 CONSTANT RCC-APB2PCENR-IOPDEN  \ GPIOD clock enable
\ General Purpose I/O Port A
0x40010800 CONSTANT GPIOA-BASE
0x00 CONSTANT GPIOA-CFGLR
0x04 CONSTANT GPIOA-CFGHR
0x08 CONSTANT GPIOA-INDR
0x0C CONSTANT GPIOA-OUTDR
0x10 CONSTANT GPIOA-BSHR
0x14 CONSTANT GPIOA-BCR
\ General Purpose I/O Port C
0x40011000 CONSTANT GPIOC-BASE
0x00 CONSTANT GPIOC-CFGLR
0x04 CONSTANT GPIOC-CFGHR
0x08 CONSTANT GPIOC-INDR
0x0C CONSTANT GPIOC-OUTDR
0x10 CONSTANT GPIOC-BSHR
0x14 CONSTANT GPIOC-BCR
\ General Purpose I/O Port D
0x40011400 CONSTANT GPIOD-BASE
0x00 CONSTANT GPIOD-CFGLR
0x04 CONSTANT GPIOD-CFGHR
0x08 CONSTANT GPIOD-INDR
0x0C CONSTANT GPIOD-OUTDR
0x10 CONSTANT GPIOD-BSHR
0x14 CONSTANT GPIOD-BCR
\ USART1
0x40013800 CONSTANT USART1-BASE
0x00 CONSTANT USART1-STATR
0x04 CONSTANT USART1-DATAR
0x08 CONSTANT USART1-BRR
0x0C CONSTANT USART1-CTLR1

\ 中断向量定义
1 CONSTANT INT-RESET  \ 
3 CONSTANT INT-MACHINESOFTWARE  \ 
7 CONSTANT INT-MACHINETIMER  \ 
11 CONSTANT INT-MACHINEEXTERNAL  \ 
25 CONSTANT INT-USART1  \ USART1 Global Interrupt

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: X1@ ( -- n ) X1 L@ ;
: X1! ( n -- ) X1 L! ;

: X2@ ( -- n ) X2 L@ ;
: X2! ( n -- ) X2 L! ;

: X3@ ( -- n ) X3 L@ ;
: X3! ( n -- ) X3 L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

\ 外设访问
\ RCC外设
: RCC-CTLR@ ( -- n ) RCC-CTLR L@ ;
: RCC-CTLR! ( n -- ) RCC-CTLR L! ;
: RCC-CFGR0@ ( -- n ) RCC-CFGR0 L@ ;
: RCC-CFGR0! ( n -- ) RCC-CFGR0 L! ;
: RCC-APB2PCENR@ ( -- n ) RCC-APB2PCENR L@ ;
: RCC-APB2PCENR! ( n -- ) RCC-APB2PCENR L! ;
: RCC-APB2PCENR-IOPAEN@ ( -- flag ) RCC-APB2PCENR@ 2 BIT@ ;
: RCC-APB2PCENR-IOPAEN! ( flag -- ) RCC-APB2PCENR@ 2 BIT! RCC-APB2PCENR! ;
: RCC-APB2PCENR-IOPCEN@ ( -- flag ) RCC-APB2PCENR@ 4 BIT@ ;
: RCC-APB2PCENR-IOPCEN! ( flag -- ) RCC-APB2PCENR@ 4 BIT! RCC-APB2PCENR! ;
: RCC-APB2PCENR-IOPDEN@ ( -- flag ) RCC-APB2PCENR@ 5 BIT@ ;
: RCC-APB2PCENR-IOPDEN! ( flag -- ) RCC-APB2PCENR@ 5 BIT! RCC-APB2PCENR! ;

\ GPIOA外设
: GPIOA-CFGLR@ ( -- n ) GPIOA-CFGLR L@ ;
: GPIOA-CFGLR! ( n -- ) GPIOA-CFGLR L! ;
: GPIOA-CFGHR@ ( -- n ) GPIOA-CFGHR L@ ;
: GPIOA-CFGHR! ( n -- ) GPIOA-CFGHR L! ;
: GPIOA-INDR@ ( -- n ) GPIOA-INDR L@ ;
: GPIOA-INDR! ( n -- ) GPIOA-INDR L! ;
: GPIOA-OUTDR@ ( -- n ) GPIOA-OUTDR L@ ;
: GPIOA-OUTDR! ( n -- ) GPIOA-OUTDR L! ;
: GPIOA-BSHR@ ( -- n ) GPIOA-BSHR L@ ;
: GPIOA-BSHR! ( n -- ) GPIOA-BSHR L! ;
: GPIOA-BCR@ ( -- n ) GPIOA-BCR L@ ;
: GPIOA-BCR! ( n -- ) GPIOA-BCR L! ;

\ GPIOC外设
: GPIOC-CFGLR@ ( -- n ) GPIOC-CFGLR L@ ;
: GPIOC-CFGLR! ( n -- ) GPIOC-CFGLR L! ;
: GPIOC-CFGHR@ ( -- n ) GPIOC-CFGHR L@ ;
: GPIOC-CFGHR! ( n -- ) GPIOC-CFGHR L! ;
: GPIOC-INDR@ ( -- n ) GPIOC-INDR L@ ;
: GPIOC-INDR! ( n -- ) GPIOC-INDR L! ;
: GPIOC-OUTDR@ ( -- n ) GPIOC-OUTDR L@ ;
: GPIOC-OUTDR! ( n -- ) GPIOC-OUTDR L! ;
: GPIOC-BSHR@ ( -- n ) GPIOC-BSHR L@ ;
: GPIOC-BSHR! ( n -- ) GPIOC-BSHR L! ;
: GPIOC-BCR@ ( -- n ) GPIOC-BCR L@ ;
: GPIOC-BCR! ( n -- ) GPIOC-BCR L! ;

\ GPIOD外设
: GPIOD-CFGLR@ ( -- n ) GPIOD-CFGLR L@ ;
: GPIOD-CFGLR! ( n -- ) GPIOD-CFGLR L! ;
: GPIOD-CFGHR@ ( -- n ) GPIOD-CFGHR L@ ;
: GPIOD-CFGHR! ( n -- ) GPIOD-CFGHR L! ;
: GPIOD-INDR@ ( -- n ) GPIOD-INDR L@ ;
: GPIOD-INDR! ( n -- ) GPIOD-INDR L! ;
: GPIOD-OUTDR@ ( -- n ) GPIOD-OUTDR L@ ;
: GPIOD-OUTDR! ( n -- ) GPIOD-OUTDR L! ;
: GPIOD-BSHR@ ( -- n ) GPIOD-BSHR L@ ;
: GPIOD-BSHR! ( n -- ) GPIOD-BSHR L! ;
: GPIOD-BCR@ ( -- n ) GPIOD-BCR L@ ;
: GPIOD-BCR! ( n -- ) GPIOD-BCR L! ;

\ USART1外设
: USART1-STATR@ ( -- n ) USART1-STATR L@ ;
: USART1-STATR! ( n -- ) USART1-STATR L! ;
: USART1-DATAR@ ( -- n ) USART1-DATAR L@ ;
: USART1-DATAR! ( n -- ) USART1-DATAR L! ;
: USART1-BRR@ ( -- n ) USART1-BRR L@ ;
: USART1-BRR! ( n -- ) USART1-BRR L! ;
: USART1-CTLR1@ ( -- n ) USART1-CTLR1 L@ ;
: USART1-CTLR1! ( n -- ) USART1-CTLR1 L! ;

\ =========================================
\ 设备初始化
\ =========================================

: CH32V003-INIT ( -- )
  \ 初始化CH32V003设备
  ." 初始化CH32V003..." CR

  \ 初始化寄存器
  0 X1!  \ Return Address
  0 X2!  \ Stack Pointer (SP)
  0 X3!  \ Global Pointer (GP)
  0 PC!  \ Program Counter

  \ 初始化外设
  \ 初始化RCC
  0 RCC-CTLR!  \ CTLR寄存器
  0 RCC-CFGR0!  \ CFGR0寄存器
  0 RCC-APB2PCENR!  \ APB2PCENR寄存器
  \ 初始化GPIOA
  0 GPIOA-CFGLR!  \ CFGLR寄存器
  0 GPIOA-CFGHR!  \ CFGHR寄存器
  0 GPIOA-INDR!  \ INDR寄存器
  0 GPIOA-OUTDR!  \ OUTDR寄存器
  0 GPIOA-BSHR!  \ BSHR寄存器
  0 GPIOA-BCR!  \ BCR寄存器
  \ 初始化GPIOC
  0 GPIOC-CFGLR!  \ CFGLR寄存器
  0 GPIOC-CFGHR!  \ CFGHR寄存器
  0 GPIOC-INDR!  \ INDR寄存器
  0 GPIOC-OUTDR!  \ OUTDR寄存器
  0 GPIOC-BSHR!  \ BSHR寄存器
  0 GPIOC-BCR!  \ BCR寄存器
  \ 初始化GPIOD
  0 GPIOD-CFGLR!  \ CFGLR寄存器
  0 GPIOD-CFGHR!  \ CFGHR寄存器
  0 GPIOD-INDR!  \ INDR寄存器
  0 GPIOD-OUTDR!  \ OUTDR寄存器
  0 GPIOD-BSHR!  \ BSHR寄存器
  0 GPIOD-BCR!  \ BCR寄存器
  \ 初始化USART1
  0 USART1-STATR!  \ STATR寄存器
  0 USART1-DATAR!  \ DATAR寄存器
  0 USART1-BRR!  \ BRR寄存器
  0 USART1-CTLR1!  \ CTLR1寄存器

  ." CH32V003初始化完成" CR
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
  X1@ X1 .R 8 .R SPACE ."  x1: " X1@ .
  X2@ X2 .R 8 .R SPACE ."  x2: " X2@ .
  X3@ X3 .R 8 .R SPACE ."  x3: " X3@ .
  PC@ PC .R 8 .R SPACE ."  pc: " PC@ .
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
: INT-MACHINESOFTWARE-HANDLER ( -- )
  ." MachineSoftware中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-MACHINESOFTWARE-ENABLE ( -- )
  INT-MACHINESOFTWARE INT-ENABLE
;

: INT-MACHINESOFTWARE-DISABLE ( -- )
  INT-MACHINESOFTWARE INT-DISABLE
;

\ 
: INT-MACHINETIMER-HANDLER ( -- )
  ." MachineTimer中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-MACHINETIMER-ENABLE ( -- )
  INT-MACHINETIMER INT-ENABLE
;

: INT-MACHINETIMER-DISABLE ( -- )
  INT-MACHINETIMER INT-DISABLE
;

\ 
: INT-MACHINEEXTERNAL-HANDLER ( -- )
  ." MachineExternal中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-MACHINEEXTERNAL-ENABLE ( -- )
  INT-MACHINEEXTERNAL INT-ENABLE
;

: INT-MACHINEEXTERNAL-DISABLE ( -- )
  INT-MACHINEEXTERNAL INT-DISABLE
;

\ USART1 Global Interrupt
: INT-USART1-HANDLER ( -- )
  ." USART1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USART1-ENABLE ( -- )
  INT-USART1 INT-ENABLE
;

: INT-USART1-DISABLE ( -- )
  INT-USART1 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  CH32V003-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

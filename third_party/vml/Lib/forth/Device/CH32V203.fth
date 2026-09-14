\ CH32V203设备定义 - Forth文件
\ 生成自: WCH/CH32V2/CH32V203
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit RISC-V MCU with 64KB Flash, 20KB RAM, 144MHz
\ CPU架构: RISC-V
\ 位宽: 32位
\ 时钟频率: 144000000 Hz

\ =========================================
\ CH32V203设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" CH32V203" ;
: MANUFACTURER  S" WCH" ;
: FAMILY        S" CH32V2" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" RISC-V" ;
32 CONSTANT BITS
144000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x04 CONSTANT X1  \ Return Address
0x08 CONSTANT X2  \ Stack Pointer (SP)
0x0C CONSTANT X3  \ Global Pointer (GP)
0x20 CONSTANT X8  \ Frame Pointer (FP)
0x28 CONSTANT X10  \ Function Argument (A0)
0x2C CONSTANT X11  \ Function Argument (A1)
0x3C CONSTANT PC  \ Program Counter

\ 内存段定义
0x08000000 CONSTANT FLASH-START
0x0800FFFF CONSTANT FLASH-END
65536 CONSTANT FLASH-SIZE  \ 
0x20000000 CONSTANT SRAM-START
0x20004FFF CONSTANT SRAM-END
20480 CONSTANT SRAM-SIZE  \ 
0x40000000 CONSTANT PERIPHERAL-START
0x4003FFFF CONSTANT PERIPHERAL-END
262144 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ Reset and Clock Control
0x40021000 CONSTANT RCC-BASE
0x00 CONSTANT RCC-RCC_CTLR
0x04 CONSTANT RCC-RCC_CFGR0
0x18 CONSTANT RCC-RCC_APB2PCENR
2 CONSTANT RCC-RCC_APB2PCENR-IOPAEN  \ GPIOA clock enable
3 CONSTANT RCC-RCC_APB2PCENR-IOPBEN  \ GPIOB clock enable
4 CONSTANT RCC-RCC_APB2PCENR-IOPCEN  \ GPIOC clock enable
\ General Purpose I/O Port A
0x40010800 CONSTANT GPIOA-BASE
0x00 CONSTANT GPIOA-CFGLR
0x04 CONSTANT GPIOA-CFGHR
0x08 CONSTANT GPIOA-INDR
0x0C CONSTANT GPIOA-OUTDR
0x10 CONSTANT GPIOA-BSHR
0x14 CONSTANT GPIOA-BCR
\ General Purpose I/O Port B
0x40010C00 CONSTANT GPIOB-BASE
0x00 CONSTANT GPIOB-CFGLR
0x04 CONSTANT GPIOB-CFGHR
0x08 CONSTANT GPIOB-INDR
0x0C CONSTANT GPIOB-OUTDR
0x10 CONSTANT GPIOB-BSHR
0x14 CONSTANT GPIOB-BCR
\ General Purpose I/O Port C
0x40011000 CONSTANT GPIOC-BASE
0x00 CONSTANT GPIOC-CFGLR
0x04 CONSTANT GPIOC-CFGHR
0x08 CONSTANT GPIOC-INDR
0x0C CONSTANT GPIOC-OUTDR
0x10 CONSTANT GPIOC-BSHR
0x14 CONSTANT GPIOC-BCR
\ USART1
0x40013800 CONSTANT USART1-BASE
0x00 CONSTANT USART1-USART_STATR
0x04 CONSTANT USART1-USART_DATAR
0x08 CONSTANT USART1-USART_BRR
0x0C CONSTANT USART1-USART_CTLR1

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

: X8@ ( -- n ) X8 L@ ;
: X8! ( n -- ) X8 L! ;

: X10@ ( -- n ) X10 L@ ;
: X10! ( n -- ) X10 L! ;

: X11@ ( -- n ) X11 L@ ;
: X11! ( n -- ) X11 L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

\ 外设访问
\ RCC外设
: RCC-RCC_CTLR@ ( -- n ) RCC-RCC_CTLR L@ ;
: RCC-RCC_CTLR! ( n -- ) RCC-RCC_CTLR L! ;
: RCC-RCC_CFGR0@ ( -- n ) RCC-RCC_CFGR0 L@ ;
: RCC-RCC_CFGR0! ( n -- ) RCC-RCC_CFGR0 L! ;
: RCC-RCC_APB2PCENR@ ( -- n ) RCC-RCC_APB2PCENR L@ ;
: RCC-RCC_APB2PCENR! ( n -- ) RCC-RCC_APB2PCENR L! ;
: RCC-RCC_APB2PCENR-IOPAEN@ ( -- flag ) RCC-RCC_APB2PCENR@ 2 BIT@ ;
: RCC-RCC_APB2PCENR-IOPAEN! ( flag -- ) RCC-RCC_APB2PCENR@ 2 BIT! RCC-RCC_APB2PCENR! ;
: RCC-RCC_APB2PCENR-IOPBEN@ ( -- flag ) RCC-RCC_APB2PCENR@ 3 BIT@ ;
: RCC-RCC_APB2PCENR-IOPBEN! ( flag -- ) RCC-RCC_APB2PCENR@ 3 BIT! RCC-RCC_APB2PCENR! ;
: RCC-RCC_APB2PCENR-IOPCEN@ ( -- flag ) RCC-RCC_APB2PCENR@ 4 BIT@ ;
: RCC-RCC_APB2PCENR-IOPCEN! ( flag -- ) RCC-RCC_APB2PCENR@ 4 BIT! RCC-RCC_APB2PCENR! ;

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

\ GPIOB外设
: GPIOB-CFGLR@ ( -- n ) GPIOB-CFGLR L@ ;
: GPIOB-CFGLR! ( n -- ) GPIOB-CFGLR L! ;
: GPIOB-CFGHR@ ( -- n ) GPIOB-CFGHR L@ ;
: GPIOB-CFGHR! ( n -- ) GPIOB-CFGHR L! ;
: GPIOB-INDR@ ( -- n ) GPIOB-INDR L@ ;
: GPIOB-INDR! ( n -- ) GPIOB-INDR L! ;
: GPIOB-OUTDR@ ( -- n ) GPIOB-OUTDR L@ ;
: GPIOB-OUTDR! ( n -- ) GPIOB-OUTDR L! ;
: GPIOB-BSHR@ ( -- n ) GPIOB-BSHR L@ ;
: GPIOB-BSHR! ( n -- ) GPIOB-BSHR L! ;
: GPIOB-BCR@ ( -- n ) GPIOB-BCR L@ ;
: GPIOB-BCR! ( n -- ) GPIOB-BCR L! ;

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

\ USART1外设
: USART1-USART_STATR@ ( -- n ) USART1-USART_STATR L@ ;
: USART1-USART_STATR! ( n -- ) USART1-USART_STATR L! ;
: USART1-USART_DATAR@ ( -- n ) USART1-USART_DATAR L@ ;
: USART1-USART_DATAR! ( n -- ) USART1-USART_DATAR L! ;
: USART1-USART_BRR@ ( -- n ) USART1-USART_BRR L@ ;
: USART1-USART_BRR! ( n -- ) USART1-USART_BRR L! ;
: USART1-USART_CTLR1@ ( -- n ) USART1-USART_CTLR1 L@ ;
: USART1-USART_CTLR1! ( n -- ) USART1-USART_CTLR1 L! ;

\ =========================================
\ 设备初始化
\ =========================================

: CH32V203-INIT ( -- )
  \ 初始化CH32V203设备
  ." 初始化CH32V203..." CR

  \ 初始化寄存器
  0 X1!  \ Return Address
  0 X2!  \ Stack Pointer (SP)
  0 X3!  \ Global Pointer (GP)
  0 X8!  \ Frame Pointer (FP)
  0 X10!  \ Function Argument (A0)
  0 X11!  \ Function Argument (A1)
  0 PC!  \ Program Counter

  \ 初始化外设
  \ 初始化RCC
  0 RCC-RCC_CTLR!  \ RCC_CTLR寄存器
  0 RCC-RCC_CFGR0!  \ RCC_CFGR0寄存器
  0 RCC-RCC_APB2PCENR!  \ RCC_APB2PCENR寄存器
  \ 初始化GPIOA
  0 GPIOA-CFGLR!  \ CFGLR寄存器
  0 GPIOA-CFGHR!  \ CFGHR寄存器
  0 GPIOA-INDR!  \ INDR寄存器
  0 GPIOA-OUTDR!  \ OUTDR寄存器
  0 GPIOA-BSHR!  \ BSHR寄存器
  0 GPIOA-BCR!  \ BCR寄存器
  \ 初始化GPIOB
  0 GPIOB-CFGLR!  \ CFGLR寄存器
  0 GPIOB-CFGHR!  \ CFGHR寄存器
  0 GPIOB-INDR!  \ INDR寄存器
  0 GPIOB-OUTDR!  \ OUTDR寄存器
  0 GPIOB-BSHR!  \ BSHR寄存器
  0 GPIOB-BCR!  \ BCR寄存器
  \ 初始化GPIOC
  0 GPIOC-CFGLR!  \ CFGLR寄存器
  0 GPIOC-CFGHR!  \ CFGHR寄存器
  0 GPIOC-INDR!  \ INDR寄存器
  0 GPIOC-OUTDR!  \ OUTDR寄存器
  0 GPIOC-BSHR!  \ BSHR寄存器
  0 GPIOC-BCR!  \ BCR寄存器
  \ 初始化USART1
  0 USART1-USART_STATR!  \ USART_STATR寄存器
  0 USART1-USART_DATAR!  \ USART_DATAR寄存器
  0 USART1-USART_BRR!  \ USART_BRR寄存器
  0 USART1-USART_CTLR1!  \ USART_CTLR1寄存器

  ." CH32V203初始化完成" CR
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
  X8@ X8 .R 8 .R SPACE ."  x8: " X8@ .
  X10@ X10 .R 8 .R SPACE ."  x10: " X10@ .
  X11@ X11 .R 8 .R SPACE ."  x11: " X11@ .
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
  CH32V203-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

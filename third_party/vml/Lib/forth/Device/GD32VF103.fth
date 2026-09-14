\ GD32VF103设备定义 - Forth文件
\ 生成自: GigaDevice/GD32/GD32VF103
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit RISC-V RV32IMAC MCU with 128KB Flash, 32KB RAM, 108MHz, STM32F103 compatible
\ CPU架构: RISC-V
\ 位宽: 32位
\ 时钟频率: 108000000 Hz

\ =========================================
\ GD32VF103设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" GD32VF103" ;
: MANUFACTURER  S" GigaDevice" ;
: FAMILY        S" GD32" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" RISC-V" ;
32 CONSTANT BITS
108000000 CONSTANT CLOCK-FREQ

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
0x0801FFFF CONSTANT FLASH-END
131072 CONSTANT FLASH-SIZE  \ 
0x20000000 CONSTANT SRAM-START
0x20007FFF CONSTANT SRAM-END
32768 CONSTANT SRAM-SIZE  \ 
0x40000000 CONSTANT PERIPHERAL-START
0x4003FFFF CONSTANT PERIPHERAL-END
262144 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ Reset and Clock Control
0x40021000 CONSTANT RCU-BASE
0x00 CONSTANT RCU-CTL
0x04 CONSTANT RCU-CFG0
0x08 CONSTANT RCU-CFG1
0x18 CONSTANT RCU-APB2EN
2 CONSTANT RCU-APB2EN-PAEN  \ GPIOA enable
3 CONSTANT RCU-APB2EN-PBEN  \ GPIOB enable
4 CONSTANT RCU-APB2EN-PCEN  \ GPIOC enable
14 CONSTANT RCU-APB2EN-USART0EN  \ USART0 enable
0x1C CONSTANT RCU-APB1EN
\ General Purpose I/O Port A
0x40010800 CONSTANT GPIOA-BASE
0x00 CONSTANT GPIOA-CTL0
0x04 CONSTANT GPIOA-CTL1
0x08 CONSTANT GPIOA-ISTAT
0x0C CONSTANT GPIOA-OCTL
0x10 CONSTANT GPIOA-BOP
0x14 CONSTANT GPIOA-BC
\ General Purpose I/O Port B
0x40010C00 CONSTANT GPIOB-BASE
0x00 CONSTANT GPIOB-CTL0
0x04 CONSTANT GPIOB-CTL1
0x08 CONSTANT GPIOB-ISTAT
0x0C CONSTANT GPIOB-OCTL
0x10 CONSTANT GPIOB-BOP
0x14 CONSTANT GPIOB-BC
\ General Purpose I/O Port C
0x40011000 CONSTANT GPIOC-BASE
0x00 CONSTANT GPIOC-CTL0
0x04 CONSTANT GPIOC-CTL1
0x08 CONSTANT GPIOC-ISTAT
0x0C CONSTANT GPIOC-OCTL
0x10 CONSTANT GPIOC-BOP
0x14 CONSTANT GPIOC-BC
\ USART0
0x40013800 CONSTANT USART0-BASE
0x00 CONSTANT USART0-STATR
0x04 CONSTANT USART0-DATAR
0x08 CONSTANT USART0-BRR
0x0C CONSTANT USART0-CTLR1

\ 中断向量定义
1 CONSTANT INT-RESET  \ 
3 CONSTANT INT-MACHINESOFTWARE  \ 
7 CONSTANT INT-MACHINETIMER  \ 
11 CONSTANT INT-MACHINEEXTERNAL  \ 
25 CONSTANT INT-USART0  \ USART0 Global Interrupt

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
\ RCU外设
: RCU-CTL@ ( -- n ) RCU-CTL L@ ;
: RCU-CTL! ( n -- ) RCU-CTL L! ;
: RCU-CFG0@ ( -- n ) RCU-CFG0 L@ ;
: RCU-CFG0! ( n -- ) RCU-CFG0 L! ;
: RCU-CFG1@ ( -- n ) RCU-CFG1 L@ ;
: RCU-CFG1! ( n -- ) RCU-CFG1 L! ;
: RCU-APB2EN@ ( -- n ) RCU-APB2EN L@ ;
: RCU-APB2EN! ( n -- ) RCU-APB2EN L! ;
: RCU-APB2EN-PAEN@ ( -- flag ) RCU-APB2EN@ 2 BIT@ ;
: RCU-APB2EN-PAEN! ( flag -- ) RCU-APB2EN@ 2 BIT! RCU-APB2EN! ;
: RCU-APB2EN-PBEN@ ( -- flag ) RCU-APB2EN@ 3 BIT@ ;
: RCU-APB2EN-PBEN! ( flag -- ) RCU-APB2EN@ 3 BIT! RCU-APB2EN! ;
: RCU-APB2EN-PCEN@ ( -- flag ) RCU-APB2EN@ 4 BIT@ ;
: RCU-APB2EN-PCEN! ( flag -- ) RCU-APB2EN@ 4 BIT! RCU-APB2EN! ;
: RCU-APB2EN-USART0EN@ ( -- flag ) RCU-APB2EN@ 14 BIT@ ;
: RCU-APB2EN-USART0EN! ( flag -- ) RCU-APB2EN@ 14 BIT! RCU-APB2EN! ;
: RCU-APB1EN@ ( -- n ) RCU-APB1EN L@ ;
: RCU-APB1EN! ( n -- ) RCU-APB1EN L! ;

\ GPIOA外设
: GPIOA-CTL0@ ( -- n ) GPIOA-CTL0 L@ ;
: GPIOA-CTL0! ( n -- ) GPIOA-CTL0 L! ;
: GPIOA-CTL1@ ( -- n ) GPIOA-CTL1 L@ ;
: GPIOA-CTL1! ( n -- ) GPIOA-CTL1 L! ;
: GPIOA-ISTAT@ ( -- n ) GPIOA-ISTAT L@ ;
: GPIOA-ISTAT! ( n -- ) GPIOA-ISTAT L! ;
: GPIOA-OCTL@ ( -- n ) GPIOA-OCTL L@ ;
: GPIOA-OCTL! ( n -- ) GPIOA-OCTL L! ;
: GPIOA-BOP@ ( -- n ) GPIOA-BOP L@ ;
: GPIOA-BOP! ( n -- ) GPIOA-BOP L! ;
: GPIOA-BC@ ( -- n ) GPIOA-BC L@ ;
: GPIOA-BC! ( n -- ) GPIOA-BC L! ;

\ GPIOB外设
: GPIOB-CTL0@ ( -- n ) GPIOB-CTL0 L@ ;
: GPIOB-CTL0! ( n -- ) GPIOB-CTL0 L! ;
: GPIOB-CTL1@ ( -- n ) GPIOB-CTL1 L@ ;
: GPIOB-CTL1! ( n -- ) GPIOB-CTL1 L! ;
: GPIOB-ISTAT@ ( -- n ) GPIOB-ISTAT L@ ;
: GPIOB-ISTAT! ( n -- ) GPIOB-ISTAT L! ;
: GPIOB-OCTL@ ( -- n ) GPIOB-OCTL L@ ;
: GPIOB-OCTL! ( n -- ) GPIOB-OCTL L! ;
: GPIOB-BOP@ ( -- n ) GPIOB-BOP L@ ;
: GPIOB-BOP! ( n -- ) GPIOB-BOP L! ;
: GPIOB-BC@ ( -- n ) GPIOB-BC L@ ;
: GPIOB-BC! ( n -- ) GPIOB-BC L! ;

\ GPIOC外设
: GPIOC-CTL0@ ( -- n ) GPIOC-CTL0 L@ ;
: GPIOC-CTL0! ( n -- ) GPIOC-CTL0 L! ;
: GPIOC-CTL1@ ( -- n ) GPIOC-CTL1 L@ ;
: GPIOC-CTL1! ( n -- ) GPIOC-CTL1 L! ;
: GPIOC-ISTAT@ ( -- n ) GPIOC-ISTAT L@ ;
: GPIOC-ISTAT! ( n -- ) GPIOC-ISTAT L! ;
: GPIOC-OCTL@ ( -- n ) GPIOC-OCTL L@ ;
: GPIOC-OCTL! ( n -- ) GPIOC-OCTL L! ;
: GPIOC-BOP@ ( -- n ) GPIOC-BOP L@ ;
: GPIOC-BOP! ( n -- ) GPIOC-BOP L! ;
: GPIOC-BC@ ( -- n ) GPIOC-BC L@ ;
: GPIOC-BC! ( n -- ) GPIOC-BC L! ;

\ USART0外设
: USART0-STATR@ ( -- n ) USART0-STATR L@ ;
: USART0-STATR! ( n -- ) USART0-STATR L! ;
: USART0-DATAR@ ( -- n ) USART0-DATAR L@ ;
: USART0-DATAR! ( n -- ) USART0-DATAR L! ;
: USART0-BRR@ ( -- n ) USART0-BRR L@ ;
: USART0-BRR! ( n -- ) USART0-BRR L! ;
: USART0-CTLR1@ ( -- n ) USART0-CTLR1 L@ ;
: USART0-CTLR1! ( n -- ) USART0-CTLR1 L! ;

\ =========================================
\ 设备初始化
\ =========================================

: GD32VF103-INIT ( -- )
  \ 初始化GD32VF103设备
  ." 初始化GD32VF103..." CR

  \ 初始化寄存器
  0 X1!  \ Return Address
  0 X2!  \ Stack Pointer (SP)
  0 X3!  \ Global Pointer (GP)
  0 X8!  \ Frame Pointer (FP)
  0 X10!  \ Function Argument (A0)
  0 X11!  \ Function Argument (A1)
  0 PC!  \ Program Counter

  \ 初始化外设
  \ 初始化RCU
  0 RCU-CTL!  \ CTL寄存器
  0 RCU-CFG0!  \ CFG0寄存器
  0 RCU-CFG1!  \ CFG1寄存器
  0 RCU-APB2EN!  \ APB2EN寄存器
  0 RCU-APB1EN!  \ APB1EN寄存器
  \ 初始化GPIOA
  0 GPIOA-CTL0!  \ CTL0寄存器
  0 GPIOA-CTL1!  \ CTL1寄存器
  0 GPIOA-ISTAT!  \ ISTAT寄存器
  0 GPIOA-OCTL!  \ OCTL寄存器
  0 GPIOA-BOP!  \ BOP寄存器
  0 GPIOA-BC!  \ BC寄存器
  \ 初始化GPIOB
  0 GPIOB-CTL0!  \ CTL0寄存器
  0 GPIOB-CTL1!  \ CTL1寄存器
  0 GPIOB-ISTAT!  \ ISTAT寄存器
  0 GPIOB-OCTL!  \ OCTL寄存器
  0 GPIOB-BOP!  \ BOP寄存器
  0 GPIOB-BC!  \ BC寄存器
  \ 初始化GPIOC
  0 GPIOC-CTL0!  \ CTL0寄存器
  0 GPIOC-CTL1!  \ CTL1寄存器
  0 GPIOC-ISTAT!  \ ISTAT寄存器
  0 GPIOC-OCTL!  \ OCTL寄存器
  0 GPIOC-BOP!  \ BOP寄存器
  0 GPIOC-BC!  \ BC寄存器
  \ 初始化USART0
  0 USART0-STATR!  \ STATR寄存器
  0 USART0-DATAR!  \ DATAR寄存器
  0 USART0-BRR!  \ BRR寄存器
  0 USART0-CTLR1!  \ CTLR1寄存器

  ." GD32VF103初始化完成" CR
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

\ USART0 Global Interrupt
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
  GD32VF103-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

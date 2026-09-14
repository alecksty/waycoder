\ nRF52832设备定义 - Forth文件
\ 生成自: Nordic/nRF52/nRF52832
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M4F BLE SoC with 512KB Flash, 64KB RAM, 64MHz
\ CPU架构: ARM-Cortex-M4F
\ 位宽: 32位
\ 时钟频率: 64000000 Hz

\ =========================================
\ nRF52832设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" nRF52832" ;
: MANUFACTURER  S" Nordic" ;
: FAMILY        S" nRF52" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M4F" ;
32 CONSTANT BITS
64000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT R0  \ 
0x04 CONSTANT R1  \ 
0x08 CONSTANT R2  \ 
0x0C CONSTANT R3  \ 
0x34 CONSTANT SP  \ 
0x38 CONSTANT LR  \ 
0x3C CONSTANT PC  \ 

\ 内存段定义
0x00000000 CONSTANT FLASH-START
0x0007FFFF CONSTANT FLASH-END
524288 CONSTANT FLASH-SIZE  \ 
0x20000000 CONSTANT SRAM-START
0x2000FFFF CONSTANT SRAM-END
65536 CONSTANT SRAM-SIZE  \ 
0x40000000 CONSTANT PERIPHERAL-START
0x400FFFFF CONSTANT PERIPHERAL-END
1048576 CONSTANT PERIPHERAL-SIZE  \ 
0x10000000 CONSTANT FICR-START
0x10000FFF CONSTANT FICR-END
4096 CONSTANT FICR-SIZE  \ Factory Information Configuration Registers

\ 外设定义
\ General Purpose I/O Port 0
0x50000000 CONSTANT GPIO_P0-BASE
0x504 CONSTANT GPIO_P0-OUT
0x508 CONSTANT GPIO_P0-OUTSET
0x50C CONSTANT GPIO_P0-OUTCLR
0x510 CONSTANT GPIO_P0-IN
0x514 CONSTANT GPIO_P0-DIR
0x518 CONSTANT GPIO_P0-DIRSET
0x51C CONSTANT GPIO_P0-DIRCLR
\ Power Control
0x40000000 CONSTANT POWER-BASE
0x1C4 CONSTANT POWER-DCDCEN
0x268 CONSTANT POWER-RAMSTATUS
\ Clock Control
0x40000000 CONSTANT CLOCK-BASE
0x108 CONSTANT CLOCK-HFCLKSTART
0x208 CONSTANT CLOCK-HFCLKSTARTED

\ 中断向量定义
0 CONSTANT INT-RESET  \ 
11 CONSTANT INT-SVCALL  \ 

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

: SP@ ( -- n ) SP L@ ;
: SP! ( n -- ) SP L! ;

: LR@ ( -- n ) LR L@ ;
: LR! ( n -- ) LR L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

\ 外设访问
\ GPIO_P0外设
: GPIO_P0-OUT@ ( -- n ) GPIO_P0-OUT L@ ;
: GPIO_P0-OUT! ( n -- ) GPIO_P0-OUT L! ;
: GPIO_P0-OUTSET@ ( -- n ) GPIO_P0-OUTSET L@ ;
: GPIO_P0-OUTSET! ( n -- ) GPIO_P0-OUTSET L! ;
: GPIO_P0-OUTCLR@ ( -- n ) GPIO_P0-OUTCLR L@ ;
: GPIO_P0-OUTCLR! ( n -- ) GPIO_P0-OUTCLR L! ;
: GPIO_P0-IN@ ( -- n ) GPIO_P0-IN L@ ;
: GPIO_P0-IN! ( n -- ) GPIO_P0-IN L! ;
: GPIO_P0-DIR@ ( -- n ) GPIO_P0-DIR L@ ;
: GPIO_P0-DIR! ( n -- ) GPIO_P0-DIR L! ;
: GPIO_P0-DIRSET@ ( -- n ) GPIO_P0-DIRSET L@ ;
: GPIO_P0-DIRSET! ( n -- ) GPIO_P0-DIRSET L! ;
: GPIO_P0-DIRCLR@ ( -- n ) GPIO_P0-DIRCLR L@ ;
: GPIO_P0-DIRCLR! ( n -- ) GPIO_P0-DIRCLR L! ;

\ POWER外设
: POWER-DCDCEN@ ( -- n ) POWER-DCDCEN L@ ;
: POWER-DCDCEN! ( n -- ) POWER-DCDCEN L! ;
: POWER-RAMSTATUS@ ( -- n ) POWER-RAMSTATUS L@ ;
: POWER-RAMSTATUS! ( n -- ) POWER-RAMSTATUS L! ;

\ CLOCK外设
: CLOCK-HFCLKSTART@ ( -- n ) CLOCK-HFCLKSTART L@ ;
: CLOCK-HFCLKSTART! ( n -- ) CLOCK-HFCLKSTART L! ;
: CLOCK-HFCLKSTARTED@ ( -- n ) CLOCK-HFCLKSTARTED L@ ;
: CLOCK-HFCLKSTARTED! ( n -- ) CLOCK-HFCLKSTARTED L! ;

\ =========================================
\ 设备初始化
\ =========================================

: NRF52832-INIT ( -- )
  \ 初始化nRF52832设备
  ." 初始化nRF52832..." CR

  \ 初始化寄存器
  0 R0!  \ 
  0 R1!  \ 
  0 R2!  \ 
  0 R3!  \ 
  0 SP!  \ 
  0 LR!  \ 
  0 PC!  \ 

  \ 初始化外设
  \ 初始化GPIO_P0
  0 GPIO_P0-OUT!  \ OUT寄存器
  0 GPIO_P0-OUTSET!  \ OUTSET寄存器
  0 GPIO_P0-OUTCLR!  \ OUTCLR寄存器
  0 GPIO_P0-IN!  \ IN寄存器
  0 GPIO_P0-DIR!  \ DIR寄存器
  0 GPIO_P0-DIRSET!  \ DIRSET寄存器
  0 GPIO_P0-DIRCLR!  \ DIRCLR寄存器
  \ 初始化POWER
  0 POWER-DCDCEN!  \ DCDCEN寄存器
  0 POWER-RAMSTATUS!  \ RAMSTATUS寄存器
  \ 初始化CLOCK
  0 CLOCK-HFCLKSTART!  \ HFCLKSTART寄存器
  0 CLOCK-HFCLKSTARTED!  \ HFCLKSTARTED寄存器

  ." nRF52832初始化完成" CR
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

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  NRF52832-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

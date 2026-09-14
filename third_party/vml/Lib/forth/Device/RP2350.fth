\ RP2350设备定义 - Forth文件
\ 生成自: Raspberry/RP2/RP2350
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: Dual Cortex-M33 + RISC-V Hazard3 MCU with 520KB SRAM, 150MHz
\ CPU架构: ARM-Cortex-M33
\ 位宽: 32位
\ 时钟频率: 150000000 Hz

\ =========================================
\ RP2350设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" RP2350" ;
: MANUFACTURER  S" Raspberry" ;
: FAMILY        S" RP2" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M33" ;
32 CONSTANT BITS
150000000 CONSTANT CLOCK-FREQ

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
0x10000000 CONSTANT FLASH-START
0x107FFFFF CONSTANT FLASH-END
8388608 CONSTANT FLASH-SIZE  \ XIP Flash
0x20000000 CONSTANT SRAM-START
0x20081FFF CONSTANT SRAM-END
532480 CONSTANT SRAM-SIZE  \ Total SRAM
0x40000000 CONSTANT PERIPHERAL-START
0x5000FFFF CONSTANT PERIPHERAL-END
16777216 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ Single-Cycle I/O (GPIO)
0xD0000000 CONSTANT SIO-BASE
0x004 CONSTANT SIO-GPIO_IN
0x010 CONSTANT SIO-GPIO_OUT
0x014 CONSTANT SIO-GPIO_OUT_SET
0x018 CONSTANT SIO-GPIO_OUT_CLR
0x01C CONSTANT SIO-GPIO_OUT_XOR
0x020 CONSTANT SIO-GPIO_OE
0x024 CONSTANT SIO-GPIO_OE_SET
0x028 CONSTANT SIO-GPIO_OE_CLR
\ IO Bank 0 (GPIO control)
0x40028000 CONSTANT IO_BANK0-BASE
0x000 CONSTANT IO_BANK0-GPIO0_STATUS
0x004 CONSTANT IO_BANK0-GPIO0_CTRL
0x008 CONSTANT IO_BANK0-GPIO1_STATUS
0x00C CONSTANT IO_BANK0-GPIO1_CTRL
\ Pad controls for GPIO 0-29
0x4002C000 CONSTANT PADS_BANK0-BASE
0x000 CONSTANT PADS_BANK0-GPIO0
0x004 CONSTANT PADS_BANK0-GPIO1
\ Reset Controller
0x4000C000 CONSTANT RESETS-BASE
0x000 CONSTANT RESETS-RESET
0x008 CONSTANT RESETS-RESET_DONE

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
\ SIO外设
: SIO-GPIO_IN@ ( -- n ) SIO-GPIO_IN L@ ;
: SIO-GPIO_IN! ( n -- ) SIO-GPIO_IN L! ;
: SIO-GPIO_OUT@ ( -- n ) SIO-GPIO_OUT L@ ;
: SIO-GPIO_OUT! ( n -- ) SIO-GPIO_OUT L! ;
: SIO-GPIO_OUT_SET@ ( -- n ) SIO-GPIO_OUT_SET L@ ;
: SIO-GPIO_OUT_SET! ( n -- ) SIO-GPIO_OUT_SET L! ;
: SIO-GPIO_OUT_CLR@ ( -- n ) SIO-GPIO_OUT_CLR L@ ;
: SIO-GPIO_OUT_CLR! ( n -- ) SIO-GPIO_OUT_CLR L! ;
: SIO-GPIO_OUT_XOR@ ( -- n ) SIO-GPIO_OUT_XOR L@ ;
: SIO-GPIO_OUT_XOR! ( n -- ) SIO-GPIO_OUT_XOR L! ;
: SIO-GPIO_OE@ ( -- n ) SIO-GPIO_OE L@ ;
: SIO-GPIO_OE! ( n -- ) SIO-GPIO_OE L! ;
: SIO-GPIO_OE_SET@ ( -- n ) SIO-GPIO_OE_SET L@ ;
: SIO-GPIO_OE_SET! ( n -- ) SIO-GPIO_OE_SET L! ;
: SIO-GPIO_OE_CLR@ ( -- n ) SIO-GPIO_OE_CLR L@ ;
: SIO-GPIO_OE_CLR! ( n -- ) SIO-GPIO_OE_CLR L! ;

\ IO_BANK0外设
: IO_BANK0-GPIO0_STATUS@ ( -- n ) IO_BANK0-GPIO0_STATUS L@ ;
: IO_BANK0-GPIO0_STATUS! ( n -- ) IO_BANK0-GPIO0_STATUS L! ;
: IO_BANK0-GPIO0_CTRL@ ( -- n ) IO_BANK0-GPIO0_CTRL L@ ;
: IO_BANK0-GPIO0_CTRL! ( n -- ) IO_BANK0-GPIO0_CTRL L! ;
: IO_BANK0-GPIO1_STATUS@ ( -- n ) IO_BANK0-GPIO1_STATUS L@ ;
: IO_BANK0-GPIO1_STATUS! ( n -- ) IO_BANK0-GPIO1_STATUS L! ;
: IO_BANK0-GPIO1_CTRL@ ( -- n ) IO_BANK0-GPIO1_CTRL L@ ;
: IO_BANK0-GPIO1_CTRL! ( n -- ) IO_BANK0-GPIO1_CTRL L! ;

\ PADS_BANK0外设
: PADS_BANK0-GPIO0@ ( -- n ) PADS_BANK0-GPIO0 L@ ;
: PADS_BANK0-GPIO0! ( n -- ) PADS_BANK0-GPIO0 L! ;
: PADS_BANK0-GPIO1@ ( -- n ) PADS_BANK0-GPIO1 L@ ;
: PADS_BANK0-GPIO1! ( n -- ) PADS_BANK0-GPIO1 L! ;

\ RESETS外设
: RESETS-RESET@ ( -- n ) RESETS-RESET L@ ;
: RESETS-RESET! ( n -- ) RESETS-RESET L! ;
: RESETS-RESET_DONE@ ( -- n ) RESETS-RESET_DONE L@ ;
: RESETS-RESET_DONE! ( n -- ) RESETS-RESET_DONE L! ;

\ =========================================
\ 设备初始化
\ =========================================

: RP2350-INIT ( -- )
  \ 初始化RP2350设备
  ." 初始化RP2350..." CR

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
  \ 初始化SIO
  0 SIO-GPIO_IN!  \ GPIO_IN寄存器
  0 SIO-GPIO_OUT!  \ GPIO_OUT寄存器
  0 SIO-GPIO_OUT_SET!  \ GPIO_OUT_SET寄存器
  0 SIO-GPIO_OUT_CLR!  \ GPIO_OUT_CLR寄存器
  0 SIO-GPIO_OUT_XOR!  \ GPIO_OUT_XOR寄存器
  0 SIO-GPIO_OE!  \ GPIO_OE寄存器
  0 SIO-GPIO_OE_SET!  \ GPIO_OE_SET寄存器
  0 SIO-GPIO_OE_CLR!  \ GPIO_OE_CLR寄存器
  \ 初始化IO_BANK0
  0 IO_BANK0-GPIO0_STATUS!  \ GPIO0_STATUS寄存器
  0 IO_BANK0-GPIO0_CTRL!  \ GPIO0_CTRL寄存器
  0 IO_BANK0-GPIO1_STATUS!  \ GPIO1_STATUS寄存器
  0 IO_BANK0-GPIO1_CTRL!  \ GPIO1_CTRL寄存器
  \ 初始化PADS_BANK0
  0 PADS_BANK0-GPIO0!  \ GPIO0寄存器
  0 PADS_BANK0-GPIO1!  \ GPIO1寄存器
  \ 初始化RESETS
  0 RESETS-RESET!  \ RESET寄存器
  0 RESETS-RESET_DONE!  \ RESET_DONE寄存器

  ." RP2350初始化完成" CR
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

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  RP2350-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

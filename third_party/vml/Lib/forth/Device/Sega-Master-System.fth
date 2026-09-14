\ Sega-Master-System设备定义 - Forth文件
\ 生成自: Sega/Master System/Sega-Master-System
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Sega Master System 8-bit video game console with Z80 CPU
\ CPU架构: Zilog Z80
\ 位宽: 8位
\ 时钟频率: 3579545 Hz

\ =========================================
\ Sega-Master-System设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Sega-Master-System" ;
: MANUFACTURER  S" Sega" ;
: FAMILY        S" Master System" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Zilog Z80" ;
8 CONSTANT BITS
3579545 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0 CONSTANT A  \ Accumulator
0 CONSTANT F  \ Flags
0 CONSTANT B  \ B
0 CONSTANT C  \ C
0 CONSTANT D  \ D
0 CONSTANT E  \ E
0 CONSTANT H  \ H
0 CONSTANT L  \ L
0 CONSTANT IX  \ Index Register X
0 CONSTANT IY  \ Index Register Y
0 CONSTANT SP  \ Stack Pointer
0 CONSTANT PC  \ Program Counter
0 CONSTANT I  \ Interrupt Vector
0 CONSTANT R  \ Memory Refresh

\ 外设定义
\ Video Display Processor (TMS9918A)
 CONSTANT VDP-BASE
0xBE CONSTANT VDP-VDP_DATA
0xBF CONSTANT VDP-VDP_ADDR
0xBF CONSTANT VDP-VDP_STATUS
\ Programmable Sound Generator (SN76489)
 CONSTANT PSG-BASE
0x7F CONSTANT PSG-PSG_DATA
\ I/O ports
 CONSTANT IO-BASE
0xDC CONSTANT IO-IO_PORT_A
0xDD CONSTANT IO-IO_PORT_B
0xDE CONSTANT IO-IO_PORT_MISC
0xDF CONSTANT IO-IO_PORT_VDP
\ Memory mapper
 CONSTANT MEMORYMAPPER-BASE
0xFFFC CONSTANT MEMORYMAPPER-MAPPER_0
0xFFFD CONSTANT MEMORYMAPPER-MAPPER_1
0xFFFE CONSTANT MEMORYMAPPER-MAPPER_2
0xFFFF CONSTANT MEMORYMAPPER-MAPPER_3
\ FM Sound Unit (optional)
 CONSTANT FMUNIT-BASE
0xF0 CONSTANT FMUNIT-FM_ADDR
0xF1 CONSTANT FMUNIT-FM_DATA
0xF2 CONSTANT FMUNIT-FM_DETECT

\ 中断向量定义
0 CONSTANT INT-RST_00  \ Restart 00h
56 CONSTANT INT-IM1  \ Interrupt Mode 1
56 CONSTANT INT-VBLANK  \ Vertical blank interrupt
100 CONSTANT INT-LINE  \ Line interrupt

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: A@ ( -- n ) A C@ ;
: A! ( n -- ) A C! ;

: F@ ( -- n ) F C@ ;
: F! ( n -- ) F C! ;

: B@ ( -- n ) B C@ ;
: B! ( n -- ) B C! ;

: C@ ( -- n ) C C@ ;
: C! ( n -- ) C C! ;

: D@ ( -- n ) D C@ ;
: D! ( n -- ) D C! ;

: E@ ( -- n ) E C@ ;
: E! ( n -- ) E C! ;

: H@ ( -- n ) H C@ ;
: H! ( n -- ) H C! ;

: L@ ( -- n ) L C@ ;
: L! ( n -- ) L C! ;

: IX@ ( -- n ) IX @ ;
: IX! ( n -- ) IX ! ;

: IY@ ( -- n ) IY @ ;
: IY! ( n -- ) IY ! ;

: SP@ ( -- n ) SP @ ;
: SP! ( n -- ) SP ! ;

: PC@ ( -- n ) PC @ ;
: PC! ( n -- ) PC ! ;

: I@ ( -- n ) I C@ ;
: I! ( n -- ) I C! ;

: R@ ( -- n ) R C@ ;
: R! ( n -- ) R C! ;

\ 外设访问
\ VDP外设
: VDP-VDP_DATA@ ( -- n ) VDP-VDP_DATA C@ ;
: VDP-VDP_DATA! ( n -- ) VDP-VDP_DATA C! ;
: VDP-VDP_ADDR@ ( -- n ) VDP-VDP_ADDR C@ ;
: VDP-VDP_ADDR! ( n -- ) VDP-VDP_ADDR C! ;
: VDP-VDP_STATUS@ ( -- n ) VDP-VDP_STATUS C@ ;
: VDP-VDP_STATUS! ( n -- ) VDP-VDP_STATUS C! ;

\ PSG外设
: PSG-PSG_DATA@ ( -- n ) PSG-PSG_DATA C@ ;
: PSG-PSG_DATA! ( n -- ) PSG-PSG_DATA C! ;

\ IO外设
: IO-IO_PORT_A@ ( -- n ) IO-IO_PORT_A C@ ;
: IO-IO_PORT_A! ( n -- ) IO-IO_PORT_A C! ;
: IO-IO_PORT_B@ ( -- n ) IO-IO_PORT_B C@ ;
: IO-IO_PORT_B! ( n -- ) IO-IO_PORT_B C! ;
: IO-IO_PORT_MISC@ ( -- n ) IO-IO_PORT_MISC C@ ;
: IO-IO_PORT_MISC! ( n -- ) IO-IO_PORT_MISC C! ;
: IO-IO_PORT_VDP@ ( -- n ) IO-IO_PORT_VDP C@ ;
: IO-IO_PORT_VDP! ( n -- ) IO-IO_PORT_VDP C! ;

\ MemoryMapper外设
: MEMORYMAPPER-MAPPER_0@ ( -- n ) MEMORYMAPPER-MAPPER_0 C@ ;
: MEMORYMAPPER-MAPPER_0! ( n -- ) MEMORYMAPPER-MAPPER_0 C! ;
: MEMORYMAPPER-MAPPER_1@ ( -- n ) MEMORYMAPPER-MAPPER_1 C@ ;
: MEMORYMAPPER-MAPPER_1! ( n -- ) MEMORYMAPPER-MAPPER_1 C! ;
: MEMORYMAPPER-MAPPER_2@ ( -- n ) MEMORYMAPPER-MAPPER_2 C@ ;
: MEMORYMAPPER-MAPPER_2! ( n -- ) MEMORYMAPPER-MAPPER_2 C! ;
: MEMORYMAPPER-MAPPER_3@ ( -- n ) MEMORYMAPPER-MAPPER_3 C@ ;
: MEMORYMAPPER-MAPPER_3! ( n -- ) MEMORYMAPPER-MAPPER_3 C! ;

\ FMUnit外设
: FMUNIT-FM_ADDR@ ( -- n ) FMUNIT-FM_ADDR C@ ;
: FMUNIT-FM_ADDR! ( n -- ) FMUNIT-FM_ADDR C! ;
: FMUNIT-FM_DATA@ ( -- n ) FMUNIT-FM_DATA C@ ;
: FMUNIT-FM_DATA! ( n -- ) FMUNIT-FM_DATA C! ;
: FMUNIT-FM_DETECT@ ( -- n ) FMUNIT-FM_DETECT C@ ;
: FMUNIT-FM_DETECT! ( n -- ) FMUNIT-FM_DETECT C! ;

\ =========================================
\ 设备初始化
\ =========================================

: SEGA_MASTER_SYSTEM-INIT ( -- )
  \ 初始化Sega-Master-System设备
  ." 初始化Sega-Master-System..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 F!  \ Flags
  0 B!  \ B
  0 C!  \ C
  0 D!  \ D
  0 E!  \ E
  0 H!  \ H
  0 L!  \ L
  0 IX!  \ Index Register X
  0 IY!  \ Index Register Y
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter
  0 I!  \ Interrupt Vector
  0 R!  \ Memory Refresh

  \ 初始化外设
  \ 初始化VDP
  0 VDP-VDP_DATA!  \ VDP_DATA寄存器
  0 VDP-VDP_ADDR!  \ VDP_ADDR寄存器
  0 VDP-VDP_STATUS!  \ VDP_STATUS寄存器
  \ 初始化PSG
  0 PSG-PSG_DATA!  \ PSG_DATA寄存器
  \ 初始化IO
  0 IO-IO_PORT_A!  \ IO_PORT_A寄存器
  0 IO-IO_PORT_B!  \ IO_PORT_B寄存器
  0 IO-IO_PORT_MISC!  \ IO_PORT_MISC寄存器
  0 IO-IO_PORT_VDP!  \ IO_PORT_VDP寄存器
  \ 初始化MemoryMapper
  0 MEMORYMAPPER-MAPPER_0!  \ MAPPER_0寄存器
  0 MEMORYMAPPER-MAPPER_1!  \ MAPPER_1寄存器
  0 MEMORYMAPPER-MAPPER_2!  \ MAPPER_2寄存器
  0 MEMORYMAPPER-MAPPER_3!  \ MAPPER_3寄存器
  \ 初始化FMUnit
  0 FMUNIT-FM_ADDR!  \ FM_ADDR寄存器
  0 FMUNIT-FM_DATA!  \ FM_DATA寄存器
  0 FMUNIT-FM_DETECT!  \ FM_DETECT寄存器

  ." Sega-Master-System初始化完成" CR
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
  A@ A .R 8 .R SPACE ."  A: " A@ .
  F@ F .R 8 .R SPACE ."  F: " F@ .
  B@ B .R 8 .R SPACE ."  B: " B@ .
  C@ C .R 8 .R SPACE ."  C: " C@ .
  D@ D .R 8 .R SPACE ."  D: " D@ .
  E@ E .R 8 .R SPACE ."  E: " E@ .
  H@ H .R 8 .R SPACE ."  H: " H@ .
  L@ L .R 8 .R SPACE ."  L: " L@ .
  IX@ IX .R 8 .R SPACE ."  IX: " IX@ .
  IY@ IY .R 8 .R SPACE ."  IY: " IY@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  I@ I .R 8 .R SPACE ."  I: " I@ .
  R@ R .R 8 .R SPACE ."  R: " R@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ Restart 00h
: INT-RST_00-HANDLER ( -- )
  ." RST_00中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RST_00-ENABLE ( -- )
  INT-RST_00 INT-ENABLE
;

: INT-RST_00-DISABLE ( -- )
  INT-RST_00 INT-DISABLE
;

\ Interrupt Mode 1
: INT-IM1-HANDLER ( -- )
  ." IM1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IM1-ENABLE ( -- )
  INT-IM1 INT-ENABLE
;

: INT-IM1-DISABLE ( -- )
  INT-IM1 INT-DISABLE
;

\ Vertical blank interrupt
: INT-VBLANK-HANDLER ( -- )
  ." VBLANK中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-VBLANK-ENABLE ( -- )
  INT-VBLANK INT-ENABLE
;

: INT-VBLANK-DISABLE ( -- )
  INT-VBLANK INT-DISABLE
;

\ Line interrupt
: INT-LINE-HANDLER ( -- )
  ." LINE中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-LINE-ENABLE ( -- )
  INT-LINE INT-ENABLE
;

: INT-LINE-DISABLE ( -- )
  INT-LINE INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  SEGA_MASTER_SYSTEM-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

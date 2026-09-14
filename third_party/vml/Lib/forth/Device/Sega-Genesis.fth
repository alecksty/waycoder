\ Sega-Genesis设备定义 - Forth文件
\ 生成自: Sega/Genesis/Mega Drive/Sega-Genesis
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Sega Genesis/Mega Drive 16-bit video game console with Motorola 68000 CPU
\ CPU架构: Motorola 68000
\ 位宽: 32位
\ 时钟频率: 7670000 Hz

\ =========================================
\ Sega-Genesis设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Sega-Genesis" ;
: MANUFACTURER  S" Sega" ;
: FAMILY        S" Genesis/Mega Drive" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Motorola 68000" ;
32 CONSTANT BITS
7670000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0 CONSTANT D0  \ Data Register 0
0 CONSTANT D1  \ Data Register 1
0 CONSTANT D2  \ Data Register 2
0 CONSTANT D3  \ Data Register 3
0 CONSTANT D4  \ Data Register 4
0 CONSTANT D5  \ Data Register 5
0 CONSTANT D6  \ Data Register 6
0 CONSTANT D7  \ Data Register 7
0 CONSTANT A0  \ Address Register 0
0 CONSTANT A1  \ Address Register 1
0 CONSTANT A2  \ Address Register 2
0 CONSTANT A3  \ Address Register 3
0 CONSTANT A4  \ Address Register 4
0 CONSTANT A5  \ Address Register 5
0 CONSTANT A6  \ Address Register 6
0 CONSTANT A7  \ Address Register 7 (SP)
0 CONSTANT PC  \ Program Counter
0 CONSTANT SR  \ Status Register

\ 外设定义
\ Video Display Processor (315-5313)
 CONSTANT VDP-BASE
0xC00000 CONSTANT VDP-VDP_DATA
0xC00004 CONSTANT VDP-VDP_CONTROL
0xC00008 CONSTANT VDP-VDP_HVCOUNTER
0xC00011 CONSTANT VDP-VDP_PSG
\ FM synthesis sound chip
 CONSTANT YM2612-BASE
0xA04000 CONSTANT YM2612-YM2612_ADDR0
0xA04001 CONSTANT YM2612-YM2612_DATA0
0xA04002 CONSTANT YM2612-YM2612_ADDR1
0xA04003 CONSTANT YM2612-YM2612_DATA1
\ I/O ports
 CONSTANT IOPORTS-BASE
0xA10002 CONSTANT IOPORTS-IO_DATA1
0xA10004 CONSTANT IOPORTS-IO_DATA2
0xA10006 CONSTANT IOPORTS-IO_DATA3
0xA10008 CONSTANT IOPORTS-IO_CTRL1
0xA1000A CONSTANT IOPORTS-IO_CTRL2
0xA1000C CONSTANT IOPORTS-IO_CTRL3
\ TradeMark Security System
 CONSTANT TMSS-BASE
0xA14000 CONSTANT TMSS-TMSS
\ Z80 bus control
 CONSTANT Z80BUS-BASE
0xA11100 CONSTANT Z80BUS-Z80_BUSREQ
0xA11200 CONSTANT Z80BUS-Z80_RESET
0xA04000 CONSTANT Z80BUS-Z80_YM2612

\ 中断向量定义
0 CONSTANT INT-RESET_SP  \ Reset (Initial SP)
4 CONSTANT INT-RESET_PC  \ Reset (Initial PC)
24 CONSTANT INT-HBLANK  \ Horizontal blank interrupt
28 CONSTANT INT-VBLANK  \ Vertical blank interrupt
32 CONSTANT INT-EXTINT1  \ External interrupt 1
36 CONSTANT INT-EXTINT2  \ External interrupt 2
40 CONSTANT INT-EXTINT3  \ External interrupt 3
44 CONSTANT INT-EXTINT4  \ External interrupt 4
48 CONSTANT INT-EXTINT5  \ External interrupt 5
52 CONSTANT INT-EXTINT6  \ External interrupt 6
56 CONSTANT INT-EXTINT7  \ External interrupt 7

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: D0@ ( -- n ) D0 L@ ;
: D0! ( n -- ) D0 L! ;

: D1@ ( -- n ) D1 L@ ;
: D1! ( n -- ) D1 L! ;

: D2@ ( -- n ) D2 L@ ;
: D2! ( n -- ) D2 L! ;

: D3@ ( -- n ) D3 L@ ;
: D3! ( n -- ) D3 L! ;

: D4@ ( -- n ) D4 L@ ;
: D4! ( n -- ) D4 L! ;

: D5@ ( -- n ) D5 L@ ;
: D5! ( n -- ) D5 L! ;

: D6@ ( -- n ) D6 L@ ;
: D6! ( n -- ) D6 L! ;

: D7@ ( -- n ) D7 L@ ;
: D7! ( n -- ) D7 L! ;

: A0@ ( -- n ) A0 L@ ;
: A0! ( n -- ) A0 L! ;

: A1@ ( -- n ) A1 L@ ;
: A1! ( n -- ) A1 L! ;

: A2@ ( -- n ) A2 L@ ;
: A2! ( n -- ) A2 L! ;

: A3@ ( -- n ) A3 L@ ;
: A3! ( n -- ) A3 L! ;

: A4@ ( -- n ) A4 L@ ;
: A4! ( n -- ) A4 L! ;

: A5@ ( -- n ) A5 L@ ;
: A5! ( n -- ) A5 L! ;

: A6@ ( -- n ) A6 L@ ;
: A6! ( n -- ) A6 L! ;

: A7@ ( -- n ) A7 L@ ;
: A7! ( n -- ) A7 L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

: SR@ ( -- n ) SR @ ;
: SR! ( n -- ) SR ! ;

\ 外设访问
\ VDP外设
: VDP-VDP_DATA@ ( -- n ) VDP-VDP_DATA @ ;
: VDP-VDP_DATA! ( n -- ) VDP-VDP_DATA ! ;
: VDP-VDP_CONTROL@ ( -- n ) VDP-VDP_CONTROL @ ;
: VDP-VDP_CONTROL! ( n -- ) VDP-VDP_CONTROL ! ;
: VDP-VDP_HVCOUNTER@ ( -- n ) VDP-VDP_HVCOUNTER @ ;
: VDP-VDP_HVCOUNTER! ( n -- ) VDP-VDP_HVCOUNTER ! ;
: VDP-VDP_PSG@ ( -- n ) VDP-VDP_PSG C@ ;
: VDP-VDP_PSG! ( n -- ) VDP-VDP_PSG C! ;

\ YM2612外设
: YM2612-YM2612_ADDR0@ ( -- n ) YM2612-YM2612_ADDR0 C@ ;
: YM2612-YM2612_ADDR0! ( n -- ) YM2612-YM2612_ADDR0 C! ;
: YM2612-YM2612_DATA0@ ( -- n ) YM2612-YM2612_DATA0 C@ ;
: YM2612-YM2612_DATA0! ( n -- ) YM2612-YM2612_DATA0 C! ;
: YM2612-YM2612_ADDR1@ ( -- n ) YM2612-YM2612_ADDR1 C@ ;
: YM2612-YM2612_ADDR1! ( n -- ) YM2612-YM2612_ADDR1 C! ;
: YM2612-YM2612_DATA1@ ( -- n ) YM2612-YM2612_DATA1 C@ ;
: YM2612-YM2612_DATA1! ( n -- ) YM2612-YM2612_DATA1 C! ;

\ IOPorts外设
: IOPORTS-IO_DATA1@ ( -- n ) IOPORTS-IO_DATA1 C@ ;
: IOPORTS-IO_DATA1! ( n -- ) IOPORTS-IO_DATA1 C! ;
: IOPORTS-IO_DATA2@ ( -- n ) IOPORTS-IO_DATA2 C@ ;
: IOPORTS-IO_DATA2! ( n -- ) IOPORTS-IO_DATA2 C! ;
: IOPORTS-IO_DATA3@ ( -- n ) IOPORTS-IO_DATA3 C@ ;
: IOPORTS-IO_DATA3! ( n -- ) IOPORTS-IO_DATA3 C! ;
: IOPORTS-IO_CTRL1@ ( -- n ) IOPORTS-IO_CTRL1 C@ ;
: IOPORTS-IO_CTRL1! ( n -- ) IOPORTS-IO_CTRL1 C! ;
: IOPORTS-IO_CTRL2@ ( -- n ) IOPORTS-IO_CTRL2 C@ ;
: IOPORTS-IO_CTRL2! ( n -- ) IOPORTS-IO_CTRL2 C! ;
: IOPORTS-IO_CTRL3@ ( -- n ) IOPORTS-IO_CTRL3 C@ ;
: IOPORTS-IO_CTRL3! ( n -- ) IOPORTS-IO_CTRL3 C! ;

\ TMSS外设
: TMSS-TMSS@ ( -- n ) TMSS-TMSS C@ ;
: TMSS-TMSS! ( n -- ) TMSS-TMSS C! ;

\ Z80Bus外设
: Z80BUS-Z80_BUSREQ@ ( -- n ) Z80BUS-Z80_BUSREQ @ ;
: Z80BUS-Z80_BUSREQ! ( n -- ) Z80BUS-Z80_BUSREQ ! ;
: Z80BUS-Z80_RESET@ ( -- n ) Z80BUS-Z80_RESET @ ;
: Z80BUS-Z80_RESET! ( n -- ) Z80BUS-Z80_RESET ! ;
: Z80BUS-Z80_YM2612@ ( -- n ) Z80BUS-Z80_YM2612 L@ ;
: Z80BUS-Z80_YM2612! ( n -- ) Z80BUS-Z80_YM2612 L! ;

\ =========================================
\ 设备初始化
\ =========================================

: SEGA_GENESIS-INIT ( -- )
  \ 初始化Sega-Genesis设备
  ." 初始化Sega-Genesis..." CR

  \ 初始化寄存器
  0 D0!  \ Data Register 0
  0 D1!  \ Data Register 1
  0 D2!  \ Data Register 2
  0 D3!  \ Data Register 3
  0 D4!  \ Data Register 4
  0 D5!  \ Data Register 5
  0 D6!  \ Data Register 6
  0 D7!  \ Data Register 7
  0 A0!  \ Address Register 0
  0 A1!  \ Address Register 1
  0 A2!  \ Address Register 2
  0 A3!  \ Address Register 3
  0 A4!  \ Address Register 4
  0 A5!  \ Address Register 5
  0 A6!  \ Address Register 6
  0 A7!  \ Address Register 7 (SP)
  0 PC!  \ Program Counter
  0 SR!  \ Status Register

  \ 初始化外设
  \ 初始化VDP
  0 VDP-VDP_DATA!  \ VDP_DATA寄存器
  0 VDP-VDP_CONTROL!  \ VDP_CONTROL寄存器
  0 VDP-VDP_HVCOUNTER!  \ VDP_HVCOUNTER寄存器
  0 VDP-VDP_PSG!  \ VDP_PSG寄存器
  \ 初始化YM2612
  0 YM2612-YM2612_ADDR0!  \ YM2612_ADDR0寄存器
  0 YM2612-YM2612_DATA0!  \ YM2612_DATA0寄存器
  0 YM2612-YM2612_ADDR1!  \ YM2612_ADDR1寄存器
  0 YM2612-YM2612_DATA1!  \ YM2612_DATA1寄存器
  \ 初始化IOPorts
  0 IOPORTS-IO_DATA1!  \ IO_DATA1寄存器
  0 IOPORTS-IO_DATA2!  \ IO_DATA2寄存器
  0 IOPORTS-IO_DATA3!  \ IO_DATA3寄存器
  0 IOPORTS-IO_CTRL1!  \ IO_CTRL1寄存器
  0 IOPORTS-IO_CTRL2!  \ IO_CTRL2寄存器
  0 IOPORTS-IO_CTRL3!  \ IO_CTRL3寄存器
  \ 初始化TMSS
  0 TMSS-TMSS!  \ TMSS寄存器
  \ 初始化Z80Bus
  0 Z80BUS-Z80_BUSREQ!  \ Z80_BUSREQ寄存器
  0 Z80BUS-Z80_RESET!  \ Z80_RESET寄存器
  0 Z80BUS-Z80_YM2612!  \ Z80_YM2612寄存器

  ." Sega-Genesis初始化完成" CR
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
  D0@ D0 .R 8 .R SPACE ."  D0: " D0@ .
  D1@ D1 .R 8 .R SPACE ."  D1: " D1@ .
  D2@ D2 .R 8 .R SPACE ."  D2: " D2@ .
  D3@ D3 .R 8 .R SPACE ."  D3: " D3@ .
  D4@ D4 .R 8 .R SPACE ."  D4: " D4@ .
  D5@ D5 .R 8 .R SPACE ."  D5: " D5@ .
  D6@ D6 .R 8 .R SPACE ."  D6: " D6@ .
  D7@ D7 .R 8 .R SPACE ."  D7: " D7@ .
  A0@ A0 .R 8 .R SPACE ."  A0: " A0@ .
  A1@ A1 .R 8 .R SPACE ."  A1: " A1@ .
  A2@ A2 .R 8 .R SPACE ."  A2: " A2@ .
  A3@ A3 .R 8 .R SPACE ."  A3: " A3@ .
  A4@ A4 .R 8 .R SPACE ."  A4: " A4@ .
  A5@ A5 .R 8 .R SPACE ."  A5: " A5@ .
  A6@ A6 .R 8 .R SPACE ."  A6: " A6@ .
  A7@ A7 .R 8 .R SPACE ."  A7: " A7@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  SR@ SR .R 8 .R SPACE ."  SR: " SR@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ Reset (Initial SP)
: INT-RESET_SP-HANDLER ( -- )
  ." RESET_SP中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET_SP-ENABLE ( -- )
  INT-RESET_SP INT-ENABLE
;

: INT-RESET_SP-DISABLE ( -- )
  INT-RESET_SP INT-DISABLE
;

\ Reset (Initial PC)
: INT-RESET_PC-HANDLER ( -- )
  ." RESET_PC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET_PC-ENABLE ( -- )
  INT-RESET_PC INT-ENABLE
;

: INT-RESET_PC-DISABLE ( -- )
  INT-RESET_PC INT-DISABLE
;

\ Horizontal blank interrupt
: INT-HBLANK-HANDLER ( -- )
  ." HBLANK中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-HBLANK-ENABLE ( -- )
  INT-HBLANK INT-ENABLE
;

: INT-HBLANK-DISABLE ( -- )
  INT-HBLANK INT-DISABLE
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

\ External interrupt 1
: INT-EXTINT1-HANDLER ( -- )
  ." EXTINT1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTINT1-ENABLE ( -- )
  INT-EXTINT1 INT-ENABLE
;

: INT-EXTINT1-DISABLE ( -- )
  INT-EXTINT1 INT-DISABLE
;

\ External interrupt 2
: INT-EXTINT2-HANDLER ( -- )
  ." EXTINT2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTINT2-ENABLE ( -- )
  INT-EXTINT2 INT-ENABLE
;

: INT-EXTINT2-DISABLE ( -- )
  INT-EXTINT2 INT-DISABLE
;

\ External interrupt 3
: INT-EXTINT3-HANDLER ( -- )
  ." EXTINT3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTINT3-ENABLE ( -- )
  INT-EXTINT3 INT-ENABLE
;

: INT-EXTINT3-DISABLE ( -- )
  INT-EXTINT3 INT-DISABLE
;

\ External interrupt 4
: INT-EXTINT4-HANDLER ( -- )
  ." EXTINT4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTINT4-ENABLE ( -- )
  INT-EXTINT4 INT-ENABLE
;

: INT-EXTINT4-DISABLE ( -- )
  INT-EXTINT4 INT-DISABLE
;

\ External interrupt 5
: INT-EXTINT5-HANDLER ( -- )
  ." EXTINT5中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTINT5-ENABLE ( -- )
  INT-EXTINT5 INT-ENABLE
;

: INT-EXTINT5-DISABLE ( -- )
  INT-EXTINT5 INT-DISABLE
;

\ External interrupt 6
: INT-EXTINT6-HANDLER ( -- )
  ." EXTINT6中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTINT6-ENABLE ( -- )
  INT-EXTINT6 INT-ENABLE
;

: INT-EXTINT6-DISABLE ( -- )
  INT-EXTINT6 INT-DISABLE
;

\ External interrupt 7
: INT-EXTINT7-HANDLER ( -- )
  ." EXTINT7中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-EXTINT7-ENABLE ( -- )
  INT-EXTINT7 INT-ENABLE
;

: INT-EXTINT7-DISABLE ( -- )
  INT-EXTINT7 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  SEGA_GENESIS-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

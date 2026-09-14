\ ILI9341设备定义 - Forth文件
\ 生成自: Ilitek/Display/ILI9341
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: ILI9341 2.8" 240x320 TFT LCD Display (SPI, 18-bit color, touch)
\ CPU架构: Display
\ 位宽: 18位
\ 时钟频率: 20000000 Hz

\ =========================================
\ ILI9341设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ILI9341" ;
: MANUFACTURER  S" Ilitek" ;
: FAMILY        S" Display" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Display" ;
18 CONSTANT BITS
20000000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT GRAM-START
0xBCFF CONSTANT GRAM-END
156672 CONSTANT GRAM-SIZE  \ Graphics RAM (240x320x18bit)

\ 外设定义
\ ILI9341 240x320 TFT (SPI, 3.3V, 2.8inch)
0x00 CONSTANT ILI9341-BASE
0x00 CONSTANT ILI9341-CMD
0x01 CONSTANT ILI9341-DATA
0x2A CONSTANT ILI9341-COL_START
0x2B CONSTANT ILI9341-PAGE_START
0x2C CONSTANT ILI9341-WRITE_RAM
0x36 CONSTANT ILI9341-MADCTL
0x3A CONSTANT ILI9341-PIXFMT
0xB1 CONSTANT ILI9341-FRMCTL
0x26 CONSTANT ILI9341-GAMMA
0x11 CONSTANT ILI9341-SLEEP_OUT
0x29 CONSTANT ILI9341-DISP_ON

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ ILI9341外设
: ILI9341-CMD@ ( -- n ) ILI9341-CMD C@ ;
: ILI9341-CMD! ( n -- ) ILI9341-CMD C! ;
: ILI9341-DATA@ ( -- n ) ILI9341-DATA C@ ;
: ILI9341-DATA! ( n -- ) ILI9341-DATA C! ;
: ILI9341-COL_START@ ( -- n ) ILI9341-COL_START @ ;
: ILI9341-COL_START! ( n -- ) ILI9341-COL_START ! ;
: ILI9341-PAGE_START@ ( -- n ) ILI9341-PAGE_START @ ;
: ILI9341-PAGE_START! ( n -- ) ILI9341-PAGE_START ! ;
: ILI9341-WRITE_RAM@ ( -- n ) ILI9341-WRITE_RAM @ ;
: ILI9341-WRITE_RAM! ( n -- ) ILI9341-WRITE_RAM ! ;
: ILI9341-MADCTL@ ( -- n ) ILI9341-MADCTL C@ ;
: ILI9341-MADCTL! ( n -- ) ILI9341-MADCTL C! ;
: ILI9341-PIXFMT@ ( -- n ) ILI9341-PIXFMT C@ ;
: ILI9341-PIXFMT! ( n -- ) ILI9341-PIXFMT C! ;
: ILI9341-FRMCTL@ ( -- n ) ILI9341-FRMCTL @ ;
: ILI9341-FRMCTL! ( n -- ) ILI9341-FRMCTL ! ;
: ILI9341-GAMMA@ ( -- n ) ILI9341-GAMMA C@ ;
: ILI9341-GAMMA! ( n -- ) ILI9341-GAMMA C! ;
: ILI9341-SLEEP_OUT@ ( -- n ) ILI9341-SLEEP_OUT  0 CHARS@ ;
: ILI9341-SLEEP_OUT! ( n -- ) ILI9341-SLEEP_OUT  0 CHARS! ;
: ILI9341-DISP_ON@ ( -- n ) ILI9341-DISP_ON  0 CHARS@ ;
: ILI9341-DISP_ON! ( n -- ) ILI9341-DISP_ON  0 CHARS! ;

\ =========================================
\ 设备初始化
\ =========================================

: ILI9341-INIT ( -- )
  \ 初始化ILI9341设备
  ." 初始化ILI9341..." CR


  \ 初始化外设
  \ 初始化ILI9341
  0 ILI9341-CMD!  \ CMD寄存器
  0 ILI9341-DATA!  \ DATA寄存器
  0 ILI9341-COL_START!  \ COL_START寄存器
  0 ILI9341-PAGE_START!  \ PAGE_START寄存器
  0 ILI9341-WRITE_RAM!  \ WRITE_RAM寄存器
  0 ILI9341-MADCTL!  \ MADCTL寄存器
  0 ILI9341-PIXFMT!  \ PIXFMT寄存器
  0 ILI9341-FRMCTL!  \ FRMCTL寄存器
  0 ILI9341-GAMMA!  \ GAMMA寄存器
  0 ILI9341-SLEEP_OUT!  \ SLEEP_OUT寄存器
  0 ILI9341-DISP_ON!  \ DISP_ON寄存器

  ." ILI9341初始化完成" CR
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

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  ILI9341-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

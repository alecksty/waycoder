\ ST7735设备定义 - Forth文件
\ 生成自: Sitronix/Display/ST7735
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: ST7735 1.8" 128x160 TFT LCD Display (SPI, 16-bit color)
\ CPU架构: Display
\ 位宽: 16位
\ 时钟频率: 16000000 Hz

\ =========================================
\ ST7735设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ST7735" ;
: MANUFACTURER  S" Sitronix" ;
: FAMILY        S" Display" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Display" ;
16 CONSTANT BITS
16000000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT GRAM-START
0x4FFF CONSTANT GRAM-END
20480 CONSTANT GRAM-SIZE  \ Graphics RAM (128x160x16bit)

\ 外设定义
\ ST7735 128x160 TFT (SPI, 3.3V-5V)
0x00 CONSTANT ST7735-BASE
0x00 CONSTANT ST7735-CMD
0x01 CONSTANT ST7735-DATA
0x2A CONSTANT ST7735-COL_START
0x2B CONSTANT ST7735-ROW_START
0x2C CONSTANT ST7735-WRITE_RAM
0x36 CONSTANT ST7735-MADCTL
0x3A CONSTANT ST7735-COLMOD
0x21 CONSTANT ST7735-INVON
0x11 CONSTANT ST7735-SLEEP_OUT
0x29 CONSTANT ST7735-DISP_ON

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ ST7735外设
: ST7735-CMD@ ( -- n ) ST7735-CMD C@ ;
: ST7735-CMD! ( n -- ) ST7735-CMD C! ;
: ST7735-DATA@ ( -- n ) ST7735-DATA C@ ;
: ST7735-DATA! ( n -- ) ST7735-DATA C! ;
: ST7735-COL_START@ ( -- n ) ST7735-COL_START @ ;
: ST7735-COL_START! ( n -- ) ST7735-COL_START ! ;
: ST7735-ROW_START@ ( -- n ) ST7735-ROW_START @ ;
: ST7735-ROW_START! ( n -- ) ST7735-ROW_START ! ;
: ST7735-WRITE_RAM@ ( -- n ) ST7735-WRITE_RAM @ ;
: ST7735-WRITE_RAM! ( n -- ) ST7735-WRITE_RAM ! ;
: ST7735-MADCTL@ ( -- n ) ST7735-MADCTL C@ ;
: ST7735-MADCTL! ( n -- ) ST7735-MADCTL C! ;
: ST7735-COLMOD@ ( -- n ) ST7735-COLMOD C@ ;
: ST7735-COLMOD! ( n -- ) ST7735-COLMOD C! ;
: ST7735-INVON@ ( -- n ) ST7735-INVON  0 CHARS@ ;
: ST7735-INVON! ( n -- ) ST7735-INVON  0 CHARS! ;
: ST7735-SLEEP_OUT@ ( -- n ) ST7735-SLEEP_OUT  0 CHARS@ ;
: ST7735-SLEEP_OUT! ( n -- ) ST7735-SLEEP_OUT  0 CHARS! ;
: ST7735-DISP_ON@ ( -- n ) ST7735-DISP_ON  0 CHARS@ ;
: ST7735-DISP_ON! ( n -- ) ST7735-DISP_ON  0 CHARS! ;

\ =========================================
\ 设备初始化
\ =========================================

: ST7735-INIT ( -- )
  \ 初始化ST7735设备
  ." 初始化ST7735..." CR


  \ 初始化外设
  \ 初始化ST7735
  0 ST7735-CMD!  \ CMD寄存器
  0 ST7735-DATA!  \ DATA寄存器
  0 ST7735-COL_START!  \ COL_START寄存器
  0 ST7735-ROW_START!  \ ROW_START寄存器
  0 ST7735-WRITE_RAM!  \ WRITE_RAM寄存器
  0 ST7735-MADCTL!  \ MADCTL寄存器
  0 ST7735-COLMOD!  \ COLMOD寄存器
  0 ST7735-INVON!  \ INVON寄存器
  0 ST7735-SLEEP_OUT!  \ SLEEP_OUT寄存器
  0 ST7735-DISP_ON!  \ DISP_ON寄存器

  ." ST7735初始化完成" CR
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
  ST7735-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

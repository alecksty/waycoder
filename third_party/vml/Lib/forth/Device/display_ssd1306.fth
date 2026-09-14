\ SSD1306设备定义 - Forth文件
\ 生成自: Solomon Systech/Display/SSD1306
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: SSD1306 128x64 OLED Display Controller (I2C/SPI)
\ CPU架构: Display
\ 位宽: 8位
\ 时钟频率: 400000 Hz

\ =========================================
\ SSD1306设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" SSD1306" ;
: MANUFACTURER  S" Solomon Systech" ;
: FAMILY        S" Display" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Display" ;
8 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT GDDRAM-START
0x3FF CONSTANT GDDRAM-END
1024 CONSTANT GDDRAM-SIZE  \ Graphic Display Data RAM (128x64 = 1024 bytes)

\ 外设定义
\ SSD1306 128x64 OLED (0x3C/0x3D I2C, 3.3V-5V)
0x3C CONSTANT SSD1306-BASE
0x00 CONSTANT SSD1306-CMD
0x40 CONSTANT SSD1306-DATA
0xAE CONSTANT SSD1306-DISPLAY_OFF
0xAF CONSTANT SSD1306-DISPLAY_ON
0x81 CONSTANT SSD1306-CONTRAST
0xA1 CONSTANT SSD1306-SEG_REMAP
0xC8 CONSTANT SSD1306-COM_SCAN
0x20 CONSTANT SSD1306-ADDR_MODE
0x21 CONSTANT SSD1306-COL_START
0x22 CONSTANT SSD1306-PAGE_START

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ SSD1306外设
: SSD1306-CMD@ ( -- n ) SSD1306-CMD C@ ;
: SSD1306-CMD! ( n -- ) SSD1306-CMD C! ;
: SSD1306-DATA@ ( -- n ) SSD1306-DATA C@ ;
: SSD1306-DATA! ( n -- ) SSD1306-DATA C! ;
: SSD1306-DISPLAY_OFF@ ( -- n ) SSD1306-DISPLAY_OFF C@ ;
: SSD1306-DISPLAY_OFF! ( n -- ) SSD1306-DISPLAY_OFF C! ;
: SSD1306-DISPLAY_ON@ ( -- n ) SSD1306-DISPLAY_ON C@ ;
: SSD1306-DISPLAY_ON! ( n -- ) SSD1306-DISPLAY_ON C! ;
: SSD1306-CONTRAST@ ( -- n ) SSD1306-CONTRAST C@ ;
: SSD1306-CONTRAST! ( n -- ) SSD1306-CONTRAST C! ;
: SSD1306-SEG_REMAP@ ( -- n ) SSD1306-SEG_REMAP C@ ;
: SSD1306-SEG_REMAP! ( n -- ) SSD1306-SEG_REMAP C! ;
: SSD1306-COM_SCAN@ ( -- n ) SSD1306-COM_SCAN C@ ;
: SSD1306-COM_SCAN! ( n -- ) SSD1306-COM_SCAN C! ;
: SSD1306-ADDR_MODE@ ( -- n ) SSD1306-ADDR_MODE C@ ;
: SSD1306-ADDR_MODE! ( n -- ) SSD1306-ADDR_MODE C! ;
: SSD1306-COL_START@ ( -- n ) SSD1306-COL_START C@ ;
: SSD1306-COL_START! ( n -- ) SSD1306-COL_START C! ;
: SSD1306-PAGE_START@ ( -- n ) SSD1306-PAGE_START C@ ;
: SSD1306-PAGE_START! ( n -- ) SSD1306-PAGE_START C! ;

\ =========================================
\ 设备初始化
\ =========================================

: SSD1306-INIT ( -- )
  \ 初始化SSD1306设备
  ." 初始化SSD1306..." CR


  \ 初始化外设
  \ 初始化SSD1306
  0 SSD1306-CMD!  \ CMD寄存器
  0 SSD1306-DATA!  \ DATA寄存器
  0 SSD1306-DISPLAY_OFF!  \ DISPLAY_OFF寄存器
  0 SSD1306-DISPLAY_ON!  \ DISPLAY_ON寄存器
  0 SSD1306-CONTRAST!  \ CONTRAST寄存器
  0 SSD1306-SEG_REMAP!  \ SEG_REMAP寄存器
  0 SSD1306-COM_SCAN!  \ COM_SCAN寄存器
  0 SSD1306-ADDR_MODE!  \ ADDR_MODE寄存器
  0 SSD1306-COL_START!  \ COL_START寄存器
  0 SSD1306-PAGE_START!  \ PAGE_START寄存器

  ." SSD1306初始化完成" CR
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
  SSD1306-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

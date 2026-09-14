\ NEO6M设备定义 - Forth文件
\ 生成自: u-blox/GPS/NEO6M
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: NEO-6M GPS Module (UART, 50-channel, -162dBm tracking)
\ CPU架构: GPS
\ 位宽: 8位
\ 时钟频率: 9600 Hz

\ =========================================
\ NEO6M设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" NEO6M" ;
: MANUFACTURER  S" u-blox" ;
: FAMILY        S" GPS" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" GPS" ;
8 CONSTANT BITS
9600 CONSTANT CLOCK-FREQ

\ 外设定义
\ NEO-6M GPS Module (UART 9600bps, 3.3V-5V)
0x00 CONSTANT NEO6M-BASE
0x00 CONSTANT NEO6M-LATITUDE
0x04 CONSTANT NEO6M-LONGITUDE
0x08 CONSTANT NEO6M-ALTITUDE
0x0C CONSTANT NEO6M-SPEED
0x0E CONSTANT NEO6M-HEADING
0x10 CONSTANT NEO6M-SATELLITES
0x11 CONSTANT NEO6M-HDOP
0x13 CONSTANT NEO6M-FIX_TYPE
0x14 CONSTANT NEO6M-DATE
0x18 CONSTANT NEO6M-TIME
0x1C CONSTANT NEO6M-VALID

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ NEO6M外设
: NEO6M-LATITUDE@ ( -- n ) NEO6M-LATITUDE L@ ;
: NEO6M-LATITUDE! ( n -- ) NEO6M-LATITUDE L! ;
: NEO6M-LONGITUDE@ ( -- n ) NEO6M-LONGITUDE L@ ;
: NEO6M-LONGITUDE! ( n -- ) NEO6M-LONGITUDE L! ;
: NEO6M-ALTITUDE@ ( -- n ) NEO6M-ALTITUDE L@ ;
: NEO6M-ALTITUDE! ( n -- ) NEO6M-ALTITUDE L! ;
: NEO6M-SPEED@ ( -- n ) NEO6M-SPEED @ ;
: NEO6M-SPEED! ( n -- ) NEO6M-SPEED ! ;
: NEO6M-HEADING@ ( -- n ) NEO6M-HEADING @ ;
: NEO6M-HEADING! ( n -- ) NEO6M-HEADING ! ;
: NEO6M-SATELLITES@ ( -- n ) NEO6M-SATELLITES C@ ;
: NEO6M-SATELLITES! ( n -- ) NEO6M-SATELLITES C! ;
: NEO6M-HDOP@ ( -- n ) NEO6M-HDOP @ ;
: NEO6M-HDOP! ( n -- ) NEO6M-HDOP ! ;
: NEO6M-FIX_TYPE@ ( -- n ) NEO6M-FIX_TYPE C@ ;
: NEO6M-FIX_TYPE! ( n -- ) NEO6M-FIX_TYPE C! ;
: NEO6M-DATE@ ( -- n ) NEO6M-DATE L@ ;
: NEO6M-DATE! ( n -- ) NEO6M-DATE L! ;
: NEO6M-TIME@ ( -- n ) NEO6M-TIME L@ ;
: NEO6M-TIME! ( n -- ) NEO6M-TIME L! ;
: NEO6M-VALID@ ( -- n ) NEO6M-VALID C@ ;
: NEO6M-VALID! ( n -- ) NEO6M-VALID C! ;

\ =========================================
\ 设备初始化
\ =========================================

: NEO6M-INIT ( -- )
  \ 初始化NEO6M设备
  ." 初始化NEO6M..." CR


  \ 初始化外设
  \ 初始化NEO6M
  0 NEO6M-LATITUDE!  \ LATITUDE寄存器
  0 NEO6M-LONGITUDE!  \ LONGITUDE寄存器
  0 NEO6M-ALTITUDE!  \ ALTITUDE寄存器
  0 NEO6M-SPEED!  \ SPEED寄存器
  0 NEO6M-HEADING!  \ HEADING寄存器
  0 NEO6M-SATELLITES!  \ SATELLITES寄存器
  0 NEO6M-HDOP!  \ HDOP寄存器
  0 NEO6M-FIX_TYPE!  \ FIX_TYPE寄存器
  0 NEO6M-DATE!  \ DATE寄存器
  0 NEO6M-TIME!  \ TIME寄存器
  0 NEO6M-VALID!  \ VALID寄存器

  ." NEO6M初始化完成" CR
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
  NEO6M-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

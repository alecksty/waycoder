\ MAX7219设备定义 - Forth文件
\ 生成自: Maxim/LED/MAX7219
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: MAX7219 8-Digit LED Display Driver (SPI, daisy-chainable, 8x8 matrix)
\ CPU架构: LED
\ 位宽: 8位
\ 时钟频率: 10000000 Hz

\ =========================================
\ MAX7219设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" MAX7219" ;
: MANUFACTURER  S" Maxim" ;
: FAMILY        S" LED" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" LED" ;
8 CONSTANT BITS
10000000 CONSTANT CLOCK-FREQ

\ 外设定义
\ MAX7219 8-Digit/8x8 Matrix Driver (4.0V-5.5V, DIP-24)
0x00 CONSTANT MAX7219-BASE
0x01 CONSTANT MAX7219-DIGIT0
0x02 CONSTANT MAX7219-DIGIT1
0x03 CONSTANT MAX7219-DIGIT2
0x04 CONSTANT MAX7219-DIGIT3
0x05 CONSTANT MAX7219-DIGIT4
0x06 CONSTANT MAX7219-DIGIT5
0x07 CONSTANT MAX7219-DIGIT6
0x08 CONSTANT MAX7219-DIGIT7
0x09 CONSTANT MAX7219-DECODE
0x0A CONSTANT MAX7219-INTENSITY
0x0B CONSTANT MAX7219-SCAN_LIMIT
0x0C CONSTANT MAX7219-SHUTDOWN
0x0F CONSTANT MAX7219-TEST

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ MAX7219外设
: MAX7219-DIGIT0@ ( -- n ) MAX7219-DIGIT0 C@ ;
: MAX7219-DIGIT0! ( n -- ) MAX7219-DIGIT0 C! ;
: MAX7219-DIGIT1@ ( -- n ) MAX7219-DIGIT1 C@ ;
: MAX7219-DIGIT1! ( n -- ) MAX7219-DIGIT1 C! ;
: MAX7219-DIGIT2@ ( -- n ) MAX7219-DIGIT2 C@ ;
: MAX7219-DIGIT2! ( n -- ) MAX7219-DIGIT2 C! ;
: MAX7219-DIGIT3@ ( -- n ) MAX7219-DIGIT3 C@ ;
: MAX7219-DIGIT3! ( n -- ) MAX7219-DIGIT3 C! ;
: MAX7219-DIGIT4@ ( -- n ) MAX7219-DIGIT4 C@ ;
: MAX7219-DIGIT4! ( n -- ) MAX7219-DIGIT4 C! ;
: MAX7219-DIGIT5@ ( -- n ) MAX7219-DIGIT5 C@ ;
: MAX7219-DIGIT5! ( n -- ) MAX7219-DIGIT5 C! ;
: MAX7219-DIGIT6@ ( -- n ) MAX7219-DIGIT6 C@ ;
: MAX7219-DIGIT6! ( n -- ) MAX7219-DIGIT6 C! ;
: MAX7219-DIGIT7@ ( -- n ) MAX7219-DIGIT7 C@ ;
: MAX7219-DIGIT7! ( n -- ) MAX7219-DIGIT7 C! ;
: MAX7219-DECODE@ ( -- n ) MAX7219-DECODE C@ ;
: MAX7219-DECODE! ( n -- ) MAX7219-DECODE C! ;
: MAX7219-INTENSITY@ ( -- n ) MAX7219-INTENSITY C@ ;
: MAX7219-INTENSITY! ( n -- ) MAX7219-INTENSITY C! ;
: MAX7219-SCAN_LIMIT@ ( -- n ) MAX7219-SCAN_LIMIT C@ ;
: MAX7219-SCAN_LIMIT! ( n -- ) MAX7219-SCAN_LIMIT C! ;
: MAX7219-SHUTDOWN@ ( -- n ) MAX7219-SHUTDOWN C@ ;
: MAX7219-SHUTDOWN! ( n -- ) MAX7219-SHUTDOWN C! ;
: MAX7219-TEST@ ( -- n ) MAX7219-TEST C@ ;
: MAX7219-TEST! ( n -- ) MAX7219-TEST C! ;

\ =========================================
\ 设备初始化
\ =========================================

: MAX7219-INIT ( -- )
  \ 初始化MAX7219设备
  ." 初始化MAX7219..." CR


  \ 初始化外设
  \ 初始化MAX7219
  0 MAX7219-DIGIT0!  \ DIGIT0寄存器
  0 MAX7219-DIGIT1!  \ DIGIT1寄存器
  0 MAX7219-DIGIT2!  \ DIGIT2寄存器
  0 MAX7219-DIGIT3!  \ DIGIT3寄存器
  0 MAX7219-DIGIT4!  \ DIGIT4寄存器
  0 MAX7219-DIGIT5!  \ DIGIT5寄存器
  0 MAX7219-DIGIT6!  \ DIGIT6寄存器
  0 MAX7219-DIGIT7!  \ DIGIT7寄存器
  0 MAX7219-DECODE!  \ DECODE寄存器
  0 MAX7219-INTENSITY!  \ INTENSITY寄存器
  0 MAX7219-SCAN_LIMIT!  \ SCAN_LIMIT寄存器
  0 MAX7219-SHUTDOWN!  \ SHUTDOWN寄存器
  0 MAX7219-TEST!  \ TEST寄存器

  ." MAX7219初始化完成" CR
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
  MAX7219-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

\ 74HC595设备定义 - Forth文件
\ 生成自: TI/NXP/GPIO/74HC595
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: 74HC595 8-bit Shift Register (SPI-compatible, serial-in parallel-out, daisy-chainable)
\ CPU架构: GPIO
\ 位宽: 8位
\ 时钟频率: 10000000 Hz

\ =========================================
\ 74HC595设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" 74HC595" ;
: MANUFACTURER  S" TI/NXP" ;
: FAMILY        S" GPIO" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" GPIO" ;
8 CONSTANT BITS
10000000 CONSTANT CLOCK-FREQ

\ 外设定义
\ 74HC595 8-bit Shift Register (2V-6V, DIP-16)
0x00 CONSTANT _74HC595-BASE
0x00 CONSTANT _74HC595-DATA
0x01 CONSTANT _74HC595-LATCH
0x02 CONSTANT _74HC595-CHAIN_COUNT
0x03 CONSTANT _74HC595-OE
0x04 CONSTANT _74HC595-CLEAR

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ 74HC595外设
: _74HC595-DATA@ ( -- n ) _74HC595-DATA C@ ;
: _74HC595-DATA! ( n -- ) _74HC595-DATA C! ;
: _74HC595-LATCH@ ( -- n ) _74HC595-LATCH C@ ;
: _74HC595-LATCH! ( n -- ) _74HC595-LATCH C! ;
: _74HC595-CHAIN_COUNT@ ( -- n ) _74HC595-CHAIN_COUNT C@ ;
: _74HC595-CHAIN_COUNT! ( n -- ) _74HC595-CHAIN_COUNT C! ;
: _74HC595-OE@ ( -- n ) _74HC595-OE C@ ;
: _74HC595-OE! ( n -- ) _74HC595-OE C! ;
: _74HC595-CLEAR@ ( -- n ) _74HC595-CLEAR C@ ;
: _74HC595-CLEAR! ( n -- ) _74HC595-CLEAR C! ;

\ =========================================
\ 设备初始化
\ =========================================

: _74HC595-INIT ( -- )
  \ 初始化74HC595设备
  ." 初始化74HC595..." CR


  \ 初始化外设
  \ 初始化74HC595
  0 _74HC595-DATA!  \ DATA寄存器
  0 _74HC595-LATCH!  \ LATCH寄存器
  0 _74HC595-CHAIN_COUNT!  \ CHAIN_COUNT寄存器
  0 _74HC595-OE!  \ OE寄存器
  0 _74HC595-CLEAR!  \ CLEAR寄存器

  ." 74HC595初始化完成" CR
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
  _74HC595-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

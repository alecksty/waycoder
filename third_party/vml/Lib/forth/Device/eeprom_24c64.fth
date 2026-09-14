\ 24C64设备定义 - Forth文件
\ 生成自: Generic/Memory/24C64
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: 24C64 64Kbit I2C Serial EEPROM (8K×8, 32-byte page write)
\ CPU架构: Memory
\ 位宽: 8位
\ 时钟频率: 400000 Hz

\ =========================================
\ 24C64设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" 24C64" ;
: MANUFACTURER  S" Generic" ;
: FAMILY        S" Memory" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Memory" ;
8 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT EEPROM-START
0x1FFF CONSTANT EEPROM-END
8192 CONSTANT EEPROM-SIZE  \ EEPROM main memory array (8KB, 32-byte page write)

\ 外设定义
\ 24C64 I2C EEPROM (0x50-0x57, 1.7V-5.5V)
0x50 CONSTANT _24C64-BASE
0x00 CONSTANT _24C64-ADDR_H
0x01 CONSTANT _24C64-ADDR_L
0x02 CONSTANT _24C64-DATA
0xFE CONSTANT _24C64-PAGE_SIZE
0xFD CONSTANT _24C64-SIZE

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ 24C64外设
: _24C64-ADDR_H@ ( -- n ) _24C64-ADDR_H C@ ;
: _24C64-ADDR_H! ( n -- ) _24C64-ADDR_H C! ;
: _24C64-ADDR_L@ ( -- n ) _24C64-ADDR_L C@ ;
: _24C64-ADDR_L! ( n -- ) _24C64-ADDR_L C! ;
: _24C64-DATA@ ( -- n ) _24C64-DATA C@ ;
: _24C64-DATA! ( n -- ) _24C64-DATA C! ;
: _24C64-PAGE_SIZE@ ( -- n ) _24C64-PAGE_SIZE C@ ;
: _24C64-PAGE_SIZE! ( n -- ) _24C64-PAGE_SIZE C! ;
: _24C64-SIZE@ ( -- n ) _24C64-SIZE @ ;
: _24C64-SIZE! ( n -- ) _24C64-SIZE ! ;

\ =========================================
\ 设备初始化
\ =========================================

: _24C64-INIT ( -- )
  \ 初始化24C64设备
  ." 初始化24C64..." CR


  \ 初始化外设
  \ 初始化24C64
  0 _24C64-ADDR_H!  \ ADDR_H寄存器
  0 _24C64-ADDR_L!  \ ADDR_L寄存器
  0 _24C64-DATA!  \ DATA寄存器
  0 _24C64-PAGE_SIZE!  \ PAGE_SIZE寄存器
  0 _24C64-SIZE!  \ SIZE寄存器

  ." 24C64初始化完成" CR
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
  _24C64-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

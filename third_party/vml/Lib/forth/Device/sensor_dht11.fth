\ DHT11设备定义 - Forth文件
\ 生成自: Aosong/Sensor/DHT11
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: Digital Temperature and Humidity Sensor (1-Wire)
\ CPU架构: Sensor
\ 位宽: 8位
\ 时钟频率: 500000 Hz

\ =========================================
\ DHT11设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" DHT11" ;
: MANUFACTURER  S" Aosong" ;
: FAMILY        S" Sensor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Sensor" ;
8 CONSTANT BITS
500000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT PACKAGE-START
0x00 CONSTANT PACKAGE-END
4 CONSTANT PACKAGE-SIZE  \ DIP-4/SMD-4

\ 外设定义
\ DHT11 1-Wire Sensor (3.0V-5.5V)
0x00 CONSTANT DHT11-BASE
0x00 CONSTANT DHT11-HUMIDITY_INT
0x01 CONSTANT DHT11-HUMIDITY_DEC
0x02 CONSTANT DHT11-TEMP_INT
0x03 CONSTANT DHT11-TEMP_DEC
0x04 CONSTANT DHT11-CHECKSUM

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ DHT11外设
: DHT11-HUMIDITY_INT@ ( -- n ) DHT11-HUMIDITY_INT C@ ;
: DHT11-HUMIDITY_INT! ( n -- ) DHT11-HUMIDITY_INT C! ;
: DHT11-HUMIDITY_DEC@ ( -- n ) DHT11-HUMIDITY_DEC C@ ;
: DHT11-HUMIDITY_DEC! ( n -- ) DHT11-HUMIDITY_DEC C! ;
: DHT11-TEMP_INT@ ( -- n ) DHT11-TEMP_INT C@ ;
: DHT11-TEMP_INT! ( n -- ) DHT11-TEMP_INT C! ;
: DHT11-TEMP_DEC@ ( -- n ) DHT11-TEMP_DEC C@ ;
: DHT11-TEMP_DEC! ( n -- ) DHT11-TEMP_DEC C! ;
: DHT11-CHECKSUM@ ( -- n ) DHT11-CHECKSUM C@ ;
: DHT11-CHECKSUM! ( n -- ) DHT11-CHECKSUM C! ;

\ =========================================
\ 设备初始化
\ =========================================

: DHT11-INIT ( -- )
  \ 初始化DHT11设备
  ." 初始化DHT11..." CR


  \ 初始化外设
  \ 初始化DHT11
  0 DHT11-HUMIDITY_INT!  \ HUMIDITY_INT寄存器
  0 DHT11-HUMIDITY_DEC!  \ HUMIDITY_DEC寄存器
  0 DHT11-TEMP_INT!  \ TEMP_INT寄存器
  0 DHT11-TEMP_DEC!  \ TEMP_DEC寄存器
  0 DHT11-CHECKSUM!  \ CHECKSUM寄存器

  ." DHT11初始化完成" CR
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
  DHT11-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

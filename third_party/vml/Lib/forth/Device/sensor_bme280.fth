\ BME280设备定义 - Forth文件
\ 生成自: Bosch/Sensor/BME280
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: BME280 Combined Humidity, Pressure, and Temperature Sensor (I2C/SPI)
\ CPU架构: Sensor
\ 位宽: 8位
\ 时钟频率: 400000 Hz

\ =========================================
\ BME280设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" BME280" ;
: MANUFACTURER  S" Bosch" ;
: FAMILY        S" Sensor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Sensor" ;
8 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 外设定义
\ BME280 Environmental Sensor (0x76/0x77, 1.71V-3.6V)
0x76 CONSTANT BME280-BASE
0xD0 CONSTANT BME280-CHIP_ID
0xE0 CONSTANT BME280-RESET
0xF2 CONSTANT BME280-CTRL_HUM
0xF3 CONSTANT BME280-STATUS
0xF4 CONSTANT BME280-CTRL_MEAS
0xF5 CONSTANT BME280-CONFIG
0xF7 CONSTANT BME280-PRESS
0xFA CONSTANT BME280-TEMP
0xFD CONSTANT BME280-HUM

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ BME280外设
: BME280-CHIP_ID@ ( -- n ) BME280-CHIP_ID C@ ;
: BME280-CHIP_ID! ( n -- ) BME280-CHIP_ID C! ;
: BME280-RESET@ ( -- n ) BME280-RESET C@ ;
: BME280-RESET! ( n -- ) BME280-RESET C! ;
: BME280-CTRL_HUM@ ( -- n ) BME280-CTRL_HUM C@ ;
: BME280-CTRL_HUM! ( n -- ) BME280-CTRL_HUM C! ;
: BME280-STATUS@ ( -- n ) BME280-STATUS C@ ;
: BME280-STATUS! ( n -- ) BME280-STATUS C! ;
: BME280-CTRL_MEAS@ ( -- n ) BME280-CTRL_MEAS C@ ;
: BME280-CTRL_MEAS! ( n -- ) BME280-CTRL_MEAS C! ;
: BME280-CONFIG@ ( -- n ) BME280-CONFIG C@ ;
: BME280-CONFIG! ( n -- ) BME280-CONFIG C! ;
: BME280-PRESS@ ( -- n ) BME280-PRESS  3 CHARS@ ;
: BME280-PRESS! ( n -- ) BME280-PRESS  3 CHARS! ;
: BME280-TEMP@ ( -- n ) BME280-TEMP  3 CHARS@ ;
: BME280-TEMP! ( n -- ) BME280-TEMP  3 CHARS! ;
: BME280-HUM@ ( -- n ) BME280-HUM @ ;
: BME280-HUM! ( n -- ) BME280-HUM ! ;

\ =========================================
\ 设备初始化
\ =========================================

: BME280-INIT ( -- )
  \ 初始化BME280设备
  ." 初始化BME280..." CR


  \ 初始化外设
  \ 初始化BME280
  0 BME280-CHIP_ID!  \ CHIP_ID寄存器
  0 BME280-RESET!  \ RESET寄存器
  0 BME280-CTRL_HUM!  \ CTRL_HUM寄存器
  0 BME280-STATUS!  \ STATUS寄存器
  0 BME280-CTRL_MEAS!  \ CTRL_MEAS寄存器
  0 BME280-CONFIG!  \ CONFIG寄存器
  0 BME280-PRESS!  \ PRESS寄存器
  0 BME280-TEMP!  \ TEMP寄存器
  0 BME280-HUM!  \ HUM寄存器

  ." BME280初始化完成" CR
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
  BME280-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

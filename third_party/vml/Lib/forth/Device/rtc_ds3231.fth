\ DS3231设备定义 - Forth文件
\ 生成自: Maxim/Dallas/RTC/DS3231
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: DS3231 I2C High-Precision RTC (±2ppm, temperature compensated, 32K EEPROM)
\ CPU架构: RTC
\ 位宽: 8位
\ 时钟频率: 400000 Hz

\ =========================================
\ DS3231设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" DS3231" ;
: MANUFACTURER  S" Maxim/Dallas" ;
: FAMILY        S" RTC" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" RTC" ;
8 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x14 CONSTANT EEPROM-START
0xFF CONSTANT EEPROM-END
236 CONSTANT EEPROM-SIZE  \ AT24C32 EEPROM (32Kbit)

\ 外设定义
\ DS3231 Precision RTC (0x68, 3.3V-5.5V)
0x68 CONSTANT DS3231-BASE
0x00 CONSTANT DS3231-SEC
0x01 CONSTANT DS3231-MIN
0x02 CONSTANT DS3231-HOUR
0x03 CONSTANT DS3231-DAY
0x04 CONSTANT DS3231-DATE
0x05 CONSTANT DS3231-MONTH_CENT
0x06 CONSTANT DS3231-YEAR
0x07 CONSTANT DS3231-ALARM1_SEC
0x08 CONSTANT DS3231-ALARM1_MIN
0x09 CONSTANT DS3231-ALARM1_HOUR
0x0B CONSTANT DS3231-ALARM2_MIN
0x0C CONSTANT DS3231-ALARM2_HOUR
0x0E CONSTANT DS3231-CTRL
0x0F CONSTANT DS3231-CTRL_STATUS
0x11 CONSTANT DS3231-TEMP_MSB
0x12 CONSTANT DS3231-TEMP_LSB

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ DS3231外设
: DS3231-SEC@ ( -- n ) DS3231-SEC C@ ;
: DS3231-SEC! ( n -- ) DS3231-SEC C! ;
: DS3231-MIN@ ( -- n ) DS3231-MIN C@ ;
: DS3231-MIN! ( n -- ) DS3231-MIN C! ;
: DS3231-HOUR@ ( -- n ) DS3231-HOUR C@ ;
: DS3231-HOUR! ( n -- ) DS3231-HOUR C! ;
: DS3231-DAY@ ( -- n ) DS3231-DAY C@ ;
: DS3231-DAY! ( n -- ) DS3231-DAY C! ;
: DS3231-DATE@ ( -- n ) DS3231-DATE C@ ;
: DS3231-DATE! ( n -- ) DS3231-DATE C! ;
: DS3231-MONTH_CENT@ ( -- n ) DS3231-MONTH_CENT C@ ;
: DS3231-MONTH_CENT! ( n -- ) DS3231-MONTH_CENT C! ;
: DS3231-YEAR@ ( -- n ) DS3231-YEAR C@ ;
: DS3231-YEAR! ( n -- ) DS3231-YEAR C! ;
: DS3231-ALARM1_SEC@ ( -- n ) DS3231-ALARM1_SEC C@ ;
: DS3231-ALARM1_SEC! ( n -- ) DS3231-ALARM1_SEC C! ;
: DS3231-ALARM1_MIN@ ( -- n ) DS3231-ALARM1_MIN C@ ;
: DS3231-ALARM1_MIN! ( n -- ) DS3231-ALARM1_MIN C! ;
: DS3231-ALARM1_HOUR@ ( -- n ) DS3231-ALARM1_HOUR C@ ;
: DS3231-ALARM1_HOUR! ( n -- ) DS3231-ALARM1_HOUR C! ;
: DS3231-ALARM2_MIN@ ( -- n ) DS3231-ALARM2_MIN C@ ;
: DS3231-ALARM2_MIN! ( n -- ) DS3231-ALARM2_MIN C! ;
: DS3231-ALARM2_HOUR@ ( -- n ) DS3231-ALARM2_HOUR C@ ;
: DS3231-ALARM2_HOUR! ( n -- ) DS3231-ALARM2_HOUR C! ;
: DS3231-CTRL@ ( -- n ) DS3231-CTRL C@ ;
: DS3231-CTRL! ( n -- ) DS3231-CTRL C! ;
: DS3231-CTRL_STATUS@ ( -- n ) DS3231-CTRL_STATUS C@ ;
: DS3231-CTRL_STATUS! ( n -- ) DS3231-CTRL_STATUS C! ;
: DS3231-TEMP_MSB@ ( -- n ) DS3231-TEMP_MSB C@ ;
: DS3231-TEMP_MSB! ( n -- ) DS3231-TEMP_MSB C! ;
: DS3231-TEMP_LSB@ ( -- n ) DS3231-TEMP_LSB C@ ;
: DS3231-TEMP_LSB! ( n -- ) DS3231-TEMP_LSB C! ;

\ =========================================
\ 设备初始化
\ =========================================

: DS3231-INIT ( -- )
  \ 初始化DS3231设备
  ." 初始化DS3231..." CR


  \ 初始化外设
  \ 初始化DS3231
  0 DS3231-SEC!  \ SEC寄存器
  0 DS3231-MIN!  \ MIN寄存器
  0 DS3231-HOUR!  \ HOUR寄存器
  0 DS3231-DAY!  \ DAY寄存器
  0 DS3231-DATE!  \ DATE寄存器
  0 DS3231-MONTH_CENT!  \ MONTH_CENT寄存器
  0 DS3231-YEAR!  \ YEAR寄存器
  0 DS3231-ALARM1_SEC!  \ ALARM1_SEC寄存器
  0 DS3231-ALARM1_MIN!  \ ALARM1_MIN寄存器
  0 DS3231-ALARM1_HOUR!  \ ALARM1_HOUR寄存器
  0 DS3231-ALARM2_MIN!  \ ALARM2_MIN寄存器
  0 DS3231-ALARM2_HOUR!  \ ALARM2_HOUR寄存器
  0 DS3231-CTRL!  \ CTRL寄存器
  0 DS3231-CTRL_STATUS!  \ CTRL_STATUS寄存器
  0 DS3231-TEMP_MSB!  \ TEMP_MSB寄存器
  0 DS3231-TEMP_LSB!  \ TEMP_LSB寄存器

  ." DS3231初始化完成" CR
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
  DS3231-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

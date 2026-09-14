\ DS18B20设备定义 - Forth文件
\ 生成自: Maxim/Dallas/Sensor/DS18B20
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: Programmable Resolution 1-Wire Digital Thermometer
\ CPU架构: Sensor
\ 位宽: 8位
\ 时钟频率: 100000 Hz

\ =========================================
\ DS18B20设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" DS18B20" ;
: MANUFACTURER  S" Maxim/Dallas" ;
: FAMILY        S" Sensor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Sensor" ;
8 CONSTANT BITS
100000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT SCRATCHPAD-START
0x08 CONSTANT SCRATCHPAD-END
9 CONSTANT SCRATCHPAD-SIZE  \ Scratchpad memory (9 bytes)
0x00 CONSTANT EEPROM-START
0x02 CONSTANT EEPROM-END
3 CONSTANT EEPROM-SIZE  \ EEPROM (TH, TL, config bytes)

\ 外设定义
\ DS18B20 1-Wire Thermometer (3.0V-5.5V, TO-92)
0x00 CONSTANT DS18B20-BASE
0x00 CONSTANT DS18B20-TEMP_LSB
0x01 CONSTANT DS18B20-TEMP_MSB
0x02 CONSTANT DS18B20-TH_REG
0x03 CONSTANT DS18B20-TL_REG
0x04 CONSTANT DS18B20-CONFIG
5 CONSTANT DS18B20-CONFIG-R0  \ Resolution select bit 0
6 CONSTANT DS18B20-CONFIG-R1  \ Resolution select bit 1 (00=9bit,10=10bit,01=11bit,11=12bit)
0x06 CONSTANT DS18B20-COUNT_REMAIN
0x07 CONSTANT DS18B20-COUNT_PER_C
0x08 CONSTANT DS18B20-CRC

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ DS18B20外设
: DS18B20-TEMP_LSB@ ( -- n ) DS18B20-TEMP_LSB C@ ;
: DS18B20-TEMP_LSB! ( n -- ) DS18B20-TEMP_LSB C! ;
: DS18B20-TEMP_MSB@ ( -- n ) DS18B20-TEMP_MSB C@ ;
: DS18B20-TEMP_MSB! ( n -- ) DS18B20-TEMP_MSB C! ;
: DS18B20-TH_REG@ ( -- n ) DS18B20-TH_REG C@ ;
: DS18B20-TH_REG! ( n -- ) DS18B20-TH_REG C! ;
: DS18B20-TL_REG@ ( -- n ) DS18B20-TL_REG C@ ;
: DS18B20-TL_REG! ( n -- ) DS18B20-TL_REG C! ;
: DS18B20-CONFIG@ ( -- n ) DS18B20-CONFIG C@ ;
: DS18B20-CONFIG! ( n -- ) DS18B20-CONFIG C! ;
: DS18B20-CONFIG-R0@ ( -- flag ) DS18B20-CONFIG@ 5 BIT@ ;
: DS18B20-CONFIG-R0! ( flag -- ) DS18B20-CONFIG@ 5 BIT! DS18B20-CONFIG! ;
: DS18B20-CONFIG-R1@ ( -- flag ) DS18B20-CONFIG@ 6 BIT@ ;
: DS18B20-CONFIG-R1! ( flag -- ) DS18B20-CONFIG@ 6 BIT! DS18B20-CONFIG! ;
: DS18B20-COUNT_REMAIN@ ( -- n ) DS18B20-COUNT_REMAIN C@ ;
: DS18B20-COUNT_REMAIN! ( n -- ) DS18B20-COUNT_REMAIN C! ;
: DS18B20-COUNT_PER_C@ ( -- n ) DS18B20-COUNT_PER_C C@ ;
: DS18B20-COUNT_PER_C! ( n -- ) DS18B20-COUNT_PER_C C! ;
: DS18B20-CRC@ ( -- n ) DS18B20-CRC C@ ;
: DS18B20-CRC! ( n -- ) DS18B20-CRC C! ;

\ =========================================
\ 设备初始化
\ =========================================

: DS18B20-INIT ( -- )
  \ 初始化DS18B20设备
  ." 初始化DS18B20..." CR


  \ 初始化外设
  \ 初始化DS18B20
  0 DS18B20-TEMP_LSB!  \ TEMP_LSB寄存器
  0 DS18B20-TEMP_MSB!  \ TEMP_MSB寄存器
  0 DS18B20-TH_REG!  \ TH_REG寄存器
  0 DS18B20-TL_REG!  \ TL_REG寄存器
  0 DS18B20-CONFIG!  \ CONFIG寄存器
  0 DS18B20-COUNT_REMAIN!  \ COUNT_REMAIN寄存器
  0 DS18B20-COUNT_PER_C!  \ COUNT_PER_C寄存器
  0 DS18B20-CRC!  \ CRC寄存器

  ." DS18B20初始化完成" CR
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
  DS18B20-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

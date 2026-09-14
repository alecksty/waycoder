\ BMP280设备定义 - Forth文件
\ 生成自: Bosch/Sensor/BMP280
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: Digital Barometric Pressure and Temperature Sensor (I2C/SPI)
\ CPU架构: Sensor
\ 位宽: 8位
\ 时钟频率: 3400000 Hz

\ =========================================
\ BMP280设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" BMP280" ;
: MANUFACTURER  S" Bosch" ;
: FAMILY        S" Sensor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Sensor" ;
8 CONSTANT BITS
3400000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT PACKAGE-START
0x00 CONSTANT PACKAGE-END
8 CONSTANT PACKAGE-SIZE  \ LGA-8 (2.0x2.5x0.95mm)

\ 外设定义
\ BMP280 I2C Sensor (0x76/0x77, 1.71V-3.6V)
0x76 CONSTANT BMP280-BASE
0xFC CONSTANT BMP280-TEMP_XLSB
0xFB CONSTANT BMP280-TEMP_LSB
0xFA CONSTANT BMP280-TEMP_MSB
0xF9 CONSTANT BMP280-PRESS_XLSB
0xF8 CONSTANT BMP280-PRESS_LSB
0xF7 CONSTANT BMP280-PRESS_MSB
0xF5 CONSTANT BMP280-CONFIG
5 CONSTANT BMP280-CONFIG-T_SB  \ Standby time in normal mode
2 CONSTANT BMP280-CONFIG-FILTER  \ Filter coefficient
0 CONSTANT BMP280-CONFIG-SPI3W_EN  \ Enable 3-wire SPI
0xF4 CONSTANT BMP280-CTRL_MEAS
0 CONSTANT BMP280-CTRL_MEAS-MODE  \ 0=sleep, 1/2=forced, 3=normal
2 CONSTANT BMP280-CTRL_MEAS-OSRS_P  \ Pressure oversampling
5 CONSTANT BMP280-CTRL_MEAS-OSRS_T  \ Temperature oversampling
0xF3 CONSTANT BMP280-STATUS
0 CONSTANT BMP280-STATUS-IM_UPDATE  \ 1=Image register update in progress
3 CONSTANT BMP280-STATUS-MEASURING  \ 1=Conversion is running
0xD0 CONSTANT BMP280-CHIP_ID
0xE0 CONSTANT BMP280-RESET

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ BMP280外设
: BMP280-TEMP_XLSB@ ( -- n ) BMP280-TEMP_XLSB C@ ;
: BMP280-TEMP_XLSB! ( n -- ) BMP280-TEMP_XLSB C! ;
: BMP280-TEMP_LSB@ ( -- n ) BMP280-TEMP_LSB C@ ;
: BMP280-TEMP_LSB! ( n -- ) BMP280-TEMP_LSB C! ;
: BMP280-TEMP_MSB@ ( -- n ) BMP280-TEMP_MSB C@ ;
: BMP280-TEMP_MSB! ( n -- ) BMP280-TEMP_MSB C! ;
: BMP280-PRESS_XLSB@ ( -- n ) BMP280-PRESS_XLSB C@ ;
: BMP280-PRESS_XLSB! ( n -- ) BMP280-PRESS_XLSB C! ;
: BMP280-PRESS_LSB@ ( -- n ) BMP280-PRESS_LSB C@ ;
: BMP280-PRESS_LSB! ( n -- ) BMP280-PRESS_LSB C! ;
: BMP280-PRESS_MSB@ ( -- n ) BMP280-PRESS_MSB C@ ;
: BMP280-PRESS_MSB! ( n -- ) BMP280-PRESS_MSB C! ;
: BMP280-CONFIG@ ( -- n ) BMP280-CONFIG C@ ;
: BMP280-CONFIG! ( n -- ) BMP280-CONFIG C! ;
: BMP280-CONFIG-T_SB@ ( -- flag ) BMP280-CONFIG@ 5 BIT@ ;
: BMP280-CONFIG-T_SB! ( flag -- ) BMP280-CONFIG@ 5 BIT! BMP280-CONFIG! ;
: BMP280-CONFIG-FILTER@ ( -- flag ) BMP280-CONFIG@ 2 BIT@ ;
: BMP280-CONFIG-FILTER! ( flag -- ) BMP280-CONFIG@ 2 BIT! BMP280-CONFIG! ;
: BMP280-CONFIG-SPI3W_EN@ ( -- flag ) BMP280-CONFIG@ 0 BIT@ ;
: BMP280-CONFIG-SPI3W_EN! ( flag -- ) BMP280-CONFIG@ 0 BIT! BMP280-CONFIG! ;
: BMP280-CTRL_MEAS@ ( -- n ) BMP280-CTRL_MEAS C@ ;
: BMP280-CTRL_MEAS! ( n -- ) BMP280-CTRL_MEAS C! ;
: BMP280-CTRL_MEAS-MODE@ ( -- flag ) BMP280-CTRL_MEAS@ 0 BIT@ ;
: BMP280-CTRL_MEAS-MODE! ( flag -- ) BMP280-CTRL_MEAS@ 0 BIT! BMP280-CTRL_MEAS! ;
: BMP280-CTRL_MEAS-OSRS_P@ ( -- flag ) BMP280-CTRL_MEAS@ 2 BIT@ ;
: BMP280-CTRL_MEAS-OSRS_P! ( flag -- ) BMP280-CTRL_MEAS@ 2 BIT! BMP280-CTRL_MEAS! ;
: BMP280-CTRL_MEAS-OSRS_T@ ( -- flag ) BMP280-CTRL_MEAS@ 5 BIT@ ;
: BMP280-CTRL_MEAS-OSRS_T! ( flag -- ) BMP280-CTRL_MEAS@ 5 BIT! BMP280-CTRL_MEAS! ;
: BMP280-STATUS@ ( -- n ) BMP280-STATUS C@ ;
: BMP280-STATUS! ( n -- ) BMP280-STATUS C! ;
: BMP280-STATUS-IM_UPDATE@ ( -- flag ) BMP280-STATUS@ 0 BIT@ ;
: BMP280-STATUS-IM_UPDATE! ( flag -- ) BMP280-STATUS@ 0 BIT! BMP280-STATUS! ;
: BMP280-STATUS-MEASURING@ ( -- flag ) BMP280-STATUS@ 3 BIT@ ;
: BMP280-STATUS-MEASURING! ( flag -- ) BMP280-STATUS@ 3 BIT! BMP280-STATUS! ;
: BMP280-CHIP_ID@ ( -- n ) BMP280-CHIP_ID C@ ;
: BMP280-CHIP_ID! ( n -- ) BMP280-CHIP_ID C! ;
: BMP280-RESET@ ( -- n ) BMP280-RESET C@ ;
: BMP280-RESET! ( n -- ) BMP280-RESET C! ;

\ =========================================
\ 设备初始化
\ =========================================

: BMP280-INIT ( -- )
  \ 初始化BMP280设备
  ." 初始化BMP280..." CR


  \ 初始化外设
  \ 初始化BMP280
  0 BMP280-TEMP_XLSB!  \ TEMP_XLSB寄存器
  0 BMP280-TEMP_LSB!  \ TEMP_LSB寄存器
  0 BMP280-TEMP_MSB!  \ TEMP_MSB寄存器
  0 BMP280-PRESS_XLSB!  \ PRESS_XLSB寄存器
  0 BMP280-PRESS_LSB!  \ PRESS_LSB寄存器
  0 BMP280-PRESS_MSB!  \ PRESS_MSB寄存器
  0 BMP280-CONFIG!  \ CONFIG寄存器
  0 BMP280-CTRL_MEAS!  \ CTRL_MEAS寄存器
  0 BMP280-STATUS!  \ STATUS寄存器
  0 BMP280-CHIP_ID!  \ CHIP_ID寄存器
  0 BMP280-RESET!  \ RESET寄存器

  ." BMP280初始化完成" CR
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
  BMP280-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

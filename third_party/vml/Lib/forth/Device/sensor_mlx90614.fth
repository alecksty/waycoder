\ MLX90614设备定义 - Forth文件
\ 生成自: Melexis/Sensor/MLX90614
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: MLX90614 Infrared Thermometer (I2C, non-contact, -70 to +380°C, 17-bit)
\ CPU架构: Sensor
\ 位宽: 17位
\ 时钟频率: 100000 Hz

\ =========================================
\ MLX90614设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" MLX90614" ;
: MANUFACTURER  S" Melexis" ;
: FAMILY        S" Sensor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Sensor" ;
17 CONSTANT BITS
100000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT EEPROM-START
0x1F CONSTANT EEPROM-END
32 CONSTANT EEPROM-SIZE  \ Internal EEPROM (calibration data)

\ 外设定义
\ MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39)
0x5A CONSTANT MLX90614-BASE
0x06 CONSTANT MLX90614-T_AMBIENT
0x07 CONSTANT MLX90614-T_OBJECT1
0x08 CONSTANT MLX90614-T_OBJECT2
0x04 CONSTANT MLX90614-RAW_IR1
0x05 CONSTANT MLX90614-RAW_IR2
0x04 CONSTANT MLX90614-EMISSIVITY

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ MLX90614外设
: MLX90614-T_AMBIENT@ ( -- n ) MLX90614-T_AMBIENT @ ;
: MLX90614-T_AMBIENT! ( n -- ) MLX90614-T_AMBIENT ! ;
: MLX90614-T_OBJECT1@ ( -- n ) MLX90614-T_OBJECT1 @ ;
: MLX90614-T_OBJECT1! ( n -- ) MLX90614-T_OBJECT1 ! ;
: MLX90614-T_OBJECT2@ ( -- n ) MLX90614-T_OBJECT2 @ ;
: MLX90614-T_OBJECT2! ( n -- ) MLX90614-T_OBJECT2 ! ;
: MLX90614-RAW_IR1@ ( -- n ) MLX90614-RAW_IR1 @ ;
: MLX90614-RAW_IR1! ( n -- ) MLX90614-RAW_IR1 ! ;
: MLX90614-RAW_IR2@ ( -- n ) MLX90614-RAW_IR2 @ ;
: MLX90614-RAW_IR2! ( n -- ) MLX90614-RAW_IR2 ! ;
: MLX90614-EMISSIVITY@ ( -- n ) MLX90614-EMISSIVITY @ ;
: MLX90614-EMISSIVITY! ( n -- ) MLX90614-EMISSIVITY ! ;

\ =========================================
\ 设备初始化
\ =========================================

: MLX90614-INIT ( -- )
  \ 初始化MLX90614设备
  ." 初始化MLX90614..." CR


  \ 初始化外设
  \ 初始化MLX90614
  0 MLX90614-T_AMBIENT!  \ T_AMBIENT寄存器
  0 MLX90614-T_OBJECT1!  \ T_OBJECT1寄存器
  0 MLX90614-T_OBJECT2!  \ T_OBJECT2寄存器
  0 MLX90614-RAW_IR1!  \ RAW_IR1寄存器
  0 MLX90614-RAW_IR2!  \ RAW_IR2寄存器
  0 MLX90614-EMISSIVITY!  \ EMISSIVITY寄存器

  ." MLX90614初始化完成" CR
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
  MLX90614-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

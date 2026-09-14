\ VL53L0X设备定义 - Forth文件
\ 生成自: STMicroelectronics/Sensor/VL53L0X
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: VL53L0X ToF Laser Distance Sensor (I2C, 2cm-200cm, 940nm VCSEL)
\ CPU架构: Sensor
\ 位宽: 16位
\ 时钟频率: 400000 Hz

\ =========================================
\ VL53L0X设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" VL53L0X" ;
: MANUFACTURER  S" STMicroelectronics" ;
: FAMILY        S" Sensor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Sensor" ;
16 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 外设定义
\ VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
0x29 CONSTANT VL53L0X-BASE
0x00 CONSTANT VL53L0X-DISTANCE
0x02 CONSTANT VL53L0X-SIGNAL_RATE
0x04 CONSTANT VL53L0X-AMBIENT_RATE
0x06 CONSTANT VL53L0X-SPAD_COUNT
0x08 CONSTANT VL53L0X-RANGE_STATUS
0x09 CONSTANT VL53L0X-TIMING_BUDGET
0x0D CONSTANT VL53L0X-INTER_MEAS
0x0E CONSTANT VL53L0X-MODE

\ 引脚定义
1 CONSTANT PIN-XSHUT  \ Shutdown pin (active low)
2 CONSTANT PIN-INT  \ Interrupt (open-drain)

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ VL53L0X外设
: VL53L0X-DISTANCE@ ( -- n ) VL53L0X-DISTANCE @ ;
: VL53L0X-DISTANCE! ( n -- ) VL53L0X-DISTANCE ! ;
: VL53L0X-SIGNAL_RATE@ ( -- n ) VL53L0X-SIGNAL_RATE @ ;
: VL53L0X-SIGNAL_RATE! ( n -- ) VL53L0X-SIGNAL_RATE ! ;
: VL53L0X-AMBIENT_RATE@ ( -- n ) VL53L0X-AMBIENT_RATE @ ;
: VL53L0X-AMBIENT_RATE! ( n -- ) VL53L0X-AMBIENT_RATE ! ;
: VL53L0X-SPAD_COUNT@ ( -- n ) VL53L0X-SPAD_COUNT @ ;
: VL53L0X-SPAD_COUNT! ( n -- ) VL53L0X-SPAD_COUNT ! ;
: VL53L0X-RANGE_STATUS@ ( -- n ) VL53L0X-RANGE_STATUS C@ ;
: VL53L0X-RANGE_STATUS! ( n -- ) VL53L0X-RANGE_STATUS C! ;
: VL53L0X-TIMING_BUDGET@ ( -- n ) VL53L0X-TIMING_BUDGET L@ ;
: VL53L0X-TIMING_BUDGET! ( n -- ) VL53L0X-TIMING_BUDGET L! ;
: VL53L0X-INTER_MEAS@ ( -- n ) VL53L0X-INTER_MEAS L@ ;
: VL53L0X-INTER_MEAS! ( n -- ) VL53L0X-INTER_MEAS L! ;
: VL53L0X-MODE@ ( -- n ) VL53L0X-MODE C@ ;
: VL53L0X-MODE! ( n -- ) VL53L0X-MODE C! ;

\ =========================================
\ 设备初始化
\ =========================================

: VL53L0X-INIT ( -- )
  \ 初始化VL53L0X设备
  ." 初始化VL53L0X..." CR


  \ 初始化外设
  \ 初始化VL53L0X
  0 VL53L0X-DISTANCE!  \ DISTANCE寄存器
  0 VL53L0X-SIGNAL_RATE!  \ SIGNAL_RATE寄存器
  0 VL53L0X-AMBIENT_RATE!  \ AMBIENT_RATE寄存器
  0 VL53L0X-SPAD_COUNT!  \ SPAD_COUNT寄存器
  0 VL53L0X-RANGE_STATUS!  \ RANGE_STATUS寄存器
  0 VL53L0X-TIMING_BUDGET!  \ TIMING_BUDGET寄存器
  0 VL53L0X-INTER_MEAS!  \ INTER_MEAS寄存器
  0 VL53L0X-MODE!  \ MODE寄存器

  ." VL53L0X初始化完成" CR
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
\ 引脚操作
\ =========================================

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  VL53L0X-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

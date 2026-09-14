\ SG90设备定义 - Forth文件
\ 生成自: Tower Pro/Motor/SG90
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: SG90 Micro Servo Motor (0-180°, 4.8V-6V)
\ CPU架构: Motor
\ 位宽: 8位
\ 时钟频率: 0 Hz

\ =========================================
\ SG90设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" SG90" ;
: MANUFACTURER  S" Tower Pro" ;
: FAMILY        S" Motor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Motor" ;
8 CONSTANT BITS
0 CONSTANT CLOCK-FREQ

\ 外设定义
\ SG90 Micro Servo (500-2500us pulse, 50Hz)
0x00 CONSTANT SG90-BASE
0x00 CONSTANT SG90-ANGLE
0x01 CONSTANT SG90-PULSE_MIN
0x03 CONSTANT SG90-PULSE_MAX
0x05 CONSTANT SG90-CURRENT_ANGLE
0x06 CONSTANT SG90-SPEED

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ SG90外设
: SG90-ANGLE@ ( -- n ) SG90-ANGLE C@ ;
: SG90-ANGLE! ( n -- ) SG90-ANGLE C! ;
: SG90-PULSE_MIN@ ( -- n ) SG90-PULSE_MIN @ ;
: SG90-PULSE_MIN! ( n -- ) SG90-PULSE_MIN ! ;
: SG90-PULSE_MAX@ ( -- n ) SG90-PULSE_MAX @ ;
: SG90-PULSE_MAX! ( n -- ) SG90-PULSE_MAX ! ;
: SG90-CURRENT_ANGLE@ ( -- n ) SG90-CURRENT_ANGLE C@ ;
: SG90-CURRENT_ANGLE! ( n -- ) SG90-CURRENT_ANGLE C! ;
: SG90-SPEED@ ( -- n ) SG90-SPEED C@ ;
: SG90-SPEED! ( n -- ) SG90-SPEED C! ;

\ =========================================
\ 设备初始化
\ =========================================

: SG90-INIT ( -- )
  \ 初始化SG90设备
  ." 初始化SG90..." CR


  \ 初始化外设
  \ 初始化SG90
  0 SG90-ANGLE!  \ ANGLE寄存器
  0 SG90-PULSE_MIN!  \ PULSE_MIN寄存器
  0 SG90-PULSE_MAX!  \ PULSE_MAX寄存器
  0 SG90-CURRENT_ANGLE!  \ CURRENT_ANGLE寄存器
  0 SG90-SPEED!  \ SPEED寄存器

  ." SG90初始化完成" CR
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
  SG90-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

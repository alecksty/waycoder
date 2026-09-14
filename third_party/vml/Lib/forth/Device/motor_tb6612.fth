\ TB6612设备定义 - Forth文件
\ 生成自: Toshiba/Motor/TB6612
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: TB6612FNG Dual DC Motor Driver (1.2A continuous, 3.2A peak, 2.5V-13.5V)
\ CPU架构: Motor
\ 位宽: 8位
\ 时钟频率: 100000 Hz

\ =========================================
\ TB6612设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" TB6612" ;
: MANUFACTURER  S" Toshiba" ;
: FAMILY        S" Motor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Motor" ;
8 CONSTANT BITS
100000 CONSTANT CLOCK-FREQ

\ 外设定义
\ TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)
0x00 CONSTANT TB6612-BASE
0x00 CONSTANT TB6612-MOTOR_A
0 CONSTANT TB6612-MOTOR_A-AIN1  \ Motor A input 1
1 CONSTANT TB6612-MOTOR_A-AIN2  \ Motor A input 2
2 CONSTANT TB6612-MOTOR_A-PWMA  \ Motor A PWM enable
0x01 CONSTANT TB6612-MOTOR_B
0 CONSTANT TB6612-MOTOR_B-BIN1  \ Motor B input 1
1 CONSTANT TB6612-MOTOR_B-BIN2  \ Motor B input 2
2 CONSTANT TB6612-MOTOR_B-PWMB  \ Motor B PWM enable
0x02 CONSTANT TB6612-SPEED_A
0x04 CONSTANT TB6612-SPEED_B
0x06 CONSTANT TB6612-STBY

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ TB6612外设
: TB6612-MOTOR_A@ ( -- n ) TB6612-MOTOR_A C@ ;
: TB6612-MOTOR_A! ( n -- ) TB6612-MOTOR_A C! ;
: TB6612-MOTOR_A-AIN1@ ( -- flag ) TB6612-MOTOR_A@ 0 BIT@ ;
: TB6612-MOTOR_A-AIN1! ( flag -- ) TB6612-MOTOR_A@ 0 BIT! TB6612-MOTOR_A! ;
: TB6612-MOTOR_A-AIN2@ ( -- flag ) TB6612-MOTOR_A@ 1 BIT@ ;
: TB6612-MOTOR_A-AIN2! ( flag -- ) TB6612-MOTOR_A@ 1 BIT! TB6612-MOTOR_A! ;
: TB6612-MOTOR_A-PWMA@ ( -- flag ) TB6612-MOTOR_A@ 2 BIT@ ;
: TB6612-MOTOR_A-PWMA! ( flag -- ) TB6612-MOTOR_A@ 2 BIT! TB6612-MOTOR_A! ;
: TB6612-MOTOR_B@ ( -- n ) TB6612-MOTOR_B C@ ;
: TB6612-MOTOR_B! ( n -- ) TB6612-MOTOR_B C! ;
: TB6612-MOTOR_B-BIN1@ ( -- flag ) TB6612-MOTOR_B@ 0 BIT@ ;
: TB6612-MOTOR_B-BIN1! ( flag -- ) TB6612-MOTOR_B@ 0 BIT! TB6612-MOTOR_B! ;
: TB6612-MOTOR_B-BIN2@ ( -- flag ) TB6612-MOTOR_B@ 1 BIT@ ;
: TB6612-MOTOR_B-BIN2! ( flag -- ) TB6612-MOTOR_B@ 1 BIT! TB6612-MOTOR_B! ;
: TB6612-MOTOR_B-PWMB@ ( -- flag ) TB6612-MOTOR_B@ 2 BIT@ ;
: TB6612-MOTOR_B-PWMB! ( flag -- ) TB6612-MOTOR_B@ 2 BIT! TB6612-MOTOR_B! ;
: TB6612-SPEED_A@ ( -- n ) TB6612-SPEED_A @ ;
: TB6612-SPEED_A! ( n -- ) TB6612-SPEED_A ! ;
: TB6612-SPEED_B@ ( -- n ) TB6612-SPEED_B @ ;
: TB6612-SPEED_B! ( n -- ) TB6612-SPEED_B ! ;
: TB6612-STBY@ ( -- n ) TB6612-STBY C@ ;
: TB6612-STBY! ( n -- ) TB6612-STBY C! ;

\ =========================================
\ 设备初始化
\ =========================================

: TB6612-INIT ( -- )
  \ 初始化TB6612设备
  ." 初始化TB6612..." CR


  \ 初始化外设
  \ 初始化TB6612
  0 TB6612-MOTOR_A!  \ MOTOR_A寄存器
  0 TB6612-MOTOR_B!  \ MOTOR_B寄存器
  0 TB6612-SPEED_A!  \ SPEED_A寄存器
  0 TB6612-SPEED_B!  \ SPEED_B寄存器
  0 TB6612-STBY!  \ STBY寄存器

  ." TB6612初始化完成" CR
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
  TB6612-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

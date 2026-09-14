\ L298N设备定义 - Forth文件
\ 生成自: STMicroelectronics/Motor/L298N
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: L298N Dual H-Bridge DC Motor Driver (2A per channel, 5V-35V)
\ CPU架构: Motor
\ 位宽: 8位
\ 时钟频率: 0 Hz

\ =========================================
\ L298N设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" L298N" ;
: MANUFACTURER  S" STMicroelectronics" ;
: FAMILY        S" Motor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Motor" ;
8 CONSTANT BITS
0 CONSTANT CLOCK-FREQ

\ 外设定义
\ L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)
0x00 CONSTANT L298N-BASE
0x00 CONSTANT L298N-MOTOR_A
0 CONSTANT L298N-MOTOR_A-IN1  \ Motor A Input 1
1 CONSTANT L298N-MOTOR_A-IN2  \ Motor A Input 2
2 CONSTANT L298N-MOTOR_A-ENA  \ Motor A Enable/PWM
0x01 CONSTANT L298N-MOTOR_B
0 CONSTANT L298N-MOTOR_B-IN3  \ Motor B Input 3
1 CONSTANT L298N-MOTOR_B-IN4  \ Motor B Input 4
2 CONSTANT L298N-MOTOR_B-ENB  \ Motor B Enable/PWM
0x02 CONSTANT L298N-SPEED_A
0x03 CONSTANT L298N-SPEED_B
0x04 CONSTANT L298N-STATUS

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ L298N外设
: L298N-MOTOR_A@ ( -- n ) L298N-MOTOR_A C@ ;
: L298N-MOTOR_A! ( n -- ) L298N-MOTOR_A C! ;
: L298N-MOTOR_A-IN1@ ( -- flag ) L298N-MOTOR_A@ 0 BIT@ ;
: L298N-MOTOR_A-IN1! ( flag -- ) L298N-MOTOR_A@ 0 BIT! L298N-MOTOR_A! ;
: L298N-MOTOR_A-IN2@ ( -- flag ) L298N-MOTOR_A@ 1 BIT@ ;
: L298N-MOTOR_A-IN2! ( flag -- ) L298N-MOTOR_A@ 1 BIT! L298N-MOTOR_A! ;
: L298N-MOTOR_A-ENA@ ( -- flag ) L298N-MOTOR_A@ 2 BIT@ ;
: L298N-MOTOR_A-ENA! ( flag -- ) L298N-MOTOR_A@ 2 BIT! L298N-MOTOR_A! ;
: L298N-MOTOR_B@ ( -- n ) L298N-MOTOR_B C@ ;
: L298N-MOTOR_B! ( n -- ) L298N-MOTOR_B C! ;
: L298N-MOTOR_B-IN3@ ( -- flag ) L298N-MOTOR_B@ 0 BIT@ ;
: L298N-MOTOR_B-IN3! ( flag -- ) L298N-MOTOR_B@ 0 BIT! L298N-MOTOR_B! ;
: L298N-MOTOR_B-IN4@ ( -- flag ) L298N-MOTOR_B@ 1 BIT@ ;
: L298N-MOTOR_B-IN4! ( flag -- ) L298N-MOTOR_B@ 1 BIT! L298N-MOTOR_B! ;
: L298N-MOTOR_B-ENB@ ( -- flag ) L298N-MOTOR_B@ 2 BIT@ ;
: L298N-MOTOR_B-ENB! ( flag -- ) L298N-MOTOR_B@ 2 BIT! L298N-MOTOR_B! ;
: L298N-SPEED_A@ ( -- n ) L298N-SPEED_A C@ ;
: L298N-SPEED_A! ( n -- ) L298N-SPEED_A C! ;
: L298N-SPEED_B@ ( -- n ) L298N-SPEED_B C@ ;
: L298N-SPEED_B! ( n -- ) L298N-SPEED_B C! ;
: L298N-STATUS@ ( -- n ) L298N-STATUS C@ ;
: L298N-STATUS! ( n -- ) L298N-STATUS C! ;

\ =========================================
\ 设备初始化
\ =========================================

: L298N-INIT ( -- )
  \ 初始化L298N设备
  ." 初始化L298N..." CR


  \ 初始化外设
  \ 初始化L298N
  0 L298N-MOTOR_A!  \ MOTOR_A寄存器
  0 L298N-MOTOR_B!  \ MOTOR_B寄存器
  0 L298N-SPEED_A!  \ SPEED_A寄存器
  0 L298N-SPEED_B!  \ SPEED_B寄存器
  0 L298N-STATUS!  \ STATUS寄存器

  ." L298N初始化完成" CR
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
  L298N-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

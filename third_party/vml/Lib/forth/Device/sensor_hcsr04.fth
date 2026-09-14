\ HC_SR04设备定义 - Forth文件
\ 生成自: Generic/Sensor/HC_SR04
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: Ultrasonic Distance Sensor (2cm-400cm)
\ CPU架构: Sensor
\ 位宽: 8位
\ 时钟频率: 0 Hz

\ =========================================
\ HC_SR04设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" HC_SR04" ;
: MANUFACTURER  S" Generic" ;
: FAMILY        S" Sensor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Sensor" ;
8 CONSTANT BITS
0 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT PACKAGE-START
0x00 CONSTANT PACKAGE-END
0 CONSTANT PACKAGE-SIZE  \ PCB Module (45x20x15mm)

\ 外设定义
\ HC-SR04 Ultrasonic Sensor (4.5V-5.5V)
0x00 CONSTANT HC_SR04-BASE
0x00 CONSTANT HC_SR04-TRIG
0x01 CONSTANT HC_SR04-DISTANCE_H
0x02 CONSTANT HC_SR04-DISTANCE_L
0x03 CONSTANT HC_SR04-STATUS
0 CONSTANT HC_SR04-STATUS-BUSY  \ 1=Measurement in progress
1 CONSTANT HC_SR04-STATUS-VALID  \ 1=Valid measurement available
2 CONSTANT HC_SR04-STATUS-TIMEOUT  \ 1=No echo received (out of range)

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ HC_SR04外设
: HC_SR04-TRIG@ ( -- n ) HC_SR04-TRIG C@ ;
: HC_SR04-TRIG! ( n -- ) HC_SR04-TRIG C! ;
: HC_SR04-DISTANCE_H@ ( -- n ) HC_SR04-DISTANCE_H C@ ;
: HC_SR04-DISTANCE_H! ( n -- ) HC_SR04-DISTANCE_H C! ;
: HC_SR04-DISTANCE_L@ ( -- n ) HC_SR04-DISTANCE_L C@ ;
: HC_SR04-DISTANCE_L! ( n -- ) HC_SR04-DISTANCE_L C! ;
: HC_SR04-STATUS@ ( -- n ) HC_SR04-STATUS C@ ;
: HC_SR04-STATUS! ( n -- ) HC_SR04-STATUS C! ;
: HC_SR04-STATUS-BUSY@ ( -- flag ) HC_SR04-STATUS@ 0 BIT@ ;
: HC_SR04-STATUS-BUSY! ( flag -- ) HC_SR04-STATUS@ 0 BIT! HC_SR04-STATUS! ;
: HC_SR04-STATUS-VALID@ ( -- flag ) HC_SR04-STATUS@ 1 BIT@ ;
: HC_SR04-STATUS-VALID! ( flag -- ) HC_SR04-STATUS@ 1 BIT! HC_SR04-STATUS! ;
: HC_SR04-STATUS-TIMEOUT@ ( -- flag ) HC_SR04-STATUS@ 2 BIT@ ;
: HC_SR04-STATUS-TIMEOUT! ( flag -- ) HC_SR04-STATUS@ 2 BIT! HC_SR04-STATUS! ;

\ =========================================
\ 设备初始化
\ =========================================

: HC_SR04-INIT ( -- )
  \ 初始化HC_SR04设备
  ." 初始化HC_SR04..." CR


  \ 初始化外设
  \ 初始化HC_SR04
  0 HC_SR04-TRIG!  \ TRIG寄存器
  0 HC_SR04-DISTANCE_H!  \ DISTANCE_H寄存器
  0 HC_SR04-DISTANCE_L!  \ DISTANCE_L寄存器
  0 HC_SR04-STATUS!  \ STATUS寄存器

  ." HC_SR04初始化完成" CR
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
  HC_SR04-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

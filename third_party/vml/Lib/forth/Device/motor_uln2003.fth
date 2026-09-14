\ ULN2003设备定义 - Forth文件
\ 生成自: ST/TI/Motor/ULN2003
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: ULN2003 7-Channel Darlington Driver + 28BYJ-48 Stepper Motor (5V)
\ CPU架构: Motor
\ 位宽: 8位
\ 时钟频率: 0 Hz

\ =========================================
\ ULN2003设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ULN2003" ;
: MANUFACTURER  S" ST/TI" ;
: FAMILY        S" Motor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Motor" ;
8 CONSTANT BITS
0 CONSTANT CLOCK-FREQ

\ 外设定义
\ ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step)
0x00 CONSTANT ULN2003-BASE
0x00 CONSTANT ULN2003-STEPPER
0x01 CONSTANT ULN2003-STEP_MODE
0x02 CONSTANT ULN2003-STEPS
0x04 CONSTANT ULN2003-DELAY_MS
0x05 CONSTANT ULN2003-POSITION
0x07 CONSTANT ULN2003-DIRECTION

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ ULN2003外设
: ULN2003-STEPPER@ ( -- n ) ULN2003-STEPPER C@ ;
: ULN2003-STEPPER! ( n -- ) ULN2003-STEPPER C! ;
: ULN2003-STEP_MODE@ ( -- n ) ULN2003-STEP_MODE C@ ;
: ULN2003-STEP_MODE! ( n -- ) ULN2003-STEP_MODE C! ;
: ULN2003-STEPS@ ( -- n ) ULN2003-STEPS @ ;
: ULN2003-STEPS! ( n -- ) ULN2003-STEPS ! ;
: ULN2003-DELAY_MS@ ( -- n ) ULN2003-DELAY_MS C@ ;
: ULN2003-DELAY_MS! ( n -- ) ULN2003-DELAY_MS C! ;
: ULN2003-POSITION@ ( -- n ) ULN2003-POSITION @ ;
: ULN2003-POSITION! ( n -- ) ULN2003-POSITION ! ;
: ULN2003-DIRECTION@ ( -- n ) ULN2003-DIRECTION C@ ;
: ULN2003-DIRECTION! ( n -- ) ULN2003-DIRECTION C! ;

\ =========================================
\ 设备初始化
\ =========================================

: ULN2003-INIT ( -- )
  \ 初始化ULN2003设备
  ." 初始化ULN2003..." CR


  \ 初始化外设
  \ 初始化ULN2003
  0 ULN2003-STEPPER!  \ STEPPER寄存器
  0 ULN2003-STEP_MODE!  \ STEP_MODE寄存器
  0 ULN2003-STEPS!  \ STEPS寄存器
  0 ULN2003-DELAY_MS!  \ DELAY_MS寄存器
  0 ULN2003-POSITION!  \ POSITION寄存器
  0 ULN2003-DIRECTION!  \ DIRECTION寄存器

  ." ULN2003初始化完成" CR
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
  ULN2003-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

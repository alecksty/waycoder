\ CCS811设备定义 - Forth文件
\ 生成自: AMS/ScioSense/Sensor/CCS811
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: CCS811 VOC/eCO2 Air Quality Sensor (I2C, 400-8192ppm CO2, 0-1187ppb TVOC)
\ CPU架构: Sensor
\ 位宽: 16位
\ 时钟频率: 400000 Hz

\ =========================================
\ CCS811设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" CCS811" ;
: MANUFACTURER  S" AMS/ScioSense" ;
: FAMILY        S" Sensor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Sensor" ;
16 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 外设定义
\ CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
0x5A CONSTANT CCS811-BASE
0x00 CONSTANT CCS811-STATUS
0x01 CONSTANT CCS811-MEAS_MODE
0x02 CONSTANT CCS811-ALG_RESULT
0x02 CONSTANT CCS811-ECO2
0x04 CONSTANT CCS811-TVOC
0x06 CONSTANT CCS811-RAW_DATA
0x0B CONSTANT CCS811-BASELINE
0x20 CONSTANT CCS811-HW_ID
0xE0 CONSTANT CCS811-ERROR_ID
0xF4 CONSTANT CCS811-APP_START
0xFF CONSTANT CCS811-SW_RESET

\ 中断向量定义
0 CONSTANT INT-INT  \ Data ready / interrupt pin

\ 引脚定义
1 CONSTANT PIN-WAKE  \ Wake pin (active low)

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ CCS811外设
: CCS811-STATUS@ ( -- n ) CCS811-STATUS C@ ;
: CCS811-STATUS! ( n -- ) CCS811-STATUS C! ;
: CCS811-MEAS_MODE@ ( -- n ) CCS811-MEAS_MODE C@ ;
: CCS811-MEAS_MODE! ( n -- ) CCS811-MEAS_MODE C! ;
: CCS811-ALG_RESULT@ ( -- n ) CCS811-ALG_RESULT L@ ;
: CCS811-ALG_RESULT! ( n -- ) CCS811-ALG_RESULT L! ;
: CCS811-ECO2@ ( -- n ) CCS811-ECO2 @ ;
: CCS811-ECO2! ( n -- ) CCS811-ECO2 ! ;
: CCS811-TVOC@ ( -- n ) CCS811-TVOC @ ;
: CCS811-TVOC! ( n -- ) CCS811-TVOC ! ;
: CCS811-RAW_DATA@ ( -- n ) CCS811-RAW_DATA @ ;
: CCS811-RAW_DATA! ( n -- ) CCS811-RAW_DATA ! ;
: CCS811-BASELINE@ ( -- n ) CCS811-BASELINE @ ;
: CCS811-BASELINE! ( n -- ) CCS811-BASELINE ! ;
: CCS811-HW_ID@ ( -- n ) CCS811-HW_ID C@ ;
: CCS811-HW_ID! ( n -- ) CCS811-HW_ID C! ;
: CCS811-ERROR_ID@ ( -- n ) CCS811-ERROR_ID C@ ;
: CCS811-ERROR_ID! ( n -- ) CCS811-ERROR_ID C! ;
: CCS811-APP_START@ ( -- n ) CCS811-APP_START C@ ;
: CCS811-APP_START! ( n -- ) CCS811-APP_START C! ;
: CCS811-SW_RESET@ ( -- n ) CCS811-SW_RESET L@ ;
: CCS811-SW_RESET! ( n -- ) CCS811-SW_RESET L! ;

\ =========================================
\ 设备初始化
\ =========================================

: CCS811-INIT ( -- )
  \ 初始化CCS811设备
  ." 初始化CCS811..." CR


  \ 初始化外设
  \ 初始化CCS811
  0 CCS811-STATUS!  \ STATUS寄存器
  0 CCS811-MEAS_MODE!  \ MEAS_MODE寄存器
  0 CCS811-ALG_RESULT!  \ ALG_RESULT寄存器
  0 CCS811-ECO2!  \ ECO2寄存器
  0 CCS811-TVOC!  \ TVOC寄存器
  0 CCS811-RAW_DATA!  \ RAW_DATA寄存器
  0 CCS811-BASELINE!  \ BASELINE寄存器
  0 CCS811-HW_ID!  \ HW_ID寄存器
  0 CCS811-ERROR_ID!  \ ERROR_ID寄存器
  0 CCS811-APP_START!  \ APP_START寄存器
  0 CCS811-SW_RESET!  \ SW_RESET寄存器

  ." CCS811初始化完成" CR
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
\ 中断处理
\ =========================================

\ Data ready / interrupt pin
: INT-INT-HANDLER ( -- )
  ." INT中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INT-ENABLE ( -- )
  INT-INT INT-ENABLE
;

: INT-INT-DISABLE ( -- )
  INT-INT INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  CCS811-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

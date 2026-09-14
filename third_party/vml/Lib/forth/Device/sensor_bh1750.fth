\ BH1750设备定义 - Forth文件
\ 生成自: ROHM/Sensor/BH1750
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: BH1750FVI Digital Ambient Light Sensor (I2C, 1-65535 lux, 16-bit)
\ CPU架构: Sensor
\ 位宽: 16位
\ 时钟频率: 400000 Hz

\ =========================================
\ BH1750设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" BH1750" ;
: MANUFACTURER  S" ROHM" ;
: FAMILY        S" Sensor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Sensor" ;
16 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 外设定义
\ BH1750 Light Sensor (0x23/0x5C, 2.4V-3.6V)
0x23 CONSTANT BH1750-BASE
0x00 CONSTANT BH1750-LUX
0x01 CONSTANT BH1750-MODE
0 CONSTANT BH1750-MODE-CONT_H  \ Continuous High Res (1lx, 120ms)
1 CONSTANT BH1750-MODE-CONT_H2  \ Continuous High Res 2 (0.5lx, 120ms)
2 CONSTANT BH1750-MODE-CONT_L  \ Continuous Low Res (4lx, 16ms)
3 CONSTANT BH1750-MODE-ONCE_H  \ One-time High Res (1lx, 120ms)
4 CONSTANT BH1750-MODE-ONCE_H2  \ One-time High Res 2 (0.5lx, 120ms)
5 CONSTANT BH1750-MODE-ONCE_L  \ One-time Low Res (4lx, 16ms)
0x01 CONSTANT BH1750-CMD_POWER_ON
0x00 CONSTANT BH1750-CMD_POWER_OFF
0x07 CONSTANT BH1750-CMD_RESET

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ BH1750外设
: BH1750-LUX@ ( -- n ) BH1750-LUX @ ;
: BH1750-LUX! ( n -- ) BH1750-LUX ! ;
: BH1750-MODE@ ( -- n ) BH1750-MODE C@ ;
: BH1750-MODE! ( n -- ) BH1750-MODE C! ;
: BH1750-MODE-CONT_H@ ( -- flag ) BH1750-MODE@ 0 BIT@ ;
: BH1750-MODE-CONT_H! ( flag -- ) BH1750-MODE@ 0 BIT! BH1750-MODE! ;
: BH1750-MODE-CONT_H2@ ( -- flag ) BH1750-MODE@ 1 BIT@ ;
: BH1750-MODE-CONT_H2! ( flag -- ) BH1750-MODE@ 1 BIT! BH1750-MODE! ;
: BH1750-MODE-CONT_L@ ( -- flag ) BH1750-MODE@ 2 BIT@ ;
: BH1750-MODE-CONT_L! ( flag -- ) BH1750-MODE@ 2 BIT! BH1750-MODE! ;
: BH1750-MODE-ONCE_H@ ( -- flag ) BH1750-MODE@ 3 BIT@ ;
: BH1750-MODE-ONCE_H! ( flag -- ) BH1750-MODE@ 3 BIT! BH1750-MODE! ;
: BH1750-MODE-ONCE_H2@ ( -- flag ) BH1750-MODE@ 4 BIT@ ;
: BH1750-MODE-ONCE_H2! ( flag -- ) BH1750-MODE@ 4 BIT! BH1750-MODE! ;
: BH1750-MODE-ONCE_L@ ( -- flag ) BH1750-MODE@ 5 BIT@ ;
: BH1750-MODE-ONCE_L! ( flag -- ) BH1750-MODE@ 5 BIT! BH1750-MODE! ;
: BH1750-CMD_POWER_ON@ ( -- n ) BH1750-CMD_POWER_ON C@ ;
: BH1750-CMD_POWER_ON! ( n -- ) BH1750-CMD_POWER_ON C! ;
: BH1750-CMD_POWER_OFF@ ( -- n ) BH1750-CMD_POWER_OFF C@ ;
: BH1750-CMD_POWER_OFF! ( n -- ) BH1750-CMD_POWER_OFF C! ;
: BH1750-CMD_RESET@ ( -- n ) BH1750-CMD_RESET C@ ;
: BH1750-CMD_RESET! ( n -- ) BH1750-CMD_RESET C! ;

\ =========================================
\ 设备初始化
\ =========================================

: BH1750-INIT ( -- )
  \ 初始化BH1750设备
  ." 初始化BH1750..." CR


  \ 初始化外设
  \ 初始化BH1750
  0 BH1750-LUX!  \ LUX寄存器
  0 BH1750-MODE!  \ MODE寄存器
  0 BH1750-CMD_POWER_ON!  \ CMD_POWER_ON寄存器
  0 BH1750-CMD_POWER_OFF!  \ CMD_POWER_OFF寄存器
  0 BH1750-CMD_RESET!  \ CMD_RESET寄存器

  ." BH1750初始化完成" CR
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
  BH1750-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

\ MCP4921设备定义 - Forth文件
\ 生成自: Microchip/DAC/MCP4921
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: MCP4921 12-bit SPI DAC (single channel, 2x buffered output)
\ CPU架构: DAC
\ 位宽: 12位
\ 时钟频率: 20000000 Hz

\ =========================================
\ MCP4921设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" MCP4921" ;
: MANUFACTURER  S" Microchip" ;
: FAMILY        S" DAC" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" DAC" ;
12 CONSTANT BITS
20000000 CONSTANT CLOCK-FREQ

\ 外设定义
\ MCP4921 12-bit DAC (SPI, 2.7V-5.5V)
0x00 CONSTANT MCP4921-BASE
0x00 CONSTANT MCP4921-DAC_VALUE
14 CONSTANT MCP4921-DAC_VALUE-BUF  \ VREF buffer (0=unbuffered, 1=buffered)
13 CONSTANT MCP4921-DAC_VALUE-GA  \ Gain (0=2x, 1=1x)
12 CONSTANT MCP4921-DAC_VALUE-SHDN  \ Shutdown (0=shutdown, 1=active)
0x02 CONSTANT MCP4921-VREF

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ MCP4921外设
: MCP4921-DAC_VALUE@ ( -- n ) MCP4921-DAC_VALUE @ ;
: MCP4921-DAC_VALUE! ( n -- ) MCP4921-DAC_VALUE ! ;
: MCP4921-DAC_VALUE-BUF@ ( -- flag ) MCP4921-DAC_VALUE@ 14 BIT@ ;
: MCP4921-DAC_VALUE-BUF! ( flag -- ) MCP4921-DAC_VALUE@ 14 BIT! MCP4921-DAC_VALUE! ;
: MCP4921-DAC_VALUE-GA@ ( -- flag ) MCP4921-DAC_VALUE@ 13 BIT@ ;
: MCP4921-DAC_VALUE-GA! ( flag -- ) MCP4921-DAC_VALUE@ 13 BIT! MCP4921-DAC_VALUE! ;
: MCP4921-DAC_VALUE-SHDN@ ( -- flag ) MCP4921-DAC_VALUE@ 12 BIT@ ;
: MCP4921-DAC_VALUE-SHDN! ( flag -- ) MCP4921-DAC_VALUE@ 12 BIT! MCP4921-DAC_VALUE! ;
: MCP4921-VREF@ ( -- n ) MCP4921-VREF @ ;
: MCP4921-VREF! ( n -- ) MCP4921-VREF ! ;

\ =========================================
\ 设备初始化
\ =========================================

: MCP4921-INIT ( -- )
  \ 初始化MCP4921设备
  ." 初始化MCP4921..." CR


  \ 初始化外设
  \ 初始化MCP4921
  0 MCP4921-DAC_VALUE!  \ DAC_VALUE寄存器
  0 MCP4921-VREF!  \ VREF寄存器

  ." MCP4921初始化完成" CR
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
  MCP4921-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

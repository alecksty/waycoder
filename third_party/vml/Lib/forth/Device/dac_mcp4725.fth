\ MCP4725设备定义 - Forth文件
\ 生成自: Microchip/DAC/MCP4725
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: MCP4725 12-bit I2C DAC (single channel, EEPROM)
\ CPU架构: DAC
\ 位宽: 12位
\ 时钟频率: 400000 Hz

\ =========================================
\ MCP4725设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" MCP4725" ;
: MANUFACTURER  S" Microchip" ;
: FAMILY        S" DAC" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" DAC" ;
12 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT EEPROM-START
0x01 CONSTANT EEPROM-END
2 CONSTANT EEPROM-SIZE  \ Power-on default DAC value

\ 外设定义
\ MCP4725 12-bit DAC (0x60-0x67, 2.7V-5.5V)
0x60 CONSTANT MCP4725-BASE
0x00 CONSTANT MCP4725-DAC_VALUE
12 CONSTANT MCP4725-DAC_VALUE-PD  \ Power-down: 0=normal,1=1kΩ,2=100kΩ,3=500kΩ
0x60 CONSTANT MCP4725-WRITE_EEPROM

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ MCP4725外设
: MCP4725-DAC_VALUE@ ( -- n ) MCP4725-DAC_VALUE @ ;
: MCP4725-DAC_VALUE! ( n -- ) MCP4725-DAC_VALUE ! ;
: MCP4725-DAC_VALUE-PD@ ( -- flag ) MCP4725-DAC_VALUE@ 12 BIT@ ;
: MCP4725-DAC_VALUE-PD! ( flag -- ) MCP4725-DAC_VALUE@ 12 BIT! MCP4725-DAC_VALUE! ;
: MCP4725-WRITE_EEPROM@ ( -- n ) MCP4725-WRITE_EEPROM @ ;
: MCP4725-WRITE_EEPROM! ( n -- ) MCP4725-WRITE_EEPROM ! ;

\ =========================================
\ 设备初始化
\ =========================================

: MCP4725-INIT ( -- )
  \ 初始化MCP4725设备
  ." 初始化MCP4725..." CR


  \ 初始化外设
  \ 初始化MCP4725
  0 MCP4725-DAC_VALUE!  \ DAC_VALUE寄存器
  0 MCP4725-WRITE_EEPROM!  \ WRITE_EEPROM寄存器

  ." MCP4725初始化完成" CR
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
  MCP4725-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

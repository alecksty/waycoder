\ MCP23017设备定义 - Forth文件
\ 生成自: Microchip/GPIO/MCP23017
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: MCP23017 16-bit I2C GPIO Expander (2 banks, interrupt, 25mA per pin)
\ CPU架构: GPIO
\ 位宽: 16位
\ 时钟频率: 400000 Hz

\ =========================================
\ MCP23017设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" MCP23017" ;
: MANUFACTURER  S" Microchip" ;
: FAMILY        S" GPIO" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" GPIO" ;
16 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 外设定义
\ MCP23017 16-bit GPIO (0x20-0x27, 1.8V-5.5V)
0x20 CONSTANT MCP23017-BASE
0x00 CONSTANT MCP23017-IODIRA
0x01 CONSTANT MCP23017-IODIRB
0x12 CONSTANT MCP23017-GPIOA
0x13 CONSTANT MCP23017-GPIOB
0x04 CONSTANT MCP23017-GPINTENA
0x05 CONSTANT MCP23017-GPINTENB
0x08 CONSTANT MCP23017-INTCONA
0x0A CONSTANT MCP23017-IOCON
0x0C CONSTANT MCP23017-GPPUA
0x0D CONSTANT MCP23017-GPPUB

\ 中断向量定义
0 CONSTANT INT-INTA  \ Port A interrupt
1 CONSTANT INT-INTB  \ Port B interrupt

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ MCP23017外设
: MCP23017-IODIRA@ ( -- n ) MCP23017-IODIRA C@ ;
: MCP23017-IODIRA! ( n -- ) MCP23017-IODIRA C! ;
: MCP23017-IODIRB@ ( -- n ) MCP23017-IODIRB C@ ;
: MCP23017-IODIRB! ( n -- ) MCP23017-IODIRB C! ;
: MCP23017-GPIOA@ ( -- n ) MCP23017-GPIOA C@ ;
: MCP23017-GPIOA! ( n -- ) MCP23017-GPIOA C! ;
: MCP23017-GPIOB@ ( -- n ) MCP23017-GPIOB C@ ;
: MCP23017-GPIOB! ( n -- ) MCP23017-GPIOB C! ;
: MCP23017-GPINTENA@ ( -- n ) MCP23017-GPINTENA C@ ;
: MCP23017-GPINTENA! ( n -- ) MCP23017-GPINTENA C! ;
: MCP23017-GPINTENB@ ( -- n ) MCP23017-GPINTENB C@ ;
: MCP23017-GPINTENB! ( n -- ) MCP23017-GPINTENB C! ;
: MCP23017-INTCONA@ ( -- n ) MCP23017-INTCONA C@ ;
: MCP23017-INTCONA! ( n -- ) MCP23017-INTCONA C! ;
: MCP23017-IOCON@ ( -- n ) MCP23017-IOCON C@ ;
: MCP23017-IOCON! ( n -- ) MCP23017-IOCON C! ;
: MCP23017-GPPUA@ ( -- n ) MCP23017-GPPUA C@ ;
: MCP23017-GPPUA! ( n -- ) MCP23017-GPPUA C! ;
: MCP23017-GPPUB@ ( -- n ) MCP23017-GPPUB C@ ;
: MCP23017-GPPUB! ( n -- ) MCP23017-GPPUB C! ;

\ =========================================
\ 设备初始化
\ =========================================

: MCP23017-INIT ( -- )
  \ 初始化MCP23017设备
  ." 初始化MCP23017..." CR


  \ 初始化外设
  \ 初始化MCP23017
  0 MCP23017-IODIRA!  \ IODIRA寄存器
  0 MCP23017-IODIRB!  \ IODIRB寄存器
  0 MCP23017-GPIOA!  \ GPIOA寄存器
  0 MCP23017-GPIOB!  \ GPIOB寄存器
  0 MCP23017-GPINTENA!  \ GPINTENA寄存器
  0 MCP23017-GPINTENB!  \ GPINTENB寄存器
  0 MCP23017-INTCONA!  \ INTCONA寄存器
  0 MCP23017-IOCON!  \ IOCON寄存器
  0 MCP23017-GPPUA!  \ GPPUA寄存器
  0 MCP23017-GPPUB!  \ GPPUB寄存器

  ." MCP23017初始化完成" CR
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
\ 中断处理
\ =========================================

\ Port A interrupt
: INT-INTA-HANDLER ( -- )
  ." INTA中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INTA-ENABLE ( -- )
  INT-INTA INT-ENABLE
;

: INT-INTA-DISABLE ( -- )
  INT-INTA INT-DISABLE
;

\ Port B interrupt
: INT-INTB-HANDLER ( -- )
  ." INTB中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-INTB-ENABLE ( -- )
  INT-INTB INT-ENABLE
;

: INT-INTB-DISABLE ( -- )
  INT-INTB INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  MCP23017-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

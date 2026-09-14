\ HD44780设备定义 - Forth文件
\ 生成自: Hitachi/Display/HD44780
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: HD44780 16x2 Character LCD Controller (4-bit/8-bit parallel or I2C via PCF8574)
\ CPU架构: Display
\ 位宽: 8位
\ 时钟频率: 0 Hz

\ =========================================
\ HD44780设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" HD44780" ;
: MANUFACTURER  S" Hitachi" ;
: FAMILY        S" Display" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Display" ;
8 CONSTANT BITS
0 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT DDRAM-START
0x4F CONSTANT DDRAM-END
80 CONSTANT DDRAM-SIZE  \ Display Data RAM (80 bytes, 2 lines)
0x00 CONSTANT CGRAM-START
0x3F CONSTANT CGRAM-END
64 CONSTANT CGRAM-SIZE  \ Character Generator RAM (8 custom chars x 8 bytes)

\ 外设定义
\ HD44780 16x2 LCD (0x27/0x3F I2C, 5V)
0x27 CONSTANT HD44780-BASE
0x00 CONSTANT HD44780-CMD
0x01 CONSTANT HD44780-DATA
0x00 CONSTANT HD44780-CTRL_RS
0x01 CONSTANT HD44780-CTRL_RW
0x02 CONSTANT HD44780-CTRL_EN
0x03 CONSTANT HD44780-CTRL_BL
0x80 CONSTANT HD44780-ADDR_DDRAM
0x40 CONSTANT HD44780-ADDR_CGRAM

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ HD44780外设
: HD44780-CMD@ ( -- n ) HD44780-CMD C@ ;
: HD44780-CMD! ( n -- ) HD44780-CMD C! ;
: HD44780-DATA@ ( -- n ) HD44780-DATA C@ ;
: HD44780-DATA! ( n -- ) HD44780-DATA C! ;
: HD44780-CTRL_RS@ ( -- n ) HD44780-CTRL_RS C@ ;
: HD44780-CTRL_RS! ( n -- ) HD44780-CTRL_RS C! ;
: HD44780-CTRL_RW@ ( -- n ) HD44780-CTRL_RW C@ ;
: HD44780-CTRL_RW! ( n -- ) HD44780-CTRL_RW C! ;
: HD44780-CTRL_EN@ ( -- n ) HD44780-CTRL_EN C@ ;
: HD44780-CTRL_EN! ( n -- ) HD44780-CTRL_EN C! ;
: HD44780-CTRL_BL@ ( -- n ) HD44780-CTRL_BL C@ ;
: HD44780-CTRL_BL! ( n -- ) HD44780-CTRL_BL C! ;
: HD44780-ADDR_DDRAM@ ( -- n ) HD44780-ADDR_DDRAM C@ ;
: HD44780-ADDR_DDRAM! ( n -- ) HD44780-ADDR_DDRAM C! ;
: HD44780-ADDR_CGRAM@ ( -- n ) HD44780-ADDR_CGRAM C@ ;
: HD44780-ADDR_CGRAM! ( n -- ) HD44780-ADDR_CGRAM C! ;

\ =========================================
\ 设备初始化
\ =========================================

: HD44780-INIT ( -- )
  \ 初始化HD44780设备
  ." 初始化HD44780..." CR


  \ 初始化外设
  \ 初始化HD44780
  0 HD44780-CMD!  \ CMD寄存器
  0 HD44780-DATA!  \ DATA寄存器
  0 HD44780-CTRL_RS!  \ CTRL_RS寄存器
  0 HD44780-CTRL_RW!  \ CTRL_RW寄存器
  0 HD44780-CTRL_EN!  \ CTRL_EN寄存器
  0 HD44780-CTRL_BL!  \ CTRL_BL寄存器
  0 HD44780-ADDR_DDRAM!  \ ADDR_DDRAM寄存器
  0 HD44780-ADDR_CGRAM!  \ ADDR_CGRAM寄存器

  ." HD44780初始化完成" CR
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
  HD44780-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

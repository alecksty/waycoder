\ PCF8574设备定义 - Forth文件
\ 生成自: NXP/TI/GPIO/PCF8574
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: PCF8574 8-bit I2C GPIO Expander (quasi-bidirectional, interrupt)
\ CPU架构: GPIO
\ 位宽: 8位
\ 时钟频率: 100000 Hz

\ =========================================
\ PCF8574设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" PCF8574" ;
: MANUFACTURER  S" NXP/TI" ;
: FAMILY        S" GPIO" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" GPIO" ;
8 CONSTANT BITS
100000 CONSTANT CLOCK-FREQ

\ 外设定义
\ PCF8574 8-bit GPIO (0x20-0x27, 2.5V-6V)
0x20 CONSTANT PCF8574-BASE
0x00 CONSTANT PCF8574-INPUT
0 CONSTANT PCF8574-INPUT-P0  \ Pin P0
1 CONSTANT PCF8574-INPUT-P1  \ Pin P1
2 CONSTANT PCF8574-INPUT-P2  \ Pin P2
3 CONSTANT PCF8574-INPUT-P3  \ Pin P3
4 CONSTANT PCF8574-INPUT-P4  \ Pin P4
5 CONSTANT PCF8574-INPUT-P5  \ Pin P5
6 CONSTANT PCF8574-INPUT-P6  \ Pin P6
7 CONSTANT PCF8574-INPUT-P7  \ Pin P7
0x01 CONSTANT PCF8574-OUTPUT
0x02 CONSTANT PCF8574-POLARITY

\ 中断向量定义
0 CONSTANT INT-INT  \ Pin change interrupt (open-drain, active low)

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ PCF8574外设
: PCF8574-INPUT@ ( -- n ) PCF8574-INPUT C@ ;
: PCF8574-INPUT! ( n -- ) PCF8574-INPUT C! ;
: PCF8574-INPUT-P0@ ( -- flag ) PCF8574-INPUT@ 0 BIT@ ;
: PCF8574-INPUT-P0! ( flag -- ) PCF8574-INPUT@ 0 BIT! PCF8574-INPUT! ;
: PCF8574-INPUT-P1@ ( -- flag ) PCF8574-INPUT@ 1 BIT@ ;
: PCF8574-INPUT-P1! ( flag -- ) PCF8574-INPUT@ 1 BIT! PCF8574-INPUT! ;
: PCF8574-INPUT-P2@ ( -- flag ) PCF8574-INPUT@ 2 BIT@ ;
: PCF8574-INPUT-P2! ( flag -- ) PCF8574-INPUT@ 2 BIT! PCF8574-INPUT! ;
: PCF8574-INPUT-P3@ ( -- flag ) PCF8574-INPUT@ 3 BIT@ ;
: PCF8574-INPUT-P3! ( flag -- ) PCF8574-INPUT@ 3 BIT! PCF8574-INPUT! ;
: PCF8574-INPUT-P4@ ( -- flag ) PCF8574-INPUT@ 4 BIT@ ;
: PCF8574-INPUT-P4! ( flag -- ) PCF8574-INPUT@ 4 BIT! PCF8574-INPUT! ;
: PCF8574-INPUT-P5@ ( -- flag ) PCF8574-INPUT@ 5 BIT@ ;
: PCF8574-INPUT-P5! ( flag -- ) PCF8574-INPUT@ 5 BIT! PCF8574-INPUT! ;
: PCF8574-INPUT-P6@ ( -- flag ) PCF8574-INPUT@ 6 BIT@ ;
: PCF8574-INPUT-P6! ( flag -- ) PCF8574-INPUT@ 6 BIT! PCF8574-INPUT! ;
: PCF8574-INPUT-P7@ ( -- flag ) PCF8574-INPUT@ 7 BIT@ ;
: PCF8574-INPUT-P7! ( flag -- ) PCF8574-INPUT@ 7 BIT! PCF8574-INPUT! ;
: PCF8574-OUTPUT@ ( -- n ) PCF8574-OUTPUT C@ ;
: PCF8574-OUTPUT! ( n -- ) PCF8574-OUTPUT C! ;
: PCF8574-POLARITY@ ( -- n ) PCF8574-POLARITY C@ ;
: PCF8574-POLARITY! ( n -- ) PCF8574-POLARITY C! ;

\ =========================================
\ 设备初始化
\ =========================================

: PCF8574-INIT ( -- )
  \ 初始化PCF8574设备
  ." 初始化PCF8574..." CR


  \ 初始化外设
  \ 初始化PCF8574
  0 PCF8574-INPUT!  \ INPUT寄存器
  0 PCF8574-OUTPUT!  \ OUTPUT寄存器
  0 PCF8574-POLARITY!  \ POLARITY寄存器

  ." PCF8574初始化完成" CR
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

\ Pin change interrupt (open-drain, active low)
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
  PCF8574-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

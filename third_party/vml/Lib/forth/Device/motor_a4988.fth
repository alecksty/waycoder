\ A4988设备定义 - Forth文件
\ 生成自: Allegro/Motor/A4988
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: A4988 Stepper Motor Driver (up to 1/16 microstepping, 2A, 8V-35V)
\ CPU架构: Motor
\ 位宽: 8位
\ 时钟频率: 0 Hz

\ =========================================
\ A4988设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" A4988" ;
: MANUFACTURER  S" Allegro" ;
: FAMILY        S" Motor" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Motor" ;
8 CONSTANT BITS
0 CONSTANT CLOCK-FREQ

\ 外设定义
\ A4988 Stepper Motor Driver (3.3V/5V logic)
0x00 CONSTANT A4988-BASE
0x00 CONSTANT A4988-CTRL
0 CONSTANT A4988-CTRL-STEP  \ Step pulse (rising edge)
1 CONSTANT A4988-CTRL-DIR  \ Direction (0=CW, 1=CCW)
2 CONSTANT A4988-CTRL-ENABLE  \ Enable (active low)
3 CONSTANT A4988-CTRL-SLEEP  \ Sleep mode (active low)
4 CONSTANT A4988-CTRL-RESET  \ Reset (active low)
0x01 CONSTANT A4988-MICROSTEP
0 CONSTANT A4988-MICROSTEP-MS1  \ Microstep select 1
1 CONSTANT A4988-MICROSTEP-MS2  \ Microstep select 2
2 CONSTANT A4988-MICROSTEP-MS3  \ Microstep select 3
0x02 CONSTANT A4988-STEPS
0x06 CONSTANT A4988-DELAY_US

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ A4988外设
: A4988-CTRL@ ( -- n ) A4988-CTRL C@ ;
: A4988-CTRL! ( n -- ) A4988-CTRL C! ;
: A4988-CTRL-STEP@ ( -- flag ) A4988-CTRL@ 0 BIT@ ;
: A4988-CTRL-STEP! ( flag -- ) A4988-CTRL@ 0 BIT! A4988-CTRL! ;
: A4988-CTRL-DIR@ ( -- flag ) A4988-CTRL@ 1 BIT@ ;
: A4988-CTRL-DIR! ( flag -- ) A4988-CTRL@ 1 BIT! A4988-CTRL! ;
: A4988-CTRL-ENABLE@ ( -- flag ) A4988-CTRL@ 2 BIT@ ;
: A4988-CTRL-ENABLE! ( flag -- ) A4988-CTRL@ 2 BIT! A4988-CTRL! ;
: A4988-CTRL-SLEEP@ ( -- flag ) A4988-CTRL@ 3 BIT@ ;
: A4988-CTRL-SLEEP! ( flag -- ) A4988-CTRL@ 3 BIT! A4988-CTRL! ;
: A4988-CTRL-RESET@ ( -- flag ) A4988-CTRL@ 4 BIT@ ;
: A4988-CTRL-RESET! ( flag -- ) A4988-CTRL@ 4 BIT! A4988-CTRL! ;
: A4988-MICROSTEP@ ( -- n ) A4988-MICROSTEP C@ ;
: A4988-MICROSTEP! ( n -- ) A4988-MICROSTEP C! ;
: A4988-MICROSTEP-MS1@ ( -- flag ) A4988-MICROSTEP@ 0 BIT@ ;
: A4988-MICROSTEP-MS1! ( flag -- ) A4988-MICROSTEP@ 0 BIT! A4988-MICROSTEP! ;
: A4988-MICROSTEP-MS2@ ( -- flag ) A4988-MICROSTEP@ 1 BIT@ ;
: A4988-MICROSTEP-MS2! ( flag -- ) A4988-MICROSTEP@ 1 BIT! A4988-MICROSTEP! ;
: A4988-MICROSTEP-MS3@ ( -- flag ) A4988-MICROSTEP@ 2 BIT@ ;
: A4988-MICROSTEP-MS3! ( flag -- ) A4988-MICROSTEP@ 2 BIT! A4988-MICROSTEP! ;
: A4988-STEPS@ ( -- n ) A4988-STEPS L@ ;
: A4988-STEPS! ( n -- ) A4988-STEPS L! ;
: A4988-DELAY_US@ ( -- n ) A4988-DELAY_US @ ;
: A4988-DELAY_US! ( n -- ) A4988-DELAY_US ! ;

\ =========================================
\ 设备初始化
\ =========================================

: A4988-INIT ( -- )
  \ 初始化A4988设备
  ." 初始化A4988..." CR


  \ 初始化外设
  \ 初始化A4988
  0 A4988-CTRL!  \ CTRL寄存器
  0 A4988-MICROSTEP!  \ MICROSTEP寄存器
  0 A4988-STEPS!  \ STEPS寄存器
  0 A4988-DELAY_US!  \ DELAY_US寄存器

  ." A4988初始化完成" CR
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
  A4988-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

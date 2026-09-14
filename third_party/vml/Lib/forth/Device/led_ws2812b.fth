\ WS2812B设备定义 - Forth文件
\ 生成自: Worldsemi/LED/WS2812B
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: WS2812B Intelligent RGB LED (single-wire, 800KHz, daisy-chainable)
\ CPU架构: LED
\ 位宽: 24位
\ 时钟频率: 800000 Hz

\ =========================================
\ WS2812B设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" WS2812B" ;
: MANUFACTURER  S" Worldsemi" ;
: FAMILY        S" LED" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" LED" ;
24 CONSTANT BITS
800000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT LED_FB-START
0xFF CONSTANT LED_FB-END
256 CONSTANT LED_FB-SIZE  \ Frame buffer (up to 256 LEDs × 3 bytes)

\ 外设定义
\ WS2812B RGB LED Strip (5V, 60mA/led)
0x00 CONSTANT WS2812B-BASE
0x00 CONSTANT WS2812B-LED_COUNT
0x02 CONSTANT WS2812B-LED_DATA
0x05 CONSTANT WS2812B-BRIGHTNESS
0x06 CONSTANT WS2812B-SHOW
0x07 CONSTANT WS2812B-CLEAR

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ WS2812B外设
: WS2812B-LED_COUNT@ ( -- n ) WS2812B-LED_COUNT @ ;
: WS2812B-LED_COUNT! ( n -- ) WS2812B-LED_COUNT ! ;
: WS2812B-LED_DATA@ ( -- n ) WS2812B-LED_DATA  3 CHARS@ ;
: WS2812B-LED_DATA! ( n -- ) WS2812B-LED_DATA  3 CHARS! ;
: WS2812B-BRIGHTNESS@ ( -- n ) WS2812B-BRIGHTNESS C@ ;
: WS2812B-BRIGHTNESS! ( n -- ) WS2812B-BRIGHTNESS C! ;
: WS2812B-SHOW@ ( -- n ) WS2812B-SHOW C@ ;
: WS2812B-SHOW! ( n -- ) WS2812B-SHOW C! ;
: WS2812B-CLEAR@ ( -- n ) WS2812B-CLEAR C@ ;
: WS2812B-CLEAR! ( n -- ) WS2812B-CLEAR C! ;

\ =========================================
\ 设备初始化
\ =========================================

: WS2812B-INIT ( -- )
  \ 初始化WS2812B设备
  ." 初始化WS2812B..." CR


  \ 初始化外设
  \ 初始化WS2812B
  0 WS2812B-LED_COUNT!  \ LED_COUNT寄存器
  0 WS2812B-LED_DATA!  \ LED_DATA寄存器
  0 WS2812B-BRIGHTNESS!  \ BRIGHTNESS寄存器
  0 WS2812B-SHOW!  \ SHOW寄存器
  0 WS2812B-CLEAR!  \ CLEAR寄存器

  ." WS2812B初始化完成" CR
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
  WS2812B-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

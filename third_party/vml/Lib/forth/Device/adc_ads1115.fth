\ ADS1115设备定义 - Forth文件
\ 生成自: Texas Instruments/ADC/ADS1115
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: ADS1115 16-bit I2C ADC (4-channel, PGA, 860SPS)
\ CPU架构: ADC
\ 位宽: 16位
\ 时钟频率: 400000 Hz

\ =========================================
\ ADS1115设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ADS1115" ;
: MANUFACTURER  S" Texas Instruments" ;
: FAMILY        S" ADC" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ADC" ;
16 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 外设定义
\ ADS1115 16-bit ADC (0x48-0x4B, 2.0V-5.5V)
0x48 CONSTANT ADS1115-BASE
0x00 CONSTANT ADS1115-CONV_RESULT
0x01 CONSTANT ADS1115-CONFIG
15 CONSTANT ADS1115-CONFIG-OS  \ Operational status/start single-shot
12 CONSTANT ADS1115-CONFIG-MUX  \ Input multiplexer: 0=A0-A1,1=A0-A3,2=A1-A3,3=A2-A3,4=A0,5=A1,6=A2,7=A3
9 CONSTANT ADS1115-CONFIG-PGA  \ PGA gain: 0=±6.144V,1=±4.096V,2=±2.048V,3=±1.024V,4=±0.512V,5=±0.256V
8 CONSTANT ADS1115-CONFIG-MODE  \ 0=continuous, 1=single-shot
5 CONSTANT ADS1115-CONFIG-DR  \ Data rate: 0=8,1=16,2=32,3=64,4=128,5=250,6=475,7=860 SPS
4 CONSTANT ADS1115-CONFIG-COMP_MODE  \ Comparator mode (0=traditional, 1=window)
3 CONSTANT ADS1115-CONFIG-COMP_POL  \ Comparator polarity (0=active low, 1=active high)
0x02 CONSTANT ADS1115-LO_THRESH
0x03 CONSTANT ADS1115-HI_THRESH

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ ADS1115外设
: ADS1115-CONV_RESULT@ ( -- n ) ADS1115-CONV_RESULT @ ;
: ADS1115-CONV_RESULT! ( n -- ) ADS1115-CONV_RESULT ! ;
: ADS1115-CONFIG@ ( -- n ) ADS1115-CONFIG @ ;
: ADS1115-CONFIG! ( n -- ) ADS1115-CONFIG ! ;
: ADS1115-CONFIG-OS@ ( -- flag ) ADS1115-CONFIG@ 15 BIT@ ;
: ADS1115-CONFIG-OS! ( flag -- ) ADS1115-CONFIG@ 15 BIT! ADS1115-CONFIG! ;
: ADS1115-CONFIG-MUX@ ( -- flag ) ADS1115-CONFIG@ 12 BIT@ ;
: ADS1115-CONFIG-MUX! ( flag -- ) ADS1115-CONFIG@ 12 BIT! ADS1115-CONFIG! ;
: ADS1115-CONFIG-PGA@ ( -- flag ) ADS1115-CONFIG@ 9 BIT@ ;
: ADS1115-CONFIG-PGA! ( flag -- ) ADS1115-CONFIG@ 9 BIT! ADS1115-CONFIG! ;
: ADS1115-CONFIG-MODE@ ( -- flag ) ADS1115-CONFIG@ 8 BIT@ ;
: ADS1115-CONFIG-MODE! ( flag -- ) ADS1115-CONFIG@ 8 BIT! ADS1115-CONFIG! ;
: ADS1115-CONFIG-DR@ ( -- flag ) ADS1115-CONFIG@ 5 BIT@ ;
: ADS1115-CONFIG-DR! ( flag -- ) ADS1115-CONFIG@ 5 BIT! ADS1115-CONFIG! ;
: ADS1115-CONFIG-COMP_MODE@ ( -- flag ) ADS1115-CONFIG@ 4 BIT@ ;
: ADS1115-CONFIG-COMP_MODE! ( flag -- ) ADS1115-CONFIG@ 4 BIT! ADS1115-CONFIG! ;
: ADS1115-CONFIG-COMP_POL@ ( -- flag ) ADS1115-CONFIG@ 3 BIT@ ;
: ADS1115-CONFIG-COMP_POL! ( flag -- ) ADS1115-CONFIG@ 3 BIT! ADS1115-CONFIG! ;
: ADS1115-LO_THRESH@ ( -- n ) ADS1115-LO_THRESH @ ;
: ADS1115-LO_THRESH! ( n -- ) ADS1115-LO_THRESH ! ;
: ADS1115-HI_THRESH@ ( -- n ) ADS1115-HI_THRESH @ ;
: ADS1115-HI_THRESH! ( n -- ) ADS1115-HI_THRESH ! ;

\ =========================================
\ 设备初始化
\ =========================================

: ADS1115-INIT ( -- )
  \ 初始化ADS1115设备
  ." 初始化ADS1115..." CR


  \ 初始化外设
  \ 初始化ADS1115
  0 ADS1115-CONV_RESULT!  \ CONV_RESULT寄存器
  0 ADS1115-CONFIG!  \ CONFIG寄存器
  0 ADS1115-LO_THRESH!  \ LO_THRESH寄存器
  0 ADS1115-HI_THRESH!  \ HI_THRESH寄存器

  ." ADS1115初始化完成" CR
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
  ADS1115-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

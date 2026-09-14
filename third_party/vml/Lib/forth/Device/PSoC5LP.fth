\ CY8C5888LTI-LP097设备定义 - Forth文件
\ 生成自: Cypress (Infineon)/PSoC/CY8C5888LTI-LP097
\ 版本: 1.0
\ 日期: 2026-04-29
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M3 PSoC 5LP with 256KB Flash, 64KB SRAM, 80MHz, UDB
\ CPU架构: ARM-Cortex-M3
\ 位宽: 32位
\ 时钟频率: 80000000 Hz

\ =========================================
\ CY8C5888LTI-LP097设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" CY8C5888LTI-LP097" ;
: MANUFACTURER  S" Cypress (Infineon)" ;
: FAMILY        S" PSoC" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M3" ;
32 CONSTANT BITS
80000000 CONSTANT CLOCK-FREQ

\ 外设定义
\ SCB UART (可编程)
0x40050000 CONSTANT UART-BASE
0x00 CONSTANT UART-CTRL
0x04 CONSTANT UART-STATUS
0x08 CONSTANT UART-TX_DATA
0x0C CONSTANT UART-RX_DATA
\ SCB I2C
0x40051000 CONSTANT I2C-BASE
0x00 CONSTANT I2C-CTRL
0x04 CONSTANT I2C-STATUS
0x08 CONSTANT I2C-TX_DATA
0x0C CONSTANT I2C-RX_DATA
\ TCPWM 定时器
0x40060000 CONSTANT TIMER-BASE
0x00 CONSTANT TIMER-CTRL
0x04 CONSTANT TIMER-STATUS
0x08 CONSTANT TIMER-CNT
0x0C CONSTANT TIMER-PERIOD
0x10 CONSTANT TIMER-CC
\ DelSig ADC 20-bit
0x40100000 CONSTANT ADC-BASE
0x00 CONSTANT ADC-CTRL
0x04 CONSTANT ADC-STATUS
0x08 CONSTANT ADC-DATA
0x10 CONSTANT ADC-CLOCK
\ GPIO 端口
0x40040000 CONSTANT GPIO-BASE
0x00 CONSTANT GPIO-DR
0x04 CONSTANT GPIO-PS
0x08 CONSTANT GPIO-IE
0x0C CONSTANT GPIO-DM
\ USB 控制器
0x40080000 CONSTANT USB-BASE
0x00 CONSTANT USB-CR0
0x04 CONSTANT USB-CR1
0x08 CONSTANT USB-STAT

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ UART外设
: UART-CTRL@ ( -- n ) UART-CTRL L@ ;
: UART-CTRL! ( n -- ) UART-CTRL L! ;
: UART-STATUS@ ( -- n ) UART-STATUS L@ ;
: UART-STATUS! ( n -- ) UART-STATUS L! ;
: UART-TX_DATA@ ( -- n ) UART-TX_DATA L@ ;
: UART-TX_DATA! ( n -- ) UART-TX_DATA L! ;
: UART-RX_DATA@ ( -- n ) UART-RX_DATA L@ ;
: UART-RX_DATA! ( n -- ) UART-RX_DATA L! ;

\ I2C外设
: I2C-CTRL@ ( -- n ) I2C-CTRL L@ ;
: I2C-CTRL! ( n -- ) I2C-CTRL L! ;
: I2C-STATUS@ ( -- n ) I2C-STATUS L@ ;
: I2C-STATUS! ( n -- ) I2C-STATUS L! ;
: I2C-TX_DATA@ ( -- n ) I2C-TX_DATA L@ ;
: I2C-TX_DATA! ( n -- ) I2C-TX_DATA L! ;
: I2C-RX_DATA@ ( -- n ) I2C-RX_DATA L@ ;
: I2C-RX_DATA! ( n -- ) I2C-RX_DATA L! ;

\ TIMER外设
: TIMER-CTRL@ ( -- n ) TIMER-CTRL L@ ;
: TIMER-CTRL! ( n -- ) TIMER-CTRL L! ;
: TIMER-STATUS@ ( -- n ) TIMER-STATUS L@ ;
: TIMER-STATUS! ( n -- ) TIMER-STATUS L! ;
: TIMER-CNT@ ( -- n ) TIMER-CNT L@ ;
: TIMER-CNT! ( n -- ) TIMER-CNT L! ;
: TIMER-PERIOD@ ( -- n ) TIMER-PERIOD L@ ;
: TIMER-PERIOD! ( n -- ) TIMER-PERIOD L! ;
: TIMER-CC@ ( -- n ) TIMER-CC L@ ;
: TIMER-CC! ( n -- ) TIMER-CC L! ;

\ ADC外设
: ADC-CTRL@ ( -- n ) ADC-CTRL L@ ;
: ADC-CTRL! ( n -- ) ADC-CTRL L! ;
: ADC-STATUS@ ( -- n ) ADC-STATUS L@ ;
: ADC-STATUS! ( n -- ) ADC-STATUS L! ;
: ADC-DATA@ ( -- n ) ADC-DATA L@ ;
: ADC-DATA! ( n -- ) ADC-DATA L! ;
: ADC-CLOCK@ ( -- n ) ADC-CLOCK L@ ;
: ADC-CLOCK! ( n -- ) ADC-CLOCK L! ;

\ GPIO外设
: GPIO-DR@ ( -- n ) GPIO-DR L@ ;
: GPIO-DR! ( n -- ) GPIO-DR L! ;
: GPIO-PS@ ( -- n ) GPIO-PS L@ ;
: GPIO-PS! ( n -- ) GPIO-PS L! ;
: GPIO-IE@ ( -- n ) GPIO-IE L@ ;
: GPIO-IE! ( n -- ) GPIO-IE L! ;
: GPIO-DM@ ( -- n ) GPIO-DM L@ ;
: GPIO-DM! ( n -- ) GPIO-DM L! ;

\ USB外设
: USB-CR0@ ( -- n ) USB-CR0 L@ ;
: USB-CR0! ( n -- ) USB-CR0 L! ;
: USB-CR1@ ( -- n ) USB-CR1 L@ ;
: USB-CR1! ( n -- ) USB-CR1 L! ;
: USB-STAT@ ( -- n ) USB-STAT L@ ;
: USB-STAT! ( n -- ) USB-STAT L! ;

\ =========================================
\ 设备初始化
\ =========================================

: CY8C5888LTI_LP097-INIT ( -- )
  \ 初始化CY8C5888LTI-LP097设备
  ." 初始化CY8C5888LTI-LP097..." CR


  \ 初始化外设
  \ 初始化UART
  0 UART-CTRL!  \ CTRL寄存器
  0 UART-STATUS!  \ STATUS寄存器
  0 UART-TX_DATA!  \ TX_DATA寄存器
  0 UART-RX_DATA!  \ RX_DATA寄存器
  \ 初始化I2C
  0 I2C-CTRL!  \ CTRL寄存器
  0 I2C-STATUS!  \ STATUS寄存器
  0 I2C-TX_DATA!  \ TX_DATA寄存器
  0 I2C-RX_DATA!  \ RX_DATA寄存器
  \ 初始化TIMER
  0 TIMER-CTRL!  \ CTRL寄存器
  0 TIMER-STATUS!  \ STATUS寄存器
  0 TIMER-CNT!  \ CNT寄存器
  0 TIMER-PERIOD!  \ PERIOD寄存器
  0 TIMER-CC!  \ CC寄存器
  \ 初始化ADC
  0 ADC-CTRL!  \ CTRL寄存器
  0 ADC-STATUS!  \ STATUS寄存器
  0 ADC-DATA!  \ DATA寄存器
  0 ADC-CLOCK!  \ CLOCK寄存器
  \ 初始化GPIO
  0 GPIO-DR!  \ DR寄存器
  0 GPIO-PS!  \ PS寄存器
  0 GPIO-IE!  \ IE寄存器
  0 GPIO-DM!  \ DM寄存器
  \ 初始化USB
  0 USB-CR0!  \ CR0寄存器
  0 USB-CR1!  \ CR1寄存器
  0 USB-STAT!  \ STAT寄存器

  ." CY8C5888LTI-LP097初始化完成" CR
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
  CY8C5888LTI_LP097-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

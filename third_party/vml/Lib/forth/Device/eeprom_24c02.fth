\ 24C02设备定义 - Forth文件
\ 生成自: Generic/Memory/24C02
\ 版本: 1.0
\ 日期: 2026-05-06
\ 作者: VML Team
\ 描述: 2Kbit I2C Serial EEPROM (256 x 8 bits)
\ CPU架构: Memory
\ 位宽: 8位
\ 时钟频率: 400000 Hz

\ =========================================
\ 24C02设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" 24C02" ;
: MANUFACTURER  S" Generic" ;
: FAMILY        S" Memory" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Memory" ;
8 CONSTANT BITS
400000 CONSTANT CLOCK-FREQ

\ 内存段定义
0x00 CONSTANT EEPROM-START
0xFF CONSTANT EEPROM-END
256 CONSTANT EEPROM-SIZE  \ EEPROM main memory array (256 bytes, 8-byte page write)

\ 外设定义
\ 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8)
0x50 CONSTANT _24C02-BASE
0xFF CONSTANT _24C02-STATUS
0 CONSTANT _24C02-STATUS-BUSY  \ 1=Write in progress
0xFE CONSTANT _24C02-PAGE_SIZE
0xFD CONSTANT _24C02-SIZE

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ 24C02外设
: _24C02-STATUS@ ( -- n ) _24C02-STATUS C@ ;
: _24C02-STATUS! ( n -- ) _24C02-STATUS C! ;
: _24C02-STATUS-BUSY@ ( -- flag ) _24C02-STATUS@ 0 BIT@ ;
: _24C02-STATUS-BUSY! ( flag -- ) _24C02-STATUS@ 0 BIT! _24C02-STATUS! ;
: _24C02-PAGE_SIZE@ ( -- n ) _24C02-PAGE_SIZE C@ ;
: _24C02-PAGE_SIZE! ( n -- ) _24C02-PAGE_SIZE C! ;
: _24C02-SIZE@ ( -- n ) _24C02-SIZE @ ;
: _24C02-SIZE! ( n -- ) _24C02-SIZE ! ;

\ =========================================
\ 设备初始化
\ =========================================

: _24C02-INIT ( -- )
  \ 初始化24C02设备
  ." 初始化24C02..." CR


  \ 初始化外设
  \ 初始化24C02
  0 _24C02-STATUS!  \ STATUS寄存器
  0 _24C02-PAGE_SIZE!  \ PAGE_SIZE寄存器
  0 _24C02-SIZE!  \ SIZE寄存器

  ." 24C02初始化完成" CR
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
  _24C02-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

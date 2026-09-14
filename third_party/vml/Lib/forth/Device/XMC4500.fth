\ XMC4500设备定义 - Forth文件
\ 生成自: Infineon/XMC4000/XMC4500
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-M4 Industrial MCU with 1MB Flash, 160KB RAM, 120MHz
\ CPU架构: ARM-Cortex-M4
\ 位宽: 32位
\ 时钟频率: 120000000 Hz

\ =========================================
\ XMC4500设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" XMC4500" ;
: MANUFACTURER  S" Infineon" ;
: FAMILY        S" XMC4000" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-M4" ;
32 CONSTANT BITS
120000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT R0  \ 
0x04 CONSTANT R1  \ 
0x08 CONSTANT R2  \ 
0x0C CONSTANT R3  \ 
0x10 CONSTANT R4  \ 
0x14 CONSTANT R5  \ 
0x34 CONSTANT SP  \ 
0x38 CONSTANT LR  \ 
0x3C CONSTANT PC  \ 

\ 内存段定义
0x08000000 CONSTANT FLASH-START
0x080FFFFF CONSTANT FLASH-END
1048576 CONSTANT FLASH-SIZE  \ 
0x1FF00000 CONSTANT SRAM-START
0x1FF0FFFF CONSTANT SRAM-END
65536 CONSTANT SRAM-SIZE  \ 
0x20000000 CONSTANT SRAM_COM-START
0x20007FFF CONSTANT SRAM_COM-END
32768 CONSTANT SRAM_COM-SIZE  \ Communication Memory
0x20010000 CONSTANT SRAM_CPU-START
0x2001FFFF CONSTANT SRAM_CPU-END
65536 CONSTANT SRAM_CPU-SIZE  \ CPU SRAM
0x40000000 CONSTANT PERIPHERAL-START
0x4FFFFFFF CONSTANT PERIPHERAL-END
268435456 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ System Control Unit
0x40020000 CONSTANT SCU-BASE
0x00 CONSTANT SCU-CLKCR
0 CONSTANT SCU-CLKCR-PCLK_SEL  \ CPU clock selection
16 CONSTANT SCU-CLKCR-FBKDIV  \ Feedback divider
0x04 CONSTANT SCU-PLLCONFIG
0x08 CONSTANT SCU-OSCHPCTRL
0x20 CONSTANT SCU-CGATSET0
4 CONSTANT SCU-CGATSET0-CG_GATE_GPIO  \ GPIO gate enable
0x24 CONSTANT SCU-CGATCLR0
\ Port 0
0x48000000 CONSTANT PORT0-BASE
0x00 CONSTANT PORT0-OUT
0x04 CONSTANT PORT0-OMR
0x10 CONSTANT PORT0-IOCR0
0x14 CONSTANT PORT0-IOCR4
0x18 CONSTANT PORT0-IOCR8
0x1C CONSTANT PORT0-IOCR12
0x24 CONSTANT PORT0-IN
\ Port 1
0x48010000 CONSTANT PORT1-BASE
0x00 CONSTANT PORT1-OUT
0x04 CONSTANT PORT1-OMR
0x10 CONSTANT PORT1-IOCR0
0x14 CONSTANT PORT1-IOCR4
0x18 CONSTANT PORT1-IOCR8
0x1C CONSTANT PORT1-IOCR12
0x24 CONSTANT PORT1-IN
\ Port 2
0x48020000 CONSTANT PORT2-BASE
0x00 CONSTANT PORT2-OUT
0x04 CONSTANT PORT2-OMR
0x10 CONSTANT PORT2-IOCR0
0x14 CONSTANT PORT2-IOCR4
0x24 CONSTANT PORT2-IN
\ Universal Serial Interface 0 (UART)
0x48030000 CONSTANT USIC0-BASE
0x00 CONSTANT USIC0-CCR
0x04 CONSTANT USIC0-PCR
0x08 CONSTANT USIC0-RBUF
0x0C CONSTANT USIC0-TBUF
0x10 CONSTANT USIC0-BRG

\ 中断向量定义
0 CONSTANT INT-RESET  \ 
11 CONSTANT INT-SVCALL  \ 
12 CONSTANT INT-USIC0_SR0  \ USIC0 Service Request 0

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: R0@ ( -- n ) R0 L@ ;
: R0! ( n -- ) R0 L! ;

: R1@ ( -- n ) R1 L@ ;
: R1! ( n -- ) R1 L! ;

: R2@ ( -- n ) R2 L@ ;
: R2! ( n -- ) R2 L! ;

: R3@ ( -- n ) R3 L@ ;
: R3! ( n -- ) R3 L! ;

: R4@ ( -- n ) R4 L@ ;
: R4! ( n -- ) R4 L! ;

: R5@ ( -- n ) R5 L@ ;
: R5! ( n -- ) R5 L! ;

: SP@ ( -- n ) SP L@ ;
: SP! ( n -- ) SP L! ;

: LR@ ( -- n ) LR L@ ;
: LR! ( n -- ) LR L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

\ 外设访问
\ SCU外设
: SCU-CLKCR@ ( -- n ) SCU-CLKCR L@ ;
: SCU-CLKCR! ( n -- ) SCU-CLKCR L! ;
: SCU-CLKCR-PCLK_SEL@ ( -- flag ) SCU-CLKCR@ 0 BIT@ ;
: SCU-CLKCR-PCLK_SEL! ( flag -- ) SCU-CLKCR@ 0 BIT! SCU-CLKCR! ;
: SCU-CLKCR-FBKDIV@ ( -- flag ) SCU-CLKCR@ 16 BIT@ ;
: SCU-CLKCR-FBKDIV! ( flag -- ) SCU-CLKCR@ 16 BIT! SCU-CLKCR! ;
: SCU-PLLCONFIG@ ( -- n ) SCU-PLLCONFIG L@ ;
: SCU-PLLCONFIG! ( n -- ) SCU-PLLCONFIG L! ;
: SCU-OSCHPCTRL@ ( -- n ) SCU-OSCHPCTRL L@ ;
: SCU-OSCHPCTRL! ( n -- ) SCU-OSCHPCTRL L! ;
: SCU-CGATSET0@ ( -- n ) SCU-CGATSET0 L@ ;
: SCU-CGATSET0! ( n -- ) SCU-CGATSET0 L! ;
: SCU-CGATSET0-CG_GATE_GPIO@ ( -- flag ) SCU-CGATSET0@ 4 BIT@ ;
: SCU-CGATSET0-CG_GATE_GPIO! ( flag -- ) SCU-CGATSET0@ 4 BIT! SCU-CGATSET0! ;
: SCU-CGATCLR0@ ( -- n ) SCU-CGATCLR0 L@ ;
: SCU-CGATCLR0! ( n -- ) SCU-CGATCLR0 L! ;

\ PORT0外设
: PORT0-OUT@ ( -- n ) PORT0-OUT L@ ;
: PORT0-OUT! ( n -- ) PORT0-OUT L! ;
: PORT0-OMR@ ( -- n ) PORT0-OMR L@ ;
: PORT0-OMR! ( n -- ) PORT0-OMR L! ;
: PORT0-IOCR0@ ( -- n ) PORT0-IOCR0 L@ ;
: PORT0-IOCR0! ( n -- ) PORT0-IOCR0 L! ;
: PORT0-IOCR4@ ( -- n ) PORT0-IOCR4 L@ ;
: PORT0-IOCR4! ( n -- ) PORT0-IOCR4 L! ;
: PORT0-IOCR8@ ( -- n ) PORT0-IOCR8 L@ ;
: PORT0-IOCR8! ( n -- ) PORT0-IOCR8 L! ;
: PORT0-IOCR12@ ( -- n ) PORT0-IOCR12 L@ ;
: PORT0-IOCR12! ( n -- ) PORT0-IOCR12 L! ;
: PORT0-IN@ ( -- n ) PORT0-IN L@ ;
: PORT0-IN! ( n -- ) PORT0-IN L! ;

\ PORT1外设
: PORT1-OUT@ ( -- n ) PORT1-OUT L@ ;
: PORT1-OUT! ( n -- ) PORT1-OUT L! ;
: PORT1-OMR@ ( -- n ) PORT1-OMR L@ ;
: PORT1-OMR! ( n -- ) PORT1-OMR L! ;
: PORT1-IOCR0@ ( -- n ) PORT1-IOCR0 L@ ;
: PORT1-IOCR0! ( n -- ) PORT1-IOCR0 L! ;
: PORT1-IOCR4@ ( -- n ) PORT1-IOCR4 L@ ;
: PORT1-IOCR4! ( n -- ) PORT1-IOCR4 L! ;
: PORT1-IOCR8@ ( -- n ) PORT1-IOCR8 L@ ;
: PORT1-IOCR8! ( n -- ) PORT1-IOCR8 L! ;
: PORT1-IOCR12@ ( -- n ) PORT1-IOCR12 L@ ;
: PORT1-IOCR12! ( n -- ) PORT1-IOCR12 L! ;
: PORT1-IN@ ( -- n ) PORT1-IN L@ ;
: PORT1-IN! ( n -- ) PORT1-IN L! ;

\ PORT2外设
: PORT2-OUT@ ( -- n ) PORT2-OUT L@ ;
: PORT2-OUT! ( n -- ) PORT2-OUT L! ;
: PORT2-OMR@ ( -- n ) PORT2-OMR L@ ;
: PORT2-OMR! ( n -- ) PORT2-OMR L! ;
: PORT2-IOCR0@ ( -- n ) PORT2-IOCR0 L@ ;
: PORT2-IOCR0! ( n -- ) PORT2-IOCR0 L! ;
: PORT2-IOCR4@ ( -- n ) PORT2-IOCR4 L@ ;
: PORT2-IOCR4! ( n -- ) PORT2-IOCR4 L! ;
: PORT2-IN@ ( -- n ) PORT2-IN L@ ;
: PORT2-IN! ( n -- ) PORT2-IN L! ;

\ USIC0外设
: USIC0-CCR@ ( -- n ) USIC0-CCR L@ ;
: USIC0-CCR! ( n -- ) USIC0-CCR L! ;
: USIC0-PCR@ ( -- n ) USIC0-PCR L@ ;
: USIC0-PCR! ( n -- ) USIC0-PCR L! ;
: USIC0-RBUF@ ( -- n ) USIC0-RBUF L@ ;
: USIC0-RBUF! ( n -- ) USIC0-RBUF L! ;
: USIC0-TBUF@ ( -- n ) USIC0-TBUF L@ ;
: USIC0-TBUF! ( n -- ) USIC0-TBUF L! ;
: USIC0-BRG@ ( -- n ) USIC0-BRG L@ ;
: USIC0-BRG! ( n -- ) USIC0-BRG L! ;

\ =========================================
\ 设备初始化
\ =========================================

: XMC4500-INIT ( -- )
  \ 初始化XMC4500设备
  ." 初始化XMC4500..." CR

  \ 初始化寄存器
  0 R0!  \ 
  0 R1!  \ 
  0 R2!  \ 
  0 R3!  \ 
  0 R4!  \ 
  0 R5!  \ 
  0 SP!  \ 
  0 LR!  \ 
  0 PC!  \ 

  \ 初始化外设
  \ 初始化SCU
  0 SCU-CLKCR!  \ CLKCR寄存器
  0 SCU-PLLCONFIG!  \ PLLCONFIG寄存器
  0 SCU-OSCHPCTRL!  \ OSCHPCTRL寄存器
  0 SCU-CGATSET0!  \ CGATSET0寄存器
  0 SCU-CGATCLR0!  \ CGATCLR0寄存器
  \ 初始化PORT0
  0 PORT0-OUT!  \ OUT寄存器
  0 PORT0-OMR!  \ OMR寄存器
  0 PORT0-IOCR0!  \ IOCR0寄存器
  0 PORT0-IOCR4!  \ IOCR4寄存器
  0 PORT0-IOCR8!  \ IOCR8寄存器
  0 PORT0-IOCR12!  \ IOCR12寄存器
  0 PORT0-IN!  \ IN寄存器
  \ 初始化PORT1
  0 PORT1-OUT!  \ OUT寄存器
  0 PORT1-OMR!  \ OMR寄存器
  0 PORT1-IOCR0!  \ IOCR0寄存器
  0 PORT1-IOCR4!  \ IOCR4寄存器
  0 PORT1-IOCR8!  \ IOCR8寄存器
  0 PORT1-IOCR12!  \ IOCR12寄存器
  0 PORT1-IN!  \ IN寄存器
  \ 初始化PORT2
  0 PORT2-OUT!  \ OUT寄存器
  0 PORT2-OMR!  \ OMR寄存器
  0 PORT2-IOCR0!  \ IOCR0寄存器
  0 PORT2-IOCR4!  \ IOCR4寄存器
  0 PORT2-IN!  \ IN寄存器
  \ 初始化USIC0
  0 USIC0-CCR!  \ CCR寄存器
  0 USIC0-PCR!  \ PCR寄存器
  0 USIC0-RBUF!  \ RBUF寄存器
  0 USIC0-TBUF!  \ TBUF寄存器
  0 USIC0-BRG!  \ BRG寄存器

  ." XMC4500初始化完成" CR
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

: .REGISTERS ( -- )
  CR ." 寄存器状态:" CR
  ." ----------" CR
  R0@ R0 .R 8 .R SPACE ."  R0: " R0@ .
  R1@ R1 .R 8 .R SPACE ."  R1: " R1@ .
  R2@ R2 .R 8 .R SPACE ."  R2: " R2@ .
  R3@ R3 .R 8 .R SPACE ."  R3: " R3@ .
  R4@ R4 .R 8 .R SPACE ."  R4: " R4@ .
  R5@ R5 .R 8 .R SPACE ."  R5: " R5@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  LR@ LR .R 8 .R SPACE ."  LR: " LR@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ 
: INT-RESET-HANDLER ( -- )
  ." Reset中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET-ENABLE ( -- )
  INT-RESET INT-ENABLE
;

: INT-RESET-DISABLE ( -- )
  INT-RESET INT-DISABLE
;

\ 
: INT-SVCALL-HANDLER ( -- )
  ." SVCall中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SVCALL-ENABLE ( -- )
  INT-SVCALL INT-ENABLE
;

: INT-SVCALL-DISABLE ( -- )
  INT-SVCALL INT-DISABLE
;

\ USIC0 Service Request 0
: INT-USIC0_SR0-HANDLER ( -- )
  ." USIC0_SR0中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-USIC0_SR0-ENABLE ( -- )
  INT-USIC0_SR0 INT-ENABLE
;

: INT-USIC0_SR0-DISABLE ( -- )
  INT-USIC0_SR0 INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  XMC4500-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

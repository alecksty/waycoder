\ Allwinner H3设备定义 - Forth文件
\ 生成自: Allwinner/H-Series/Allwinner H3
\ 版本: 1.0
\ 日期: 2026-04-29
\ 作者: VML Team
\ 描述: 32-bit ARM Cortex-A7 Quad-core SoC with 512KB L2 Cache, 1.6GHz, Mali-400 GPU
\ CPU架构: ARM-Cortex-A7
\ 位宽: 32位
\ 时钟频率: 1200000000 Hz

\ =========================================
\ Allwinner H3设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Allwinner H3" ;
: MANUFACTURER  S" Allwinner" ;
: FAMILY        S" H-Series" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" ARM-Cortex-A7" ;
32 CONSTANT BITS
1200000000 CONSTANT CLOCK-FREQ

\ 外设定义
\ UART 0 (debug console)
0x01C28000 CONSTANT UART0-BASE
0x00 CONSTANT UART0-RBR
0x00 CONSTANT UART0-THR
0x04 CONSTANT UART0-IER
0x08 CONSTANT UART0-IIR
0x08 CONSTANT UART0-FCR
0x0C CONSTANT UART0-LCR
0x10 CONSTANT UART0-MCR
0x14 CONSTANT UART0-LSR
0x18 CONSTANT UART0-MSR
0x00 CONSTANT UART0-DLL
0x04 CONSTANT UART0-DLH
\ UART 1
0x01C28400 CONSTANT UART1-BASE
0x00 CONSTANT UART1-RBR
0x00 CONSTANT UART1-THR
0x14 CONSTANT UART1-LSR
\ GPIO 控制器
0x01C20800 CONSTANT GPIO-BASE
0x00 CONSTANT GPIO-PA_CFG0
0x04 CONSTANT GPIO-PA_CFG1
0x10 CONSTANT GPIO-PA_DAT
0x14 CONSTANT GPIO-PA_DRV0
0x1C CONSTANT GPIO-PA_PUL0
0x24 CONSTANT GPIO-PB_CFG0
0x34 CONSTANT GPIO-PB_DAT
0x48 CONSTANT GPIO-PC_CFG0
0x58 CONSTANT GPIO-PC_DAT
\ AVS 定时器
0x01C20C00 CONSTANT TIMER-BASE
0x00 CONSTANT TIMER-CNT0
0x04 CONSTANT TIMER-CNT1
0x08 CONSTANT TIMER-CTRL
0x0C CONSTANT TIMER-INTV
\ 时钟控制单元
0x01C20000 CONSTANT CCU-BASE
0x000 CONSTANT CCU-PLL1_CFG
0x010 CONSTANT CCU-PLL3_CFG
0x050 CONSTANT CCU-CPU_AXI_CFG
0x054 CONSTANT CCU-AHB1_APB1_CFG
0x058 CONSTANT CCU-APB2_CFG
0x060 CONSTANT CCU-BUS_GATE0
0x064 CONSTANT CCU-BUS_GATE1
0x068 CONSTANT CCU-BUS_GATE2

\ =========================================
\ 寄存器访问字
\ =========================================

\ 外设访问
\ UART0外设
: UART0-RBR@ ( -- n ) UART0-RBR L@ ;
: UART0-RBR! ( n -- ) UART0-RBR L! ;
: UART0-THR@ ( -- n ) UART0-THR L@ ;
: UART0-THR! ( n -- ) UART0-THR L! ;
: UART0-IER@ ( -- n ) UART0-IER L@ ;
: UART0-IER! ( n -- ) UART0-IER L! ;
: UART0-IIR@ ( -- n ) UART0-IIR L@ ;
: UART0-IIR! ( n -- ) UART0-IIR L! ;
: UART0-FCR@ ( -- n ) UART0-FCR L@ ;
: UART0-FCR! ( n -- ) UART0-FCR L! ;
: UART0-LCR@ ( -- n ) UART0-LCR L@ ;
: UART0-LCR! ( n -- ) UART0-LCR L! ;
: UART0-MCR@ ( -- n ) UART0-MCR L@ ;
: UART0-MCR! ( n -- ) UART0-MCR L! ;
: UART0-LSR@ ( -- n ) UART0-LSR L@ ;
: UART0-LSR! ( n -- ) UART0-LSR L! ;
: UART0-MSR@ ( -- n ) UART0-MSR L@ ;
: UART0-MSR! ( n -- ) UART0-MSR L! ;
: UART0-DLL@ ( -- n ) UART0-DLL L@ ;
: UART0-DLL! ( n -- ) UART0-DLL L! ;
: UART0-DLH@ ( -- n ) UART0-DLH L@ ;
: UART0-DLH! ( n -- ) UART0-DLH L! ;

\ UART1外设
: UART1-RBR@ ( -- n ) UART1-RBR L@ ;
: UART1-RBR! ( n -- ) UART1-RBR L! ;
: UART1-THR@ ( -- n ) UART1-THR L@ ;
: UART1-THR! ( n -- ) UART1-THR L! ;
: UART1-LSR@ ( -- n ) UART1-LSR L@ ;
: UART1-LSR! ( n -- ) UART1-LSR L! ;

\ GPIO外设
: GPIO-PA_CFG0@ ( -- n ) GPIO-PA_CFG0 L@ ;
: GPIO-PA_CFG0! ( n -- ) GPIO-PA_CFG0 L! ;
: GPIO-PA_CFG1@ ( -- n ) GPIO-PA_CFG1 L@ ;
: GPIO-PA_CFG1! ( n -- ) GPIO-PA_CFG1 L! ;
: GPIO-PA_DAT@ ( -- n ) GPIO-PA_DAT L@ ;
: GPIO-PA_DAT! ( n -- ) GPIO-PA_DAT L! ;
: GPIO-PA_DRV0@ ( -- n ) GPIO-PA_DRV0 L@ ;
: GPIO-PA_DRV0! ( n -- ) GPIO-PA_DRV0 L! ;
: GPIO-PA_PUL0@ ( -- n ) GPIO-PA_PUL0 L@ ;
: GPIO-PA_PUL0! ( n -- ) GPIO-PA_PUL0 L! ;
: GPIO-PB_CFG0@ ( -- n ) GPIO-PB_CFG0 L@ ;
: GPIO-PB_CFG0! ( n -- ) GPIO-PB_CFG0 L! ;
: GPIO-PB_DAT@ ( -- n ) GPIO-PB_DAT L@ ;
: GPIO-PB_DAT! ( n -- ) GPIO-PB_DAT L! ;
: GPIO-PC_CFG0@ ( -- n ) GPIO-PC_CFG0 L@ ;
: GPIO-PC_CFG0! ( n -- ) GPIO-PC_CFG0 L! ;
: GPIO-PC_DAT@ ( -- n ) GPIO-PC_DAT L@ ;
: GPIO-PC_DAT! ( n -- ) GPIO-PC_DAT L! ;

\ TIMER外设
: TIMER-CNT0@ ( -- n ) TIMER-CNT0 L@ ;
: TIMER-CNT0! ( n -- ) TIMER-CNT0 L! ;
: TIMER-CNT1@ ( -- n ) TIMER-CNT1 L@ ;
: TIMER-CNT1! ( n -- ) TIMER-CNT1 L! ;
: TIMER-CTRL@ ( -- n ) TIMER-CTRL L@ ;
: TIMER-CTRL! ( n -- ) TIMER-CTRL L! ;
: TIMER-INTV@ ( -- n ) TIMER-INTV L@ ;
: TIMER-INTV! ( n -- ) TIMER-INTV L! ;

\ CCU外设
: CCU-PLL1_CFG@ ( -- n ) CCU-PLL1_CFG L@ ;
: CCU-PLL1_CFG! ( n -- ) CCU-PLL1_CFG L! ;
: CCU-PLL3_CFG@ ( -- n ) CCU-PLL3_CFG L@ ;
: CCU-PLL3_CFG! ( n -- ) CCU-PLL3_CFG L! ;
: CCU-CPU_AXI_CFG@ ( -- n ) CCU-CPU_AXI_CFG L@ ;
: CCU-CPU_AXI_CFG! ( n -- ) CCU-CPU_AXI_CFG L! ;
: CCU-AHB1_APB1_CFG@ ( -- n ) CCU-AHB1_APB1_CFG L@ ;
: CCU-AHB1_APB1_CFG! ( n -- ) CCU-AHB1_APB1_CFG L! ;
: CCU-APB2_CFG@ ( -- n ) CCU-APB2_CFG L@ ;
: CCU-APB2_CFG! ( n -- ) CCU-APB2_CFG L! ;
: CCU-BUS_GATE0@ ( -- n ) CCU-BUS_GATE0 L@ ;
: CCU-BUS_GATE0! ( n -- ) CCU-BUS_GATE0 L! ;
: CCU-BUS_GATE1@ ( -- n ) CCU-BUS_GATE1 L@ ;
: CCU-BUS_GATE1! ( n -- ) CCU-BUS_GATE1 L! ;
: CCU-BUS_GATE2@ ( -- n ) CCU-BUS_GATE2 L@ ;
: CCU-BUS_GATE2! ( n -- ) CCU-BUS_GATE2 L! ;

\ =========================================
\ 设备初始化
\ =========================================

: ALLWINNER_H3-INIT ( -- )
  \ 初始化Allwinner H3设备
  ." 初始化Allwinner H3..." CR


  \ 初始化外设
  \ 初始化UART0
  0 UART0-RBR!  \ RBR寄存器
  0 UART0-THR!  \ THR寄存器
  0 UART0-IER!  \ IER寄存器
  0 UART0-IIR!  \ IIR寄存器
  0 UART0-FCR!  \ FCR寄存器
  0 UART0-LCR!  \ LCR寄存器
  0 UART0-MCR!  \ MCR寄存器
  0 UART0-LSR!  \ LSR寄存器
  0 UART0-MSR!  \ MSR寄存器
  0 UART0-DLL!  \ DLL寄存器
  0 UART0-DLH!  \ DLH寄存器
  \ 初始化UART1
  0 UART1-RBR!  \ RBR寄存器
  0 UART1-THR!  \ THR寄存器
  0 UART1-LSR!  \ LSR寄存器
  \ 初始化GPIO
  0 GPIO-PA_CFG0!  \ PA_CFG0寄存器
  0 GPIO-PA_CFG1!  \ PA_CFG1寄存器
  0 GPIO-PA_DAT!  \ PA_DAT寄存器
  0 GPIO-PA_DRV0!  \ PA_DRV0寄存器
  0 GPIO-PA_PUL0!  \ PA_PUL0寄存器
  0 GPIO-PB_CFG0!  \ PB_CFG0寄存器
  0 GPIO-PB_DAT!  \ PB_DAT寄存器
  0 GPIO-PC_CFG0!  \ PC_CFG0寄存器
  0 GPIO-PC_DAT!  \ PC_DAT寄存器
  \ 初始化TIMER
  0 TIMER-CNT0!  \ CNT0寄存器
  0 TIMER-CNT1!  \ CNT1寄存器
  0 TIMER-CTRL!  \ CTRL寄存器
  0 TIMER-INTV!  \ INTV寄存器
  \ 初始化CCU
  0 CCU-PLL1_CFG!  \ PLL1_CFG寄存器
  0 CCU-PLL3_CFG!  \ PLL3_CFG寄存器
  0 CCU-CPU_AXI_CFG!  \ CPU_AXI_CFG寄存器
  0 CCU-AHB1_APB1_CFG!  \ AHB1_APB1_CFG寄存器
  0 CCU-APB2_CFG!  \ APB2_CFG寄存器
  0 CCU-BUS_GATE0!  \ BUS_GATE0寄存器
  0 CCU-BUS_GATE1!  \ BUS_GATE1寄存器
  0 CCU-BUS_GATE2!  \ BUS_GATE2寄存器

  ." Allwinner H3初始化完成" CR
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
  ALLWINNER_H3-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

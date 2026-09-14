\ ESP32-C3设备定义 - Forth文件
\ 生成自: Espressif/ESP32-C/ESP32-C3
\ 版本: 1.0
\ 日期: 2026-04-28
\ 作者: VML Team
\ 描述: 32-bit RISC-V single-core WiFi + BLE SoC, 160MHz, 400KB SRAM
\ CPU架构: RISC-V
\ 位宽: 32位
\ 时钟频率: 160000000 Hz

\ =========================================
\ ESP32-C3设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" ESP32-C3" ;
: MANUFACTURER  S" Espressif" ;
: FAMILY        S" ESP32-C" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" RISC-V" ;
32 CONSTANT BITS
160000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x04 CONSTANT X1  \ Return Address
0x08 CONSTANT X2  \ Stack Pointer (SP)
0x0C CONSTANT X3  \ Global Pointer (GP)
0x20 CONSTANT X8  \ Frame Pointer (FP)
0x28 CONSTANT X10  \ Function Argument (A0)
0x2C CONSTANT X11  \ Function Argument (A1)
0x3C CONSTANT PC  \ Program Counter

\ 内存段定义
0x42000000 CONSTANT FLASH-START
0x427FFFFF CONSTANT FLASH-END
8388608 CONSTANT FLASH-SIZE  \ Flash via Cache
0x3FC80000 CONSTANT SRAM-START
0x3FCE3FFF CONSTANT SRAM-END
409600 CONSTANT SRAM-SIZE  \ Internal SRAM
0x60000000 CONSTANT PERIPHERAL-START
0x600FFFFF CONSTANT PERIPHERAL-END
1048576 CONSTANT PERIPHERAL-SIZE  \ 

\ 外设定义
\ General Purpose I/O
0x60004000 CONSTANT GPIO-BASE
0x04 CONSTANT GPIO-OUT
0x08 CONSTANT GPIO-OUT_W1TS
0x0C CONSTANT GPIO-OUT_W1TC
0x10 CONSTANT GPIO-IN
0x20 CONSTANT GPIO-ENABLE
0x24 CONSTANT GPIO-ENABLE_W1TS
0x28 CONSTANT GPIO-ENABLE_W1TC
\ I/O MUX
0x60009000 CONSTANT IO_MUX-BASE
0x00 CONSTANT IO_MUX-GPIO0
0x04 CONSTANT IO_MUX-GPIO1
0x08 CONSTANT IO_MUX-GPIO2
0x0C CONSTANT IO_MUX-GPIO3
\ RTC Control
0x60008000 CONSTANT RTC_CNTL-BASE
0x00 CONSTANT RTC_CNTL-OPTIONS0
0x30 CONSTANT RTC_CNTL-CLK_CONF

\ 中断向量定义
1 CONSTANT INT-RESET  \ 
3 CONSTANT INT-MACHINESOFTWARE  \ 
7 CONSTANT INT-MACHINETIMER  \ 
11 CONSTANT INT-MACHINEEXTERNAL  \ 

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: X1@ ( -- n ) X1 L@ ;
: X1! ( n -- ) X1 L! ;

: X2@ ( -- n ) X2 L@ ;
: X2! ( n -- ) X2 L! ;

: X3@ ( -- n ) X3 L@ ;
: X3! ( n -- ) X3 L! ;

: X8@ ( -- n ) X8 L@ ;
: X8! ( n -- ) X8 L! ;

: X10@ ( -- n ) X10 L@ ;
: X10! ( n -- ) X10 L! ;

: X11@ ( -- n ) X11 L@ ;
: X11! ( n -- ) X11 L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

\ 外设访问
\ GPIO外设
: GPIO-OUT@ ( -- n ) GPIO-OUT L@ ;
: GPIO-OUT! ( n -- ) GPIO-OUT L! ;
: GPIO-OUT_W1TS@ ( -- n ) GPIO-OUT_W1TS L@ ;
: GPIO-OUT_W1TS! ( n -- ) GPIO-OUT_W1TS L! ;
: GPIO-OUT_W1TC@ ( -- n ) GPIO-OUT_W1TC L@ ;
: GPIO-OUT_W1TC! ( n -- ) GPIO-OUT_W1TC L! ;
: GPIO-IN@ ( -- n ) GPIO-IN L@ ;
: GPIO-IN! ( n -- ) GPIO-IN L! ;
: GPIO-ENABLE@ ( -- n ) GPIO-ENABLE L@ ;
: GPIO-ENABLE! ( n -- ) GPIO-ENABLE L! ;
: GPIO-ENABLE_W1TS@ ( -- n ) GPIO-ENABLE_W1TS L@ ;
: GPIO-ENABLE_W1TS! ( n -- ) GPIO-ENABLE_W1TS L! ;
: GPIO-ENABLE_W1TC@ ( -- n ) GPIO-ENABLE_W1TC L@ ;
: GPIO-ENABLE_W1TC! ( n -- ) GPIO-ENABLE_W1TC L! ;

\ IO_MUX外设
: IO_MUX-GPIO0@ ( -- n ) IO_MUX-GPIO0 L@ ;
: IO_MUX-GPIO0! ( n -- ) IO_MUX-GPIO0 L! ;
: IO_MUX-GPIO1@ ( -- n ) IO_MUX-GPIO1 L@ ;
: IO_MUX-GPIO1! ( n -- ) IO_MUX-GPIO1 L! ;
: IO_MUX-GPIO2@ ( -- n ) IO_MUX-GPIO2 L@ ;
: IO_MUX-GPIO2! ( n -- ) IO_MUX-GPIO2 L! ;
: IO_MUX-GPIO3@ ( -- n ) IO_MUX-GPIO3 L@ ;
: IO_MUX-GPIO3! ( n -- ) IO_MUX-GPIO3 L! ;

\ RTC_CNTL外设
: RTC_CNTL-OPTIONS0@ ( -- n ) RTC_CNTL-OPTIONS0 L@ ;
: RTC_CNTL-OPTIONS0! ( n -- ) RTC_CNTL-OPTIONS0 L! ;
: RTC_CNTL-CLK_CONF@ ( -- n ) RTC_CNTL-CLK_CONF L@ ;
: RTC_CNTL-CLK_CONF! ( n -- ) RTC_CNTL-CLK_CONF L! ;

\ =========================================
\ 设备初始化
\ =========================================

: ESP32_C3-INIT ( -- )
  \ 初始化ESP32-C3设备
  ." 初始化ESP32-C3..." CR

  \ 初始化寄存器
  0 X1!  \ Return Address
  0 X2!  \ Stack Pointer (SP)
  0 X3!  \ Global Pointer (GP)
  0 X8!  \ Frame Pointer (FP)
  0 X10!  \ Function Argument (A0)
  0 X11!  \ Function Argument (A1)
  0 PC!  \ Program Counter

  \ 初始化外设
  \ 初始化GPIO
  0 GPIO-OUT!  \ OUT寄存器
  0 GPIO-OUT_W1TS!  \ OUT_W1TS寄存器
  0 GPIO-OUT_W1TC!  \ OUT_W1TC寄存器
  0 GPIO-IN!  \ IN寄存器
  0 GPIO-ENABLE!  \ ENABLE寄存器
  0 GPIO-ENABLE_W1TS!  \ ENABLE_W1TS寄存器
  0 GPIO-ENABLE_W1TC!  \ ENABLE_W1TC寄存器
  \ 初始化IO_MUX
  0 IO_MUX-GPIO0!  \ GPIO0寄存器
  0 IO_MUX-GPIO1!  \ GPIO1寄存器
  0 IO_MUX-GPIO2!  \ GPIO2寄存器
  0 IO_MUX-GPIO3!  \ GPIO3寄存器
  \ 初始化RTC_CNTL
  0 RTC_CNTL-OPTIONS0!  \ OPTIONS0寄存器
  0 RTC_CNTL-CLK_CONF!  \ CLK_CONF寄存器

  ." ESP32-C3初始化完成" CR
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
  X1@ X1 .R 8 .R SPACE ."  x1: " X1@ .
  X2@ X2 .R 8 .R SPACE ."  x2: " X2@ .
  X3@ X3 .R 8 .R SPACE ."  x3: " X3@ .
  X8@ X8 .R 8 .R SPACE ."  x8: " X8@ .
  X10@ X10 .R 8 .R SPACE ."  x10: " X10@ .
  X11@ X11 .R 8 .R SPACE ."  x11: " X11@ .
  PC@ PC .R 8 .R SPACE ."  pc: " PC@ .
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
: INT-MACHINESOFTWARE-HANDLER ( -- )
  ." MachineSoftware中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-MACHINESOFTWARE-ENABLE ( -- )
  INT-MACHINESOFTWARE INT-ENABLE
;

: INT-MACHINESOFTWARE-DISABLE ( -- )
  INT-MACHINESOFTWARE INT-DISABLE
;

\ 
: INT-MACHINETIMER-HANDLER ( -- )
  ." MachineTimer中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-MACHINETIMER-ENABLE ( -- )
  INT-MACHINETIMER INT-ENABLE
;

: INT-MACHINETIMER-DISABLE ( -- )
  INT-MACHINETIMER INT-DISABLE
;

\ 
: INT-MACHINEEXTERNAL-HANDLER ( -- )
  ." MachineExternal中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-MACHINEEXTERNAL-ENABLE ( -- )
  INT-MACHINEEXTERNAL INT-ENABLE
;

: INT-MACHINEEXTERNAL-DISABLE ( -- )
  INT-MACHINEEXTERNAL INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  ESP32_C3-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

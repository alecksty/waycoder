\ Apple-II设备定义 - Forth文件
\ 生成自: Apple Computer/Apple II/Apple-II
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Apple II personal computer with MOS 6502 CPU, 48KB RAM, and color graphics
\ CPU架构: MOS 6502
\ 位宽: 8位
\ 时钟频率: 1023000 Hz

\ =========================================
\ Apple-II设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Apple-II" ;
: MANUFACTURER  S" Apple Computer" ;
: FAMILY        S" Apple II" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" MOS 6502" ;
8 CONSTANT BITS
1023000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0 CONSTANT A  \ Accumulator
0 CONSTANT X  \ Index Register X
0 CONSTANT Y  \ Index Register Y
0 CONSTANT SP  \ Stack Pointer
0 CONSTANT PC  \ Program Counter
0 CONSTANT P  \ Status Register

\ 外设定义
\ Apple II keyboard
 CONSTANT KEYBOARD-BASE
0xC000 CONSTANT KEYBOARD-KBD
0xC010 CONSTANT KEYBOARD-KBDSTRB
\ Built-in speaker
 CONSTANT SPEAKER-BASE
0xC030 CONSTANT SPEAKER-SPKR
\ Cassette tape interface
 CONSTANT CASSETTE-BASE
0xC060 CONSTANT CASSETTE-TAPEIN
0xC020 CONSTANT CASSETTE-TAPEOUT
\ Game controller port
 CONSTANT GAMEPORT-BASE
0xC064 CONSTANT GAMEPORT-PADDLE0
0xC065 CONSTANT GAMEPORT-PADDLE1
0xC066 CONSTANT GAMEPORT-PADDLE2
0xC067 CONSTANT GAMEPORT-PADDLE3
0xC061 CONSTANT GAMEPORT-BUTTON0
0xC062 CONSTANT GAMEPORT-BUTTON1
\ Disk II controller
 CONSTANT DISKCONTROLLER-BASE
0xC0E0 CONSTANT DISKCONTROLLER-DISKUNIT
0xC0E8 CONSTANT DISKCONTROLLER-DISKCMD
0xC0E9 CONSTANT DISKCONTROLLER-DISKSTAT
0xC0EA CONSTANT DISKCONTROLLER-DISKDATA

\ 中断向量定义
65526 CONSTANT INT-NMI  \ Non-maskable interrupt
65528 CONSTANT INT-RESET  \ Reset vector
65530 CONSTANT INT-IRQ  \ Interrupt request
65532 CONSTANT INT-BRK  \ Break instruction

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: A@ ( -- n ) A C@ ;
: A! ( n -- ) A C! ;

: X@ ( -- n ) X C@ ;
: X! ( n -- ) X C! ;

: Y@ ( -- n ) Y C@ ;
: Y! ( n -- ) Y C! ;

: SP@ ( -- n ) SP C@ ;
: SP! ( n -- ) SP C! ;

: PC@ ( -- n ) PC @ ;
: PC! ( n -- ) PC ! ;

: P@ ( -- n ) P C@ ;
: P! ( n -- ) P C! ;

\ 外设访问
\ Keyboard外设
: KEYBOARD-KBD@ ( -- n ) KEYBOARD-KBD C@ ;
: KEYBOARD-KBD! ( n -- ) KEYBOARD-KBD C! ;
: KEYBOARD-KBDSTRB@ ( -- n ) KEYBOARD-KBDSTRB C@ ;
: KEYBOARD-KBDSTRB! ( n -- ) KEYBOARD-KBDSTRB C! ;

\ Speaker外设
: SPEAKER-SPKR@ ( -- n ) SPEAKER-SPKR C@ ;
: SPEAKER-SPKR! ( n -- ) SPEAKER-SPKR C! ;

\ Cassette外设
: CASSETTE-TAPEIN@ ( -- n ) CASSETTE-TAPEIN C@ ;
: CASSETTE-TAPEIN! ( n -- ) CASSETTE-TAPEIN C! ;
: CASSETTE-TAPEOUT@ ( -- n ) CASSETTE-TAPEOUT C@ ;
: CASSETTE-TAPEOUT! ( n -- ) CASSETTE-TAPEOUT C! ;

\ GamePort外设
: GAMEPORT-PADDLE0@ ( -- n ) GAMEPORT-PADDLE0 C@ ;
: GAMEPORT-PADDLE0! ( n -- ) GAMEPORT-PADDLE0 C! ;
: GAMEPORT-PADDLE1@ ( -- n ) GAMEPORT-PADDLE1 C@ ;
: GAMEPORT-PADDLE1! ( n -- ) GAMEPORT-PADDLE1 C! ;
: GAMEPORT-PADDLE2@ ( -- n ) GAMEPORT-PADDLE2 C@ ;
: GAMEPORT-PADDLE2! ( n -- ) GAMEPORT-PADDLE2 C! ;
: GAMEPORT-PADDLE3@ ( -- n ) GAMEPORT-PADDLE3 C@ ;
: GAMEPORT-PADDLE3! ( n -- ) GAMEPORT-PADDLE3 C! ;
: GAMEPORT-BUTTON0@ ( -- n ) GAMEPORT-BUTTON0 C@ ;
: GAMEPORT-BUTTON0! ( n -- ) GAMEPORT-BUTTON0 C! ;
: GAMEPORT-BUTTON1@ ( -- n ) GAMEPORT-BUTTON1 C@ ;
: GAMEPORT-BUTTON1! ( n -- ) GAMEPORT-BUTTON1 C! ;

\ DiskController外设
: DISKCONTROLLER-DISKUNIT@ ( -- n ) DISKCONTROLLER-DISKUNIT C@ ;
: DISKCONTROLLER-DISKUNIT! ( n -- ) DISKCONTROLLER-DISKUNIT C! ;
: DISKCONTROLLER-DISKCMD@ ( -- n ) DISKCONTROLLER-DISKCMD C@ ;
: DISKCONTROLLER-DISKCMD! ( n -- ) DISKCONTROLLER-DISKCMD C! ;
: DISKCONTROLLER-DISKSTAT@ ( -- n ) DISKCONTROLLER-DISKSTAT C@ ;
: DISKCONTROLLER-DISKSTAT! ( n -- ) DISKCONTROLLER-DISKSTAT C! ;
: DISKCONTROLLER-DISKDATA@ ( -- n ) DISKCONTROLLER-DISKDATA C@ ;
: DISKCONTROLLER-DISKDATA! ( n -- ) DISKCONTROLLER-DISKDATA C! ;

\ =========================================
\ 设备初始化
\ =========================================

: APPLE_II-INIT ( -- )
  \ 初始化Apple-II设备
  ." 初始化Apple-II..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 X!  \ Index Register X
  0 Y!  \ Index Register Y
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter
  0 P!  \ Status Register

  \ 初始化外设
  \ 初始化Keyboard
  0 KEYBOARD-KBD!  \ KBD寄存器
  0 KEYBOARD-KBDSTRB!  \ KBDSTRB寄存器
  \ 初始化Speaker
  0 SPEAKER-SPKR!  \ SPKR寄存器
  \ 初始化Cassette
  0 CASSETTE-TAPEIN!  \ TAPEIN寄存器
  0 CASSETTE-TAPEOUT!  \ TAPEOUT寄存器
  \ 初始化GamePort
  0 GAMEPORT-PADDLE0!  \ PADDLE0寄存器
  0 GAMEPORT-PADDLE1!  \ PADDLE1寄存器
  0 GAMEPORT-PADDLE2!  \ PADDLE2寄存器
  0 GAMEPORT-PADDLE3!  \ PADDLE3寄存器
  0 GAMEPORT-BUTTON0!  \ BUTTON0寄存器
  0 GAMEPORT-BUTTON1!  \ BUTTON1寄存器
  \ 初始化DiskController
  0 DISKCONTROLLER-DISKUNIT!  \ DISKUNIT寄存器
  0 DISKCONTROLLER-DISKCMD!  \ DISKCMD寄存器
  0 DISKCONTROLLER-DISKSTAT!  \ DISKSTAT寄存器
  0 DISKCONTROLLER-DISKDATA!  \ DISKDATA寄存器

  ." Apple-II初始化完成" CR
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
  A@ A .R 8 .R SPACE ."  A: " A@ .
  X@ X .R 8 .R SPACE ."  X: " X@ .
  Y@ Y .R 8 .R SPACE ."  Y: " Y@ .
  SP@ SP .R 8 .R SPACE ."  SP: " SP@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  P@ P .R 8 .R SPACE ."  P: " P@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ Non-maskable interrupt
: INT-NMI-HANDLER ( -- )
  ." NMI中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-NMI-ENABLE ( -- )
  INT-NMI INT-ENABLE
;

: INT-NMI-DISABLE ( -- )
  INT-NMI INT-DISABLE
;

\ Reset vector
: INT-RESET-HANDLER ( -- )
  ." RESET中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET-ENABLE ( -- )
  INT-RESET INT-ENABLE
;

: INT-RESET-DISABLE ( -- )
  INT-RESET INT-DISABLE
;

\ Interrupt request
: INT-IRQ-HANDLER ( -- )
  ." IRQ中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-IRQ-ENABLE ( -- )
  INT-IRQ INT-ENABLE
;

: INT-IRQ-DISABLE ( -- )
  INT-IRQ INT-DISABLE
;

\ Break instruction
: INT-BRK-HANDLER ( -- )
  ." BRK中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-BRK-ENABLE ( -- )
  INT-BRK INT-ENABLE
;

: INT-BRK-DISABLE ( -- )
  INT-BRK INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  APPLE_II-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

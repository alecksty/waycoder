\ Commodore-PET设备定义 - Forth文件
\ 生成自: Commodore International/PET/Commodore-PET
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Commodore PET 2001 personal computer with MOS 6502 CPU and built-in monitor
\ CPU架构: MOS 6502
\ 位宽: 8位
\ 时钟频率: 1000000 Hz

\ =========================================
\ Commodore-PET设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Commodore-PET" ;
: MANUFACTURER  S" Commodore International" ;
: FAMILY        S" PET" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" MOS 6502" ;
8 CONSTANT BITS
1000000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0 CONSTANT A  \ Accumulator
0 CONSTANT X  \ Index Register X
0 CONSTANT Y  \ Index Register Y
0 CONSTANT SP  \ Stack Pointer
0 CONSTANT PC  \ Program Counter
0 CONSTANT P  \ Status Register

\ 外设定义
\ Peripheral Interface Adapter 1 (6520)
 CONSTANT PIA1-BASE
0xE810 CONSTANT PIA1-PIA1_DDRA
0xE811 CONSTANT PIA1-PIA1_ORA
0xE812 CONSTANT PIA1-PIA1_DDRB
0xE813 CONSTANT PIA1-PIA1_ORB
0xE814 CONSTANT PIA1-PIA1_CRA
0xE815 CONSTANT PIA1-PIA1_CRB
\ Peripheral Interface Adapter 2 (6520)
 CONSTANT PIA2-BASE
0xE820 CONSTANT PIA2-PIA2_DDRA
0xE821 CONSTANT PIA2-PIA2_ORA
0xE822 CONSTANT PIA2-PIA2_DDRB
0xE823 CONSTANT PIA2-PIA2_ORB
0xE824 CONSTANT PIA2-PIA2_CRA
0xE825 CONSTANT PIA2-PIA2_CRB
\ Versatile Interface Adapter (6522)
 CONSTANT VIA-BASE
0xE840 CONSTANT VIA-VIA_ORB
0xE841 CONSTANT VIA-VIA_ORA
0xE842 CONSTANT VIA-VIA_DDRB
0xE843 CONSTANT VIA-VIA_DDRA
0xE844 CONSTANT VIA-VIA_T1CL
0xE845 CONSTANT VIA-VIA_T1CH
0xE846 CONSTANT VIA-VIA_T1LL
0xE847 CONSTANT VIA-VIA_T1LH
0xE848 CONSTANT VIA-VIA_T2CL
0xE849 CONSTANT VIA-VIA_T2CH
0xE84A CONSTANT VIA-VIA_SR
0xE84B CONSTANT VIA-VIA_ACR
0xE84C CONSTANT VIA-VIA_PCR
0xE84D CONSTANT VIA-VIA_IFR
0xE84E CONSTANT VIA-VIA_IER
\ CRT Controller (6545)
 CONSTANT CRTC-BASE
0xE880 CONSTANT CRTC-CRTC_ADDR
0xE881 CONSTANT CRTC-CRTC_DATA
\ Cassette tape interface
 CONSTANT CASSETTE-BASE
0xE840 CONSTANT CASSETTE-CASS_MOTOR
0xE842 CONSTANT CASSETTE-CASS_WRITE
0xE812 CONSTANT CASSETTE-CASS_READ
\ IEEE-488 bus interface
 CONSTANT IEEE488-BASE
0xE801 CONSTANT IEEE488-IEEE_DATA
0xE802 CONSTANT IEEE488-IEEE_STATUS
0xE803 CONSTANT IEEE488-IEEE_CONTROL

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
\ PIA1外设
: PIA1-PIA1_DDRA@ ( -- n ) PIA1-PIA1_DDRA C@ ;
: PIA1-PIA1_DDRA! ( n -- ) PIA1-PIA1_DDRA C! ;
: PIA1-PIA1_ORA@ ( -- n ) PIA1-PIA1_ORA C@ ;
: PIA1-PIA1_ORA! ( n -- ) PIA1-PIA1_ORA C! ;
: PIA1-PIA1_DDRB@ ( -- n ) PIA1-PIA1_DDRB C@ ;
: PIA1-PIA1_DDRB! ( n -- ) PIA1-PIA1_DDRB C! ;
: PIA1-PIA1_ORB@ ( -- n ) PIA1-PIA1_ORB C@ ;
: PIA1-PIA1_ORB! ( n -- ) PIA1-PIA1_ORB C! ;
: PIA1-PIA1_CRA@ ( -- n ) PIA1-PIA1_CRA C@ ;
: PIA1-PIA1_CRA! ( n -- ) PIA1-PIA1_CRA C! ;
: PIA1-PIA1_CRB@ ( -- n ) PIA1-PIA1_CRB C@ ;
: PIA1-PIA1_CRB! ( n -- ) PIA1-PIA1_CRB C! ;

\ PIA2外设
: PIA2-PIA2_DDRA@ ( -- n ) PIA2-PIA2_DDRA C@ ;
: PIA2-PIA2_DDRA! ( n -- ) PIA2-PIA2_DDRA C! ;
: PIA2-PIA2_ORA@ ( -- n ) PIA2-PIA2_ORA C@ ;
: PIA2-PIA2_ORA! ( n -- ) PIA2-PIA2_ORA C! ;
: PIA2-PIA2_DDRB@ ( -- n ) PIA2-PIA2_DDRB C@ ;
: PIA2-PIA2_DDRB! ( n -- ) PIA2-PIA2_DDRB C! ;
: PIA2-PIA2_ORB@ ( -- n ) PIA2-PIA2_ORB C@ ;
: PIA2-PIA2_ORB! ( n -- ) PIA2-PIA2_ORB C! ;
: PIA2-PIA2_CRA@ ( -- n ) PIA2-PIA2_CRA C@ ;
: PIA2-PIA2_CRA! ( n -- ) PIA2-PIA2_CRA C! ;
: PIA2-PIA2_CRB@ ( -- n ) PIA2-PIA2_CRB C@ ;
: PIA2-PIA2_CRB! ( n -- ) PIA2-PIA2_CRB C! ;

\ VIA外设
: VIA-VIA_ORB@ ( -- n ) VIA-VIA_ORB C@ ;
: VIA-VIA_ORB! ( n -- ) VIA-VIA_ORB C! ;
: VIA-VIA_ORA@ ( -- n ) VIA-VIA_ORA C@ ;
: VIA-VIA_ORA! ( n -- ) VIA-VIA_ORA C! ;
: VIA-VIA_DDRB@ ( -- n ) VIA-VIA_DDRB C@ ;
: VIA-VIA_DDRB! ( n -- ) VIA-VIA_DDRB C! ;
: VIA-VIA_DDRA@ ( -- n ) VIA-VIA_DDRA C@ ;
: VIA-VIA_DDRA! ( n -- ) VIA-VIA_DDRA C! ;
: VIA-VIA_T1CL@ ( -- n ) VIA-VIA_T1CL C@ ;
: VIA-VIA_T1CL! ( n -- ) VIA-VIA_T1CL C! ;
: VIA-VIA_T1CH@ ( -- n ) VIA-VIA_T1CH C@ ;
: VIA-VIA_T1CH! ( n -- ) VIA-VIA_T1CH C! ;
: VIA-VIA_T1LL@ ( -- n ) VIA-VIA_T1LL C@ ;
: VIA-VIA_T1LL! ( n -- ) VIA-VIA_T1LL C! ;
: VIA-VIA_T1LH@ ( -- n ) VIA-VIA_T1LH C@ ;
: VIA-VIA_T1LH! ( n -- ) VIA-VIA_T1LH C! ;
: VIA-VIA_T2CL@ ( -- n ) VIA-VIA_T2CL C@ ;
: VIA-VIA_T2CL! ( n -- ) VIA-VIA_T2CL C! ;
: VIA-VIA_T2CH@ ( -- n ) VIA-VIA_T2CH C@ ;
: VIA-VIA_T2CH! ( n -- ) VIA-VIA_T2CH C! ;
: VIA-VIA_SR@ ( -- n ) VIA-VIA_SR C@ ;
: VIA-VIA_SR! ( n -- ) VIA-VIA_SR C! ;
: VIA-VIA_ACR@ ( -- n ) VIA-VIA_ACR C@ ;
: VIA-VIA_ACR! ( n -- ) VIA-VIA_ACR C! ;
: VIA-VIA_PCR@ ( -- n ) VIA-VIA_PCR C@ ;
: VIA-VIA_PCR! ( n -- ) VIA-VIA_PCR C! ;
: VIA-VIA_IFR@ ( -- n ) VIA-VIA_IFR C@ ;
: VIA-VIA_IFR! ( n -- ) VIA-VIA_IFR C! ;
: VIA-VIA_IER@ ( -- n ) VIA-VIA_IER C@ ;
: VIA-VIA_IER! ( n -- ) VIA-VIA_IER C! ;

\ CRTC外设
: CRTC-CRTC_ADDR@ ( -- n ) CRTC-CRTC_ADDR C@ ;
: CRTC-CRTC_ADDR! ( n -- ) CRTC-CRTC_ADDR C! ;
: CRTC-CRTC_DATA@ ( -- n ) CRTC-CRTC_DATA C@ ;
: CRTC-CRTC_DATA! ( n -- ) CRTC-CRTC_DATA C! ;

\ Cassette外设
: CASSETTE-CASS_MOTOR@ ( -- n ) CASSETTE-CASS_MOTOR C@ ;
: CASSETTE-CASS_MOTOR! ( n -- ) CASSETTE-CASS_MOTOR C! ;
: CASSETTE-CASS_WRITE@ ( -- n ) CASSETTE-CASS_WRITE C@ ;
: CASSETTE-CASS_WRITE! ( n -- ) CASSETTE-CASS_WRITE C! ;
: CASSETTE-CASS_READ@ ( -- n ) CASSETTE-CASS_READ C@ ;
: CASSETTE-CASS_READ! ( n -- ) CASSETTE-CASS_READ C! ;

\ IEEE488外设
: IEEE488-IEEE_DATA@ ( -- n ) IEEE488-IEEE_DATA C@ ;
: IEEE488-IEEE_DATA! ( n -- ) IEEE488-IEEE_DATA C! ;
: IEEE488-IEEE_STATUS@ ( -- n ) IEEE488-IEEE_STATUS C@ ;
: IEEE488-IEEE_STATUS! ( n -- ) IEEE488-IEEE_STATUS C! ;
: IEEE488-IEEE_CONTROL@ ( -- n ) IEEE488-IEEE_CONTROL C@ ;
: IEEE488-IEEE_CONTROL! ( n -- ) IEEE488-IEEE_CONTROL C! ;

\ =========================================
\ 设备初始化
\ =========================================

: COMMODORE_PET-INIT ( -- )
  \ 初始化Commodore-PET设备
  ." 初始化Commodore-PET..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 X!  \ Index Register X
  0 Y!  \ Index Register Y
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter
  0 P!  \ Status Register

  \ 初始化外设
  \ 初始化PIA1
  0 PIA1-PIA1_DDRA!  \ PIA1_DDRA寄存器
  0 PIA1-PIA1_ORA!  \ PIA1_ORA寄存器
  0 PIA1-PIA1_DDRB!  \ PIA1_DDRB寄存器
  0 PIA1-PIA1_ORB!  \ PIA1_ORB寄存器
  0 PIA1-PIA1_CRA!  \ PIA1_CRA寄存器
  0 PIA1-PIA1_CRB!  \ PIA1_CRB寄存器
  \ 初始化PIA2
  0 PIA2-PIA2_DDRA!  \ PIA2_DDRA寄存器
  0 PIA2-PIA2_ORA!  \ PIA2_ORA寄存器
  0 PIA2-PIA2_DDRB!  \ PIA2_DDRB寄存器
  0 PIA2-PIA2_ORB!  \ PIA2_ORB寄存器
  0 PIA2-PIA2_CRA!  \ PIA2_CRA寄存器
  0 PIA2-PIA2_CRB!  \ PIA2_CRB寄存器
  \ 初始化VIA
  0 VIA-VIA_ORB!  \ VIA_ORB寄存器
  0 VIA-VIA_ORA!  \ VIA_ORA寄存器
  0 VIA-VIA_DDRB!  \ VIA_DDRB寄存器
  0 VIA-VIA_DDRA!  \ VIA_DDRA寄存器
  0 VIA-VIA_T1CL!  \ VIA_T1CL寄存器
  0 VIA-VIA_T1CH!  \ VIA_T1CH寄存器
  0 VIA-VIA_T1LL!  \ VIA_T1LL寄存器
  0 VIA-VIA_T1LH!  \ VIA_T1LH寄存器
  0 VIA-VIA_T2CL!  \ VIA_T2CL寄存器
  0 VIA-VIA_T2CH!  \ VIA_T2CH寄存器
  0 VIA-VIA_SR!  \ VIA_SR寄存器
  0 VIA-VIA_ACR!  \ VIA_ACR寄存器
  0 VIA-VIA_PCR!  \ VIA_PCR寄存器
  0 VIA-VIA_IFR!  \ VIA_IFR寄存器
  0 VIA-VIA_IER!  \ VIA_IER寄存器
  \ 初始化CRTC
  0 CRTC-CRTC_ADDR!  \ CRTC_ADDR寄存器
  0 CRTC-CRTC_DATA!  \ CRTC_DATA寄存器
  \ 初始化Cassette
  0 CASSETTE-CASS_MOTOR!  \ CASS_MOTOR寄存器
  0 CASSETTE-CASS_WRITE!  \ CASS_WRITE寄存器
  0 CASSETTE-CASS_READ!  \ CASS_READ寄存器
  \ 初始化IEEE488
  0 IEEE488-IEEE_DATA!  \ IEEE_DATA寄存器
  0 IEEE488-IEEE_STATUS!  \ IEEE_STATUS寄存器
  0 IEEE488-IEEE_CONTROL!  \ IEEE_CONTROL寄存器

  ." Commodore-PET初始化完成" CR
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
  COMMODORE_PET-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

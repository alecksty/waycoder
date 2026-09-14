\ Macintosh-128K设备定义 - Forth文件
\ 生成自: Apple Computer/Macintosh/Macintosh-128K
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display
\ CPU架构: Motorola 68000
\ 位宽: 32位
\ 时钟频率: 7998000 Hz

\ =========================================
\ Macintosh-128K设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Macintosh-128K" ;
: MANUFACTURER  S" Apple Computer" ;
: FAMILY        S" Macintosh" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" Motorola 68000" ;
32 CONSTANT BITS
7998000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0 CONSTANT D0  \ Data Register 0
0 CONSTANT D1  \ Data Register 1
0 CONSTANT D2  \ Data Register 2
0 CONSTANT D3  \ Data Register 3
0 CONSTANT D4  \ Data Register 4
0 CONSTANT D5  \ Data Register 5
0 CONSTANT D6  \ Data Register 6
0 CONSTANT D7  \ Data Register 7
0 CONSTANT A0  \ Address Register 0
0 CONSTANT A1  \ Address Register 1
0 CONSTANT A2  \ Address Register 2
0 CONSTANT A3  \ Address Register 3
0 CONSTANT A4  \ Address Register 4
0 CONSTANT A5  \ Address Register 5
0 CONSTANT A6  \ Address Register 6
0 CONSTANT A7  \ Address Register 7 (SP)
0 CONSTANT PC  \ Program Counter
0 CONSTANT SR  \ Status Register

\ 外设定义
\ Versatile Interface Adapter (6522)
 CONSTANT VIA-BASE
0xE80000 CONSTANT VIA-VIA_ORB
0xE80001 CONSTANT VIA-VIA_ORA
0xE80002 CONSTANT VIA-VIA_DDRB
0xE80003 CONSTANT VIA-VIA_DDRA
0xE80004 CONSTANT VIA-VIA_T1CL
0xE80005 CONSTANT VIA-VIA_T1CH
0xE80006 CONSTANT VIA-VIA_T1LL
0xE80007 CONSTANT VIA-VIA_T1LH
0xE80008 CONSTANT VIA-VIA_T2CL
0xE80009 CONSTANT VIA-VIA_T2CH
0xE8000A CONSTANT VIA-VIA_SR
0xE8000B CONSTANT VIA-VIA_ACR
0xE8000C CONSTANT VIA-VIA_PCR
0xE8000D CONSTANT VIA-VIA_IFR
0xE8000E CONSTANT VIA-VIA_IER
0xE8000F CONSTANT VIA-VIA_ORA2
\ Integrated Woz Machine (floppy controller)
 CONSTANT IWM-BASE
0xD00000 CONSTANT IWM-IWM_Q6
0xD00002 CONSTANT IWM-IWM_Q7
0xD00004 CONSTANT IWM-IWM_PH0
0xD00006 CONSTANT IWM-IWM_PH1
0xD00008 CONSTANT IWM-IWM_PH2
0xD0000A CONSTANT IWM-IWM_PH3
\ Zilog 8530 Serial Communications Controller
 CONSTANT SCC-BASE
0x500000 CONSTANT SCC-SCC_CA
0x500002 CONSTANT SCC-SCC_DA
0x500004 CONSTANT SCC-SCC_CB
0x500006 CONSTANT SCC-SCC_DB
\ Built-in speaker
 CONSTANT SOUND-BASE
0xE80100 CONSTANT SOUND-SOUND_VOL
0xE80102 CONSTANT SOUND-SOUND_FREQ

\ 中断向量定义
0 CONSTANT INT-RESET_SP  \ Reset (Initial SP)
4 CONSTANT INT-RESET_PC  \ Reset (Initial PC)
24 CONSTANT INT-AUTOVECTOR1  \ Auto vector 1
25 CONSTANT INT-AUTOVECTOR2  \ Auto vector 2
26 CONSTANT INT-AUTOVECTOR3  \ Auto vector 3
27 CONSTANT INT-AUTOVECTOR4  \ Auto vector 4
28 CONSTANT INT-AUTOVECTOR5  \ Auto vector 5
29 CONSTANT INT-AUTOVECTOR6  \ Auto vector 6
30 CONSTANT INT-AUTOVECTOR7  \ Auto vector 7
31 CONSTANT INT-SPURIOUS  \ Spurious interrupt

\ =========================================
\ 寄存器访问字
\ =========================================

\ 通用寄存器访问
: D0@ ( -- n ) D0 L@ ;
: D0! ( n -- ) D0 L! ;

: D1@ ( -- n ) D1 L@ ;
: D1! ( n -- ) D1 L! ;

: D2@ ( -- n ) D2 L@ ;
: D2! ( n -- ) D2 L! ;

: D3@ ( -- n ) D3 L@ ;
: D3! ( n -- ) D3 L! ;

: D4@ ( -- n ) D4 L@ ;
: D4! ( n -- ) D4 L! ;

: D5@ ( -- n ) D5 L@ ;
: D5! ( n -- ) D5 L! ;

: D6@ ( -- n ) D6 L@ ;
: D6! ( n -- ) D6 L! ;

: D7@ ( -- n ) D7 L@ ;
: D7! ( n -- ) D7 L! ;

: A0@ ( -- n ) A0 L@ ;
: A0! ( n -- ) A0 L! ;

: A1@ ( -- n ) A1 L@ ;
: A1! ( n -- ) A1 L! ;

: A2@ ( -- n ) A2 L@ ;
: A2! ( n -- ) A2 L! ;

: A3@ ( -- n ) A3 L@ ;
: A3! ( n -- ) A3 L! ;

: A4@ ( -- n ) A4 L@ ;
: A4! ( n -- ) A4 L! ;

: A5@ ( -- n ) A5 L@ ;
: A5! ( n -- ) A5 L! ;

: A6@ ( -- n ) A6 L@ ;
: A6! ( n -- ) A6 L! ;

: A7@ ( -- n ) A7 L@ ;
: A7! ( n -- ) A7 L! ;

: PC@ ( -- n ) PC L@ ;
: PC! ( n -- ) PC L! ;

: SR@ ( -- n ) SR @ ;
: SR! ( n -- ) SR ! ;

\ 外设访问
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
: VIA-VIA_ORA2@ ( -- n ) VIA-VIA_ORA2 C@ ;
: VIA-VIA_ORA2! ( n -- ) VIA-VIA_ORA2 C! ;

\ IWM外设
: IWM-IWM_Q6@ ( -- n ) IWM-IWM_Q6 C@ ;
: IWM-IWM_Q6! ( n -- ) IWM-IWM_Q6 C! ;
: IWM-IWM_Q7@ ( -- n ) IWM-IWM_Q7 C@ ;
: IWM-IWM_Q7! ( n -- ) IWM-IWM_Q7 C! ;
: IWM-IWM_PH0@ ( -- n ) IWM-IWM_PH0 C@ ;
: IWM-IWM_PH0! ( n -- ) IWM-IWM_PH0 C! ;
: IWM-IWM_PH1@ ( -- n ) IWM-IWM_PH1 C@ ;
: IWM-IWM_PH1! ( n -- ) IWM-IWM_PH1 C! ;
: IWM-IWM_PH2@ ( -- n ) IWM-IWM_PH2 C@ ;
: IWM-IWM_PH2! ( n -- ) IWM-IWM_PH2 C! ;
: IWM-IWM_PH3@ ( -- n ) IWM-IWM_PH3 C@ ;
: IWM-IWM_PH3! ( n -- ) IWM-IWM_PH3 C! ;

\ SCC外设
: SCC-SCC_CA@ ( -- n ) SCC-SCC_CA C@ ;
: SCC-SCC_CA! ( n -- ) SCC-SCC_CA C! ;
: SCC-SCC_DA@ ( -- n ) SCC-SCC_DA C@ ;
: SCC-SCC_DA! ( n -- ) SCC-SCC_DA C! ;
: SCC-SCC_CB@ ( -- n ) SCC-SCC_CB C@ ;
: SCC-SCC_CB! ( n -- ) SCC-SCC_CB C! ;
: SCC-SCC_DB@ ( -- n ) SCC-SCC_DB C@ ;
: SCC-SCC_DB! ( n -- ) SCC-SCC_DB C! ;

\ Sound外设
: SOUND-SOUND_VOL@ ( -- n ) SOUND-SOUND_VOL C@ ;
: SOUND-SOUND_VOL! ( n -- ) SOUND-SOUND_VOL C! ;
: SOUND-SOUND_FREQ@ ( -- n ) SOUND-SOUND_FREQ C@ ;
: SOUND-SOUND_FREQ! ( n -- ) SOUND-SOUND_FREQ C! ;

\ =========================================
\ 设备初始化
\ =========================================

: MACINTOSH_128K-INIT ( -- )
  \ 初始化Macintosh-128K设备
  ." 初始化Macintosh-128K..." CR

  \ 初始化寄存器
  0 D0!  \ Data Register 0
  0 D1!  \ Data Register 1
  0 D2!  \ Data Register 2
  0 D3!  \ Data Register 3
  0 D4!  \ Data Register 4
  0 D5!  \ Data Register 5
  0 D6!  \ Data Register 6
  0 D7!  \ Data Register 7
  0 A0!  \ Address Register 0
  0 A1!  \ Address Register 1
  0 A2!  \ Address Register 2
  0 A3!  \ Address Register 3
  0 A4!  \ Address Register 4
  0 A5!  \ Address Register 5
  0 A6!  \ Address Register 6
  0 A7!  \ Address Register 7 (SP)
  0 PC!  \ Program Counter
  0 SR!  \ Status Register

  \ 初始化外设
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
  0 VIA-VIA_ORA2!  \ VIA_ORA2寄存器
  \ 初始化IWM
  0 IWM-IWM_Q6!  \ IWM_Q6寄存器
  0 IWM-IWM_Q7!  \ IWM_Q7寄存器
  0 IWM-IWM_PH0!  \ IWM_PH0寄存器
  0 IWM-IWM_PH1!  \ IWM_PH1寄存器
  0 IWM-IWM_PH2!  \ IWM_PH2寄存器
  0 IWM-IWM_PH3!  \ IWM_PH3寄存器
  \ 初始化SCC
  0 SCC-SCC_CA!  \ SCC_CA寄存器
  0 SCC-SCC_DA!  \ SCC_DA寄存器
  0 SCC-SCC_CB!  \ SCC_CB寄存器
  0 SCC-SCC_DB!  \ SCC_DB寄存器
  \ 初始化Sound
  0 SOUND-SOUND_VOL!  \ SOUND_VOL寄存器
  0 SOUND-SOUND_FREQ!  \ SOUND_FREQ寄存器

  ." Macintosh-128K初始化完成" CR
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
  D0@ D0 .R 8 .R SPACE ."  D0: " D0@ .
  D1@ D1 .R 8 .R SPACE ."  D1: " D1@ .
  D2@ D2 .R 8 .R SPACE ."  D2: " D2@ .
  D3@ D3 .R 8 .R SPACE ."  D3: " D3@ .
  D4@ D4 .R 8 .R SPACE ."  D4: " D4@ .
  D5@ D5 .R 8 .R SPACE ."  D5: " D5@ .
  D6@ D6 .R 8 .R SPACE ."  D6: " D6@ .
  D7@ D7 .R 8 .R SPACE ."  D7: " D7@ .
  A0@ A0 .R 8 .R SPACE ."  A0: " A0@ .
  A1@ A1 .R 8 .R SPACE ."  A1: " A1@ .
  A2@ A2 .R 8 .R SPACE ."  A2: " A2@ .
  A3@ A3 .R 8 .R SPACE ."  A3: " A3@ .
  A4@ A4 .R 8 .R SPACE ."  A4: " A4@ .
  A5@ A5 .R 8 .R SPACE ."  A5: " A5@ .
  A6@ A6 .R 8 .R SPACE ."  A6: " A6@ .
  A7@ A7 .R 8 .R SPACE ."  A7: " A7@ .
  PC@ PC .R 8 .R SPACE ."  PC: " PC@ .
  SR@ SR .R 8 .R SPACE ."  SR: " SR@ .
;

\ =========================================
\ 中断处理
\ =========================================

\ Reset (Initial SP)
: INT-RESET_SP-HANDLER ( -- )
  ." RESET_SP中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET_SP-ENABLE ( -- )
  INT-RESET_SP INT-ENABLE
;

: INT-RESET_SP-DISABLE ( -- )
  INT-RESET_SP INT-DISABLE
;

\ Reset (Initial PC)
: INT-RESET_PC-HANDLER ( -- )
  ." RESET_PC中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-RESET_PC-ENABLE ( -- )
  INT-RESET_PC INT-ENABLE
;

: INT-RESET_PC-DISABLE ( -- )
  INT-RESET_PC INT-DISABLE
;

\ Auto vector 1
: INT-AUTOVECTOR1-HANDLER ( -- )
  ." AUTOVECTOR1中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-AUTOVECTOR1-ENABLE ( -- )
  INT-AUTOVECTOR1 INT-ENABLE
;

: INT-AUTOVECTOR1-DISABLE ( -- )
  INT-AUTOVECTOR1 INT-DISABLE
;

\ Auto vector 2
: INT-AUTOVECTOR2-HANDLER ( -- )
  ." AUTOVECTOR2中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-AUTOVECTOR2-ENABLE ( -- )
  INT-AUTOVECTOR2 INT-ENABLE
;

: INT-AUTOVECTOR2-DISABLE ( -- )
  INT-AUTOVECTOR2 INT-DISABLE
;

\ Auto vector 3
: INT-AUTOVECTOR3-HANDLER ( -- )
  ." AUTOVECTOR3中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-AUTOVECTOR3-ENABLE ( -- )
  INT-AUTOVECTOR3 INT-ENABLE
;

: INT-AUTOVECTOR3-DISABLE ( -- )
  INT-AUTOVECTOR3 INT-DISABLE
;

\ Auto vector 4
: INT-AUTOVECTOR4-HANDLER ( -- )
  ." AUTOVECTOR4中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-AUTOVECTOR4-ENABLE ( -- )
  INT-AUTOVECTOR4 INT-ENABLE
;

: INT-AUTOVECTOR4-DISABLE ( -- )
  INT-AUTOVECTOR4 INT-DISABLE
;

\ Auto vector 5
: INT-AUTOVECTOR5-HANDLER ( -- )
  ." AUTOVECTOR5中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-AUTOVECTOR5-ENABLE ( -- )
  INT-AUTOVECTOR5 INT-ENABLE
;

: INT-AUTOVECTOR5-DISABLE ( -- )
  INT-AUTOVECTOR5 INT-DISABLE
;

\ Auto vector 6
: INT-AUTOVECTOR6-HANDLER ( -- )
  ." AUTOVECTOR6中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-AUTOVECTOR6-ENABLE ( -- )
  INT-AUTOVECTOR6 INT-ENABLE
;

: INT-AUTOVECTOR6-DISABLE ( -- )
  INT-AUTOVECTOR6 INT-DISABLE
;

\ Auto vector 7
: INT-AUTOVECTOR7-HANDLER ( -- )
  ." AUTOVECTOR7中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-AUTOVECTOR7-ENABLE ( -- )
  INT-AUTOVECTOR7 INT-ENABLE
;

: INT-AUTOVECTOR7-DISABLE ( -- )
  INT-AUTOVECTOR7 INT-DISABLE
;

\ Spurious interrupt
: INT-SPURIOUS-HANDLER ( -- )
  ." SPURIOUS中断处理" CR
  \ 添加具体的中断处理代码
;

: INT-SPURIOUS-ENABLE ( -- )
  INT-SPURIOUS INT-ENABLE
;

: INT-SPURIOUS-DISABLE ( -- )
  INT-SPURIOUS INT-DISABLE
;

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  MACINTOSH_128K-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

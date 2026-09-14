\ Apple-IIe设备定义 - Forth文件
\ 生成自: Apple Computer/Apple II/Apple-IIe
\ 版本: 1.0
\ 日期: 2026-04-17
\ 作者: VML Team
\ 描述: Apple II Enhanced - 8-bit personal computer with MOS 6502 CPU
\ CPU架构: MOS-6502
\ 位宽: 8位
\ 时钟频率: 1021800 Hz

\ =========================================
\ Apple-IIe设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" Apple-IIe" ;
: MANUFACTURER  S" Apple Computer" ;
: FAMILY        S" Apple II" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" MOS-6502" ;
8 CONSTANT BITS
1021800 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT A  \ Accumulator
0x01 CONSTANT X  \ X Index Register
0x02 CONSTANT Y  \ Y Index Register
0x03 CONSTANT SP  \ Stack Pointer
0x04 CONSTANT PC  \ Program Counter
0x06 CONSTANT P  \ Processor Status
0 CONSTANT P-C  \ Carry Flag
1 CONSTANT P-Z  \ Zero Flag
2 CONSTANT P-I  \ Interrupt Disable
3 CONSTANT P-D  \ Decimal Mode
4 CONSTANT P-B  \ Break Command
5 CONSTANT P-U  \ Unused
6 CONSTANT P-V  \ Overflow Flag
7 CONSTANT P-N  \ Negative Flag

\ 内存段定义
0x0000 CONSTANT MAIN_RAM-START
0xBFFF CONSTANT MAIN_RAM-END
49152 CONSTANT MAIN_RAM-SIZE  \ Main RAM (48KB base, up to 64KB with slot RAM)
0x0400 CONSTANT TEXT_RAM-START
0x07FF CONSTANT TEXT_RAM-END
1024 CONSTANT TEXT_RAM-SIZE  \ Text screen buffer (40x24)
0x2000 CONSTANT HIRES_RAM-START
0x5FFF CONSTANT HIRES_RAM-END
16384 CONSTANT HIRES_RAM-SIZE  \ High-resolution graphics buffer
0x0400 CONSTANT AUX_RAM-START
0x09FF CONSTANT AUX_RAM-END
1536 CONSTANT AUX_RAM-SIZE  \ 80-column text auxiliary RAM
0xC100 CONSTANT MONITOR_ROM-START
0xCFFF CONSTANT MONITOR_ROM-END
3840 CONSTANT MONITOR_ROM-SIZE  \ Monitor ROM (applesoft/Integer)
0xD000 CONSTANT BASIC_ROM-START
0xFFFF CONSTANT BASIC_ROM-END
12288 CONSTANT BASIC_ROM-SIZE  \ Applesoft BASIC ROM
0xC100 CONSTANT SLOT_ROM-START
0xC7FF CONSTANT SLOT_ROM-END
768 CONSTANT SLOT_ROM-SIZE  \ Expansion Slot ROM
0xC080 CONSTANT MMIO-START
0xC0FF CONSTANT MMIO-END
128 CONSTANT MMIO-SIZE  \ I/O Select (slot space)

\ 外设定义
\ Versatile Interface Adapter (6522)
0xC000 CONSTANT VIA-BASE
0xC000 CONSTANT VIA-ORB
0xC001 CONSTANT VIA-ORA
0xC002 CONSTANT VIA-DDRB
0xC003 CONSTANT VIA-DDRA
0xC004 CONSTANT VIA-T1C
0xC006 CONSTANT VIA-T1L
0xC008 CONSTANT VIA-T2C
0xC00A CONSTANT VIA-SR
0xC00B CONSTANT VIA-ACR
0xC00C CONSTANT VIA-PCR
0xC00D CONSTANT VIA-IFG
0xC00E CONSTANT VIA-IER
0xC00F CONSTANT VIA-ORA_NH
\ Peripheral Interface Adapter (6520)
0xC010 CONSTANT PIA-BASE
0xC010 CONSTANT PIA-PA
0xC011 CONSTANT PIA-PB
0xC012 CONSTANT PIA-DDRA
0xC013 CONSTANT PIA-DDRB
0xC014 CONSTANT PIA-CA1
0xC015 CONSTANT PIA-CA2
0xC016 CONSTANT PIA-CB1
0xC017 CONSTANT PIA-CB2
\ Keyboard (via PIA)
0xC000 CONSTANT KBD-BASE
0xC000 CONSTANT KBD-KEYDATA
0xC010 CONSTANT KBD-KEYSTROBE
0xC025 CONSTANT KBD-KBDCTRL
0xC026 CONSTANT KBD-KBDERR
\ Speaker
0xC030 CONSTANT SPEAKER-BASE
0xC030 CONSTANT SPEAKER-SPKR
\ Game I/O Port
0xC050 CONSTANT GAME_PORT-BASE
0xC061 CONSTANT GAME_PORT-GAME_SW0
0xC062 CONSTANT GAME_PORT-GAME_SW1
0xC064 CONSTANT GAME_PORT-GAME_AN0
0xC065 CONSTANT GAME_PORT-GAME_AN1
0xC066 CONSTANT GAME_PORT-GAME_AN2
0xC067 CONSTANT GAME_PORT-GAME_AN3
0xC070 CONSTANT GAME_PORT-GAME_TRIG
\ Disk II Controller
0xC0E0 CONSTANT DISKII-BASE
0xC0E0 CONSTANT DISKII-PHASE0
0xC0E1 CONSTANT DISKII-PHASE1
0xC0E2 CONSTANT DISKII-PHASE2
0xC0E3 CONSTANT DISKII-PHASE3
0xC0EC CONSTANT DISKII-Q6L
0xC0ED CONSTANT DISKII-Q7L
0xC0EE CONSTANT DISKII-Q6R
0xC0EF CONSTANT DISKII-Q7R
\ Video Display Generator
0xC050 CONSTANT VIDEO-BASE
0xC050 CONSTANT VIDEO-TXTCLR
0xC051 CONSTANT VIDEO-MIXCLR
0xC054 CONSTANT VIDEO-TXTPAGE2
0xC055 CONSTANT VIDEO-TXTPAGE1
0xC056 CONSTANT VIDEO-LORES
0xC057 CONSTANT VIDEO-HIRES
0xC05E CONSTANT VIDEO-DHIRESON
0xC058 CONSTANT VIDEO-AN0
0xC059 CONSTANT VIDEO-AN1
0xC05A CONSTANT VIDEO-AN2
0xC05B CONSTANT VIDEO-AN3
0xC000 CONSTANT VIDEO-_80STORE
\ RAM Read/Write Control
0xC080 CONSTANT RAMRD-BASE
0xCFFF CONSTANT RAMRD-INTCXROM

\ 中断向量定义
0 CONSTANT INT-RESET  \ Power-on Reset
1 CONSTANT INT-NMI  \ Non-Maskable Interrupt (from VIA)
2 CONSTANT INT-IRQ  \ IRQ from VIA/timer/slot
3 CONSTANT INT-BRK  \ BRK Instruction

\ 引脚定义
1 CONSTANT PIN-VCC  \ +5V Power
2 CONSTANT PIN-GND  \ Ground
3 CONSTANT PIN-RESET  \ System Reset
4 CONSTANT PIN-CLK  \ System Clock (1.023MHz NTSC)
5 CONSTANT PIN-RDY  \ CPU Ready
6 CONSTANT PIN-NMI  \ Non-Maskable Interrupt
7 CONSTANT PIN-IRQ  \ Interrupt Request
8 CONSTANT PIN-SO  \ Set Overflow
9 CONSTANT PIN-RWB  \ Read/Write Bar
10 CONSTANT PIN-SYNC  \ Instruction Sync
11 CONSTANT PIN-A0_A15  \ Address Bus (16-bit)
12 CONSTANT PIN-D0_D7  \ Data Bus (8-bit)
13 CONSTANT PIN-PHASE0  \ Phase 0 (4MHz system)
14 CONSTANT PIN-PHASE1  \ Phase 1
15 CONSTANT PIN-PHASE2  \ Phase 2
16 CONSTANT PIN-PHASE3  \ Phase 3

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
: P-C@ ( -- flag ) P@ 0 BIT@ ;
: P-C! ( flag -- ) P@ 0 BIT! P! ;
: P-C-SET ( -- ) TRUE P-C! ;
: P-C-CLR ( -- ) FALSE P-C! ;
: P-Z@ ( -- flag ) P@ 1 BIT@ ;
: P-Z! ( flag -- ) P@ 1 BIT! P! ;
: P-Z-SET ( -- ) TRUE P-Z! ;
: P-Z-CLR ( -- ) FALSE P-Z! ;
: P-I@ ( -- flag ) P@ 2 BIT@ ;
: P-I! ( flag -- ) P@ 2 BIT! P! ;
: P-I-SET ( -- ) TRUE P-I! ;
: P-I-CLR ( -- ) FALSE P-I! ;
: P-D@ ( -- flag ) P@ 3 BIT@ ;
: P-D! ( flag -- ) P@ 3 BIT! P! ;
: P-D-SET ( -- ) TRUE P-D! ;
: P-D-CLR ( -- ) FALSE P-D! ;
: P-B@ ( -- flag ) P@ 4 BIT@ ;
: P-B! ( flag -- ) P@ 4 BIT! P! ;
: P-B-SET ( -- ) TRUE P-B! ;
: P-B-CLR ( -- ) FALSE P-B! ;
: P-U@ ( -- flag ) P@ 5 BIT@ ;
: P-U! ( flag -- ) P@ 5 BIT! P! ;
: P-U-SET ( -- ) TRUE P-U! ;
: P-U-CLR ( -- ) FALSE P-U! ;
: P-V@ ( -- flag ) P@ 6 BIT@ ;
: P-V! ( flag -- ) P@ 6 BIT! P! ;
: P-V-SET ( -- ) TRUE P-V! ;
: P-V-CLR ( -- ) FALSE P-V! ;
: P-N@ ( -- flag ) P@ 7 BIT@ ;
: P-N! ( flag -- ) P@ 7 BIT! P! ;
: P-N-SET ( -- ) TRUE P-N! ;
: P-N-CLR ( -- ) FALSE P-N! ;

\ 外设访问
\ VIA外设
: VIA-ORB@ ( -- n ) VIA-ORB C@ ;
: VIA-ORB! ( n -- ) VIA-ORB C! ;
: VIA-ORA@ ( -- n ) VIA-ORA C@ ;
: VIA-ORA! ( n -- ) VIA-ORA C! ;
: VIA-DDRB@ ( -- n ) VIA-DDRB C@ ;
: VIA-DDRB! ( n -- ) VIA-DDRB C! ;
: VIA-DDRA@ ( -- n ) VIA-DDRA C@ ;
: VIA-DDRA! ( n -- ) VIA-DDRA C! ;
: VIA-T1C@ ( -- n ) VIA-T1C @ ;
: VIA-T1C! ( n -- ) VIA-T1C ! ;
: VIA-T1L@ ( -- n ) VIA-T1L @ ;
: VIA-T1L! ( n -- ) VIA-T1L ! ;
: VIA-T2C@ ( -- n ) VIA-T2C @ ;
: VIA-T2C! ( n -- ) VIA-T2C ! ;
: VIA-SR@ ( -- n ) VIA-SR C@ ;
: VIA-SR! ( n -- ) VIA-SR C! ;
: VIA-ACR@ ( -- n ) VIA-ACR C@ ;
: VIA-ACR! ( n -- ) VIA-ACR C! ;
: VIA-PCR@ ( -- n ) VIA-PCR C@ ;
: VIA-PCR! ( n -- ) VIA-PCR C! ;
: VIA-IFG@ ( -- n ) VIA-IFG C@ ;
: VIA-IFG! ( n -- ) VIA-IFG C! ;
: VIA-IER@ ( -- n ) VIA-IER C@ ;
: VIA-IER! ( n -- ) VIA-IER C! ;
: VIA-ORA_NH@ ( -- n ) VIA-ORA_NH C@ ;
: VIA-ORA_NH! ( n -- ) VIA-ORA_NH C! ;

\ PIA外设
: PIA-PA@ ( -- n ) PIA-PA C@ ;
: PIA-PA! ( n -- ) PIA-PA C! ;
: PIA-PB@ ( -- n ) PIA-PB C@ ;
: PIA-PB! ( n -- ) PIA-PB C! ;
: PIA-DDRA@ ( -- n ) PIA-DDRA C@ ;
: PIA-DDRA! ( n -- ) PIA-DDRA C! ;
: PIA-DDRB@ ( -- n ) PIA-DDRB C@ ;
: PIA-DDRB! ( n -- ) PIA-DDRB C! ;
: PIA-CA1@ ( -- n ) PIA-CA1 C@ ;
: PIA-CA1! ( n -- ) PIA-CA1 C! ;
: PIA-CA2@ ( -- n ) PIA-CA2 C@ ;
: PIA-CA2! ( n -- ) PIA-CA2 C! ;
: PIA-CB1@ ( -- n ) PIA-CB1 C@ ;
: PIA-CB1! ( n -- ) PIA-CB1 C! ;
: PIA-CB2@ ( -- n ) PIA-CB2 C@ ;
: PIA-CB2! ( n -- ) PIA-CB2 C! ;

\ KBD外设
: KBD-KEYDATA@ ( -- n ) KBD-KEYDATA C@ ;
: KBD-KEYDATA! ( n -- ) KBD-KEYDATA C! ;
: KBD-KEYSTROBE@ ( -- n ) KBD-KEYSTROBE C@ ;
: KBD-KEYSTROBE! ( n -- ) KBD-KEYSTROBE C! ;
: KBD-KBDCTRL@ ( -- n ) KBD-KBDCTRL C@ ;
: KBD-KBDCTRL! ( n -- ) KBD-KBDCTRL C! ;
: KBD-KBDERR@ ( -- n ) KBD-KBDERR C@ ;
: KBD-KBDERR! ( n -- ) KBD-KBDERR C! ;

\ SPEAKER外设
: SPEAKER-SPKR@ ( -- n ) SPEAKER-SPKR C@ ;
: SPEAKER-SPKR! ( n -- ) SPEAKER-SPKR C! ;

\ GAME_PORT外设
: GAME_PORT-GAME_SW0@ ( -- n ) GAME_PORT-GAME_SW0 C@ ;
: GAME_PORT-GAME_SW0! ( n -- ) GAME_PORT-GAME_SW0 C! ;
: GAME_PORT-GAME_SW1@ ( -- n ) GAME_PORT-GAME_SW1 C@ ;
: GAME_PORT-GAME_SW1! ( n -- ) GAME_PORT-GAME_SW1 C! ;
: GAME_PORT-GAME_AN0@ ( -- n ) GAME_PORT-GAME_AN0 C@ ;
: GAME_PORT-GAME_AN0! ( n -- ) GAME_PORT-GAME_AN0 C! ;
: GAME_PORT-GAME_AN1@ ( -- n ) GAME_PORT-GAME_AN1 C@ ;
: GAME_PORT-GAME_AN1! ( n -- ) GAME_PORT-GAME_AN1 C! ;
: GAME_PORT-GAME_AN2@ ( -- n ) GAME_PORT-GAME_AN2 C@ ;
: GAME_PORT-GAME_AN2! ( n -- ) GAME_PORT-GAME_AN2 C! ;
: GAME_PORT-GAME_AN3@ ( -- n ) GAME_PORT-GAME_AN3 C@ ;
: GAME_PORT-GAME_AN3! ( n -- ) GAME_PORT-GAME_AN3 C! ;
: GAME_PORT-GAME_TRIG@ ( -- n ) GAME_PORT-GAME_TRIG C@ ;
: GAME_PORT-GAME_TRIG! ( n -- ) GAME_PORT-GAME_TRIG C! ;

\ DISKII外设
: DISKII-PHASE0@ ( -- n ) DISKII-PHASE0 C@ ;
: DISKII-PHASE0! ( n -- ) DISKII-PHASE0 C! ;
: DISKII-PHASE1@ ( -- n ) DISKII-PHASE1 C@ ;
: DISKII-PHASE1! ( n -- ) DISKII-PHASE1 C! ;
: DISKII-PHASE2@ ( -- n ) DISKII-PHASE2 C@ ;
: DISKII-PHASE2! ( n -- ) DISKII-PHASE2 C! ;
: DISKII-PHASE3@ ( -- n ) DISKII-PHASE3 C@ ;
: DISKII-PHASE3! ( n -- ) DISKII-PHASE3 C! ;
: DISKII-Q6L@ ( -- n ) DISKII-Q6L C@ ;
: DISKII-Q6L! ( n -- ) DISKII-Q6L C! ;
: DISKII-Q7L@ ( -- n ) DISKII-Q7L C@ ;
: DISKII-Q7L! ( n -- ) DISKII-Q7L C! ;
: DISKII-Q6R@ ( -- n ) DISKII-Q6R C@ ;
: DISKII-Q6R! ( n -- ) DISKII-Q6R C! ;
: DISKII-Q7R@ ( -- n ) DISKII-Q7R C@ ;
: DISKII-Q7R! ( n -- ) DISKII-Q7R C! ;

\ VIDEO外设
: VIDEO-TXTCLR@ ( -- n ) VIDEO-TXTCLR C@ ;
: VIDEO-TXTCLR! ( n -- ) VIDEO-TXTCLR C! ;
: VIDEO-MIXCLR@ ( -- n ) VIDEO-MIXCLR C@ ;
: VIDEO-MIXCLR! ( n -- ) VIDEO-MIXCLR C! ;
: VIDEO-TXTPAGE2@ ( -- n ) VIDEO-TXTPAGE2 C@ ;
: VIDEO-TXTPAGE2! ( n -- ) VIDEO-TXTPAGE2 C! ;
: VIDEO-TXTPAGE1@ ( -- n ) VIDEO-TXTPAGE1 C@ ;
: VIDEO-TXTPAGE1! ( n -- ) VIDEO-TXTPAGE1 C! ;
: VIDEO-LORES@ ( -- n ) VIDEO-LORES C@ ;
: VIDEO-LORES! ( n -- ) VIDEO-LORES C! ;
: VIDEO-HIRES@ ( -- n ) VIDEO-HIRES C@ ;
: VIDEO-HIRES! ( n -- ) VIDEO-HIRES C! ;
: VIDEO-DHIRESON@ ( -- n ) VIDEO-DHIRESON C@ ;
: VIDEO-DHIRESON! ( n -- ) VIDEO-DHIRESON C! ;
: VIDEO-AN0@ ( -- n ) VIDEO-AN0 C@ ;
: VIDEO-AN0! ( n -- ) VIDEO-AN0 C! ;
: VIDEO-AN1@ ( -- n ) VIDEO-AN1 C@ ;
: VIDEO-AN1! ( n -- ) VIDEO-AN1 C! ;
: VIDEO-AN2@ ( -- n ) VIDEO-AN2 C@ ;
: VIDEO-AN2! ( n -- ) VIDEO-AN2 C! ;
: VIDEO-AN3@ ( -- n ) VIDEO-AN3 C@ ;
: VIDEO-AN3! ( n -- ) VIDEO-AN3 C! ;
: VIDEO-_80STORE@ ( -- n ) VIDEO-_80STORE C@ ;
: VIDEO-_80STORE! ( n -- ) VIDEO-_80STORE C! ;

\ RAMRD外设
: RAMRD-INTCXROM@ ( -- n ) RAMRD-INTCXROM C@ ;
: RAMRD-INTCXROM! ( n -- ) RAMRD-INTCXROM C! ;

\ =========================================
\ 设备初始化
\ =========================================

: APPLE_IIE-INIT ( -- )
  \ 初始化Apple-IIe设备
  ." 初始化Apple-IIe..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 X!  \ X Index Register
  0 Y!  \ Y Index Register
  0 SP!  \ Stack Pointer
  0 PC!  \ Program Counter
  0 P!  \ Processor Status

  \ 初始化外设
  \ 初始化VIA
  0 VIA-ORB!  \ ORB寄存器
  0 VIA-ORA!  \ ORA寄存器
  0 VIA-DDRB!  \ DDRB寄存器
  0 VIA-DDRA!  \ DDRA寄存器
  0 VIA-T1C!  \ T1C寄存器
  0 VIA-T1L!  \ T1L寄存器
  0 VIA-T2C!  \ T2C寄存器
  0 VIA-SR!  \ SR寄存器
  0 VIA-ACR!  \ ACR寄存器
  0 VIA-PCR!  \ PCR寄存器
  0 VIA-IFG!  \ IFG寄存器
  0 VIA-IER!  \ IER寄存器
  0 VIA-ORA_NH!  \ ORA_NH寄存器
  \ 初始化PIA
  0 PIA-PA!  \ PA寄存器
  0 PIA-PB!  \ PB寄存器
  0 PIA-DDRA!  \ DDRA寄存器
  0 PIA-DDRB!  \ DDRB寄存器
  0 PIA-CA1!  \ CA1寄存器
  0 PIA-CA2!  \ CA2寄存器
  0 PIA-CB1!  \ CB1寄存器
  0 PIA-CB2!  \ CB2寄存器
  \ 初始化KBD
  0 KBD-KEYDATA!  \ KEYDATA寄存器
  0 KBD-KEYSTROBE!  \ KEYSTROBE寄存器
  0 KBD-KBDCTRL!  \ KBDCTRL寄存器
  0 KBD-KBDERR!  \ KBDERR寄存器
  \ 初始化SPEAKER
  0 SPEAKER-SPKR!  \ SPKR寄存器
  \ 初始化GAME_PORT
  0 GAME_PORT-GAME_SW0!  \ GAME_SW0寄存器
  0 GAME_PORT-GAME_SW1!  \ GAME_SW1寄存器
  0 GAME_PORT-GAME_AN0!  \ GAME_AN0寄存器
  0 GAME_PORT-GAME_AN1!  \ GAME_AN1寄存器
  0 GAME_PORT-GAME_AN2!  \ GAME_AN2寄存器
  0 GAME_PORT-GAME_AN3!  \ GAME_AN3寄存器
  0 GAME_PORT-GAME_TRIG!  \ GAME_TRIG寄存器
  \ 初始化DISKII
  0 DISKII-PHASE0!  \ PHASE0寄存器
  0 DISKII-PHASE1!  \ PHASE1寄存器
  0 DISKII-PHASE2!  \ PHASE2寄存器
  0 DISKII-PHASE3!  \ PHASE3寄存器
  0 DISKII-Q6L!  \ Q6L寄存器
  0 DISKII-Q7L!  \ Q7L寄存器
  0 DISKII-Q6R!  \ Q6R寄存器
  0 DISKII-Q7R!  \ Q7R寄存器
  \ 初始化VIDEO
  0 VIDEO-TXTCLR!  \ TXTCLR寄存器
  0 VIDEO-MIXCLR!  \ MIXCLR寄存器
  0 VIDEO-TXTPAGE2!  \ TXTPAGE2寄存器
  0 VIDEO-TXTPAGE1!  \ TXTPAGE1寄存器
  0 VIDEO-LORES!  \ LORES寄存器
  0 VIDEO-HIRES!  \ HIRES寄存器
  0 VIDEO-DHIRESON!  \ DHIRESON寄存器
  0 VIDEO-AN0!  \ AN0寄存器
  0 VIDEO-AN1!  \ AN1寄存器
  0 VIDEO-AN2!  \ AN2寄存器
  0 VIDEO-AN3!  \ AN3寄存器
  0 VIDEO-_80STORE!  \ 80STORE寄存器
  \ 初始化RAMRD
  0 RAMRD-INTCXROM!  \ INTCXROM寄存器

  ." Apple-IIe初始化完成" CR
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
\ 引脚操作
\ =========================================

\ =========================================
\ 中断处理
\ =========================================

\ Power-on Reset
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

\ Non-Maskable Interrupt (from VIA)
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

\ IRQ from VIA/timer/slot
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

\ BRK Instruction
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
  APPLE_IIE-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

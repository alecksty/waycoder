\ MOS-6507设备定义 - Forth文件
\ 生成自: MOS Technology/MOS-6502/MOS-6507
\ 版本: 1.0
\ 日期: 2026-04-16
\ 作者: VML Team
\ 描述: Atari 2600 VCS main processor - MOS 6507 (simplified 6502) @ 1.19MHz with TIA and RIOT
\ CPU架构: MOS-6507
\ 位宽: 8位
\ 时钟频率: 1190000 Hz

\ =========================================
\ MOS-6507设备定义
\ =========================================

\ 设备信息
: DEVICE-NAME   S" MOS-6507" ;
: MANUFACTURER  S" MOS Technology" ;
: FAMILY        S" MOS-6502" ;
: VERSION       S" 1.0" ;
: ARCHITECTURE  S" MOS-6507" ;
8 CONSTANT BITS
1190000 CONSTANT CLOCK-FREQ

\ 寄存器地址定义
0x00 CONSTANT A  \ Accumulator
0x01 CONSTANT X  \ X Index
0x02 CONSTANT Y  \ Y Index
0x03 CONSTANT SP  \ Stack Pointer (6-bit, 128-byte stack)
0x04 CONSTANT PC  \ Program Counter (16-bit)
0x06 CONSTANT P  \ Processor Status
7 CONSTANT P-N  \ Negative
6 CONSTANT P-V  \ Overflow
4 CONSTANT P-B  \ Break
3 CONSTANT P-D  \ Decimal Mode (N/A on 6507)
2 CONSTANT P-I  \ Interrupt Disable
1 CONSTANT P-Z  \ Zero
0 CONSTANT P-C  \ Carry

\ 内存段定义
0x0000 CONSTANT TIA_REGS-START
0x007F CONSTANT TIA_REGS-END
128 CONSTANT TIA_REGS-SIZE  \ TIA Registers
0x0080 CONSTANT RIOT_RAM-START
0x00FF CONSTANT RIOT_RAM-END
128 CONSTANT RIOT_RAM-SIZE  \ RIOT 128byte RAM mirrored
0x0280 CONSTANT RIOT_IO-START
0x029F CONSTANT RIOT_IO-END
32 CONSTANT RIOT_IO-SIZE  \ RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
0x1000 CONSTANT CART_ROM-START
0x1FFF CONSTANT CART_ROM-END
4096 CONSTANT CART_ROM-SIZE  \ Cartridge ROM (4KB, bank-switched)

\ 外设定义
\ Television Interface Adaptor (Video + Audio + I/O)
0x0000 CONSTANT TIA-BASE
0x00 CONSTANT TIA-VSYNC
0x01 CONSTANT TIA-VBLANK
7 CONSTANT TIA-VBLANK-D7  \ Inhibit D7 (1=disable D7 output to PB7)
6 CONSTANT TIA-VBLANK-D6  \ Inhibit D6 (1=disable D6 output to PB6)
5 CONSTANT TIA-VBLANK-D5  \ Inhibit D5 (1=disable D5 output to PB5)
4 CONSTANT TIA-VBLANK-D4  \ Inhibit D4 (1=disable D4 output to PB4)
3 CONSTANT TIA-VBLANK-D3  \ Inhibit D3 (1=disable D3 output to PB3)
2 CONSTANT TIA-VBLANK-D2  \ Inhibit D2 (1=disable D2 output to PB2)
1 CONSTANT TIA-VBLANK-D1  \ Inhibit D1 (1=disable D1 output to PB1)
0 CONSTANT TIA-VBLANK-D0  \ Inhibit D0 (1=disable D0 output to PB0)
5 CONSTANT TIA-VBLANK-VBW  \ Vertical Blank Enable (1=set VBLANK)
1 CONSTANT TIA-VBLANK-VBL  \ Vertical Blank Set (1=V-Blank active)
0 CONSTANT TIA-VBLANK-RESBL  \ Reset Blank (1=allow VSYNC/VBLANK reset on clock)
0x02 CONSTANT TIA-WSYNC
0x03 CONSTANT TIA-RSYNC
0x04 CONSTANT TIA-NUSIZ0
0 CONSTANT TIA-NUSIZ0-NUSIZ  \ Number/Size Code (0-7)
0 CONSTANT TIA-NUSIZ0-MISSILE_SIZE  \ Missile Size
6 CONSTANT TIA-NUSIZ0-RESM0  \ Reset M0
7 CONSTANT TIA-NUSIZ0-RESM1  \ Reset M1
0x05 CONSTANT TIA-NUSIZ1
0x06 CONSTANT TIA-COLUP0
0x07 CONSTANT TIA-COLUP1
0x08 CONSTANT TIA-COLUPF
0x09 CONSTANT TIA-COLUBK
0x0A CONSTANT TIA-CTRLPF
0 CONSTANT TIA-CTRLPF-DELL  \ Delay Playfield L (Reflected/Left score)
0 CONSTANT TIA-CTRLPF-BALL_SIZE  \ Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
5 CONSTANT TIA-CTRLPF-REF  \ Reflect (1=mirror playfield)
6 CONSTANT TIA-CTRLPF-SCORE  \ Score Mode (1=use player colors for L/R halves)
7 CONSTANT TIA-CTRLPF-DELBL  \ Delay Ball (1=delay ball 1 clock)
0x0B CONSTANT TIA-REFPL
0x0D CONSTANT TIA-PF0
0x0E CONSTANT TIA-PF1
0x0F CONSTANT TIA-PF2
0x10 CONSTANT TIA-RESP0
0x11 CONSTANT TIA-RESP1
0x12 CONSTANT TIA-RESM0
0x13 CONSTANT TIA-RESM1
0x14 CONSTANT TIA-RESBL
0x15 CONSTANT TIA-AUDC0
0 CONSTANT TIA-AUDC0-VOL  \ Volume (0-15)
0 CONSTANT TIA-AUDC0-TONE  \ Tone Divisor (5-bit counter)
0x16 CONSTANT TIA-AUDC1
0x17 CONSTANT TIA-AUDF0
0x18 CONSTANT TIA-AUDF1
0x19 CONSTANT TIA-AUDV0
0x1A CONSTANT TIA-AUDV1
0x1B CONSTANT TIA-GRP0
0x1C CONSTANT TIA-GRP1
0x1D CONSTANT TIA-DGRP0
0x1E CONSTANT TIA-DGRP1
0x1F CONSTANT TIA-ENAM0
0x20 CONSTANT TIA-ENAM1
0x21 CONSTANT TIA-ENABL
0x22 CONSTANT TIA-HMP0
0x23 CONSTANT TIA-HMP1
0x24 CONSTANT TIA-HMM0
0x25 CONSTANT TIA-HMM1
0x26 CONSTANT TIA-HMBL
0x27 CONSTANT TIA-VDEL0
0x28 CONSTANT TIA-VDEL1
0x29 CONSTANT TIA-VDELBL
0x2A CONSTANT TIA-RESBB
0x2A CONSTANT TIA-HMOVE
0x2B CONSTANT TIA-HMCLR
0x30 CONSTANT TIA-CXM0P
0x31 CONSTANT TIA-CXM1P
0x32 CONSTANT TIA-CXP0FB
0x33 CONSTANT TIA-CXP1FB
0x34 CONSTANT TIA-CXM0FB
0x35 CONSTANT TIA-CXM1FB
0x36 CONSTANT TIA-CXBLPF
0x37 CONSTANT TIA-CXPPMM
0x38 CONSTANT TIA-INPT0
0x39 CONSTANT TIA-INPT1
0x3A CONSTANT TIA-INPT2
0x3B CONSTANT TIA-INPT3
0x3C CONSTANT TIA-INPT4
0x3D CONSTANT TIA-INPT5
\ RAM, I/O, Timer (6532 RIOT)
0x0080 CONSTANT RIOT-BASE
0x280 CONSTANT RIOT-SWCHA
0x281 CONSTANT RIOT-SWACNT
0x282 CONSTANT RIOT-SWCHB
1 CONSTANT RIOT-SWCHB-RESET  \ Game Reset Switch (0=pressed)
2 CONSTANT RIOT-SWCHB-SELECT  \ Game Select Switch (0=pressed)
3 CONSTANT RIOT-SWCHB-DIFFB  \ Difficulty B (0=hard, 1=easy)
4 CONSTANT RIOT-SWCHB-DIFFA  \ Difficulty A (0=hard, 1=easy)
0x283 CONSTANT RIOT-SWBCNT
0x284 CONSTANT RIOT-INTIM
0x285 CONSTANT RIOT-TIMINT
0x294 CONSTANT RIOT-TIM1T
0x295 CONSTANT RIOT-TIM8T
0x296 CONSTANT RIOT-TIM64T
0x297 CONSTANT RIOT-TIM1024T
\ Controller Port 1 (Joystick)
0x280 CONSTANT CONTROLLER1-BASE
0x280 CONSTANT CONTROLLER1-SWCHA
\ Controller Port 2 (Joystick)
0x281 CONSTANT CONTROLLER2-BASE
0x280 CONSTANT CONTROLLER2-SWCHA

\ 中断向量定义
0 CONSTANT INT-RESET  \ Power-On Reset

\ 引脚定义
1 CONSTANT PIN-VSS  \ Ground
2 CONSTANT PIN-VCC  \ Power Supply
3 CONSTANT PIN-PHI0  \ Clock Input (1.19MHz NTSC / 1.18MHz PAL)
4 CONSTANT PIN-RESET  \ Reset (active low)
5 CONSTANT PIN-A0  \ Address Bus Bit 0
6 CONSTANT PIN-A1  \ Address Bus Bit 1
7 CONSTANT PIN-A2  \ Address Bus Bit 2
8 CONSTANT PIN-A3  \ Address Bus Bit 3
9 CONSTANT PIN-A4  \ Address Bus Bit 4
10 CONSTANT PIN-A5  \ Address Bus Bit 5
11 CONSTANT PIN-A6  \ Address Bus Bit 6
12 CONSTANT PIN-A7  \ Address Bus Bit 7
13 CONSTANT PIN-A8  \ Address Bus Bit 8
14 CONSTANT PIN-A9  \ Address Bus Bit 9
15 CONSTANT PIN-A10  \ Address Bus Bit 10
16 CONSTANT PIN-A11  \ Address Bus Bit 11
17 CONSTANT PIN-A12  \ Address Bus Bit 12
18 CONSTANT PIN-D0  \ Data Bus Bit 0
19 CONSTANT PIN-D1  \ Data Bus Bit 1
20 CONSTANT PIN-D2  \ Data Bus Bit 2
21 CONSTANT PIN-D3  \ Data Bus Bit 3
22 CONSTANT PIN-D4  \ Data Bus Bit 4
23 CONSTANT PIN-D5  \ Data Bus Bit 5
24 CONSTANT PIN-D6  \ Data Bus Bit 6
25 CONSTANT PIN-D7  \ Data Bus Bit 7
26 CONSTANT PIN-RDY  \ Ready (stops CPU on read)
27 CONSTANT PIN-R_W  \ Read/Write (1=Read, 0=Write)
28 CONSTANT PIN-NC  \ Not Connected

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
: P-N@ ( -- flag ) P@ 7 BIT@ ;
: P-N! ( flag -- ) P@ 7 BIT! P! ;
: P-N-SET ( -- ) TRUE P-N! ;
: P-N-CLR ( -- ) FALSE P-N! ;
: P-V@ ( -- flag ) P@ 6 BIT@ ;
: P-V! ( flag -- ) P@ 6 BIT! P! ;
: P-V-SET ( -- ) TRUE P-V! ;
: P-V-CLR ( -- ) FALSE P-V! ;
: P-B@ ( -- flag ) P@ 4 BIT@ ;
: P-B! ( flag -- ) P@ 4 BIT! P! ;
: P-B-SET ( -- ) TRUE P-B! ;
: P-B-CLR ( -- ) FALSE P-B! ;
: P-D@ ( -- flag ) P@ 3 BIT@ ;
: P-D! ( flag -- ) P@ 3 BIT! P! ;
: P-D-SET ( -- ) TRUE P-D! ;
: P-D-CLR ( -- ) FALSE P-D! ;
: P-I@ ( -- flag ) P@ 2 BIT@ ;
: P-I! ( flag -- ) P@ 2 BIT! P! ;
: P-I-SET ( -- ) TRUE P-I! ;
: P-I-CLR ( -- ) FALSE P-I! ;
: P-Z@ ( -- flag ) P@ 1 BIT@ ;
: P-Z! ( flag -- ) P@ 1 BIT! P! ;
: P-Z-SET ( -- ) TRUE P-Z! ;
: P-Z-CLR ( -- ) FALSE P-Z! ;
: P-C@ ( -- flag ) P@ 0 BIT@ ;
: P-C! ( flag -- ) P@ 0 BIT! P! ;
: P-C-SET ( -- ) TRUE P-C! ;
: P-C-CLR ( -- ) FALSE P-C! ;

\ 外设访问
\ TIA外设
: TIA-VSYNC@ ( -- n ) TIA-VSYNC C@ ;
: TIA-VSYNC! ( n -- ) TIA-VSYNC C! ;
: TIA-VBLANK@ ( -- n ) TIA-VBLANK C@ ;
: TIA-VBLANK! ( n -- ) TIA-VBLANK C! ;
: TIA-VBLANK-D7@ ( -- flag ) TIA-VBLANK@ 7 BIT@ ;
: TIA-VBLANK-D7! ( flag -- ) TIA-VBLANK@ 7 BIT! TIA-VBLANK! ;
: TIA-VBLANK-D6@ ( -- flag ) TIA-VBLANK@ 6 BIT@ ;
: TIA-VBLANK-D6! ( flag -- ) TIA-VBLANK@ 6 BIT! TIA-VBLANK! ;
: TIA-VBLANK-D5@ ( -- flag ) TIA-VBLANK@ 5 BIT@ ;
: TIA-VBLANK-D5! ( flag -- ) TIA-VBLANK@ 5 BIT! TIA-VBLANK! ;
: TIA-VBLANK-D4@ ( -- flag ) TIA-VBLANK@ 4 BIT@ ;
: TIA-VBLANK-D4! ( flag -- ) TIA-VBLANK@ 4 BIT! TIA-VBLANK! ;
: TIA-VBLANK-D3@ ( -- flag ) TIA-VBLANK@ 3 BIT@ ;
: TIA-VBLANK-D3! ( flag -- ) TIA-VBLANK@ 3 BIT! TIA-VBLANK! ;
: TIA-VBLANK-D2@ ( -- flag ) TIA-VBLANK@ 2 BIT@ ;
: TIA-VBLANK-D2! ( flag -- ) TIA-VBLANK@ 2 BIT! TIA-VBLANK! ;
: TIA-VBLANK-D1@ ( -- flag ) TIA-VBLANK@ 1 BIT@ ;
: TIA-VBLANK-D1! ( flag -- ) TIA-VBLANK@ 1 BIT! TIA-VBLANK! ;
: TIA-VBLANK-D0@ ( -- flag ) TIA-VBLANK@ 0 BIT@ ;
: TIA-VBLANK-D0! ( flag -- ) TIA-VBLANK@ 0 BIT! TIA-VBLANK! ;
: TIA-VBLANK-VBW@ ( -- flag ) TIA-VBLANK@ 5 BIT@ ;
: TIA-VBLANK-VBW! ( flag -- ) TIA-VBLANK@ 5 BIT! TIA-VBLANK! ;
: TIA-VBLANK-VBL@ ( -- flag ) TIA-VBLANK@ 1 BIT@ ;
: TIA-VBLANK-VBL! ( flag -- ) TIA-VBLANK@ 1 BIT! TIA-VBLANK! ;
: TIA-VBLANK-RESBL@ ( -- flag ) TIA-VBLANK@ 0 BIT@ ;
: TIA-VBLANK-RESBL! ( flag -- ) TIA-VBLANK@ 0 BIT! TIA-VBLANK! ;
: TIA-WSYNC@ ( -- n ) TIA-WSYNC C@ ;
: TIA-WSYNC! ( n -- ) TIA-WSYNC C! ;
: TIA-RSYNC@ ( -- n ) TIA-RSYNC C@ ;
: TIA-RSYNC! ( n -- ) TIA-RSYNC C! ;
: TIA-NUSIZ0@ ( -- n ) TIA-NUSIZ0 C@ ;
: TIA-NUSIZ0! ( n -- ) TIA-NUSIZ0 C! ;
: TIA-NUSIZ0-NUSIZ@ ( -- flag ) TIA-NUSIZ0@ 0 BIT@ ;
: TIA-NUSIZ0-NUSIZ! ( flag -- ) TIA-NUSIZ0@ 0 BIT! TIA-NUSIZ0! ;
: TIA-NUSIZ0-MISSILE_SIZE@ ( -- flag ) TIA-NUSIZ0@ 0 BIT@ ;
: TIA-NUSIZ0-MISSILE_SIZE! ( flag -- ) TIA-NUSIZ0@ 0 BIT! TIA-NUSIZ0! ;
: TIA-NUSIZ0-RESM0@ ( -- flag ) TIA-NUSIZ0@ 6 BIT@ ;
: TIA-NUSIZ0-RESM0! ( flag -- ) TIA-NUSIZ0@ 6 BIT! TIA-NUSIZ0! ;
: TIA-NUSIZ0-RESM1@ ( -- flag ) TIA-NUSIZ0@ 7 BIT@ ;
: TIA-NUSIZ0-RESM1! ( flag -- ) TIA-NUSIZ0@ 7 BIT! TIA-NUSIZ0! ;
: TIA-NUSIZ1@ ( -- n ) TIA-NUSIZ1 C@ ;
: TIA-NUSIZ1! ( n -- ) TIA-NUSIZ1 C! ;
: TIA-COLUP0@ ( -- n ) TIA-COLUP0 C@ ;
: TIA-COLUP0! ( n -- ) TIA-COLUP0 C! ;
: TIA-COLUP1@ ( -- n ) TIA-COLUP1 C@ ;
: TIA-COLUP1! ( n -- ) TIA-COLUP1 C! ;
: TIA-COLUPF@ ( -- n ) TIA-COLUPF C@ ;
: TIA-COLUPF! ( n -- ) TIA-COLUPF C! ;
: TIA-COLUBK@ ( -- n ) TIA-COLUBK C@ ;
: TIA-COLUBK! ( n -- ) TIA-COLUBK C! ;
: TIA-CTRLPF@ ( -- n ) TIA-CTRLPF C@ ;
: TIA-CTRLPF! ( n -- ) TIA-CTRLPF C! ;
: TIA-CTRLPF-DELL@ ( -- flag ) TIA-CTRLPF@ 0 BIT@ ;
: TIA-CTRLPF-DELL! ( flag -- ) TIA-CTRLPF@ 0 BIT! TIA-CTRLPF! ;
: TIA-CTRLPF-BALL_SIZE@ ( -- flag ) TIA-CTRLPF@ 0 BIT@ ;
: TIA-CTRLPF-BALL_SIZE! ( flag -- ) TIA-CTRLPF@ 0 BIT! TIA-CTRLPF! ;
: TIA-CTRLPF-REF@ ( -- flag ) TIA-CTRLPF@ 5 BIT@ ;
: TIA-CTRLPF-REF! ( flag -- ) TIA-CTRLPF@ 5 BIT! TIA-CTRLPF! ;
: TIA-CTRLPF-SCORE@ ( -- flag ) TIA-CTRLPF@ 6 BIT@ ;
: TIA-CTRLPF-SCORE! ( flag -- ) TIA-CTRLPF@ 6 BIT! TIA-CTRLPF! ;
: TIA-CTRLPF-DELBL@ ( -- flag ) TIA-CTRLPF@ 7 BIT@ ;
: TIA-CTRLPF-DELBL! ( flag -- ) TIA-CTRLPF@ 7 BIT! TIA-CTRLPF! ;
: TIA-REFPL@ ( -- n ) TIA-REFPL C@ ;
: TIA-REFPL! ( n -- ) TIA-REFPL C! ;
: TIA-PF0@ ( -- n ) TIA-PF0 C@ ;
: TIA-PF0! ( n -- ) TIA-PF0 C! ;
: TIA-PF1@ ( -- n ) TIA-PF1 C@ ;
: TIA-PF1! ( n -- ) TIA-PF1 C! ;
: TIA-PF2@ ( -- n ) TIA-PF2 C@ ;
: TIA-PF2! ( n -- ) TIA-PF2 C! ;
: TIA-RESP0@ ( -- n ) TIA-RESP0 C@ ;
: TIA-RESP0! ( n -- ) TIA-RESP0 C! ;
: TIA-RESP1@ ( -- n ) TIA-RESP1 C@ ;
: TIA-RESP1! ( n -- ) TIA-RESP1 C! ;
: TIA-RESM0@ ( -- n ) TIA-RESM0 C@ ;
: TIA-RESM0! ( n -- ) TIA-RESM0 C! ;
: TIA-RESM1@ ( -- n ) TIA-RESM1 C@ ;
: TIA-RESM1! ( n -- ) TIA-RESM1 C! ;
: TIA-RESBL@ ( -- n ) TIA-RESBL C@ ;
: TIA-RESBL! ( n -- ) TIA-RESBL C! ;
: TIA-AUDC0@ ( -- n ) TIA-AUDC0 C@ ;
: TIA-AUDC0! ( n -- ) TIA-AUDC0 C! ;
: TIA-AUDC0-VOL@ ( -- flag ) TIA-AUDC0@ 0 BIT@ ;
: TIA-AUDC0-VOL! ( flag -- ) TIA-AUDC0@ 0 BIT! TIA-AUDC0! ;
: TIA-AUDC0-TONE@ ( -- flag ) TIA-AUDC0@ 0 BIT@ ;
: TIA-AUDC0-TONE! ( flag -- ) TIA-AUDC0@ 0 BIT! TIA-AUDC0! ;
: TIA-AUDC1@ ( -- n ) TIA-AUDC1 C@ ;
: TIA-AUDC1! ( n -- ) TIA-AUDC1 C! ;
: TIA-AUDF0@ ( -- n ) TIA-AUDF0 C@ ;
: TIA-AUDF0! ( n -- ) TIA-AUDF0 C! ;
: TIA-AUDF1@ ( -- n ) TIA-AUDF1 C@ ;
: TIA-AUDF1! ( n -- ) TIA-AUDF1 C! ;
: TIA-AUDV0@ ( -- n ) TIA-AUDV0 C@ ;
: TIA-AUDV0! ( n -- ) TIA-AUDV0 C! ;
: TIA-AUDV1@ ( -- n ) TIA-AUDV1 C@ ;
: TIA-AUDV1! ( n -- ) TIA-AUDV1 C! ;
: TIA-GRP0@ ( -- n ) TIA-GRP0 C@ ;
: TIA-GRP0! ( n -- ) TIA-GRP0 C! ;
: TIA-GRP1@ ( -- n ) TIA-GRP1 C@ ;
: TIA-GRP1! ( n -- ) TIA-GRP1 C! ;
: TIA-DGRP0@ ( -- n ) TIA-DGRP0 C@ ;
: TIA-DGRP0! ( n -- ) TIA-DGRP0 C! ;
: TIA-DGRP1@ ( -- n ) TIA-DGRP1 C@ ;
: TIA-DGRP1! ( n -- ) TIA-DGRP1 C! ;
: TIA-ENAM0@ ( -- n ) TIA-ENAM0 C@ ;
: TIA-ENAM0! ( n -- ) TIA-ENAM0 C! ;
: TIA-ENAM1@ ( -- n ) TIA-ENAM1 C@ ;
: TIA-ENAM1! ( n -- ) TIA-ENAM1 C! ;
: TIA-ENABL@ ( -- n ) TIA-ENABL C@ ;
: TIA-ENABL! ( n -- ) TIA-ENABL C! ;
: TIA-HMP0@ ( -- n ) TIA-HMP0 C@ ;
: TIA-HMP0! ( n -- ) TIA-HMP0 C! ;
: TIA-HMP1@ ( -- n ) TIA-HMP1 C@ ;
: TIA-HMP1! ( n -- ) TIA-HMP1 C! ;
: TIA-HMM0@ ( -- n ) TIA-HMM0 C@ ;
: TIA-HMM0! ( n -- ) TIA-HMM0 C! ;
: TIA-HMM1@ ( -- n ) TIA-HMM1 C@ ;
: TIA-HMM1! ( n -- ) TIA-HMM1 C! ;
: TIA-HMBL@ ( -- n ) TIA-HMBL C@ ;
: TIA-HMBL! ( n -- ) TIA-HMBL C! ;
: TIA-VDEL0@ ( -- n ) TIA-VDEL0 C@ ;
: TIA-VDEL0! ( n -- ) TIA-VDEL0 C! ;
: TIA-VDEL1@ ( -- n ) TIA-VDEL1 C@ ;
: TIA-VDEL1! ( n -- ) TIA-VDEL1 C! ;
: TIA-VDELBL@ ( -- n ) TIA-VDELBL C@ ;
: TIA-VDELBL! ( n -- ) TIA-VDELBL C! ;
: TIA-RESBB@ ( -- n ) TIA-RESBB C@ ;
: TIA-RESBB! ( n -- ) TIA-RESBB C! ;
: TIA-HMOVE@ ( -- n ) TIA-HMOVE C@ ;
: TIA-HMOVE! ( n -- ) TIA-HMOVE C! ;
: TIA-HMCLR@ ( -- n ) TIA-HMCLR C@ ;
: TIA-HMCLR! ( n -- ) TIA-HMCLR C! ;
: TIA-CXM0P@ ( -- n ) TIA-CXM0P C@ ;
: TIA-CXM0P! ( n -- ) TIA-CXM0P C! ;
: TIA-CXM1P@ ( -- n ) TIA-CXM1P C@ ;
: TIA-CXM1P! ( n -- ) TIA-CXM1P C! ;
: TIA-CXP0FB@ ( -- n ) TIA-CXP0FB C@ ;
: TIA-CXP0FB! ( n -- ) TIA-CXP0FB C! ;
: TIA-CXP1FB@ ( -- n ) TIA-CXP1FB C@ ;
: TIA-CXP1FB! ( n -- ) TIA-CXP1FB C! ;
: TIA-CXM0FB@ ( -- n ) TIA-CXM0FB C@ ;
: TIA-CXM0FB! ( n -- ) TIA-CXM0FB C! ;
: TIA-CXM1FB@ ( -- n ) TIA-CXM1FB C@ ;
: TIA-CXM1FB! ( n -- ) TIA-CXM1FB C! ;
: TIA-CXBLPF@ ( -- n ) TIA-CXBLPF C@ ;
: TIA-CXBLPF! ( n -- ) TIA-CXBLPF C! ;
: TIA-CXPPMM@ ( -- n ) TIA-CXPPMM C@ ;
: TIA-CXPPMM! ( n -- ) TIA-CXPPMM C! ;
: TIA-INPT0@ ( -- n ) TIA-INPT0 C@ ;
: TIA-INPT0! ( n -- ) TIA-INPT0 C! ;
: TIA-INPT1@ ( -- n ) TIA-INPT1 C@ ;
: TIA-INPT1! ( n -- ) TIA-INPT1 C! ;
: TIA-INPT2@ ( -- n ) TIA-INPT2 C@ ;
: TIA-INPT2! ( n -- ) TIA-INPT2 C! ;
: TIA-INPT3@ ( -- n ) TIA-INPT3 C@ ;
: TIA-INPT3! ( n -- ) TIA-INPT3 C! ;
: TIA-INPT4@ ( -- n ) TIA-INPT4 C@ ;
: TIA-INPT4! ( n -- ) TIA-INPT4 C! ;
: TIA-INPT5@ ( -- n ) TIA-INPT5 C@ ;
: TIA-INPT5! ( n -- ) TIA-INPT5 C! ;

\ RIOT外设
: RIOT-SWCHA@ ( -- n ) RIOT-SWCHA C@ ;
: RIOT-SWCHA! ( n -- ) RIOT-SWCHA C! ;
: RIOT-SWACNT@ ( -- n ) RIOT-SWACNT C@ ;
: RIOT-SWACNT! ( n -- ) RIOT-SWACNT C! ;
: RIOT-SWCHB@ ( -- n ) RIOT-SWCHB C@ ;
: RIOT-SWCHB! ( n -- ) RIOT-SWCHB C! ;
: RIOT-SWCHB-RESET@ ( -- flag ) RIOT-SWCHB@ 1 BIT@ ;
: RIOT-SWCHB-RESET! ( flag -- ) RIOT-SWCHB@ 1 BIT! RIOT-SWCHB! ;
: RIOT-SWCHB-SELECT@ ( -- flag ) RIOT-SWCHB@ 2 BIT@ ;
: RIOT-SWCHB-SELECT! ( flag -- ) RIOT-SWCHB@ 2 BIT! RIOT-SWCHB! ;
: RIOT-SWCHB-DIFFB@ ( -- flag ) RIOT-SWCHB@ 3 BIT@ ;
: RIOT-SWCHB-DIFFB! ( flag -- ) RIOT-SWCHB@ 3 BIT! RIOT-SWCHB! ;
: RIOT-SWCHB-DIFFA@ ( -- flag ) RIOT-SWCHB@ 4 BIT@ ;
: RIOT-SWCHB-DIFFA! ( flag -- ) RIOT-SWCHB@ 4 BIT! RIOT-SWCHB! ;
: RIOT-SWBCNT@ ( -- n ) RIOT-SWBCNT C@ ;
: RIOT-SWBCNT! ( n -- ) RIOT-SWBCNT C! ;
: RIOT-INTIM@ ( -- n ) RIOT-INTIM C@ ;
: RIOT-INTIM! ( n -- ) RIOT-INTIM C! ;
: RIOT-TIMINT@ ( -- n ) RIOT-TIMINT C@ ;
: RIOT-TIMINT! ( n -- ) RIOT-TIMINT C! ;
: RIOT-TIM1T@ ( -- n ) RIOT-TIM1T C@ ;
: RIOT-TIM1T! ( n -- ) RIOT-TIM1T C! ;
: RIOT-TIM8T@ ( -- n ) RIOT-TIM8T C@ ;
: RIOT-TIM8T! ( n -- ) RIOT-TIM8T C! ;
: RIOT-TIM64T@ ( -- n ) RIOT-TIM64T C@ ;
: RIOT-TIM64T! ( n -- ) RIOT-TIM64T C! ;
: RIOT-TIM1024T@ ( -- n ) RIOT-TIM1024T C@ ;
: RIOT-TIM1024T! ( n -- ) RIOT-TIM1024T C! ;

\ CONTROLLER1外设
: CONTROLLER1-SWCHA@ ( -- n ) CONTROLLER1-SWCHA C@ ;
: CONTROLLER1-SWCHA! ( n -- ) CONTROLLER1-SWCHA C! ;

\ CONTROLLER2外设
: CONTROLLER2-SWCHA@ ( -- n ) CONTROLLER2-SWCHA C@ ;
: CONTROLLER2-SWCHA! ( n -- ) CONTROLLER2-SWCHA C! ;

\ =========================================
\ 设备初始化
\ =========================================

: MOS_6507-INIT ( -- )
  \ 初始化MOS-6507设备
  ." 初始化MOS-6507..." CR

  \ 初始化寄存器
  0 A!  \ Accumulator
  0 X!  \ X Index
  0 Y!  \ Y Index
  0 SP!  \ Stack Pointer (6-bit, 128-byte stack)
  0 PC!  \ Program Counter (16-bit)
  0 P!  \ Processor Status

  \ 初始化外设
  \ 初始化TIA
  0 TIA-VSYNC!  \ VSYNC寄存器
  0 TIA-VBLANK!  \ VBLANK寄存器
  0 TIA-WSYNC!  \ WSYNC寄存器
  0 TIA-RSYNC!  \ RSYNC寄存器
  0 TIA-NUSIZ0!  \ NUSIZ0寄存器
  0 TIA-NUSIZ1!  \ NUSIZ1寄存器
  0 TIA-COLUP0!  \ COLUP0寄存器
  0 TIA-COLUP1!  \ COLUP1寄存器
  0 TIA-COLUPF!  \ COLUPF寄存器
  0 TIA-COLUBK!  \ COLUBK寄存器
  0 TIA-CTRLPF!  \ CTRLPF寄存器
  0 TIA-REFPL!  \ REFPL寄存器
  0 TIA-PF0!  \ PF0寄存器
  0 TIA-PF1!  \ PF1寄存器
  0 TIA-PF2!  \ PF2寄存器
  0 TIA-RESP0!  \ RESP0寄存器
  0 TIA-RESP1!  \ RESP1寄存器
  0 TIA-RESM0!  \ RESM0寄存器
  0 TIA-RESM1!  \ RESM1寄存器
  0 TIA-RESBL!  \ RESBL寄存器
  0 TIA-AUDC0!  \ AUDC0寄存器
  0 TIA-AUDC1!  \ AUDC1寄存器
  0 TIA-AUDF0!  \ AUDF0寄存器
  0 TIA-AUDF1!  \ AUDF1寄存器
  0 TIA-AUDV0!  \ AUDV0寄存器
  0 TIA-AUDV1!  \ AUDV1寄存器
  0 TIA-GRP0!  \ GRP0寄存器
  0 TIA-GRP1!  \ GRP1寄存器
  0 TIA-DGRP0!  \ DGRP0寄存器
  0 TIA-DGRP1!  \ DGRP1寄存器
  0 TIA-ENAM0!  \ ENAM0寄存器
  0 TIA-ENAM1!  \ ENAM1寄存器
  0 TIA-ENABL!  \ ENABL寄存器
  0 TIA-HMP0!  \ HMP0寄存器
  0 TIA-HMP1!  \ HMP1寄存器
  0 TIA-HMM0!  \ HMM0寄存器
  0 TIA-HMM1!  \ HMM1寄存器
  0 TIA-HMBL!  \ HMBL寄存器
  0 TIA-VDEL0!  \ VDEL0寄存器
  0 TIA-VDEL1!  \ VDEL1寄存器
  0 TIA-VDELBL!  \ VDELBL寄存器
  0 TIA-RESBB!  \ RESBB寄存器
  0 TIA-HMOVE!  \ HMOVE寄存器
  0 TIA-HMCLR!  \ HMCLR寄存器
  0 TIA-CXM0P!  \ CXM0P寄存器
  0 TIA-CXM1P!  \ CXM1P寄存器
  0 TIA-CXP0FB!  \ CXP0FB寄存器
  0 TIA-CXP1FB!  \ CXP1FB寄存器
  0 TIA-CXM0FB!  \ CXM0FB寄存器
  0 TIA-CXM1FB!  \ CXM1FB寄存器
  0 TIA-CXBLPF!  \ CXBLPF寄存器
  0 TIA-CXPPMM!  \ CXPPMM寄存器
  0 TIA-INPT0!  \ INPT0寄存器
  0 TIA-INPT1!  \ INPT1寄存器
  0 TIA-INPT2!  \ INPT2寄存器
  0 TIA-INPT3!  \ INPT3寄存器
  0 TIA-INPT4!  \ INPT4寄存器
  0 TIA-INPT5!  \ INPT5寄存器
  \ 初始化RIOT
  0 RIOT-SWCHA!  \ SWCHA寄存器
  0 RIOT-SWACNT!  \ SWACNT寄存器
  0 RIOT-SWCHB!  \ SWCHB寄存器
  0 RIOT-SWBCNT!  \ SWBCNT寄存器
  0 RIOT-INTIM!  \ INTIM寄存器
  0 RIOT-TIMINT!  \ TIMINT寄存器
  0 RIOT-TIM1T!  \ TIM1T寄存器
  0 RIOT-TIM8T!  \ TIM8T寄存器
  0 RIOT-TIM64T!  \ TIM64T寄存器
  0 RIOT-TIM1024T!  \ TIM1024T寄存器
  \ 初始化CONTROLLER1
  0 CONTROLLER1-SWCHA!  \ SWCHA寄存器
  \ 初始化CONTROLLER2
  0 CONTROLLER2-SWCHA!  \ SWCHA寄存器

  ." MOS-6507初始化完成" CR
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

\ Power-On Reset
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

\ =========================================
\ 示例程序
\ =========================================

: EXAMPLE ( -- )
  MOS_6507-INIT
  .DEVICE-INFO
  .REGISTERS
  CR ." 示例程序运行完成" CR
;

\ 自动运行示例
( EXAMPLE )

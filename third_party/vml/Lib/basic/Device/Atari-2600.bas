' MOS-6507寄存器定义
' 生成自: MOS Technology/MOS-6502/MOS-6507
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: Atari 2600 VCS main processor - MOS 6507 (simplified 6502) @ 1.19MHz with TIA and RIOT

' CPU架构: MOS-6507
' 位宽: 8位
' 时钟频率: 1190000 Hz

' 寄存器定义
' Accumulator
CONST A = 0x00

' X Index
CONST X = 0x01

' Y Index
CONST Y = 0x02

' Stack Pointer (6-bit, 128-byte stack)
CONST SP = 0x03

' Program Counter (16-bit)
CONST PC = 0x04

' Processor Status
CONST P = 0x06
CONST P_N = 7  ' Negative
CONST P_V = 6  ' Overflow
CONST P_B = 4  ' Break
CONST P_D = 3  ' Decimal Mode (N/A on 6507)
CONST P_I = 2  ' Interrupt Disable
CONST P_Z = 1  ' Zero
CONST P_C = 0  ' Carry

' 内存段定义
' TIA Registers
CONST TIA_REGS_START = 0x0000
CONST TIA_REGS_END = 0x007F
CONST TIA_REGS_SIZE = 128

' RIOT 128byte RAM mirrored
CONST RIOT_RAM_START = 0x0080
CONST RIOT_RAM_END = 0x00FF
CONST RIOT_RAM_SIZE = 128

' RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
CONST RIOT_IO_START = 0x0280
CONST RIOT_IO_END = 0x029F
CONST RIOT_IO_SIZE = 32

' Cartridge ROM (4KB, bank-switched)
CONST CART_ROM_START = 0x1000
CONST CART_ROM_END = 0x1FFF
CONST CART_ROM_SIZE = 4096

' 外设定义
' Television Interface Adaptor (Video + Audio + I/O)
CONST TIA_BASE = 0x0000
CONST TIA_VSYNC = 0x00
CONST TIA_VBLANK = 0x01
CONST TIA_VBLANK_D7 = 7  ' Inhibit D7 (1=disable D7 output to PB7)
CONST TIA_VBLANK_D6 = 6  ' Inhibit D6 (1=disable D6 output to PB6)
CONST TIA_VBLANK_D5 = 5  ' Inhibit D5 (1=disable D5 output to PB5)
CONST TIA_VBLANK_D4 = 4  ' Inhibit D4 (1=disable D4 output to PB4)
CONST TIA_VBLANK_D3 = 3  ' Inhibit D3 (1=disable D3 output to PB3)
CONST TIA_VBLANK_D2 = 2  ' Inhibit D2 (1=disable D2 output to PB2)
CONST TIA_VBLANK_D1 = 1  ' Inhibit D1 (1=disable D1 output to PB1)
CONST TIA_VBLANK_D0 = 0  ' Inhibit D0 (1=disable D0 output to PB0)
CONST TIA_VBLANK_VBW = 5  ' Vertical Blank Enable (1=set VBLANK)
CONST TIA_VBLANK_VBL = 1  ' Vertical Blank Set (1=V-Blank active)
CONST TIA_VBLANK_RESBL = 0  ' Reset Blank (1=allow VSYNC/VBLANK reset on clock)
CONST TIA_WSYNC = 0x02
CONST TIA_RSYNC = 0x03
CONST TIA_NUSIZ0 = 0x04
CONST TIA_NUSIZ0_NUSIZ = 0  ' Number/Size Code (0-7)
CONST TIA_NUSIZ0_MISSILE_SIZE = 0  ' Missile Size
CONST TIA_NUSIZ0_RESM0 = 6  ' Reset M0
CONST TIA_NUSIZ0_RESM1 = 7  ' Reset M1
CONST TIA_NUSIZ1 = 0x05
CONST TIA_COLUP0 = 0x06
CONST TIA_COLUP1 = 0x07
CONST TIA_COLUPF = 0x08
CONST TIA_COLUBK = 0x09
CONST TIA_CTRLPF = 0x0A
CONST TIA_CTRLPF_DELL = 0  ' Delay Playfield L (Reflected/Left score)
CONST TIA_CTRLPF_BALL_SIZE = 0  ' Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
CONST TIA_CTRLPF_REF = 5  ' Reflect (1=mirror playfield)
CONST TIA_CTRLPF_SCORE = 6  ' Score Mode (1=use player colors for L/R halves)
CONST TIA_CTRLPF_DELBL = 7  ' Delay Ball (1=delay ball 1 clock)
CONST TIA_REFPL = 0x0B
CONST TIA_PF0 = 0x0D
CONST TIA_PF1 = 0x0E
CONST TIA_PF2 = 0x0F
CONST TIA_RESP0 = 0x10
CONST TIA_RESP1 = 0x11
CONST TIA_RESM0 = 0x12
CONST TIA_RESM1 = 0x13
CONST TIA_RESBL = 0x14
CONST TIA_AUDC0 = 0x15
CONST TIA_AUDC0_VOL = 0  ' Volume (0-15)
CONST TIA_AUDC0_TONE = 0  ' Tone Divisor (5-bit counter)
CONST TIA_AUDC1 = 0x16
CONST TIA_AUDF0 = 0x17
CONST TIA_AUDF1 = 0x18
CONST TIA_AUDV0 = 0x19
CONST TIA_AUDV1 = 0x1A
CONST TIA_GRP0 = 0x1B
CONST TIA_GRP1 = 0x1C
CONST TIA_DGRP0 = 0x1D
CONST TIA_DGRP1 = 0x1E
CONST TIA_ENAM0 = 0x1F
CONST TIA_ENAM1 = 0x20
CONST TIA_ENABL = 0x21
CONST TIA_HMP0 = 0x22
CONST TIA_HMP1 = 0x23
CONST TIA_HMM0 = 0x24
CONST TIA_HMM1 = 0x25
CONST TIA_HMBL = 0x26
CONST TIA_VDEL0 = 0x27
CONST TIA_VDEL1 = 0x28
CONST TIA_VDELBL = 0x29
CONST TIA_RESBB = 0x2A
CONST TIA_HMOVE = 0x2A
CONST TIA_HMCLR = 0x2B
CONST TIA_CXM0P = 0x30
CONST TIA_CXM1P = 0x31
CONST TIA_CXP0FB = 0x32
CONST TIA_CXP1FB = 0x33
CONST TIA_CXM0FB = 0x34
CONST TIA_CXM1FB = 0x35
CONST TIA_CXBLPF = 0x36
CONST TIA_CXPPMM = 0x37
CONST TIA_INPT0 = 0x38
CONST TIA_INPT1 = 0x39
CONST TIA_INPT2 = 0x3A
CONST TIA_INPT3 = 0x3B
CONST TIA_INPT4 = 0x3C
CONST TIA_INPT5 = 0x3D

' RAM, I/O, Timer (6532 RIOT)
CONST RIOT_BASE = 0x0080
CONST RIOT_SWCHA = 0x280
CONST RIOT_SWACNT = 0x281
CONST RIOT_SWCHB = 0x282
CONST RIOT_SWCHB_RESET = 1  ' Game Reset Switch (0=pressed)
CONST RIOT_SWCHB_SELECT = 2  ' Game Select Switch (0=pressed)
CONST RIOT_SWCHB_DIFFB = 3  ' Difficulty B (0=hard, 1=easy)
CONST RIOT_SWCHB_DIFFA = 4  ' Difficulty A (0=hard, 1=easy)
CONST RIOT_SWBCNT = 0x283
CONST RIOT_INTIM = 0x284
CONST RIOT_TIMINT = 0x285
CONST RIOT_TIM1T = 0x294
CONST RIOT_TIM8T = 0x295
CONST RIOT_TIM64T = 0x296
CONST RIOT_TIM1024T = 0x297

' Controller Port 1 (Joystick)
CONST CONTROLLER1_BASE = 0x280
CONST CONTROLLER1_SWCHA = 0x280

' Controller Port 2 (Joystick)
CONST CONTROLLER2_BASE = 0x281
CONST CONTROLLER2_SWCHA = 0x280

' 中断向量定义
CONST RESET_VECTOR = 0  ' Power-On Reset

' 引脚定义
CONST PIN_VSS = 1  ' Ground
CONST PIN_VCC = 2  ' Power Supply
CONST PIN_PHI0 = 3  ' Clock Input (1.19MHz NTSC / 1.18MHz PAL)
CONST PIN_RESET = 4  ' Reset (active low)
CONST PIN_A0 = 5  ' Address Bus Bit 0
CONST PIN_A1 = 6  ' Address Bus Bit 1
CONST PIN_A2 = 7  ' Address Bus Bit 2
CONST PIN_A3 = 8  ' Address Bus Bit 3
CONST PIN_A4 = 9  ' Address Bus Bit 4
CONST PIN_A5 = 10  ' Address Bus Bit 5
CONST PIN_A6 = 11  ' Address Bus Bit 6
CONST PIN_A7 = 12  ' Address Bus Bit 7
CONST PIN_A8 = 13  ' Address Bus Bit 8
CONST PIN_A9 = 14  ' Address Bus Bit 9
CONST PIN_A10 = 15  ' Address Bus Bit 10
CONST PIN_A11 = 16  ' Address Bus Bit 11
CONST PIN_A12 = 17  ' Address Bus Bit 12
CONST PIN_D0 = 18  ' Data Bus Bit 0
CONST PIN_D1 = 19  ' Data Bus Bit 1
CONST PIN_D2 = 20  ' Data Bus Bit 2
CONST PIN_D3 = 21  ' Data Bus Bit 3
CONST PIN_D4 = 22  ' Data Bus Bit 4
CONST PIN_D5 = 23  ' Data Bus Bit 5
CONST PIN_D6 = 24  ' Data Bus Bit 6
CONST PIN_D7 = 25  ' Data Bus Bit 7
CONST PIN_RDY = 26  ' Ready (stops CPU on read)
CONST PIN_R_W = 27  ' Read/Write (1=Read, 0=Write)
CONST PIN_NC = 28  ' Not Connected

' 设备初始化子程序
SUB mos_6507_init()
    ' 初始化代码
END SUB

' 常用函数
FUNCTION read_register(addr AS INTEGER) AS INTEGER
    ' 读取寄存器值
    RETURN PEEK(addr)
END FUNCTION

SUB write_register(addr AS INTEGER, value AS INTEGER)
    ' 写入寄存器值
    POKE addr, value
END SUB

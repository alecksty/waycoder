' Macintosh-128K寄存器定义
' 生成自: Apple Computer/Macintosh/Macintosh-128K
' 版本: 1.0
' 日期: 2026-04-17
' 作者: VML Team
' 描述: Original Macintosh 128K with Motorola 68000 CPU, 128KB RAM, and 9-inch monochrome display

' CPU架构: Motorola 68000
' 位宽: 32位
' 时钟频率: 7998000 Hz

' 寄存器定义
' Data Register 0
CONST D0 = 0

' Data Register 1
CONST D1 = 0

' Data Register 2
CONST D2 = 0

' Data Register 3
CONST D3 = 0

' Data Register 4
CONST D4 = 0

' Data Register 5
CONST D5 = 0

' Data Register 6
CONST D6 = 0

' Data Register 7
CONST D7 = 0

' Address Register 0
CONST A0 = 0

' Address Register 1
CONST A1 = 0

' Address Register 2
CONST A2 = 0

' Address Register 3
CONST A3 = 0

' Address Register 4
CONST A4 = 0

' Address Register 5
CONST A5 = 0

' Address Register 6
CONST A6 = 0

' Address Register 7 (SP)
CONST A7 = 0

' Program Counter
CONST PC = 0

' Status Register
CONST SR = 0

' 外设定义
' Versatile Interface Adapter (6522)
CONST VIA_BASE = 
CONST VIA_VIA_ORB = 0xE80000
CONST VIA_VIA_ORA = 0xE80001
CONST VIA_VIA_DDRB = 0xE80002
CONST VIA_VIA_DDRA = 0xE80003
CONST VIA_VIA_T1CL = 0xE80004
CONST VIA_VIA_T1CH = 0xE80005
CONST VIA_VIA_T1LL = 0xE80006
CONST VIA_VIA_T1LH = 0xE80007
CONST VIA_VIA_T2CL = 0xE80008
CONST VIA_VIA_T2CH = 0xE80009
CONST VIA_VIA_SR = 0xE8000A
CONST VIA_VIA_ACR = 0xE8000B
CONST VIA_VIA_PCR = 0xE8000C
CONST VIA_VIA_IFR = 0xE8000D
CONST VIA_VIA_IER = 0xE8000E
CONST VIA_VIA_ORA2 = 0xE8000F

' Integrated Woz Machine (floppy controller)
CONST IWM_BASE = 
CONST IWM_IWM_Q6 = 0xD00000
CONST IWM_IWM_Q7 = 0xD00002
CONST IWM_IWM_PH0 = 0xD00004
CONST IWM_IWM_PH1 = 0xD00006
CONST IWM_IWM_PH2 = 0xD00008
CONST IWM_IWM_PH3 = 0xD0000A

' Zilog 8530 Serial Communications Controller
CONST SCC_BASE = 
CONST SCC_SCC_CA = 0x500000
CONST SCC_SCC_DA = 0x500002
CONST SCC_SCC_CB = 0x500004
CONST SCC_SCC_DB = 0x500006

' Built-in speaker
CONST SOUND_BASE = 
CONST SOUND_SOUND_VOL = 0xE80100
CONST SOUND_SOUND_FREQ = 0xE80102

' 中断向量定义
CONST RESET_SP_VECTOR = 0  ' Reset (Initial SP)
CONST RESET_PC_VECTOR = 4  ' Reset (Initial PC)
CONST AUTOVECTOR1_VECTOR = 24  ' Auto vector 1
CONST AUTOVECTOR2_VECTOR = 25  ' Auto vector 2
CONST AUTOVECTOR3_VECTOR = 26  ' Auto vector 3
CONST AUTOVECTOR4_VECTOR = 27  ' Auto vector 4
CONST AUTOVECTOR5_VECTOR = 28  ' Auto vector 5
CONST AUTOVECTOR6_VECTOR = 29  ' Auto vector 6
CONST AUTOVECTOR7_VECTOR = 30  ' Auto vector 7
CONST SPURIOUS_VECTOR = 31  ' Spurious interrupt

' 设备初始化子程序
SUB macintosh_128k_init()
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

' Apple-II寄存器定义
' 生成自: Apple Computer/Apple II/Apple-II
' 版本: 1.0
' 日期: 2026-04-17
' 作者: VML Team
' 描述: Apple II personal computer with MOS 6502 CPU, 48KB RAM, and color graphics

' CPU架构: MOS 6502
' 位宽: 8位
' 时钟频率: 1023000 Hz

' 寄存器定义
' Accumulator
CONST A = 0

' Index Register X
CONST X = 0

' Index Register Y
CONST Y = 0

' Stack Pointer
CONST SP = 0

' Program Counter
CONST PC = 0

' Status Register
CONST P = 0

' 外设定义
' Apple II keyboard
CONST KEYBOARD_BASE = 
CONST KEYBOARD_KBD = 0xC000
CONST KEYBOARD_KBDSTRB = 0xC010

' Built-in speaker
CONST SPEAKER_BASE = 
CONST SPEAKER_SPKR = 0xC030

' Cassette tape interface
CONST CASSETTE_BASE = 
CONST CASSETTE_TAPEIN = 0xC060
CONST CASSETTE_TAPEOUT = 0xC020

' Game controller port
CONST GAMEPORT_BASE = 
CONST GAMEPORT_PADDLE0 = 0xC064
CONST GAMEPORT_PADDLE1 = 0xC065
CONST GAMEPORT_PADDLE2 = 0xC066
CONST GAMEPORT_PADDLE3 = 0xC067
CONST GAMEPORT_BUTTON0 = 0xC061
CONST GAMEPORT_BUTTON1 = 0xC062

' Disk II controller
CONST DISKCONTROLLER_BASE = 
CONST DISKCONTROLLER_DISKUNIT = 0xC0E0
CONST DISKCONTROLLER_DISKCMD = 0xC0E8
CONST DISKCONTROLLER_DISKSTAT = 0xC0E9
CONST DISKCONTROLLER_DISKDATA = 0xC0EA

' 中断向量定义
CONST NMI_VECTOR = 65526  ' Non-maskable interrupt
CONST RESET_VECTOR = 65528  ' Reset vector
CONST IRQ_VECTOR = 65530  ' Interrupt request
CONST BRK_VECTOR = 65532  ' Break instruction

' 设备初始化子程序
SUB apple_ii_init()
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

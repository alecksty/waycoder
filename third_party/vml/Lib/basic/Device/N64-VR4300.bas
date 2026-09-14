' NEC-VR4300寄存器定义
' 生成自: NEC/MIPS-R4000/NEC-VR4300
' 版本: 1.0
' 日期: 2026-04-16
' 作者: VML Team
' 描述: Nintendo 64 main processor - NEC VR4300 (MIPS R4300i-compatible) @ 93.75MHz, 64-bit R4000-like

' CPU架构: MIPS-R4300i
' 位宽: 64位
' 时钟频率: 93750000 Hz

' 寄存器定义
' Hard-wired Zero
CONST R0 = 0x00

' Assembler Temporary
CONST R1 = 0x08

' Value Return
CONST R2 = 0x10

' Expression Evaluation
CONST R3 = 0x18

' Expression Evaluation
CONST R4 = 0x20

' Expression Evaluation
CONST R5 = 0x28

' Expression Evaluation
CONST R6 = 0x30

' Expression Evaluation
CONST R7 = 0x38

' Expression Evaluation
CONST R8 = 0x40

' Expression Evaluation
CONST R9 = 0x48

' Expression Evaluation
CONST R10 = 0x50

' Expression Evaluation
CONST R11 = 0x58

' Expression Evaluation
CONST R12 = 0x60

' Expression Evaluation
CONST R13 = 0x68

' Expression Evaluation
CONST R14 = 0x70

' Expression Evaluation
CONST R15 = 0x78

' Saved Value
CONST R16 = 0x80

' Saved Value
CONST R17 = 0x88

' Saved Value
CONST R18 = 0x90

' Saved Value
CONST R19 = 0x98

' Saved Value
CONST R20 = 0xA0

' Saved Value
CONST R21 = 0xA8

' Saved Value
CONST R22 = 0xB0

' Saved Value
CONST R23 = 0xB8

' Temporary
CONST R24 = 0xC0

' Temporary
CONST R25 = 0xC8

' Kernel Reserved
CONST R26 = 0xD0

' Kernel Reserved
CONST R27 = 0xD8

' Global Pointer
CONST R28 = 0xE0

' Stack Pointer
CONST R29 = 0xE8

' Frame Pointer
CONST R30 = 0xF0

' Return Address
CONST R31 = 0xF8

' Multiply/Divide High (64-bit)
CONST HI = 0x100

' Multiply/Divide Low (64-bit)
CONST LO = 0x108

' Program Counter
CONST PC = 0x110

' LLAddr / LLBit (for LL/SC)
CONST LLB = 0x118

' TLB Index
CONST CP0_INDEX = 0x200

' TLB Random
CONST CP0_RANDOM = 0x208

' TLB EntryLo 0 (even page)
CONST CP0_ENTRYLO0 = 0x210

' TLB EntryLo 1 (odd page)
CONST CP0_ENTRYLO1 = 0x218

' Context Register (PTE base)
CONST CP0_CONTEXT = 0x220

' Page Mask (variable page size)
CONST CP0_PAGEMASK = 0x228

' TLB Wired
CONST CP0_WIRED = 0x230

' Bad Virtual Address
CONST CP0_BADVADDR = 0x238

' Count (incrementing timer)
CONST CP0_COUNT = 0x240

' TLB EntryHi (VPN2 + ASID)
CONST CP0_ENTRYHI = 0x250

' Compare (timer interrupt)
CONST CP0_COMPARE = 0x258

' Status Register
CONST CP0_STATUS = 0x260
CONST CP0_STATUS_IE = 0  ' Interrupt Enable
CONST CP0_STATUS_EXL = 1  ' Exception Level
CONST CP0_STATUS_ERL = 2  ' Error Level
CONST CP0_STATUS_KSU = 0  ' Kernel/User Mode
CONST CP0_STATUS_UX = 5  ' User Mode 64-bit (1=64-bit user)
CONST CP0_STATUS_SX = 6  ' Supervisor Mode 64-bit
CONST CP0_STATUS_KX = 7  ' Kernel Mode 64-bit
CONST CP0_STATUS_IM0_7 = 0  ' Interrupt Mask
CONST CP0_STATUS_CU0 = 28  ' Coprocessor 0 Usable
CONST CP0_STATUS_BEV = 22  ' Bootstrap Exception Vector
CONST CP0_STATUS_TS = 21  ' TLB Shutdown
CONST CP0_STATUS_FR = 26  ' Floating-Point Register Mode (32 double)

' Cause Register
CONST CP0_CAUSE = 0x268
CONST CP0_CAUSE_EXCCODE = 0  ' Exception Code
CONST CP0_CAUSE_IP0_7 = 0  ' Interrupt Pending
CONST CP0_CAUSE_BD = 31  ' Branch Delay Slot
CONST CP0_CAUSE_CE = 0  ' Coprocessor Error

' Exception PC
CONST CP0_EPC = 0x270

' Config Register
CONST CP0_CONFIG = 0x280

' Load Linked Address
CONST CP0_LLADDR = 0x288

' WatchLo (data/instruction break)
CONST CP0_WATCHLO = 0x290

' WatchHi
CONST CP0_WATCHHI = 0x298

' Extended Context
CONST CP0_XCONTEXT = 0x2A0

' Tag/Process ID
CONST CP0_PID = 0x2B0

' Debug Register
CONST CP0_DEBUG = 0x2D8

' Performance Counter
CONST CP0_PERF = 0x2F0

' 内存段定义
' RDRAM (4MB base, up to 8MB)
CONST RDRAM_START = 0x00000000
CONST RDRAM_END = 0x003FFFFF
CONST RDRAM_SIZE = 4194304

' RDRAM Registers
CONST RDRAM_REG_START = 0x18000000
CONST RDRAM_REG_END = 0x18000FFF
CONST RDRAM_REG_SIZE = 4096

' RCP SP Memory / DMEM (2KB)
CONST SP_MEM_START = 0x1FC00000
CONST SP_MEM_END = 0x1FC007FF
CONST SP_MEM_SIZE = 2048

' RCP SP Instruction Memory / IMEM (2KB)
CONST SP_IMEM_START = 0x1FC00800
CONST SP_IMEM_END = 0x1FC00FFF
CONST SP_IMEM_SIZE = 2048

' RCP Register Area
CONST RCP_REGS_START = 0x1FC00000
CONST RCP_REGS_END = 0x1FC3FFFF
CONST RCP_REGS_SIZE = 262144

' PI (Peripheral Interface) Registers
CONST PI_REGS_START = 0x1FC00000
CONST PI_REGS_END = 0x1FC007FF
CONST PI_REGS_SIZE = 2048

' VI (Video Interface) Registers
CONST VI_REGS_START = 0x1FC002C0
CONST VI_REGS_END = 0x1FC002FF
CONST VI_REGS_SIZE = 64

' AI (Audio Interface) Registers
CONST AI_REGS_START = 0x1FC00500
CONST AI_REGS_END = 0x1FC0053F
CONST AI_REGS_SIZE = 64

' SI (Serial Interface) Registers
CONST SI_REGS_START = 0x1FC004C0
CONST SI_REGS_END = 0x1FC004FF
CONST SI_REGS_SIZE = 64

' PI Bus DRAM (cartridge)
CONST PI_DRAM_START = 0xA0000000
CONST PI_DRAM_END = 0xA4000000
CONST PI_DRAM_SIZE = 67108864

' Cartridge ROM (up to 256MB)
CONST CART_ROM_START = 0xB0000000
CONST CART_ROM_END = 0xBFFFFFFF
CONST CART_ROM_SIZE = 268435456

' PIF-NUS ROM/RAM (CIC)
CONST PIF_RAM_START = 0x1FC007C0
CONST PIF_RAM_END = 0x1FC007FF
CONST PIF_RAM_SIZE = 64

' 外设定义
' Reality Signal Processor (Audio/Video microcode engine)
CONST RSP_BASE = 0x04040000
CONST RSP_SP_MEM_ADDR = 0x00
CONST RSP_SP_DRAM_ADDR = 0x04
CONST RSP_SP_RD_LEN = 0x08
CONST RSP_SP_WR_LEN = 0x0C
CONST RSP_SP_STATUS = 0x10
CONST RSP_SP_STATUS_BROKE = 0  ' Command Queue Broke
CONST RSP_SP_STATUS_SLEEP = 2  ' SP Sleep
CONST RSP_SP_STATUS_GOODMATCH = 3  ' DMEM/IMEM Goodmatch
CONST RSP_SP_STATUS_SSTEP = 4  ' Single Step
CONST RSP_SP_STATUS_INTSIG = 5  ' Interrupt Signal
CONST RSP_SP_STATUS_HALT = 6  ' Halt
CONST RSP_SP_STATUS_CLEAR = 7  ' Clear SP Status
CONST RSP_SP_STATUS_INTR_BRK = 8  ' IntrOnBreak
CONST RSP_SP_STATUS_SIGNAL0 = 12  ' Software Signal 0
CONST RSP_SP_STATUS_SIGNAL1 = 13  ' Software Signal 1
CONST RSP_SP_STATUS_SIGNAL2 = 14  ' Software Signal 2
CONST RSP_SP_STATUS_SIGNAL3 = 15  ' Software Signal 3
CONST RSP_SP_STATUS_SIGNAL4 = 16  ' Software Signal 4
CONST RSP_SP_STATUS_SIGNAL5 = 17  ' Software Signal 5
CONST RSP_SP_STATUS_SIGNAL6 = 18  ' Software Signal 6
CONST RSP_SP_STATUS_SIGNAL7 = 19  ' Software Signal 7
CONST RSP_SP_DMA_FULL = 0x14
CONST RSP_SP_DMA_BUSY = 0x18
CONST RSP_SP_SEMAPHORE = 0x1C
CONST RSP_SP_PC = 0x20
CONST RSP_SP_IBIST = 0x24

' Reality Drawing Processor (Triangle/Quad rasterizer)
CONST RDP_BASE = 0x04100000
CONST RDP_DP_START = 0x00
CONST RDP_DP_END = 0x04
CONST RDP_DP_CURRENT = 0x08
CONST RDP_DP_STATUS = 0x0C
CONST RDP_DP_STATUS_TERMINATE = 0  ' Terminator
CONST RDP_DP_STATUS_PIPE_BUSY = 1  ' Pipeline Busy
CONST RDP_DP_STATUS_TOMINO_BUSY = 2  ' ToMini Busy
CONST RDP_DP_STATUS_PIPE_FLUSH = 3  ' Pipeline Flush
CONST RDP_DP_STATUS_TOMINO_FLUSH = 4  ' ToMini Flush
CONST RDP_DP_STATUS_FREEZE = 5  ' Freeze
CONST RDP_DP_STATUS_START_GCLK = 24  ' Start GCLK
CONST RDP_DP_CLOCK = 0x10
CONST RDP_DP_BUFBUSY = 0x14
CONST RDP_DP_PIPEBUSY = 0x18
CONST RDP_DP_TMEM = 0x1C

' Video Interface (scanout engine)
CONST VI_BASE = 0x04400000
CONST VI_VI_STATUS = 0x00
CONST VI_VI_STATUS_TYPE = 0  ' Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i)
CONST VI_VI_STATUS_DITHER_FILTER = 6  ' Dither Filter Enable
CONST VI_VI_STATUS_GAMMA = 7  ' Gamma Correction Enable
CONST VI_VI_STATUS_GAMMA_DITHER = 8  ' Gamma Dither Enable
CONST VI_VI_STATUS_DIVOT = 9  ' Divot Control
CONST VI_VI_STATUS_SERRATION = 10  '  Serration Enable (for interlaced)
CONST VI_VI_ORIGIN = 0x04
CONST VI_VI_WIDTH = 0x08
CONST VI_VI_V_INTR = 0x0C
CONST VI_VI_V_CURRENT = 0x10
CONST VI_VI_BURST = 0x14
CONST VI_VI_H_SYNC = 0x18
CONST VI_VI_H_SYNC_LEAP = 0x1C
CONST VI_VI_H_VIDEO = 0x20
CONST VI_VI_V_VIDEO = 0x24
CONST VI_VI_V_BURST = 0x28
CONST VI_VI_X_SCALE = 0x2C
CONST VI_VI_Y_SCALE = 0x30

' Audio Interface (DAC)
CONST AI_BASE = 0x04500000
CONST AI_AI_DRAM_ADDR = 0x00
CONST AI_AI_LEN = 0x04
CONST AI_AI_CONTROL = 0x08
CONST AI_AI_CONTROL_DMA_ENABLE = 0  ' DMA Enable
CONST AI_AI_CONTROL_DMA_FIFO_FULL = 1  ' DMA FIFO Full
CONST AI_AI_STATUS = 0x0C
CONST AI_AI_DACRATE = 0x10
CONST AI_AI_BITRATE = 0x14

' Peripheral Interface (cartridge bus)
CONST PI_BASE = 0x04600000
CONST PI_PI_DRAM_ADDR = 0x00
CONST PI_PI_CART_ADDR = 0x04
CONST PI_PI_RD_LEN = 0x08
CONST PI_PI_WR_LEN = 0x0C
CONST PI_PI_STATUS = 0x10
CONST PI_PI_STATUS_DMA_BUSY = 0  ' DMA Busy
CONST PI_PI_STATUS_IO_BUSY = 1  ' I/O Busy
CONST PI_PI_STATUS_ERROR = 2  ' Bus Error
CONST PI_PI_BSD_DOM1_LAT = 0x14
CONST PI_PI_BSD_DOM1_PWD = 0x18
CONST PI_PI_BSD_DOM1_PGS = 0x1C
CONST PI_PI_BSD_DOM1_RLS = 0x20
CONST PI_PI_BSD_DOM2_LAT = 0x24
CONST PI_PI_BSD_DOM2_PWD = 0x28
CONST PI_PI_BSD_DOM2_PGS = 0x2C
CONST PI_PI_BSD_DOM2_RLS = 0x30

' Serial Interface (Controller Pak / 64DD)
CONST SI_BASE = 0x04800000
CONST SI_SI_DRAM_ADDR = 0x00
CONST SI_SI_PIF_ADDR_RD64B = 0x04
CONST SI_SI_PIF_ADDR_WR64B = 0x08
CONST SI_SI_STATUS = 0x10
CONST SI_SI_STATUS_DMA_BUSY = 0  ' DMA Busy
CONST SI_SI_STATUS_IO_BUSY = 1  ' I/O Busy
CONST SI_SI_STATUS_INTERRUPT = 12  ' SI Interrupt

' PIF (CIC / NUSYC - anti-piracy/copy protection)
CONST PIF_BASE = 0x1FC007C0
CONST PIF_PIF_CMD0 = 0x00
CONST PIF_PIF_CMD1 = 0x01
CONST PIF_PIF_CMD2 = 0x02
CONST PIF_PIF_CMD3 = 0x03
CONST PIF_PIF_CMD4 = 0x04
CONST PIF_PIF_CMD5 = 0x05
CONST PIF_PIF_CMD6 = 0x06
CONST PIF_PIF_CMD7 = 0x07
CONST PIF_PIF_STATUS = 0x3F

' Interrupt Control
CONST INTERRUPT_BASE = 0x1FC00200
CONST INTERRUPT_MI_MODE = 0x00
CONST INTERRUPT_MI_MODE_INIT_MODE = 0  ' Initialize Mode
CONST INTERRUPT_MI_MODE_EBUS_TEST = 1  ' EBUS Test Mode
CONST INTERRUPT_MI_VERSION = 0x04
CONST INTERRUPT_MI_INTR = 0x08
CONST INTERRUPT_MI_INTR_SP = 0  ' SP Interrupt Pending
CONST INTERRUPT_MI_INTR_SI = 1  ' SI Interrupt Pending
CONST INTERRUPT_MI_INTR_AI = 2  ' AI Interrupt Pending
CONST INTERRUPT_MI_INTR_VI = 3  ' VI Interrupt Pending
CONST INTERRUPT_MI_INTR_PI = 4  ' PI Interrupt Pending
CONST INTERRUPT_MI_INTR_DP = 5  ' DP Interrupt Pending
CONST INTERRUPT_MI_INTR_MASK = 0x0C
CONST INTERRUPT_MI_INTR_MASK_SP_MASK = 0  ' SP Interrupt Mask
CONST INTERRUPT_MI_INTR_MASK_SI_MASK = 1  ' SI Interrupt Mask
CONST INTERRUPT_MI_INTR_MASK_AI_MASK = 2  ' AI Interrupt Mask
CONST INTERRUPT_MI_INTR_MASK_VI_MASK = 3  ' VI Interrupt Mask
CONST INTERRUPT_MI_INTR_MASK_PI_MASK = 4  ' PI Interrupt Mask
CONST INTERRUPT_MI_INTR_MASK_DP_MASK = 5  ' DP Interrupt Mask

' Controller Interface (SI channel 0-3)
CONST CONTROLLER_BASE = 0x1FC00600
CONST CONTROLLER_SI_CH0_DATA = 0x00
CONST CONTROLLER_SI_CH1_DATA = 0x08
CONST CONTROLLER_SI_CH2_DATA = 0x10
CONST CONTROLLER_SI_CH3_DATA = 0x18

' 中断向量定义
CONST RESET_VECTOR = 0  ' Soft Reset / NMI
CONST TLB_REFILL_VECTOR = 1  ' TLB Refill (I) / TLB Refill (D)
CONST CACHE_ERROR_VECTOR = 2  ' Cache Error
CONST GENERAL_EXCEPTION_VECTOR = 3  ' General Exception
CONST RSP_VECTOR = 4  ' RSP Interrupt (microcode signal)
CONST RDP_VECTOR = 5  ' RDP Interrupt (display list complete)
CONST VI_VECTOR = 6  ' VI Interrupt (V-Blank / scanline)
CONST AI_VECTOR = 7  ' AI Interrupt (audio DMA complete)
CONST PI_VECTOR = 8  ' PI Interrupt (cartridge DMA)
CONST SI_VECTOR = 9  ' SI Interrupt (serial interface)
CONST TIMER_COMPARE_VECTOR = 10  ' Timer Compare (CP0 Count == Compare)

' 引脚定义
CONST PIN_VCC = 1  ' Power Supply (3.3V)
CONST PIN_VSS = 2  ' Ground
CONST PIN_CLK = 3  ' System Clock (93.75MHz from CIC/PLL)
CONST PIN_RESET = 4  ' Reset (active low)
CONST PIN_NMI = 5  ' Non-Maskable Interrupt
CONST PIN_INT0 = 6  ' Interrupt 0 (RCP)
CONST PIN_INT1 = 7  ' Interrupt 1 (cartridge)
CONST PIN_INT2 = 8  ' Interrupt 2 (SI)
CONST PIN_INT3 = 9  ' Interrupt 3 (PIF)
CONST PIN_AB0 = 10  ' Address Bus Bit 0
CONST PIN_AB1 = 11  ' Address Bus Bit 1
CONST PIN_AB2 = 12  ' Address Bus Bit 2
CONST PIN_AB3 = 13  ' Address Bus Bit 3
CONST PIN_AB4 = 14  ' Address Bus Bit 4
CONST PIN_AB5 = 15  ' Address Bus Bit 5
CONST PIN_AB6 = 16  ' Address Bus Bit 6
CONST PIN_AB7 = 17  ' Address Bus Bit 7
CONST PIN_AB8 = 18  ' Address Bus Bit 8
CONST PIN_AB9 = 19  ' Address Bus Bit 9
CONST PIN_AB10 = 20  ' Address Bus Bit 10
CONST PIN_AB11 = 21  ' Address Bus Bit 11
CONST PIN_AB12 = 22  ' Address Bus Bit 12
CONST PIN_AB13 = 23  ' Address Bus Bit 13
CONST PIN_AB14 = 24  ' Address Bus Bit 14
CONST PIN_AB15 = 25  ' Address Bus Bit 15
CONST PIN_AB16 = 26  ' Address Bus Bit 16
CONST PIN_AB17 = 27  ' Address Bus Bit 17
CONST PIN_AB18 = 28  ' Address Bus Bit 18
CONST PIN_AB19 = 29  ' Address Bus Bit 19
CONST PIN_AB20 = 30  ' Address Bus Bit 20
CONST PIN_AB21 = 31  ' Address Bus Bit 21
CONST PIN_AB22 = 32  ' Address Bus Bit 22
CONST PIN_AB23 = 33  ' Address Bus Bit 23
CONST PIN_AB24 = 34  ' Address Bus Bit 24
CONST PIN_AB25 = 35  ' Address Bus Bit 25
CONST PIN_AB26 = 36  ' Address Bus Bit 26
CONST PIN_AB27 = 37  ' Address Bus Bit 27
CONST PIN_AB28 = 38  ' Address Bus Bit 28
CONST PIN_AB29 = 39  ' Address Bus Bit 29
CONST PIN_AB30 = 40  ' Address Bus Bit 30
CONST PIN_AB31 = 41  ' Address Bus Bit 31
CONST PIN_AB32 = 42  ' Address Bus Bit 32
CONST PIN_AB33 = 43  ' Address Bus Bit 33
CONST PIN_AB34 = 44  ' Address Bus Bit 34
CONST PIN_AB35 = 45  ' Address Bus Bit 35
CONST PIN_DB0 = 46  ' Data Bus Bit 0
CONST PIN_DB1 = 47  ' Data Bus Bit 1
CONST PIN_DB2 = 48  ' Data Bus Bit 2
CONST PIN_DB3 = 49  ' Data Bus Bit 3
CONST PIN_DB4 = 50  ' Data Bus Bit 4
CONST PIN_DB5 = 51  ' Data Bus Bit 5
CONST PIN_DB6 = 52  ' Data Bus Bit 6
CONST PIN_DB7 = 53  ' Data Bus Bit 7
CONST PIN_DB8 = 54  ' Data Bus Bit 8
CONST PIN_DB9 = 55  ' Data Bus Bit 9
CONST PIN_DB10 = 56  ' Data Bus Bit 10
CONST PIN_DB11 = 57  ' Data Bus Bit 11
CONST PIN_DB12 = 58  ' Data Bus Bit 12
CONST PIN_DB13 = 59  ' Data Bus Bit 13
CONST PIN_DB14 = 60  ' Data Bus Bit 14
CONST PIN_DB15 = 61  ' Data Bus Bit 15
CONST PIN_DB16 = 62  ' Data Bus Bit 16
CONST PIN_DB17 = 63  ' Data Bus Bit 17
CONST PIN_DB18 = 64  ' Data Bus Bit 18
CONST PIN_DB19 = 65  ' Data Bus Bit 19
CONST PIN_DB20 = 66  ' Data Bus Bit 20
CONST PIN_DB21 = 67  ' Data Bus Bit 21
CONST PIN_DB22 = 68  ' Data Bus Bit 22
CONST PIN_DB23 = 69  ' Data Bus Bit 23
CONST PIN_DB24 = 70  ' Data Bus Bit 24
CONST PIN_DB25 = 71  ' Data Bus Bit 25
CONST PIN_DB26 = 72  ' Data Bus Bit 26
CONST PIN_DB27 = 73  ' Data Bus Bit 27
CONST PIN_DB28 = 74  ' Data Bus Bit 28
CONST PIN_DB29 = 75  ' Data Bus Bit 29
CONST PIN_DB30 = 76  ' Data Bus Bit 30
CONST PIN_DB31 = 77  ' Data Bus Bit 31
CONST PIN_BE0 = 78  ' Byte Enable 0
CONST PIN_BE1 = 79  ' Byte Enable 1
CONST PIN_BE2 = 80  ' Byte Enable 2
CONST PIN_BE3 = 81  ' Byte Enable 3
CONST PIN_NCS0 = 82  ' Chip Select 0 (RDRAM)
CONST PIN_NCS1 = 83  ' Chip Select 1 (RCP)
CONST PIN_NCS2 = 84  ' Chip Select 2 (PIF ROM)
CONST PIN_NCS3 = 85  ' Chip Select 3 (Cartridge)
CONST PIN_NWR = 86  ' Write Enable
CONST PIN_NRD = 87  ' Read Enable
CONST PIN_EKN = 88  ' Audio DAC Data (I2S/EKN format)
CONST PIN_AUDIO_L = 89  ' Audio Left Output
CONST PIN_AUDIO_R = 90  ' Audio Right Output
CONST PIN_VIDEO_R = 91  ' Video Output Red
CONST PIN_VIDEO_G = 92  ' Video Output Green
CONST PIN_VIDEO_B = 93  ' Video Output Blue
CONST PIN_SYNC = 94  ' Video Sync

' 设备初始化子程序
SUB nec_vr4300_init()
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

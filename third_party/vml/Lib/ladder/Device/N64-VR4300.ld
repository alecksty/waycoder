(*
 * NEC-VR4300梯形图定义
 * 生成自: NEC/MIPS-R4000/NEC-VR4300
 * 版本: 1.0
 * 日期: 2026-04-16
 * 作者: VML Team
 * 描述: Nintendo 64 main processor - NEC VR4300 (MIPS R4300i-compatible) @ 93.75MHz, 64-bit R4000-like
 * CPU架构: MIPS-R4300i
 * 位宽: 64位
 * 时钟频率: 93750000 Hz
 *)

PROGRAM NEC_VR4300
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : LWORD; (* Hard-wired Zero *)
    R1 AT %M0x08 : LWORD; (* Assembler Temporary *)
    R2 AT %M0x10 : LWORD; (* Value Return *)
    R3 AT %M0x18 : LWORD; (* Expression Evaluation *)
    R4 AT %M0x20 : LWORD; (* Expression Evaluation *)
    R5 AT %M0x28 : LWORD; (* Expression Evaluation *)
    R6 AT %M0x30 : LWORD; (* Expression Evaluation *)
    R7 AT %M0x38 : LWORD; (* Expression Evaluation *)
    R8 AT %M0x40 : LWORD; (* Expression Evaluation *)
    R9 AT %M0x48 : LWORD; (* Expression Evaluation *)
    R10 AT %M0x50 : LWORD; (* Expression Evaluation *)
    R11 AT %M0x58 : LWORD; (* Expression Evaluation *)
    R12 AT %M0x60 : LWORD; (* Expression Evaluation *)
    R13 AT %M0x68 : LWORD; (* Expression Evaluation *)
    R14 AT %M0x70 : LWORD; (* Expression Evaluation *)
    R15 AT %M0x78 : LWORD; (* Expression Evaluation *)
    R16 AT %M0x80 : LWORD; (* Saved Value *)
    R17 AT %M0x88 : LWORD; (* Saved Value *)
    R18 AT %M0x90 : LWORD; (* Saved Value *)
    R19 AT %M0x98 : LWORD; (* Saved Value *)
    R20 AT %M0xA0 : LWORD; (* Saved Value *)
    R21 AT %M0xA8 : LWORD; (* Saved Value *)
    R22 AT %M0xB0 : LWORD; (* Saved Value *)
    R23 AT %M0xB8 : LWORD; (* Saved Value *)
    R24 AT %M0xC0 : LWORD; (* Temporary *)
    R25 AT %M0xC8 : LWORD; (* Temporary *)
    R26 AT %M0xD0 : LWORD; (* Kernel Reserved *)
    R27 AT %M0xD8 : LWORD; (* Kernel Reserved *)
    R28 AT %M0xE0 : LWORD; (* Global Pointer *)
    R29 AT %M0xE8 : LWORD; (* Stack Pointer *)
    R30 AT %M0xF0 : LWORD; (* Frame Pointer *)
    R31 AT %M0xF8 : LWORD; (* Return Address *)
    HI AT %M0x100 : LWORD; (* Multiply/Divide High (64-bit) *)
    LO AT %M0x108 : LWORD; (* Multiply/Divide Low (64-bit) *)
    PC AT %M0x110 : LWORD; (* Program Counter *)
    LLB AT %M0x118 : LWORD; (* LLAddr / LLBit (for LL/SC) *)
    CP0_INDEX AT %M0x200 : LWORD; (* TLB Index *)
    CP0_RANDOM AT %M0x208 : LWORD; (* TLB Random *)
    CP0_ENTRYLO0 AT %M0x210 : LWORD; (* TLB EntryLo 0 (even page) *)
    CP0_ENTRYLO1 AT %M0x218 : LWORD; (* TLB EntryLo 1 (odd page) *)
    CP0_CONTEXT AT %M0x220 : LWORD; (* Context Register (PTE base) *)
    CP0_PAGEMASK AT %M0x228 : LWORD; (* Page Mask (variable page size) *)
    CP0_WIRED AT %M0x230 : LWORD; (* TLB Wired *)
    CP0_BADVADDR AT %M0x238 : LWORD; (* Bad Virtual Address *)
    CP0_COUNT AT %M0x240 : LWORD; (* Count (incrementing timer) *)
    CP0_ENTRYHI AT %M0x250 : LWORD; (* TLB EntryHi (VPN2 + ASID) *)
    CP0_COMPARE AT %M0x258 : LWORD; (* Compare (timer interrupt) *)
    CP0_STATUS AT %M0x260 : LWORD; (* Status Register *)
    CP0_STATUS_IE AT %M0x260.0 : BOOL; (* Interrupt Enable *)
    CP0_STATUS_EXL AT %M0x260.1 : BOOL; (* Exception Level *)
    CP0_STATUS_ERL AT %M0x260.2 : BOOL; (* Error Level *)
    CP0_STATUS_KSU AT %M0x260.0 : BOOL; (* Kernel/User Mode *)
    CP0_STATUS_UX AT %M0x260.5 : BOOL; (* User Mode 64-bit (1=64-bit user) *)
    CP0_STATUS_SX AT %M0x260.6 : BOOL; (* Supervisor Mode 64-bit *)
    CP0_STATUS_KX AT %M0x260.7 : BOOL; (* Kernel Mode 64-bit *)
    CP0_STATUS_IM0_7 AT %M0x260.0 : BOOL; (* Interrupt Mask *)
    CP0_STATUS_CU0 AT %M0x260.28 : BOOL; (* Coprocessor 0 Usable *)
    CP0_STATUS_BEV AT %M0x260.22 : BOOL; (* Bootstrap Exception Vector *)
    CP0_STATUS_TS AT %M0x260.21 : BOOL; (* TLB Shutdown *)
    CP0_STATUS_FR AT %M0x260.26 : BOOL; (* Floating-Point Register Mode (32 double) *)
    CP0_CAUSE AT %M0x268 : LWORD; (* Cause Register *)
    CP0_CAUSE_EXCCODE AT %M0x268.0 : BOOL; (* Exception Code *)
    CP0_CAUSE_IP0_7 AT %M0x268.0 : BOOL; (* Interrupt Pending *)
    CP0_CAUSE_BD AT %M0x268.31 : BOOL; (* Branch Delay Slot *)
    CP0_CAUSE_CE AT %M0x268.0 : BOOL; (* Coprocessor Error *)
    CP0_EPC AT %M0x270 : LWORD; (* Exception PC *)
    CP0_CONFIG AT %M0x280 : LWORD; (* Config Register *)
    CP0_LLADDR AT %M0x288 : LWORD; (* Load Linked Address *)
    CP0_WATCHLO AT %M0x290 : LWORD; (* WatchLo (data/instruction break) *)
    CP0_WATCHHI AT %M0x298 : LWORD; (* WatchHi *)
    CP0_XCONTEXT AT %M0x2A0 : LWORD; (* Extended Context *)
    CP0_PID AT %M0x2B0 : LWORD; (* Tag/Process ID *)
    CP0_DEBUG AT %M0x2D8 : LWORD; (* Debug Register *)
    CP0_PERF AT %M0x2F0 : LWORD; (* Performance Counter *)

    (* 外设变量 *)
    (* Reality Signal Processor (Audio/Video microcode engine) *)
    RSP_BASE AT %MW0x04040000 : WORD;
    RSP_SP_MEM_ADDR AT %MW0x00 : DWORD;
    RSP_SP_DRAM_ADDR AT %MW0x04 : DWORD;
    RSP_SP_RD_LEN AT %MW0x08 : DWORD;
    RSP_SP_WR_LEN AT %MW0x0C : DWORD;
    RSP_SP_STATUS AT %MW0x10 : DWORD;
    RSP_SP_STATUS_BROKE AT %MW0x10.0 : BOOL; (* Command Queue Broke *)
    RSP_SP_STATUS_SLEEP AT %MW0x10.2 : BOOL; (* SP Sleep *)
    RSP_SP_STATUS_GOODMATCH AT %MW0x10.3 : BOOL; (* DMEM/IMEM Goodmatch *)
    RSP_SP_STATUS_SSTEP AT %MW0x10.4 : BOOL; (* Single Step *)
    RSP_SP_STATUS_INTSIG AT %MW0x10.5 : BOOL; (* Interrupt Signal *)
    RSP_SP_STATUS_HALT AT %MW0x10.6 : BOOL; (* Halt *)
    RSP_SP_STATUS_CLEAR AT %MW0x10.7 : BOOL; (* Clear SP Status *)
    RSP_SP_STATUS_INTR_BRK AT %MW0x10.8 : BOOL; (* IntrOnBreak *)
    RSP_SP_STATUS_SIGNAL0 AT %MW0x10.12 : BOOL; (* Software Signal 0 *)
    RSP_SP_STATUS_SIGNAL1 AT %MW0x10.13 : BOOL; (* Software Signal 1 *)
    RSP_SP_STATUS_SIGNAL2 AT %MW0x10.14 : BOOL; (* Software Signal 2 *)
    RSP_SP_STATUS_SIGNAL3 AT %MW0x10.15 : BOOL; (* Software Signal 3 *)
    RSP_SP_STATUS_SIGNAL4 AT %MW0x10.16 : BOOL; (* Software Signal 4 *)
    RSP_SP_STATUS_SIGNAL5 AT %MW0x10.17 : BOOL; (* Software Signal 5 *)
    RSP_SP_STATUS_SIGNAL6 AT %MW0x10.18 : BOOL; (* Software Signal 6 *)
    RSP_SP_STATUS_SIGNAL7 AT %MW0x10.19 : BOOL; (* Software Signal 7 *)
    RSP_SP_DMA_FULL AT %MW0x14 : DWORD;
    RSP_SP_DMA_BUSY AT %MW0x18 : DWORD;
    RSP_SP_SEMAPHORE AT %MW0x1C : DWORD;
    RSP_SP_PC AT %MW0x20 : DWORD;
    RSP_SP_IBIST AT %MW0x24 : DWORD;
    (* Reality Drawing Processor (Triangle/Quad rasterizer) *)
    RDP_BASE AT %MW0x04100000 : WORD;
    RDP_DP_START AT %MW0x00 : DWORD;
    RDP_DP_END AT %MW0x04 : DWORD;
    RDP_DP_CURRENT AT %MW0x08 : DWORD;
    RDP_DP_STATUS AT %MW0x0C : DWORD;
    RDP_DP_STATUS_TERMINATE AT %MW0x0C.0 : BOOL; (* Terminator *)
    RDP_DP_STATUS_PIPE_BUSY AT %MW0x0C.1 : BOOL; (* Pipeline Busy *)
    RDP_DP_STATUS_TOMINO_BUSY AT %MW0x0C.2 : BOOL; (* ToMini Busy *)
    RDP_DP_STATUS_PIPE_FLUSH AT %MW0x0C.3 : BOOL; (* Pipeline Flush *)
    RDP_DP_STATUS_TOMINO_FLUSH AT %MW0x0C.4 : BOOL; (* ToMini Flush *)
    RDP_DP_STATUS_FREEZE AT %MW0x0C.5 : BOOL; (* Freeze *)
    RDP_DP_STATUS_START_GCLK AT %MW0x0C.24 : BOOL; (* Start GCLK *)
    RDP_DP_CLOCK AT %MW0x10 : DWORD;
    RDP_DP_BUFBUSY AT %MW0x14 : DWORD;
    RDP_DP_PIPEBUSY AT %MW0x18 : DWORD;
    RDP_DP_TMEM AT %MW0x1C : DWORD;
    (* Video Interface (scanout engine) *)
    VI_BASE AT %MW0x04400000 : WORD;
    VI_VI_STATUS AT %MW0x00 : DWORD;
    VI_VI_STATUS_TYPE AT %MW0x00.0 : BOOL; (* Display Type (0=blank, 1=reserved, 2=480i, 3=240p, 4=1080i, 5=576i) *)
    VI_VI_STATUS_DITHER_FILTER AT %MW0x00.6 : BOOL; (* Dither Filter Enable *)
    VI_VI_STATUS_GAMMA AT %MW0x00.7 : BOOL; (* Gamma Correction Enable *)
    VI_VI_STATUS_GAMMA_DITHER AT %MW0x00.8 : BOOL; (* Gamma Dither Enable *)
    VI_VI_STATUS_DIVOT AT %MW0x00.9 : BOOL; (* Divot Control *)
    VI_VI_STATUS_SERRATION AT %MW0x00.10 : BOOL; (*  Serration Enable (for interlaced) *)
    VI_VI_ORIGIN AT %MW0x04 : DWORD;
    VI_VI_WIDTH AT %MW0x08 : DWORD;
    VI_VI_V_INTR AT %MW0x0C : DWORD;
    VI_VI_V_CURRENT AT %MW0x10 : DWORD;
    VI_VI_BURST AT %MW0x14 : DWORD;
    VI_VI_H_SYNC AT %MW0x18 : DWORD;
    VI_VI_H_SYNC_LEAP AT %MW0x1C : DWORD;
    VI_VI_H_VIDEO AT %MW0x20 : DWORD;
    VI_VI_V_VIDEO AT %MW0x24 : DWORD;
    VI_VI_V_BURST AT %MW0x28 : DWORD;
    VI_VI_X_SCALE AT %MW0x2C : DWORD;
    VI_VI_Y_SCALE AT %MW0x30 : DWORD;
    (* Audio Interface (DAC) *)
    AI_BASE AT %MW0x04500000 : WORD;
    AI_AI_DRAM_ADDR AT %MW0x00 : DWORD;
    AI_AI_LEN AT %MW0x04 : DWORD;
    AI_AI_CONTROL AT %MW0x08 : DWORD;
    AI_AI_CONTROL_DMA_ENABLE AT %MW0x08.0 : BOOL; (* DMA Enable *)
    AI_AI_CONTROL_DMA_FIFO_FULL AT %MW0x08.1 : BOOL; (* DMA FIFO Full *)
    AI_AI_STATUS AT %MW0x0C : DWORD;
    AI_AI_DACRATE AT %MW0x10 : DWORD;
    AI_AI_BITRATE AT %MW0x14 : DWORD;
    (* Peripheral Interface (cartridge bus) *)
    PI_BASE AT %MW0x04600000 : WORD;
    PI_PI_DRAM_ADDR AT %MW0x00 : DWORD;
    PI_PI_CART_ADDR AT %MW0x04 : DWORD;
    PI_PI_RD_LEN AT %MW0x08 : DWORD;
    PI_PI_WR_LEN AT %MW0x0C : DWORD;
    PI_PI_STATUS AT %MW0x10 : DWORD;
    PI_PI_STATUS_DMA_BUSY AT %MW0x10.0 : BOOL; (* DMA Busy *)
    PI_PI_STATUS_IO_BUSY AT %MW0x10.1 : BOOL; (* I/O Busy *)
    PI_PI_STATUS_ERROR AT %MW0x10.2 : BOOL; (* Bus Error *)
    PI_PI_BSD_DOM1_LAT AT %MW0x14 : DWORD;
    PI_PI_BSD_DOM1_PWD AT %MW0x18 : DWORD;
    PI_PI_BSD_DOM1_PGS AT %MW0x1C : DWORD;
    PI_PI_BSD_DOM1_RLS AT %MW0x20 : DWORD;
    PI_PI_BSD_DOM2_LAT AT %MW0x24 : DWORD;
    PI_PI_BSD_DOM2_PWD AT %MW0x28 : DWORD;
    PI_PI_BSD_DOM2_PGS AT %MW0x2C : DWORD;
    PI_PI_BSD_DOM2_RLS AT %MW0x30 : DWORD;
    (* Serial Interface (Controller Pak / 64DD) *)
    SI_BASE AT %MW0x04800000 : WORD;
    SI_SI_DRAM_ADDR AT %MW0x00 : DWORD;
    SI_SI_PIF_ADDR_RD64B AT %MW0x04 : DWORD;
    SI_SI_PIF_ADDR_WR64B AT %MW0x08 : DWORD;
    SI_SI_STATUS AT %MW0x10 : DWORD;
    SI_SI_STATUS_DMA_BUSY AT %MW0x10.0 : BOOL; (* DMA Busy *)
    SI_SI_STATUS_IO_BUSY AT %MW0x10.1 : BOOL; (* I/O Busy *)
    SI_SI_STATUS_INTERRUPT AT %MW0x10.12 : BOOL; (* SI Interrupt *)
    (* PIF (CIC / NUSYC - anti-piracy/copy protection) *)
    PIF_BASE AT %MW0x1FC007C0 : WORD;
    PIF_PIF_CMD0 AT %MW0x00 : BYTE;
    PIF_PIF_CMD1 AT %MW0x01 : BYTE;
    PIF_PIF_CMD2 AT %MW0x02 : BYTE;
    PIF_PIF_CMD3 AT %MW0x03 : BYTE;
    PIF_PIF_CMD4 AT %MW0x04 : BYTE;
    PIF_PIF_CMD5 AT %MW0x05 : BYTE;
    PIF_PIF_CMD6 AT %MW0x06 : BYTE;
    PIF_PIF_CMD7 AT %MW0x07 : BYTE;
    PIF_PIF_STATUS AT %MW0x3F : BYTE;
    (* Interrupt Control *)
    INTERRUPT_BASE AT %MW0x1FC00200 : WORD;
    INTERRUPT_MI_MODE AT %MW0x00 : DWORD;
    INTERRUPT_MI_MODE_INIT_MODE AT %MW0x00.0 : BOOL; (* Initialize Mode *)
    INTERRUPT_MI_MODE_EBUS_TEST AT %MW0x00.1 : BOOL; (* EBUS Test Mode *)
    INTERRUPT_MI_VERSION AT %MW0x04 : DWORD;
    INTERRUPT_MI_INTR AT %MW0x08 : DWORD;
    INTERRUPT_MI_INTR_SP AT %MW0x08.0 : BOOL; (* SP Interrupt Pending *)
    INTERRUPT_MI_INTR_SI AT %MW0x08.1 : BOOL; (* SI Interrupt Pending *)
    INTERRUPT_MI_INTR_AI AT %MW0x08.2 : BOOL; (* AI Interrupt Pending *)
    INTERRUPT_MI_INTR_VI AT %MW0x08.3 : BOOL; (* VI Interrupt Pending *)
    INTERRUPT_MI_INTR_PI AT %MW0x08.4 : BOOL; (* PI Interrupt Pending *)
    INTERRUPT_MI_INTR_DP AT %MW0x08.5 : BOOL; (* DP Interrupt Pending *)
    INTERRUPT_MI_INTR_MASK AT %MW0x0C : DWORD;
    INTERRUPT_MI_INTR_MASK_SP_MASK AT %MW0x0C.0 : BOOL; (* SP Interrupt Mask *)
    INTERRUPT_MI_INTR_MASK_SI_MASK AT %MW0x0C.1 : BOOL; (* SI Interrupt Mask *)
    INTERRUPT_MI_INTR_MASK_AI_MASK AT %MW0x0C.2 : BOOL; (* AI Interrupt Mask *)
    INTERRUPT_MI_INTR_MASK_VI_MASK AT %MW0x0C.3 : BOOL; (* VI Interrupt Mask *)
    INTERRUPT_MI_INTR_MASK_PI_MASK AT %MW0x0C.4 : BOOL; (* PI Interrupt Mask *)
    INTERRUPT_MI_INTR_MASK_DP_MASK AT %MW0x0C.5 : BOOL; (* DP Interrupt Mask *)
    (* Controller Interface (SI channel 0-3) *)
    CONTROLLER_BASE AT %MW0x1FC00600 : WORD;
    CONTROLLER_SI_CH0_DATA AT %MW0x00 : LWORD;
    CONTROLLER_SI_CH1_DATA AT %MW0x08 : LWORD;
    CONTROLLER_SI_CH2_DATA AT %MW0x10 : LWORD;
    CONTROLLER_SI_CH3_DATA AT %MW0x18 : LWORD;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Soft Reset / NMI *)
    INT_TLB_REFILL : BOOL; (* TLB Refill (I) / TLB Refill (D) *)
    INT_CACHE_ERROR : BOOL; (* Cache Error *)
    INT_GENERAL_EXCEPTION : BOOL; (* General Exception *)
    INT_RSP : BOOL; (* RSP Interrupt (microcode signal) *)
    INT_RDP : BOOL; (* RDP Interrupt (display list complete) *)
    INT_VI : BOOL; (* VI Interrupt (V-Blank / scanline) *)
    INT_AI : BOOL; (* AI Interrupt (audio DMA complete) *)
    INT_PI : BOOL; (* PI Interrupt (cartridge DMA) *)
    INT_SI : BOOL; (* SI Interrupt (serial interface) *)
    INT_TIMER_COMPARE : BOOL; (* Timer Compare (CP0 Count == Compare) *)

END_VAR

BEGIN
    (* NEC-VR4300初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|R0>0|--(监控灯)--
    --|R1>0|--(监控灯)--
    --|R2>0|--(监控灯)--
    --|R3>0|--(监控灯)--
    --|R4>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_TLB_REFILL|--[中断处理]--
    --|INT_CACHE_ERROR|--[中断处理]--
    --|INT_GENERAL_EXCEPTION|--[中断处理]--
    --|INT_RSP|--[中断处理]--
    --|INT_RDP|--[中断处理]--
    --|INT_VI|--[中断处理]--
    --|INT_AI|--[中断处理]--
    --|INT_PI|--[中断处理]--
    --|INT_SI|--[中断处理]--
    --|INT_TIMER_COMPARE|--[中断处理]--

END_PROGRAM

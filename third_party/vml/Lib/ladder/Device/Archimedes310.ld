(*
 * Acorn-Archimedes-A310梯形图定义
 * 生成自: Acorn Computers/Archimedes/Acorn-Archimedes-A310
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Acorn Archimedes A310 - First ARM-based home computer with RISC OS, ARM250 @ 26MHz
 * CPU架构: ARM250
 * 位宽: 32位
 * 时钟频率: 26000000 Hz
 *)

PROGRAM Acorn_Archimedes_A310
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    R0 AT %M0x00 : DWORD; (* General Purpose Register 0 *)
    R1 AT %M0x04 : DWORD; (* General Purpose Register 1 *)
    R2 AT %M0x08 : DWORD; (* General Purpose Register 2 *)
    R3 AT %M0x0C : DWORD; (* General Purpose Register 3 *)
    R4 AT %M0x10 : DWORD; (* General Purpose Register 4 *)
    R5 AT %M0x14 : DWORD; (* General Purpose Register 5 *)
    R6 AT %M0x18 : DWORD; (* General Purpose Register 6 *)
    R7 AT %M0x1C : DWORD; (* General Purpose Register 7 *)
    R8 AT %M0x20 : DWORD; (* General Purpose Register 8 *)
    R9 AT %M0x24 : DWORD; (* General Purpose Register 9 *)
    R10 AT %M0x28 : DWORD; (* General Purpose Register 10 *)
    R11 AT %M0x2C : DWORD; (* General Purpose Register 11 (fp) *)
    R12 AT %M0x30 : DWORD; (* General Purpose Register 12 *)
    SP AT %M0x34 : DWORD; (* Stack Pointer (R13) *)
    LR AT %M0x38 : DWORD; (* Link Register (R14) *)
    PC AT %M0x3C : DWORD; (* Program Counter (R15) *)
    PSR AT %M0x40 : DWORD; (* Processor Status Register *)
    PSR_MODE AT %M0x40.0 : BOOL; (* Mode bits (0-4) *)
    PSR_T AT %M0x40.5 : BOOL; (* Thumb state *)
    PSR_F AT %M0x40.6 : BOOL; (* FIQ disable *)
    PSR_I AT %M0x40.7 : BOOL; (* IRQ disable *)
    PSR_V AT %M0x40.28 : BOOL; (* Overflow *)
    PSR_C AT %M0x40.29 : BOOL; (* Carry *)
    PSR_Z AT %M0x40.30 : BOOL; (* Zero *)
    PSR_N AT %M0x40.31 : BOOL; (* Negative *)

    (* 外设变量 *)
    (* I/O Controller (IOC) - Interrupt/Keyboard/RTC *)
    IOC_BASE AT %MW0x03000000 : WORD;
    IOC_IOC_TIMER1 AT %MW0x03000000 : DWORD;
    IOC_IOC_TIMER2 AT %MW0x03000004 : DWORD;
    IOC_IOC_IOSEL AT %MW0x03000008 : DWORD;
    IOC_IOC_IRQST AT %MW0x0300000C : DWORD;
    IOC_IOC_IRQLATCH AT %MW0x03000010 : DWORD;
    IOC_IOC_FIQST AT %MW0x03000014 : DWORD;
    IOC_IOC_FIQEN AT %MW0x03000018 : DWORD;
    IOC_IOC_IRQEN AT %MW0x0300001C : DWORD;
    IOC_IOC_KBDDATA AT %MW0x03000020 : DWORD;
    IOC_IOC_KBDCR AT %MW0x03000024 : DWORD;
    IOC_IOC_RTCDR AT %MW0x03000028 : DWORD;
    IOC_IOC_RTCCR AT %MW0x0300002C : DWORD;
    IOC_IOC_PRST AT %MW0x03000030 : DWORD;
    IOC_IOC_PORTA AT %MW0x03000034 : DWORD;
    IOC_IOC_PORTB AT %MW0x03000038 : DWORD;
    IOC_IOC_PORTC AT %MW0x0300003C : DWORD;
    (* Memory Controller (MEMC1) *)
    MEMC_BASE AT %MW0x03200000 : WORD;
    MEMC_MEMC_PT AT %MW0x03200000 : DWORD;
    MEMC_MEMC_CTRL AT %MW0x03200004 : DWORD;
    MEMC_MEMC_DRAM AT %MW0x03200008 : DWORD;
    MEMC_MEMC_ERR AT %MW0x0320000C : DWORD;
    (* Video Controller - VIDC1 *)
    VIDC_BASE AT %MW0x03400000 : WORD;
    VIDC_VIDC_PALETTE AT %MW0x03400000 : DWORD;
    VIDC_VIDC_STARTL AT %MW0x03400004 : DWORD;
    VIDC_VIDC_STARTH AT %MW0x03400008 : DWORD;
    VIDC_VIDC_CONFIG AT %MW0x0340000C : DWORD;
    VIDC_VIDC_HDISP AT %MW0x03400010 : DWORD;
    VIDC_VIDC_VDISP AT %MW0x03400014 : DWORD;
    VIDC_VIDC_HSYNC AT %MW0x03400018 : DWORD;
    VIDC_VIDC_VSYNC AT %MW0x0340001C : DWORD;
    VIDC_VIDC_BORDER AT %MW0x03400020 : DWORD;
    VIDC_VIDC_CURSOR AT %MW0x03400024 : DWORD;
    VIDC_VIDC_SOUND AT %MW0x03400028 : DWORD;
    (* Intel 82710 Floppy Disk Controller *)
    FDC_BASE AT %MW0x03010000 : WORD;
    FDC_FDC_STATUS AT %MW0x03010000 : BYTE;
    FDC_FDC_COMMAND AT %MW0x03010000 : BYTE;
    FDC_FDC_TRACK AT %MW0x03010004 : BYTE;
    FDC_FDC_SECTOR AT %MW0x03010008 : BYTE;
    FDC_FDC_DATA AT %MW0x0301000C : BYTE;
    (* Serial Port (via IOC) *)
    SERIAL_BASE AT %MW0x03010010 : WORD;
    SERIAL_SERIAL_TX AT %MW0x03010010 : BYTE;
    SERIAL_SERIAL_RX AT %MW0x03010014 : BYTE;
    SERIAL_SERIAL_CTRL AT %MW0x03010018 : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Reset *)
    INT_UND : BOOL; (* Undefined instruction *)
    INT_SWI : BOOL; (* Software Interrupt (SWI/SVC) *)
    INT_PABORT : BOOL; (* Prefetch Abort *)
    INT_DABORT : BOOL; (* Data Abort *)
    INT_ADDRESS : BOOL; (* Address Exception *)
    INT_IRQ : BOOL; (* IRQ interrupt (IOC) *)
    INT_FIQ : BOOL; (* FIQ interrupt (VIDC) *)

END_VAR

BEGIN
    (* Acorn-Archimedes-A310初始化梯形图 *)
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
    --|INT_UND|--[中断处理]--
    --|INT_SWI|--[中断处理]--
    --|INT_PABORT|--[中断处理]--
    --|INT_DABORT|--[中断处理]--
    --|INT_ADDRESS|--[中断处理]--
    --|INT_IRQ|--[中断处理]--
    --|INT_FIQ|--[中断处理]--

END_PROGRAM

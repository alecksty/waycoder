(*
 * Apple-IIe梯形图定义
 * 生成自: Apple Computer/Apple II/Apple-IIe
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Apple II Enhanced - 8-bit personal computer with MOS 6502 CPU
 * CPU架构: MOS-6502
 * 位宽: 8位
 * 时钟频率: 1021800 Hz
 *)

PROGRAM Apple_IIe
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    A AT %M0x00 : BYTE; (* Accumulator *)
    X AT %M0x01 : BYTE; (* X Index Register *)
    Y AT %M0x02 : BYTE; (* Y Index Register *)
    SP AT %M0x03 : BYTE; (* Stack Pointer *)
    PC AT %M0x04 : WORD; (* Program Counter *)
    P AT %M0x06 : BYTE; (* Processor Status *)
    P_C AT %M0x06.0 : BOOL; (* Carry Flag *)
    P_Z AT %M0x06.1 : BOOL; (* Zero Flag *)
    P_I AT %M0x06.2 : BOOL; (* Interrupt Disable *)
    P_D AT %M0x06.3 : BOOL; (* Decimal Mode *)
    P_B AT %M0x06.4 : BOOL; (* Break Command *)
    P_U AT %M0x06.5 : BOOL; (* Unused *)
    P_V AT %M0x06.6 : BOOL; (* Overflow Flag *)
    P_N AT %M0x06.7 : BOOL; (* Negative Flag *)

    (* 外设变量 *)
    (* Versatile Interface Adapter (6522) *)
    VIA_BASE AT %MW0xC000 : WORD;
    VIA_ORB AT %MW0xC000 : BYTE;
    VIA_ORA AT %MW0xC001 : BYTE;
    VIA_DDRB AT %MW0xC002 : BYTE;
    VIA_DDRA AT %MW0xC003 : BYTE;
    VIA_T1C AT %MW0xC004 : WORD;
    VIA_T1L AT %MW0xC006 : WORD;
    VIA_T2C AT %MW0xC008 : WORD;
    VIA_SR AT %MW0xC00A : BYTE;
    VIA_ACR AT %MW0xC00B : BYTE;
    VIA_PCR AT %MW0xC00C : BYTE;
    VIA_IFG AT %MW0xC00D : BYTE;
    VIA_IER AT %MW0xC00E : BYTE;
    VIA_ORA_NH AT %MW0xC00F : BYTE;
    (* Peripheral Interface Adapter (6520) *)
    PIA_BASE AT %MW0xC010 : WORD;
    PIA_PA AT %MW0xC010 : BYTE;
    PIA_PB AT %MW0xC011 : BYTE;
    PIA_DDRA AT %MW0xC012 : BYTE;
    PIA_DDRB AT %MW0xC013 : BYTE;
    PIA_CA1 AT %MW0xC014 : BYTE;
    PIA_CA2 AT %MW0xC015 : BYTE;
    PIA_CB1 AT %MW0xC016 : BYTE;
    PIA_CB2 AT %MW0xC017 : BYTE;
    (* Keyboard (via PIA) *)
    KBD_BASE AT %MW0xC000 : WORD;
    KBD_KEYDATA AT %MW0xC000 : BYTE;
    KBD_KEYSTROBE AT %MW0xC010 : BYTE;
    KBD_KBDCTRL AT %MW0xC025 : BYTE;
    KBD_KBDERR AT %MW0xC026 : BYTE;
    (* Speaker *)
    SPEAKER_BASE AT %MW0xC030 : WORD;
    SPEAKER_SPKR AT %MW0xC030 : BYTE;
    (* Game I/O Port *)
    GAME_PORT_BASE AT %MW0xC050 : WORD;
    GAME_PORT_GAME_SW0 AT %MW0xC061 : BYTE;
    GAME_PORT_GAME_SW1 AT %MW0xC062 : BYTE;
    GAME_PORT_GAME_AN0 AT %MW0xC064 : BYTE;
    GAME_PORT_GAME_AN1 AT %MW0xC065 : BYTE;
    GAME_PORT_GAME_AN2 AT %MW0xC066 : BYTE;
    GAME_PORT_GAME_AN3 AT %MW0xC067 : BYTE;
    GAME_PORT_GAME_TRIG AT %MW0xC070 : BYTE;
    (* Disk II Controller *)
    DISKII_BASE AT %MW0xC0E0 : WORD;
    DISKII_PHASE0 AT %MW0xC0E0 : BYTE;
    DISKII_PHASE1 AT %MW0xC0E1 : BYTE;
    DISKII_PHASE2 AT %MW0xC0E2 : BYTE;
    DISKII_PHASE3 AT %MW0xC0E3 : BYTE;
    DISKII_Q6L AT %MW0xC0EC : BYTE;
    DISKII_Q7L AT %MW0xC0ED : BYTE;
    DISKII_Q6R AT %MW0xC0EE : BYTE;
    DISKII_Q7R AT %MW0xC0EF : BYTE;
    (* Video Display Generator *)
    VIDEO_BASE AT %MW0xC050 : WORD;
    VIDEO_TXTCLR AT %MW0xC050 : BYTE;
    VIDEO_MIXCLR AT %MW0xC051 : BYTE;
    VIDEO_TXTPAGE2 AT %MW0xC054 : BYTE;
    VIDEO_TXTPAGE1 AT %MW0xC055 : BYTE;
    VIDEO_LORES AT %MW0xC056 : BYTE;
    VIDEO_HIRES AT %MW0xC057 : BYTE;
    VIDEO_DHIRESON AT %MW0xC05E : BYTE;
    VIDEO_AN0 AT %MW0xC058 : BYTE;
    VIDEO_AN1 AT %MW0xC059 : BYTE;
    VIDEO_AN2 AT %MW0xC05A : BYTE;
    VIDEO_AN3 AT %MW0xC05B : BYTE;
    VIDEO__80STORE AT %MW0xC000 : BYTE;
    (* RAM Read/Write Control *)
    RAMRD_BASE AT %MW0xC080 : WORD;
    RAMRD_INTCXROM AT %MW0xCFFF : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Power-on Reset *)
    INT_NMI : BOOL; (* Non-Maskable Interrupt (from VIA) *)
    INT_IRQ : BOOL; (* IRQ from VIA/timer/slot *)
    INT_BRK : BOOL; (* BRK Instruction *)

END_VAR

BEGIN
    (* Apple-IIe初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 3: 寄存器监控 *)
    --|A>0|--(监控灯)--
    --|X>0|--(监控灯)--
    --|Y>0|--(监控灯)--
    --|SP>0|--(监控灯)--
    --|PC>0|--(监控灯)--

    (* 网络 4: 中断处理 *)
    --|INT_RESET|--[中断处理]--
    --|INT_NMI|--[中断处理]--
    --|INT_IRQ|--[中断处理]--
    --|INT_BRK|--[中断处理]--

END_PROGRAM

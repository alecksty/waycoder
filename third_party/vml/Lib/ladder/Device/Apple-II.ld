(*
 * Apple-II梯形图定义
 * 生成自: Apple Computer/Apple II/Apple-II
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Apple II personal computer with MOS 6502 CPU, 48KB RAM, and color graphics
 * CPU架构: MOS 6502
 * 位宽: 8位
 * 时钟频率: 1023000 Hz
 *)

PROGRAM Apple_II
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)
    A AT %M0 : BYTE; (* Accumulator *)
    X AT %M0 : BYTE; (* Index Register X *)
    Y AT %M0 : BYTE; (* Index Register Y *)
    SP AT %M0 : BYTE; (* Stack Pointer *)
    PC AT %M0 : WORD; (* Program Counter *)
    P AT %M0 : BYTE; (* Status Register *)

    (* 外设变量 *)
    (* Apple II keyboard *)
    KEYBOARD_BASE AT %MW : WORD;
    KEYBOARD_KBD AT %MW0xC000 : BYTE;
    KEYBOARD_KBDSTRB AT %MW0xC010 : BYTE;
    (* Built-in speaker *)
    SPEAKER_BASE AT %MW : WORD;
    SPEAKER_SPKR AT %MW0xC030 : BYTE;
    (* Cassette tape interface *)
    CASSETTE_BASE AT %MW : WORD;
    CASSETTE_TAPEIN AT %MW0xC060 : BYTE;
    CASSETTE_TAPEOUT AT %MW0xC020 : BYTE;
    (* Game controller port *)
    GAMEPORT_BASE AT %MW : WORD;
    GAMEPORT_PADDLE0 AT %MW0xC064 : BYTE;
    GAMEPORT_PADDLE1 AT %MW0xC065 : BYTE;
    GAMEPORT_PADDLE2 AT %MW0xC066 : BYTE;
    GAMEPORT_PADDLE3 AT %MW0xC067 : BYTE;
    GAMEPORT_BUTTON0 AT %MW0xC061 : BYTE;
    GAMEPORT_BUTTON1 AT %MW0xC062 : BYTE;
    (* Disk II controller *)
    DISKCONTROLLER_BASE AT %MW : WORD;
    DISKCONTROLLER_DISKUNIT AT %MW0xC0E0 : BYTE;
    DISKCONTROLLER_DISKCMD AT %MW0xC0E8 : BYTE;
    DISKCONTROLLER_DISKSTAT AT %MW0xC0E9 : BYTE;
    DISKCONTROLLER_DISKDATA AT %MW0xC0EA : BYTE;

    (* 中断变量 *)
    INT_NMI : BOOL; (* Non-maskable interrupt *)
    INT_RESET : BOOL; (* Reset vector *)
    INT_IRQ : BOOL; (* Interrupt request *)
    INT_BRK : BOOL; (* Break instruction *)

END_VAR

BEGIN
    (* Apple-II初始化梯形图 *)
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
    --|INT_NMI|--[中断处理]--
    --|INT_RESET|--[中断处理]--
    --|INT_IRQ|--[中断处理]--
    --|INT_BRK|--[中断处理]--

END_PROGRAM

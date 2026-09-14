(*
 * Commodore-64梯形图定义
 * 生成自: Commodore/C64/Commodore-64
 * 版本: 1.0
 * 日期: 2026-04-17
 * 作者: VML Team
 * 描述: Commodore 64 - Best-selling 8-bit home computer with MOS 6510 CPU, VIC-II graphics, and SID audio
 * CPU架构: MOS-6510
 * 位宽: 8位
 * 时钟频率: 1022727 Hz
 *)

PROGRAM Commodore_64
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
    P_B AT %M0x06.4 : BOOL; (* Break Flag *)
    P_U AT %M0x06.5 : BOOL; (* Unused *)
    P_V AT %M0x06.6 : BOOL; (* Overflow Flag *)
    P_N AT %M0x06.7 : BOOL; (* Negative Flag *)
    PORT AT %M0x00 : BYTE; (* I/O Port (6510 only: DDR + data) *)

    (* 外设变量 *)
    (* Video Interface Chip II - 6567/6569 *)
    VICII_BASE AT %MW0xD000 : WORD;
    VICII_SP0X AT %MW0xD000 : BYTE;
    VICII_SP0Y AT %MW0xD001 : BYTE;
    VICII_SP1X AT %MW0xD002 : BYTE;
    VICII_SP1Y AT %MW0xD003 : BYTE;
    VICII_SP2X AT %MW0xD004 : BYTE;
    VICII_SP2Y AT %MW0xD005 : BYTE;
    VICII_SP3X AT %MW0xD006 : BYTE;
    VICII_SP3Y AT %MW0xD007 : BYTE;
    VICII_SP4X AT %MW0xD008 : BYTE;
    VICII_SP4Y AT %MW0xD009 : BYTE;
    VICII_SP5X AT %MW0xD00A : BYTE;
    VICII_SP5Y AT %MW0xD00B : BYTE;
    VICII_SP6X AT %MW0xD00C : BYTE;
    VICII_SP6Y AT %MW0xD00D : BYTE;
    VICII_SP7X AT %MW0xD00E : BYTE;
    VICII_SP7Y AT %MW0xD00F : BYTE;
    VICII_MSIGX AT %MW0xD010 : BYTE;
    VICII_SCROLY AT %MW0xD011 : BYTE;
    VICII_SCROLX AT %MW0xD016 : BYTE;
    VICII_YPSTOP AT %MW0xD012 : BYTE;
    VICII_LPX AT %MW0xD013 : BYTE;
    VICII_LPY AT %MW0xD014 : BYTE;
    VICII_SPENA AT %MW0xD015 : BYTE;
    VICII_CSPMC AT %MW0xD017 : BYTE;
    VICII_MM0 AT %MW0xD018 : BYTE;
    VICII_VM01 AT %MW0xD016 : BYTE;
    VICII_VICBAS AT %MW0xD018 : BYTE;
    VICII_IRQMASK AT %MW0xD019 : BYTE;
    VICII_IRQST AT %MW0xD01A : BYTE;
    VICII_SPBGPR AT %MW0xD01B : BYTE;
    VICII_SPMC AT %MW0xD01C : BYTE;
    VICII_SP1C AT %MW0xD025 : BYTE;
    VICII_SP2C AT %MW0xD026 : BYTE;
    VICII_SPBC AT %MW0xD027 : BYTE;
    VICII_SP1C0 AT %MW0xD028 : BYTE;
    VICII_SP2C0 AT %MW0xD029 : BYTE;
    VICII_SP3C0 AT %MW0xD02A : BYTE;
    VICII_SP4C0 AT %MW0xD02B : BYTE;
    VICII_SP5C0 AT %MW0xD02C : BYTE;
    VICII_SP6C0 AT %MW0xD02D : BYTE;
    VICII_SP7C0 AT %MW0xD02E : BYTE;
    VICII_REG_FD AT %MW0xD01D : BYTE;
    VICII_BGCOL0 AT %MW0xD021 : BYTE;
    VICII_BGCOL1 AT %MW0xD022 : BYTE;
    VICII_BGCOL2 AT %MW0xD023 : BYTE;
    VICII_BGCOL3 AT %MW0xD024 : BYTE;
    (* Sound Interface Device 6581/8580 *)
    SID_BASE AT %MW0xD400 : WORD;
    SID_FREQ1LO AT %MW0xD400 : BYTE;
    SID_FREQ1HI AT %MW0xD401 : BYTE;
    SID_PW1LO AT %MW0xD402 : BYTE;
    SID_PW1HI AT %MW0xD403 : BYTE;
    SID_CR1 AT %MW0xD404 : BYTE;
    SID_AD1 AT %MW0xD405 : BYTE;
    SID_SR1 AT %MW0xD406 : BYTE;
    SID_FREQ2LO AT %MW0xD407 : BYTE;
    SID_FREQ2HI AT %MW0xD408 : BYTE;
    SID_PW2LO AT %MW0xD409 : BYTE;
    SID_PW2HI AT %MW0xD40A : BYTE;
    SID_CR2 AT %MW0xD40B : BYTE;
    SID_AD2 AT %MW0xD40C : BYTE;
    SID_SR2 AT %MW0xD40D : BYTE;
    SID_FREQ3LO AT %MW0xD40E : BYTE;
    SID_FREQ3HI AT %MW0xD40F : BYTE;
    SID_PW3LO AT %MW0xD410 : BYTE;
    SID_PW3HI AT %MW0xD411 : BYTE;
    SID_CR3 AT %MW0xD412 : BYTE;
    SID_AD3 AT %MW0xD413 : BYTE;
    SID_SR3 AT %MW0xD414 : BYTE;
    SID_FCH AT %MW0xD415 : BYTE;
    SID_FCL AT %MW0xD416 : BYTE;
    SID_RES_FLT AT %MW0xD417 : BYTE;
    SID_VOLUME AT %MW0xD418 : BYTE;
    SID_POTX AT %MW0xD419 : BYTE;
    SID_POTY AT %MW0xD41A : BYTE;
    SID_OSC3 AT %MW0xD41B : BYTE;
    SID_ENV3 AT %MW0xD41C : BYTE;
    (* Complex Interface Adapter 1 - Keyboard/Serial *)
    CIA1_BASE AT %MW0xDC00 : WORD;
    CIA1_PRA AT %MW0xDC00 : BYTE;
    CIA1_PRB AT %MW0xDC01 : BYTE;
    CIA1_DDRA AT %MW0xDC02 : BYTE;
    CIA1_DDRB AT %MW0xDC03 : BYTE;
    CIA1_TA_LO AT %MW0xDC04 : BYTE;
    CIA1_TA_HI AT %MW0xDC05 : BYTE;
    CIA1_TB_LO AT %MW0xDC06 : BYTE;
    CIA1_TB_HI AT %MW0xDC07 : BYTE;
    CIA1_TOD_TENTH AT %MW0xDC08 : BYTE;
    CIA1_TOD_SEC AT %MW0xDC09 : BYTE;
    CIA1_TOD_MIN AT %MW0xDC0A : BYTE;
    CIA1_TOD_HR AT %MW0xDC0B : BYTE;
    CIA1_SDR AT %MW0xDC0C : BYTE;
    CIA1_ICR AT %MW0xDC0D : BYTE;
    CIA1_CRA AT %MW0xDC0E : BYTE;
    CIA1_CRB AT %MW0xDC0F : BYTE;
    (* Complex Interface Adapter 2 - Serial/Bus *)
    CIA2_BASE AT %MW0xDD00 : WORD;
    CIA2_PRA AT %MW0xDD00 : BYTE;
    CIA2_PRB AT %MW0xDD01 : BYTE;
    CIA2_DDRA AT %MW0xDD02 : BYTE;
    CIA2_DDRB AT %MW0xDD03 : BYTE;
    CIA2_TA_LO AT %MW0xDD04 : BYTE;
    CIA2_TA_HI AT %MW0xDD05 : BYTE;
    CIA2_TB_LO AT %MW0xDD06 : BYTE;
    CIA2_TB_HI AT %MW0xDD07 : BYTE;
    CIA2_TOD_TENTH AT %MW0xDD08 : BYTE;
    CIA2_TOD_SEC AT %MW0xDD09 : BYTE;
    CIA2_TOD_MIN AT %MW0xDD0A : BYTE;
    CIA2_TOD_HR AT %MW0xDD0B : BYTE;
    CIA2_SDR AT %MW0xDD0C : BYTE;
    CIA2_ICR AT %MW0xDD0D : BYTE;
    CIA2_CRA AT %MW0xDD0E : BYTE;
    CIA2_CRB AT %MW0xDD0F : BYTE;
    (* Color RAM (4-bit per char cell) *)
    COLORRAM_BASE AT %MW0xD800 : WORD;
    COLORRAM_COLOR AT %MW0xD800 : BYTE;
    (* IEC Serial Bus (via CIA1) *)
    IEC_BASE AT %MW0xDC00 : WORD;
    IEC_IEC_DATA AT %MW0xDC00 : BYTE;
    IEC_IEC_CLOCK AT %MW0xDC01 : BYTE;

    (* 中断变量 *)
    INT_RESET : BOOL; (* Power-on / Reset *)
    INT_NMI : BOOL; (* Non-Maskable Interrupt *)
    INT_IRQ : BOOL; (* IRQ (VIC raster / CIA timer) *)

END_VAR

BEGIN
    (* Commodore-64初始化梯形图 *)
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

END_PROGRAM

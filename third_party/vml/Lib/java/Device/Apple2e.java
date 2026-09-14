package vml.device.applecomputer.apple_iie;

/**
 * Apple-IIe 寄存器定义
 * 生成自: Apple Computer/Apple II/Apple-IIe
 * 版本: 1.0
 */
public final class Apple_IIe {
    private Apple_IIe() {} // 工具类
    // CPU架构: MOS-6502, 8位, 1021800 Hz

    // 寄存器定义
    // Accumulator
    public static final int A_ADDR = (int)0x00;

    // X Index Register
    public static final int X_ADDR = (int)0x01;

    // Y Index Register
    public static final int Y_ADDR = (int)0x02;

    // Stack Pointer
    public static final int SP_ADDR = (int)0x03;

    // Program Counter
    public static final int PC_ADDR = (int)0x04;

    // Processor Status
    public static final int P_ADDR = (int)0x06;
    public static final int P_C = 0;  // Carry Flag
    public static final int P_Z = 1;  // Zero Flag
    public static final int P_I = 2;  // Interrupt Disable
    public static final int P_D = 3;  // Decimal Mode
    public static final int P_B = 4;  // Break Command
    public static final int P_U = 5;  // Unused
    public static final int P_V = 6;  // Overflow Flag
    public static final int P_N = 7;  // Negative Flag

    // 内存段定义
    // Main RAM (48KB base, up to 64KB with slot RAM)
    public static final int MAIN_RAM_START = (int)0x0000;
    public static final int MAIN_RAM_END = (int)0xBFFF;
    public static final int MAIN_RAM_SIZE = 49152;

    // Text screen buffer (40x24)
    public static final int TEXT_RAM_START = (int)0x0400;
    public static final int TEXT_RAM_END = (int)0x07FF;
    public static final int TEXT_RAM_SIZE = 1024;

    // High-resolution graphics buffer
    public static final int HIRES_RAM_START = (int)0x2000;
    public static final int HIRES_RAM_END = (int)0x5FFF;
    public static final int HIRES_RAM_SIZE = 16384;

    // 80-column text auxiliary RAM
    public static final int AUX_RAM_START = (int)0x0400;
    public static final int AUX_RAM_END = (int)0x09FF;
    public static final int AUX_RAM_SIZE = 1536;

    // Monitor ROM (applesoft/Integer)
    public static final int MONITOR_ROM_START = (int)0xC100;
    public static final int MONITOR_ROM_END = (int)0xCFFF;
    public static final int MONITOR_ROM_SIZE = 3840;

    // Applesoft BASIC ROM
    public static final int BASIC_ROM_START = (int)0xD000;
    public static final int BASIC_ROM_END = (int)0xFFFF;
    public static final int BASIC_ROM_SIZE = 12288;

    // Expansion Slot ROM
    public static final int SLOT_ROM_START = (int)0xC100;
    public static final int SLOT_ROM_END = (int)0xC7FF;
    public static final int SLOT_ROM_SIZE = 768;

    // I/O Select (slot space)
    public static final int MMIO_START = (int)0xC080;
    public static final int MMIO_END = (int)0xC0FF;
    public static final int MMIO_SIZE = 128;

    // 外设定义
    // Versatile Interface Adapter (6522)
    public static final int VIA_BASE = (int)0xC000;
    public static final int VIA_ORB = (int)0x00018000;
    public static final int VIA_ORA = (int)0x00018001;
    public static final int VIA_DDRB = (int)0x00018002;
    public static final int VIA_DDRA = (int)0x00018003;
    public static final int VIA_T1C = (int)0x00018004;
    public static final int VIA_T1L = (int)0x00018006;
    public static final int VIA_T2C = (int)0x00018008;
    public static final int VIA_SR = (int)0x0001800A;
    public static final int VIA_ACR = (int)0x0001800B;
    public static final int VIA_PCR = (int)0x0001800C;
    public static final int VIA_IFG = (int)0x0001800D;
    public static final int VIA_IER = (int)0x0001800E;
    public static final int VIA_ORA_NH = (int)0x0001800F;

    // Peripheral Interface Adapter (6520)
    public static final int PIA_BASE = (int)0xC010;
    public static final int PIA_PA = (int)0x00018020;
    public static final int PIA_PB = (int)0x00018021;
    public static final int PIA_DDRA = (int)0x00018022;
    public static final int PIA_DDRB = (int)0x00018023;
    public static final int PIA_CA1 = (int)0x00018024;
    public static final int PIA_CA2 = (int)0x00018025;
    public static final int PIA_CB1 = (int)0x00018026;
    public static final int PIA_CB2 = (int)0x00018027;

    // Keyboard (via PIA)
    public static final int KBD_BASE = (int)0xC000;
    public static final int KBD_KEYDATA = (int)0x00018000;
    public static final int KBD_KEYSTROBE = (int)0x00018010;
    public static final int KBD_KBDCTRL = (int)0x00018025;
    public static final int KBD_KBDERR = (int)0x00018026;

    // Speaker
    public static final int SPEAKER_BASE = (int)0xC030;
    public static final int SPEAKER_SPKR = (int)0x00018060;

    // Game I/O Port
    public static final int GAME_PORT_BASE = (int)0xC050;
    public static final int GAME_PORT_GAME_SW0 = (int)0x000180B1;
    public static final int GAME_PORT_GAME_SW1 = (int)0x000180B2;
    public static final int GAME_PORT_GAME_AN0 = (int)0x000180B4;
    public static final int GAME_PORT_GAME_AN1 = (int)0x000180B5;
    public static final int GAME_PORT_GAME_AN2 = (int)0x000180B6;
    public static final int GAME_PORT_GAME_AN3 = (int)0x000180B7;
    public static final int GAME_PORT_GAME_TRIG = (int)0x000180C0;

    // Disk II Controller
    public static final int DISKII_BASE = (int)0xC0E0;
    public static final int DISKII_PHASE0 = (int)0x000181C0;
    public static final int DISKII_PHASE1 = (int)0x000181C1;
    public static final int DISKII_PHASE2 = (int)0x000181C2;
    public static final int DISKII_PHASE3 = (int)0x000181C3;
    public static final int DISKII_Q6L = (int)0x000181CC;
    public static final int DISKII_Q7L = (int)0x000181CD;
    public static final int DISKII_Q6R = (int)0x000181CE;
    public static final int DISKII_Q7R = (int)0x000181CF;

    // Video Display Generator
    public static final int VIDEO_BASE = (int)0xC050;
    public static final int VIDEO_TXTCLR = (int)0x000180A0;
    public static final int VIDEO_MIXCLR = (int)0x000180A1;
    public static final int VIDEO_TXTPAGE2 = (int)0x000180A4;
    public static final int VIDEO_TXTPAGE1 = (int)0x000180A5;
    public static final int VIDEO_LORES = (int)0x000180A6;
    public static final int VIDEO_HIRES = (int)0x000180A7;
    public static final int VIDEO_DHIRESON = (int)0x000180AE;
    public static final int VIDEO_AN0 = (int)0x000180A8;
    public static final int VIDEO_AN1 = (int)0x000180A9;
    public static final int VIDEO_AN2 = (int)0x000180AA;
    public static final int VIDEO_AN3 = (int)0x000180AB;
    public static final int VIDEO__80STORE = (int)0x00018050;

    // RAM Read/Write Control
    public static final int RAMRD_BASE = (int)0xC080;
    public static final int RAMRD_INTCXROM = (int)0x0001907F;

    // 中断向量定义
    public static final int IRQ_RESET = 0;  // Power-on Reset
    public static final int IRQ_NMI = 1;  // Non-Maskable Interrupt (from VIA)
    public static final int IRQ_IRQ = 2;  // IRQ from VIA/timer/slot
    public static final int IRQ_BRK = 3;  // BRK Instruction

    // 引脚定义
    public static final int PIN_VCC = 1;  // +5V Power
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_RESET = 3;  // System Reset
    public static final int PIN_CLK = 4;  // System Clock (1.023MHz NTSC)
    public static final int PIN_RDY = 5;  // CPU Ready
    public static final int PIN_NMI = 6;  // Non-Maskable Interrupt
    public static final int PIN_IRQ = 7;  // Interrupt Request
    public static final int PIN_SO = 8;  // Set Overflow
    public static final int PIN_RWB = 9;  // Read/Write Bar
    public static final int PIN_SYNC = 10;  // Instruction Sync
    public static final int PIN_A0_A15 = 11;  // Address Bus (16-bit)
    public static final int PIN_D0_D7 = 12;  // Data Bus (8-bit)
    public static final int PIN_PHASE0 = 13;  // Phase 0 (4MHz system)
    public static final int PIN_PHASE1 = 14;  // Phase 1
    public static final int PIN_PHASE2 = 15;  // Phase 2
    public static final int PIN_PHASE3 = 16;  // Phase 3

    public static native void apple_iie_init();
}

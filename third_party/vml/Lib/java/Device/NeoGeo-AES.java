package vml.device.snk.neogeo_68000;

/**
 * NeoGeo-68000 寄存器定义
 * 生成自: SNK/M68K/NeoGeo-68000
 * 版本: 1.0
 */
public final class NeoGeo_68000 {
    private NeoGeo_68000() {} // 工具类
    // CPU架构: MC68000, 32位, 12000000 Hz

    // 寄存器定义
    // Data Register 0
    public static final int D0_ADDR = (int)0x00;

    // Data Register 1
    public static final int D1_ADDR = (int)0x04;

    // Data Register 2
    public static final int D2_ADDR = (int)0x08;

    // Data Register 3
    public static final int D3_ADDR = (int)0x0C;

    // Data Register 4
    public static final int D4_ADDR = (int)0x10;

    // Data Register 5
    public static final int D5_ADDR = (int)0x14;

    // Data Register 6
    public static final int D6_ADDR = (int)0x18;

    // Data Register 7
    public static final int D7_ADDR = (int)0x1C;

    // Address Register 0
    public static final int A0_ADDR = (int)0x20;

    // Address Register 1
    public static final int A1_ADDR = (int)0x24;

    // Address Register 2
    public static final int A2_ADDR = (int)0x28;

    // Address Register 3
    public static final int A3_ADDR = (int)0x2C;

    // Address Register 4
    public static final int A4_ADDR = (int)0x30;

    // Address Register 5
    public static final int A5_ADDR = (int)0x34;

    // Address Register 6
    public static final int A6_ADDR = (int)0x38;

    // User Stack Pointer (USP)
    public static final int A7_ADDR = (int)0x3C;

    // Supervisor Stack Pointer (SSP)
    public static final int SP_ADDR = (int)0x3C;

    // Program Counter
    public static final int PC_ADDR = (int)0x40;

    // Status Register
    public static final int SR_ADDR = (int)0x44;
    public static final int SR_C = 0;  // Carry
    public static final int SR_V = 1;  // Overflow
    public static final int SR_Z = 2;  // Zero
    public static final int SR_N = 3;  // Negative
    public static final int SR_X = 4;  // Extend
    public static final int SR_I0 = 8;  // Interrupt Mask 0
    public static final int SR_I1 = 9;  // Interrupt Mask 1
    public static final int SR_I2 = 10;  // Interrupt Mask 2
    public static final int SR_M = 11;  // Master/Interrupt
    public static final int SR_S = 13;  // Supervisor/User
    public static final int SR_T0 = 14;  // Trace Mode 0
    public static final int SR_T1 = 15;  // Trace Mode 1

    // 内存段定义
    // Work RAM (64KB)
    public static final int WORK_RAM_START = (int)0x100000;
    public static final int WORK_RAM_END = (int)0x10FFFF;
    public static final int WORK_RAM_SIZE = 65536;

    // Backup SRAM (battery-backed, 64KB)
    public static final int BACKUP_RAM_START = (int)0x200000;
    public static final int BACKUP_RAM_END = (int)0x20FFFF;
    public static final int BACKUP_RAM_SIZE = 65536;

    // Fix Layer ROM (512KB)
    public static final int FIX_ROM_START = (int)0x000000;
    public static final int FIX_ROM_END = (int)0x07FFFF;
    public static final int FIX_ROM_SIZE = 524288;

    // Sprite ROM (up to 1MB)
    public static final int SPR_ROM_START = (int)0x400000;
    public static final int SPR_ROM_END = (int)0x4FFFFF;
    public static final int SPR_ROM_SIZE = 1048576;

    // Audio ROM (up to 64KB)
    public static final int AUDIO_ROM_START = (int)0x800000;
    public static final int AUDIO_ROM_END = (int)0x80FFFF;
    public static final int AUDIO_ROM_SIZE = 65536;

    // Cartridge ROM (up to 512KB, expandable)
    public static final int CART_ROM_START = (int)0xC00000;
    public static final int CART_ROM_END = (int)0xC7FFFF;
    public static final int CART_ROM_SIZE = 524288;

    // I/O Area (VDP, YM2610, Z80 port, etc.)
    public static final int IO_AREA_START = (int)0x300000;
    public static final int IO_AREA_END = (int)0x3FFFFF;
    public static final int IO_AREA_SIZE = 1048576;

    // Z80 Work RAM (2KB)
    public static final int Z80_RAM_START = (int)0x10000;
    public static final int Z80_RAM_END = (int)0x107FF;
    public static final int Z80_RAM_SIZE = 2048;

    // 外设定义
    // Z80 Audio Coprocessor @ 4MHz
    public static final int Z80_BASE = (int)0x300000;
    public static final int Z80_Z80_A = (int)0x00300000;
    public static final int Z80_Z80_F = (int)0x00300001;
    public static final int Z80_Z80_B = (int)0x00300002;
    public static final int Z80_Z80_C = (int)0x00300003;
    public static final int Z80_Z80_D = (int)0x00300004;
    public static final int Z80_Z80_E = (int)0x00300005;
    public static final int Z80_Z80_H = (int)0x00300006;
    public static final int Z80_Z80_L = (int)0x00300007;
    public static final int Z80_Z80_AF = (int)0x00300008;
    public static final int Z80_Z80_BC = (int)0x0030000A;
    public static final int Z80_Z80_DE = (int)0x0030000C;
    public static final int Z80_Z80_HL = (int)0x0030000E;
    public static final int Z80_Z80_IX = (int)0x00300010;
    public static final int Z80_Z80_IY = (int)0x00300012;
    public static final int Z80_Z80_SP = (int)0x00300014;
    public static final int Z80_Z80_PC = (int)0x00300016;
    public static final int Z80_Z80_I = (int)0x00300018;
    public static final int Z80_Z80_R = (int)0x00300019;
    public static final int Z80_Z80_IM = (int)0x0030001A;
    public static final int Z80_Z80_BUSREQ = (int)0x0030001E;
    public static final int Z80_Z80_RESET = (int)0x0030001F;

    // Yamaha YM2610 FM + ADPCM Audio Generator
    public static final int YM2610_BASE = (int)0x300000;
    public static final int YM2610_YM_ADDR_A0 = (int)0x00300000;
    public static final int YM2610_YM_DATA_A0 = (int)0x00300001;
    public static final int YM2610_YM_ADDR_A1 = (int)0x00300002;
    public static final int YM2610_YM_DATA_A1 = (int)0x00300003;
    public static final int YM2610_YM_ADDR_B0 = (int)0x00300004;
    public static final int YM2610_YM_DATA_B0 = (int)0x00300005;
    public static final int YM2610_YM_TEST = (int)0x00300008;
    public static final int YM2610_YM_FM_CH0_FREQ_L = (int)0x003000A0;
    public static final int YM2610_YM_FM_CH0_FREQ_H = (int)0x003000A4;
    public static final int YM2610_YM_FM_CH1_FREQ_L = (int)0x003000A1;
    public static final int YM2610_YM_FM_CH1_FREQ_H = (int)0x003000A5;
    public static final int YM2610_YM_FM_CH2_FREQ_L = (int)0x003000A2;
    public static final int YM2610_YM_FM_CH2_FREQ_H = (int)0x003000A6;
    public static final int YM2610_YM_FM_CH3_FREQ_L = (int)0x003000A3;
    public static final int YM2610_YM_FM_CH3_FREQ_H = (int)0x003000A7;
    public static final int YM2610_YM_FM_KEY_ON = (int)0x00300028;
    public static final int YM2610_YM_FM_CH0_ALG = (int)0x003000B0;
    public static final int YM2610_YM_FM_CH1_ALG = (int)0x003000B1;
    public static final int YM2610_YM_FM_CH2_ALG = (int)0x003000B2;
    public static final int YM2610_YM_FM_CH3_ALG = (int)0x003000B3;
    public static final int YM2610_YM_FM_TIMER_H = (int)0x00300024;
    public static final int YM2610_YM_FM_TIMER_L = (int)0x00300025;
    public static final int YM2610_YM_FM_TIMER_CTRL = (int)0x00300027;
    public static final int YM2610_YM_FM_TIMER_CTRL_TIMER_A_START = 0;  // Timer A Start
    public static final int YM2610_YM_FM_TIMER_CTRL_TIMER_B_START = 1;  // Timer B Start
    public static final int YM2610_YM_FM_TIMER_CTRL_LOAD_A = 2;  // Load Timer A
    public static final int YM2610_YM_FM_TIMER_CTRL_LOAD_B = 3;  // Load Timer B
    public static final int YM2610_YM_FM_TIMER_CTRL_IRQ_EN_A = 4;  // Timer A IRQ Enable
    public static final int YM2610_YM_FM_TIMER_CTRL_IRQ_EN_B = 5;  // Timer B IRQ Enable
    public static final int YM2610_YM_FM_TIMER_CTRL_CSM_MODE = 7;  // CSM Mode (auto Key-On after timer A)
    public static final int YM2610_YM_FM_CH0_DETUNE = (int)0x00300030;
    public static final int YM2610_YM_FM_CH0_MUL = (int)0x00300030;
    public static final int YM2610_YM_FM_CH0_TL = (int)0x00300040;
    public static final int YM2610_YM_FM_CH0_KS_AR = (int)0x00300050;
    public static final int YM2610_YM_FM_CH0_AM_DR = (int)0x00300060;
    public static final int YM2610_YM_FM_CH0_SR = (int)0x00300070;
    public static final int YM2610_YM_FM_CH0_RR_SL = (int)0x00300080;
    public static final int YM2610_YM_FM_CH0_SSG = (int)0x00300090;
    public static final int YM2610_YM_SSG_CHA_FREQ_L = (int)0x00300000;
    public static final int YM2610_YM_SSG_CHA_FREQ_H = (int)0x00300001;
    public static final int YM2610_YM_SSG_CHB_FREQ_L = (int)0x00300002;
    public static final int YM2610_YM_SSG_CHB_FREQ_H = (int)0x00300003;
    public static final int YM2610_YM_SSG_CHC_FREQ_L = (int)0x00300004;
    public static final int YM2610_YM_SSG_CHC_FREQ_H = (int)0x00300005;
    public static final int YM2610_YM_SSG_CHA_VOL = (int)0x00300008;
    public static final int YM2610_YM_SSG_CHB_VOL = (int)0x00300009;
    public static final int YM2610_YM_SSG_CHC_VOL = (int)0x0030000A;
    public static final int YM2610_YM_SSG_MIXER = (int)0x00300007;
    public static final int YM2610_YM_SSG_ENV_FREQ_L = (int)0x0030000B;
    public static final int YM2610_YM_SSG_ENV_FREQ_H = (int)0x0030000C;
    public static final int YM2610_YM_SSG_ENV_SHAPE = (int)0x0030000D;
    public static final int YM2610_YM_SSG_IO_A = (int)0x0030000E;
    public static final int YM2610_YM_SSG_IO_B = (int)0x0030000F;
    public static final int YM2610_YM_ADPCM_STATUS = (int)0x00300010;
    public static final int YM2610_YM_ADPCM_START = (int)0x00300011;
    public static final int YM2610_YM_ADPCM_END = (int)0x00300012;
    public static final int YM2610_YM_ADPCM_VOL_L = (int)0x00300013;
    public static final int YM2610_YM_ADPCM_VOL_R = (int)0x00300014;
    public static final int YM2610_YM_DELTA_N_L = (int)0x00300015;
    public static final int YM2610_YM_DELTA_N_H = (int)0x00300016;
    public static final int YM2610_YM_ADPCM_B_START = (int)0x00300018;
    public static final int YM2610_YM_ADPCM_B_END = (int)0x00300019;
    public static final int YM2610_YM_ADPCM_B_VOL = (int)0x0030001A;
    public static final int YM2610_YM_ADPCM_B_CTRL = (int)0x0030001B;

    // Neo Geo VDP (Video Display Processor)
    public static final int YGV628_BASE = (int)0x3C0000;
    public static final int YGV628_VRAM_ADDR_L = (int)0x003C0000;
    public static final int YGV628_VRAM_ADDR_H = (int)0x003C0001;
    public static final int YGV628_VRAM_DATA = (int)0x003C0002;
    public static final int YGV628_VRAM_READ = (int)0x003C0003;
    public static final int YGV628_CRAM_ADDR = (int)0x003C0004;
    public static final int YGV628_CRAM_DATA = (int)0x003C0005;
    public static final int YGV628_VDP_STATUS = (int)0x003C0006;
    public static final int YGV628_VDP_STATUS_VBLANK = 0;  // V-Blank Flag
    public static final int YGV628_VDP_STATUS_FIELD = 1;  // Field (0=even, 1=odd for interlace)
    public static final int YGV628_VDP_STATUS_ODD_FIELD = 1;  // Odd Field Flag
    public static final int YGV628_VDP_STATUS_DMA_BUSY = 2;  // DMA Busy
    public static final int YGV628_VDP_STATUS_SPRITE_OVERFLOW = 3;  // Sprite Overflow (more than 16 per line)
    public static final int YGV628_VDP_STATUS_SPRITE_COLLISION = 4;  // Sprite Collision
    public static final int YGV628_VDP_CTRL = (int)0x003C0007;
    public static final int YGV628_VDP_CTRL_VRAM_INC = 0;  // VRAM Auto-Increment (0=+1, 1=+2)
    public static final int YGV628_VDP_CTRL_ROW_SCROLL = 1;  // Row Scroll Mode
    public static final int YGV628_VDP_CTRL_COL_SCROLL = 2;  // Column Scroll Mode
    public static final int YGV628_VDP_CTRL_FIX_DISP = 3;  // Fix Layer Display
    public static final int YGV628_VDP_CTRL_SPR_DISP = 4;  // Sprite Layer Display
    public static final int YGV628_VDP_CTRL_SCROLL2_DISP = 5;  // Scroll Layer 2 Display
    public static final int YGV628_VDP_CTRL_SCROLL1_DISP = 6;  // Scroll Layer 1 Display
    public static final int YGV628_VDP_CTRL_DMA_ENABLE = 7;  // DMA Enable
    public static final int YGV628_SCROLL1_BASE = (int)0x003C0008;
    public static final int YGV628_SCROLL2_BASE = (int)0x003C000A;
    public static final int YGV628_SPR_BASE = (int)0x003C000C;
    public static final int YGV628_SPR_COUNT = (int)0x003C000E;
    public static final int YGV628_WINDOW_X = (int)0x003C0010;
    public static final int YGV628_WINDOW_Y = (int)0x003C0011;
    public static final int YGV628_WINDOW_W = (int)0x003C0012;
    public static final int YGV628_WINDOW_H = (int)0x003C0013;
    public static final int YGV628_LINE_SCROLL_L = (int)0x003C0014;
    public static final int YGV628_LINE_SCROLL_H = (int)0x003C0015;
    public static final int YGV628_RASTER_COMP = (int)0x003C0016;
    public static final int YGV628_H_TIMING = (int)0x003C0018;
    public static final int YGV628_V_TIMING = (int)0x003C0019;
    public static final int YGV628_DMA_SRC_L = (int)0x003C001A;
    public static final int YGV628_DMA_SRC_H = (int)0x003C001B;
    public static final int YGV628_DMA_SRC_B = (int)0x003C001C;
    public static final int YGV628_DMA_DEST_L = (int)0x003C001D;
    public static final int YGV628_DMA_DEST_H = (int)0x003C001E;
    public static final int YGV628_DMA_COUNT = (int)0x003C001F;

    // Neo Geo System Driver / Controller
    public static final int NEODRIVER_BASE = (int)0x310000;
    public static final int NEODRIVER_PDI0 = (int)0x00310000;
    public static final int NEODRIVER_PDI0_UP = 0;  // Up (0=pressed)
    public static final int NEODRIVER_PDI0_DOWN = 1;  // Down (0=pressed)
    public static final int NEODRIVER_PDI0_LEFT = 2;  // Left (0=pressed)
    public static final int NEODRIVER_PDI0_RIGHT = 3;  // Right (0=pressed)
    public static final int NEODRIVER_PDI0_A = 4;  // A Button (0=pressed)
    public static final int NEODRIVER_PDI0_B = 5;  // B Button (0=pressed)
    public static final int NEODRIVER_PDI0_C = 6;  // C Button (0=pressed)
    public static final int NEODRIVER_PDI0_D = 7;  // D Button (0=pressed)
    public static final int NEODRIVER_PDI1 = (int)0x00310001;
    public static final int NEODRIVER_PDI2 = (int)0x00310002;
    public static final int NEODRIVER_PDI3 = (int)0x00310003;
    public static final int NEODRIVER_PDO0 = (int)0x00310004;
    public static final int NEODRIVER_PDO1 = (int)0x00310005;
    public static final int NEODRIVER_PDO2 = (int)0x00310006;
    public static final int NEODRIVER_PDO3 = (int)0x00310007;
    public static final int NEODRIVER_DIPSEL1 = (int)0x00310008;
    public static final int NEODRIVER_DIPSEL1_COIN_SELECT = 0;  // Coin Select (0=common, 1=1 coin 1 credit)
    public static final int NEODRIVER_DIPSEL1_FREE_PLAY = 1;  // Free Play
    public static final int NEODRIVER_DIPSEL1_DEMO_SOUND = 2;  // Demo Sound
    public static final int NEODRIVER_DIPSEL1_CHIP_MODE = 3;  // Chip Mode (0=AES, 1=MVS)
    public static final int NEODRIVER_DIPSEL1_CONTROLLER_TYPE = 4;  // Controller Type (0=standard, 1=keyboard)
    public static final int NEODRIVER_DIPSEL2 = (int)0x00310009;
    public static final int NEODRIVER_DIPSEL3 = (int)0x0031000A;
    public static final int NEODRIVER_DIPSEL4 = (int)0x0031000B;
    public static final int NEODRIVER_SYSCTRL = (int)0x0031000C;
    public static final int NEODRIVER_SYSCTRL_RTSEL = 0;  // Real Time Switch Select
    public static final int NEODRIVER_SYSCTRL_RESERVED0 = 1;  // Reserved
    public static final int NEODRIVER_SYSCTRL_SCC = 2;  // System Clock Control
    public static final int NEODRIVER_SYSCTRL_PHEN = 3;  // PHEN (bus timing)
    public static final int NEODRIVER_SYSCTRL_PCK2 = 4;  // PCK2 (bus timing)
    public static final int NEODRIVER_SYSCTRL_PCK1 = 5;  // PCK1 (bus timing)
    public static final int NEODRIVER_SYSCTRL_CKDIV2 = 6;  // Clock Divide by 2
    public static final int NEODRIVER_SYSCTRL_FEFIX = 7;  // FE Fix
    public static final int NEODRIVER_IRQMASK = (int)0x0031000D;
    public static final int NEODRIVER_IRQMASK_VBLANK_MASK = 0;  // V-Blank Interrupt Mask
    public static final int NEODRIVER_IRQMASK_HBLANK_MASK = 1;  // H-Blank Interrupt Mask
    public static final int NEODRIVER_IRQMASK_VECTOR_IN_MASK = 2;  // Vector In (from Z80) Mask
    public static final int NEODRIVER_IRQMASK_SYSTEM_IN_MASK = 3;  // System Input (JAMMA) Mask
    public static final int NEODRIVER_IRQFLAG = (int)0x0031000E;
    public static final int NEODRIVER_SECAM_MODE = (int)0x0031000F;

    // Controller Port 1
    public static final int CONTROLLER1_BASE = (int)0x310000;
    public static final int CONTROLLER1_PDI0 = (int)0x00310000;

    // Controller Port 2
    public static final int CONTROLLER2_BASE = (int)0x310001;
    public static final int CONTROLLER2_PDI1 = (int)0x00310001;

    // Memory Card Interface
    public static final int MEMORY_CARD_BASE = (int)0x320000;
    public static final int MEMORY_CARD_CARD_DATA = (int)0x00320000;
    public static final int MEMORY_CARD_CARD_STATUS = (int)0x00320001;
    public static final int MEMORY_CARD_CARD_STATUS_INSERTED = 0;  // Card Inserted (0=yes)
    public static final int MEMORY_CARD_CARD_STATUS_WRITE_PROTECT = 1;  // Write Protected (0=yes)
    public static final int MEMORY_CARD_CARD_STATUS_READY = 2;  // Ready for I/O
    public static final int MEMORY_CARD_CARD_CTRL = (int)0x00320002;

    // Cartridge Bank Switching
    public static final int CART_BANK_BASE = (int)0x2FFFF0;
    public static final int CART_BANK_BANK_REG = (int)0x002FFFF0;

    // 中断向量定义
    public static final int IRQ_RESET_SP = 1;  // Reset Initial Stack Pointer
    public static final int IRQ_RESET_PC = 2;  // Reset Initial PC
    public static final int IRQ_BUS_ERROR = 3;  // Bus Error
    public static final int IRQ_ADDRESS_ERROR = 4;  // Address Error
    public static final int IRQ_ILLEGAL_INSTR = 5;  // Illegal Instruction
    public static final int IRQ_ZERO_DIVIDE = 6;  // Zero Divide
    public static final int IRQ_CHK_EXCEPTION = 7;  // CHK Exception
    public static final int IRQ_TRAPV = 8;  // TRAPV Exception
    public static final int IRQ_PRIVILEGE = 9;  // Privilege Violation
    public static final int IRQ_TRACE = 10;  // Trace
    public static final int IRQ_LINE_A = 11;  // Line 1010 Emulator
    public static final int IRQ_LINE_F = 12;  // Line 1111 Emulator
    public static final int IRQ_IRQ1 = 24;  // H-Blank / VDP Interrupt (raster)
    public static final int IRQ_IRQ2 = 25;  // V-Blank / Frame End Interrupt
    public static final int IRQ_IRQ3 = 26;  // System Controller / Z80 Vector In
    public static final int IRQ_IRQ4 = 27;  // JAMMA / System Input
    public static final int IRQ_IRQ5 = 28;  // Z80 Interrupt Request
    public static final int IRQ_TRAP0 = 32;  // TRAP #0 (system call)
    public static final int IRQ_TRAP1 = 33;  // TRAP #1

    // 引脚定义
    public static final int PIN_VCC = 1;  // Power Supply (5V)
    public static final int PIN_GND = 2;  // Ground
    public static final int PIN_CLK = 3;  // System Clock (12MHz for 68K)
    public static final int PIN_RESET = 4;  // Reset (active low)
    public static final int PIN_HALT = 5;  // Halt (stops CPU)
    public static final int PIN_NMI = 6;  // Non-Maskable Interrupt
    public static final int PIN_IPL0 = 7;  // Interrupt Priority Level 0
    public static final int PIN_IPL1 = 8;  // Interrupt Priority Level 1
    public static final int PIN_IPL2 = 9;  // Interrupt Priority Level 2
    public static final int PIN_DTACK = 10;  // Data Acknowledge (active low)
    public static final int PIN_BERR = 11;  // Bus Error (active low)
    public static final int PIN_BR = 12;  // Bus Request (active low)
    public static final int PIN_BG = 13;  // Bus Grant (active low)
    public static final int PIN_A0 = 14;  // Address Bus Bit 0
    public static final int PIN_A1 = 15;  // Address Bus Bit 1
    public static final int PIN_A2 = 16;  // Address Bus Bit 2
    public static final int PIN_A3 = 17;  // Address Bus Bit 3
    public static final int PIN_A4 = 18;  // Address Bus Bit 4
    public static final int PIN_A5 = 19;  // Address Bus Bit 5
    public static final int PIN_A6 = 20;  // Address Bus Bit 6
    public static final int PIN_A7 = 21;  // Address Bus Bit 7
    public static final int PIN_A8 = 22;  // Address Bus Bit 8
    public static final int PIN_A9 = 23;  // Address Bus Bit 9
    public static final int PIN_A10 = 24;  // Address Bus Bit 10
    public static final int PIN_A11 = 25;  // Address Bus Bit 11
    public static final int PIN_A12 = 26;  // Address Bus Bit 12
    public static final int PIN_A13 = 27;  // Address Bus Bit 13
    public static final int PIN_A14 = 28;  // Address Bus Bit 14
    public static final int PIN_A15 = 29;  // Address Bus Bit 15
    public static final int PIN_A16 = 30;  // Address Bus Bit 16
    public static final int PIN_A17 = 31;  // Address Bus Bit 17
    public static final int PIN_A18 = 32;  // Address Bus Bit 18
    public static final int PIN_A19 = 33;  // Address Bus Bit 19
    public static final int PIN_A20 = 34;  // Address Bus Bit 20
    public static final int PIN_A21 = 35;  // Address Bus Bit 21
    public static final int PIN_A22 = 36;  // Address Bus Bit 22
    public static final int PIN_A23 = 37;  // Address Bus Bit 23
    public static final int PIN_D0 = 38;  // Data Bus Bit 0
    public static final int PIN_D1 = 39;  // Data Bus Bit 1
    public static final int PIN_D2 = 40;  // Data Bus Bit 2
    public static final int PIN_D3 = 41;  // Data Bus Bit 3
    public static final int PIN_D4 = 42;  // Data Bus Bit 4
    public static final int PIN_D5 = 43;  // Data Bus Bit 5
    public static final int PIN_D6 = 44;  // Data Bus Bit 6
    public static final int PIN_D7 = 45;  // Data Bus Bit 7
    public static final int PIN_D8 = 46;  // Data Bus Bit 8
    public static final int PIN_D9 = 47;  // Data Bus Bit 9
    public static final int PIN_D10 = 48;  // Data Bus Bit 10
    public static final int PIN_D11 = 49;  // Data Bus Bit 11
    public static final int PIN_D12 = 50;  // Data Bus Bit 12
    public static final int PIN_D13 = 51;  // Data Bus Bit 13
    public static final int PIN_D14 = 52;  // Data Bus Bit 14
    public static final int PIN_D15 = 53;  // Data Bus Bit 15
    public static final int PIN_AS = 54;  // Address Strobe (active low)
    public static final int PIN_UDS = 55;  // Upper Data Strobe (active low)
    public static final int PIN_LDS = 56;  // Lower Data Strobe (active low)
    public static final int PIN_R_W = 57;  // Read/Write (1=Read, 0=Write)
    public static final int PIN_FC0 = 58;  // Function Code 0
    public static final int PIN_FC1 = 59;  // Function Code 1
    public static final int PIN_FC2 = 60;  // Function Code 2
    public static final int PIN_E = 61;  // E Clock (Enable, for Z80 sync)
    public static final int PIN_VPA = 62;  // Valid Peripheral Address (for Z80 I/O)
    public static final int PIN_VM = 63;  // Valid Memory (for Z80 memory access)
    public static final int PIN_BKGR = 64;  // Background Audio Mix (analog output)
    public static final int PIN_AUDIO_OUT = 65;  // Main Audio Output (Left)
    public static final int PIN_AUDIO_R = 66;  // Audio Right Channel
    public static final int PIN_VIDEO_R = 67;  // Video Output Red
    public static final int PIN_VIDEO_G = 68;  // Video Output Green
    public static final int PIN_VIDEO_B = 69;  // Video Output Blue
    public static final int PIN_SYNC = 70;  // Video Sync

    public static native void neogeo_68000_init();
}

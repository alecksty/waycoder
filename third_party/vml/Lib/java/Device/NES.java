package vml.device.nintendo.nintendo_entertainment_system;

/**
 * Nintendo Entertainment System 寄存器定义
 * 生成自: Nintendo/NES/Nintendo Entertainment System
 * 版本: 
 */
public final class Nintendo Entertainment System {
    private Nintendo Entertainment System() {} // 工具类
    // CPU架构: 6502, 0位, 0 Hz

    // 外设定义
    // Picture Processing Unit (Ricoh 2C02)
    public static final int PPU_BASE = (int);
    public static final int PPU_PPUCTRL = (int)0x00002000;
    public static final int PPU_PPUCTRL_NMI = 7;  // VBlank NMI enable
    public static final int PPU_PPUCTRL_MASTERSLAVE = 6;  // Master/slave select
    public static final int PPU_PPUCTRL_SPRITESIZE = 5;  // Sprite size (0=8x8, 1=8x16)
    public static final int PPU_PPUCTRL_BGPATTERN = 4;  // Background pattern table address
    public static final int PPU_PPUCTRL_SPRITEPATTERN = 3;  // Sprite pattern table address
    public static final int PPU_PPUCTRL_VRAMINCREMENT = 2;  // VRAM address increment (0=1, 1=32)
    public static final int PPU_PPUCTRL_NAMETABLE = 0;  // Nametable address
    public static final int PPU_PPUMASK = (int)0x00002001;
    public static final int PPU_PPUMASK_EMPHASIZEBLUE = 7;  // Emphasize blue
    public static final int PPU_PPUMASK_EMPHASIZEGREEN = 6;  // Emphasize green
    public static final int PPU_PPUMASK_EMPHASIZERED = 5;  // Emphasize red
    public static final int PPU_PPUMASK_SHOWSPRITES = 4;  // Show sprites
    public static final int PPU_PPUMASK_SHOWBACKGROUND = 3;  // Show background
    public static final int PPU_PPUMASK_SHOWLEFTSPRITES = 2;  // Show sprites in left 8 pixels
    public static final int PPU_PPUMASK_SHOWLEFTBACKGROUND = 1;  // Show background in left 8 pixels
    public static final int PPU_PPUMASK_GRAYSCALE = 0;  // Grayscale mode
    public static final int PPU_PPUSTATUS = (int)0x00002002;
    public static final int PPU_PPUSTATUS_VBLANK = 7;  // VBlank started
    public static final int PPU_PPUSTATUS_SPRITE0HIT = 6;  // Sprite 0 hit
    public static final int PPU_PPUSTATUS_SPRITEOVERFLOW = 5;  // Sprite overflow
    public static final int PPU_OAMADDR = (int)0x00002003;
    public static final int PPU_OAMDATA = (int)0x00002004;
    public static final int PPU_PPUSCROLL = (int)0x00002005;
    public static final int PPU_PPUADDR = (int)0x00002006;
    public static final int PPU_PPUDATA = (int)0x00002007;
    public static final int PPU_OAMDMA = (int)0x00004014;

    // Audio Processing Unit (Ricoh 2A03)
    public static final int APU_BASE = (int);
    public static final int APU_SQ1_VOL = (int)0x00004000;
    public static final int APU_SQ1_VOL_DUTY = 6;  // Duty cycle
    public static final int APU_SQ1_VOL_LENGTHCOUNTERHALT = 5;  // Length counter halt/envelope loop
    public static final int APU_SQ1_VOL_CONSTANTVOLUME = 4;  // Constant volume
    public static final int APU_SQ1_VOL_VOLUME = 0;  // Volume/envelope period
    public static final int APU_SQ1_SWEEP = (int)0x00004001;
    public static final int APU_SQ1_SWEEP_ENABLED = 7;  // Sweep enabled
    public static final int APU_SQ1_SWEEP_PERIOD = 4;  // Sweep period
    public static final int APU_SQ1_SWEEP_NEGATE = 3;  // Sweep negate
    public static final int APU_SQ1_SWEEP_SHIFT = 0;  // Sweep shift amount
    public static final int APU_SQ1_LO = (int)0x00004002;
    public static final int APU_SQ1_HI = (int)0x00004003;
    public static final int APU_SQ1_HI_LENGTHCOUNTER = 3;  // Length counter load
    public static final int APU_SQ1_HI_TIMERHIGH = 0;  // Timer high bits
    public static final int APU_SQ2_VOL = (int)0x00004004;
    public static final int APU_SQ2_SWEEP = (int)0x00004005;
    public static final int APU_SQ2_LO = (int)0x00004006;
    public static final int APU_SQ2_HI = (int)0x00004007;
    public static final int APU_TRI_LINEAR = (int)0x00004008;
    public static final int APU_TRI_LINEAR_CONTROL = 7;  // Length counter halt/linear counter control
    public static final int APU_TRI_LINEAR_PERIOD = 0;  // Linear counter load
    public static final int APU_TRI_LO = (int)0x0000400A;
    public static final int APU_TRI_HI = (int)0x0000400B;
    public static final int APU_NOISE_VOL = (int)0x0000400C;
    public static final int APU_NOISE_LO = (int)0x0000400E;
    public static final int APU_NOISE_LO_MODE = 7;  // Noise mode
    public static final int APU_NOISE_LO_PERIOD = 0;  // Noise period
    public static final int APU_NOISE_HI = (int)0x0000400F;
    public static final int APU_DMC_FREQ = (int)0x00004010;
    public static final int APU_DMC_FREQ_IRQ = 7;  // IRQ enable
    public static final int APU_DMC_FREQ_LOOP = 6;  // Loop flag
    public static final int APU_DMC_FREQ_FREQUENCY = 0;  // Frequency index
    public static final int APU_DMC_RAW = (int)0x00004011;
    public static final int APU_DMC_START = (int)0x00004012;
    public static final int APU_DMC_LEN = (int)0x00004013;
    public static final int APU_OAMDMA = (int)0x00004014;
    public static final int APU_APUSTATUS = (int)0x00004015;
    public static final int APU_APUSTATUS_DMCINTERRUPT = 7;  // DMC interrupt flag
    public static final int APU_APUSTATUS_FRAMEINTERRUPT = 6;  // Frame interrupt flag
    public static final int APU_APUSTATUS_DMCENABLED = 4;  // DMC enabled
    public static final int APU_APUSTATUS_NOISEENABLED = 3;  // Noise enabled
    public static final int APU_APUSTATUS_TRIANGLEENABLED = 2;  // Triangle enabled
    public static final int APU_APUSTATUS_SQUARE2ENABLED = 1;  // Square 2 enabled
    public static final int APU_APUSTATUS_SQUARE1ENABLED = 0;  // Square 1 enabled
    public static final int APU_APUFRAME = (int)0x00004017;
    public static final int APU_APUFRAME_MODE = 7;  // Frame counter mode
    public static final int APU_APUFRAME_IRQINHIBIT = 6;  // IRQ inhibit

    // Controller Interface
    public static final int CONTROLLER_BASE = (int);
    public static final int CONTROLLER_JOY1 = (int)0x00004016;
    public static final int CONTROLLER_JOY1_A = 7;  // A button
    public static final int CONTROLLER_JOY1_B = 6;  // B button
    public static final int CONTROLLER_JOY1_SELECT = 5;  // Select button
    public static final int CONTROLLER_JOY1_START = 4;  // Start button
    public static final int CONTROLLER_JOY1_UP = 3;  // Up direction
    public static final int CONTROLLER_JOY1_DOWN = 2;  // Down direction
    public static final int CONTROLLER_JOY1_LEFT = 1;  // Left direction
    public static final int CONTROLLER_JOY1_RIGHT = 0;  // Right direction
    public static final int CONTROLLER_JOY2 = (int)0x00004017;

    // Memory Mapper (Cartridge)
    public static final int MAPPER_BASE = (int);
    public static final int MAPPER_PRGROM = (int)0x00000000;
    public static final int MAPPER_CHRROM = (int)0x00000000;
    public static final int MAPPER_PRGRAM = (int)0x00000000;
    public static final int MAPPER_CHRRAM = (int)0x00000000;

    // 中断向量定义
    public static final int IRQ_NMI = 65530;  // Non-maskable interrupt (VBlank)
    public static final int IRQ_RESET = 65532;  // Reset vector
    public static final int IRQ_IRQ = 65534;  // Interrupt request

    public static native void nintendo_entertainment_system_init();
}

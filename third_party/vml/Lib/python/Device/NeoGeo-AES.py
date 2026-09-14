"""
NeoGeo-68000设备定义 - Python模块
生成自: SNK/M68K/NeoGeo-68000
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: SNK Neo Geo AES main processor - Motorola 68000 @ 12MHz + Z80 @ 4MHz (audio coprocessor)
CPU架构: MC68000
位宽: 32位
时钟频率: 12000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class NeoGeo_68000:
    """NeoGeo-68000设备类"""

    # 设备信息
    DEVICE_NAME = "NeoGeo-68000"
    MANUFACTURER = "SNK"
    FAMILY = "M68K"
    VERSION = "1.0"
    ARCHITECTURE = "MC68000"
    BITS = 32
    CLOCK_FREQUENCY = 12000000

    # 寄存器地址定义
    D0_ADDR = 0x00  # Data Register 0
    D1_ADDR = 0x04  # Data Register 1
    D2_ADDR = 0x08  # Data Register 2
    D3_ADDR = 0x0C  # Data Register 3
    D4_ADDR = 0x10  # Data Register 4
    D5_ADDR = 0x14  # Data Register 5
    D6_ADDR = 0x18  # Data Register 6
    D7_ADDR = 0x1C  # Data Register 7
    A0_ADDR = 0x20  # Address Register 0
    A1_ADDR = 0x24  # Address Register 1
    A2_ADDR = 0x28  # Address Register 2
    A3_ADDR = 0x2C  # Address Register 3
    A4_ADDR = 0x30  # Address Register 4
    A5_ADDR = 0x34  # Address Register 5
    A6_ADDR = 0x38  # Address Register 6
    A7_ADDR = 0x3C  # User Stack Pointer (USP)
    SP_ADDR = 0x3C  # Supervisor Stack Pointer (SSP)
    PC_ADDR = 0x40  # Program Counter
    SR_ADDR = 0x44  # Status Register
    SR_C_BIT = 0  # Carry
    SR_V_BIT = 1  # Overflow
    SR_Z_BIT = 2  # Zero
    SR_N_BIT = 3  # Negative
    SR_X_BIT = 4  # Extend
    SR_I0_BIT = 8  # Interrupt Mask 0
    SR_I1_BIT = 9  # Interrupt Mask 1
    SR_I2_BIT = 10  # Interrupt Mask 2
    SR_M_BIT = 11  # Master/Interrupt
    SR_S_BIT = 13  # Supervisor/User
    SR_T0_BIT = 14  # Trace Mode 0
    SR_T1_BIT = 15  # Trace Mode 1

    # 内存段定义
    WORK_RAM_START = 0x100000
    WORK_RAM_END = 0x10FFFF
    WORK_RAM_SIZE = 65536  # Work RAM (64KB)
    BACKUP_RAM_START = 0x200000
    BACKUP_RAM_END = 0x20FFFF
    BACKUP_RAM_SIZE = 65536  # Backup SRAM (battery-backed, 64KB)
    FIX_ROM_START = 0x000000
    FIX_ROM_END = 0x07FFFF
    FIX_ROM_SIZE = 524288  # Fix Layer ROM (512KB)
    SPR_ROM_START = 0x400000
    SPR_ROM_END = 0x4FFFFF
    SPR_ROM_SIZE = 1048576  # Sprite ROM (up to 1MB)
    AUDIO_ROM_START = 0x800000
    AUDIO_ROM_END = 0x80FFFF
    AUDIO_ROM_SIZE = 65536  # Audio ROM (up to 64KB)
    CART_ROM_START = 0xC00000
    CART_ROM_END = 0xC7FFFF
    CART_ROM_SIZE = 524288  # Cartridge ROM (up to 512KB, expandable)
    IO_AREA_START = 0x300000
    IO_AREA_END = 0x3FFFFF
    IO_AREA_SIZE = 1048576  # I/O Area (VDP, YM2610, Z80 port, etc.)
    Z80_RAM_START = 0x10000
    Z80_RAM_END = 0x107FF
    Z80_RAM_SIZE = 2048  # Z80 Work RAM (2KB)

    # 外设定义
    # Z80 Audio Coprocessor @ 4MHz
    Z80_BASE = 0x300000
    Z80_Z80_A_ADDR = 0x00
    Z80_Z80_F_ADDR = 0x01
    Z80_Z80_B_ADDR = 0x02
    Z80_Z80_C_ADDR = 0x03
    Z80_Z80_D_ADDR = 0x04
    Z80_Z80_E_ADDR = 0x05
    Z80_Z80_H_ADDR = 0x06
    Z80_Z80_L_ADDR = 0x07
    Z80_Z80_AF_ADDR = 0x08
    Z80_Z80_BC_ADDR = 0x0A
    Z80_Z80_DE_ADDR = 0x0C
    Z80_Z80_HL_ADDR = 0x0E
    Z80_Z80_IX_ADDR = 0x10
    Z80_Z80_IY_ADDR = 0x12
    Z80_Z80_SP_ADDR = 0x14
    Z80_Z80_PC_ADDR = 0x16
    Z80_Z80_I_ADDR = 0x18
    Z80_Z80_R_ADDR = 0x19
    Z80_Z80_IM_ADDR = 0x1A
    Z80_Z80_BUSREQ_ADDR = 0x1E
    Z80_Z80_RESET_ADDR = 0x1F
    # Yamaha YM2610 FM + ADPCM Audio Generator
    YM2610_BASE = 0x300000
    YM2610_YM_ADDR_A0_ADDR = 0x00
    YM2610_YM_DATA_A0_ADDR = 0x01
    YM2610_YM_ADDR_A1_ADDR = 0x02
    YM2610_YM_DATA_A1_ADDR = 0x03
    YM2610_YM_ADDR_B0_ADDR = 0x04
    YM2610_YM_DATA_B0_ADDR = 0x05
    YM2610_YM_TEST_ADDR = 0x08
    YM2610_YM_FM_CH0_FREQ_L_ADDR = 0xA0
    YM2610_YM_FM_CH0_FREQ_H_ADDR = 0xA4
    YM2610_YM_FM_CH1_FREQ_L_ADDR = 0xA1
    YM2610_YM_FM_CH1_FREQ_H_ADDR = 0xA5
    YM2610_YM_FM_CH2_FREQ_L_ADDR = 0xA2
    YM2610_YM_FM_CH2_FREQ_H_ADDR = 0xA6
    YM2610_YM_FM_CH3_FREQ_L_ADDR = 0xA3
    YM2610_YM_FM_CH3_FREQ_H_ADDR = 0xA7
    YM2610_YM_FM_KEY_ON_ADDR = 0x28
    YM2610_YM_FM_CH0_ALG_ADDR = 0xB0
    YM2610_YM_FM_CH1_ALG_ADDR = 0xB1
    YM2610_YM_FM_CH2_ALG_ADDR = 0xB2
    YM2610_YM_FM_CH3_ALG_ADDR = 0xB3
    YM2610_YM_FM_TIMER_H_ADDR = 0x24
    YM2610_YM_FM_TIMER_L_ADDR = 0x25
    YM2610_YM_FM_TIMER_CTRL_ADDR = 0x27
    YM2610_YM_FM_TIMER_CTRL_TIMER_A_START_BIT = 0  # Timer A Start
    YM2610_YM_FM_TIMER_CTRL_TIMER_B_START_BIT = 1  # Timer B Start
    YM2610_YM_FM_TIMER_CTRL_LOAD_A_BIT = 2  # Load Timer A
    YM2610_YM_FM_TIMER_CTRL_LOAD_B_BIT = 3  # Load Timer B
    YM2610_YM_FM_TIMER_CTRL_IRQ_EN_A_BIT = 4  # Timer A IRQ Enable
    YM2610_YM_FM_TIMER_CTRL_IRQ_EN_B_BIT = 5  # Timer B IRQ Enable
    YM2610_YM_FM_TIMER_CTRL_CSM_MODE_BIT = 7  # CSM Mode (auto Key-On after timer A)
    YM2610_YM_FM_CH0_DETUNE_ADDR = 0x30
    YM2610_YM_FM_CH0_MUL_ADDR = 0x30
    YM2610_YM_FM_CH0_TL_ADDR = 0x40
    YM2610_YM_FM_CH0_KS_AR_ADDR = 0x50
    YM2610_YM_FM_CH0_AM_DR_ADDR = 0x60
    YM2610_YM_FM_CH0_SR_ADDR = 0x70
    YM2610_YM_FM_CH0_RR_SL_ADDR = 0x80
    YM2610_YM_FM_CH0_SSG_ADDR = 0x90
    YM2610_YM_SSG_CHA_FREQ_L_ADDR = 0x00
    YM2610_YM_SSG_CHA_FREQ_H_ADDR = 0x01
    YM2610_YM_SSG_CHB_FREQ_L_ADDR = 0x02
    YM2610_YM_SSG_CHB_FREQ_H_ADDR = 0x03
    YM2610_YM_SSG_CHC_FREQ_L_ADDR = 0x04
    YM2610_YM_SSG_CHC_FREQ_H_ADDR = 0x05
    YM2610_YM_SSG_CHA_VOL_ADDR = 0x08
    YM2610_YM_SSG_CHB_VOL_ADDR = 0x09
    YM2610_YM_SSG_CHC_VOL_ADDR = 0x0A
    YM2610_YM_SSG_MIXER_ADDR = 0x07
    YM2610_YM_SSG_ENV_FREQ_L_ADDR = 0x0B
    YM2610_YM_SSG_ENV_FREQ_H_ADDR = 0x0C
    YM2610_YM_SSG_ENV_SHAPE_ADDR = 0x0D
    YM2610_YM_SSG_IO_A_ADDR = 0x0E
    YM2610_YM_SSG_IO_B_ADDR = 0x0F
    YM2610_YM_ADPCM_STATUS_ADDR = 0x10
    YM2610_YM_ADPCM_START_ADDR = 0x11
    YM2610_YM_ADPCM_END_ADDR = 0x12
    YM2610_YM_ADPCM_VOL_L_ADDR = 0x13
    YM2610_YM_ADPCM_VOL_R_ADDR = 0x14
    YM2610_YM_DELTA_N_L_ADDR = 0x15
    YM2610_YM_DELTA_N_H_ADDR = 0x16
    YM2610_YM_ADPCM_B_START_ADDR = 0x18
    YM2610_YM_ADPCM_B_END_ADDR = 0x19
    YM2610_YM_ADPCM_B_VOL_ADDR = 0x1A
    YM2610_YM_ADPCM_B_CTRL_ADDR = 0x1B
    # Neo Geo VDP (Video Display Processor)
    YGV628_BASE = 0x3C0000
    YGV628_VRAM_ADDR_L_ADDR = 0x00
    YGV628_VRAM_ADDR_H_ADDR = 0x01
    YGV628_VRAM_DATA_ADDR = 0x02
    YGV628_VRAM_READ_ADDR = 0x03
    YGV628_CRAM_ADDR_ADDR = 0x04
    YGV628_CRAM_DATA_ADDR = 0x05
    YGV628_VDP_STATUS_ADDR = 0x06
    YGV628_VDP_STATUS_VBLANK_BIT = 0  # V-Blank Flag
    YGV628_VDP_STATUS_FIELD_BIT = 1  # Field (0=even, 1=odd for interlace)
    YGV628_VDP_STATUS_ODD_FIELD_BIT = 1  # Odd Field Flag
    YGV628_VDP_STATUS_DMA_BUSY_BIT = 2  # DMA Busy
    YGV628_VDP_STATUS_SPRITE_OVERFLOW_BIT = 3  # Sprite Overflow (more than 16 per line)
    YGV628_VDP_STATUS_SPRITE_COLLISION_BIT = 4  # Sprite Collision
    YGV628_VDP_CTRL_ADDR = 0x07
    YGV628_VDP_CTRL_VRAM_INC_BIT = 0  # VRAM Auto-Increment (0=+1, 1=+2)
    YGV628_VDP_CTRL_ROW_SCROLL_BIT = 1  # Row Scroll Mode
    YGV628_VDP_CTRL_COL_SCROLL_BIT = 2  # Column Scroll Mode
    YGV628_VDP_CTRL_FIX_DISP_BIT = 3  # Fix Layer Display
    YGV628_VDP_CTRL_SPR_DISP_BIT = 4  # Sprite Layer Display
    YGV628_VDP_CTRL_SCROLL2_DISP_BIT = 5  # Scroll Layer 2 Display
    YGV628_VDP_CTRL_SCROLL1_DISP_BIT = 6  # Scroll Layer 1 Display
    YGV628_VDP_CTRL_DMA_ENABLE_BIT = 7  # DMA Enable
    YGV628_SCROLL1_BASE_ADDR = 0x08
    YGV628_SCROLL2_BASE_ADDR = 0x0A
    YGV628_SPR_BASE_ADDR = 0x0C
    YGV628_SPR_COUNT_ADDR = 0x0E
    YGV628_WINDOW_X_ADDR = 0x10
    YGV628_WINDOW_Y_ADDR = 0x11
    YGV628_WINDOW_W_ADDR = 0x12
    YGV628_WINDOW_H_ADDR = 0x13
    YGV628_LINE_SCROLL_L_ADDR = 0x14
    YGV628_LINE_SCROLL_H_ADDR = 0x15
    YGV628_RASTER_COMP_ADDR = 0x16
    YGV628_H_TIMING_ADDR = 0x18
    YGV628_V_TIMING_ADDR = 0x19
    YGV628_DMA_SRC_L_ADDR = 0x1A
    YGV628_DMA_SRC_H_ADDR = 0x1B
    YGV628_DMA_SRC_B_ADDR = 0x1C
    YGV628_DMA_DEST_L_ADDR = 0x1D
    YGV628_DMA_DEST_H_ADDR = 0x1E
    YGV628_DMA_COUNT_ADDR = 0x1F
    # Neo Geo System Driver / Controller
    NEODRIVER_BASE = 0x310000
    NEODRIVER_PDI0_ADDR = 0x00
    NEODRIVER_PDI0_UP_BIT = 0  # Up (0=pressed)
    NEODRIVER_PDI0_DOWN_BIT = 1  # Down (0=pressed)
    NEODRIVER_PDI0_LEFT_BIT = 2  # Left (0=pressed)
    NEODRIVER_PDI0_RIGHT_BIT = 3  # Right (0=pressed)
    NEODRIVER_PDI0_A_BIT = 4  # A Button (0=pressed)
    NEODRIVER_PDI0_B_BIT = 5  # B Button (0=pressed)
    NEODRIVER_PDI0_C_BIT = 6  # C Button (0=pressed)
    NEODRIVER_PDI0_D_BIT = 7  # D Button (0=pressed)
    NEODRIVER_PDI1_ADDR = 0x01
    NEODRIVER_PDI2_ADDR = 0x02
    NEODRIVER_PDI3_ADDR = 0x03
    NEODRIVER_PDO0_ADDR = 0x04
    NEODRIVER_PDO1_ADDR = 0x05
    NEODRIVER_PDO2_ADDR = 0x06
    NEODRIVER_PDO3_ADDR = 0x07
    NEODRIVER_DIPSEL1_ADDR = 0x08
    NEODRIVER_DIPSEL1_COIN_SELECT_BIT = 0  # Coin Select (0=common, 1=1 coin 1 credit)
    NEODRIVER_DIPSEL1_FREE_PLAY_BIT = 1  # Free Play
    NEODRIVER_DIPSEL1_DEMO_SOUND_BIT = 2  # Demo Sound
    NEODRIVER_DIPSEL1_CHIP_MODE_BIT = 3  # Chip Mode (0=AES, 1=MVS)
    NEODRIVER_DIPSEL1_CONTROLLER_TYPE_BIT = 4  # Controller Type (0=standard, 1=keyboard)
    NEODRIVER_DIPSEL2_ADDR = 0x09
    NEODRIVER_DIPSEL3_ADDR = 0x0A
    NEODRIVER_DIPSEL4_ADDR = 0x0B
    NEODRIVER_SYSCTRL_ADDR = 0x0C
    NEODRIVER_SYSCTRL_RTSEL_BIT = 0  # Real Time Switch Select
    NEODRIVER_SYSCTRL_RESERVED0_BIT = 1  # Reserved
    NEODRIVER_SYSCTRL_SCC_BIT = 2  # System Clock Control
    NEODRIVER_SYSCTRL_PHEN_BIT = 3  # PHEN (bus timing)
    NEODRIVER_SYSCTRL_PCK2_BIT = 4  # PCK2 (bus timing)
    NEODRIVER_SYSCTRL_PCK1_BIT = 5  # PCK1 (bus timing)
    NEODRIVER_SYSCTRL_CKDIV2_BIT = 6  # Clock Divide by 2
    NEODRIVER_SYSCTRL_FEFIX_BIT = 7  # FE Fix
    NEODRIVER_IRQMASK_ADDR = 0x0D
    NEODRIVER_IRQMASK_VBLANK_MASK_BIT = 0  # V-Blank Interrupt Mask
    NEODRIVER_IRQMASK_HBLANK_MASK_BIT = 1  # H-Blank Interrupt Mask
    NEODRIVER_IRQMASK_VECTOR_IN_MASK_BIT = 2  # Vector In (from Z80) Mask
    NEODRIVER_IRQMASK_SYSTEM_IN_MASK_BIT = 3  # System Input (JAMMA) Mask
    NEODRIVER_IRQFLAG_ADDR = 0x0E
    NEODRIVER_SECAM_MODE_ADDR = 0x0F
    # Controller Port 1
    CONTROLLER1_BASE = 0x310000
    CONTROLLER1_PDI0_ADDR = 0x00
    # Controller Port 2
    CONTROLLER2_BASE = 0x310001
    CONTROLLER2_PDI1_ADDR = 0x00
    # Memory Card Interface
    MEMORY_CARD_BASE = 0x320000
    MEMORY_CARD_CARD_DATA_ADDR = 0x00
    MEMORY_CARD_CARD_STATUS_ADDR = 0x01
    MEMORY_CARD_CARD_STATUS_INSERTED_BIT = 0  # Card Inserted (0=yes)
    MEMORY_CARD_CARD_STATUS_WRITE_PROTECT_BIT = 1  # Write Protected (0=yes)
    MEMORY_CARD_CARD_STATUS_READY_BIT = 2  # Ready for I/O
    MEMORY_CARD_CARD_CTRL_ADDR = 0x02
    # Cartridge Bank Switching
    CART_BANK_BASE = 0x2FFFF0
    CART_BANK_BANK_REG_ADDR = 0x00

    # 中断向量定义
    INT_RESET_SP = 1  # Reset Initial Stack Pointer
    INT_RESET_PC = 2  # Reset Initial PC
    INT_BUS_ERROR = 3  # Bus Error
    INT_ADDRESS_ERROR = 4  # Address Error
    INT_ILLEGAL_INSTR = 5  # Illegal Instruction
    INT_ZERO_DIVIDE = 6  # Zero Divide
    INT_CHK_EXCEPTION = 7  # CHK Exception
    INT_TRAPV = 8  # TRAPV Exception
    INT_PRIVILEGE = 9  # Privilege Violation
    INT_TRACE = 10  # Trace
    INT_LINE_A = 11  # Line 1010 Emulator
    INT_LINE_F = 12  # Line 1111 Emulator
    INT_IRQ1 = 24  # H-Blank / VDP Interrupt (raster)
    INT_IRQ2 = 25  # V-Blank / Frame End Interrupt
    INT_IRQ3 = 26  # System Controller / Z80 Vector In
    INT_IRQ4 = 27  # JAMMA / System Input
    INT_IRQ5 = 28  # Z80 Interrupt Request
    INT_TRAP0 = 32  # TRAP #0 (system call)
    INT_TRAP1 = 33  # TRAP #1

    # 引脚定义
    PIN_VCC = 1  # Power Supply (5V)
    PIN_GND = 2  # Ground
    PIN_CLK = 3  # System Clock (12MHz for 68K)
    PIN_RESET = 4  # Reset (active low)
    PIN_HALT = 5  # Halt (stops CPU)
    PIN_NMI = 6  # Non-Maskable Interrupt
    PIN_IPL0 = 7  # Interrupt Priority Level 0
    PIN_IPL1 = 8  # Interrupt Priority Level 1
    PIN_IPL2 = 9  # Interrupt Priority Level 2
    PIN_DTACK = 10  # Data Acknowledge (active low)
    PIN_BERR = 11  # Bus Error (active low)
    PIN_BR = 12  # Bus Request (active low)
    PIN_BG = 13  # Bus Grant (active low)
    PIN_A0 = 14  # Address Bus Bit 0
    PIN_A1 = 15  # Address Bus Bit 1
    PIN_A2 = 16  # Address Bus Bit 2
    PIN_A3 = 17  # Address Bus Bit 3
    PIN_A4 = 18  # Address Bus Bit 4
    PIN_A5 = 19  # Address Bus Bit 5
    PIN_A6 = 20  # Address Bus Bit 6
    PIN_A7 = 21  # Address Bus Bit 7
    PIN_A8 = 22  # Address Bus Bit 8
    PIN_A9 = 23  # Address Bus Bit 9
    PIN_A10 = 24  # Address Bus Bit 10
    PIN_A11 = 25  # Address Bus Bit 11
    PIN_A12 = 26  # Address Bus Bit 12
    PIN_A13 = 27  # Address Bus Bit 13
    PIN_A14 = 28  # Address Bus Bit 14
    PIN_A15 = 29  # Address Bus Bit 15
    PIN_A16 = 30  # Address Bus Bit 16
    PIN_A17 = 31  # Address Bus Bit 17
    PIN_A18 = 32  # Address Bus Bit 18
    PIN_A19 = 33  # Address Bus Bit 19
    PIN_A20 = 34  # Address Bus Bit 20
    PIN_A21 = 35  # Address Bus Bit 21
    PIN_A22 = 36  # Address Bus Bit 22
    PIN_A23 = 37  # Address Bus Bit 23
    PIN_D0 = 38  # Data Bus Bit 0
    PIN_D1 = 39  # Data Bus Bit 1
    PIN_D2 = 40  # Data Bus Bit 2
    PIN_D3 = 41  # Data Bus Bit 3
    PIN_D4 = 42  # Data Bus Bit 4
    PIN_D5 = 43  # Data Bus Bit 5
    PIN_D6 = 44  # Data Bus Bit 6
    PIN_D7 = 45  # Data Bus Bit 7
    PIN_D8 = 46  # Data Bus Bit 8
    PIN_D9 = 47  # Data Bus Bit 9
    PIN_D10 = 48  # Data Bus Bit 10
    PIN_D11 = 49  # Data Bus Bit 11
    PIN_D12 = 50  # Data Bus Bit 12
    PIN_D13 = 51  # Data Bus Bit 13
    PIN_D14 = 52  # Data Bus Bit 14
    PIN_D15 = 53  # Data Bus Bit 15
    PIN_AS = 54  # Address Strobe (active low)
    PIN_UDS = 55  # Upper Data Strobe (active low)
    PIN_LDS = 56  # Lower Data Strobe (active low)
    PIN_R_W = 57  # Read/Write (1=Read, 0=Write)
    PIN_FC0 = 58  # Function Code 0
    PIN_FC1 = 59  # Function Code 1
    PIN_FC2 = 60  # Function Code 2
    PIN_E = 61  # E Clock (Enable, for Z80 sync)
    PIN_VPA = 62  # Valid Peripheral Address (for Z80 I/O)
    PIN_VM = 63  # Valid Memory (for Z80 memory access)
    PIN_BKGR = 64  # Background Audio Mix (analog output)
    PIN_AUDIO_OUT = 65  # Main Audio Output (Left)
    PIN_AUDIO_R = 66  # Audio Right Channel
    PIN_VIDEO_R = 67  # Video Output Red
    PIN_VIDEO_G = 68  # Video Output Green
    PIN_VIDEO_B = 69  # Video Output Blue
    PIN_SYNC = 70  # Video Sync

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["D0"] = {
            "address": 0x00,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 0",
            "value": 0
        }
        self._registers["D1"] = {
            "address": 0x04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 1",
            "value": 0
        }
        self._registers["D2"] = {
            "address": 0x08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 2",
            "value": 0
        }
        self._registers["D3"] = {
            "address": 0x0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 3",
            "value": 0
        }
        self._registers["D4"] = {
            "address": 0x10,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 4",
            "value": 0
        }
        self._registers["D5"] = {
            "address": 0x14,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 5",
            "value": 0
        }
        self._registers["D6"] = {
            "address": 0x18,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 6",
            "value": 0
        }
        self._registers["D7"] = {
            "address": 0x1C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Data Register 7",
            "value": 0
        }
        self._registers["A0"] = {
            "address": 0x20,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 0",
            "value": 0
        }
        self._registers["A1"] = {
            "address": 0x24,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 1",
            "value": 0
        }
        self._registers["A2"] = {
            "address": 0x28,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 2",
            "value": 0
        }
        self._registers["A3"] = {
            "address": 0x2C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 3",
            "value": 0
        }
        self._registers["A4"] = {
            "address": 0x30,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 4",
            "value": 0
        }
        self._registers["A5"] = {
            "address": 0x34,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 5",
            "value": 0
        }
        self._registers["A6"] = {
            "address": 0x38,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Address Register 6",
            "value": 0
        }
        self._registers["A7"] = {
            "address": 0x3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "User Stack Pointer (USP)",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Supervisor Stack Pointer (SSP)",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x40,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["SR"] = {
            "address": 0x44,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Status Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["Z80"] = {
            "base": 0x300000,
            "type": "audio_cpu",
            "description": "Z80 Audio Coprocessor @ 4MHz",
            "registers": {
                "Z80_A": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_F": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_B": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_C": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_D": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_E": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_H": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_L": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_AF_": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_BC_": {
                    "address": 0x0A,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_DE_": {
                    "address": 0x0C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_HL_": {
                    "address": 0x0E,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_IX": {
                    "address": 0x10,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_IY": {
                    "address": 0x12,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_SP": {
                    "address": 0x14,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_PC": {
                    "address": 0x16,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "Z80_I": {
                    "address": 0x18,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_R": {
                    "address": 0x19,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_IM": {
                    "address": 0x1A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_BUSREQ": {
                    "address": 0x1E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_RESET": {
                    "address": 0x1F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["YM2610"] = {
            "base": 0x300000,
            "type": "audio",
            "description": "Yamaha YM2610 FM + ADPCM Audio Generator",
            "registers": {
                "YM_ADDR_A0": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_DATA_A0": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADDR_A1": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_DATA_A1": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADDR_B0": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_DATA_B0": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_TEST": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_FREQ_L": {
                    "address": 0xA0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_FREQ_H": {
                    "address": 0xA4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH1_FREQ_L": {
                    "address": 0xA1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH1_FREQ_H": {
                    "address": 0xA5,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH2_FREQ_L": {
                    "address": 0xA2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH2_FREQ_H": {
                    "address": 0xA6,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH3_FREQ_L": {
                    "address": 0xA3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH3_FREQ_H": {
                    "address": 0xA7,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_KEY_ON": {
                    "address": 0x28,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_ALG": {
                    "address": 0xB0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH1_ALG": {
                    "address": 0xB1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH2_ALG": {
                    "address": 0xB2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH3_ALG": {
                    "address": 0xB3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_TIMER_H": {
                    "address": 0x24,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_TIMER_L": {
                    "address": 0x25,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_TIMER_CTRL": {
                    "address": 0x27,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_DETune": {
                    "address": 0x30,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_MUL": {
                    "address": 0x30,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_TL": {
                    "address": 0x40,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_KS_AR": {
                    "address": 0x50,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_AM_DR": {
                    "address": 0x60,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_SR": {
                    "address": 0x70,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_RR_SL": {
                    "address": 0x80,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_FM_CH0_SSG": {
                    "address": 0x90,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_CHA_FREQ_L": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_CHA_FREQ_H": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_CHB_FREQ_L": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_CHB_FREQ_H": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_CHC_FREQ_L": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_CHC_FREQ_H": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_CHA_VOL": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_CHB_VOL": {
                    "address": 0x09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_CHC_VOL": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_MIXER": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_ENV_FREQ_L": {
                    "address": 0x0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_ENV_FREQ_H": {
                    "address": 0x0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_ENV_SHAPE": {
                    "address": 0x0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_IO_A": {
                    "address": 0x0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_SSG_IO_B": {
                    "address": 0x0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADPCM_STATUS": {
                    "address": 0x10,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADPCM_START": {
                    "address": 0x11,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADPCM_END": {
                    "address": 0x12,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADPCM_VOL_L": {
                    "address": 0x13,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADPCM_VOL_R": {
                    "address": 0x14,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_DELTA_N_L": {
                    "address": 0x15,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_DELTA_N_H": {
                    "address": 0x16,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADPCM_B_START": {
                    "address": 0x18,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADPCM_B_END": {
                    "address": 0x19,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADPCM_B_VOL": {
                    "address": 0x1A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YM_ADPCM_B_CTRL": {
                    "address": 0x1B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["YGV628"] = {
            "base": 0x3C0000,
            "type": "video",
            "description": "Neo Geo VDP (Video Display Processor)",
            "registers": {
                "VRAM_ADDR_L": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VRAM_ADDR_H": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VRAM_DATA": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VRAM_READ": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRAM_ADDR": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRAM_DATA": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_STATUS": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_CTRL": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCROLL1_BASE": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SCROLL2_BASE": {
                    "address": 0x0A,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SPR_BASE": {
                    "address": 0x0C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SPR_COUNT": {
                    "address": 0x0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WINDOW_X": {
                    "address": 0x10,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WINDOW_Y": {
                    "address": 0x11,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WINDOW_W": {
                    "address": 0x12,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WINDOW_H": {
                    "address": 0x13,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LINE_SCROLL_L": {
                    "address": 0x14,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LINE_SCROLL_H": {
                    "address": 0x15,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RASTER_COMP": {
                    "address": 0x16,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "H_TIMING": {
                    "address": 0x18,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V_TIMING": {
                    "address": 0x19,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMA_SRC_L": {
                    "address": 0x1A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMA_SRC_H": {
                    "address": 0x1B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMA_SRC_B": {
                    "address": 0x1C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMA_DEST_L": {
                    "address": 0x1D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMA_DEST_H": {
                    "address": 0x1E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMA_COUNT": {
                    "address": 0x1F,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["NEODRIVER"] = {
            "base": 0x310000,
            "type": "system",
            "description": "Neo Geo System Driver / Controller",
            "registers": {
                "PDI0": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PDI1": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PDI2": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PDI3": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PDO0": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PDO1": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PDO2": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PDO3": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIPSEL1": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIPSEL2": {
                    "address": 0x09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIPSEL3": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DIPSEL4": {
                    "address": 0x0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SYSCTRL": {
                    "address": 0x0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IRQMASK": {
                    "address": 0x0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IRQFLAG": {
                    "address": 0x0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SECAM_MODE": {
                    "address": 0x0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CONTROLLER1"] = {
            "base": 0x310000,
            "type": "input",
            "description": "Controller Port 1",
            "registers": {
                "PDI0": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CONTROLLER2"] = {
            "base": 0x310001,
            "type": "input",
            "description": "Controller Port 2",
            "registers": {
                "PDI1": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["MEMORY_CARD"] = {
            "base": 0x320000,
            "type": "storage",
            "description": "Memory Card Interface",
            "registers": {
                "CARD_DATA": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CARD_STATUS": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CARD_CTRL": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CART_BANK"] = {
            "base": 0x2FFFF0,
            "type": "memory",
            "description": "Cartridge Bank Switching",
            "registers": {
                "BANK_REG": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }

    def read_register(self, name: str) -> int:
        """读取寄存器值"""
        if name in self._registers:
            return self._registers[name]["value"]
        raise KeyError(f"寄存器 {name} 不存在")

    def write_register(self, name: str, value: int):
        """写入寄存器值"""
        if name in self._registers:
            reg = self._registers[name]
            max_value = (1 << (reg["size"] * 8)) - 1
            if value < 0 or value > max_value:
                raise ValueError(f"值 {value} 超出范围 [0, {max_value}]")
            reg["value"] = value
        else:
            raise KeyError(f"寄存器 {name} 不存在")

    def set_bit(self, register_name: str, bit: int, value: bool):
        """设置寄存器位"""
        if register_name in self._registers:
            reg = self._registers[register_name]
            if value:
                reg["value"] |= (1 << bit)
            else:
                reg["value"] &= ~(1 << bit)
        else:
            raise KeyError(f"寄存器 {register_name} 不存在")

    def get_bit(self, register_name: str, bit: int) -> bool:
        """获取寄存器位"""
        if register_name in self._registers:
            reg = self._registers[register_name]
            return (reg["value"] >> bit) & 1 == 1
        raise KeyError(f"寄存器 {register_name} 不存在")

    def get_device_info(self) -> dict:
        """获取设备信息"""
        return {
            "name": self.DEVICE_NAME,
            "manufacturer": self.MANUFACTURER,
            "family": self.FAMILY,
            "version": self.VERSION,
            "architecture": self.ARCHITECTURE,
            "bits": self.BITS,
            "clock_frequency": self.CLOCK_FREQUENCY
        }

    def get_register_info(self, name: str) -> Optional[dict]:
        """获取寄存器信息"""
        return self._registers.get(name)

    def get_peripheral_info(self, name: str) -> Optional[dict]:
        """获取外设信息"""
        return self._peripherals.get(name)

    def reset(self):
        """重置设备"""
        for reg in self._registers.values():
            reg["value"] = 0
        for peripheral in self._peripherals.values():
            for reg in peripheral["registers"].values():
                reg["value"] = 0

    def __str__(self) -> str:
        """字符串表示"""
        info = self.get_device_info()
        return f"NeoGeo_68000({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = NeoGeo_68000()
    print(f"设备: {device}")
    print(f"设备信息: {device.get_device_info()}")
    print()
    
    # 演示寄存器操作
    if device.Cpu.Registers.RegisterList.Count > 0:
        first_reg = device.Cpu.Registers.RegisterList[0].Name
        print(f"第一个寄存器: {first_reg}")
        device.write_register(first_reg, 0x55)
        value = device.read_register(first_reg)
        print(f"读取值: 0x{value:X}")

"""
Ricoh-5A22设备定义 - Python模块
生成自: Ricoh/MOS-6502/Ricoh-5A22
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: Super Nintendo Entertainment System (SNES) main processor - 16-bit 6502 variant with enhanced capabilities
CPU架构: Ricoh-5A22
位宽: 16位
时钟频率: 3580000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Ricoh_5A22:
    """Ricoh-5A22设备类"""

    # 设备信息
    DEVICE_NAME = "Ricoh-5A22"
    MANUFACTURER = "Ricoh"
    FAMILY = "MOS-6502"
    VERSION = "1.0"
    ARCHITECTURE = "Ricoh-5A22"
    BITS = 16
    CLOCK_FREQUENCY = 3580000

    # 寄存器地址定义
    A_ADDR = 0x00  # Accumulator (8-bit, expandable to 16-bit)
    B_ADDR = 0x01  # Accumulator high byte when 16-bit
    X_ADDR = 0x02  # X Index Register (8/16-bit)
    Y_ADDR = 0x03  # Y Index Register (8/16-bit)
    SP_ADDR = 0x04  # Stack Pointer (8-bit, banked)
    PC_ADDR = 0x06  # Program Counter (16-bit)
    D_ADDR = 0x08  # Direct Page Register
    P_ADDR = 0x0A  # Processor Status
    P_N_BIT = 0  # Negative
    P_V_BIT = 1  # Overflow
    P_M_BIT = 2  # Memory/Accumulator Select (0=16-bit, 1=8-bit)
    P_X_BIT = 3  # Index Select (0=16-bit, 1=8-bit)
    P_B_BIT = 4  # Break
    P_D_BIT = 5  # Decimal Mode
    P_I_BIT = 6  # Interrupt Disable
    P_Z_BIT = 7  # Zero
    P_C_BIT = 8  # Carry

    # 内存段定义
    WRAM_START = 0x7E0000
    WRAM_END = 0x7FFFFF
    WRAM_SIZE = 131072  # Work RAM (128KB internal)
    SRAM_START = 0x600000
    SRAM_END = 0x6FFFFF
    SRAM_SIZE = 1048576  # Save RAM / Cartridge SRAM
    CART_ROM_START = 0x800000
    CART_ROM_END = 0xFFFFFF
    CART_ROM_SIZE = 8388608  # Cartridge ROM (LoROM/HiROM mapping)
    PPU1_REGS_START = 0x2100
    PPU1_REGS_END = 0x213F
    PPU1_REGS_SIZE = 64  # PPU1 Registers (background)
    PPU2_REGS_START = 0x2140
    PPU2_REGS_END = 0x217F
    PPU2_REGS_SIZE = 64  # PPU2 Registers (sprites)
    PPU3_REGS_START = 0x2180
    PPU3_REGS_END = 0x21FF
    PPU3_REGS_SIZE = 128  # PPU3 Registers (extra)
    APU_REGS_START = 0x2140
    APU_REGS_END = 0x217F
    APU_REGS_SIZE = 64  # APU I/O Registers
    CPU_IO_START = 0x2000
    CPU_IO_END = 0x20FF
    CPU_IO_SIZE = 256  # CPU I/O Ports
    DMA_REGS_START = 0x4300
    DMA_REGS_END = 0x437F
    DMA_REGS_SIZE = 128  # DMA Channel Registers
    HDMA_REGS_START = 0x4380
    HDMA_REGS_END = 0x43FF
    HDMA_REGS_SIZE = 128  # HDMA Channel Registers

    # 外设定义
    # Picture Processing Unit 1 - Background Rendering
    PPU1_BASE = 0x2100
    PPU1_INIDISP_ADDR = 0x2100
    PPU1_OBSEL_ADDR = 0x2101
    PPU1_OAMADDL_ADDR = 0x2102
    PPU1_OAMADDH_ADDR = 0x2103
    PPU1_OAMDATA_ADDR = 0x2104
    PPU1_BGMODE_ADDR = 0x2105
    PPU1_MOSAIC_ADDR = 0x2106
    PPU1_BG1SC_ADDR = 0x2107
    PPU1_BG2SC_ADDR = 0x2108
    PPU1_BG3SC_ADDR = 0x2109
    PPU1_BG4SC_ADDR = 0x210A
    PPU1_BG12NBA_ADDR = 0x210B
    PPU1_BG34NBA_ADDR = 0x210C
    PPU1_BG1HOFS_ADDR = 0x210D
    PPU1_BG1VOFS_ADDR = 0x210E
    PPU1_BG2HOFS_ADDR = 0x210F
    PPU1_BG2VOFS_ADDR = 0x2110
    PPU1_BG3HOFS_ADDR = 0x2111
    PPU1_BG3VOFS_ADDR = 0x2112
    PPU1_BG4HOFS_ADDR = 0x2113
    PPU1_BG4VOFS_ADDR = 0x2114
    PPU1_VMAIN_ADDR = 0x2115
    PPU1_VMADDL_ADDR = 0x2116
    PPU1_VMADDH_ADDR = 0x2117
    PPU1_VMDATAL_ADDR = 0x2118
    PPU1_VMDATAH_ADDR = 0x2119
    PPU1_M7SEL_ADDR = 0x211A
    PPU1_M7A_ADDR = 0x211B
    PPU1_M7B_ADDR = 0x211C
    PPU1_M7C_ADDR = 0x211D
    PPU1_M7D_ADDR = 0x211E
    PPU1_M7X_ADDR = 0x211F
    PPU1_M7Y_ADDR = 0x2120
    PPU1_CGADD_ADDR = 0x2121
    PPU1_CGDATA_ADDR = 0x2122
    PPU1_W12SEL_ADDR = 0x2123
    PPU1_W34SEL_ADDR = 0x2124
    PPU1_WOBJSEL_ADDR = 0x2125
    PPU1_WH0_ADDR = 0x2126
    PPU1_WH1_ADDR = 0x2127
    PPU1_WH2_ADDR = 0x2128
    PPU1_WH3_ADDR = 0x2129
    PPU1_WBGLOG_ADDR = 0x212A
    PPU1_WOBJLOG_ADDR = 0x212B
    PPU1_TM_ADDR = 0x212C
    PPU1_TS_ADDR = 0x212D
    PPU1_TMW_ADDR = 0x212E
    PPU1_TSW_ADDR = 0x212F
    PPU1_CGSWSEL_ADDR = 0x2130
    PPU1_CGADSUB_ADDR = 0x2131
    PPU1_SETINI_ADDR = 0x2133
    # Picture Processing Unit 2 - Sprite Rendering
    PPU2_BASE = 0x2140
    PPU2_OAMDATAREAD_ADDR = 0x2138
    PPU2_VMDATAREAD_ADDR = 0x2139
    PPU2_VMDATAHREAD_ADDR = 0x213A
    PPU2_CGDATAREAD_ADDR = 0x213B
    PPU2_OPHCT_ADDR = 0x213C
    PPU2_OPVCT_ADDR = 0x213D
    PPU2_STAT78_ADDR = 0x213F
    # Sony SPC700 Audio CPU (8-bit)
    SPC700_BASE = 0x00
    SPC700_PC_ADDR = 0x00
    SPC700_A_ADDR = 0x02
    SPC700_X_ADDR = 0x03
    SPC700_Y_ADDR = 0x04
    SPC700_SP_ADDR = 0x05
    SPC700_PSW_ADDR = 0x06
    SPC700_TEST_ADDR = 0x0F
    # S-DSP Audio DSP (8-channel ADPCM)
    DSP_BASE = 0x00
    DSP_MVOL_L_ADDR = 0x0C
    DSP_MVOL_R_ADDR = 0x1C
    DSP_EVOL_L_ADDR = 0x2C
    DSP_EVOL_R_ADDR = 0x3C
    DSP_KON_ADDR = 0x4C
    DSP_KOFF_ADDR = 0x5C
    DSP_KONKOFF_ADDR = 0x4D
    DSP_FLG_ADDR = 0x6C
    DSP_ENDX_ADDR = 0x7D
    DSP_EBUST_ADDR = 0x6D
    DSP_EDL_ADDR = 0x7D
    DSP_ENV0_ADDR = 0x00
    DSP_OUT0_ADDR = 0x1C
    DSP_ENV1_ADDR = 0x01
    DSP_OUT1_ADDR = 0x2C
    DSP_ENV2_ADDR = 0x02
    DSP_OUT2_ADDR = 0x3C
    DSP_ENV3_ADDR = 0x03
    DSP_OUT3_ADDR = 0x4C
    DSP_ENV4_ADDR = 0x04
    DSP_OUT4_ADDR = 0x5C
    DSP_ENV5_ADDR = 0x05
    DSP_OUT5_ADDR = 0x6C
    DSP_ENV6_ADDR = 0x06
    DSP_OUT6_ADDR = 0x7C
    DSP_ENV7_ADDR = 0x07
    DSP_OUT7_ADDR = 0x0D
    DSP_V0SRC_ADDR = 0x08
    DSP_V1SRC_ADDR = 0x09
    DSP_V2SRC_ADDR = 0x0A
    DSP_V3SRC_ADDR = 0x0B
    DSP_V4SRC_ADDR = 0x18
    DSP_V5SRC_ADDR = 0x19
    DSP_V6SRC_ADDR = 0x1A
    DSP_V7SRC_ADDR = 0x1B
    DSP_V0PITCHL_ADDR = 0x02
    DSP_V0PITCHH_ADDR = 0x03
    DSP_V1PITCHL_ADDR = 0x12
    DSP_V1PITCHH_ADDR = 0x13
    DSP_V2PITCHL_ADDR = 0x22
    DSP_V2PITCHH_ADDR = 0x23
    DSP_V3PITCHL_ADDR = 0x32
    DSP_V3PITCHH_ADDR = 0x33
    DSP_V4PITCHL_ADDR = 0x42
    DSP_V4PITCHH_ADDR = 0x43
    DSP_V5PITCHL_ADDR = 0x52
    DSP_V5PITCHH_ADDR = 0x53
    DSP_V6PITCHL_ADDR = 0x62
    DSP_V6PITCHH_ADDR = 0x63
    DSP_V7PITCHL_ADDR = 0x72
    DSP_V7PITCHH_ADDR = 0x73
    DSP_V0ADSR0_ADDR = 0x04
    DSP_V0ADSR1_ADDR = 0x05
    DSP_V0ADSR2_ADDR = 0x06
    DSP_V1ADSR0_ADDR = 0x14
    DSP_V1ADSR1_ADDR = 0x15
    DSP_V1ADSR2_ADDR = 0x16
    DSP_V2ADSR0_ADDR = 0x24
    DSP_V2ADSR1_ADDR = 0x25
    DSP_V2ADSR2_ADDR = 0x26
    DSP_V3ADSR0_ADDR = 0x34
    DSP_V3ADSR1_ADDR = 0x35
    DSP_V3ADSR2_ADDR = 0x36
    DSP_V4ADSR0_ADDR = 0x44
    DSP_V4ADSR1_ADDR = 0x45
    DSP_V4ADSR2_ADDR = 0x46
    DSP_V5ADSR0_ADDR = 0x54
    DSP_V5ADSR1_ADDR = 0x55
    DSP_V5ADSR2_ADDR = 0x56
    DSP_V6ADSR0_ADDR = 0x64
    DSP_V6ADSR1_ADDR = 0x65
    DSP_V6ADSR2_ADDR = 0x66
    DSP_V7ADSR0_ADDR = 0x74
    DSP_V7ADSR1_ADDR = 0x75
    DSP_V7ADSR2_ADDR = 0x76
    DSP_V0GAIN_ADDR = 0x07
    DSP_V1GAIN_ADDR = 0x17
    DSP_V2GAIN_ADDR = 0x27
    DSP_V3GAIN_ADDR = 0x37
    DSP_V4GAIN_ADDR = 0x47
    DSP_V5GAIN_ADDR = 0x57
    DSP_V6GAIN_ADDR = 0x67
    DSP_V7GAIN_ADDR = 0x77
    DSP_V0WAVE_ADDR = 0x0D
    DSP_V1WAVE_ADDR = 0x1D
    DSP_V2WAVE_ADDR = 0x2D
    DSP_V3WAVE_ADDR = 0x3D
    DSP_V4WAVE_ADDR = 0x4D
    DSP_V5WAVE_ADDR = 0x5D
    DSP_V6WAVE_ADDR = 0x6D
    DSP_V7WAVE_ADDR = 0x7D
    # Direct Memory Access Controller
    DMA_BASE = 0x4300
    DMA_DMAP0_ADDR = 0x4300
    DMA_BBAD0_ADDR = 0x4301
    DMA_A1T0L_ADDR = 0x4302
    DMA_A1T0H_ADDR = 0x4303
    DMA_A1B0_ADDR = 0x4304
    DMA_DAS0L_ADDR = 0x4305
    DMA_DAS0H_ADDR = 0x4306
    DMA_DASB0_ADDR = 0x4307
    DMA_A2A0_ADDR = 0x4308
    DMA_A2A1_ADDR = 0x4309
    DMA_A2B0_ADDR = 0x430A
    DMA_NTT0_ADDR = 0x430B
    DMA_DMAP1_ADDR = 0x4310
    DMA_BBAD1_ADDR = 0x4311
    DMA_A1T1L_ADDR = 0x4312
    DMA_A1T1H_ADDR = 0x4313
    DMA_A1B1_ADDR = 0x4314
    DMA_DAS1L_ADDR = 0x4315
    DMA_DAS1H_ADDR = 0x4316
    DMA_DASB1_ADDR = 0x4317
    DMA_DMAP2_ADDR = 0x4320
    DMA_BBAD2_ADDR = 0x4321
    DMA_A1T2L_ADDR = 0x4322
    DMA_A1T2H_ADDR = 0x4323
    DMA_A1B2_ADDR = 0x4324
    DMA_DAS2L_ADDR = 0x4325
    DMA_DAS2H_ADDR = 0x4326
    DMA_DASB2_ADDR = 0x4327
    DMA_DMAP3_ADDR = 0x4330
    DMA_BBAD3_ADDR = 0x4331
    DMA_A1T3L_ADDR = 0x4332
    DMA_A1T3H_ADDR = 0x4333
    DMA_A1B3_ADDR = 0x4334
    DMA_DAS3L_ADDR = 0x4335
    DMA_DAS3H_ADDR = 0x4336
    DMA_DASB3_ADDR = 0x4337
    DMA_MDMAEN_ADDR = 0x4350
    # Horizontal DMA (scanline-based)
    HDMA_BASE = 0x4380
    HDMA_HDMAP0_ADDR = 0x4380
    HDMA_HBAD0_ADDR = 0x4381
    HDMA_A1T0L_ADDR = 0x4382
    HDMA_A1T0H_ADDR = 0x4383
    HDMA_A1B0_ADDR = 0x4384
    HDMA_DAS0L_ADDR = 0x4385
    HDMA_DAS0H_ADDR = 0x4386
    HDMA_HDMAP1_ADDR = 0x4388
    HDMA_HBAD1_ADDR = 0x4389
    HDMA_A1T1L_ADDR = 0x438A
    HDMA_A1T1H_ADDR = 0x438B
    HDMA_A1B1_ADDR = 0x438C
    HDMA_DAS1L_ADDR = 0x438D
    HDMA_DAS1H_ADDR = 0x438E
    HDMA_HDMAP2_ADDR = 0x4390
    HDMA_HBAD2_ADDR = 0x4391
    HDMA_A1T2L_ADDR = 0x4392
    HDMA_A1T2H_ADDR = 0x4393
    HDMA_A1B2_ADDR = 0x4394
    HDMA_DAS2L_ADDR = 0x4395
    HDMA_DAS2H_ADDR = 0x4396
    HDMA_HDMAP3_ADDR = 0x4398
    HDMA_HBAD3_ADDR = 0x4399
    HDMA_A1T3L_ADDR = 0x439A
    HDMA_A1T3H_ADDR = 0x439B
    HDMA_A1B3_ADDR = 0x439C
    HDMA_DAS3L_ADDR = 0x439D
    HDMA_DAS3H_ADDR = 0x439E
    HDMA_HDMAEN_ADDR = 0x43F0
    # Controller Port 1
    CONTROLLER1_BASE = 0x4016
    CONTROLLER1_JOYPAD1_ADDR = 0x4016
    CONTROLLER1_JOYSTROBE_ADDR = 0x4016
    # Controller Port 2
    CONTROLLER2_BASE = 0x4017
    CONTROLLER2_JOYPAD2_ADDR = 0x4017
    CONTROLLER2_RDNMI_ADDR = 0x4210
    CONTROLLER2_TIMEUP_ADDR = 0x4211
    CONTROLLER2_HVBJOY_ADDR = 0x4212
    # Timer / IRQ Control
    TIMER_BASE = 0x4200
    TIMER_NMITIMEN_ADDR = 0x4200
    TIMER_NMITIMEN_VBLANK_NMI_BIT = 7  # V-Blank NMI Enable
    TIMER_NMITIMEN_HTIMER_EN_BIT = 4  # H-Counter IRQ Enable
    TIMER_NMITIMEN_VTIMER_EN_BIT = 5  # V-Counter IRQ Enable
    TIMER_WRI00_ADDR = 0x4201
    TIMER_HTIMEL_ADDR = 0x4202
    TIMER_HTIMEH_ADDR = 0x4203
    TIMER_VTIMEL_ADDR = 0x4204
    TIMER_VTIMEH_ADDR = 0x4205
    TIMER_MEMSEL_ADDR = 0x420D

    # 中断向量定义
    INT_RESET = 0  # Reset
    INT_NMI = 1  # Non-Maskable Interrupt (V-Blank)
    INT_IRQ = 2  # IRQ / BRK (Timer, HDMA, Controller)
    INT_TIMER_IRQ = 3  # H/V Counter Timer IRQ

    # 引脚定义
    PIN_VCC = 1  # Power Supply
    PIN_GND = 2  # Ground
    PIN_CLK = 3  # System Clock Input (21.47727 MHz)
    PIN_RESET = 4  # Reset Signal
    PIN_NMI = 5  # Non-Maskable Interrupt
    PIN_IRQ = 6  # Interrupt Request
    PIN_RDY = 7  # Ready / Wait State
    PIN_AB0 = 8  # Address Bus Bit 0
    PIN_AB1 = 9  # Address Bus Bit 1
    PIN_AB2 = 10  # Address Bus Bit 2
    PIN_AB3 = 11  # Address Bus Bit 3
    PIN_AB4 = 12  # Address Bus Bit 4
    PIN_AB5 = 13  # Address Bus Bit 5
    PIN_AB6 = 14  # Address Bus Bit 6
    PIN_AB7 = 15  # Address Bus Bit 7
    PIN_AB8 = 16  # Address Bus Bit 8
    PIN_AB9 = 17  # Address Bus Bit 9
    PIN_AB10 = 18  # Address Bus Bit 10
    PIN_AB11 = 19  # Address Bus Bit 11
    PIN_AB12 = 20  # Address Bus Bit 12
    PIN_AB13 = 21  # Address Bus Bit 13
    PIN_AB14 = 22  # Address Bus Bit 14
    PIN_AB15 = 23  # Address Bus Bit 15
    PIN_AB16 = 24  # Address Bus Bit 16
    PIN_AB17 = 25  # Address Bus Bit 17
    PIN_AB18 = 26  # Address Bus Bit 18
    PIN_AB19 = 27  # Address Bus Bit 19
    PIN_AB20 = 28  # Address Bus Bit 20
    PIN_AB21 = 29  # Address Bus Bit 21
    PIN_AB22 = 30  # Address Bus Bit 22
    PIN_AB23 = 31  # Address Bus Bit 23
    PIN_DB0 = 32  # Data Bus Bit 0
    PIN_DB1 = 33  # Data Bus Bit 1
    PIN_DB2 = 34  # Data Bus Bit 2
    PIN_DB3 = 35  # Data Bus Bit 3
    PIN_DB4 = 36  # Data Bus Bit 4
    PIN_DB5 = 37  # Data Bus Bit 5
    PIN_DB6 = 38  # Data Bus Bit 6
    PIN_DB7 = 39  # Data Bus Bit 7
    PIN_PHI = 40  # Phase Out Clock

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["A"] = {
            "address": 0x00,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Accumulator (8-bit, expandable to 16-bit)",
            "value": 0
        }
        self._registers["B"] = {
            "address": 0x01,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Accumulator high byte when 16-bit",
            "value": 0
        }
        self._registers["X"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "X Index Register (8/16-bit)",
            "value": 0
        }
        self._registers["Y"] = {
            "address": 0x03,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Y Index Register (8/16-bit)",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x04,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Stack Pointer (8-bit, banked)",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x06,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter (16-bit)",
            "value": 0
        }
        self._registers["D"] = {
            "address": 0x08,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Direct Page Register",
            "value": 0
        }
        self._registers["P"] = {
            "address": 0x0A,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Processor Status",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PPU1"] = {
            "base": 0x2100,
            "type": "video",
            "description": "Picture Processing Unit 1 - Background Rendering",
            "registers": {
                "INIDISP": {
                    "address": 0x2100,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OBSEL": {
                    "address": 0x2101,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OAMADDL": {
                    "address": 0x2102,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OAMADDH": {
                    "address": 0x2103,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OAMDATA": {
                    "address": 0x2104,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BGMODE": {
                    "address": 0x2105,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MOSAIC": {
                    "address": 0x2106,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BG1SC": {
                    "address": 0x2107,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BG2SC": {
                    "address": 0x2108,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BG3SC": {
                    "address": 0x2109,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BG4SC": {
                    "address": 0x210A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BG12NBA": {
                    "address": 0x210B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BG34NBA": {
                    "address": 0x210C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BG1HOFS": {
                    "address": 0x210D,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BG1VOFS": {
                    "address": 0x210E,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BG2HOFS": {
                    "address": 0x210F,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BG2VOFS": {
                    "address": 0x2110,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BG3HOFS": {
                    "address": 0x2111,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BG3VOFS": {
                    "address": 0x2112,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BG4HOFS": {
                    "address": 0x2113,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BG4VOFS": {
                    "address": 0x2114,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "VMAIN": {
                    "address": 0x2115,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VMADDL": {
                    "address": 0x2116,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VMADDH": {
                    "address": 0x2117,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VMDATAL": {
                    "address": 0x2118,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VMDATAH": {
                    "address": 0x2119,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "M7SEL": {
                    "address": 0x211A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "M7A": {
                    "address": 0x211B,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "M7B": {
                    "address": 0x211C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "M7C": {
                    "address": 0x211D,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "M7D": {
                    "address": 0x211E,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "M7X": {
                    "address": 0x211F,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "M7Y": {
                    "address": 0x2120,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CGADD": {
                    "address": 0x2121,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CGDATA": {
                    "address": 0x2122,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "W12SEL": {
                    "address": 0x2123,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "W34SEL": {
                    "address": 0x2124,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WOBJSEL": {
                    "address": 0x2125,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WH0": {
                    "address": 0x2126,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WH1": {
                    "address": 0x2127,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WH2": {
                    "address": 0x2128,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WH3": {
                    "address": 0x2129,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WBGLOG": {
                    "address": 0x212A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WOBJLOG": {
                    "address": 0x212B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TM": {
                    "address": 0x212C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TS": {
                    "address": 0x212D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TMW": {
                    "address": 0x212E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TSW": {
                    "address": 0x212F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CGSWSEL": {
                    "address": 0x2130,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CGADSUB": {
                    "address": 0x2131,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SETINI": {
                    "address": 0x2133,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PPU2"] = {
            "base": 0x2140,
            "type": "video",
            "description": "Picture Processing Unit 2 - Sprite Rendering",
            "registers": {
                "OAMDATAREAD": {
                    "address": 0x2138,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VMDATAREAD": {
                    "address": 0x2139,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VMDATAHREAD": {
                    "address": 0x213A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CGDATAREAD": {
                    "address": 0x213B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OPHCT": {
                    "address": 0x213C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OPVCT": {
                    "address": 0x213D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "STAT78": {
                    "address": 0x213F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SPC700"] = {
            "base": 0x00,
            "type": "audio_cpu",
            "description": "Sony SPC700 Audio CPU (8-bit)",
            "registers": {
                "PC": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "A": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "X": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Y": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PSW": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TEST": {
                    "address": 0x0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["DSP"] = {
            "base": 0x00,
            "type": "audio",
            "description": "S-DSP Audio DSP (8-channel ADPCM)",
            "registers": {
                "MVOL_L": {
                    "address": 0x0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MVOL_R": {
                    "address": 0x1C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EVOL_L": {
                    "address": 0x2C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EVOL_R": {
                    "address": 0x3C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KON": {
                    "address": 0x4C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KOFF": {
                    "address": 0x5C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KONKOFF": {
                    "address": 0x4D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FLG": {
                    "address": 0x6C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENDX": {
                    "address": 0x7D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EBUST": {
                    "address": 0x6D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EDL": {
                    "address": 0x7D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV0": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OUT0": {
                    "address": 0x1C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV1": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OUT1": {
                    "address": 0x2C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV2": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OUT2": {
                    "address": 0x3C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV3": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OUT3": {
                    "address": 0x4C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV4": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OUT4": {
                    "address": 0x5C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV5": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OUT5": {
                    "address": 0x6C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV6": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OUT6": {
                    "address": 0x7C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV7": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OUT7": {
                    "address": 0x0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V0SRC": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V1SRC": {
                    "address": 0x09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V2SRC": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V3SRC": {
                    "address": 0x0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V4SRC": {
                    "address": 0x18,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V5SRC": {
                    "address": 0x19,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V6SRC": {
                    "address": 0x1A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V7SRC": {
                    "address": 0x1B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V0PITCHL": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V0PITCHH": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V1PITCHL": {
                    "address": 0x12,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V1PITCHH": {
                    "address": 0x13,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V2PITCHL": {
                    "address": 0x22,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V2PITCHH": {
                    "address": 0x23,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V3PITCHL": {
                    "address": 0x32,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V3PITCHH": {
                    "address": 0x33,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V4PITCHL": {
                    "address": 0x42,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V4PITCHH": {
                    "address": 0x43,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V5PITCHL": {
                    "address": 0x52,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V5PITCHH": {
                    "address": 0x53,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V6PITCHL": {
                    "address": 0x62,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V6PITCHH": {
                    "address": 0x63,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V7PITCHL": {
                    "address": 0x72,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V7PITCHH": {
                    "address": 0x73,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V0ADSR0": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V0ADSR1": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V0ADSR2": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V1ADSR0": {
                    "address": 0x14,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V1ADSR1": {
                    "address": 0x15,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V1ADSR2": {
                    "address": 0x16,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V2ADSR0": {
                    "address": 0x24,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V2ADSR1": {
                    "address": 0x25,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V2ADSR2": {
                    "address": 0x26,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V3ADSR0": {
                    "address": 0x34,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V3ADSR1": {
                    "address": 0x35,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V3ADSR2": {
                    "address": 0x36,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V4ADSR0": {
                    "address": 0x44,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V4ADSR1": {
                    "address": 0x45,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V4ADSR2": {
                    "address": 0x46,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V5ADSR0": {
                    "address": 0x54,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V5ADSR1": {
                    "address": 0x55,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V5ADSR2": {
                    "address": 0x56,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V6ADSR0": {
                    "address": 0x64,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V6ADSR1": {
                    "address": 0x65,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V6ADSR2": {
                    "address": 0x66,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V7ADSR0": {
                    "address": 0x74,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V7ADSR1": {
                    "address": 0x75,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V7ADSR2": {
                    "address": 0x76,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V0GAIN": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V1GAIN": {
                    "address": 0x17,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V2GAIN": {
                    "address": 0x27,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V3GAIN": {
                    "address": 0x37,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V4GAIN": {
                    "address": 0x47,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V5GAIN": {
                    "address": 0x57,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V6GAIN": {
                    "address": 0x67,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V7GAIN": {
                    "address": 0x77,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V0WAVE": {
                    "address": 0x0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V1WAVE": {
                    "address": 0x1D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V2WAVE": {
                    "address": 0x2D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V3WAVE": {
                    "address": 0x3D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V4WAVE": {
                    "address": 0x4D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V5WAVE": {
                    "address": 0x5D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V6WAVE": {
                    "address": 0x6D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V7WAVE": {
                    "address": 0x7D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["DMA"] = {
            "base": 0x4300,
            "type": "dma",
            "description": "Direct Memory Access Controller",
            "registers": {
                "DMAP0": {
                    "address": 0x4300,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BBAD0": {
                    "address": 0x4301,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T0L": {
                    "address": 0x4302,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T0H": {
                    "address": 0x4303,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1B0": {
                    "address": 0x4304,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS0L": {
                    "address": 0x4305,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS0H": {
                    "address": 0x4306,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DASB0": {
                    "address": 0x4307,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A2A0": {
                    "address": 0x4308,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A2A1": {
                    "address": 0x4309,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A2B0": {
                    "address": 0x430A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "NTT0": {
                    "address": 0x430B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMAP1": {
                    "address": 0x4310,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BBAD1": {
                    "address": 0x4311,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T1L": {
                    "address": 0x4312,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T1H": {
                    "address": 0x4313,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1B1": {
                    "address": 0x4314,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS1L": {
                    "address": 0x4315,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS1H": {
                    "address": 0x4316,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DASB1": {
                    "address": 0x4317,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMAP2": {
                    "address": 0x4320,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BBAD2": {
                    "address": 0x4321,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T2L": {
                    "address": 0x4322,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T2H": {
                    "address": 0x4323,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1B2": {
                    "address": 0x4324,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS2L": {
                    "address": 0x4325,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS2H": {
                    "address": 0x4326,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DASB2": {
                    "address": 0x4327,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DMAP3": {
                    "address": 0x4330,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BBAD3": {
                    "address": 0x4331,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T3L": {
                    "address": 0x4332,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T3H": {
                    "address": 0x4333,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1B3": {
                    "address": 0x4334,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS3L": {
                    "address": 0x4335,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS3H": {
                    "address": 0x4336,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DASB3": {
                    "address": 0x4337,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MDMAEN": {
                    "address": 0x4350,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["HDMA"] = {
            "base": 0x4380,
            "type": "dma",
            "description": "Horizontal DMA (scanline-based)",
            "registers": {
                "HDMAP0": {
                    "address": 0x4380,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HBAD0": {
                    "address": 0x4381,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T0L": {
                    "address": 0x4382,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T0H": {
                    "address": 0x4383,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1B0": {
                    "address": 0x4384,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS0L": {
                    "address": 0x4385,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS0H": {
                    "address": 0x4386,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HDMAP1": {
                    "address": 0x4388,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HBAD1": {
                    "address": 0x4389,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T1L": {
                    "address": 0x438A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T1H": {
                    "address": 0x438B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1B1": {
                    "address": 0x438C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS1L": {
                    "address": 0x438D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS1H": {
                    "address": 0x438E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HDMAP2": {
                    "address": 0x4390,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HBAD2": {
                    "address": 0x4391,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T2L": {
                    "address": 0x4392,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T2H": {
                    "address": 0x4393,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1B2": {
                    "address": 0x4394,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS2L": {
                    "address": 0x4395,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS2H": {
                    "address": 0x4396,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HDMAP3": {
                    "address": 0x4398,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HBAD3": {
                    "address": 0x4399,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T3L": {
                    "address": 0x439A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1T3H": {
                    "address": 0x439B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "A1B3": {
                    "address": 0x439C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS3L": {
                    "address": 0x439D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DAS3H": {
                    "address": 0x439E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HDMAEN": {
                    "address": 0x43F0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CONTROLLER1"] = {
            "base": 0x4016,
            "type": "input",
            "description": "Controller Port 1",
            "registers": {
                "JOYPAD1": {
                    "address": 0x4016,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "JOYSTROBE": {
                    "address": 0x4016,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CONTROLLER2"] = {
            "base": 0x4017,
            "type": "input",
            "description": "Controller Port 2",
            "registers": {
                "JOYPAD2": {
                    "address": 0x4017,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RDNMI": {
                    "address": 0x4210,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TIMEUP": {
                    "address": 0x4211,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HVBJOY": {
                    "address": 0x4212,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER"] = {
            "base": 0x4200,
            "type": "timer",
            "description": "Timer / IRQ Control",
            "registers": {
                "NMITIMEN": {
                    "address": 0x4200,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WRI00": {
                    "address": 0x4201,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HTIMEL": {
                    "address": 0x4202,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HTIMEH": {
                    "address": 0x4203,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VTIMEL": {
                    "address": 0x4204,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VTIMEH": {
                    "address": 0x4205,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MEMSEL": {
                    "address": 0x420D,
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
        return f"Ricoh_5A22({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Ricoh_5A22()
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

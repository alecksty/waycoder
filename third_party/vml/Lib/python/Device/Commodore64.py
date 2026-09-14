"""
Commodore-64设备定义 - Python模块
生成自: Commodore/C64/Commodore-64
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Commodore 64 - Best-selling 8-bit home computer with MOS 6510 CPU, VIC-II graphics, and SID audio
CPU架构: MOS-6510
位宽: 8位
时钟频率: 1022727 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Commodore_64:
    """Commodore-64设备类"""

    # 设备信息
    DEVICE_NAME = "Commodore-64"
    MANUFACTURER = "Commodore"
    FAMILY = "C64"
    VERSION = "1.0"
    ARCHITECTURE = "MOS-6510"
    BITS = 8
    CLOCK_FREQUENCY = 1022727

    # 寄存器地址定义
    A_ADDR = 0x00  # Accumulator
    X_ADDR = 0x01  # X Index Register
    Y_ADDR = 0x02  # Y Index Register
    SP_ADDR = 0x03  # Stack Pointer
    PC_ADDR = 0x04  # Program Counter
    P_ADDR = 0x06  # Processor Status
    P_C_BIT = 0  # Carry Flag
    P_Z_BIT = 1  # Zero Flag
    P_I_BIT = 2  # Interrupt Disable
    P_D_BIT = 3  # Decimal Mode
    P_B_BIT = 4  # Break Flag
    P_U_BIT = 5  # Unused
    P_V_BIT = 6  # Overflow Flag
    P_N_BIT = 7  # Negative Flag
    PORT_ADDR = 0x00  # I/O Port (6510 only: DDR + data)

    # 内存段定义
    RAM_START = 0x0000
    RAM_END = 0xFFFF
    RAM_SIZE = 65536  # 64KB main RAM
    BASIC_ROM_START = 0xA000
    BASIC_ROM_END = 0xBFFF
    BASIC_ROM_SIZE = 8192  # BASIC interpreter ROM
    KERNAL_ROM_START = 0xE000
    KERNAL_ROM_END = 0xFFFF
    KERNAL_ROM_SIZE = 8192  # KERNAL operating system ROM
    CHAR_ROM_START = 0xD000
    CHAR_ROM_END = 0xDFFF
    CHAR_ROM_SIZE = 4096  # Character generator ROM
    IO_RAM_START = 0xD000
    IO_RAM_END = 0xDFFF
    IO_RAM_SIZE = 4096  # I/O + RAM window (switchable)

    # 外设定义
    # Video Interface Chip II - 6567/6569
    VICII_BASE = 0xD000
    VICII_SP0X_ADDR = 0xD000
    VICII_SP0Y_ADDR = 0xD001
    VICII_SP1X_ADDR = 0xD002
    VICII_SP1Y_ADDR = 0xD003
    VICII_SP2X_ADDR = 0xD004
    VICII_SP2Y_ADDR = 0xD005
    VICII_SP3X_ADDR = 0xD006
    VICII_SP3Y_ADDR = 0xD007
    VICII_SP4X_ADDR = 0xD008
    VICII_SP4Y_ADDR = 0xD009
    VICII_SP5X_ADDR = 0xD00A
    VICII_SP5Y_ADDR = 0xD00B
    VICII_SP6X_ADDR = 0xD00C
    VICII_SP6Y_ADDR = 0xD00D
    VICII_SP7X_ADDR = 0xD00E
    VICII_SP7Y_ADDR = 0xD00F
    VICII_MSIGX_ADDR = 0xD010
    VICII_SCROLY_ADDR = 0xD011
    VICII_SCROLX_ADDR = 0xD016
    VICII_YPSTOP_ADDR = 0xD012
    VICII_LPX_ADDR = 0xD013
    VICII_LPY_ADDR = 0xD014
    VICII_SPENA_ADDR = 0xD015
    VICII_CSPMC_ADDR = 0xD017
    VICII_MM0_ADDR = 0xD018
    VICII_VM01_ADDR = 0xD016
    VICII_VICBAS_ADDR = 0xD018
    VICII_IRQMASK_ADDR = 0xD019
    VICII_IRQST_ADDR = 0xD01A
    VICII_SPBGPR_ADDR = 0xD01B
    VICII_SPMC_ADDR = 0xD01C
    VICII_SP1C_ADDR = 0xD025
    VICII_SP2C_ADDR = 0xD026
    VICII_SPBC_ADDR = 0xD027
    VICII_SP1C0_ADDR = 0xD028
    VICII_SP2C0_ADDR = 0xD029
    VICII_SP3C0_ADDR = 0xD02A
    VICII_SP4C0_ADDR = 0xD02B
    VICII_SP5C0_ADDR = 0xD02C
    VICII_SP6C0_ADDR = 0xD02D
    VICII_SP7C0_ADDR = 0xD02E
    VICII_REG_FD_ADDR = 0xD01D
    VICII_BGCOL0_ADDR = 0xD021
    VICII_BGCOL1_ADDR = 0xD022
    VICII_BGCOL2_ADDR = 0xD023
    VICII_BGCOL3_ADDR = 0xD024
    # Sound Interface Device 6581/8580
    SID_BASE = 0xD400
    SID_FREQ1LO_ADDR = 0xD400
    SID_FREQ1HI_ADDR = 0xD401
    SID_PW1LO_ADDR = 0xD402
    SID_PW1HI_ADDR = 0xD403
    SID_CR1_ADDR = 0xD404
    SID_AD1_ADDR = 0xD405
    SID_SR1_ADDR = 0xD406
    SID_FREQ2LO_ADDR = 0xD407
    SID_FREQ2HI_ADDR = 0xD408
    SID_PW2LO_ADDR = 0xD409
    SID_PW2HI_ADDR = 0xD40A
    SID_CR2_ADDR = 0xD40B
    SID_AD2_ADDR = 0xD40C
    SID_SR2_ADDR = 0xD40D
    SID_FREQ3LO_ADDR = 0xD40E
    SID_FREQ3HI_ADDR = 0xD40F
    SID_PW3LO_ADDR = 0xD410
    SID_PW3HI_ADDR = 0xD411
    SID_CR3_ADDR = 0xD412
    SID_AD3_ADDR = 0xD413
    SID_SR3_ADDR = 0xD414
    SID_FCH_ADDR = 0xD415
    SID_FCL_ADDR = 0xD416
    SID_RES_FLT_ADDR = 0xD417
    SID_VOLUME_ADDR = 0xD418
    SID_POTX_ADDR = 0xD419
    SID_POTY_ADDR = 0xD41A
    SID_OSC3_ADDR = 0xD41B
    SID_ENV3_ADDR = 0xD41C
    # Complex Interface Adapter 1 - Keyboard/Serial
    CIA1_BASE = 0xDC00
    CIA1_PRA_ADDR = 0xDC00
    CIA1_PRB_ADDR = 0xDC01
    CIA1_DDRA_ADDR = 0xDC02
    CIA1_DDRB_ADDR = 0xDC03
    CIA1_TA_LO_ADDR = 0xDC04
    CIA1_TA_HI_ADDR = 0xDC05
    CIA1_TB_LO_ADDR = 0xDC06
    CIA1_TB_HI_ADDR = 0xDC07
    CIA1_TOD_TENTH_ADDR = 0xDC08
    CIA1_TOD_SEC_ADDR = 0xDC09
    CIA1_TOD_MIN_ADDR = 0xDC0A
    CIA1_TOD_HR_ADDR = 0xDC0B
    CIA1_SDR_ADDR = 0xDC0C
    CIA1_ICR_ADDR = 0xDC0D
    CIA1_CRA_ADDR = 0xDC0E
    CIA1_CRB_ADDR = 0xDC0F
    # Complex Interface Adapter 2 - Serial/Bus
    CIA2_BASE = 0xDD00
    CIA2_PRA_ADDR = 0xDD00
    CIA2_PRB_ADDR = 0xDD01
    CIA2_DDRA_ADDR = 0xDD02
    CIA2_DDRB_ADDR = 0xDD03
    CIA2_TA_LO_ADDR = 0xDD04
    CIA2_TA_HI_ADDR = 0xDD05
    CIA2_TB_LO_ADDR = 0xDD06
    CIA2_TB_HI_ADDR = 0xDD07
    CIA2_TOD_TENTH_ADDR = 0xDD08
    CIA2_TOD_SEC_ADDR = 0xDD09
    CIA2_TOD_MIN_ADDR = 0xDD0A
    CIA2_TOD_HR_ADDR = 0xDD0B
    CIA2_SDR_ADDR = 0xDD0C
    CIA2_ICR_ADDR = 0xDD0D
    CIA2_CRA_ADDR = 0xDD0E
    CIA2_CRB_ADDR = 0xDD0F
    # Color RAM (4-bit per char cell)
    COLORRAM_BASE = 0xD800
    COLORRAM_COLOR_ADDR = 0xD800
    # IEC Serial Bus (via CIA1)
    IEC_BASE = 0xDC00
    IEC_IEC_DATA_ADDR = 0xDC00
    IEC_IEC_CLOCK_ADDR = 0xDC01

    # 中断向量定义
    INT_RESET = 0  # Power-on / Reset
    INT_NMI = 1  # Non-Maskable Interrupt
    INT_IRQ = 2  # IRQ (VIC raster / CIA timer)

    # 引脚定义
    PIN_VCC = 1  # +5V Power
    PIN_GND = 2  # Ground
    PIN_RESET = 3  # System Reset
    PIN_CLK = 4  # System Clock (~1MHz)
    PIN_DOTCLK = 5  # VIC Dot Clock (8MHz NTSC / 7.8MHz PAL)
    PIN_AEC = 6  # Address Enable Control (VIC steals cycles)
    PIN_BA = 7  # Bus Available (from VIC)
    PIN_IRQ = 8  # Interrupt Request
    PIN_NMI = 9  # Non-Maskable Interrupt
    PIN_RWB = 10  # Read/Write
    PIN_A0_A15 = 11  # Address Bus
    PIN_D0_D7 = 12  # Data Bus

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
            "description": "Accumulator",
            "value": 0
        }
        self._registers["X"] = {
            "address": 0x01,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "X Index Register",
            "value": 0
        }
        self._registers["Y"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Y Index Register",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x03,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x04,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["P"] = {
            "address": 0x06,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Processor Status",
            "value": 0
        }
        self._registers["PORT"] = {
            "address": 0x00,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "I/O Port (6510 only: DDR + data)",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["VICII"] = {
            "base": 0xD000,
            "type": "video",
            "description": "Video Interface Chip II - 6567/6569",
            "registers": {
                "SP0X": {
                    "address": 0xD000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP0Y": {
                    "address": 0xD001,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP1X": {
                    "address": 0xD002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP1Y": {
                    "address": 0xD003,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP2X": {
                    "address": 0xD004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP2Y": {
                    "address": 0xD005,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP3X": {
                    "address": 0xD006,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP3Y": {
                    "address": 0xD007,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP4X": {
                    "address": 0xD008,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP4Y": {
                    "address": 0xD009,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP5X": {
                    "address": 0xD00A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP5Y": {
                    "address": 0xD00B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP6X": {
                    "address": 0xD00C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP6Y": {
                    "address": 0xD00D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP7X": {
                    "address": 0xD00E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP7Y": {
                    "address": 0xD00F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MSIGX": {
                    "address": 0xD010,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCROLY": {
                    "address": 0xD011,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCROLX": {
                    "address": 0xD016,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "YPSTOP": {
                    "address": 0xD012,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LPX": {
                    "address": 0xD013,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LPY": {
                    "address": 0xD014,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SPENA": {
                    "address": 0xD015,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CSPMC": {
                    "address": 0xD017,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MM0": {
                    "address": 0xD018,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VM01": {
                    "address": 0xD016,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VICBAS": {
                    "address": 0xD018,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IRQMASK": {
                    "address": 0xD019,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IRQST": {
                    "address": 0xD01A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SPBGPR": {
                    "address": 0xD01B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SPMC": {
                    "address": 0xD01C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP1C": {
                    "address": 0xD025,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP2C": {
                    "address": 0xD026,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SPBC": {
                    "address": 0xD027,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP1C0": {
                    "address": 0xD028,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP2C0": {
                    "address": 0xD029,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP3C0": {
                    "address": 0xD02A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP4C0": {
                    "address": 0xD02B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP5C0": {
                    "address": 0xD02C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP6C0": {
                    "address": 0xD02D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SP7C0": {
                    "address": 0xD02E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "REG_FD": {
                    "address": 0xD01D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BGCOL0": {
                    "address": 0xD021,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BGCOL1": {
                    "address": 0xD022,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BGCOL2": {
                    "address": 0xD023,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BGCOL3": {
                    "address": 0xD024,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SID"] = {
            "base": 0xD400,
            "type": "audio",
            "description": "Sound Interface Device 6581/8580",
            "registers": {
                "FREQ1LO": {
                    "address": 0xD400,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ1HI": {
                    "address": 0xD401,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PW1LO": {
                    "address": 0xD402,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PW1HI": {
                    "address": 0xD403,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CR1": {
                    "address": 0xD404,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AD1": {
                    "address": 0xD405,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SR1": {
                    "address": 0xD406,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ2LO": {
                    "address": 0xD407,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ2HI": {
                    "address": 0xD408,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PW2LO": {
                    "address": 0xD409,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PW2HI": {
                    "address": 0xD40A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CR2": {
                    "address": 0xD40B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AD2": {
                    "address": 0xD40C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SR2": {
                    "address": 0xD40D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ3LO": {
                    "address": 0xD40E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ3HI": {
                    "address": 0xD40F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PW3LO": {
                    "address": 0xD410,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PW3HI": {
                    "address": 0xD411,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CR3": {
                    "address": 0xD412,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AD3": {
                    "address": 0xD413,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SR3": {
                    "address": 0xD414,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FCH": {
                    "address": 0xD415,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FCL": {
                    "address": 0xD416,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RES_FLT": {
                    "address": 0xD417,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VOLUME": {
                    "address": 0xD418,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "POTX": {
                    "address": 0xD419,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "POTY": {
                    "address": 0xD41A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OSC3": {
                    "address": 0xD41B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV3": {
                    "address": 0xD41C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CIA1"] = {
            "base": 0xDC00,
            "type": "timer",
            "description": "Complex Interface Adapter 1 - Keyboard/Serial",
            "registers": {
                "PRA": {
                    "address": 0xDC00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PRB": {
                    "address": 0xDC01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRA": {
                    "address": 0xDC02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRB": {
                    "address": 0xDC03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TA_LO": {
                    "address": 0xDC04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TA_HI": {
                    "address": 0xDC05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TB_LO": {
                    "address": 0xDC06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TB_HI": {
                    "address": 0xDC07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TOD_TENTH": {
                    "address": 0xDC08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TOD_SEC": {
                    "address": 0xDC09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TOD_MIN": {
                    "address": 0xDC0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TOD_HR": {
                    "address": 0xDC0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SDR": {
                    "address": 0xDC0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ICR": {
                    "address": 0xDC0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRA": {
                    "address": 0xDC0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRB": {
                    "address": 0xDC0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CIA2"] = {
            "base": 0xDD00,
            "type": "timer",
            "description": "Complex Interface Adapter 2 - Serial/Bus",
            "registers": {
                "PRA": {
                    "address": 0xDD00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PRB": {
                    "address": 0xDD01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRA": {
                    "address": 0xDD02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRB": {
                    "address": 0xDD03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TA_LO": {
                    "address": 0xDD04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TA_HI": {
                    "address": 0xDD05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TB_LO": {
                    "address": 0xDD06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TB_HI": {
                    "address": 0xDD07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TOD_TENTH": {
                    "address": 0xDD08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TOD_SEC": {
                    "address": 0xDD09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TOD_MIN": {
                    "address": 0xDD0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TOD_HR": {
                    "address": 0xDD0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SDR": {
                    "address": 0xDD0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ICR": {
                    "address": 0xDD0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRA": {
                    "address": 0xDD0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRB": {
                    "address": 0xDD0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["COLORRAM"] = {
            "base": 0xD800,
            "type": "memory",
            "description": "Color RAM (4-bit per char cell)",
            "registers": {
                "COLOR": {
                    "address": 0xD800,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["IEC"] = {
            "base": 0xDC00,
            "type": "bus",
            "description": "IEC Serial Bus (via CIA1)",
            "registers": {
                "IEC_DATA": {
                    "address": 0xDC00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IEC_CLOCK": {
                    "address": 0xDC01,
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
        return f"Commodore_64({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Commodore_64()
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

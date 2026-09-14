"""
MOS-6507设备定义 - Python模块
生成自: MOS Technology/MOS-6502/MOS-6507
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: Atari 2600 VCS main processor - MOS 6507 (simplified 6502) @ 1.19MHz with TIA and RIOT
CPU架构: MOS-6507
位宽: 8位
时钟频率: 1190000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class MOS_6507:
    """MOS-6507设备类"""

    # 设备信息
    DEVICE_NAME = "MOS-6507"
    MANUFACTURER = "MOS Technology"
    FAMILY = "MOS-6502"
    VERSION = "1.0"
    ARCHITECTURE = "MOS-6507"
    BITS = 8
    CLOCK_FREQUENCY = 1190000

    # 寄存器地址定义
    A_ADDR = 0x00  # Accumulator
    X_ADDR = 0x01  # X Index
    Y_ADDR = 0x02  # Y Index
    SP_ADDR = 0x03  # Stack Pointer (6-bit, 128-byte stack)
    PC_ADDR = 0x04  # Program Counter (16-bit)
    P_ADDR = 0x06  # Processor Status
    P_N_BIT = 7  # Negative
    P_V_BIT = 6  # Overflow
    P_B_BIT = 4  # Break
    P_D_BIT = 3  # Decimal Mode (N/A on 6507)
    P_I_BIT = 2  # Interrupt Disable
    P_Z_BIT = 1  # Zero
    P_C_BIT = 0  # Carry

    # 内存段定义
    TIA_REGS_START = 0x0000
    TIA_REGS_END = 0x007F
    TIA_REGS_SIZE = 128  # TIA Registers
    RIOT_RAM_START = 0x0080
    RIOT_RAM_END = 0x00FF
    RIOT_RAM_SIZE = 128  # RIOT 128byte RAM mirrored
    RIOT_IO_START = 0x0280
    RIOT_IO_END = 0x029F
    RIOT_IO_SIZE = 32  # RIOT I/O Registers (SWCHA/SWACNT/SWCHB/SWBCNT/INTIM)
    CART_ROM_START = 0x1000
    CART_ROM_END = 0x1FFF
    CART_ROM_SIZE = 4096  # Cartridge ROM (4KB, bank-switched)

    # 外设定义
    # Television Interface Adaptor (Video + Audio + I/O)
    TIA_BASE = 0x0000
    TIA_VSYNC_ADDR = 0x00
    TIA_VBLANK_ADDR = 0x01
    TIA_VBLANK_D7_BIT = 7  # Inhibit D7 (1=disable D7 output to PB7)
    TIA_VBLANK_D6_BIT = 6  # Inhibit D6 (1=disable D6 output to PB6)
    TIA_VBLANK_D5_BIT = 5  # Inhibit D5 (1=disable D5 output to PB5)
    TIA_VBLANK_D4_BIT = 4  # Inhibit D4 (1=disable D4 output to PB4)
    TIA_VBLANK_D3_BIT = 3  # Inhibit D3 (1=disable D3 output to PB3)
    TIA_VBLANK_D2_BIT = 2  # Inhibit D2 (1=disable D2 output to PB2)
    TIA_VBLANK_D1_BIT = 1  # Inhibit D1 (1=disable D1 output to PB1)
    TIA_VBLANK_D0_BIT = 0  # Inhibit D0 (1=disable D0 output to PB0)
    TIA_VBLANK_VBW_BIT = 5  # Vertical Blank Enable (1=set VBLANK)
    TIA_VBLANK_VBL_BIT = 1  # Vertical Blank Set (1=V-Blank active)
    TIA_VBLANK_RESBL_BIT = 0  # Reset Blank (1=allow VSYNC/VBLANK reset on clock)
    TIA_WSYNC_ADDR = 0x02
    TIA_RSYNC_ADDR = 0x03
    TIA_NUSIZ0_ADDR = 0x04
    TIA_NUSIZ0_NUSIZ_BIT = 0  # Number/Size Code (0-7)
    TIA_NUSIZ0_MISSILE_SIZE_BIT = 0  # Missile Size
    TIA_NUSIZ0_RESM0_BIT = 6  # Reset M0
    TIA_NUSIZ0_RESM1_BIT = 7  # Reset M1
    TIA_NUSIZ1_ADDR = 0x05
    TIA_COLUP0_ADDR = 0x06
    TIA_COLUP1_ADDR = 0x07
    TIA_COLUPF_ADDR = 0x08
    TIA_COLUBK_ADDR = 0x09
    TIA_CTRLPF_ADDR = 0x0A
    TIA_CTRLPF_DELL_BIT = 0  # Delay Playfield L (Reflected/Left score)
    TIA_CTRLPF_BALL_SIZE_BIT = 0  # Ball Size (0=1, 1=2, 2=3, 3=4, 4=5, 5=6, 6=7, 7=8 clocks)
    TIA_CTRLPF_REF_BIT = 5  # Reflect (1=mirror playfield)
    TIA_CTRLPF_SCORE_BIT = 6  # Score Mode (1=use player colors for L/R halves)
    TIA_CTRLPF_DELBL_BIT = 7  # Delay Ball (1=delay ball 1 clock)
    TIA_REFPL_ADDR = 0x0B
    TIA_PF0_ADDR = 0x0D
    TIA_PF1_ADDR = 0x0E
    TIA_PF2_ADDR = 0x0F
    TIA_RESP0_ADDR = 0x10
    TIA_RESP1_ADDR = 0x11
    TIA_RESM0_ADDR = 0x12
    TIA_RESM1_ADDR = 0x13
    TIA_RESBL_ADDR = 0x14
    TIA_AUDC0_ADDR = 0x15
    TIA_AUDC0_VOL_BIT = 0  # Volume (0-15)
    TIA_AUDC0_TONE_BIT = 0  # Tone Divisor (5-bit counter)
    TIA_AUDC1_ADDR = 0x16
    TIA_AUDF0_ADDR = 0x17
    TIA_AUDF1_ADDR = 0x18
    TIA_AUDV0_ADDR = 0x19
    TIA_AUDV1_ADDR = 0x1A
    TIA_GRP0_ADDR = 0x1B
    TIA_GRP1_ADDR = 0x1C
    TIA_DGRP0_ADDR = 0x1D
    TIA_DGRP1_ADDR = 0x1E
    TIA_ENAM0_ADDR = 0x1F
    TIA_ENAM1_ADDR = 0x20
    TIA_ENABL_ADDR = 0x21
    TIA_HMP0_ADDR = 0x22
    TIA_HMP1_ADDR = 0x23
    TIA_HMM0_ADDR = 0x24
    TIA_HMM1_ADDR = 0x25
    TIA_HMBL_ADDR = 0x26
    TIA_VDEL0_ADDR = 0x27
    TIA_VDEL1_ADDR = 0x28
    TIA_VDELBL_ADDR = 0x29
    TIA_RESBB_ADDR = 0x2A
    TIA_HMOVE_ADDR = 0x2A
    TIA_HMCLR_ADDR = 0x2B
    TIA_CXM0P_ADDR = 0x30
    TIA_CXM1P_ADDR = 0x31
    TIA_CXP0FB_ADDR = 0x32
    TIA_CXP1FB_ADDR = 0x33
    TIA_CXM0FB_ADDR = 0x34
    TIA_CXM1FB_ADDR = 0x35
    TIA_CXBLPF_ADDR = 0x36
    TIA_CXPPMM_ADDR = 0x37
    TIA_INPT0_ADDR = 0x38
    TIA_INPT1_ADDR = 0x39
    TIA_INPT2_ADDR = 0x3A
    TIA_INPT3_ADDR = 0x3B
    TIA_INPT4_ADDR = 0x3C
    TIA_INPT5_ADDR = 0x3D
    # RAM, I/O, Timer (6532 RIOT)
    RIOT_BASE = 0x0080
    RIOT_SWCHA_ADDR = 0x280
    RIOT_SWACNT_ADDR = 0x281
    RIOT_SWCHB_ADDR = 0x282
    RIOT_SWCHB_RESET_BIT = 1  # Game Reset Switch (0=pressed)
    RIOT_SWCHB_SELECT_BIT = 2  # Game Select Switch (0=pressed)
    RIOT_SWCHB_DIFFB_BIT = 3  # Difficulty B (0=hard, 1=easy)
    RIOT_SWCHB_DIFFA_BIT = 4  # Difficulty A (0=hard, 1=easy)
    RIOT_SWBCNT_ADDR = 0x283
    RIOT_INTIM_ADDR = 0x284
    RIOT_TIMINT_ADDR = 0x285
    RIOT_TIM1T_ADDR = 0x294
    RIOT_TIM8T_ADDR = 0x295
    RIOT_TIM64T_ADDR = 0x296
    RIOT_TIM1024T_ADDR = 0x297
    # Controller Port 1 (Joystick)
    CONTROLLER1_BASE = 0x280
    CONTROLLER1_SWCHA_ADDR = 0x280
    # Controller Port 2 (Joystick)
    CONTROLLER2_BASE = 0x281
    CONTROLLER2_SWCHA_ADDR = 0x280

    # 中断向量定义
    INT_RESET = 0  # Power-On Reset

    # 引脚定义
    PIN_VSS = 1  # Ground
    PIN_VCC = 2  # Power Supply
    PIN_PHI0 = 3  # Clock Input (1.19MHz NTSC / 1.18MHz PAL)
    PIN_RESET = 4  # Reset (active low)
    PIN_A0 = 5  # Address Bus Bit 0
    PIN_A1 = 6  # Address Bus Bit 1
    PIN_A2 = 7  # Address Bus Bit 2
    PIN_A3 = 8  # Address Bus Bit 3
    PIN_A4 = 9  # Address Bus Bit 4
    PIN_A5 = 10  # Address Bus Bit 5
    PIN_A6 = 11  # Address Bus Bit 6
    PIN_A7 = 12  # Address Bus Bit 7
    PIN_A8 = 13  # Address Bus Bit 8
    PIN_A9 = 14  # Address Bus Bit 9
    PIN_A10 = 15  # Address Bus Bit 10
    PIN_A11 = 16  # Address Bus Bit 11
    PIN_A12 = 17  # Address Bus Bit 12
    PIN_D0 = 18  # Data Bus Bit 0
    PIN_D1 = 19  # Data Bus Bit 1
    PIN_D2 = 20  # Data Bus Bit 2
    PIN_D3 = 21  # Data Bus Bit 3
    PIN_D4 = 22  # Data Bus Bit 4
    PIN_D5 = 23  # Data Bus Bit 5
    PIN_D6 = 24  # Data Bus Bit 6
    PIN_D7 = 25  # Data Bus Bit 7
    PIN_RDY = 26  # Ready (stops CPU on read)
    PIN_R_W = 27  # Read/Write (1=Read, 0=Write)
    PIN_NC = 28  # Not Connected

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
            "description": "X Index",
            "value": 0
        }
        self._registers["Y"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Y Index",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x03,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Stack Pointer (6-bit, 128-byte stack)",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x04,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter (16-bit)",
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

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["TIA"] = {
            "base": 0x0000,
            "type": "video",
            "description": "Television Interface Adaptor (Video + Audio + I/O)",
            "registers": {
                "VSYNC": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VBLANK": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "WSYNC": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RSYNC": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "NUSIZ0": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "NUSIZ1": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "COLUP0": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "COLUP1": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "COLUPF": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "COLUBK": {
                    "address": 0x09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRLPF": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "REFPL": {
                    "address": 0x0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PF0": {
                    "address": 0x0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PF1": {
                    "address": 0x0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PF2": {
                    "address": 0x0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RESP0": {
                    "address": 0x10,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RESP1": {
                    "address": 0x11,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RESM0": {
                    "address": 0x12,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RESM1": {
                    "address": 0x13,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RESBL": {
                    "address": 0x14,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AUDC0": {
                    "address": 0x15,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AUDC1": {
                    "address": 0x16,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AUDF0": {
                    "address": 0x17,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AUDF1": {
                    "address": 0x18,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AUDV0": {
                    "address": 0x19,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AUDV1": {
                    "address": 0x1A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GRP0": {
                    "address": 0x1B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GRP1": {
                    "address": 0x1C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DGRP0": {
                    "address": 0x1D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DGRP1": {
                    "address": 0x1E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENAM0": {
                    "address": 0x1F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENAM1": {
                    "address": 0x20,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENABL": {
                    "address": 0x21,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HMP0": {
                    "address": 0x22,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HMP1": {
                    "address": 0x23,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HMM0": {
                    "address": 0x24,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HMM1": {
                    "address": 0x25,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HMBL": {
                    "address": 0x26,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDEL0": {
                    "address": 0x27,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDEL1": {
                    "address": 0x28,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDELBL": {
                    "address": 0x29,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RESBB": {
                    "address": 0x2A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HMOVE": {
                    "address": 0x2A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HMCLR": {
                    "address": 0x2B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CXM0P": {
                    "address": 0x30,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CXM1P": {
                    "address": 0x31,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CXP0FB": {
                    "address": 0x32,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CXP1FB": {
                    "address": 0x33,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CXM0FB": {
                    "address": 0x34,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CXM1FB": {
                    "address": 0x35,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CXBLPF": {
                    "address": 0x36,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CXPPMM": {
                    "address": 0x37,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "INPT0": {
                    "address": 0x38,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "INPT1": {
                    "address": 0x39,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "INPT2": {
                    "address": 0x3A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "INPT3": {
                    "address": 0x3B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "INPT4": {
                    "address": 0x3C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "INPT5": {
                    "address": 0x3D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["RIOT"] = {
            "base": 0x0080,
            "type": "system",
            "description": "RAM, I/O, Timer (6532 RIOT)",
            "registers": {
                "SWCHA": {
                    "address": 0x280,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SWACNT": {
                    "address": 0x281,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SWCHB": {
                    "address": 0x282,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SWBCNT": {
                    "address": 0x283,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "INTIM": {
                    "address": 0x284,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TIMINT": {
                    "address": 0x285,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TIM1T": {
                    "address": 0x294,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TIM8T": {
                    "address": 0x295,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TIM64T": {
                    "address": 0x296,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TIM1024T": {
                    "address": 0x297,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CONTROLLER1"] = {
            "base": 0x280,
            "type": "input",
            "description": "Controller Port 1 (Joystick)",
            "registers": {
                "SWCHA": {
                    "address": 0x280,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CONTROLLER2"] = {
            "base": 0x281,
            "type": "input",
            "description": "Controller Port 2 (Joystick)",
            "registers": {
                "SWCHA": {
                    "address": 0x280,
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
        return f"MOS_6507({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = MOS_6507()
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

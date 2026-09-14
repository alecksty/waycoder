"""
Amstrad-CPC-464设备定义 - Python模块
生成自: Amstrad/CPC/Amstrad-CPC-464
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Amstrad CPC 464 - British 8-bit home computer with Z80 CPU and built-in cassette recorder
CPU架构: Z80A
位宽: 8位
时钟频率: 4000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Amstrad_CPC_464:
    """Amstrad-CPC-464设备类"""

    # 设备信息
    DEVICE_NAME = "Amstrad-CPC-464"
    MANUFACTURER = "Amstrad"
    FAMILY = "CPC"
    VERSION = "1.0"
    ARCHITECTURE = "Z80A"
    BITS = 8
    CLOCK_FREQUENCY = 4000000

    # 寄存器地址定义
    A_ADDR = 0x00  # Accumulator
    F_ADDR = 0x01  # Flags
    F_C_BIT = 0  # Carry
    F_N_BIT = 1  # Subtract
    F_PV_BIT = 2  # Parity/Overflow
    F_H_BIT = 4  # Half Carry
    F_Z_BIT = 6  # Zero
    F_S_BIT = 7  # Sign
    B_ADDR = 0x02  # B Register
    C_ADDR = 0x03  # C Register
    D_ADDR = 0x04  # D Register
    E_ADDR = 0x05  # E Register
    H_ADDR = 0x06  # H Register
    L_ADDR = 0x07  # L Register
    AF_ADDR = 0x08  # Alternate AF
    BC_ADDR = 0x0A  # Alternate BC
    DE_ADDR = 0x0C  # Alternate DE
    HL_ADDR = 0x0E  # Alternate HL
    I_ADDR = 0x10  # Interrupt Vector
    R_ADDR = 0x11  # Refresh
    IX_ADDR = 0x12  # Index X
    IY_ADDR = 0x14  # Index Y
    SP_ADDR = 0x16  # Stack Pointer
    PC_ADDR = 0x18  # Program Counter

    # 内存段定义
    LOWER_ROM_START = 0x0000
    LOWER_ROM_END = 0x3FFF
    LOWER_ROM_SIZE = 16384  # Lower ROM (AMSDOS / CP/M)
    RAM_BANK0_START = 0x0000
    RAM_BANK0_END = 0x3FFF
    RAM_BANK0_SIZE = 16384  # Lower RAM bank (switchable)
    RAM_MAIN_START = 0x4000
    RAM_MAIN_END = 0xBFFF
    RAM_MAIN_SIZE = 32768  # Main RAM (32KB)
    UPPER_ROM_START = 0xC000
    UPPER_ROM_END = 0xFFFF
    UPPER_ROM_SIZE = 16384  # Upper ROM (BASIC)

    # 外设定义
    # Gate Array - Custom ASIC (video/sound/RAM control)
    GA_BASE = 0x7F00
    GA_GA_MR_ADDR = 0x7F00
    GA_GA_IR_ADDR = 0x7F01
    GA_GA_R1_ADDR = 0x7F02
    GA_GA_R2_ADDR = 0x7F03
    GA_GA_R3_ADDR = 0x7F04
    GA_GA_R4_ADDR = 0x7F05
    GA_GA_R5_ADDR = 0x7F06
    GA_GA_R6_ADDR = 0x7F07
    GA_GA_R7_ADDR = 0x7F08
    # CRT Controller 6845 - Video timing
    CRTC_BASE = 0xBC00
    CRTC_CRTC_REG_ADDR = 0xBC00
    CRTC_CRTC_DATA_ADDR = 0xBD00
    CRTC_CRTC_H_TOTAL_ADDR = 0xBC01
    CRTC_CRTC_H_DISP_ADDR = 0xBC02
    CRTC_CRTC_HSYNC_POS_ADDR = 0xBC03
    CRTC_CRTC_HSYNC_WIDTH_ADDR = 0xBC04
    CRTC_CRTC_V_TOTAL_ADDR = 0xBC05
    CRTC_CRTC_V_TOTAL_ADJ_ADDR = 0xBC06
    CRTC_CRTC_V_DISP_ADDR = 0xBC07
    CRTC_CRTC_VSYNC_POS_ADDR = 0xBC08
    CRTC_CRTC_INTERLACE_ADDR = 0xBC09
    CRTC_CRTC_CURSOR_START_ADDR = 0xBC0A
    CRTC_CRTC_CURSOR_END_ADDR = 0xBC0B
    CRTC_CRTC_SA_HI_ADDR = 0xBC0C
    CRTC_CRTC_SA_LO_ADDR = 0xBC0D
    CRTC_CRTC_CURSOR_HI_ADDR = 0xBC0E
    CRTC_CRTC_CURSOR_LO_ADDR = 0xBC0F
    # AY-3-8912 Programmable Sound Generator
    PSG_BASE = 0xF400
    PSG_PSG_REG_ADDR = 0xF400
    PSG_PSG_DATA_ADDR = 0xF600
    PSG_FREQ_A_LO_ADDR = 0xF400
    PSG_FREQ_A_HI_ADDR = 0xF401
    PSG_FREQ_B_LO_ADDR = 0xF402
    PSG_FREQ_B_HI_ADDR = 0xF403
    PSG_FREQ_C_LO_ADDR = 0xF404
    PSG_FREQ_C_HI_ADDR = 0xF405
    PSG_NOISE_FREQ_ADDR = 0xF406
    PSG_ENABLE_ADDR = 0xF407
    PSG_VOL_A_ADDR = 0xF408
    PSG_VOL_B_ADDR = 0xF409
    PSG_VOL_C_ADDR = 0xF40A
    PSG_ENV_FREQ_LO_ADDR = 0xF40B
    PSG_ENV_FREQ_HI_ADDR = 0xF40C
    PSG_ENV_SHAPE_ADDR = 0xF40D
    PSG_PORT_A_ADDR = 0xF40E
    PSG_PORT_B_ADDR = 0xF40F
    # WD1772 Floppy Disk Controller (via expansion)
    FDC_BASE = 0xF800
    FDC_FDC_STATUS_ADDR = 0xF8E0
    FDC_FDC_COMMAND_ADDR = 0xF8E0
    FDC_FDC_TRACK_ADDR = 0xF8E1
    FDC_FDC_SECTOR_ADDR = 0xF8E2
    FDC_FDC_DATA_ADDR = 0xF8E3
    # Centronics Parallel Printer Port
    PRINTER_BASE = 0xEE
    PRINTER_PRN_DATA_ADDR = 0xEE
    PRINTER_PRN_STROBE_ADDR = 0xEF

    # 中断向量定义
    INT_RESET = 0  # Power-on / Reset
    INT_NMI = 1  # Non-Maskable Interrupt
    INT_INT = 2  # Gate Array interrupt (50Hz vertical blank)

    # 引脚定义
    PIN_VCC = 1  # +5V Power
    PIN_GND = 2  # Ground
    PIN_CLK = 3  # Z80 Clock (4MHz)
    PIN_A0_A15 = 4  # Address Bus
    PIN_D0_D7 = 5  # Data Bus
    PIN_MREQ = 6  # Memory Request
    PIN_IORQ = 7  # I/O Request
    PIN_RD = 8  # Read
    PIN_WR = 9  # Write
    PIN_INT = 10  # Interrupt Request
    PIN_NMI = 11  # Non-Maskable Interrupt
    PIN_RESET = 12  # Reset

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
        self._registers["F"] = {
            "address": 0x01,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Flags",
            "value": 0
        }
        self._registers["B"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "B Register",
            "value": 0
        }
        self._registers["C"] = {
            "address": 0x03,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "C Register",
            "value": 0
        }
        self._registers["D"] = {
            "address": 0x04,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "D Register",
            "value": 0
        }
        self._registers["E"] = {
            "address": 0x05,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "E Register",
            "value": 0
        }
        self._registers["H"] = {
            "address": 0x06,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "H Register",
            "value": 0
        }
        self._registers["L"] = {
            "address": 0x07,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "L Register",
            "value": 0
        }
        self._registers["AF'"] = {
            "address": 0x08,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate AF",
            "value": 0
        }
        self._registers["BC'"] = {
            "address": 0x0A,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate BC",
            "value": 0
        }
        self._registers["DE'"] = {
            "address": 0x0C,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate DE",
            "value": 0
        }
        self._registers["HL'"] = {
            "address": 0x0E,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate HL",
            "value": 0
        }
        self._registers["I"] = {
            "address": 0x10,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Interrupt Vector",
            "value": 0
        }
        self._registers["R"] = {
            "address": 0x11,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Refresh",
            "value": 0
        }
        self._registers["IX"] = {
            "address": 0x12,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Index X",
            "value": 0
        }
        self._registers["IY"] = {
            "address": 0x14,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Index Y",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x16,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x18,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["GA"] = {
            "base": 0x7F00,
            "type": "video",
            "description": "Gate Array - Custom ASIC (video/sound/RAM control)",
            "registers": {
                "GA_MR": {
                    "address": 0x7F00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GA_IR": {
                    "address": 0x7F01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GA_R1": {
                    "address": 0x7F02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GA_R2": {
                    "address": 0x7F03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GA_R3": {
                    "address": 0x7F04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GA_R4": {
                    "address": 0x7F05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GA_R5": {
                    "address": 0x7F06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GA_R6": {
                    "address": 0x7F07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GA_R7": {
                    "address": 0x7F08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CRTC"] = {
            "base": 0xBC00,
            "type": "video",
            "description": "CRT Controller 6845 - Video timing",
            "registers": {
                "CRTC_REG": {
                    "address": 0xBC00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_DATA": {
                    "address": 0xBD00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_H_TOTAL": {
                    "address": 0xBC01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_H_DISP": {
                    "address": 0xBC02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_HSYNC_POS": {
                    "address": 0xBC03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_HSYNC_WIDTH": {
                    "address": 0xBC04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_V_TOTAL": {
                    "address": 0xBC05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_V_TOTAL_ADJ": {
                    "address": 0xBC06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_V_DISP": {
                    "address": 0xBC07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_VSYNC_POS": {
                    "address": 0xBC08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_INTERLACE": {
                    "address": 0xBC09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_CURSOR_START": {
                    "address": 0xBC0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_CURSOR_END": {
                    "address": 0xBC0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_SA_HI": {
                    "address": 0xBC0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_SA_LO": {
                    "address": 0xBC0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_CURSOR_HI": {
                    "address": 0xBC0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CRTC_CURSOR_LO": {
                    "address": 0xBC0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PSG"] = {
            "base": 0xF400,
            "type": "audio",
            "description": "AY-3-8912 Programmable Sound Generator",
            "registers": {
                "PSG_REG": {
                    "address": 0xF400,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PSG_DATA": {
                    "address": 0xF600,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_A_LO": {
                    "address": 0xF400,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_A_HI": {
                    "address": 0xF401,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_B_LO": {
                    "address": 0xF402,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_B_HI": {
                    "address": 0xF403,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_C_LO": {
                    "address": 0xF404,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_C_HI": {
                    "address": 0xF405,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "NOISE_FREQ": {
                    "address": 0xF406,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENABLE": {
                    "address": 0xF407,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VOL_A": {
                    "address": 0xF408,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VOL_B": {
                    "address": 0xF409,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VOL_C": {
                    "address": 0xF40A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV_FREQ_LO": {
                    "address": 0xF40B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV_FREQ_HI": {
                    "address": 0xF40C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV_SHAPE": {
                    "address": 0xF40D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORT_A": {
                    "address": 0xF40E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORT_B": {
                    "address": 0xF40F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["FDC"] = {
            "base": 0xF800,
            "type": "storage",
            "description": "WD1772 Floppy Disk Controller (via expansion)",
            "registers": {
                "FDC_STATUS": {
                    "address": 0xF8E0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FDC_COMMAND": {
                    "address": 0xF8E0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FDC_TRACK": {
                    "address": 0xF8E1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FDC_SECTOR": {
                    "address": 0xF8E2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FDC_DATA": {
                    "address": 0xF8E3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PRINTER"] = {
            "base": 0xEE,
            "type": "output",
            "description": "Centronics Parallel Printer Port",
            "registers": {
                "PRN_DATA": {
                    "address": 0xEE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PRN_STROBE": {
                    "address": 0xEF,
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
        return f"Amstrad_CPC_464({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Amstrad_CPC_464()
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

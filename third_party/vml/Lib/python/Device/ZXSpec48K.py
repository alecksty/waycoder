"""
ZX-Spectrum-48K设备定义 - Python模块
生成自: Sinclair Research/ZX Spectrum/ZX-Spectrum-48K
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Sinclair ZX Spectrum 48K - Iconic British 8-bit home computer with Z80A CPU and ULA graphics
CPU架构: Z80A
位宽: 8位
时钟频率: 3500000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class ZX_Spectrum_48K:
    """ZX-Spectrum-48K设备类"""

    # 设备信息
    DEVICE_NAME = "ZX-Spectrum-48K"
    MANUFACTURER = "Sinclair Research"
    FAMILY = "ZX Spectrum"
    VERSION = "1.0"
    ARCHITECTURE = "Z80A"
    BITS = 8
    CLOCK_FREQUENCY = 3500000

    # 寄存器地址定义
    A_ADDR = 0x00  # Accumulator
    F_ADDR = 0x01  # Flags Register
    F_C_BIT = 0  # Carry
    F_N_BIT = 1  # Add/Subtract
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
    I_ADDR = 0x10  # Interrupt Vector Register
    R_ADDR = 0x11  # Refresh Counter
    IX_ADDR = 0x12  # Index X
    IY_ADDR = 0x14  # Index Y
    SP_ADDR = 0x16  # Stack Pointer
    PC_ADDR = 0x18  # Program Counter

    # 内存段定义
    ROM_START = 0x0000
    ROM_END = 0x3FFF
    ROM_SIZE = 16384  # 48KB ZX Spectrum ROM (BASIC + monitor)
    VIDEO_RAM_START = 0x4000
    VIDEO_RAM_END = 0x57FF
    VIDEO_RAM_SIZE = 6144  # Display file (256x192 bitmap)
    ATTR_RAM_START = 0x5800
    ATTR_RAM_END = 0x5AFF
    ATTR_RAM_SIZE = 768  # Attribute file (32x24 color cells)
    USER_RAM_START = 0x5B00
    USER_RAM_END = 0xFFFF
    USER_RAM_SIZE = 40960  # User RAM (40KB)

    # 外设定义
    # Uncommitted Logic Array - Sinclair custom IC
    ULA_BASE = 0xFE
    ULA_BORDER_ADDR = 0xFE
    ULA_KBD_ROW0_ADDR = 0xFE
    ULA_KBD_ROW1_ADDR = 0xFE
    ULA_KBD_ROW2_ADDR = 0xFE
    ULA_KBD_ROW3_ADDR = 0xFE
    ULA_KBD_ROW4_ADDR = 0xFE
    ULA_KBD_ROW5_ADDR = 0xFE
    ULA_KBD_ROW6_ADDR = 0xFE
    ULA_KBD_ROW7_ADDR = 0xFE
    ULA_KBD_ROW8_ADDR = 0xFE
    # Keyboard Matrix (40 keys, 8 rows x 5 cols)
    KEYBOARD_BASE = 0xFE
    KEYBOARD_KBD_IN_ADDR = 0xFE
    # Internal Beeper
    BEEPER_BASE = 0xFE
    BEEPER_BEEP_ADDR = 0xFE
    # Tape Interface
    TAPE_BASE = 0xFE
    TAPE_EAR_IN_ADDR = 0xFE
    TAPE_MIC_OUT_ADDR = 0xFE
    # Kempston Joystick Interface
    JOYSTICK_BASE = 0xF7FE
    JOYSTICK_KEMPSTON_ADDR = 0xF7FE

    # 中断向量定义
    INT_RESET = 0  # Power-on / Reset
    INT_NMI = 1  # Non-Maskable Interrupt (BREAK key)
    INT_INT = 2  # Maskable Interrupt (ULA vertical blank, 50Hz)

    # 引脚定义
    PIN_VCC = 1  # +5V Power
    PIN_GND = 2  # Ground
    PIN_CLK = 3  # Z80 Clock (3.5MHz)
    PIN_M1 = 4  # Machine Cycle 1
    PIN_MREQ = 5  # Memory Request
    PIN_IORQ = 6  # I/O Request
    PIN_RD = 7  # Read
    PIN_WR = 8  # Write
    PIN_HALT = 9  # Halt State
    PIN_BUSAK = 10  # Bus Acknowledge
    PIN_WAIT = 11  # Wait State (ULA inserts)
    PIN_INT = 12  # Interrupt Request
    PIN_NMI = 13  # Non-Maskable Interrupt
    PIN_RESET = 14  # Reset
    PIN_A0_A15 = 15  # Address Bus (16-bit)
    PIN_D0_D7 = 16  # Data Bus (8-bit)

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
            "description": "Flags Register",
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
            "description": "Interrupt Vector Register",
            "value": 0
        }
        self._registers["R"] = {
            "address": 0x11,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Refresh Counter",
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
        self._peripherals["ULA"] = {
            "base": 0xFE,
            "type": "video",
            "description": "Uncommitted Logic Array - Sinclair custom IC",
            "registers": {
                "BORDER": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBD_ROW0": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBD_ROW1": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBD_ROW2": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBD_ROW3": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBD_ROW4": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBD_ROW5": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBD_ROW6": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBD_ROW7": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBD_ROW8": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["KEYBOARD"] = {
            "base": 0xFE,
            "type": "input",
            "description": "Keyboard Matrix (40 keys, 8 rows x 5 cols)",
            "registers": {
                "KBD_IN": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["BEEPER"] = {
            "base": 0xFE,
            "type": "audio",
            "description": "Internal Beeper",
            "registers": {
                "BEEP": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TAPE"] = {
            "base": 0xFE,
            "type": "storage",
            "description": "Tape Interface",
            "registers": {
                "EAR_IN": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MIC_OUT": {
                    "address": 0xFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["JOYSTICK"] = {
            "base": 0xF7FE,
            "type": "input",
            "description": "Kempston Joystick Interface",
            "registers": {
                "KEMPSTON": {
                    "address": 0xF7FE,
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
        return f"ZX_Spectrum_48K({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = ZX_Spectrum_48K()
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

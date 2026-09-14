"""
MSX1设备定义 - Python模块
生成自: Various (ASCII/Awanaga/MSX Association)/MSX/MSX1
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: MSX - Standardized 8-bit home computer with Z80A CPU, TMS9918A graphics, and AY-3-8910 audio
CPU架构: Z80A
位宽: 8位
时钟频率: 3579545 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class MSX1:
    """MSX1设备类"""

    # 设备信息
    DEVICE_NAME = "MSX1"
    MANUFACTURER = "Various (ASCII/Awanaga/MSX Association)"
    FAMILY = "MSX"
    VERSION = "1.0"
    ARCHITECTURE = "Z80A"
    BITS = 8
    CLOCK_FREQUENCY = 3579545

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
    IY_ADDR = 0x14  # Index Y (usually = 0xF38F)
    SP_ADDR = 0x16  # Stack Pointer
    PC_ADDR = 0x18  # Program Counter

    # 内存段定义
    SLOT0_ROM_START = 0x0000
    SLOT0_ROM_END = 0x7FFF
    SLOT0_ROM_SIZE = 32768  # Cartridge/SUB-ROM / Main-ROM
    SYSROM_START = 0x0000
    SYSROM_END = 0x3FFF
    SYSROM_SIZE = 16384  # MSX-BIOS ROM
    EXTROM_START = 0x4000
    EXTROM_END = 0x7FFF
    EXTROM_SIZE = 16384  # Extension ROM (cartridge)
    MAIN_RAM_START = 0x4000
    MAIN_RAM_END = 0xC000
    MAIN_RAM_SIZE = 32768  # Main RAM (32KB working area)
    WORK_RAM_START = 0xC000
    WORK_RAM_END = 0xFFFF
    WORK_RAM_SIZE = 16384  # Work RAM (16KB)
    SYSVAR_START = 0xF000
    SYSVAR_END = 0xFCA0
    SYSVAR_SIZE = 3232  # System variables area
    SLOTS_START = 0x8000
    SLOTS_END = 0xFFFF
    SLOTS_SIZE = 32768  # Slot-mapped memory

    # 外设定义
    # TMS9918A Video Display Processor
    VDP_BASE = 0x98
    VDP_VDP_REG0_ADDR = 0x99
    VDP_VDP_REG1_ADDR = 0x99
    VDP_VDP_REG2_ADDR = 0x99
    VDP_VDP_REG3_ADDR = 0x99
    VDP_VDP_REG4_ADDR = 0x99
    VDP_VDP_REG5_ADDR = 0x99
    VDP_VDP_REG6_ADDR = 0x99
    VDP_VDP_REG7_ADDR = 0x99
    VDP_VDP_STATUS_ADDR = 0x99
    VDP_VDP_DATA_ADDR = 0x98
    VDP_VDP_POT_ADDR = 0x98
    # AY-3-8910 Programmable Sound Generator
    PSG_BASE = 0xA0
    PSG_PSG_REG_ADDR = 0xA1
    PSG_PSG_DATA_ADDR = 0xA3
    PSG_FREQ_A_LO_ADDR = 0xA0
    PSG_FREQ_A_HI_ADDR = 0xA1
    PSG_FREQ_B_LO_ADDR = 0xA2
    PSG_FREQ_B_HI_ADDR = 0xA3
    PSG_FREQ_C_LO_ADDR = 0xA4
    PSG_FREQ_C_HI_ADDR = 0xA5
    PSG_NOISE_FREQ_ADDR = 0xA6
    PSG_ENABLE_ADDR = 0xA7
    PSG_VOL_A_ADDR = 0xA8
    PSG_VOL_B_ADDR = 0xA9
    PSG_VOL_C_ADDR = 0xAA
    PSG_ENV_FREQ_LO_ADDR = 0xAB
    PSG_ENV_FREQ_HI_ADDR = 0xAC
    PSG_ENV_SHAPE_ADDR = 0xAD
    PSG_PORT_A_ADDR = 0xAE
    PSG_PORT_B_ADDR = 0xAF
    # PPI 8255 Programmable Peripheral Interface
    PPI_BASE = 0xA8
    PPI_PPI_PA_ADDR = 0xA8
    PPI_PPI_PB_ADDR = 0xA9
    PPI_PPI_PC_ADDR = 0xAA
    PPI_PPI_CTRL_ADDR = 0xAB
    # MSX Slot Expansion System
    SLOTEXP_BASE = 0x0000
    SLOTEXP_SLOT0_ADDR = 0xFCC0
    SLOTEXP_SLOT1_ADDR = 0xFCC1
    SLOTEXP_SLOT2_ADDR = 0xFCC2
    SLOTEXP_SLOT3_ADDR = 0xFCC3
    SLOTEXP_EXPTBL0_ADDR = 0xFCC4
    SLOTEXP_EXPTBL1_ADDR = 0xFCC5
    SLOTEXP_EXPTBL2_ADDR = 0xFCC6
    SLOTEXP_EXPTBL3_ADDR = 0xFCC7

    # 中断向量定义
    INT_RESET = 0  # Power-on / Reset
    INT_NMI = 1  # Non-Maskable Interrupt
    INT_INT = 2  # VDP Vertical Interrupt (frame)

    # 引脚定义
    PIN_VCC = 1  # +5V Power
    PIN_GND = 2  # Ground
    PIN_CLK = 3  # Z80 Clock (3.58MHz)
    PIN_A0_A15 = 4  # Address Bus
    PIN_D0_D7 = 5  # Data Bus
    PIN_MREQ = 6  # Memory Request
    PIN_IORQ = 7  # I/O Request
    PIN_RD = 8  # Read
    PIN_WR = 9  # Write
    PIN_INT = 10  # Interrupt Request
    PIN_NMI = 11  # Non-Maskable Interrupt
    PIN_RESET = 12  # Reset
    PIN_SLTSL = 13  # Slot select (for memory mapping)
    PIN_WAIT = 14  # Wait (for slow I/O)

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
            "description": "Index Y (usually = 0xF38F)",
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
        self._peripherals["VDP"] = {
            "base": 0x98,
            "type": "video",
            "description": "TMS9918A Video Display Processor",
            "registers": {
                "VDP_REG0": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_REG1": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_REG2": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_REG3": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_REG4": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_REG5": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_REG6": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_REG7": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_STATUS": {
                    "address": 0x99,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_DATA": {
                    "address": 0x98,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_POT": {
                    "address": 0x98,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PSG"] = {
            "base": 0xA0,
            "type": "audio",
            "description": "AY-3-8910 Programmable Sound Generator",
            "registers": {
                "PSG_REG": {
                    "address": 0xA1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PSG_DATA": {
                    "address": 0xA3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_A_LO": {
                    "address": 0xA0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_A_HI": {
                    "address": 0xA1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_B_LO": {
                    "address": 0xA2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_B_HI": {
                    "address": 0xA3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_C_LO": {
                    "address": 0xA4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FREQ_C_HI": {
                    "address": 0xA5,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "NOISE_FREQ": {
                    "address": 0xA6,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENABLE": {
                    "address": 0xA7,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VOL_A": {
                    "address": 0xA8,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VOL_B": {
                    "address": 0xA9,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VOL_C": {
                    "address": 0xAA,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV_FREQ_LO": {
                    "address": 0xAB,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV_FREQ_HI": {
                    "address": 0xAC,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV_SHAPE": {
                    "address": 0xAD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORT_A": {
                    "address": 0xAE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORT_B": {
                    "address": 0xAF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PPI"] = {
            "base": 0xA8,
            "type": "gpio",
            "description": "PPI 8255 Programmable Peripheral Interface",
            "registers": {
                "PPI_PA": {
                    "address": 0xA8,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPI_PB": {
                    "address": 0xA9,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPI_PC": {
                    "address": 0xAA,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PPI_CTRL": {
                    "address": 0xAB,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SLOTEXP"] = {
            "base": 0x0000,
            "type": "bus",
            "description": "MSX Slot Expansion System",
            "registers": {
                "SLOT0": {
                    "address": 0xFCC0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SLOT1": {
                    "address": 0xFCC1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SLOT2": {
                    "address": 0xFCC2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SLOT3": {
                    "address": 0xFCC3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EXPTBL0": {
                    "address": 0xFCC4,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EXPTBL1": {
                    "address": 0xFCC5,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EXPTBL2": {
                    "address": 0xFCC6,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EXPTBL3": {
                    "address": 0xFCC7,
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
        return f"MSX1({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = MSX1()
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

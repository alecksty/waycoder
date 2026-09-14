"""
Macintosh-128K设备定义 - Python模块
生成自: Apple Computer/Macintosh/Macintosh-128K
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Apple Macintosh 128K - First Macintosh - Motorola 68000, 128KB RAM, 512x342 display
CPU架构: MC68000
位宽: 32位
时钟频率: 7833600 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Macintosh_128K:
    """Macintosh-128K设备类"""

    # 设备信息
    DEVICE_NAME = "Macintosh-128K"
    MANUFACTURER = "Apple Computer"
    FAMILY = "Macintosh"
    VERSION = "1.0"
    ARCHITECTURE = "MC68000"
    BITS = 32
    CLOCK_FREQUENCY = 7833600

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
    A7_ADDR = 0x3C  # Stack Pointer (USP)
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
    SR_S_BIT = 13  # Supervisor/User
    SR_T0_BIT = 14  # Trace Mode 0
    SR_T1_BIT = 15  # Trace Mode 1

    # 内存段定义
    RAM_START = 0x000000
    RAM_END = 0x01FFFF
    RAM_SIZE = 131072  # Main RAM (128KB unified)
    ROM_START = 0x40000000
    ROM_END = 0x4001FFFF
    ROM_SIZE = 131072  # Mac ROM (128KB)
    FRAMEBUFFER_START = 0x00400000
    FRAMEBUFFER_END = 0x00400555
    FRAMEBUFFER_SIZE = 1366  # Screen bitmap (512x342x1 = 21792 bytes)
    FRAMEBUFFER2_START = 0x00410000
    FRAMEBUFFER2_END = 0x00410555
    FRAMEBUFFER2_SIZE = 1366  # Shadow screen (double-buffering)
    VIA_START = 0x00E00000
    VIA_END = 0x00E0FFFF
    VIA_SIZE = 4096  # VIA 6522 (I/O)
    SCC_START = 0x00F00000
    SCC_END = 0x00F0FFFF
    SCC_SIZE = 4096  # SCC 8530 (serial)
    ADB_START = 0x01600000
    ADB_END = 0x0160FFFF
    ADB_SIZE = 4096  # ADB bus
    IWM_START = 0x01E00000
    IWM_END = 0x01E0FFFF
    IWM_SIZE = 4096  # IWM floppy controller

    # 外设定义
    # Versatile Interface Adapter 6522
    VIA_BASE = 0xE00000
    VIA_ORB_ADDR = 0xE00000
    VIA_ORA_ADDR = 0xE00002
    VIA_DDRB_ADDR = 0xE00004
    VIA_DDRA_ADDR = 0xE00006
    VIA_T1C_L_ADDR = 0xE00008
    VIA_T1C_H_ADDR = 0xE0000A
    VIA_T1L_L_ADDR = 0xE0000C
    VIA_T1L_H_ADDR = 0xE0000E
    VIA_T2C_L_ADDR = 0xE00010
    VIA_T2C_H_ADDR = 0xE00012
    VIA_SR_ADDR = 0xE00014
    VIA_ACR_ADDR = 0xE00016
    VIA_PCR_ADDR = 0xE00018
    VIA_IFR_ADDR = 0xE0001E
    VIA_IER_ADDR = 0xE0001E
    # SCC 8530 Serial Communications Controller
    SCC_BASE = 0xF00000
    SCC_SCC_CHA_B_ADDR = 0xF00000
    SCC_SCC_CHA_C_ADDR = 0xF00002
    SCC_SCC_CHB_D_ADDR = 0xF00004
    SCC_SCC_CHB_CT_ADDR = 0xF00006
    # Integrated Woz Machine - Floppy Disk Controller
    IWM_BASE = 0x1E00000
    IWM_IWM_DATA_ADDR = 0x1E00000
    IWM_IWM_MODE_ADDR = 0x1E00008
    IWM_IWM_Q6L_ADDR = 0x1E00020
    IWM_IWM_Q7L_ADDR = 0x1E00022
    IWM_IWM_Q6R_ADDR = 0x1E00024
    IWM_IWM_Q7R_ADDR = 0x1E00026
    # Video Graphics Controller (custom Apple chip)
    VGC_BASE = 0x00F20000
    VGC_VGC_MODE_ADDR = 0x00F20000
    VGC_VGC_START_HI_ADDR = 0x00F20002
    VGC_VGC_START_LO_ADDR = 0x00F20004
    # Apple Desktop Bus
    ADB_BASE = 0x01600000
    ADB_ADB_DATA_ADDR = 0x01600000
    ADB_ADB_STATUS_ADDR = 0x01600004
    ADB_ADB_CMD_ADDR = 0x01600008

    # 中断向量定义
    INT_RESET = 1  # Reset Initial SP
    INT_RESET_PC = 2  # Reset Initial PC
    INT_IRQ1 = 24  # VIA interrupt (level 1)
    INT_IRQ2 = 25  # SCC interrupt (level 2)
    INT_IRQ3 = 26  # ADB / VIA (level 3)
    INT_IRQ4 = 27  # ADB / VIA (level 4)

    # 引脚定义
    PIN_VCC = 1  # +5V Power
    PIN_GND = 2  # Ground
    PIN_CLK = 3  # 16MHz master clock / 7.83MHz CPU clock
    PIN_FC0 = 4  # Function Code 0
    PIN_FC1 = 5  # Function Code 1
    PIN_FC2 = 6  # Function Code 2
    PIN_AS = 7  # Address Strobe
    PIN_UDS = 8  # Upper Data Strobe
    PIN_LDS = 9  # Lower Data Strobe
    PIN_RWB = 10  # Read/Write
    PIN_DTACK = 11  # Data Acknowledge
    PIN_BERR = 12  # Bus Error
    PIN_BR = 13  # Bus Request
    PIN_BG = 14  # Bus Grant
    PIN_BGACK = 15  # Bus Grant Acknowledge
    PIN_IPL0 = 16  # Interrupt Priority 0
    PIN_IPL1 = 17  # Interrupt Priority 1
    PIN_IPL2 = 18  # Interrupt Priority 2
    PIN_RESET = 19  # Reset
    PIN_HALT = 20  # Halt
    PIN_A1_A23 = 21  # Address Bus (24-bit)
    PIN_D0_D15 = 22  # Data Bus (16-bit)

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
            "description": "Stack Pointer (USP)",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x40,
            "size": 4,
            "type": "uint32",
            "access": "r",
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
        self._peripherals["VIA"] = {
            "base": 0xE00000,
            "type": "gpio",
            "description": "Versatile Interface Adapter 6522",
            "registers": {
                "ORB": {
                    "address": 0xE00000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ORA": {
                    "address": 0xE00002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRB": {
                    "address": 0xE00004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRA": {
                    "address": 0xE00006,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "T1C_L": {
                    "address": 0xE00008,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "T1C_H": {
                    "address": 0xE0000A,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "T1L_L": {
                    "address": 0xE0000C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "T1L_H": {
                    "address": 0xE0000E,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "T2C_L": {
                    "address": 0xE00010,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "T2C_H": {
                    "address": 0xE00012,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SR": {
                    "address": 0xE00014,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ACR": {
                    "address": 0xE00016,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PCR": {
                    "address": 0xE00018,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IFR": {
                    "address": 0xE0001E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IER": {
                    "address": 0xE0001E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SCC"] = {
            "base": 0xF00000,
            "type": "serial",
            "description": "SCC 8530 Serial Communications Controller",
            "registers": {
                "SCC_CHA_B": {
                    "address": 0xF00000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCC_CHA_C": {
                    "address": 0xF00002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCC_CHB_D": {
                    "address": 0xF00004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SCC_CHB_CT": {
                    "address": 0xF00006,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["IWM"] = {
            "base": 0x1E00000,
            "type": "storage",
            "description": "Integrated Woz Machine - Floppy Disk Controller",
            "registers": {
                "IWM_DATA": {
                    "address": 0x1E00000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_MODE": {
                    "address": 0x1E00008,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_Q6L": {
                    "address": 0x1E00020,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_Q7L": {
                    "address": 0x1E00022,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_Q6R": {
                    "address": 0x1E00024,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IWM_Q7R": {
                    "address": 0x1E00026,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["VGC"] = {
            "base": 0x00F20000,
            "type": "video",
            "description": "Video Graphics Controller (custom Apple chip)",
            "registers": {
                "VGC_MODE": {
                    "address": 0x00F20000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VGC_START_HI": {
                    "address": 0x00F20002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VGC_START_LO": {
                    "address": 0x00F20004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["ADB"] = {
            "base": 0x01600000,
            "type": "bus",
            "description": "Apple Desktop Bus",
            "registers": {
                "ADB_DATA": {
                    "address": 0x01600000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADB_STATUS": {
                    "address": 0x01600004,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADB_CMD": {
                    "address": 0x01600008,
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
        return f"Macintosh_128K({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Macintosh_128K()
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

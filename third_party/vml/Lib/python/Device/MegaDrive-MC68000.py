"""
Motorola-68000设备定义 - Python模块
生成自: Motorola/68000/Motorola-68000
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: 16/32-bit microprocessor used in Sega Genesis, Amiga, Atari ST, Macintosh
CPU架构: MC68000
位宽: 32位
时钟频率: 7670452 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Motorola_68000:
    """Motorola-68000设备类"""

    # 设备信息
    DEVICE_NAME = "Motorola-68000"
    MANUFACTURER = "Motorola"
    FAMILY = "68000"
    VERSION = "1.0"
    ARCHITECTURE = "MC68000"
    BITS = 32
    CLOCK_FREQUENCY = 7670452

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
    SR_M_BIT = 11  # Master/Interrupt
    SR_S_BIT = 13  # Supervisor/User
    SR_T0_BIT = 14  # Trace Mode 0
    SR_T1_BIT = 15  # Trace Mode 1

    # 内存段定义
    RAM_START = 0x000000
    RAM_END = 0x3FFFFF
    RAM_SIZE = 4194304  # System RAM (4MB)
    ROM_START = 0x000000
    ROM_END = 0x3FFFFF
    ROM_SIZE = 4194304  # Cartridge ROM
    IO_START = 0xA00000
    IO_END = 0xA1FFFF
    IO_SIZE = 131072  # I/O Register Area
    VDP_START = 0xC00000
    VDP_END = 0xC0001F
    VDP_SIZE = 32  # VDP Registers
    VRAM_START = 0xE00000
    VRAM_END = 0xE3FFFF
    VRAM_SIZE = 262144  # Video RAM (256KB)

    # 外设定义
    # Video Display Processor (TMS9918A variant)
    VDP_BASE = 0xC00000
    VDP_DATA_ADDR = 0x00
    VDP_CTRL_ADDR = 0x04
    VDP_HVCOUNT_ADDR = 0x08
    VDP_HVB_STATUS_ADDR = 0x0A
    # Programmable Sound Generator (AY-3-8910)
    PSG_BASE = 0xC00011
    PSG_CH_A_FREQ_ADDR = 0x00
    PSG_CH_A_VOL_ADDR = 0x08
    PSG_CH_B_FREQ_ADDR = 0x02
    PSG_CH_B_VOL_ADDR = 0x09
    PSG_CH_C_FREQ_ADDR = 0x04
    PSG_CH_C_VOL_ADDR = 0x0A
    PSG_NOISE_FREQ_ADDR = 0x06
    PSG_MIXER_ADDR = 0x07
    PSG_ENV_FREQ_ADDR = 0x0D
    PSG_ENV_SHAPE_ADDR = 0x0B
    # Z80 Secondary CPU (Sound)
    Z80_BASE = 0xA00000
    Z80_Z80_RESET_ADDR = 0x00
    Z80_Z80_BUSREQ_ADDR = 0x04
    Z80_Z80_STATUS_ADDR = 0x08
    # Bank Register
    BANK_REG_BASE = 0xA12000
    BANK_REG_ROM_BANK_ADDR = 0x00
    BANK_REG_RAM_BANK_ADDR = 0x04
    # Hardware Version
    HW_VERSION_BASE = 0xA10001
    HW_VERSION_VERSION_ADDR = 0x00
    # Controller Port 1
    CONTROLLER1_BASE = 0xA10003
    CONTROLLER1_DATA_ADDR = 0x00
    CONTROLLER1_CTRL_ADDR = 0x04
    # Controller Port 2
    CONTROLLER2_BASE = 0xA10005
    CONTROLLER2_DATA_ADDR = 0x00
    CONTROLLER2_CTRL_ADDR = 0x04
    # External Port
    EXT_PORT_BASE = 0xA10007
    EXT_PORT_DATA_ADDR = 0x00
    # DMA Controller
    DMA_BASE = 0xA10008
    DMA_SOURCE_ADDR = 0x00
    DMA_DEST_ADDR = 0x04
    DMA_COUNT_ADDR = 0x08
    DMA_CTRL_ADDR = 0x0A
    # Hardware Timer
    TIMER_BASE = 0xA1000E
    TIMER_H_COUNTER_ADDR = 0x00
    TIMER_V_COUNTER_ADDR = 0x04

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
    INT_IRQ1 = 24  # External Interrupt 1 (H-Blank)
    INT_IRQ2 = 25  # External Interrupt 2 (V-Blank)
    INT_IRQ3 = 26  # External Interrupt 3
    INT_IRQ4 = 27  # External Interrupt 4 (D-Req)
    INT_IRQ5 = 28  # External Interrupt 5
    INT_IRQ6 = 29  # External Interrupt 6
    INT_IRQ7 = 30  # External Interrupt 7
    INT_TRAP0 = 32  # TRAP #0
    INT_TRAP1 = 33  # TRAP #1
    INT_TRAP15 = 47  # TRAP #15

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
        self._peripherals["VDP"] = {
            "base": 0xC00000,
            "type": "video",
            "description": "Video Display Processor (TMS9918A variant)",
            "registers": {
                "DATA": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CTRL": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "HVCOUNT": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "HVB_STATUS": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PSG"] = {
            "base": 0xC00011,
            "type": "audio",
            "description": "Programmable Sound Generator (AY-3-8910)",
            "registers": {
                "CH_A_FREQ": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH_A_VOL": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH_B_FREQ": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH_B_VOL": {
                    "address": 0x09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH_C_FREQ": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH_C_VOL": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "NOISE_FREQ": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MIXER": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV_FREQ": {
                    "address": 0x0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ENV_SHAPE": {
                    "address": 0x0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["Z80"] = {
            "base": 0xA00000,
            "type": "cpu",
            "description": "Z80 Secondary CPU (Sound)",
            "registers": {
                "Z80_RESET": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_BUSREQ": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Z80_STATUS": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["BANK_REG"] = {
            "base": 0xA12000,
            "type": "memory",
            "description": "Bank Register",
            "registers": {
                "ROM_BANK": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RAM_BANK": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["HW_VERSION"] = {
            "base": 0xA10001,
            "type": "system",
            "description": "Hardware Version",
            "registers": {
                "VERSION": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CONTROLLER1"] = {
            "base": 0xA10003,
            "type": "input",
            "description": "Controller Port 1",
            "registers": {
                "DATA": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRL": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CONTROLLER2"] = {
            "base": 0xA10005,
            "type": "input",
            "description": "Controller Port 2",
            "registers": {
                "DATA": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CTRL": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["EXT_PORT"] = {
            "base": 0xA10007,
            "type": "io",
            "description": "External Port",
            "registers": {
                "DATA": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["DMA"] = {
            "base": 0xA10008,
            "type": "dma",
            "description": "DMA Controller",
            "registers": {
                "SOURCE": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DEST": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "COUNT": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CTRL": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER"] = {
            "base": 0xA1000E,
            "type": "timer",
            "description": "Hardware Timer",
            "registers": {
                "H_COUNTER": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "V_COUNTER": {
                    "address": 0x04,
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
        return f"Motorola_68000({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Motorola_68000()
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

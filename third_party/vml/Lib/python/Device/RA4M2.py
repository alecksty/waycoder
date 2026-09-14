"""
RA4M2设备定义 - Python模块
生成自: Renesas/RA/RA4M2
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit ARM Cortex-M4 MCU with 256KB Flash, 128KB RAM, 100MHz
CPU架构: ARM-Cortex-M4
位宽: 32位
时钟频率: 100000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class RA4M2:
    """RA4M2设备类"""

    # 设备信息
    DEVICE_NAME = "RA4M2"
    MANUFACTURER = "Renesas"
    FAMILY = "RA"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M4"
    BITS = 32
    CLOCK_FREQUENCY = 100000000

    # 寄存器地址定义
    R0_ADDR = 0x00  # 
    R1_ADDR = 0x04  # 
    R2_ADDR = 0x08  # 
    R3_ADDR = 0x0C  # 
    R4_ADDR = 0x10  # 
    R5_ADDR = 0x14  # 
    SP_ADDR = 0x34  # 
    LR_ADDR = 0x38  # 
    PC_ADDR = 0x3C  # 

    # 内存段定义
    FLASH_START = 0x00000000
    FLASH_END = 0x0003FFFF
    FLASH_SIZE = 262144  # 
    SRAM_START = 0x1FFE0000
    SRAM_END = 0x1FFE7FFF
    SRAM_SIZE = 32768  # SRAM0
    SRAM1_START = 0x20000000
    SRAM1_END = 0x20017FFF
    SRAM1_SIZE = 98304  # SRAM1
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x400FFFFF
    PERIPHERAL_SIZE = 1048576  # 

    # 外设定义
    # Module Stop Control
    MSTP_BASE = 0x40020000
    MSTP_MSTPCR_A_ADDR = 0x20
    MSTP_MSTPCR_A_MSTP41_BIT = 9  # GPIO A stop
    MSTP_MSTPCR_A_MSTP42_BIT = 10  # GPIO B stop
    MSTP_MSTPCR_B_ADDR = 0x24
    MSTP_MSTPCR_C_ADDR = 0x28
    MSTP_MSTPCR_D_ADDR = 0x2C
    # Interrupt Controller Unit
    ICU_BASE = 0x40030000
    ICU_IRQCR0_ADDR = 0x600
    ICU_IRQCR1_ADDR = 0x602
    # General Purpose I/O Port A
    GPIOA_BASE = 0x40040000
    GPIOA_PDR_ADDR = 0x00
    GPIOA_PODR_ADDR = 0x04
    GPIOA_PIDR_ADDR = 0x08
    GPIOA_PMR_ADDR = 0x10
    GPIOA_PCR_ADDR = 0x18
    # General Purpose I/O Port B
    GPIOB_BASE = 0x40040020
    GPIOB_PDR_ADDR = 0x00
    GPIOB_PODR_ADDR = 0x04
    GPIOB_PIDR_ADDR = 0x08
    GPIOB_PMR_ADDR = 0x10
    # SCI UART 0
    SCIUART0_BASE = 0x40070000
    SCIUART0_SCR_ADDR = 0x00
    SCIUART0_BRR_ADDR = 0x04
    SCIUART0_TDR_ADDR = 0x08
    SCIUART0_RDR_ADDR = 0x0C
    SCIUART0_SSR_ADDR = 0x10

    # 中断向量定义
    INT_RESET = 0  # 
    INT_SVCALL = 11  # 
    INT_SCIUART0_RXI = 24  # SCI UART0 Receive Interrupt
    INT_SCIUART0_TXI = 25  # SCI UART0 Transmit Interrupt

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["R0"] = {
            "address": 0x00,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R1"] = {
            "address": 0x04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R2"] = {
            "address": 0x08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R3"] = {
            "address": 0x0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R4"] = {
            "address": 0x10,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R5"] = {
            "address": 0x14,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x34,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["LR"] = {
            "address": 0x38,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["MSTP"] = {
            "base": 0x40020000,
            "type": "ClockControl",
            "description": "Module Stop Control",
            "registers": {
                "MSTPCR_A": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MSTPCR_B": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MSTPCR_C": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MSTPCR_D": {
                    "address": 0x2C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ICU"] = {
            "base": 0x40030000,
            "type": "InterruptControl",
            "description": "Interrupt Controller Unit",
            "registers": {
                "IRQCR0": {
                    "address": 0x600,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "IRQCR1": {
                    "address": 0x602,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOA"] = {
            "base": 0x40040000,
            "type": "GPIO",
            "description": "General Purpose I/O Port A",
            "registers": {
                "PDR": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "PODR": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "PIDR": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "PMR": {
                    "address": 0x10,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "PCR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOB"] = {
            "base": 0x40040020,
            "type": "GPIO",
            "description": "General Purpose I/O Port B",
            "registers": {
                "PDR": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "PODR": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "PIDR": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "PMR": {
                    "address": 0x10,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["SCIUART0"] = {
            "base": 0x40070000,
            "type": "UART",
            "description": "SCI UART 0",
            "registers": {
                "SCR": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "BRR": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TDR": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RDR": {
                    "address": 0x0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SSR": {
                    "address": 0x10,
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
        return f"RA4M2({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = RA4M2()
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

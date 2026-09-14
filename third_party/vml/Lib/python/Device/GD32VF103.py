"""
GD32VF103设备定义 - Python模块
生成自: GigaDevice/GD32/GD32VF103
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit RISC-V RV32IMAC MCU with 128KB Flash, 32KB RAM, 108MHz, STM32F103 compatible
CPU架构: RISC-V
位宽: 32位
时钟频率: 108000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class GD32VF103:
    """GD32VF103设备类"""

    # 设备信息
    DEVICE_NAME = "GD32VF103"
    MANUFACTURER = "GigaDevice"
    FAMILY = "GD32"
    VERSION = "1.0"
    ARCHITECTURE = "RISC-V"
    BITS = 32
    CLOCK_FREQUENCY = 108000000

    # 寄存器地址定义
    X1_ADDR = 0x04  # Return Address
    X2_ADDR = 0x08  # Stack Pointer (SP)
    X3_ADDR = 0x0C  # Global Pointer (GP)
    X8_ADDR = 0x20  # Frame Pointer (FP)
    X10_ADDR = 0x28  # Function Argument (A0)
    X11_ADDR = 0x2C  # Function Argument (A1)
    PC_ADDR = 0x3C  # Program Counter

    # 内存段定义
    FLASH_START = 0x08000000
    FLASH_END = 0x0801FFFF
    FLASH_SIZE = 131072  # 
    SRAM_START = 0x20000000
    SRAM_END = 0x20007FFF
    SRAM_SIZE = 32768  # 
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x4003FFFF
    PERIPHERAL_SIZE = 262144  # 

    # 外设定义
    # Reset and Clock Control
    RCU_BASE = 0x40021000
    RCU_CTL_ADDR = 0x00
    RCU_CFG0_ADDR = 0x04
    RCU_CFG1_ADDR = 0x08
    RCU_APB2EN_ADDR = 0x18
    RCU_APB2EN_PAEN_BIT = 2  # GPIOA enable
    RCU_APB2EN_PBEN_BIT = 3  # GPIOB enable
    RCU_APB2EN_PCEN_BIT = 4  # GPIOC enable
    RCU_APB2EN_USART0EN_BIT = 14  # USART0 enable
    RCU_APB1EN_ADDR = 0x1C
    # General Purpose I/O Port A
    GPIOA_BASE = 0x40010800
    GPIOA_CTL0_ADDR = 0x00
    GPIOA_CTL1_ADDR = 0x04
    GPIOA_ISTAT_ADDR = 0x08
    GPIOA_OCTL_ADDR = 0x0C
    GPIOA_BOP_ADDR = 0x10
    GPIOA_BC_ADDR = 0x14
    # General Purpose I/O Port B
    GPIOB_BASE = 0x40010C00
    GPIOB_CTL0_ADDR = 0x00
    GPIOB_CTL1_ADDR = 0x04
    GPIOB_ISTAT_ADDR = 0x08
    GPIOB_OCTL_ADDR = 0x0C
    GPIOB_BOP_ADDR = 0x10
    GPIOB_BC_ADDR = 0x14
    # General Purpose I/O Port C
    GPIOC_BASE = 0x40011000
    GPIOC_CTL0_ADDR = 0x00
    GPIOC_CTL1_ADDR = 0x04
    GPIOC_ISTAT_ADDR = 0x08
    GPIOC_OCTL_ADDR = 0x0C
    GPIOC_BOP_ADDR = 0x10
    GPIOC_BC_ADDR = 0x14
    # USART0
    USART0_BASE = 0x40013800
    USART0_STATR_ADDR = 0x00
    USART0_DATAR_ADDR = 0x04
    USART0_BRR_ADDR = 0x08
    USART0_CTLR1_ADDR = 0x0C

    # 中断向量定义
    INT_RESET = 1  # 
    INT_MACHINESOFTWARE = 3  # 
    INT_MACHINETIMER = 7  # 
    INT_MACHINEEXTERNAL = 11  # 
    INT_USART0 = 25  # USART0 Global Interrupt

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["x1"] = {
            "address": 0x04,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Return Address",
            "value": 0
        }
        self._registers["x2"] = {
            "address": 0x08,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Stack Pointer (SP)",
            "value": 0
        }
        self._registers["x3"] = {
            "address": 0x0C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Global Pointer (GP)",
            "value": 0
        }
        self._registers["x8"] = {
            "address": 0x20,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Frame Pointer (FP)",
            "value": 0
        }
        self._registers["x10"] = {
            "address": 0x28,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Function Argument (A0)",
            "value": 0
        }
        self._registers["x11"] = {
            "address": 0x2C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Function Argument (A1)",
            "value": 0
        }
        self._registers["pc"] = {
            "address": 0x3C,
            "size": 4,
            "type": "uint32",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["RCU"] = {
            "base": 0x40021000,
            "type": "ResetClock",
            "description": "Reset and Clock Control",
            "registers": {
                "CTL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFG0": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFG1": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB2EN": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "APB1EN": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOA"] = {
            "base": 0x40010800,
            "type": "GPIO",
            "description": "General Purpose I/O Port A",
            "registers": {
                "CTL0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTL1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ISTAT": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OCTL": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BOP": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BC": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOB"] = {
            "base": 0x40010C00,
            "type": "GPIO",
            "description": "General Purpose I/O Port B",
            "registers": {
                "CTL0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTL1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ISTAT": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OCTL": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BOP": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BC": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOC"] = {
            "base": 0x40011000,
            "type": "GPIO",
            "description": "General Purpose I/O Port C",
            "registers": {
                "CTL0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTL1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ISTAT": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OCTL": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BOP": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BC": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART0"] = {
            "base": 0x40013800,
            "type": "UART",
            "description": "USART0",
            "registers": {
                "STATR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATAR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTLR1": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
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
        return f"GD32VF103({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = GD32VF103()
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

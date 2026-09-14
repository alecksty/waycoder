"""
XMC4500设备定义 - Python模块
生成自: Infineon/XMC4000/XMC4500
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit ARM Cortex-M4 Industrial MCU with 1MB Flash, 160KB RAM, 120MHz
CPU架构: ARM-Cortex-M4
位宽: 32位
时钟频率: 120000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class XMC4500:
    """XMC4500设备类"""

    # 设备信息
    DEVICE_NAME = "XMC4500"
    MANUFACTURER = "Infineon"
    FAMILY = "XMC4000"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M4"
    BITS = 32
    CLOCK_FREQUENCY = 120000000

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
    FLASH_START = 0x08000000
    FLASH_END = 0x080FFFFF
    FLASH_SIZE = 1048576  # 
    SRAM_START = 0x1FF00000
    SRAM_END = 0x1FF0FFFF
    SRAM_SIZE = 65536  # 
    SRAM_COM_START = 0x20000000
    SRAM_COM_END = 0x20007FFF
    SRAM_COM_SIZE = 32768  # Communication Memory
    SRAM_CPU_START = 0x20010000
    SRAM_CPU_END = 0x2001FFFF
    SRAM_CPU_SIZE = 65536  # CPU SRAM
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x4FFFFFFF
    PERIPHERAL_SIZE = 268435456  # 

    # 外设定义
    # System Control Unit
    SCU_BASE = 0x40020000
    SCU_CLKCR_ADDR = 0x00
    SCU_CLKCR_PCLK_SEL_BIT = 0  # CPU clock selection
    SCU_CLKCR_FBKDIV_BIT = 16  # Feedback divider
    SCU_PLLCONFIG_ADDR = 0x04
    SCU_OSCHPCTRL_ADDR = 0x08
    SCU_CGATSET0_ADDR = 0x20
    SCU_CGATSET0_CG_GATE_GPIO_BIT = 4  # GPIO gate enable
    SCU_CGATCLR0_ADDR = 0x24
    # Port 0
    PORT0_BASE = 0x48000000
    PORT0_OUT_ADDR = 0x00
    PORT0_OMR_ADDR = 0x04
    PORT0_IOCR0_ADDR = 0x10
    PORT0_IOCR4_ADDR = 0x14
    PORT0_IOCR8_ADDR = 0x18
    PORT0_IOCR12_ADDR = 0x1C
    PORT0_IN_ADDR = 0x24
    # Port 1
    PORT1_BASE = 0x48010000
    PORT1_OUT_ADDR = 0x00
    PORT1_OMR_ADDR = 0x04
    PORT1_IOCR0_ADDR = 0x10
    PORT1_IOCR4_ADDR = 0x14
    PORT1_IOCR8_ADDR = 0x18
    PORT1_IOCR12_ADDR = 0x1C
    PORT1_IN_ADDR = 0x24
    # Port 2
    PORT2_BASE = 0x48020000
    PORT2_OUT_ADDR = 0x00
    PORT2_OMR_ADDR = 0x04
    PORT2_IOCR0_ADDR = 0x10
    PORT2_IOCR4_ADDR = 0x14
    PORT2_IN_ADDR = 0x24
    # Universal Serial Interface 0 (UART)
    USIC0_BASE = 0x48030000
    USIC0_CCR_ADDR = 0x00
    USIC0_PCR_ADDR = 0x04
    USIC0_RBUF_ADDR = 0x08
    USIC0_TBUF_ADDR = 0x0C
    USIC0_BRG_ADDR = 0x10

    # 中断向量定义
    INT_RESET = 0  # 
    INT_SVCALL = 11  # 
    INT_USIC0_SR0 = 12  # USIC0 Service Request 0

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
        self._peripherals["SCU"] = {
            "base": 0x40020000,
            "type": "ClockControl",
            "description": "System Control Unit",
            "registers": {
                "CLKCR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PLLCONFIG": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OSCHPCTRL": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CGATSET0": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CGATCLR0": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PORT0"] = {
            "base": 0x48000000,
            "type": "GPIO",
            "description": "Port 0",
            "registers": {
                "OUT": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OMR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR0": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR4": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR8": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR12": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IN": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PORT1"] = {
            "base": 0x48010000,
            "type": "GPIO",
            "description": "Port 1",
            "registers": {
                "OUT": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OMR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR0": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR4": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR8": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR12": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IN": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["PORT2"] = {
            "base": 0x48020000,
            "type": "GPIO",
            "description": "Port 2",
            "registers": {
                "OUT": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OMR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR0": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IOCR4": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IN": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USIC0"] = {
            "base": 0x48030000,
            "type": "UART",
            "description": "Universal Serial Interface 0 (UART)",
            "registers": {
                "CCR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PCR": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RBUF": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TBUF": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BRG": {
                    "address": 0x10,
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
        return f"XMC4500({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = XMC4500()
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

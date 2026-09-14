"""
TMS320F280049设备定义 - Python模块
生成自: Texas Instruments/C2000/TMS320F280049
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit C28x DSP + CLA MCU with 256KB Flash, 100KB RAM, 100MHz
CPU架构: C28x-DSP
位宽: 32位
时钟频率: 100000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class TMS320F280049:
    """TMS320F280049设备类"""

    # 设备信息
    DEVICE_NAME = "TMS320F280049"
    MANUFACTURER = "Texas Instruments"
    FAMILY = "C2000"
    VERSION = "1.0"
    ARCHITECTURE = "C28x-DSP"
    BITS = 32
    CLOCK_FREQUENCY = 100000000

    # 寄存器地址定义
    AL_ADDR = 0x00  # Accumulator Low
    AH_ADDR = 0x02  # Accumulator High
    PH_ADDR = 0x04  # Product High
    PL_ADDR = 0x06  # Product Low
    TREG_ADDR = 0x08  # Temporary Register
    AR0_ADDR = 0x0A  # 
    AR1_ADDR = 0x0C  # 
    ST0_ADDR = 0x20  # Status 0
    ST1_ADDR = 0x22  # Status 1
    PC_ADDR = 0x24  # Program Counter
    SP_ADDR = 0x26  # Stack Pointer

    # 内存段定义
    FLASH_START = 0x080000
    FLASH_END = 0x0BFFFF
    FLASH_SIZE = 262144  # 
    SRAM_LS_START = 0x008000
    SRAM_LS_END = 0x00BFFF
    SRAM_LS_SIZE = 16384  # Local Shared RAM
    SRAM_GS_START = 0x00C000
    SRAM_GS_END = 0x01FFFF
    SRAM_GS_SIZE = 81920  # Global Shared RAM
    PERIPHERAL_START = 0x400000
    PERIPHERAL_END = 0x40FFFF
    PERIPHERAL_SIZE = 65536  # 

    # 外设定义
    # PLL Clock Control
    PLL_BASE = 0x5C10
    PLL_SYSPLLCTL1_ADDR = 0x00
    PLL_SYSPLLCTL2_ADDR = 0x02
    PLL_CLKSRCCTL1_ADDR = 0x04
    PLL_CLKSRCCTL2_ADDR = 0x06
    # GPIO Control Registers
    GPIO_CTRL_BASE = 0x7C00
    GPIO_CTRL_GPACTRL_ADDR = 0x00
    GPIO_CTRL_GPAQSEL1_ADDR = 0x02
    GPIO_CTRL_GPAQSEL2_ADDR = 0x04
    GPIO_CTRL_GPAMUX1_ADDR = 0x06
    GPIO_CTRL_GPAMUX2_ADDR = 0x08
    GPIO_CTRL_GPADIR_ADDR = 0x0A
    GPIO_CTRL_GPAPUD_ADDR = 0x0C
    # GPIO Data Registers
    GPIO_DATA_BASE = 0x7F00
    GPIO_DATA_GPADAT_ADDR = 0x00
    GPIO_DATA_GPASET_ADDR = 0x02
    GPIO_DATA_GPACLEAR_ADDR = 0x04
    GPIO_DATA_GPATOGGLE_ADDR = 0x06
    GPIO_DATA_GPBDAT_ADDR = 0x08
    GPIO_DATA_GPBSET_ADDR = 0x0A
    GPIO_DATA_GPBCLEAR_ADDR = 0x0C
    GPIO_DATA_GPBTOGGLE_ADDR = 0x0E
    # GPIO B Control
    GPIO_B_CTRL_BASE = 0x7C20
    GPIO_B_CTRL_GPBMUX1_ADDR = 0x00
    GPIO_B_CTRL_GPBMUX2_ADDR = 0x02
    GPIO_B_CTRL_GPBDIR_ADDR = 0x04
    GPIO_B_CTRL_GPBPUD_ADDR = 0x06
    # SCI-A UART
    SCI_A_BASE = 0x7320
    SCI_A_SCICCR_ADDR = 0x00
    SCI_A_SCICTL1_ADDR = 0x02
    SCI_A_SCIBAUD_ADDR = 0x04
    SCI_A_SCIRXBUF_ADDR = 0x0A
    SCI_A_SCITXBUF_ADDR = 0x0C

    # 中断向量定义
    INT_RESET = 1  # 
    INT_SCIA_RX = 8  # SCI-A Receive Interrupt
    INT_SCIA_TX = 9  # SCI-A Transmit Interrupt

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["AL"] = {
            "address": 0x00,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Accumulator Low",
            "value": 0
        }
        self._registers["AH"] = {
            "address": 0x02,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Accumulator High",
            "value": 0
        }
        self._registers["PH"] = {
            "address": 0x04,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Product High",
            "value": 0
        }
        self._registers["PL"] = {
            "address": 0x06,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Product Low",
            "value": 0
        }
        self._registers["TREG"] = {
            "address": 0x08,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Temporary Register",
            "value": 0
        }
        self._registers["AR0"] = {
            "address": 0x0A,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["AR1"] = {
            "address": 0x0C,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["ST0"] = {
            "address": 0x20,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Status 0",
            "value": 0
        }
        self._registers["ST1"] = {
            "address": 0x22,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Status 1",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x24,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x26,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PLL"] = {
            "base": 0x5C10,
            "type": "ClockControl",
            "description": "PLL Clock Control",
            "registers": {
                "SYSPLLCTL1": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SYSPLLCTL2": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CLKSRCCTL1": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CLKSRCCTL2": {
                    "address": 0x06,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO_CTRL"] = {
            "base": 0x7C00,
            "type": "GPIO",
            "description": "GPIO Control Registers",
            "registers": {
                "GPACTRL": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPAQSEL1": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPAQSEL2": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPAMUX1": {
                    "address": 0x06,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPAMUX2": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPADIR": {
                    "address": 0x0A,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPAPUD": {
                    "address": 0x0C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO_DATA"] = {
            "base": 0x7F00,
            "type": "GPIO",
            "description": "GPIO Data Registers",
            "registers": {
                "GPADAT": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPASET": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPACLEAR": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPATOGGLE": {
                    "address": 0x06,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPBDAT": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPBSET": {
                    "address": 0x0A,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPBCLEAR": {
                    "address": 0x0C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPBTOGGLE": {
                    "address": 0x0E,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO_B_CTRL"] = {
            "base": 0x7C20,
            "type": "GPIO",
            "description": "GPIO B Control",
            "registers": {
                "GPBMUX1": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPBMUX2": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPBDIR": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "GPBPUD": {
                    "address": 0x06,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["SCI_A"] = {
            "base": 0x7320,
            "type": "UART",
            "description": "SCI-A UART",
            "registers": {
                "SCICCR": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SCICTL1": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SCIBAUD": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SCIRXBUF": {
                    "address": 0x0A,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SCITXBUF": {
                    "address": 0x0C,
                    "size": 2,
                    "type": "uint16",
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
        return f"TMS320F280049({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = TMS320F280049()
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

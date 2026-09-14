"""
EFR32MG24设备定义 - Python模块
生成自: Silicon Labs/EFR32/EFR32MG24
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 32-bit ARM Cortex-M33 MCU with 1536KB Flash, 256KB RAM, 78MHz, Zigbee/Thread/Matter
CPU架构: ARM-Cortex-M33
位宽: 32位
时钟频率: 78000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class EFR32MG24:
    """EFR32MG24设备类"""

    # 设备信息
    DEVICE_NAME = "EFR32MG24"
    MANUFACTURER = "Silicon Labs"
    FAMILY = "EFR32"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M33"
    BITS = 32
    CLOCK_FREQUENCY = 78000000

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
    FLASH_END = 0x0817FFFF
    FLASH_SIZE = 1572864  # 
    SRAM_START = 0x20000000
    SRAM_END = 0x2003FFFF
    SRAM_SIZE = 262144  # 
    PERIPHERAL_START = 0x40000000
    PERIPHERAL_END = 0x4007FFFF
    PERIPHERAL_SIZE = 524288  # 

    # 外设定义
    # Clock Management Unit
    CMU_BASE = 0x40080000
    CMU_CTRL_ADDR = 0x00
    CMU_HFCORECLKCFG_ADDR = 0x08
    CMU_HFPERCLKEN0_ADDR = 0x10
    CMU_HFPERCLKEN0_GPIOEN_BIT = 4  # GPIO clock enable
    CMU_HFPERCLKEN0_USART0EN_BIT = 12  # USART0 clock enable
    CMU_HFPERCLKEN0_USART1EN_BIT = 13  # USART1 clock enable
    CMU_LFBCLKEN0_ADDR = 0x20
    # GPIO Controller
    GPIO_BASE = 0x40088000
    GPIO_PORT_A_CTRL_ADDR = 0x00
    GPIO_PORT_B_CTRL_ADDR = 0x04
    GPIO_PORT_C_CTRL_ADDR = 0x08
    GPIO_PORT_D_CTRL_ADDR = 0x0C
    GPIO_MODEL_ADDR = 0x10
    GPIO_MODEH_ADDR = 0x14
    GPIO_DOUT_ADDR = 0x1C
    GPIO_DOUTSET_ADDR = 0x20
    GPIO_DOUTCLR_ADDR = 0x24
    GPIO_DOUTTGL_ADDR = 0x28
    GPIO_DIN_ADDR = 0x2C
    # GPIO Port A extended
    GPIO_PA_BASE = 0x40088400
    GPIO_PA_PA_CFG_ADDR = 0x00
    GPIO_PA_PA_PINOUT_ADDR = 0x04
    # GPIO Port B extended
    GPIO_PB_BASE = 0x40088800
    GPIO_PB_PB_CFG_ADDR = 0x00
    # USART 0
    USART0_BASE = 0x40060000
    USART0_CTRL_ADDR = 0x00
    USART0_CMD_ADDR = 0x04
    USART0_STATUS_ADDR = 0x08
    USART0_RXDATA_ADDR = 0x0C
    USART0_TXDATA_ADDR = 0x10
    USART0_CLKDIV_ADDR = 0x14

    # 中断向量定义
    INT_RESET = 0  # 
    INT_SVCALL = 11  # 
    INT_USART0_RX = 12  # USART0 Receive Interrupt
    INT_USART0_TX = 13  # USART0 Transmit Interrupt

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
        self._peripherals["CMU"] = {
            "base": 0x40080000,
            "type": "ClockControl",
            "description": "Clock Management Unit",
            "registers": {
                "CTRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HFCORECLKCFG": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "HFPERCLKEN0": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LFBCLKEN0": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO"] = {
            "base": 0x40088000,
            "type": "GPIO",
            "description": "GPIO Controller",
            "registers": {
                "PORT_A_CTRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PORT_B_CTRL": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PORT_C_CTRL": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PORT_D_CTRL": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MODEL": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MODEH": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DOUT": {
                    "address": 0x1C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DOUTSET": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DOUTCLR": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DOUTTGL": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIN": {
                    "address": 0x2C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO_PA"] = {
            "base": 0x40088400,
            "type": "GPIO",
            "description": "GPIO Port A extended",
            "registers": {
                "PA_CFG": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PA_PINOUT": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO_PB"] = {
            "base": 0x40088800,
            "type": "GPIO",
            "description": "GPIO Port B extended",
            "registers": {
                "PB_CFG": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART0"] = {
            "base": 0x40060000,
            "type": "UART",
            "description": "USART 0",
            "registers": {
                "CTRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CMD": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RXDATA": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TXDATA": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLKDIV": {
                    "address": 0x14,
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
        return f"EFR32MG24({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = EFR32MG24()
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

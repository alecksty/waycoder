"""
i.MX RT1062设备定义 - Python模块
生成自: NXP/i.MX RT/i.MX RT1062
版本: 1.0
日期: 2026-04-29
作者: VML Team
描述: 32-bit ARM Cortex-M7 MCU with 1MB SRAM, 600MHz, crossover processor
CPU架构: ARM-Cortex-M7
位宽: 32位
时钟频率: 528000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class i.MX RT1062:
    """i.MX RT1062设备类"""

    # 设备信息
    DEVICE_NAME = "i.MX RT1062"
    MANUFACTURER = "NXP"
    FAMILY = "i.MX RT"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M7"
    BITS = 32
    CLOCK_FREQUENCY = 528000000

    # 外设定义
    # LPUART 1
    UART1_BASE = 0x40184000
    UART1_VERID_ADDR = 0x000
    UART1_CTRL_ADDR = 0x010
    UART1_STAT_ADDR = 0x014
    UART1_DATA_ADDR = 0x01C
    UART1_BAUD_ADDR = 0x024
    # LPUART 2
    UART2_BASE = 0x40188000
    UART2_CTRL_ADDR = 0x010
    UART2_STAT_ADDR = 0x014
    UART2_DATA_ADDR = 0x01C
    UART2_BAUD_ADDR = 0x024
    # GPIO 1
    GPIO1_BASE = 0x401B8000
    GPIO1_DR_ADDR = 0x000
    GPIO1_GDIR_ADDR = 0x004
    GPIO1_PSR_ADDR = 0x008
    GPIO1_ICR1_ADDR = 0x00C
    GPIO1_ICR2_ADDR = 0x010
    GPIO1_IMR_ADDR = 0x014
    GPIO1_ISR_ADDR = 0x018
    GPIO1_EDGE_SEL_ADDR = 0x01C
    # GPT 定时器 1
    GPT1_BASE = 0x401EC000
    GPT1_CR_ADDR = 0x000
    GPT1_PR_ADDR = 0x004
    GPT1_SR_ADDR = 0x008
    GPT1_IR_ADDR = 0x00C
    GPT1_OCR1_ADDR = 0x010
    GPT1_CNT_ADDR = 0x024
    # USB OTG 1
    USB1_BASE = 0x402E0000
    USB1_ID_ADDR = 0x000
    USB1_OTGSC_ADDR = 0x00C
    USB1_USBCMD_ADDR = 0x100
    USB1_PORTSC1_ADDR = 0x184

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["UART1"] = {
            "base": 0x40184000,
            "type": "uart",
            "description": "LPUART 1",
            "registers": {
                "VERID": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTRL": {
                    "address": 0x010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STAT": {
                    "address": 0x014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA": {
                    "address": 0x01C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BAUD": {
                    "address": 0x024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART2"] = {
            "base": 0x40188000,
            "type": "uart",
            "description": "LPUART 2",
            "registers": {
                "CTRL": {
                    "address": 0x010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STAT": {
                    "address": 0x014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA": {
                    "address": 0x01C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BAUD": {
                    "address": 0x024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO1"] = {
            "base": 0x401B8000,
            "type": "gpio",
            "description": "GPIO 1",
            "registers": {
                "DR": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "GDIR": {
                    "address": 0x004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSR": {
                    "address": 0x008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ICR1": {
                    "address": 0x00C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ICR2": {
                    "address": 0x010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IMR": {
                    "address": 0x014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ISR": {
                    "address": 0x018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EDGE_SEL": {
                    "address": 0x01C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPT1"] = {
            "base": 0x401EC000,
            "type": "timer",
            "description": "GPT 定时器 1",
            "registers": {
                "CR": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PR": {
                    "address": 0x004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SR": {
                    "address": 0x008,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IR": {
                    "address": 0x00C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OCR1": {
                    "address": 0x010,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USB1"] = {
            "base": 0x402E0000,
            "type": "usb",
            "description": "USB OTG 1",
            "registers": {
                "ID": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OTGSC": {
                    "address": 0x00C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "USBCMD": {
                    "address": 0x100,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PORTSC1": {
                    "address": 0x184,
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
        return f"i.MX RT1062({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = i.MX RT1062()
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

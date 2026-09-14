"""
MSP432P401R设备定义 - Python模块
生成自: Texas Instruments/MSP432/MSP432P401R
版本: 1.0
日期: 2026-04-29
作者: VML Team
描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 64KB SRAM, 48MHz, FPU
CPU架构: ARM-Cortex-M4F
位宽: 32位
时钟频率: 48000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class MSP432P401R:
    """MSP432P401R设备类"""

    # 设备信息
    DEVICE_NAME = "MSP432P401R"
    MANUFACTURER = "Texas Instruments"
    FAMILY = "MSP432"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M4F"
    BITS = 32
    CLOCK_FREQUENCY = 48000000

    # 外设定义
    # eUSCI_A0 UART
    UART0_BASE = 0x40001000
    UART0_CTLW0_ADDR = 0x00
    UART0_BRW_ADDR = 0x06
    UART0_UCA0TXBUF_ADDR = 0x08
    UART0_UCA0RXBUF_ADDR = 0x0A
    UART0_IFG_ADDR = 0x0C
    UART0_IE_ADDR = 0x0E
    # eUSCI_A1 UART
    UART1_BASE = 0x40002000
    UART1_CTLW0_ADDR = 0x00
    UART1_BRW_ADDR = 0x06
    UART1_TXBUF_ADDR = 0x08
    UART1_RXBUF_ADDR = 0x0A
    UART1_IFG_ADDR = 0x0C
    UART1_IE_ADDR = 0x0E
    # Timer_A0 16bit
    TIMER0_BASE = 0x40003000
    TIMER0_CTL_ADDR = 0x00
    TIMER0_R_ADDR = 0x10
    TIMER0_CCR0_ADDR = 0x12
    TIMER0_CCR1_ADDR = 0x14
    TIMER0_CCR2_ADDR = 0x16
    TIMER0_EX0_ADDR = 0x20
    # ADC14 14-bit
    ADC14_BASE = 0x40006000
    ADC14_CTL0_ADDR = 0x00
    ADC14_CTL1_ADDR = 0x02
    ADC14_LO_ADDR = 0x04
    ADC14_HI_ADDR = 0x06
    ADC14_MCTL0_ADDR = 0x08
    ADC14_MEM0_ADDR = 0x20

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
        self._peripherals["UART0"] = {
            "base": 0x40001000,
            "type": "uart",
            "description": "eUSCI_A0 UART",
            "registers": {
                "CTLW0": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BRW": {
                    "address": 0x06,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "UCA0TXBUF": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "UCA0RXBUF": {
                    "address": 0x0A,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "IFG": {
                    "address": 0x0C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "IE": {
                    "address": 0x0E,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["UART1"] = {
            "base": 0x40002000,
            "type": "uart",
            "description": "eUSCI_A1 UART",
            "registers": {
                "CTLW0": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "BRW": {
                    "address": 0x06,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "TXBUF": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "RXBUF": {
                    "address": 0x0A,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "IFG": {
                    "address": 0x0C,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "IE": {
                    "address": 0x0E,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER0"] = {
            "base": 0x40003000,
            "type": "timer",
            "description": "Timer_A0 16bit",
            "registers": {
                "CTL": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "R": {
                    "address": 0x10,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CCR0": {
                    "address": 0x12,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x14,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CCR2": {
                    "address": 0x16,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "EX0": {
                    "address": 0x20,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC14"] = {
            "base": 0x40006000,
            "type": "adc",
            "description": "ADC14 14-bit",
            "registers": {
                "CTL0": {
                    "address": 0x00,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "CTL1": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "LO": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "HI": {
                    "address": 0x06,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "MCTL0": {
                    "address": 0x08,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "MEM0": {
                    "address": 0x20,
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
        return f"MSP432P401R({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = MSP432P401R()
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

"""
TM4C123GH6PM设备定义 - Python模块
生成自: Texas Instruments/Tiva C/TM4C123GH6PM
版本: 1.0
日期: 2026-04-29
作者: VML Team
描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 32KB SRAM, 80MHz, USB
CPU架构: ARM-Cortex-M4F
位宽: 32位
时钟频率: 80000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class TM4C123GH6PM:
    """TM4C123GH6PM设备类"""

    # 设备信息
    DEVICE_NAME = "TM4C123GH6PM"
    MANUFACTURER = "Texas Instruments"
    FAMILY = "Tiva C"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M4F"
    BITS = 32
    CLOCK_FREQUENCY = 80000000

    # 外设定义
    # UART 0
    UART0_BASE = 0x4000C000
    UART0_DR_ADDR = 0x000
    UART0_FR_ADDR = 0x018
    UART0_IBRD_ADDR = 0x024
    UART0_FBRD_ADDR = 0x028
    UART0_LCRH_ADDR = 0x02C
    UART0_CTL_ADDR = 0x030
    UART0_IM_ADDR = 0x038
    UART0_RIS_ADDR = 0x03C
    UART0_ICR_ADDR = 0x044
    # UART 1
    UART1_BASE = 0x4000D000
    UART1_DR_ADDR = 0x000
    UART1_FR_ADDR = 0x018
    UART1_IBRD_ADDR = 0x024
    UART1_FBRD_ADDR = 0x028
    UART1_LCRH_ADDR = 0x02C
    UART1_CTL_ADDR = 0x030
    # GPIO Port A
    GPIOA_BASE = 0x40004000
    GPIOA_DATA_ADDR = 0x3FC
    GPIOA_DIR_ADDR = 0x400
    GPIOA_IS_ADDR = 0x404
    GPIOA_IBE_ADDR = 0x408
    GPIOA_IEV_ADDR = 0x40C
    GPIOA_IM_ADDR = 0x410
    GPIOA_RIS_ADDR = 0x414
    GPIOA_MIS_ADDR = 0x418
    GPIOA_ICR_ADDR = 0x41C
    GPIOA_AFSEL_ADDR = 0x420
    GPIOA_DEN_ADDR = 0x51C
    # 16/32-bit Timer 0
    TIMER0_BASE = 0x40030000
    TIMER0_CFG_ADDR = 0x000
    TIMER0_TAMR_ADDR = 0x004
    TIMER0_CTL_ADDR = 0x00C
    TIMER0_ILR_ADDR = 0x028
    TIMER0_V_ADDR = 0x038
    TIMER0_ICR_ADDR = 0x024
    # ADC 0
    ADC0_BASE = 0x40038000
    ADC0_ACTSS_ADDR = 0x000
    ADC0_EMUX_ADDR = 0x014
    ADC0_SSMUX0_ADDR = 0x040
    ADC0_SSFIFO0_ADDR = 0x048
    ADC0_PROC_ADDR = 0x030

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
            "base": 0x4000C000,
            "type": "uart",
            "description": "UART 0",
            "registers": {
                "DR": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FR": {
                    "address": 0x018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IBRD": {
                    "address": 0x024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FBRD": {
                    "address": 0x028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LCRH": {
                    "address": 0x02C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTL": {
                    "address": 0x030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IM": {
                    "address": 0x038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RIS": {
                    "address": 0x03C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ICR": {
                    "address": 0x044,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["UART1"] = {
            "base": 0x4000D000,
            "type": "uart",
            "description": "UART 1",
            "registers": {
                "DR": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FR": {
                    "address": 0x018,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IBRD": {
                    "address": 0x024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "FBRD": {
                    "address": 0x028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "LCRH": {
                    "address": 0x02C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTL": {
                    "address": 0x030,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOA"] = {
            "base": 0x40004000,
            "type": "gpio",
            "description": "GPIO Port A",
            "registers": {
                "DATA": {
                    "address": 0x3FC,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DIR": {
                    "address": 0x400,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IS": {
                    "address": 0x404,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IBE": {
                    "address": 0x408,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IEV": {
                    "address": 0x40C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IM": {
                    "address": 0x410,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RIS": {
                    "address": 0x414,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "MIS": {
                    "address": 0x418,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ICR": {
                    "address": 0x41C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AFSEL": {
                    "address": 0x420,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DEN": {
                    "address": 0x51C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER0"] = {
            "base": 0x40030000,
            "type": "timer",
            "description": "16/32-bit Timer 0",
            "registers": {
                "CFG": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TAMR": {
                    "address": 0x004,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CTL": {
                    "address": 0x00C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ILR": {
                    "address": 0x028,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "V": {
                    "address": 0x038,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ICR": {
                    "address": 0x024,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC0"] = {
            "base": 0x40038000,
            "type": "adc",
            "description": "ADC 0",
            "registers": {
                "ACTSS": {
                    "address": 0x000,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "EMUX": {
                    "address": 0x014,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSMUX0": {
                    "address": 0x040,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SSFIFO0": {
                    "address": 0x048,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PROC": {
                    "address": 0x030,
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
        return f"TM4C123GH6PM({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = TM4C123GH6PM()
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

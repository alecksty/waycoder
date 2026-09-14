"""
STM32F303CCT6设备定义 - Python模块
生成自: STMicroelectronics/STM32/STM32F303CCT6
版本: 1.0
日期: 2026-04-29
作者: VML Team
描述: 32-bit ARM Cortex-M4F MCU with 256KB Flash, 48KB SRAM, 72MHz, FPU+DSP
CPU架构: ARM-Cortex-M4F
位宽: 32位
时钟频率: 72000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class STM32F303CCT6:
    """STM32F303CCT6设备类"""

    # 设备信息
    DEVICE_NAME = "STM32F303CCT6"
    MANUFACTURER = "STMicroelectronics"
    FAMILY = "STM32"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M4F"
    BITS = 32
    CLOCK_FREQUENCY = 72000000

    # 外设定义
    # USART 1
    USART1_BASE = 0x40013800
    USART1_SR_ADDR = 0x00
    USART1_DR_ADDR = 0x04
    USART1_BRR_ADDR = 0x08
    USART1_CR1_ADDR = 0x0C
    USART1_CR2_ADDR = 0x10
    USART1_CR3_ADDR = 0x14
    # USART 2
    USART2_BASE = 0x40004400
    USART2_SR_ADDR = 0x00
    USART2_DR_ADDR = 0x04
    USART2_BRR_ADDR = 0x08
    USART2_CR1_ADDR = 0x0C
    # USART 3
    USART3_BASE = 0x40004800
    USART3_SR_ADDR = 0x00
    USART3_DR_ADDR = 0x04
    USART3_BRR_ADDR = 0x08
    USART3_CR1_ADDR = 0x0C
    # GPIO Port A
    GPIOA_BASE = 0x48000000
    GPIOA_MODER_ADDR = 0x00
    GPIOA_OTYPER_ADDR = 0x04
    GPIOA_OSPEEDR_ADDR = 0x08
    GPIOA_PUPDR_ADDR = 0x0C
    GPIOA_IDR_ADDR = 0x10
    GPIOA_ODR_ADDR = 0x14
    GPIOA_BSRR_ADDR = 0x18
    GPIOA_AFRL_ADDR = 0x20
    GPIOA_AFRH_ADDR = 0x24
    # 高级定时器 1
    TIM1_BASE = 0x40012C00
    TIM1_CR1_ADDR = 0x00
    TIM1_CNT_ADDR = 0x24
    TIM1_PSC_ADDR = 0x28
    TIM1_ARR_ADDR = 0x2C
    TIM1_CCR1_ADDR = 0x34
    # ADC 1
    ADC1_BASE = 0x50000000
    ADC1_SR_ADDR = 0x00
    ADC1_CR_ADDR = 0x08
    ADC1_CFGR_ADDR = 0x0C
    ADC1_SMPR1_ADDR = 0x14
    ADC1_DR_ADDR = 0x40

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
        self._peripherals["USART1"] = {
            "base": 0x40013800,
            "type": "uart",
            "description": "USART 1",
            "registers": {
                "SR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
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
                "CR1": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR2": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR3": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART2"] = {
            "base": 0x40004400,
            "type": "uart",
            "description": "USART 2",
            "registers": {
                "SR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
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
                "CR1": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USART3"] = {
            "base": 0x40004800,
            "type": "uart",
            "description": "USART 3",
            "registers": {
                "SR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
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
                "CR1": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIOA"] = {
            "base": 0x48000000,
            "type": "gpio",
            "description": "GPIO Port A",
            "registers": {
                "MODER": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OTYPER": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "OSPEEDR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PUPDR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IDR": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ODR": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "BSRR": {
                    "address": 0x18,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AFRL": {
                    "address": 0x20,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "AFRH": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIM1"] = {
            "base": 0x40012C00,
            "type": "timer",
            "description": "高级定时器 1",
            "registers": {
                "CR1": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x24,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PSC": {
                    "address": 0x28,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "ARR": {
                    "address": 0x2C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CCR1": {
                    "address": 0x34,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC1"] = {
            "base": 0x50000000,
            "type": "adc",
            "description": "ADC 1",
            "registers": {
                "SR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CFGR": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "SMPR1": {
                    "address": 0x14,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DR": {
                    "address": 0x40,
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
        return f"STM32F303CCT6({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = STM32F303CCT6()
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

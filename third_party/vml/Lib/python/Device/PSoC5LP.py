"""
CY8C5888LTI-LP097设备定义 - Python模块
生成自: Cypress (Infineon)/PSoC/CY8C5888LTI-LP097
版本: 1.0
日期: 2026-04-29
作者: VML Team
描述: 32-bit ARM Cortex-M3 PSoC 5LP with 256KB Flash, 64KB SRAM, 80MHz, UDB
CPU架构: ARM-Cortex-M3
位宽: 32位
时钟频率: 80000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class CY8C5888LTI_LP097:
    """CY8C5888LTI-LP097设备类"""

    # 设备信息
    DEVICE_NAME = "CY8C5888LTI-LP097"
    MANUFACTURER = "Cypress (Infineon)"
    FAMILY = "PSoC"
    VERSION = "1.0"
    ARCHITECTURE = "ARM-Cortex-M3"
    BITS = 32
    CLOCK_FREQUENCY = 80000000

    # 外设定义
    # SCB UART (可编程)
    UART_BASE = 0x40050000
    UART_CTRL_ADDR = 0x00
    UART_STATUS_ADDR = 0x04
    UART_TX_DATA_ADDR = 0x08
    UART_RX_DATA_ADDR = 0x0C
    # SCB I2C
    I2C_BASE = 0x40051000
    I2C_CTRL_ADDR = 0x00
    I2C_STATUS_ADDR = 0x04
    I2C_TX_DATA_ADDR = 0x08
    I2C_RX_DATA_ADDR = 0x0C
    # TCPWM 定时器
    TIMER_BASE = 0x40060000
    TIMER_CTRL_ADDR = 0x00
    TIMER_STATUS_ADDR = 0x04
    TIMER_CNT_ADDR = 0x08
    TIMER_PERIOD_ADDR = 0x0C
    TIMER_CC_ADDR = 0x10
    # DelSig ADC 20-bit
    ADC_BASE = 0x40100000
    ADC_CTRL_ADDR = 0x00
    ADC_STATUS_ADDR = 0x04
    ADC_DATA_ADDR = 0x08
    ADC_CLOCK_ADDR = 0x10
    # GPIO 端口
    GPIO_BASE = 0x40040000
    GPIO_DR_ADDR = 0x00
    GPIO_PS_ADDR = 0x04
    GPIO_IE_ADDR = 0x08
    GPIO_DM_ADDR = 0x0C
    # USB 控制器
    USB_BASE = 0x40080000
    USB_CR0_ADDR = 0x00
    USB_CR1_ADDR = 0x04
    USB_STAT_ADDR = 0x08

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
        self._peripherals["UART"] = {
            "base": 0x40050000,
            "type": "uart",
            "description": "SCB UART (可编程)",
            "registers": {
                "CTRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TX_DATA": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RX_DATA": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["I2C"] = {
            "base": 0x40051000,
            "type": "i2c",
            "description": "SCB I2C",
            "registers": {
                "CTRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "TX_DATA": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "RX_DATA": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER"] = {
            "base": 0x40060000,
            "type": "timer",
            "description": "TCPWM 定时器",
            "registers": {
                "CTRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CNT": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PERIOD": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CC": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC"] = {
            "base": 0x40100000,
            "type": "adc",
            "description": "DelSig ADC 20-bit",
            "registers": {
                "CTRL": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DATA": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CLOCK": {
                    "address": 0x10,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["GPIO"] = {
            "base": 0x40040000,
            "type": "gpio",
            "description": "GPIO 端口",
            "registers": {
                "DR": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "PS": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "IE": {
                    "address": 0x08,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "DM": {
                    "address": 0x0C,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
            }
        }
        self._peripherals["USB"] = {
            "base": 0x40080000,
            "type": "usb",
            "description": "USB 控制器",
            "registers": {
                "CR0": {
                    "address": 0x00,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "CR1": {
                    "address": 0x04,
                    "size": 4,
                    "type": "uint32",
                    "value": 0
                },
                "STAT": {
                    "address": 0x08,
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
        return f"CY8C5888LTI_LP097({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = CY8C5888LTI_LP097()
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

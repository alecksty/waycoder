"""
SAMD21设备定义 - Python模块
生成自: Atmel (Microchip)/SAM D/SAMD21
版本: 
日期: 
作者: 
描述: Atmel SAM D21 ARM Cortex-M0+ based microcontroller
CPU架构: ARM Cortex-M0+
位宽: 0位
时钟频率: 0 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class SAMD21:
    """SAMD21设备类"""

    # 设备信息
    DEVICE_NAME = "SAMD21"
    MANUFACTURER = "Atmel (Microchip)"
    FAMILY = "SAM D"
    VERSION = ""
    ARCHITECTURE = "ARM Cortex-M0+"
    BITS = 0
    CLOCK_FREQUENCY = 0

    # 外设定义
    # Power Manager
    PM_BASE = 
    PM_PM_CTRL_ADDR = 0x40000400
    # System Controller
    SYSCTRL_BASE = 
    SYSCTRL_SYSCTRL_INTENCLR_ADDR = 0x40000800
    # Generic Clock Generator
    GCLK_BASE = 
    GCLK_GCLK_CTRL_ADDR = 0x40000C00
    # Watchdog Timer
    WDT_BASE = 
    WDT_WDT_CTRL_ADDR = 0x40001000
    # Real-Time Clock
    RTC_BASE = 
    RTC_RTC_CTRL_ADDR = 0x40001400
    # External Interrupt Controller
    EIC_BASE = 
    EIC_EIC_CTRL_ADDR = 0x40001800
    # Serial Communication Interface 0
    SERCOM0_BASE = 
    SERCOM0_SERCOM0_I2CM_CTRLA_ADDR = 0x42000800
    # Analog-to-Digital Converter
    ADC_BASE = 
    ADC_ADC_CTRLA_ADDR = 0x42002000
    # Digital-to-Analog Converter
    DAC_BASE = 
    DAC_DAC_CTRLA_ADDR = 0x42002400
    # General Purpose I/O
    PORT_BASE = 
    PORT_PORT_DIR_ADDR = 0x41004400
    # Timer/Counter 0
    TC0_BASE = 
    TC0_TC0_CTRLA_ADDR = 0x42002800
    # USB Device Controller
    USB_BASE = 
    USB_USB_CTRLA_ADDR = 0x41005000

    # 中断向量定义
    INT_RESET = 0  # Reset vector
    INT_NONMASKABLEINT = 1  # Non-maskable interrupt
    INT_HARDFAULT = 2  # Hard fault
    INT_SVCALL = 3  # Supervisor call
    INT_PENDSV = 4  # Pendable service call
    INT_SYSTICK = 5  # System tick timer
    INT_PM = 6  # Power Manager
    INT_SYSCTRL = 7  # System Controller
    INT_WDT = 8  # Watchdog Timer
    INT_RTC = 9  # Real-Time Clock
    INT_EIC = 10  # External Interrupt Controller
    INT_NVMCTRL = 11  # Non-Volatile Memory Controller
    INT_DMAC = 12  # Direct Memory Access Controller
    INT_USB = 13  # USB Device Controller
    INT_EVSYS = 14  # Event System
    INT_SERCOM0 = 15  # Serial Communication Interface 0
    INT_SERCOM1 = 16  # Serial Communication Interface 1
    INT_SERCOM2 = 17  # Serial Communication Interface 2
    INT_SERCOM3 = 18  # Serial Communication Interface 3
    INT_SERCOM4 = 19  # Serial Communication Interface 4
    INT_SERCOM5 = 20  # Serial Communication Interface 5
    INT_TCC0 = 21  # Timer/Counter for Control 0
    INT_TCC1 = 22  # Timer/Counter for Control 1
    INT_TCC2 = 23  # Timer/Counter for Control 2
    INT_TC3 = 24  # Timer/Counter 3
    INT_TC4 = 25  # Timer/Counter 4
    INT_TC5 = 26  # Timer/Counter 5
    INT_TC6 = 27  # Timer/Counter 6
    INT_TC7 = 28  # Timer/Counter 7
    INT_ADC = 29  # Analog-to-Digital Converter
    INT_AC = 30  # Analog Comparator
    INT_DAC = 31  # Digital-to-Analog Converter
    INT_PTC = 32  # Peripheral Touch Controller
    INT_I2S = 33  # Inter-IC Sound Interface

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
        self._peripherals["PM"] = {
            "base": ,
            "type": "PowerManager",
            "description": "Power Manager",
            "registers": {
                "PM_CTRL": {
                    "address": 0x40000400,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["SYSCTRL"] = {
            "base": ,
            "type": "SystemController",
            "description": "System Controller",
            "registers": {
                "SYSCTRL_INTENCLR": {
                    "address": 0x40000800,
                    "size": 32,
                    "type": "bytes[32]",
                    "value": 0
                },
            }
        }
        self._peripherals["GCLK"] = {
            "base": ,
            "type": "ClockGenerator",
            "description": "Generic Clock Generator",
            "registers": {
                "GCLK_CTRL": {
                    "address": 0x40000C00,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["WDT"] = {
            "base": ,
            "type": "Watchdog",
            "description": "Watchdog Timer",
            "registers": {
                "WDT_CTRL": {
                    "address": 0x40001000,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["RTC"] = {
            "base": ,
            "type": "RealTimeClock",
            "description": "Real-Time Clock",
            "registers": {
                "RTC_CTRL": {
                    "address": 0x40001400,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
            }
        }
        self._peripherals["EIC"] = {
            "base": ,
            "type": "ExternalInterrupt",
            "description": "External Interrupt Controller",
            "registers": {
                "EIC_CTRL": {
                    "address": 0x40001800,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["SERCOM0"] = {
            "base": ,
            "type": "SerialCommunication",
            "description": "Serial Communication Interface 0",
            "registers": {
                "SERCOM0_I2CM_CTRLA": {
                    "address": 0x42000800,
                    "size": 32,
                    "type": "bytes[32]",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC"] = {
            "base": ,
            "type": "AnalogDigitalConverter",
            "description": "Analog-to-Digital Converter",
            "registers": {
                "ADC_CTRLA": {
                    "address": 0x42002000,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["DAC"] = {
            "base": ,
            "type": "DigitalAnalogConverter",
            "description": "Digital-to-Analog Converter",
            "registers": {
                "DAC_CTRLA": {
                    "address": 0x42002400,
                    "size": 8,
                    "type": "uint64",
                    "value": 0
                },
            }
        }
        self._peripherals["PORT"] = {
            "base": ,
            "type": "GPIO",
            "description": "General Purpose I/O",
            "registers": {
                "PORT_DIR": {
                    "address": 0x41004400,
                    "size": 32,
                    "type": "bytes[32]",
                    "value": 0
                },
            }
        }
        self._peripherals["TC0"] = {
            "base": ,
            "type": "TimerCounter",
            "description": "Timer/Counter 0",
            "registers": {
                "TC0_CTRLA": {
                    "address": 0x42002800,
                    "size": 16,
                    "type": "bytes[16]",
                    "value": 0
                },
            }
        }
        self._peripherals["USB"] = {
            "base": ,
            "type": "UniversalSerialBus",
            "description": "USB Device Controller",
            "registers": {
                "USB_CTRLA": {
                    "address": 0x41005000,
                    "size": 8,
                    "type": "uint64",
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
        return f"SAMD21({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = SAMD21()
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

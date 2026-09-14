"""
ATtiny13设备定义 - Python模块
生成自: Atmel/AVR/ATtiny13
版本: 1.0
日期: 2026-04-28
作者: VML Team
描述: 8-bit AVR MCU with 1KB Flash, 64B RAM, 64B EEPROM, 20MHz, tiny
CPU架构: AVR
位宽: 8位
时钟频率: 20000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class ATtiny13:
    """ATtiny13设备类"""

    # 设备信息
    DEVICE_NAME = "ATtiny13"
    MANUFACTURER = "Atmel"
    FAMILY = "AVR"
    VERSION = "1.0"
    ARCHITECTURE = "AVR"
    BITS = 8
    CLOCK_FREQUENCY = 20000000

    # 寄存器地址定义
    R0_ADDR = 0x00  # 
    R1_ADDR = 0x01  # 
    R2_ADDR = 0x02  # 
    R16_ADDR = 0x10  # 
    R17_ADDR = 0x11  # 
    R26_ADDR = 0x1A  # XL
    R27_ADDR = 0x1B  # XH
    R28_ADDR = 0x1C  # YL
    R29_ADDR = 0x1D  # YH
    R30_ADDR = 0x1E  # ZL
    R31_ADDR = 0x1F  # ZH
    SPL_ADDR = 0x5D  # Stack Pointer Low
    SPH_ADDR = 0x5E  # Stack Pointer High
    SREG_ADDR = 0x5F  # Status Register

    # 内存段定义
    FLASH_START = 0x0000
    FLASH_END = 0x03FF
    FLASH_SIZE = 1024  # 
    SRAM_START = 0x0060
    SRAM_END = 0x009F
    SRAM_SIZE = 64  # 
    EEPROM_START = 0x0000
    EEPROM_END = 0x003F
    EEPROM_SIZE = 64  # 
    IO_START = 0x00
    IO_END = 0x1F
    IO_SIZE = 32  # 
    EXTIO_START = 0x20
    EXTIO_END = 0x5F
    EXTIO_SIZE = 64  # 

    # 外设定义
    # Port B (only port)
    PORTB_BASE = 0x18
    PORTB_DDRB_ADDR = 0x17
    PORTB_PORTB_ADDR = 0x18
    PORTB_PINB_ADDR = 0x19
    # 8-bit Timer/Counter0
    TIMER0_BASE = 0x33
    TIMER0_TCCR0A_ADDR = 0x33
    TIMER0_TCCR0B_ADDR = 0x33
    TIMER0_TCNT0_ADDR = 0x32
    TIMER0_OCR0A_ADDR = 0x36
    TIMER0_OCR0B_ADDR = 0x35
    TIMER0_TIMSK0_ADDR = 0x39
    TIMER0_TIFR0_ADDR = 0x38
    # Analog-to-Digital
    ADC_BASE = 0x04
    ADC_ADMUX_ADDR = 0x07
    ADC_ADCSRA_ADDR = 0x06
    ADC_ADCL_ADDR = 0x04
    ADC_ADCH_ADDR = 0x05

    # 中断向量定义
    INT_RESET = 1  # 
    INT_INT0 = 2  # External Interrupt 0
    INT_PCINT0 = 3  # Pin Change Interrupt
    INT_TIM0_OVF = 4  # Timer0 Overflow
    INT_TIM0_COMPA = 5  # Timer0 Compare A
    INT_WDT = 6  # Watchdog Timeout
    INT_ADC = 7  # ADC Conversion Complete

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
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R1"] = {
            "address": 0x01,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R2"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R16"] = {
            "address": 0x10,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R17"] = {
            "address": 0x11,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "",
            "value": 0
        }
        self._registers["R26"] = {
            "address": 0x1A,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "XL",
            "value": 0
        }
        self._registers["R27"] = {
            "address": 0x1B,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "XH",
            "value": 0
        }
        self._registers["R28"] = {
            "address": 0x1C,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "YL",
            "value": 0
        }
        self._registers["R29"] = {
            "address": 0x1D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "YH",
            "value": 0
        }
        self._registers["R30"] = {
            "address": 0x1E,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "ZL",
            "value": 0
        }
        self._registers["R31"] = {
            "address": 0x1F,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "ZH",
            "value": 0
        }
        self._registers["SPL"] = {
            "address": 0x5D,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Stack Pointer Low",
            "value": 0
        }
        self._registers["SPH"] = {
            "address": 0x5E,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Stack Pointer High",
            "value": 0
        }
        self._registers["SREG"] = {
            "address": 0x5F,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Status Register",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["PORTB"] = {
            "base": 0x18,
            "type": "GPIO",
            "description": "Port B (only port)",
            "registers": {
                "DDRB": {
                    "address": 0x17,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORTB": {
                    "address": 0x18,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PINB": {
                    "address": 0x19,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["TIMER0"] = {
            "base": 0x33,
            "type": "Timer",
            "description": "8-bit Timer/Counter0",
            "registers": {
                "TCCR0A": {
                    "address": 0x33,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TCCR0B": {
                    "address": 0x33,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TCNT0": {
                    "address": 0x32,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OCR0A": {
                    "address": 0x36,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "OCR0B": {
                    "address": 0x35,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TIMSK0": {
                    "address": 0x39,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TIFR0": {
                    "address": 0x38,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["ADC"] = {
            "base": 0x04,
            "type": "ADC",
            "description": "Analog-to-Digital",
            "registers": {
                "ADMUX": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADCSRA": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADCL": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ADCH": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
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
        return f"ATtiny13({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = ATtiny13()
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

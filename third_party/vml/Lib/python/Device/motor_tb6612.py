"""
TB6612设备定义 - Python模块
生成自: Toshiba/Motor/TB6612
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: TB6612FNG Dual DC Motor Driver (1.2A continuous, 3.2A peak, 2.5V-13.5V)
CPU架构: Motor
位宽: 8位
时钟频率: 100000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class TB6612:
    """TB6612设备类"""

    # 设备信息
    DEVICE_NAME = "TB6612"
    MANUFACTURER = "Toshiba"
    FAMILY = "Motor"
    VERSION = "1.0"
    ARCHITECTURE = "Motor"
    BITS = 8
    CLOCK_FREQUENCY = 100000

    # 外设定义
    # TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)
    TB6612_BASE = 0x00
    TB6612_MOTOR_A_ADDR = 0x00
    TB6612_MOTOR_A_AIN1_BIT = 0  # Motor A input 1
    TB6612_MOTOR_A_AIN2_BIT = 1  # Motor A input 2
    TB6612_MOTOR_A_PWMA_BIT = 2  # Motor A PWM enable
    TB6612_MOTOR_B_ADDR = 0x01
    TB6612_MOTOR_B_BIN1_BIT = 0  # Motor B input 1
    TB6612_MOTOR_B_BIN2_BIT = 1  # Motor B input 2
    TB6612_MOTOR_B_PWMB_BIT = 2  # Motor B PWM enable
    TB6612_SPEED_A_ADDR = 0x02
    TB6612_SPEED_B_ADDR = 0x04
    TB6612_STBY_ADDR = 0x06

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
        self._peripherals["TB6612"] = {
            "base": 0x00,
            "type": "GPIO/PWM",
            "description": "TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)",
            "registers": {
                "MOTOR_A": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MOTOR_B": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SPEED_A": {
                    "address": 0x02,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SPEED_B": {
                    "address": 0x04,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "STBY": {
                    "address": 0x06,
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
        return f"TB6612({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = TB6612()
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

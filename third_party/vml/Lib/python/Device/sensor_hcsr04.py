"""
HC_SR04设备定义 - Python模块
生成自: Generic/Sensor/HC_SR04
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: Ultrasonic Distance Sensor (2cm-400cm)
CPU架构: Sensor
位宽: 8位
时钟频率: 0 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class HC_SR04:
    """HC_SR04设备类"""

    # 设备信息
    DEVICE_NAME = "HC_SR04"
    MANUFACTURER = "Generic"
    FAMILY = "Sensor"
    VERSION = "1.0"
    ARCHITECTURE = "Sensor"
    BITS = 8
    CLOCK_FREQUENCY = 0

    # 内存段定义
    PACKAGE_START = 0x00
    PACKAGE_END = 0x00
    PACKAGE_SIZE = 0  # PCB Module (45x20x15mm)

    # 外设定义
    # HC-SR04 Ultrasonic Sensor (4.5V-5.5V)
    HC_SR04_BASE = 0x00
    HC_SR04_TRIG_ADDR = 0x00
    HC_SR04_DISTANCE_H_ADDR = 0x01
    HC_SR04_DISTANCE_L_ADDR = 0x02
    HC_SR04_STATUS_ADDR = 0x03
    HC_SR04_STATUS_BUSY_BIT = 0  # 1=Measurement in progress
    HC_SR04_STATUS_VALID_BIT = 1  # 1=Valid measurement available
    HC_SR04_STATUS_TIMEOUT_BIT = 2  # 1=No echo received (out of range)

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
        self._peripherals["HC_SR04"] = {
            "base": 0x00,
            "type": "GPIO",
            "description": "HC-SR04 Ultrasonic Sensor (4.5V-5.5V)",
            "registers": {
                "TRIG": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DISTANCE_H": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DISTANCE_L": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x03,
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
        return f"HC_SR04({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = HC_SR04()
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

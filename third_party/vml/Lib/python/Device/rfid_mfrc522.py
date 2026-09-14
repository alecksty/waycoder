"""
MFRC522设备定义 - Python模块
生成自: NXP/RFID/MFRC522
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: MFRC522 13.56MHz RFID/NFC Reader (SPI, ISO 14443A, MIFARE)
CPU架构: RFID
位宽: 8位
时钟频率: 10000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class MFRC522:
    """MFRC522设备类"""

    # 设备信息
    DEVICE_NAME = "MFRC522"
    MANUFACTURER = "NXP"
    FAMILY = "RFID"
    VERSION = "1.0"
    ARCHITECTURE = "RFID"
    BITS = 8
    CLOCK_FREQUENCY = 10000000

    # 内存段定义
    FIFO_START = 0x00
    FIFO_END = 0x3F
    FIFO_SIZE = 64  # 64-byte FIFO buffer

    # 外设定义
    # MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)
    MFRC522_BASE = 0x00
    MFRC522_CMD_ADDR = 0x01
    MFRC522_COM_IRQ_ADDR = 0x04
    MFRC522_COM_IRQ_TX_IRQ_BIT = 6  # Transmitter interrupt
    MFRC522_COM_IRQ_RX_IRQ_BIT = 5  # Receiver interrupt
    MFRC522_COM_IRQ_IDLE_IRQ_BIT = 4  # Idle interrupt
    MFRC522_COM_IRQ_TIMER_IRQ_BIT = 0  # Timer interrupt
    MFRC522_COM_IRQ_EN_ADDR = 0x05
    MFRC522_ERROR_ADDR = 0x06
    MFRC522_STATUS2_ADDR = 0x08
    MFRC522_FIFO_DATA_ADDR = 0x09
    MFRC522_FIFO_LEVEL_ADDR = 0x0A
    MFRC522_TX_CTRL_ADDR = 0x14
    MFRC522_TX_ASK_ADDR = 0x15
    MFRC522_MODE_ADDR = 0x11
    MFRC522_VERSION_ADDR = 0x37

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
        self._peripherals["MFRC522"] = {
            "base": 0x00,
            "type": "SPI",
            "description": "MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)",
            "registers": {
                "CMD": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "COM_IRQ": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "COM_IRQ_EN": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ERROR": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "STATUS2": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FIFO_DATA": {
                    "address": 0x09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FIFO_LEVEL": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TX_CTRL": {
                    "address": 0x14,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TX_ASK": {
                    "address": 0x15,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MODE": {
                    "address": 0x11,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VERSION": {
                    "address": 0x37,
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
        return f"MFRC522({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = MFRC522()
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

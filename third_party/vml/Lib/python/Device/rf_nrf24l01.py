"""
NRF24L01设备定义 - Python模块
生成自: Nordic/RF/NRF24L01
版本: 1.0
日期: 2026-05-06
作者: VML Team
描述: nRF24L01+ 2.4GHz RF Transceiver (SPI, 2Mbps, 125-channel, 6-pipe)
CPU架构: RF
位宽: 8位
时钟频率: 10000000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class NRF24L01:
    """NRF24L01设备类"""

    # 设备信息
    DEVICE_NAME = "NRF24L01"
    MANUFACTURER = "Nordic"
    FAMILY = "RF"
    VERSION = "1.0"
    ARCHITECTURE = "RF"
    BITS = 8
    CLOCK_FREQUENCY = 10000000

    # 外设定义
    # nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)
    NRF24L01_BASE = 0x00
    NRF24L01_CONFIG_ADDR = 0x00
    NRF24L01_CONFIG_PWR_UP_BIT = 1  # Power up (1=on)
    NRF24L01_CONFIG_PRIM_RX_BIT = 0  # Primary RX mode (1=RX, 0=TX)
    NRF24L01_EN_AA_ADDR = 0x01
    NRF24L01_EN_RXADDR_ADDR = 0x02
    NRF24L01_SETUP_AW_ADDR = 0x03
    NRF24L01_SETUP_RETR_ADDR = 0x04
    NRF24L01_RF_CH_ADDR = 0x05
    NRF24L01_RF_SETUP_ADDR = 0x06
    NRF24L01_RF_SETUP_RF_PWR_BIT = 1  # TX power: 00=-18dBm,01=-12dBm,10=-6dBm,11=0dBm
    NRF24L01_RF_SETUP_RF_DR_BIT = 3  # Data rate: 0=1Mbps,1=2Mbps
    NRF24L01_STATUS_ADDR = 0x07
    NRF24L01_RX_PW_P0_ADDR = 0x11
    NRF24L01_FIFO_STATUS_ADDR = 0x17
    NRF24L01_TX_PAYLOAD_ADDR = 0xA0
    NRF24L01_RX_PAYLOAD_ADDR = 0x61

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
        self._peripherals["NRF24L01"] = {
            "base": 0x00,
            "type": "SPI",
            "description": "nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)",
            "registers": {
                "CONFIG": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EN_AA": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "EN_RXADDR": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SETUP_AW": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SETUP_RETR": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RF_CH": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RF_SETUP": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "STATUS": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "RX_PW_P0": {
                    "address": 0x11,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "FIFO_STATUS": {
                    "address": 0x17,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TX_PAYLOAD": {
                    "address": 0xA0,
                    "size": 32,
                    "type": "bytes[32]",
                    "value": 0
                },
                "RX_PAYLOAD": {
                    "address": 0x61,
                    "size": 32,
                    "type": "bytes[32]",
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
        return f"NRF24L01({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = NRF24L01()
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

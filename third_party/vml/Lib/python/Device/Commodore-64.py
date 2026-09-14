"""
Commodore-64设备定义 - Python模块
生成自: Commodore International/Commodore 64/Commodore-64
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Commodore 64 home computer with MOS 6510 CPU, 64KB RAM, and SID sound chip
CPU架构: MOS 6510
位宽: 8位
时钟频率: 985248 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Commodore_64:
    """Commodore-64设备类"""

    # 设备信息
    DEVICE_NAME = "Commodore-64"
    MANUFACTURER = "Commodore International"
    FAMILY = "Commodore 64"
    VERSION = "1.0"
    ARCHITECTURE = "MOS 6510"
    BITS = 8
    CLOCK_FREQUENCY = 985248

    # 寄存器地址定义
    A_ADDR = 0  # Accumulator
    X_ADDR = 0  # Index Register X
    Y_ADDR = 0  # Index Register Y
    SP_ADDR = 0  # Stack Pointer
    PC_ADDR = 0  # Program Counter
    P_ADDR = 0  # Status Register
    PORT_ADDR = 1  # I/O Port (6510 specific)

    # 外设定义
    # Video Interface Chip II
    VIC_II_BASE = 
    VIC_II_VIC_CTRL1_ADDR = 0xD011
    VIC_II_VIC_CTRL2_ADDR = 0xD016
    VIC_II_VIC_RASTER_ADDR = 0xD012
    VIC_II_VIC_MEMPTR_ADDR = 0xD018
    VIC_II_VIC_IRQ_ADDR = 0xD019
    VIC_II_VIC_IRQMASK_ADDR = 0xD01A
    VIC_II_VIC_BORDER_ADDR = 0xD020
    VIC_II_VIC_BG0_ADDR = 0xD021
    VIC_II_VIC_BG1_ADDR = 0xD022
    VIC_II_VIC_BG2_ADDR = 0xD023
    VIC_II_VIC_BG3_ADDR = 0xD024
    VIC_II_VIC_SPRITE0_X_ADDR = 0xD000
    VIC_II_VIC_SPRITE0_Y_ADDR = 0xD001
    VIC_II_VIC_SPRITE1_X_ADDR = 0xD002
    VIC_II_VIC_SPRITE1_Y_ADDR = 0xD003
    # Sound Interface Device (6581)
    SID_BASE = 
    SID_SID_VOICE1_FREQ_LO_ADDR = 0xD400
    SID_SID_VOICE1_FREQ_HI_ADDR = 0xD401
    SID_SID_VOICE1_PW_LO_ADDR = 0xD402
    SID_SID_VOICE1_PW_HI_ADDR = 0xD403
    SID_SID_VOICE1_CTRL_ADDR = 0xD404
    SID_SID_VOICE1_AD_ADDR = 0xD405
    SID_SID_VOICE1_SR_ADDR = 0xD406
    SID_SID_VOICE2_FREQ_LO_ADDR = 0xD407
    SID_SID_VOICE2_FREQ_HI_ADDR = 0xD408
    SID_SID_VOICE2_PW_LO_ADDR = 0xD409
    SID_SID_VOICE2_PW_HI_ADDR = 0xD40A
    SID_SID_VOICE2_CTRL_ADDR = 0xD40B
    SID_SID_VOICE2_AD_ADDR = 0xD40C
    SID_SID_VOICE2_SR_ADDR = 0xD40D
    SID_SID_VOICE3_FREQ_LO_ADDR = 0xD40E
    SID_SID_VOICE3_FREQ_HI_ADDR = 0xD40F
    SID_SID_VOICE3_PW_LO_ADDR = 0xD410
    SID_SID_VOICE3_PW_HI_ADDR = 0xD411
    SID_SID_VOICE3_CTRL_ADDR = 0xD412
    SID_SID_VOICE3_AD_ADDR = 0xD413
    SID_SID_VOICE3_SR_ADDR = 0xD414
    SID_SID_FILTER_CUTOFF_LO_ADDR = 0xD415
    SID_SID_FILTER_CUTOFF_HI_ADDR = 0xD416
    SID_SID_FILTER_CTRL_ADDR = 0xD417
    SID_SID_VOLUME_ADDR = 0xD418
    SID_SID_POTX_ADDR = 0xD419
    SID_SID_POTY_ADDR = 0xD41A
    SID_SID_OSC3_ADDR = 0xD41B
    SID_SID_ENV3_ADDR = 0xD41C
    # Complex Interface Adapter 1 (6526)
    CIA1_BASE = 
    CIA1_CIA1_PRA_ADDR = 0xDC00
    CIA1_CIA1_PRB_ADDR = 0xDC01
    CIA1_CIA1_DDRA_ADDR = 0xDC02
    CIA1_CIA1_DDRB_ADDR = 0xDC03
    CIA1_CIA1_TALO_ADDR = 0xDC04
    CIA1_CIA1_TAHI_ADDR = 0xDC05
    CIA1_CIA1_TBLO_ADDR = 0xDC06
    CIA1_CIA1_TBHI_ADDR = 0xDC07
    CIA1_CIA1_TODTEN_ADDR = 0xDC08
    CIA1_CIA1_TODSEC_ADDR = 0xDC09
    CIA1_CIA1_TODMIN_ADDR = 0xDC0A
    CIA1_CIA1_TODHR_ADDR = 0xDC0B
    CIA1_CIA1_SDR_ADDR = 0xDC0C
    CIA1_CIA1_ICR_ADDR = 0xDC0D
    CIA1_CIA1_CRA_ADDR = 0xDC0E
    CIA1_CIA1_CRB_ADDR = 0xDC0F
    # Complex Interface Adapter 2 (6526)
    CIA2_BASE = 
    CIA2_CIA2_PRA_ADDR = 0xDD00
    CIA2_CIA2_PRB_ADDR = 0xDD01
    CIA2_CIA2_DDRA_ADDR = 0xDD02
    CIA2_CIA2_DDRB_ADDR = 0xDD03
    CIA2_CIA2_TALO_ADDR = 0xDD04
    CIA2_CIA2_TAHI_ADDR = 0xDD05
    CIA2_CIA2_TBLO_ADDR = 0xDD06
    CIA2_CIA2_TBHI_ADDR = 0xDD07
    CIA2_CIA2_TODTEN_ADDR = 0xDD08
    CIA2_CIA2_TODSEC_ADDR = 0xDD09
    CIA2_CIA2_TODMIN_ADDR = 0xDD0A
    CIA2_CIA2_TODHR_ADDR = 0xDD0B
    CIA2_CIA2_SDR_ADDR = 0xDD0C
    CIA2_CIA2_ICR_ADDR = 0xDD0D
    CIA2_CIA2_CRA_ADDR = 0xDD0E
    CIA2_CIA2_CRB_ADDR = 0xDD0F

    # 中断向量定义
    INT_IRQ = 65532  # Maskable Interrupt
    INT_NMI = 65534  # Non-Maskable Interrupt
    INT_RESET = 65526  # Reset Vector

    def __init__(self, memory_base: int = 0):
        """初始化设备"""
        self.memory_base = memory_base
        self._registers = {}
        self._peripherals = {}
        self._initialize_registers()
        self._initialize_peripherals()

    def _initialize_registers(self):
        """初始化寄存器""""
        self._registers["A"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Accumulator",
            "value": 0
        }
        self._registers["X"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Index Register X",
            "value": 0
        }
        self._registers["Y"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Index Register Y",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["P"] = {
            "address": 0,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Status Register",
            "value": 0
        }
        self._registers["PORT"] = {
            "address": 1,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "I/O Port (6510 specific)",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["VIC-II"] = {
            "base": ,
            "type": "Video",
            "description": "Video Interface Chip II",
            "registers": {
                "VIC_CTRL1": {
                    "address": 0xD011,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_CTRL2": {
                    "address": 0xD016,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_RASTER": {
                    "address": 0xD012,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_MEMPTR": {
                    "address": 0xD018,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_IRQ": {
                    "address": 0xD019,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_IRQMASK": {
                    "address": 0xD01A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_BORDER": {
                    "address": 0xD020,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_BG0": {
                    "address": 0xD021,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_BG1": {
                    "address": 0xD022,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_BG2": {
                    "address": 0xD023,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_BG3": {
                    "address": 0xD024,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_SPRITE0_X": {
                    "address": 0xD000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_SPRITE0_Y": {
                    "address": 0xD001,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_SPRITE1_X": {
                    "address": 0xD002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VIC_SPRITE1_Y": {
                    "address": 0xD003,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SID"] = {
            "base": ,
            "type": "Audio",
            "description": "Sound Interface Device (6581)",
            "registers": {
                "SID_VOICE1_FREQ_LO": {
                    "address": 0xD400,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE1_FREQ_HI": {
                    "address": 0xD401,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE1_PW_LO": {
                    "address": 0xD402,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE1_PW_HI": {
                    "address": 0xD403,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE1_CTRL": {
                    "address": 0xD404,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE1_AD": {
                    "address": 0xD405,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE1_SR": {
                    "address": 0xD406,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE2_FREQ_LO": {
                    "address": 0xD407,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE2_FREQ_HI": {
                    "address": 0xD408,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE2_PW_LO": {
                    "address": 0xD409,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE2_PW_HI": {
                    "address": 0xD40A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE2_CTRL": {
                    "address": 0xD40B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE2_AD": {
                    "address": 0xD40C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE2_SR": {
                    "address": 0xD40D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE3_FREQ_LO": {
                    "address": 0xD40E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE3_FREQ_HI": {
                    "address": 0xD40F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE3_PW_LO": {
                    "address": 0xD410,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE3_PW_HI": {
                    "address": 0xD411,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE3_CTRL": {
                    "address": 0xD412,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE3_AD": {
                    "address": 0xD413,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOICE3_SR": {
                    "address": 0xD414,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_FILTER_CUTOFF_LO": {
                    "address": 0xD415,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_FILTER_CUTOFF_HI": {
                    "address": 0xD416,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_FILTER_CTRL": {
                    "address": 0xD417,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_VOLUME": {
                    "address": 0xD418,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_POTX": {
                    "address": 0xD419,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_POTY": {
                    "address": 0xD41A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_OSC3": {
                    "address": 0xD41B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "SID_ENV3": {
                    "address": 0xD41C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CIA1"] = {
            "base": ,
            "type": "IO",
            "description": "Complex Interface Adapter 1 (6526)",
            "registers": {
                "CIA1_PRA": {
                    "address": 0xDC00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_PRB": {
                    "address": 0xDC01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_DDRA": {
                    "address": 0xDC02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_DDRB": {
                    "address": 0xDC03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_TALO": {
                    "address": 0xDC04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_TAHI": {
                    "address": 0xDC05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_TBLO": {
                    "address": 0xDC06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_TBHI": {
                    "address": 0xDC07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_TODTEN": {
                    "address": 0xDC08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_TODSEC": {
                    "address": 0xDC09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_TODMIN": {
                    "address": 0xDC0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_TODHR": {
                    "address": 0xDC0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_SDR": {
                    "address": 0xDC0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_ICR": {
                    "address": 0xDC0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_CRA": {
                    "address": 0xDC0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA1_CRB": {
                    "address": 0xDC0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["CIA2"] = {
            "base": ,
            "type": "IO",
            "description": "Complex Interface Adapter 2 (6526)",
            "registers": {
                "CIA2_PRA": {
                    "address": 0xDD00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_PRB": {
                    "address": 0xDD01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_DDRA": {
                    "address": 0xDD02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_DDRB": {
                    "address": 0xDD03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_TALO": {
                    "address": 0xDD04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_TAHI": {
                    "address": 0xDD05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_TBLO": {
                    "address": 0xDD06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_TBHI": {
                    "address": 0xDD07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_TODTEN": {
                    "address": 0xDD08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_TODSEC": {
                    "address": 0xDD09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_TODMIN": {
                    "address": 0xDD0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_TODHR": {
                    "address": 0xDD0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_SDR": {
                    "address": 0xDD0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_ICR": {
                    "address": 0xDD0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_CRA": {
                    "address": 0xDD0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CIA2_CRB": {
                    "address": 0xDD0F,
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
        return f"Commodore_64({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Commodore_64()
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

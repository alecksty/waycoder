"""
Apple-IIe设备定义 - Python模块
生成自: Apple Computer/Apple II/Apple-IIe
版本: 1.0
日期: 2026-04-17
作者: VML Team
描述: Apple II Enhanced - 8-bit personal computer with MOS 6502 CPU
CPU架构: MOS-6502
位宽: 8位
时钟频率: 1021800 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Apple_IIe:
    """Apple-IIe设备类"""

    # 设备信息
    DEVICE_NAME = "Apple-IIe"
    MANUFACTURER = "Apple Computer"
    FAMILY = "Apple II"
    VERSION = "1.0"
    ARCHITECTURE = "MOS-6502"
    BITS = 8
    CLOCK_FREQUENCY = 1021800

    # 寄存器地址定义
    A_ADDR = 0x00  # Accumulator
    X_ADDR = 0x01  # X Index Register
    Y_ADDR = 0x02  # Y Index Register
    SP_ADDR = 0x03  # Stack Pointer
    PC_ADDR = 0x04  # Program Counter
    P_ADDR = 0x06  # Processor Status
    P_C_BIT = 0  # Carry Flag
    P_Z_BIT = 1  # Zero Flag
    P_I_BIT = 2  # Interrupt Disable
    P_D_BIT = 3  # Decimal Mode
    P_B_BIT = 4  # Break Command
    P_U_BIT = 5  # Unused
    P_V_BIT = 6  # Overflow Flag
    P_N_BIT = 7  # Negative Flag

    # 内存段定义
    MAIN_RAM_START = 0x0000
    MAIN_RAM_END = 0xBFFF
    MAIN_RAM_SIZE = 49152  # Main RAM (48KB base, up to 64KB with slot RAM)
    TEXT_RAM_START = 0x0400
    TEXT_RAM_END = 0x07FF
    TEXT_RAM_SIZE = 1024  # Text screen buffer (40x24)
    HIRES_RAM_START = 0x2000
    HIRES_RAM_END = 0x5FFF
    HIRES_RAM_SIZE = 16384  # High-resolution graphics buffer
    AUX_RAM_START = 0x0400
    AUX_RAM_END = 0x09FF
    AUX_RAM_SIZE = 1536  # 80-column text auxiliary RAM
    MONITOR_ROM_START = 0xC100
    MONITOR_ROM_END = 0xCFFF
    MONITOR_ROM_SIZE = 3840  # Monitor ROM (applesoft/Integer)
    BASIC_ROM_START = 0xD000
    BASIC_ROM_END = 0xFFFF
    BASIC_ROM_SIZE = 12288  # Applesoft BASIC ROM
    SLOT_ROM_START = 0xC100
    SLOT_ROM_END = 0xC7FF
    SLOT_ROM_SIZE = 768  # Expansion Slot ROM
    MMIO_START = 0xC080
    MMIO_END = 0xC0FF
    MMIO_SIZE = 128  # I/O Select (slot space)

    # 外设定义
    # Versatile Interface Adapter (6522)
    VIA_BASE = 0xC000
    VIA_ORB_ADDR = 0xC000
    VIA_ORA_ADDR = 0xC001
    VIA_DDRB_ADDR = 0xC002
    VIA_DDRA_ADDR = 0xC003
    VIA_T1C_ADDR = 0xC004
    VIA_T1L_ADDR = 0xC006
    VIA_T2C_ADDR = 0xC008
    VIA_SR_ADDR = 0xC00A
    VIA_ACR_ADDR = 0xC00B
    VIA_PCR_ADDR = 0xC00C
    VIA_IFG_ADDR = 0xC00D
    VIA_IER_ADDR = 0xC00E
    VIA_ORA_NH_ADDR = 0xC00F
    # Peripheral Interface Adapter (6520)
    PIA_BASE = 0xC010
    PIA_PA_ADDR = 0xC010
    PIA_PB_ADDR = 0xC011
    PIA_DDRA_ADDR = 0xC012
    PIA_DDRB_ADDR = 0xC013
    PIA_CA1_ADDR = 0xC014
    PIA_CA2_ADDR = 0xC015
    PIA_CB1_ADDR = 0xC016
    PIA_CB2_ADDR = 0xC017
    # Keyboard (via PIA)
    KBD_BASE = 0xC000
    KBD_KEYDATA_ADDR = 0xC000
    KBD_KEYSTROBE_ADDR = 0xC010
    KBD_KBDCTRL_ADDR = 0xC025
    KBD_KBDERR_ADDR = 0xC026
    # Speaker
    SPEAKER_BASE = 0xC030
    SPEAKER_SPKR_ADDR = 0xC030
    # Game I/O Port
    GAME_PORT_BASE = 0xC050
    GAME_PORT_GAME_SW0_ADDR = 0xC061
    GAME_PORT_GAME_SW1_ADDR = 0xC062
    GAME_PORT_GAME_AN0_ADDR = 0xC064
    GAME_PORT_GAME_AN1_ADDR = 0xC065
    GAME_PORT_GAME_AN2_ADDR = 0xC066
    GAME_PORT_GAME_AN3_ADDR = 0xC067
    GAME_PORT_GAME_TRIG_ADDR = 0xC070
    # Disk II Controller
    DISKII_BASE = 0xC0E0
    DISKII_PHASE0_ADDR = 0xC0E0
    DISKII_PHASE1_ADDR = 0xC0E1
    DISKII_PHASE2_ADDR = 0xC0E2
    DISKII_PHASE3_ADDR = 0xC0E3
    DISKII_Q6L_ADDR = 0xC0EC
    DISKII_Q7L_ADDR = 0xC0ED
    DISKII_Q6R_ADDR = 0xC0EE
    DISKII_Q7R_ADDR = 0xC0EF
    # Video Display Generator
    VIDEO_BASE = 0xC050
    VIDEO_TXTCLR_ADDR = 0xC050
    VIDEO_MIXCLR_ADDR = 0xC051
    VIDEO_TXTPAGE2_ADDR = 0xC054
    VIDEO_TXTPAGE1_ADDR = 0xC055
    VIDEO_LORES_ADDR = 0xC056
    VIDEO_HIRES_ADDR = 0xC057
    VIDEO_DHIRESON_ADDR = 0xC05E
    VIDEO_AN0_ADDR = 0xC058
    VIDEO_AN1_ADDR = 0xC059
    VIDEO_AN2_ADDR = 0xC05A
    VIDEO_AN3_ADDR = 0xC05B
    VIDEO__80STORE_ADDR = 0xC000
    # RAM Read/Write Control
    RAMRD_BASE = 0xC080
    RAMRD_INTCXROM_ADDR = 0xCFFF

    # 中断向量定义
    INT_RESET = 0  # Power-on Reset
    INT_NMI = 1  # Non-Maskable Interrupt (from VIA)
    INT_IRQ = 2  # IRQ from VIA/timer/slot
    INT_BRK = 3  # BRK Instruction

    # 引脚定义
    PIN_VCC = 1  # +5V Power
    PIN_GND = 2  # Ground
    PIN_RESET = 3  # System Reset
    PIN_CLK = 4  # System Clock (1.023MHz NTSC)
    PIN_RDY = 5  # CPU Ready
    PIN_NMI = 6  # Non-Maskable Interrupt
    PIN_IRQ = 7  # Interrupt Request
    PIN_SO = 8  # Set Overflow
    PIN_RWB = 9  # Read/Write Bar
    PIN_SYNC = 10  # Instruction Sync
    PIN_A0_A15 = 11  # Address Bus (16-bit)
    PIN_D0_D7 = 12  # Data Bus (8-bit)
    PIN_PHASE0 = 13  # Phase 0 (4MHz system)
    PIN_PHASE1 = 14  # Phase 1
    PIN_PHASE2 = 15  # Phase 2
    PIN_PHASE3 = 16  # Phase 3

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
            "address": 0x00,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Accumulator",
            "value": 0
        }
        self._registers["X"] = {
            "address": 0x01,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "X Index Register",
            "value": 0
        }
        self._registers["Y"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Y Index Register",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x03,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x04,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["P"] = {
            "address": 0x06,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Processor Status",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["VIA"] = {
            "base": 0xC000,
            "type": "timer",
            "description": "Versatile Interface Adapter (6522)",
            "registers": {
                "ORB": {
                    "address": 0xC000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ORA": {
                    "address": 0xC001,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRB": {
                    "address": 0xC002,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRA": {
                    "address": 0xC003,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "T1C": {
                    "address": 0xC004,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "T1L": {
                    "address": 0xC006,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "T2C": {
                    "address": 0xC008,
                    "size": 2,
                    "type": "uint16",
                    "value": 0
                },
                "SR": {
                    "address": 0xC00A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ACR": {
                    "address": 0xC00B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PCR": {
                    "address": 0xC00C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IFG": {
                    "address": 0xC00D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "IER": {
                    "address": 0xC00E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ORA_NH": {
                    "address": 0xC00F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PIA"] = {
            "base": 0xC010,
            "type": "gpio",
            "description": "Peripheral Interface Adapter (6520)",
            "registers": {
                "PA": {
                    "address": 0xC010,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PB": {
                    "address": 0xC011,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRA": {
                    "address": 0xC012,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DDRB": {
                    "address": 0xC013,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CA1": {
                    "address": 0xC014,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CA2": {
                    "address": 0xC015,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CB1": {
                    "address": 0xC016,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CB2": {
                    "address": 0xC017,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["KBD"] = {
            "base": 0xC000,
            "type": "input",
            "description": "Keyboard (via PIA)",
            "registers": {
                "KEYDATA": {
                    "address": 0xC000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KEYSTROBE": {
                    "address": 0xC010,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBDCTRL": {
                    "address": 0xC025,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "KBDERR": {
                    "address": 0xC026,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SPEAKER"] = {
            "base": 0xC030,
            "type": "audio",
            "description": "Speaker",
            "registers": {
                "SPKR": {
                    "address": 0xC030,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["GAME_PORT"] = {
            "base": 0xC050,
            "type": "input",
            "description": "Game I/O Port",
            "registers": {
                "GAME_SW0": {
                    "address": 0xC061,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GAME_SW1": {
                    "address": 0xC062,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GAME_AN0": {
                    "address": 0xC064,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GAME_AN1": {
                    "address": 0xC065,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GAME_AN2": {
                    "address": 0xC066,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GAME_AN3": {
                    "address": 0xC067,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "GAME_TRIG": {
                    "address": 0xC070,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["DISKII"] = {
            "base": 0xC0E0,
            "type": "storage",
            "description": "Disk II Controller",
            "registers": {
                "PHASE0": {
                    "address": 0xC0E0,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PHASE1": {
                    "address": 0xC0E1,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PHASE2": {
                    "address": 0xC0E2,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PHASE3": {
                    "address": 0xC0E3,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Q6L": {
                    "address": 0xC0EC,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Q7L": {
                    "address": 0xC0ED,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Q6R": {
                    "address": 0xC0EE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "Q7R": {
                    "address": 0xC0EF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["VIDEO"] = {
            "base": 0xC050,
            "type": "video",
            "description": "Video Display Generator",
            "registers": {
                "TXTCLR": {
                    "address": 0xC050,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "MIXCLR": {
                    "address": 0xC051,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TXTPAGE2": {
                    "address": 0xC054,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "TXTPAGE1": {
                    "address": 0xC055,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "LORES": {
                    "address": 0xC056,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HIRES": {
                    "address": 0xC057,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "DHIRESON": {
                    "address": 0xC05E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AN0": {
                    "address": 0xC058,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AN1": {
                    "address": 0xC059,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AN2": {
                    "address": 0xC05A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "AN3": {
                    "address": 0xC05B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "80STORE": {
                    "address": 0xC000,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["RAMRD"] = {
            "base": 0xC080,
            "type": "memory",
            "description": "RAM Read/Write Control",
            "registers": {
                "INTCXROM": {
                    "address": 0xCFFF,
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
        return f"Apple_IIe({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Apple_IIe()
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

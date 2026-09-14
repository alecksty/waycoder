"""
Zilog-Z80设备定义 - Python模块
生成自: Zilog/Z80/Zilog-Z80
版本: 1.0
日期: 2026-04-16
作者: VML Team
描述: Sega Master System (Mark III) main processor - Zilog Z80A @ 3.58MHz
CPU架构: Z80
位宽: 8位
时钟频率: 3580000 Hz
"""

import ctypes
import struct
from typing import Union, Optional

class Zilog_Z80:
    """Zilog-Z80设备类"""

    # 设备信息
    DEVICE_NAME = "Zilog-Z80"
    MANUFACTURER = "Zilog"
    FAMILY = "Z80"
    VERSION = "1.0"
    ARCHITECTURE = "Z80"
    BITS = 8
    CLOCK_FREQUENCY = 3580000

    # 寄存器地址定义
    A_ADDR = 0x00  # Accumulator
    F_ADDR = 0x01  # Flags Register
    F_C_BIT = 0  # Carry
    F_N_BIT = 1  # Subtract
    F_P_BIT = 2  # Parity/Overflow
    F_H_BIT = 4  # Half Carry
    F_Z_BIT = 6  # Zero
    F_S_BIT = 7  # Sign/Negative
    B_ADDR = 0x02  # B Register
    C_ADDR = 0x03  # C Register
    D_ADDR = 0x04  # D Register
    E_ADDR = 0x05  # E Register
    H_ADDR = 0x06  # H Register
    L_ADDR = 0x07  # L Register
    AF_ADDR = 0x08  # Alternate AF
    BC_ADDR = 0x0A  # Alternate BC
    DE_ADDR = 0x0C  # Alternate DE
    HL_ADDR = 0x0E  # Alternate HL
    IX_ADDR = 0x10  # Index Register X
    IY_ADDR = 0x12  # Index Register Y
    SP_ADDR = 0x14  # Stack Pointer
    PC_ADDR = 0x16  # Program Counter
    I_ADDR = 0x18  # Interrupt Vector Register
    R_ADDR = 0x19  # Memory Refresh Register
    IM_ADDR = 0x1A  # Interrupt Mode (0/1/2)

    # 内存段定义
    WRAM_START = 0xC000
    WRAM_END = 0xC7FF
    WRAM_SIZE = 2048  # Work RAM (2KB internal)
    WRAM_SHADOW_START = 0xE000
    WRAM_SHADOW_END = 0xE7FF
    WRAM_SHADOW_SIZE = 2048  # Work RAM Shadow (Echo RAM)
    VRAM_START = 0x4000
    VRAM_END = 0x7FFF
    VRAM_SIZE = 16384  # Video RAM (16KB)
    SRAM_START = 0x8000
    SRAM_END = 0xBFFF
    SRAM_SIZE = 16384  # Cartridge SRAM (if present)
    CART_ROM_START = 0x0000
    CART_ROM_END = 0x7FFF
    CART_ROM_SIZE = 32768  # Cartridge ROM (up to 48KB)
    BIOS_START = 0x0000
    BIOS_END = 0x1FFF
    BIOS_SIZE = 8192  # BIOS ROM (Master System built-in, 8KB)
    IO_REGS_START = 0x3F00
    IO_REGS_END = 0x3FFF
    IO_REGS_SIZE = 256  # I/O Register Area

    # 外设定义
    # Video Display Processor (TMS9918A variant)
    VDP_BASE = 0xBE
    VDP_VDP_CTRL_ADDR = 0xBF
    VDP_VDP_DATA_ADDR = 0xBE
    VDP_VDP_STATUS_ADDR = 0xBF
    VDP_VDP_STATUS_FIFO_FULL_BIT = 0  # VRAM to CPU Transfer Pending
    VDP_VDP_STATUS_FIFO_EMPTY_BIT = 1  # VRAM Write FIFO Empty
    VDP_VDP_STATUS_INT_FLAG_BIT = 7  # V-Blank / Sprite Collision Flag
    VDP_R0_ADDR = 0x00
    VDP_R0_M3_BIT = 0  # Mode 3 Enable
    VDP_R0_M2_BIT = 1  # Mode 2 Enable
    VDP_R0_M1_BIT = 2  # Mode 1 Enable
    VDP_R0_DISPLAY_DISABLE_BIT = 3  # Display Disable (1=blank screen)
    VDP_R0_VIRQ_EN_BIT = 4  # Vertical Interrupt Enable
    VDP_R0_M4_BIT = 5  # Mode 4 Enable (SMS2 only)
    VDP_R0_SPRITE_SHIFT_BIT = 6  # Sprite Double Height
    VDP_R0_HVC_LATCH_BIT = 7  # H-Counter Latch Enable
    VDP_R1_ADDR = 0x01
    VDP_R1_DISPLAY_BIT = 3  # Display Enable (1=active)
    VDP_R1_FRAME_INT_BIT = 4  # Frame Interrupt (V-Blank) Enable
    VDP_R1_M4_BIT = 5  # Mode 4 (256-color)
    VDP_R1_SMS_MODE_BIT = 6  # SMS Display Mode (vs Coleco)
    VDP_R1_EXT_VIDEO_BIT = 7  # External Video Enable
    VDP_R2_ADDR = 0x02
    VDP_R3_ADDR = 0x03
    VDP_R4_ADDR = 0x04
    VDP_R5_ADDR = 0x05
    VDP_R6_ADDR = 0x06
    VDP_R7_ADDR = 0x07
    VDP_R8_ADDR = 0x08
    VDP_R8_HSCROLL_EN_BIT = 0  # Horizontal Scroll Enable
    VDP_R8_VSCROLL_EN_BIT = 1  # Vertical Scroll Enable
    VDP_R8_LINE_INT_BIT = 4  # Line Interrupt Enable
    VDP_R8_VSCROLL_2X_BIT = 7  # Vertical Scroll 2x Speed
    VDP_R9_ADDR = 0x09
    VDP_R10_ADDR = 0x0A
    VDP_R11_ADDR = 0x0B
    VDP_R12_ADDR = 0x0C
    VDP_R13_ADDR = 0x0D
    VDP_R14_ADDR = 0x0E
    VDP_R15_ADDR = 0x0F
    VDP_VCOUNTER_ADDR = 0x7E
    VDP_HCOUNTER_ADDR = 0x7F
    # SN76489 Programmable Sound Generator (3 Square + 1 Noise)
    PSG_BASE = 0x7F
    PSG_CH0_FREQ_ADDR = 0x00
    PSG_CH1_FREQ_ADDR = 0x02
    PSG_CH2_FREQ_ADDR = 0x04
    PSG_CH3_CONFIG_ADDR = 0x06
    PSG_CH3_CONFIG_TYPE_BIT = 0  # Noise Type (0=White, 1=Periodic, 2-3=Periodic at freq/2^type)
    PSG_CH3_CONFIG_VOLUME_BIT = 0  # Volume (0-15)
    PSG_CH0_VOLUME_ADDR = 0x01
    PSG_CH1_VOLUME_ADDR = 0x03
    PSG_CH2_VOLUME_ADDR = 0x05
    # I/O Port Registers
    PORTS_BASE = 0x3F
    PORTS_PORT_A_ADDR = 0x3F
    PORTS_PORT_A_UP_BIT = 0  # Up (0=pressed)
    PORTS_PORT_A_DOWN_BIT = 1  # Down (0=pressed)
    PORTS_PORT_A_LEFT_BIT = 2  # Left (0=pressed)
    PORTS_PORT_A_RIGHT_BIT = 3  # Right (0=pressed)
    PORTS_PORT_A_TR_BIT = 4  # Button TR (0=pressed)
    PORTS_PORT_A_TL_BIT = 5  # Button TL (0=pressed)
    PORTS_PORT_B_ADDR = 0x3F
    PORTS_PORT_B_UP_BIT = 0  # Up (0=pressed)
    PORTS_PORT_B_DOWN_BIT = 1  # Down (0=pressed)
    PORTS_PORT_B_LEFT_BIT = 2  # Left (0=pressed)
    PORTS_PORT_B_RIGHT_BIT = 3  # Right (0=pressed)
    PORTS_PORT_B_TR_BIT = 4  # Button TR (0=pressed)
    PORTS_PORT_B_TL_BIT = 5  # Button TL (0=pressed)
    PORTS_PORT_A_DDR_ADDR = 0x3F
    PORTS_PORT_B_DDR_ADDR = 0x3F
    # Sega Mapper (Memory Bank Switching)
    SEGAMAPPER_BASE = 0xFFFD
    SEGAMAPPER_ROM_BANK0_ADDR = 0xFFFD
    SEGAMAPPER_ROM_BANK1_ADDR = 0xFFFE
    SEGAMAPPER_ROM_BANK2_ADDR = 0xFFFF
    # Memory Mapper Control
    MAPPER_BASE = 0xFFFF
    MAPPER_SRAM_BANK_ADDR = 0xFFF8

    # 中断向量定义
    INT_NMI = 0  # Non-Maskable Interrupt (Pause button / V-Blank)
    INT_INT_VBLANK = 1  # V-Blank Interrupt (Frame end)
    INT_INT_LINE = 2  # Scanline Interrupt (Line counter match)
    INT_INT_EXT = 3  # External I/O Interrupt

    # 引脚定义
    PIN_A = 1  # Power Supply
    PIN_GND = 2  # Ground
    PIN_PHI = 3  # System Clock (3.579545 MHz NTSC / 3.546894 MHz PAL)
    PIN_RESET = 4  # Reset (active low)
    PIN_M1 = 5  # Machine Cycle 1 (instruction fetch)
    PIN_MREQ = 6  # Memory Request
    PIN_IORQ = 7  # I/O Request
    PIN_RD = 8  # Read Strobe
    PIN_WR = 9  # Write Strobe
    PIN_HALT = 10  # Halt State
    PIN_WAIT = 11  # Wait State Request
    PIN_INT = 12  # Interrupt Request (active low)
    PIN_NMI = 13  # Non-Maskable Interrupt (active low)
    PIN_BUSRQ = 14  # Bus Request (active low)
    PIN_BUSAK = 15  # Bus Acknowledge (active low)
    PIN_A0 = 16  # Address Bus Bit 0
    PIN_A1 = 17  # Address Bus Bit 1
    PIN_A2 = 18  # Address Bus Bit 2
    PIN_A3 = 19  # Address Bus Bit 3
    PIN_A4 = 20  # Address Bus Bit 4
    PIN_A5 = 21  # Address Bus Bit 5
    PIN_A6 = 22  # Address Bus Bit 6
    PIN_A7 = 23  # Address Bus Bit 7
    PIN_A8 = 24  # Address Bus Bit 8
    PIN_A9 = 25  # Address Bus Bit 9
    PIN_A10 = 26  # Address Bus Bit 10
    PIN_A11 = 27  # Address Bus Bit 11
    PIN_A12 = 28  # Address Bus Bit 12
    PIN_A13 = 29  # Address Bus Bit 13
    PIN_A14 = 30  # Address Bus Bit 14
    PIN_A15 = 31  # Address Bus Bit 15
    PIN_D0 = 32  # Data Bus Bit 0
    PIN_D1 = 33  # Data Bus Bit 1
    PIN_D2 = 34  # Data Bus Bit 2
    PIN_D3 = 35  # Data Bus Bit 3
    PIN_D4 = 36  # Data Bus Bit 4
    PIN_D5 = 37  # Data Bus Bit 5
    PIN_D6 = 38  # Data Bus Bit 6
    PIN_D7 = 39  # Data Bus Bit 7
    PIN_AUDIO_OUT = 40  # Audio Output
    PIN_VIDEO_SYNC = 41  # Composite Video Sync
    PIN_VIDEO_OUT = 42  # Composite Video Output

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
        self._registers["F"] = {
            "address": 0x01,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Flags Register",
            "value": 0
        }
        self._registers["B"] = {
            "address": 0x02,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "B Register",
            "value": 0
        }
        self._registers["C"] = {
            "address": 0x03,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "C Register",
            "value": 0
        }
        self._registers["D"] = {
            "address": 0x04,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "D Register",
            "value": 0
        }
        self._registers["E"] = {
            "address": 0x05,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "E Register",
            "value": 0
        }
        self._registers["H"] = {
            "address": 0x06,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "H Register",
            "value": 0
        }
        self._registers["L"] = {
            "address": 0x07,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "L Register",
            "value": 0
        }
        self._registers["AF_"] = {
            "address": 0x08,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate AF",
            "value": 0
        }
        self._registers["BC_"] = {
            "address": 0x0A,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate BC",
            "value": 0
        }
        self._registers["DE_"] = {
            "address": 0x0C,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate DE",
            "value": 0
        }
        self._registers["HL_"] = {
            "address": 0x0E,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Alternate HL",
            "value": 0
        }
        self._registers["IX"] = {
            "address": 0x10,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Index Register X",
            "value": 0
        }
        self._registers["IY"] = {
            "address": 0x12,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Index Register Y",
            "value": 0
        }
        self._registers["SP"] = {
            "address": 0x14,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Stack Pointer",
            "value": 0
        }
        self._registers["PC"] = {
            "address": 0x16,
            "size": 2,
            "type": "uint16",
            "access": "rw",
            "description": "Program Counter",
            "value": 0
        }
        self._registers["I"] = {
            "address": 0x18,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Interrupt Vector Register",
            "value": 0
        }
        self._registers["R"] = {
            "address": 0x19,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Memory Refresh Register",
            "value": 0
        }
        self._registers["IM"] = {
            "address": 0x1A,
            "size": 1,
            "type": "uint8",
            "access": "rw",
            "description": "Interrupt Mode (0/1/2)",
            "value": 0
        }

    def _initialize_peripherals(self):
        """初始化外设"""
        self._peripherals["VDP"] = {
            "base": 0xBE,
            "type": "video",
            "description": "Video Display Processor (TMS9918A variant)",
            "registers": {
                "VDP_CTRL": {
                    "address": 0xBF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_DATA": {
                    "address": 0xBE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VDP_STATUS": {
                    "address": 0xBF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R0": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R1": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R2": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R3": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R4": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R5": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R6": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R7": {
                    "address": 0x07,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R8": {
                    "address": 0x08,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R9": {
                    "address": 0x09,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R10": {
                    "address": 0x0A,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R11": {
                    "address": 0x0B,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R12": {
                    "address": 0x0C,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R13": {
                    "address": 0x0D,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R14": {
                    "address": 0x0E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "R15": {
                    "address": 0x0F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "VCOUNTER": {
                    "address": 0x7E,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "HCOUNTER": {
                    "address": 0x7F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PSG"] = {
            "base": 0x7F,
            "type": "audio",
            "description": "SN76489 Programmable Sound Generator (3 Square + 1 Noise)",
            "registers": {
                "CH0_FREQ": {
                    "address": 0x00,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH1_FREQ": {
                    "address": 0x02,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH2_FREQ": {
                    "address": 0x04,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH3_CONFIG": {
                    "address": 0x06,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH0_VOLUME": {
                    "address": 0x01,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH1_VOLUME": {
                    "address": 0x03,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "CH2_VOLUME": {
                    "address": 0x05,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["PORTS"] = {
            "base": 0x3F,
            "type": "io",
            "description": "I/O Port Registers",
            "registers": {
                "PORT_A": {
                    "address": 0x3F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORT_B": {
                    "address": 0x3F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORT_A_DDR": {
                    "address": 0x3F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "PORT_B_DDR": {
                    "address": 0x3F,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["SegaMapper"] = {
            "base": 0xFFFD,
            "type": "memory",
            "description": "Sega Mapper (Memory Bank Switching)",
            "registers": {
                "ROM_BANK0": {
                    "address": 0xFFFD,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ROM_BANK1": {
                    "address": 0xFFFE,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
                "ROM_BANK2": {
                    "address": 0xFFFF,
                    "size": 1,
                    "type": "uint8",
                    "value": 0
                },
            }
        }
        self._peripherals["MAPPER"] = {
            "base": 0xFFFF,
            "type": "memory",
            "description": "Memory Mapper Control",
            "registers": {
                "SRAM_BANK": {
                    "address": 0xFFF8,
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
        return f"Zilog_Z80({info['name']} v{info['version']})"

if __name__ == "__main__":
    # 使用示例
    device = Zilog_Z80()
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

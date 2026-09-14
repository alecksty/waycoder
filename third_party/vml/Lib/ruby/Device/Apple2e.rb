# Apple-IIe 设备定义 - Ruby 模块
# 生成自: Apple Computer/Apple II/Apple-IIe
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: Apple II Enhanced - 8-bit personal computer with MOS 6502 CPU
# CPU架构: MOS-6502
# 位宽: 8位
# 时钟频率: 1021800 Hz

module Apple_IIe

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

end

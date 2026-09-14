# Apple-II 设备定义 - Ruby 模块
# 生成自: Apple Computer/Apple II/Apple-II
# 版本: 1.0
# 日期: 2026-04-17
# 作者: VML Team
# 描述: Apple II personal computer with MOS 6502 CPU, 48KB RAM, and color graphics
# CPU架构: MOS 6502
# 位宽: 8位
# 时钟频率: 1023000 Hz

module Apple_II

  # 寄存器地址定义
  A_ADDR = 0  # Accumulator
  X_ADDR = 0  # Index Register X
  Y_ADDR = 0  # Index Register Y
  SP_ADDR = 0  # Stack Pointer
  PC_ADDR = 0  # Program Counter
  P_ADDR = 0  # Status Register

  # 外设定义
  # Apple II keyboard
  KEYBOARD_BASE = 
  KEYBOARD_KBD_ADDR = 0xC000
  KEYBOARD_KBDSTRB_ADDR = 0xC010
  # Built-in speaker
  SPEAKER_BASE = 
  SPEAKER_SPKR_ADDR = 0xC030
  # Cassette tape interface
  CASSETTE_BASE = 
  CASSETTE_TAPEIN_ADDR = 0xC060
  CASSETTE_TAPEOUT_ADDR = 0xC020
  # Game controller port
  GAMEPORT_BASE = 
  GAMEPORT_PADDLE0_ADDR = 0xC064
  GAMEPORT_PADDLE1_ADDR = 0xC065
  GAMEPORT_PADDLE2_ADDR = 0xC066
  GAMEPORT_PADDLE3_ADDR = 0xC067
  GAMEPORT_BUTTON0_ADDR = 0xC061
  GAMEPORT_BUTTON1_ADDR = 0xC062
  # Disk II controller
  DISKCONTROLLER_BASE = 
  DISKCONTROLLER_DISKUNIT_ADDR = 0xC0E0
  DISKCONTROLLER_DISKCMD_ADDR = 0xC0E8
  DISKCONTROLLER_DISKSTAT_ADDR = 0xC0E9
  DISKCONTROLLER_DISKDATA_ADDR = 0xC0EA

  # 中断向量定义
  INT_NMI = 65526  # Non-maskable interrupt
  INT_RESET = 65528  # Reset vector
  INT_IRQ = 65530  # Interrupt request
  INT_BRK = 65532  # Break instruction

end

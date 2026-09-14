' ULN2003寄存器定义
' 生成自: ST/TI/Motor/ULN2003
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: ULN2003 7-Channel Darlington Driver + 28BYJ-48 Stepper Motor (5V)

' CPU架构: Motor
' 位宽: 8位
' 时钟频率: 0 Hz

' 外设定义
' ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step)
CONST ULN2003_BASE = 0x00
CONST ULN2003_STEPPER = 0x00
CONST ULN2003_STEP_MODE = 0x01
CONST ULN2003_STEPS = 0x02
CONST ULN2003_DELAY_MS = 0x04
CONST ULN2003_POSITION = 0x05
CONST ULN2003_DIRECTION = 0x07

' 设备初始化子程序
SUB uln2003_init()
    ' 初始化代码
END SUB

' 常用函数
FUNCTION read_register(addr AS INTEGER) AS INTEGER
    ' 读取寄存器值
    RETURN PEEK(addr)
END FUNCTION

SUB write_register(addr AS INTEGER, value AS INTEGER)
    ' 写入寄存器值
    POKE addr, value
END SUB

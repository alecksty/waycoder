' SG90寄存器定义
' 生成自: Tower Pro/Motor/SG90
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: SG90 Micro Servo Motor (0-180°, 4.8V-6V)

' CPU架构: Motor
' 位宽: 8位
' 时钟频率: 0 Hz

' 外设定义
' SG90 Micro Servo (500-2500us pulse, 50Hz)
CONST SG90_BASE = 0x00
CONST SG90_ANGLE = 0x00
CONST SG90_PULSE_MIN = 0x01
CONST SG90_PULSE_MAX = 0x03
CONST SG90_CURRENT_ANGLE = 0x05
CONST SG90_SPEED = 0x06

' 设备初始化子程序
SUB sg90_init()
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

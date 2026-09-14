' TB6612寄存器定义
' 生成自: Toshiba/Motor/TB6612
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: TB6612FNG Dual DC Motor Driver (1.2A continuous, 3.2A peak, 2.5V-13.5V)

' CPU架构: Motor
' 位宽: 8位
' 时钟频率: 100000 Hz

' 外设定义
' TB6612 Dual Motor Driver (2.5V-13.5V, 1.2A/3.2A peak)
CONST TB6612_BASE = 0x00
CONST TB6612_MOTOR_A = 0x00
CONST TB6612_MOTOR_A_AIN1 = 0  ' Motor A input 1
CONST TB6612_MOTOR_A_AIN2 = 1  ' Motor A input 2
CONST TB6612_MOTOR_A_PWMA = 2  ' Motor A PWM enable
CONST TB6612_MOTOR_B = 0x01
CONST TB6612_MOTOR_B_BIN1 = 0  ' Motor B input 1
CONST TB6612_MOTOR_B_BIN2 = 1  ' Motor B input 2
CONST TB6612_MOTOR_B_PWMB = 2  ' Motor B PWM enable
CONST TB6612_SPEED_A = 0x02
CONST TB6612_SPEED_B = 0x04
CONST TB6612_STBY = 0x06

' 设备初始化子程序
SUB tb6612_init()
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

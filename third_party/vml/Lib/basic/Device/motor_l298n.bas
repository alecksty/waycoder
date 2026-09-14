' L298N寄存器定义
' 生成自: STMicroelectronics/Motor/L298N
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: L298N Dual H-Bridge DC Motor Driver (2A per channel, 5V-35V)

' CPU架构: Motor
' 位宽: 8位
' 时钟频率: 0 Hz

' 外设定义
' L298N Dual H-Bridge Motor Driver (5V logic, 5-35V motor)
CONST L298N_BASE = 0x00
CONST L298N_MOTOR_A = 0x00
CONST L298N_MOTOR_A_IN1 = 0  ' Motor A Input 1
CONST L298N_MOTOR_A_IN2 = 1  ' Motor A Input 2
CONST L298N_MOTOR_A_ENA = 2  ' Motor A Enable/PWM
CONST L298N_MOTOR_B = 0x01
CONST L298N_MOTOR_B_IN3 = 0  ' Motor B Input 3
CONST L298N_MOTOR_B_IN4 = 1  ' Motor B Input 4
CONST L298N_MOTOR_B_ENB = 2  ' Motor B Enable/PWM
CONST L298N_SPEED_A = 0x02
CONST L298N_SPEED_B = 0x03
CONST L298N_STATUS = 0x04

' 设备初始化子程序
SUB l298n_init()
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

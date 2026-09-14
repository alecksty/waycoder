' MLX90614寄存器定义
' 生成自: Melexis/Sensor/MLX90614
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: MLX90614 Infrared Thermometer (I2C, non-contact, -70 to +380°C, 17-bit)

' CPU架构: Sensor
' 位宽: 17位
' 时钟频率: 100000 Hz

' 内存段定义
' Internal EEPROM (calibration data)
CONST EEPROM_START = 0x00
CONST EEPROM_END = 0x1F
CONST EEPROM_SIZE = 32

' 外设定义
' MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39)
CONST MLX90614_BASE = 0x5A
CONST MLX90614_T_AMBIENT = 0x06
CONST MLX90614_T_OBJECT1 = 0x07
CONST MLX90614_T_OBJECT2 = 0x08
CONST MLX90614_RAW_IR1 = 0x04
CONST MLX90614_RAW_IR2 = 0x05
CONST MLX90614_EMISSIVITY = 0x04

' 设备初始化子程序
SUB mlx90614_init()
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

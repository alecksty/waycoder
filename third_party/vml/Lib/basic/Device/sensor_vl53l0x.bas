' VL53L0X寄存器定义
' 生成自: STMicroelectronics/Sensor/VL53L0X
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: VL53L0X ToF Laser Distance Sensor (I2C, 2cm-200cm, 940nm VCSEL)

' CPU架构: Sensor
' 位宽: 16位
' 时钟频率: 400000 Hz

' 外设定义
' VL53L0X ToF Distance Sensor (0x29, 2.6V-3.5V)
CONST VL53L0X_BASE = 0x29
CONST VL53L0X_DISTANCE = 0x00
CONST VL53L0X_SIGNAL_RATE = 0x02
CONST VL53L0X_AMBIENT_RATE = 0x04
CONST VL53L0X_SPAD_COUNT = 0x06
CONST VL53L0X_RANGE_STATUS = 0x08
CONST VL53L0X_TIMING_BUDGET = 0x09
CONST VL53L0X_INTER_MEAS = 0x0D
CONST VL53L0X_MODE = 0x0E

' 引脚定义
CONST PIN_XSHUT = 1  ' Shutdown pin (active low)
CONST PIN_INT = 2  ' Interrupt (open-drain)

' 设备初始化子程序
SUB vl53l0x_init()
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

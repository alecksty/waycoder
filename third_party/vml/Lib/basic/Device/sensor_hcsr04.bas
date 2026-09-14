' HC_SR04寄存器定义
' 生成自: Generic/Sensor/HC_SR04
' 版本: 1.0
' 日期: 2026-05-06
' 作者: VML Team
' 描述: Ultrasonic Distance Sensor (2cm-400cm)

' CPU架构: Sensor
' 位宽: 8位
' 时钟频率: 0 Hz

' 内存段定义
' PCB Module (45x20x15mm)
CONST PACKAGE_START = 0x00
CONST PACKAGE_END = 0x00
CONST PACKAGE_SIZE = 0

' 外设定义
' HC-SR04 Ultrasonic Sensor (4.5V-5.5V)
CONST HC_SR04_BASE = 0x00
CONST HC_SR04_TRIG = 0x00
CONST HC_SR04_DISTANCE_H = 0x01
CONST HC_SR04_DISTANCE_L = 0x02
CONST HC_SR04_STATUS = 0x03
CONST HC_SR04_STATUS_BUSY = 0  ' 1=Measurement in progress
CONST HC_SR04_STATUS_VALID = 1  ' 1=Valid measurement available
CONST HC_SR04_STATUS_TIMEOUT = 2  ' 1=No echo received (out of range)

' 设备初始化子程序
SUB hc_sr04_init()
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

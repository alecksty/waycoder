# HC_SR04 设备定义 - R 脚本
# 生成自: Generic/Sensor/HC_SR04
# 版本: 1.0
# 日期: 2026-05-06
# 作者: VML Team
# 描述: Ultrasonic Distance Sensor (2cm-400cm)
# CPU架构: Sensor
# 位宽: 8位
# 时钟频率: 0 Hz

# 内存段定义
PACKAGE_START <- 0x00
PACKAGE_END <- 0x00
PACKAGE_SIZE <- 0  # PCB Module (45x20x15mm)

# 外设定义
# HC-SR04 Ultrasonic Sensor (4.5V-5.5V)
HC_SR04_BASE <- 0x00
HC_SR04_TRIG_ADDR <- 0x00
HC_SR04_DISTANCE_H_ADDR <- 0x01
HC_SR04_DISTANCE_L_ADDR <- 0x02
HC_SR04_STATUS_ADDR <- 0x03
HC_SR04_STATUS_BUSY_BIT <- 0  # 1=Measurement in progress
HC_SR04_STATUS_VALID_BIT <- 1  # 1=Valid measurement available
HC_SR04_STATUS_TIMEOUT_BIT <- 2  # 1=No echo received (out of range)


# DHT11 设备定义 - R 脚本
# 生成自: Aosong/Sensor/DHT11
# 版本: 1.0
# 日期: 2026-05-06
# 作者: VML Team
# 描述: Digital Temperature and Humidity Sensor (1-Wire)
# CPU架构: Sensor
# 位宽: 8位
# 时钟频率: 500000 Hz

# 内存段定义
PACKAGE_START <- 0x00
PACKAGE_END <- 0x00
PACKAGE_SIZE <- 4  # DIP-4/SMD-4

# 外设定义
# DHT11 1-Wire Sensor (3.0V-5.5V)
DHT11_BASE <- 0x00
DHT11_HUMIDITY_INT_ADDR <- 0x00
DHT11_HUMIDITY_DEC_ADDR <- 0x01
DHT11_TEMP_INT_ADDR <- 0x02
DHT11_TEMP_DEC_ADDR <- 0x03
DHT11_CHECKSUM_ADDR <- 0x04


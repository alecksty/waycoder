# 24C02 设备定义 - R 脚本
# 生成自: Generic/Memory/24C02
# 版本: 1.0
# 日期: 2026-05-06
# 作者: VML Team
# 描述: 2Kbit I2C Serial EEPROM (256 x 8 bits)
# CPU架构: Memory
# 位宽: 8位
# 时钟频率: 400000 Hz

# 内存段定义
EEPROM_START <- 0x00
EEPROM_END <- 0xFF
EEPROM_SIZE <- 256  # EEPROM main memory array (256 bytes, 8-byte page write)

# 外设定义
# 24C02 I2C EEPROM (0x50-0x57, 1.8V-5.5V, DIP-8)
_24C02_BASE <- 0x50
_24C02_STATUS_ADDR <- 0xFF
_24C02_STATUS_BUSY_BIT <- 0  # 1=Write in progress
_24C02_PAGE_SIZE_ADDR <- 0xFE
_24C02_SIZE_ADDR <- 0xFD


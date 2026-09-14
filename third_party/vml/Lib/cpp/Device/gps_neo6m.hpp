#ifndef NEO6M_HPP
#define NEO6M_HPP

// NEO6M寄存器定义
// 生成自: u-blox/GPS/NEO6M
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: GPS
// 位宽: 8位
// 时钟频率: 9600 Hz

// 外设定义
// NEO-6M GPS Module (UART 9600bps, 3.3V-5V)
#define NEO6M_BASE 0x00
#define NEO6M_LATITUDE (*(volatile uint32_t*)0x00000000)
#define NEO6M_LONGITUDE (*(volatile uint32_t*)0x00000004)
#define NEO6M_ALTITUDE (*(volatile uint32_t*)0x00000008)
#define NEO6M_SPEED (*(volatile uint16_t*)0x0000000C)
#define NEO6M_HEADING (*(volatile uint16_t*)0x0000000E)
#define NEO6M_SATELLITES (*(volatile uint8_t*)0x00000010)
#define NEO6M_HDOP (*(volatile uint16_t*)0x00000011)
#define NEO6M_FIX_TYPE (*(volatile uint8_t*)0x00000013)
#define NEO6M_DATE (*(volatile uint32_t*)0x00000014)
#define NEO6M_TIME (*(volatile uint32_t*)0x00000018)
#define NEO6M_VALID (*(volatile uint8_t*)0x0000001C)

void neo6m_init(void);

#ifdef __cplusplus
}
#endif

#endif // NEO6M_HPP

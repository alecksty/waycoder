#ifndef MLX90614_HPP
#define MLX90614_HPP

// MLX90614寄存器定义
// 生成自: Melexis/Sensor/MLX90614
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 17位
// 时钟频率: 100000 Hz

// 内存段定义
// Internal EEPROM (calibration data)
#define EEPROM_START 0x00
#define EEPROM_END 0x1F
#define EEPROM_SIZE 32

// 外设定义
// MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39)
#define MLX90614_BASE 0x5A
#define MLX90614_T_AMBIENT (*(volatile uint16_t*)0x00000060)
#define MLX90614_T_OBJECT1 (*(volatile uint16_t*)0x00000061)
#define MLX90614_T_OBJECT2 (*(volatile uint16_t*)0x00000062)
#define MLX90614_RAW_IR1 (*(volatile uint16_t*)0x0000005E)
#define MLX90614_RAW_IR2 (*(volatile uint16_t*)0x0000005F)
#define MLX90614_EMISSIVITY (*(volatile uint16_t*)0x0000005E)

void mlx90614_init(void);

#ifdef __cplusplus
}
#endif

#endif // MLX90614_HPP

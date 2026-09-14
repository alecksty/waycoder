#ifndef MPU6050_HPP
#define MPU6050_HPP

// MPU6050寄存器定义
// 生成自: InvenSense/TDK/Sensor/MPU6050
// 版本: 1.0
// 日期: 2026-05-06


#ifdef __cplusplus
extern "C" {
#endif

// CPU架构: Sensor
// 位宽: 8位
// 时钟频率: 400000 Hz

// 内存段定义
// QFN-24 (4x4x0.9mm)
#define PACKAGE_START 0x00
#define PACKAGE_END 0x00
#define PACKAGE_SIZE 24

// 外设定义
// MPU6050 IMU (0x68/0x69, 2.375V-3.46V)
#define MPU6050_BASE 0x68
#define MPU6050_SMPLRT_DIV (*(volatile uint8_t*)0x00000081)
#define MPU6050_CONFIG (*(volatile uint8_t*)0x00000082)
#define MPU6050_CONFIG_DLPF_CFG 0  // Digital low-pass filter configuration
#define MPU6050_GYRO_CONFIG (*(volatile uint8_t*)0x00000083)
#define MPU6050_GYRO_CONFIG_FS_SEL 3  // Gyro full scale: 0=±250, 1=±500, 2=±1000, 3=±2000 °/s
#define MPU6050_ACCEL_CONFIG (*(volatile uint8_t*)0x00000084)
#define MPU6050_ACCEL_CONFIG_AFS_SEL 3  // Accel full scale: 0=±2g, 1=±4g, 2=±8g, 3=±16g
#define MPU6050_ACCEL_XOUT_H (*(volatile uint8_t*)0x000000A3)
#define MPU6050_ACCEL_XOUT_L (*(volatile uint8_t*)0x000000A4)
#define MPU6050_ACCEL_YOUT_H (*(volatile uint8_t*)0x000000A5)
#define MPU6050_ACCEL_YOUT_L (*(volatile uint8_t*)0x000000A6)
#define MPU6050_ACCEL_ZOUT_H (*(volatile uint8_t*)0x000000A7)
#define MPU6050_ACCEL_ZOUT_L (*(volatile uint8_t*)0x000000A8)
#define MPU6050_TEMP_OUT_H (*(volatile uint8_t*)0x000000A9)
#define MPU6050_TEMP_OUT_L (*(volatile uint8_t*)0x000000AA)
#define MPU6050_GYRO_XOUT_H (*(volatile uint8_t*)0x000000AB)
#define MPU6050_GYRO_XOUT_L (*(volatile uint8_t*)0x000000AC)
#define MPU6050_GYRO_YOUT_H (*(volatile uint8_t*)0x000000AD)
#define MPU6050_GYRO_YOUT_L (*(volatile uint8_t*)0x000000AE)
#define MPU6050_GYRO_ZOUT_H (*(volatile uint8_t*)0x000000AF)
#define MPU6050_GYRO_ZOUT_L (*(volatile uint8_t*)0x000000B0)
#define MPU6050_PWR_MGMT_1 (*(volatile uint8_t*)0x000000D3)
#define MPU6050_PWR_MGMT_1_DEVICE_RESET 7  // 1=Reset all internal registers
#define MPU6050_PWR_MGMT_1_SLEEP 6  // 1=Sleep mode
#define MPU6050_PWR_MGMT_1_CYCLE 5  // 1=Cycle mode
#define MPU6050_PWR_MGMT_1_CLKSEL 0  // Clock source select
#define MPU6050_WHO_AM_I (*(volatile uint8_t*)0x000000DD)

void mpu6050_init(void);

#ifdef __cplusplus
}
#endif

#endif // MPU6050_HPP

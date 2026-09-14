! MPU6050 设备定义 - Fortran 模块
! 生成自: InvenSense/TDK/Sensor/MPU6050
! 版本: 1.0
! 日期: 2026-05-06
! 作者: VML Team
! 描述: 6-Axis MEMS Accelerometer and Gyroscope (I2C)
! CPU架构: Sensor
! 位宽: 8位
! 时钟频率: 400000 Hz

module mpu6050_device
  implicit none

  ! 内存段定义
  integer, parameter :: PACKAGE_START = 0x00
  integer, parameter :: PACKAGE_END = 0x00
  integer, parameter :: PACKAGE_SIZE = 24  ! QFN-24 (4x4x0.9mm)

  ! 外设定义
  ! MPU6050 IMU (0x68/0x69, 2.375V-3.46V)
  integer, parameter :: MPU6050_BASE = 0x68
  integer, parameter :: MPU6050_SMPLRT_DIV_ADDR = 0x19
  integer, parameter :: MPU6050_CONFIG_ADDR = 0x1A
  integer, parameter :: MPU6050_CONFIG_DLPF_CFG_BIT = 0  ! Digital low-pass filter configuration
  integer, parameter :: MPU6050_GYRO_CONFIG_ADDR = 0x1B
  integer, parameter :: MPU6050_GYRO_CONFIG_FS_SEL_BIT = 3  ! Gyro full scale: 0=±250, 1=±500, 2=±1000, 3=±2000 °/s
  integer, parameter :: MPU6050_ACCEL_CONFIG_ADDR = 0x1C
  integer, parameter :: MPU6050_ACCEL_CONFIG_AFS_SEL_BIT = 3  ! Accel full scale: 0=±2g, 1=±4g, 2=±8g, 3=±16g
  integer, parameter :: MPU6050_ACCEL_XOUT_H_ADDR = 0x3B
  integer, parameter :: MPU6050_ACCEL_XOUT_L_ADDR = 0x3C
  integer, parameter :: MPU6050_ACCEL_YOUT_H_ADDR = 0x3D
  integer, parameter :: MPU6050_ACCEL_YOUT_L_ADDR = 0x3E
  integer, parameter :: MPU6050_ACCEL_ZOUT_H_ADDR = 0x3F
  integer, parameter :: MPU6050_ACCEL_ZOUT_L_ADDR = 0x40
  integer, parameter :: MPU6050_TEMP_OUT_H_ADDR = 0x41
  integer, parameter :: MPU6050_TEMP_OUT_L_ADDR = 0x42
  integer, parameter :: MPU6050_GYRO_XOUT_H_ADDR = 0x43
  integer, parameter :: MPU6050_GYRO_XOUT_L_ADDR = 0x44
  integer, parameter :: MPU6050_GYRO_YOUT_H_ADDR = 0x45
  integer, parameter :: MPU6050_GYRO_YOUT_L_ADDR = 0x46
  integer, parameter :: MPU6050_GYRO_ZOUT_H_ADDR = 0x47
  integer, parameter :: MPU6050_GYRO_ZOUT_L_ADDR = 0x48
  integer, parameter :: MPU6050_PWR_MGMT_1_ADDR = 0x6B
  integer, parameter :: MPU6050_PWR_MGMT_1_DEVICE_RESET_BIT = 7  ! 1=Reset all internal registers
  integer, parameter :: MPU6050_PWR_MGMT_1_SLEEP_BIT = 6  ! 1=Sleep mode
  integer, parameter :: MPU6050_PWR_MGMT_1_CYCLE_BIT = 5  ! 1=Cycle mode
  integer, parameter :: MPU6050_PWR_MGMT_1_CLKSEL_BIT = 0  ! Clock source select
  integer, parameter :: MPU6050_WHO_AM_I_ADDR = 0x75

end module mpu6050_device

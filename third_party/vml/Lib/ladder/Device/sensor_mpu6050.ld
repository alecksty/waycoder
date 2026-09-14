(*
 * MPU6050梯形图定义
 * 生成自: InvenSense/TDK/Sensor/MPU6050
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: 6-Axis MEMS Accelerometer and Gyroscope (I2C)
 * CPU架构: Sensor
 * 位宽: 8位
 * 时钟频率: 400000 Hz
 *)

PROGRAM MPU6050
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* MPU6050 IMU (0x68/0x69, 2.375V-3.46V) *)
    MPU6050_BASE AT %MW0x68 : WORD;
    MPU6050_SMPLRT_DIV AT %MW0x19 : BYTE;
    MPU6050_CONFIG AT %MW0x1A : BYTE;
    MPU6050_CONFIG_DLPF_CFG AT %MW0x1A.0 : BOOL; (* Digital low-pass filter configuration *)
    MPU6050_GYRO_CONFIG AT %MW0x1B : BYTE;
    MPU6050_GYRO_CONFIG_FS_SEL AT %MW0x1B.3 : BOOL; (* Gyro full scale: 0=±250, 1=±500, 2=±1000, 3=±2000 °/s *)
    MPU6050_ACCEL_CONFIG AT %MW0x1C : BYTE;
    MPU6050_ACCEL_CONFIG_AFS_SEL AT %MW0x1C.3 : BOOL; (* Accel full scale: 0=±2g, 1=±4g, 2=±8g, 3=±16g *)
    MPU6050_ACCEL_XOUT_H AT %MW0x3B : BYTE;
    MPU6050_ACCEL_XOUT_L AT %MW0x3C : BYTE;
    MPU6050_ACCEL_YOUT_H AT %MW0x3D : BYTE;
    MPU6050_ACCEL_YOUT_L AT %MW0x3E : BYTE;
    MPU6050_ACCEL_ZOUT_H AT %MW0x3F : BYTE;
    MPU6050_ACCEL_ZOUT_L AT %MW0x40 : BYTE;
    MPU6050_TEMP_OUT_H AT %MW0x41 : BYTE;
    MPU6050_TEMP_OUT_L AT %MW0x42 : BYTE;
    MPU6050_GYRO_XOUT_H AT %MW0x43 : BYTE;
    MPU6050_GYRO_XOUT_L AT %MW0x44 : BYTE;
    MPU6050_GYRO_YOUT_H AT %MW0x45 : BYTE;
    MPU6050_GYRO_YOUT_L AT %MW0x46 : BYTE;
    MPU6050_GYRO_ZOUT_H AT %MW0x47 : BYTE;
    MPU6050_GYRO_ZOUT_L AT %MW0x48 : BYTE;
    MPU6050_PWR_MGMT_1 AT %MW0x6B : BYTE;
    MPU6050_PWR_MGMT_1_DEVICE_RESET AT %MW0x6B.7 : BOOL; (* 1=Reset all internal registers *)
    MPU6050_PWR_MGMT_1_SLEEP AT %MW0x6B.6 : BOOL; (* 1=Sleep mode *)
    MPU6050_PWR_MGMT_1_CYCLE AT %MW0x6B.5 : BOOL; (* 1=Cycle mode *)
    MPU6050_PWR_MGMT_1_CLKSEL AT %MW0x6B.0 : BOOL; (* Clock source select *)
    MPU6050_WHO_AM_I AT %MW0x75 : BYTE;

END_VAR

BEGIN
    (* MPU6050初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM

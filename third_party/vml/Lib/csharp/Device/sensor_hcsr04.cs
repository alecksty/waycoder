using System;

namespace VML.Device.Generic.HC_SR04
{
    /// <summary>
    /// HC_SR04 寄存器定义
    /// 生成自: Generic/Sensor/HC_SR04
    /// 版本: 1.0
    /// </summary>
    public static class HC_SR04
    {
        // CPU架构: Sensor, 8位, 0 Hz

        // 内存段定义
        // PCB Module (45x20x15mm)
        public const int PACKAGE_START = 0x00;
        public const int PACKAGE_END = 0x00;
        public const int PACKAGE_SIZE = 0;

        // 外设定义
        // HC-SR04 Ultrasonic Sensor (4.5V-5.5V)
        public const int HC_SR04_BASE = 0x00;
        public static unsafe byte* HC_SR04_TRIG => (byte*)0x00000000;
        public static unsafe byte* HC_SR04_DISTANCE_H => (byte*)0x00000001;
        public static unsafe byte* HC_SR04_DISTANCE_L => (byte*)0x00000002;
        public static unsafe byte* HC_SR04_STATUS => (byte*)0x00000003;
        public const int HC_SR04_STATUS_BUSY = 0;  // 1=Measurement in progress
        public const int HC_SR04_STATUS_VALID = 1;  // 1=Valid measurement available
        public const int HC_SR04_STATUS_TIMEOUT = 2;  // 1=No echo received (out of range)

        public static void hc_sr04_init()
        {
            // 硬件初始化代码
        }
    }
}

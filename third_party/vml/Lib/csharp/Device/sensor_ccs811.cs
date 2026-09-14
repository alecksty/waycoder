using System;

namespace VML.Device.AMS/ScioSense.CCS811
{
    /// <summary>
    /// CCS811 寄存器定义
    /// 生成自: AMS/ScioSense/Sensor/CCS811
    /// 版本: 1.0
    /// </summary>
    public static class CCS811
    {
        // CPU架构: Sensor, 16位, 400000 Hz

        // 外设定义
        // CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
        public const int CCS811_BASE = 0x5A;
        public static unsafe byte* CCS811_STATUS => (byte*)0x0000005A;
        public static unsafe byte* CCS811_MEAS_MODE => (byte*)0x0000005B;
        public static unsafe uint* CCS811_ALG_RESULT => (uint*)0x0000005C;
        public static unsafe ushort* CCS811_ECO2 => (ushort*)0x0000005C;
        public static unsafe ushort* CCS811_TVOC => (ushort*)0x0000005E;
        public static unsafe ushort* CCS811_RAW_DATA => (ushort*)0x00000060;
        public static unsafe ushort* CCS811_BASELINE => (ushort*)0x00000065;
        public static unsafe byte* CCS811_HW_ID => (byte*)0x0000007A;
        public static unsafe byte* CCS811_ERROR_ID => (byte*)0x0000013A;
        public static unsafe byte* CCS811_APP_START => (byte*)0x0000014E;
        public static unsafe uint* CCS811_SW_RESET => (uint*)0x00000159;

        // 中断向量定义
        public const int IRQ_INT = 0;  // Data ready / interrupt pin

        // 引脚定义
        public const int PIN_WAKE = 1;  // Wake pin (active low)

        public static void ccs811_init()
        {
            // 硬件初始化代码
        }
    }
}

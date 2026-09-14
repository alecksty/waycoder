using System;

namespace VML.Device.u-blox.NEO6M
{
    /// <summary>
    /// NEO6M 寄存器定义
    /// 生成自: u-blox/GPS/NEO6M
    /// 版本: 1.0
    /// </summary>
    public static class NEO6M
    {
        // CPU架构: GPS, 8位, 9600 Hz

        // 外设定义
        // NEO-6M GPS Module (UART 9600bps, 3.3V-5V)
        public const int NEO6M_BASE = 0x00;
        public static unsafe uint* NEO6M_LATITUDE => (uint*)0x00000000;
        public static unsafe uint* NEO6M_LONGITUDE => (uint*)0x00000004;
        public static unsafe uint* NEO6M_ALTITUDE => (uint*)0x00000008;
        public static unsafe ushort* NEO6M_SPEED => (ushort*)0x0000000C;
        public static unsafe ushort* NEO6M_HEADING => (ushort*)0x0000000E;
        public static unsafe byte* NEO6M_SATELLITES => (byte*)0x00000010;
        public static unsafe ushort* NEO6M_HDOP => (ushort*)0x00000011;
        public static unsafe byte* NEO6M_FIX_TYPE => (byte*)0x00000013;
        public static unsafe uint* NEO6M_DATE => (uint*)0x00000014;
        public static unsafe uint* NEO6M_TIME => (uint*)0x00000018;
        public static unsafe byte* NEO6M_VALID => (byte*)0x0000001C;

        public static void neo6m_init()
        {
            // 硬件初始化代码
        }
    }
}

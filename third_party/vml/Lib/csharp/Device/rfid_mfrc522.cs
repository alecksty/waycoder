using System;

namespace VML.Device.NXP.MFRC522
{
    /// <summary>
    /// MFRC522 寄存器定义
    /// 生成自: NXP/RFID/MFRC522
    /// 版本: 1.0
    /// </summary>
    public static class MFRC522
    {
        // CPU架构: RFID, 8位, 10000000 Hz

        // 内存段定义
        // 64-byte FIFO buffer
        public const int FIFO_START = 0x00;
        public const int FIFO_END = 0x3F;
        public const int FIFO_SIZE = 64;

        // 外设定义
        // MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)
        public const int MFRC522_BASE = 0x00;
        public static unsafe byte* MFRC522_CMD => (byte*)0x00000001;
        public static unsafe byte* MFRC522_COM_IRQ => (byte*)0x00000004;
        public const int MFRC522_COM_IRQ_TX_IRQ = 6;  // Transmitter interrupt
        public const int MFRC522_COM_IRQ_RX_IRQ = 5;  // Receiver interrupt
        public const int MFRC522_COM_IRQ_IDLE_IRQ = 4;  // Idle interrupt
        public const int MFRC522_COM_IRQ_TIMER_IRQ = 0;  // Timer interrupt
        public static unsafe byte* MFRC522_COM_IRQ_EN => (byte*)0x00000005;
        public static unsafe byte* MFRC522_ERROR => (byte*)0x00000006;
        public static unsafe byte* MFRC522_STATUS2 => (byte*)0x00000008;
        public static unsafe byte* MFRC522_FIFO_DATA => (byte*)0x00000009;
        public static unsafe byte* MFRC522_FIFO_LEVEL => (byte*)0x0000000A;
        public static unsafe byte* MFRC522_TX_CTRL => (byte*)0x00000014;
        public static unsafe byte* MFRC522_TX_ASK => (byte*)0x00000015;
        public static unsafe byte* MFRC522_MODE => (byte*)0x00000011;
        public static unsafe byte* MFRC522_VERSION => (byte*)0x00000037;

        public static void mfrc522_init()
        {
            // 硬件初始化代码
        }
    }
}

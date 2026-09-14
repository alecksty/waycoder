using System;

namespace VML.Device.Nordic.NRF24L01
{
    /// <summary>
    /// NRF24L01 寄存器定义
    /// 生成自: Nordic/RF/NRF24L01
    /// 版本: 1.0
    /// </summary>
    public static class NRF24L01
    {
        // CPU架构: RF, 8位, 10000000 Hz

        // 外设定义
        // nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)
        public const int NRF24L01_BASE = 0x00;
        public static unsafe byte* NRF24L01_CONFIG => (byte*)0x00000000;
        public const int NRF24L01_CONFIG_PWR_UP = 1;  // Power up (1=on)
        public const int NRF24L01_CONFIG_PRIM_RX = 0;  // Primary RX mode (1=RX, 0=TX)
        public static unsafe byte* NRF24L01_EN_AA => (byte*)0x00000001;
        public static unsafe byte* NRF24L01_EN_RXADDR => (byte*)0x00000002;
        public static unsafe byte* NRF24L01_SETUP_AW => (byte*)0x00000003;
        public static unsafe byte* NRF24L01_SETUP_RETR => (byte*)0x00000004;
        public static unsafe byte* NRF24L01_RF_CH => (byte*)0x00000005;
        public static unsafe byte* NRF24L01_RF_SETUP => (byte*)0x00000006;
        public const int NRF24L01_RF_SETUP_RF_PWR = 1;  // TX power: 00=-18dBm,01=-12dBm,10=-6dBm,11=0dBm
        public const int NRF24L01_RF_SETUP_RF_DR = 3;  // Data rate: 0=1Mbps,1=2Mbps
        public static unsafe byte* NRF24L01_STATUS => (byte*)0x00000007;
        public static unsafe byte* NRF24L01_RX_PW_P0 => (byte*)0x00000011;
        public static unsafe byte* NRF24L01_FIFO_STATUS => (byte*)0x00000017;
        public static unsafe uint* NRF24L01_TX_PAYLOAD => (uint*)0x000000A0;
        public static unsafe uint* NRF24L01_RX_PAYLOAD => (uint*)0x00000061;

        public static void nrf24l01_init()
        {
            // 硬件初始化代码
        }
    }
}

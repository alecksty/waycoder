package vml.device.nordic.nrf24l01;

/**
 * NRF24L01 寄存器定义
 * 生成自: Nordic/RF/NRF24L01
 * 版本: 1.0
 */
public final class NRF24L01 {
    private NRF24L01() {} // 工具类
    // CPU架构: RF, 8位, 10000000 Hz

    // 外设定义
    // nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V)
    public static final int NRF24L01_BASE = (int)0x00;
    public static final int NRF24L01_CONFIG = (int)0x00000000;
    public static final int NRF24L01_CONFIG_PWR_UP = 1;  // Power up (1=on)
    public static final int NRF24L01_CONFIG_PRIM_RX = 0;  // Primary RX mode (1=RX, 0=TX)
    public static final int NRF24L01_EN_AA = (int)0x00000001;
    public static final int NRF24L01_EN_RXADDR = (int)0x00000002;
    public static final int NRF24L01_SETUP_AW = (int)0x00000003;
    public static final int NRF24L01_SETUP_RETR = (int)0x00000004;
    public static final int NRF24L01_RF_CH = (int)0x00000005;
    public static final int NRF24L01_RF_SETUP = (int)0x00000006;
    public static final int NRF24L01_RF_SETUP_RF_PWR = 1;  // TX power: 00=-18dBm,01=-12dBm,10=-6dBm,11=0dBm
    public static final int NRF24L01_RF_SETUP_RF_DR = 3;  // Data rate: 0=1Mbps,1=2Mbps
    public static final int NRF24L01_STATUS = (int)0x00000007;
    public static final int NRF24L01_RX_PW_P0 = (int)0x00000011;
    public static final int NRF24L01_FIFO_STATUS = (int)0x00000017;
    public static final int NRF24L01_TX_PAYLOAD = (int)0x000000A0;
    public static final int NRF24L01_RX_PAYLOAD = (int)0x00000061;

    public static native void nrf24l01_init();
}

package vml.device.nxp.mfrc522;

/**
 * MFRC522 寄存器定义
 * 生成自: NXP/RFID/MFRC522
 * 版本: 1.0
 */
public final class MFRC522 {
    private MFRC522() {} // 工具类
    // CPU架构: RFID, 8位, 10000000 Hz

    // 内存段定义
    // 64-byte FIFO buffer
    public static final int FIFO_START = (int)0x00;
    public static final int FIFO_END = (int)0x3F;
    public static final int FIFO_SIZE = 64;

    // 外设定义
    // MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz)
    public static final int MFRC522_BASE = (int)0x00;
    public static final int MFRC522_CMD = (int)0x00000001;
    public static final int MFRC522_COM_IRQ = (int)0x00000004;
    public static final int MFRC522_COM_IRQ_TX_IRQ = 6;  // Transmitter interrupt
    public static final int MFRC522_COM_IRQ_RX_IRQ = 5;  // Receiver interrupt
    public static final int MFRC522_COM_IRQ_IDLE_IRQ = 4;  // Idle interrupt
    public static final int MFRC522_COM_IRQ_TIMER_IRQ = 0;  // Timer interrupt
    public static final int MFRC522_COM_IRQ_EN = (int)0x00000005;
    public static final int MFRC522_ERROR = (int)0x00000006;
    public static final int MFRC522_STATUS2 = (int)0x00000008;
    public static final int MFRC522_FIFO_DATA = (int)0x00000009;
    public static final int MFRC522_FIFO_LEVEL = (int)0x0000000A;
    public static final int MFRC522_TX_CTRL = (int)0x00000014;
    public static final int MFRC522_TX_ASK = (int)0x00000015;
    public static final int MFRC522_MODE = (int)0x00000011;
    public static final int MFRC522_VERSION = (int)0x00000037;

    public static native void mfrc522_init();
}

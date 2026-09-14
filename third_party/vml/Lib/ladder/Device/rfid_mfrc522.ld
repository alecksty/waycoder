(*
 * MFRC522梯形图定义
 * 生成自: NXP/RFID/MFRC522
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MFRC522 13.56MHz RFID/NFC Reader (SPI, ISO 14443A, MIFARE)
 * CPU架构: RFID
 * 位宽: 8位
 * 时钟频率: 10000000 Hz
 *)

PROGRAM MFRC522
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* MFRC522 NFC Reader (SPI, 3.3V, 13.56MHz) *)
    MFRC522_BASE AT %MW0x00 : WORD;
    MFRC522_CMD AT %MW0x01 : BYTE;
    MFRC522_COM_IRQ AT %MW0x04 : BYTE;
    MFRC522_COM_IRQ_TX_IRQ AT %MW0x04.6 : BOOL; (* Transmitter interrupt *)
    MFRC522_COM_IRQ_RX_IRQ AT %MW0x04.5 : BOOL; (* Receiver interrupt *)
    MFRC522_COM_IRQ_IDLE_IRQ AT %MW0x04.4 : BOOL; (* Idle interrupt *)
    MFRC522_COM_IRQ_TIMER_IRQ AT %MW0x04.0 : BOOL; (* Timer interrupt *)
    MFRC522_COM_IRQ_EN AT %MW0x05 : BYTE;
    MFRC522_ERROR AT %MW0x06 : BYTE;
    MFRC522_STATUS2 AT %MW0x08 : BYTE;
    MFRC522_FIFO_DATA AT %MW0x09 : BYTE;
    MFRC522_FIFO_LEVEL AT %MW0x0A : BYTE;
    MFRC522_TX_CTRL AT %MW0x14 : BYTE;
    MFRC522_TX_ASK AT %MW0x15 : BYTE;
    MFRC522_MODE AT %MW0x11 : BYTE;
    MFRC522_VERSION AT %MW0x37 : BYTE;

END_VAR

BEGIN
    (* MFRC522初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM

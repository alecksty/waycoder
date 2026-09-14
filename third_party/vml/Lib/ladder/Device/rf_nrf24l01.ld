(*
 * NRF24L01梯形图定义
 * 生成自: Nordic/RF/NRF24L01
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: nRF24L01+ 2.4GHz RF Transceiver (SPI, 2Mbps, 125-channel, 6-pipe)
 * CPU架构: RF
 * 位宽: 8位
 * 时钟频率: 10000000 Hz
 *)

PROGRAM NRF24L01
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* nRF24L01+ 2.4GHz Transceiver (SPI, 1.9V-3.6V) *)
    NRF24L01_BASE AT %MW0x00 : WORD;
    NRF24L01_CONFIG AT %MW0x00 : BYTE;
    NRF24L01_CONFIG_PWR_UP AT %MW0x00.1 : BOOL; (* Power up (1=on) *)
    NRF24L01_CONFIG_PRIM_RX AT %MW0x00.0 : BOOL; (* Primary RX mode (1=RX, 0=TX) *)
    NRF24L01_EN_AA AT %MW0x01 : BYTE;
    NRF24L01_EN_RXADDR AT %MW0x02 : BYTE;
    NRF24L01_SETUP_AW AT %MW0x03 : BYTE;
    NRF24L01_SETUP_RETR AT %MW0x04 : BYTE;
    NRF24L01_RF_CH AT %MW0x05 : BYTE;
    NRF24L01_RF_SETUP AT %MW0x06 : BYTE;
    NRF24L01_RF_SETUP_RF_PWR AT %MW0x06.1 : BOOL; (* TX power: 00=-18dBm,01=-12dBm,10=-6dBm,11=0dBm *)
    NRF24L01_RF_SETUP_RF_DR AT %MW0x06.3 : BOOL; (* Data rate: 0=1Mbps,1=2Mbps *)
    NRF24L01_STATUS AT %MW0x07 : BYTE;
    NRF24L01_RX_PW_P0 AT %MW0x11 : BYTE;
    NRF24L01_FIFO_STATUS AT %MW0x17 : BYTE;
    NRF24L01_TX_PAYLOAD AT %MW0xA0 : ARRAY[0..31] OF BOOL;
    NRF24L01_RX_PAYLOAD AT %MW0x61 : ARRAY[0..31] OF BOOL;

END_VAR

BEGIN
    (* NRF24L01初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM

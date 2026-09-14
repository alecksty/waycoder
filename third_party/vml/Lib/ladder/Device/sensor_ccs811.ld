(*
 * CCS811梯形图定义
 * 生成自: AMS/ScioSense/Sensor/CCS811
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: CCS811 VOC/eCO2 Air Quality Sensor (I2C, 400-8192ppm CO2, 0-1187ppb TVOC)
 * CPU架构: Sensor
 * 位宽: 16位
 * 时钟频率: 400000 Hz
 *)

PROGRAM CCS811
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V) *)
    CCS811_BASE AT %MW0x5A : WORD;
    CCS811_STATUS AT %MW0x00 : BYTE;
    CCS811_MEAS_MODE AT %MW0x01 : BYTE;
    CCS811_ALG_RESULT AT %MW0x02 : DWORD;
    CCS811_ECO2 AT %MW0x02 : WORD;
    CCS811_TVOC AT %MW0x04 : WORD;
    CCS811_RAW_DATA AT %MW0x06 : WORD;
    CCS811_BASELINE AT %MW0x0B : WORD;
    CCS811_HW_ID AT %MW0x20 : BYTE;
    CCS811_ERROR_ID AT %MW0xE0 : BYTE;
    CCS811_APP_START AT %MW0xF4 : BYTE;
    CCS811_SW_RESET AT %MW0xFF : DWORD;

    (* 中断变量 *)
    INT_INT : BOOL; (* Data ready / interrupt pin *)

END_VAR

BEGIN
    (* CCS811初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 4: 中断处理 *)
    --|INT_INT|--[中断处理]--

END_PROGRAM

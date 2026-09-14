(*
 * APDS9960梯形图定义
 * 生成自: Broadcom/Avago/Sensor/APDS9960
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: APDS9960 Gesture/Proximity/Ambient Light/RGB Sensor (I2C)
 * CPU架构: Sensor
 * 位宽: 8位
 * 时钟频率: 400000 Hz
 *)

PROGRAM APDS9960
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* APDS9960 Gesture/RGB Sensor (0x39, 3.3V) *)
    APDS9960_BASE AT %MW0x39 : WORD;
    APDS9960_ENABLE AT %MW0x80 : BYTE;
    APDS9960_GESTURE AT %MW0xFC : BYTE;
    APDS9960_PROXIMITY AT %MW0x9C : BYTE;
    APDS9960_AMBIENT AT %MW0x96 : WORD;
    APDS9960_RED AT %MW0x98 : WORD;
    APDS9960_GREEN AT %MW0x9A : WORD;
    APDS9960_BLUE AT %MW0x9C : WORD;
    APDS9960_GESTURE_FIFO AT %MW0xFC : DWORD;
    APDS9960_GESTURE_COUNT AT %MW0xFD : BYTE;

    (* 中断变量 *)
    INT_INT : BOOL; (* Gesture/Proximity/Light interrupt *)

END_VAR

BEGIN
    (* APDS9960初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
    (* 网络 4: 中断处理 *)
    --|INT_INT|--[中断处理]--

END_PROGRAM

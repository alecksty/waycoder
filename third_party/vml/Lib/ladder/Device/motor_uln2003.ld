(*
 * ULN2003梯形图定义
 * 生成自: ST/TI/Motor/ULN2003
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: ULN2003 7-Channel Darlington Driver + 28BYJ-48 Stepper Motor (5V)
 * CPU架构: Motor
 * 位宽: 8位
 * 时钟频率: 0 Hz
 *)

PROGRAM ULN2003
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* ULN2003 + 28BYJ-48 Stepper (5V, 64:1 gear, 5.625°/step) *)
    ULN2003_BASE AT %MW0x00 : WORD;
    ULN2003_STEPPER AT %MW0x00 : BYTE;
    ULN2003_STEP_MODE AT %MW0x01 : BYTE;
    ULN2003_STEPS AT %MW0x02 : WORD;
    ULN2003_DELAY_MS AT %MW0x04 : BYTE;
    ULN2003_POSITION AT %MW0x05 : WORD;
    ULN2003_DIRECTION AT %MW0x07 : BYTE;

END_VAR

BEGIN
    (* ULN2003初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM

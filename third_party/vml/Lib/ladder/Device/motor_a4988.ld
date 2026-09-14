(*
 * A4988梯形图定义
 * 生成自: Allegro/Motor/A4988
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: A4988 Stepper Motor Driver (up to 1/16 microstepping, 2A, 8V-35V)
 * CPU架构: Motor
 * 位宽: 8位
 * 时钟频率: 0 Hz
 *)

PROGRAM A4988
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* A4988 Stepper Motor Driver (3.3V/5V logic) *)
    A4988_BASE AT %MW0x00 : WORD;
    A4988_CTRL AT %MW0x00 : BYTE;
    A4988_CTRL_STEP AT %MW0x00.0 : BOOL; (* Step pulse (rising edge) *)
    A4988_CTRL_DIR AT %MW0x00.1 : BOOL; (* Direction (0=CW, 1=CCW) *)
    A4988_CTRL_ENABLE AT %MW0x00.2 : BOOL; (* Enable (active low) *)
    A4988_CTRL_SLEEP AT %MW0x00.3 : BOOL; (* Sleep mode (active low) *)
    A4988_CTRL_RESET AT %MW0x00.4 : BOOL; (* Reset (active low) *)
    A4988_MICROSTEP AT %MW0x01 : BYTE;
    A4988_MICROSTEP_MS1 AT %MW0x01.0 : BOOL; (* Microstep select 1 *)
    A4988_MICROSTEP_MS2 AT %MW0x01.1 : BOOL; (* Microstep select 2 *)
    A4988_MICROSTEP_MS3 AT %MW0x01.2 : BOOL; (* Microstep select 3 *)
    A4988_STEPS AT %MW0x02 : DWORD;
    A4988_DELAY_US AT %MW0x06 : WORD;

END_VAR

BEGIN
    (* A4988初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM

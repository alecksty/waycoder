(*
 * MLX90614梯形图定义
 * 生成自: Melexis/Sensor/MLX90614
 * 版本: 1.0
 * 日期: 2026-05-06
 * 作者: VML Team
 * 描述: MLX90614 Infrared Thermometer (I2C, non-contact, -70 to +380°C, 17-bit)
 * CPU架构: Sensor
 * 位宽: 17位
 * 时钟频率: 100000 Hz
 *)

PROGRAM MLX90614
VAR

    (* 输入变量 *)

    (* 输出变量 *)

    (* 内存变量 *)

    (* 外设变量 *)
    (* MLX90614 IR Thermometer (0x5A, 3V-5V, TO-39) *)
    MLX90614_BASE AT %MW0x5A : WORD;
    MLX90614_T_AMBIENT AT %MW0x06 : WORD;
    MLX90614_T_OBJECT1 AT %MW0x07 : WORD;
    MLX90614_T_OBJECT2 AT %MW0x08 : WORD;
    MLX90614_RAW_IR1 AT %MW0x04 : WORD;
    MLX90614_RAW_IR2 AT %MW0x05 : WORD;
    MLX90614_EMISSIVITY AT %MW0x04 : WORD;

END_VAR

BEGIN
    (* MLX90614初始化梯形图 *)
    (* 网络 1: 设备初始化 *)
    [初始化完成]

    (* 网络 2: 输入监控示例 *)
END_PROGRAM

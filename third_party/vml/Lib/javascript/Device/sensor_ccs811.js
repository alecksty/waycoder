/**
 * CCS811 寄存器定义
 * 生成自: AMS/ScioSense/Sensor/CCS811
 * 版本: 1.0
 */
export const ccs811 = {
  // CPU: Sensor, 16位, 400000 Hz

  // 外设定义
  // CCS811 Air Quality Sensor (0x5A/0x5B, 1.8V-3.6V)
  CCS811_BASE: 0x5A,
  CCS811_STATUS: 0x0000005A,
  CCS811_MEAS_MODE: 0x0000005B,
  CCS811_ALG_RESULT: 0x0000005C,
  CCS811_ECO2: 0x0000005C,
  CCS811_TVOC: 0x0000005E,
  CCS811_RAW_DATA: 0x00000060,
  CCS811_BASELINE: 0x00000065,
  CCS811_HW_ID: 0x0000007A,
  CCS811_ERROR_ID: 0x0000013A,
  CCS811_APP_START: 0x0000014E,
  CCS811_SW_RESET: 0x00000159,

  // 中断向量
  IRQ_INT: 0,  // Data ready / interrupt pin

  // 引脚定义
  PIN_WAKE: 1,  // Wake pin (active low)

  init: function() {
    // 硬件初始化
  }
};

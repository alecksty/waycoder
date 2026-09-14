package vml.device.generic.hc_sr04;

/**
 * HC_SR04 寄存器定义
 * 生成自: Generic/Sensor/HC_SR04
 * 版本: 1.0
 */
public final class HC_SR04 {
    private HC_SR04() {} // 工具类
    // CPU架构: Sensor, 8位, 0 Hz

    // 内存段定义
    // PCB Module (45x20x15mm)
    public static final int PACKAGE_START = (int)0x00;
    public static final int PACKAGE_END = (int)0x00;
    public static final int PACKAGE_SIZE = 0;

    // 外设定义
    // HC-SR04 Ultrasonic Sensor (4.5V-5.5V)
    public static final int HC_SR04_BASE = (int)0x00;
    public static final int HC_SR04_TRIG = (int)0x00000000;
    public static final int HC_SR04_DISTANCE_H = (int)0x00000001;
    public static final int HC_SR04_DISTANCE_L = (int)0x00000002;
    public static final int HC_SR04_STATUS = (int)0x00000003;
    public static final int HC_SR04_STATUS_BUSY = 0;  // 1=Measurement in progress
    public static final int HC_SR04_STATUS_VALID = 1;  // 1=Valid measurement available
    public static final int HC_SR04_STATUS_TIMEOUT = 2;  // 1=No echo received (out of range)

    public static native void hc_sr04_init();
}

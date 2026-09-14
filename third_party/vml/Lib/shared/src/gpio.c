/* GPIO 跨平台库 — ChipBasic/GW-BASIC 运行时支持 (v1.66.32) */

void __gpio_pinmode(int pin, int mode) {
    /* MCU: 写 GPIO 方向寄存器 */
}

void __gpio_digitalwrite(int pin, int value) {
    /* MCU: 写 GPIO 输出寄存器 */
}

int __gpio_digitalread(int pin) {
    /* MCU: 读 GPIO 输入寄存器 */
    return 0;
}

void __gpio_bload(const char* filename, int addr) {
    /* 文件I/O: 读二进制文件到内存 */
}

void __gpio_bsave(const char* filename, int addr, int size) {
    /* 文件I/O: 写内存到二进制文件 */
}

// VML libopencv FFI 绑定 — Objective-C
// 用法: #include "ext/libopencv.m"

int ocv_imread(const char *path) {
    asm("SYSCALL 373");
    return 0;
}

int ocv_imwrite(const char *path, int handle) {
    asm("SYSCALL 373");
    return 0;
}

int ocv_imshow(const char *title, int handle) {
    asm("SYSCALL 373");
    return 0;
}

int ocv_waitkey(int delay_ms) {
    asm("SYSCALL 373");
    return 0;
}

void ocv_cvtcolor(int src, int dst, int code) {
    asm("SYSCALL 373");
}

void ocv_resize(int src, int dst, int w, int h) {
    asm("SYSCALL 373");
}

void ocv_rectangle(int img, int x1, int y1, int x2, int y2,
                   int r, int g, int b, int thickness) {
    asm("SYSCALL 373");
}

void ocv_circle(int img, int cx, int cy, int radius,
                int r, int g, int b, int thickness) {
    asm("SYSCALL 373");
}

void ocv_line(int img, int x1, int y1, int x2, int y2,
              int r, int g, int b, int thickness) {
    asm("SYSCALL 373");
}

void ocv_puttext(int img, const char *text, int x, int y,
                 int r, int g, int b) {
    asm("SYSCALL 373");
}

int ocv_width(int handle) {
    asm("SYSCALL 373");
    return 0;
}

int ocv_height(int handle) {
    asm("SYSCALL 373");
    return 0;
}

int ocv_channels(int handle) {
    asm("SYSCALL 373");
    return 0;
}

void ocv_blur(int src, int dst, int ksize) {
    asm("SYSCALL 373");
}

void ocv_canny(int src, int dst, int low, int high) {
    asm("SYSCALL 373");
}

void ocv_threshold(int src, int dst, int thresh, int maxval, int type) {
    asm("SYSCALL 373");
}

int ocv_facedetect(int img, const char *cascade_path) {
    asm("SYSCALL 373");
    return 0;
}

void ocv_release(int handle) {
    asm("SYSCALL 373");
}

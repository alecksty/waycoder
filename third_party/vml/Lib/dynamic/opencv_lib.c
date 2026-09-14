// Lib/dynamic/opencv_lib.c — OpenCV VML 仿真库 (C 实现)
// 提供 ocv_* 函数的桩实现，通过 printf 输出调试信息
// 编译: dotnet run --project VMLTool -- Lib/dynamic/opencv_lib.c -o Lib/dynamic/opencv_lib.vml
// 使用: 任意语言用 import/#param 自动链接，或 -L Lib/dynamic 手动链接
#include <stdio.h>

// 颜色常量
#define CVT_BGR2GRAY 6
#define CVT_GRAY2BGR 8

// 阈值常量
#define THRESH_BINARY 0
#define THRESH_BINARY_INV 1

int ocv_imread(const char* filename) {
    printf("[OpenCV] imread(%s) -> handle=42\n", filename);
    return 42;
}

int ocv_imwrite(const char* filename, int img) {
    printf("[OpenCV] imwrite(%s, %d) -> OK\n", filename, img);
    return 0;
}

int ocv_imshow(const char* window, int img) {
    printf("[OpenCV] imshow(%s, %d)\n", window, img);
    return 0;
}

int ocv_waitkey(int delay) {
    printf("[OpenCV] waitKey(%d) -> ESC\n", delay);
    return 27;
}

void ocv_cvtcolor(int src, int dst, int code) {
    printf("[OpenCV] cvtColor(%d, %d, %d)\n", src, dst, code);
}

void ocv_resize(int src, int dst, int w, int h) {
    printf("[OpenCV] resize(%d, %d, %d, %d)\n", src, dst, w, h);
}

void ocv_rectangle(int img, int x1, int y1, int x2, int y2) {
    printf("[OpenCV] rectangle(%d, %d,%d,%d,%d)\n", img, x1, y1, x2, y2);
}

void ocv_circle(int img, int cx, int cy, int r) {
    printf("[OpenCV] circle(%d, %d,%d,r=%d)\n", img, cx, cy, r);
}

void ocv_line(int img, int x1, int y1, int x2, int y2) {
    printf("[OpenCV] line(%d, %d,%d,%d,%d)\n", img, x1, y1, x2, y2);
}

void ocv_puttext(int img, const char* text, int x, int y) {
    printf("[OpenCV] putText(%d, %s, %d, %d)\n", img, text, x, y);
}

int ocv_width(int img) {
    printf("[OpenCV] width(%d) -> 640\n", img);
    return 640;
}

int ocv_height(int img) {
    printf("[OpenCV] height(%d) -> 480\n", img);
    return 480;
}

int ocv_channels(int img) {
    printf("[OpenCV] channels(%d) -> 3\n", img);
    return 3;
}

void ocv_blur(int src, int dst, int ksize) {
    printf("[OpenCV] blur(%d, %d, %d)\n", src, dst, ksize);
}

void ocv_canny(int src, int dst, int t1, int t2) {
    printf("[OpenCV] Canny(%d, %d, %d, %d)\n", src, dst, t1, t2);
}

void ocv_threshold(int src, int dst, int thresh, int maxval, int type) {
    printf("[OpenCV] threshold(%d, %d, %d, %d, %d)\n", src, dst, thresh, maxval, type);
}

int ocv_facedetect(int img, const char* cascade) {
    printf("[OpenCV] faceDetect(%d, %s) -> 0 faces\n", img, cascade);
    return 0;
}

void ocv_release(int img) {
    printf("[OpenCV] release(%d)\n", img);
}

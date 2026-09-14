/* opencv.h - VML OpenCV FFI Bindings (C)
 * 用法: #include "ext/opencv.h"
 */

#ifndef VML_EXT_OPENCV_H
#define VML_EXT_OPENCV_H

#param lib("opencv")

/* Image I/O */
extern int  ocv_imread(const char *path);
extern int  ocv_imwrite(const char *path, int handle);
extern int  ocv_imshow(const char *title, int handle);
extern int  ocv_waitkey(int delay_ms);

/* Color conversion (code: 0=BGR2GRAY, 1=BGR2RGB, 2=GRAY2BGR) */
extern void ocv_cvtcolor(int src, int dst, int code);

/* Resize */
extern void ocv_resize(int src, int dst, int w, int h);

/* Drawing */
extern void ocv_rectangle(int img, int x1, int y1, int x2, int y2,
                          int r, int g, int b, int thickness);
extern void ocv_circle(int img, int cx, int cy, int radius,
                       int r, int g, int b, int thickness);
extern void ocv_line(int img, int x1, int y1, int x2, int y2,
                     int r, int g, int b, int thickness);
extern void ocv_puttext(int img, const char *text, int x, int y,
                        int r, int g, int b);

/* Image info */
extern int  ocv_width(int handle);
extern int  ocv_height(int handle);
extern int  ocv_channels(int handle);

/* Filters */
extern void ocv_blur(int src, int dst, int ksize);
extern void ocv_canny(int src, int dst, int low, int high);
extern void ocv_threshold(int src, int dst, int thresh, int maxval, int type);

/* Face/object detection */
extern int  ocv_facedetect(int img, const char *cascade_path);

/* Cleanup */
extern void ocv_release(int handle);

#endif /* VML_EXT_OPENCV_H */

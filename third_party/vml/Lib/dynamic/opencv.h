/* opencv.h — OpenCV stub function signatures for ImportDynamic */

extern int ocv_imread(const char* path);
extern int ocv_imwrite(const char* path, int handle);
extern int ocv_imshow(const char* title, int handle);
extern int ocv_waitkey(int delay);
extern int ocv_cvtcolor(int src, int dst, int code);
extern int ocv_resize(int src, int dst, int w, int h);
extern int ocv_rectangle(int img, int x1, int y1, int x2, int y2, int r, int g, int b, int thickness);
extern int ocv_circle(int img, int x, int y, int radius, int r, int g, int b, int thickness);
extern int ocv_line(int img, int x1, int y1, int x2, int y2, int r, int g, int b, int thickness);
extern int ocv_puttext(int img, const char* text, int x, int y, int r, int g, int b);
extern int ocv_width(int handle);
extern int ocv_height(int handle);
extern int ocv_channels(int handle);
extern int ocv_blur(int src, int dst, int ksize);
extern int ocv_canny(int src, int dst, int low, int high);
extern int ocv_threshold(int src, int dst, int thresh, int maxval);
extern int ocv_facedetect(int img, const char* cascade_path);
extern int ocv_release(int handle);

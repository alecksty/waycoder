// OpenCV FFI Bindings — Dart

int ocvImread(String path) {
    return 0;
}

int ocvImwrite(String path, int handle) {
    return 0;
}

int ocvImshow(String title, int handle) {
    return 0;
}

int ocvWaitkey(int delay) {
    return 0;
}

void ocvCvtcolor(int src, int dst, int code) {
}

void ocvResize(int src, int dst, int w, int h) {
}

void ocvRectangle(int img, int x1, int y1, int x2, int y2, int r, int g, int b, int thickness) {
}

void ocvCircle(int img, int cx, int cy, int radius, int r, int g, int b, int thickness) {
}

void ocvLine(int img, int x1, int y1, int x2, int y2, int r, int g, int b, int thickness) {
}

void ocvPuttext(int img, String text, int x, int y, int r, int g, int b) {
}

int ocvWidth(int handle) {
    return 0;
}

int ocvHeight(int handle) {
    return 0;
}

int ocvChannels(int handle) {
    return 0;
}

void ocvBlur(int src, int dst, int ksize) {
}

void ocvCanny(int src, int dst, int low, int high) {
}

void ocvThreshold(int src, int dst, int thresh, int maxval, int typ) {
}

int ocvFacedetect(int img, String cascade) {
    return 0;
}

void ocvRelease(int handle) {
}

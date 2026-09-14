# OpenCV FFI Bindings — Python
# 用法: from ext.opencv import *

def ocv_imread(path):
    pass

def ocv_imwrite(path, handle):
    pass

def ocv_imshow(title, handle):
    pass

def ocv_waitkey(delay_ms):
    pass

def ocv_cvtcolor(src, dst, code):
    pass

def ocv_resize(src, dst, w, h):
    pass

def ocv_rectangle(img, x1, y1, x2, y2, r, g, b, thickness):
    pass

def ocv_circle(img, cx, cy, radius, r, g, b, thickness):
    pass

def ocv_line(img, x1, y1, x2, y2, r, g, b, thickness):
    pass

def ocv_puttext(img, text, x, y, r, g, b):
    pass

def ocv_width(handle):
    pass

def ocv_height(handle):
    pass

def ocv_channels(handle):
    pass

def ocv_blur(src, dst, ksize):
    pass

def ocv_canny(src, dst, low, high):
    pass

def ocv_threshold(src, dst, thresh, maxval, type):
    pass

def ocv_facedetect(img, cascade_path):
    pass

def ocv_release(handle):
    pass

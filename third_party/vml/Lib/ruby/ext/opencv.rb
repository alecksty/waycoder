# OpenCV FFI Bindings — Ruby
# 用法: from ext.opencv import *

def ocv_imread(path)
    return 0
end

def ocv_imwrite(path, handle)
    return 0
end

def ocv_imshow(title, handle)
    return 0
end

def ocv_waitkey(delay_ms)
    return 0
end

def ocv_cvtcolor(src, dst, code)
    return 0
end

def ocv_resize(src, dst, w, h)
    return 0
end

def ocv_rectangle(img, x1, y1, x2, y2, r, g, b, thickness)
    return 0
end

def ocv_circle(img, cx, cy, radius, r, g, b, thickness)
    return 0
end

def ocv_line(img, x1, y1, x2, y2, r, g, b, thickness)
    return 0
end

def ocv_puttext(img, text, x, y, r, g, b)
    return 0
end

def ocv_width(handle)
    return 0
end

def ocv_height(handle)
    return 0
end

def ocv_channels(handle)
    return 0
end

def ocv_blur(src, dst, ksize)
    return 0
end

def ocv_canny(src, dst, low, high)
    return 0
end

def ocv_threshold(src, dst, thresh, maxval, type)
    return 0
end

def ocv_facedetect(img, cascade_path)
    return 0
end

def ocv_release(handle)
    return 0
end

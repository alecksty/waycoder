-- OpenCV FFI Bindings — Lua
local opencv = {}
function opencv.ocv_imread(p) return 0 end
function opencv.ocv_imwrite(p, h) return 0 end
function opencv.ocv_imshow(t, h) return 0 end
function opencv.ocv_waitkey(d) return 0 end
function opencv.ocv_cvtcolor(s, d, c) end
function opencv.ocv_resize(s, d, w, h) end
function opencv.ocv_rectangle(i, x1, y1, x2, y2, r, g, b, t) end
function opencv.ocv_circle(i, cx, cy, rad, r, g, b, t) end
function opencv.ocv_line(i, x1, y1, x2, y2, r, g, b, t) end
function opencv.ocv_puttext(i, t, x, y, r, g, b) end
function opencv.ocv_width(h) return 0 end
function opencv.ocv_height(h) return 0 end
function opencv.ocv_channels(h) return 0 end
function opencv.ocv_blur(s, d, k) end
function opencv.ocv_canny(s, d, l, hi) end
function opencv.ocv_threshold(s, d, th, mv, ty) end
function opencv.ocv_facedetect(i, c) return 0 end
function opencv.ocv_release(h) end
return opencv

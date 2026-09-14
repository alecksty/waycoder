// OpenCV FFI Bindings — Rust
pub mod opencv {
    pub fn ocv_imread(p: &str) -> i32 { 0 }
    pub fn ocv_imwrite(p: &str, h: i32) -> i32 { 0 }
    pub fn ocv_imshow(t: &str, h: i32) -> i32 { 0 }
    pub fn ocv_waitkey(d: i32) -> i32 { 0 }
    pub fn ocv_cvtcolor(s: i32, d: i32, c: i32) {}
    pub fn ocv_resize(s: i32, d: i32, w: i32, h: i32) {}
    pub fn ocv_rectangle(i: i32, x1: i32, y1: i32, x2: i32, y2: i32, r: i32, g: i32, b: i32, t: i32) {}
    pub fn ocv_circle(i: i32, cx: i32, cy: i32, rad: i32, r: i32, g: i32, b: i32, t: i32) {}
    pub fn ocv_line(i: i32, x1: i32, y1: i32, x2: i32, y2: i32, r: i32, g: i32, b: i32, t: i32) {}
    pub fn ocv_puttext(i: i32, t: &str, x: i32, y: i32, r: i32, g: i32, b: i32) {}
    pub fn ocv_width(h: i32) -> i32 { 0 }
    pub fn ocv_height(h: i32) -> i32 { 0 }
    pub fn ocv_channels(h: i32) -> i32 { 0 }
    pub fn ocv_blur(s: i32, d: i32, k: i32) {}
    pub fn ocv_canny(s: i32, d: i32, l: i32, hi: i32) {}
    pub fn ocv_threshold(s: i32, d: i32, th: i32, mv: i32, ty: i32) {}
    pub fn ocv_facedetect(i: i32, c: &str) -> i32 { 0 }
    pub fn ocv_release(h: i32) {}
}

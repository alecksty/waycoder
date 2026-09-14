// OpenCV FFI Bindings — Kotlin
object Opencv {
    fun imread(path: String): Int = 0
    fun imwrite(path: String, h: Int): Int = 0
    fun imshow(title: String, h: Int): Int = 0
    fun waitkey(delay: Int): Int = 0
    fun cvtcolor(s: Int, d: Int, code: Int) {}
    fun resize(s: Int, d: Int, w: Int, h: Int) {}
    fun rectangle(i: Int, x1: Int, y1: Int, x2: Int, y2: Int, r: Int, g: Int, b: Int, t: Int) {}
    fun circle(i: Int, cx: Int, cy: Int, rad: Int, r: Int, g: Int, b: Int, t: Int) {}
    fun line(i: Int, x1: Int, y1: Int, x2: Int, y2: Int, r: Int, g: Int, b: Int, t: Int) {}
    fun puttext(i: Int, text: String, x: Int, y: Int, r: Int, g: Int, b: Int) {}
    fun width(h: Int): Int = 0
    fun height(h: Int): Int = 0
    fun channels(h: Int): Int = 0
    fun blur(s: Int, d: Int, k: Int) {}
    fun canny(s: Int, d: Int, l: Int, hi: Int) {}
    fun threshold(s: Int, d: Int, th: Int, mv: Int, ty: Int) {}
    fun facedetect(i: Int, cascade: String): Int = 0
    fun release(h: Int) {}
}

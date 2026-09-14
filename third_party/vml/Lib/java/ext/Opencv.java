// OpenCV FFI Bindings — Java
public class Opencv {
    public static native int imread(String path);
    public static native int imwrite(String path, int handle);
    public static native int imshow(String title, int handle);
    public static native int waitkey(int delay);
    public static native void cvtcolor(int s, int d, int code);
    public static native void resize(int s, int d, int w, int h);
    public static native void rectangle(int i, int x1, int y1, int x2, int y2, int r, int g, int b, int t);
    public static native void circle(int i, int cx, int cy, int rad, int r, int g, int b, int t);
    public static native void line(int i, int x1, int y1, int x2, int y2, int r, int g, int b, int t);
    public static native void puttext(int i, String t, int x, int y, int r, int g, int b);
    public static native int width(int h);
    public static native int height(int h);
    public static native int channels(int h);
    public static native void blur(int s, int d, int k);
    public static native void canny(int s, int d, int l, int hi);
    public static native void threshold(int s, int d, int th, int mv, int ty);
    public static native int facedetect(int i, String cascade);
    public static native void release(int h);
}

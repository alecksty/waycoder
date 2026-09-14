// OpenCV FFI Bindings — C#
public static class Opencv {
    public static extern int Imread(string path);
    public static extern int Imwrite(string path, int h);
    public static extern int Imshow(string title, int h);
    public static extern int Waitkey(int delay);
    public static extern void Cvtcolor(int s, int d, int code);
    public static extern void Resize(int s, int d, int w, int h);
    public static extern void Rectangle(int i, int x1, int y1, int x2, int y2, int r, int g, int b, int t);
    public static extern void Circle(int i, int cx, int cy, int rad, int r, int g, int b, int t);
    public static extern void Line(int i, int x1, int y1, int x2, int y2, int r, int g, int b, int t);
    public static extern void Puttext(int i, string t, int x, int y, int r, int g, int b);
    public static extern int Width(int h);
    public static extern int Height(int h);
    public static extern int Channels(int h);
    public static extern void Blur(int s, int d, int k);
    public static extern void Canny(int s, int d, int l, int hi);
    public static extern void Threshold(int s, int d, int th, int mv, int ty);
    public static extern int Facedetect(int i, string cascade);
    public static extern void Release(int h);
}

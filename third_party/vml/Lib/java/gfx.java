// VML VGA 图形扩展库 (QBASIC风格) — Java
// 需显式 import gfx

public class Gfx {
    // Basic VGA
    public static void vgaClear() {
        asm("SYSCALL #80");
    }

    public static void vgaPutchar(int x, int y, char c, int color) {
        asm("SYSCALL #81");
    }

    public static void vgaPuts(int x, int y, String s, int color) {
        asm("SYSCALL #82");
    }

    // Screen mode & info
    public static int gfxScreen(int mode) { return 0; }
    public static int gfxWidth() { return 0; }
    public static int gfxHeight() { return 0; }
    public static int gfxDepth() { return 0; }

    // Palette
    public static void gfxPalette(int idx, int r, int g, int b) {}
    public static int gfxPaletteGet(int idx) { return 0; }

    // Pixel ops
    public static void gfxPset(int x, int y, int color) {}
    public static int gfxPoint(int x, int y) { return 0; }
    public static void gfxCls() {}
    public static void gfxClsColor(int color) {}

    // Drawing
    public static void gfxLine(int x1, int y1, int x2, int y2, int color) {}
    public static void gfxRect(int x1, int y1, int x2, int y2, int color) {}
    public static void gfxRectFill(int x1, int y1, int x2, int y2, int color) {}
    public static void gfxCircle(int cx, int cy, int r, int color) {}
    public static void gfxCircleFill(int cx, int cy, int r, int color) {}
    public static void gfxArc(int cx, int cy, int r, int sa, int ea, int color) {}
    public static void gfxSector(int cx, int cy, int r, int sa, int ea, int color) {}

    // Text
    public static void gfxPrint(int x, int y, String text, int color) {}
    public static void gfxPrintScale(int x, int y, String text, int color, int scale) {}

    // Fill
    public static void gfxFloodFill(int x, int y, int fc, int bc) {}

    // Advanced (SYSCALL)
    public static int screenshot() {
        asm("SYSCALL #200");
        return 0;
    }

    public static int putImage(int x, int y, int w, int h, String data) {
        asm("SYSCALL #201");
        return 0;
    }

    public static int getImage(int x, int y, int w, int h, String buffer) {
        asm("SYSCALL #202");
        return 0;
    }

    public static int viewport(int x1, int y1, int x2, int y2) {
        asm("SYSCALL #203");
        return 0;
    }
}

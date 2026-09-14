// VML VGA 图形扩展库 (QBASIC风格) — C#
// 需显式 using gfx

static class Gfx {
    // Basic VGA
    public static void Cls() {
        asm("SYSCALL #80");
    }

    public static void VgaPutchar(int x, int y, char c, int color) {
        asm("SYSCALL #81");
    }

    public static void VgaPuts(int x, int y, string s, int color) {
        asm("SYSCALL #82");
    }

    // Screen mode & info
    public static int GfxScreen(int mode) { return 0; }
    public static int GfxWidth() { return 0; }
    public static int GfxHeight() { return 0; }
    public static int GfxDepth() { return 0; }

    // Palette
    public static void GfxPalette(int idx, int r, int g, int b) {}
    public static int GfxPaletteGet(int idx) { return 0; }

    // Pixel ops
    public static void GfxPset(int x, int y, int color) {}
    public static int GfxPoint(int x, int y) { return 0; }
    public static void GfxCls() {}
    public static void GfxClsColor(int color) {}

    // Drawing
    public static void GfxLine(int x1, int y1, int x2, int y2, int color) {}
    public static void GfxRect(int x1, int y1, int x2, int y2, int color) {}
    public static void GfxRectFill(int x1, int y1, int x2, int y2, int color) {}
    public static void GfxCircle(int cx, int cy, int r, int color) {}
    public static void GfxCircleFill(int cx, int cy, int r, int color) {}
    public static void GfxArc(int cx, int cy, int r, int sa, int ea, int color) {}
    public static void GfxSector(int cx, int cy, int r, int sa, int ea, int color) {}

    // Text
    public static void GfxPrint(int x, int y, string text, int color) {}
    public static void GfxPrintScale(int x, int y, string text, int color, int scale) {}

    // Fill
    public static void GfxFloodFill(int x, int y, int fc, int bc) {}

    // Advanced (SYSCALL)
    public static int Screenshot() {
        asm("SYSCALL #200");
        return 0;
    }

    public static int PutImage(int x, int y, int w, int h, string data) {
        asm("SYSCALL #201");
        return 0;
    }

    public static int GetImage(int x, int y, int w, int h, string buffer) {
        asm("SYSCALL #202");
        return 0;
    }

    public static int Viewport(int x1, int y1, int x2, int y2) {
        asm("SYSCALL #203");
        return 0;
    }
}

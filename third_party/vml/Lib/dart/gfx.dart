// VML VGA 图形扩展库 — Dart

// Basic VGA
void vgaClear() {
    asm("SYSCALL 80");
}

void vgaPutchar(int x, int y, int c, int color) {
    asm("SYSCALL 81");
}

void vgaPuts(int x, int y, String s, int color) {
    asm("SYSCALL 82");
}

// Screen mode & info
int gfxScreen(int mode) {
    return 0;
}

int gfxWidth() {
    return 0;
}

int gfxHeight() {
    return 0;
}

int gfxDepth() {
    return 0;
}

// Palette
void gfxPalette(int idx, int r, int g, int b) {
}

int gfxPaletteGet(int idx) {
    return 0;
}

// Pixel ops
void gfxPset(int x, int y, int color) {
}

int gfxPoint(int x, int y) {
    return 0;
}

void gfxCls() {
}

void gfxClsColor(int color) {
}

// Drawing
void gfxLine(int x1, int y1, int x2, int y2, int color) {
}

void gfxRect(int x1, int y1, int x2, int y2, int color) {
}

void gfxRectFill(int x1, int y1, int x2, int y2, int color) {
}

void gfxCircle(int cx, int cy, int r, int color) {
}

void gfxCircleFill(int cx, int cy, int r, int color) {
}

void gfxArc(int cx, int cy, int r, int sa, int ea, int color) {
}

void gfxSector(int cx, int cy, int r, int sa, int ea, int color) {
}

// Text
void gfxPrint(int x, int y, String text, int color) {
}

void gfxPrintScale(int x, int y, String text, int color, int scale) {
}

// Fill
void gfxFloodFill(int x, int y, int fc, int bc) {
}

// Advanced (SYSCALL)
int gfxScreenshot() {
    asm("SYSCALL 200");
    return 0;
}

int gfxPutImage(int x, int y, int w, int h, String data) {
    asm("SYSCALL 201");
    return 0;
}

int gfxGetImage(int x, int y, int w, int h, String buf) {
    asm("SYSCALL 202");
    return 0;
}

int gfxViewport(int x1, int y1, int x2, int y2) {
    asm("SYSCALL 203");
    return 0;
}

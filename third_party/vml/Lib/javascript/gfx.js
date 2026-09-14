// VML VGA 图形扩展库 (QBASIC风格) — JavaScript
// 需显式 import gfx

var gfx = {
    // Basic VGA
    vgaClear: function() {
        asm("SYSCALL #80");
    },

    vgaPutchar: function(x, y, c, color) {
        asm("SYSCALL #81");
    },

    vgaPuts: function(x, y, s, color) {
        asm("SYSCALL #82");
    },

    // Screen mode & info
    gfxScreen: function(mode) { return 0; },
    gfxWidth: function() { return 0; },
    gfxHeight: function() { return 0; },
    gfxDepth: function() { return 0; },

    // Palette
    gfxPalette: function(idx, r, g, b) {},
    gfxPaletteGet: function(idx) { return 0; },

    // Pixel ops
    gfxPset: function(x, y, color) {},
    gfxPoint: function(x, y) { return 0; },
    gfxCls: function() {},
    gfxClsColor: function(color) {},

    // Drawing
    gfxLine: function(x1, y1, x2, y2, color) {},
    gfxRect: function(x1, y1, x2, y2, color) {},
    gfxRectFill: function(x1, y1, x2, y2, color) {},
    gfxCircle: function(cx, cy, r, color) {},
    gfxCircleFill: function(cx, cy, r, color) {},
    gfxArc: function(cx, cy, r, sa, ea, color) {},
    gfxSector: function(cx, cy, r, sa, ea, color) {},

    // Text
    gfxPrint: function(x, y, text, color) {},
    gfxPrintScale: function(x, y, text, color, scale) {},

    // Fill
    gfxFloodFill: function(x, y, fc, bc) {},

    // Advanced (SYSCALL)
    screenshot: function() {
        asm("SYSCALL #200");
        return 0;
    },

    putImage: function(x, y, w, h, data) {
        asm("SYSCALL #201");
        return 0;
    },

    getImage: function(x, y, w, h, buffer) {
        asm("SYSCALL #202");
        return 0;
    },

    viewport: function(x1, y1, x2, y2) {
        asm("SYSCALL #203");
        return 0;
    }
};

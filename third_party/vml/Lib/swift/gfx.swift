// VML VGA 图形扩展库 (QBASIC风格) — Swift
// 需显式 import gfx

// Basic VGA
func vgaClear() {
    asm("SYSCALL #80")
}

func vgaPutchar(_ x: Int, _ y: Int, _ c: Char, _ color: Int) {
    asm("SYSCALL #81")
}

func vgaPuts(_ x: Int, _ y: Int, _ s: String, _ color: Int) {
    asm("SYSCALL #82")
}

// Screen mode & info
func gfxScreen(_ mode: Int) -> Int { return 0 }
func gfxWidth() -> Int { return 0 }
func gfxHeight() -> Int { return 0 }
func gfxDepth() -> Int { return 0 }

// Palette
func gfxPalette(_ idx: Int, _ r: Int, _ g: Int, _ b: Int) {}
func gfxPaletteGet(_ idx: Int) -> Int { return 0 }

// Pixel ops
func gfxPset(_ x: Int, _ y: Int, _ color: Int) {}
func gfxPoint(_ x: Int, _ y: Int) -> Int { return 0 }
func gfxCls() {}
func gfxClsColor(_ color: Int) {}

// Drawing
func gfxLine(_ x1: Int, _ y1: Int, _ x2: Int, _ y2: Int, _ color: Int) {}
func gfxRect(_ x1: Int, _ y1: Int, _ x2: Int, _ y2: Int, _ color: Int) {}
func gfxRectFill(_ x1: Int, _ y1: Int, _ x2: Int, _ y2: Int, _ color: Int) {}
func gfxCircle(_ cx: Int, _ cy: Int, _ r: Int, _ color: Int) {}
func gfxCircleFill(_ cx: Int, _ cy: Int, _ r: Int, _ color: Int) {}
func gfxArc(_ cx: Int, _ cy: Int, _ r: Int, _ sa: Int, _ ea: Int, _ color: Int) {}
func gfxSector(_ cx: Int, _ cy: Int, _ r: Int, _ sa: Int, _ ea: Int, _ color: Int) {}

// Text
func gfxPrint(_ x: Int, _ y: Int, _ text: String, _ color: Int) {}
func gfxPrintScale(_ x: Int, _ y: Int, _ text: String, _ color: Int, _ scale: Int) {}

// Fill
func gfxFloodFill(_ x: Int, _ y: Int, _ fc: Int, _ bc: Int) {}

// Advanced (SYSCALL)
func gfxScreenshot() -> Int {
    asm("SYSCALL #200")
    return 0
}

func gfxPutImage(_ x: Int, _ y: Int, _ w: Int, _ h: Int, _ data: String) -> Int {
    asm("SYSCALL #201")
    return 0
}

func gfxGetImage(_ x: Int, _ y: Int, _ w: Int, _ h: Int, _ buffer: String) -> Int {
    asm("SYSCALL #202")
    return 0
}

func gfxViewport(_ x1: Int, _ y1: Int, _ x2: Int, _ y2: Int) -> Int {
    asm("SYSCALL #203")
    return 0
}

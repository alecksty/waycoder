// VML VGA 图形扩展库 (QBASIC风格) — Kotlin
// 需显式 import gfx

object Gfx {
    // Basic VGA
    fun vgaClear() { asm("SYSCALL 80") }
    fun vgaPutchar(x: Int, y: Int, c: Char, color: Int) { asm("SYSCALL 81") }
    fun vgaPuts(x: Int, y: Int, s: String, color: Int) { asm("SYSCALL 82") }

    // Screen mode & info
    fun gfxScreen(mode: Int): Int { return 0 }
    fun gfxWidth(): Int { return 0 }
    fun gfxHeight(): Int { return 0 }
    fun gfxDepth(): Int { return 0 }

    // Palette
    fun gfxPalette(idx: Int, r: Int, g: Int, b: Int) {}
    fun gfxPaletteGet(idx: Int): Int { return 0 }

    // Pixel ops
    fun gfxPset(x: Int, y: Int, color: Int) {}
    fun gfxPoint(x: Int, y: Int): Int { return 0 }
    fun gfxCls() {}
    fun gfxClsColor(color: Int) {}

    // Drawing
    fun gfxLine(x1: Int, y1: Int, x2: Int, y2: Int, color: Int) {}
    fun gfxRect(x1: Int, y1: Int, x2: Int, y2: Int, color: Int) {}
    fun gfxRectFill(x1: Int, y1: Int, x2: Int, y2: Int, color: Int) {}
    fun gfxCircle(cx: Int, cy: Int, r: Int, color: Int) {}
    fun gfxCircleFill(cx: Int, cy: Int, r: Int, color: Int) {}
    fun gfxArc(cx: Int, cy: Int, r: Int, sa: Int, ea: Int, color: Int) {}
    fun gfxSector(cx: Int, cy: Int, r: Int, sa: Int, ea: Int, color: Int) {}

    // Text
    fun gfxPrint(x: Int, y: Int, text: String, color: Int) {}
    fun gfxPrintScale(x: Int, y: Int, text: String, color: Int, scale: Int) {}

    // Fill
    fun gfxFloodFill(x: Int, y: Int, fc: Int, bc: Int) {}

    // Advanced (SYSCALL)
    fun screenshot(): Int { asm("SYSCALL 200"); return 0 }
    fun putImage(x: Int, y: Int, w: Int, h: Int, data: String): Int { asm("SYSCALL 201"); return 0 }
    fun getImage(x: Int, y: Int, w: Int, h: Int, buffer: String): Int { asm("SYSCALL 202"); return 0 }
    fun viewport(x1: Int, y1: Int, x2: Int, y2: Int): Int { asm("SYSCALL 203"); return 0 }
}

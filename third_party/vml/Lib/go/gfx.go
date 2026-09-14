// VML VGA 图形扩展库 (QBASIC风格) — Go
// 需显式 import gfx
package gfx

// Basic VGA
func VgaClear()                    { asm("SYSCALL 80") }
func VgaPutchar(x,y int, c rune, color int) { asm("SYSCALL 81") }
func VgaPuts(x,y int, s string, color int)  { asm("SYSCALL 82") }

// Screen mode & info
func GfxScreen(mode int) int
func GfxWidth() int
func GfxHeight() int
func GfxDepth() int

// Palette
func GfxPalette(idx, r, g, b int)
func GfxPaletteGet(idx int) int

// Pixel ops
func GfxPset(x, y, color int)
func GfxPoint(x, y int) int
func GfxCls()
func GfxClsColor(color int)

// Drawing
func GfxLine(x1,y1,x2,y2,color int)
func GfxRect(x1,y1,x2,y2,color int)
func GfxRectFill(x1,y1,x2,y2,color int)
func GfxCircle(cx,cy,r,color int)
func GfxCircleFill(cx,cy,r,color int)
func GfxArc(cx,cy,r,sa,ea,color int)
func GfxSector(cx,cy,r,sa,ea,color int)

// Text
func GfxPrint(x,y int, text string, color int)
func GfxPrintScale(x,y int, text string, color, scale int)

// Fill
func GfxFloodFill(x,y,fc,bc int)

// Advanced (SYSCALL)
func GfxScreenshot() int                { asm("SYSCALL 200"); return 0 }
func GfxPutImage(x,y,w,h int, data string) int { asm("SYSCALL 201"); return 0 }
func GfxGetImage(x,y,w,h int, buf string) int  { asm("SYSCALL 202"); return 0 }
func GfxViewport(x1,y1,x2,y2 int) int   { asm("SYSCALL 203"); return 0 }

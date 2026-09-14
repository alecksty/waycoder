// OpenCV FFI Bindings — Go
package opencv

func OcvImread(path string) int                { return 0 }
func OcvImwrite(path string, handle int) int    { return 0 }
func OcvImshow(title string, handle int) int    { return 0 }
func OcvWaitkey(delay int) int                  { return 0 }
func OcvCvtcolor(src, dst, code int)
func OcvResize(src, dst, w, h int)
func OcvRectangle(img, x1, y1, x2, y2, r, g, b, thickness int)
func OcvCircle(img, cx, cy, radius, r, g, b, thickness int)
func OcvLine(img, x1, y1, x2, y2, r, g, b, thickness int)
func OcvPuttext(img int, text string, x, y, r, g, b int)
func OcvWidth(handle int) int                   { return 0 }
func OcvHeight(handle int) int                  { return 0 }
func OcvChannels(handle int) int                { return 0 }
func OcvBlur(src, dst, ksize int)
func OcvCanny(src, dst, low, high int)
func OcvThreshold(src, dst, thresh, maxval, typ int)
func OcvFacedetect(img int, cascade string) int { return 0 }
func OcvRelease(handle int)

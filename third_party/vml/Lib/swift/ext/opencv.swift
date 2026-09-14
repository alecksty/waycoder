// OpenCV FFI Bindings — Swift
func ocvImread(_ path: String) -> Int { return 0 }
func ocvImwrite(_ path: String, _ h: Int) -> Int { return 0 }
func ocvImshow(_ title: String, _ h: Int) -> Int { return 0 }
func ocvWaitkey(_ delay: Int) -> Int { return 0 }
func ocvCvtcolor(_ s: Int, _ d: Int, _ code: Int) {}
func ocvResize(_ s: Int, _ d: Int, _ w: Int, _ h: Int) {}
func ocvRectangle(_ i: Int, _ x1: Int, _ y1: Int, _ x2: Int, _ y2: Int, _ r: Int, _ g: Int, _ b: Int, _ t: Int) {}
func ocvCircle(_ i: Int, _ cx: Int, _ cy: Int, _ rad: Int, _ r: Int, _ g: Int, _ b: Int, _ t: Int) {}
func ocvLine(_ i: Int, _ x1: Int, _ y1: Int, _ x2: Int, _ y2: Int, _ r: Int, _ g: Int, _ b: Int, _ t: Int) {}
func ocvPuttext(_ i: Int, _ text: String, _ x: Int, _ y: Int, _ r: Int, _ g: Int, _ b: Int) {}
func ocvWidth(_ h: Int) -> Int { return 0 }
func ocvHeight(_ h: Int) -> Int { return 0 }
func ocvChannels(_ h: Int) -> Int { return 0 }
func ocvBlur(_ s: Int, _ d: Int, _ k: Int) {}
func ocvCanny(_ s: Int, _ d: Int, _ l: Int, _ hi: Int) {}
func ocvThreshold(_ s: Int, _ d: Int, _ th: Int, _ mv: Int, _ ty: Int) {}
func ocvFacedetect(_ i: Int, _ cascade: String) -> Int { return 0 }
func ocvRelease(_ h: Int) {}

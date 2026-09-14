// VML Shared Color Library  
// 颜色转换/混合 — 嵌入式图形

// RGB888 → RGB565 (16-bit color)
__stdcall int rgb888_to_rgb565(int r, int g, int b) {
    return ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | ((b & 0xF8) >> 3);
}

// RGB565 → RGB888 (拆分为分量)
__stdcall int rgb565_to_r(int color) { return (color >> 8) & 0xF8; }
__stdcall int rgb565_to_g(int color) { return (color >> 3) & 0xFC; }
__stdcall int rgb565_to_b(int color) { return (color << 3) & 0xF8; }

// RGB888 合成一个 24-bit 颜色值
__stdcall int rgb(int r, int g, int b) {
    if (r < 0) r = 0; if (r > 255) r = 255;
    if (g < 0) g = 0; if (g > 255) g = 255;
    if (b < 0) b = 0; if (b > 255) b = 255;
    return (r << 16) | (g << 8) | b;
}

// 提取RGB分量
__stdcall int red(int color)   { return (color >> 16) & 0xFF; }
__stdcall int green(int color) { return (color >> 8) & 0xFF; }
__stdcall int blue(int color)  { return color & 0xFF; }

// 灰度转换 (ITU-R BT.601)
__stdcall int grayscale(int r, int g, int b) {
    return (r * 299 + g * 587 + b * 114) / 1000;
}

// 颜色混合 (alpha=0..255, 0=全透明 255=全不透明)
__stdcall int color_blend(int fg, int bg, int alpha) {
    int fr = red(fg), fg_g = green(fg), fb = blue(fg);
    int br = red(bg), bg_g = green(bg), bb = blue(bg);
    int a = alpha > 255 ? 255 : (alpha < 0 ? 0 : alpha);
    int ia = 255 - a;
    int r = (fr * a + br * ia) / 255;
    int g = (fg_g * a + bg_g * ia) / 255;
    int b = (fb * a + bb * ia) / 255;
    return rgb(r, g, b);
}

// 颜色亮度调整 (amount: -255..255)
__stdcall int color_brightness(int color, int amount) {
    int r = red(color) + amount;
    int g = green(color) + amount;
    int b = blue(color) + amount;
    if (r < 0) r = 0; if (r > 255) r = 255;
    if (g < 0) g = 0; if (g > 255) g = 255;
    if (b < 0) b = 0; if (b > 255) b = 255;
    return rgb(r, g, b);
}

// HSV → RGB (h=0..359, s=0..255, v=0..255)
__stdcall int hsv_to_rgb(int h, int s, int v) {
    if (s == 0) return rgb(v, v, v);
    h = h % 360;
    if (h < 0) h += 360;
    int region = h / 60;
    int remainder = (h - region * 60) * 255 / 60;
    int p = (v * (255 - s)) / 255;
    int q = (v * (255 - (s * remainder) / 255)) / 255;
    int t = (v * (255 - (s * (255 - remainder)) / 255)) / 255;
    switch (region) {
        case 0:  return rgb(v, t, p);
        case 1:  return rgb(q, v, p);
        case 2:  return rgb(p, v, t);
        case 3:  return rgb(p, q, v);
        case 4:  return rgb(t, p, v);
        default: return rgb(v, p, q);
    }
}

// 预定义颜色
__stdcall int color_black()   { return 0x000000; }
__stdcall int color_white()   { return 0xFFFFFF; }
__stdcall int color_red()     { return 0xFF0000; }
__stdcall int color_green()   { return 0x00FF00; }
__stdcall int color_blue()    { return 0x0000FF; }
__stdcall int color_yellow()  { return 0xFFFF00; }
__stdcall int color_cyan()    { return 0x00FFFF; }
__stdcall int color_magenta() { return 0xFF00FF; }

#!/usr/bin/env python3
"""对 vmlcli 导出的帧做**逐点采样**（不靠眼睛）。

本机没有 PIL/numpy —— `zlib` 在手就够：`PngEncoder` 写的是 8-bit RGBA、
每行 filter 0、单个 IDAT，直接 inflate 就能拿到像素。

用法：python3 sample_png.py <帧.png>      退出码 0 = 全部符合。
几何取自 `Examples/c/draw_colors.c`（3 列 × 5 行，格心取样）——
改了那个程序就要回来改这里的取样点，**两边是同一份布局的两处实现**，
所以取样点都写成"由格号算出来"而不是写死像素。
"""
import sys, zlib, struct

def decode(path):
    data = open(path, 'rb').read()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', "不是 PNG"
    pos, idat, w = 8, b'', None
    while pos < len(data):
        ln = struct.unpack('>I', data[pos:pos+4])[0]
        typ = data[pos+4:pos+8]
        body = data[pos+8:pos+8+ln]
        if typ == b'IHDR':
            w, h, depth, ctype, comp, filt, inter = struct.unpack('>IIBBBBB', body)
            assert depth == 8 and ctype == 6 and inter == 0, (depth, ctype, inter)
        elif typ == b'IDAT':
            idat += body
        elif typ == b'IEND':
            break
        pos += 12 + ln
    raw = zlib.decompress(idat)
    stride = w * 4
    px = bytearray(w * h * 4)
    prev = bytearray(stride)
    for y in range(h):
        f = raw[y * (stride + 1)]
        line = bytearray(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)])
        assert f == 0, f"未预期的 filter {f}"
        px[y*stride:(y+1)*stride] = line
        prev = line
    return w, h, px

def at(w, px, x, y):
    i = (y * w + x) * 4
    return (px[i], px[i+1], px[i+2], px[i+3])

def main():
    path = sys.argv[1]
    w, h, px = decode(path)
    print(f"画布 {w}x{h}")
    cw, ch = w // 3, h // 5
    cxx = lambda c: c * cw + cw // 2
    cyy = lambda r: r * ch + ch // 2
    # (标签, x, y, 期望色或 None=不判)
    C = (0x3C, 0x6E, 0xB4)
    probes = [
        ("0  实心矩形",       cxx(0), cyy(0), C),
        ("1  圆角矩形",       cxx(1), cyy(0), C),
        ("2  实心圆",         cxx(2), cyy(0), C),
        ("3  实心椭圆",       cxx(0), cyy(1), C),
        ("4  实心多边形",     cxx(1), cyy(1) + 14, C),
        ("5  粗折线(左边)",   cxx(1) - (cw//3)//2, cyy(1), C),
        ("6  粗直线",         cxx(0), cyy(2), C),
        ("7  路径填充",       cxx(1), cyy(2) + 14, C),
        ("8  路径描边(左边)", cxx(2) - (cw//3)//2, cyy(2), C),
        ("9  空心矩形描边",   cxx(0), cyy(3) - ch//3, C),
        ("10 线性渐变",       cxx(1), cyy(3), None),
        ("11 回归格(渐变后)", cxx(2), cyy(3), C),
        ("12 回归格(径向后)", cxx(0), cyy(4) - ch//5, C),
        ("13 回归格",         cxx(1), cyy(4), C),
        ("14 收尾纯色圆",     cxx(2), cyy(4), C),
    ]
    bad = 0
    for label, x, y, want in probes:
        got = at(w, px, x, y)
        if want is None:
            print(f"  {label:18s} ({x:3d},{y:3d}) = #{got[0]:02X}{got[1]:02X}{got[2]:02X}  (不判)")
            continue
        ok = got[:3] == want
        if not ok: bad += 1
        print(f"  {label:18s} ({x:3d},{y:3d}) = #{got[0]:02X}{got[1]:02X}{got[2]:02X}"
              f"  期望 #{want[0]:02X}{want[1]:02X}{want[2]:02X}  {'OK' if ok else '✘ 不符'}")
    # 10 号格必须是"渐变的两个端点色"，不能是纯色
    g0 = at(w, px, cxx(1) - cw//3 + 6, cyy(3))
    g1 = at(w, px, cxx(1) + cw//3 - 6, cyy(3))
    print(f"  10 号渐变两端：左 #{g0[0]:02X}{g0[1]:02X}{g0[2]:02X} → 右 #{g1[0]:02X}{g1[1]:02X}{g1[2]:02X}"
          f"  {'OK（两端不同 = 真的是渐变）' if g0[:3] != g1[:3] else '✘ 两端同色 = 渐变没生效'}")
    if g0[:3] == g1[:3]: bad += 1
    print(f"\n结论：{'全部符合' if bad == 0 else f'{bad} 处不符'}")
    return 1 if bad else 0

if __name__ == '__main__':
    sys.exit(main())

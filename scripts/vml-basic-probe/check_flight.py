#!/usr/bin/env python3
"""`put_bitmap_flight.bas` 的判据 —— **有没有拖尾**。

GORILLA.BAS 的香蕉靠「先 XOR 擦旧位置、再 PSET 画新位置」飞过天空
（`GORILLAS.BAS:377` 的 `DrawBan`）。擦除能不能**精确还原**决定画面是
"香蕉飞过去"还是"拖出一条黄带"，而这两者在静态截图上都"看着有香蕉"——
所以判据必须采**路径上早先走过的点**，看它们有没有回到天空色。

⚠ 天空是**索引 0**（`PALETTE 0, 1` 把索引 0 的颜色改成蓝，索引本身还是 0）。
   擦除能精确还原，靠的就是"精灵索引 14 异或自身 = 0 = 天空索引"。

用法： check_flight.py <帧.png>
"""
import struct
import sys
import zlib


def read_png(path):
    """极简 PNG 解码（非隔行、8 位）。本仓没有 PIL，而 zlib 是标准库。"""
    d = open(path, 'rb').read()
    assert d[:8] == b'\x89PNG\r\n\x1a\n', '不是 PNG'
    pos, idat = 8, b''
    w = h = ct = None
    while pos < len(d):
        ln = struct.unpack('>I', d[pos:pos+4])[0]
        typ = d[pos+4:pos+8]
        data = d[pos+8:pos+8+ln]
        if typ == b'IHDR':
            w, h, bd, ct = struct.unpack('>IIBB', data[:10])
            assert bd == 8, f'只处理 8 位深，实得 {bd}'
        elif typ == b'IDAT':
            idat += data
        elif typ == b'IEND':
            break
        pos += 12 + ln
    assert w is not None and h is not None and ct is not None, '缺 IHDR'
    ch = {0: 1, 2: 3, 4: 2, 6: 4}[ct]
    raw = zlib.decompress(idat)
    stride = w * ch
    out, prev, p = bytearray(), bytearray(stride), 0
    for _ in range(h):
        f = raw[p]
        p += 1
        line = bytearray(raw[p:p+stride])
        p += stride
        if f == 1:
            for i in range(ch, stride):
                line[i] = (line[i] + line[i-ch]) & 255
        elif f == 2:
            for i in range(stride):
                line[i] = (line[i] + prev[i]) & 255
        elif f == 3:
            for i in range(stride):
                a = line[i-ch] if i >= ch else 0
                line[i] = (line[i] + ((a + prev[i]) >> 1)) & 255
        elif f == 4:
            for i in range(stride):
                a = line[i-ch] if i >= ch else 0
                b = prev[i]
                c = prev[i-ch] if i >= ch else 0
                pp = a + b - c
                pa, pb, pc = abs(pp-a), abs(pp-b), abs(pp-c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 255
        out += line
        prev = line

    def px(x, y):
        o = y * stride + x * ch
        return (out[o], out[o+1], out[o+2])

    return w, h, px


BAN = (0xFF, 0xFF, 0x00)   # EGA 索引 14
SKY = (0x00, 0x00, 0xAA)   # 索引 0（被 PALETTE 0,1 改成蓝）


def near(a, b, tol=24):
    return all(abs(x-y) <= tol for x, y in zip(a, b))


def main():
    W, H, px = read_png(sys.argv[1])
    cases = [
        (545, 60, BAN, '终点 = 香蕉色（最后一步画上了）'),
        (300, 180, SKY, '路径中段 = 天空色（**擦干净了，没有拖尾**）'),
        (480, 90, SKY, '路径后段 = 天空色'),
        (65, 300, SKY, '起点 = 天空色'),
    ]
    bad = 0
    print(f'飞行用例 帧 {W}x{H}')
    for x, y, want, desc in cases:
        got = px(x, y)
        ok = near(got, want)
        bad += 0 if ok else 1
        mark = '✅' if ok else '❌'
        print(f'  {mark} ({x},{y}) {desc}：'
              f'实得 #{got[0]:02X}{got[1]:02X}{got[2]:02X}，'
              f'期望 #{want[0]:02X}{want[1]:02X}{want[2]:02X}')
    return 1 if bad else 0


if __name__ == '__main__':
    sys.exit(main())

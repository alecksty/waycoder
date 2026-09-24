#!/usr/bin/env python3
"""极简 PNG 解码（非隔行、8 位）—— 画面类判据的**唯一**取像素入口。

本仓没有 PIL，而 zlib 是标准库 —— 所以自己解。

⚠ 本文件是把 `put-bitmap.sh` 里的内联解码与 `check_flight.py` 里的那份**收成一份**的
   产物（本仓的头号坑就是"同一规则两处实现"）。新写画面类判据一律 `from png_pixels import
   read_png`；那两处旧的内联副本**行为逐字相同**，等下次改到它们时顺手换过来。

用法：
    from png_pixels import read_png
    w, h, px = read_png('frame.png')
    px(x, y)  -> (r, g, b)
"""

import struct
import zlib


def read_png(path):
    d = open(path, 'rb').read()
    assert d[:8] == b'\x89PNG\r\n\x1a\n', '不是 PNG'
    pos, idat = 8, b''
    w = h = ct = None
    while pos < len(d):
        ln = struct.unpack('>I', d[pos:pos + 4])[0]
        typ = d[pos + 4:pos + 8]
        data = d[pos + 8:pos + 8 + ln]
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
        line = bytearray(raw[p:p + stride])
        p += stride
        if f == 1:
            for i in range(ch, stride):
                line[i] = (line[i] + line[i - ch]) & 255
        elif f == 2:
            for i in range(stride):
                line[i] = (line[i] + prev[i]) & 255
        elif f == 3:
            for i in range(stride):
                a = line[i - ch] if i >= ch else 0
                line[i] = (line[i] + ((a + prev[i]) >> 1)) & 255
        elif f == 4:
            for i in range(stride):
                a = line[i - ch] if i >= ch else 0
                b = prev[i]
                c = prev[i - ch] if i >= ch else 0
                pp = a + b - c
                pa, pb, pc = abs(pp - a), abs(pp - b), abs(pp - c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 255
        out += line
        prev = line

    def px(x, y):
        o = y * stride + x * ch
        return (out[o], out[o + 1], out[o + 2])

    return w, h, px


def near(a, b, tol=24):
    return all(abs(x - y) <= tol for x, y in zip(a, b))


def hexs(c):
    return '#%02X%02X%02X' % c

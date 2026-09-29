#!/usr/bin/env python3
"""应用图标上架前自查 —— 判据是「**有没有透明像素**」，与 App Store 同口径。

## 为什么单为这件事写一个脚本

App Store 拒收这类图标的报错原文是：

    Invalid App Store Icon. The App Store Icon in the asset catalog in 'X.app'
    can't be transparent nor contain an Alpha Channel.

这句话**两种情况都提了**（透明 / 有 alpha 通道），于是很容易按字面去追「把 alpha 通道去掉」——
而那条路在 .NET MAUI 上走不通：**Resizetizer 对 iOS 图标恒定输出 colorType=6（RGBA）**。
`Color=...` 设上、换成官方文档那套 `ForegroundFile` + `Color` 形态，产出**仍然是 RGBA、alpha 全 255**
（v0.96.577 两种形态都逐字节量过）；而大量 MAUI 应用照样上架成功。
⇒ 真正被拒的是「**有透明像素**」那一种，全不透明的 alpha 通道是被接受的。

**这个错误只在真正上传时才暴露**（本地 build / archive 全绿），而且定位成本极高
—— 所以把判据钉在这里，改完图标本地跑一次即可。

## 判据

1. **尺寸 1024x1024**（营销图标）；
2. **零个非不透明像素** —— 即所有像素 alpha == 255。带 alpha 通道本身**不算错**，
   脚本会把它作为**提示**打出来（源图若是 RGB，连通道都不该有，那是更干净的形态）；
3. 顺带报出图片属性，便于与上一次对比。

用法：
    python scripts/check-appicon.py                      # 默认查 MAUI 的 appicon.png
    python scripts/check-appicon.py 某个/图标.png         # 查别的文件
"""

import struct
import sys
import zlib
from pathlib import Path

DEFAULT = "WayCoder.Maui/Resources/AppIcon/appicon.png"
EXPECT_W = EXPECT_H = 1024


def load_png(path):
    """把 PNG 解成 (w, h, color_type, channels, 逐行还原后的像素字节)。

    自己写而不是用 PIL：本机与 CI 上都不保证装了 Pillow，而这里只需要
    「8 位、非隔行」这一小块子集。**过滤器必须逐行还原**（Sub/Up/Average/Paeth），
    少了这一步拿到的是压缩前的原始字节、alpha 读出来全是噪声
    （⚠ 源图自己的行为 filter=0 时看不出问题，换成别的工具导出的图就会误判）。
    """
    data = Path(path).read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("不是 PNG 文件")

    i, idat = 8, bytearray()
    w = h = bit_depth = color_type = None
    while i < len(data):
        length = struct.unpack(">I", data[i : i + 4])[0]
        kind = data[i + 4 : i + 8]
        body = data[i + 8 : i + 8 + length]
        if kind == b"IHDR":
            w, h, bit_depth, color_type, _comp, _filt, interlace = struct.unpack(">IIBBBBB", body)
            if interlace:
                raise ValueError("不支持隔行 PNG")
        elif kind == b"IDAT":
            idat += body
        i += 12 + length

    # IHDR 必须是第一个块；真缺了就在这儿报清楚，别让它一路 None 下去
    # （否则后面会崩在 `int(None) * int` 之类的算术上，看不出是文件坏了）。
    if w is None or h is None or bit_depth is None or color_type is None:
        raise ValueError("缺少 IHDR 块")

    channels = {0: 1, 2: 3, 3: 1, 4: 2, 6: 4}.get(color_type)
    if channels is None:
        raise ValueError(f"不支持的颜色类型 {color_type}")
    if bit_depth != 8:
        raise ValueError(f"不支持的位深 {bit_depth}")

    raw = zlib.decompress(bytes(idat))
    stride = w * channels
    out = bytearray(h * stride)
    pos, prev = 0, bytearray(stride)
    for y in range(h):
        ftype = raw[pos]
        pos += 1
        line = bytearray(raw[pos : pos + stride])
        pos += stride
        if ftype == 1:  # Sub
            for x in range(channels, stride):
                line[x] = (line[x] + line[x - channels]) & 255
        elif ftype == 2:  # Up
            for x in range(stride):
                line[x] = (line[x] + prev[x]) & 255
        elif ftype == 3:  # Average
            for x in range(stride):
                left = line[x - channels] if x >= channels else 0
                line[x] = (line[x] + ((left + prev[x]) >> 1)) & 255
        elif ftype == 4:  # Paeth
            for x in range(stride):
                left = line[x - channels] if x >= channels else 0
                up = prev[x]
                upleft = prev[x - channels] if x >= channels else 0
                p = left + up - upleft
                pa, pb, pc = abs(p - left), abs(p - up), abs(p - upleft)
                pred = left if (pa <= pb and pa <= pc) else (up if pb <= pc else upleft)
                line[x] = (line[x] + pred) & 255
        elif ftype != 0:
            raise ValueError(f"第 {y} 行出现未知过滤器 {ftype}")
        out[y * stride : (y + 1) * stride] = line
        prev = line

    return w, h, color_type, channels, bytes(out)


def main(argv):
    path = argv[1] if len(argv) > 1 else DEFAULT
    if not Path(path).exists():
        print(f"✘ 找不到图标文件：{path}")
        return 2

    try:
        w, h, color_type, channels, px = load_png(path)
    except Exception as exc:  # noqa: BLE001 - 解析失败要给出可读原因
        print(f"✘ 读不出这张 PNG：{exc}")
        return 2

    names = {0: "灰度", 2: "RGB", 3: "索引", 4: "灰度+alpha", 6: "RGBA"}
    print(f"文件       : {path}")
    print(f"尺寸       : {w}x{h}")
    print(f"颜色类型   : colorType={color_type}（{names[color_type]}）")

    bad = []
    if (w, h) != (EXPECT_W, EXPECT_H):
        bad.append(f"尺寸应为 {EXPECT_W}x{EXPECT_H}，实际 {w}x{h}")

    if color_type in (4, 6):
        alpha = px[channels - 1 :: channels]
        opaque = sum(1 for a in alpha if a == 255)
        total = w * h
        transparent = total - opaque
        lo, hi = min(alpha), max(alpha)
        print(f"alpha      : min={lo} max={hi}")
        if transparent:
            bad.append(
                f"有 {transparent}/{total} 个像素不透明度为 {lo}~{hi}（非全 255）—— "
                "**这正是 App Store 会拒的那种图**（'can't be transparent'）"
            )
        else:
            print("             ✔ 全 255：alpha 通道存在但**没有任何透明像素** ⇒ 可接受")
            print("             （建议：源图改成不带 alpha 通道的 RGB，更干净；但不是硬要求）")
    else:
        print("alpha      : 无 alpha 通道 ✔（最干净的形态）")

    if bad:
        print()
        for line in bad:
            print(f"✘ {line}")
        print()
        print("改法：这张图是从同目录的 appicon.dsl 用仓库的绘图 DSL 画出来的 ——")
        print("      改 .dsl 里的 `rect 0 0 1024 1024 @bg` 那类**铺满整幅**的背景指令，")
        print("      重画后出图时剥掉 alpha 通道，别直接编辑 PNG。")
        return 1

    print()
    print("✔ 通过：这张图标可以直接上架。")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

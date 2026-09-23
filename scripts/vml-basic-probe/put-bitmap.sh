#!/usr/bin/env bash
# `PUT` 一块**手打包在 DATA 里的 QBasic 位图** —— 判据是**逐像素**，不是肉眼。
#
# 为什么单列一条：`vml-basic-probe/run.sh` 那套判据是"程序打印出来的值"，
# 而这条路的产物是**画面**。画面类的判据只能取像素 —— 而且必须取，
# 因为它的历史故障形态正是"**编得过、跑得动、什么都看不见、一个错都不报**"
# （宿主按句柄查不到就什么都不画）。
#
# 两组用例：
#
# ① `put_bitmap.bas` —— 解码与三种方式的落笔
#   (104,100) 月牙内部 → 香蕉色（索引 14 = 黄 #FFFF00）
#   (100,100) 月牙外面 → 背景色（索引 1  = 蓝 #0000AA）
#   (304,100) XOR 一次 → **白**（蓝 1 ^ 香蕉 14 = 索引 15）—— 「按索引异或」的签名
#   (504,100) XOR 两次 → 背景色（异或自逆 ⇒ 精确还原）
#
# ② `put_bitmap_flight.bas` —— **游戏真正用到的那套用法**（GORILLAS.BAS:377 的 DrawBan）：
#   沿斜线飞 40 步，每步「先 XOR 擦旧位置、再 PSET 画新位置」，判据是**有没有拖尾**
#   （路径上早先走过的点必须回到天空色）。这条决定画面是"香蕉飞过去"还是"拖出一条黄带"——
#   而这两种在静态截图上都"看着有香蕉"，所以只能在路径上采点。
#
# 用法： scripts/vml-basic-probe/put-bitmap.sh
# 依赖： scripts/vmlcli（先 `dotnet build scripts/vmlcli/vmlcli.csproj -c Release`）
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CLI="${VMLCLI:-$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll}"
BAS="$ROOT/scripts/vml-basic-probe/put_bitmap.bas"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

if [ ! -f "$CLI" ]; then
    echo "✘ 找不到 $CLI —— 先跑： dotnet build scripts/vmlcli/vmlcli.csproj -c Release"
    exit 2
fi

dotnet "$CLI" "$BAS" --timeout 10 --frame "$TMP/frame.png" >/dev/null 2>"$TMP/err.txt"
if [ ! -s "$TMP/frame.png" ]; then
    echo "✘ 没抓到帧 —— 程序没跑起来？"
    tail -5 "$TMP/err.txt"
    exit 1
fi

python3 - "$TMP/frame.png" <<'PY'
import struct, sys, zlib

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
    ch = {0: 1, 2: 3, 4: 2, 6: 4}[ct]
    raw = zlib.decompress(idat)
    stride = w * ch
    out, prev, p = bytearray(), bytearray(stride), 0
    for _ in range(h):
        f = raw[p]; p += 1
        line = bytearray(raw[p:p+stride]); p += stride
        if f == 1:
            for i in range(ch, stride): line[i] = (line[i] + line[i-ch]) & 255
        elif f == 2:
            for i in range(stride): line[i] = (line[i] + prev[i]) & 255
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

W, H, px = read_png(sys.argv[1])
BG   = (0x00, 0x00, 0xAA)   # EGA 索引 1
BAN  = (0xFF, 0xFF, 0x00)   # EGA 索引 14

def near(a, b, tol=24):
    return all(abs(x-y) <= tol for x, y in zip(a, b))

WHITE = (0xFF, 0xFF, 0xFF)  # EGA 索引 15
cases = [
    (104, 100, BAN,   'PSET：月牙内部 = 香蕉色'),
    (100, 100, BG,    'PSET：月牙外面 = 背景色（颜色 0 的像素跳过）'),
    # ⚠ 这一条是**索引异或**的签名，也是本闸门最要紧的一条：
    #   蓝(索引 1) ^ 香蕉(索引 14) = 索引 15 = 白。
    #   若实现改成按 RGB 异或，这里会得到 #FFFFAA（黄味的新颜色），当场红。
    (304, 100, WHITE, 'XOR：蓝 ^ 香蕉 = 白（**按索引**异或，不是按 RGB）'),
    (504, 100, BG,    'XOR × 2：异或自逆 ⇒ 精确还原背景（"擦除"靠的就是它）'),
]
bad = 0
print(f'帧 {W}x{H}')
for x, y, want, desc in cases:
    got = px(x, y)
    ok = near(got, want)
    bad += 0 if ok else 1
    print(f'  {"✅" if ok else "❌"} ({x},{y}) {desc}：实得 #{got[0]:02X}{got[1]:02X}{got[2]:02X}，期望 #{want[0]:02X}{want[1]:02X}{want[2]:02X}')
sys.exit(1 if bad else 0)
PY
rc=$?
if [ $rc -ne 0 ]; then
    echo "❌ PUT 手打包位图：① 解码/落笔 有判据没过"
    exit 1
fi

# ── ② 飞行用例：PSET 画 + XOR 擦，判据是**不留拖尾** ────────────────────────
dotnet "$CLI" "$ROOT/scripts/vml-basic-probe/put_bitmap_flight.bas" \
    --timeout 15 --frame "$TMP/flight.png" >/dev/null 2>"$TMP/err2.txt"
if [ ! -s "$TMP/flight.png" ]; then
    echo "❌ 飞行用例没抓到帧"
    tail -5 "$TMP/err2.txt"
    exit 1
fi

python3 "$ROOT/scripts/vml-basic-probe/check_flight.py" "$TMP/flight.png"
rc2=$?
echo "──────────────────────────────────────────────"
if [ $rc2 -eq 0 ]; then
    echo "✅ PUT 手打包位图：① 解码/落笔 4/4 ＋ ② 飞行不留拖尾 4/4"
else
    echo "❌ PUT 手打包位图：② 飞行用例有判据没过"
fi
exit $rc2

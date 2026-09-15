#!/usr/bin/env bash
# 把 VML 标准库打成 App 内置资产 WayCoder.Maui/Resources/Raw/vml_lib.zip。
#
# 为什么是一个 zip 而不是逐文件 MauiAsset：Lib/ 有 5300+ 个文件 / 39 MB，
# 打进 APK 是 6 MB；而且 MAUI **没有「列出资产目录」的 API** —— 逐文件放进去
# 就没法在运行时知道有哪些文件，必须先写死一份清单（又一张平行表）。
#
# zip 里的**顶层条目是 `Lib/`、`vmltool.config.xml` 与 `Examples/`** —— 与解压后
# `EnsureLibExtracted()` 期望的目录布局一一对应（它返回的 root 下面就是 Lib/）。
# `Examples/` 是给 `MauiBootstrap.EnsureExamples()` 用的（它把 `Examples/` 下**平铺一层**
# 解到 `<workspace>/examples/`，让用户与 AI 在手机上就有现成的示例程序可跑）。
#
# ⚠ **示例要打进去，光是放进 `Examples/` 目录不够** —— 这个脚本原先只打 Lib 与配置，
#    于是 `EnsureExamples()` 一个条目都找不到、手机上没有示例（而桌面看目录一切正常）。
#
# ⚠ **示例只收两层，绝不递归整棵树。** 上游 `Examples/` 底下挂着 stb / stm32 那种整套工程
#    （128 个条目里 107 个是它们），而 `EnsureExamples()` 是**平铺**解包的（丢掉目录），
#    递归进来就是上千个文件糊进 `examples/` 一个目录、还有重名互相覆盖。
#    收的是 `Examples/README.md`（第 1 层）与 `Examples/<语言>/<文件>`（第 2 层）——
#    第 2 层正好等于"平铺后还能一一对应"的那一层，也就是示例的约定存放位置。
#    ⇒ **以后加示例/游戏，直接放 `Examples/<语言>/` 下，别建子目录**（子目录不会进包）。
#
# ⚠ `*.gen.vml` 是本地跑出来的中间产物（每个几十 KB），不进包。
#
# ⚠ `vmltool.config.xml` 必须打进去，少它整个链接阶段会被跳过（见 MauiVml 注释）。
#
# ⚠ 改完这个 zip 不用去改 `MauiVml.LibVersion` —— 那边用 zip 的**内容指纹**
#    判断要不要重新解压（改了文件指纹自然变）。`LibVersion` 只是给人看的。
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VML="$ROOT/third_party/vml"
OUT="$ROOT/WayCoder.Maui/Resources/Raw/vml_lib.zip"

[ -d "$VML/Lib" ] || { echo "✘ 找不到 $VML/Lib" >&2; exit 1; }
[ -f "$VML/vmltool.config.xml" ] || { echo "✘ 找不到 $VML/vmltool.config.xml" >&2; exit 1; }

TMP="$OUT.tmp"
rm -f "$TMP"

# -X 去掉多余的文件属性（否则同样的内容在 mac/linux 上产出的 zip 字节不同，
#    指纹会跟着变、白解压一次；虽然不影响正确性，但没必要）
if command -v zip >/dev/null 2>&1; then
    ( cd "$VML" && zip -q -r -X "$TMP" Lib vmltool.config.xml )
    ( cd "$VML" && zip -q -X "$TMP" \
        $(find Examples -maxdepth 1 -type f ! -name '*.gen.vml') \
        $(find Examples -mindepth 2 -maxdepth 2 -type f ! -name '*.gen.vml') )
else
    # 没有 `zip` 的机器（例如 Windows Git Bash 默认不带）走 Python —— 用**固定时间戳**
    # 保证同样的内容每次产出同样的字节（与 `zip -X` 的意图一致）。
    echo "ℹ 未找到 zip，改用 Python zipfile"
    python - "$VML" "$TMP" <<'PY'
import os, sys, zipfile
root, out = sys.argv[1], sys.argv[2]

def examples_files():
    """示例只收第 1 层（README）与第 2 层（<语言>/<文件>），见脚本头部注释。"""
    base = os.path.join(root, "Examples")
    for name in sorted(os.listdir(base)):
        full = os.path.join(base, name)
        if os.path.isfile(full):
            yield "Examples/" + name
        elif os.path.isdir(full):
            for sub in sorted(os.listdir(full)):
                if os.path.isfile(os.path.join(full, sub)):
                    yield "Examples/" + name + "/" + sub

with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    def add(rel):
        rel = rel.replace(os.sep, "/")
        full = os.path.join(root, rel)
        if os.path.isdir(full):
            for name in sorted(os.listdir(full)):
                add(rel + "/" + name)
            return
        info = zipfile.ZipInfo(rel, date_time=(1980, 1, 1, 0, 0, 0))
        info.compress_type = zipfile.ZIP_DEFLATED
        info.external_attr = 0o644 << 16
        with open(full, "rb") as f:
            z.writestr(info, f.read())
    add("Lib")
    add("vmltool.config.xml")
    for rel in examples_files():
        if rel.endswith(".gen.vml"):
            continue
        add(rel)
PY
fi

mv -f "$TMP" "$OUT"

echo "✔ $OUT"
ls -la "$OUT" | awk '{print "  大小: " $5 " 字节"}'
echo "  顶层条目：$(python -c "
import sys, zipfile
print(' '.join(sorted({n.split('/')[0] for n in zipfile.ZipFile(sys.argv[1]).namelist()})))
" "$OUT")"
echo "  示例：$(python -c "
import sys, zipfile
ns=[n for n in zipfile.ZipFile(sys.argv[1]).namelist() if n.startswith('Examples/') and n.count('/')==2]
print(len(ns), '个 →', ' '.join(sorted(ns)))
" "$OUT")"

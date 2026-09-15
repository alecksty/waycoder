#!/usr/bin/env bash
# 把 VML 标准库打成 App 内置资产 WayCoder.Maui/Resources/Raw/vml_lib.zip。
#
# 为什么是一个 zip 而不是逐文件 MauiAsset：Lib/ 有 5300+ 个文件 / 39 MB，
# 打进 APK 是 6 MB；而且 MAUI **没有「列出资产目录」的 API** —— 逐文件放进去
# 就没法在运行时知道有哪些文件，必须先写死一份清单（又一张平行表）。
#
# zip 里的**顶层条目就是 `Lib/` 与 `vmltool.config.xml`** —— 与解压后
# `EnsureLibExtracted()` 期望的目录布局一一对应（它返回的 root 下面就是 Lib/）。
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
( cd "$VML" && zip -q -r -X "$TMP" Lib vmltool.config.xml )

mv -f "$TMP" "$OUT"

echo "✔ $OUT"
unzip -l "$OUT" | tail -2
echo "  顶层条目：$(unzip -l "$OUT" | awk '{print $4}' | grep -v '^$' | cut -d/ -f1 | sort -u | grep -v '^Name$' | tr '\n' ' ')"

#!/usr/bin/env bash
# VML 跨语言**浮点**判据 —— 每门语言一份 `langs/f.<ext>`，跑出来逐行核对。
#
# 为什么单独立一套：`Examples/vml/float_ops.vml` 只证明了**汇编层**（`.float`/`.double`）是好的，
# 而"前端把浮点编成什么"是另一回事。2026-09-27 实测：**22 门里只有 C 是对的** ——
# Python/JS/C++ 打印出垃圾整数、BASIC 把常量截成整数、C#/Lua 静默无输出、
# Go 连 `var d float64 = -0.5` 都编不过（未声明的变量 d）。
#
# 判据（三门探针的期望输出，逐行）：
#     F-MUL=  628    ← (int)(3.14 × 2.0 × 100)
#     F-NEG=  -50    ← (int)(-0.5 × 100)
#     F-HEX=  1600   ← (int)(0x10 × 100)
#
# 用法：scripts/vml-float-probe/run-langs.sh [c py …]
# ⚠ 前置：先 `dotnet build scripts/vmlcli/vmlcli.csproj -c Release`
set -uo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
CLI="$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll"
VML_HOME="$ROOT/third_party/vml"

[ -f "$CLI" ] || { echo "✘ 先构建 vmlcli（dotnet build scripts/vmlcli/vmlcli.csproj -c Release）"; exit 2; }

want=$'F-MUL=\n628\nF-NEG=\n-50\nF-HEX=\n1600'
# ── `f2.*`：float/double/**int64** 三组运算（2026-09-27 用户要求"每种语言都要写
#    double/float 的运算代码、还要 int64 的运算"）。大数只作中间量、输出压回小整数，
#    好让判据不依赖各语言的 long 打印。
#    实测矩阵（`run-langs.sh f2` 会打印）：**11 门全部未通过** ——
#      c      D-MUL=627（double 乘精度不对）/ 其余对
#      cpp/js 浮点值当整数打（缺 float→int 转换）
#      lua    同 JS；bas 常量被截成整数；cs/py/rb/pas 编译失败；go/java 撞寄存器类闸（真缺陷）
want2=$'F-MUL=\n628\nD-MUL=\n628\nF-NEG=\n-50\nD-NEG=\n-50\nL-ADD=\n4\nL-MUL=\n2\nL-NEG=\n-4'
pass=0; fail=0; skip=0
for f in "$HERE"/langs/f.* "$HERE"/langs/f2.*; do
    [ -f "$f" ] || continue
    name="$(basename "$f")"
    case "$name" in f2.*) want="$want2";; *) want="$want";; esac
    if [ $# -gt 0 ]; then
        ext="${name#f.}"; ext="${ext#2.}"; keep=0
        for a in "$@"; do [ "$a" = "$ext" ] && keep=1; done
        [ "$keep" = 1 ] || continue
    fi
    got=$(VML_HOME="$VML_HOME" dotnet "$CLI" "$f" --timeout 25 2>&1 | grep -vE '^\[dbg\]|^✔|已注册|成功链接|链接|最终修复|别名')
    if [ "$got" = "$want" ]; then
        printf "  ✅ %-8s 正确\n" "${name#f.}"; pass=$((pass+1))
    else
        made=$(printf '%s\n' "$got" | tail -6 | tr '\n' ' ')
        if [ -z "$made" ]; then
            printf "  ✘ %-8s 无输出（或编译失败）\n" "${name#f.}"; fail=$((fail+1))
        else
            printf "  ✘ %-8s 实得: %s\n" "${name#f.}" "$made"; fail=$((fail+1))
        fi
    fi
done
echo
echo "浮点探针：通过 $pass / 失败 $fail / 跳过 $skip（判据见本脚本头部）"
[ "$fail" = 0 ]

#!/usr/bin/env bash
# C 前端**指针 / 结构体 / 内存**判据 —— 每个用例一段最小复现，判据是**打印出来的值**。
#
# 为什么要单独立一套：`vml-out-probe` 压的是"跨语言输出"、`vml-abi-probe` 压的是调用约定，
# 两边用的都是**十几行的直筒程序**（几个整数、几个字符串），**一个指针都没碰**。
# 而"给手机版写一层 curses 库"这件事，内部全是 `WINDOW *`（结构体指针）、`char **`、
# `malloc`/`free` —— 这些写法在 C 前端到底能不能用，**此前一条判据都没有**。
#
# 第一版的直接动因（2026-09-21）：移植 nyancat 时 `nyan_show(char ** fr)` 编译全绿、
# 运行不报错，**但取到的全是空**（3 帧只发出 3 个 ESC）—— 即"能编、能跑、结果静默错"，
# 正是本仓反复出现的那种形态。逐个探针查比逐个项目试快得多。
#
# 用法：
#   scripts/vml-c-probe/run.sh            # 全部
#   scripts/vml-c-probe/run.sh 02 05      # 按编号前缀挑
#
# 依赖桌面的 `scripts/vmlcli`（与手机端等价的编译+运行流水线）。
# ⚠ 前置：先 `dotnet build scripts/vmlcli/vmlcli.csproj -c Release`。
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CLI="$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll"
CASES="$ROOT/scripts/vml-c-probe/cases"

if [ ! -f "$CLI" ]; then
    echo "✘ 找不到 $CLI —— 先跑： dotnet build scripts/vmlcli/vmlcli.csproj -c Release"
    exit 2
fi

pick=("$@")
pass=0; fail=0; red=0
for f in "$CASES"/*.c; do
    name="$(basename "$f")"
    if [ ${#pick[@]} -gt 0 ]; then
        hit=0
        for p in "${pick[@]}"; do [[ "$name" == "$p"* ]] && hit=1; done
        [ "$hit" = 1 ] || continue
    fi
    out="$(timeout 120 dotnet "$CLI" "$f" 2>&1)"
    # 用例里带 `// EXPECT:` 注释行给期望值；逐行比对
    exp="$(grep -o "EXPECT:.*" "$f" | sed 's/EXPECT: *//')"
    # **已知红**：这条判据压的是"已确认、尚未修"的缺陷。留它在套件里的价值是
    # **钉住症状**（修好那天它会自己变绿），但它不该被算进"通过/失败" ——
    # 否则「N/N 全绿」这个信号就被一条已知项永久污染了（vml-abi-probe 同款处置）。
    known_red=0
    if grep -q "KNOWN-RED" "$f"; then known_red=1; fi
    if [ -z "$exp" ]; then
        printf "  %-18s %s\n" "$name" "$(echo "$out" | grep -E '^[A-Za-z0-9-]+=' | tr '\n' ' ')"
        continue
    fi
    got="$(echo "$out" | grep -oE '^[A-Za-z0-9+-]+=.*' | tr '\n' '|' | sed 's/|$//')"
    if [ "$got" = "$exp" ]; then
        printf "  ✅ %-18s %s\n" "$name" "$got"
        pass=$((pass+1))
    elif [ "$known_red" = 1 ]; then
        printf "  ⚠️  %-18s 已知红（缺陷未修）：期望 %s / 实得 %s\n" "$name" "$exp" "$got"
        red=$((red+1))
    else
        printf "  ❌ %-18s\n     期望 %s\n     实得 %s\n" "$name" "$exp" "$got"
        fail=$((fail+1))
    fi
done

echo "──────────────────────────────────────────────"
echo "通过 $pass / 失败 $fail / 已知红 $red"
[ "$fail" -eq 0 ]

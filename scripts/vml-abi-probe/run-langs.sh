#!/usr/bin/env bash
# VML 跨语言调用约定判据 —— 每种语言一条，跑的是**运行期**行为。
#
# 为什么单独立一套（与 run.sh 的 6 条 C 探针并列）：
# `scripts/maui-vml-verify/corpus/` 那 22 条骨架全绿**证明不了**调用约定对 ——
# 骨架里的库调用都是单参（或参数不参与判据），而「实参反序」只在**两个以上实参**时才露馅，
# 「压了不清」的栈漂移要攒够几次才踩到关键变量。
#
# 两类探针（`langs/` 下，按文件名前缀分）：
#
#   abi.<ext>    **实参顺序**：调 `ipow(2,3)` 并打印。判据 `ABI=8`。
#                 - `ABI=9` = 实参整体反序（左→右压栈）
#                 - `ABI=0` = 第二个实参没传到（只进 R0-R3、不压栈）
#                 - `ABI=1` = `ipow(2,0)`，第二个实参读成 0
#
#   drift.<ext>  **栈漂移**：反复调用库函数后，累加/循环变量必须还是一字未动。
#                判据是各语言自己那个和（见 EXPECT_* 表）——「压了不清」的
#                每次调用净漏 4 或 8 字节，攒几次就把调用方栈帧踩花。
#
# 用法：
#   scripts/vml-abi-probe/run-langs.sh              # 全部
#   scripts/vml-abi-probe/run-langs.sh cpp java     # 按语言挑（匹配 `*.cpp` / `*.java`）
#
# 依赖桌面的 `scripts/vmlcli`（与手机端等价的编译+运行流水线，约 1~2 秒/条）。

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
VMLCLI="${VMLCLI:-$REPO/scripts/vmlcli}"
DLL="$VMLCLI/bin/Release/net10.0/vmlcli.dll"
TIMEOUT="${TIMEOUT:-30}"

if [[ ! -f "$DLL" ]]; then
    echo "✘ 找不到 vmlcli：$DLL" >&2
    echo "  先建一次：dotnet build $VMLCLI -c Release" >&2
    exit 2
fi

# drift 探针的期望值（每个语言算的和不同，只能列表；键 = 扩展名）
declare -A DRIFT_EXPECT=(
    [f90]=126      # 2^1+…+2^6
    [d]=126
    [rb]=126
    [lua]=126
    [m]=126
    [r]=7          # 循环 c(3,4) 两轮都要跑到（3 次 print 先制造 12 字节漂移）
    [cs]=190       # 0+1+…+19
    # ── 2026-09-17 补齐：以下 15 条是「循环里调 ipow(2,i)」的统一形态，
    #    判据一律 2^1+…+2^6 = 126（反序得 91 = i^2 之和；第 2 参丢得 6 = ipow(2,0)）——
    #    目的是把跨语言判据从 7 种语言扩到**全部 22 种**：22 语言骨架全绿证明不了多参调用。
    [c]=126
    [cpp]=126
    [java]=126
    [js]=126
    [py]=126
    [go]=126
    [kt]=126
    [pas]=126
    [bas]=126
    [rs]=126
    [scm]=126
    [swift]=126
    [dart]=126
    [fth]=126
    [ld]=126
)

shopt -s nullglob
probes=("$HERE"/langs/abi.* "$HERE"/langs/drift.*)
if [[ $# -gt 0 ]]; then
    filtered=()
    for p in "${probes[@]}"; do
        for want in "$@"; do
            # 两种写法都收：`cpp`（按扩展名）与 `drift.r`（按整个文件名）
            [[ "$(basename "$p")" == *".$want" || "$(basename "$p")" == "$want" ]] && filtered+=("$p")
        done
    done
    probes=("${filtered[@]}")
fi
shopt -u nullglob

if [[ ${#probes[@]} -eq 0 ]]; then
    echo "✘ 没有匹配的探针（语言名用扩展名：c cpp js rb java lua f90 d m r cs …）。" >&2
    exit 2
fi

pass=0; fail=0; failed_names=()
printf '%-10s %-10s %-6s %s\n' "探针" "扩展名" "结果" "观测值"
printf '%s\n' "------------------------------------------------------------"

for p in "${probes[@]}"; do
    base="$(basename "$p")"; kind="${base%%.*}"; ext="${base#*.}"
    case "$kind" in
        abi)   expect="ABI=8" ;;
        drift) expect="DRIFT=${DRIFT_EXPECT[$ext]:-?}" ;;
    esac
    out="$(cd "$TMPDIR" && dotnet "$DLL" "$p" --timeout "$TIMEOUT" 2>/dev/null)"
    got="$(grep -a -o "$(tr 'a-z' 'A-Z' <<<"$kind")=[0-9-]*" <<<"$out" | head -1)"

    if [[ "$got" == "$expect" ]]; then
        verdict="PASS"; pass=$((pass + 1))
    else
        verdict="FAIL"; fail=$((fail + 1)); failed_names+=("$base")
    fi
    if [[ -z "$got" ]]; then
        if grep -qa "VM execution cancelled" <<<"$out"; then got="(跑不完/超时)"
        else got="(没有输出)"; fi
    fi
    printf '%-10s %-10s %-6s 期望 %s，实得 %s\n' "$kind" "$ext" "$verdict" "$expect" "$got"
done

printf '%s\n' "------------------------------------------------------------"
echo "通过 ${pass} / 失败 ${fail}（共 ${#probes[@]} 条）"
if [[ $fail -gt 0 ]]; then
    echo "失败：${failed_names[*]}"
    echo "  abi   → ABI=9 实参反序 / ABI=0 第 2 参未传到 / ABI=1 第 2 参读成 0"
    echo "  drift → 反复调库后累加值被栈漂移踩花"
    exit 1
fi

#!/usr/bin/env bash
# VML 调用约定判据 —— 在**改动之前**立，改完之后逐条复跑。
#
# 用法：
#   scripts/vml-abi-probe/run.sh              # 跑全部探针
#   scripts/vml-abi-probe/run.sh p3 p5        # 只跑指定的几条（前缀匹配）
#
# 判据（每条探针都是同一个形状）：
#   通过 → stdout 里有 `ABI-OK`，且没有 `VM execution cancelled`
#   失败 → 探针自己打印 `ABI-FAIL …` 然后**挂死**；CLI 的 --timeout 会把它截成
#          `VM execution cancelled`，所以「跑不完」也算失败。
#
#   ⚠ **不能只看探针打印出的掩码**：漂移踩的正是打印路径自己用的栈，
#     实测出现过「掩码印出 0、紧接着的分支却走了失败那一支」的情形
#     （同一个局部连续读两遍得到 0 和 242）。挂死信号才是可信的那一个。
#
# 依赖桌面的 `scripts/vmlcli`（与手机端逐字等价的编译+运行流水线，
# 迭代成本约 1 秒 / 条，而不是打 APK 的 2 分钟）。

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
VMLCLI="${VMLCLI:-$REPO/scripts/vmlcli}"
DLL="$VMLCLI/bin/Release/net10.0/vmlcli.dll"
TIMEOUT=5

if [[ ! -f "$DLL" ]]; then
    echo "✘ 找不到 vmlcli：$DLL" >&2
    echo "  它是这套判据的跑法（与手机端等价、秒级）。先建一次：" >&2
    echo "    dotnet build $VMLCLI -c Release" >&2
    echo "  或设 VMLCLI=<路径> 指到别处。" >&2
    exit 2
fi

# 收集探针（可只跑前缀匹配的那几条）
shopt -s nullglob
probes=("$HERE"/probes/*.c)
if [[ $# -gt 0 ]]; then
    filtered=()
    for p in "${probes[@]}"; do
        for want in "$@"; do
            [[ "$(basename "$p")" == "$want"* ]] && filtered+=("$p")
        done
    done
    probes=("${filtered[@]}")
fi
shopt -u nullglob

if [[ ${#probes[@]} -eq 0 ]]; then
    echo "✘ 没有匹配的探针。" >&2
    exit 2
fi

pass=0
fail=0
failed_names=()

printf '%-24s %-6s %s\n' "探针" "结果" "输出（截断）"
printf '%s\n' "------------------------------------------------------------------------"

for p in "${probes[@]}"; do
    name="$(basename "$p" .c)"
    out="$(cd "$TMPDIR" && dotnet "$DLL" "$p" --timeout "$TIMEOUT" 2>/dev/null)"
    # 只看探针自己的 stdout；`VM execution cancelled` 是 CLI 在超时时补的
    if grep -q '^ABI-OK' <<<"$out" && ! grep -q 'VM execution cancelled' <<<"$out"; then
        verdict="PASS"; pass=$((pass + 1))
    else
        verdict="FAIL"; fail=$((fail + 1)); failed_names+=("$name")
    fi
    brief="$(tr '\n' '|' <<<"$out" | cut -c1-60)"
    printf '%-24s %-6s %s\n' "$name" "$verdict" "$brief"
done

printf '%s\n' "------------------------------------------------------------------------"
# ⚠ `$fail` 后面紧跟中文全角括号会被 bash 当成变量名的一部分（报「未绑定的变量」），
#    必须写成 `${fail}`。
echo "通过 ${pass} / 失败 ${fail}（共 ${#probes[@]} 条）"
if [[ $fail -gt 0 ]]; then
    echo "失败：${failed_names[*]}"
    exit 1
fi

#!/usr/bin/env bash
# 共享库 C 函数的单元测试 —— 逐函数对着算，判据是「自己算一遍再和库的返回比」。
#
# 用法：
#   third_party/vml/test_shared/run.sh            # 全部
#   third_party/vml/test_shared/run.sh strlen     # 挑一个（按文件名，不带 .c）
#
# 约定：每个用例自己 print_int/print_str 报 PASS/FAIL，**退出码非 0 即失败**。
# 判据不经过 stdio 做推导（本仓踩过：C 的 puts 在字面量多的程序里会串行/重复，
# 把 stdio 的毛病看成 codegen 的毛病）。

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../../.." && pwd)"
DLL="${VMLCLI:-$REPO/scripts/vmlcli}/bin/Release/net10.0/vmlcli.dll"
TIMEOUT="${TIMEOUT:-40}"

# ⚠ 外层兜底超时要用对命令名：**macOS 没有 `timeout`**（那是 coreutils 的，装了也叫
#   `gtimeout`）。旧写法直接调 `timeout` ⇒ 每一条都 `未找到命令`，而那条错误又在
#   `2>/dev/null` 里 ⇒ **空输出 + 全判 FAIL**，看着像"用例全挂"，其实是脚本没跑起来。
#   （`vmlcli` 自己那个 `--timeout` 是**程序运行**的时限，管不住"卡在编译"。）
if command -v timeout >/dev/null 2>&1; then TIMEOUT_CMD=(timeout $((TIMEOUT + 60)))
elif command -v gtimeout >/dev/null 2>&1; then TIMEOUT_CMD=(gtimeout $((TIMEOUT + 60)))
else TIMEOUT_CMD=(); fi

[ -f "$DLL" ] || { echo "✘ 找不到 vmlcli：$DLL（先 dotnet build scripts/vmlcli -c Release）" >&2; exit 2; }

shopt -s nullglob
files=("$HERE"/*.c)
if [[ $# -gt 0 ]]; then
    filtered=()
    for f in "${files[@]}"; do
        for want in "$@"; do
            [[ "$(basename "$f" .c)" == "$want" ]] && filtered+=("$f")
        done
    done
    files=("${filtered[@]}")
fi
shopt -u nullglob

[ ${#files[@]} -gt 0 ] || { echo "✘ 没有匹配的用例（$HERE/*.c）" >&2; exit 2; }

pass=0; fail=0; skip=0; smoke=0; failed=(); skipped=(); smoked=()

printf '%-28s %-6s %s\n' "用例" "结果" "输出"
printf '%s\n' "--------------------------------------------------------------------------"

for f in "${files[@]}"; do
    name="$(basename "$f" .c)"
    # ⚠ **必须把 stderr 一起收**：程序自己的 `print_*` 输出在桌面宿主上走的是
    #   **stderr**（与 `[dbg]`、链接进度同一个流），只留 stdout 会把它整段丢掉 ——
    #   表现是**每条用例都判 FAIL、输出栏空白**，而直接跑同一个文件却是 `PASS`。
    #   （判据是下面那几处 **子串** 匹配，多出来的进度噪音不影响。）
    out="$("${TIMEOUT_CMD[@]}" dotnet "$DLL" "$f" --timeout "$TIMEOUT" 2>&1 | tr -d '\0')"
    rc=$?
    # 用例的三种结论：
    #   SKIP  <理由> —— 明确不测（例：浮点路径当前不可用）
    #   SMOKE        —— 只到「没崩/没挂/没超时」这一层（无返回值、副作用在设备上、
    #                   观测通道还没搭的函数就用它）。**单独计一档，不算 PASS** ——
    #                   不许拿「没崩」冒充「正确」。
    #   PASS         —— 断言全过
    if [[ "$out" == *"SKIP"* ]]; then
        verdict="SKIP"; skip=$((skip + 1)); skipped+=("$name")
    elif [[ "$out" == *"SMOKE"* ]]; then
        verdict="SMOKE"; smoke=$((smoke + 1)); smoked+=("$name")
    elif [[ $rc -eq 0 && "$out" == *"PASS"* ]]; then
        verdict="PASS"; pass=$((pass + 1))
    else
        verdict="FAIL"; fail=$((fail + 1)); failed+=("$name")
    fi
    brief="$(printf '%s' "$out" | awk '{printf "%s / ", $0}' | cut -c1-52)"
    printf '%-28s %-6s %s\n' "$name" "$verdict" "$brief"
done

printf '%s\n' "--------------------------------------------------------------------------"
echo "通过 ${pass} / 失败 ${fail} / 冒烟 ${smoke} / 跳过 ${skip}（共 ${#files[@]} 条）"
[ $smoke -gt 0 ] && echo "⚠ 冒烟 ${smoke} 条**只证明没崩、不证明正确**：${smoked[*]}"
[ $fail -gt 0 ] && { echo "失败：${failed[*]}"; exit 1; }
[ $skip -gt 0 ] && echo "跳过：${skipped[*]}"
exit 0

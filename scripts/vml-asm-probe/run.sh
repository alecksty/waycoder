#!/usr/bin/env bash
# **VML 汇编指令**的单元测试 —— 用例是手写的 `.vml`，判据是**打印出来的数值**。
#
# ## 为什么要单独立一套（与 `vml-c-probe` 的分工）
#
# `vml-c-probe` 的用例是 **C 程序**：它压的是"前端 + 汇编器 + 运行时"**整条链**，
# 拿到红不容易判断是哪一层坏的。
#
# 这一套的用例是**裸汇编**：中间没有前端。于是一条红就**只能是汇编器/VM**，
# 定位一步到位。加指令时该配的就是这一套 —— 前端那边（`unsigned` 会不会正确
# 选到 `DIVU`）是**另一条判据**，两边都要有，别互相顶替。
#
# ## 判据怎么读
#
# 每个用例用 `; EXPECT:` 写一行期望值（空格分隔），程序把算出来的数**按同样的顺序**
# 打印出来；runner 逐字比。**期望值是人自己算的**，不从被测实现里推导 ——
# 否则等于自己证明自己。
#
# ⚠ 挑值的原则：**挑有符号与无符号结果不同的**。挑 `100/7` 那种两边同值的，
#   把 `DIVU` 接回 `DIV` 也照样绿，这条判据就是白写的。
#
# ## 一条前置：vmlcli 必须能直跑 `.vml`
#
# `scripts/vmlcli` 的 `.vml` 直通是**随这一套一起加的**（v0.96.373）——
# 在此之前它只认那 22 个前端扩展名，`foo.vml` 会报"认不出这个扩展名"，
# 而手机端的 `MauiVml.Run` 一直是认的（桌面/手机流水线的一个缺口）。
#
# 用法：
#   scripts/vml-asm-probe/run.sh              # 全部
#   scripts/vml-asm-probe/run.sh 03           # 按编号前缀挑
#
# 依赖桌面的 `scripts/vmlcli`（与手机端等价的编译+运行流水线）。
# ⚠ 前置：先 `dotnet build scripts/vmlcli/vmlcli.csproj -c Release`。
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CLI="$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll"
CASES="$ROOT/scripts/vml-asm-probe/cases"

if [ ! -f "$CLI" ]; then
    echo "✘ 找不到 $CLI —— 先跑： dotnet build scripts/vmlcli/vmlcli.csproj -c Release"
    exit 2
fi

RUN_TIMEOUT=60

# 跨平台超时：macOS 自带没有 `timeout`/`gtimeout`（见 vml-c-probe 里那段记的坑：
# 直接写 `timeout …` 会让每个用例都拿到 `command not found`，看上去像"全红"）。
run_to() {
    if command -v timeout >/dev/null 2>&1; then timeout "$RUN_TIMEOUT" "$@"
    elif command -v gtimeout >/dev/null 2>&1; then gtimeout "$RUN_TIMEOUT" "$@"
    else "$@"; fi
}

pick=("$@")
pass=0; fail=0
failed=()

printf '%-34s %-6s %s\n' "用例" "结果" "实得 / 期望"
printf '%s\n' "--------------------------------------------------------------------------"

for f in "$CASES"/*.vml; do
    name="$(basename "$f" .vml)"
    if [ ${#pick[@]} -gt 0 ]; then
        hit=0
        for p in "${pick[@]}"; do [[ "$name" == "$p"* ]] && hit=1; done
        [ "$hit" = 1 ] || continue
    fi

    # 期望值：`; EXPECT: a b c`
    want="$(sed -n 's/^;;* *EXPECT: *//p' "$f" | head -1)"
    if [ -z "$want" ]; then
        printf '%-34s %-6s %s\n' "$name" "SKIP" "用例里没有 `; EXPECT:` 行"
        continue
    fi

    # ⚠ **只取 stdout**：vmlcli 的契约是 stdout = 程序自己的输出 / stderr = 编译链接日志。
    #   写成 `2>&1` 的话几十行"已注册前端…"会把判据行淹掉（c-probe 那边踩过）。
    out="$(run_to dotnet "$CLI" "$f" 2>/dev/null | tr -s '[:space:]' ' ' | sed 's/^ //; s/ $//')"

    if [ "$out" = "$want" ]; then
        printf '%-34s %-6s %s\n' "$name" "✅" "$out"
        pass=$((pass + 1))
    else
        printf '%-34s %-6s 实得 [%s]\n%-42s期望 [%s]\n' "$name" "❌" "$out" "" "$want"
        fail=$((fail + 1)); failed+=("$name")
    fi
done

echo "--------------------------------------------------------------------------"
if [ "$fail" -eq 0 ]; then
    echo "全部通过（$pass 个用例）"
else
    echo "失败 $fail / 共 $((pass + fail)) 个：${failed[*]}"
fi
exit $([ "$fail" -eq 0 ] && echo 0 || echo 1)

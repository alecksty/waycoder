#!/usr/bin/env bash
# VML 跨语言**输出**判据 —— 每门语言一份 `langs/out.<ext>`，判据是**逐字节比对输出**。
#
# 为什么要单独立一套（与 `vml-abi-probe` 的调用约定判据并列）：
# 22 条骨架里每条都打印 `SKEL-SUM=14`，看起来"输出是好的" —— 但**那一条路径太窄**：
# 它只打印「一个纯 ASCII 字面量 + 一个整数」，不换行也不带标点。
# 用户真机实测的原话是「**很多语言没有输出，或者输出错误**」。
#
# 判据：三个探针程序都必须恰好输出三行，**逐字节相同**：
#
#     OUT-STR=abc
#     OUT-INT=42
#     OUT-PUN=hello, world
#
# 三行分别压的是：纯文本字面量 / 整数 / **带空格与逗号的字面量**。
# 最后一行是刻意挑的 —— 带标点的字符串最容易暴露「字符串被截断」「转义被吃掉」
# 「按空白切词」这类前端缺陷，而 `SKEL-SUM=14` 那种无空格形态照不出来。
#
# 用法：
#   scripts/vml-out-probe/run-langs.sh            # 全部
#   scripts/vml-out-probe/run-langs.sh c py       # 按扩展名挑
#
# 依赖桌面的 `scripts/vmlcli`（与手机端等价的编译+运行流水线）。
#
# ⚠ **校准状态（2026-09-17 首轮：0/22）**：探针文件是用「每门语言一套打印模板」批量生成的，
#    首轮全红，而且**红的主要是模板、不是语言** —— 最典型的是换行：输出拼成了
#    `OUT-STR=abc<sep>42<sep>OUT-PUN=…`，说明我给的"怎么打一个换行"大多不对。
#    已能分辨出的**真信号**（与模板无关，值得单独追）：
#      · `go` / `kt` 在字符串后面多打了一个 `0`（我拿 `println_int(0)` 当换行用，它们照打了）
#      · `swift` 直接 `CALL main` 崩（外层包装写法与该前端不符）
#      · `rs` 少了整行整数输出
#    下一步：**逐门照 `scripts/maui-vml-verify/corpus/<语言>/skel.<ext>` 的打印段校准**
#    （那份是各语言唯一被验证过能打印的样板），校准完这 22 条才真正在量「语言」。

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
DLL="${VMLCLI:-$REPO/scripts/vmlcli}/bin/Release/net10.0/vmlcli.dll"
TIMEOUT="${TIMEOUT:-30}"

[ -f "$DLL" ] || { echo "✘ 找不到 vmlcli：$DLL（先 dotnet build scripts/vmlcli -c Release）" >&2; exit 2; }

EXPECT=$'OUT-STR=abc\nOUT-INT=42\nOUT-PUN=hello, world'

shopt -s nullglob
# 两个家族：
#   out.<ext>  —— **共享库**那条路（println_str / println_int，见 Lib/shared/console.vml）
#   nat.<ext>  —— **这门语言自己的**标准输出函数（C 的 printf / Python 的 print /
#                 Go 的 println / Kotlin 的 println / JS 的 console.log …）
# 两条路是两套实现，坏一条不代表另一条好 —— 用户点出来的正是这个：
# 「各语言还要测试自己的标准输出函数，现在只有测试共享库的」。
files=("$HERE"/langs/out.* "$HERE"/langs/nat.*)
if [[ $# -gt 0 ]]; then
    filtered=()
    for f in "${files[@]}"; do
        for want in "$@"; do
            [[ "${f##*.}" == "$want" ]] && filtered+=("$f")
            # 也可以按家族挑：`run-langs.sh nat`
            [[ "$(basename "$f")" == "$want".* ]] && filtered+=("$f")
        done
    done
    files=("${filtered[@]}")
fi
shopt -u nullglob

[ ${#files[@]} -gt 0 ] || { echo "✘ 没有匹配的探针" >&2; exit 2; }

pass=0; fail=0; failed=()
printf '%-10s %-6s %s\n' "探针" "结果" "输出（ / = 换行）"
printf '%s\n' "--------------------------------------------------------------------"

for f in "${files[@]}"; do
    ext="${f##*.}"
    out="$(cd "$(dirname "$f")" && timeout $((TIMEOUT + 20)) dotnet "$DLL" "$f" --timeout "$TIMEOUT" 2>/dev/null | tr -d '\0')"
    # 只看程序自己的输出：vmlcli 的编译/链接进度走 stderr，这里已经 2>/dev/null 掉了；
    # 再掐掉 `? 运行完成…` 那一行兜底
    got="$(printf '%s\n' "$out" | grep -av '^?' | sed -e 's/[[:space:]]*$//')"
    if [[ "$got" == "$EXPECT" ]]; then
        verdict="PASS"; pass=$((pass + 1))
    else
        verdict="FAIL"; fail=$((fail + 1)); failed+=("$(basename "$f")")
    fi
    # ⚠ 不能用 `tr '\n' '⏎'` —— tr 按**字节**替换，而 ⏎ 是多字节 UTF-8，
    #    换完只剩它的首字节，输出会变成一串乱码（第一版就踩了，读起来像"语言坏了"）。
    brief="$(printf '%s' "$got" | awk '{printf "%s / ", $0}' | cut -c1-58)"
    printf '%-10s %-6s %s\n' "$(basename "$f")" "$verdict" "$brief"
done

printf '%s\n' "--------------------------------------------------------------------"
echo "通过 ${pass} / 失败 ${fail}（共 ${#files[@]} 条）"
[ $fail -gt 0 ] && { echo "失败：${failed[*]}"; exit 1; }
exit 0

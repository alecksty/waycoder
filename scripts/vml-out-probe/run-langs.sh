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
# ⚠ **校准状态（2026-09-17，v0.96.208）**：**23 通过 / 3 输出不符 / 2 编译失败**（28 条）。
#    一路校准下来红的主要是**我自己的探针**、不是语言 —— 反复踩到的三类：
#      · **注释前缀写错**（`//` 用在 Python/Pascal/Ladder/R/Ruby/Scheme/Lua 上 ⇒ 编译期就抛）。
#        这是**最贵的一个坑**：它把"编译失败"伪装成"运行了没输出"。
#      · **拿别的语言的换行/分隔语义套这一门**（Go/Python 的 println/print 在操作数间插空格、
#        Kotlin 的 println 只吃一个实参）⇒ 各语言的原生 print 语义不同，期望必须分开：
#        探针旁边放一份 `<探针>.expect` 覆盖默认期望。
#      · **忘记整数实参从 args[0] 起**（自己写用例时也踩过：期望按 args[2] 写）。
#    ⇒ **runner 现在把「编译期抛异常」与「跑完输出不对」分成两档报**（ERR / FAIL）——
#      早先一律 2>/dev/null，5 个编译失败被报成「输出为空」，害得"修编译器"和
#      "修探针"混在一起看不出来。
#    剩下的真信号（与探针无关，值得单独追）：
#      · `out.rs` —— `println!("OUT-INT={}", 42)` 打出 `OUT-INT=`（整数整个丢了）
#      · ~~`out.js`~~ 已修：那条"编译运行都正常却零输出"是**探针自己的bug** ——
#        JS 前端对**不认识的函数名**会把名字当变量、编成 `MOVE R1,var_<名>; CALL R0`
#        ⇒ 运行期跳野地址（骨架 corpus/javascript/skel.js 里逐字记着）。
#        修法是先把库函数声明成 `native function ... {}`。**又一个"看着像语言坏了、
#        其实是探针"** —— 这类"编译全绿、零输出"最容易被误判成前端缺陷。
#      · `out.fth` —— 第二行 ` OUT-INT=42` 多一个**前导空格**
#      · `nat.py` —— 多实参 `print("OUT-INT=", 42)` 打出 ` 1036`（字面量丢了、整数也不对）
#      · `out.f90` / `nat.kt` —— 仍**编译期抛异常**，先修探针再看输出

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
# ⚠ 排除 *.expect —— 它们是**期望值**不是探针，glob 会一并匹配到
#   （实测过：nat.go.expect / nat.py.expect 被当成探针去编译）
keep=(); for f in "${files[@]}"; do [[ "$f" == *.expect ]] || keep+=("$f"); done
files=("${keep[@]}")
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

pass=0; fail=0; err=0; failed=(); errored=()
printf '%-10s %-6s %s\n' "探针" "结果" "输出（ / = 换行）"
printf '%s\n' "--------------------------------------------------------------------"

for f in "${files[@]}"; do
    ext="${f##*.}"
    # 该探针自带期望就用它（各语言的原生 print 语义不同：Go/Python 的 println/print
    # 在操作数间**插一个空格**，Kotlin 的 println 只吃一个实参）—— 统一成一行是错的。
    exp="$EXPECT"
    [[ -f "$f.expect" ]] && exp="$(cat "$f.expect")"
    # ⚠ stderr 要**单独接住**：编译期抛异常与「跑完输出不对」是两回事。
    #   早先一律 2>/dev/null，把 5 个**编译失败**报成了「输出为空」，
    #   于是「修编译器」和「修探针注释前缀」混在一起看不出来（实测踩过）。
    errf="$(mktemp)"
    out="$(cd "$(dirname "$f")" && timeout $((TIMEOUT + 20)) dotnet "$DLL" "$f" --timeout "$TIMEOUT" 2>"$errf" | tr -d '\0')"
    # 只看程序自己的输出：vmlcli 的编译/链接进度走 stderr；再掐掉 `? 运行完成…` 那一行兜底
    got="$(printf '%s\n' "$out" | grep -av '^?' | sed -e 's/[[:space:]]*$//')"
    if grep -qaE 'Unhandled exception|CompilationException|ParseException' "$errf"; then
        verdict="ERR"; err=$((err + 1)); errored+=("$(basename "$f")")
        brief="编译期抛异常：$(grep -aoE '[A-Za-z]+Exception' "$errf" | head -1)"
    elif [[ "$got" == "$exp" ]]; then
        verdict="PASS"; pass=$((pass + 1))
    else
        verdict="FAIL"; fail=$((fail + 1)); failed+=("$(basename "$f")")
    fi
    rm -f "$errf"
    # ⚠ 不能用 `tr '\n' '⏎'` —— tr 按**字节**替换，而 ⏎ 是多字节 UTF-8，
    #    换完只剩它的首字节，输出会变成一串乱码（第一版就踩了，读起来像"语言坏了"）。
    brief="$(printf '%s' "$got" | awk '{printf "%s / ", $0}' | cut -c1-58)"
    printf '%-10s %-6s %s\n' "$(basename "$f")" "$verdict" "$brief"
done

printf '%s\n' "--------------------------------------------------------------------"
echo "通过 ${pass} / 输出不符 ${fail} / **编译失败 ${err}**（共 ${#files[@]} 条）"
[ $err -gt 0 ] && echo "⚠ 编译期就抛异常的探针（先修探针/前端，还没轮到看输出）：${errored[*]}"
[ $fail -gt 0 ] && { echo "失败：${failed[*]}"; exit 1; }
exit 0

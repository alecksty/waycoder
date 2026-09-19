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
# ⚠ **校准状态（2026-09-17，v0.96.208）**：**28 / 28 全绿**（2026-09-17 v0.96.210）。
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
#    ⚠ 还有两类**探针写法本身就不被支持**，同样伪装成"语言坏了"：
#      · Fortran 前端**只支持表控输出 print ***，不支持格式化 print '(A,I0)'
#        （报 "expected * after print (got StringLiteral)"）
#      · Kotlin 的 println **只吃一个实参**（报 "Expected ')' ... got Comma"）
#      两者都是编译期抛异常 ⇒ 现在会被 ERR 档单独点出来。
#    ⚠ 改本文件时注意：**别用带反引号的 shell heredoc 写它** ——
#      反引号会被 bash 当命令替换，把命令的输出塞进注释里（实测踩过：
#      注释里出现了 `无法初始化设备 PRN` 这句）。改完必须回读。
#    ⚠ **判别"是探针坏了还是语言坏了"的顺序**（这一轮 17→28 全靠它）：
#      ① 看 stderr —— 编译期抛异常 ⇒ 先修探针，别去看输出（ERR 档）
#      ② 编译过了但零输出/输出怪 ⇒ **看生成的汇编**（`--vml` 导出）
#         这一轮靠它抓出三条真前端缺陷：
#           · Rust `println!` 整数实参被静默丢（类型名 "int" vs "integer" 对不上，
#             且 if/else 链没有兜底 else）—— 汇编里**从来没有那次 print_int**
#           · Python 多实参 `print` 的实参读取位置**整个反了**（`(Count-1-i)*4`
#             应在 `Args.Count==1` 时恰好对 ⇒ 单实参永远照不出来）—— 汇编里
#             两个实参的读槽是对调的
#           · Forth `."` 把**分隔符空格**当成了字符串内容 ⇒ 每行多一个前导空格
#         **三者的共同点：编译链接全绿。** "绿"恰恰最容易让人下错结论。
#      ③ 只有当汇编里那一处调用/取值确实错了，才轮到怀疑语言。


set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
DLL="${VMLCLI:-$REPO/scripts/vmlcli}/bin/Release/net10.0/vmlcli.dll"
TIMEOUT="${TIMEOUT:-30}"

[ -f "$DLL" ] || { echo "✘ 找不到 vmlcli：$DLL（先 dotnet build scripts/vmlcli -c Release）" >&2; exit 2; }

# ⚠ `timeout` 是 **GNU coreutils** 的命令，**macOS 默认没有**（本仓日常在 Mac 上验证）。
#   缺了它的表现极具误导性：整条命令行直接 `command not found` ⇒ 抓到空输出 ⇒
#   **28 条探针一起报 FAIL，而一条真问题都看不出来** —— 比不跑还糟（会让人去"修语言"）。
#   实测 2026-09-19：手工单跑 out.c 三行全对，走本脚本却 0/28。
#   有 `timeout` 就用，其次 `gtimeout`（brew coreutils 装的那个），都没有就**不加外壳** ——
#   vmlcli 自己的 `--timeout` 已经能在 VM 层把跑不完的程序掐掉，外壳只是多一层兜底。
#   ⚠ 用函数而不是数组：macOS 自带的是 bash 3.2，空数组配 `set -u` 展开会报 unbound variable。
if command -v timeout >/dev/null 2>&1; then TIMEOUT_BIN=timeout
elif command -v gtimeout >/dev/null 2>&1; then TIMEOUT_BIN=gtimeout
else TIMEOUT_BIN=""; fi
run_probe() {
    if [ -n "$TIMEOUT_BIN" ]; then "$TIMEOUT_BIN" $((TIMEOUT + 20)) "$@"; else "$@"; fi
}

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
    out="$(cd "$(dirname "$f")" && run_probe dotnet "$DLL" "$f" --timeout "$TIMEOUT" 2>"$errf" | tr -d '\0')"
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

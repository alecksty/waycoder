#!/usr/bin/env bash
# 优化（`-O` / 设置 → 编译 · 优化级别）的判据。
#
# ## 为什么要有这一套
#
# 死代码消除（可达性分析）上游给它标的评语是"实验性, 链接库程序误删除代码"，
# 而它错起来的样子是**最坏的一种**：编译全绿、产物看着正常，**跑起来才崩**
# （2026-09-27 实测：46 字节的 hello.c 删到 11 条指令，然后崩在 `未找到标签: lib_io_puts`）。
# 三个根因都出在"标签只在 `Program.Labels` 字典里、指令上一个都没有"这个事实上
# （详见 `DeadCodeEliminationPass` / `ControlFlowGraph` / `OptimizationPipeline` 的注释）。
#
# 修完之后，**必须有一条能跑的判据持续压着它**，否则下次谁再动优化，回归是静默的。
#
# ## 四条判据
#
#   ① **开关表**：`--dump-opt-policy` 打出的两档 —— 有已知缺陷的 pass 必须恒为 `false`；
#      `deadcode` 必须 L1=false / L2=true（它就是 O2 与 O1 的全部差别）。
#   ② **优化不能改变行为**：`-O0` / `-O1` / `-O2` 三者运行输出**逐字节相同**。
#   ③ **O2 必须真的变小**：`-O2` 的指令数要**严格少于** `-O0`。
#      ⚠ 这条是**正向**判据，故意与 ①② 的方向相反 —— 只判"没坏"的话，
#        "DCE 悄悄不生效了"这种回归会一路绿灯（本仓记过：不响的闸比没有更糟）。
#   ④ **跨语言**：`vml-out-probe/langs/out.*` 那 22 门语言，`-O0` 与 `-O2` 的运行输出
#      必须逐字节相同。可达性分析一旦认错标签，最先炸的就是"C 能跑、别的前端不能跑"
#      这类分叉 —— 单语言的判据照不出来。
#
# ⚠ **`timeout` 不是通用命令**：它是 GNU coreutils 的，macOS 自带没有（装了 coreutils 才有
#   `gtimeout`）。直接写 `timeout …` 在 macOS 上每次都是 `command not found` ⇒ 判据读到的是
#   那句报错本身（`vml-c-probe` 早期就这么白红过一轮）。见 `run_to()`。
# ⚠ **只取 stdout**：`vmlcli` 的契约是 `stdout = 程序自己的输出` / `stderr = 编译链接日志`，
#   合并（`2>&1`）会让"运行输出相同"这条判据被几十行编译日志淹掉。
#
# 用法：
#   scripts/vml-opt-probe/run.sh
#
# 依赖桌面的 `scripts/vmlcli`（与手机端同一条流水线）。
# ⚠ 前置：先 `dotnet build scripts/vmlcli/vmlcli.csproj -c Release`。
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CLI="$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll"
DOTNET="${WAYCODER_DOTNET:-dotnet}"

if [ ! -f "$CLI" ]; then
    echo "✘ 找不到 $CLI —— 先跑： dotnet build scripts/vmlcli/vmlcli.csproj -c Release"
    exit 2
fi

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

PASS=0; FAIL=0
check() { # check <名字> <实得> <期望>
    if [ "$2" = "$3" ]; then
        echo "  ✅ $1 = $2"; PASS=$((PASS + 1))
    else
        echo "  ❌ $1 = $2（期望 $3）"; FAIL=$((FAIL + 1))
    fi
}

run_to() { # run_to <秒> <stdout文件> <stderr文件> <参数…>
    local secs="$1" outf="$2" errf="$3"; shift 3
    local runner=""
    command -v gtimeout >/dev/null 2>&1 && runner="gtimeout $secs"
    command -v timeout  >/dev/null 2>&1 && [ -z "$runner" ] && runner="timeout $secs"
    # shellcheck disable=SC2086
    $runner "$DOTNET" "$CLI" "$@" >"$outf" 2>"$errf"
}

# ── 用例源码：写得**极简**是有意的 ──
# 它要能跑出可比的输出，同时把"链进来的标准库有多大"这件事一起显出来
# （实测 46 字节的 hello.c 链进来 69637 条指令 —— 那正是 O2 要砍掉的部分）。
SRC="$TMP/hello.c"
cat > "$SRC" <<'EOF'
int main() { puts("hello world"); return 0; }
EOF

echo "═══ ① 开关表：有已知缺陷的 pass 必须恒为 false；deadcode 只在 O2 开 ═══"
run_to 60 "$TMP/policy.out" "$TMP/policy.err" --dump-opt-policy
if [ ! -s "$TMP/policy.out" ]; then
    echo "  ❌ --dump-opt-policy 没有任何输出"; FAIL=$((FAIL + 1))
else
    # ⚠ **`grep -i` 是必须的**：C# 的 `bool.ToString()` 打出来是 `True`/`False`（首字母大写），
    #   而命令行惯例是小写 —— 第一版按 `=true$` 匹配，于是"开关表全对"被判成了 7 条红（实测）。
    for lv in 1 2 3; do
        grep -qi "^L$lv.nop=true$" "$TMP/policy.out" && PASS=$((PASS+1)) \
            || { echo "  ❌ L$lv nop 不是 true"; FAIL=$((FAIL+1)); }
        for pass in constantfolding jumpchaining deadstore copyprop peephole; do
            if grep -qi "^L$lv.$pass=false$" "$TMP/policy.out"; then
                PASS=$((PASS + 1))
            else
                # 这条红意味着有人打开了上游标注"会误删代码/标签损坏"的 pass ——
                # 去看 OptimizationPolicy 里逐条记的原因，别直接改判据。
                echo "  ❌ L$lv $pass 被打开了（上游标注它有已知缺陷）"; FAIL=$((FAIL + 1))
            fi
        done
    done
    # `deadcode` 是 O2 与 O1 的**全部差别**：L1 关、L2 开
    grep -qi '^L1.deadcode=false$' "$TMP/policy.out" && PASS=$((PASS+1)) \
        || { echo "  ❌ L1 的死代码消除应当关闭"; FAIL=$((FAIL+1)); }
    grep -qi '^L2.deadcode=true$' "$TMP/policy.out" && PASS=$((PASS+1)) \
        || { echo "  ❌ L2 的死代码消除应当打开（否则 O2 与 O1 没区别）"; FAIL=$((FAIL+1)); }
    grep -qi '^L3.jumpchaining=false$' "$TMP/policy.out" && PASS=$((PASS+1)) \
        || { echo "  ❌ L3 打开了跳转链（上游标注有标签损坏 bug）"; FAIL=$((FAIL+1)); }
    echo "  ℹ $(grep -i 'deadcode' "$TMP/policy.out" | tr '\n' ' ')"
fi

echo "═══ ② 优化不改变行为（-O0 / -O1 / -O2 输出逐字节相同）═══"
run_to 120 "$TMP/o0.out" "$TMP/o0.err" "$SRC"
run_to 120 "$TMP/o1.out" "$TMP/o1.err" "$SRC" -O1
run_to 120 "$TMP/o2.out" "$TMP/o2.err" "$SRC" -O2
run_to 120 "$TMP/o3.out" "$TMP/o3.err" "$SRC" -O3
check "O0 输出" "$(cat "$TMP/o0.out")" "hello world"
check "O1 输出" "$(cat "$TMP/o1.out")" "hello world"
check "O2 输出" "$(cat "$TMP/o2.out")" "hello world"
check "O3 输出" "$(cat "$TMP/o3.out")" "hello world"

echo "═══ ③ O2 必须真的变小（正向判据）═══"
# ⚠ **必须先转成 UTF-8 再 grep**：`vmlcli` 的日志经 Windows 控制台写出，是**系统 OEM 代码页**
#   （中文系统 = GBK），而日志里除标记外还有中文散文 —— 在 GBK 字节里按 UTF-8 的规则切，
#   读出来的行会断在多字节字符中间。实测量到的数是**对的**（68339 → 28），只是 grep 读不出来。
#
# ⚠ **判据认的是语言无关的方括号标记，不是中文散文**（`[link-done]` / `[opt]`）：
#   此前它 grep 的是 `链接完成，总指令数: ` / `优化 O2：`，而这两行的散文现在会**跟着系统语言变**
#   —— 英文 locale 的机器上永远匹配不到 ⇒ **判据静默失效**（报「没能从编译日志里读到指令数」，
#   看着像优化没生效，实际是语言）。标记是 ASCII、与语言无关，两个 locale 下都取得到。
#   本仓对此有明确立场：「判据必须能跑，不响的闸比没有更糟」。
#   ⚠ Linux/macOS 上日志本来就是 UTF-8，`iconv -f UTF-8 -t UTF-8` 会**成功** ⇒
#     只有它失败时才回落 GBK，两边都对（不能无条件按 GBK 转，那会把好数据转坏）。
errtext() {
    if iconv -f UTF-8 -t UTF-8 "$1" >/dev/null 2>&1; then
        cat "$1"
    else
        iconv -f GBK -t UTF-8 "$1" 2>/dev/null || cat "$1"
    fi
}
# ⚠ `tr -d ','` 是**必须的**：指令数一旦带上千位分隔符（`69,648`），
#   只取数字会读到**最后一段**（`648`）—— 而 `648 < 69648` 仍然成立 ⇒ 判据**假绿**。
#   今天的产出端用的是无分隔符的 `{Count}`，但这条判据不该依赖那个巧合。
n0="$(errtext "$TMP/o0.err" | grep -oE '\[link-done\].*' | tr -d ',' | grep -oE '[0-9]+' | tail -1)"
n2="$(errtext "$TMP/o2.err" | grep -oE '\[opt\].*' | tr -d ',' | grep -oE '[0-9]+' | tail -1)"
echo "  ℹ 指令数：O0=${n0:-?} → O2=${n2:-?}"
if [ -n "$n0" ] && [ -n "$n2" ]; then
    if [ "$n2" -lt "$n0" ]; then
        echo "  ✅ O2 确实变小了（省 $((n0 - n2)) 条，${n0} → ${n2}）"; PASS=$((PASS + 1))
    else
        # 不变就说明死代码消除没生效（开关没接上 / 护栏误触发 / pass 被跳过）
        echo "  ❌ O2 没有变小（$n0 → $n2）—— 死代码消除没起作用"; FAIL=$((FAIL + 1))
    fi
else
    echo "  ❌ 没能从编译日志里读到指令数（先确认 errtext 能把日志读成 UTF-8）"; FAIL=$((FAIL + 1))
fi
n3="$(errtext "$TMP/o3.err" | grep -oE '\[opt\].*' | tr -d ',' | grep -oE '[0-9]+' | tail -1)"
if [ -n "$n2" ] && [ -n "$n3" ]; then
    if [ "$n3" -le "$n2" ]; then
        echo "  ✅ O3 不比 O2 差（$n2 → $n3）"; PASS=$((PASS + 1))
    else
        echo "  ❌ O3 反而比 O2 大（$n2 → $n3）"; FAIL=$((FAIL + 1))
    fi
fi

echo "═══ ④ 跨语言：-O0 与 -O2 运行输出必须逐字节相同 ═══"
LANGS_DIR="$ROOT/scripts/vml-out-probe/langs"
if [ -d "$LANGS_DIR" ]; then
    same=0; diff=0; skipped=0
    for f in "$LANGS_DIR"/out.*; do
        case "$f" in *.expect) continue;; esac
        name="$(basename "$f")"
        run_to 120 "$TMP/l0.out" "$TMP/l0.err" "$f"
        run_to 120 "$TMP/l2.out" "$TMP/l2.err" "$f" -O2
        run_to 120 "$TMP/l3.out" "$TMP/l3.err" "$f" -O3
        if [ ! -s "$TMP/l0.out" ]; then
            # `-O0` 自己就没输出 ⇒ 这一门在本机不可判（Ladder 那类只能编译的前端在这里），
            # **不算通过也不算失败**，但要显式报出来 —— 别让"跳过"被读成"通过"。
            skipped=$((skipped + 1)); echo "  ⚠ $name：-O0 无输出，跳过"; continue
        fi
        if cmp -s "$TMP/l0.out" "$TMP/l2.out" && cmp -s "$TMP/l0.out" "$TMP/l3.out"; then
            same=$((same + 1))
        else
            diff=$((diff + 1))
            echo "  ❌ $name：-O2/-O3 的输出与 -O0 不同"
            diff "$TMP/l0.out" "$TMP/l2.out" | head -4
            diff "$TMP/l0.out" "$TMP/l3.out" | head -4
        fi
    done
    echo "  ℹ 一致 $same 门 · 不同 $diff 门 · 跳过 $skipped 门"
    PASS=$((PASS + same)); FAIL=$((FAIL + diff))
else
    echo "  ❌ 找不到跨语言语料：$LANGS_DIR"; FAIL=$((FAIL + 1))
fi

echo
echo "───────────────"
echo "通过 $PASS · 失败 $FAIL"
[ "$FAIL" -eq 0 ] || exit 1

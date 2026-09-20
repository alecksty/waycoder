#!/usr/bin/env bash
# 校验 `third_party/vml` 的本地适配**全部**有据可查、不会悄悄丢。
#
# ## 分家之后，这个脚本验什么（2026-09-20 重写）
#
# 本副本自 2026-09-17 起是「移动设备手机端专用」的独立分支（见 `third_party/vml/FORK.md`）：
#   · 不再从上游同步 ⇒ `sync.sh` 已删除；
#   · `patches/` 已退役（FORK.md：**没有功能作用**，只剩「我们相对上游改了什么」的历史记录）。
#
# ⇒ 旧判据「vendor + 补丁 == 工作区」的**前提消失了**。它验的是「rsync 会不会把我们的
#    改动冲掉」，而没有 rsync 了。分家后继续跑它，只会把**正当的分家改动**报成红
#    —— 实测：131 个 ✘ 里有 49 个属于这一类（`VMLAssembler`/`VMLPrepares` 的源码改动、
#    `Lib/<语言>/{util,syscall}.vml`、`modules.json`、`waycoder_ui.h`，它们本来就该
#    在树里、本来就不该进补丁）。那批红被当成「一直绿」是危险的：**红得太久就没人看了**。
#
# 现在验三条**分家后仍然有意义**的：
#
# **① `Lib/` 生成物 == 用本仓 GenLib 重生成的结果。**
#    最强的一条：它证明的是 `Lib == f(源码, 前端, GenLib)`，而不是「碰巧没人动过」。
#    改坏了源码 / 前端 / 生成器的任何一环都会在这里现形。
#    （2026-09-20 实测它抓出**签入的 `Lib/shared/*.vml` 出自更早版本的 GenLib** ——
#      那时还不发 `; <行号>: <源码>` 注释，82 个文件对不上。跑一次 `GenLib -A` 即合。）
#
# **② 剪枝清单 == 磁盘现实。**
#    手机端用不到的东西（单片机设备定义、古董机 BIOS、桌面 GPU/GUI 绑定、PC 显存头…）
#    已在 `third_party/vml/PRUNED.txt` 里**声明**删除。两个方向都验：
#      · 清单里的模式必须**真的为空** —— 否则清单在说谎（文件被谁恢复了？）；
#      · git 里登记、磁盘上却没有的文件必须**都在清单里** —— 否则是**意外删除**。
#    参考系是**当前 git 索引**（不是 vendor 提交）：分家后树本身就是真源，
#    「这个文件该不该在」的答案只能来自「我们上次提交时它在不在」。
#
# **③ 手工文件必须被 git 跟踪。**
#    GenLib **不产出**的那些（`modules.json` / `naming.json` / 手写 `.vml` / README …）
#    是**输入**不是产物，丢了没法重生成。分家后保护它们的是 git，所以这里验「已被跟踪」
#    —— 防「新写了一个手工文件却忘了 `git add`」。
#
# 用法: scripts/check-vml-patches.sh
#
# ⚠ 文件名里的 `patches` 已是**历史遗留**（补丁机制退役了）。保留旧名是为了不动
#    FORK.md / docs/ 里的一堆引用；将来要改名就一起改。
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VML="$ROOT/third_party/vml"
TMP="$(mktemp -d)"
fail=0

cleanup() { rm -rf "$TMP"; }
trap cleanup EXIT

PRUNED="$VML/PRUNED.txt"
[ -f "$PRUNED" ] || { echo "✘ 找不到剪枝清单 $PRUNED" >&2; exit 1; }

# 清单：`#` 起注释（含行尾注释）、空行忽略
mapfile -t PRUNED_PATTERNS < <(sed 's/#.*//' "$PRUNED" | sed 's/[[:space:]]*$//' | grep -v '^[[:space:]]*$')
[ "${#PRUNED_PATTERNS[@]}" -gt 0 ] || { echo "✘ 剪枝清单是空的 —— 判据②会空过" >&2; exit 1; }

is_pruned() {
    local p="$1" pat
    for pat in "${PRUNED_PATTERNS[@]}"; do
        case "$p" in $pat) return 0 ;; esac
    done
    return 1
}

echo "── ① Lib：生成物 == 重生成结果 ──"
# ⚠ 只把「非 C 源」的 mtime 拨到 1970。GenLib 的 `-b` 是「`.vml` 比 `.c` 旧才重编」，
#    把 `.c` 一起拨老会让它判定「不比源旧」而**整批跳过** —— 第一版原型就这么假绿过
#    （日志写着「0 编译, 106 跳过」，共享库一个都没重生成，比对自然全过）。
REF="$TMP/.ref"
touch -t 198001010000 "$REF"

cp -Rp "$VML/Lib" "$TMP/Lib"
find "$TMP/Lib" -type f -not -path "*/shared/src/*" -exec touch -t 197001010000 {} +

source "$(dirname "${BASH_SOURCE[0]}")/lib/portable-timeout.sh"
# ⚠ GenLib 要重编 100 个模块，**这份校验脚本本身也可能卡住**（用户要求
#   「编译和测试都要有防卡死机制」）。给一个明显大于合法耗时的时限（实测 100 个
#   模块几十秒，给 600 秒），超时按 124 判 —— 不能让它把校验脚本永久挂住。
if (cd "$VML" && run_with_timeout "${GENLIB_TIMEOUT:-600}" dotnet run --project tools/GenLib -- -A -r "$TMP") > "$TMP/genlib.log" 2>&1; then
    echo "  ✔ GenLib -A 重生成成功（$(grep -a -m1 '完成:' "$TMP/genlib.log" || echo '')）"
else
    echo "  ✘ GenLib -A 重生成失败（或超时 ${GENLIB_TIMEOUT:-600}s）—— Lib/ 与它的输入已经对不上了：" >&2
    tail -15 "$TMP/genlib.log" >&2
    exit 1
fi

# 生成物 = mtime 不再停在 1970 的那些
# ⚠ `.DS_Store` 必须排除：macOS 浏览过目录就会生成、且**未被 git 跟踪**
#    ⇒ 不排除的话「本地新增的手工文件」这条必然误报（已实测踩到）。
find "$TMP/Lib" -type f -not -name .DS_Store -newer "$REF" \
    | sed "s|^$TMP/Lib/||" | sort > "$TMP/gen.txt"
gen_count="$(wc -l < "$TMP/gen.txt" | tr -d ' ')"
[ "$gen_count" -gt 0 ] || { echo "  ✘ 没认出任何生成物 —— 时间基准失效了，这道判据会空过" >&2; exit 1; }

gen_bad=0
while IFS= read -r f; do
    [ -n "$f" ] || continue
    if [ ! -f "$VML/Lib/$f" ]; then
        if is_pruned "Lib/$f"; then
            echo "  ℹ Lib/$f 是生成物但已按清单剪枝（有意删除，跳过）"
        else
            echo "  ✘ Lib/$f 是重生成产出，工作区里却没有" >&2; gen_bad=1
        fi
    elif ! cmp -s "$TMP/Lib/$f" "$VML/Lib/$f"; then
        echo "  ✘ Lib/$f 与重生成结果不一致（跑一次 GenLib -A 就好）" >&2; gen_bad=1
    fi
done < "$TMP/gen.txt"
[ "$gen_bad" -eq 0 ] && echo "  ✔ 生成物 $gen_count 个，与重生成结果逐字节相同" || fail=1

echo "── ② 剪枝清单 == 磁盘现实 ──"
( cd "$VML" && find Lib -type f -not -name .DS_Store ) | sed 's|^Lib/||' | sort > "$TMP/all.txt"
git -C "$ROOT" ls-files third_party/vml/Lib | sed 's|^third_party/vml/Lib/||' | sort > "$TMP/index.txt"

# ②a 清单里的模式必须真的为空
declare -A pat_hits=()
for pat in "${PRUNED_PATTERNS[@]}"; do pat_hits["$pat"]=0; done
while IFS= read -r f; do
    [ -n "$f" ] || continue
    for pat in "${PRUNED_PATTERNS[@]}"; do
        case "Lib/$f" in $pat) pat_hits["$pat"]=$(( ${pat_hits["$pat"]} + 1 )) ;; esac
    done
done < "$TMP/all.txt"

pat_bad=0
for pat in "${PRUNED_PATTERNS[@]}"; do
    n="${pat_hits["$pat"]}"
    if [ "$n" -gt 0 ]; then
        echo "  ✘ 清单说已剪枝，磁盘上却还有 $n 个文件匹配：$pat" >&2
        pat_bad=1
    fi
done
[ "$pat_bad" -eq 0 ] && echo "  ✔ 清单 ${#PRUNED_PATTERNS[@]} 条模式，覆盖的路径全部确已删除" || fail=1

# ②b 被删的必须都在清单里（参考系 = 当前 git 索引，不是 vendor 提交）
undeclared=0
while IFS= read -r f; do
    [ -n "$f" ] || continue
    is_pruned "Lib/$f" && continue
    echo "  ✘ Lib/$f 在 git 里登记、磁盘上却没有，且不在剪枝清单里（意外删除？）" >&2
    undeclared=$((undeclared + 1))
done < <(comm -23 "$TMP/index.txt" "$TMP/all.txt")
if [ "$undeclared" -eq 0 ]; then
    echo "  ✔ 未登记的删除 0 个（git 里有、磁盘上没有的，全在清单里）"
else
    fail=1
fi

echo "── ③ 手工文件必须被 git 跟踪 ──"
comm -23 "$TMP/all.txt" "$TMP/gen.txt" > "$TMP/hand.txt"
untracked=0
while IFS= read -r f; do
    [ -n "$f" ] || continue
    grep -qxF "$f" "$TMP/index.txt" || {
        echo "  ✘ Lib/$f 是手工文件却没被 git 跟踪（忘了 git add？）" >&2
        untracked=$((untracked + 1))
    }
done < "$TMP/hand.txt"
if [ "$untracked" -eq 0 ]; then
    echo "  ✔ 手工文件 $(wc -l < "$TMP/hand.txt" | tr -d ' ') 个，全部已跟踪（丢了没法重生成，靠 git 保）"
else
    fail=1
fi

if [ "$fail" -eq 0 ]; then
    echo "✅ 本地适配全部有据可查（Lib/ 与生成器一致；剪枝清单与磁盘一致；手工文件已跟踪）"
fi
exit "$fail"

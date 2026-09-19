#!/usr/bin/env bash
# VML 编译期**诊断**判据 —— 与 `vml-out-probe` 是**相反极性**的一套，别混在一起。
#
#   `vml-out-probe`：程序**必须编译成功**，且输出逐字节正确。
#   本套：程序**必须编译失败**，且 stderr 里要点名出错的那个标识符。
#
# 为什么必须分开（这是从 out-probe 自己的注释里学来的教训）：那一套早期把
# 「编译期抛异常」与「跑完输出不对」混成一档，于是 5 个**编译失败**被报成
# 「输出为空」，害得"修编译器"和"修探针"混在一起看不出来。
# 反之亦然 —— 把"必须失败"的用例塞进 out-probe，它会把**正确行为判成 FAIL**。
#
# 用法：
#   scripts/vml-diag-probe/run.sh            # 全部
#   scripts/vml-diag-probe/run.sh link-clean # 只跑某组
#   scripts/vml-diag-probe/run.sh undef-fn c # 某组里只跑 .c
#
# 依赖桌面的 `scripts/vmlcli`（与手机端等价的编译流水线）。

set -uo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
DLL="${VMLCLI:-$REPO/scripts/vmlcli}/bin/Release/net10.0/vmlcli.dll"
TIMEOUT="${TIMEOUT:-30}"

[ -f "$DLL" ] || { echo "✘ 找不到 vmlcli：$DLL（先 dotnet build scripts/vmlcli -c Release）" >&2; exit 2; }

# ⚠ `timeout` 是 GNU coreutils 的，macOS 默认没有 —— 缺了它会整条命令行 command not found、
#   抓到空输出、**所有用例一起报错**，看上去像"全坏了"（`vml-out-probe/run-langs.sh` 上踩过）。
#   用函数而不是数组：macOS 自带 bash 3.2，空数组配 `set -u` 展开会报 unbound variable。
if command -v timeout >/dev/null 2>&1; then TIMEOUT_BIN=timeout
elif command -v gtimeout >/dev/null 2>&1; then TIMEOUT_BIN=gtimeout
else TIMEOUT_BIN=""; fi
run_probe() {
    if [ -n "$TIMEOUT_BIN" ]; then "$TIMEOUT_BIN" $((TIMEOUT + 20)) "$@"; else "$@"; fi
}

pass=0; fail=0; crash=0
failed=(); crashed=()

# ── 组一：link-clean ────────────────────────────────────────────────────────
# **误报为零**的持续保证：正常程序编译时，链接器不该打出任何「未解析标签」。
#
# 为什么这条重要：链接器里那个 `ReportUnresolved` 会把「调用了一个不存在的函数」
# 全列出来（它本来就写好了、只是当警告打）。要让「未定义函数」变成编译期硬错误，
# 前提就是**正常程序的这个清单必须为空** —— 否则一升档，正常程序全编不过。
#
# ⚠ 判的是**用户档**（"错误: 用户代码里有 N 处调用指向不存在的函数"），不是总数。
#
# 链接器现在按**指令来源**分两档报（`LibraryLinker.ReportUnresolved` 的 `userEnd` 索引边界）：
#   · **用户档** = 前端为你这份源码产出的指令里的未解析调用 ⇒ 就是**你写错了一个函数名**，
#     这一档必须恒为 0，也是"未定义函数升级成编译期硬错误"的前提。
#   · **库档** = 链接进来的库模块内部的未解析调用 ⇒ 历史遗留的**死包装器**
#     （`Lib/pascal/console.vml` 的 `PASCAL_PRINT_LONG` 调 `call print_long`，
#      而 `io64` 模块没被 auto-link 拉进来 —— 目标函数是真的，只是没链上）。
#     这些路径从没被执行过，所以一直没暴露。**归 Tier 2 待办，不拦本次。**
#
# 2026-09-19 实测：**用户档 22 门全是 0**；库档只有 4 门非零（bas 33 / cs 32 / ld 32 / pas 32），
# 其余 18 门库档也是 0。清 `modules.json` 里 56 个虚构条目之前，库档是 60/49/49/49
# —— 那一轮把**虚构**的那部分清掉了，剩下的是**真实但没链上**的那部分。
group_link_clean() {
    local filter="${1:-}"
    shopt -s nullglob
    local files=("$REPO"/scripts/vml-out-probe/langs/out.*)
    shopt -u nullglob
    for f in "${files[@]}"; do
        [[ "$f" == *.expect ]] && continue
        [[ -n "$filter" && "${f##*.}" != "$filter" ]] && continue
        local errf; errf="$(mktemp)"
        ( cd "$(dirname "$f")" && run_probe dotnet "$DLL" "$f" --vml /tmp/_diagprobe.vml ) >/dev/null 2>"$errf"
        local u l
        u="$(grep -ao '用户代码里有 [0-9]* 处' "$errf" | head -1 | grep -o '[0-9]*')"
        l="$(grep -ao '库代码里有 [0-9]* 个' "$errf" | head -1 | grep -o '[0-9]*')"
        if [ -z "$u" ] || [ "$u" = "0" ]; then
            printf '%-12s PASS   库档 %s\n' "$(basename "$f")" "${l:-0}"; pass=$((pass + 1))
        else
            printf '%-12s FAIL   用户档 %s 处（库档 %s）\n' "$(basename "$f")" "$u" "${l:-0}"
            fail=$((fail + 1)); failed+=("$(basename "$f")")
        fi
        rm -f "$errf"
    done
}

# ── 组二：undef-fn（未定义函数必须**编译期**报错）────────────────────────────
#
# 三档判定（**CRASH 必须单列**）：这正是"修复没生效、只是换个地方崩"的伪装形态 ——
# 今天的行为就是「编译通过 → 运行期 KeyNotFoundException: 未找到标签」。
# 只能判「没编过」的话，那种形态也会被算成 PASS。
#
#   PASS  = 退出码非 0，stderr 里同时有「编译失败/error:」**和**用例点名的标识符，
#           且**没有** Unhandled exception / KeyNotFoundException / VmlLabelException
#   CRASH = 出现上面那三种异常（= 还是在运行期/链接期崩，不是干净的编译期诊断）
#   FAIL  = 编译"成功"了（退出码 0）
group_undef_fn() {
    local filter="${1:-}"
    local any=0
    shopt -s nullglob
    local files=("$HERE"/cases/undef-fn.*)
    shopt -u nullglob
    for f in "${files[@]}"; do
        [[ "$f" == *.expect ]] && continue
        [[ -n "$filter" && "${f##*.}" != "$filter" ]] && continue
        any=1
        # 用例同目录下放一个 `<用例>.sym`，里面写"必须被点名的标识符"
        local sym; sym="$(cat "$f.sym" 2>/dev/null || echo "nosuch")"
        local errf out; errf="$(mktemp)"; out="$(mktemp)"
        local rc=0
        ( cd "$(dirname "$f")" && run_probe dotnet "$DLL" "$(basename "$f")" --timeout 5 ) >"$out" 2>"$errf" || rc=$?
        local verdict brief
        if grep -qaE 'Unhandled exception|KeyNotFoundException|VmlLabelException' "$errf"; then
            verdict="CRASH"; crash=$((crash + 1)); crashed+=("$(basename "$f")")
            brief="$(grep -aoE 'KeyNotFoundException|VmlLabelException|Unhandled exception' "$errf" | head -1)"
        elif [ "$rc" -eq 0 ]; then
            verdict="FAIL"; fail=$((fail + 1)); failed+=("$(basename "$f")")
            brief="编译通过了（应编译失败）"
        elif ! grep -qa "$sym" "$errf"; then
            verdict="FAIL"; fail=$((fail + 1)); failed+=("$(basename "$f")")
            brief="失败但没点名 '$sym'"
        else
            verdict="PASS"; pass=$((pass + 1)); brief="点名 '$sym'"
        fi
        rm -f "$errf" "$out"
        printf '%-14s %-6s %s\n' "$(basename "$f")" "$verdict" "$brief"
    done
    [ "$any" -eq 1 ] || echo "（undef-fn 组还没有用例 —— P2 阶段补）"
}

# ── 组三：dyn-global（动态语言必须**仍然编得过**）────────────────────────────
# 6 门动态语言（js/lua/py/r/rb/scm）的「未声明即隐式全局」是**合法语义**，
# 不是缺陷。本组就是那道反向护栏：豁免改错了会在这里变红。
group_dyn_global() {
    local filter="${1:-}"
    local any=0
    shopt -s nullglob
    local files=("$HERE"/cases/dyn-global.*)
    shopt -u nullglob
    for f in "${files[@]}"; do
        [[ -n "$filter" && "${f##*.}" != "$filter" ]] && continue
        any=1
        local errf; errf="$(mktemp)"
        local rc=0
        ( cd "$(dirname "$f")" && run_probe dotnet "$DLL" "$(basename "$f")" --timeout 10 2>"$errf" ) >/dev/null || rc=$?
        if [ "$rc" -eq 0 ] && ! grep -qaE 'Unhandled exception|error:' "$errf"; then
            printf '%-16s PASS\n' "$(basename "$f")"; pass=$((pass + 1))
        else
            printf '%-16s FAIL   隐式全局被拒了\n' "$(basename "$f")"
            fail=$((fail + 1)); failed+=("$(basename "$f")")
        fi
        rm -f "$errf"
    done
    [ "$any" -eq 1 ] || echo "（dyn-global 组还没有用例 —— P6 阶段补）"
}

GROUP="${1:-all}"
FILTER="${2:-}"

printf '%-14s %-6s %s\n' "用例" "结果" "说明"
printf '%s\n' "--------------------------------------------------------------"
case "$GROUP" in
    link-clean) group_link_clean "$FILTER" ;;
    undef-fn)   group_undef_fn "$FILTER" ;;
    dyn-global) group_dyn_global "$FILTER" ;;
    all)
        echo "── link-clean（正常程序不该有未解析标签）──"
        group_link_clean "$FILTER"
        echo "── undef-fn（未定义函数必须编译期报错）──"
        group_undef_fn "$FILTER"
        echo "── dyn-global（动态语言的隐式全局必须仍然合法）──"
        group_dyn_global "$FILTER"
        ;;
    *) echo "✘ 未知的组：$GROUP（可选 link-clean / undef-fn / dyn-global）" >&2; exit 2 ;;
esac
printf '%s\n' "--------------------------------------------------------------"
echo "通过 $pass / 不符 $fail / **崩溃 $crash**"
[ ${#failed[@]}  -gt 0 ] && echo "不符：${failed[*]}"
[ ${#crashed[@]} -gt 0 ] && echo "⚠ 仍在运行期/链接期崩（不是干净的编译期诊断）：${crashed[*]}"
[ $fail -gt 0 ] || [ $crash -gt 0 ] && exit 1
exit 0

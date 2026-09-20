#!/usr/bin/env bash
# `Examples/` 全量**编译**检查 —— 只判"能不能编过"，不跑（很多是交互式游戏，跑了只会超时）。
#
# 为什么要有这一层（`vml-out-probe` 之外）：
#   那一套是**每门语言一条十几行的最小程序**。它照不出
#   「真实例子引用了一个没被 auto-link 拉进来的库函数」这类问题 ——
#   而那正是一整类缺陷的形态（库函数名在映射表里缺一条，例子就编不过）。
#
#   **这不是假设**：把「未定义函数」从警告升成编译期硬错误（v0.96.268）之后，
#   `vml-out-probe` 29/29 全绿、`link-clean` 22/22 全绿，看着"零误伤"；
#   可 `Examples/` 里当场炸出 **7 个**：`asm`（csharp/java/ruby/swift/r）、
#   `lua_table_set/get`（lua）、`sub_ui_call_json_s`（fortran）、`shared_file_test`（objc）、
#   `wend_56`（r）。语料只铺一层，结论就只在那一层成立。
#
# 用法：scripts/vml-diag-probe/examples-build.sh
#
# ⚠ 关于 `timeout`（2026-09-20 修订，用户要求「编译和测试都要有防卡死机制」）：
#
#   原先这里写的是「**别用** `timeout`」—— 理由是它是 GNU coreutils 的，macOS 默认没有，
#   缺了会整条命令 `command not found` ⇒ 输出为空 ⇒ **每个例子都被误判成"通过"**
#   （第一版报「通过 90 / 失败 0」，去掉 `timeout` 重跑才看到 14 个失败）。
#
#   那条结论是**半对的**：它诊断对了"缺 timeout ⇒ 假绿"，但开出的方子是"别用超时"，
#   而**没有超时的下场是把整个套件挂死**——一份让前端空转的源码（本仓真出过，
#   见 `FRONTEND_DEFECTS.md` 的 Dart 那条）会让这里永远跑不完。
#   ⇒ 正解是两条都堵上：
#     · `timeout`/`gtimeout` **有就用**（快、可靠）；
#     · 没有就用手写的 POSIX 版（`&` + `wait` + 定时 `kill`），**两条路都真会超时**；
#     · 而且**超时按退出码单独判 FAIL**，不靠 grep 输出 ——
#       被杀掉时 stderr 里没有「编译失败」字样，靠 grep 只会把它读成"通过"（假绿的老病根）。
source "$(dirname "${BASH_SOURCE[0]}")/../lib/portable-timeout.sh"
EX_TIMEOUT="${EX_TIMEOUT:-120}"   # 单个例子的编译时限（秒）。桌面实测最慢几秒，120 足够宽松。

set -uo pipefail
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DLL="${VMLCLI:-$REPO/scripts/vmlcli}/bin/Release/net10.0/vmlcli.dll"

[ -f "$DLL" ] || { echo "✘ 找不到 vmlcli：$DLL" >&2; exit 2; }

ok=0; fail=0; failed=()
for f in "$REPO"/third_party/vml/Examples/*/*; do
    [ -f "$f" ] || continue
    # ── 已知**按设计**编不过的（不是缺陷，是孤儿存根）───────────────────────────
    # `Examples/*/file_io.*`（csharp / java / ruby / r / swift / objc）是一组
    # **SharedLib 演示存根**：内容是 `asm("CALL shared_file_test")` + `asm("LOAD R0 #100")`
    # + `asm("SYSCALL 3")`，靠**内嵌汇编**去调一个配套的共享库。
    # 两个理由让它们编不过，**都不该修成能编**：
    #   ① 新版**只在 C 类语言保留内嵌汇编**，其余语言取消（改为"只能调 C 写好的库"）
    #      ⇒ `asm("…")` 在这些语言上本来就已经不是支持的能力了，编不过是对的；
    #   ② 被调的那个 `shared_file_test` **在整个仓库里没有任何地方定义**
    #      （`Examples/SharedLib/` 下只有一个 build 脚本和一个预编好的 `test_mylib_c.vml`）
    #      ⇒ 就算把 asm 换成普通调用，也还是缺实现。
    # 结论：它们不是"坏了的例子"，是**已经不成立的例子**。留着当红灯只会训练人去忽略红灯。
    # （是重写成"调 C 库"的现代写法、还是删掉，由仓库决定 —— 这里先显式排除。）
    case "$f" in
        # 非源码
        *.md|*.txt|*.h|*.json|*.sh|*.bat|*.ps1|*.xml|*.zip) continue ;;
        # ⚠ `file_io.js` 与上面六个**是同一类**（同样是 `asm("CALL shared_file_test")`），
        #   此前漏在清单外，靠 JS 那条"发一条指向不存在变量的间接调用"的缺陷**假绿**通过 ——
        #   v0.96.282 把那个静默缺陷改成硬报错之后它才现形。补进排除。
        */file_io.cs|*/file_io.java|*/file_io.rb|*/file_io.r|*/file_io.swift|*/file_io.m|*/file_io.js) continue ;;
        # ⚠ `.vml` 是**已经编好的汇编**，不是源码；喂给前端只会得到"认不出扩展名"
        #   （`OpenCV/cv_demo.vml` / `SharedLib/test_mylib_c.vml` 就是这么被误报的）
        *.vml) continue ;;
        # `.gen.vml` 中间产物
        *.gen.vml) continue ;;
    esac
    # ⚠ 分两步取「输出」与「退出码」—— `$(...)` 会把退出码吃掉，
    #   而**超时必须按退出码判**（被杀掉时输出里没有「编译失败」字样，靠 grep 会读成通过）。
    out="$(cd "$(dirname "$f")" && run_with_timeout "$EX_TIMEOUT" dotnet "$DLL" "$(basename "$f")" --vml /tmp/_exbuild.vml 2>&1)"
    rc=$?
    if [ "$rc" -eq 124 ]; then
        printf 'FAIL %s  （**超时**：%s 秒没返回 —— 编译器卡死）\n' \
            "$(echo "$f" | sed 's|.*/Examples/||')" "$EX_TIMEOUT"
        fail=$((fail+1)); failed+=("$(echo "$f" | sed 's|.*/Examples/||') (超时)")
        continue
    fi
    err="$(printf '%s' "$out" | grep -a "编译失败\|error:" | head -2)"
    if [ -n "$err" ]; then
        printf 'FAIL %s\n' "$(echo "$f" | sed 's|.*/Examples/||')"
        printf '%s\n' "$err" | sed 's/^/     /'
        fail=$((fail + 1)); failed+=("$(echo "$f" | sed 's|.*/Examples/||')")
    else ok=$((ok + 1)); fi
done

echo "--------------------------------------------------------------"
echo "通过 $ok / 失败 $fail"
[ $fail -gt 0 ] && { echo "失败：${failed[*]}"; exit 1; }
exit 0

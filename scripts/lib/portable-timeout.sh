#!/usr/bin/env bash
# 便携超时 —— **所有判据脚本共用的唯一实现**（`source` 进来用）。
#
# ## 为什么要有这个文件
#
# 本仓的判据脚本都要驱动编译器/跑程序，而"编译器卡死"是真出过的事
# （`FRONTEND_DEFECTS.md` 的 Dart 那条：一份写了一半的源码让前端**永不返回**）。
# 没有超时的下场不是"报错"，而是**整个套件挂死** —— CI 上表现为跑不完，
# 本地表现为"看着像卡住"（实际可能是管道缓冲，见下）。
#
# ## 三条踩过的坑，都写进这段代码里
#
# ① **`timeout` 是 GNU coreutils 的，macOS 默认没有**。
#    缺了它直接 `command not found` ⇒ 输出为空 ⇒ 调用方若靠 grep 输出判成败，
#    **每一个用例都会被误判成"通过"**（`examples-build.sh` 第一版报「通过 90 / 失败 0」
#    就是这么来的）。这是最危险的一种失败：它给的是假的信心。
#    ⇒ 有 `timeout`/`gtimeout` 就用，没有就用手写的 POSIX 版（`&` + `wait` + 定时 `kill`）。
#
# ② **超时必须按退出码判，不能靠 grep 输出**。被杀掉时 stderr 里没有任何"失败"字样，
#    靠输出判会把超时读成成功。本库把超时**统一归一成 124**（GNU 的约定），
#    调用方只判一个数 —— `[ "$rc" -eq 124 ]`。
#
# ③ **`$(...)` 会吃掉退出码**，所以调用方必须写成两步：
#        out="$(run_with_timeout 60 dotnet ... 2>&1)"; rc=$?
#
# ## 用法
#
#     source "$(dirname "${BASH_SOURCE[0]}")/../lib/portable-timeout.sh"
#     out="$(run_with_timeout 120 dotnet "$DLL" "$f" 2>&1)"; rc=$?
#     if [ "$rc" -eq 124 ]; then ...超时...; fi
#
# ⚠ 阈值别抄一个数了事：**要明显大于合法耗时**（本仓手机端编译看门狗用 180 秒，
#   桌面单个例程几秒，所以桌面给 120 秒）。给得太小 = 把正常的慢判成卡死。

TIMEOUT_BIN=""
if command -v timeout >/dev/null 2>&1; then TIMEOUT_BIN=timeout
elif command -v gtimeout >/dev/null 2>&1; then TIMEOUT_BIN=gtimeout
fi

# run_with_timeout <秒> <命令...>
# 超时返回 124；其余情况原样返回被调命令的退出码。
run_with_timeout() {
    local secs="$1"; shift
    if [ -n "$TIMEOUT_BIN" ]; then
        "$TIMEOUT_BIN" "$secs" "$@"
        return $?
    fi
    # POSIX 手写版：后台跑 + 定时器到点杀。**必须 kill -9**
    #（前端卡在纯计算循环里时不响应 TERM）。
    "$@" &
    local pid=$!
    ( sleep "$secs"; kill -9 "$pid" 2>/dev/null ) &
    local watcher=$!
    wait "$pid"
    local rc=$?
    kill "$watcher" 2>/dev/null
    wait "$watcher" 2>/dev/null
    [ "$rc" -eq 137 ] && rc=124          # 被我们杀掉 = 超时（归一成 GNU 的约定）
    return "$rc"
}

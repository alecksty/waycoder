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

# ── 全量预算（「整套跑下来最多花多久」）──────────────────────────────────────
#
# ⚠ 单次超时**只保证"每一项"有界**，不保证"一整轮"有界：83 个例程每个都刚好耗掉
#   119 秒（阈值 120）就是 2.7 小时 —— 一项都没超时，整轮却不可用。用户的要求是
#   「**全量**测试编译**有可能**卡住或者死循环」⇒ 全量这一层也要有自己的护栏。
#
# 判据：**墙钟预算**（默认 1800 秒）。每调一次 `run_with_timeout` 前检查一次：
#   · 超了 ⇒ 打一行说明（含已用时/预算/建议）然后 `exit 3`（**不是 1**：
#     "预算用尽"与"用例失败"是两种结论，混在一个退出码里下游分不出来）；
#   · 没超 ⇒ 把**单次时限收紧到"剩余预算"**，于是最后的尾巴不会又各跑一整个阈值。
#
# ⚠ **预算要按健康时长定尺寸**。第一版给的是 1800 秒（30 分钟），而本仓桌面全套例程
#   健康时长约 3.5 分钟 —— 10 倍余量意味着"真出问题也要等半小时它才说话"，
#   那种护栏等于没有（用户当场就指出了这一点：等待时间本身就是成本）。
#   现在给 600 秒 ≈ 健康时长的 3 倍：慢机器不假红，真卡住十分钟内一定停。
PROBE_BUDGET="${PROBE_BUDGET:-600}"
PROBE_START="$(date +%s)"

# ── 连续超时 trip（**系统性问题**的即时判据）────────────────────────────────
#
# 预算管的是"总共等多久"，但**单看某一条超时说明不了什么**（可能就那一个例程坏）。
# 而"连着 N 条都超时"是另一种结论：不是某个用例的问题，是**编译器/环境整体挂了** ——
# 这时候继续把剩下 80 条各等一遍毫无意义，应当**立刻停**。
# ⚠ 与预算分开是有意的：它们回答两个不同的问题（"等够了没" vs "是不是全坏了"），
#   混成一个判据会让报错说不清是哪一种。
PROBE_TRIP_LIMIT="${PROBE_TRIP_LIMIT:-3}"
PROBE_TRIP_COUNT=0

# 每次**非超时**结果都要调一次（超时由 trip_on_timeout 自己记账）。
trip_reset() { PROBE_TRIP_COUNT=0; }

# 每次超时调一次；连续超时达到上限就打印原因并 exit 4。
trip_on_timeout() {
    PROBE_TRIP_COUNT=$((PROBE_TRIP_COUNT + 1))
    if [ "$PROBE_TRIP_COUNT" -lt "$PROBE_TRIP_LIMIT" ]; then return 0; fi
    {
        echo ""
        echo "✘ 连续 ${PROBE_TRIP_COUNT} 条超时（上限 ${PROBE_TRIP_LIMIT}）—— 这不是某个用例坏了，"
        echo "  是**整体挂了**（前端卡死 / 环境异常 / 链接库坏了）。继续跑完剩下的毫无意义，就地停。"
        # ⚠ 双引号里**不能出现反引号** —— bash 会把它当命令替换执行
        #   （本仓早记过这条：`run-langs.sh` 头部那条警告）。用「」代替。
        echo "  ⇒ 单独跑最后那一条复现（PROBE_BUDGET / 单次时限都可调），"
        echo "    并看 third_party/vml/FRONTEND_DEFECTS.md 的「防卡死」一节。"
    } >&2
    exit 4
}

# elapsed_text —— 给汇总行用：**让"跑了多久"从日志里读得出来**，不靠人回忆。
elapsed_text() { echo "$(( $(date +%s) - PROBE_START ))s（预算 ${PROBE_BUDGET}s）"; }

# budget_check —— **由调用方在每一轮循环的开头调一次**。
#
# ⚠ 为什么不是塞进 `run_with_timeout` 里（第一版就是那么写的，**实测不生效**）：
#   调用点长这样 —— `out="$(cd … && run_with_timeout … 2>&1)"`，
#   那是一个**命令替换的子 shell**，里面的 `exit 3` 只结束那个子 shell，
#   主循环照跑不误（实测 `PROBE_BUDGET=5` 跑完了全部 83 项）。
#   预算要能真正中断整轮，**只能在主 shell 里 exit** ⇒ 单独一个函数，由主循环调。
#
# 判据：墙钟超预算 ⇒ 打印说明 + `exit 3`。
# ⚠ 退出码用 **3 而不是 1**：「预算用尽」与「用例失败」是两种结论，
#   混在一个码里下游分不出来（CI 会把它读成"有失败用例"，而真相是"没跑完"）。
budget_check() {
    local elapsed=$(( $(date +%s) - PROBE_START ))
    if [ "$elapsed" -lt "$PROBE_BUDGET" ]; then return 0; fi
    {
        echo ""
        echo "✘ 全量预算用尽：已用 ${elapsed}s ≥ 预算 ${PROBE_BUDGET}s（PROBE_BUDGET 可调）。"
        echo "  这不一定是死循环，但**必须停下来** —— 否则整轮永远跑不完，"
        echo "  而「跑不完」与「卡死」在 CI 上看起来一模一样。"
        echo "  ⇒ 先看最后一条进度输出停在哪，再单独跑那一个用例（多数是前端卡住，"
        echo "    见 third_party/vml/FRONTEND_DEFECTS.md 的「防卡死」一节）。"
    } >&2
    exit 3
}

# budget_clamp <想要的秒数> —— 回显**"在剩余预算内实际能用"的秒数**（至少 1）。
#
# 用途：调用方要在**报错文案里说出自己那一刀实际等了多久**时，先调它拿数。
# 「归因错了就等于报错不对」—— 预算把 120 秒夹到 1 秒时，用例确实会超时，
# 但那时报「120 秒没返回」是假话（它只等了 1 秒），会把排查引向"编译器卡死"这个错方向。
budget_clamp() {
    local want="$1"
    local left=$(( PROBE_BUDGET - ( $(date +%s) - PROBE_START ) ))
    [ "$left" -lt 1 ] && left=1
    if [ "$want" -gt "$left" ]; then echo "$left"; else echo "$want"; fi
}

# run_with_timeout <秒> <命令...>
# 超时返回 124；其余情况原样返回被调命令的退出码。
run_with_timeout() {
    local secs="$1"; shift
    # 单次时限不越过**剩余**预算，免得最后一项又各跑一整个阈值。
    # ⚠ 必须**夹到 ≥1 秒**：剩余预算可能算出 0 或负数，
    #   而 `timeout -3` 是个错误的命令行（实测把正常用例误判成"超时"）。
    local left=$(( PROBE_BUDGET - ( $(date +%s) - PROBE_START ) ))
    [ "$left" -lt 1 ] && left=1
    # ⚠ 这里也夹一次是**兜底**（调用方可能没夹）。但调用方**想知道自己那一刀实际用了多少**
    #   就不能靠这里回传 —— 调用点通常是 `out="$( … run_with_timeout … )"`，
    #   那是**命令替换的子 shell**，在里面赋的变量传不回父 shell（实测
    #   `set -u` 下直接 `unbound variable`）。所以另给一个 `budget_clamp`，
    #   由调用方**在父 shell 里**先把时限算出来、报错时引用它。
    if [ "$secs" -gt "$left" ]; then secs="$left"; fi
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

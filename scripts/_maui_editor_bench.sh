#!/usr/bin/env bash
# 移动端编辑器「滚动性能」基准 —— 可复现的前后对比。
#
# 为什么需要它：`adb shell input swipe` 产生的滚动比人手可复现得多，
# 而 `dumpsys gfxinfo framestats` 能把一帧拆成「App 自己绘制」与「其它」，
# 这是判断「卡在哪」的唯一客观依据（手感只能定性）。
#
# 用法：
#   1) 先在手机上把编辑器打开到某个文件（本脚本不负责导航）
#   2) ./scripts/_maui_editor_bench.sh [滑动次数]
#
# 输出：
#   - gfxinfo 汇总（卡顿比例 / 各分位帧时长）
#   - 逐帧 frame stats（App 的 Draw 耗时 —— 这是要盯的数）
#   - 若 App 打了 WCPERF 诊断日志，一并把分段耗时打出来
set -uo pipefail

SERIAL="${SERIAL:-cd53cb14}"
PKG="${PKG:-com.companyname.waycoder.maui}"
N="${1:-6}"

adb -s "$SERIAL" logcat -c 2>/dev/null
adb -s "$SERIAL" shell dumpsys gfxinfo "$PKG" reset >/dev/null 2>&1

echo "▶ 滑动 $N 次（每次 350ms，滑动区间 y 1600→600）"
for _ in $(seq 1 "$N"); do
  adb -s "$SERIAL" shell input swipe 540 1600 540 600 350 >/dev/null 2>&1
done
sleep 3

echo
echo "═══ gfxinfo 汇总 ═══"
adb -s "$SERIAL" shell dumpsys gfxinfo "$PKG" 2>/dev/null \
  | grep -iE 'Total frames|Janky frames:|50th percentile|90th percentile|95th percentile|99th percentile' | head -6

echo
echo "═══ 逐帧：App Draw 耗时（DrawStart→SyncQueued）═══"
adb -s "$SERIAL" shell dumpsys gfxinfo "$PKG" framestats 2>/dev/null \
  | sed -n '/---PROFILEDATA---/,/---PROFILEDATA---/p' \
  | awk -F, 'NR>1 && NF>20 && $9+0>0 {
      d=($14-$9)/1e6; n++; sd+=d; if(d>md)md=d;
    } END {
      if(n>0) printf "帧数=%d  平均 Draw=%.0fms  最大 Draw=%.0fms\n", n, sd/n, md;
      else print "(无帧数据 —— 确认 App 在编辑器界面且确实滚动了)"
    }'

echo
echo "═══ WCPERF 分段诊断（若版本带该日志）═══"
adb -s "$SERIAL" logcat -d -s WCPERF 2>/dev/null | tail -12

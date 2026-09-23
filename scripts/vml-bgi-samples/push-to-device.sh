#!/usr/bin/env bash
# 把 BGI 样例推到**已连接设备**的工作区（手机 / 模拟器通用）。
#
# 用法：
#   bash .scratch/bgi/push-to-device.sh              # 自动挑唯一在线的设备
#   bash .scratch/bgi/push-to-device.sh <serial>     # 指定设备（adb devices 里的那一列）
#
# ⚠ **只推程序文件，不动 App**。手机上能不能跑取决于 App 版本：
#    BGI 垫层（`Lib/c/graphics.h`）是 **v0.96.359** 进的仓库，更老的版本
#    `#include <graphics.h>` 直接编不过。
#    查手机上装的版本：`adb shell dumpsys package com.tanso.waycoder | grep versionName`
#
# ⚠ 别顺手 `adb install -r` 装本机打的包：本机 keystore 与手机上生效的那把
#    多半不是同一个（仓库钉在 WayCoder.Maui/keystore.sha256），装不上，
#    而"卸载重装"会**删掉手机上的 API Key 与会话**。
set -euo pipefail

SERIAL="${1:-}"
ADB=(adb)
if [[ -n "$SERIAL" ]]; then ADB=(adb -s "$SERIAL"); fi

WS=/storage/emulated/0/waycoder/workspace
HERE="$(cd "$(dirname "$0")" && pwd)"
# ⚠ **样例源码不随仓库走**（`.scratch/bgi/real/` 是抓来的老程序，未跟踪）。
#    本脚本会尽力把它们一起推过去；没有就只推随仓库的 `keygame.c`。

echo "设备："
"${ADB[@]}" devices -l | sed -n '2,$p'
echo "手机上的 App 版本："
"${ADB[@]}" shell dumpsys package com.tanso.waycoder 2>/dev/null | grep -m1 versionName || echo "  （没装 / 读不到）"

"${ADB[@]}" shell mkdir -p "$WS/examples/bgi" >/dev/null

push() {  # push <本地文件> <远端名>
    "${ADB[@]}" push "$1" "$WS/examples/bgi/$2" >/dev/null && echo "  ✔ $2"
}

# 绘图样例（只验画面）—— 抓来的老程序放在 `.scratch/bgi/real/`（未跟踪）
REAL="$HERE/../../.scratch/bgi/real"
for f in pie.cpp Hut.cpp smile.cpp Concentric.cpp barChart.cpp; do
    [[ -f "$REAL/$f" ]] && push "$REAL/$f" "$f"
done
# 键盘判据（验输入；需要 App ≥ v0.96.387 才有那条修好的 getch）
push "$HERE/keygame.c" keygame.c

echo
echo "到手机上跑（命令行页）："
echo "  vml run examples/bgi/pie.cpp"
echo "  vml run examples/bgi/keygame.c      # 方向键移动红块、q 退出"

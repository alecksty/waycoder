#!/bin/bash
# 用法: ./_run.sh <页面> [虚拟时间ms]
# 用 headless Chrome 打开页面，输出标题 + <pre> 文本，供命令行核对。
PAGE="${1:-_selftest.html}"
BUDGET="${2:-60000}"
CHROME="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
DIR="$(cd "$(dirname "$0")" && pwd)"
"$CHROME" --headless=new --no-sandbox --mute-audio \
  --use-gl=angle --use-angle=swiftshader --enable-unsafe-swiftshader \
  --virtual-time-budget="$BUDGET" --dump-dom "http://localhost:8777/$PAGE" 2>/dev/null \
  | python3 "$DIR/_extract.py"

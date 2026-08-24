#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 快速单模型测试
# 用法: ./scripts/quick-test.sh <模型名> ["自定义提示词"]
# 示例: ./scripts/quick-test.sh deepseek-v4-flash
#       ./scripts/quick-test.sh gpt-5.4-mini "写一个计算器"
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

MODEL="${1:?用法: quick-test.sh <模型名> [提示词]}"
PROMPT="${2:-写一个 C# 控制台程序，经典贪吃蛇游戏，支持 WASD 控制，纯 ANSI 渲染，300 行以内。用 write_file 写入文件。}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
OUTPUT_DIR="$REPO_DIR/quick-test-output"
mkdir -p "$OUTPUT_DIR"

LOG_FILE="$OUTPUT_DIR/${TIMESTAMP}_${MODEL//:/_}.log"

echo "═══════════════════════════════════════════"
echo "  WayCoder 快速测试"
echo "  模型: $MODEL"
echo "  日志: $LOG_FILE"
echo "═══════════════════════════════════════════"
echo ""

# 编译
cd "$REPO_DIR/WayCoder"
echo "🔨 编译中..."
dotnet publish -c Release -o "$REPO_DIR/publish" --nologo -v q 2>&1 | tail -1
echo ""

echo "🚀 开始测试..."
START=$(date +%s)

"$REPO_DIR/publish/WayCoder.exe" \
  -m "$MODEL" \
  -p "$PROMPT" \
  --yolo \
  2>&1 | tee "$LOG_FILE"

END=$(date +%s)
ELAPSED=$((END - START))

echo ""
echo "───────────────────────────────────────────"
echo "⏱ 总耗时: ${ELAPSED}s"
echo "📄 日志: $LOG_FILE"
echo "───────────────────────────────────────────"

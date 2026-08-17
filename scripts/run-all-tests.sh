#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 全模型跑分 — 云端 + 本地一站式
# 用法: ./scripts/run-all-tests.sh [--local] [--all]
#   --local  只测本地模型
#   --all    云端 + 本地全部
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
MODE="${1:-cloud}"

echo "══════════════════════════════════════════════════════════"
echo "  WayCoder 全模型跑分"
echo "  模式: $MODE"
echo "  开始: $(date '+%Y-%m-%d %H:%M:%S')"
echo "══════════════════════════════════════════════════════════"
echo ""

case "$MODE" in
  cloud)
    echo "## 云端模型测试"
    for LEVEL in easy medium hard; do
      echo ""
      echo "### 级别: $LEVEL ###"
      bash "$SCRIPT_DIR/bench-models.sh" "$LEVEL"
    done
    ;;

  local|--local)
    echo "## 本地模型测试"
    for LEVEL in easy medium; do
      echo ""
      echo "### 级别: $LEVEL ###"
      bash "$SCRIPT_DIR/bench-local.sh" "$LEVEL"
    done
    ;;

  all|--all)
    echo "## 第一阶段: 云端模型"
    for LEVEL in easy medium hard; do
      bash "$SCRIPT_DIR/bench-models.sh" "$LEVEL"
    done

    echo ""
    echo "## 第二阶段: 本地模型"
    for LEVEL in easy medium; do
      bash "$SCRIPT_DIR/bench-local.sh" "$LEVEL"
    done
    ;;

  *)
    echo "用法: run-all-tests.sh [cloud|local|all]"
    exit 1
    ;;
esac

echo ""
echo "══════════════════════════════════════════════════════════"
echo "  全部完成！$(date '+%Y-%m-%d %H:%M:%S')"
echo "  结果: $(dirname "$SCRIPT_DIR")/bench-results/"
echo "══════════════════════════════════════════════════════════"

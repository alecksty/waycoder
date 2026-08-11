#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 多模型性能基准测试
# 用法: ./scripts/bench-models.sh [任务级别: easy|medium|hard]
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
RESULT_DIR="$REPO_DIR/bench-results"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LEVEL="${1:-medium}"

# ── 任务定义 ──
case "$LEVEL" in
  easy)
    TASK="写一个 C# 控制台程序 SnakeGame.cs，经典贪吃蛇游戏，支持 WASD，100 行以内。用 write_file 直接写入文件。"
    ;;
  medium)
    TASK="写一个 C# 控制台程序 TetrisAttack.cs，经典俄罗斯方块对战游戏：7种方块 + SRS旋转 + 重力 + 消行计分，300行以内，纯ANSI渲染。用 write_file 写入。"
    ;;
  hard)
    TASK="写一个 C# 控制台程序 GameOfLife.cs，康威生命游戏：无限网格 + 图案库(滑翔机/脉冲星/轻型飞船) + 鼠标点击放置 + 保存/加载 + 速度控制，400行以内。用 write_file 写入。"
    ;;
  *)
    echo "未知级别: $LEVEL (用 easy|medium|hard)"
    exit 1
    ;;
esac

# ── 模型列表 ──
MODELS=(
  "deepseek-v4-flash"
  "deepseek-chat"
  "deepseek-v4-pro"
)

# 可选: 如果有 GPT 密钥
if [ -n "${OPENAI_API_KEY:-}" ] || [ -n "${GPT_API_KEY:-}" ]; then
  MODELS+=("gpt-5.4-mini")
fi

# 可选: 如果 Ollama 运行中，加入本地模型对比
if curl -s "${OLLAMA_BASE_URL:-http://localhost:11434/v1}/models" > /dev/null 2>&1; then
  # 检查已安装的本地模型
  for LM in "qwen2.5-coder:7b" "qwen2.5-coder:3b" "codellama:7b"; do
    if curl -s "${OLLAMA_BASE_URL:-http://localhost:11434}/api/tags" 2>/dev/null | grep -q "\"name\":\"$LM\""; then
      MODELS+=("$LM (local)")
      LOCAL_BASE="${OLLAMA_BASE_URL:-http://localhost:11434/v1}"
    fi
  done
fi

# ── 初始化 ──
mkdir -p "$RESULT_DIR"
cd "$REPO_DIR/WayCoder"

echo "═══════════════════════════════════════════"
echo "  WayCoder 模型基准测试"
echo "  任务级别: $LEVEL"
echo "  时间: $TIMESTAMP"
echo "  模型数: ${#MODELS[@]}"
echo "═══════════════════════════════════════════"
echo ""

# 编译
echo "🔨 编译 WayCoder..."
dotnet publish -c Release -o "$REPO_DIR/publish" --nologo -v q 2>&1 | tail -1
echo ""

# ── 运行测试 ──
declare -A RESULTS
for MODEL in "${MODELS[@]}"; do
  LOG_FILE="$RESULT_DIR/${TIMESTAMP}_${LEVEL}_${MODEL//:/_}.log"

  echo "───────────────────────────────────────────"
  echo "🚀 测试: $MODEL"
  echo "   日志: $LOG_FILE"
  echo ""

  START_TIME=$(date +%s)

  # 本地模型 vs 云端模型
  if [[ "$MODEL" == *"(local)" ]]; then
    REAL_MODEL="${MODEL% (local)}"
    BASE_URL="${OLLAMA_BASE_URL:-http://localhost:11434/v1}"
    TIMEOUT=180
  else
    REAL_MODEL="$MODEL"
    BASE_URL=""
    TIMEOUT=120
  fi

  # 运行 WayCoder
  set +e
  if [ -n "$BASE_URL" ]; then
    timeout $TIMEOUT "$REPO_DIR/publish/WayCoder.exe" \
      -m "$REAL_MODEL" \
      --base-url "$BASE_URL" \
      -p "$TASK" \
      --yolo \
      > "$LOG_FILE" 2>&1
  else
    timeout $TIMEOUT "$REPO_DIR/publish/WayCoder.exe" \
      -m "$REAL_MODEL" \
      -p "$TASK" \
      --yolo \
      > "$LOG_FILE" 2>&1
  fi
  EXIT_CODE=$?
  set -e

  END_TIME=$(date +%s)
  ELAPSED=$((END_TIME - START_TIME))

  # ── 分析结果 ──
  if [ $EXIT_CODE -eq 0 ]; then
    # 检查是否生成了文件
    GENERATED=$(grep -c "write_file\|已创建\|写入" "$LOG_FILE" 2>/dev/null || echo 0)
    LINES=$(grep -oP '写了|写入.*行|written.*lines' "$LOG_FILE" 2>/dev/null | tail -1 || echo "?")
    STATUS="✅ 完成"
  elif [ $EXIT_CODE -eq 124 ]; then
    STATUS="⏱ 超时 (120s)"
    LINES="N/A"
  else
    STATUS="❌ 失败 (exit=$EXIT_CODE)"
    LINES="N/A"
  fi

  RESULTS["$MODEL"]="$ELAPSED|$STATUS|$LINES"

  echo "   结果: $STATUS"
  echo "   耗时: ${ELAPSED}s"
  echo ""
done

# ── 汇总表格 ──
echo ""
echo "═══════════════════════════════════════════"
echo "  📊 测试汇总"
echo "═══════════════════════════════════════════"
printf "  %-25s %8s  %s\n" "模型" "耗时" "结果"
echo "  ─────────────────────────────────────────"
for MODEL in "${MODELS[@]}"; do
  IFS='|' read -r TIME STATUS LINES <<< "${RESULTS[$MODEL]}"
  printf "  %-25s %6ss  %s\n" "$MODEL" "$TIME" "$STATUS"
done
echo "═══════════════════════════════════════════"
echo "  日志目录: $RESULT_DIR"
echo "═══════════════════════════════════════════"

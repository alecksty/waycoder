#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 本地模型测试（Ollama）
# 用法: ./scripts/bench-local.sh [级别: easy|medium|hard]
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
RESULT_DIR="$REPO_DIR/bench-results"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LEVEL="${1:-easy}"
OLLAMA_URL="${OLLAMA_BASE_URL:-http://localhost:11434/v1}"

# ── 任务定义 ──
case "$LEVEL" in
  easy)
    TASK="写一个 C# 控制台程序 GuessNumber.cs，猜数字游戏：1-100 随机数，玩家输入猜测，提示太大/太小，计次数，80行以内。用 write_file 写入。"
    ;;
  medium)
    TASK="写一个 C# 控制台程序 TypingTutor.cs，打字练习：随机生成英文句子，计时+统计正确率+WPM，200行以内。用 write_file 写入。"
    ;;
  hard)
    TASK="写一个 C# 控制台程序 FileExplorer.cs，终端文件浏览器：目录树浏览+文件预览+复制/移动/删除，纯ANSI渲染，350行以内。用 write_file 写入。"
    ;;
  *)
    echo "未知级别: $LEVEL (用 easy|medium|hard)"
    exit 1
    ;;
esac

# ── 本地模型列表（按优先级排序） ──
LOCAL_MODELS=(
  # 无 thinking 模型（优先测试）
  "qwen2.5-coder:7b"       # 最佳选择 — 无 thinking + 够大做工具调用
  "qwen2.5-coder:3b"       # 小但可能够用
  "codellama:7b"            # Meta 代码模型
  "deepseek-coder-v2:16b"   # DeepSeek 本地版
  # thinking 模型（推理消耗 token，仅对比测试）
  "qwen3:8b"                # 有 thinking，对比用
  "deepseek-r1:8b"          # 有 thinking，对比用
)

# ── 检查 Ollama 是否运行 ──
echo "═══════════════════════════════════════════"
echo "  WayCoder 本地模型测试"
echo "  任务级别: $LEVEL"
echo "  Ollama: $OLLAMA_URL"
echo "═══════════════════════════════════════════"
echo ""

if ! curl -s "$OLLAMA_URL/models" > /dev/null 2>&1; then
  echo "❌ Ollama 未运行！请先启动: ollama serve"
  exit 1
fi

echo "✅ Ollama 已连接"
echo ""

# 列出已安装的模型
echo "📦 已安装模型:"
curl -s "${OLLAMA_URL%/v1}/api/tags" | grep -oP '"name":"[^"]+"' | head -20 || echo "  (无法获取)"
echo ""

# ── 编译 ──
mkdir -p "$RESULT_DIR"
cd "$REPO_DIR/WayCoder"
echo "🔨 编译 WayCoder..."
dotnet publish -c Release -o "$REPO_DIR/publish" --nologo -v q 2>&1 | tail -1
echo ""

# ── 运行测试 ──
declare -A RESULTS
TESTED=0
SKIPPED=0

for MODEL in "${LOCAL_MODELS[@]}"; do
  SAFE_NAME="${MODEL//:/_}"
  LOG_FILE="$RESULT_DIR/${TIMESTAMP}_${LEVEL}_local_${SAFE_NAME}.log"

  # 检查模型是否已安装
  if ! curl -s "${OLLAMA_URL%/v1}/api/tags" | grep -q "\"name\":\"$MODEL\""; then
    echo "───────────────────────────────────────────"
    echo "⏭ 跳过: $MODEL (未安装)"
    echo "   安装: ollama pull $MODEL"
    SKIPPED=$((SKIPPED + 1))
    echo ""
    continue
  fi

  echo "───────────────────────────────────────────"
  echo "🚀 测试: $MODEL"
  echo "   日志: $LOG_FILE"

  START_TIME=$(date +%s)

  # 本地模型给更多时间（120s）
  set +e
  timeout 180 "$REPO_DIR/publish/WayCoder.exe" \
    -m "$MODEL" \
    --base-url "$OLLAMA_URL" \
    -p "$TASK" \
    --yolo \
    > "$LOG_FILE" 2>&1
  EXIT_CODE=$?
  set -e

  END_TIME=$(date +%s)
  ELAPSED=$((END_TIME - START_TIME))
  TESTED=$((TESTED + 1))

  # 分析
  if [ $EXIT_CODE -eq 0 ]; then
    # 检查是否有 reasoning 输出
    REASONING_CHARS=$(grep -c "«dim»" "$LOG_FILE" 2>/dev/null || echo 0)
    FILE_COUNT=$(find "$REPO_DIR" -name "*.cs" -newer "$LOG_FILE" -type f 2>/dev/null | wc -l)
    if [ "$FILE_COUNT" -gt 0 ]; then
      STATUS="✅ 完成 ($FILE_COUNT 文件)"
    else
      STATUS="⚠ 无输出文件"
    fi
  elif [ $EXIT_CODE -eq 124 ]; then
    STATUS="⏱ 超时 (180s)"
  else
    STATUS="❌ 失败 (exit=$EXIT_CODE)"
    # 检查是否是推理耗尽
    if grep -q "«dim»" "$LOG_FILE" 2>/dev/null; then
      STATUS="$STATUS [推理耗尽]"
    fi
  fi

  RESULTS["$MODEL"]="${ELAPSED}s|$STATUS"

  echo "   结果: $STATUS"
  echo "   耗时: ${ELAPSED}s"
  echo ""
done

# ── 汇总 ──
echo ""
echo "═══════════════════════════════════════════"
echo "  📊 本地模型测试汇总"
echo "═══════════════════════════════════════════"
printf "  %-25s %8s  %s\n" "模型" "耗时" "结果"
echo "  ─────────────────────────────────────────"
for MODEL in "${LOCAL_MODELS[@]}"; do
  if [ -n "${RESULTS[$MODEL]+x}" ]; then
    IFS='|' read -r TIME STATUS <<< "${RESULTS[$MODEL]}"
    printf "  %-25s %8s  %s\n" "$MODEL" "$TIME" "$STATUS"
  fi
done
echo "───────────────────────────────────────────"
echo "  测试: $TESTED | 跳过(未安装): $SKIPPED"
echo "  日志: $RESULT_DIR"
echo ""
echo "  💡 安装缺失模型:"
echo "    ollama pull qwen2.5-coder:7b"
echo "    ollama pull codellama:7b"
echo "═══════════════════════════════════════════"

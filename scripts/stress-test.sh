#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 压力测试 — 超长复杂任务（跨文件重构级别）
# 用法: ./scripts/stress-test.sh [模型名]
# ═══════════════════════════════════════════════════════════════
set -euo pipefail

MODEL="${1:-deepseek-v4-flash}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
OUTPUT_DIR="$REPO_DIR/stress-test-output"
WORK_DIR="$OUTPUT_DIR/${TIMESTAMP}_${MODEL//:/_}"
mkdir -p "$WORK_DIR"

LOG_FILE="$WORK_DIR/waycoder.log"

# 极端复杂任务
PROMPT="在当前目录创建以下文件：

1. MiniKanban/Models/TaskItem.cs — 看板任务模型：Id, Title, Description, Status(Todo/InProgress/Done), Priority(High/Medium/Low), CreatedAt, DueDate
2. MiniKanban/Models/KanbanBoard.cs — 看板：Name, List<TaskItem>，方法 AddTask/RemoveTask/MoveTask
3. MiniKanban/Services/TaskService.cs — 任务服务：CRUD + 按状态/优先级筛选 + 排序
4. MiniKanban/UI/KanbanRenderer.cs — ANSI 彩色终端渲染：三列(Todo/InProgress/Done)，颜色区分优先级
5. MiniKanban/Program.cs — 入口：命令行参数 add/list/move/delete，用法说明

要求：纯 C# (.NET 10)，零外部依赖，ANSI 颜色，编译通过。先 todo_write 列计划，然后逐个文件 write_file 写入。"

echo "═══════════════════════════════════════════"
echo "  WayCoder 压力测试"
echo "  模型: $MODEL"
echo "  工作目录: $WORK_DIR"
echo "  任务: MiniKanban (5 文件跨模块)"
echo "═══════════════════════════════════════════"
echo ""

# 编译
cd "$REPO_DIR/WayCoder"
echo "🔨 编译中..."
dotnet publish -c Release -o "$REPO_DIR/publish" --nologo -v q 2>&1 | tail -1
echo ""

cd "$WORK_DIR"
echo "🚀 开始..."
START=$(date +%s)

"$REPO_DIR/publish/WayCoder.exe" \
  -m "$MODEL" \
  -p "$PROMPT" \
  --yolo \
  2>&1 | tee "$LOG_FILE"

END=$(date +%s)

# 统计
echo ""
echo "═══════════════════════════════════════════"
echo "  📊 结果统计"
echo "═══════════════════════════════════════════"

FILE_COUNT=$(find "$WORK_DIR" -name "*.cs" -type f 2>/dev/null | wc -l)
TOTAL_LINES=$(find "$WORK_DIR" -name "*.cs" -type f -exec cat {} \; 2>/dev/null | wc -l)

echo "  模型:        $MODEL"
echo "  耗时:        $((END - START))s"
echo "  生成文件数:  $FILE_COUNT / 5"
echo "  总代码行数:  $TOTAL_LINES"

# 编译验证
if [ $FILE_COUNT -gt 0 ]; then
  echo ""
  echo "🔍 编译验证..."
  cd "$WORK_DIR"
  if dotnet build --nologo -v q 2>&1; then
    echo "  ✅ 编译通过"
  else
    echo "  ❌ 编译失败"
  fi
fi

echo "═══════════════════════════════════════════"

#!/bin/bash
# 一键回归：单元自测 + 随机压力测试 + 端到端任务流程
# 用法: ./_check.sh            （全部跑）
#       ./_check.sh selftest   （只跑某一项：selftest | stress | e2e）
# 需要先在 demo/ 目录起一个静态服务器：python3 -m http.server 8777
set -u
DIR="$(cd "$(dirname "$0")" && pwd)"
WHAT="${1:-all}"

port_up() { curl -s -o /dev/null "http://localhost:8777/sky-combat/index.html"; }

if ! port_up; then
  echo "⚠ 静态服务器未启动。请在 demo/ 目录执行： python3 -m http.server 8777"
  exit 2
fi

# run_one <页面> <虚拟时间ms> <说明> <期望标题里的成功标记>
# headless Chrome 的 virtual-time-budget 有时会提前把任务掐掉（同一轮连跑多项时更明显），
# 所以失败重试一次，避免把「工具链抖动」误报成游戏回归。
run_one() {
  local page="$1" budget="$2" label="$3" marker="$4"
  echo "=== $label ($page) ==="
  local out title attempt
  for attempt in 1 2; do
    out="$("$DIR/_run.sh" "sky-combat/$page" "$budget" 2>&1)"
    title="$(printf '%s\n' "$out" | head -1)"
    case "$title" in
      *"$marker"*) break ;;
      *) [ "$attempt" = 1 ] && echo "（第 1 次未跑完，重试…）" ;;
    esac
  done
  printf '%s\n' "$out" | tail -18
  echo "→ $title"
  case "$title" in
    *"$marker"*) return 0 ;;
    *) return 1 ;;
  esac
}

fails=0
if [ "$WHAT" = "all" ] || [ "$WHAT" = "selftest" ]; then
  run_one _selftest.html 150000 "单元自测" "RESULT_OK" || fails=$((fails + 1))
fi
if [ "$WHAT" = "all" ] || [ "$WHAT" = "stress" ]; then
  run_one _stress.html 180000 "随机压力" "STRESS_OK" || fails=$((fails + 1))
fi
if [ "$WHAT" = "all" ] || [ "$WHAT" = "e2e" ]; then
  run_one _e2e.html 240000 "端到端流程" "E2E_DONE" || fails=$((fails + 1))
fi

echo
if [ "$fails" -eq 0 ]; then echo "✅ 全部通过"; else echo "❌ $fails 项未通过"; fi
exit "$fails"

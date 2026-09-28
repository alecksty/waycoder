#!/usr/bin/env bash
# Ladder 前端回归套件 —— **每修一条缺陷就加一个用例**。
#
# 为什么要有它：Ladder 的缺陷有一个共同形态 —— **静默**。
#   调用名加错前缀 ⇒ 整门语言调不到任何库函数（只有链接期报"未定义的函数"）；
#   块内的裸打印不认 ⇒ 那条语句**一声不响地消失**（不报错、不生成代码）。
#   前者"编不过"还好，后者是**写得出来、跑起来少一截**，只有真跑一遍才知道。
#
# 用法：bash scripts/vml-ladder-probe/run.sh          （全跑）
#       bash scripts/vml-ladder-probe/run.sh l1       （只跑名字含 l1 的）
#
# 约定：用例**自己判定**、全部通过时打印 `ALL PASS`。runner 只看这一行 ——
#   比对比整段输出来得稳（Ladder 的 `PRINT_CHAR 10` 换行是显式发的，
#   不同例子的排版本来就不同）。
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
VMLCLI="$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll"
FILTER="${1:-}"

if [[ ! -f "$VMLCLI" ]]; then
  echo "✘ 没找到 vmlcli：$VMLCLI"
  echo "  先构建：dotnet build scripts/vmlcli -c Release"
  exit 2
fi

pass=0; fail=0
for f in "$HERE"/cases/*.ld; do
  name="$(basename "$f" .ld)"
  if [[ -n "$FILTER" && "$name" != *"$FILTER"* ]]; then continue; fi
  out="$(dotnet "$VMLCLI" "$f" 2>&1)"
  if grep -q 'ALL PASS' <<<"$out"; then
    pass=$((pass+1)); printf '✔ %s\n' "$name"
  else
    fail=$((fail+1))
    printf '✘ %s\n' "$name"
    printf '%s\n' "$out" | grep -aE 'error|FAIL|未定义的函数|undefined' | head -3 | sed 's/^/     /'
  fi
done

echo "─── 通过 $pass / 失败 $fail ───"
if [[ $fail -gt 0 ]]; then exit 1; fi
exit 0

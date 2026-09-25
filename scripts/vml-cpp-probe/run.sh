#!/usr/bin/env bash
# C++ 前端回归套件 —— **每修一条缺陷就加一个用例**。
#
# 为什么要有它：这个前端的历史缺陷里有相当一批是"能编、能跑、**结果错、不报错**"
# （最狠的一条是"给类加个成员就一帧都导不出来"）。这类问题**只有真跑一遍才知道**，
# 而且改别处时极易带回来 —— 所以每条修复都钉一个用例。
#
# 用法：bash scripts/vml-cpp-probe/run.sh          （全跑）
#       bash scripts/vml-cpp-probe/run.sh f38      （只跑名字含 f38 的）
#
# 约定：用例**自己判定**、全部通过时打印 `ALL PASS`。runner 只看这一行 ——
#   比对比整段输出来得稳（本平台的 `cout` 会吞字面量，输出本身就不完全可靠）。
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
for f in "$HERE"/cases/*.cpp; do
  name="$(basename "$f" .cpp)"
  if [[ -n "$FILTER" && "$name" != *"$FILTER"* ]]; then continue; fi
  out="$(dotnet "$VMLCLI" "$f" 2>&1)"
  if grep -q 'ALL PASS' <<<"$out"; then
    pass=$((pass+1)); printf '✔ %s\n' "$name"
  else
    fail=$((fail+1))
    printf '✘ %s\n' "$name"
    grep -E 'error|坏了|不对|被踩' <<<"$out" | head -3 | sed 's/^/     /'
  fi
done

echo "─── 通过 $pass / 失败 $fail ───"
if [[ $fail -gt 0 ]]; then exit 1; fi
exit 0

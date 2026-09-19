#!/usr/bin/env bash
# 用**当前**前端把 Lib/shared/src/*.c 全量重生成到临时目录，与签入的 .vml 逐个比对。
# 目的：量清楚「取形参地址」这一处前端修复对 Lib/ 的影响面。
set -uo pipefail
REPO=/Users/shitanyu/Desktop/source/mycoder/my-coder
VML=$REPO/third_party/vml
DLL=$REPO/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll
OUT=$(mktemp -d)
same=0; diff=0; fail=0
for src in "$VML"/Lib/shared/src/*.c; do
  b=$(basename "$src" .c)
  if dotnet "$DLL" --rebuild-lib "$src" --out "$OUT/$b.vml" >/dev/null 2>&1; then
    if [ -f "$VML/Lib/shared/$b.vml" ]; then
      if cmp -s "$OUT/$b.vml" "$VML/Lib/shared/$b.vml"; then same=$((same+1)); else echo "DIFF $b"; diff=$((diff+1)); fi
    else echo "NEW  $b（签入目录里没有）"; diff=$((diff+1)); fi
  else echo "FAIL $b"; fail=$((fail+1)); fi
done
echo "=== 相同 $same / 有差异 $diff / 生成失败 $fail ==="
echo "临时目录: $OUT"

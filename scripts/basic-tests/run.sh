#!/usr/bin/env bash
# BASIC 回归跑分：逐个跑 .bas，打印程序自身输出（过滤编译/链接噪音）
cd "$(dirname "$0")"
HOST="dotnet run -c Release --no-build --project /d/code-agents/WayCoder/.scratch/vmlhost -- run"
for f in t*.bas; do
    printf "%-22s " "$f"
    out=$($HOST "$f" ${EXTRA:-} 2>&1 | grep -aE 'idiv=|mul=|a5=|a2=|in=|sub=|half=|top=|sum=|w=|BRIDGE|^[0-9]+$|未找到|VML 错误|error' | tr '\n' ' ')
    echo "$out"
done

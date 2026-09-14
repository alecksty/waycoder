#!/bin/bash
# Curl HTTP 请求示例 — 编译并运行
cd "$(dirname "$0")/../.."
echo "=== 编译 Curl 示例 ==="
dotnet run --project VMLTool -- Examples/Curl/fetch_c.c -L Lib/curl -o Examples/Curl/fetch_c.vml
echo ""
echo "=== 运行 ==="
dotnet run --project VMLTool -- -r Examples/Curl/fetch_c.vml

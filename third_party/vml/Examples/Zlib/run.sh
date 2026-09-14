#!/bin/bash
# Zlib 压缩/解压示例 — 编译并运行
cd "$(dirname "$0")/../.."
echo "=== 编译 Zlib 示例 ==="
dotnet run --project VMLTool -- Examples/Zlib/compress_c.c -L Lib/zlib -o Examples/Zlib/compress_c.vml
echo ""
echo "=== 运行 ==="
dotnet run --project VMLTool -- -r Examples/Zlib/compress_c.vml

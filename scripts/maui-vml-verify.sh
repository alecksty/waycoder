#!/usr/bin/env bash
# 安卓设备端 VML 验收装置 —— 一条命令跑完「装机 → 布语料 → 逐条跑 → 出表」。
#
#   scripts/maui-vml-verify.sh                     # 用设备上已有的包跑全部语料
#   scripts/maui-vml-verify.sh --apk               # 先装默认 Release 产物再跑
#   scripts/maui-vml-verify.sh --only skel-        # 只跑「每语言骨架」那一组
#   scripts/maui-vml-verify.sh --list              # 只列语料
#   scripts/maui-vml-verify.sh --serial <设备> --pkg <包名>
#
# 产物：scripts/maui-vml-verify/report.tsv（表格）+ report.raw.txt（每条的原始输出）
#       env.txt（本次环境指纹；同输入两次跑，这份应当完全一致）
#
# 判定分类与"为什么这么做"见 scripts/maui-vml-verify/verify.py 的文档注释。
set -euo pipefail
exec python3 "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/maui-vml-verify/verify.py" "$@"

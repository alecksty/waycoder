#!/usr/bin/env bash
# 从上游 VML 仓库同步内置副本，并**重新施加本地适配**。
#   用法: third_party/vml/sync.sh ~/Desktop/source/vml/vml
#
# 本地适配有四处于（不改会让 MAUI 编不过）：
#   ① 25 个 csproj 的 OutputType: Exe → Library   （否则 NETSDK1150：自包含应用不能引用非自包含 Exe）
#   ② 去掉 StartupObject（19 个）                （否则 CS2017：库不能指定 /main）
#   ③ 去掉 RuntimeIdentifiers（24 个）           （否则 NETSDK1047：它的列表里没有 android-arm64）
#   ④ 去掉 PublishAot（25 个）                   （AOT 发布要求 RID；我们只要它们的代码，不做 AOT 发布）
# 之所以要脚本化：rsync 是覆盖式的，会把这两处改动冲掉。
set -euo pipefail
UP="${1:?用法: sync.sh <上游 VML 仓库路径>}"
DST="$(cd "$(dirname "$0")" && pwd)"

for d in VMLAssembler VMLRuntime VMLPlugins VMLPrepares VMLTool VMLTranslators VMLToHex Lib; do
  rsync -a --delete --exclude 'bin/' --exclude 'obj/' --exclude 'Examples/' "$UP/$d/" "$DST/$d/"
done
for f in VERSION LICENSE Directory.Build.props .editorconfig; do
  [ -f "$UP/$f" ] && cp "$UP/$f" "$DST/"
done
[ -f "$UP/vmltool.config.xml" ] && cp "$UP/vmltool.config.xml" "$DST/"
[ -f "$UP/README.md" ] && cp "$UP/README.md" "$DST/VML-README.md"

python3 - "$DST" <<'PY'
import os, re, sys
root = sys.argv[1]
exe = start = rid = aot = 0
for dirpath, dirnames, files in os.walk(root):
    dirnames[:] = [d for d in dirnames if d not in ('bin', 'obj')]
    for f in files:
        if not f.endswith('.csproj'):
            continue
        p = os.path.join(dirpath, f)
        d = open(p, encoding='utf-8', newline='').read()
        n = d.replace('<OutputType>Exe</OutputType>', '<OutputType>Library</OutputType>')
        if n != d: exe += 1
        d2 = re.sub(r'\s*<StartupObject>[^<]*</StartupObject>', '', n)
        if d2 != n: start += 1
        d3 = re.sub(r'\s*<RuntimeIdentifiers>[^<]*</RuntimeIdentifiers>', '', d2)
        if d3 != d2: rid += 1
        d4 = re.sub(r'\s*<PublishAot>[^<]*</PublishAot>', '', d3)
        if d4 != d3: aot += 1
        if d4 != d:
            open(p, 'w', encoding='utf-8', newline='').write(d4)
print(f'本地适配已重新施加: OutputType {exe}, StartupObject {start}, RuntimeIdentifiers {rid}, PublishAot {aot}')
PY
echo "同步完成。版本: $(head -c 40 "$DST/VERSION" 2>/dev/null | tr -d '\n')"
echo "⚠ 别忘了更新 README.md 顶部的版本号与提交哈希。"

#!/usr/bin/env bash
# 从上游 VML 仓库同步内置副本，并**重新施加本地适配**。
#   用法: third_party/vml/sync.sh ~/Desktop/source/vml/vml
#
# 本地适配分两类：
#
# 【A】csproj 属性（不改会让 MAUI **编不过**）—— 由下面的 python 步骤重新施加：
#   ① 25 个 csproj 的 OutputType: Exe → Library   （否则 NETSDK1150：自包含应用不能引用非自包含 Exe）
#   ② 去掉 StartupObject（19 个）                （否则 CS2017：库不能指定 /main）
#   ③ 去掉 RuntimeIdentifiers（24 个）           （否则 NETSDK1047：它的列表里没有 android-arm64）
#   ④ 去掉 PublishAot（25 个）                   （AOT 发布要求 RID；我们只要它们的代码，不做 AOT 发布）
#
# 【B】源码级适配 —— 由 patches/*.patch 重新施加（`git apply`）：
#   ① 0001-file-system-root.patch：给 VmRuntime 加 `FileSystemRoot` 沙箱根，
#      并让 `ExecuteFileOpen` 按它解析/拒绝路径。不加**编得过、跑起来才出问题**
#      （手机上 `open("a.txt","w")` 会落到进程 CWD，报 "Read-only file system" 或写到别处）。
#   ② 0002-c-codegen-fixes.patch：C 前端两处**错误代码生成**（都是"编得过、跑起来才错"）：
#      ① 参数寄存器早装载 —— cdecl 调用里 `MOVE Ri, R0` 在下一次实参求值前就做了，
#         而求值会用 R0/R1 当临时寄存器，把已装好的参数冲掉。实参只要是带运算的表达式
#         就会中招（`f(p+3*k, q+3*k, 3*k)` 的第二个参数算成第一个的值）。
#      ② R12-4 撞车 —— asm 结果的暂存槽与**第一个局部变量**是同一个偏移，
#         任何一条 asm 执行完都会把那个变量覆盖成上一条 syscall 的返回值。
#         现象就是"传给手机端弹窗的字符串大多是空的/是乱码"。
#      两处都带最小复现用例（见 patches 旁的注释）与实测记录（Lib/README-ui.md）。
#   ③ 0003-c-const-array-dim.patch：C 前端的**数组维度不折常量表达式**。
#      `int b[BW * BH]`（宏展开成 `10 * 20`）被判成运行时 VLA，编译期尺寸留空 ⇒
#      **静默按 1 个元素分配**，之后所有下标都写到别的变量上（编得过、跑起来数据全乱）。
#      修法是给数组维度接一个常量折叠器（字面量 / 一元 ±!~ / 常量算术与位运算）。
#      复现用例 `.scratch/vmlhost/tests/arrdim.c`；`Examples/c/tetris.c` 的棋盘用的就是它。
#   ④ 0004-c-global-array-elem-type.patch：C 前端的**全局 char/short 数组按 32 位读写**。
#      `InferExpressionType(ArrayAccess)` 只查 `variableTypes`（只装局部变量），全局数组
#      查不到就退化成 `ExprType.Int` ⇒ `char g[8]` 的 `g[0]` 走 MOVE（32 位）而不是 MOVEB。
#      现象是「长度对、内容不对」（`g[0]='A'` 之后 `g[0]=='A'` 为 false），写还会越界。
#      局部数组一直是对的，所以这个坑只在全局数组上冒头 —— 很容易误判成"局部数组传参坏了"。
#      复现用例 `.scratch/vmlhost/tests/globchar.c`（判定绕过 stdio：每条结论用一个频率报出来）。
#   ⑤ 0005-assembler-compact-word-array.patch：汇编器支持**批量数据** `.word[N] [默认值]`。
#      `int a[1024];` 原来生成 1024 行 `.word 0`（空 main 的 .vml 有 1071 行），而数据在内存里
#      本来就是压缩的（编译器放的是 `int[1024]`），只有"落成文本"这一步把它摊平。改后 47 行。
#      ⚠ **新语法，旧版汇编器读不懂**（会把 `[1024] 0` 当值解析、静默变 0）⇒ 回灌上游时
#      解析侧与序列化侧必须**同步升级**，发版说明里要写清楚。只做了 4 字节字（`.byte[N]` 没做）。
#   ⑥ 0006-assembler-word-handled-twice.patch：汇编器把一行 `.word` **处理两遍** ⇒ 数组初值
#      静默错位。`ParseData` 的 if/else-if 链在 `.data` 那一行断了（新起了个 `if` 而不是
#      `else if`），于是后面的 `.dword`/`.byte`/`.word` 分支挂到了 `.data` 上，一行 `.word 7`
#      被 `.word` 专用分支和 `HandleDataDirective` 各加一次 ⇒ `int c[3]={7,8,9}` 变成
#      `7,7,8,8,9,9`（`c[1]` 读到 7、`c[2]` 读到 8）。零初值数组只是白占一倍内存，所以棋盘类
#      程序一直看着正常，有初值的才露馅。修法只有一个词（`if` → `else if`）。
#      最小复现：`.data` / `p1:` / 三行 `.word 7|8|9` 喂给 `Assemble`，看 `DataSection["p1"]`。
#
# 之所以要脚本化：rsync 是覆盖式的，两类改动都会被冲掉。
# 【B】用 patch 而不是"再抄一遍源码"：改动本身可 review、可 diff；
# 且 apply 失败会**直接退出**，不会让本地适配悄悄消失。
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

# 【B】源码级适配：rsync 刚把它们冲掉了，逐条重放。
# patch 里的路径是**仓库根相对**的（`a/third_party/vml/...`），所以用 `git -C "$ROOT"`。
ROOT="$(cd "$DST/../.." && pwd)"
for p in "$DST"/patches/*.patch; do
  [ -f "$p" ] || continue
  name="$(basename "$p")"
  if git -C "$ROOT" apply --check "$p" 2>/dev/null; then
    git -C "$ROOT" apply "$p"
    echo "✔ 已施加 $name"
  elif git -C "$ROOT" apply --check --reverse "$p" 2>/dev/null; then
    echo "• $name 已在（跳过）"
  else
    echo "✘ $name 打不上 —— 多半是上游改了同一处，需要手工合并后重新生成 patch" >&2
    exit 1
  fi
done

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

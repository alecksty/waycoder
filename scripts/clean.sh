#!/bin/bash
# ═══════════════════════════════════════════════════════════════
# WayCoder 清除编译垃圾
# 用法: ./scripts/clean.sh [--dry-run|-n] [--no-gc] [--scratch]
#
# 覆盖的工程：
#   WayCoder/            主 CLI + TUI          —— bin/obj + 旁路输出 bin2/obj2
#   WayCoder.Gui/        Avalonia GUI          —— bin/obj
#   WayCoder.Maui/       .NET MAUI (Android/iOS) —— bin/obj、.gradle、*.apk/*.aab
#   WayCoder.Preview/    预览宿主              —— bin/obj
#   third_party/vml/     vendored VML 的 35 个 C# 工程 —— bin/obj
#                        + 本地中间产物 *.gen.vml / *.vmb
#                        （**不**动它 Lib/ 下已跟踪的 2041 个 .vml，见下方「刻意不碰」）
#   vscode-extension/    VS Code 扩展 (TS)     —— node_modules、out/、*.vsix
#   WayCoder/3d-game/    零构建前端游戏         —— 无构建产物（仅 __pycache__）
#
# 说明: 删除 C# 项目的 bin/obj、IDE 缓存 .vs/.idea、*.user/*.suo/*.binlog 用户配置、
#       node_modules 依赖、StarGo 五子棋的 MSVC 产物、独立 publish 目录、
#       MAUI 的 .gradle 与 *.apk/*.aab、Python 的 __pycache__/*.pyc/.venv/venv、
#       vscode-extension 的 out/ 与 *.vsix、TestResults / BenchmarkDotNet.Artifacts /
#       AppPackages / .store / verify_build / stress-test-output 测试产物、
#       dist/ 下的陈旧发布产物、**VML 本地中间产物 *.gen.vml / *.vmb**；
#       清理完成后自动 git gc --aggressive 压缩仓库。
#
#       关于 VML 中间产物：判据是「没有任何构建把它们当源读」——
#       *.gen.vml 被 .gitignore 明写，且 make-vml-lib.sh 与 vml-diag-probe/examples-build.sh
#       都显式把它排除出遍历（不然会被当成待编译的例子）；*.vmb 是 VMLTool 的编译终点
#       （Program.Compile.cs「→ VML 二进制 → 停止」）。这两个 pattern 已用
#       `git ls-files '*.gen.vml' '*.vmb'` 验证过「已跟踪 0 个」，脚本里还留了同一句做闸门。
#
#       关于 --scratch：.gitignore 把 .scratch/ 定义为「草稿区：验证截图、一次性探针源码、
#       交接草稿」——是**有意保留**的本地材料（CLAUDE.md 多处验证脚手架就在这儿，如
#       .scratch/vmlround 的 --play/--lines/--best），故默认不动，要清得显式给 --scratch。
#
#       关于 bin2/obj2：主 bin/ 被运行中的 exe 锁住时，构建会改用 -p:OutputPath=bin2/
#       落到这里（见 .gitignore），属构建产物，一并清理。
#
# 刻意不碰（防误伤，勿加进来）：
#   third_party/vml/Lib/**/*.vml  —— 2041 个文件是**已跟踪**的 vendored 源。vml 自带的
#                                    Lib/cleanup.sh 会删掉它们（那是上游仓库的语义，
#                                    在我们这边等于删源码并弄脏 git 状态）。
#                                    ⚠ 同理**绝不用 `*.vml` 通配**：Examples/ 下还有 2 个
#                                    .vml 是例子源码，且 `vmlcli --rebuild-lib` 会往
#                                    Lib/<语言>/ 写 .vml —— 那些是**产物但已跟踪**，
#                                    删了就是删源码，只能让 git 报 dirty，不能靠 clean 清。
#   .scratch/                     —— 草稿区，有意保留的本地材料（--scratch 才清）
#   WayCoder/works/、WayCoder/saves/  —— 智能体工作区与存档，是用户数据不是垃圾
#   logs/、.waycoder/、dist/.waycoder/、.claude/、.crush/、.codex/  —— 运行时与 AI 工具状态
#   WayCoder.Maui/Resources/Raw/vml_lib.zip、WayCoder/UI/WEB/WebAssets.Generated.cs
#                                    —— 已跟踪/被引用的产物，删了要重新生成才能构建
#
#       全部为 .gitignore 忽略的构建产物，不影响源码与 git 状态。
#       --dry-run 只列出将删除项，不实际删除；--no-gc 跳过 gc。
# ═══════════════════════════════════════════════════════════════
set -uo pipefail

DRY_RUN=0
NO_GC=0
SCRATCH=0
for arg in "$@"; do
    case "$arg" in
        --dry-run|-n) DRY_RUN=1 ;;
        --no-gc) NO_GC=1 ;;
        --scratch) SCRATCH=1 ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
cd "$REPO_DIR"

if [[ "$DRY_RUN" -eq 1 ]]; then
    echo "🔎 干跑模式：仅列出将删除项，不实际删除"
else
    BEFORE=$(du -sk . 2>/dev/null | awk '{print $1}')
fi

SKIPPED_SCRATCH=0
del() {
    # ── .scratch/ 统一闸门（放在这个唯一删除出口，别散到上面各段各加一条 -not -path）──
    # 文件头把 .scratch/ 定义为「有意保留的本地草稿材料」（--scratch 才清），但上面各段是按
    # **目录名 / 扩展名**全树匹配的，会伸进 .scratch 内部。2026-09-23 实测踩到：
    #   · bin/obj 那一段删掉了 .scratch/oldprogs/src/number/ 下 **70 个 NetBSD 源码目录** ——
    #     NetBSD 的 bin/ 里放的是**源码**（external/bsd/flex/bin、external/apache2/llvm/bin…），
    #     不是构建产物；obj/ 同理。
    #   · *.user 那段删掉了 unbound 的**测试夹具** external/bsd/unbound/dist/…/bad.user。
    # ⚠ 收口在这里：漏一处就是这个坑重演（本仓头号教训 —— 规则要收口成唯一实现）。
    if [[ "$SCRATCH" -ne 1 && "$1" == *"/.scratch/"* ]]; then
        SKIPPED_SCRATCH=$((SKIPPED_SCRATCH + 1))
        return
    fi
    if [[ "$DRY_RUN" -eq 1 ]]; then
        echo "  [将删] $1"
    else
        rm -rf -- "$1" && echo "  [已删] $1"
    fi
}

# 通用目录查找：$1 = find 的匹配表达式（已括号化），$2 = 额外排除（可选）
# ⚠ 匹配式里**不要写引号**：变量展开不会剥离引号，`-name '*.apk'` 会变成「找名字里含
#    单引号的文件」而**静默一个都不匹配**（本脚本曾因此让 *.apk/*.aab 与 *.pyc 两段
#    长期空转，删不掉任何东西也不报错）。通配由 find 的 -name 自己解释，所以这里在
#    子 shell 里 `set -f` 关掉 pathname expansion —— 否则裸写的 `*.apk` 会先被**当前
#    目录**的同名文件顶替成一组具体路径，find 拿到的就不是模式了。
find_dirs() {
    local match="$1" extra="${2:-}"
    ( set -f; find . -type d \( $match \) -not -path "./.git/*" -not -path "*/node_modules/*" $extra -print0 2>/dev/null )
}

# 通用文件查找：$1 = find 的匹配表达式，$2 = 额外排除（可选）。引号禁忌同上。
find_files() {
    local match="$1" extra="${2:-}"
    ( set -f; find . -type f \( $match \) -not -path "./.git/*" $extra -print0 2>/dev/null )
}

echo "── C# 构建产物 bin/obj（含旁路 bin2/obj2）──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find_dirs "-name bin -o -name obj -o -name bin2 -o -name obj2")

echo "── VML 本地中间产物 (*.gen.vml / *.vmb) ──"
# 闸门：这两个 pattern 一旦命中 git 已跟踪文件，就整体跳过 —— 说明 pattern 写错了
# （比如误写成 *.vml），此时删下去等于删源码（Lib/ 下 2041 个 .vml 是 vendored 源）。
VML_TRACKED="$(git ls-files -- '*.gen.vml' '*.vmb' 2>/dev/null)"
if [[ -n "$VML_TRACKED" ]]; then
    echo "  ✘ 已跳过：清理 pattern 命中了 git 已跟踪文件（先修 pattern，别删）"
    echo "$VML_TRACKED" | sed 's/^/      /'
else
    while IFS= read -r -d '' f; do del "$f"; done < \
        <(find_files "-name *.gen.vml -o -name *.vmb")
fi

echo "── IDE 缓存 .vs ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name ".vs" -not -path "./.git/*" -print0 2>/dev/null)

echo "── node_modules 依赖 ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name node_modules -not -path "./.git/*" -print0 2>/dev/null)

echo "── StarGo 五子棋 MSVC 产物 ──"
if [[ -d StarGo ]]; then
    while IFS= read -r -d '' d; do del "$d"; done < \
        <(find StarGo -type d \( -name x64 -o -name Release -o -name Debug -o -name Win32 \) -print0 2>/dev/null)
    while IFS= read -r -d '' f; do del "$f"; done < \
        <(find StarGo -type f \( -name "*.exe" -o -name "*.obj" -o -name "*.ilk" -o -name "*.pdb" -o -name "*.lib" -o -name "*.exp" \) -print0 2>/dev/null)
fi

echo "── JetBrains Rider/IntelliJ 缓存 .idea ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name ".idea" -not -path "./.git/*" -print0 2>/dev/null)

echo "── VS/Rider 用户级配置 (*.user / *.suo) ──"
while IFS= read -r -d '' f; do del "$f"; done < \
    <(find . -type f \( -name "*.user" -o -name "*.suo" -o -name "*.binlog" \) \
        -not -path "./.git/*" -not -path "*/.vs/*" -not -path "*/bin/*" -not -path "*/obj/*" -print0 2>/dev/null)

echo "── 独立 publish 目录（bin/obj 内已由上面清理）──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name publish -not -path "./.git/*" -not -path "*/bin/*" -not -path "*/obj/*" -print0 2>/dev/null)

echo "── MAUI/Android Gradle 构建缓存 .gradle ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d -name ".gradle" -not -path "./.git/*" -print0 2>/dev/null)

echo "── MAUI/Android 打包产物 (*.apk / *.aab) ──"
while IFS= read -r -d '' f; do del "$f"; done < \
    <(find_files "-name *.apk -o -name *.aab")

echo "── vscode-extension 产物 (out/ 与 *.vsix) ──"
if [[ -d vscode-extension ]]; then
    while IFS= read -r -d '' d; do del "$d"; done < \
        <(find vscode-extension -type d -name out -not -path "*/node_modules/*" -print0 2>/dev/null)
    while IFS= read -r -d '' f; do del "$f"; done < \
        <(find vscode-extension -type f -name "*.vsix" -print0 2>/dev/null)
fi

echo "── Python 缓存 (__pycache__ / *.pyc / .venv / venv) ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d \( -name __pycache__ -o -name .venv -o -name venv \) \
        -not -path "./.git/*" -not -path "*/node_modules/*" -print0 2>/dev/null)
while IFS= read -r -d '' f; do del "$f"; done < \
    <(find_files "-name *.pyc")

echo "── 测试/打包产物 (TestResults / BenchmarkDotNet.Artifacts / AppPackages / .store / verify_build / stress-test-output) ──"
while IFS= read -r -d '' d; do del "$d"; done < \
    <(find . -type d \( -name TestResults -o -name BenchmarkDotNet.Artifacts -o -name AppPackages -o -name ".store" \
        -o -name verify_build -o -name stress-test-output \) \
        -not -path "./.git/*" -not -path "*/bin/*" -not -path "*/obj/*" -print0 2>/dev/null)

if [[ "$SCRATCH" -eq 1 ]]; then
    echo "── 草稿区 .scratch/（--scratch 显式开启）──"
    # 只删名为 .scratch 的目录（仓库根 + 各探针目录下都有），.scratch 本身是 gitignore 命中的
    while IFS= read -r -d '' d; do del "$d"; done < \
        <(find . -type d -name .scratch -not -path "./.git/*" -print0 2>/dev/null)
else
    echo "── 草稿区 .scratch/（默认保留，要清加 --scratch）──"
fi

echo "── dist/ 陈旧发布产物（保留 .waycoder 用户数据）──"
if [[ -d dist ]]; then
    while IFS= read -r -d '' item; do
        if [[ "$(basename "$item")" != ".waycoder" ]]; then
            del "$item"
        fi
    done < <(find dist -mindepth 1 -maxdepth 1 -print0 2>/dev/null)
else
    echo "  (无 dist 目录)"
fi

if [[ "$NO_GC" -eq 1 ]]; then
    echo "── Git 仓库压缩（--no-gc 已跳过）──"
else
    echo "── Git 仓库压缩（gc --aggressive + prune）──"
    if git rev-parse --git-dir >/dev/null 2>&1; then
        if [[ "$DRY_RUN" -eq 1 ]]; then
            echo "  [将执行] git gc --aggressive --prune=now"
        else
            GIT_BEFORE=$(du -sk .git 2>/dev/null | awk '{print $1}')
            git gc --aggressive --prune=now >/dev/null && echo "  [已压缩] git gc --aggressive --prune=now"
            GIT_AFTER=$(du -sk .git 2>/dev/null | awk '{print $1}')
            echo "  .git: ${GIT_BEFORE} KB → ${GIT_AFTER} KB"
        fi
    else
        echo "  (无 .git 仓库)"
    fi
fi

if [[ "$SKIPPED_SCRATCH" -gt 0 ]]; then
    echo "── .scratch/ 闸门 ──"
    echo "  .scratch/ 下 ${SKIPPED_SCRATCH} 项按约定保留（要清加 --scratch）"
fi

if [[ "$DRY_RUN" -eq 1 ]]; then
    echo "───────────────────────────────────────────"
    echo "🔎 干跑结束：以上为将删除项，未实际删除。"
else
    AFTER=$(du -sk . 2>/dev/null | awk '{print $1}')
    FREED=$(( (BEFORE - AFTER) / 1024 ))
    echo "───────────────────────────────────────────"
    echo "✅ 清理完成，释放约 ${FREED} MB。"
    echo "   （需重建：dotnet build 重新生成 bin/obj；扩展开发再 npm install）"
    echo "   保留：third_party/vml/Lib/**/*.vml（已跟踪的 vendored 源）、"
    echo "         WayCoder/works|saves、logs/、.waycoder/、dist/.waycoder、*.keystore、"
    echo "         .scratch/（草稿区，--scratch 才清）"
fi

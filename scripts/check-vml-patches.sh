#!/usr/bin/env bash
# 校验 `third_party/vml` 的本地适配是否**全部**进了 patches/。
#
# ## 为什么需要这个脚本
#
# `sync.sh` 的 rsync 列表 = `VMLAssembler VMLRuntime VMLPlugins VMLPrepares VMLTool
# VMLTranslators VMLToHex Lib`（含 `Lib`，带 `--delete`）——
# **凡在这个列表里、又没进补丁的改动，跑一次同步就没了**（手机上的游戏会直接编不过）。
#
# 而"我们改过哪些文件"**不能靠记忆枚举**：v0.96.181 就这么漏过一次 ——
# 手写的 0007 只包了三个提交里对 `Lib/` 的改动，卸下来一验才发现 `Lib/` 还差 3023 行
# （共享 UI 调用库那一整块），另有 `VMLPrepares/` 下 9 个文件也没有补丁。
#
# ## 判据
#
# 把补丁依次打到"vendor 那次提交"的树上，结果必须与当前工作区**逐字节相同**。
# 本仓库有那次 vendor 提交（此后没再跑过 sync.sh），所以这个判据在本地就能验 ——
# 不需要上游副本。
#
# 用法: scripts/check-vml-patches.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VML="$ROOT/third_party/vml"

VENDOR="$(git -C "$ROOT" log --diff-filter=A --format=%h -- third_party/vml/VERSION | tail -1)"
[ -n "$VENDOR" ] || { echo "✘ 找不到 vendor 提交（third_party/vml/VERSION 是谁加进来的？）" >&2; exit 1; }
echo "上游基准 = vendor 提交 $VENDOR"

WT="$(mktemp -d)"
cleanup() { git -C "$ROOT" worktree remove --force "$WT" >/dev/null 2>&1 || true; rm -rf "$WT"; }
trap cleanup EXIT

git -C "$ROOT" worktree add --detach "$WT" "$VENDOR" >/dev/null 2>&1

mkdir -p "$WT/third_party/vml/patches"
cp "$VML"/patches/*.patch "$WT/third_party/vml/patches/"
for p in "$WT"/third_party/vml/patches/*.patch; do
  name="$(basename "$p")"
  if git -C "$WT" apply "$p" 2>/dev/null; then
    echo "  ✔ $name"
  else
    echo "  ✘ $name 打不上 —— 补丁与"vendor 状态"不符（需要重新生成）" >&2
    exit 1
  fi
done

# 逐目录比对。跳过 csproj（那类由 sync.sh 的 python 步骤机械施加，不进补丁）、
# bin/obj（构建产物）、Examples（rsync 明确排除，改动能留住）。
fail=0
for d in VMLAssembler VMLRuntime VMLPlugins VMLPrepares VMLTool VMLTranslators VMLToHex Lib; do
  if diff -r -q -x '*.csproj' -x bin -x obj -x Examples \
        "$WT/third_party/vml/$d" "$VML/$d" > /tmp/wc-vml-patch-diff.txt 2>&1; then
    echo "  ✔ $d 一致"
  else
    echo "  ✘ $d 与「vendor+补丁」不一致 —— 这些改动没进补丁、同步时会丢：" >&2
    head -20 /tmp/wc-vml-patch-diff.txt >&2
    fail=1
  fi
done

# `vmltool.config.xml` 与 `VERSION` 不在 rsync 列表里、但由 sync.sh 用 `cp` 覆盖
# （见 sync.sh 的 `for f in VERSION LICENSE Directory.Build.props .editorconfig` 与那条
#  `cp "$UP/vmltool.config.xml"`）⇒ 改动同样会丢，必须一起比。
# ⚠ 这条是 v0.96.185 补上的：改 `vmltool.config.xml` 给各语言挂 `vmlui.vml` 时才发现
#    复核脚本根本没看这个文件 —— **判据漏了一个文件，等于那道防线对它是空的**。
for f in vmltool.config.xml VERSION; do
  if diff -q "$WT/third_party/vml/$f" "$VML/$f" > /dev/null 2>&1; then
    echo "  ✔ $f 一致"
  else
    echo "  ✘ $f 与「vendor+补丁」不一致 —— 同步时会被 cp 覆盖掉：" >&2
    diff "$WT/third_party/vml/$f" "$VML/$f" | head -10 >&2
    fail=1
  fi
done

[ "$fail" -eq 0 ] && echo "✅ 全部本地适配都在 patches/ 里（vendor + 补丁 == 当前工作区）"
exit "$fail"

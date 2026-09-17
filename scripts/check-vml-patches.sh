#!/usr/bin/env bash
# 校验 `third_party/vml` 的本地适配**全部**有据可查、不会在同步/重生成中悄悄丢。
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
# ## 两条判据（`Lib/` 与其余七个目录**不同**，2026-09-17 改）
#
# **① 其余七个目录 + `vmltool.config.xml` / `VERSION`：vendor + 补丁 == 工作区。**
#    `VMLPrepares/` 等是**手写源码**，补丁是它们的唯一凭据，逐字节比最直接。
#
# **② `Lib/`：按「可重生成」验，不再进补丁。**
#    `Lib/` 里的东西**几乎全是生成物** —— 2026-09-17 把调用约定统一之后重生成过一遍，
#    1621 个文件变了而 `Lib/shared/src/*.c` **一字未改**，这就是它「是生成物」的实证。
#    给生成物拍快照（往补丁里塞约 4 MB、而且每重生成一次就要再塞一份）没有意义；
#    有意义的判据是**它跟自己的输入对不对得上**：
#      ②a 生成物：用工作区自己的 GenLib 重生成到临时目录 → 与工作区**逐字节相同**
#          （这比「vendor+补丁」更强：它证明的是「Lib == f(源码, 前端, GenLib)」，
#            而不是「碰巧没人动过」；改坏了任何一环都会在这里现形）
#      ②b 手工部分：GenLib **不产出**的那些（手写 `.vml` / `modules.json` / `naming.json`
#          / 构建脚本 / README / 各语言 `Device/` …）仍按 vendor+补丁 验 ——
#          它们才是 `rsync --delete` 真会吃掉的东西
#
# 用法: scripts/check-vml-patches.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VML="$ROOT/third_party/vml"
TMP="$(mktemp -d)"
fail=0

VENDOR="$(git -C "$ROOT" log --diff-filter=A --format=%h -- third_party/vml/VERSION | tail -1)"
[ -n "$VENDOR" ] || { echo "✘ 找不到 vendor 提交（third_party/vml/VERSION 是谁加进来的？）" >&2; exit 1; }
echo "上游基准 = vendor 提交 $VENDOR"

WT="$TMP/wt"
cleanup() { git -C "$ROOT" worktree remove --force "$WT" >/dev/null 2>&1 || true; rm -rf "$TMP"; }
trap cleanup EXIT

git -C "$ROOT" worktree add --detach "$WT" "$VENDOR" >/dev/null 2>&1

mkdir -p "$WT/third_party/vml/patches"
cp "$VML"/patches/*.patch "$WT/third_party/vml/patches/"
for p in "$WT"/third_party/vml/patches/*.patch; do
  name="$(basename "$p")"
  if git -C "$WT" apply "$p" 2>/dev/null; then
    echo "  ✔ $name"
  else
    echo "  ✘ $name 打不上 —— 补丁与\"vendor 状态\"不符（需要重新生成）" >&2
    exit 1
  fi
done

# ⚠ 比较口径分两种（2026-09-17 定）：
#   · ①②-b 比的是「vendor 工作树 + 补丁」与「工作区」—— 两边行尾各由**各自提交的
#     .gitattributes** 决定（临时工作树签出的是 vendor 提交，用的是旧规则），所以只比
#     **内容**（--strip-trailing-cr）。行尾是 .gitattributes 的事，不是这条判据要管的事。
#   · ②a 比的是「GenLib 刚生成」与「工作区」—— 两边都在我们控制下，故仍**逐字节**。
#     这道逐字节正是抓到 GenLib 用 AppendLine（Environment.NewLine ⇒ Windows 写 CRLF）
#     导致 Lib/ 平台相关的那道网，绝不能放宽。
echo "── ① 手写源码：vendor + 补丁 == 工作区 ──"
# 逐目录比对。跳过 csproj（那类由 sync.sh 的 python 步骤机械施加，不进补丁）、
# bin/obj（构建产物）、Examples（rsync 明确排除，改动能留住）、.DS_Store
# （macOS 浏览过目录就会生成，未被 git 跟踪 ⇒ 只在工作区里有、临时工作树里没有，
#   不排除的话在 Mac 上**必然**误报「不一致」，这道防线就形同虚设）。
for d in VMLAssembler VMLRuntime VMLPlugins VMLPrepares VMLTool VMLTranslators VMLToHex; do
  if diff -r -q --strip-trailing-cr -x '*.csproj' -x bin -x obj -x Examples -x .DS_Store \
        "$WT/third_party/vml/$d" "$VML/$d" > "$TMP/diff.txt" 2>&1; then
    echo "  ✔ $d 一致"
  else
    echo "  ✘ $d 与「vendor+补丁」不一致 —— 这些改动没进补丁、同步时会丢：" >&2
    head -20 "$TMP/diff.txt" >&2
    fail=1
  fi
done

# `vmltool.config.xml` 与 `VERSION` 不在 rsync 列表里、但由 sync.sh 用 `cp` 覆盖
# （见 sync.sh 的 `for f in VERSION LICENSE Directory.Build.props .editorconfig` 与那条
#  `cp "$UP/vmltool.config.xml"`）⇒ 改动同样会丢，必须一起比。
# ⚠ 这条是 v0.96.185 补上的：改 `vmltool.config.xml` 给各语言挂 `vmlui.vml` 时才发现
#    复核脚本根本没看这个文件 —— **判据漏了一个文件，等于那道防线对它是空的**。
for f in vmltool.config.xml VERSION; do
  if diff -q --strip-trailing-cr "$WT/third_party/vml/$f" "$VML/$f" > /dev/null 2>&1; then
    echo "  ✔ $f 一致"
  else
    echo "  ✘ $f 与「vendor+补丁」不一致 —— 同步时会被 cp 覆盖掉：" >&2
    diff --strip-trailing-cr "$WT/third_party/vml/$f" "$VML/$f" | head -10 >&2
    fail=1
  fi
done

echo "── ② Lib：重生成 == 工作区（生成物）/ vendor + 补丁 == 工作区（手工部分）──"
REF="$TMP/.ref"          # 判定「GenLib 碰过哪些文件」的时间基准
touch -t 198001010000 "$REF"

cp -Rp "$VML/Lib" "$TMP/Lib"
# ⚠ 只把「非 C 源」的 mtime 拨到 1970。GenLib 的 `-b` 是「`.vml` 比 `.c` 旧才重编」，
#    把 `.c` 一起拨老会让它判定「不比源旧」而**整批跳过** —— 第一版原型就这么假绿过
#    （日志写着「0 编译, 106 跳过」，共享库一个都没重生成，比对自然全过）。
find "$TMP/Lib" -type f -not -path "*/shared/src/*" -exec touch -t 197001010000 {} +

if (cd "$VML" && dotnet run --project tools/GenLib -- -A -r "$TMP" ) > "$TMP/genlib.log" 2>&1; then
  echo "  ✔ GenLib -A 重生成成功（$(grep -a -m1 '完成:' "$TMP/genlib.log" || echo '')）"
else
  echo "  ✘ GenLib -A 重生成失败 —— Lib/ 与它的输入已经对不上了：" >&2
  tail -15 "$TMP/genlib.log" >&2
  exit 1
fi

# ②a GenLib 写过的文件 = mtime 不再停在 1970 的那些
# ⚠ `.DS_Store` 必须排除：macOS 浏览过目录就会生成、且**未被 git 跟踪** ⇒
#    临时工作树里没有它，不排除的话「本地新增的手工文件」这条**必然**误报（已实测踩到）。
find "$TMP/Lib" -type f -not -name .DS_Store -newer "$REF" \
  | sed "s|^$TMP/Lib/||" | sort > "$TMP/gen.txt"
gen_count="$(wc -l < "$TMP/gen.txt" | tr -d ' ')"
[ "$gen_count" -gt 0 ] || { echo "  ✘ 没认出任何生成物 —— 时间基准失效了，这道判据会空过" >&2; exit 1; }

gen_bad=0
while IFS= read -r f; do
  if [ ! -f "$VML/Lib/$f" ]; then
    echo "  ✘ Lib/$f 是重生成产出，工作区里却没有" >&2; gen_bad=1
  elif ! cmp -s "$TMP/Lib/$f" "$VML/Lib/$f"; then
    echo "  ✘ Lib/$f 与重生成结果不一致（跑一次 GenLib -A 就好）" >&2; gen_bad=1
  fi
done < "$TMP/gen.txt"
if [ "$gen_bad" -eq 0 ]; then
  echo "  ✔ 生成物 $gen_count 个，与重生成结果逐字节相同"
else
  fail=1
fi

# ②b 手工部分 = 工作区 Lib 里、GenLib 不产出的那些
( cd "$VML" && find Lib -type f -not -name .DS_Store ) | sed 's|^Lib/||' | sort > "$TMP/all.txt"
comm -23 "$TMP/all.txt" "$TMP/gen.txt" > "$TMP/hand.txt"
hand_bad=0
while IFS= read -r f; do
  [ -n "$f" ] || continue
  if [ ! -f "$WT/third_party/vml/Lib/$f" ]; then
    echo "  ✘ Lib/$f 是本地新增的手工文件，没进补丁（同步时会被 --delete 删掉）" >&2; hand_bad=1
  elif ! diff -q --strip-trailing-cr "$WT/third_party/vml/Lib/$f" "$VML/Lib/$f" > /dev/null 2>&1; then
    echo "  ✘ Lib/$f 的手工改动没进补丁" >&2; hand_bad=1
  fi
done < "$TMP/hand.txt"

# 反方向：vendor+补丁 里有、工作区却没有的（本地删过）—— rsync 会把它加回来
( cd "$WT/third_party/vml" && find Lib -type f -not -name .DS_Store ) | sed 's|^Lib/||' | sort > "$TMP/wt-all.txt"
while IFS= read -r f; do
  [ -n "$f" ] || continue
  echo "  ✘ Lib/$f 在「vendor+补丁」里存在、工作区里没有（本地删过？同步会加回来）" >&2
  hand_bad=1
done < <(comm -13 "$TMP/all.txt" "$TMP/wt-all.txt")

if [ "$hand_bad" -eq 0 ]; then
  echo "  ✔ 手工部分 $(wc -l < "$TMP/hand.txt" | tr -d ' ') 个，与「vendor+补丁」一致"
else
  fail=1
fi

[ "$fail" -eq 0 ] && echo "✅ 本地适配全部有据可查（手写源码进补丁；Lib/ 生成物与输入一致）"
exit "$fail"

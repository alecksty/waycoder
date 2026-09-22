#!/usr/bin/env bash
# 校验「ISA 文档 == 真实的操作码表」—— 加了指令却忘写文档，必须当场报出来。
#
# ## 为什么要这条判据
#
# 判据不是"文档写得全不全"（那要靠人读），而是**机械可求差**的那件事：
#   `VMLAssembler/OpCode.cs` 里每一个操作码，`VML_ASSEMBLY_SPEC.md` 里必须**出现过**。
#
# 实测踩过一次（2026-09-22）：v0.96.360 往 ISA 里加了 **13 条无符号指令**
# （ZEXTL/DIVU/…/JBE，号段 113–125），**代码、VM、汇编器、前端全接好了、端到端 11/11 通过**，
# 唯独**文档一个字没写** —— 而"指令集少了说明"这种事**没有任何编译器会报错**，
# 于是它可以一直烂在那儿，直到有人想手写汇编时才发现「这条指令文档里根本没有」。
# 这正是本仓最怕的那类失败：**静默**。
#
# ## 判据怎么读
#
# 只看"名字有没有出现"，**不判语义对不对**（那必须人读）。所以它**通过 ≠ 文档写对了**，
# 但**不通过 = 一定有指令没写**。反过来说：**别为了让这条变绿而把名字硬塞进去** ——
# 名字进去了、语义是错的/过时的，这条抓不到，反而更糟。
#
# ⚠ 名字匹配用**整词**（`grep -w`），否则 `JB` 会被 `JBE` 里的子串骗过去。
#
# 用法: scripts/check-asm-doc.sh
set -uo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OPCODE="$ROOT/third_party/vml/VMLAssembler/OpCode.cs"
SPEC="$ROOT/third_party/vml/VMLAssembler/VML_ASSEMBLY_SPEC.md"

for f in "$OPCODE" "$SPEC"; do
    [ -f "$f" ] || { echo "✘ 找不到 $f" >&2; exit 1; }
done

# 枚举成员：缩进 + 全大写名 + `= 数字`（值与注释都在行尾，不管）
mapfile -t OPS < <(grep -oE '^[[:space:]]+[A-Z][A-Z0-9_]*[[:space:]]*=[[:space:]]*[0-9]+' "$OPCODE" \
    | grep -oE '[A-Z][A-Z0-9_]*' | sort -u)

total="${#OPS[@]}"
[ "$total" -gt 0 ] || { echo "✘ 从 OpCode.cs 里一个操作码都没抽出来 —— 判据会空过（格式变了？）" >&2; exit 1; }

missing=()
for op in "${OPS[@]}"; do
    grep -qw -- "$op" "$SPEC" || missing+=("$op")
done

echo "── ISA 文档覆盖 ──"
echo "  OpCode.cs 操作码 $total 个；文档未提及 ${#missing[@]} 个"

if [ "${#missing[@]}" -eq 0 ]; then
    echo "  ✔ 全部已提及（⚠ 只证明「名字在」，语义对不对要人读）"
    exit 0
fi

echo "  ✘ 以下操作码在 VML_ASSEMBLY_SPEC.md 里**一次都没出现**（加了指令忘写文档？）：" >&2
for i in "${missing[@]}"; do echo "      $i" >&2; done
echo "  ⇒ 去 VMLAssembler/VML_ASSEMBLY_SPEC.md 的「指令集」表与对应小节补上。" >&2
exit 1

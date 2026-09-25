#!/usr/bin/env bash
# BASIC 前端**语言特性判据** —— 每个用例一段最小复现，判据是**打印出来的值**。
#
# 为什么要单独立一套（与 `vml-out-probe` / `vml-abi-probe` 并列）：
# 那两套压的是"跨语言输出"与"调用约定"，而**这门语言本身能不能用**在当时没有任何判据 ——
# `gorilla.bas` 头部记了一串"必须绕开"的写法（数组不能用、SUB 里不能套括号、不能写 AND…），
# 那些记录来自 v0.96.3xx 期间的实测；本轮逐条重测，发现**绝大多数已经修好**，
# 而真正还坏着的（FOR 负步长 / CHR$ / SELECT CASE / DATA-READ）当时一条判据都没有。
#
# 判据形态：**顶层 vs SUB 内**各来一遍。历史经验是"顶层基本是好的、SUB 体才是重灾区"
# （同一段表达式在两种位置给不同答案），所以只用顶层写的判据会漏掉一半。
#
# 用法：
#   scripts/vml-basic-probe/run.sh            # 全部
#   scripts/vml-basic-probe/run.sh 01 06      # 按编号前缀挑
#
# 依赖桌面的 `scripts/vmlcli`（与手机端等价的编译+运行流水线）。
# ⚠ 前置：先 `dotnet build scripts/vmlcli/vmlcli.csproj -c Release`。
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# CLI 可以被 `VMLCLI=<路径>` 覆盖 —— 用来跑**另一份构建**（例如自测用的
# 旁路输出目录 `.scratch/vc/`：主输出目录常年被别的进程/别的 agent 占着锁）。
CLI="${VMLCLI:-$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll}"
CASES="$ROOT/scripts/vml-basic-probe/cases"

if [ ! -f "$CLI" ]; then
    echo "✘ 找不到 $CLI —— 先跑： dotnet build scripts/vmlcli/vmlcli.csproj -c Release"
    exit 2
fi

pick=("$@")
pass=0; fail=0; red=0
for f in "$CASES"/*.bas; do
    name="$(basename "$f")"
    if [ ${#pick[@]} -gt 0 ]; then
        hit=0
        for p in "${pick[@]}"; do [[ "$name" == "$p"* ]] && hit=1; done
        [ "$hit" = 1 ] || continue
    fi
    out="$(dotnet "$CLI" "$f" 2>&1)"
    # 用例里带 `' EXPECT:` 注释行给期望值；逐行比对
    exp="$(grep -o "EXPECT:.*" "$f" | sed 's/EXPECT: *//')"
    # **已知红**：这条判据压的是"已确认、尚未修"的缺陷。留它在套件里的价值是
    # **钉住症状**（修好那天它会自己变绿），但它不该被算进"通过/失败" ——
    # 否则「N/N 全绿」这个信号就被一条已知项永久污染了（vml-abi-probe 同款处置）。
    known_red=0
    if grep -q "KNOWN-RED" "$f"; then known_red=1; fi
    if [ -z "$exp" ]; then
        printf "  %-16s %s\n" "$name" "$(echo "$out" | grep -E '^[A-Za-z0-9-]+=' | tr '\n' ' ')"
        continue
    fi
    # ⚠ 取实际输出用的是"标识符 =... "这个形状，而**标识符可以是汉字**
    #   （词法走 .NET 的 char.IsLetter，是 Unicode 感知的）。
    #   原先这里写死 ASCII 字符类 `[A-Za-z0-9+-]+` ⇒ 汉字行一条都匹配不上，
    #   实际值恒为空、用例**永远红**，而 `--` 直接跑程序却完全正常 ——
    #   判据坏在尺子上，不是坏在被测物上。改成"行首到 = 之间不含空格"。
    #   ⚠ **要同时排除控制字符**：`CLS` 之类会往 stdout 吐 `\x1b[2J\x1b[H`，
    #     放宽成"行首到 = 之间没有空格"之后，`\x1b[2J\x1b[HB=2` 这种行也会被当成判据
    #     （实测把用例 54 弄红了）。标识符的字符集是 `[:alnum:]` + `_` + **汉字**，
    #     与"控制字符"不重叠 —— 用 POSIX 字符类一次写清。
    got="$(echo "$out" | grep -oE '^[^[:space:][:cntrl:]=]+=.*' | tr '\n' '|' | sed 's/|$//')"
    if [ "$got" = "$exp" ]; then
        printf "  ✅ %-16s %s\n" "$name" "$got"
        pass=$((pass+1))
    elif [ "$known_red" = 1 ]; then
        printf "  ⚠️  %-16s 已知红（缺陷未修）：期望 %s / 实得 %s\n" "$name" "$exp" "$got"
        red=$((red+1))
    else
        printf "  ❌ %-16s\n     期望 %s\n     实得 %s\n" "$name" "$exp" "$got"
        fail=$((fail+1))
    fi
done

echo "──────────────────────────────────────────────"
echo "通过 $pass / 失败 $fail / 已知红 $red"
[ "$fail" -eq 0 ]

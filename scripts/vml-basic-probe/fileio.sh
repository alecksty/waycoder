#!/usr/bin/env bash
# BASIC 文件 I/O 的判据 —— `OPEN` / `CLOSE` / `PRINT #` / `INPUT #` / `LINE INPUT #`。
#
# ## 为什么单列一条（不放进 run.sh 的 cases/）
#
# ① **要在临时目录里跑**：这组用例会**真的建文件**。放进 `cases/` 的话（run.sh 在仓库根跑）
#    每跑一次就在仓库里留一个 `*.txt` —— 与 `palette.sh`/`put-bitmap.sh` 单列是同一个理由。
# ② **要检查文件内容**：`cases/` 的判据只有"程序 stdout 里那几行 `名字=值`"，
#    而文件 I/O 最容易出的错是**写进去的内容不对**（甚至根本没写），
#    那在 stdout 上是看不出来的 —— 必须 `cat` 出来比。
# ③ **要两步**：先写、再读，两次运行用**同一个文件**。这是"往返"判据，
#    单跑一次证明不了句柄表、定位、关闭那几处都对。
#
# ## 判据的分量
#
# 这一组是 v0.96.503 那一批修复的闸门。修之前**三处同时坏**（句柄根本没存、
# 用了**设备**号而不是**文件**号、`INPUT #` 从**键盘**读），症状分别是
# "文件里什么都没有"、"写进去就没了"、"跑起来卡住不动" —— 三种都**不报错**。
set -u

CLI="${VMLCLI:-scripts/vmlcli/bin/Release/net10.0/vmlcli.dll}"
if [ ! -f "$(dirname "$0")/../../$CLI" ] && [ ! -f "$CLI" ]; then
    echo "✘ 找不到 vmlcli：$CLI（先 dotnet build scripts/vmlcli/vmlcli.csproj -c Release）" >&2
    exit 2
fi
CLI_ABS="$(cd "$(dirname "$CLI")" && pwd)/$(basename "$CLI")"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
cd "$WORK" || exit 2

pass=0; fail=0
ok()   { echo "  ✅ $1"; pass=$((pass+1)); }
bad()  { echo "  ❌ $1"; echo "     期望 $2"; echo "     实得 $3"; fail=$((fail+1)); }

# ── ① 写：三项各一行 ────────────────────────────────────────────────
cat > w.bas <<'EOF'
OPEN "t.txt" FOR OUTPUT AS #1
PRINT #1, "hello"
PRINT #1, 42
PRINT #1, 3.5
CLOSE #1
EOF
dotnet "$CLI_ABS" w.bas --timeout 20 >/dev/null 2>&1

if [ -f t.txt ]; then
    got="$(tr '\n' '|' < t.txt)"
    exp='hello|42|3.5|'
    [ "$got" = "$exp" ] && ok "写：文件内容 = $got" || bad "写：文件内容" "$exp" "$got"
else
    bad "写：文件未创建" "t.txt 存在" "不存在"
fi

# ── ② 读回来（字符串 / 整数 / 浮点三类各一）─────────────────────────
cat > r.bas <<'EOF'
OPEN "t.txt" FOR INPUT AS #2
INPUT #2, a$
INPUT #2, b
INPUT #2, c#
CLOSE #2
PRINT "A=[", a$, "] B=", b, " C=", c#
EOF
out="$(dotnet "$CLI_ABS" r.bas --timeout 20 2>/dev/null | grep -E '^A=' | head -1)"
exp='A=[hello] B=42 C=3.5'
[ "$out" = "$exp" ] && ok "读：$out" || bad "读：往返" "$exp" "$out"

# ── ③ APPEND：第二次打开要**接着写**，不是从头覆写 ──────────────────
cat > a.bas <<'EOF'
OPEN "t.txt" FOR APPEND AS #3
PRINT #3, "tail"
CLOSE #3
EOF
dotnet "$CLI_ABS" a.bas --timeout 20 >/dev/null 2>&1
got="$(tr '\n' '|' < t.txt)"
exp='hello|42|3.5|tail|'
[ "$got" = "$exp" ] && ok "APPEND：$got" || bad "APPEND：接着写" "$exp" "$got"

# ── ④ LINE INPUT #：读整行（含空格），不被空格切开 ──────────────────
cat > l.bas <<'EOF'
OPEN "t.txt" FOR INPUT AS #4
LINE INPUT #4, s$
CLOSE #4
PRINT "L=[", s$, "]"
EOF
out="$(dotnet "$CLI_ABS" l.bas --timeout 20 2>/dev/null | grep -E '^L=' | head -1)"
exp='L=[hello]'
[ "$out" = "$exp" ] && ok "LINE INPUT #：$out" || bad "LINE INPUT #" "$exp" "$out"

# ── ⑤ FREEFILE：返回**当前没用**的最小文件号 ────────────────────────
#
# ⚠ 判据里刻意**先开 #0**（而不是 #1）：宿主给的第一个句柄**就是 0**，
#   而"这一格是空的"也用 0 表示 —— 两者撞在一起时 `FREEFILE` 会把
#   已经打开的文件当成空闲。实测就是这样先踩到的（B 应为 1 却得 0）。
cat > ff.bas <<'EOF'
PRINT "A="; FREEFILE
OPEN "t.txt" FOR OUTPUT AS #0
PRINT "B="; FREEFILE
OPEN "t2.txt" FOR OUTPUT AS #1
PRINT "C="; FREEFILE
CLOSE #0
PRINT "D="; FREEFILE
EOF
out="$(dotnet "$CLI_ABS" ff.bas --timeout 20 2>/dev/null | grep -E '^[ABCD]=' | tr '\n' ' ')"
exp='A=0 B=1 C=2 D=0 '
[ "$out" = "$exp" ] && ok "FREEFILE：$out" || bad "FREEFILE（占用/释放）" "$exp" "$out"

echo "──────────────────────────────────────────────"
echo "通过 $pass / 失败 $fail"
[ "$fail" -eq 0 ] || exit 1

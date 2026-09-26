#!/usr/bin/env bash
# vmlcli 宿主验收 —— 「桌面上能做的事，与手机上一样多」。
#
# 两条独立判据，都不靠肉眼看日志：
#   【A】syscall 探针：把宿主给程序的每个值用音效频率报出来（[vml-audio] tone hz=…），
#        与期望序列**逐条**比对。覆盖 屏幕/存档/对话框/输入/定时器。
#   【B】像素体检：跑 Examples/c/draw_colors.c，把 ui_present 拍下的那一帧渲成 PNG，
#        再**逐格取样**核实颜色（本机无 PIL —— 自带一个 zlib 解码的采样器）。
#
# 用法：bash scripts/vmlcli-verify/run.sh
# ⚠ 前置：先 `dotnet build scripts/vmlcli/vmlcli.csproj -c Release`（本脚本不代劳，
#   避免把"构建失败"与"验收失败"混成同一个退出码）。
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CLI="$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll"
HERE="$ROOT/scripts/vmlcli-verify"
VML_HOME="$ROOT/third_party/vml"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

fail=0
if [ ! -f "$CLI" ]; then
    echo "✘ 找不到 $CLI —— 先跑： dotnet build scripts/vmlcli/vmlcli.csproj -c Release"
    exit 2
fi

# ══════════════════════════════════════════════════════════════════════════
# 【A】syscall 探针
# ══════════════════════════════════════════════════════════════════════════
echo "── 【A】宿主 syscall 探针（scripts/vmlcli-verify/host_probe.c）"

VML_HOME="$VML_HOME" dotnet "$CLI" "$HERE/host_probe.c" --timeout 20 \
    --screen 320x240 \
    --answer cancel --answer 你好 \
    --input "$HERE/events.txt" \
    --store "$TMP/store.json" >"$TMP/out.txt" 2>"$TMP/err.txt"

got="$(grep -o 'tone hz=[0-9]*' "$TMP/err.txt" | sed 's/tone hz=//' | tr '\n' ' ')"
want="10320 10240 10001 11002 12104 9999 15001 16006 1001 4037 1002 4013 1011 5800 6600 1012 7001 17001 1009 2001 3007 "

if [ "$got" = "$want" ]; then
    echo "  ✔ 20 项全部符合（屏幕 / 存档 / 对话框 / 输入 / 窗口消息 / 定时器）"
else
    echo "  ✘ 探针输出不符"
    echo "     期望：$want"
    echo "     实得：$got"
    fail=1
fi

# 存档真的落盘了吗（键已加 vml. 前缀 —— 与手机端 Preferences 同一套语义）
if grep -q '"vml.probe.k": *"hi"' "$TMP/store.json" 2>/dev/null; then
    echo "  ✔ 存档已落盘（--store，键带 vml. 前缀）"
else
    echo "  ✘ 存档没写出预期的键"; fail=1
fi

# ══════════════════════════════════════════════════════════════════════════
# 【B】像素体检（draw_colors.c）
# ══════════════════════════════════════════════════════════════════════════
echo "── 【B】绘图帧像素体检（Examples/c/draw_colors.c）"

cat > "$TMP/close.txt" <<'EOF'
# 程序画完并 present 之后关窗（不等超时，免得白等）
1500 close
EOF

VML_HOME="$VML_HOME" dotnet "$CLI" "$ROOT/third_party/vml/Examples/c/draw_colors.c" \
    --timeout 30 --screen 480x640 --input "$TMP/close.txt" \
    --frame "$TMP/colors.png" >/dev/null 2>"$TMP/colors.log"

if [ ! -f "$TMP/colors.png" ]; then
    echo "  ✘ 没导出 PNG —— 看上面的 stderr"; fail=1
else
    if python3 "$HERE/sample_png.py" "$TMP/colors.png"; then
        echo "  ✔ 15 格取样 + 渐变两端全部符合"
    else
        echo "  ✘ 像素体检不通过"; fail=1
    fi
fi

# ══════════════════════════════════════════════════════════════════════════
# 【C】多点触控注入（touch_probe.c）
# ══════════════════════════════════════════════════════════════════════════
# 治的是：桌面输入脚本原先只 `PostInput`（投队列消息），**不碰 `ui_touch` 的槽位表**
# ⇒ 任何 `ui_touch(slot)` 轮询在桌面永远读到"没按"，多点触控程序只能上真机验。
# 判据：脚本投哪几个槽位，日志里就该出现哪几个频率（`10<slot>00 + x/10`）。
echo "── 【C】多点触控注入（scripts/vmlcli-verify/touch_probe.c）"

printf '%s\n' \
    '400 touchn_down 1 60 300' \
    '400 touchn_down 2 130 300' \
    '900 touchn_up 1 60 300' \
    '900 touchn_up 2 130 300' \
    '1400 close' > "$TMP/touch.txt"

VML_HOME="$VML_HOME" dotnet "$CLI" "$HERE/touch_probe.c" --timeout 20 \
    --screen 320x240 --input "$TMP/touch.txt" \
    >"$TMP/touch_out.txt" 2>"$TMP/touch_err.txt"

# `sort -u`：程序每 16ms 一拍都在报"这个槽位按着"，日志里是连续一串，去重后才好比对。
gotm="$(grep -o 'tone hz=[0-9]*' "$TMP/touch_err.txt" | sed 's/tone hz=//' | sort -u | tr '\n' ' ')"
wantm="10106 10213 "
if [ "$gotm" = "$wantm" ]; then
    echo "  ✔ 两个槽位各自被 ui_touch 读到（10106=slot1/x60、10213=slot2/x130）"
else
    echo "  ✘ 多点触控槽位没被读到"
    echo "     期望：$wantm"
    echo "     实得：$gotm"
    fail=1
fi

# 老语法（单指 touch*，等价槽位 0）必须照旧可用 —— 这条钉住"改动是超集不是替换"
printf '%s\n' \
    '400 touchdown 45 300' \
    '900 touchup 45 300' \
    '1400 close' > "$TMP/touch_old.txt"

VML_HOME="$VML_HOME" dotnet "$CLI" "$HERE/touch_probe.c" --timeout 20 \
    --screen 320x240 --input "$TMP/touch_old.txt" \
    >"$TMP/touch_old_out.txt" 2>"$TMP/touch_old_err.txt"

goto="$(grep -o 'tone hz=[0-9]*' "$TMP/touch_old_err.txt" | sed 's/tone hz=//' | sort -u | tr '\n' ' ')"
if [ "$goto" = "10004 " ]; then
    echo "  ✔ 老语法 touchdown 仍走槽位 0（10004=slot0/x45）"
else
    echo "  ✘ 老的单指语法被改坏了"; echo "     期望：10004 "; echo "     实得：$goto"; fail=1
fi

# ══════════════════════════════════════════════════════════════════════════
# 【D】声音：把 `--wav` 录下来的 PCM 做频率检测
# ══════════════════════════════════════════════════════════════════════════
# 治的是「音对不对只能靠耳朵」：录成 WAV 之后，"复音有没有真叠加""音高有没有算错"
# 都成了可数字判定的东西（`tone_check.py` 用 Goertzel 量频率）。
# ⚠ 它**不需要声卡** —— 没设备时 DesktopAudio 会另起线程按实时速率录。
echo "── 【D】复音与音准（Examples/c/audio_test.c + --wav + tone_check.py）"

VML_HOME="$VML_HOME" dotnet "$CLI" "$ROOT/third_party/vml/Examples/c/audio_test.c" \
    --timeout 45 --screen 360x620 --wav "$TMP/at.wav" \
    >"$TMP/at_out.txt" 2>"$TMP/at_err.txt"

if [ ! -s "$TMP/at.wav" ]; then
    echo "  ✘ --wav 没产出文件（看 $TMP/at_err.txt）"
    tail -3 "$TMP/at_err.txt" | sed 's/^/     /'
    fail=1
else
    python3 "$HERE/tone_check.py" "$TMP/at.wav" || fail=1
fi

echo
if [ "$fail" = 0 ]; then echo "全部通过"; else echo "有失败项"; fi
exit "$fail"

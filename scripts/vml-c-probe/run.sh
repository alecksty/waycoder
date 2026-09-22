#!/usr/bin/env bash
# C 前端**指针 / 结构体 / 内存**判据 —— 每个用例一段最小复现，判据是**打印出来的值**。
#
# 为什么要单独立一套：`vml-out-probe` 压的是"跨语言输出"、`vml-abi-probe` 压的是调用约定，
# 两边用的都是**十几行的直筒程序**（几个整数、几个字符串），**一个指针都没碰**。
# 而"给手机版写一层 curses 库"这件事，内部全是 `WINDOW *`（结构体指针）、`char **`、
# `malloc`/`free` —— 这些写法在 C 前端到底能不能用，**此前一条判据都没有**。
#
# 第一版的直接动因（2026-09-21）：移植 nyancat 时 `nyan_show(char ** fr)` 编译全绿、
# 运行不报错，**但取到的全是空**（3 帧只发出 3 个 ESC）—— 即"能编、能跑、结果静默错"，
# 正是本仓反复出现的那种形态。逐个探针查比逐个项目试快得多。
#
# ## 判据怎么读（本轮补，三条都会让人读到假结果）
#
# ① **只取 stdout**：`vmlcli` 的契约是 `stdout = 程序自己的输出` / `stderr = 编译链接日志`。
#    本脚本早期写的是 `2>&1`，于是几十行"已注册前端…"把判据行**淹掉**。
# ② **`timeout` 不是通用命令**：它是 GNU coreutils 的，**macOS 自带没有**（装了 coreutils
#    才有 `gtimeout`）。早期直接写 `timeout 120 dotnet …`，在 macOS 上每次都
#    `command not found` ⇒ `out` 拿到的是那句报错 ⇒ **每个用例都"实得为空"**，
#    看上去像"全红"，而真去查用例又都是对的。见下面 `run_to()`。
# ③ **ANSI 转可读字面量**：`ESC[46m` → `\e[46m`。`conio` 这类库唯一的产物**就是** ANSI
#    字节流（它画进命令行页的字符网格），不转义的话判据行里全是控制符，颜色/属性
#    **一个都判不了**。纯文本用例不受影响（没有 ESC 就没有替换）。
#
# ## 写用例的两条约定
#
# · 判据行 = **独占一行**的 `KEY=值`。⚠ 用 `conio` 画过东西之后，库输出的字符会与
#   判据**粘在同一行**（实测 `menuP2=14,5`：`menu` 是库画的、`P2=14,5` 才是判据）——
#   而 `^KEY=` 是行首锚定的，粘住就匹配不到。**判据 `printf` 前先打个 `\n`** 即可
#   （用 `printf("\n…")` 而不是 `putch('\n')`：后者会推光标、动了被测量的状态）。
# · `// EXPECT:` 给期望值（`|` 分隔）；**已知缺陷**的用例加 `// KNOWN-RED`，
#   它不计入"通过/失败"，只钉住症状（修好那天会自己变绿）。
#
# 用法：
#   scripts/vml-c-probe/run.sh            # 全部
#   scripts/vml-c-probe/run.sh 02 05      # 按编号前缀挑
#
# 依赖桌面的 `scripts/vmlcli`（与手机端等价的编译+运行流水线）。
# ⚠ 前置：先 `dotnet build scripts/vmlcli/vmlcli.csproj -c Release`。
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CLI="$ROOT/scripts/vmlcli/bin/Release/net10.0/vmlcli.dll"
CASES="$ROOT/scripts/vml-c-probe/cases"

if [ ! -f "$CLI" ]; then
    echo "✘ 找不到 $CLI —— 先跑： dotnet build scripts/vmlcli/vmlcli.csproj -c Release"
    exit 2
fi

RUN_TIMEOUT=120

# 跨平台超时：macOS 自带没有 `timeout`/`gtimeout`，两个都没有时退回 `vmlcli --timeout`
# （那个只管**运行**阶段，编译本身在桌面是秒级，够用）。
run_to() {
    if command -v timeout >/dev/null 2>&1; then timeout "$RUN_TIMEOUT" "$@"
    elif command -v gtimeout >/dev/null 2>&1; then gtimeout "$RUN_TIMEOUT" "$@"
    else "$@"; fi
}

# ANSI → 可读字面量。用 sed 的 **BRE**（不写 `?`/`+` 这类 GNU 扩展：BSD sed 的 BRE 里
# `?` 是**字面量**，写了会静默不匹配）；字符类里的 `?` 是字面量，两边都认。
ESC=$(printf '\033')
ansi2txt() { sed "s/${ESC}\[\([0-9;?]*\)\([A-Za-z]\)/\\\\e[\1\2/g"; }

pick=("$@")
pass=0; fail=0; red=0
for f in "$CASES"/*.c; do
    name="$(basename "$f")"
    if [ ${#pick[@]} -gt 0 ]; then
        hit=0
        for p in "${pick[@]}"; do [[ "$name" == "$p"* ]] && hit=1; done
        [ "$hit" = 1 ] || continue
    fi

    # 用例可以用 `// STDIN: <文本>` 声明脚本化标准输入（给 getch/scanf 这类读字节的程序）。
    # 换行写字面 `\n`，由 vmlcli 自己翻译（见它 CaptureIo 的注释）。
    #
    # ⚠ **必须锚行首**。写成 `grep -o 'STDIN: .*'` 会**先命中用例注释里那句示例**
    #   （"`// STDIN: wsad`（run.sh 认这行…"）—— 于是喂进去的是那段说明文字。
    #   而那串文字的**前四个字符恰好也是 `wsad`**，前四条判据照样绿，
    #   只有第五条（第 5 个键）才露馅。判据装置自己骗人，比被测代码出错更难查。
    stdin_spec="$(grep -o '^// STDIN: .*' "$f" | head -1 | sed 's|^// STDIN: *||')"
    stdin_args=()
    [ -n "$stdin_spec" ] && stdin_args=(--stdin "$stdin_spec")

    # 用例可以用 `// ARGS: -l foo` 给程序喂命令行参数（**按空白切分**，不做引号解析 ——
    # 与 `vmlcli --arg` 的语义一致：要带空格的参数就整段当一个值给）。
    # 同样必须锚行首：注释里提到这个形状的地方不能被当成声明（STDIN 那条踩过）。
    args_spec="$(grep -o '^// ARGS: .*' "$f" | head -1 | sed 's|^// ARGS: *||')"
    arg_args=()
    if [ -n "$args_spec" ]; then
        for one in $args_spec; do arg_args+=(--arg "$one"); done
    fi

    # ⚠ 用**裸 `mktemp`**，不要 `-t vmlprobe` —— `-t <模板>` 是 BSD/macOS 的写法，
    #   GNU coreutils 要求模板里至少 3 个 X，于是 Windows(Git Bash)/Linux 上直接
    #   `mktemp: too few X's in template 'vmlprobe'` ⇒ errf 为空 ⇒ **整套用例全报
    #   「实得 」而没有任何一条真正跑过**（本仓 2026-09-22 实测：0/27，看着像全红，
    #   其实是脚手架没跑起来）。本目录其他脚本用的都是裸 `mktemp`，照那个写。
    errf="$(mktemp)"
    # `${arr[@]+...}`：bash 3.2（macOS 自带）在 `set -u` 下展开**空数组**会报 unbound variable
    out="$(run_to dotnet "$CLI" "$f" --timeout 60 ${stdin_args[@]+"${stdin_args[@]}"} ${arg_args[@]+"${arg_args[@]}"} 2>"$errf" | ansi2txt)"

    # ── `// EXPECT-COMPILE-ERROR: <子串>`：这条用例**应当编译失败** ──
    #
    # 加这条约定的理由：**"编译器该报错却崩了/静默通过"这一类此前一条判据都没有**，
    # 而它们恰恰是最难发现的（编译期就结束，跑不到任何输出）。
    # 实测 `int main(){ (void)getenv(); }` 报的是
    # `Index was out of range. Must be non-negative...` —— 一行 .NET 异常文本，
    # **没有文件名、没有行号、没有诊断码**，而它被当成"正常的编译失败"吞掉了。
    #
    # 判据是 **stderr 里出现该子串**（诊断文本从 stderr 出，见脚本开头那三条约定）。
    # 可以写**多条**（每条都必须出现）—— 用于压"一次要把该报的都报出来"，
    # 例如 `cases/30` 同时缺 getenv/POKE/setenv 的参数：只报第一个的话，
    # 用户得改一个编一次、来回好几轮。
    exp_errs="$(grep -o "EXPECT-COMPILE-ERROR:.*" "$f" | sed 's/EXPECT-COMPILE-ERROR: *//')"
    if [ -n "$exp_errs" ]; then
        missing=""
        while IFS= read -r one; do
            [ -z "$one" ] && continue
            grep -qF "$one" "$errf" 2>/dev/null || missing="$missing 「$one」"
        done <<< "$exp_errs"
        if [ -z "$missing" ]; then
            printf "  ✅ %-18s 编译失败且诊断齐备（%s 条断言）\n" "$name" "$(printf '%s\n' "$exp_errs" | wc -l | tr -d ' ')"
            pass=$((pass+1))
        else
            printf "  ❌ %-18s\n     期望编译失败且诊断含%s\n     实得 stderr 尾部 %s\n" \
                "$name" "$missing" "$(tail -3 "$errf" 2>/dev/null | tr '\n' ' ')"
            fail=$((fail+1))
        fi
        rm -f "$errf"
        continue
    fi

    # 用例里带 `// EXPECT:` 注释行给期望值；逐行比对
    exp="$(grep -o "EXPECT:.*" "$f" | sed 's/EXPECT: *//')"
    # **已知红**：这条判据压的是"已确认、尚未修"的缺陷。留它在套件里的价值是
    # **钉住症状**（修好那天它会自己变绿），但它不该被算进"通过/失败" ——
    # 否则「N/N 全绿」这个信号就被一条已知项永久污染了（vml-abi-probe 同款处置）。
    known_red=0
    if grep -q "KNOWN-RED" "$f"; then known_red=1; fi
    if [ -z "$exp" ]; then
        printf "  %-18s %s\n" "$name" "$(echo "$out" | grep -E '^[A-Za-z0-9-]+=' | tr '\n' ' ')"
        rm -f "$errf"; continue
    fi
    got="$(echo "$out" | grep -oE '^[A-Za-z0-9+-]+=.*' | tr '\n' '|' | sed 's/|$//')"
    if [ "$got" != "$exp" ] && [ "$known_red" = 0 ]; then
        printf "  ❌ %-18s\n     期望 %s\n     实得 %s\n" "$name" "$exp" "$got"
        # 实得为空十有八九是**编译/链接就失败了**（这类噪音全在 stderr）——
        # 不把 stderr 尾巴带出来，看到的就只是"空的"，没法往下查。
        if [ -z "$out" ] && [ -s "$errf" ]; then
            printf "     （stderr 尾部）%s\n" "$(tail -3 "$errf" | tr '\n' ' ')"
        fi
        fail=$((fail+1))
        rm -f "$errf"
        continue
    fi

    # ── `// EXPECT-VML: <子串>`：**产物文本**里必须出现该子串 ──────────────────
    #
    # 为什么要有这条（它是唯一一条不跑程序、只看产物的）：
    # 「**数据只在别的数据里被引用**」这一类东西（指针表 `char *t[] = {"abc"}` 的字符串、
    # 多级指针表、`argv` 那种），在**运行路径**上活着，在**文本路径**上却会被
    # 死代码消除整批删掉 —— 因为判"谁被引用"的那段只认**标签名字符串**，
    # 而文本往返一趟之后元素变成了 `LabelRef`。
    # 症状非常隐蔽：数据段里 `.word L_x` **照旧原样输出**，只是 `L_x` 退化成
    # `.text` 里的一个**空标签** —— 看起来"数据都在"，只有内容是空的。
    # 用户侧的后果是**手机「VML 编译」出来的 `.vml` 跑起来指针全 NULL**
    #（那条路正是 `ToString()` → 存盘 → 独立汇编运行）。
    # 判据只能是"产物里有没有那段正文"—— 运行路径跑一百遍也照不出来。
    # 实测（2026-09-22）：修前 `grep -c '"aa"' out.vml` == 0，修后 == 1。
    vml_expects="$(grep -o '^// EXPECT-VML: .*' "$f" | head -1 | sed 's|^// EXPECT-VML: *||')"
    if [ -n "$vml_expects" ]; then
        vml_out="$(mktemp)"
        run_to dotnet "$CLI" "$f" --vml "$vml_out" >/dev/null 2>&1
        vml_missing=""
        while IFS= read -r one; do
            [ -z "$one" ] && continue
            grep -qF "$one" "$vml_out" 2>/dev/null || vml_missing="$vml_missing 「$one」"
        done <<< "$vml_expects"
        rm -f "$vml_out"
        if [ -n "$vml_missing" ]; then
            printf "  ❌ %-18s\n     产物文本里缺%s（只被别的数据引用的东西被死代码消除删掉了）\n" \
                "$name" "$vml_missing"
            fail=$((fail+1))
            rm -f "$errf"
            continue
        fi
    fi

    if [ "$got" = "$exp" ]; then
        printf "  ✅ %-18s %s\n" "$name" "$got"
        pass=$((pass+1))
    elif [ "$known_red" = 1 ]; then
        printf "  ⚠️  %-18s 已知红（缺陷未修）：期望 %s / 实得 %s\n" "$name" "$exp" "$got"
        red=$((red+1))
    fi
    rm -f "$errf"
done

echo "──────────────────────────────────────────────"
echo "通过 $pass / 失败 $fail / 已知红 $red"
[ "$fail" -eq 0 ]

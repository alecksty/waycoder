#!/usr/bin/env bash
# 老程序**兼容性**探针 —— 跑 `third_party/vml/compat-corpus/<语言>/` 下的遗留程序。
#
# ⚠ 与 `vml-diag-probe/examples-build.sh` 的分工（**别混**）：
#   · `examples-build` 管**要随 APK 分发**的示例（`Examples/`）—— 那是**精选**的：
#     老语言留几个就够（用户 2026-09-28 定：「老语言举几个 BGI 例子就够了」），
#     因为 `Examples/` 会进 `vml_lib.zip`，堆满编不过的老程序只会把失败清单淹掉。
#   · **本脚本管「兼容能力」**：老语言的**遗留代码库很大**，多一层兼容就有一批老程序
#     能跑起来（用户 2026-09-28：「这些古老语言一定遗留不少代码，只要对他们的代码库
#     做适当的兼容，很多代码又可以跑起来」）。判据不能是"我修了感觉能跑" ——
#     得有一批**真实语料**兜着，否则修完这一个、下一个同样的写法照样编不过。
#   所以语料放 `compat-corpus/`（**不在 `Examples/` 下 ⇒ 不进 APK 包**），
#   与"要不要收进示例"是两件事。
#
# 输出两样东西：
#   ① **通过率** —— 兼容进度的判据（数字要能拿来比）。
#   ② **缺口分布** —— 按错误消息聚合，**下一批修什么就看它**
#      （按影响面排序，别按示例逐个修 —— 单个示例常是多层缺口叠加，
#       修一层只前进一层；按缺口修才是一次修一片）。
#
# 用法：
#   scripts/vml-compat-probe/run.sh              # 全部语言
#   scripts/vml-compat-probe/run.sh pascal       # 只跑 pascal
#
# ⚠ 退出码：**只要有失败就是 1**（这是"兼容进度"的闸门，不是"示例是否健康"）——
#   想要"看进度但不红"就自己 `|| true`。

source "$(dirname "${BASH_SOURCE[0]}")/../lib/portable-timeout.sh"
CP_TIMEOUT="${CP_TIMEOUT:-60}"   # 单个程序的编译时限（秒）

set -uo pipefail
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DLL="${VMLCLI:-$REPO/scripts/vmlcli}/bin/Release/net10.0/vmlcli.dll"
CORPUS="$REPO/third_party/vml/compat-corpus"

[ -f "$DLL" ] || { echo "✘ 找不到 vmlcli：$DLL" >&2; exit 2; }
[ -d "$CORPUS" ] || { echo "✘ 找不到语料目录：$CORPUS" >&2; exit 2; }

ok=0; fail=0; skipped=0; failed=(); errs=(); noerr=(); missing=()

# ── 清单：先列出来，进度才有分母 ───────────────────────────────────────────
langs=("$@")
[ ${#langs[@]} -eq 0 ] && mapfile -t langs < <(ls "$CORPUS")

targets=()
for lang in "${langs[@]}"; do
    [ -d "$CORPUS/$lang" ] || continue
    for f in "$CORPUS/$lang"/*; do
        [ -f "$f" ] || continue
        case "$f" in
            # 非源文件（许可/来源/文档）——它们不是程序，拿它们当"编不过"只会训人忽略红灯
            *.md|*.txt|*.h|*.json|*.sh|*.bat|*.ps1|*.xml|*.bi) continue ;;
        esac
        targets+=("$f")
    done
done

total=${#targets[@]}
echo "语料 $total 份（$CORPUS）"
[ "$total" -eq 0 ] && { echo "（没有可跑的语料）"; exit 0; }

i=0
for f in "${targets[@]}"; do
    i=$((i + 1))
    rel="${f#"$CORPUS"/}"
    printf '\r[%d/%d] %-46s' "$i" "$total" "$rel"

    # ⚠ 时限**先在父 shell 里算好**（`budget_clamp`）—— 在 `$( … )` 里算的话那两个变量
    #   回不来（子 shell）；⚠ 变量名**别用 `to`**（`to=` 在 zsh/某些 shell 里像保留字，
    #   而 `examples-build.sh` 里用 `to` 只是因为它跑在 bash 下 —— 这里换个名字少一类坑）。
    lim="$(budget_clamp "$CP_TIMEOUT")"
    # ⚠ 判据**必须是退出码**，不能是"输出里有没有 error"：vmlcli 的失败路径并不都带
    #   `error:` 字样（「✘ 找不到文件」等早退分支一个字都不匹配）⇒ 靠 grep 会把一整类
    #   失败读成"通过"（`examples-build` 就为此修过一次，实测漏报了 avc_file_select.pas）。
    # ⚠ `--vml`（编完即退、不运行）：这是**编译兼容性**探针，跑游戏既慢又没意义，
    #   而且游戏会等输入 ⇒ 每条都撞超时。
    out="$(cd "$(dirname "$f")" && run_with_timeout "$lim" dotnet "$DLL" "$(basename "$f")" \
        --vml "${TMPDIR:-/tmp}/_compat.vml" 2>&1)"
    rc=$?

    if [ "$rc" -eq 0 ]; then
        ok=$((ok + 1))
        trip_reset
    else
        # ⚠ 「缺配套文件」**不计入 fail** —— 那不是前端编不过，是这份语料不成立
        #   （见下面那条判据）。单独计数、单独列名，否则它会一直占着失败栏。
        if grep -q "找不到包含文件" <<< "$out"; then
            skipped=$((skipped + 1))
            missing+=("$rel")
            errs+=("(缺配套文件：程序自带的 \${I} 包含文件没进来)")
            continue
        fi
        fail=$((fail + 1))
        failed+=("$rel")
        if [ "$rc" -eq 124 ]; then
            errs+=("(超时：${lim}s 未返回)")
            # 连着好几条超时 = 整体挂了，就地停（见 portable-timeout.sh）
            trip_on_timeout
        else
            # 缺口分类：抓第一行错误，把数字抹平成占位符（否则同一个缺口会因为行号不同
            # 被拆成几百条，聚合就没意义了）
            # ⚠ **先摘"缺配套文件"的** —— 那不是前端编不过，是**这份语料不成立**：
            #   程序自带的数据/包含文件没跟着进来（`{$I cube.vec}`，实测 tpdem_* 系列），
            #   与 `examples-build` 里 `file_io.*` 那条同源（「不是坏了的例子，
            #   是已经不成立的例子」）。混在失败栏里只会训练人忽略红灯。
            #   ⚠ **只认「找不到包含文件」**：`{$I}` 拉进来的必然是**程序自带的**文件；
            #   而「找不到单元」**不能**这么判 —— 那可能是真该补的库
            #   （`ktp_rose` 的 `uses Graph` 要的正是补 `graph.pas` 的类型段）。
            if printf '%s' "$out" | grep -q "找不到包含文件"; then
                missing+=("$rel")
                errs+=("(缺配套文件：程序自带的 \${I} 包含文件没进来)")
            else
            msg=$(printf '%s' "$out" | grep -oE "error: [^（，(]*" | head -1 | sed 's/[0-9]\+/N/g' | cut -c1-58)
            if [ -z "$msg" ]; then
                # ⚠ 「无 error 行」单独成一档**并留下文件名** —— 它是一整类**未知**缺口
                #   （崩溃 / 早退 / 前端抛 .NET 异常），只说"有 9 个"没法查；
                #   带上文件名才点得进去（实测这 9 个里就藏着「字符串常量数组」
                #   那条 `FormatException`）。
                msg="(无 error 行：崩溃 / 早退)"
                noerr+=("$rel")
            fi
            errs+=("$msg")
            fi
        fi
    fi
done
printf '\r\033[K'

echo "--------------------------------------------------------------"
echo "兼容 通过 $ok / 失败 $fail / 缺配套(不计) $skipped   （共 $total 份 · 单个时限 ${CP_TIMEOUT}s）"

if [ "$fail" -gt 0 ]; then
    echo
    echo "── 缺口分布（**按影响面排序，下一批修最大的那类**）──"
    printf '%s\n' "${errs[@]}" | sort | uniq -c | sort -rn | head -25

    echo
    if [ ${#missing[@]} -gt 0 ]; then
        echo "── 「缺配套文件」的那几份（**不计入前端缺陷**：程序自带的 \${I} 包含文件没跟着语料进来）──"
        printf '  %s\n' "${missing[@]}"
        echo
    fi

    if [ ${#noerr[@]} -gt 0 ]; then
        echo "── 「无 error 行」的那几份（**要挨个点进去**：崩溃 / 早退 / 前端抛异常）──"
        printf '  %s\n' "${noerr[@]}"
        echo
    fi

    echo "── 失败清单 ──"
    printf '  %s\n' "${failed[@]}" | sed "s|^|  |" | head -40
    [ "$fail" -gt 40 ] && echo "  …（还有 $((fail - 40)) 份）"
    exit 1
fi

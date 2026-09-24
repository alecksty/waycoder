#!/usr/bin/env bash
# 把 VML 标准库打成 App 内置资产 WayCoder.Maui/Resources/Raw/vml_lib.zip。
#
# 为什么是一个 zip 而不是逐文件 MauiAsset：Lib/ 有 5300+ 个文件 / 39 MB，
# 打进 APK 是 6 MB；而且 MAUI **没有「列出资产目录」的 API** —— 逐文件放进去
# 就没法在运行时知道有哪些文件，必须先写死一份清单（又一张平行表）。
#
# zip 里的**顶层条目是 `Lib/`、`vmltool.config.xml` 与 `Examples/`** —— 与解压后
# `EnsureLibExtracted()` 期望的目录布局一一对应（它返回的 root 下面就是 Lib/）。
# `Examples/` 是给 `MauiBootstrap.EnsureExamples()` 用的（它把 `Examples/` 下**平铺一层**
# 解到 `<workspace>/examples/`，让用户与 AI 在手机上就有现成的示例程序可跑）。
#
# ⚠ **示例要打进去，光是放进 `Examples/` 目录不够** —— 这个脚本原先只打 Lib 与配置，
#    于是 `EnsureExamples()` 一个条目都找不到、手机上没有示例（而桌面看目录一切正常）。
#
# ⚠ **示例只收 1~3 层，绝不递归整棵树。** 上游 `Examples/` 底下曾挂着 stb / stm32 那种整套工程，
#    递归进来就是上千个文件（那批现在已按 `PRUNED.txt` 清掉，但仍不该放开递归）。
#    收三层，实测量过代价很小（第 1 层 1 个、第 2 层 116 个、第 3 层 **21 个**）：
#      `Examples/README.md` / `Examples/<语言>/<文件>` / `Examples/<语言>/<子目录>/<文件>`
#    第 3 层是 2026-09-22 加的 —— 此前只到第 2 层，于是 `Examples/c/old/*.c`
#    这种"按类型分目录"的老程序**静默不进包**，而桌面直接读仓库、看不出来。
#    （`EnsureExamples()` 早已改成**按 zip 条目保留目录结构**解包，所以三层不会互相覆盖。）
#
# ⚠ `*.gen.vml` 是本地跑出来的中间产物（每个几十 KB），不进包。
#
# ⚠ **第三方 Pascal 老程序语料不进包**（`Examples/pascal/` 下的 8 类前缀，
#    见下面 `EXAMPLES_PASCAL_CORPUS_RE`）—— 它们**留在仓库里**，只是不随包分发。
#
# ⚠ `vmltool.config.xml` 必须打进去，少它整个链接阶段会被跳过（见 MauiVml 注释）。
#
# ⚠ 改完这个 zip 不用去改 `MauiVml.LibVersion` —— 那边用**内容指纹**判断要不要重新解压
#    （改了文件指纹自然变，不改就不解压）。`LibVersion` 只是写进标记给人看的。
#    指纹落在随包的 `Resources/Raw/vml_lib.hash` 里，由本脚本末尾算出（算法见那段注释）。
#    **app 侧的解压位置是 App 私有目录（`AppDataDirectory/vml`）**，与 `Global.Home` 无关 ——
#    那个位置会随「所有文件访问」权限跳，标记跟着跳就等于每次授权都白解压一遍。
set -euo pipefail

# ⚠ **别用裸 `python`** —— macOS 上只有 `python3`，裸 `python` 报「未找到命令」退出 127。
#    而本脚本的失败点恰好落在「zip 已写好、指纹还没算」之间 ⇒ 留下**新 zip + 旧指纹**的矛盾状态。
#    消费方 `MauiVml.EnsureLibExtracted()` 的解压判据是**指纹而非 zip** ⇒ 设备永远不重新解压，
#    **修复就这么静默地不生效**（实测踩过：补丁 0033 给 Lib 新增 `lua/luatable.vml`，
#    桌面能跑、手机上 Lua 一直报 `未找到标签: lua_table_get`）。
PYTHON="${PYTHON:-}"
if [ -z "$PYTHON" ]; then
  for c in python3 python; do command -v "$c" >/dev/null 2>&1 && { PYTHON="$c"; break; }; done
fi
[ -n "$PYTHON" ] || { echo "✘ 找不到 python3/python，本脚本需要 Python 3" >&2; exit 1; }
"$PYTHON" -c 'import sys; sys.exit(0 if sys.version_info.major >= 3 else 1)' \
  || { echo "✘ $PYTHON 不是 Python 3" >&2; exit 1; }

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VML="$ROOT/third_party/vml"
OUT="$ROOT/WayCoder.Maui/Resources/Raw/vml_lib.zip"

[ -d "$VML/Lib" ] || { echo "✘ 找不到 $VML/Lib" >&2; exit 1; }
[ -f "$VML/vmltool.config.xml" ] || { echo "✘ 找不到 $VML/vmltool.config.xml" >&2; exit 1; }

TMP="$OUT.tmp"
rm -f "$TMP"

# ⚠ **移动端不需要 PC / DOS / 单片机那一类库** —— 它们提供的是那几种机器上的硬件或
#    操作系统接口，手机上既没有对应设备、也没有对应调用方（手机上跑的是 `vmlui`
#    那套 `ui_*` 接口）。排掉它们能少打一批字节，也少一批"文件不存在"的悬空 `.linked`。
#
#    每个模块都有**两份**：`shared/<名>.vml`（实现）与各语言目录下的 `<lang>/<名>.vml`
#    （GenLib 生成的转发 shim）⇒ **两份都要排**，只排 shared 那份等于没排。
#
#    判据是「这个模块提供的接口在手机上有没有对应的东西」，不是"名字看着像 PC"：
#      · dos / vga_text              —— DOS 与 PC 文本控制台（VGA 文本页、BIOS 端口…）
#      · graphics / graph            —— BGI 绘图（`initgraph`/`putpixel` 那一套，Turbo C 时代）
#      · browser_gfx                 —— 浏览器 canvas 专用
#      · gpio                        —— 单片机引脚
#    ⚠ `device` / `device64` **没排** —— 名字像硬件层，但它可能是 VM 自己的设备抽象
#      （`ui_*` 那条链上要用），排错会让手机上的绘图/音效失灵。要用先查清调用方再排。
#    ⚠⚠ **这张表已经因为同一个错误错过两次了：按"名字听着像 DOS/PC"排，而实现早已原生。**
#      判据必须是「**读它的实现**，看它落在哪一层」，不是看它叫什么。两次都是
#      「手机上编译直接报『未定义的函数 X』、而桌面上（`vmlcli`，不走这个 zip）跑得好好的」：
#      · **`conio`**（2026-09-21 移出）—— `Lib/shared/src/conio.c` 全部落在 `ui_*` 上
#        （`ui_win_open`/`ui_rect`/`ui_text`/`ui_present`/`ui_wait`）。症状是 `gotoxy` 未定义。
#      · **`crt`**（2026-09-24 移出）—— 它的头一行就写着「**替换 VGA 显存写入方案，使用
#        ANSI escape codes**」，是**纯软件** 80×25 文本屏 + `SYSCALL 400` 输出 ANSI，
#        文件里一个 `0xB8000`/`outb` 都没有。症状是 `CRT_TEXTCOLOR` / `CRT_TEXTBACKGROUND` /
#        `CRT_CLRSCR` 三个未定义 —— 而 BASIC 前端的 **ui_* 后端**在 `COLOR` 语句上
#        **照旧会发这三个调用**（`CodeGenerator.Qbasic.UiGfx.cs` 里写明了"文本模式下
#        COLOR 仍然要改终端配色"），于是**任何含 `COLOR` 的程序在手机上都编不过**：
#        实测 `Examples/basic/gfx_demo.bas`、`gfx_modes.bas` 就是这么挂的。
#        ⇒ 加新模块时**必须回来检查这张表**：它是"同一份清单在第二处实现"的典型形态。
MOBILE_EXCLUDE=(dos vga_text graphics graph browser_gfx gpio)

# ── 示例文件清单：**只在这里算一次**，两条打包路径共用 ─────────────────────────
#
# ⚠⚠ 这里是本仓头号坑（「同一规则两处实现」）的重点防守位。
#
#   本脚本有**两条打包路径**（有 `zip` 走 Info-ZIP；没有则走 Python `zipfile` ——
#   Windows Git Bash 默认不带 zip，**本机走的就是 Python 那条**）。而「收哪些示例」
#   这条规则原先在两边**各写了一遍**：shell 里三个 `find`、Python 里 `examples_files()`
#   三层 `os.listdir`。⇒ 层数、排除项这类规则只要改一处忘另一处，两条路径就会产出
#   **不同的包**，而「本机走哪条」只取决于有没有 `zip` —— 本地验过的与别人机器上打出来的
#   不是同一个包，且**一个字节的报错都没有**。
#
#   选的是「让第二条实现**不复存在**」，而不是「两处引用同一个变量」：
#   清单在这里算好、落进一个临时文件，两条路径都只是**消费**它 —— Python 侧那份目录遍历
#   （`examples_files()`）已随本次改动删除。少一份实现就少一处能漂移的地方，
#   那比「记得同步」可靠。（若将来必须在某条路径上再写一遍遍历，那就说明该把它抽成
#   一个独立脚本、两边都调它，而不是在两条路径里各写一份。）
#
#   排除判据按**文件名前缀**（这批语料带得很整齐）：
EXAMPLES_PASCAL_CORPUS_RE='^Examples/pascal/(avc_|g7iles_|gcorail_|gmsdos_|gnc_|ktp_|swag_|tpdem_)'
#
#   为什么排它们：`Examples/pascal/` 下这 8 类是**真实存在的老 Pascal 程序原文**
#   （SWAG 语料、Turbo Pascal 示例盘、各类 DOS 工具与老游戏）。它们是「老程序兼容性」
#   这条线的**判据来源**（目标原话是"拿真实存在的老程序原文，一个字符都不改，编译+运行"）
#   ⇒ **仓库里一份都不能删**（`scripts/vml-diag-probe/examples-build*.sh`、
#   `scripts/vml-out-probe/` 等也都引用 `Examples/pascal/`）。
#   但它们在手机上**基本编不过**（实测这一批只有个位数能编过），随包发出去 =
#   用户在示例列表里看到一片点开就报错的东西（**要的是"不进包"，不是"删掉"**）。
#   保留的好示例（`demo_*.pas` / `catch.pas` / `sysinfo.pas`）照旧进包。
#
#   ⚠ 这 8 个前缀在 `docs/老程序兼容性.md` §十七 里**也列了一份**（连同语料来源、
#     许可证与逐条缺陷档案）。那**是说明文档、不参与打包**，本文件这一行才是"进不进包"
#     的可执行真源；两边都改的场合只有"语料增删了"一种，改完顺手把文档那行也对一下。
#
#   ⚠ 改这个正则 = 改「手机上能见到哪些 Pascal 示例」，改完**必须重跑本脚本**：
#     `vml_lib.zip` 是签入的生成物，不重跑则手机侧的内容指纹不变、改动会被静默吞掉。
EX_LIST="$(mktemp)"
{
    cd "$VML"
    find Examples -maxdepth 1 -type f ! -name '*.gen.vml'
    find Examples -mindepth 2 -maxdepth 2 -type f ! -name '*.gen.vml'
    find Examples -mindepth 3 -maxdepth 3 -type f ! -name '*.gen.vml'
} | sort | grep -Ev "$EXAMPLES_PASCAL_CORPUS_RE" > "$EX_LIST" || true
# （`|| true` 是**为了把话说清楚**，不是为了放行：`grep` 一行都没匹配上时返回 1，
#   而 `set -e` 会抢在下面那条检查之前就退出 —— 结果是一个**没有理由的退出码 1**。
#   现在三条失败路径（grep 无匹配 / `find` 出错 / 排除规则写过头）都汇到下面这一句话。）
[ -s "$EX_LIST" ] || { echo "✘ 示例清单为空：$EX_LIST（find/grep 出错？或排除规则写过头了）" >&2; exit 1; }
# **反方向检查**：必须保留的好示例一份都不能少。
# ⚠ 这条不是冗余 —— 上面那个"排除正则"改过头时（比如把 `^Examples/pascal/` 整个排掉），
#   清单会**合法地**变小、后面"包里的条目数 vs 清单"那道自查也照样通过，
#   只有手机上的示例列表才看得出来。这里钉住这几份**用户点名要留的**（尤其 `demo_*`）。
for must in Examples/pascal/demo_std.pas Examples/pascal/demo_tty.pas \
            Examples/pascal/demo_bgi.pas Examples/pascal/demo_ui.pas \
            Examples/pascal/catch.pas Examples/pascal/sysinfo.pas; do
    grep -qxF "$must" "$EX_LIST" || {
        echo "✘ 该保留的示例被排掉了：$must（排除规则写过头了）" >&2; exit 1; }
done

# -X 去掉多余的文件属性（否则同样的内容在 mac/linux 上产出的 zip 字节不同，
#    指纹会跟着变、白解压一次；虽然不影响正确性，但没必要）
if command -v zip >/dev/null 2>&1; then
    # 把 `MOBILE_EXCLUDE` 展开成 zip 的 `-x` 排除模式：shared/<名>.vml 与 <lang>/<名>.vml
    ZIP_EX=()
    for m in "${MOBILE_EXCLUDE[@]}"; do
        ZIP_EX+=(-x "Lib/shared/$m.vml" -x "Lib/*/$m.vml")
    done
    # ⚠ `-x` 必须**跟在要打包的路径之后**：Info-ZIP 把 `-x` 之后所有不以 `-` 开头的参数
    #   一律当成排除模式 ⇒ 写成 `zip ... "${ZIP_EX[@]}" Lib vmltool.config.xml` 会把
    #   `Lib` 与 `vmltool.config.xml` 也当排除项，一个文件都选不中，报
    #   `zip error: Invalid command arguments (nothing to select from)`。
    #   （`MOBILE_EXCLUDE` 为空时 `-x` 根本不出现，所以这个 bug 只在加了排除项之后才暴露。）
    ( cd "$VML" && zip -q -r -X "$TMP" Lib vmltool.config.xml "${ZIP_EX[@]}" )
    # 示例：吃上面那份**算好的清单**（`-@` = 从 stdin 读要打包的文件名）。
    # ⚠ 不能用 `zip -X "$TMP" $(cat "$EX_LIST")`：那是靠 word-splitting 传参，
    #   文件名一带空格就断成两截（`Examples/...` 目前没有这种名字，但没必要留这个雷）。
    ( cd "$VML" && zip -q -X "$TMP" -@ < "$EX_LIST" )
else
    # 没有 `zip` 的机器（例如 Windows Git Bash 默认不带）走 Python —— 用**固定时间戳**
    # 保证同样的内容每次产出同样的字节（与 `zip -X` 的意图一致）。
    echo "ℹ 未找到 zip，改用 Python zipfile"
    "$PYTHON" - "$VML" "$TMP" "$EX_LIST" "${MOBILE_EXCLUDE[@]}" <<'PY'
import os, sys, zipfile
root, out, ex_list = sys.argv[1], sys.argv[2], sys.argv[3]
exclude = set(sys.argv[4:])   # 移动端不需要的模块名（见脚本头部的 MOBILE_EXCLUDE）

with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    def add(rel):
        rel = rel.replace(os.sep, "/")
        # 移动端不需要的 PC/DOS/单片机模块：`Lib/shared/<名>.vml` 与
        # `Lib/<语言>/<名>.vml` 两份都排（只排一份等于没排）。
        base = os.path.basename(rel)
        if rel.startswith("Lib/") and base.endswith(".vml") and base[:-4] in exclude:
            return
        full = os.path.join(root, rel)
        if os.path.isdir(full):
            for name in sorted(os.listdir(full)):
                add(rel + "/" + name)
            return
        info = zipfile.ZipInfo(rel, date_time=(1980, 1, 1, 0, 0, 0))
        info.compress_type = zipfile.ZIP_DEFLATED
        info.external_attr = 0o644 << 16
        with open(full, "rb") as f:
            z.writestr(info, f.read())
    add("Lib")
    add("vmltool.config.xml")
    # 示例：**清单由 shell 侧算好传进来**（`$EX_LIST`，见脚本上半段那段注释）——
    # 这里刻意**不做任何目录遍历**：一旦这里再走一遍 `os.listdir`，
    # 「收哪些示例」就又变回两处实现，而两条路径"本机走哪条"只取决于有没有 `zip`。
    with open(ex_list, encoding="utf-8") as fh:
        for rel in (ln.strip() for ln in fh):
            if rel and not rel.endswith(".gen.vml"):
                add(rel)
PY
fi

# ── 自查：包里 `Examples/` 条目数必须与**清单逐条对上** ────────────────────────
#
# ⚠ 这条不是装饰。`zip` 那条路**在某些机器上根本跑不到**（本机 Windows Git Bash 不带 zip
#    ⇒ 走的是 Python 那条），而"打包语句写坏了"的表现是**包里少一批示例**、
#    而整个脚本**退出码仍是 0、指纹照算** —— 只有在手机上打开示例列表才看得出来。
#    这里拿清单当分母当场对一次，**两条路径在同一处被验到**（这正是上面把清单抽出来的红利：
#    有了唯一的一份"应该有啥"，才可能回头验"实际有啥"）。
EX_WANT="$(wc -l < "$EX_LIST" | tr -d '[:space:]')"
EX_GOT="$("$PYTHON" -c '
import sys, zipfile
with zipfile.ZipFile(sys.argv[1]) as z:
    print(sum(1 for n in z.namelist() if n.startswith("Examples/")))
' "$TMP")"
[ "$EX_GOT" = "$EX_WANT" ] || {
    echo "✘ 包里的示例条目（$EX_GOT）与清单（$EX_WANT）对不上 —— 打包那条路径出错了" >&2; exit 1; }
rm -f "$EX_LIST"

# ── Help/：把 App 内置的说明文档也打进包 ────────────────────────────────
#
# 用途与 Examples/ 一样：解到工作区 `help/`，**用户能自己翻阅**（文件页里点开看，
# 或者拿去喂给 AI）。App 内「使用说明」读的是 `Resources/Raw/help/**` 那一份，
# 这里是**同一个源的另一个落地点** —— `scripts/make-ui-help.py` /
# `make-lang-help.py` 生成的还是那一份，不存在两份要对着改。
#
# 走**暂存目录**而不是直接加 `Resources/Raw/help`：zip 里的路径必须带 `Help/` 前缀，
# 而 Info-ZIP 只能按"当前目录下的相对路径"存档，直接加会写成 `Resources/Raw/help/...`，
# 与 `EnsureHelp()` 期望的布局对不上。
HELP_SRC="$ROOT/WayCoder.Maui/Resources/Raw/help"
if [ -d "$HELP_SRC" ]; then
    STAGE="$(mktemp -d)"
    mkdir -p "$STAGE/Help"
    cp -R "$HELP_SRC/." "$STAGE/Help/"
    if command -v zip >/dev/null 2>&1; then
        ( cd "$STAGE" && zip -q -r -X "$TMP" Help )
    else
        "$PYTHON" - "$STAGE" "$TMP" <<'PYEOF'
import os, sys, zipfile
stage, out = sys.argv[1], sys.argv[2]
base = os.path.join(stage, "Help")
with zipfile.ZipFile(out, "a", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for dirpath, _, files in os.walk(base):
        for f in sorted(files):
            full = os.path.join(dirpath, f)
            rel = "Help/" + os.path.relpath(full, base).replace(os.sep, "/")
            info = zipfile.ZipInfo(rel, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            with open(full, "rb") as fh:
                z.writestr(info, fh.read())
PYEOF
    fi
    rm -rf "$STAGE"
    echo "ℹ 已把说明文档打进包（Help/）"
fi

# ⚠ **先算指纹、再落盘 —— 顺序不能反。**
#    原先的顺序是「先把 zip 移到 $OUT（就在这一行）→ 再算指纹」，于是指纹那一步一旦失败，
#    就留下**新 zip + 旧指纹**的矛盾状态。而设备的解压判据是**指纹**、不是 zip
#    ⇒ 它永远不重新解压、**修复静默不生效**（补丁 0033 新增 `Lib/lua/luatable.vml` 就是这么被吞掉的：
#    桌面读 `Lib/` 直接跑得通，手机上一路报 `未找到标签: lua_table_get`）。
#    现在两个都先写临时文件、最后一起移入；**指纹失败则两个都不动**（留旧的、自洽）。

# ── 随包的**内容指纹**（`vml_lib.hash`）────────────────────────────────────
#
# 解压判据用它。为什么不是直接哈希 zip：zip 里带着**文件时间戳**，同一份内容重新打一次包
# 字节就不一样（本脚本虽然尽量固定时间戳，但 `zip -X` 只管得住扩展属性那部分），
# 于是"内容没变却白解压 39 MB"。
#
# 所以这份指纹按「条目名 + 长度 + 内容」算，**完全不看时间戳** ⇒ 内容一样指纹就一样。
# 读回来的成本也从 6 MB 降到几十字节（应用每次启动都会读一次）。
#
# ⚠ 消费方是 `MauiVml.LibFingerprint()`，只有那一处。改这里的算法必须同步改那边，
#    否则老包会反复解压（那边读不到/读不出就退回哈希整个 zip，不会崩，只是慢）。
HASH_OUT="$ROOT/WayCoder.Maui/Resources/Raw/vml_lib.hash"
"$PYTHON" - "$TMP" "$HASH_OUT.tmp" <<'PY'
import sys, zipfile, hashlib
zip_path, out = sys.argv[1], sys.argv[2]
h = hashlib.sha256()
with zipfile.ZipFile(zip_path) as z:
    for name in sorted(z.namelist()):
        if name.endswith('/'):
            continue
        data = z.read(name)
        h.update(name.encode('utf-8')); h.update(b'\0')
        h.update(str(len(data)).encode()); h.update(b'\0')
        h.update(data)
digest = h.hexdigest()
with open(out, "w", encoding="utf-8", newline="\n") as f:
    f.write(digest)
print("  指纹:", digest)
PY

# 两个都算好了才落盘 —— 保证「zip 与指纹要么都是新的、要么都是旧的」，不会一新一旧
mv -f "$TMP" "$OUT"
mv -f "$HASH_OUT.tmp" "$HASH_OUT"

echo "✔ $OUT"
ls -la "$OUT" | awk '{print "  大小: " $5 " 字节"}'
echo "  顶层条目：$("$PYTHON" -c "
import sys, zipfile
print(' '.join(sorted({n.split('/')[0] for n in zipfile.ZipFile(sys.argv[1]).namelist()})))
" "$OUT")"
echo "  示例：$("$PYTHON" -c "
import sys, zipfile
ns=[n for n in zipfile.ZipFile(sys.argv[1]).namelist() if n.startswith('Examples/') and n.count('/')==2]
print(len(ns), '个 →', ' '.join(sorted(ns)))
" "$OUT")"

# ── 陈旧告警：签入的那份 zip 与仓库 `Lib/` 是否已经对不上 ────────────────
#
# 为什么需要这一条：**这个 zip 是签入仓库的生成物**，而它的消费方只有手机
# （桌面 `vmlcli` 直接读 `third_party/vml/Lib/`）。所以 `Lib/` 改了而 zip 没重建时，
# **桌面一切正常、手机上是旧的** —— 而且设备侧的判据是内容指纹，指纹跟着 zip 一起算，
# zip 没重建则指纹没变 ⇒ **连"要不要重新解压"都不会触发**。全绿、无警告、静默陈旧。
#
# 实测（2026-09-19）：签入的 zip 停在 v0.96.258，一路到 v0.96.295 都没人重建过 ——
# 37 个版本里所有 `Lib/` 改动一个都没到手机上（解开新旧 zip 逐文件比：421 个文件不同）。
#
# 判据刻意做得**保守**：只在「工作区里这份 zip 相对签入版本变了」时提醒，不看时间戳
# （`git pull` / 切分支都会动 mtime，拿它当判据会天天误报）。它不是门禁 ——
# `build-apk.sh` 每次都重建，所以要防的是「改了 `Lib/` 却一直没打 APK」。
if command -v git >/dev/null 2>&1 && git -C "$ROOT" rev-parse --git-dir >/dev/null 2>&1; then
    if ! git -C "$ROOT" diff --quiet -- "$OUT" 2>/dev/null; then
        echo
        echo "⚠ 签入的 vml_lib.zip 与仓库 Lib/ 原本**不一致** —— 刚重建的这份是新的。"
        echo "  意味着：这段时间里手机上跑的是旧标准库（桌面看不出来，因为桌面不读这个包）。"
        echo "  ⇒ 记得把 Resources/Raw/vml_lib.zip 与 vml_lib.hash 一起提交。"
    fi
fi

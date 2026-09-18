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
# ⚠ **示例只收两层，绝不递归整棵树。** 上游 `Examples/` 底下挂着 stb / stm32 那种整套工程
#    （128 个条目里 107 个是它们），而 `EnsureExamples()` 是**平铺**解包的（丢掉目录），
#    递归进来就是上千个文件糊进 `examples/` 一个目录、还有重名互相覆盖。
#    收的是 `Examples/README.md`（第 1 层）与 `Examples/<语言>/<文件>`（第 2 层）——
#    第 2 层正好等于"平铺后还能一一对应"的那一层，也就是示例的约定存放位置。
#    ⇒ **以后加示例/游戏，直接放 `Examples/<语言>/` 下，别建子目录**（子目录不会进包）。
#
# ⚠ `*.gen.vml` 是本地跑出来的中间产物（每个几十 KB），不进包。
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
#      · crt / dos / vga_text / conio —— DOS 与 PC 文本控制台（CRT_GOTOXY、VGA 文本页…）
#      · graphics / graph            —— BGI 绘图（`initgraph`/`putpixel` 那一套，Turbo C 时代）
#      · browser_gfx                 —— 浏览器 canvas 专用
#      · gpio                        —— 单片机引脚
#    ⚠ `device` / `device64` **没排** —— 名字像硬件层，但它可能是 VM 自己的设备抽象
#      （`ui_*` 那条链上要用），排错会让手机上的绘图/音效失灵。要用先查清调用方再排。
MOBILE_EXCLUDE=(crt dos vga_text conio graphics graph browser_gfx gpio)

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
    ( cd "$VML" && zip -q -X "$TMP" \
        $(find Examples -maxdepth 1 -type f ! -name '*.gen.vml') \
        $(find Examples -mindepth 2 -maxdepth 2 -type f ! -name '*.gen.vml') )
else
    # 没有 `zip` 的机器（例如 Windows Git Bash 默认不带）走 Python —— 用**固定时间戳**
    # 保证同样的内容每次产出同样的字节（与 `zip -X` 的意图一致）。
    echo "ℹ 未找到 zip，改用 Python zipfile"
    "$PYTHON" - "$VML" "$TMP" "${MOBILE_EXCLUDE[@]}" <<'PY'
import os, sys, zipfile
root, out = sys.argv[1], sys.argv[2]
exclude = set(sys.argv[3:])   # 移动端不需要的模块名（见脚本头部的 MOBILE_EXCLUDE）

def examples_files():
    """示例只收第 1 层（README）与第 2 层（<语言>/<文件>），见脚本头部注释。"""
    base = os.path.join(root, "Examples")
    for name in sorted(os.listdir(base)):
        full = os.path.join(base, name)
        if os.path.isfile(full):
            yield "Examples/" + name
        elif os.path.isdir(full):
            for sub in sorted(os.listdir(full)):
                if os.path.isfile(os.path.join(full, sub)):
                    yield "Examples/" + name + "/" + sub

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
    for rel in examples_files():
        if rel.endswith(".gen.vml"):
            continue
        add(rel)
PY
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

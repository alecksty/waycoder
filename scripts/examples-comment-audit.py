#!/usr/bin/env python3
"""例程注释双语化 —— 判据脚本。

两条判据，一条硬一条软：

**① 硬判据：代码一字未动。** 把注释按语言剥掉、再去掉所有空白，
对剩下的字符序列取 SHA256，与 `HEAD` 版比 —— **必须完全相同**。
改注释唯一可能出的错就是"顺手碰坏了代码"（插进字符串续行、切断了块注释…），
这条一票否决，而且它**不依赖人眼**。

**② 软判据：中文注释有没有配上英文。** 对每条含中文的注释行，往后看若干行，
若**同一注释块内**能找到一条"也是注释、不含中文、且含 ≥3 个拉丁字母"的行 ⇒ 认为已配对。
（它会被"附近本来就有英文注释"蒙混，所以它是**驱动把活干完的进度条**，不是证明。）

用法：
    python scripts/examples-comment-audit.py                 # 全部进包源码
    python scripts/examples-comment-audit.py --code-only     # 只跑硬判据（改完必跑）
    python scripts/examples-comment-audit.py --list          # 只列还没配英文的文件与条数
    python scripts/examples-comment-audit.py c/ swift/       # 路径子串过滤

退出码：0 = 硬判据全过且没有未配对的（--code-only 时只看硬判据）；1 = 有问题。
"""
import hashlib
import os
import re
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROOT = os.path.join(REPO, "third_party", "vml", "Examples")
CJK = re.compile(r"[一-鿿]")
LATIN = re.compile(r"[A-Za-z]{3,}")

# ── 注释标记（按语言；与 examples-lang-audit.py 同源）────────────────────────
LINE_COMMENT = {
    ".c": ["//"], ".h": ["//"], ".cpp": ["//"], ".hpp": ["//"], ".cs": ["//"],
    ".java": ["//"], ".js": ["//"], ".go": ["//"], ".rs": ["//"], ".swift": ["//"],
    ".m": ["//"], ".mm": ["//"], ".kt": ["//"], ".dart": ["//"], ".d": ["//"],
    ".vml": [";"], ".ld": ["#"], ".py": ["#"], ".rb": ["#"], ".r": ["#"],
    ".lua": ["--"], ".f90": ["!"], ".fth": ["\\"], ".scm": [";"], ".fs": ["\\"],
    ".bas": ["'", "REM"], ".bi": ["'"], ".pas": ["//"], ".sh": ["#"], ".ps1": ["#"],
}
BLOCK_COMMENT = {
    ".c": [("/*", "*/")], ".h": [("/*", "*/")], ".cpp": [("/*", "*/")], ".hpp": [("/*", "*/")],
    ".cs": [("/*", "*/")], ".java": [("/*", "*/")], ".js": [("/*", "*/")], ".go": [("/*", "*/")],
    ".rs": [("/*", "*/")], ".swift": [("/*", "*/")], ".m": [("/*", "*/")], ".mm": [("/*", "*/")],
    ".kt": [("/*", "*/")], ".dart": [("/*", "*/")], ".d": [("/*", "*/")],
    ".pas": [("{", "}"), ("(*", "*)")],
    ".fth": [("(", ")")],
    ".scm": [("#|", "|#")],
}
# 进包规则（照抄 make-vml-lib.sh）
EXCL_PASCAL = re.compile(r"^Examples/pascal/(avc_|g7iles_|gcorail_|gmsdos_|gnc_|ktp_|swag_|tpdem_)")


def is_comment_line(line, ext):
    s = line.strip()
    for m in LINE_COMMENT.get(ext, []):
        if m.isalpha():                      # BASIC 的 REM 只认行首
            if re.match(r"^\s*" + m + r"\b", line):
                return True
        elif s.startswith(m):
            return True
    return False


# 各语言的**字符串引号**（用于"跳过字符串"再找注释起点）。默认只有双引号。
_QUOTES = {
    ".bas": '"', ".bi": '"', ".pas": "'", ".scm": '"', ".fs": '"', ".fth": '"',
    ".vml": '"', ".ld": '"',
}
_QUOTES_DEFAULT = "\"'"
_DOUBLED = {".bas", ".bi", ".pas"}          # 用「把引号写两遍」转义的语言


def true_comment_start(line, ext):
    """**跳过字符串字面量**后，注释标记（行注释或 BASIC 的 REM）的第一个位置；没有则 None。

    ⚠ 这一层是补出来的洞：原先直接 `line.find(mark)`，而 `;`（Scheme）、`#`（Python/R）、
    `--`（Lua）这些标记**也会出现在字符串里** —— 实测 `scheme/demo_tty.scm` 的
    `(print_str "清屏 ESC[2J … 定位 ESC[r;cH …")` 被当成"注释里有中文"，
    于是**字符串里的中文**被错误要求配英文（那恰恰是**不该动**的运行时文字）。
    """
    marks = LINE_COMMENT.get(ext, [])
    quotes = _QUOTES.get(ext, _QUOTES_DEFAULT)
    doubled = ext in _DOUBLED
    i, n = 0, len(line)
    while i < n:
        c = line[i]
        if c in quotes:                      # 进入字符串
            q = c
            i += 1
            while i < n:
                if not doubled and line[i] == "\\":
                    i += 2
                    continue
                if line[i] == q:
                    if doubled and i + 1 < n and line[i + 1] == q:
                        i += 2
                        continue
                    i += 1
                    break
                i += 1
            continue
        for m in marks:
            if m.isalpha():                  # BASIC 的 REM：只认行首（前面只有空白）
                if not line[:i].strip() and re.match(m + r"\b", line[i:], re.I):
                    return i
            elif line.startswith(m, i):
                return i
        i += 1
    return None


def cjk_in_comment(line, ext):
    """这一行里的中文**是否落在注释里**。

    ⚠ 必须区分"注释"与"字符串字面量"：`ui_text(8, 8, "得分", …)` 里的中文是**运行时打出去的字**，
    不是注释，**不该要求配英文**（改了它等于改程序输出）。所以判据是**位置**：
    中文出现在注释起始符**之后**才算注释。行尾注释（`call putchar(109)  ! m ⇒ …`）由此也能被看见。
    """
    if not CJK.search(line):
        return False
    cut = true_comment_start(line, ext)
    if cut is None:
        return False
    return bool(CJK.search(line[cut:]))


def strip_comments(text, ext):
    """注释换成等长空白（保结构），供硬判据用。"""
    out = list(text)
    for a, b in BLOCK_COMMENT.get(ext, []):
        i = 0
        while True:
            s = "".join(out).find(a, i)
            if s < 0:
                break
            e = "".join(out).find(b, s + len(a))
            e = len(out) if e < 0 else e + len(b)
            for k in range(s, e):
                if out[k] != "\n":
                    out[k] = " "
            i = e
    marks = LINE_COMMENT.get(ext, [])
    lines = "".join(out).split("\n")
    for n, line in enumerate(lines):
        cut = None
        for m in marks:
            if m.isalpha():
                if re.match(r"\s*" + m + r"\b", line):
                    cut = 0 if cut is None else min(cut, 0)
            else:
                p = line.find(m)
                if p >= 0 and (cut is None or p < cut):
                    cut = p
        if cut is not None:
            lines[n] = line[:cut] + " " * (len(line) - cut)
    return "\n".join(lines)


def code_hash(text, ext):
    code = strip_comments(text, ext)
    return hashlib.sha256("".join(c for c in code if not c.isspace()).encode()).hexdigest()[:16]


def block_flags(text, ext):
    """每行是否落在**块注释**里（含起止行）。

    ⚠ 补出来的洞：`is_comment_line` 只认行注释，于是 **Pascal 的 `{ … }`、Scheme 的 `#| … |#`、
    Forth 的 `( … )`** 里的行既不被当作注释行、也就永远"配不上对" ——
    实测 `_selftest/out.pas` 那三行中文（跨行 `{ }` 注释）就是这么被漏掉的。
    """
    lines = text.split("\n")
    flags = [False] * len(lines)
    for a, b in BLOCK_COMMENT.get(ext, []):
        i = 0
        while True:
            s = text.find(a, i)
            if s < 0:
                break
            e = text.find(b, s + len(a))
            e = len(text) if e < 0 else e + len(b)
            for L in range(text.count("\n", 0, s), text.count("\n", 0, e) + 1):
                if 0 <= L < len(flags):
                    flags[L] = True
            i = e
    return flags


def packaged():
    out = []
    for root, dirs, files in os.walk(ROOT):
        rel = os.path.relpath(root, os.path.dirname(ROOT)).replace("\\", "/")
        if rel.count("/") not in (1, 2, 3):
            continue
        for f in files:
            relf = f"{rel}/{f}"
            if f.endswith(".gen.vml") or f == ".DS_Store" or EXCL_PASCAL.match(relf):
                continue
            ext = os.path.splitext(f)[1].lower()
            if ext in LINE_COMMENT or ext in BLOCK_COMMENT:
                out.append(os.path.join(root, f))
    return sorted(set(out))


def head_text(relpath):
    r = subprocess.run(["git", "show", f"HEAD:{relpath}"], cwd=REPO,
                       capture_output=True)
    return None if r.returncode else r.stdout.decode("utf-8", "surrogateescape")


def unpaired(lines, ext, lookahead=1, in_block=None):
    """返回"含中文注释、但**紧邻的下一行**不是它的英文注释"的行号。

    两条演进（都记在案，别再退回去）：
    · 原来 `lookahead=6`：往后 5 行内**任何**一条"不含中文、含 ≥3 拉丁字母"的注释行都算配对
      —— 于是**静默漏掉**文件头那种散文注释（旁边一行 `// <input>:6: error: …` 就把中文行
      "配对"掉了）。实测漏了 57 条。现在收紧成**紧邻下一行**，代价是**多行中文注释块要逐行
      交错翻译**（中文行 / 英文行 / 中文行 / 英文行…）—— 刻意的：只有这个形状能被机械校验。
    · 原来只认 `is_comment_line`（整行注释）⇒ **行尾注释整个看不见**（实测漏了 59 条）。
      现在改用 `cjk_in_comment`（按**位置**判：中文在注释起始符之后才算）——
      字符串字面量里的中文因此仍然**不算**（那是运行时打出去的字，改了等于改输出）。

    ⚠⚠ **别再往这里加"密度/比例"判据**（试过、撤了）：曾经要求"中文占注释正文一半以上才算"
    来避免"英文里引用中文被误报"，结果**把真注释一起漏掉** —— `// style 1 = ┌ ─ │ ┐ └ ┘ 单线框`
    （制表符不算 CJK ⇒ 密度不过）、`// 整除写 ~/` 这类短注释全被吞了。
    **宁可假阳性**（作者把英文行里的中文引用改成"描述"即可，成本很小），**不可假阴性**（漏掉的
    中文注释会永远躺在那里，而判据报 0）。
    """
    bad = []
    for i, line in enumerate(lines):
        in_blk_i = bool(in_block[i]) if in_block else False
        # ⚠ **主体只应是"真的含中文的注释行"**，两种算：
        #   ① 行注释里含中文（`cjk_in_comment`）；② **块注释里的行且含中文**。
        #   曾经写成 `in_blk_i or cjk_in_comment(...)` —— 那等于把**块注释里的每一行**
        #   （含英文行、含 `*/` 行）都当主体，于是"逐行交错"（中文/英文/中文/英文）**永远过不了**：
        #   每个英文行的下一行是中文行。反过来，只写 `cjk_in_comment(line)` 也会漏 ——
        #   **Pascal 块注释里的行没有 `//` 标记**，`true_comment_start` 找不到注释起点。
        #   `in_block` 因此既要能当**主体**（配合"该行含中文"），也要能当**配对侧**（下面 `in_blk_j`）。
        if not (cjk_in_comment(line, ext) or (in_blk_i and CJK.search(line))):
            continue
        ok = False
        for j in range(i + 1, min(len(lines), i + 1 + lookahead)):
            lj = lines[j]
            in_blk_j = bool(in_block[j]) if in_block else False
            if not (in_blk_j or is_comment_line(lj, ext)):
                break
            if not CJK.search(lj) and LATIN.search(lj):
                ok = True
                break
        if not ok:
            bad.append(i + 1)
    return bad


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    code_only = "--code-only" in sys.argv
    listing = "--list" in sys.argv
    files = [f for f in packaged() if not args or any(a in f.replace("\\", "/") for a in args)]
    code_bad, unpaired_rows, skipped = [], [], []
    for f in files:
        rel = os.path.relpath(f, REPO).replace("\\", "/")
        ext = os.path.splitext(f)[1].lower()
        # ⚠ 只收 **UTF-8 合法**的文件。非 UTF-8（`basic/thirdparty/` 那批 DOS 老 BASIC 是 GBK）
        #   有两个坑：① 解码有损 ⇒ 哈希会漂（实测 5 个文件假红）；② 往里插 UTF-8 注释会变成
        #   **混合编码**，而 App 的编辑器按 UTF-8 读 —— 它们的字符串字面量还是 GBK 字节，
        #   改编码会改掉程序**输出的字节**。所以本批**明确不碰**，单列出来。
        raw = open(f, "rb").read()
        try:
            raw.decode("utf-8")
        except UnicodeDecodeError:
            skipped.append(rel)
            continue
        cur = raw.decode("utf-8")
        old = head_text(rel)
        if old is not None and code_hash(old, ext) != code_hash(cur, ext):
            code_bad.append(rel)
        if not code_only:
            bad = unpaired(cur.split("\n"), ext, in_block=block_flags(cur, ext))
            if bad:
                unpaired_rows.append((len(bad), rel, bad[:4]))
    print(f"检查了 {len(files) - len(skipped)} 个进包源码文件（UTF-8）")
    if skipped:
        print(f"⚠ 跳过 {len(skipped)} 个非 UTF-8 文件（GBK 老例程，本批不碰）：")
        for s in skipped:
            print("   ", s)
    print()
    if code_bad:
        print(f"❌ 【硬判据】代码被改动了 {len(code_bad)} 个文件（注释之外的东西变了）：")
        for r in code_bad[:20]:
            print("   ", r)
    else:
        print("✔ 【硬判据】代码一字未动（注释剥掉、去空白后逐文件哈希相同）")
    if not code_only:
        tot = sum(n for n, _, _ in unpaired_rows)
        print(f"{'✔' if not unpaired_rows else '❌'} 【软判据】还没配英文的中文注释：{tot} 条 / {len(unpaired_rows)} 个文件")
        if listing:
            for n, r, sample in sorted(unpaired_rows, reverse=True):
                print(f"   {n:>4}  {r}   (如 {sample})")
    return 1 if (code_bad or (not code_only and unpaired_rows)) else 0


if __name__ == "__main__":
    sys.exit(main())

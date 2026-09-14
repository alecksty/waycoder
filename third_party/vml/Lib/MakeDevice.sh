#!/bin/bash
# ============================================================
# Lib/MakeDevice.sh — VML MCU 设备头文件生成器
# 用法: ./MakeDevice.sh [-v] [-j N] [-c]
# ============================================================
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
LIB_DIR="$SCRIPT_DIR"
GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
info()  { echo -e "${YELLOW}[..]${NC} $1" >&2; }
pass()  { echo -e "${GREEN}[OK]${NC} $1" >&2; }

VERBOSE=false; CLEAN_MODE=false
VML_JOBS=${VML_JOBS:-$(sysctl -n hw.ncpu 2>/dev/null || echo 8)}
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help) echo "用法: $0 [-v] [-j N] [-c]"; exit 0 ;;
        -c|--clean) CLEAN_MODE=true; shift ;;
        -v|--verbose) VERBOSE=true; shift ;;
        -j) VML_JOBS="$2"; shift 2 ;;
        *) echo "未知: $1"; exit 1 ;;
    esac
done

# ==================== Clean mode ====================
if $CLEAN_MODE; then
    echo "清理设备头文件..."
    for d in "$LIB_DIR"/*/Device; do [ -d "$d" ] && rm -rf "$d"; done
    pass "清理完成"
    exit 0
fi

# ==================== Verify GenDev project ====================
GENDEV_PROJ="$PROJECT_ROOT/tools/GenDev"
[ -d "$GENDEV_PROJ" ] || { echo -e "${RED}找不到 GenDev 项目${NC}"; exit 1; }
pass "GenDev: dotnet run --project tools/GenDev"

# ==================== Write Python driver to temp file ====================
TMP_PY=$(mktemp /tmp/make_device_XXXXXX.py)
trap "rm -f $TMP_PY" EXIT

cat > "$TMP_PY" << 'PYEOF'
import os, sys, subprocess, shutil, glob
from concurrent.futures import ThreadPoolExecutor, as_completed

project_root = sys.argv[1]
lib_dir      = sys.argv[2]
max_workers  = int(sys.argv[3])
verbose      = sys.argv[4] == "true"

# Use dotnet run instead of published binary to avoid XmlSerializer trimming issues
GENDEV_CMD = ["dotnet", "run", "--project",
              os.path.join(project_root, "tools", "GenDev"),
              "--"]

LANGS = {
    "c":          ("c",          ".h"),
    "cpp":        ("cpp",        ".hpp"),
    "objc":       ("objc",       ".h"),
    "basic":      ("Basic",      ".bas"),
    "pascal":     ("Pascal",     ".pas"),
    "vml":        ("vml",        ".vml"),
    "ladder":     ("ladder",     ".ld"),
    "python":     ("python",     ".py"),
    "forth":      ("forth",      ".fth"),
    "lua":        ("lua",        ".lua"),
    "go":         ("go",         ".go"),
    "rust":       ("rust",       ".rs"),
    "java":       ("java",       ".java"),
    "javascript": ("javascript", ".js"),
    "swift":      ("swift",      ".swift"),
    "csharp":     ("csharp",     ".cs"),
    "kotlin":     ("Kotlin",     ".kt"),
    "scheme":     ("Scheme",     ".scm"),
    "ruby":       ("ruby",       ".rb"),
    "dart":       ("dart",       ".dart"),
    "r":          ("r",          ".r"),
    "d":          ("d",          ".d"),
    "fortran":    ("fortran",    ".f90"),
}

for key, (dir_name, ext) in LANGS.items():
    os.makedirs(os.path.join(lib_dir, dir_name, "Device"), exist_ok=True)

c_dev = os.path.join(lib_dir, "c", "Device")
devices_dir = os.path.join(project_root, "Devices")
xml_files = sorted(glob.glob(os.path.join(devices_dir, "**", "*.xml"), recursive=True))
total = len(xml_files)
print(f"  设备: {total} | 语言: {len(LANGS)} | 并行: {max_workers}")

def process_device(xml_path):
    name = os.path.splitext(os.path.basename(xml_path))[0]
    out_base = os.path.join(c_dev, f"{name}.h")

    cmd = GENDEV_CMD + ["-i", xml_path, "-o", out_base, "-a"]
    result = subprocess.run(cmd, capture_output=True, text=True, cwd=project_root)

    if result.returncode != 0:
        print(f"  !! {name}: {result.stderr.strip()[:200]}")
        return ("FAIL", name)

    for lang_key, (dir_name, ext) in LANGS.items():
        if lang_key == "c":
            continue
        src = os.path.join(c_dev, f"{name}{ext}")
        dst = os.path.join(lib_dir, dir_name, "Device", f"{name}{ext}")
        if os.path.isfile(src):
            if lang_key == "objc":
                shutil.copy2(src, dst)
            else:
                shutil.move(src, dst)

    if verbose:
        print(f"  OK: {name}")
    return ("OK", name)

ok_count = 0
fail_count = 0

with ThreadPoolExecutor(max_workers=max_workers) as executor:
    futures = {executor.submit(process_device, x): x for x in xml_files}
    for future in as_completed(futures):
        status, name = future.result()
        if status == "OK":
            ok_count += 1
        else:
            fail_count += 1

print(f"\n  成功: {ok_count}  失败: {fail_count}")
print(f"  总计: {ok_count * len(LANGS)} 个头文件 ({ok_count} x {len(LANGS)} 语言)\n")

print(f"  {'语言':<14} {'文件数':>5}")
print(f"  {'------------':<14} {'-----':>5}")
c_total = 0
for lang_key, (dir_name, ext) in LANGS.items():
    d = os.path.join(lib_dir, dir_name, "Device")
    cnt = len(glob.glob(os.path.join(d, f"*{ext}")))
    c_total += cnt
    print(f"  {lang_key:<14} {cnt:>5}")
print(f"  {'总计':<14} {c_total:>5}")
PYEOF

python3 "$TMP_PY" "$PROJECT_ROOT" "$LIB_DIR" "$VML_JOBS" "$VERBOSE"

#!/usr/bin/env bash
#
# rebuild_shared.sh — 重建 Lib/shared/ 下所有共享库的 .vml 文件
# 使用 C 编译器的 --no-link 模式，确保每个 .vml 只包含自身代码（无自动链接）
#
# 用法:
#   ./Lib/shared/rebuild_shared.sh
#   ./Lib/shared/rebuild_shared.sh --modules sysinfo,io,string
#   ./Lib/shared/rebuild_shared.sh --skip-backup
#
# 跨平台: Linux / macOS / Windows (Git Bash / WSL)

set -euo pipefail

# ── 参数解析 ──
MODULES=""
SKIP_BACKUP=false
PROJECT_ROOT=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --modules|-m)
            MODULES="$2"
            shift 2
            ;;
        --skip-backup|-s)
            SKIP_BACKUP=true
            shift
            ;;
        --project-root|-p)
            PROJECT_ROOT="$2"
            shift 2
            ;;
        --help|-h)
            echo "用法: $0 [选项]"
            echo "选项:"
            echo "  -m, --modules <list>     只重建指定模块（逗号分隔，不含 .c）"
            echo "  -s, --skip-backup        跳过备份"
            echo "  -p, --project-root <dir> 指定项目根目录（默认自动检测）"
            echo "  -h, --help               显示此帮助"
            exit 0
            ;;
        *)
            echo "未知参数: $1"
            exit 1
            ;;
    esac
done

# ── 检测项目根目录 ──
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ -z "$PROJECT_ROOT" ]]; then
    # 脚本在 Lib/shared/ 下，往上两级就是项目根
    PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
fi

SRC_DIR="$PROJECT_ROOT/Lib/shared/src"
OUT_DIR="$PROJECT_ROOT/Lib/shared"
BACKUP_DIR="$OUT_DIR/backup"
C_COMPILER="$PROJECT_ROOT/VMLPrepares/CCompiler"

# 自动发现 src/ 下所有 .c 文件
if [[ -n "$MODULES" ]]; then
    IFS=',' read -ra MODULE_LIST <<< "$MODULES"
else
    MODULE_LIST=()
    for f in "$SRC_DIR"/*.c; do
        [[ -f "$f" ]] && MODULE_LIST+=("$(basename "$f" .c)")
    done
fi

echo "=== VML 共享库重建脚本 ==="
echo "源文件目录: $SRC_DIR  (发现 ${#MODULE_LIST[@]} 个模块)"
echo "源文件目录: $SRC_DIR"
echo "输出目录:   $OUT_DIR"
echo "模块数:     ${#MODULE_LIST[@]}"
echo ""

# ── 验证 C 编译器可用 ──
if [[ ! -f "$C_COMPILER/CCompiler.csproj" ]]; then
    echo "错误: 未找到 C 编译器项目: $C_COMPILER" >&2
    exit 1
fi

# ── 备份 ──
if ! $SKIP_BACKUP; then
    mkdir -p "$BACKUP_DIR"
    backup_count=0
    for mod in "${MODULE_LIST[@]}"; do
        src="$OUT_DIR/$mod.vml"
        dst="$BACKUP_DIR/$mod.vml"
        if [[ -f "$src" ]]; then
            cp -f "$src" "$dst"
            backup_count=$((backup_count + 1))
        fi
    done
    echo "已备份 $backup_count 个 .vml 文件到 $BACKUP_DIR"
fi

# ── 编译每个模块 ──
ok_count=0
fail_count=0
for mod in "${MODULE_LIST[@]}"; do
    src_file="$SRC_DIR/$mod.c"
    out_file="$OUT_DIR/$mod.vml"

    if [[ ! -f "$src_file" ]]; then
        echo "警告: 源文件不存在，跳过: $src_file" >&2
        fail_count=$((fail_count + 1))
        continue
    fi

    echo "[$mod] 编译中..."
    set +e
    output=$(dotnet build "$C_COMPILER" -q && dotnet run --project "$C_COMPILER" --no-build -- --no-link "$src_file" -o "$out_file" 2>&1)
    exit_code=$?
    set -e

    if [[ $exit_code -eq 0 && -f "$out_file" ]]; then
        instr_count=$(grep -cE '^\s+(PUSH|MOVE|STORE|LOAD|CMP|JMP|JE|JNE|JL|JLE|JG|JGE|JZ|JNZ|ADD|SUB|MUL|DIV|MOD|AND|OR|XOR|NOT|SHL|SHR|ASM|SYSCALL|CALL|RET|NOP|ENTER|LEA|TEST)\b' "$out_file" || true)
        echo "  -> $out_file ($instr_count 指令)"
        ok_count=$((ok_count + 1))
    else
        echo "  -> 编译失败!" >&2
        echo "$output" >&2
        fail_count=$((fail_count + 1))
    fi
done

echo ""
echo "=== 编译完成 ==="
echo "成功: $ok_count  失败: $fail_count"

if [[ $fail_count -gt 0 ]]; then
    exit 1
fi

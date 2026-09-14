#!/bin/bash
# ============================================================
# publish_tools.sh — 编译发布核心工具到 tools/bin/<rid>/
#
# 工具列表:
#   VMLTool            主 CLI (C 编译器, 汇编器, 翻译器等)
#   GenLib            共享库管理
#   GenDyn      动态库 FFI 绑定生成
#   GenDev  设备代码生成
#
# 用法:
#   ./publish_tools.sh                    # 当前平台
#   ./publish_tools.sh -r osx-x64         # 指定 RID
#   ./publish_tools.sh -r win-x64         # 交叉编译 Windows
#   ./publish_tools.sh -r linux-x64       # 交叉编译 Linux
#   ./publish_tools.sh --list             # 列出常用 RID
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_BASE="$PROJECT_ROOT/tools/bin"

# 颜色
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
info() { echo -e "${YELLOW}[..]${NC} $1" >&2; }
pass() { echo -e "${GREEN}[OK]${NC} $1" >&2; }
fail() { echo -e "${RED}[!!]${NC} $1" >&2; }

# ============================================================
# 平台检测
# ============================================================
detect_rid() {
    local os arch
    case "$(uname -s)" in
        Darwin) os="osx" ;;
        Linux)  os="linux" ;;
        MINGW*|MSYS*|CYGWIN*) os="win" ;;
        *) echo "unknown"; return 1 ;;
    esac
    case "$(uname -m)" in
        x86_64|amd64) arch="x64" ;;
        arm64|aarch64) arch="arm64" ;;
        *) arch="x64" ;;
    esac
    echo "${os}-${arch}"
}

# ============================================================
# 工具定义: 项目路径, 程序集名, 显示名
# ============================================================
TOOLS=(
    "VMLTool/VMLTool.csproj|VMLTool|vmltool"
    "tools/GenLib/GenLib.csproj|GenLib|genlib"
    "tools/GenDyn/GenDyn.csproj|GenDyn|gendyn"
    "tools/GenDev/GenDev.csproj|GenDev|gendev"
)

# ============================================================
# 主流程
# ============================================================
main() {
    local rid=""
    local list_only=false

    while [ $# -gt 0 ]; do
        case "$1" in
            -r|--runtime) rid="$2"; shift 2 ;;
            --list) list_only=true; shift ;;
            -h|--help)
                echo "publish_tools — 编译发布核心工具到 tools/bin/<rid>/"
                echo ""
                echo "用法: publish_tools.sh [-r RID] [--list]"
                echo ""
                echo "选项:"
                echo "  -r, --runtime RID   目标运行时标识符 (默认: 当前平台)"
                echo "  --list              列出常用 RID"
                echo ""
                echo "工具: VMLTool, GenLib, GenDyn, GenDev"
                echo ""
                echo "常用 RID:"
                echo "  osx-x64    osx-arm64"
                echo "  linux-x64  linux-arm64"
                echo "  win-x64    win-x86    win-arm64"
                exit 0
                ;;
            *) fail "未知参数: $1"; exit 1 ;;
        esac
    done

    if $list_only; then
        echo "常用 RID:"
        echo "  osx-x64       macOS Intel"
        echo "  osx-arm64     macOS Apple Silicon"
        echo "  linux-x64     Linux x86_64"
        echo "  linux-arm64   Linux ARM64"
        echo "  win-x64       Windows x64"
        echo "  win-x86       Windows x86"
        echo "  win-arm64     Windows ARM64"
        exit 0
    fi

    [ -z "$rid" ] && rid="$(detect_rid)"

    # 验证 RID 格式，防止路径遍历
    if ! [[ "$rid" =~ ^[a-zA-Z][a-zA-Z0-9.-]+-[a-zA-Z][a-zA-Z0-9.-]+$ ]]; then
        fail "无效的 RID 格式: $rid (预期格式: os-arch, 如 osx-x64)"
        exit 1
    fi

    info "目标平台: $rid"
    local outdir="$OUTPUT_BASE/$rid"
    mkdir -p "$outdir"

    echo "============================================"
    echo "  编译发布工具 → tools/bin/${rid}/"
    echo "============================================"
    echo ""

    local total=${#TOOLS[@]}
    local passed=0
    local failed=0

    for tool_def in "${TOOLS[@]}"; do
        IFS='|' read -r proj_rel asm_name display <<< "$tool_def"
        local proj="$PROJECT_ROOT/$proj_rel"

        info "发布 $display ($proj_rel)"
        if dotnet publish "$proj" -c Release -r "$rid" -o "$outdir" --self-contained 2>&1 |
            while IFS= read -r line; do
                case "$line" in
                    *error*) echo "  $line" >&2 ;;
                esac
            done
            [ ${PIPESTATUS[0]} -eq 0 ]; then
            pass "$display"
            passed=$((passed + 1))
        else
            fail "$display"
            failed=$((failed + 1))
        fi
    done

    echo ""
    echo "============================================"
    echo "  完成: $passed 成功, $failed 失败"
    echo "============================================"
    echo ""

    # 创建符号链接: tools/bin/<shortname> → tools/bin/<rid>/<asm_name>
    if [ $passed -gt 0 ]; then
        info "创建符号链接 tools/bin/ → tools/bin/${rid}/..."
        for tool_def in "${TOOLS[@]}"; do
            IFS='|' read -r proj_rel asm_name display <<< "$tool_def"
            local target="$rid/$asm_name"
            if [ -f "$outdir/$asm_name" ]; then
                # 短名 (vmltool, genlib, ...)
                ln -sf "$target" "$OUTPUT_BASE/$display" 2>/dev/null || true
                # 程序集名 (VMLTool, GenLib, ...)
                ln -sf "$target" "$OUTPUT_BASE/$asm_name" 2>/dev/null || true
            fi
        done
        pass "符号链接已创建"
        echo ""
        echo "各 build 脚本现在可直接从 tools/bin/ 查找工具"
    fi

    [ $failed -eq 0 ] || exit 1
}

main "$@"

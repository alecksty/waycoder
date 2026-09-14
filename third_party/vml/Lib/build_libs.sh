#!/bin/bash
# ============================================================
# Lib/build_libs.sh — 重新编译生成所有必需的 VML 库文件
#
# 编译流程:
#   1. 编译 shared/src/*.c    → shared/<name>.vml    (共享库模块)
#   2. 编译 shared/*.c        → shared/<name>.vml    (根级共享源)
#   3. 编译 c/*.c             → c/<name>.vml         (C 标准库)
#   4. 编译 c/src/*.c         → c/<name>.vml         (C 子模块)
#   5. 生成各语言 builtin.vml / stdlib.vml / vmllib.vml
#
# 三层库架构:
#   builtin.vml  — 编译器自动链接，必备运行时 (PEEK/POKE/基本IO)
#   stdlib.vml   — 常用标准库 (string/math/io/file/printf 等)
#   vmllib.vml   — 完整共享库 (所有 shared/ 模块)
#
# 用法:
#   ./build_libs.sh            # 全部编译
#   ./build_libs.sh --shared   # 仅编译共享库模块
#   ./build_libs.sh --lang     # 仅生成各语言聚合文件
#   ./build_libs.sh --c        # 仅编译 C 标准库
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# VML_HOME 环境变量优先，否则自动检测项目根目录
if [ -n "${VML_HOME:-}" ]; then
    PROJECT_ROOT="$(cd "$VML_HOME" && pwd)"
else
    PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
fi

# 跨平台 sed -i 兼容
if [[ "$OSTYPE" == "darwin"* ]]; then
    SED_I="sed -i ''"
else
    SED_I="sed -i"
fi

# 颜色输出
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

PASSED=0
FAILED=0

pass() { echo -e "${GREEN}[OK]${NC} $1" >&2; PASSED=$((PASSED + 1)); }
fail() { echo -e "${RED}[FAIL]${NC} $1" >&2; FAILED=$((FAILED + 1)); }
info() { echo -e "${YELLOW}[..]${NC} $1" >&2; }

# ============================================================
# 工具查找: tools/bin/ → tools/ → 标准 dotnet 构建输出 → 源码发布
# 返回格式: "path|runner"  其中 runner 为 "dotnet " 或 ""
# ============================================================
resolve_tool() {
    local name="$1" proj="$2"
    local asm_name=""
    asm_name="$(basename "$proj")"

    # 辅助: 检查 .dll 或 .exe 是否存在
    find_dll_or_exe() {
        local dir="$1" base="$2"
        # .exe (Windows 自包含)
        for ext in exe dll; do
            if [ -f "$dir/$base.$ext" ]; then
                echo "$dir/$base.$ext"
                return 0
            fi
        done
        return 1
    }

    # 1) tools/bin/<name> (上次发布)
    local found=""
    found="$(find_dll_or_exe "$PROJECT_ROOT/tools/bin" "$name" || true)"
    if [ -n "$found" ]; then
        if [[ "$found" == *.dll ]]; then
            echo "dotnet $found"
        else
            echo "$found"
        fi
        return
    fi
    found="$(find_dll_or_exe "$PROJECT_ROOT/tools/bin" "$asm_name" || true)"
    if [ -n "$found" ]; then
        if [[ "$found" == *.dll ]]; then
            echo "dotnet $found"
        else
            echo "$found"
        fi
        return
    fi

    # 2) tools/<name> (自包含发布包)
    found="$(find_dll_or_exe "$PROJECT_ROOT/tools" "$name" || true)"
    if [ -n "$found" ]; then
        if [[ "$found" == *.dll ]]; then
            echo "dotnet $found"
        else
            echo "$found"
        fi
        return
    fi
    found="$(find_dll_or_exe "$PROJECT_ROOT/tools" "$asm_name" || true)"
    if [ -n "$found" ]; then
        if [[ "$found" == *.dll ]]; then
            echo "dotnet $found"
        else
            echo "$found"
        fi
        return
    fi

    # 3) 标准 dotnet build 输出 (bin/Debug/net*/ 或 bin/Release/net*/)
    if [ -d "$PROJECT_ROOT/$proj" ]; then
        for cfg in Debug Release; do
            for tfm_dir in "$PROJECT_ROOT/$proj/bin/$cfg"/net*; do
                if [ -d "$tfm_dir" ]; then
                    found="$(find_dll_or_exe "$tfm_dir" "$asm_name" || true)"
                    if [ -n "$found" ]; then
                        if [[ "$found" == *.dll ]]; then
                            echo "dotnet $found"
                        else
                            echo "$found"
                        fi
                        return
                    fi
                fi
            done
        done
    fi

    # 4) 有源码 → dotnet publish 到 tools/bin/，下次直接使用
    if [ -d "$PROJECT_ROOT/$proj" ]; then
        local tools_bin="$PROJECT_ROOT/tools/bin"
        mkdir -p "$tools_bin"
        info "首次使用，正在编译发布 ${proj} → tools/bin/ ..."
        dotnet publish "$PROJECT_ROOT/$proj" -c Release -o "$tools_bin" >&2 || {
            fail "编译发布失败"
            echo ""
            return
        }
        pass "发布完成"
        found="$(find_dll_or_exe "$tools_bin" "$name" || find_dll_or_exe "$tools_bin" "$asm_name" || true)"
        if [ -z "$found" ]; then
            fail "找不到编译产物: $tools_bin/$name.{dll,exe} 或 $tools_bin/$asm_name.{dll,exe}"
        fi
        if [ -n "$found" ]; then
            if [[ "$found" == *.dll ]]; then
                echo "dotnet $found"
            else
                echo "$found"
            fi
        fi
    else
        echo ""
    fi
}

VMLTOOL="$(resolve_tool "vmltool" "VMLTool")"
[ -z "$VMLTOOL" ] && { echo "错误: 找不到 vmltool (发布目录或源码项目)"; exit 1; }

compile_c() {
    # 编译单个 C 文件为 VML
    # 用法: compile_c <c_file> <output_vml> [extra_flags...]
    # 所有路径均使用绝对路径，确保子目录 cd 后仍正确
    local cfile="$1"
    local vmlfile="$2"
    shift 2
    local extra="${@:-}"

    if [ ! -f "$cfile" ]; then
        fail "源文件不存在: $cfile"
        return 1
    fi

    local cdir
    cdir="$(dirname "$cfile")"
    local cname
    cname="$(basename "$cfile")"

    # 在源文件目录下编译，使相对路径 #include 正常工作
    # vmlfile 使用绝对路径，cd 后仍正确
    (
        cd "$cdir" || exit 1
        local inc_flags="-I $SCRIPT_DIR/shared -I $SCRIPT_DIR/c -I $SCRIPT_DIR"
        set +e
        local log
        log="$($VMLTOOL -c "$cname" -o "$vmlfile" $inc_flags $extra --mode mcu --no-link 2>&1)"
        local rc=$?
        if [ $rc -ne 0 ]; then
            echo "$log" >&2
        fi
        exit $rc
    )
}

# ============================================================
# Phase 1: 编译共享库模块 (shared/src/*.c)
# ============================================================
build_shared_modules() {
    echo ""
    echo "============================================"
    echo "  Phase 1: 编译共享库模块 (shared/src/)"
    echo "============================================"

    local src_dir="$SCRIPT_DIR/shared/src"
    local out_dir="$SCRIPT_DIR/shared"

    for cfile in "$src_dir"/*.c; do
        [ -f "$cfile" ] || continue
        local name
        name="$(basename "$cfile" .c)"
        local vmlfile="$out_dir/${name}.vml"

        info "编译 shared/src/${name}.c → shared/${name}.vml"
        if compile_c "$cfile" "$vmlfile"; then
            pass "shared/${name}.vml"
        else
            fail "shared/${name}.vml"
        fi
    done
}

# ============================================================
# Phase 2: 编译 shared/ 根级 C 源文件
# ============================================================
build_shared_root() {
    echo ""
    echo "============================================"
    echo "  Phase 2: 编译 shared/ 根级源文件"
    echo "============================================"

    local dir="$SCRIPT_DIR/shared"

    for cfile in "$dir"/*.c; do
        [ -f "$cfile" ] || continue
        local name
        name="$(basename "$cfile" .c)"
        local vmlfile="$dir/${name}.vml"

        info "编译 shared/${name}.c → shared/${name}.vml"
        if compile_c "$cfile" "$vmlfile"; then
            pass "shared/${name}.vml"
        else
            fail "shared/${name}.vml"
        fi
    done
}

# ============================================================
# Phase 3: 编译 C 标准库 (c/*.c)
# ============================================================
build_c_libs() {
    echo ""
    echo "============================================"
    echo "  Phase 3: 编译 C 标准库 (c/)"
    echo "============================================"

    local dir="$SCRIPT_DIR/c"

    for cfile in "$dir"/*.c; do
        [ -f "$cfile" ] || continue
        local name
        name="$(basename "$cfile" .c)"
        local vmlfile="$dir/${name}.vml"

        info "编译 c/${name}.c → c/${name}.vml"
        if compile_c "$cfile" "$vmlfile"; then
            pass "c/${name}.vml"
        else
            fail "c/${name}.vml (可接受 — 部分 C 源可能作为模块使用)"
        fi
    done

    # C 子目录 (如 c/src/printf.c)
    if [ -d "$dir/src" ]; then
        for cfile in "$dir/src"/*.c; do
            [ -f "$cfile" ] || continue
            local name
            name="$(basename "$cfile" .c)"
            local vmlfile="$dir/${name}.vml"

            info "编译 c/src/${name}.c → c/${name}.vml"
            if compile_c "$cfile" "$vmlfile"; then
                pass "c/${name}.vml"
            else
                fail "c/${name}.vml"
            fi
        done
    fi
}

# ============================================================
# Phase 4: 生成各语言 builtin / stdlib / vmllib
# ============================================================

# 各语言的 builtin.vml 模板
# 编译器自动链接此文件，包含必备运行时
# 不同语言层级不同：C 精简，BASIC 全量，其他标准
write_builtin() {
    local lang_dir="$1"
    local lang_name="$2"
    local lang_lower
    lang_lower=$(echo "$lang_name" | tr '[:upper:]' '[:lower:]')
    local lang_upper
    lang_upper=$(echo "$lang_name" | tr '[:lower:]' '[:upper:]')
    local level="standard"
    case "$lang_lower" in
        c|cpp) level="minimal" ;;
        basic) level="full" ;;
    esac

    cat > "$lang_dir/builtin.vml" << 'BUILTIN_HEADER'
; ============================================================
; LANG_NAME Built-in Library (auto-linked by compiler)
; 必备运行时 — 编译器自动链接此文件
;
; 编译器自动定义宏: VML_LANG_UPPER
; 可按语言/模式条件引入库:
;   #ifdef VML_CPP
;   .linked  "../shared/string.vml"
;   #endif
; ============================================================
BUILTIN_HEADER

    # 核心层（所有语言）
    cat >> "$lang_dir/builtin.vml" << 'CORE'
; ---- 核心内置函数 (PEEK/POKE/putchar/abs/min/max/random/sleep/alloc) ----
.linked  "../shared/builtins.vml"

; ---- VML 系统调用包装器 (vml_print_*/vml_alloc/vml_mem_*/vml_random/...) ----
.linked  "../shared/vmlsys.vml"

; ---- 系统调用常量 (SYS_* / CFG_*) ----
.linked  "../shared/syscall.inc.vml"

; ---- 设备操作基础 ----
.linked  "../shared/device.vml"

; ---- 系统信息 (随机种子/日期时间/退出) ----
.linked  "../shared/sysinfo.vml"

; ---- 软浮点 / 软 int64 (编译器按 -float32/float64/int64 模式自动定义宏) ----
#ifdef VML_FLOAT32_SOFT
.linked  "../shared/softfloat.vml"
#endif
#ifdef VML_FLOAT64_SOFT
.linked  "../shared/softdouble.vml"
#endif
#ifdef VML_INT64_SOFT
.linked  "../shared/softint64.vml"
#endif
CORE

    if [ "$level" = "minimal" ]; then
        cat >> "$lang_dir/builtin.vml" << 'CDECL'
; ---- C 变参函数 (cdecl: printf/scanf 系列) ----
.linked  "../shared/io.vml"
.linked  "../shared/printf.vml"
.linked  "../shared/scanf.vml"
CDECL
    fi

    if [ "$level" != "minimal" ]; then
        cat >> "$lang_dir/builtin.vml" << 'STANDARD'
; ---- 基础浮点操作 ----
.linked  "../shared/float.vml"

; ---- 系统调用常量 (SYS_* / CFG_*) ----
.linked  "../shared/syscall.inc.vml"

; ---- 控制台 I/O (print/println/input) ----
.linked  "console.vml"

; ---- 输入输出 ----
.linked  "../shared/io.vml"

; ---- 格式化输入输出 ----
.linked  "../shared/printf.vml"

; ---- 字符串操作 ----
.linked  "../shared/string.vml"

; ---- 数学函数 ----
.linked  "../shared/math.vml"

; ---- 类型转换 ----
.linked  "../shared/convert.vml"
.linked  "conv.vml"

; ---- 字符分类 ----
.linked  "../shared/ctype.vml"

; ---- 位运算 ----
.linked  "../shared/bitops.vml"

; ---- 内存管理 ----
.linked  "../shared/memory.vml"
STANDARD
    fi

    # Lua 额外运行时
    if [ "$lang_name" = "lua" ] && [ -f "$lang_dir/lua_meta.vml" ]; then
        echo '.linked  "lua_meta.vml"' >> "$lang_dir/builtin.vml"
    fi

    if [ "$level" = "full" ]; then
        cat >> "$lang_dir/builtin.vml" << 'FULL'
; ---- BASIC 兼容层（全量库） ----
.linked  "../shared/string.vml"
.linked  "../shared/math.vml"
.linked  "../shared/io.vml"
.linked  "../shared/file.vml"
.linked  "../shared/ctype.vml"
.linked  "../shared/bitops.vml"
.linked  "../shared/convert.vml"
.linked  "../shared/memory.vml"
.linked  "../shared/util.vml"
.linked  "../shared/readline.vml"
.linked  "../shared/vga_text.vml"
.linked  "../shared/graphics.vml"
.linked  "../shared/browser_gfx.vml"
FULL
    fi

    eval "$SED_I 's/LANG_UPPER/${lang_upper}/g' $lang_dir/builtin.vml"
    eval "$SED_I 's/LANG_NAME/${lang_name}/g' $lang_dir/builtin.vml"
    pass "${lang_name}/builtin.vml"
}

# 各语言的 stdlib.vml 模板
# 手动 .linked  使用，包含常用标准库
write_stdlib() {
    local lang_dir="$1"
    local lang_name="$2"
    cat > "$lang_dir/stdlib.vml" << 'STDLIB_EOF'
; ============================================================
; LANG_NAME Standard Library
; 常用标准库 — 使用时在代码中添加: .linked  "stdlib.vml"
;
; 包含: string, math, io, file, printf, ctype, memory, util
; ============================================================

; ---- 先包含内置库 ----
.linked  "builtin.vml"

; ---- 字符串操作 (strlen/strcpy/strcmp/strcat 等) ----
.linked  "../shared/string.vml"

; ---- 数学函数 (sin/cos/tan/sqrt/pow/log 等) ----
.linked  "../shared/math.vml"

; ---- 输入输出 (getchar/putchar/puts/gets 等) ----
.linked  "../shared/io.vml"

; ---- 文件操作 (fopen/fclose/fread/fwrite 等) ----
.linked  "../shared/file.vml"

; ---- 格式化输入输出 (printf/sprintf/scanf 等) ----
.linked  "../shared/printf.vml"

; ---- 字符分类 (isalpha/isdigit/isspace 等) ----
.linked  "../shared/ctype.vml"

; ---- 位运算 (AND/OR/XOR/NOT/SHL/SHR) ----
.linked  "../shared/bitops.vml"

; ---- 类型转换 (atoi/itoa/ftoa 等) ----
.linked  "../shared/convert.vml"

; ---- 内存管理扩展 ----
.linked  "../shared/memory.vml"

; ---- 工具函数 ----
.linked  "../shared/util.vml"

; ---- 控制台读行 (readline) ----
.linked  "../shared/readline.vml"
STDLIB_EOF
    eval "$SED_I 's/LANG_NAME/${lang_name}/g' $lang_dir/stdlib.vml"
    pass "${lang_name}/stdlib.vml"
}

# 各语言的 vmllib.vml 模板
# 手动 .linked  使用，包含全部共享库
write_vmllib() {
    local lang_dir="$1"
    local lang_name="$2"
    cat > "$lang_dir/vmllib.vml" << 'VMLLIB_EOF'
; ============================================================
; LANG_NAME VML Complete Library
; 完整共享库 — 使用时在代码中添加: .linked  "vmllib.vml"
;
; 包含所有 shared/ 模块
; ============================================================

; ---- 先包含标准库 ----
.linked  "stdlib.vml"

; ---- OS 模式扩展 (进程/线程等, --mode os 下有效) ----
.linked  "../shared/os.vml"

; ---- 网络函数 ----
.linked  "../shared/network.vml"

; ---- 调试函数 ----
.linked  "../shared/debug.vml"

; ---- 图形库 (VGA 绘图) ----
.linked  "../shared/graphics.vml"

; ---- VGA 文本模式 ----
.linked  "../shared/vga_text.vml"

; ---- 浏览器图形 (WASM Canvas) ----
.linked  "../shared/browser_gfx.vml"

; ---- 软件浮点扩展 (完整精度) ----
.linked  "../shared/softfloat.vml"
.linked  "../shared/softint64.vml"
.linked  "../shared/softdouble.vml"
VMLLIB_EOF
    eval "$SED_I 's/LANG_NAME/${lang_name}/g' $lang_dir/vmllib.vml"
    pass "${lang_name}/vmllib.vml"
}

build_lang_aggregators() {
    echo ""
    echo "============================================"
    echo "  Phase 4: 生成各语言 builtin/stdlib/vmllib"
    echo "============================================"

    # 所有语言目录
    local all_langs=(
        "Basic"
        "c"
        "cpp"
        "csharp"
        "forth"
        "go"
        "java"
        "javascript"
        "Kotlin"
        "ladder"
        "lua"
        "Pascal"
        "python"
        "rust"
        "Scheme"
        "swift"
        "ruby"
        "dart"
        "objc"
        "r"
        "d"
        "fortran"
    )

    for lang in "${all_langs[@]}"; do
        local lang_dir="$SCRIPT_DIR/$lang"
        if [ ! -d "$lang_dir" ]; then
            info "跳过 $lang (目录不存在)"
            continue
        fi

        echo "--- $lang ---"
        write_builtin "$lang_dir" "$lang"
        write_stdlib "$lang_dir" "$lang"
        write_vmllib "$lang_dir" "$lang"
    done
}

# ============================================================
# Phase 5: 编译动态库 (可选)
# ============================================================
build_dynamic() {
    echo ""
    echo "============================================"
    echo "  Phase 5: 编译动态库 (dynamic/)"
    echo "============================================"

    local dir="$SCRIPT_DIR/dynamic"

    for cfile in "$dir"/*.c; do
        [ -f "$cfile" ] || continue
        local name
        name="$(basename "$cfile" .c)"
        local vmlfile="$dir/${name}.vml"

        info "编译 dynamic/${name}.c → dynamic/${name}.vml"
        if compile_c "$cfile" "$vmlfile"; then
            pass "dynamic/${name}.vml"
        else
            fail "dynamic/${name}.vml (可能需要外部头文件)"
        fi
    done
}

# ============================================================
# 主流程
# ============================================================

print_summary() {
    echo ""
    echo "============================================"
    echo "  构建完成: $PASSED 成功, $FAILED 失败"
    echo "============================================"
    echo ""
    echo "各语言库文件 (builtin.vml / stdlib.vml / vmllib.vml):"
    echo "  编译器自动链接 → builtin.vml   (必备运行时)"
    echo "  按需手动引入   → stdlib.vml    (常用标准库)"
    echo "  按需手动引入   → vmllib.vml    (完整共享库)"
    echo ""
    echo "使用示例:"
    echo "  // C 代码中:"
    echo "  #include <stdio.h>   // 标准头文件"
    echo "  // 编译时自动链接 Lib/c/builtin.vml"
    echo ""
    echo "  // VML 汇编中:"
    echo "  .linked  \"stdlib.vml\"   // 引入常用标准库"
    echo "  .linked  \"vmllib.vml\"   // 引入完整共享库"
}

main() {
    cd "$SCRIPT_DIR"

    echo "VML Library Build Script"
    echo "项目根目录: $PROJECT_ROOT"
    echo "工作目录:   $SCRIPT_DIR"

    # 显示使用的工具路径
    info "VMLTool: $VMLTOOL"

    local mode="${1:-all}"

    case "$mode" in
        --shared)
            build_shared_modules
            build_shared_root
            ;;
        --c)
            build_c_libs
            ;;
        --lang)
            build_lang_aggregators
            ;;
        --dynamic)
            build_dynamic
            ;;
        all|*)
            build_shared_modules
            build_shared_root
            build_c_libs
            build_lang_aggregators
            # 默认不编译动态库 (需要外部依赖)
            ;;
    esac

    print_summary
}

main "$@"

#!/bin/bash
# ============================================================
# Lib/build_static — 静态库绑定生成器
#
# 从用户编写的 C 源码编译 .vml，生成 16 语言绑定文件。
# 内部调用 VMLTool (C 编译器) + GenLib (绑定生成)。
#
# 用法:
#   ./build_static myui.c                    # 编译+生成所有语言绑定
#   ./build_static myui.c -o libgfx          # 指定输出库名
#   ./build_static myui.c --lang basic,pascal # 仅生成指定语言
#
# 示例 (myui.c):
#   int setpoint(int x, int y, int co) { ... }
#   void clear_screen(int color) { ... }
#
# 生成文件:
#   shared/myui.vml             — VML 编译产物 (静态链接)
#   c/myui.h                    — C/C++ 头文件
#   <lang>/ext/myui.<ext>       — 各语言绑定文件
#
# 相关工具:
#   GenLib (tools/GenLib)     — VML 标准共享库管理 (shared_/vml_ 前缀函数)
#   GenDyn (tools/GenDyn) — 外部动态库 FFI 绑定
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
info()  { echo -e "${YELLOW}[..]${NC} $1" >&2; }
pass()  { echo -e "${GREEN}[OK]${NC} $1" >&2; }
fail()  { echo -e "${RED}[!!]${NC} $1" >&2; }
upper() { echo "$1" | tr '[:lower:]' '[:upper:]'; }
capitalize() { echo "$1" | awk '{print toupper(substr($0,1,1)) substr($0,2)}'; }

# 自动检测工具: tools/ → tools/bin/ → 源码编译发布到 tools/bin/
resolve_tool() {
    local name="$1" proj="$2"
    # 1) tools/<name> (发布包)
    if [ -f "$SCRIPT_DIR/../tools/$name" ] && [ -x "$SCRIPT_DIR/../tools/$name" ]; then
        echo "$SCRIPT_DIR/../tools/$name"
        return
    elif [ -f "$SCRIPT_DIR/../tools/$name.exe" ]; then
        echo "$SCRIPT_DIR/../tools/$name.exe"
        return
    fi
    # 2) tools/bin/<name> (上次源码编译发布)
    local asm_name=""
    asm_name="$(basename "$proj")"
    if [ -f "$SCRIPT_DIR/../tools/bin/$name" ] && [ -x "$SCRIPT_DIR/../tools/bin/$name" ]; then
        echo "$SCRIPT_DIR/../tools/bin/$name"
        return
    elif [ -f "$SCRIPT_DIR/../tools/bin/$name.exe" ]; then
        echo "$SCRIPT_DIR/../tools/bin/$name.exe"
        return
    elif [ -f "$SCRIPT_DIR/../tools/bin/$asm_name" ] && [ -x "$SCRIPT_DIR/../tools/bin/$asm_name" ]; then
        echo "$SCRIPT_DIR/../tools/bin/$asm_name"
        return
    elif [ -f "$SCRIPT_DIR/../tools/bin/$asm_name.exe" ]; then
        echo "$SCRIPT_DIR/../tools/bin/$asm_name.exe"
        return
    fi
    # 3) 有源码 → dotnet publish 到 tools/bin/，下次直接使用
    if [ -d "$PROJECT_ROOT/$proj" ]; then
        local tools_bin="$SCRIPT_DIR/../tools/bin"
        mkdir -p "$tools_bin"
        info "首次使用，正在编译发布 ${proj} → tools/bin/ ..."
        dotnet publish "$PROJECT_ROOT/$proj" -c Release -o "$tools_bin" >&2 || {
            fail "编译发布失败"
            echo ""
            return
        }
        pass "发布完成"
        # 查找可执行文件: 短名称 → 程序集名
        local found=""
        local asm_name=""
        asm_name="$(basename "$proj")"
        for candidate in "$tools_bin/$name" "$tools_bin/$name.exe" "$tools_bin/$asm_name" "$tools_bin/$asm_name.exe"; do
            if [ -f "$candidate" ] && [ -x "$candidate" ]; then
                found="$candidate"; break
            elif [ -f "$candidate" ] && [[ "$candidate" == *.exe ]]; then
                found="$candidate"; break
            fi
        done
        if [ -z "$found" ]; then
            fail "找不到编译产物: $tools_bin/$name 或 $tools_bin/$asm_name"
        fi
        echo "${found:-}"
    else
        echo ""
    fi
}

VMLTOOL="$(resolve_tool "vmltool" "VMLTool")"
[ -z "$VMLTOOL" ] && { echo "错误: 找不到 vmltool (发布目录或源码项目)"; exit 1; }

# ============================================================
# 解析 C 函数签名
# ============================================================
parse_functions() {
    local cfile="$1"
    grep -n '^\s*\(void\|int\|char\|short\|long\|float\|double\|unsigned\)\s\+[_*a-zA-Z][_a-zA-Z0-9]*\s*(' "$cfile" |
        grep -v '^\s*//' |
        sed 's/^[0-9]*://; s/\s*{.*//; s/^\s*//' |
        while IFS= read -r line; do
            local rest="${line#* }"
            local ret_type="${line%% *}"
            local func_name="${rest%%(*}"
            func_name="${func_name##* }"
            case "$func_name" in if|while|for|return|switch|case|sizeof) continue ;; esac
            local params="${rest#*(}"
            params="${params%%)*}"
            params="$(echo "$params" | sed 's/\[[^]]*\]//g')"
            echo "$ret_type|$func_name|$params"
        done
}

# ============================================================
# 语言绑定生成器
# ============================================================

gen_vml_include() {
    local libname="$1" out="$2" funcs="$3"
    {
        echo "; ${libname}.vml — 静态库"
        echo "; 用法: .linked  \"../shared/${libname}.vml\""
        echo ""
        echo "$funcs" | while IFS='|' read -r ret name params; do
            echo "; ${ret} ${name}(${params})"
        done
        echo ""
    } > "$out"
}

gen_c_header() {
    local libname="$1" out="$2" funcs="$3"
    {
        echo "/* ${libname}.h — C/C++ 头文件 (静态链接) */"
        echo "/* 用法: #include \"${libname}.h\" */"
        echo "#ifndef $(upper "$libname")_H"
        echo "#define $(upper "$libname")_H"
        echo "#ifdef __cplusplus"
        echo "extern \"C\" {"
        echo "#endif"
        echo "$funcs" | while IFS='|' read -r ret name params; do
            echo "${ret} ${name}(${params});"
        done
        echo "#ifdef __cplusplus"
        echo "}"
        echo "#endif"
        echo "#endif"
    } > "$out"
}

gen_basic_binding() {
    local libname="$1" out="$2" funcs="$3"
    {
        echo "' ${libname}.bas — VML ${libname} Static Bindings (BASIC)"
        echo "' Auto-generated by build_static"
        echo ""
        echo "$funcs" | while IFS='|' read -r ret name params; do
            local basic_ret="INTEGER"
            [ "$ret" = "void" ] && basic_ret=""
            local decl=""
            [ -n "$basic_ret" ] && decl="DECLARE FUNCTION ${name}(" || decl="DECLARE SUB ${name}("
            local first=true
            IFS=',' read -ra param_arr <<< "$params"
            for p in "${param_arr[@]}"; do
                p="$(echo "$p" | xargs)"
                [ -z "$p" ] || [ "$p" = "void" ] && continue
                [ "$first" = true ] || decl+=", "
                first=false
                local pname="${p##* }"
                local ptype="${p% $pname}"
                local bas_type="AS INTEGER"
                [ "$ptype" = "char*" ] && bas_type="AS STRING"
                decl+="BYVAL ${pname} ${bas_type}"
            done
            decl+=")"
            [ -n "$basic_ret" ] && decl+=" AS INTEGER"
            echo "$decl"
            echo "    asm(\"CALL ${name}\")"
            [ -n "$basic_ret" ] && echo "    ${name} = R0"
            echo "END $([ -n "$basic_ret" ] && echo "FUNCTION" || echo "SUB")"
            echo ""
        done
    } > "$out"
}

gen_pascal_binding() {
    local libname="$1" out="$2" funcs="$3"
    {
        echo "{ ${libname}.pas — VML ${libname} Static Bindings (Pascal) }"
        echo "{ Auto-generated by build_static }"
        echo "interface"
        echo ""
        echo "$funcs" | while IFS='|' read -r ret name params; do
            local pas_ret="integer"
            [ "$ret" = "void" ] && pas_ret=""
            local decl="function ${name}("
            local first=true
            IFS=',' read -ra param_arr <<< "$params"
            for p in "${param_arr[@]}"; do
                p="$(echo "$p" | xargs)"
                [ -z "$p" ] || [ "$p" = "void" ] && continue
                [ "$first" = true ] || decl+="; "
                first=false
                local pname="${p##* }"
                local ptype="${p% $pname}"
                local pas_type="integer"
                [ "$ptype" = "char*" ] && pas_type="string"
                decl+="${pname}: ${pas_type}"
            done
            decl+=")"
            [ -n "$pas_ret" ] && decl+=": ${pas_ret}"
            echo "${decl}; external;"
        done
    } > "$out"
}

gen_python_binding() {
    local libname="$1" out="$2" funcs="$3"
    {
        echo "# ${libname}.py — VML ${libname} Static Bindings (Python)"
        echo "# Auto-generated by build_static"
        echo ""
        echo "$funcs" | while IFS='|' read -r ret name params; do
            local py_ret="int"
            [ "$ret" = "void" ] && py_ret="None"
            local decl="def ${name}("
            local first=true
            IFS=',' read -ra param_arr <<< "$params"
            for p in "${param_arr[@]}"; do
                p="$(echo "$p" | xargs)"
                [ -z "$p" ] || [ "$p" = "void" ] && continue
                [ "$first" = true ] || decl+=", "
                first=false
                local pname="${p##* }"
                local ptype="${p% $pname}"
                local py_type="int"
                [ "$ptype" = "char*" ] && py_type="str"
                decl+="${pname}: ${py_type}"
            done
            decl+=") -> ${py_ret}:"
            echo "$decl"
            echo "    pass  # VML static call"
            echo ""
        done
    } > "$out"
}

gen_stub_binding() {
    local lang="$1" out="$2" funcs="$3" cmt="$4"
    {
        echo "${cmt} ${libname}.${out##*.} — VML ${libname} Static Bindings (${lang})"
        echo "${cmt} Auto-generated by build_static"
        echo ""
        echo "$funcs" | while IFS='|' read -r ret name params; do
            echo "${cmt} ${ret} ${name}(${params})"
        done
    } > "$out"
}

gen_go_binding() {
    local libname="$1" out="$2" funcs="$3"
    {
        echo "// ${libname}.go — VML ${libname} Static Bindings (Go)"
        echo "// Auto-generated by build_static"
        echo "package ext"
        echo "/*"
        echo "$funcs" | while IFS='|' read -r ret name params; do
            echo "${ret} ${name}(${params});"
        done
        echo "*/"
        echo "import \"C\""
        echo ""
        echo "$funcs" | while IFS='|' read -r ret name params; do
            local go_ret=""
            [ "$ret" != "void" ] && go_ret="int { return C.${name}("
            local cap_name
            cap_name="$(capitalize "$name")"
            local decl="func ${cap_name}("
            IFS=',' read -ra param_arr <<< "$params"
            local call_args=""
            local first=true
            for p in "${param_arr[@]}"; do
                p="$(echo "$p" | xargs)"
                [ -z "$p" ] || [ "$p" = "void" ] && continue
                [ "$first" = true ] || { decl+=", "; call_args+=", "; }
                first=false
                local pname="${p##* }"
                local ptype="${p% $pname}"
                local gotype="int"; local gocast="C.int"
                [ "$ptype" = "char*" ] && { gotype="string"; gocast="C.CString"; }
                decl+="${pname} ${gotype}"
                call_args+="${gocast}(${pname})"
            done
            decl+=")"
            if [ "$ret" != "void" ]; then
                decl+=" ${go_ret}${call_args}) }"
            else
                decl+=" { C.${name}(${call_args}) }"
            fi
            echo "$decl"
        done
    } > "$out"
}

gen_rust_binding() {
    local libname="$1" out="$2" funcs="$3"
    {
        echo "// ${libname}.rs — VML ${libname} Static Bindings (Rust)"
        echo "// Auto-generated by build_static"
        echo "extern \"C\" {"
        echo "$funcs" | while IFS='|' read -r ret name params; do
            local rs_ret=""
            [ "$ret" != "void" ] && rs_ret=" -> i32"
            local decl="    fn ${name}("
            local first=true
            IFS=',' read -ra param_arr <<< "$params"
            for p in "${param_arr[@]}"; do
                p="$(echo "$p" | xargs)"
                [ -z "$p" ] || [ "$p" = "void" ] && continue
                [ "$first" = true ] || decl+=", "
                first=false
                local pname="${p##* }"
                local ptype="${p% $pname}"
                local rstype="i32"
                [ "$ptype" = "char*" ] && rstype="*const u8"
                decl+="${pname}: ${rstype}"
            done
            decl+=")${rs_ret};"
            echo "$decl"
        done
        echo "}"
    } > "$out"
}

# ============================================================
# 主流程
# ============================================================

main() {
    cd "$SCRIPT_DIR"
    local cfile="" libname="" lang_filter=""

    while [ $# -gt 0 ]; do
        case "$1" in
            -o) libname="$2"; shift 2 ;;
            --lang) lang_filter="$2"; shift 2 ;;
            -h|--help)
                echo "build_static — 用户 C 源码 → VML + 16 语言静态绑定"
                echo "用法: build_static <file.c> [-o libname] [--lang bas,pas,...]"
                echo ""
                echo "相关工具:"
                echo "  GenLib        VML 标准共享库管理 (Lib/shared/src/*.c)"
                echo "  GenDyn   外部动态库 FFI 绑定生成"
                exit 0
                ;;
            *) cfile="$1"; shift ;;
        esac
    done

    [ -z "$cfile" ] && { fail "用法: build_static <file.c> [-o libname] [--lang bas,pas,...]"; exit 1; }
    [ ! -f "$cfile" ] && { fail "文件不存在: $cfile"; exit 1; }
    [ -z "$libname" ] && libname="$(basename "$cfile" .c)"

    info "解析: $cfile"
    local funcs
    funcs="$(parse_functions "$cfile")"
    if [ -z "$funcs" ]; then
        fail "未找到函数定义"
        exit 1
    fi
    echo "$funcs" | while IFS='|' read -r ret name params; do
        echo "  ${ret} ${name}(${params})"
    done

    # 编译 C → VML (与 GenLib build 命令相同的方式)
    info "编译 ${cfile} → shared/${libname}.vml"
    local comp_log
    comp_log="$($VMLTOOL -c "$cfile" -o "$SCRIPT_DIR/shared/${libname}.vml" --no-link -I "$SCRIPT_DIR/shared" -I "$SCRIPT_DIR/c" --mode mcu 2>&1)" || {
        echo "$comp_log"
        fail "编译失败"
        exit 1
    }
    pass "shared/${libname}.vml"

    # 生成各语言绑定
    local langs=("vml" "c" "cpp" "basic" "pascal" "python" "lua" "javascript" "go" "rust" "csharp" "java" "kotlin" "swift" "forth" "ladder" "scheme" "ruby" "dart" "objc" "r" "d" "fortran")

    for lang in "${langs[@]}"; do
        [ -n "$lang_filter" ] && [[ ",${lang_filter}," != *",${lang},"* ]] && continue

        local outdir="" outext="" cmt=""
        case "$lang" in
            vml)  outdir="$SCRIPT_DIR/shared"; outext="vml" ;;
            c)    outdir="$SCRIPT_DIR/c"; outext="h" ;;
            cpp)  outdir="$SCRIPT_DIR/cpp"; outext="h" ;;
            basic) outdir="$SCRIPT_DIR/Basic/ext"; outext="bas" ;;
            pascal) outdir="$SCRIPT_DIR/Pascal/ext"; outext="pas" ;;
            python) outdir="$SCRIPT_DIR/python/ext"; outext="py" ;;
            lua)  outdir="$SCRIPT_DIR/lua/ext"; outext="lua" ;;
            javascript) outdir="$SCRIPT_DIR/javascript/ext"; outext="js" ;;
            go)   outdir="$SCRIPT_DIR/go/ext"; outext="go" ;;
            rust) outdir="$SCRIPT_DIR/rust/ext"; outext="rs" ;;
            csharp) outdir="$SCRIPT_DIR/csharp/ext"; outext="cs" ;;
            java) outdir="$SCRIPT_DIR/java/ext"; outext="java" ;;
            kotlin) outdir="$SCRIPT_DIR/Kotlin/ext"; outext="kt" ;;
            swift) outdir="$SCRIPT_DIR/swift/ext"; outext="swift" ;;
            forth) outdir="$SCRIPT_DIR/forth/ext"; outext="fth" ;;
            ladder) outdir="$SCRIPT_DIR/ladder/ext"; outext="ld" ;;
            scheme) outdir="$SCRIPT_DIR/Scheme/ext"; outext="scm" ;;
            ruby) outdir="$SCRIPT_DIR/ruby/ext"; outext="rb" ;;
            dart) outdir="$SCRIPT_DIR/dart/ext"; outext="dart" ;;
            objc) outdir="$SCRIPT_DIR/objc/ext"; outext="h" ;;
            r) outdir="$SCRIPT_DIR/r/ext"; outext="r" ;;
            d) outdir="$SCRIPT_DIR/d/ext"; outext="d" ;;
            fortran) outdir="$SCRIPT_DIR/fortran/ext"; outext="f90" ;;
        esac
        [ -z "$outdir" ] && continue
        mkdir -p "$outdir"

        local outfile="$outdir/${libname}.$outext"
        case "$lang" in
            vml)  gen_vml_include "$libname" "$outfile" "$funcs" ;;
            c|cpp) gen_c_header "$libname" "$outfile" "$funcs" ;;
            basic) gen_basic_binding "$libname" "$outfile" "$funcs" ;;
            pascal) gen_pascal_binding "$libname" "$outfile" "$funcs" ;;
            python) gen_python_binding "$libname" "$outfile" "$funcs" ;;
            go)   gen_go_binding "$libname" "$outfile" "$funcs" ;;
            rust) gen_rust_binding "$libname" "$outfile" "$funcs" ;;
            lua)  gen_stub_binding "Lua" "$outfile" "$funcs" "--" ;;
            javascript) gen_stub_binding "JavaScript" "$outfile" "$funcs" "//" ;;
            csharp) gen_stub_binding "C#" "$outfile" "$funcs" "//" ;;
            java) gen_stub_binding "Java" "$outfile" "$funcs" "//" ;;
            kotlin) gen_stub_binding "Kotlin" "$outfile" "$funcs" "//" ;;
            swift) gen_stub_binding "Swift" "$outfile" "$funcs" "//" ;;
            forth) gen_stub_binding "Forth" "$outfile" "$funcs" "\\\\" ;;
            ladder) gen_stub_binding "Ladder" "$outfile" "$funcs" "//" ;;
            scheme) gen_stub_binding "Scheme" "$outfile" "$funcs" ";;" ;;
            ruby) gen_stub_binding "Ruby" "$outfile" "$funcs" "#" ;;
            dart) gen_stub_binding "Dart" "$outfile" "$funcs" "//" ;;
            objc) gen_stub_binding "ObjC" "$outfile" "$funcs" "//" ;;
            r) gen_stub_binding "R" "$outfile" "$funcs" "#" ;;
            d) gen_stub_binding "D" "$outfile" "$funcs" "//" ;;
            fortran) gen_stub_binding "Fortran" "$outfile" "$funcs" "!" ;;
        esac
        pass "${lang}/${libname}.$outext"
    done

    echo ""
    echo "===== 完成 ====="
    echo "VML 静态库:  shared/${libname}.vml"
    echo "C 头文件:    c/${libname}.h"
    echo "各语言绑定:  <lang>/ext/${libname}.<ext>"
    echo ""
    echo "使用示例 (BASIC):"
    echo "  .linked  \"${libname}.bas\""
    echo "  result = Setpoint(10, 20, 255)"
}

main "$@"

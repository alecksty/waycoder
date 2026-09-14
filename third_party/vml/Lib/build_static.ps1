# ============================================================
# Lib/build_static.ps1 — 静态库绑定生成器 (Windows PowerShell)
#
# 从用户编写的 C 源码编译 .vml，生成 16 语言绑定文件。
# 内部调用 VMLTool (C 编译器)。
#
# 用法:
#   .\build_static.ps1 myui.c                    # 编译+生成所有语言绑定
#   .\build_static.ps1 myui.c -o libgfx          # 指定输出库名
#   .\build_static.ps1 myui.c -Lang basic,pascal # 仅生成指定语言
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
#   GenLib (tools/GenLib)     — VML 标准共享库管理
#   GenDyn (tools/GenDyn) — 外部动态库 FFI 绑定
# ============================================================
param(
    [Parameter(Position=0)][string]$CFile = "",
    [string]$OutputName = "",
    [string]$Lang = "",
    [switch]$Help
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# 自动检测工具: tools/ → tools/bin/ → 源码编译发布到 tools/bin/
$VMLToolBin = ""
$toolsDir = "$ScriptDir\..\tools"
$toolsBinDir = "$toolsDir\bin"
if (Test-Path "$toolsDir\vmltool.exe" -PathType Leaf) {
    $VMLToolBin = "$toolsDir\vmltool.exe"
} elseif (Test-Path "$toolsDir\vmltool" -PathType Leaf) {
    $VMLToolBin = "$toolsDir\vmltool"
} elseif (Test-Path "$toolsBinDir\vmltool.exe" -PathType Leaf) {
    $VMLToolBin = "$toolsBinDir\vmltool.exe"
} elseif (Test-Path "$toolsBinDir\vmltool" -PathType Leaf) {
    $VMLToolBin = "$toolsBinDir\vmltool"
} elseif (Test-Path "$toolsBinDir\VMLTool.exe" -PathType Leaf) {
    $VMLToolBin = "$toolsBinDir\VMLTool.exe"
} elseif (Test-Path "$toolsBinDir\VMLTool" -PathType Leaf) {
    $VMLToolBin = "$toolsBinDir\VMLTool"
}
$needPublish = $false
if (-not $VMLToolBin -and (Test-Path "$ProjectRoot\VMLTool")) { $needPublish = $true }
elseif (-not $VMLToolBin) { Write-Fail "找不到 vmltool (发布目录或源码项目)"; exit 1 }

function Invoke-VMLTool([string[]]$Args) {
    if ($needPublish) {
        Write-Info "首次使用，正在编译发布 VMLTool → tools/bin/ ..."
        New-Item -ItemType Directory -Path $toolsBinDir -Force | Out-Null
        dotnet publish "$ProjectRoot\VMLTool" -c Release -o $toolsBinDir 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Fail "编译发布失败"; exit 1 }
        Write-Pass "VMLTool 发布完成"
        # 查找可执行文件: 短名称 → 程序集名
        if (Test-Path "$toolsBinDir\vmltool.exe" -PathType Leaf) { $VMLToolBin = "$toolsBinDir\vmltool.exe" }
        elseif (Test-Path "$toolsBinDir\vmltool" -PathType Leaf) { $VMLToolBin = "$toolsBinDir\vmltool" }
        elseif (Test-Path "$toolsBinDir\VMLTool.exe" -PathType Leaf) { $VMLToolBin = "$toolsBinDir\VMLTool.exe" }
        elseif (Test-Path "$toolsBinDir\VMLTool" -PathType Leaf) { $VMLToolBin = "$toolsBinDir\VMLTool" }
        if (-not $VMLToolBin) { Write-Fail "找不到编译产物: $toolsBinDir\vmltool 或 $toolsBinDir\VMLTool"; exit 1 }
        $needPublish = $false
    }
    if ($VMLToolBin) {
        & $VMLToolBin $Args 2>&1
    } else {
        dotnet run --project "$ProjectRoot\VMLTool" -- $Args 2>&1
    }
}

function Write-Info  { Write-Host "[..] $args" -ForegroundColor Yellow }
function Write-Pass  { Write-Host "[OK] $args" -ForegroundColor Green }
function Write-Fail  { Write-Host "[!!] $args" -ForegroundColor Red }
function ToUpper([string]$s) { $s.ToUpper() }
function Capitalize([string]$s) {
    if ([string]::IsNullOrEmpty($s)) { return $s }
    return $s.Substring(0,1).ToUpper() + $s.Substring(1)
}

if ($Help -or [string]::IsNullOrEmpty($CFile)) {
    Write-Host "build_static.ps1 — 用户 C 源码 → VML + 16 语言静态绑定"
    Write-Host "用法: .\build_static.ps1 <file.c> [-OutputName <name>] [-Lang bas,pas,...]"
    Write-Host ""
    Write-Host "相关工具:"
    Write-Host "  GenLib        VML 标准共享库管理 (Lib/shared/src/*.c)"
    Write-Host "  GenDyn   外部动态库 FFI 绑定生成"
    exit 0
}

if (-not (Test-Path $CFile)) { Write-Fail "文件不存在: $CFile"; exit 1 }
if ([string]::IsNullOrEmpty($OutputName)) { $OutputName = [System.IO.Path]::GetFileNameWithoutExtension($CFile) }

# ============================================================
# 解析 C 函数签名
# ============================================================
function Parse-Functions([string]$CFile) {
    $content = Get-Content $CFile -Raw
    $pattern = '(?m)^\s*(void|int|char|short|long|float|double|unsigned)\s+[_*a-zA-Z][_a-zA-Z0-9]*\s*\('
    $lines = [regex]::Matches($content, "$pattern.*") | ForEach-Object {
        ($_.Value -replace '\s*\{.*', '') -replace '^\s*', ''
    }
    $result = @()
    foreach ($line in $lines) {
        if ($line -match '^\s*//') { continue }
        $rest = $line -replace '^\S+\s+', ''
        $retType = ($line -split '\s+')[0]
        $funcName = ($rest -split '\(')[0]
        $funcName = ($funcName -split '\s+')[-1]
        $keywords = @('if','while','for','return','switch','case','sizeof')
        if ($funcName -in $keywords) { continue }
        $params = $rest -replace '^[^(]*\(', '' -replace '\).*', ''
        $params = $params -replace '\[[^\]]*\]', ''
        $result += [PSCustomObject]@{ Ret=$retType; Name=$funcName; Params=$params.Trim() }
    }
    return $result
}

# ============================================================
# 生成器函数
# ============================================================

function Gen-VmlInclude($libname, $out, $funcs) {
    $sb = @()
    $sb += "; $libname.vml — 静态库"
    $sb += '; 用法: .linked  "../shared/' + $libname + '.vml"'
    $sb += ""
    foreach ($f in $funcs) { $sb += "; $($f.Ret) $($f.Name)($($f.Params))" }
    $sb | Out-File -FilePath $out -Encoding utf8
}

function Gen-CHeader($libname, $out, $funcs) {
    $upper = $libname.ToUpper()
    $sb = @()
    $sb += "/* $libname.h — C/C++ 头文件 (静态链接) */"
    $sb += "/* 用法: #include `"$libname.h`" */"
    $sb += "#ifndef ${upper}_H"
    $sb += "#define ${upper}_H"
    $sb += "#ifdef __cplusplus"
    $sb += 'extern "C" {'
    $sb += "#endif"
    foreach ($f in $funcs) { $sb += "$($f.Ret) $($f.Name)($($f.Params));" }
    $sb += "#ifdef __cplusplus"
    $sb += "}"
    $sb += "#endif"
    $sb += "#endif"
    $sb | Out-File -FilePath $out -Encoding utf8
}

function Gen-BasicBinding($libname, $out, $funcs) {
    $sb = @()
    $sb += "' $libname.bas — VML $libname Static Bindings (BASIC)"
    $sb += "' Auto-generated by build_static"
    $sb += ""
    foreach ($f in $funcs) {
        $basicRet = if ($f.Ret -eq 'void') { '' } else { 'INTEGER' }
        $decl = if ($basicRet) { "DECLARE FUNCTION $($f.Name)(" } else { "DECLARE SUB $($f.Name)(" }
        $first = $true
        $plist = $f.Params -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -ne 'void' }
        foreach ($p in $plist) {
            if (-not $first) { $decl += ", " }; $first = $false
            $pname = ($p -split '\s+')[-1]
            $ptype = $p -replace "\s+$pname`$", ''
            $basType = if ($ptype -eq 'char*') { 'STRING' } else { 'INTEGER' }
            $decl += "BYVAL $pname AS $basType"
        }
        $decl += ")"
        if ($basicRet) { $decl += " AS INTEGER" }
        $sb += $decl
        $sb += "    asm(`"CALL $($f.Name)`")"
        if ($basicRet) { $sb += "    $($f.Name) = R0" }
        $endKw = if ($basicRet) { "FUNCTION" } else { "SUB" }
        $sb += "END $endKw"
        $sb += ""
    }
    $sb | Out-File -FilePath $out -Encoding utf8
}

function Gen-PascalBinding($libname, $out, $funcs) {
    $sb = @()
    $sb += "{ $libname.pas — VML $libname Static Bindings (Pascal) }"
    $sb += "{ Auto-generated by build_static }"
    $sb += "interface"
    $sb += ""
    foreach ($f in $funcs) {
        $pasRet = if ($f.Ret -eq 'void') { '' } else { 'integer' }
        $decl = "function $($f.Name)("
        $first = $true
        $plist = $f.Params -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -ne 'void' }
        foreach ($p in $plist) {
            if (-not $first) { $decl += "; " }; $first = $false
            $pname = ($p -split '\s+')[-1]
            $ptype = $p -replace "\s+$pname`$", ''
            $pasType = if ($ptype -eq 'char*') { 'string' } else { 'integer' }
            $decl += "$pname`:` $pasType"
        }
        $decl += ")"
        if ($pasRet) { $decl += ": $pasRet" }
        $sb += "$decl; external;"
    }
    $sb | Out-File -FilePath $out -Encoding utf8
}

function Gen-PythonBinding($libname, $out, $funcs) {
    $sb = @()
    $sb += "# $libname.py — VML $libname Static Bindings (Python)"
    $sb += "# Auto-generated by build_static"
    $sb += ""
    foreach ($f in $funcs) {
        $pyRet = if ($f.Ret -eq 'void') { 'None' } else { 'int' }
        $decl = "def $($f.Name)("
        $first = $true
        $plist = $f.Params -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -ne 'void' }
        foreach ($p in $plist) {
            if (-not $first) { $decl += ", " }; $first = $false
            $pname = ($p -split '\s+')[-1]
            $ptype = $p -replace "\s+$pname`$", ''
            $pyType = if ($ptype -eq 'char*') { 'str' } else { 'int' }
            $decl += "$pname`:` $pyType"
        }
        $decl += ") -> $pyRet`:`"
        $sb += $decl
        $sb += "    pass  # VML static call"
        $sb += ""
    }
    $sb | Out-File -FilePath $out -Encoding utf8
}

function Gen-GoBinding($libname, $out, $funcs) {
    $sb = @()
    $sb += "// $libname.go — VML $libname Static Bindings (Go)"
    $sb += "// Auto-generated by build_static"
    $sb += "package ext"
    $sb += "/*"
    foreach ($f in $funcs) { $sb += "$($f.Ret) $($f.Name)($($f.Params));" }
    $sb += "*/"
    $sb += 'import "C"'
    $sb += ""
    foreach ($f in $funcs) {
        $capName = Capitalize $f.Name
        $goRet = if ($f.Ret -ne 'void') { 'int { return C.' + $f.Name + '(' } else { '' }
        $decl = "func $capName("
        $callArgs = ""
        $first = $true
        $plist = $f.Params -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -ne 'void' }
        foreach ($p in $plist) {
            if (-not $first) { $decl += ", "; $callArgs += ", " }; $first = $false
            $pname = ($p -split '\s+')[-1]
            $ptype = $p -replace "\s+$pname`$", ''
            $goType = if ($ptype -eq 'char*') { 'string' } else { 'int' }
            $goCast = if ($ptype -eq 'char*') { 'C.CString' } else { 'C.int' }
            $decl += "$pname $goType"
            $callArgs += "$goCast($pname)"
        }
        $decl += ")"
        if ($f.Ret -ne 'void') { $decl += " $goRet$callArgs) }" }
        else { $decl += " { C.$($f.Name)($callArgs) }" }
        $sb += $decl
    }
    $sb | Out-File -FilePath $out -Encoding utf8
}

function Gen-RustBinding($libname, $out, $funcs) {
    $sb = @()
    $sb += "// $libname.rs — VML $libname Static Bindings (Rust)"
    $sb += "// Auto-generated by build_static"
    $sb += 'extern "C" {'
    foreach ($f in $funcs) {
        $rsRet = if ($f.Ret -ne 'void') { ' -> i32' } else { '' }
        $decl = "    fn $($f.Name)("
        $first = $true
        $plist = $f.Params -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -and $_ -ne 'void' }
        foreach ($p in $plist) {
            if (-not $first) { $decl += ", " }; $first = $false
            $pname = ($p -split '\s+')[-1]
            $ptype = $p -replace "\s+$pname`$", ''
            $rsType = if ($ptype -eq 'char*') { '*const u8' } else { 'i32' }
            $decl += "$pname`:` $rsType"
        }
        $decl += ")$rsRet;"
        $sb += $decl
    }
    $sb += "}"
    $sb | Out-File -FilePath $out -Encoding utf8
}

function Gen-StubBinding($lang, $out, $funcs, $cmt) {
    $ext = [System.IO.Path]::GetExtension($out)
    $sb = @()
    $sb += "$cmt $lang binding — VML $OutputName Static Bindings ($lang)"
    $sb += "$cmt Auto-generated by build_static"
    $sb += ""
    foreach ($f in $funcs) { $sb += "$cmt $($f.Ret) $($f.Name)($($f.Params))" }
    $sb | Out-File -FilePath $out -Encoding utf8
}

# ============================================================
# 主流程
# ============================================================

Set-Location $ScriptDir

Write-Info "解析: $CFile"
$funcs = Parse-Functions $CFile
if ($funcs.Count -eq 0) { Write-Fail "未找到函数定义"; exit 1 }
foreach ($f in $funcs) { Write-Host "  $($f.Ret) $($f.Name)($($f.Params))" }

# 编译 C → VML
$vmlOut = "$ScriptDir\shared\$OutputName.vml"
Write-Info "编译 $CFile → shared/$OutputName.vml"
$result = Invoke-VMLTool -c $CFile -o $vmlOut --no-link -I "$ScriptDir\shared" -I "$ScriptDir\c" --mode mcu
if ($LASTEXITCODE -ne 0) {
    Write-Host $result
    Write-Fail "编译失败"
    exit 1
}
Write-Pass "shared/$OutputName.vml"

# 语言输出目录映射
$langDirs = @{
    'vml' = @{ Dir="$ScriptDir\shared"; Ext="vml" }
    'c' = @{ Dir="$ScriptDir\c"; Ext="h" }
    'cpp' = @{ Dir="$ScriptDir\cpp"; Ext="h" }
    'basic' = @{ Dir="$ScriptDir\Basic\ext"; Ext="bas" }
    'pascal' = @{ Dir="$ScriptDir\Pascal\ext"; Ext="pas" }
    'python' = @{ Dir="$ScriptDir\python\ext"; Ext="py" }
    'lua' = @{ Dir="$ScriptDir\lua\ext"; Ext="lua" }
    'javascript' = @{ Dir="$ScriptDir\javascript\ext"; Ext="js" }
    'go' = @{ Dir="$ScriptDir\go\ext"; Ext="go" }
    'rust' = @{ Dir="$ScriptDir\rust\ext"; Ext="rs" }
    'csharp' = @{ Dir="$ScriptDir\csharp\ext"; Ext="cs" }
    'java' = @{ Dir="$ScriptDir\java\ext"; Ext="java" }
    'kotlin' = @{ Dir="$ScriptDir\Kotlin\ext"; Ext="kt" }
    'swift' = @{ Dir="$ScriptDir\swift\ext"; Ext="swift" }
    'forth' = @{ Dir="$ScriptDir\forth\ext"; Ext="fth" }
    'ladder' = @{ Dir="$ScriptDir\ladder\ext"; Ext="ld" }
    'scheme' = @{ Dir="$ScriptDir\Scheme\ext"; Ext="scm" }
    'ruby' = @{ Dir="$ScriptDir\ruby\ext"; Ext="rb" }
    'dart' = @{ Dir="$ScriptDir\dart\ext"; Ext="dart" }
    'objc' = @{ Dir="$ScriptDir\objc\ext"; Ext="h" }
    'r' = @{ Dir="$ScriptDir\r\ext"; Ext="r" }
    'd' = @{ Dir="$ScriptDir\d\ext"; Ext="d" }
    'fortran' = @{ Dir="$ScriptDir\fortran\ext"; Ext="f90" }
}

$langFilter = if ($Lang) { $Lang -split ',' | ForEach-Object { $_.Trim().ToLower() } } else { @() }

foreach ($entry in $langDirs.GetEnumerator()) {
    $lang = $entry.Key
    if ($langFilter.Count -gt 0 -and $lang -notin $langFilter) { continue }

    $dir = $entry.Value.Dir
    $ext = $entry.Value.Ext
    $outDir = $dir
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }
    $outFile = "$outDir\$OutputName.$ext"

    switch ($lang) {
        'vml' { Gen-VmlInclude $OutputName $outFile $funcs }
        'c' { Gen-CHeader $OutputName $outFile $funcs }
        'cpp' { Gen-CHeader $OutputName $outFile $funcs }
        'basic' { Gen-BasicBinding $OutputName $outFile $funcs }
        'pascal' { Gen-PascalBinding $OutputName $outFile $funcs }
        'python' { Gen-PythonBinding $OutputName $outFile $funcs }
        'go' { Gen-GoBinding $OutputName $outFile $funcs }
        'rust' { Gen-RustBinding $OutputName $outFile $funcs }
        'lua' { Gen-StubBinding 'Lua' $outFile $funcs '--' }
        'javascript' { Gen-StubBinding 'JavaScript' $outFile $funcs '//' }
        'csharp' { Gen-StubBinding 'C#' $outFile $funcs '//' }
        'java' { Gen-StubBinding 'Java' $outFile $funcs '//' }
        'kotlin' { Gen-StubBinding 'Kotlin' $outFile $funcs '//' }
        'swift' { Gen-StubBinding 'Swift' $outFile $funcs '//' }
        'forth' { Gen-StubBinding 'Forth' $outFile $funcs '\' }
        'ladder' { Gen-StubBinding 'Ladder' $outFile $funcs '//' }
        'scheme' { Gen-StubBinding 'Scheme' $outFile $funcs ';;' }
        'ruby' { Gen-StubBinding 'Ruby' $outFile $funcs '#' }
        'dart' { Gen-StubBinding 'Dart' $outFile $funcs '//' }
        'objc' { Gen-StubBinding 'ObjC' $outFile $funcs '//' }
        'r' { Gen-StubBinding 'R' $outFile $funcs '#' }
        'd' { Gen-StubBinding 'D' $outFile $funcs '//' }
        'fortran' { Gen-StubBinding 'Fortran' $outFile $funcs '!' }
    }
    Write-Pass "$lang/$OutputName.$ext"
}

Write-Host ""
Write-Host "===== 完成 ====="
Write-Host "VML 静态库:  shared/$OutputName.vml"
Write-Host "C 头文件:    c/$OutputName.h"
Write-Host "各语言绑定:  <lang>/ext/$OutputName.<ext>"
Write-Host ""
Write-Host "使用示例 (BASIC):"
Write-Host '  .linked  "' + $OutputName + '.bas"'
Write-Host "  result = Setpoint(10, 20, 255)"

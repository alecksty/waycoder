# ============================================================
# Lib/build_libs.ps1 — Windows: rebuild all VML library files
# ============================================================
param([switch]$Shared, [switch]$C, [switch]$Lang)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$Passed = 0; $Failed = 0

function Pass($m) { Write-Host "[OK] $m" -ForegroundColor Green; $script:Passed++ }
function Fail($m) { Write-Host "[FAIL] $m" -ForegroundColor Red; $script:Failed++ }
function Info($m) { Write-Host "[..] $m" -ForegroundColor Yellow }

# ---- resolve vmltool ----
function tool {
    foreach ($c in @("$ScriptDir\..\tools\vmltool.exe", "$ScriptDir\..\tools\bin\vmltool.exe", "$ScriptDir\..\tools\bin\VMLTool.exe")) {
        if (Test-Path $c) { return (Resolve-Path $c).Path }
    }
    $proj = "$ProjectRoot\VMLTool\VMLTool.csproj"
    if (Test-Path $proj) {
        $bin = "$ScriptDir\..\tools\bin"
        ni -Force -ItemType Directory $bin | Out-Null
        Info "Publishing VMLTool..."
        dotnet publish $proj -c Release -o $bin 2>&1 | Out-Null
        $exe = Get-ChildItem "$bin\*.exe" | Select-Object -First 1
        if ($exe) { return $exe.FullName }
    }
    return $null
}
$VTOOL = tool
if (-not $VTOOL) { Write-Host "ERROR: vmltool not found"; exit 1 }
Info "VMLTool: $VTOOL"

# ---- compile one C file ----
function cc($cfile, $vml) {
    if (-not (Test-Path $cfile)) { Fail "missing: $cfile"; return $false }
    $dir = Split-Path -Parent $cfile
    $name = Split-Path -Leaf $cfile
    Push-Location $dir
    $inc = "-I $ScriptDir\shared -I $ScriptDir\c -I $ScriptDir"
    $log = & $VTOOL -c $name -o $vml $inc --mode mcu --no-link 2>&1
    $ok = ($LASTEXITCODE -eq 0)
    Pop-Location
    if (-not $ok) { Write-Host $log }
    return $ok
}

# ---- write a .vml file with lines ----
function wv($path, $lines) {
    $utf8 = New-Object System.Text.UTF8Encoding $true
    [System.IO.File]::WriteAllLines($path, $lines, $utf8)
}

# ---- Phase 1: shared/src/*.c ----
function build_shared_src {
    Write-Host "`n=== Phase 1: shared/src/ ==="
    foreach ($c in Get-ChildItem "$ScriptDir\shared\src\*.c") {
        $n = $c.BaseName; $v = "$ScriptDir\shared\$n.vml"
        Info "shared/src/$n.c"
        if (cc $c.FullName $v) { Pass "shared/$n.vml" } else { Fail "shared/$n.vml" }
    }
}

# ---- Phase 2: shared/*.c root ----
function build_shared_root {
    Write-Host "`n=== Phase 2: shared/ root ==="
    foreach ($c in Get-ChildItem "$ScriptDir\shared\*.c") {
        $n = $c.BaseName; $v = "$ScriptDir\shared\$n.vml"
        Info "shared/$n.c"
        if (cc $c.FullName $v) { Pass "shared/$n.vml" } else { Fail "shared/$n.vml" }
    }
}

# ---- Phase 3: c/*.c ----
function build_c {
    Write-Host "`n=== Phase 3: c/ ==="
    foreach ($c in Get-ChildItem "$ScriptDir\c\*.c") {
        $n = $c.BaseName; $v = "$ScriptDir\c\$n.vml"
        Info "c/$n.c"
        if (cc $c.FullName $v) { Pass "c/$n.vml" } else { Fail "c/$n.vml" }
    }
    if (Test-Path "$ScriptDir\c\src") {
        foreach ($c in Get-ChildItem "$ScriptDir\c\src\*.c") {
            $n = $c.BaseName; $v = "$ScriptDir\c\$n.vml"
            Info "c/src/$n.c"
            if (cc $c.FullName $v) { Pass "c/$n.vml" } else { Fail "c/$n.vml" }
        }
    }
}

# ---- Phase 4: builtin/stdlib/vmllib per language ----
function build_lang {
    Write-Host "`n=== Phase 4: language aggregators ==="
    $langs = @("Basic","c","cpp","csharp","forth","go","java","javascript","Kotlin","ladder","lua","Pascal","python","rust","Scheme","swift","ruby","dart","objc","r","d","fortran")
    foreach ($lang in $langs) {
        $d = "$ScriptDir\$lang"
        if (-not (Test-Path $d)) { Info "skip $lang"; continue }
        Write-Host "--- $lang ---"
        $ll = $lang.ToLower()
        $lv = if ($ll -eq "c" -or $ll -eq "cpp") { "minimal" } elseif ($ll -eq "basic") { "full" } else { "standard" }

        # builtin.vml
        $b = @()
        $b += "; $lang builtin library (auto-linked)"
        $b += '.linked  "../shared/builtins.vml"'
        $b += '.linked  "../shared/io.vml"'
        $b += '.linked  "../shared/device.vml"'
        $b += '.linked  "../shared/sysinfo.vml"'
        $b += "#ifdef VML_FLOAT32_SOFT"
        $b += '.linked  "../shared/softfloat.vml"'
        $b += "#endif"
        $b += "#ifdef VML_FLOAT64_SOFT"
        $b += '.linked  "../shared/softdouble.vml"'
        $b += "#endif"
        $b += "#ifdef VML_INT64_SOFT"
        $b += '.linked  "../shared/softint64.vml"'
        $b += "#endif"
        if ($lv -eq "minimal") {
            $b += '.linked  "../shared/printf.vml"'
            $b += '.linked  "../shared/scanf.vml"'
        }
        if ($lv -ne "minimal") {
            $b += '.linked  "../shared/float.vml"'
            $b += '.linked  "../shared/syscall.inc.vml"'
            $b += '.linked  "console.vml"'
        }
        # Lua 额外运行时
        if ($ll -eq "lua" -and (Test-Path "$d\lua_meta.vml")) {
            $b += '.linked  "lua_meta.vml"'
        }
        if ($lv -eq "full") {
            $b += '.linked  "../shared/string.vml"'
            $b += '.linked  "../shared/math.vml"'
            $b += '.linked  "../shared/io.vml"'
            $b += '.linked  "../shared/file.vml"'
            $b += '.linked  "../shared/ctype.vml"'
            $b += '.linked  "../shared/bitops.vml"'
            $b += '.linked  "../shared/convert.vml"'
            $b += '.linked  "../shared/memory.vml"'
            $b += '.linked  "../shared/util.vml"'
            $b += '.linked  "../shared/readline.vml"'
            $b += '.linked  "../shared/vga_text.vml"'
            $b += '.linked  "../shared/graphics.vml"'
            $b += '.linked  "../shared/browser_gfx.vml"'
        }
        wv "$d\builtin.vml" $b
        Pass "$lang/builtin.vml"

        # stdlib.vml
        $s = @()
        $s += "; $lang standard library"
        $s += '.linked  "builtin.vml"'
        $s += '.linked  "../shared/string.vml"'
        $s += '.linked  "../shared/math.vml"'
        $s += '.linked  "../shared/io.vml"'
        $s += '.linked  "../shared/file.vml"'
        $s += '.linked  "../shared/printf.vml"'
        $s += '.linked  "../shared/ctype.vml"'
        $s += '.linked  "../shared/bitops.vml"'
        $s += '.linked  "../shared/convert.vml"'
        $s += '.linked  "../shared/memory.vml"'
        $s += '.linked  "../shared/util.vml"'
        $s += '.linked  "../shared/readline.vml"'
        wv "$d\stdlib.vml" $s
        Pass "$lang/stdlib.vml"

        # vmllib.vml
        $v = @()
        $v += "; $lang complete library"
        $v += '.linked  "stdlib.vml"'
        $v += '.linked  "../shared/os.vml"'
        $v += '.linked  "../shared/network.vml"'
        $v += '.linked  "../shared/debug.vml"'
        $v += '.linked  "../shared/graphics.vml"'
        $v += '.linked  "../shared/vga_text.vml"'
        $v += '.linked  "../shared/browser_gfx.vml"'
        $v += '.linked  "../shared/softfloat.vml"'
        $v += '.linked  "../shared/softint64.vml"'
        $v += '.linked  "../shared/softdouble.vml"'
        wv "$d\vmllib.vml" $v
        Pass "$lang/vmllib.vml"
    }
}

# ---- main ----
Set-Location $ScriptDir
Write-Host "VML Library Build (Windows)"
if ($Shared) { build_shared_src; build_shared_root }
elseif ($C) { build_c }
elseif ($Lang) { build_lang }
else { build_shared_src; build_shared_root; build_c; build_lang }

Write-Host "`n============================================"
Write-Host "  Done: $Passed passed, $Failed failed"
Write-Host "============================================"

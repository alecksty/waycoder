#!/usr/bin/env pwsh
<#
.SYNOPSIS
    VML MCU 设备头文件生成器 (Windows / macOS / Linux)
.DESCRIPTION
    遍历 Devices/ 目录下所有设备 XML，为 22 种语言生成设备头文件。
    内部调用 GenDev 工具。
.PARAMETER Clean
    清理已生成的设备头文件
.PARAMETER Verbose
    显示详细输出
.PARAMETER Jobs
    并行度 (默认 CPU 核心数)
.EXAMPLE
    .\MakeDevice.ps1
    .\MakeDevice.ps1 -Clean
    .\MakeDevice.ps1 -Verbose -Jobs 8
#>

param(
    [switch]$Clean,
    [switch]$Verbose,
    [int]$Jobs = 0,
    [switch]$Help
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$DevicesDir = Join-Path $ProjectRoot "Devices"
$LibDir = $ScriptDir
$ToolsDir = Join-Path $ProjectRoot "tools"
$ToolsBinDir = Join-Path $ToolsDir "bin"

function Write-Info  { Write-Host "[..] $args" -ForegroundColor Yellow }
function Write-Pass  { Write-Host "[OK] $args" -ForegroundColor Green }
function Write-Fail  { Write-Host "[!!] $args" -ForegroundColor Red }

# ============================================================
# 工具查找: tools/ → tools/bin/ → 源码编译发布到 tools/bin/
# ============================================================
$DeviceCodeGenBin = ""

# 1) tools/ 目录
if (Test-Path "$ToolsDir\gendev.exe" -PathType Leaf) {
    $DeviceCodeGenBin = "$ToolsDir\gendev.exe"
}

# 2) tools/bin/ 目录 (短名 + 程序集名)
if (-not $DeviceCodeGenBin) {
    foreach ($name in @("gendev", "GenDev")) {
        $p = if ($IsWindows) { "$ToolsBinDir\$name.exe" } else { "$ToolsBinDir\$name" }
        if (Test-Path $p -PathType Leaf) {
            $DeviceCodeGenBin = $p
            break
        }
    }
}

# 3) 有源码 → 延迟发布 (首次调用 Invoke-DeviceCodeGen 时)
$needPublish = $false
if (-not $DeviceCodeGenBin -and (Test-Path "$ProjectRoot\tools\GenDev")) {
    $needPublish = $true
}

if (-not $DeviceCodeGenBin -and -not $needPublish) {
    Write-Fail "找不到 gendev (发布目录或源码项目)"
    exit 1
}

function Invoke-DeviceCodeGen([string[]]$Args) {
    if ($needPublish) {
        Write-Info "首次使用，正在编译发布 tools/GenDev → tools/bin/ ..."
        $proj = "$ProjectRoot\tools\GenDev\GenDev.csproj"
        dotnet publish $proj -c Release -o $ToolsBinDir 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Fail "编译发布失败"
            exit 1
        }
        Write-Pass "发布完成"
        $needPublish = $false
        # 查找编译产物
        foreach ($name in @("gendev", "GenDev")) {
            $p = if ($IsWindows) { "$ToolsBinDir\$name.exe" } else { "$ToolsBinDir\$name" }
            if (Test-Path $p -PathType Leaf) {
                $script:DeviceCodeGenBin = $p
                break
            }
        }
        if (-not $script:DeviceCodeGenBin) {
            Write-Fail "找不到编译产物"
            exit 1
        }
    }
    & $script:DeviceCodeGenBin $Args 2>&1
}

# ============================================================
# 语言映射
# ============================================================
$LanguageMap = @{
    "c" = "c"; "basic" = "Basic"; "pascal" = "Pascal"; "vml" = "vml"
    "ladder" = "ladder"; "python" = "python"; "forth" = "forth"; "lua" = "lua"
    "go" = "go"; "rust" = "rust"; "java" = "java"; "javascript" = "javascript"
    "swift" = "swift"; "csharp" = "csharp"; "cpp" = "cpp"; "kotlin" = "Kotlin"
    "scheme" = "Scheme"; "ruby" = "ruby"; "dart" = "dart"; "objc" = "objc"; "r" = "r"; "d" = "d"; "fortran" = "fortran"
}

$LanguageExtensions = @{
    "c" = ".h"; "basic" = ".bas"; "pascal" = ".pas"; "vml" = ".vml"
    "ladder" = ".ld"; "python" = ".py"; "forth" = ".fth"; "lua" = ".lua"
    "go" = ".go"; "rust" = ".rs"; "java" = ".java"; "javascript" = ".js"
    "swift" = ".swift"; "csharp" = ".cs"; "cpp" = ".hpp"; "kotlin" = ".kt"
    "scheme" = ".scm"; "ruby" = ".rb"; "dart" = ".dart"; "objc" = ".h"; "r" = ".r"; "d" = ".d"; "fortran" = ".f90"
}

# ============================================================
# 清理
# ============================================================
function Clean-DeviceFiles {
    Write-Info "清理设备头文件..."
    $totalCleaned = 0
    foreach ($lang in $LanguageMap.Keys) {
        $langDir = $LanguageMap[$lang]
        $deviceDir = Join-Path $LibDir $langDir "Device"
        if (Test-Path $deviceDir) {
            $files = Get-ChildItem $deviceDir -Filter "*$($LanguageExtensions[$lang])" -File
            foreach ($file in $files) {
                Remove-Item $file.FullName -Force
                if ($Verbose) { Write-Host "  删除: $($file.Name)" -ForegroundColor Yellow }
                $totalCleaned++
            }
        }
    }
    Write-Pass "清理完成，共删除 $totalCleaned 个文件"
}

# ============================================================
# 生成
# ============================================================
function Generate-DeviceFiles {
    Write-Info "GenDev: $DeviceCodeGenBin"
    Write-Host ""
    Write-Host "============================================"
    Write-Host "  VML MCU 设备头文件生成"
    Write-Host "============================================"

    if (-not (Test-Path $DevicesDir)) {
        Write-Fail "设备目录不存在: $DevicesDir"
        exit 1
    }

    $deviceFiles = Get-ChildItem $DevicesDir -Recurse -Filter "*.xml" -File
    $totalDevices = $deviceFiles.Count

    if ($totalDevices -eq 0) {
        Write-Host "警告: 未找到设备描述文件"
        return
    }

    $jobs = if ($Jobs -gt 0) { $Jobs } elseif ($env:VML_JOBS) { [int]$env:VML_JOBS } else { [Environment]::ProcessorCount }
    Write-Host "  共 $totalDevices 个设备, 并行度: $jobs"
    Write-Host ""

    # 为每种语言创建 Device 目录
    foreach ($lang in $LanguageMap.Keys) {
        $langDir = $LanguageMap[$lang]
        $deviceDir = Join-Path $LibDir $langDir "Device"
        if (-not (Test-Path $deviceDir)) {
            New-Item -ItemType Directory -Path $deviceDir -Force | Out-Null
        }
    }

    $successCount = 0
    $failCount = 0
    $lock = [System.Threading.Mutex]::new()

    $deviceFiles | ForEach-Object -Parallel {
        $deviceFile = $_
        $deviceName = $deviceFile.BaseName
        $LibDir = $using:LibDir
        $invokeCmd = $using:DeviceCodeGenBin
        $Verbose = $using:Verbose
        $lock = $using:lock

        $langDirs = @{
            c="c"; basic="Basic"; pascal="Pascal"; vml="vml"; ladder="ladder"
            python="python"; forth="forth"; lua="lua"; go="go"; rust="rust"
            java="java"; javascript="javascript"; swift="swift"; csharp="csharp"; cpp="cpp"
            kotlin="Kotlin"; scheme="Scheme"; ruby="ruby"; dart="dart"; objc="objc"; r="r"; d="d"; fortran="fortran"
        }
        $langExts = @{
            c=".h"; basic=".bas"; pascal=".pas"; vml=".vml"; ladder=".ld"
            python=".py"; forth=".fth"; lua=".lua"; go=".go"; rust=".rs"
            java=".java"; javascript=".js"; swift=".swift"; csharp=".cs"; cpp=".hpp"
            kotlin=".kt"; scheme=".scm"; ruby=".rb"; dart=".dart"; objc=".h"; r=".r"; d=".d"; fortran=".f90"
        }

        $cOutDir = Join-Path $LibDir "c" "Device"
        $cOutFile = Join-Path $cOutDir "$deviceName.h"

        & $invokeCmd -i $deviceFile.FullName -o $cOutFile -a 2>$null
        $ok = ($LASTEXITCODE -eq 0)

        if ($ok) {
            foreach ($ext in $langExts.GetEnumerator()) {
                if ($ext.Key -eq "c") { continue }
                $src = Join-Path $cOutDir "$deviceName$($ext.Value)"
                $dstDir = Join-Path $LibDir $langDirs[$ext.Key] "Device"
                $dst = Join-Path $dstDir "$deviceName$($ext.Value)"
                if (Test-Path $src) { Move-Item $src $dst -Force }
            }
            $null = $lock.WaitOne()
            $script:successCount++
            $lock.ReleaseMutex()
            if ($Verbose) { Write-Host "  OK: $deviceName" -ForegroundColor Green }
        } else {
            $null = $lock.WaitOne()
            $script:failCount++
            $lock.ReleaseMutex()
            Write-Host "  失败: $deviceName" -ForegroundColor Red
        }
    } -ThrottleLimit $jobs

    Write-Host ""
    if ($failCount -gt 0) {
        Write-Host "  完成: $successCount 成功, $failCount 失败"
    } else {
        Write-Host "  完成: $successCount 个设备 × 22 种语言 = $($successCount * 22) 个头文件"
    }
    Write-Host ""
    foreach ($lang in $LanguageMap.Keys) {
        $langDir = $LanguageMap[$lang]
        $deviceDir = Join-Path $LibDir $langDir "Device"
        if (Test-Path $deviceDir) {
            $cnt = (Get-ChildItem $deviceDir -Filter "*$($LanguageExtensions[$lang])" -File | Measure-Object).Count
            Write-Host "  ${lang}: $cnt 个文件"
        }
    }
}

# ============================================================
# 主流程
# ============================================================
if ($Help) {
    Write-Host "VML MCU 设备头文件生成器"
    Write-Host "用法: MakeDevice.ps1 [选项]"
    Write-Host "  -Clean     清理所有生成的设备头文件"
    Write-Host "  -Verbose   详细输出"
    Write-Host "  -Jobs N    并行度 (默认: CPU 核心数)"
    Write-Host "  -Help      显示帮助"
    Write-Host ""
    Write-Host "示例:"
    Write-Host "  .\MakeDevice.ps1"
    Write-Host "  .\MakeDevice.ps1 -Clean"
    Write-Host "  .\MakeDevice.ps1 -Verbose -Jobs 8"
    exit 0
}

if ($Clean) {
    Clean-DeviceFiles
} else {
    Generate-DeviceFiles
}

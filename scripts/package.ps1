# ═══════════════════════════════════════════════════════════════
# WayCoder 一键多平台打包 (PowerShell / Windows 原生)
#   当前平台 → NativeAOT（零依赖原生单文件）
#   其他平台 → 非 AOT（自包含单文件 JIT，可跨平台交叉发布）
#
# 用法:
#   .\scripts\package.ps1                      # 打包全部 6 个平台
#   .\scripts\package.ps1 win-x64 linux-x64    # 只打包指定平台
#
# 产物: dist\waycoder-<版本>-<RID>.zip (Windows) / .tar.gz (Linux/macOS)
#
# 依赖: Windows AOT 需要 Visual Studio 2022 C++ 工具链（MSVC 链接器），
#       脚本会自动把 VS Installer 目录加入 PATH 以定位 vswhere.exe。
# ═══════════════════════════════════════════════════════════════
param(
    [string[]]$Rids = @()
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoDir   = Split-Path -Parent $ScriptDir
$Proj      = Join-Path $RepoDir "WayCoder\WayCoder.csproj"
$SrcDir    = Join-Path $RepoDir "WayCoder"
$Dist      = Join-Path $RepoDir "dist"

# ── 版本号：从 Global.cs 提取 ──
$globalCs = Get-Content (Join-Path $SrcDir "Config\Global.cs") -Raw
$Version  = if ($globalCs -match 'Version\s*=\s*"(v[0-9.]+)"') { $Matches[1] }
            else { "v$(Get-Date -Format 'yyyyMMdd')" }

# ── 当前平台检测（Windows 原生脚本，宿主固定为 win） ──
$HostRid = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'win-arm64' } else { 'win-x64' }

# Windows AOT 需要 vswhere.exe（VS Installer）定位 MSVC 链接器，确保其在 PATH
$VsInstaller = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer"
if (Test-Path $VsInstaller) {
    if ($env:PATH -notlike "*$VsInstaller*") {
        $env:PATH = "$VsInstaller;$env:PATH"
    }
}

# ── 目标平台（默认 6 个，可命令行覆盖） ──
$DefaultRids = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')
$TargetRids  = if ($Rids.Count -eq 0) { $DefaultRids } else { $Rids }

Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  WayCoder 打包 ($Version)" -ForegroundColor Cyan
Write-Host "  当前平台: $HostRid → NativeAOT" -ForegroundColor Cyan
Write-Host "  目标平台: $($TargetRids -join ' ')" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan

New-Item -ItemType Directory -Force -Path $Dist | Out-Null

foreach ($Rid in $TargetRids) {
    $IsAot  = ($Rid -eq $HostRid)
    $Mode   = if ($IsAot) { "NativeAOT" } else { "JIT（非 AOT）" }
    $Out    = Join-Path $Dist $Rid

    Write-Host ""
    Write-Host "▶ [$Rid] $Mode 编译中…" -ForegroundColor Yellow
    Remove-Item $Out -Recurse -Force -ErrorAction SilentlyContinue

    # 清理 obj/bin，避免不同 RID 之间的还原状态污染（否则会误报 Cross-OS）
    Remove-Item (Join-Path $SrcDir "obj"), (Join-Path $SrcDir "bin") -Recurse -Force -ErrorAction SilentlyContinue

    $aotVal = $IsAot.ToString().ToLower()
    dotnet publish $Proj -c Release -r $Rid -o $Out `
        --self-contained true -p:PublishAot=$aotVal -p:PublishSingleFile=true `
        --nologo -v q

    # ── 打包 ──
    if ($Rid -like 'win-*') {
        $Archive = Join-Path $Dist "waycoder-$Version-$Rid.zip"
        Remove-Item $Archive -ErrorAction SilentlyContinue
        Get-ChildItem $Out | Where-Object { $_.Extension -ne '.pdb' } |
            Compress-Archive -DestinationPath $Archive -Force
        Write-Host "  ✅ $Archive" -ForegroundColor Green
    }
    else {
        $Archive = Join-Path $Dist "waycoder-$Version-$Rid.tar.gz"
        Remove-Item $Archive -ErrorAction SilentlyContinue
        tar -czf $Archive -C $Out --exclude='*.pdb' .
        Write-Host "  ✅ $Archive" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  打包完成，产物:" -ForegroundColor Cyan
Get-ChildItem (Join-Path $Dist "waycoder-*") | ForEach-Object {
    Write-Host ("  {0,-12} {1,10:N0} KB" -f $_.Name, ($_.Length / 1KB))
}
Write-Host "═══════════════════════════════════════════════" -ForegroundColor Cyan

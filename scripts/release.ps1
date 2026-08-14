# ═══════════════════════════════════════════════════════════════
# WayCoder 一键发布（半自动，PowerShell 版）
#
# 流程（6 步）：
#   1. 编译全部 6 平台包（复用 scripts/package.ps1）
#   2. 计算全部产物 SHA256
#   3. winget：生成新版本 manifest 目录 + 填 sha256 + 本地 winget validate
#   4. brew  ：更新 formula 版本号 + sha256
#   5. apt   ：Windows 无 dpkg-deb，打印待执行命令
#   6. 打印「提交到各服务器」的精确命令，人工确认后逐条执行
#
# 用法:
#   .\scripts\release.ps1                  # 全流程
#   .\scripts\release.ps1 -SkipBuild       # 已有 dist 产物，跳过编译
#
# 依赖: dotnet 10 + PowerShell 5.1+；winget 校验可选（未装则跳过）
# ═══════════════════════════════════════════════════════════════
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoDir   = Split-Path -Parent $ScriptDir
$SrcDir    = Join-Path $RepoDir "WayCoder"
$Dist      = Join-Path $RepoDir "dist"
$Packaging = Join-Path $RepoDir "packaging"

# ── 版本号（带 v / 不带 v 两种形式）──
$globalCs = Get-Content (Join-Path $SrcDir "Config\Global.cs") -Raw
$Version  = if ($globalCs -match 'Version\s*=\s*"(v[0-9.]+)"') { $Matches[1] }
            else { "v$(Get-Date -Format 'yyyyMMdd')" }
$Ver = $Version -replace '^v', ''

function C([string]$m)   { Write-Host $m -ForegroundColor Cyan }
function Ok([string]$m)  { Write-Host $m -ForegroundColor Green }
function Warn([string]$m){ Write-Host $m -ForegroundColor Yellow }

function Get-Sha256([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

C "═══════════════════════════════════════════════"
C "  WayCoder 一键发布 ($Version)"
C "═══════════════════════════════════════════════"

# ═══ 1. 编译 ═══
if ($SkipBuild) {
    Warn "▶ 跳过编译（-SkipBuild）"
} else {
    C "▶ 1/6 编译全部平台包…"
    & (Join-Path $RepoDir "scripts\package.ps1")
}

# ═══ 2. SHA256 ═══
C "▶ 2/6 计算 SHA256…"
$Rids = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'osx-x64', 'osx-arm64')
$Sha = @{}
foreach ($Rid in $Rids) {
    $ext = if ($Rid -like 'win-*') { 'zip' } else { 'tar.gz' }
    $a = Join-Path $Dist "waycoder-$Version-$Rid.$ext"
    if (-not (Test-Path $a)) { throw "缺少产物: $a" }
    $Sha[$Rid] = Get-Sha256 $a
    Write-Host ("  {0,-12} {1}" -f $Rid, $Sha[$Rid])
}

# ═══ 3. winget ═══
C "▶ 3/6 生成 winget manifest…"
$WingetBase = Join-Path $Packaging "winget\manifests\a\Aleckstygit\WayCoder"
$LatestVer  = Get-ChildItem $WingetBase -Directory |
    Sort-Object { [version]$_.Name } -Descending |
    Select-Object -First 1 -ExpandProperty Name
$NewDir = Join-Path $WingetBase $Ver

if ($LatestVer -eq $Ver) {
    Warn "  winget manifest $Ver 已存在，跳过生成（如需重生成请先删除 $NewDir）"
} else {
    if (Test-Path $NewDir) { Remove-Item $NewDir -Recurse -Force }
    Copy-Item (Join-Path $WingetBase $LatestVer) $NewDir -Recurse
    # 版本号全局替换（PackageVersion + InstallerUrl 里的 vX.Y.Z 一并覆盖）
    Get-ChildItem $NewDir -Filter *.yaml | ForEach-Object {
        $c = Get-Content $_.FullName -Raw
        $c = $c -replace [regex]::Escape($LatestVer), $Ver
        Set-Content $_.FullName $c -NoNewline
    }
    # 填 sha256（顺序：x64 → arm64，与 installer.yaml 一致）
    $Inst = Join-Path $NewDir "Aleckstygit.WayCoder.installer.yaml"
    $c = Get-Content $Inst -Raw
    $c = [regex]::Replace($c, 'REPLACE_WITH_SHA256', $Sha['win-x64'], 1)
    $c = [regex]::Replace($c, 'REPLACE_WITH_SHA256', $Sha['win-arm64'], 1)
    Set-Content $Inst $c -NoNewline
    Ok "  已生成 $NewDir"
}

if (Get-Command winget -ErrorAction SilentlyContinue) {
    winget validate $NewDir
    if ($LASTEXITCODE -eq 0) { Ok "  winget validate 通过" } else { Warn "  winget validate 有告警，请检查" }
} else {
    Warn "  未检测到 winget，跳过本地校验（可手动: winget validate `"$NewDir`"）"
}

# ═══ 4. brew ═══
C "▶ 4/6 更新 brew formula…"
$Formula = Join-Path $Packaging "brew\waycoder.rb"
$f = Get-Content $Formula -Raw
if ($f -match 'version "([0-9.]+)"') {
    $OldVer = $Matches[1]
    $f = $f -replace [regex]::Escape($OldVer), $Ver
    $f = [regex]::Replace($f, 'REPLACE_WITH_SHA256', $Sha['osx-arm64'], 1)
    $f = [regex]::Replace($f, 'REPLACE_WITH_SHA256', $Sha['osx-x64'], 1)
    Set-Content $Formula $f -NoNewline
    Ok "  已更新 $Formula → $Ver"
} else {
    Warn "  无法从 formula 提取版本号，跳过（请手动改 $Formula）"
}

# ═══ 5. apt ═══
C "▶ 5/6 apt .deb…"
Warn "  Windows 环境无 dpkg-deb，跳过 .deb 打包（下方打印待执行命令）"

# ═══ 6. 打印提交命令 ═══
Write-Host ""
C "═══════════════════════════════════════════════"
C "  提交到各服务器 —— 请人工确认后逐条执行"
C "═══════════════════════════════════════════════"

Write-Host ""
C "【0. 上传发行资产】"
Write-Host @"
  ① Gitee Release（国内主渠道，waycoder --update 优先走这里）:
    https://gitee.com/aleckstygit/my-coder/releases
    上传 dist\waycoder-$Version-*.zip / *.tar.gz 共 6 个资产

  ② GitHub Release（海外 mirror；winget/brew 清单 URL 必须指向它——Gitee 附件是数字 ID URL 不可预测）:
    走 Actions： git push github $Version
    或手动： https://github.com/alecksty/waycoder/releases/new?tag=$Version
"@

Write-Host ""
C "【1. winget → microsoft/winget-pkgs】"
Write-Host @"
  已生成（含 sha256）: $NewDir
  提交步骤（需已登录 gh + fork microsoft/winget-pkgs）:
    gh repo fork microsoft/winget-pkgs --clone
    Copy-Item -Recurse "$NewDir" `"`$HOME\...\winget-pkgs\manifests\a\Aleckstygit\WayCoder\$Ver`"
    cd 到 fork 目录
    git add -A; git commit -m "New version: Aleckstygit.WayCoder $Ver"
    git push; gh pr create --title "New version: Aleckstygit.WayCoder $Ver" --body "更新到 $Ver"
  本地免提交安装测试:
    winget install --manifest "$NewDir"
"@

Write-Host ""
C "【2. brew → gitee tap aleckstygit/homebrew-waycoder】"
Write-Host @"
  已更新（含 sha256）: $Formula
  提交步骤:
    git clone https://gitee.com/aleckstygit/homebrew-waycoder /tmp/homebrew-waycoder
    Copy-Item "$Formula" /tmp/homebrew-waycoder/Formula/waycoder.rb   # 若 tap 公式在根目录则去掉 Formula/
    cd /tmp/homebrew-waycoder; git add .; git commit -m "waycoder $Ver"; git push
  macOS 校验:
    brew audit --strict waycoder; brew test waycoder
"@

Write-Host ""
C "【3. apt → 自建 reprepro 仓库 + Pages 静态托管】"
Write-Host @"
  在 Linux 上执行（.deb 打包 + 仓库入库）:
    ./packaging/apt/build-deb.sh dist/linux-x64/waycoder $Ver amd64
    ./packaging/apt/build-deb.sh dist/linux-arm64/waycoder $Ver arm64

    mkdir -p repo/conf
    cat > repo/conf/distributions <<'CONF'
Origin: WayCoder
Label: WayCoder
Codename: stable
Architectures: amd64 arm64
Components: main
Description: WayCoder 官方 apt 仓库
SignWith: <你的 GPG key id>
CONF
    reprepro -b repo includedeb stable waycoder_${Ver}_amd64.deb
    reprepro -b repo includedeb stable waycoder_${Ver}_arm64.deb

    # 把 repo/ 发布到 Gitee/GitHub Pages（需 GPG 签名，详见 packaging/apt/README.md）
"@

Write-Host ""
Ok "完成。产物与清单已就绪，请按上述命令逐条提交。"

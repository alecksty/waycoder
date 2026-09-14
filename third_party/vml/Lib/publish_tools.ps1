# ============================================================
# publish_tools.ps1 — 编译发布核心工具到 tools/bin/<rid>/
#
# 工具列表:
#   VMLTool            主 CLI (C 编译器, 汇编器, 翻译器等)
#   GenLib            共享库管理
#   GenDyn      动态库 FFI 绑定生成
#   GenDev  设备代码生成
#
# 用法:
#   .\publish_tools.ps1                    # 当前平台
#   .\publish_tools.ps1 -r osx-x64         # 指定 RID
#   .\publish_tools.ps1 -r win-x64         # 当前平台 (默认)
#   .\publish_tools.ps1 -r linux-x64       # 交叉编译 Linux
#   .\publish_tools.ps1 --list             # 列出常用 RID
# ============================================================
param(
    [string]$Runtime = "",
    [switch]$List,
    [switch]$Help
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$OutputBase = "$ProjectRoot\tools\bin"

function Write-Info  { Write-Host "[..] $args" -ForegroundColor Yellow }
function Write-Pass  { Write-Host "[OK] $args" -ForegroundColor Green }
function Write-Fail  { Write-Host "[!!] $args" -ForegroundColor Red }

# ============================================================
# 平台检测
# ============================================================
function Get-DefaultRid {
    if ($IsMacOS)     { $os = "osx" }
    elseif ($IsLinux) { $os = "linux" }
    else              { $os = "win" }

    $arch = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
    # 覆盖 ARM64
    if ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") { $arch = "arm64" }

    return "${os}-${arch}"
}

# ============================================================
# 工具定义
# ============================================================
$Tools = @(
    @{ Proj="VMLTool/VMLTool.csproj";                 Asm="VMLTool";             Name="vmltool" },
    @{ Proj="tools/GenLib/GenLib.csproj";            Asm="GenLib";             Name="genlib" },
    @{ Proj="tools/GenDyn/GenDyn.csproj"; Asm="GenDyn";      Name="gendyn" },
    @{ Proj="tools/GenDev/GenDev.csproj"; Asm="GenDev"; Name="gendev" }
)

# ============================================================
# 主流程
# ============================================================
function Main {
    if ($Help) {
        Write-Host "publish_tools — 编译发布核心工具到 tools/bin/<rid>/"
        Write-Host ""
        Write-Host "用法: publish_tools.ps1 [-r RID] [--list]"
        Write-Host ""
        Write-Host "选项:"
        Write-Host "  -r RID       目标运行时标识符 (默认: 当前平台)"
        Write-Host "  --list       列出常用 RID"
        Write-Host ""
        Write-Host "工具: VMLTool, GenLib, GenDyn, GenDev"
        Write-Host ""
        Write-Host "常用 RID:"
        Write-Host "  win-x64    win-x86    win-arm64"
        Write-Host "  osx-x64    osx-arm64"
        Write-Host "  linux-x64  linux-arm64"
        return
    }

    if ($List) {
        Write-Host "常用 RID:"
        Write-Host "  win-x64       Windows x64"
        Write-Host "  win-x86       Windows x86"
        Write-Host "  win-arm64     Windows ARM64"
        Write-Host "  osx-x64       macOS Intel"
        Write-Host "  osx-arm64     macOS Apple Silicon"
        Write-Host "  linux-x64     Linux x86_64"
        Write-Host "  linux-arm64   Linux ARM64"
        return
    }

    $rid = if ($Runtime) { $Runtime } else { Get-DefaultRid }

    # 验证 RID 格式，防止路径遍历
    $ridPattern = '^[a-zA-Z][a-zA-Z0-9.-]+-[a-zA-Z][a-zA-Z0-9.-]+$'
    if ($rid -notmatch $ridPattern) {
        Write-Fail "无效的 RID 格式: $rid (预期格式: os-arch, 如 win-x64)"
        exit 1
    }

    Write-Info "目标平台: $rid"
    $outdir = "$OutputBase\$rid"
    New-Item -ItemType Directory -Path $outdir -Force | Out-Null

    Write-Host ""
    Write-Host "============================================"
    Write-Host "  编译发布工具 → tools/bin/${rid}/"
    Write-Host "============================================"
    Write-Host ""

    $passed = 0
    $failed = 0

    foreach ($tool in $Tools) {
        $proj = Join-Path $ProjectRoot $tool.Proj
        $display = $tool.Name

        Write-Info "发布 $display ($($tool.Proj))"
        dotnet publish $proj -c Release -r $rid -o $outdir --self-contained 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Pass $display
            $passed++
        } else {
            Write-Fail $display
            $failed++
        }
    }

    Write-Host ""
    Write-Host "============================================"
    Write-Host "  完成: $passed 成功, $failed 失败"
    Write-Host "============================================"
    Write-Host ""

    if ($passed -gt 0) {
        Write-Host "输出目录: $outdir"
        Write-Host ""

        # 创建符号链接 / 复制到 tools/bin/ 供 build 脚本使用
        if ($IsWindows) {
            Write-Info "Windows: 复制可执行文件到 tools/bin/ (无需管理员权限)"
            foreach ($tool in $Tools) {
                $src = Join-Path $outdir "$($tool.Asm).exe"
                if (Test-Path $src) {
                    Copy-Item $src "$OutputBase\$($tool.Name).exe" -Force
                    Copy-Item $src "$OutputBase\$($tool.Asm).exe" -Force
                }
            }
        } else {
            Write-Info "创建符号链接 tools/bin/<name> → tools/bin/${rid}/..."
            foreach ($tool in $Tools) {
                $target = Join-Path $rid "$($tool.Asm)"
                if (Test-Path (Join-Path $outdir $tool.Asm)) {
                    New-Item -ItemType SymbolicLink -Path "$OutputBase\$($tool.Name)" -Target $target -Force | Out-Null
                    New-Item -ItemType SymbolicLink -Path "$OutputBase\$($tool.Asm)" -Target $target -Force | Out-Null
                    Write-Pass "$($tool.Name) ($($tool.Asm))"
                }
            }
        }
        Write-Host ""
        Write-Host "各 build 脚本会自动从 tools/bin/ 查找工具"
    }

    if ($failed -gt 0) { exit 1 }
}

Main

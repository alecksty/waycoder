# ═══════════════════════════════════════════════════════════════
# WayCoder 多模型基准测试 (PowerShell)
# 用法: .\scripts\bench-models.ps1 [-Level easy|medium|hard]
# ═══════════════════════════════════════════════════════════════
param(
    [ValidateSet("easy","medium","hard")]
    [string]$Level = "medium"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoDir = Split-Path -Parent $ScriptDir
$ResultDir = "$RepoDir\bench-results"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

$Tasks = @{
    easy   = "写一个 C# 控制台程序 DiceGame.cs，骰子对战：玩家 vs AI，各掷3骰子比大小，3局2胜，80行以内。用 write_file 写入。"
    medium = "写一个 C# 控制台程序 MazeRunner.cs，迷宫生成(DFS) + WASD移动 + 计时器 + 10个关卡递增难度，300行以内。用 write_file 写入。"
    hard   = "写一个 C# 控制台程序 MiniDB.cs，内存键值数据库：SET/GET/DEL/EXISTS/KEYS/COUNT/SAVE/LOAD + 索引 + 事务日志，400行以内。用 write_file 写入。"
}

$Task = $Tasks[$Level]

$Models = @(
    "deepseek-v4-flash"
    "deepseek-chat"
    "deepseek-v4-pro"
)

# 如果设了 OpenAI 密钥，加入 GPT 模型
if ($env:OPENAI_API_KEY -or $env:GPT_API_KEY) {
    $Models += "gpt-5.4-mini"
}

New-Item -ItemType Directory -Force -Path $ResultDir | Out-Null
Set-Location "$RepoDir\WayCoder"

Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  WayCoder 模型基准测试 (PowerShell)" -ForegroundColor Cyan
Write-Host "  任务级别: $Level" -ForegroundColor Cyan
Write-Host "  模型数: $($Models.Count)" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "🔨 编译 WayCoder..." -ForegroundColor Yellow
dotnet publish -c Release -o "$RepoDir\publish" --nologo -v q 2>&1 | Select-Object -Last 1
Write-Host ""

$Results = @()

foreach ($Model in $Models) {
    $SafeName = $Model.Replace(":", "_").Replace("/", "_")
    $LogFile = "$ResultDir\${Timestamp}_${Level}_${SafeName}.log"

    Write-Host "───────────────────────────────────────────" -ForegroundColor Gray
    Write-Host "🚀 测试: $Model" -ForegroundColor Green
    Write-Host "   日志: $LogFile"

    $StartTime = Get-Date

    try {
        $proc = Start-Process -FilePath "$RepoDir\publish\WayCoder.exe" `
            -ArgumentList "-m", $Model, "-p", $Task, "--yolo" `
            -NoNewWindow -Wait -RedirectStandardOutput $LogFile -RedirectStandardError "$LogFile.err"

        $Elapsed = [math]::Round(((Get-Date) - $StartTime).TotalSeconds, 1)

        $logContent = Get-Content $LogFile -Raw -ErrorAction SilentlyContinue
        if ($logContent -match "错误|失败|Exception|error") {
            $Status = "⚠ 可能有错误"
        } else {
            $Status = "✅ 完成"
        }

        # 检查生成的代码
        $codeLines = ($logContent -split "`n" | Select-String "```" | Measure-Object).Count
    }
    catch {
        $Elapsed = [math]::Round(((Get-Date) - $StartTime).TotalSeconds, 1)
        $Status = "❌ 失败: $_"
    }

    $Results += [PSCustomObject]@{
        Model  = $Model
        Time   = "$($Elapsed)s"
        Status = $Status
        Log    = $LogFile
    }

    Write-Host "   结果: $Status" -ForegroundColor $(if ($Status -like "✅*") { "Green" } else { "Red" })
    Write-Host "   耗时: ${Elapsed}s"
    Write-Host ""
}

# 汇总表格
Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  📊 测试汇总" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
$Results | Format-Table -Property Model, Time, Status -AutoSize
Write-Host "  日志目录: $ResultDir" -ForegroundColor Gray
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan

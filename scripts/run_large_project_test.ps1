# WayCoder 大项目自编程测试
# 测试 WayCoder 独立编写 1 万行 C# 项目的能力
param(
    [string]$ProjectName = "TaskTracker",
    [string]$OutputDir = "D:\code-agents\WayCoder\test_output",
    [int]$TargetLines = 10000,
    [int]$MaxRounds = 200,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$waycoderDir = Resolve-Path "$scriptDir\..\WayCoder"

# 清理输出目录
$projectDir = "$OutputDir\$ProjectName"
if (Test-Path $projectDir) {
    Remove-Item -Recurse -Force $projectDir
}
New-Item -ItemType Directory -Force -Path $projectDir | Out-Null

Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  WayCoder 大项目自编程测试" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  项目: $ProjectName" -ForegroundColor Yellow
Write-Host "  目标: ${TargetLines}+ 行 C# 代码" -ForegroundColor Yellow
Write-Host "  输出: $projectDir" -ForegroundColor Yellow
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ═══════════════════════════════════════════
# 测试提示词（基于 v0.34.1 经验优化）
# ═══════════════════════════════════════════

$prompt = @"
不要读已有代码，直接写文件。跳过探索阶段。

# 任务：创建 C# 任务追踪器应用

在 `$projectDir` 目录下创建一个完整的 C# 任务追踪器应用。

## 架构要求

项目结构（约 35-40 个文件）：
- `Program.cs` — 入口 + CLI 参数解析
- `Models/` — 数据模型 (TaskItem, Project, Tag, Priority, User)
- `Services/` — 业务逻辑 (TaskService, ProjectService, TagService, ExportService)
- `Storage/` — 持久化 (JsonFileStore, SqliteStore, BackupManager)
- `CLI/` — 命令行界面 (CommandParser, CommandRouter, HelpFormatter)
- `Sync/` — 同步引擎 (SyncEngine, ConflictResolver, SyncProtocol)
- `Utils/` — 工具 (IdGenerator, Validator, DateTimeHelper, StringHelper)
- `Config/` — 配置 (AppConfig, ConfigLoader, ConfigValidator)

## 核心数据模型 (Models/)

1. **TaskItem.cs** (~120行): Id, Title, Description, Status(Todo/InProgress/Done/Archived), Priority, CreatedAt, UpdatedAt, DueDate, ProjectId, Tags, ParentTaskId, Recurrence, ReminderAt
2. **Project.cs** (~80行): Id, Name, Description, Color, IsArchived, CreatedAt, TaskCount
3. **Tag.cs** (~60行): Id, Name, Color, UsageCount
4. **Priority.cs** (~40行): 枚举 + 扩展方法 (排序、显示色)
5. **User.cs** (~60行): Id, Name, Email, Preferences, DefaultProjectId

## 存储层 (Storage/)

6. **JsonFileStore.cs** (~350行): JSON 文件读写、CRUD、索引、全文搜索、备份前写入
7. **SqliteStore.cs** (~300行): Microsoft.Data.Sqlite 实现，参数化查询，迁移
8. **BackupManager.cs** (~150行): 自动备份，旋转策略（保留最近 10 个），恢复

## 命令行界面 (CLI/)

9. **CommandParser.cs** (~250行): 解析 `task add "title" --priority high --due 2026-09-01` 等命令
10. **CommandRouter.cs** (~200行): 路由到对应 Service 方法，返回格式化结果
11. **HelpFormatter.cs** (~180行): 生成彩色帮助文本，每命令带示例
12. **OutputFormatter.cs** (~200行): 表格、JSON、CSV 三种输出格式

## 业务服务 (Services/)

13. **TaskService.cs** (~400行): CRUD + 搜索 + 排序 + 过滤 + 批量操作 + 递归子任务
14. **ProjectService.cs** (~200行): 项目管理 + 统计 + 嵌套项目
15. **TagService.cs** (~150行): 标签 CRUD + 自动补全 + 合并
16. **ExportService.cs** (~200行): 导出为 JSON/CSV/iCal/Markdown
17. **ImportService.cs** (~180行): 从 JSON/CSV/Todo.txt 导入
18. **StatisticsService.cs** (~200行): 完成率、趋势图数据、热力图数据、每周报告

## 同步引擎 (Sync/)

19. **SyncEngine.cs** (~350行): 双向同步，CRDT 风格合并，冲突检测
20. **ConflictResolver.cs** (~200行): 冲突策略 (LastWriteWins/Manual/ThreeWay)，冲突记录
21. **SyncProtocol.cs** (~150行): WebSocket 协议定义，消息序列化
22. **SyncClient.cs** (~250行): WebSocket 客户端，自动重连，心跳

## 工具类 (Utils/)

23. **IdGenerator.cs** (~100行): ULID + short-id 两种生成方式
24. **Validator.cs** (~120行): 输入验证（标题长度、日期范围、优先级值等）
25. **DateTimeHelper.cs** (~150行): 相对时间（"3天后"）、ISO 8601、时区转换
26. **StringHelper.cs** (~100行): 截断、高亮匹配、slug 生成
27. **CollectionHelper.cs** (~80行): 分页、批量操作、树形结构遍历

## 配置 (Config/)

28. **AppConfig.cs** (~120行): 所有配置项，环境变量覆盖
29. **ConfigLoader.cs** (~100行): JSON 配置文件加载 + 验证 + 默认值
30. **ConfigValidator.cs** (~80行): 配置项校验（路径存在、值范围等）

## Program.cs

31. **Program.cs** (~250行): 主入口，CLI 模式 + 交互模式 + Web API 模式

## 额外文件

32. **WebApi/Program.cs** (~200行): ASP.NET Core Minimal API
33. **WebApi/TaskEndpoints.cs** (~250行): RESTful CRUD 端点
34. **WebApi/Middleware/ErrorHandling.cs** (~80行)
35. **WebApi/Middleware/RequestLogging.cs** (~80行)
36. **Tests/TaskServiceTests.cs** (~300行): xUnit 风格单元测试
37. **Tests/SyncEngineTests.cs** (~200行)
38. **Tests/CommandParserTests.cs** (~200行)

## 关键约束

- 每个文件必须完整，不依赖"之后补充"
- 使用 System.Text.Json 做序列化
- 文件路径：$projectDir
- 命名空间：$ProjectName
- 所有 public 方法带 XML 文档注释
- 为关键类实现 IEquatable<T> 和 ToString()
"@

if ($DryRun) {
    Write-Host "🔍 干运行模式 — 提示词已生成" -ForegroundColor Green
    Write-Host ""
    Write-Host "提示词长度: $($prompt.Length) 字符" -ForegroundColor Yellow
    Write-Host "预计文件数: ~38" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "运行真实测试: .\run_large_project_test.ps1" -ForegroundColor Cyan
    exit 0
}

# ═══════════════════════════════════════════
# 运行 WayCoder
# ═══════════════════════════════════════════

$startTime = Get-Date
Write-Host "🚀 启动 WayCoder..." -ForegroundColor Green
Write-Host ""

Push-Location $waycoderDir

try {
    # 记录提示词以便恢复
    $promptFile = "$OutputDir\last_prompt.txt"
    $prompt | Out-File -FilePath $promptFile -Encoding UTF8

    # 运行 WayCoder
    $output = & dotnet run -- -p $prompt --max-rounds $MaxRounds --yolo 2>&1
    $exitCode = $LASTEXITCODE

    # 保存完整输出
    $outputFile = "$OutputDir\waycoder_output.txt"
    $output | Out-File -FilePath $outputFile -Encoding UTF8
}
finally {
    Pop-Location
}

$endTime = Get-Date
$duration = $endTime - $startTime

# ═══════════════════════════════════════════
# 收集指标
# ═══════════════════════════════════════════

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  测试结果" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan

Write-Host "  耗时: $($duration.ToString('hh\:mm\:ss'))" -ForegroundColor Yellow
Write-Host "  退出码: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })

if (Test-Path $projectDir) {
    $allFiles = Get-ChildItem -Path $projectDir -Recurse -File -Filter "*.cs"
    $totalFiles = $allFiles.Count
    $totalLines = 0
    $totalChars = 0
    foreach ($f in $allFiles) {
        $content = Get-Content $f.FullName -Encoding UTF8
        $totalLines += $content.Count
        $totalChars += ($content | Measure-Object -Character).Characters
    }

    Write-Host "  .cs 文件数: $totalFiles" -ForegroundColor $(if ($totalFiles -ge 30) { "Green" } else { "Yellow" })
    Write-Host "  代码行数: $totalLines" -ForegroundColor $(if ($totalLines -ge $TargetLines) { "Green" } else { "Yellow" })
    Write-Host "  字符数: $totalChars" -ForegroundColor Gray

    # 文件大小分布
    Write-Host ""
    Write-Host "  文件大小分布:" -ForegroundColor Cyan
    $allFiles | Sort-Object Length -Descending | Select-Object -First 10 | ForEach-Object {
        $lines = (Get-Content $_.FullName -Encoding UTF8).Count
        Write-Host "    $($_.Name.PadRight(30)) $($lines.ToString().PadLeft(5)) 行  $($_.Length.ToString().PadLeft(8)) bytes"
    }

    # 检查编译
    Write-Host ""
    Write-Host "  编译检查:" -ForegroundColor Cyan
    $csprojPath = "$projectDir\$ProjectName.csproj"
    if (-not (Test-Path $csprojPath)) {
        # 创建 project 文件
        @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>$ProjectName</RootNamespace>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
"@ | Out-File -FilePath $csprojPath -Encoding UTF8
    }

    Push-Location $projectDir
    try {
        $buildResult = & dotnet build 2>&1
        $buildExit = $LASTEXITCODE
        if ($buildExit -eq 0) {
            Write-Host "    ✅ 编译成功" -ForegroundColor Green
        } else {
            $errorCount = ($buildResult | Select-String "error CS").Count
            $warningCount = ($buildResult | Select-String "warning CS").Count
            Write-Host "    ❌ 编译失败: $errorCount 错误, $warningCount 警告" -ForegroundColor Red
            # 保存编译日志
            $buildResult | Out-File -FilePath "$OutputDir\build_errors.txt" -Encoding UTF8
        }
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "  ❌ 未找到输出目录" -ForegroundColor Red
}

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  输出文件:" -ForegroundColor Gray
Write-Host "    提示词: $OutputDir\last_prompt.txt" -ForegroundColor Gray
Write-Host "    完整输出: $OutputDir\waycoder_output.txt" -ForegroundColor Gray
Write-Host "    项目代码: $projectDir" -ForegroundColor Gray
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan

# CLAUDE.md

本文件为 Claude Code（claude.ai/code）在此仓库中工作时提供指导。

## 项目概述

WayCoder（道码）是一个中文版易用编程智能体，C# (.NET 10) 实现，AOT 编译为单文件 exe。原名 CoreCoder，因商标冲突更名。

## 常用命令

```bash
# C# 版
cd WayCoder
dotnet publish -c Release            # AOT 编译
dotnet run -- --test                 # 1339 自测
dotnet run -- -p "提示词"            # 一次性模式
dotnet run -- --watch                # Watch 模式 (监听 AI! 注释)
```

## 架构

```
WayCoder/
├── Program.cs         入口 + CLI + REPL (ANSI 全屏 TUI)
├── Agent.cs           主循环
├── AgentSlot.cs       多 Agent 工作区 (F1-F10 槽位切换)
├── LLM.cs             LLM 客户端 (流式 + 回退)
├── ContextManager.cs  Crush 风格上下文管理 (token 追踪 + 自动摘要)
├── SessionManager.cs  会话持久化
├── SystemPrompt.cs    系统提示词 (对标 Crush coder.md.tpl，15 个结构化区块)
├── Config.cs          配置 (.env 加载)
├── WatchMode.cs        Watch 模式 (文件监听 + AI! 注释)
├── PermissionManager.cs 权限确认系统
├── ProjectContext.cs  项目检测 + CLAUDE.md 加载
├── ReviewMode.cs      代码审查模式
├── FallbackLLM.cs     模型回退链
├── MemoryStore.cs     记忆系统 (旧格式, 迁移源)
├── StructuredMemory.cs 结构化记忆 (frontmatter 多文件 + MEMORY.md 索引)
├── BackgroundTask.cs  后台任务
├── DebugLog.cs        调试日志
├── SelfTest.cs        1339+ 项自测
├── FileLockManager.cs 文件锁 (防并发修改冲突)
├── UI/                 终端 TUI 控件库 (19 文件)
│   ├── ScreenManager.cs 全屏缓冲 + 弹窗菜单 + 侧栏
│   ├── SettingsPage.cs  设置界面 (Schema 自动布局)
│   ├── WindowManager.cs 窗口管理器 (Z-order/模态/Toast)
│   ├── InputManager.cs  键盘+鼠标+resize 输入拦截
│   ├── MarkdownRenderer.cs Markdown 解析引擎
│   ├── TuiMarkdown.cs   Markdown→ANSI 渲染
│   ├── DiffRenderer.cs  统一 diff 渲染
│   ├── DiffPreview.cs   diff 预览 + 逐 hunk 确认
│   ├── TuiInput.cs      多行输入区 + 智能提示面板
│   ├── TuiHelper.cs     CJK 宽度计算 + 文本工具
│   ├── TuiColors.cs     统一配色常量
│   ├── TuiBox.cs        对话框
│   ├── TuiTable.cs      表格控件
│   ├── TuiList.cs       列表选单
│   ├── TuiPrompt.cs     输入框
│   ├── TuiProgress.cs   进度条
│   ├── TuiBanner.cs     欢迎横幅
│   ├── BoxBuffer.cs     矩形缓冲区基类
│   └── Gui/            GUI 占位（预留扩展）
├── Edit/               终端源码编辑器 (4 文件)
│   ├── Editor.cs       编辑器引擎 (光标/缓冲/渲染)
│   ├── Syntax.cs       语法高亮 (14 种语言)
│   ├── DiagnosticManager.cs Lint 诊断集成
│   └── Gui/            GUI 编辑器占位（预留扩展）
├── Infra/              基础设施 (7 文件)
│   ├── BashGuard.cs     命令安全防护 (70+ 禁止 + 47 安全白名单)
│   ├── FileTracker.cs   文件追踪 (SHA256 + 变更检测)
│   ├── ErrorLog.cs      统一错误日志 (四级 + 自动轮转)
│   ├── FileIgnoreManager.cs .gitignore + .waycoderignore 规则引擎
│   ├── DesktopNotifier.cs  桌面通知 (终端闪烁 + 响铃 + Toast)
│   ├── PdfExtractor.cs  PDF 文本提取 (PdfPig, AOT 兼容，分页)
│   └── OfficeExtractor.cs  Office 文档提取 (DOCX/XLSX/PPTX, 零依赖)
└── Tools/             34 个工具
    ├── BashTool.cs    GitTool.cs    LspTool.cs
    ├── ReadFileTool.cs FetchTool.cs MemoryTool.cs
    ├── WriteFileTool.cs TodoTool.cs  LintTool.cs
    ├── EditFileTool.cs AgentTool.cs  WebSearchTool.cs
    ├── GlobTool.cs    GrepTool.cs    GitPRTool.cs
    ├── PsTool.cs      KillTool.cs    LsTool.cs
    ├── MkdirTool.cs   RmTool.cs      CdTool.cs
    ├── FindReplaceTool.cs CpTool.cs  MvTool.cs
    ├── DiffTool.cs    TreeTool.cs    WcTool.cs
    ├── StatTool.cs    PwdTool.cs     SkillTool.cs
    ├── DocTool.cs      DownloadTool.cs MultiEditTool.cs
    └── JobOutputTool.cs JobKillTool.cs 后台任务管理
```

## 关键设计决策

- **edit_file 使用唯一子串匹配**，不用行号，安全可审查
- **上下文压缩三层让步**：50% 裁剪 → 70% LLM 摘要 → 90% 硬折叠；Crush 风格真实 token 追踪（AddUsage/ShouldStopAndSummarize），大窗口 20K buffer / 小窗口 20% 比例
- **推理内容处理**：`reasoning_content`（DeepSeek V4）/ `reasoning`（Ollama/qwen）实时显示但不存入对话历史 — 显示=让用户看到思考过程，不存=不污染 API 调用
- **子智能体通过不给 agent 工具来约束**，不靠规则
- **多 Agent 工作区**：F1-F10 切换 10 个独立会话槽位，各占各的屏幕；状态栏 10 数字指示条（白底=当前屏，灰=空闲 绿=工作 黄=等权限 红=出错）；Agent 运行时禁止切换；AgentTool.ParentAgent 切槽位时重绑
- **AOT 编译：JSON 手写序列化**，`JsonHelper.SerializeArgs` 替代 `JsonSerializer`
- **权限系统**：bash/write/edit/agent 默认行内确认（三行黄底渲染），`/perm yolo` 跳过
- **双模型架构**：大模型做复杂任务，小模型做压缩/摘要，自动分工省钱
- **模型回退链**：失败自动尝试备选 deepseek-v4-flash→deepseek-v4-pro→gemini-2.0-flash(免费)→qwen-turbo→glm-4-flash→gpt-5.4-mini，自动解析跨供应商 API Key
- **文件锁**：FileLockManager 防止多 Agent 并发修改冲突，30s 超时自动释放
- **Watch 模式**：FileSystemWatcher 监听文件变更 → 提取 AI! / AI? 注释 → 线程安全队列 → REPL 轮询执行
- **全屏缓冲 UI**：备用屏 + 每帧重绘 + 行内权限块 + 弹窗菜单 + 侧栏面板 + 居中对话框
- **UI 控件库**：`UI/` 目录封装 TUI 控件（未来拆分 Tty 底层 + View 视图），`UI/Gui/` 预留 GUI 扩展
- **结构化记忆**：`.corecoder/memory/*.md` frontmatter 多文件 + MEMORY.md 索引，`memory` 工具与系统提示词注入均走结构化格式，首次使用自动从旧 memory.md 迁移
- **Diff 预览**：`WAYCODER_DIFF_PREVIEW=1` 开启，write_file/edit_file 写前逐 hunk 确认（Y/N/A/Q），非交互模式（管道/重定向/测试）自动跳过
- **Bash 安全防护**：`BashGuard` 三层拦截（命令名 + 参数 + 安全白名单），70+ 禁止命令，47 安全只读命令免确认
- **文件追踪**：`FileTracker` SHA256 哈希记录 + 外部变更检测 + LRU 淘汰，防止 Stale-Read
- **自动续写**：检测"口述代码"（content >300 字符 + 代码标记）→ 追问使其写文件；首轮只分析不动手 → 追问执行
- **自动摘要**：Crush 风格上下文预算检查 → 触发小模型压缩 → 注入继续提示 → 重置计数器
- **文档读取**：PDF 文本提取（PdfPig，AOT 兼容）+ Office 文档提取（DOCX/XLSX/PPTX，ZipArchive + XmlReader 零依赖）+ Markdown 结构化渲染 + CSV 表格解析 + HTML 标签剥离
- **SystemPrompt 对标 Crush**：`$"""` 原始字符串改用无 `$` 前缀+`.Replace()` 注入，避免代码示例中 `{` / `{{` 花括号导致 C# 插值解析错误。15 个结构化 XML 区块覆盖编辑/测试/错误恢复/任务完成完整指南
- **SHA256 循环检测**：每轮对（assistant 消息 + 工具结果）做哈希，8 轮窗口内相同哈希出现 3+ 次触发 3 级递进式反循环提示（换方法→重新评估→严重警告重置）

## 非显而易见的约束

- **孤立的工具消息是非法的**：压缩时必须保持 tool 消息紧跟其 assistant 消息
- **AOT 禁止反射**：不能用 `GetMethod`/`GetType` 等运行时反射
- **Markup 标记**：使用 `«»` 书名号 (`«color»text«/»`)，不与方括号 `[` `]` 冲突，无需双写转义
- **异步上下文**：`AsyncLocal<string>` 替代 `threading.local()` 用于 bash cwd 跟踪

## 添加新工具 (C# 版)

1. 在 `Tools/` 创建类，实现 `ITool` 接口
2. 在 `ToolRegistry.cs` 注册
3. 在 `PermissionManager.cs` 决定是否需要确认
4. 在 `SelfTest.cs` 添加测试

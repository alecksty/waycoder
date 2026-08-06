# CLAUDE.md

本文件为 Claude Code（claude.ai/code）在此仓库中工作时提供指导。

## 项目概述

CoreCoder 是一个极简 AI 编程智能体，C# (.NET 10) 实现，AOT 编译为单文件 exe。

## 常用命令

```bash
# C# 版
cd CoreCoderSharp
dotnet publish -c Release            # AOT 编译
dotnet run -- --test                 # 245 自测
dotnet run -- -p "提示词"            # 一次性模式
```

## 架构

```
CoreCoderSharp/
├── Program.cs         入口 + CLI + REPL (彩色 TUI)
├── Agent.cs           主循环
├── LLM.cs             LLM 客户端 (流式 + 回退)
├── ContextManager.cs  三层上下文压缩
├── SessionManager.cs  会话持久化
├── SystemPrompt.cs    系统提示词 (含项目检测)
├── Config.cs          配置 (.env 加载)
├── PermissionManager.cs 权限确认系统
├── ProjectContext.cs  项目检测 + CLAUDE.md 加载
├── ReviewMode.cs      代码审查模式
├── FallbackLLM.cs     模型回退链
├── MemoryStore.cs     记忆系统
├── BackgroundTask.cs  后台任务
├── DebugLog.cs        调试日志
├── SelfTest.cs        245 项自测
└── Tools/             29 个工具
    ├── BashTool.cs    GitTool.cs    LspTool.cs
    ├── ReadFileTool.cs FetchTool.cs MemoryTool.cs
    ├── WriteFileTool.cs TodoTool.cs  LintTool.cs
    ├── EditFileTool.cs AgentTool.cs  WebSearchTool.cs
    ├── GlobTool.cs    GrepTool.cs    GitPRTool.cs
    ├── PsTool.cs      KillTool.cs    LsTool.cs
    ├── MkdirTool.cs   RmTool.cs      CdTool.cs
    ├── FindReplaceTool.cs CpTool.cs  MvTool.cs
    ├── DiffTool.cs    TreeTool.cs    WcTool.cs
    ├── StatTool.cs    PwdTool.cs
```

## 关键设计决策

- **edit_file 使用唯一子串匹配**，不用行号，安全可审查
- **上下文压缩三层让步**：50% 裁剪 → 70% LLM 摘要 → 90% 硬折叠
- **子智能体通过不给 agent 工具来约束**，不靠规则
- **AOT 编译：JSON 手写序列化**，`JsonHelper.SerializeArgs` 替代 `JsonSerializer`
- **权限系统**：bash/write/edit/agent 默认需确认，`/perm yolo` 跳过
- **模型回退链**：deepseek-v4-flash → deepseek-v4-pro → gpt-5.4-mini

## 非显而易见的约束

- **孤立的工具消息是非法的**：压缩时必须保持 tool 消息紧跟其 assistant 消息
- **AOT 禁止反射**：不能用 `GetMethod`/`GetType` 等运行时反射
- **Spectre.Console 标记**：`[` `]` 会被解析为格式标签，中文方括号需双写 `[[选项]]`
- **异步上下文**：`AsyncLocal<string>` 替代 `threading.local()` 用于 bash cwd 跟踪

## 添加新工具 (C# 版)

1. 在 `Tools/` 创建类，实现 `ITool` 接口
2. 在 `ToolRegistry.cs` 注册
3. 在 `PermissionManager.cs` 决定是否需要确认
4. 在 `SelfTest.cs` 添加测试

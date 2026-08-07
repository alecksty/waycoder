# WayCoder（道码）竞品分析与差异化策略

> v0.17.4 | 2026-08-07

## 一、竞品概况

| 产品 | 技术栈 | 部署 | 定价 | 核心优势 |
|------|--------|------|------|---------|
| **Claude Code** | Node.js/TypeScript | npm 全局安装 | Claude API 按量 | 深度推理、大上下文、Agent Teams |
| **Codex CLI** | Rust | npm/brew 安装 | ChatGPT Plus 免费 | 开源、高性能、三级沙箱 |
| **Aider** | Python | pip 安装 | 自带模型 | 最成熟开源、Git 原生、100+ 模型 |
| **Cursor** | Electron/TypeScript | 安装包 | $20/月 | GUI 体验、LSP 深度集成 |
| **GitHub Copilot** | - | IDE 插件 | $10/月 | IDE 深度集成 |
| **Goose** | Rust | brew/cargo 安装 | 自带模型 | Linux 基金会治理、15+ 提供商 |
| **Gemini CLI** | TS/Go | npm 安装 | Gemini API | 1M 上下文（2026.6 停止免费） |
| **WayCoder** | **C# .NET 10 AOT** | **单文件 exe (~8MB)** | **自带模型** | **零依赖、瞬时启动、中文原生、29 工具** |

## 二、WayCoder 差异化优势

### 2.1 部署优势（所有竞品做不到）

```
竞品: npm install -g / cargo install / pip install / 下载安装包
我们:  scp waycoder.exe server:/usr/local/bin/  ← 一个文件，3 秒搞定
```

| 指标 | WayCoder | Claude Code | Codex CLI | Aider | Cursor |
|------|----------|-------------|-----------|-------|--------|
| 文件大小 | ~8MB AOT | ~200MB (node_modules) | ~30MB | ~100MB (依赖) | ~500MB |
| 启动时间 | <0.1s | ~1s | ~0.5s | ~0.5s | ~3s |
| 内存占用 | ~50MB | ~200MB | ~100MB | ~150MB | ~500MB |
| 运行依赖 | **零** | Node.js 18+ | 无 | Python 3.10+ | 安装包 |
| 离线可用 | ✅ AOT | ❌ | ✅ | ❌ | ❌ |

### 2.2 中文原生适配

- CJK 宽度感知（全角/半角自动兼容）
- 中文系统提示词 + 错误消息 + UI 全部中文
- 中文语法高亮支持
- 竞品普遍英文优先，中文排版经常错位

### 2.3 独有功能矩阵

| 功能 | WayCoder | Claude Code | Codex CLI | Aider | Goose |
|------|:---:|:---:|:---:|:---:|:---:|
| AOT 单文件 0 依赖 | ✅ | ❌ | ❌ | ❌ | ❌ |
| 内置 TUI 编辑器 (14 语言) | ✅ | ❌ | ❌ | ❌ | ❌ |
| 编辑器实时 Lint 诊断 | ✅ 10+ linter | ❌ | ❌ | ❌ | ❌ |
| 双模型自动分工省钱 | ✅ | ❌ | ❌ | ❌ | ❌ |
| 全屏缓冲 TUI 控件库 | ✅ | ❌ | ❌ | ❌ | ❌ |
| 中文原生 | ✅ | ❌ | ❌ | ❌ | ❌ |
| 文件锁机制（多 Agent 并发安全） | ✅ | ❌ | ❌ | ❌ | ❌ |
| 380 项内置自测 | ✅ | ❌ | ❌ | ❌ | ❌ |
| Git 自动提交 | ✅ | ❌ | ✅ | ✅ | ❌ |
| Watch 模式 (AI! 注释) | ✅ | ❌ | ❌ | ✅ | ❌ |
| 会话自动保存/恢复 | ✅ | ✅ | ❌ | ❌ | ❌ |
| `/stats` 用量统计面板 | ✅ | ✅ | ❌ | ❌ | ❌ |
| Checkpoint 持久化 | ✅ | ❌ | ❌ | ❌ | ❌ |
| Diff-based Code Review | ✅ | ❌ | ✅ | ❌ | ❌ |
| CJK 感知 Token 估算 | ✅ | ❌ | ❌ | ❌ | ❌ |
| 沙箱执行 | ✅ 三级 | ❌ | ✅ 三级 | ❌ | ❌ |
| MCP HTTP/SSE 传输 | ✅ | ✅ 800+ | ✅ | ❌ | ✅ 原生 |
| 模型回退链 | ✅ | ✅ | ❌ | ❌ | ❌ |
| 自动 Test 修复循环 | ✅ | ✅ | ✅ | ✅ | ❌ |
| Prompt 缓存 | ✅ SHA256 | ✅ | ✅ 75% | ❌ | ❌ |
| 彩色多角色聊天 TUI | ✅ v2 | ❌ | ❌ | ❌ | ❌ |
| 侧栏面板 (任务/文件/锁/MCP) | ✅ | ❌ | ❌ | ❌ | ❌ |

## 三、仍需补齐的差距

### 🔴 P0 — 严重差距

> ✅ **已全部清零！**

### 🟠 P1 — 中等差距

| 差距 | 竞品参考 | 说明 |
|------|---------|------|
| **语义记忆** | Claude Code Memory | 当前是关键词匹配，缺少向量相似度搜索 |

### 🟡 P2 — 轻度差距

| 差距 | 说明 |
|------|------|
| IDE 插件集成 | 仅 Watch 模式作为桥接 |
| 多模态 (图片/音频) | Codex CLI、Gemini CLI 已支持 |

### ✅ 已清零的差距

| 原差距 | 清零版本 | 说明 |
|--------|:---:|------|
| 自动 Test 修复循环 | v0.16.3 | `AppendTestFeedbackAsync` + 6 种构建系统 + 60s 防抖 |
| Prompt 缓存 | v0.17.0 | SHA256 本地追踪 + /stats 展示 |
| 沙箱执行 | v0.17.1 | 三级沙箱 (suggest/auto-edit/full-auto) + 环境清理 + 内存监控 |
| TUI 编辑器 Lint 诊断 | v0.17.2 | 保存时自动 lint + gutter 指示器 + 错误行高亮 + 10+ linter |
| MCP HTTP/SSE 传输 | v0.17.3 | 传输抽象层 + HTTP POST + SSE 流 + 工具发现缓存 (24h TTL) |
| Terminal.Gui v2 TUI | v0.17.4 | 彩色多角色聊天 + 侧栏面板 + 输入历史 + 会话恢复 |
| 移除 Terminal.Gui v2 恢复 AOT | v0.17.5 | 回退 ANSI TUI，恢复单文件 NativeAOT 编译 |
| 子智能体递归 | v0.17.5 | 多层嵌套子智能体 + AsyncLocal 深度追踪 + 可配置深度 |
| `/undo` 按文件恢复 | v0.17.5 | CheckpointManager 支持 filePath 参数 + 文件锁检查 |
| Lint/Tool 超时可配置 | v0.17.5 | ToolTimeoutSec + LintTimeoutSec 配置项 + 环境变量 |

## 四、核心指标对比

| 指标 | WayCoder | Claude Code | Codex CLI | Aider | Cursor |
|------|----------|-------------|-----------|-------|--------|
| 自测覆盖 | 380 项 | - | - | - | - |
| 工具数量 | 29 个 | 20+ | 15+ | ~10 | IDE 内置 |
| 语言高亮 | 14 种 | - | - | - | 全 IDE |
| Lint 诊断 | 10+ 种 | - | - | - | 全 IDE |
| 启动速度 | ★★★★★ | ★★★ | ★★★★ | ★★★ | ★★ |
| 中文体验 | ★★★★★ | ★★★ | ★★ | ★★ | ★★★ |
| 零依赖 | ★★★★★ | ★ | ★★★★ | ★★ | ★ |
| 终端体验 | ★★★★★ | ★★★★★ | ★★★★★ | ★★★★ | ★★★ |
| 并发安全 | ★★★★★ | ★★★ | ★★★ | ★★★ | ★★★ |
| 代码自愈 | ★★★★ (lint+test) | ★★★★★ | ★★★★★ | ★★★★★ | ★★★★ |
| 沙箱安全 | ★★★★ 三级 | ★★ | ★★★★★ 三级 | ★★ | ★★★ |
| MCP 生态 | ★★★★ HTTP+SSE | ★★★★★ 800+ | ★★★ | ★ | ★★★★★ |

## 五、一句话定位

> **WayCoder = 零依赖瞬时启动的中文版 AI 编程助手，scp 一个 exe 到服务器就能用，内置双模型省钱 + 29 个工具 + ANSI 全屏 TUI 开箱即用。**

---

## 附录：2026.08 CLI AI Coding Agent 格局

```
第一梯队（全功能旗舰）:
  Claude Code ── 最强 Agent 自主性，200K→1M 上下文，Teams 子智能体，闭源
  Codex CLI   ── Rust 高性能，开源，ChatGPT Plus 免费带，增长最快

第二梯队（成熟开源）:
  Aider       ── Git 原生，100+ 模型随意换，最成熟开源方案
  Goose       ── Linux 基金会，15+ 提供商，但编码正确率最低

第三梯队（差异化解）:
  WayCoder    ── 零依赖单文件 AOT，中文原生，双模型省钱，ANSI 全屏 TUI
                 唯一内置编辑器 + lint 诊断 + 沙箱的 CLI Agent
  Gemini CLI  ── 曾免费 1M 上下文，2026.6 停止免费后被 Antigravity 取代
```
